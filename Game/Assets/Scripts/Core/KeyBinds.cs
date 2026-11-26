// 단축키 (2026-11-26 사용자: 환경설정에서 키를 바꿀 수 있게)
// - 바꿀 수 있는 동작: 이동 4 · 대시 · 상호작용 · 인벤토리 · 지도 · 불릿 파티 · 총 스킬. 마우스 버튼(오른쪽 · 휠 · 옆 버튼)도 지정 가능
// - 고정: Esc(일시정지 · 닫기 · 지정 취소) · 좌클릭(사격 · UI 클릭) · 대화 넘기기(아무 키)
// - 저장: PlayerPrefs "cattang.key.<동작>" = "k:<Key 이름>" 또는 "m:<버튼>"
// - 같은 키를 다른 동작에 지정하면 두 동작의 키를 서로 맞바꿈
// - 화면 글은 KeyBinds.Name(Act) ("E", "Space", "우클릭"), 안내 문구 색칠은 KeyBinds.Gold(Act)
using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

namespace CatTang
{
    /// <summary>바꿀 수 있는 동작 (순서 = 설정 창 줄 순서)</summary>
    public enum Act { Up, Down, Left, Right, Dash, Interact, Inventory, Map, BulletParty, Skill }

    /// <summary>지정할 수 있는 마우스 버튼 (좌클릭은 사격 · UI 클릭이라 고정)</summary>
    public enum MouseBtn { Right, Middle, Back, Forward }

    /// <summary>키 하나: 키보드 키 또는 마우스 버튼</summary>
    public readonly struct Bind : IEquatable<Bind>
    {
        public readonly bool mouse;
        public readonly Key key;
        public readonly MouseBtn button;

        Bind(bool mouse, Key key, MouseBtn button) { this.mouse = mouse; this.key = key; this.button = button; }
        public static Bind Of(Key k) => new(false, k, default);
        public static Bind Of(MouseBtn b) => new(true, Key.None, b);

        public bool Equals(Bind o) => mouse == o.mouse && (mouse ? button == o.button : key == o.key);
        public override bool Equals(object o) => o is Bind b && Equals(b);
        public override int GetHashCode() => mouse ? 1000 + (int)button : (int)key;

        public string Save() => mouse ? "m:" + button : "k:" + key;
        public static bool TryLoad(string s, out Bind b)
        {
            b = default;
            if (string.IsNullOrEmpty(s) || s.Length < 3 || s[1] != ':') return false;
            var v = s.Substring(2);
            if (s[0] == 'k' && Enum.TryParse(v, out Key k) && KeyBinds.Allowed(k)) { b = Of(k); return true; }
            if (s[0] == 'm' && Enum.TryParse(v, out MouseBtn m)) { b = Of(m); return true; }
            return false;
        }
    }

    public static class KeyBinds
    {
        const string Prefix = "cattang.key.";
        static readonly int Count = Enum.GetValues(typeof(Act)).Length;
        static Bind[] binds;

        /// <summary>키가 바뀌면 (안내 문구 · 설정 창 다시 그리기용)</summary>
        public static event Action Changed;

        /// <summary>설정 창이 "키를 누르세요" 상태 — 그동안 게임 입력(Down/Held)은 모두 false</summary>
        public static bool Listening { get; internal set; }
        static int listenEndFrame = -10;

        /// <summary>이번 프레임의 Esc를 키 지정 취소가 썼는지 — Esc로 창을 닫는 곳은 이것이 true면 건너뛰기</summary>
        public static bool EscConsumed => Listening || Time.frameCount - listenEndFrame <= 1;
        internal static void EndListen() { Listening = false; listenEndFrame = Time.frameCount; }

        // ---------- 기본값 · 이름 ----------
        public static Bind Default(Act a) => a switch
        {
            Act.Up => Bind.Of(Key.W), Act.Down => Bind.Of(Key.S), Act.Left => Bind.Of(Key.A), Act.Right => Bind.Of(Key.D),
            Act.Dash => Bind.Of(Key.Space), Act.Interact => Bind.Of(Key.E), Act.Inventory => Bind.Of(Key.Tab),
            Act.Map => Bind.Of(Key.M), Act.BulletParty => Bind.Of(Key.Q), Act.Skill => Bind.Of(MouseBtn.Right),
            _ => Bind.Of(Key.None)
        };

