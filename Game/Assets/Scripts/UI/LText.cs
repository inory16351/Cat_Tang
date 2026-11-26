// 다국어 글 (2026-11-26): 코드가 text에 한국어 원문을 넣으면 지금 언어로 바꿔 보여 줌 · 언어가 바뀌면 원문으로 다시 번역
// UIKit.Label 등 글을 만드는 곳이 Text 대신 이것을 붙임 (GetComponent<Text>()로도 그대로 잡힘)
using UnityEngine.UI;

namespace CatTang
{
    public class LText : Text
    {
        string src;

        public override string text
        {
            get => base.text;
            set { src = value; base.text = Loc.T(value); }
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            Loc.Changed += Retranslate;
            if (src != null) { var t = Loc.T(src); if (t != base.text) base.text = t; }
        }

        protected override void OnDisable()
        {
            Loc.Changed -= Retranslate;
            base.OnDisable();
        }

        void Retranslate() { if (src != null) base.text = Loc.T(src); }
    }
}
