// 스토리 맵 전용 배경음악 (2026-11-25 사용자: "제미나이 크롬으로 스토리맵 전용 브금"): Resources/Audio/bgm_story_{장}
//   1 = 어린 날 시계탑 광장 · 2 = 배웅하던 승강장(비 오는 밤) · 3 = 거울 같은 호숫가. 곡이 없으면 그 장 맵 곡
using UnityEngine;

namespace CatTang
{
    public static class StoryBgm
    {
        public static bool Has(int chapter) => Resources.Load<AudioClip>("Audio/bgm_story_" + chapter);
        public static string For(int chapter, string fallbackKind) => Has(chapter) ? "bgm_story_" + chapter : $"bgm_{fallbackKind}_{chapter}";
    }
}
