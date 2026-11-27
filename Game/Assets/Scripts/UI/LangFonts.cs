// 언어별 글꼴 (2026-11-27 다국어 6개 추가)
//   기본 = 제목 Black Han Sans · 본문 Gothic A1 (한국어 · 영어)
//   Black Han Sans에는 악센트(é ü ß) · 키릴 글자가 없어서 프랑스어 · 독일어 · 러시아어 제목은 Gothic A1 Bold
//   일본어 · 중국어 · 아랍어는 프로젝트에 글꼴이 있으면(Resources/Fonts/<언어>/Title · Body · Bold) 그것, 없으면 OS 글꼴
//   (Windows: Yu Gothic UI · Microsoft YaHei UI · Segoe UI / macOS: Hiragino · PingFang · Geeza Pro)
//   UIKit.Title · Body · BodyBold 가 지금 언어 글꼴을 돌려주고, 언어를 바꾸면 LText가 Remap으로 같은 역할 글꼴로 갈아 끼움
using System.Collections.Generic;
using UnityEngine;

namespace CatTang
{
    public static class LangFonts
    {
        public const int Title = 0, Body = 1, Bold = 2;

        static Font[] baseFonts;
        static readonly Dictionary<string, Font[]> cache = new();

        static readonly Dictionary<string, (string[] body, string[] bold)> Os = new()
        {
            ["ja"] = (new[] { "Yu Gothic UI", "Yu Gothic", "Meiryo UI", "Meiryo", "Hiragino Sans", "Hiragino Kaku Gothic ProN", "Noto Sans JP", "Noto Sans CJK JP", "MS Gothic" },
                      new[] { "Yu Gothic UI Bold", "Yu Gothic Bold", "Meiryo UI Bold", "Meiryo Bold", "Hiragino Sans W6", "Hiragino Kaku Gothic ProN W6", "Noto Sans JP Bold", "Noto Sans CJK JP Bold", "Yu Gothic UI" }),
            ["zh"] = (new[] { "Microsoft YaHei UI", "Microsoft YaHei", "PingFang SC", "Noto Sans SC", "Noto Sans CJK SC", "SimHei" },
                      new[] { "Microsoft YaHei UI Bold", "Microsoft YaHei Bold", "PingFang SC Semibold", "Noto Sans SC Bold", "Noto Sans CJK SC Bold", "Microsoft YaHei UI" }),
            ["ar"] = (new[] { "Segoe UI", "Tahoma", "Geeza Pro", "Noto Sans Arabic", "Arial" },
                      new[] { "Segoe UI Bold", "Tahoma Bold", "Geeza Pro Bold", "Noto Sans Arabic Bold", "Arial Bold", "Segoe UI" }),
        };

        /// <summary>UIKit.Init이 기본 글꼴 셋을 알려 줌</summary>
        public static void SetBase(Font title, Font body, Font bold)
        {
            baseFonts = new[] { title, body, bold };
            cache.Clear();
        }

        static Font[] Set(string lang)
        {
            if (baseFonts == null) UIKit.Init();
            if (!cache.TryGetValue(lang, out var set)) { set = Build(lang); cache[lang] = set; }
            return set;
        }

        /// <summary>지금 언어의 역할(Title · Body · Bold) 글꼴</summary>
        public static Font Get(int r)
        {
            var set = Set(Loc.Lang);
            return set[r] ? set[r] : baseFonts[r];
        }

        static Font[] Build(string lang)
        {
            switch (lang)
            {
                case "fr": case "de": case "ru":
                    return new[] { baseFonts[Bold], baseFonts[Body], baseFonts[Bold] };
                case "ja": case "zh": case "ar":
                {
                    var t = Resources.Load<Font>("Fonts/" + lang + "/Title");
                    var b = Resources.Load<Font>("Fonts/" + lang + "/Body");
                    var bb = Resources.Load<Font>("Fonts/" + lang + "/Bold");
                    if (!b || !bb)
                    {
                        var (body, bold) = Os[lang];
                        if (!b) b = Font.CreateDynamicFontFromOSFont(body, 32);
                        if (!bb) bb = Font.CreateDynamicFontFromOSFont(bold, 32);
                        if (b) b.name = lang + "_os_body";
                        if (bb) bb.name = lang + "_os_bold";
                    }
                    return new[] { t ? t : bb, b, bb };
                }
                default:
                    return baseFonts;
            }
        }

        /// <summary>그 언어에서 이 글꼴의 역할 (Title · Body · Bold 순서로 먼저 맞는 것, 모르는 글꼴 = -1)</summary>
        public static int RoleOf(Font f, string lang)
        {
            if (!f) return -1;
            var set = Set(lang);
            for (int r = 0; r < set.Length; r++) if (set[r] == f) return r;
            for (int r = 0; r < baseFonts.Length; r++) if (baseFonts[r] == f) return r;
            return -1;
        }
    }
}
