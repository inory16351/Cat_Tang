// 피해 숫자: 숫자마다 따로 그린 이미지(Resources/UI/digits/d0~d9)를 이어 붙여 띄움. 튀어 오르며 커졌다 작아지고 사라짐.
// 치명타는 전용 글꼴(Resources/UI/digits_crit/d0~d9, Codex — 위 뜨거운 흰 노랑 · 아래 진홍, 금테, 기울임)으로 크게 · 더 세게 튀고 · 떨리며 조금 더 오래 (2026-11-17 사용자)
//  전용 그림이 없으면 일반 숫자를 붉게 물들여 씀
// 정지된 적 추가 피해(영원 6개 등)는 초록 글꼴(Resources/UI/digits_eternity, Codex — 위 민트 흰색 · 아래 에메랄드, 짙은 초록 테)로
//  본 숫자 위에 "+몫"처럼 하나 더 띄움 (2026-11-17 사용자). 그림이 없으면 일반 숫자를 초록으로
using System.Collections.Generic;
using UnityEngine;

namespace CatTang
{
    public class DamageNumber : MonoBehaviour
    {
        static readonly Material[] mats = new Material[10], critMats = new Material[10], bonusMats = new Material[10];
        static readonly float[] aspect = new float[10], critAspect = new float[10], bonusAspect = new float[10];
        static bool critLoaded, hasCritArt, hasBonusArt;
        static Mesh quad;
        readonly List<MeshRenderer> parts = new();
        float born, scale, life = 0.75f;
        bool crit;
        Color tint = Color.white;
        Vector3 vel;
        MaterialPropertyBlock mpb;

        static bool Load()
        {
            // 재질이 사라졌으면(플레이 모드 도메인 재로드 꺼짐 등으로 정적 캐시만 남은 경우) 다시 만듦
            if (mats[0] && (!hasCritArt || critMats[0]) && (!hasBonusArt || bonusMats[0])) return true;
            var sh = Shader.Find("Sprites/Default");
            if (!LoadSet("UI/digits/d", sh, mats, aspect, 3100)) return false;
            critLoaded = false;
            if (!critLoaded)
            {
                critLoaded = true;
                hasCritArt = LoadSet("UI/digits_crit/d", sh, critMats, critAspect, 3101);
                hasBonusArt = LoadSet("UI/digits_eternity/d", sh, bonusMats, bonusAspect, 3101);
            }
            if (!quad)
            {
                var tmp = GameObject.CreatePrimitive(PrimitiveType.Quad);
                quad = tmp.GetComponent<MeshFilter>().sharedMesh;
                Destroy(tmp);
            }
            return true;
        }

        static bool LoadSet(string path, Shader sh, Material[] m, float[] asp, int queue)
        {
            for (int i = 0; i < 10; i++)
            {
                var tex = Resources.Load<Texture2D>(path + i);
                if (!tex) return false;
                m[i] = new Material(sh) { mainTexture = tex, renderQueue = queue };
                asp[i] = (float)tex.width / tex.height;
            }
            return true;
        }

        /// <summary>정지된 적 추가 피해 몫: 초록 숫자 (본 숫자보다 조금 작게, 늦게 · 덜 튀며)</summary>
        public static void SpawnBonus(Vector3 pos, float dmg)
        {
            if (!Load()) return;
            if (dmg < 0.5f) return;
            Spawn(pos, dmg, false, true);
        }

        /// <summary>조건부 추가 피해 몫 (보스 · 표식 · 상태 이상 · 거리 · 판정 시각 등): 금빛 숫자 (2026-11-18 사용자). 초록 숫자와 같은 크기 · 움직임</summary>
        public static void SpawnCond(Vector3 pos, float dmg)
        {
            if (!Load()) return;
            if (dmg < 0.5f) return;
            Spawn(pos, dmg, false, true, true);
        }

        public static void Spawn(Vector3 pos, float dmg, bool crit = false) => Spawn(pos, dmg, crit, false);

        static readonly Color CondGold = new(1f, 0.8f, 0.25f);

        static void Spawn(Vector3 pos, float dmg, bool crit, bool bonus, bool cond = false)
        {
            if (!Load()) return;
            var go = new GameObject(crit ? "DmgCrit" : "Dmg");
            go.transform.position = pos + new Vector3(Random.Range(-0.35f, 0.35f), crit ? 0.25f : 0, 0);
            var d = go.AddComponent<DamageNumber>();
            // 2026-11-17 사용자: 피해 숫자가 잘 안 보임 → 전체 1.5배 (0.4 · 0.5 · 0.62 → 0.6 · 0.75 · 0.93), 치명타는 그 1.35배
            d.scale = (dmg >= 30 ? 0.93f : dmg >= 15 ? 0.75f : 0.6f) * (crit ? 1.35f : 1f);
            d.vel = new Vector3(Random.Range(-0.6f, 0.6f), crit ? 3.8f : 3.2f, 0);
            d.crit = crit;
            d.life = crit ? 0.95f : 0.75f;
            bool art = crit && hasCritArt || bonus && !cond && hasBonusArt;   // 금빛은 일반 숫자를 물들임
            var useMats = !art ? mats : bonus ? bonusMats : critMats;
            var useAspect = !art ? aspect : bonus ? bonusAspect : critAspect;
            if (crit && !art) d.tint = new Color(1f, 0.45f, 0.4f);
            if (bonus && !art) d.tint = cond ? CondGold : new Color(0.35f, 1f, 0.45f);
            if (bonus) { d.scale *= 0.85f; d.vel.y = 2.6f; d.life = 0.85f; }
            d.born = Time.time;
            d.mpb = new MaterialPropertyBlock();
            string s = Mathf.Max(1, Mathf.RoundToInt(dmg)).ToString();
            float total = 0;
            foreach (var c in s) total += useAspect[c - '0'] * 0.82f;
            float x = -total / 2;
            foreach (var c in s)
            {
                int n = c - '0';
                var p = new GameObject("d" + n);
                p.transform.SetParent(go.transform, false);
                float w = useAspect[n] * 0.82f;
                p.transform.localPosition = new Vector3(x + w / 2, 0, 0);
                p.transform.localScale = new Vector3(useAspect[n], 1, 1);
                p.AddComponent<MeshFilter>().sharedMesh = quad;
                var mr = p.AddComponent<MeshRenderer>();
                mr.sharedMaterial = useMats[n];
                mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                d.parts.Add(mr);
                x += w;
            }
            go.transform.localScale = Vector3.one * d.scale;
        }

        void Update()
        {
            float t = (Time.time - born) / life;
            if (t >= 1) { Destroy(gameObject); return; }
            vel.y -= 9f * Time.deltaTime;
            transform.position += vel * Time.deltaTime;
            var cam = Camera.main;
            if (cam) transform.rotation = cam.transform.rotation;
            float peak = crit ? 1.5f : 1.25f;
            float pop = t < 0.15f ? Mathf.Lerp(0.4f, peak, t / 0.15f) : Mathf.Lerp(peak, 0.9f, Mathf.Clamp01((t - 0.15f) / 0.3f));
            transform.localScale = Vector3.one * scale * pop;
            if (crit && t < 0.35f) transform.position += cam ? cam.transform.right * Mathf.Sin(Time.time * 90f) * 0.03f * (1 - t / 0.35f) : Vector3.zero;   // 치명타: 처음에 떨림
            float a = t < 0.6f ? 1 : 1 - (t - 0.6f) / 0.4f;
            mpb.SetColor("_Color", new Color(tint.r, tint.g, tint.b, a));
            foreach (var r in parts) r.SetPropertyBlock(mpb);
        }
    }
}
