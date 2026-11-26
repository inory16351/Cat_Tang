// 환경설정 '조작' 탭 — 단축키 바꾸기 (2026-11-26 사용자)
// - KeyBindsUI.Build(body): body(가운데 기준 약 1060 × 420) 안에 동작마다 한 줄 (동작 이름 + 키 버튼), 두 열
// - 키 버튼 클릭 → "키를 누르세요" → 다음 키 · 마우스 버튼으로 지정 (Esc = 취소, 다른 곳 좌클릭 = 취소)
// - 같은 키를 고르면 두 동작의 키를 서로 맞바꿈 (KeyBinds.Set)
// - 키캡 그림: Resources/UI/Settings/keycap.png · keycap_listen.png (없으면 panel_plain)
using System;
using System.Collections.Generic;
using UnityEngine;

using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace CatTang
{
    public class KeyBindsUI : MonoBehaviour
    {
        const float RowH = 50, RowStep = 56, ColW = 470, CapW = 190, CapH = 46;

        class Row { public Act act; public Image cap; public Text key; public Text name; }
        readonly List<Row> rows = new();
        Row listening;
        int listenFrame;

        /// <summary>body 안에 단축키 줄들 + '기본값으로' 버튼을 만듦</summary>
        public static void Build(RectTransform body)
        {
            var ui = body.gameObject.GetComponent<KeyBindsUI>();
            if (ui) { ui.Refresh(); return; }
            ui = body.gameObject.AddComponent<KeyBindsUI>();
            ui.Make(body);
        }

        void Make(RectTransform body)
        {
            var acts = (Act[])Enum.GetValues(typeof(Act));
            int perCol = acts.Length > 6 ? Mathf.CeilToInt(acts.Length / 2f) : acts.Length;
            int cols = acts.Length > 6 ? 2 : 1;
            for (int i = 0; i < acts.Length; i++)
            {
                int c = i / perCol, r = i % perCol;
                float x = cols == 1 ? 0 : (c == 0 ? -ColW / 2 - 24 : ColW / 2 + 24);
                rows.Add(MakeRow(body, acts[i], new Vector2(x, -10 - RowH / 2 - r * RowStep)));
            }

            var hint = UIKit.Label("KeyHint", body, new Vector2(0, 0), new Vector2(20, 30), new Vector2(660, 44), 20, TextAnchor.MiddleLeft, false, UIKit.Muted);
            hint.rectTransform.pivot = new Vector2(0, 0.5f);
            hint.text = "키 칸을 누르고 새 키를 눌러요. 이미 쓰는 키면 서로 바뀌어요. (Esc 취소)";

            var reset = UIKit.MakeButton("ResetKeys", body, new Vector2(1, 0), new Vector2(-20, 30), new Vector2(240, 60), "기본값으로", () => { Cancel(); KeyBinds.ResetAll(); }, 24);
            ((RectTransform)reset.transform).pivot = new Vector2(1, 0.5f);

            Refresh();
        }

        Row MakeRow(RectTransform body, Act act, Vector2 pos)
        {
            var row = new Row { act = act };
            var rt = UIKit.MakeRect("Key_" + act, body, new Vector2(0.5f, 1), pos, new Vector2(ColW, RowH), new Vector2(0.5f, 0.5f));

            var line = rt.gameObject.AddComponent<Image>();                 // 줄 바탕 (환경설정 row 그림, 없으면 옅은 검정)
            var rowArt = SettingsUI.Art("row");
            if (rowArt) { line.sprite = rowArt; line.type = Image.Type.Sliced; line.pixelsPerUnitMultiplier = 2f; }
            else line.color = new Color(0, 0, 0, 0.18f);
            line.raycastTarget = false;

            row.name = UIKit.Label("Name", rt, new Vector2(0, 0.5f), new Vector2(22, 0), new Vector2(ColW - CapW - 50, RowH), 26, TextAnchor.MiddleLeft);
            row.name.rectTransform.pivot = new Vector2(0, 0.5f);
            row.name.text = KeyBinds.ActName(act);

            var capRt = UIKit.MakeRect("Cap", rt, new Vector2(1, 0.5f), new Vector2(-8, 0), new Vector2(CapW, CapH), new Vector2(1, 0.5f));
            row.cap = capRt.gameObject.AddComponent<Image>();
            row.cap.raycastTarget = true;
            var btn = capRt.gameObject.AddComponent<Button>();
            btn.targetGraphic = row.cap;
            var cb = btn.colors;
            cb.normalColor = Color.white; cb.highlightedColor = new Color(1f, 0.93f, 0.75f); cb.pressedColor = new Color(0.85f, 0.8f, 0.7f); cb.selectedColor = Color.white;
            btn.colors = cb;
            btn.onClick.AddListener(() => { Sfx.Play("ui_click", 0.6f, 0.03f, 1); StartListen(row); });
            capRt.gameObject.AddComponent<UiSound>();

            row.key = UIKit.Label("Key", capRt, new Vector2(0.5f, 0.5f), new Vector2(0, 1), new Vector2(CapW - 24, CapH - 8), 24, TextAnchor.MiddleCenter, true);
            return row;
        }

        static Sprite CapSprite(bool listen)
        {
            var s = SettingsUI.Art(listen ? "keycap_listen" : "keycap");      // 임포트 설정의 9-slice 테두리 그대로
            if (!s && listen) s = SettingsUI.Art("keycap");
            return s ? s : UIKit.Spr("panel_plain");
        }

        void Refresh()
        {
            foreach (var r in rows)
            {
                bool on = r == listening;
                var spr = CapSprite(on);
                r.cap.sprite = spr;
                bool custom = spr && spr.name.StartsWith("keycap");
                bool hasListenArt = on && SettingsUI.Art("keycap_listen");
                r.cap.type = Image.Type.Sliced; r.cap.pixelsPerUnitMultiplier = custom ? 2.4f : 2.2f;
                r.cap.color = on && !hasListenArt ? new Color(1f, 0.85f, 0.45f) : Color.white;       // 듣는 중 그림이 없으면 금빛으로 물들임
                r.key.text = on ? "키를 누르세요" : KeyBinds.Name(r.act);
                r.key.color = on ? UIKit.Gold : (KeyBinds.IsDefault(r.act) ? UIKit.Cream : new Color(1f, 0.9f, 0.6f));
            }
        }

        void StartListen(Row r)
        {
            listening = r;
            listenFrame = Time.frameCount;
            KeyBinds.Listening = true;
            Refresh();
        }

        void Cancel()
        {
            if (listening == null) return;
            listening = null;
            KeyBinds.EndListen();
            Refresh();
        }

        void OnEnable() => KeyBinds.Changed += Refresh;

        void OnDisable()
        {
            KeyBinds.Changed -= Refresh;
            Cancel();
        }

        void Update()
        {
            if (listening == null) return;
            if (Time.frameCount == listenFrame) return;                     // 누른 그 클릭은 건너뜀
            var kb = Keyboard.current; var m = Mouse.current;
            if (kb != null && kb.escapeKey.wasPressedThisFrame) { Cancel(); return; }
            if (KeyBinds.CapturePressed(out var b))
            {
                var act = listening.act;
                listening = null;
                KeyBinds.EndListen();
                KeyBinds.Set(act, b);                                       // Changed → Refresh
                Sfx.Play("ui_click", 0.6f, 0.03f, 1);
                Refresh();
                return;
            }
            if (m != null && m.leftButton.wasPressedThisFrame) Cancel();   // 다른 곳을 클릭 (다른 키 칸이면 그 칸이 다시 듣기 시작)
        }
    }
}