        /// <summary>동작 이름 (한국어 원문 — LText가 번역)</summary>
        public static string ActName(Act a) => a switch
        {
            Act.Up => "위로 이동", Act.Down => "아래로 이동", Act.Left => "왼쪽으로 이동", Act.Right => "오른쪽으로 이동",
            Act.Dash => "대시", Act.Interact => "상호작용", Act.Inventory => "인벤토리", Act.Map => "지도",
            Act.BulletParty => "불릿 파티", Act.Skill => "총 스킬", _ => a.ToString()
        };

        /// <summary>지정 불가 키: Esc(고정) · 없음</summary>
        public static bool Allowed(Key k) => k != Key.None && k != Key.Escape;

        static void Ensure()
        {
            if (binds != null) return;
            binds = new Bind[Count];
            for (int i = 0; i < Count; i++)
            {
                var a = (Act)i;
                binds[i] = Bind.TryLoad(PlayerPrefs.GetString(Prefix + a, ""), out var b) ? b : Default(a);
            }
            // 저장이 꼬여 같은 키가 둘이면 뒤쪽을 기본값으로 (기본값도 겹치면 그대로 둠)
            for (int i = 0; i < Count; i++)
                for (int j = 0; j < i; j++)
                    if (binds[i].Equals(binds[j])) binds[i] = Default((Act)i);
        }

        public static Bind Get(Act a) { Ensure(); return binds[(int)a]; }

        // ---------- 읽기 ----------
        static ButtonControl Control(Bind b)
        {
            if (b.mouse)
            {
                var m = Mouse.current;
                if (m == null) return null;
                return b.button switch
                {
                    MouseBtn.Right => m.rightButton, MouseBtn.Middle => m.middleButton,
                    MouseBtn.Back => m.backButton, MouseBtn.Forward => m.forwardButton, _ => null
                };
            }
            var kb = Keyboard.current;
            if (kb == null || b.key == Key.None) return null;
            try { return kb[b.key]; } catch { return null; }
        }

        /// <summary>이번 프레임에 눌렀는지</summary>
        public static bool Down(Act a)
        {
            if (Listening) return false;
            var c = Control(Get(a));
            return c != null && c.wasPressedThisFrame;
        }

        /// <summary>누르고 있는지</summary>
        public static bool Held(Act a)
        {
            if (Listening) return false;
            var c = Control(Get(a));
            return c != null && c.isPressed;
        }

        /// <summary>이번 프레임에 뗐는지</summary>
        public static bool Released(Act a)
        {
            if (Listening) return false;
            var c = Control(Get(a));
            return c != null && c.wasReleasedThisFrame;
        }

        /// <summary>이동 입력 (x = 오른쪽 − 왼쪽, y = 위 − 아래). 정규화 안 함</summary>
        public static Vector2 Move() => new((Held(Act.Right) ? 1 : 0) - (Held(Act.Left) ? 1 : 0), (Held(Act.Up) ? 1 : 0) - (Held(Act.Down) ? 1 : 0));

        /// <summary>이동 키 중 하나라도 이번 프레임에 눌렀는지</summary>
        public static bool AnyMoveDown() => Down(Act.Up) || Down(Act.Down) || Down(Act.Left) || Down(Act.Right);

        // ---------- 화면 글 ----------
        /// <summary>화면에 쓸 키 이름: "E", "Space", "우클릭"</summary>
        public static string Name(Act a) => Name(Get(a));

        /// <summary>안내 문구용 금색 키 이름: &lt;color=#FFD54A&gt;E&lt;/color&gt;</summary>
        public static string Gold(Act a) => $"<color=#FFD54A>{Name(a)}</color>";

        /// <summary>이동 4키를 한 덩어리로: 기본이면 "WASD", 아니면 "↑←↓→"처럼 이어 붙임</summary>
        public static string MoveName()
        {
            string u = Name(Act.Up), l = Name(Act.Left), d = Name(Act.Down), r = Name(Act.Right);
            return u.Length == 1 && l.Length == 1 && d.Length == 1 && r.Length == 1 ? u + l + d + r : $"{u} {l} {d} {r}";
        }

