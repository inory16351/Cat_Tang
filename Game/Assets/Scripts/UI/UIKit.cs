// UI 만들기 도구: 스킨 이미지(Resources/UI, 9-슬라이스) · 글꼴(Black Han Sans 제목 / Gothic A1 본문) · 패널 · 버튼 · 글 · 아이콘
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace CatTang
{
    [Serializable]
    class SkinJson
    {
        public int[] panel_plain, tooltip, button, button_hover, slot, banner, hp_fill, hp_interior;
    }

    public static class UIKit
    {
        public static Font Title, Body, BodyBold;
        static readonly Dictionary<string, Sprite> sprites = new();
        static SkinJson skin;

        public static readonly Color Cream = new Color(0.96f, 0.93f, 0.86f);
        public static readonly Color Gold = new Color(0.98f, 0.78f, 0.32f);
        public static readonly Color Muted = new Color(0.72f, 0.68f, 0.78f);
        public static readonly Color Ink = new Color(0.07f, 0.05f, 0.1f, 0.88f);
        public static readonly Color Red = new Color(0.86f, 0.28f, 0.22f);
        public static readonly Color Blue = new Color(0.45f, 0.8f, 1f);

        public static void Init()
        {
            if (Title != null && skin != null) return;
            Title = Resources.Load<Font>("Fonts/BlackHanSans-Regular");
            Body = Resources.Load<Font>("Fonts/GothicA1-Regular");
            BodyBold = Resources.Load<Font>("Fonts/GothicA1-Bold");
            var fallback = Font.CreateDynamicFontFromOSFont(new[] { "Malgun Gothic", "Arial" }, 28);
            Title ??= fallback; Body ??= fallback; BodyBold ??= Body;
            var js = Resources.Load<TextAsset>("UI/ui_skin");
            skin = js ? JsonUtility.FromJson<SkinJson>(js.text) : new SkinJson();
        }

        public static int[] HpInterior => skin?.hp_interior;

        /// <summary>Resources/UI/name 텍스처를 스프라이트로 (테두리 값이 있으면 9-슬라이스)</summary>
        public static Sprite Spr(string name)
        {
            Init();
            if (sprites.TryGetValue(name, out var s) && s) return s;
            var tex = Resources.Load<Texture2D>("UI/" + name);
            if (!tex) return null;
            int[] b = name switch
            {
                "panel_plain" => skin.panel_plain, "tooltip" => skin.tooltip, "button" => skin.button, "button_hover" => skin.button_hover,
                "slot" => skin.slot, "banner" => skin.banner, "hp_fill" => skin.hp_fill, _ => null
            };
            var border = b != null && b.Length == 4 ? new Vector4(b[0], b[1], b[2], b[3]) : Vector4.zero;
            s = UnityEngine.Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100, 0, SpriteMeshType.FullRect, border);
            s.name = name;
            sprites[name] = s;
            return s;
        }

        public static Sprite Icon(string path)
        {
            if (sprites.TryGetValue(path, out var s) && s) return s;
            var tex = Resources.Load<Texture2D>(path);
            if (!tex) return null;
            s = UnityEngine.Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));
            sprites[path] = s;
            return s;
        }

        public static Sprite BulletIcon(BulletDef b) => b == null ? null : Icon("Icons/bullets/bullet_" + b.Id);
        public static Sprite GunIcon(GunDef g) => g == null ? null : Icon("Icons/guns/gun_" + g.Id);

        public static Color GradeColor(string g) => g switch
        {
            "Common" => new Color(0.75f, 0.77f, 0.8f), "Rare" => new Color(0.42f, 0.66f, 1f),
            "Epic" => new Color(0.78f, 0.5f, 1f), "Legendary" => new Color(1f, 0.74f, 0.25f), _ => Color.white
        };

        // ---------- 만들기 ----------
        public static RectTransform MakeRect(string name, Transform parent, Vector2 anchor, Vector2 pos, Vector2 size, Vector2? pivot = null)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = rt.anchorMax = anchor;
            rt.pivot = pivot ?? anchor;
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            return rt;
        }

        public static RectTransform Stretch(string name, Transform parent)
        {
            var rt = MakeRect(name, parent, Vector2.zero, Vector2.zero, Vector2.zero);
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.offsetMin = rt.offsetMax = Vector2.zero;
            return rt;
        }

        public static Image Img(string name, Transform parent, string sprite, Vector2 anchor, Vector2 pos, Vector2 size, bool sliced = false, float ppuMul = 1f)
        {
            var rt = MakeRect(name, parent, anchor, pos, size);
            var im = rt.gameObject.AddComponent<Image>();
            im.sprite = Spr(sprite);
            im.raycastTarget = false;
            if (sliced && im.sprite) { im.type = Image.Type.Sliced; im.pixelsPerUnitMultiplier = ppuMul; }
            else im.preserveAspect = true;
            return im;
        }

        /// <summary>스킨 패널 (위아래 대칭 놋쇠 테두리). ornament = 아래 가운데 톱니 장식</summary>
        public static Image Panel(string name, Transform parent, Vector2 anchor, Vector2 pos, Vector2 size, bool ornament = false, string skinName = "panel_plain", float ppuMul = 1.4f)
        {
            var im = Img(name, parent, skinName, anchor, pos, size, true, ppuMul);
            im.raycastTarget = true;
            if (ornament)
            {
                var o = Img("Ornament", im.transform, "panel_ornament", new Vector2(0.5f, 0), new Vector2(0, -24), new Vector2(220, 80));
                o.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            }
            return im;
        }

        public static Text Label(string name, Transform parent, Vector2 anchor, Vector2 pos, Vector2 size, int fontSize, TextAnchor align, bool title = false, Color? color = null)
        {
            Init();
            var rt = MakeRect(name, parent, anchor, pos, size);
            var t = rt.gameObject.AddComponent<Text>();
            t.font = title ? Title : Body;
            t.fontSize = fontSize;
            t.alignment = align;
            t.color = color ?? Cream;
            t.supportRichText = true;
            t.raycastTarget = false;
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            var sh = rt.gameObject.AddComponent<Shadow>();
            sh.effectColor = new Color(0, 0, 0, 0.75f);
            sh.effectDistance = new Vector2(2, -2);
            return t;
        }

        /// <summary>제목 리본 배너 + 글</summary>
        public static Text Banner(string text, Transform parent, Vector2 anchor, Vector2 pos, float width = 520)
        {
            var b = Img("Banner", parent, "banner", anchor, pos, new Vector2(width, width * 0.3f), true, 1.2f);
            var t = Label("Text", b.transform, new Vector2(0.5f, 0.5f), new Vector2(0, 6), new Vector2(width - 160, width * 0.2f), 34, TextAnchor.MiddleCenter, true);
            t.text = text;
            return t;
        }

        public static Button MakeButton(string name, Transform parent, Vector2 anchor, Vector2 pos, Vector2 size, string label, UnityAction onClick, int fontSize = 28)
        {
            var im = Img(name, parent, "button", anchor, pos, size, true, 2.2f);
            im.raycastTarget = true;
            var b = im.gameObject.AddComponent<Button>();
            b.transition = Selectable.Transition.SpriteSwap;
            var st = b.spriteState;
            st.highlightedSprite = Spr("button_hover");
            st.pressedSprite = Spr("button_hover");
            st.selectedSprite = Spr("button");
            b.spriteState = st;
            b.targetGraphic = im;
            if (onClick != null) b.onClick.AddListener(onClick);
            var t = Label("Text", im.transform, new Vector2(0.5f, 0.5f), new Vector2(0, 2), size - new Vector2(60, 16), fontSize, TextAnchor.MiddleCenter, true);
            t.text = label;
            return b;
        }

        public static void SetInteractable(Button b, bool on)
        {
            b.interactable = on;
            var t = b.GetComponentInChildren<Text>();
            if (t) t.color = on ? Cream : new Color(0.6f, 0.58f, 0.62f);
            var im = b.GetComponent<Image>();
            if (im) im.color = on ? Color.white : new Color(0.65f, 0.62f, 0.65f);
        }

        public static Button CloseButton(Transform parent, UnityAction onClick)
        {
            var im = Img("Close", parent, "close", new Vector2(1, 1), new Vector2(18, 18), new Vector2(76, 76));
            im.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            im.raycastTarget = true;
            var b = im.gameObject.AddComponent<Button>();
            b.onClick.AddListener(onClick);
            return b;
        }

        /// <summary>전체 화면 어둡게 깔기</summary>
        public static Image Dim(Transform parent, float a = 0.72f)
        {
            var rt = Stretch("Dim", parent);
            var im = rt.gameObject.AddComponent<Image>();
            im.color = new Color(0.04f, 0.03f, 0.07f, a);
            return im;
        }

        public static string GradeKo(string g) => g switch { "Common" => "일반", "Rare" => "희귀", "Epic" => "영웅", "Legendary" => "전설", _ => g };

        public static string CondKo(BulletDef b) => b.CondType switch
        {
            "Odd" => "홀수 시각", "Even" => "짝수 시각", "Exact" => $"{b.CondValue}시", "GreaterThan" => $"{b.CondValue}시 초과",
            "LessThan" => $"{b.CondValue}시 미만", "MultipleOf" => $"{b.CondValue}의 배수", "Negative" => "음수 시각",
            "Zero" => "0시", "Prime" => "소수 시각", _ => b.CondType
        };
    }
}
