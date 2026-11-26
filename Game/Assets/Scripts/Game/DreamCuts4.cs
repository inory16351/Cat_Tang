// 꿈4 '마지막 장' (내부 키 dream6 = DreamOf(5) · 5장 클리어 · 2026-11-26 사용자 확정, HANDOFF.md "2026-11-26 — 5장 스토리 · 엔딩 기획 확정")
//   유화 임파스토 · 자막 없음 · 곡 = Resources/Audio/story_dream4 (크롬 Gemini, 약 30초)
//   늘 마지막 장 전에 잠들던 그림책 → 병상의 늙은 손이 같은 책을 마지막 장까지 넘김 → 책 속 고양이 영웅이 결투 뒤 총을 땅에 내려놓음
//   → 노을 길을 걸어감(발자국 = 시계 눈금) → 길 끝 빨간 숄의 그녀가 손을 내밂 → 손이 닿기 직전 11:59:59에서 초침이 멈춤 → 하얗게
// 그림 = Resources/Story/Dreams/dream6/*.jpg (크롬 ChatGPT 유화 1672x941 → 1450 JPG, 원본 Art/story_src/dream6/, 정리 docs/tools/dream6_import.py)
//   _j = 선 떨림용 살짝 일그러뜨린 판(붓질이 살아 움직이게, 초당 8번 교대)
using System.Collections.Generic;
using UnityEngine;

namespace CatTang
{
    public static partial class DreamCuts
    {
        static readonly Color OilGround = new(0.16f, 0.09f, 0.07f);

        /// <summary>꿈4 유화: 리노컷 도우미(L1 · Boil)를 쓰되 바탕을 짙은 갈색 캔버스로</summary>
        static MShot Oil(MShot s) { s.tint = OilGround; return s; }

