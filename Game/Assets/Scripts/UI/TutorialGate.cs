// 튜토리얼 동안 아직 안 배운 기능 잠금 (2026-11-25)
// 판 1 · 2(튜토리얼) 저장에서만 잠금: 입력을 무시하고 그 기능의 HUD도 숨김 → 가르치는 순간 Unlock → 입력이 풀리고 HUD가 금빛 번쩍 + 톡 튀며 나타남 (GameUI)
// 튜토리얼을 끝낸 저장(tut2) · 장을 넘은 저장 · 튜토리얼 기록이 없는 예전 저장은 전부 열림
//
//  기능                         | 가르치는 때 (StoryRun.cs)                          | 잠긴 동안
//  이동 · 조준 · 사격 · 대시      | 판 1 깨어난 직후 (조작 안내 줄)                     | 늘 열림
//  인벤토리 (Tab)               | 판 2: 처음 'Tab 키를 눌러...' 안내가 뜰 때            | Tab 무시 · 도움말 줄에서 빠짐
//                              |  (첫 총탄 · 첫 연산 · 첫 시너지 중 먼저 오는 것)      |
//  판정 · 활성화 표시 (다음 칸 줄) | 판 2: 시각 조건 안내(인벤토리)를 다 본 뒤               | HUD '다음 n시 · 판정 · 활성화' 줄 숨김
//  총 스킬 (우클릭 · 백스텝 샷)   | 판 2: 총알 시계 안내 뒤 적이 처음 가까이 붙을 때 (새 안내) | 우클릭 무시 · '총 스킬' 표시 숨김
//  불릿 파티 (Q · 열기)          | 판 2: 열기가 처음 다 찼을 때 (고양이 독백 뒤)          | Q 무시 · 열기 칸 · '열기' 글 · 가장자리 불길 숨김 (열기는 계속 참)
//  시너지 (특성 목록 HUD)        | 시너지가 처음 켜져 인벤토리 능력치 탭에서 설명한 뒤      | 왼쪽 특성 목록 숨김
//  미니맵                       | (안내 없음 — 판 2부터 그냥 보임)                     | 판 1 동안만 MiniMapView.Suppressed (StoryRun1)
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace CatTang
{
    public enum TutFeature { Inventory, Ultimate, Skill, Judge, Synergy }

    public static class TutorialGate
    {
        const int Count = 5;
        static string Key(TutFeature f) => f switch
        {
            TutFeature.Inventory => "gate_tab",
            TutFeature.Ultimate => "gate_ult",
            TutFeature.Skill => "gate_skill",
            TutFeature.Judge => "gate_judge",
            _ => "gate_synergy",
        };

        /// <summary>튜토리얼을 끝냈거나(판 2 안내를 다 봄) · 장을 넘었거나 · 튜토리얼 기록이 없는 예전 저장(이미 판을 돈 슬롯) → 전부 열림</summary>
        public static bool AllOpen { get { Sync(); return all; } }

        public static bool Open(TutFeature f) { Sync(); return all || seen[(int)f]; }

        /// <summary>기능을 처음 가르치는 순간 (이미 열려 있으면 아무 일 없음) → GameUI가 HUD 등장 연출</summary>
        public static event Action<TutFeature> Unlocked;

        public static void Unlock(TutFeature f)
        {
            if (Open(f)) return;
            Meta.MarkStory(Key(f));
            frame = -1;
            Unlocked?.Invoke(f);
        }

        // 한 프레임에 여러 번 묻으므로 프레임마다 한 번만 읽음
        static int frame = -1;
        static bool all;
        static readonly bool[] seen = new bool[Count];
        static void Sync()
        {
            if (frame == Time.frameCount) return;
            frame = Time.frameCount;
            all = Meta.ChaptersCleared > 0 || Meta.StorySeen(StoryRun.KeyTut2)
                  || (!Meta.FirstRunPending && !Meta.StorySeen(StoryRun.KeyTut1));
            for (int i = 0; i < Count; i++) seen[i] = Meta.StorySeen(Key((TutFeature)i));
        }

        // ---------- 등장 연출: 금빛 빛이 번쩍 퍼지고 · 대상이 톡 튀어나옴 ----------
        static Sprite glow;
        static Sprite Glow()
        {
            if (glow) return glow;
            const int N = 96;
            var t = new Texture2D(N, N, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            for (int y = 0; y < N; y++)
                for (int x = 0; x < N; x++)
                {
                    float dx = (x + 0.5f) / N * 2 - 1, dy = (y + 0.5f) / N * 2 - 1;
                    t.SetPixel(x, y, new Color(1, 1, 1, Mathf.Pow(Mathf.Clamp01(1 - Mathf.Sqrt(dx * dx + dy * dy)), 1.8f)));
                }
            t.Apply();
            return glow = Sprite.Create(t, new Rect(0, 0, N, N), new Vector2(0.5f, 0.5f));
        }

        static float Out3(float u) { u = Mathf.Clamp01(u); return 1 - (1 - u) * (1 - u) * (1 - u); }
        static float OutBack(float u) { const float c = 2.2f; float v = u - 1; return 1 + (c + 1) * v * v * v + c * v * v; }

        /// <summary>rt 뒤에 금빛 빛을 번쩍 퍼뜨리고, pop이면 rt를 작게 → 크게 → 제 크기로 튀김 (시간이 멈춰 있어도 돎)</summary>
        public static IEnumerator Reveal(RectTransform rt, bool pop = true, float delay = 0f)
        {
            if (!rt) yield break;
            if (delay > 0) yield return new WaitForSecondsRealtime(delay);
            if (!rt) yield break;
            // 빛: 같은 부모 · 같은 자리(가운데 기준), 대상 바로 뒤
            var fl = new GameObject("RevealFlash", typeof(RectTransform)).GetComponent<RectTransform>();
            fl.SetParent(rt.parent, false);
            fl.anchorMin = rt.anchorMin; fl.anchorMax = rt.anchorMax;
            fl.pivot = new Vector2(0.5f, 0.5f);
            var size = rt.rect.size;
            fl.sizeDelta = new Vector2(Mathf.Max(80, size.x * 1.15f), Mathf.Max(80, size.y * 1.6f));
            fl.anchoredPosition = rt.anchoredPosition + Vector2.Scale(new Vector2(0.5f, 0.5f) - rt.pivot, size);
            fl.SetSiblingIndex(rt.GetSiblingIndex());
            var im = fl.gameObject.AddComponent<Image>();
            im.sprite = Glow(); im.raycastTarget = false;
            var gold = new Color(1f, 0.86f, 0.4f);
            Sfx.Play("ui_buy", 0.45f);
            var s0 = rt.localScale;
            const float T = 0.6f;
            for (float t = 0; t < T && rt; t += Time.unscaledDeltaTime)
            {
                float u = t / T;
                fl.localScale = Vector3.one * Mathf.Lerp(0.7f, 1.5f, Out3(u));
                im.color = new Color(gold.r, gold.g, gold.b, 0.9f * (u < 0.15f ? u / 0.15f : 1 - (u - 0.15f) / 0.85f));
                if (pop) rt.localScale = s0 * (u < 0.25f ? Mathf.Lerp(0.4f, 1.18f, u / 0.25f) : Mathf.LerpUnclamped(1.18f, 1f, OutBack((u - 0.25f) / 0.75f)));
                yield return null;
            }
            if (rt) rt.localScale = s0;
            if (fl) UnityEngine.Object.Destroy(fl.gameObject);
        }

        // ---------- 개발용 (지금 고른 저장 슬롯 기준) ----------
        /// <summary>판 2 튜토리얼 직전 상태로: 오프닝 · 판 1은 본 것으로, 판 2 안내 · 시너지 · 연산 안내 · 기능 잠금 기록은 지움</summary>
        public static void DevResetToRun2()
        {
            Meta.MarkStory(StoryRun.KeyOpening);
            Meta.MarkStory("dream1");
            Meta.MarkStory(StoryRun.KeyTut1);
            foreach (var k in new[] { StoryRun.KeyTut2, StoryHints.KeyClock, StoryHints.KeyCond, StoryHints.KeyHeat, StoryHints.KeySkill, StoryRun.KeySynergy, OpHints.Key, OpHints.KeyDoor })
                PlayerPrefs.DeleteKey(Meta.StoryPrefKey(k));
            for (int i = 0; i < Count; i++) PlayerPrefs.DeleteKey(Meta.StoryPrefKey(Key((TutFeature)i)));
            PlayerPrefs.Save();
            frame = -1;
        }

        /// <summary>연산 첫 등장 안내만 다시</summary>
        public static void DevResetOps()
        {
            foreach (var k in new[] { OpHints.Key, OpHints.KeyDoor }) PlayerPrefs.DeleteKey(Meta.StoryPrefKey(k));
            PlayerPrefs.Save();
        }

#if UNITY_EDITOR
        [UnityEditor.MenuItem("CatTang/튜토리얼/판 2 튜토리얼 직전으로 (지금 슬롯)")]
        static void MenuRun2() { DevResetToRun2(); Debug.Log($"[튜토리얼] 슬롯 {Meta.ActiveSlot}: 판 2 튜토리얼 직전 상태 (장 클리어 {Meta.ChaptersCleared} — 0이어야 잠금이 걸림)"); }

        [UnityEditor.MenuItem("CatTang/튜토리얼/연산 안내 다시 보기 (지금 슬롯)")]
        static void MenuOps() { DevResetOps(); Debug.Log($"[튜토리얼] 슬롯 {Meta.ActiveSlot}: 연산 첫 등장 안내 기록 지움"); }
#endif
    }
}
