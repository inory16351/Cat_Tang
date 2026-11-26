// 바닥에 새겨지는 3D 숫자 (2026-11-26 사용자: 문자판 칸 숫자 그림 배지가 바닥과 안 어울림 → 블렌더 숫자가 바닥에 스스슥 생김)
// - 모델: 블렌더 Art/tools/blender_kit_ch4_floornum.py → Resources/Kit/Ch4/FloorNum_{1..12} (검은 숫자 + 은빛 테, 보스 문자판과 같은 결)
// - 생김: 왼쪽에서 오른쪽으로 긁히듯 펼쳐짐(가로 0 → 1, 0.3초) + 살짝 바닥에서 올라옴 + 긁는 소리 · 먼지
// - 바꿈(SetNumber): 짧게 눌렀다 튀어나오며 다른 숫자로 (뒤섞기 중 0.12초마다 바뀌어도 읽힘)
// - 사라짐(Erase): 오른쪽으로 쓸려 지워짐(0.25초) 뒤 삭제
// - 쓰는 곳: 4장 쌍둥이 '뒤섞인 문자판' (Ch4Twins.Shuffled)
using System.Collections;
using UnityEngine;

namespace CatTang
{
    public class FloorNumeral : MonoBehaviour
    {
        const float Size = 2.0f;                       // 모델 지름 (m)
        static readonly GameObject[] prefabs = new GameObject[13];
        Transform wipe, model;
        int number;
        float scale = 1f;
        Coroutine pop;

        public int Number => number;

        /// <summary>at(바닥 위)에 숫자 n을 스스슥 새김. delay = 차례로 나타나게 늦춤. diameter = 테 지름(m)</summary>
        public static FloorNumeral Make(Vector3 at, int n, float diameter = 2.4f, float delay = 0f)
        {
            var go = new GameObject("FloorNum_" + n);
            go.transform.position = at + Vector3.up * 0.02f;
            var f = go.AddComponent<FloorNumeral>();
            f.scale = diameter / Size;
            // 긁히는 축: 화면 왼쪽(−X) 끝을 기준으로 가로 크기를 키움 → 왼쪽에서 오른쪽으로 펼쳐짐
            f.wipe = new GameObject("Wipe").transform;
            f.wipe.SetParent(go.transform, false);
            f.wipe.localPosition = new Vector3(-diameter / 2f, 0, 0);
            f.SetModel(n);
            f.StartCoroutine(f.Appear(delay));
            return f;
        }

        void SetModel(int n)
        {
            number = n;
            if (model) Destroy(model.gameObject);
            if (!prefabs[n]) prefabs[n] = Resources.Load<GameObject>("Kit/Ch4/FloorNum_" + n);
            GameObject m;
            if (prefabs[n]) m = Instantiate(prefabs[n], wipe, false);
            else
            {
                m = new GameObject("NoModel_" + n);
                m.transform.SetParent(wipe, false);
            }
            foreach (var c in m.GetComponentsInChildren<Collider>()) Destroy(c);
            foreach (var r in m.GetComponentsInChildren<Renderer>()) r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            m.transform.localPosition = new Vector3(Size * scale / 2f, 0, 0);
            m.transform.localScale *= scale;
            model = m.transform;
        }

        IEnumerator Appear(float delay)
        {
            wipe.localScale = new Vector3(0.001f, 1, 1);
            if (delay > 0) yield return new WaitForSeconds(delay);
            Sfx.Play("e4_tick", 0.35f, 0.25f, 2);
            Enemy.Fx3("Stardust", transform.position + Vector3.up * 0.2f, 0.6f);
            const float T = 0.3f;
            for (float t = 0; t < T; t += Time.deltaTime)
            {
                float k = t / T, e = 1f - (1f - k) * (1f - k);
                wipe.localScale = new Vector3(Mathf.Max(0.001f, e), 1, 1);
                wipe.localPosition = new Vector3(wipe.localPosition.x, -0.04f * (1f - e), 0);
                yield return null;
            }
            wipe.localScale = Vector3.one;
            wipe.localPosition = new Vector3(wipe.localPosition.x, 0, 0);
        }

        /// <summary>다른 숫자로 바꿈 (눌렸다 튀어나옴)</summary>
        public void SetNumber(int n)
        {
            if (n == number) return;
            SetModel(n);
            if (pop != null) StopCoroutine(pop);
            pop = StartCoroutine(Pop());
        }

        IEnumerator Pop()
        {
            const float T = 0.14f;
            for (float t = 0; t < T; t += Time.deltaTime)
            {
                float k = t / T;
                float s = k < 0.4f ? Mathf.Lerp(0.7f, 1.15f, k / 0.4f) : Mathf.Lerp(1.15f, 1f, (k - 0.4f) / 0.6f);
                transform.localScale = new Vector3(s, 1, s);
                yield return null;
            }
            transform.localScale = Vector3.one;
            pop = null;
        }

        /// <summary>오른쪽으로 쓸려 지워진 뒤 삭제</summary>
        public void Erase(float delay = 0f)
        {
            if (!isActiveAndEnabled) { Destroy(gameObject); return; }
            StopAllCoroutines();
            StartCoroutine(EraseCo(delay));
        }

        IEnumerator EraseCo(float delay)
        {
            if (delay > 0) yield return new WaitForSeconds(delay);
            // 오른쪽 끝을 기준으로 줄임 → 왼쪽부터 지워짐
            float w = Size * scale;
            var start = wipe.localPosition;
            const float T = 0.25f;
            for (float t = 0; t < T; t += Time.deltaTime)
            {
                float k = t / T;
                wipe.localScale = new Vector3(Mathf.Max(0.001f, 1f - k), 1, 1);
                wipe.localPosition = start + new Vector3(w * k, -0.03f * k, 0);
                yield return null;
            }
            Destroy(gameObject);
        }
    }
}
