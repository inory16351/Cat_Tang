// 코스튬 모델 프리팹 만들기 (2026-11-24): Hyper3D(Rodin) 모델 + human 리깅 FBX → 휴머노이드 아바타 → 주인공과 같은 애니메이터 · 양손 리볼버 · KataPoser
// 원본: Art/models/incoming/hyper3d_costume/<Id>/ (원화 Art/tripo_input/costume/cos_<Id>_apose.png, ChatGPT)
//   → Assets/Art/Characters/CatHero/Costumes/<ModelKey>/<ModelKey>.fbx + _basecolor.png
// 결과: Resources/Costumes/<ModelKey>.prefab — PlayerController가 입은 코스튬의 ModelKey로 찾아 기본 모델과 바꿔 끼움
// 크기 · 비율: 블렌더에서 원본(CatHero) 신체 비율을 재서 머리 · 팔 뼈를 키우고 귀 끝 높이를 맞춰 구운 FBX를 씀 → 여기선 주인공 ModelScale만
//   (2026-11-24 사용자: 신체 비율 측정 후 조정 · 키 원본과 같게. 측정 Art/tools/blender_measure_body.py · blender_proportion_sheet.py)
// 메뉴: Cat_Tang/코스튬 모델 프리팹 만들기
using System.IO;
using UnityEditor;
using UnityEngine;

namespace CatTang.EditorTools
{
    public static class CostumeModelBuilder
    {
        const string SrcDir = "Assets/Art/Characters/CatHero/Costumes/";
        const string OutDir = "Assets/Resources/Costumes/";
        const string HeroDir = "Assets/Art/Characters/CatHero/";

        [MenuItem("Cat_Tang/코스튬 모델 프리팹 만들기")]
        public static void BuildAll()
        {
            Directory.CreateDirectory(OutDir);
            var hero = AssetDatabase.LoadAssetAtPath<GameObject>(CatHeroBuilder.PrefabPath);
            float heroHead = HeadHeight(hero);
            int made = 0;
            foreach (var dir in Directory.GetDirectories(SrcDir))
            {
                string key = Path.GetFileName(dir);
                if (Build(key, heroHead)) made++;
            }
            AssetDatabase.SaveAssets();
            Debug.Log($"[Costume] 프리팹 {made}개 → {OutDir}");
        }

        /// <summary>프리팹 안 머리 뼈의 높이 (월드, 루트 기준)</summary>
        static float HeadHeight(GameObject prefab)
        {
            if (!prefab) return 1.4f;
            var inst = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            var a = inst.GetComponentInChildren<Animator>();
            float h = 1.4f;
            if (a && a.isHuman) { var head = a.GetBoneTransform(HumanBodyBones.Head); if (head) h = head.position.y - inst.transform.position.y; }
            Object.DestroyImmediate(inst);
            return h;
        }

