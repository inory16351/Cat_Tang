// 다국어 (2026-11-26 사용자: 번역 진행 · 환경설정에서 언어 선택)
//   원문 = 코드 · 표의 한국어 그대로. 화면에 글을 넣는 순간 LText(UI) · Interactable.WorldLabel(월드 글)이 Loc.T로 바꿈
//   사전 = Resources/Lang/<언어>.json  {"entries":[{"ko":"…","en":"…"}]} (Art/tools/loc_extract.py로 뽑고 번역 · Gemini 검증)
//   2026-11-27 프랑스어 · 독일어 · 러시아어 · 일본어 · 중국어(간체) · 아랍어 추가: <언어>.json 항목 = {"ko","en"(참고),"<언어>"}
//     (Art/tools/loc_lang.py split · merge · check, 검수 Art/tools/loc_agy_review.py). 아랍어 = 오른쪽→왼쪽 (RTL, ArabicText · LText)
//   찾는 순서: ① 통째로 ② 줄마다(\n) ③ 자리 표시 틀({0}…, 보간 문자열에서 뽑은 항목 — 구멍에 든 값도 다시 번역) ④ 숫자를 {n}으로 바꾼 틀
//   못 찾은 한국어는 원문 그대로 두고 Misses에 모음 (개발용: CatTang/번역/못 찾은 문구 저장)
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;

namespace CatTang
{
    public static class Loc
    {
        const string Pref = "cattang.opt.lang";
        public static readonly (string code, string name)[] Languages =
        {
            ("ko", "한국어"), ("en", "English"), ("ja", "日本語"), ("zh", "简体中文"),
            ("fr", "Français"), ("de", "Deutsch"), ("ru", "Русский"), ("ar", "العربية"),
        };

        public static string Lang
        {
            get { lang ??= PlayerPrefs.GetString(Pref, "ko"); return lang; }
            set
            {
                if (value == Lang) return;
                lang = value;
                PlayerPrefs.SetString(Pref, value); PlayerPrefs.Save();
                loaded = null;
                memo.Clear();
                Changed?.Invoke();
            }
        }
        static string lang;
        public static bool Korean => Lang == "ko";
        /// <summary>오른쪽→왼쪽 언어 (아랍어): LText가 글자 잇기 · 순서 뒤집기 · 정렬 좌우 바꿈</summary>
        public static bool RTL => Lang == "ar";
        /// <summary>낱말 사이에 띄어쓰기가 없는 언어 (고유명사 찾기에서 낱말 경계를 안 봄)</summary>
        public static bool NoWordSpaces => Lang == "ja" || Lang == "zh";
        /// <summary>언어가 바뀌면 (LText가 원문으로 다시 번역)</summary>
        public static event Action Changed;

        [Serializable] class Entry { public string ko, en, fr, de, ru, ja, zh, ar; }

        static string Pick(Entry e) => Lang switch
        {
            "en" => e.en, "fr" => e.fr, "de" => e.de, "ru" => e.ru, "ja" => e.ja, "zh" => e.zh, "ar" => e.ar,
            _ => null
        };
        [Serializable] class File { public List<Entry> entries = new(); }

        static string loaded;
        static readonly Dictionary<string, string> exact = new();
        static readonly Dictionary<string, string> numeric = new();                         // 숫자 → {n} 틀
        static readonly List<(Regex re, string en, int holes)> templates = new();
        static readonly Dictionary<string, string> memo = new();
        public static readonly HashSet<string> Misses = new();

        static readonly Regex Hangul = new("[가-힣]", RegexOptions.Compiled);
        static readonly Regex Hole = new(@"\{(\d+)\}", RegexOptions.Compiled);
        static readonly Regex Num = new(@"-?\d+(?:\.\d+)?", RegexOptions.Compiled);

        public static bool HasHangul(string s) => !string.IsNullOrEmpty(s) && Hangul.IsMatch(s);