        static MShot[] Dream6
        {
            get
            {
                var L = new List<MShot>();
                void A(params MShot[] s) => L.AddRange(s);
                var Z = Vector2.zero;

                // ══ A 어린 날 0~5.0초: 이불 속 그림책 → 마지막 장 전에 잠듦 (느린 밀어 들어가기 · 디졸브)
                A(Dark(0.6f),
                  Oil(L1(2.0f, "이불 속 그림책을 든 아이 손 (느리게 다가감)", "child_a", new(-20, 10), new(10, 0), 1.06f, 1.14f, 0, 0, 0.8f, tin: MTrans.Dissolve, tinDur: 0.5f, ease: 0)),
                  Oil(Boil(2.4f, "잠든 아이, 마지막 장은 넘기지 못함 (붓질 떨림)", new[] { "child_b", "child_b_j" }, new(10, 0), new(-10, -10), 1.12f, 1.18f, 0, -1, 0.6f, tin: MTrans.Dissolve, tinDur: 0.6f, ease: 0, fps: 8f)));

                // ══ B 지금 5.0~10.4초: 병상 협탁의 늙은 손 → 책장을 넘김 → 마지막 장 (줌 관통으로 책 속으로)
                A(Oil(L1(2.0f, "병상 협탁, 같은 그림책 위의 늙은 손 (같은 구도로 세월 도약)", "old_a", new(20, 0), new(-10, 0), 1.18f, 1.1f, 1, 0, 0.8f, tin: MTrans.Dissolve, tinDur: 0.7f, ease: 0, b0: 0.006f, b1: 0.0008f)),
                  Oil(L1(1.8f, "떨리는 손가락이 책장을 넘김 (빛이 쏟아짐)", "old_b", new(60, -20), new(-30, 0), 1.2f, 1.28f, -2, 1, 1.5f, new[] { Q(0.5f, null, 3) }, ease: 1)),
                  Oil(L1(1.6f, "마지막 장 — 노을 길과 고양이 (줌으로 책 속으로)", "old_c", Z, new(0, -40), 1.04f, 1.5f, 0, 0, 0.5f, ease: 1)));

                // ══ C 책 속 10.4~17.4초: 결투 → 총을 내려놓음
                A(Oil(L1(1.8f, "노을 거리의 결투 (줌 관통으로 들어옴)", "duel_a", new(40, 0), new(-20, 0), 1.3f, 1.12f, 0, 0, 1.2f, new[] { Q(0.85f, null, 6) }, tin: MTrans.Zoom, tinDur: 0.35f, ease: 1)),
                  Oil(L1(1.6f, "결투 뒤, 총을 늘어뜨린 고양이 (흩날리는 먼지)", "duel_b", new(-30, 0), new(20, 0), 1.1f, 1.16f, 0, 1, 1.5f, shutter: 0.2f)),
                  Oil(Boil(2.0f, "무릎 꿇고 쌍권총을 땅에 내려놓음 (붓질 떨림)", new[] { "lay_a", "lay_a_j" }, new(20, -10), new(0, -20), 1.12f, 1.2f, 0, 0, 0.6f, tin: MTrans.Dissolve, tinDur: 0.4f, ease: 0, fps: 8f)),
                  Oil(L1(1.6f, "모래 위 두 자루 총, 떠나는 손 (초점 이동)", "lay_b", new(-40, 10), new(-10, 0), 1.08f, 1.14f, 0, 0, 0.6f, b0: 0.008f, b1: 0.0008f, ease: 0)));

                // ══ D 노을 길 17.4~23.6초: 걸어가는 뒷모습 → 시계 눈금 같은 발자국(위에서 · 돌며) → 아득한 지평선
                A(Oil(Boil(2.4f, "총 없이 노을 길을 걸어가는 고양이 (붓질 떨림)", new[] { "walk_a", "walk_a_j" }, new(0, 20), new(0, -10), 1.06f, 1.16f, 0, 0, 0.8f, tin: MTrans.Dissolve, tinDur: 0.5f, ease: 0, fps: 8f)),
                  Oil(L1(1.6f, "시계 눈금처럼 고른 발자국 (위에서 · 돌며)", "walk_b", Z, new(0, 30), 1.2f, 1.24f, -4, 4, 0.5f)),
                  Oil(L1(2.2f, "지평선으로 작아지는 고양이 (느리게 물러남)", "walk_c", new(0, -20), Z, 1.22f, 1.04f, 0, 0, 0.4f, tin: MTrans.Dissolve, tinDur: 0.5f, ease: 0)));

                // ══ E 길 끝 23.6~29.8초: 빨간 숄의 그녀 → 닿기 직전의 두 손 → 11:59:59 멈춤 → 하얗게
                A(Oil(Boil(2.0f, "길 끝에서 손을 내미는 빨간 숄의 그녀 (붓질 떨림)", new[] { "her_a", "her_a_j" }, new(0, 10), new(0, -10), 1.1f, 1.2f, 0, 0, 0.6f, tin: MTrans.Dissolve, tinDur: 0.5f, ease: 0, fps: 8f)),
                  Oil(L1(2.0f, "닿기 직전의 두 손 (빛나는 틈으로 아주 느리게)", "hands_a", new(0, 0), new(20, -10), 1.06f, 1.3f, 0, 0, 0.3f, ease: 0)),
                  Oil(Boil(1.4f, "11:59:59 — 초침이 멈춤 (정지 · 붓질 떨림)", new[] { "clock_a", "clock_a_j" }, Z, Z, 1.08f, 1.1f, 0, 0, 0f, new[] { Q(0.15f, "!e4_tick_2", 6, 0.08f) }, fps: 8f)),
                  new MShot { dur = 0.3f, tint = Color.white, tin = MTrans.Dissolve, tinDur = 0.25f });   // 합계 29.3초 = 곡 story_dream4
                return L.ToArray();
            }
        }

        /// <summary>엔딩 병실 (내부 7 · Ch5Ending, 12:00 종 뒤): 하얗게 → 병실 협탁의 손과 그림책 · 심전도 평탄 → 마지막 장 → 어둠 (곡 없음, '끝' 자막은 Ch5Ending)</summary>
        static MShot[] Ending7
        {
            get
            {
                var Z = Vector2.zero;
                return new[]
                {
                    new MShot { dur = 0.6f, tint = Color.white },
                    Oil(L1(4.2f, "병실 — 협탁의 그림책 위에 놓인 손 · 심전도가 평평해짐 (아주 느리게 물러남)", "old_a", Z, new(0, 10), 1.16f, 1.06f, 0, 0, 0.3f,
                           new[] { Q(0.25f, "!story_flatline", 0) }, tin: MTrans.Dissolve, tinDur: 1.2f, ease: 0, b0: 0.004f, b1: 0.0008f)),
                    Oil(L1(4.0f, "그림책 마지막 장 (가만히)", "old_c", new(0, -10), new(0, 0), 1.08f, 1.04f, 0, 0, 0f, tin: MTrans.Dissolve, tinDur: 1.4f, ease: 0)),
                    Dark(1.6f, null, MTrans.Dissolve, 1.2f),
                };
            }
        }
    }
}
