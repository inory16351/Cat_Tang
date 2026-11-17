// 경험치 보석 (세피리아식): 적이 쓰러진 자리에서 튀어나와 잠깐 흩어졌다가 고양이에게 빨려 옴 → 닿으면 경험치
// 모델 = 블렌더 + Codex 3D 보석(Resources/Kit/Ch1/XpGem, 빙글빙글 돎). 없으면 Codex 그림 UI/xp_orb, 그것도 없으면 청록 구
using UnityEngine;

namespace CatTang
{
    public class XpOrb : MonoBehaviour
    {
        public const float Scatter = 0.35f, PickR = 0.7f;
        static readonly Color Teal = new(0.45f, 0.95f, 1f);

        int value;
        bool spinning;
        Vector3 vel;
        float born, speed;
        Transform look;
        static Sprite spr;
        static Material sphereMat;

        public static void Spawn(Vector3 at, int value)
        {
            var go = new GameObject("XpOrb");
            go.transform.position = at;
            var o = go.AddComponent<XpOrb>();
            o.value = value;
            o.born = Time.time;
            // 위 · 옆으로 톡 튀어나옴
            var side = Random.insideUnitCircle.normalized * Random.Range(1.5f, 3.2f);
            o.vel = new Vector3(side.x, Random.Range(3.5f, 5.5f), side.y);
            o.Build();
        }

        void Build()
        {
            look = new GameObject("Look").transform;
            look.SetParent(transform, false);
            look.localRotation = Quaternion.Euler(0, Random.Range(0f, 360f), Random.Range(-15f, 15f));
            // 3D 보석 모델 (블렌더 + Codex: Art/tools/blender_kit_xpgem.py → Resources/Kit/Ch1/XpGem). 없으면 Codex 그림, 그것도 없으면 구
            var gem = Kit.Shared(RunManager.Instance ? RunManager.Instance.Chapter : null, "XpGem");
            if (!spr && !gem) spr = UIKit.Spr("xp_orb");
            if (gem)
            {
                var m = Instantiate(gem, look, false);
                m.transform.localScale *= 1.7f;                     // 높은 게임 카메라에서 읽히게 (모델 0.35m → 약 0.6m)
                foreach (var c in m.GetComponentsInChildren<Collider>()) Destroy(c);
                foreach (var r in m.GetComponentsInChildren<Renderer>()) r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                spinning = true;
            }
            else if (spr)
            {
                var sr = look.gameObject.AddComponent<SpriteRenderer>();
                sr.sprite = spr;
                var sh = Shader.Find("CatTang/Sprite");
                if (sh) sr.sharedMaterial = SpriteMat(sh);
                float s = 0.55f / Mathf.Max(0.01f, spr.bounds.size.y);
                look.localScale = Vector3.one * s;
                look.gameObject.AddComponent<Billboard>();
            }
            else
            {
                var g = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                Destroy(g.GetComponent<Collider>());
                g.transform.SetParent(look, false);
                g.transform.localScale = Vector3.one * 0.3f;
                if (!sphereMat) sphereMat = RoomBuilder.Mat(Teal, 1f);
                g.GetComponent<Renderer>().sharedMaterial = sphereMat;
            }
            var tr = gameObject.AddComponent<TrailRenderer>();
            tr.time = 0.18f;
            tr.widthMultiplier = 0.14f;
            tr.widthCurve = AnimationCurve.Linear(0, 1, 1, 0);
            var sh2 = Shader.Find("CatTang/AdditiveGlow") ?? Shader.Find("Sprites/Default");
            tr.sharedMaterial = TrailMat(sh2);
            var gr = new Gradient();
            gr.SetKeys(new[] { new GradientColorKey(Teal, 0), new GradientColorKey(Teal, 1) }, new[] { new GradientAlphaKey(0.7f, 0), new GradientAlphaKey(0, 1) });
            tr.colorGradient = gr;
            tr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }

        static Material spriteMat, trailMat;
        static Material SpriteMat(Shader sh) { if (!spriteMat) spriteMat = new Material(sh); return spriteMat; }
        static Material TrailMat(Shader sh) { if (!trailMat) trailMat = new Material(sh); return trailMat; }

        void Update()
        {
            var p = PlayerController.Instance;
            float age = Time.time - born;
            if (age < Scatter || !p)
            {
                // 흩어짐: 중력 + 바닥에서 튕김
                vel += Vector3.down * 18f * Time.deltaTime;
                transform.position += vel * Time.deltaTime;
                if (transform.position.y < 0.35f) { var q = transform.position; q.y = 0.35f; transform.position = q; vel.y = Mathf.Abs(vel.y) * 0.35f; vel.x *= 0.6f; vel.z *= 0.6f; }
            }
            else
            {
                // 빨려 옴: 점점 빨라짐
                var target = p.transform.position + Vector3.up * 0.9f;
                speed = Mathf.Min(30f, speed + 40f * Time.deltaTime);
                transform.position = Vector3.MoveTowards(transform.position, target, Mathf.Max(6f, speed) * Time.deltaTime);
                if ((transform.position - target).sqrMagnitude < PickR * PickR)
                {
                    RunManager.Instance?.AddXp(value);
                    FxBurst.Spawn(target, 0.6f, FxBurst.Kind.Spark, Teal);
                    Destroy(gameObject);
                    return;
                }
            }
            if (look)
            {
                look.localPosition = Vector3.up * Mathf.Sin((Time.time - born) * 9f) * 0.06f;
                if (spinning) look.Rotate(0, 360f * Time.deltaTime, 0, Space.World);   // 3D 보석은 빙글빙글 (면마다 빛이 달라 반짝임)
            }
            if (age > 8f) { RunManager.Instance?.AddXp(value); Destroy(gameObject); }       // 혹시 못 따라오면 그냥 지급
        }
    }
}
