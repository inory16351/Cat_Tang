// 고유명사 (2026-11-18 사용자: 이 게임에서만 쓰는 낱말 설명)
// - 화면의 모든 글(UIKit.Label → TermText)이 고유명사를 파란색으로 칠함 (글이 바뀔 때마다 다시)
// - 설명은 낱말에 마우스를 올리는 링크 방식이 아니라(사용자: 호버링 UI라 불편), 세피리아 아이템 툴팁처럼
//   CursorTip이 뜰 때 그 툴팁 글 속 고유명사의 설명 상자를 툴팁 옆에 작게 쌓아 띄움 (CursorTip.Fill → Terms.Find)
// 낱말 표: 아래 Fixed + 특성 표(Trait.csv)의 이름 · 설명. 새 고유명사는 Fixed에 한 줄 추가
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace CatTang
{
    public static class Terms
    {
        public const string LinkHex = "#2E9BE6";                 // 링크 색 (파랑: 크림 카드 · 어두운 HUD 양쪽에서 읽히고 금빛 · 크림 글자와 갈림)
        public static readonly Color LinkColor = new(0.45f, 0.75f, 1f);

        // 설명할 낱말 (2026-11-18 사용자: 특성 설명 + 연산은 판정 시각 정도만. 보면 알거나 장르에 익숙한 개념(특성 · 연산 슬롯 등)은 설명 안 함)
        static readonly (string name, string desc)[] Fixed =
        {
            ("판정 시각", "탄이 놓인 시각에 연산을 적용한 값. 이 값이 자리 조건에 맞으면 탄이 활성화된다.\n12를 넘거나 0 이하가 되어도 그대로 둔다 (-6시 · 14시 등)."),
        };

        static Dictionary<string, string> map;
        static Regex re;

        static string builtFor;
        static void Ensure()
        {
            if (re != null && builtFor == Loc.Lang) return;
            builtFor = Loc.Lang;
            re = null;
            map = new Dictionary<string, string>();
            // 다국어: 낱말 · 설명도 지금 언어로 (화면 글이 이미 번역된 뒤에 칠하므로)
            foreach (var (n, d) in Fixed) map[Loc.T(n)] = Loc.T(d);
            foreach (var t in Tables.Traits.Values)
                if (!string.IsNullOrEmpty(t.Name) && !map.ContainsKey(Loc.T(t.Name))) map[Loc.T(t.Name)] = Loc.T(t.Desc);
            if (map.Count == 0) return;
            // 긴 이름부터 (연산 슬롯 > 연산), 앞 글자가 한글이면 다른 낱말의 일부로 봄 (영어는 낱말 경계)
            var keys = map.Keys.OrderByDescending(k => k.Length).Select(Regex.Escape);
            re = Loc.Korean ? new Regex($"(?<![가-힣])(?:{string.Join("|", keys)})", RegexOptions.CultureInvariant)
               : Loc.NoWordSpaces ? new Regex($"(?:{string.Join("|", keys)})", RegexOptions.CultureInvariant)     // 일본어 · 중국어: 낱말 경계 없음
                            : new Regex($@"\b(?:{string.Join("|", keys)})\b", RegexOptions.CultureInvariant);
        }

        /// <summary>표가 다시 읽힌 뒤(특성 이름이 바뀜) 다시 만들게</summary>
        public static void Reset() { re = null; map = null; }

        public static string Desc(string term) { Ensure(); return map != null && map.TryGetValue(term, out var d) ? d : null; }

        static readonly int[] Tiers = { 2, 4, 6 };
        const string On = "#FFD54A", Off = "#8C8696";

        /// <summary>
        /// 특성 설명 상자 (롤체 시너지처럼, 2026-11-18 사용자): 제목 = 이름 + 단계 2 / 4 / 6 (닿은 단계는 금빛) + 지금 활성화된 수,
        /// 본문 = 한 줄 설명 + 단계별 효과(켜진 단계 금빛 · 아직 회색) + 다음 단계까지 남은 수. 특성이 아니면 false
        /// </summary>
        public static bool TraitBox(string term, out string title, out string body)
        {
            title = body = null;
            var t = Tables.Traits.Values.FirstOrDefault(x => x.Name == term);
            if (t == null) return false;
            var rev = PlayerController.Instance ? PlayerController.Instance.rev : null;
            int n = 0, tier = 0, red = 0;
            if (rev != null) { rev.TraitCounts().TryGetValue(t.Id, out n); tier = rev.TierOf(n); red = rev.TierThresholdReduce; }
            var steps = string.Join(" / ", Tiers.Select(k => $"<color={(tier >= k ? On : Off)}>{k - red}</color>"));
            title = $"{t.Name}   <size=15>{steps}   <color=#F3EDE2>활성화 {n}</color></size>";
            var lines = new List<string> { t.Desc, "" };
            foreach (var (trait, tr, effect) in Tables.TraitTiers.Where(x => x.trait == t.Id).OrderBy(x => x.tier))
            {
                string d = Tables.Effects.TryGetValue(effect, out var e) ? e.Desc : "";
                lines.Add($"<color={(tier == tr ? On : Off)}><b>({tr - red})</b> {d}</color>");
            }
            int next = Tiers.Select(k => k - red).Where(k => k > n).DefaultIfEmpty(0).First();
            lines.Add(next > 0 ? $"<size=13><color={Off}>다음 단계까지 {next - n}개</color></size>" : $"<size=13><color={On}>최고 단계</color></size>");
            body = string.Join("\n", lines);
            return true;
        }

        static readonly Regex Tag = new("<[^>]+>");

        /// <summary>글 속 고유명사를 링크 색으로 칠함. 태그 안 · 이미 칠한 글은 건드리지 않음</summary>
        public static string Colorize(string s)
        {
            if (string.IsNullOrEmpty(s) || s.Contains(LinkHex)) return s;
            Ensure();
            if (re == null) return s;
            var sb = new System.Text.StringBuilder(s.Length + 32);
            int last = 0;
            foreach (Match tag in Tag.Matches(s))
            {
                sb.Append(re.Replace(s.Substring(last, tag.Index - last), Wrap));
                sb.Append(tag.Value);
                last = tag.Index + tag.Length;
            }
            sb.Append(re.Replace(s.Substring(last), Wrap));
            return sb.ToString();
        }

        static string Wrap(Match m) => $"<color={LinkHex}>{m.Value}</color>";

        /// <summary>글 속 고유명사를 나온 순서대로 (같은 낱말은 한 번, 태그 제외)</summary>
        public static List<string> Find(string s, int max = 4)
        {
            var list = new List<string>();
            if (string.IsNullOrEmpty(s)) return list;
            Ensure();
            if (re == null) return list;
            foreach (Match m in re.Matches(Tag.Replace(s, " ")))
                if (!list.Contains(m.Value)) { list.Add(m.Value); if (list.Count >= max) break; }
            return list;
        }
    }

    /// <summary>UIKit.Label이 모든 글에 붙임: 글이 바뀌면 고유명사를 칠함</summary>
    [RequireComponent(typeof(Text))]
    public class TermText : MonoBehaviour
    {
        public Text text;
        string lastSet;

        void Awake() => text = GetComponent<Text>();

        void LateUpdate()
        {
            if (!text || ReferenceEquals(text.text, lastSet)) return;
            var c = Terms.Colorize(text.text);
            if (!ReferenceEquals(c, text.text))
            {
                if (text is LText lt) lt.SetShown(c);                    // 원문(번역 전 한국어)은 그대로 둠 — 언어를 바꾸면 원문에서 다시 번역
                else text.text = c;
            }
            lastSet = text.text;
        }
    }
}