        static void Ensure()
        {
            if (loaded == Lang) return;
            loaded = Lang;
            exact.Clear(); numeric.Clear(); templates.Clear(); memo.Clear();
            if (Korean) return;
            var ta = Resources.Load<TextAsset>("Lang/" + Lang);
            if (!ta) { Debug.LogWarning("[Loc] 사전 없음 Resources/Lang/" + Lang); return; }
            File f;
            try { f = JsonUtility.FromJson<File>(ta.text); }
            catch (Exception e) { Debug.LogWarning("[Loc] 사전 읽기 실패 " + e.Message); return; }
            if (f?.entries == null) return;
            foreach (var e in f.entries)
            {
                var tr = Pick(e);
                if (string.IsNullOrEmpty(e.ko) || string.IsNullOrEmpty(tr)) continue;
                exact[e.ko] = tr;
                if (Hole.IsMatch(e.ko))
                {
                    // {0} 자리마다 아무 글(가장 짧게) — 앞뒤 글은 그대로
                    var sb = new StringBuilder("^");
                    int last = 0, holes = 0;
                    foreach (Match m in Hole.Matches(e.ko))
                    {
                        sb.Append(Regex.Escape(e.ko.Substring(last, m.Index - last)));
                        sb.Append("(?<h").Append(m.Groups[1].Value).Append(">.+?)");
                        last = m.Index + m.Length; holes++;
                    }
                    sb.Append(Regex.Escape(e.ko.Substring(last))).Append('$');
                    try { templates.Add((new Regex(sb.ToString(), RegexOptions.Singleline), tr, holes)); } catch { }
                }
                else if (Num.IsMatch(e.ko)) numeric[Generalize(e.ko, out _)] = tr;
            }
            templates.Sort((a, b) => b.re.ToString().Length.CompareTo(a.re.ToString().Length));   // 글자가 많이 박힌 틀부터
        }

        static string Generalize(string s, out List<string> nums)
        {
            var list = new List<string>();
            int i = 0;
            var g = Num.Replace(s, m => { list.Add(m.Value); return "{" + (i++) + "}"; });
            nums = list;
            return g;
        }

        /// <summary>화면에 보일 글 → 지금 언어. 한국어 모드 · 한글 없는 글은 그대로</summary>
        public static string T(string s)
        {
            if (string.IsNullOrEmpty(s) || Korean || !HasHangul(s)) return s;
            foreach (var (_, name) in Languages) if (s == name) return s;                // 언어 이름은 그 언어 그대로 (설정 창)
            Ensure();
            if (memo.TryGetValue(s, out var hit)) return hit;
            var r = Lines(s, 0);
            if (memo.Count > 4000) memo.Clear();                                           // HUD 숫자처럼 계속 바뀌는 글로 무한히 쌓이지 않게
            memo[s] = r;
            return r;
        }

        static string Lines(string s, int depth)
        {
            if (One(s, depth, out var r)) return r;
            if (s.IndexOf('\n') >= 0)
            {
                var parts = s.Split('\n');
                for (int i = 0; i < parts.Length; i++) parts[i] = Piece(parts[i], depth);
                return string.Join("\n", parts);
            }
            return Piece(s, depth);
        }

        /// <summary>한 줄: 통째로 못 찾으면 ' · ' 로 이어 붙인 조각마다</summary>
        static string Piece(string s, int depth)
        {
            if (!HasHangul(s)) return s;
            if (One(s, depth, out var r)) return r;
            if (s.Contains(" · "))
            {
                var parts = s.Split(new[] { " · " }, StringSplitOptions.None);
                bool any = false;
                for (int i = 0; i < parts.Length; i++) { if (One(parts[i], depth, out var p)) { parts[i] = p; any = true; } }
                if (any) return string.Join(" · ", parts);
            }
            if (depth == 0) Misses.Add(s);
            return s;
        }

        static bool One(string s, int depth, out string r)
        {
            r = s;
            if (!HasHangul(s)) return true;
            // 앞뒤 공백은 떼고 찾고 다시 붙임
            int a = 0, b = s.Length;
            while (a < b && char.IsWhiteSpace(s[a])) a++;
            while (b > a && char.IsWhiteSpace(s[b - 1])) b--;
            string core = s.Substring(a, b - a), pre = s.Substring(0, a), post = s.Substring(b);
            if (exact.TryGetValue(core, out var e)) { r = pre + e + post; return true; }
            if (depth < 3)
                foreach (var (re, en, _) in templates)
                {
                    var m = re.Match(core);
                    if (!m.Success) continue;
                    var outS = Hole.Replace(en, h => { var g = m.Groups["h" + h.Groups[1].Value]; return g.Success ? Lines(g.Value, depth + 1) : h.Value; });
                    r = pre + outS + post;
                    return true;
                }
            var gen = Generalize(core, out var nums);
            if (nums.Count > 0 && numeric.TryGetValue(gen, out var ng))
            {
                r = pre + Hole.Replace(ng, h => { int k = int.Parse(h.Groups[1].Value); return k < nums.Count ? nums[k] : h.Value; }) + post;
                return true;
            }
            return false;
        }

        /// <summary>LText가 아닌 글(TextMesh 월드 글 등)에 넣을 때: 번역 + 아랍어면 글자 잇기 · 화면 순서 (자동 줄바꿈 없는 글만)</summary>
        public static string Shown(string s) => ArabicText.Visual(T(s));

        /// <summary>개발용: 못 찾은 한국어 글 목록</summary>
        public static string DumpMisses() => string.Join("\n", Misses);
    }
}