        public static string Name(Bind b)
        {
            if (b.mouse) return b.button switch
            {
                MouseBtn.Right => "우클릭", MouseBtn.Middle => "휠 클릭", MouseBtn.Back => "마우스 4", MouseBtn.Forward => "마우스 5", _ => "?"
            };
            var k = b.key;
            if (k >= Key.A && k <= Key.Z) return k.ToString();
            if (k >= Key.Digit1 && k <= Key.Digit0) return k == Key.Digit0 ? "0" : ((int)(k - Key.Digit1) + 1).ToString();
            if (k >= Key.F1 && k <= Key.F12) return k.ToString();
            if (k >= Key.Numpad0 && k <= Key.Numpad9) return "숫자패드 " + (int)(k - Key.Numpad0);
            switch (k)
            {
                case Key.None: return "없음";
                case Key.Space: return "Space";
                case Key.Tab: return "Tab";
                case Key.Enter: return "Enter";
                case Key.NumpadEnter: return "숫자패드 Enter";
                case Key.Backspace: return "Backspace";
                case Key.LeftShift: return "왼쪽 Shift";
                case Key.RightShift: return "오른쪽 Shift";
                case Key.LeftCtrl: return "왼쪽 Ctrl";
                case Key.RightCtrl: return "오른쪽 Ctrl";
                case Key.LeftAlt: return "왼쪽 Alt";
                case Key.RightAlt: return "오른쪽 Alt";
                case Key.CapsLock: return "Caps Lock";
                case Key.UpArrow: return "↑";
                case Key.DownArrow: return "↓";
                case Key.LeftArrow: return "←";
                case Key.RightArrow: return "→";
                case Key.Backquote: return "`";
                case Key.Minus: return "-";
                case Key.Equals: return "=";
                case Key.LeftBracket: return "[";
                case Key.RightBracket: return "]";
                case Key.Backslash: return "\\";
                case Key.Semicolon: return ";";
                case Key.Quote: return "'";
                case Key.Comma: return ",";
                case Key.Period: return ".";
                case Key.Slash: return "/";
                case Key.Insert: return "Insert";
                case Key.Delete: return "Delete";
                case Key.Home: return "Home";
                case Key.End: return "End";
                case Key.PageUp: return "Page Up";
                case Key.PageDown: return "Page Down";
            }
            var kb = Keyboard.current;
            if (kb != null)
            {
                try { var dn = kb[k].displayName; if (!string.IsNullOrEmpty(dn)) return dn; } catch { }
            }
            return k.ToString();
        }

        // ---------- 바꾸기 ----------
        /// <summary>지정. 다른 동작이 같은 키를 쓰고 있으면 그 동작은 이 동작의 원래 키를 받음 (맞바꿈)</summary>
        public static void Set(Act a, Bind b)
        {
            Ensure();
            if (!b.mouse && !Allowed(b.key)) return;
            var old = binds[(int)a];
            if (old.Equals(b)) return;
            for (int i = 0; i < Count; i++)
                if (i != (int)a && binds[i].Equals(b)) { binds[i] = old; Save((Act)i); }
            binds[(int)a] = b;
            Save(a);
            PlayerPrefs.Save();
            Changed?.Invoke();
        }

        public static void Set(Act a, Key k) => Set(a, Bind.Of(k));
        public static void Set(Act a, MouseBtn m) => Set(a, Bind.Of(m));

        /// <summary>한 동작만 기본값으로 (겹치면 맞바꿈)</summary>
        public static void Reset(Act a) => Set(a, Default(a));

        /// <summary>전부 기본값으로</summary>
        public static void ResetAll()
        {
            Ensure();
            for (int i = 0; i < Count; i++) { binds[i] = Default((Act)i); PlayerPrefs.DeleteKey(Prefix + (Act)i); }
            PlayerPrefs.Save();
            Changed?.Invoke();
        }

        public static bool IsDefault(Act a) => Get(a).Equals(Default(a));

        static void Save(Act a) => PlayerPrefs.SetString(Prefix + a, binds[(int)a].Save());

        /// <summary>이번 프레임에 새로 누른 키 · 마우스 버튼 (설정 창 지정용). Esc · 좌클릭은 안 잡음</summary>
        public static bool CapturePressed(out Bind b)
        {
            b = default;
            var kb = Keyboard.current;
            if (kb != null && kb.anyKey.wasPressedThisFrame)
            {
                foreach (var kc in kb.allKeys)
                {
                    if (kc == null || !kc.wasPressedThisFrame || !Allowed(kc.keyCode)) continue;
                    b = Bind.Of(kc.keyCode);
                    return true;
                }
            }
            var m = Mouse.current;
            if (m != null)
            {
                if (m.rightButton.wasPressedThisFrame) { b = Bind.Of(MouseBtn.Right); return true; }
                if (m.middleButton.wasPressedThisFrame) { b = Bind.Of(MouseBtn.Middle); return true; }
                if (m.backButton.wasPressedThisFrame) { b = Bind.Of(MouseBtn.Back); return true; }
                if (m.forwardButton.wasPressedThisFrame) { b = Bind.Of(MouseBtn.Forward); return true; }
            }
            return false;
        }
    }
}
