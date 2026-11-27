// 다국어 글 (2026-11-26): 코드가 text에 한국어 원문을 넣으면 지금 언어로 바꿔 보여 줌 · 언어가 바뀌면 원문으로 다시 번역
// UIKit.Label 등 글을 만드는 곳이 Text 대신 이것을 붙임 (GetComponent<Text>()로도 그대로 잡힘)
// 2026-11-27 다국어 6개: 언어가 바뀌면 글꼴도 같은 역할(제목 · 본문 · 굵게)의 그 언어 글꼴로 (LangFonts)
//   아랍어 글이 있으면 그릴 때만 글자 잇기 + 줄마다 오른쪽→왼쪽 순서 (자동 줄바꿈 위치는 논리 순서로 먼저 재서 맞춤),
//   아랍어 모드면 왼쪽 정렬 ↔ 오른쪽 정렬. text 값(논리 순서)은 그대로라 TermText · 길이 재기 등은 그대로 동작
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.UI;

namespace CatTang
{
    public class LText : Text
    {
        string src;
        string fontLang;                                                 // 지금 글꼴을 본 때의 언어 (언어가 바뀌면 그 언어 기준 역할로 갈아 끼움)
        Font fontSeen;

        public override string text
        {
            get => base.text;
            set { src = value; SyncFont(); base.text = Loc.T(value); }
        }

        /// <summary>원문은 그대로 두고 보이는 글만 바꿈 (TermText 고유명사 색칠)</summary>
        public void SetShown(string shown) => base.text = shown;

        protected override void OnEnable()
        {
            base.OnEnable();
            Loc.Changed += Retranslate;
            SyncFont();
            if (src != null) { var t = Loc.T(src); if (t != base.text) base.text = t; }
        }

        protected override void OnDisable()
        {
            Loc.Changed -= Retranslate;
            base.OnDisable();
        }

        void Retranslate()
        {
            SyncFont();
            if (src != null) base.text = Loc.T(src);
        }

        /// <summary>글꼴을 지금 언어에 맞춤: 코드가 넣은 글꼴은 그때 언어 기준으로 역할을 알아내 지금 언어의 같은 역할로</summary>
        void SyncFont()
        {
            var lang = Loc.Lang;
            var f = font;
            if (f != fontSeen) fontLang = lang;                          // 코드가 방금 넣은 글꼴 = 지금 언어 기준
            if (fontLang != lang && f)
            {
                int r = LangFonts.RoleOf(f, fontLang);
                if (r >= 0) { var nf = LangFonts.Get(r); if (nf && nf != f) font = nf; }
            }
            fontSeen = font; fontLang = lang;
        }

        // ---------------------------------------------------------------- 아랍어 (RTL)
        static readonly FieldInfo FontDataField = typeof(Text).GetField("m_FontData", BindingFlags.NonPublic | BindingFlags.Instance);
        static readonly Regex Tag = new(@"</?[a-zA-Z]+(=[^>]*)?>", RegexOptions.Compiled);
        static TextGenerator measure;
        string visualFor, visualOut;
        float visualW; int visualSize;

        protected override void OnPopulateMesh(VertexHelper toFill)
        {
            string logical = m_Text;
            if (font == null || !ArabicText.Has(logical)) { base.OnPopulateMesh(toFill); return; }
            var fd = FontDataField?.GetValue(this) as FontData;
            var align0 = alignment;
            if (fd != null && Loc.RTL) fd.alignment = Mirror(align0);
            float w = rectTransform.rect.width;
            if (visualFor != logical || !Mathf.Approximately(visualW, w) || visualSize != fontSize)
            {
                visualOut = Visual(logical);
                visualFor = logical; visualW = w; visualSize = fontSize;
            }
            m_Text = visualOut;
            try { base.OnPopulateMesh(toFill); }
            finally { m_Text = logical; if (fd != null) fd.alignment = align0; }
        }

