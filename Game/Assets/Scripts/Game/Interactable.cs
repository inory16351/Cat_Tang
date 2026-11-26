// E로 상호작용하는 것들: 문(다음 방 · 보상 표시) · 상인 · 마을 시설 · 챕터 문
using System.Collections.Generic;
using UnityEngine;

namespace CatTang
{
    public abstract class Interactable : MonoBehaviour
    {
        public static readonly List<Interactable> All = new();
        public float radius = 2.2f;
        public string prompt = "살펴보기";
        public virtual bool Available => true;

        protected virtual void OnEnable() => All.Add(this);
        protected virtual void OnDisable() => All.Remove(this);
        public abstract void Interact(PlayerController p);

        /// <summary>월드에 뜨는 글 (제목 글꼴)</summary>
        public static TextMesh WorldLabel(Transform parent, string text, Vector3 localPos, float size, Color color)
        {
            UIKit.Init();
            var go = new GameObject("Label");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            var tm = go.AddComponent<TextMesh>();
            tm.font = UIKit.Title;
            go.GetComponent<MeshRenderer>().sharedMaterial = UIKit.Title.material;
            tm.text = Loc.T(text);                                  // 다국어 (Loc)
            tm.fontSize = 64;
            tm.characterSize = size;
            tm.anchor = TextAnchor.MiddleCenter;
            tm.alignment = TextAlignment.Center;
            tm.color = color;
            go.AddComponent<Billboard>();
            return tm;
        }

        /// <summary>월드에 뜨는 아이콘 쿼드 (Resources 텍스처)</summary>
        public static GameObject WorldIcon(Transform parent, string res, Vector3 localPos, float size)
        {
            var tex = Resources.Load<Texture2D>(res);
            var go = GameObject.CreatePrimitive(PrimitiveType.Quad);
            Destroy(go.GetComponent<Collider>());
            go.name = "Icon";
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            float aspect = tex ? (float)tex.width / tex.height : 1;
            go.transform.localScale = new Vector3(size * aspect, size, 1);
            var mat = new Material(Shader.Find("Sprites/Default"));
            mat.mainTexture = tex;
            go.GetComponent<MeshRenderer>().sharedMaterial = mat;
            go.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            go.AddComponent<Billboard>();
            return go;
        }
    }

    public enum RewardKind { Gold, Gun, Bullet, Op, Shard, Boss, Armor }

}
