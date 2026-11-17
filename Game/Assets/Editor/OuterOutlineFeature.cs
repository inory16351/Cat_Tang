// 적 바깥 테두리용 렌더러 기능 (2026-11-17): 툰 셰이더의 "CatTangOuterOutline" 패스를 불투명 다음에 그림.
// 몸 · 외곽선이 그린 스텐실 바깥에만 색 띠가 생겨 형체 안쪽(팔 · 몸 겹친 곳)에는 안 생김.
// 메뉴를 다시 눌러도 이미 있으면 안 더함.
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace CatTang.EditorTools
{
    public static class OuterOutlineFeature
    {
        const string RendererPath = "Assets/Settings/PC_Renderer.asset";
        const string FeatureName = "적 바깥 테두리 (CatTangOuterOutline)";

        [MenuItem("CatTang/적 바깥 테두리 렌더러 기능 추가")]
        public static string Add()
        {
            var data = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(RendererPath);
            if (!data) return "렌더러 없음";
            foreach (var f in data.rendererFeatures)
                if (f && f.name == FeatureName) return "이미 있음";
            var ro = ScriptableObject.CreateInstance<RenderObjects>();
            ro.name = FeatureName;
            ro.settings.passTag = "CatTangOuterOutline";
            ro.settings.Event = RenderPassEvent.AfterRenderingOpaques;
            ro.settings.filterSettings.RenderQueueType = RenderQueueType.Opaque;
            ro.settings.filterSettings.LayerMask = ~0;
            ro.settings.filterSettings.PassNames = new[] { "CatTangOuterOutline" };
            AssetDatabase.AddObjectToAsset(ro, data);
            data.rendererFeatures.Add(ro);
            // 내부 맵(이름 → id)도 갱신해야 직렬화가 맞음
            var so = new SerializedObject(data);
            var map = so.FindProperty("m_RendererFeatureMap");
            if (map != null)
            {
                map.arraySize++;
                AssetDatabase.TryGetGUIDAndLocalFileIdentifier(ro, out _, out long id);
                map.GetArrayElementAtIndex(map.arraySize - 1).longValue = id;
                so.ApplyModifiedProperties();
            }
            data.SetDirty();
            EditorUtility.SetDirty(data);
            AssetDatabase.SaveAssets();
            return "추가함";
        }
    }
}