        static bool Build(string key, float heroHead)
        {
            string fbxPath = SrcDir + key + "/" + key + ".fbx";
            if (!(AssetImporter.GetAtPath(fbxPath) is ModelImporter imp)) { Debug.Log($"[Costume] {key} FBX 없음 — 건너뜀"); return false; }
            if (imp.animationType != ModelImporterAnimationType.Human || imp.materialImportMode != ModelImporterMaterialImportMode.None)
            {
                imp.animationType = ModelImporterAnimationType.Human;
                imp.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
                imp.materialImportMode = ModelImporterMaterialImportMode.None;
                imp.SaveAndReimport();
            }
            var fbx = AssetDatabase.LoadAssetAtPath<GameObject>(fbxPath);
            if (!fbx) return false;

            var root = new GameObject(key);
            var model = (GameObject)PrefabUtility.InstantiatePrefab(fbx);
            PrefabUtility.UnpackPrefabInstance(model, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            model.name = "Body";
            model.transform.SetParent(root.transform, false);

            var anim = model.GetComponent<Animator>();
            if (!anim) anim = model.AddComponent<Animator>();
            if (!anim.isHuman) Debug.LogWarning($"[Costume] {key}: 휴머노이드 아바타가 아님 — 애니메이션이 안 맞을 수 있음");
            anim.runtimeAnimatorController = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(HeroDir + "CatHero.controller");
            anim.applyRootMotion = false;
            anim.cullingMode = AnimatorCullingMode.AlwaysAnimate;

            // 키 · 비율: FBX가 이미 블렌더에서 주인공과 같은 단위 · 비율로 구워져 있음 (Art/tools/blender_costume_proportion.py,
            //   머리 · 팔 뼈 배율 + 귀 끝 높이 맞춤, 배율 기록 Art/models/incoming/hyper3d_costume/proportion_fit.json) → 주인공과 같은 ModelScale만 곱함
            model.transform.localScale = Vector3.one * CatHeroBuilder.ModelScale;
            var head = anim.isHuman ? anim.GetBoneTransform(HumanBodyBones.Head) : null;
            if (head && heroHead > 0.01f)
            {
                float h = head.position.y - root.transform.position.y;
                if (h < heroHead * 0.5f || h > heroHead * 2f) Debug.LogWarning($"[Costume] {key}: 머리 높이 {h:0.##} (주인공 {heroHead:0.##}) — 단위가 어긋났을 수 있음");
            }
            // 발바닥을 땅에
            var smrs = model.GetComponentsInChildren<SkinnedMeshRenderer>();
            float minY = float.MaxValue;
            foreach (var s in smrs) { s.updateWhenOffscreen = true; minY = Mathf.Min(minY, s.bounds.min.y); }
            if (minY < float.MaxValue) model.transform.localPosition -= new Vector3(0, minY - root.transform.position.y, 0);

            // 툰 재질: 주인공 재질을 복사해 기본색 그림만 바꿈
            var mat = ToonMat(key);
            foreach (var s in smrs) s.sharedMaterial = mat;

            // 양손 리볼버 + KataPoser: 주인공 프리팹의 총을 그대로 복사해 이 모델 손에
            var heroPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(CatHeroBuilder.PrefabPath);
            var heroPoser = heroPrefab ? heroPrefab.GetComponent<KataPoser>() : null;
            var poser = root.AddComponent<KataPoser>();
            poser.anim = anim;
            if (heroPoser && heroPoser.gunR && heroPoser.gunL)
            {
                var gR = Object.Instantiate(heroPoser.gunR.gameObject, root.transform).transform; gR.name = "Gun_R";
                var gL = Object.Instantiate(heroPoser.gunL.gameObject, root.transform).transform; gL.name = "Gun_L";
                var rh = anim.GetBoneTransform(HumanBodyBones.RightHand); var lh = anim.GetBoneTransform(HumanBodyBones.LeftHand);
                if (rh) gR.position = rh.position; if (lh) gL.position = lh.position;
                poser.gunR = gR; poser.gunL = gL;
                poser.muzzleR = gR.Find("Muzzle"); poser.muzzleL = gL.Find("Muzzle");
            }

            float scale = model.transform.localScale.x;
            PrefabUtility.SaveAsPrefabAsset(root, OutDir + key + ".prefab");
            Object.DestroyImmediate(root);                         // model도 같이 사라짐 → 배율은 미리 읽어 둠
            Debug.Log($"[Costume] {key} 프리팹 저장 (배율 {scale:0.###})");
            return true;
        }

        static Material ToonMat(string key)
        {
            string p = SrcDir + key + "/" + key + "_Toon.mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(p);
            if (!m)
            {
                var heroMat = AssetDatabase.LoadAssetAtPath<Material>(HeroDir + "CatHero_Toon.mat");
                m = heroMat ? new Material(heroMat) : new Material(Shader.Find("CatTang/Toon"));
                AssetDatabase.CreateAsset(m, p);
            }
            var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(SrcDir + key + "/" + key + "_basecolor.png");
            if (tex) { m.SetTexture("_BaseMap", tex); if (m.HasProperty("_MainTex")) m.SetTexture("_MainTex", tex); }
            foreach (var n in new[] { "_NormalMap", "_BumpMap", "_MetallicMap", "_RoughnessMap" }) if (m.HasProperty(n)) m.SetTexture(n, null);
            EditorUtility.SetDirty(m);
            return m;
        }
    }
}
