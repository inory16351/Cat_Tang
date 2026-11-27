// 아랍어 글자 잇기 + 오른쪽→왼쪽 (2026-11-27 다국어 6개 추가)
//   Unity 옛 UI Text · TextMesh는 아랍어 글자 모양(첫 · 가운데 · 끝 · 홀로)도, 오른쪽→왼쪽 순서도 모름
//   Shape  : 논리 순서 그대로 글자를 표시형(U+FE70~FEFF, 람-알리프 합자)으로 바꿈
//   Reorder: 한 줄을 화면 순서로 (바탕 = 오른쪽→왼쪽, 라틴 글자 · 숫자 덩어리는 왼쪽→오른쪽 그대로, 리치 텍스트 태그 짝 유지)
//   Visual : 줄마다 Shape + Reorder (줄 바꿈을 직접 안 하는 글: TextMesh 월드 글 등). UI 글은 LText가 줄 나눔까지 맞춰 부름
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace CatTang
{
    public static class ArabicText
    {
        // 글자 → (홀로, 끝, 첫, 가운데). 첫 · 가운데가 0이면 오른쪽(앞 글자)하고만 이어짐
        static readonly Dictionary<char, (char iso, char fin, char ini, char med)> Forms = new()
        {
            ['ء'] = ('ﺀ', '\0', '\0', '\0'),
            ['آ'] = ('ﺁ', 'ﺂ', '\0', '\0'),
            ['أ'] = ('ﺃ', 'ﺄ', '\0', '\0'),
            ['ؤ'] = ('ﺅ', 'ﺆ', '\0', '\0'),
            ['إ'] = ('ﺇ', 'ﺈ', '\0', '\0'),
            ['ئ'] = ('ﺉ', 'ﺊ', 'ﺋ', 'ﺌ'),
            ['ا'] = ('ﺍ', 'ﺎ', '\0', '\0'),
            ['ب'] = ('ﺏ', 'ﺐ', 'ﺑ', 'ﺒ'),
            ['ة'] = ('ﺓ', 'ﺔ', '\0', '\0'),
            ['ت'] = ('ﺕ', 'ﺖ', 'ﺗ', 'ﺘ'),
            ['ث'] = ('ﺙ', 'ﺚ', 'ﺛ', 'ﺜ'),
            ['ج'] = ('ﺝ', 'ﺞ', 'ﺟ', 'ﺠ'),
            ['ح'] = ('ﺡ', 'ﺢ', 'ﺣ', 'ﺤ'),
            ['خ'] = ('ﺥ', 'ﺦ', 'ﺧ', 'ﺨ'),
            ['د'] = ('ﺩ', 'ﺪ', '\0', '\0'),
            ['ذ'] = ('ﺫ', 'ﺬ', '\0', '\0'),
            ['ر'] = ('ﺭ', 'ﺮ', '\0', '\0'),
            ['ز'] = ('ﺯ', 'ﺰ', '\0', '\0'),
            ['س'] = ('ﺱ', 'ﺲ', 'ﺳ', 'ﺴ'),
            ['ش'] = ('ﺵ', 'ﺶ', 'ﺷ', 'ﺸ'),
            ['ص'] = ('ﺹ', 'ﺺ', 'ﺻ', 'ﺼ'),
            ['ض'] = ('ﺽ', 'ﺾ', 'ﺿ', 'ﻀ'),
            ['ط'] = ('ﻁ', 'ﻂ', 'ﻃ', 'ﻄ'),
            ['ظ'] = ('ﻅ', 'ﻆ', 'ﻇ', 'ﻈ'),
            ['ع'] = ('ﻉ', 'ﻊ', 'ﻋ', 'ﻌ'),
            ['غ'] = ('ﻍ', 'ﻎ', 'ﻏ', 'ﻐ'),
            ['ـ'] = ('ـ', 'ـ', 'ـ', 'ـ'),
            ['ف'] = ('ﻑ', 'ﻒ', 'ﻓ', 'ﻔ'),
            ['ق'] = ('ﻕ', 'ﻖ', 'ﻗ', 'ﻘ'),
            ['ك'] = ('ﻙ', 'ﻚ', 'ﻛ', 'ﻜ'),
            ['ل'] = ('ﻝ', 'ﻞ', 'ﻟ', 'ﻠ'),
            ['م'] = ('ﻡ', 'ﻢ', 'ﻣ', 'ﻤ'),
            ['ن'] = ('ﻥ', 'ﻦ', 'ﻧ', 'ﻨ'),
            ['ه'] = ('ﻩ', 'ﻪ', 'ﻫ', 'ﻬ'),
            ['و'] = ('ﻭ', 'ﻮ', '\0', '\0'),
            ['ى'] = ('ﻯ', 'ﻰ', '\0', '\0'),
            ['ي'] = ('ﻱ', 'ﻲ', 'ﻳ', 'ﻴ'),
            ['پ'] = ('ﭖ', 'ﭗ', 'ﭘ', 'ﭙ'),
            ['چ'] = ('ﭺ', 'ﭻ', 'ﭼ', 'ﭽ'),
            ['ژ'] = ('ﮊ', 'ﮋ', '\0', '\0'),
            ['ک'] = ('ﮎ', 'ﮏ', 'ﮐ', 'ﮑ'),
            ['گ'] = ('ﮒ', 'ﮓ', 'ﮔ', 'ﮕ'),
            ['ی'] = ('ﯼ', 'ﯽ', 'ﯾ', 'ﯿ'),
        };

        // 람 + 알리프 류 → (홀로, 끝) 합자
        static readonly Dictionary<char, (char iso, char fin)> LamAlef = new()
        {
            ['آ'] = ('ﻵ', 'ﻶ'),
            ['أ'] = ('ﻷ', 'ﻸ'),
            ['إ'] = ('ﻹ', 'ﻺ'),
            ['ا'] = ('ﻻ', 'ﻼ'),
        };

        static readonly Regex Tag = new(@"</?[a-zA-Z]+(=[^>]*)?>", RegexOptions.Compiled);

        public static bool IsArabic(char c) =>
            (c >= '؀' && c <= 'ۿ') || (c >= 'ݐ' && c <= 'ݿ') || (c >= 'ﭐ' && c <= '﷿') || (c >= 'ﹰ' && c <= '﻿');

        public static bool Has(string s)
        {
            if (string.IsNullOrEmpty(s)) return false;
            foreach (var c in s) if (IsArabic(c)) return true;
            return false;
        }

        /// <summary>모음 부호 등 잇기에 상관없는 글자 (양옆 글자끼리 이어 봄)</summary>
        static bool Transparent(char c) => (c >= 'ً' && c <= 'ٟ') || c == 'ٰ' || (c >= 'ۖ' && c <= 'ۭ');

        static bool JoinsLeft(char c) => Forms.TryGetValue(c, out var f) && f.ini != '\0';       // 다음 글자(왼쪽)와 이어짐
        static bool JoinsRight(char c) => Forms.TryGetValue(c, out var f) && f.fin != '\0';      // 앞 글자(오른쪽)와 이어짐

        /// <summary>논리 순서 그대로 표시형 글자로 (태그 · 라틴 글자는 그대로, 태그는 잇기를 끊음)</summary>
        public static string Shape(string s)
        {
            if (!Has(s)) return s;
            var sb = new StringBuilder(s.Length);
            int n = s.Length;
            // 태그 자리 표시 (태그 안의 글자는 건너뜀)
            var inTag = new bool[n];
            foreach (Match m in Tag.Matches(s)) for (int k = m.Index; k < m.Index + m.Length; k++) inTag[k] = true;

            char Prev(int i) { for (int k = i - 1; k >= 0; k--) { if (inTag[k]) return '\0'; if (!Transparent(s[k])) return s[k]; } return '\0'; }
            char NextAt(int i, out int at) { for (int k = i + 1; k < n; k++) { if (inTag[k]) { at = -1; return '\0'; } if (!Transparent(s[k])) { at = k; return s[k]; } } at = -1; return '\0'; }

            for (int i = 0; i < n; i++)
            {
                char c = s[i];
                if (inTag[i] || !Forms.TryGetValue(c, out var f)) { sb.Append(c); continue; }
                char p = Prev(i);
                bool joinPrev = p != '\0' && JoinsLeft(p) && JoinsRight(c);
                char nx = NextAt(i, out int nAt);

                // 람 + 알리프 → 합자 (알리프 자리는 건너뜀)
                if (c == 'ل' && nx != '\0' && LamAlef.TryGetValue(nx, out var la))
                {
                    sb.Append(joinPrev ? la.fin : la.iso);
                    for (int k = i + 1; k < nAt; k++) sb.Append(s[k]);                         // 사이의 모음 부호
                    i = nAt;
                    continue;
                }
                bool joinNext = nx != '\0' && JoinsLeft(c) && JoinsRight(nx);
                sb.Append(joinPrev && joinNext ? f.med : joinPrev ? f.fin : joinNext ? f.ini : f.iso);
            }
            return sb.ToString();
        }

        enum Dir { R, L, N }

        static Dir Kind(char c)
        {
            if (IsArabic(c)) return (c >= '٠' && c <= '٩') || (c >= '۰' && c <= '۹') ? Dir.L : Dir.R;
            if (char.IsLetterOrDigit(c)) return Dir.L;
            return Dir.N;
        }

        static char Mirror(char c) => c switch
        {
            '(' => ')', ')' => '(', '[' => ']', ']' => '[', '{' => '}', '}' => '{',
            '«' => '»', '»' => '«', '‹' => '›', '›' => '‹', '<' => '>', '>' => '<',
            _ => c
        };

        struct Tok { public string text; public bool tag; public Dir dir; public int pair; public bool close; }

        /// <summary>한 줄(줄바꿈 없음)을 화면 순서로. 바탕 방향 = 오른쪽→왼쪽</summary>
        public static string Reorder(string line)
        {
            if (!Has(line)) return line;
            // 1) 토큰: 태그 하나 = 토큰 하나, 나머지는 글자마다
            var toks = new List<Tok>(line.Length);
            int i = 0;
            var stack = new List<(string name, int idx)>();
            while (i < line.Length)
            {
                var m = line[i] == '<' ? Tag.Match(line, i) : null;
                if (m != null && m.Success && m.Index == i)
                {
                    var t = new Tok { text = m.Value, tag = true, dir = Dir.N, pair = -1, close = m.Value.StartsWith("</") };
                    string name = m.Value.TrimStart('<', '/').Split('=', '>')[0].ToLowerInvariant();
                    if (t.close)
                    {
                        for (int k = stack.Count - 1; k >= 0; k--)
                            if (stack[k].name == name) { t.pair = stack[k].idx; var o = toks[stack[k].idx]; o.pair = toks.Count; toks[stack[k].idx] = o; stack.RemoveAt(k); break; }
                    }
                    else stack.Add((name, toks.Count));
                    toks.Add(t);
                    i += m.Length;
                    continue;
                }
                toks.Add(new Tok { text = line[i].ToString(), dir = Kind(line[i]), pair = -1 });
                i++;
            }

            // 2) 중립(공백 · 기호 · 태그): 양옆 강한 글자가 둘 다 L이면 L, 아니면 바탕(R)
            int nTok = toks.Count;
            var dirs = new Dir[nTok];
            for (int k = 0; k < nTok; k++) dirs[k] = toks[k].dir;
            for (int k = 0; k < nTok; k++)
            {
                if (dirs[k] != Dir.N) continue;
                int a = k - 1; while (a >= 0 && toks[a].dir == Dir.N) a--;
                int b = k + 1; while (b < nTok && toks[b].dir == Dir.N) b++;
                bool l = a >= 0 && b < nTok && toks[a].dir == Dir.L && toks[b].dir == Dir.L;
                dirs[k] = l ? Dir.L : Dir.R;
            }

            // 3) 덩어리(run)로 묶어 순서를 뒤집음: R 덩어리는 안쪽도 뒤집고, L 덩어리는 그대로
            var order = new List<int>(nTok);
            int s0 = 0;
            var runs = new List<(int from, int to, Dir d)>();
            for (int k = 1; k <= nTok; k++)
                if (k == nTok || dirs[k] != dirs[s0]) { runs.Add((s0, k, dirs[s0])); s0 = k; }
            for (int r = runs.Count - 1; r >= 0; r--)
            {
                var (from, to, d) = runs[r];
                if (d == Dir.L) for (int k = from; k < to; k++) order.Add(k);
                else for (int k = to - 1; k >= from; k--) order.Add(k);
            }

            // 4) 태그 짝: 화면 순서에서 닫는 태그가 여는 태그보다 앞에 오면 둘의 글을 맞바꿈
            var pos = new int[nTok];
            for (int k = 0; k < order.Count; k++) pos[order[k]] = k;
            var outText = new string[order.Count];
            for (int k = 0; k < order.Count; k++)
            {
                var t = toks[order[k]];
                string txt = t.text;
                if (!t.tag && dirs[order[k]] == Dir.R) txt = Mirror(txt[0]).ToString();
                outText[k] = txt;
            }
            for (int k = 0; k < nTok; k++)
            {
                var t = toks[k];
                if (!t.tag || t.close || t.pair < 0) continue;
                int po = pos[k], pc = pos[t.pair];
                if (pc < po) { var tmp = outText[po]; outText[po] = outText[pc]; outText[pc] = tmp; }
            }
            return string.Concat(outText);
        }

        /// <summary>줄마다 Shape + Reorder (자동 줄바꿈 없는 글)</summary>
        public static string Visual(string s)
        {
            if (!Has(s)) return s;
            var lines = s.Split('\n');
            for (int i = 0; i < lines.Length; i++) lines[i] = Reorder(Shape(lines[i]));
            return string.Join("\n", lines);
        }
    }
}
