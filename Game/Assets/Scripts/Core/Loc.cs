// 다국어 (2026-11-26 사용자: 번역 진행 · 환경설정에서 언어 선택)
//   원문 = 코드 · 표의 한국어 그대로. 화면에 글을 넣는 순간 LText(UI) · Interactable.WorldLabel(월드 글)이 Loc.T로 바꿈
//   사전 = Resources/Lang/<언어>.json  {"entries":[{"ko":"…","en":"…"}]} (Art/tools/loc_extract.py로 뽑고 번역 · Gemini 검증)
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
        public static readonly (string code, string name)[] Languages = { ("ko", "한국어"), ("en", "English") };

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
        /// <summary>언어가 바뀌면 (LText가 원문으로 다시 번역)</summary>
        public static event Action Changed;

        [Serializable] class Entry { public string ko, en; }
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
                if (string.IsNullOrEmpty(e.ko) || string.IsNullOrEmpty(e.en)) continue;
                exact[e.ko] = e.en;
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
                    try { templates.Add((new Regex(sb.ToString(), RegexOptions.Singleline), e.en, holes)); } catch { }
                }
                else if (Num.IsMatch(e.ko)) numeric[Generalize(e.ko, out _)] = e.en;
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

        /// <summary>개발용: 못 찾은 한국어 글 목록</summary>
        public static string DumpMisses() => string.Join("\n", Misses);
    }
}
