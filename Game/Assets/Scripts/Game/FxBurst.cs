// 월드 이펙트: CatTang/FX 셰이더를 쓰는 카메라 정면 쿼드. _Progress를 0→1로 흘려 보냄.
using UnityEngine;

namespace CatTang
{
    public class FxBurst : MonoBehaviour
    {
        public enum Kind { Muzzle, Hit, Explosion, Gold }
        static Material baseMat;
        static readonly int ProgressId = Shader.PropertyToID("_Progress");
        static readonly int ModeId = Shader.PropertyToID("_Mode");
        static readonly int SeedId = Shader.PropertyToID("_Seed");
        static readonly int TintId = Shader.PropertyToID("_Tint");

        MeshRenderer mr;
        MaterialPropertyBlock mpb;
        float born, life;

        public static void Spawn(Vector3 pos, float size, Kind kind) => Spawn(pos, size, kind, Color.white);

        /// <summary>tint: 탄 색 (흰색이면 원래 색)</summary>
        public static void Spawn(Vector3 pos, float size, Kind kind, Color tint)
        {
            if (baseMat == null)
            {
                var sh = Shader.Find("CatTang/FX");
                if (sh == null) return;
                baseMat = new Material(sh);
            }
            var go = GameObject.CreatePrimitive(PrimitiveType.Quad);
            go.name = "FX_" + kind;
            Destroy(go.GetComponent<Collider>());
            go.transform.position = pos;
            go.transform.localScale = Vector3.one * size;
            var f = go.AddComponent<FxBurst>();
            f.mr = go.GetComponent<MeshRenderer>();
            f.mr.sharedMaterial = baseMat;
            f.mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            f.mpb = new MaterialPropertyBlock();
            f.mpb.SetFloat(ModeId, kind == Kind.Gold ? 2f : 0f);
            f.mpb.SetFloat(SeedId, Random.Range(0f, 100f));
            f.mpb.SetColor(TintId, tint);
            f.life = kind switch { Kind.Muzzle => 0.18f, Kind.Hit => 0.3f, Kind.Explosion => 0.55f, _ => 0.5f };
            f.born = Time.time;
            f.Face();
        }

        void Face()
        {
            var cam = Camera.main;
            if (cam) transform.rotation = cam.transform.rotation;
        }

        void Update()
        {
            float k = (Time.time - born) / life;
            if (k >= 1) { Destroy(gameObject); return; }
            mpb.SetFloat(ProgressId, k);
            mr.SetPropertyBlock(mpb);
            Face();
        }
    }
}