        static TextAnchor Mirror(TextAnchor a) => a switch
        {
            TextAnchor.UpperLeft => TextAnchor.UpperRight, TextAnchor.UpperRight => TextAnchor.UpperLeft,
            TextAnchor.MiddleLeft => TextAnchor.MiddleRight, TextAnchor.MiddleRight => TextAnchor.MiddleLeft,
            TextAnchor.LowerLeft => TextAnchor.LowerRight, TextAnchor.LowerRight => TextAnchor.LowerLeft,
            _ => a
        };

        /// <summary>논리 순서 글 → 화면에 그릴 글 (줄 나눔 → 줄마다 화면 순서)</summary>
        string Visual(string logical)
        {
            string shaped = ArabicText.Shape(logical);
            List<string> lines = horizontalOverflow == HorizontalWrapMode.Wrap ? WrapLines(shaped) : new List<string>(shaped.Split('\n'));
            var sb = new StringBuilder(shaped.Length + 16);
            for (int i = 0; i < lines.Count; i++)
            {
                if (i > 0) sb.Append('\n');
                sb.Append(ArabicText.Reorder(lines[i]));
            }
            return sb.ToString();
        }

        /// <summary>자동 줄바꿈 위치를 (태그 뺀) 논리 순서 글로 재서 줄로 나눔. 여러 줄에 걸친 태그는 줄마다 닫고 다시 엶</summary>
        List<string> WrapLines(string s)
        {
            bool rich = supportRichText;
            // 태그 뺀 글 + 원래 위치
            var plain = new StringBuilder(s.Length);
            var map = new List<int>(s.Length);
            for (int i = 0; i < s.Length;)
            {
                if (rich && s[i] == '<') { var m = Tag.Match(s, i); if (m.Success && m.Index == i) { i += m.Length; continue; } }
                plain.Append(s[i]); map.Add(i); i++;
            }
            var settings = GetGenerationSettings(rectTransform.rect.size);
            settings.richText = false;
            measure ??= new TextGenerator();
            measure.Populate(plain.ToString(), settings);
            var info = measure.lines;

            // 줄 경계 (원래 글 위치): 앞 줄 마지막 글자 바로 뒤. 닫는 태그는 앞 줄, 여는 태그는 다음 줄
            var cuts = new List<int> { 0 };
            for (int k = 1; k < info.Count; k++)
            {
                int ps = info[k].startCharIdx;
                if (ps <= 0 || ps > map.Count) continue;
                int b = map[ps - 1] + 1;
                while (rich && b < s.Length && s[b] == '<' && b + 1 < s.Length && s[b + 1] == '/')
                {
                    var m = Tag.Match(s, b); if (!m.Success || m.Index != b) break; b += m.Length;
                }
                if (b > cuts[cuts.Count - 1]) cuts.Add(b);
            }
            cuts.Add(s.Length);

            var lines = new List<string>(cuts.Count);
            var open = new List<(string name, string tag)>();
            for (int k = 0; k + 1 < cuts.Count; k++)
            {
                string seg = s.Substring(cuts[k], cuts[k + 1] - cuts[k]).TrimEnd('\n', ' ');
                var lb = new StringBuilder();
                foreach (var o in open) lb.Append(o.tag);                // 앞 줄에서 열린 태그 다시 엶
                lb.Append(seg);
                if (rich)
                    foreach (Match m in Tag.Matches(seg))
                    {
                        string name = m.Value.TrimStart('<', '/').Split('=', '>')[0].ToLowerInvariant();
                        if (m.Value.StartsWith("</")) { for (int q = open.Count - 1; q >= 0; q--) if (open[q].name == name) { open.RemoveAt(q); break; } }
                        else open.Add((name, m.Value));
                    }
                for (int q = open.Count - 1; q >= 0; q--) lb.Append("</").Append(open[q].name).Append('>');   // 이 줄에서 닫음
                lines.Add(lb.ToString());
            }
            return lines;
        }
    }
}
