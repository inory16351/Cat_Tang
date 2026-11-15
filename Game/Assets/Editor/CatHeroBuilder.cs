// 주인공 고양이 프리팹 만들기: Tripo 모델(FBX) + 얼굴 가중치 고친 메시 + 툰 재질 + 애니메이터 + 양손 리볼버 + KataPoser
// 메뉴: Cat_Tang/Build Cat Hero Prefab  →  Assets/Art/Characters/CatHero/CatHero.prefab
using UnityEditor;
using UnityEngine;

namespace CatTang.EditorTools
{
    public static class CatHeroBuilder
    {
        const string Dir = "Assets/Art/Characters/CatHero/";
        public const string PrefabPath = Dir + "CatHero.prefab";
        public const float ModelScale = 1.8f;           // 모델 원본 키 약 1m → 1.76m

        [MenuItem("Cat_Tang/Build Cat Hero Prefab")]
        public static void Build()
        {
            var fbx = AssetDatabase.LoadAssetAtPath<GameObject>(Dir + "CatHero.fbx");
            var fixedMesh = AssetDatabase.LoadAssetAtPath<Mesh>(Dir + "CatHero_FixedWeights.asset");
            var mat = AssetDatabase.LoadAssetAtPath<Material>(Dir + "CatHero_Toon.mat");
            var ctrl = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(Dir + "CatHero.controller");
            if (!fbx) { Debug.LogError("[CatHero] FBX 없음"); return; }

            var root = new GameObject("CatHero");
            var model = (GameObject)PrefabUtility.InstantiatePrefab(fbx);
            PrefabUtility.UnpackPrefabInstance(model, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            model.name = "Body";
            model.transform.SetParent(root.transform, false);
            model.transform.localScale = Vector3.one * ModelScale;

            var smr = model.GetComponentInChildren<SkinnedMeshRenderer>();
            if (fixedMesh) smr.sharedMesh = fixedMesh;
            if (mat) smr.sharedMaterial = mat;
            smr.updateWhenOffscreen = true;

            var anim = model.GetComponent<Animator>();
            if (!anim) anim = model.AddComponent<Animator>();
            anim.runtimeAnimatorController = ctrl;
            anim.applyRootMotion = false;
            anim.cullingMode = AnimatorCullingMode.AlwaysAnimate;

            // 양손 리볼버 (도형, 모델링 전 임시) — 손 위치에 붙고 방향은 KataPoser가 매 프레임 맞춤
            var gold = SceneMat("CatHero_GunGold", new Color(0.9f, 0.68f, 0.28f));
            var dark = SceneMat("CatHero_GunDark", new Color(0.22f, 0.16f, 0.14f));
            var grip = SceneMat("CatHero_GunGrip", new Color(0.45f, 0.26f, 0.16f));
            var rHand = anim.GetBoneTransform(HumanBodyBones.RightHand);
            var lHand = anim.GetBoneTransform(HumanBodyBones.LeftHand);
            var gunFbx = AssetDatabase.LoadAssetAtPath<GameObject>(GunFbx);
            Transform mR, mL;
            var gR = gunFbx ? GunModel("Gun_R", root.transform, gunFbx, out mR) : Revolver("Gun_R", root.transform, gold, dark, grip, out mR);
            var gL = gunFbx ? GunModel("Gun_L", root.transform, gunFbx, out mL) : Revolver("Gun_L", root.transform, gold, dark, grip, out mL);
            gR.position = rHand.position; gL.position = lHand.position;

            var poser = root.AddComponent<KataPoser>();
            poser.anim = anim;
            poser.gunR = gR; poser.gunL = gL; poser.muzzleR = mR; poser.muzzleL = mL;

            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            Object.DestroyImmediate(root);
            AssetDatabase.SaveAssets();
            Debug.Log("[CatHero] 프리팹 저장: " + PrefabPath);
        }

        // Tripo 리볼버 (보안관의 리볼버): 원본 메시는 총구 = -X, 윗면 = +Y, 손잡이 = +X 아래쪽
        const string GunFbx = "Assets/Art/Props/Guns/gun_2001.fbx";
        static readonly Vector3 GunGripPoint = new Vector3(0.27f, 0.22f, 0f);     // 원본 메시 기준 손잡이 가운데 (주먹이 쥐는 곳)
        static readonly Vector3 GunMuzzlePoint = new Vector3(-0.5f, 0.6f, 0f);    // 원본 메시 기준 총구 끝
        public const float GunScale = 0.56f;                                     // 원본 길이 약 1m → 0.55m

        // 총 원점 = 손잡이 가운데, +z = 총구 방향, +y = 윗면
        static Transform GunModel(string name, Transform parent, GameObject fbx, out Transform muzzle)
        {
            var g = new GameObject(name).transform;
            g.SetParent(parent, false);
            var mesh = (GameObject)PrefabUtility.InstantiatePrefab(fbx);
            PrefabUtility.UnpackPrefabInstance(mesh, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            mesh.name = "Mesh";
            mesh.transform.SetParent(g, false);
            var rot = Quaternion.Euler(0, 90, 0);                                  // -X → +Z
            mesh.transform.localRotation = rot;
            mesh.transform.localScale = Vector3.one * GunScale;
            mesh.transform.localPosition = -(rot * GunGripPoint) * GunScale;
            var mat = GunMat();
            foreach (var r in mesh.GetComponentsInChildren<MeshRenderer>()) { r.sharedMaterial = mat; r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On; }
            muzzle = new GameObject("Muzzle").transform;
            muzzle.SetParent(g, false);
            muzzle.localPosition = rot * (GunMuzzlePoint - GunGripPoint) * GunScale + new Vector3(0, 0, 0.02f);
            return g;
        }

        static Material GunMat()
        {
            string p = Dir + "Gun2001_Toon.mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(p);
            if (!m) { m = new Material(Shader.Find("CatTang/Toon")); AssetDatabase.CreateAsset(m, p); }
            m.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Art/Props/Guns/gun_2001_basecolor.png"));
            m.SetColor("_BaseColor", Color.white);
            m.SetColor("_ShadeColor", new Color(0.75f, 0.62f, 0.55f));
            m.SetColor("_SecondShadeColor", new Color(0.5f, 0.38f, 0.34f));
            if (m.HasProperty("_OutlineWidth")) m.SetFloat("_OutlineWidth", 2f);
            if (m.HasProperty("_RimStrength")) m.SetFloat("_RimStrength", 0.45f);
            EditorUtility.SetDirty(m);
            return m;
        }

        static Material SceneMat(string name, Color c)
        {
            string p = Dir + name + ".mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(p);
            if (!m) { m = new Material(Shader.Find("CatTang/Toon")); AssetDatabase.CreateAsset(m, p); }
            m.SetColor("_BaseColor", c);
            if (m.HasProperty("_OutlineWidth")) m.SetFloat("_OutlineWidth", 3f);
            if (m.HasProperty("_RimStrength")) m.SetFloat("_RimStrength", 0.5f);
            EditorUtility.SetDirty(m);
            return m;
        }

        // 리볼버: +z = 총구 방향, 원점 = 손잡이 윗부분(손 안)
        static Transform Revolver(string name, Transform parent, Material gold, Material dark, Material grip, out Transform muzzle)
        {
            var g = new GameObject(name).transform;
            g.SetParent(parent, false);
            void P(PrimitiveType t, string n, Vector3 pos, Vector3 scale, Material m, Vector3 euler)
            {
                var o = GameObject.CreatePrimitive(t);
                Object.DestroyImmediate(o.GetComponent<Collider>());
                o.name = n;
                o.transform.SetParent(g, false);
                o.transform.localPosition = pos;
                o.transform.localScale = scale;
                o.transform.localEulerAngles = euler;
                o.GetComponent<MeshRenderer>().sharedMaterial = m;
            }
            P(PrimitiveType.Cylinder, "Barrel", new Vector3(0, 0.06f, 0.2f), new Vector3(0.05f, 0.16f, 0.05f), gold, new Vector3(90, 0, 0));
            P(PrimitiveType.Cylinder, "Drum", new Vector3(0, 0.05f, 0.06f), new Vector3(0.1f, 0.05f, 0.1f), dark, new Vector3(90, 0, 0));
            P(PrimitiveType.Cube, "Frame", new Vector3(0, 0.05f, 0.03f), new Vector3(0.05f, 0.08f, 0.16f), gold, Vector3.zero);
            P(PrimitiveType.Cube, "Grip", new Vector3(0, -0.04f, -0.04f), new Vector3(0.045f, 0.13f, 0.06f), grip, new Vector3(-18, 0, 0));
            muzzle = new GameObject("Muzzle").transform;
            muzzle.SetParent(g, false);
            muzzle.localPosition = new Vector3(0, 0.06f, 0.37f);
            g.localScale = Vector3.one * 1.6f;        // 2.5등신 비율에 맞춰 큼직하게 (원화처럼)
            return g;
        }
    }
}
