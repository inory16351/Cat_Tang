// 환경설정 내용 (2026-11-26 4탭): 로비 '설정' 창과 판 · 마을의 Esc 일시정지 창이 같이 씀
//   위 = 탭 머리 4개(소리 · 화면 · 게임 · 조작, 그림 UI/Settings/tab · tab_on + 아이콘), 아래 = settings_frame 테두리 안에 줄(row)들
//   소리: 전체 · 음악 · 효과음 음량 · 창 밖에서 소리 끄기
//   화면: 창 모드 · 해상도 · 수직 동기화 · 화면 흔들림 · 번쩍임 줄이기 · UI 크기
//   게임: 언어 · 대사 자동 넘김 · 글자 속도 · 조준선 모양 · 조준선 크기 · 보스 패턴 연습(장마다 체크)
//   조작: KeyBindsUI (단축키 바꾸기)
//   마지막에 본 탭은 기억 (창을 다시 열면 그 탭)
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace CatTang
{
    public static class SettingsUI
    {
        const float TabH = 72f, RowH = 52f, RowStep = 58f, LabelX = 36f, CtrlX = 250f, CtrlW = 430f;

        static int tab;                                                      // 마지막에 본 탭
        static readonly string[] TabNames = { "소리", "화면", "게임", "조작" };
        static readonly string[] TabIcons = { "icon_sound", "icon_display", "icon_game", "icon_keys" };

        static Sprite L(string n) => UIKit.Icon("UI/Lobby/" + n);

        /// <summary>환경설정 그림 (UI/Settings, 임포트 설정의 9-slice 테두리 그대로)</summary>
        public static Sprite Art(string n) => Resources.Load<Sprite>("UI/Settings/" + n);

        static Material Fx(float hatch = 0.4f)
        {
            var sh = Shader.Find("CatTang/UIFlatFx");
            if (!sh) return null;
            var m = new Material(sh);
            m.SetFloat("_Hatch", hatch); m.SetFloat("_ShineEvery", 3.5f);
            return m;
        }

        static Image Sliced(RectTransform rt, string art, float ppuMul)
        {
            var img = rt.gameObject.AddComponent<Image>();
            var s = Art(art);
            if (s) { img.sprite = s; img.type = Image.Type.Sliced; img.pixelsPerUnitMultiplier = ppuMul; }
            else { img.sprite = UIKit.Spr("panel_plain"); img.type = Image.Type.Sliced; }
            return img;
        }

        /// <summary>body(가운데 기준 약 1060 × 480) 안에 탭 + 설정 내용을 채움. top · gap은 예전 호출 호환용 (안 씀)</summary>
        public static void Build(RectTransform body, float top = 170f, float gap = 92f)
        {
            for (int i = body.childCount - 1; i >= 0; i--) Object.Destroy(body.GetChild(i).gameObject);
            var kb = body.GetComponent<KeyBindsUI>(); if (kb) Object.Destroy(kb);

            float w = body.rect.width > 10 ? body.rect.width : 1060f;

            // 내용 테두리 (탭 아래 전부)
            var frame = UIKit.MakeRect("Frame", body, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            frame.anchorMin = Vector2.zero; frame.anchorMax = Vector2.one;
            frame.offsetMin = Vector2.zero; frame.offsetMax = new Vector2(0, -TabH + 6);
            var fi = Sliced(frame, "settings_frame", 2.6f);
            fi.raycastTarget = false;

            var content = UIKit.MakeRect("Content", frame, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            content.anchorMin = Vector2.zero; content.anchorMax = Vector2.one;
            content.offsetMin = new Vector2(30, 24); content.offsetMax = new Vector2(-30, -24);

            // 탭 머리
            var heads = new List<(Image bg, Text label, Image icon)>();
            float tw = Mathf.Min(250f, (w - 40f) / TabNames.Length);
            for (int i = 0; i < TabNames.Length; i++)
            {
                int k = i;
                var rt = UIKit.MakeRect("Tab_" + i, body, new Vector2(0, 1), new Vector2(20 + tw * i + tw / 2, -TabH / 2), new Vector2(tw - 8, TabH));
                rt.pivot = new Vector2(0.5f, 0.5f);
                var bg = Sliced(rt, "tab", 2.2f);
                var icon = UIKit.MakeRect("Icon", rt, new Vector2(0, 0.5f), new Vector2(40, 2), new Vector2(42, 42)).gameObject.AddComponent<Image>();
                icon.sprite = Art(TabIcons[i]); icon.preserveAspect = true; icon.raycastTarget = false;
                if (!icon.sprite) icon.enabled = false;
                var lb = UIKit.Label("T", rt, new Vector2(0.5f, 0.5f), new Vector2(32, 2), new Vector2(tw - 100, TabH - 12), 28, TextAnchor.MiddleCenter, true);
                UIKit.NoFit(lb, 20);
                lb.text = TabNames[i];
                var b = rt.gameObject.AddComponent<Button>();
                b.targetGraphic = bg;
                b.onClick.AddListener(() => { if (tab == k) return; tab = k; Draw(); Fill(content); Sfx.Play("ui_click", 0.6f, 0.03f, 1); });
                rt.gameObject.AddComponent<UiSound>();
                heads.Add((bg, lb, icon));
            }

            void Draw()
            {
                for (int i = 0; i < heads.Count; i++)
                {
                    bool on = i == tab;
                    var s = Art(on ? "tab_on" : "tab"); if (s) heads[i].bg.sprite = s;
                    heads[i].bg.color = on ? Color.white : new Color(0.78f, 0.74f, 0.76f);
                    heads[i].label.color = on ? UIKit.Gold : UIKit.Muted;
                    heads[i].icon.color = on ? Color.white : new Color(0.75f, 0.72f, 0.72f);
                    ((RectTransform)heads[i].bg.transform).localScale = on ? new Vector3(1.03f, 1.03f, 1) : Vector3.one;
                }
            }

            Draw();
            Fill(content);
        }

        static void Fill(RectTransform content)
        {
            for (int i = content.childCount - 1; i >= 0; i--) Object.Destroy(content.GetChild(i).gameObject);
            var kb = content.GetComponent<KeyBindsUI>(); if (kb) Object.Destroy(kb);

            float y = -RowH / 2 - 4;
            void Next() => y -= RowStep;
            switch (tab)
            {
                case 0:
                    Slider(content, "전체 음량", y, GameSettings.MasterVolume, v => GameSettings.MasterVolume = v); Next();
                    Slider(content, "음악 음량", y, GameSettings.MusicVolume, v => GameSettings.MusicVolume = v); Next();
                    Slider(content, "효과음 음량", y, GameSettings.SfxVolume, v => GameSettings.SfxVolume = v); Next();
                    Toggle(content, "창 밖에서 소리 끄기", y, GameSettings.MuteUnfocused, v => GameSettings.MuteUnfocused = v);
                    break;
                case 1:
                {
                    Choice(content, "창 모드", y, new[] { "전체 화면", "창", "테두리 없는 창" }, GameSettings.WindowMode, i => GameSettings.WindowMode = i); Next();
                    var res = Resolutions(out int cur);
                    var names = new string[res.Count];
                    for (int i = 0; i < res.Count; i++) names[i] = res[i].x <= 0 ? "모니터 그대로" : $"{res[i].x} × {res[i].y}";
                    Choice(content, "해상도", y, names, cur, i => GameSettings.Resolution = res[i]); Next();
                    Toggle(content, "수직 동기화", y, GameSettings.VSync, v => GameSettings.VSync = v); Next();
                    Slider(content, "화면 흔들림", y, GameSettings.ShakeScale, v => GameSettings.ShakeScale = v); Next();
                    Toggle(content, "번쩍임 · 화면 효과 줄이기", y, GameSettings.ReduceFlash, v => GameSettings.ReduceFlash = v); Next();
                    var scales = new[] { 0.8f, 0.9f, 1f, 1.1f, 1.2f, 1.3f };
                    int si = Nearest(scales, GameSettings.UiScale);
                    Choice(content, "UI 크기", y, new[] { "80%", "90%", "100%", "110%", "120%", "130%" }, si, i => GameSettings.UiScale = scales[i]);
                    break;
                }
                case 2:
                {
                    var langs = new string[Loc.Languages.Length]; int li = 0;
                    for (int i = 0; i < langs.Length; i++) { langs[i] = Loc.Languages[i].name; if (Loc.Languages[i].code == Loc.Lang) li = i; }
                    Choice(content, "언어", y, langs, li, i => Loc.Lang = Loc.Languages[i].code); Next();   // 언어 이름은 그 언어로 (번역 안 함)
                    Toggle(content, "대사 자동 넘김", y, GameSettings.DialogueAuto, v => GameSettings.DialogueAuto = v); Next();
                    Choice(content, "글자 속도", y, new[] { "느림", "보통", "빠름", "바로" }, GameSettings.TextSpeed, i => GameSettings.TextSpeed = i); Next();
                    Choice(content, "조준선 모양", y, new[] { "기본형", "십자", "점" }, GameSettings.CursorStyle, i => GameSettings.CursorStyle = i); Next();
                    Slider(content, "조준선 크기", y, Mathf.InverseLerp(0.6f, 1.6f, GameSettings.CursorSize), v => GameSettings.CursorSize = Mathf.Lerp(0.6f, 1.6f, v)); Next();
                    BossPractice(content, y);
                    break;
                }
                default:
                    KeyBindsUI.Build(content);
                    break;
            }
        }

        static int Nearest(float[] a, float v)
        {
            int best = 0;
            for (int i = 1; i < a.Length; i++) if (Mathf.Abs(a[i] - v) < Mathf.Abs(a[best] - v)) best = i;
            return best;
        }

        /// <summary>고를 수 있는 해상도 (맨 앞 = 모니터 그대로). cur = 지금 설정 위치</summary>
        static List<Vector2Int> Resolutions(out int cur)
        {
            var list = new List<Vector2Int> { Vector2Int.zero };
            foreach (var r in Screen.resolutions)
            {
                var v = new Vector2Int(r.width, r.height);
                if (v.x < 1024 || list.Contains(v)) continue;
                list.Add(v);
            }
            var set = GameSettings.Resolution;
            cur = Mathf.Max(0, list.IndexOf(set.x > 0 ? set : Vector2Int.zero));
            if (set.x > 0 && !list.Contains(set)) { list.Add(set); cur = list.Count - 1; }
            return list;
        }

        // ---------- 줄 ----------

        static RectTransform Row(RectTransform body, string label, float y)
        {
            var rt = UIKit.MakeRect("Row", body, new Vector2(0.5f, 1), new Vector2(0, y), new Vector2(0, RowH));
            rt.anchorMin = new Vector2(0, 1); rt.anchorMax = new Vector2(1, 1);
            rt.sizeDelta = new Vector2(0, RowH);
            var bg = Sliced(rt, "row", 2f);
            bg.raycastTarget = false;
            var lb = UIKit.Label("L", rt, new Vector2(0, 0.5f), new Vector2(LabelX, 0), new Vector2(420, RowH - 6), 28, TextAnchor.MiddleLeft, true, UIKit.Cream);
            lb.rectTransform.pivot = new Vector2(0, 0.5f);
            UIKit.NoFit(lb, 20);
            lb.text = label;
            return rt;
        }

        /// <summary>값 칸: 줄 가운데에서 오른쪽 (가운데 기준 x = CtrlX)</summary>
        static RectTransform Ctrl(RectTransform row, string name, float width, float height) =>
            UIKit.MakeRect(name, row, new Vector2(0.5f, 0.5f), new Vector2(CtrlX - (CtrlW - width) / 2, 0), new Vector2(width, height));

        static void Slider(RectTransform body, string label, float y, float value, System.Action<float> set)
        {
            var row = Row(body, label, y);
            var track = Ctrl(row, "Track", CtrlW, 34);
            var ti = track.gameObject.AddComponent<Image>();
            if (L("slider_track")) { ti.sprite = L("slider_track"); ti.material = Fx(); } else ti.color = new Color(0.2f, 0.12f, 0.1f);
            var knob = UIKit.MakeRect("Knob", track, new Vector2(value, 0.5f), Vector2.zero, new Vector2(52, 52));
            knob.pivot = new Vector2(0.5f, 0.5f);
            var ki = knob.gameObject.AddComponent<Image>();
            ki.raycastTarget = false;
            if (L("slider_knob")) { ki.sprite = L("slider_knob"); ki.preserveAspect = true; ki.material = Fx(0.3f); } else ki.color = UIKit.Gold;
            var drag = track.gameObject.AddComponent<SliderDrag>();
            drag.knob = knob; drag.onChange = set;
        }

        static Image Check(RectTransform parent, Vector2 pos, float size, bool value, System.Action<bool> set)
        {
            bool on = value;
            var rt = UIKit.MakeRect("Check", parent, new Vector2(0.5f, 0.5f), pos, new Vector2(size, size));
            rt.pivot = new Vector2(0.5f, 0.5f);
            var img = rt.gameObject.AddComponent<Image>();
            img.preserveAspect = true;
            img.material = Fx();
            void Draw() { var s = L(on ? "check_on" : "check_off"); if (s) img.sprite = s; else img.color = on ? UIKit.Gold : new Color(0.3f, 0.2f, 0.2f); }
            Draw();
            var b = rt.gameObject.AddComponent<Button>();
            b.targetGraphic = img;
            b.onClick.AddListener(() => { on = !on; set(on); Draw(); Sfx.Play("ui_click", 0.6f, 0.03f, 1); });
            rt.gameObject.AddComponent<HoverScale>().amount = 1.12f;
            return img;
        }

        static void Toggle(RectTransform body, string label, float y, bool value, System.Action<bool> set)
        {
            var row = Row(body, label, y);
            Check(row, new Vector2(CtrlX - CtrlW / 2 + 28, 0), 52, value, set);
        }

        /// <summary>◀ 값 ▶ 고르기 (끝에서 반대쪽으로 돌아감)</summary>
        static void Choice(RectTransform body, string label, float y, string[] options, int index, System.Action<int> set)
        {
            var row = Row(body, label, y);
            int cur = Mathf.Clamp(index, 0, options.Length - 1);
            var box = Ctrl(row, "Choice", CtrlW, RowH);
            var val = UIKit.Label("V", box, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(CtrlW - 130, RowH - 6), 27, TextAnchor.MiddleCenter, true, UIKit.Gold);
            UIKit.NoFit(val, 18);
            void Show() => val.text = options[cur];
            Show();
            void Arrow(string art, float x, int dir)
            {
                var rt = UIKit.MakeRect(art, box, new Vector2(0.5f, 0.5f), new Vector2(x, 0), new Vector2(46, 46));
                rt.pivot = new Vector2(0.5f, 0.5f);
                var img = rt.gameObject.AddComponent<Image>();
                img.sprite = Art(art); img.preserveAspect = true;
                if (!img.sprite) img.color = UIKit.Gold;
                var b = rt.gameObject.AddComponent<Button>();
                b.targetGraphic = img;
                b.onClick.AddListener(() =>
                {
                    if (options.Length < 2) return;
                    cur = (cur + dir + options.Length) % options.Length;
                    Show(); set(cur); Sfx.Play("ui_click", 0.6f, 0.03f, 1);
                });
                rt.gameObject.AddComponent<HoverScale>().amount = 1.15f;
            }
            Arrow("arrow_left", -CtrlW / 2 + 28, -1);
            Arrow("arrow_right", CtrlW / 2 - 28, 1);
        }

        /// <summary>보스 패턴 연습: 1~5장 체크 (켜면 그 장 스토리 맵을 이미 봤어도 다음 도전에서 대화 없이 패턴 튜토리얼만)</summary>
        static void BossPractice(RectTransform body, float y)
        {
            var row = Row(body, "보스 패턴 연습 (장별)", y);
            float x0 = CtrlX - CtrlW / 2 + 24, step = CtrlW / 5f;
            for (int c = 1; c <= 5; c++)
            {
                int ch = c;
                float x = x0 + step * (c - 1);
                Check(row, new Vector2(x, 0), 40, GameSettings.BossPracticeCh(ch), v => GameSettings.SetBossPracticeCh(ch, v));
                var lb = UIKit.Label("Ch" + c, row, new Vector2(0.5f, 0.5f), new Vector2(x + 48, 0), new Vector2(56, RowH - 8), 22, TextAnchor.MiddleLeft, false, UIKit.Cream);
                lb.text = c.ToString();
            }
        }
    }
}
