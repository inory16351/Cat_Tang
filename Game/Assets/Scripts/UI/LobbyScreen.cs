// 메인 로비 (2026-11-18 사용자: 게임 이름 11:59 · 하데스풍 카툰 키 일러스트 + 주인공 · 그림에 맞춘 타이포 · 버튼 · 패널 ·
//  메뉴 = 새로 시작 / 이어하기 / 설정 / 게임 종료, 이어하기를 눌러야 슬롯이 나옴 · 그림이 그려지는 듯한 연출)
// 그림: Codex CLI (docs/images/ui_src/prompts/lobby_key.txt · lobby_ui2.txt) → docs/tools/ui_lobby_cut.py · ui_lobby_ink.py → Resources/UI/Lobby
//
// 등장 연출 (클릭 · 키로 건너뜀):
//  고양이 얼굴을 크게 잡은 채 양피지가 밝아짐 → 잉크 선(key_ink)이 얼굴에서부터 한 줄씩 그어지며 바깥으로 뻗고(reveal_ink),
//  동시에 카메라가 천천히 뒤로 빠져 전체 그림으로 펼쳐짐 (옛 갱플랭크 로그인 화면 느낌) → 색(key)이 주인공 가슴 · 하늘 시계에서
//  번져 나가며 채워짐(reveal_color, 가장자리 녹은 금빛) → 제목이 '쾅' 찍힘 + 흔들림 → 메뉴 버튼이 오른쪽에서 차례로 미끄러져 들어옴
// 평소: 그림 숨쉬듯 확대 · 마우스 시차 · 붉은 먼지 · 불티 · 제목 1초마다 똑딱
// 메뉴: 새로 시작(슬롯 고르기 — 쓴 슬롯은 두 번 눌러 덮어쓰기) / 이어하기(쓴 슬롯만, 없으면 꺼짐) / 설정(전체 · 음악 음량, 전체 화면) / 게임 종료(확인 창)
// 슬롯을 고르면 잿빛 라벤더 안개로 덮이며 마을로 → 마을에서 같은 안개가 걷히며 고양이가 깨어남 (VillageWake)
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace CatTang
{
    public class LobbyScreen : MonoBehaviour
    {
        public const string GameTitle = "11:59";
        /// <summary>개발용: 0 이상이면 등장 연출 시각을 이 값에 고정 (화면 확인)</summary>
        public static float DebugAge = -1f;
        static readonly Color Haze = new(0.78f, 0.74f, 0.8f);
        static readonly Color Parchment = new(0.87f, 0.79f, 0.65f);
        static readonly Color Ember = new(1f, 0.62f, 0.22f);
        const float TitleX = 470f, TitleY = -135f;   // 2026-11-26 새 로고(세로로 김): 위 끝을 올려 메뉴 '새로 시작' 위에서 끝나게
        const float IntroLen = 8.2f;
        // 얼굴 클로즈업 → 줌 아웃 (2026-11-18 사용자: 옛 갱플랭크 로그인 화면처럼 고양이 얼굴부터 선이 그려지며 카메라가 뒤로 빠져 전체로)
        static readonly Vector2 Face = new((0.255f - 0.5f) * 1920f, (0.5f - 0.33f) * 1080f);   // key.png 속 고양이 얼굴 (ui_lobby_ink.py FACE와 같게)
        const float ZoomFrom = 1.9f, ZoomTo = 1.06f, ZoomStart = 0.5f, ZoomLen = 6.8f;

        enum Mode { None, New, Continue, Settings }

        RectTransform root, art, title, menuBox;
        Image hazeImg, parchment;
        LobbyVfx vfx;
        bool vfxOn;
        CanvasGroup titleGroup;
        GameObject panel;
        RectTransform panelBody;
        Mode mode;
        readonly List<RectTransform> fogs = new();
        readonly List<(RectTransform rt, Image img, float speed, float phase)> motes = new();
        readonly List<RectTransform> menuButtons = new();
        int confirmSlot = -1;
        bool loading, introDone, titleStamped;
        float born, stampAt;
        Vector2 parallax;

        static Sprite L(string n) => UIKit.Icon("UI/Lobby/" + n);

        /// <summary>플랫 그림에 입히는 효과 재질 (CatTang/UIFlatFx: 녹은 금 · 해칭 · 명암 · 윗변 하이라이트 · 빛 훑기 · _Hover). 칸마다 따로 (마우스 올림 값이 따로라서)</summary>
        static Material FxMat(float hatch = 0.55f, float seed = 0f, float shineEvery = 3.5f)
        {
            var sh = Shader.Find("CatTang/UIFlatFx");
            if (!sh) return null;
            var m = new Material(sh);
            m.SetFloat("_Hatch", hatch); m.SetFloat("_Seed", seed); m.SetFloat("_ShineEvery", shineEvery);
            return m;
        }

        static bool AnySlot()
        {
            for (int s = 1; s <= Meta.SlotCount; s++) if (Meta.SlotUsed(s)) return true;
            return false;
        }

        void Start()
        {
            Time.timeScale = 1f;
            Tables.Load();
            UIKit.Init();
            born = Time.unscaledTime;
            var cgo = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            cgo.transform.SetParent(transform, false);
            cgo.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = cgo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            UiScaleFollow.Add(scaler);   // 환경설정 UI 크기 (2026-11-26)
            root = (RectTransform)cgo.transform;
            if (FindFirstObjectByType<EventSystem>() == null)
                new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));

            BuildArt();
            BuildAtmosphere();
            BuildVignette();

            // 제목 타이포
            title = UIKit.MakeRect("Title", root, new Vector2(0.5f, 1), new Vector2(TitleX, TitleY), new Vector2(578, 475));   // 폭 = 로고 비율(1124:925)에 맞춤 — 칸이 더 넓으면 preserveAspect가 그림을 왼쪽에 붙여 메뉴와 가운데가 어긋남 (2026-11-26 사용자)   // 2026-11-26 새 로고(시계 성문 + 고양이 실루엣)가 세로로 길어 높이 475 (위 끝 135 → 아래 끝 610, 메뉴 625)
            titleGroup = title.gameObject.AddComponent<CanvasGroup>();
            titleGroup.alpha = 0;
            if (L("title"))
            {
                DropShadow(title, L("title"), new Vector2(22, -8), 0.75f, true);   // 해(왼쪽 위 시계 뒤) 반대쪽으로 (2026-11-26 사용자: 로고 그림자를 화면에 맞춰)
                var ti = UIKit.Stretch("Face", title).gameObject.AddComponent<Image>();
                ti.sprite = L("title"); ti.preserveAspect = true; ti.raycastTarget = false;
                ti.material = FxMat(0.45f, 0f, 8f);
                if (ti.material) ti.material.SetFloat("_ShineDur", 4.5f);   // 광택은 4.5초에 걸쳐 천천히 훑고 3.5초 쉼 (2026-11-26 사용자: 너무 빠름)
            }
            else
            {
                var tt = UIKit.Label("T", title, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(680, 300), 190, TextAnchor.MiddleCenter, true, UIKit.Gold);
                tt.text = GameTitle;
            }

            vfx = LobbyVfx.Create(art, title);                 // 햇살 · 녹은 금 방울 · 총구 반짝임 · 불똥 (그림과 같이 움직임)

            // 메뉴: 새로 시작 · 이어하기 · 설정 · 게임 종료 (2026-11-19 사용자: 메인에도 게임 종료)
            menuBox = UIKit.MakeRect("Menu", root, new Vector2(0.5f, 0.5f), new Vector2(TitleX, -255), new Vector2(520, 460));   // 위쪽 끝은 그대로, 아래로 한 칸 늘림
            AddMenu("새로 시작", 0, () => OpenPanel(Mode.New));
            AddMenu("이어하기", 1, () => OpenPanel(Mode.Continue));
            AddMenu("설정", 2, () => OpenPanel(Mode.Settings));
            AddMenu("게임 종료", 3, OpenQuitConfirm);
            RefreshContinue();
            foreach (var m in menuButtons) m.gameObject.SetActive(false);

            var hz = UIKit.Stretch("Haze", root).gameObject.AddComponent<Image>();
            hz.raycastTarget = false; hz.color = new Color(Haze.r, Haze.g, Haze.b, 0);
            hazeImg = hz;
            Music.Play("bgm_village");
        }

        // ---------- 그림을 그리는 과정 (2026-11-18 사용자: 선이 균일하게 그어지면 그림 같지 않음 → 과정 그림 몇 장을 겹침) ----------
        // 단계 그림 (docs/tools/ui_lobby_stages.py, 모두 key와 같은 자리):
        //  stage1 파란 연필 밑그림(Codex) → stage2 연필 스케치(Codex) → stage3 잉크 외곽선 → stage4 잉크 + 해칭 → stage5 평면 색칠 → key 완성(그늘 · 빛)
        // (예전: 마스크로 드러냄 — 지금은 LobbyStrokes가 획을 실제 선으로 긋고, 나머지는 알파로 겹침)
        // 앞 단계는 다음 단계가 덮일 때 옅어졌다 사라짐 (밑그림 위에 스케치를 얹고 지우개로 지우는 느낌)
        class Stage
        {
            public Image img; public Material mat; public LobbyStrokes strokes; public float baseA = 1f;
            public float start, dur;                      // 드러나는 시각 · 길이 (mat이 없으면 알파로)
            public Vector2[] fade;                        // (시각, 남는 알파) 꺾은선. 비면 계속 1
        }
        readonly List<Stage> stages = new();

        void BuildArt()
        {
            art = UIKit.Stretch("Art", root);
            art.pivot = new Vector2(0.5f, 0.5f);
            art.localScale = Vector3.one * 1.06f;
            var dark = UIKit.Stretch("Dark", art).gameObject.AddComponent<Image>();
            dark.color = new Color(0.06f, 0.03f, 0.06f); dark.raycastTarget = false;
            parchment = UIKit.Stretch("Parchment", art).gameObject.AddComponent<Image>();
            parchment.color = new Color(Parchment.r, Parchment.g, Parchment.b, 0); parchment.raycastTarget = false;

            var key = L("key") ? L("key") : L("bg");
            bool staged = L("stage3") && L("stage5") && Shader.Find("CatTang/UIReveal");
            if (staged)
            {
                // 2026-11-18 사용자: 마스크로 드러내지 말고 선 하나하나가 그어지게, 원으로 퍼지지 말고 여기저기서 그려지며 합쳐지게
                //  연필 획(strokes_pencil) → 잉크 획(strokes_ink)을 LobbyStrokes가 실제 선으로 그음 (펜 여러 자루가 그림 여기저기서, ui_lobby_vector.py)
                //  → 해칭(stage4) · 평면 색(stage5) · 완성은 마스크 없이 서서히 겹쳐 나타남
                var pencil = LobbyStrokes.Create(art, "strokes_pencil", new Color(0.33f, 0.28f, 0.27f, 0.85f), 0.45f);
                if (pencil) stages.Add(new Stage { strokes = pencil, baseA = pencil.color.a, start = 0.5f, dur = 3.8f, fade = new[] { new Vector2(6.0f, 1f), new Vector2(6.6f, 0f) } });
                var inkS = LobbyStrokes.Create(art, "strokes_ink", new Color(0.09f, 0.05f, 0.07f, 1f), 0.7f);
                if (inkS) stages.Add(new Stage { strokes = inkS, baseA = inkS.color.a, start = 3.0f, dur = 2.8f, fade = new[] { new Vector2(7.0f, 1f), new Vector2(7.5f, 0f) } });
                // 흑백 해칭 · 명암 디테일은 아까처럼 퍼지며 더해지고(사용자), 색 전에 흑백 상태를 잠깐 보여 줌
                AddStage("stage4", "reveal_radial", 5.2f, 0.9f, 0.16f, new Color(0.1f, 0.05f, 0.08f, 0f), new[] { new Vector2(7.0f, 1f), new Vector2(7.5f, 0f) });
                AddStage("stage5", "reveal_radial", 6.3f, 0.9f, 0.16f, new Color(1f, 0.55f, 0.15f, 0.9f), null);   // 색도 흑백 디테일처럼 퍼지며 입혀짐 (사용자)
                var fin = UIKit.Stretch("Final", art).gameObject.AddComponent<Image>();
                fin.sprite = key; fin.raycastTarget = false; fin.color = new Color(1, 1, 1, 0);
                // 완성 그림도 아주 살짝 계속 일렁임 (로토스코핑 느낌 유지)
                var fm = new Material(Shader.Find("CatTang/UIReveal"));
                fm.SetFloat("_Progress", 2f); fm.SetFloat("_Soft", 0.01f); fm.SetColor("_EdgeColor", new Color(0, 0, 0, 0));
                fin.material = fm;
                stages.Add(new Stage { img = fin, start = 7.1f, dur = 0.6f });
                return;
            }
            // 단계 그림이 없으면 예전처럼 잉크 → 색
            var col = UIKit.Stretch("Color", art).gameObject.AddComponent<Image>();
            col.raycastTarget = false;
            if (key) col.sprite = key; else col.color = new Color(0.24f, 0.12f, 0.14f);
        }

        /// <summary>마스크 없이 알파로 서서히 나타나는 단계</summary>
        void AddFade(string sprite, float start, float dur, Vector2[] fade)
        {
            if (!L(sprite)) return;
            var im = UIKit.Stretch(sprite, art).gameObject.AddComponent<Image>();
            im.sprite = L(sprite); im.raycastTarget = false; im.color = new Color(1, 1, 1, 0);
            stages.Add(new Stage { img = im, start = start, dur = dur, fade = fade });
        }

        void AddStage(string sprite, string mask, float start, float dur, float soft, Color edge, Vector2[] fade)
        {
            if (!L(sprite)) return;
            var m = new Material(Shader.Find("CatTang/UIReveal"));
            m.SetTexture("_Mask", Resources.Load<Texture2D>("UI/Lobby/" + mask));
            m.SetFloat("_Soft", soft);
            m.SetColor("_EdgeColor", edge);
            m.SetFloat("_Progress", 0);
            // (라인 보일 · 금빛 가장자리는 2026-11-18 사용자: 떨리는 작열 효과 빼기, 과함)
            var im = UIKit.Stretch(sprite, art).gameObject.AddComponent<Image>();
            im.sprite = L(sprite); im.material = m; im.raycastTarget = false;
            stages.Add(new Stage { img = im, mat = m, start = start, dur = dur, fade = fade });
        }

        void StepStages(float age)
        {
            foreach (var st in stages)
            {
                float p = Mathf.Clamp01((age - st.start) / st.dur);
                float a = 1f;
                if (st.fade != null)
                {
                    float prevT = 0, prevA = 1;
                    foreach (var k in st.fade)
                    {
                        if (age <= k.x) { a = Mathf.Lerp(prevA, k.y, Mathf.InverseLerp(prevT, k.x, age)); break; }
                        prevT = k.x; prevA = k.y; a = k.y;
                    }
                }
                if (st.strokes)
                {
                    st.strokes.SetProgress(p);
                    var c = st.strokes.color; c.a = (st.strokes == null ? 1 : st.baseA) * a; st.strokes.color = c;
                    continue;
                }
                if (st.mat) { st.mat.SetFloat("_Progress", p * (1f + st.mat.GetFloat("_Soft")) * 1.02f); st.img.color = new Color(1, 1, 1, a); }
                else st.img.color = new Color(1, 1, 1, Ease(p) * a);
                st.img.enabled = a > 0.001f && (st.mat ? p > 0 : true);
            }
        }

        void BuildAtmosphere()
        {
            var fogTex = new[] { Resources.Load<Texture2D>("FX/smoke_0"), Resources.Load<Texture2D>("FX/smoke_1") };
            for (int i = 0; i < 6; i++)
            {
                var t = fogTex[i % 2];
                if (!t) break;
                var ri = UIKit.MakeRect("Dust" + i, root, new Vector2(0.5f, 0.5f), new Vector2(Random.Range(-1100f, 1100f), Random.Range(-500f, -150f)), new Vector2(900, 380)).gameObject.AddComponent<RawImage>();
                ri.texture = t; ri.raycastTarget = false;
                ri.color = new Color(0.55f, 0.3f, 0.25f, 0);
                fogs.Add(ri.rectTransform);
            }
            for (int i = 0; i < 40; i++)
            {
                var m = UIKit.MakeRect("Ember" + i, root, new Vector2(0.5f, 0.5f), new Vector2(Random.Range(-960f, 960f), Random.Range(-560f, 560f)), Vector2.one * Random.Range(3f, 8f)).gameObject.AddComponent<Image>();
                m.color = new Color(Ember.r, Random.Range(0.4f, 0.8f), Ember.b, 0); m.raycastTarget = false;
                m.rectTransform.localRotation = Quaternion.Euler(0, 0, 45);
                motes.Add((m.rectTransform, m, Random.Range(20f, 60f), Random.Range(0f, 6.28f)));
            }
        }

        /// <summary>그림자 (2026-11-18 사용자: 그림자 넣기): 같은 그림을 검게 · 아래오른쪽으로 · 세 겹(멀수록 옅게) = 부드러운 그림자. parent의 맨 앞 자식들이라 그림 뒤에 그려짐</summary>
        static void DropShadow(RectTransform parent, Sprite s, Vector2 off, float alpha, bool preserve)
        {
            if (!s) return;
            for (int k = 2; k >= 0; k--)
            {
                var sh = UIKit.Stretch("Shadow" + k, parent);
                sh.SetAsFirstSibling();
                var o = off * (1f + k * 0.35f);
                sh.offsetMin = o; sh.offsetMax = o;
                var im = sh.gameObject.AddComponent<Image>();
                im.sprite = s; im.preserveAspect = preserve; im.raycastTarget = false;
                im.color = new Color(0.03f, 0.0f, 0.02f, alpha * (k == 0 ? 0.6f : k == 1 ? 0.3f : 0.15f));
            }
        }

        /// <summary>비네트 (2026-11-18 사용자: 비네트 맞추기): 그림 가장자리 · 모서리를 그림의 어두운 보랏빛 검정으로 눌러 주고,
        /// 오른쪽(제목 · 메뉴 자리)은 조금 더 어둡게 해 글자가 읽히게. 그림 위 · 제목 아래</summary>
        void BuildVignette()
        {
            const int W = 192, H = 108;
            var tex = new Texture2D(W, H, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            var px = new Color32[W * H];
            for (int y = 0; y < H; y++)
                for (int x = 0; x < W; x++)
                {
                    float u = x / (W - 1f) * 2 - 1, v = y / (H - 1f) * 2 - 1;
                    float d = Mathf.Sqrt(u * u * 0.75f + v * v);
                    float a = Edge(0.62f, 1.45f, d) * 0.78f;                            // 가장자리 · 모서리
                    a = Mathf.Max(a, Edge(0.2f, 1f, u) * 0.32f);                         // 오른쪽 메뉴 자리 (글자가 읽히게)
                    a = Mathf.Max(a, Edge(-0.6f, -1f, v) * 0.4f);                        // 아래 땅
                    px[y * W + x] = new Color32(10, 3, 9, (byte)(Mathf.Clamp01(a) * 255));
                }
            tex.SetPixels32(px); tex.Apply();
            var ri = UIKit.Stretch("Vignette", root).gameObject.AddComponent<RawImage>();
            ri.texture = tex; ri.raycastTarget = false;
        }

        // ---------- 메뉴 ----------
        void AddMenu(string label, int i, UnityEngine.Events.UnityAction onClick)
        {
            var b = PlateButton("Menu" + i, menuBox, new Vector2(0.5f, 1), new Vector2(0, -60 - i * 100), new Vector2(520, 84), label, onClick, "menu_button", 38);
            b.gameObject.AddComponent<CanvasGroup>();
            menuButtons.Add((RectTransform)b.transform);
        }

        /// <summary>이어하기: 쓴 슬롯이 없으면 꺼짐</summary>
        void RefreshContinue()
        {
            if (menuButtons.Count < 2) return;
            bool any = AnySlot();
            menuButtons[1].GetComponent<Button>().interactable = any;
            var cg = menuButtons[1].GetComponent<CanvasGroup>();
            if (cg && introDone) cg.alpha = any ? 1 : 0.4f;
        }

        /// <summary>그림 판 버튼 (normal / hover 그림 바꿈 + 마우스 올리면 커짐). 그림이 없으면 기본 스킨</summary>
        Button PlateButton(string name, Transform parent, Vector2 anchor, Vector2 pos, Vector2 size, string label, UnityEngine.Events.UnityAction onClick, string plate, int fontSize)
        {
            var normal = L(plate) ? L(plate) : L("button");
            var hover = L(plate) ? L(plate + "_hover") : L("button_hover");
            if (!normal) return UIKit.MakeButton(name, parent, anchor, pos, size, label, onClick, fontSize);
            var rt = UIKit.MakeRect(name, parent, anchor, pos, size);
            DropShadow(rt, normal, new Vector2(6, -9), 0.6f, false);
            var img = UIKit.Stretch("Face", rt).gameObject.AddComponent<Image>();
            img.sprite = normal;
            img.material = FxMat(0.6f, Random.value * 10f, 5f);
            var b = rt.gameObject.AddComponent<Button>();
            b.targetGraphic = img;
            b.transition = Selectable.Transition.SpriteSwap;
            b.spriteState = new SpriteState { highlightedSprite = hover, pressedSprite = hover, selectedSprite = normal, disabledSprite = normal };
            b.onClick.AddListener(() => Sfx.Play("ui_click", 0.6f, 0.03f, 1));
            b.onClick.AddListener(onClick);
            var hs = rt.gameObject.AddComponent<HoverScale>(); hs.amount = 1.06f; hs.fx = img.material;
            var t = UIKit.Label("Text", rt, new Vector2(0.5f, 0.5f), new Vector2(size.x * 0.03f, 2), new Vector2(size.x * 0.8f, size.y), fontSize, TextAnchor.MiddleCenter, true, new Color(1f, 0.9f, 0.72f));
            t.text = label;
            return b;
        }

        Button IconButton(string name, Transform parent, Vector2 anchor, Vector2 pos, float size, string icon, string fallback, UnityEngine.Events.UnityAction onClick)
        {
            if (!L(icon)) return UIKit.MakeButton(name, parent, anchor, pos, new Vector2(size * 1.8f, size), fallback, onClick, 18);
            var rt = UIKit.MakeRect(name, parent, anchor, pos, Vector2.one * size);
            var img = rt.gameObject.AddComponent<Image>();
            img.sprite = L(icon); img.preserveAspect = true; img.material = FxMat(0.4f, Random.value * 10f);
            var b = rt.gameObject.AddComponent<Button>();
            b.targetGraphic = img;
            b.colors = new ColorBlock { normalColor = new Color(0.88f, 0.84f, 0.8f), highlightedColor = Color.white, pressedColor = new Color(1f, 0.7f, 0.6f), selectedColor = Color.white, disabledColor = Color.gray, colorMultiplier = 1, fadeDuration = 0.08f };
            b.onClick.AddListener(() => Sfx.Play("ui_click", 0.6f, 0.03f, 1));
            b.onClick.AddListener(onClick);
            rt.gameObject.AddComponent<HoverScale>().amount = 1.15f;
            return b;
        }

        // ---------- 패널 ----------
        void OpenPanel(Mode m)
        {
            if (loading) return;
            ClosePanel();
            mode = m;
            confirmSlot = -1;
            panel = new GameObject("Panel", typeof(RectTransform));
            panel.transform.SetParent(root, false);
            var prt = (RectTransform)panel.transform;
            prt.anchorMin = Vector2.zero; prt.anchorMax = Vector2.one; prt.offsetMin = prt.offsetMax = Vector2.zero;
            var dim = UIKit.Stretch("Dim", prt).gameObject.AddComponent<Image>();
            dim.color = new Color(0.04f, 0.01f, 0.03f, 0.62f);
            dim.gameObject.AddComponent<Button>().onClick.AddListener(() => { Sfx.Play("ui_close", 0.7f); ClosePanel(); });
            var frame = UIKit.MakeRect("Frame", prt, new Vector2(0.5f, 0.5f), new Vector2(0, -20), new Vector2(1320, 880));
            DropShadow(frame, L("panel"), new Vector2(16, -22), 0.75f, true);
            var fi = UIKit.Stretch("Face", frame).gameObject.AddComponent<Image>();
            fi.raycastTarget = true;
            if (L("panel")) { fi.sprite = L("panel"); fi.preserveAspect = true; fi.material = FxMat(0.6f, 3f, 6f); }
            else fi.sprite = UIKit.Spr("panel_plain");
            var head = UIKit.Label("Head", frame, new Vector2(0.5f, 1), new Vector2(0, -312), new Vector2(800, 60), 44, TextAnchor.MiddleCenter, true, UIKit.Gold);
            head.text = m == Mode.New ? "새로 시작" : m == Mode.Continue ? "이어하기" : "설정";
            IconButton("Back", frame, new Vector2(0, 1), new Vector2(190, -312), 60, "icon_back", "뒤로", ClosePanel);
            panelBody = UIKit.MakeRect("Body", frame, new Vector2(0.5f, 0.5f), new Vector2(0, -120), new Vector2(1060, 480));
            if (m == Mode.Settings) SettingsBody(panelBody); else SlotsBody(panelBody);
            StartCoroutine(Pop(frame));
        }

        void ClosePanel()
        {
            if (panel) Destroy(panel);
            panel = null;
            panelBody = null;
            mode = Mode.None;
        }

        /// <summary>슬롯 패널만 다시 그림 (확인 상태 유지)</summary>
        void RedrawSlots()
        {
            RefreshContinue();
            if (mode == Mode.Continue && !AnySlot()) { ClosePanel(); return; }
            if (!panelBody) return;
            for (int c = panelBody.childCount - 1; c >= 0; c--) Destroy(panelBody.GetChild(c).gameObject);
            SlotsBody(panelBody);
        }

        IEnumerator Pop(RectTransform rt)
        {
            var cg = rt.gameObject.AddComponent<CanvasGroup>();
            for (float t = 0; t < 1f && rt; t += Time.unscaledDeltaTime / 0.22f)
            {
                float e = 1 - Mathf.Pow(1 - t, 3);
                cg.alpha = t; rt.localScale = Vector3.one * Mathf.Lerp(0.9f, 1f, e);
                yield return null;
            }
            if (rt) { cg.alpha = 1; rt.localScale = Vector3.one; }
        }

        void SlotsBody(RectTransform body)
        {
            for (int s = 1; s <= Meta.SlotCount; s++)
            {
                int slot = s;
                bool used = Meta.SlotUsed(s);
                bool usable = mode == Mode.New || used;
                var card = UIKit.MakeRect("Slot" + s, body, new Vector2(0.5f, 0.5f), new Vector2((s - 2) * 300, 22), new Vector2(222, 372));
                DropShadow(card, L("slot_card"), new Vector2(9, -13), 0.65f, true);
                var ci = UIKit.Stretch("Face", card).gameObject.AddComponent<Image>();
                if (L("slot_card")) { ci.sprite = L("slot_card"); ci.preserveAspect = true; ci.material = FxMat(0.6f, s * 2.1f, 6f); }
                else ci.sprite = UIKit.Spr("panel_plain");
                if (!usable) ci.color = new Color(0.55f, 0.5f, 0.5f);
                else { var hs = card.gameObject.AddComponent<HoverScale>(); hs.fx = ci.material; }

                var hd = UIKit.Label("Head", card, new Vector2(0.5f, 0.5f), new Vector2(0, 58), new Vector2(200, 40), 30, TextAnchor.MiddleCenter, true, UIKit.Gold);
                hd.text = $"기억 {s}";
                var tx = UIKit.Label("Info", card, new Vector2(0.5f, 0.5f), new Vector2(0, -8), new Vector2(190, 100), 17, TextAnchor.MiddleCenter, false, UIKit.Cream);
                int ch = Meta.SlotChapters(s);
                tx.text = used
                    ? $"{(ch >= Meta.ChapterCount ? "11:59를 넘음" : ch == 0 ? "아직 1장 앞" : $"{ch}장까지 넘음")}\n시간의 조각 {Meta.SlotShards(s)} · 판 {Meta.SlotRuns(s)}번\n<size=15>{Meta.SlotLastPlayed(s)}</size>"
                    : "<color=#9C8F9C>비어 있는 기억</color>";
                if (!usable) continue;

                string label = mode == Mode.Continue ? "이어하기" : !used ? "여기서 시작" : confirmSlot == s ? "정말 덮어쓸까?" : "덮어쓰기";
                PlateButton("Go", card, new Vector2(0.5f, 0), new Vector2(0, 52), new Vector2(190, 36), label, () =>
                {
                    if (mode == Mode.New && Meta.SlotUsed(slot))
                    {
                        if (confirmSlot != slot) { confirmSlot = slot; RedrawSlots(); return; }
                        Meta.DeleteSlot(slot);
                    }
                    Enter(slot);
                }, "menu_button", 22);
                if (mode == Mode.Continue)
                {
                    IconButton("Delete", card, new Vector2(1, 1), new Vector2(-48, -120), 46, "icon_delete", "지우기", () =>
                    {
                        if (confirmSlot == slot) { Meta.DeleteSlot(slot); confirmSlot = -1; }
                        else confirmSlot = slot;
                        RedrawSlots();
                    });
                    if (confirmSlot == s)
                    {
                        var warn = UIKit.Label("Warn", card, new Vector2(1, 1), new Vector2(-48, -82), new Vector2(160, 30), 18, TextAnchor.MiddleCenter, true, new Color(1f, 0.5f, 0.4f));
                        warn.text = "한 번 더 = 지움";
                    }
                }
            }
        }

        void SettingsBody(RectTransform body)
        {
            SettingsUI.Build(body);                                        // 4탭 소리 · 화면 · 게임 · 조작 (Esc 일시정지 창과 같이 씀)
        }

        /// <summary>게임 종료 확인 창 (2026-11-19 사용자: 로비 버튼 → '정말 종료하시겠습니까?' → 예 = 꺼짐). 패널 자리를 같이 써서 Esc · 바깥 클릭 = 닫기</summary>
        void OpenQuitConfirm()
        {
            if (loading) return;
            ClosePanel();
            panel = new GameObject("QuitConfirm", typeof(RectTransform));
            panel.transform.SetParent(root, false);
            var prt = (RectTransform)panel.transform;
            prt.anchorMin = Vector2.zero; prt.anchorMax = Vector2.one; prt.offsetMin = prt.offsetMax = Vector2.zero;
            var dim = UIKit.Stretch("Dim", prt).gameObject.AddComponent<Image>();
            dim.color = new Color(0.04f, 0.01f, 0.03f, 0.62f);
            dim.gameObject.AddComponent<Button>().onClick.AddListener(() => { Sfx.Play("ui_close", 0.7f); ClosePanel(); });
            const float k = 0.6f;                                                    // 설정 창 판을 60%로
            var frame = UIKit.MakeRect("Frame", prt, new Vector2(0.5f, 0.5f), new Vector2(0, -20), new Vector2(1320, 880) * k);
            DropShadow(frame, L("panel"), new Vector2(12, -16), 0.75f, true);
            var fi = UIKit.Stretch("Face", frame).gameObject.AddComponent<Image>();
            fi.raycastTarget = true;
            if (L("panel")) { fi.sprite = L("panel"); fi.preserveAspect = true; fi.material = FxMat(0.6f, 3f, 6f); }
            else fi.sprite = UIKit.Spr("panel_plain");
            var q = UIKit.Label("Question", frame, new Vector2(0.5f, 0.5f), new Vector2(0, -10), new Vector2(620, 60), 36, TextAnchor.MiddleCenter, true, UIKit.Gold);
            q.text = "정말 종료하시겠습니까?";
            PlateButton("Yes", frame, new Vector2(0.5f, 0.5f), new Vector2(-140, -100), new Vector2(230, 60), "예", QuitGame, "menu_button", 28);
            PlateButton("No", frame, new Vector2(0.5f, 0.5f), new Vector2(140, -100), new Vector2(230, 60), "아니오", ClosePanel, "menu_button", 28);
            StartCoroutine(Pop(frame));
        }

        static void QuitGame()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        // ---------- 연출 ----------
        void Update()
        {
            float now = Time.unscaledTime, age = DebugAge >= 0 ? DebugAge : now - born;
            var kb = Keyboard.current; var mouse = Mouse.current;
            bool skip = (kb != null && kb.anyKey.wasPressedThisFrame) || (mouse != null && mouse.leftButton.wasPressedThisFrame);
            if (!introDone && skip && age > 0.2f) { born -= IntroLen; age += IntroLen; }

            // 1) 그림이 그려짐: 양피지 → 잉크 선 → 색 번짐
            float pParch = Mathf.Clamp01(age / 0.4f);
            if (parchment) parchment.color = new Color(Parchment.r, Parchment.g, Parchment.b, pParch * (1 - Mathf.Clamp01((age - 6.9f) / 0.7f)));
            StepStages(Mathf.Floor(age * 12f) / 12f);             // 손그림 애니처럼 12장/초로 끊어 진행 (로토스코핑)
            if (!introDone && age >= IntroLen) FinishIntro();

            // 2) 제목 '쾅' (색이 거의 다 찼을 때)
            if (!vfxOn && age > 7.2f && vfx) { vfxOn = true; vfx.Begin(); }
            if (!titleStamped && age > 7.8f) { titleStamped = true; stampAt = now; Burst(); }
            Vector2 jitter = Vector2.zero;
            if (title)
            {
                float st = titleStamped ? Mathf.Clamp01((now - stampAt) / 0.28f) : 0;
                titleGroup.alpha = titleStamped && !loading ? Mathf.Clamp01(st * 3f) : 0;
                float shake = titleStamped ? Mathf.Exp(-(now - stampAt) * 9f) * 10f * GameSettings.ShakeScale : 0;   // 환경설정 흔들림 세기 (2026-11-26)
                if (shake > 0.2f) jitter = Random.insideUnitCircle * shake;
                title.anchoredPosition = new Vector2(TitleX, TitleY + Mathf.Sin(now * 0.9f) * 4f) + jitter;
                float tick = introDone ? Mathf.Exp(-(now % 1f) * 14f) * 0.018f : 0;
                title.localScale = Vector3.one * (st < 1 ? Mathf.Lerp(1.6f, 1f, Ease(st)) : 1f + tick);
            }

            // 3) 평소: 그림 숨쉬기 · 시차 · 먼지 · 불티
            Vector2 m = Vector2.zero;
            if (mouse != null) { var mp = mouse.position.ReadValue(); m = new Vector2(mp.x / Mathf.Max(1, Screen.width) - 0.5f, mp.y / Mathf.Max(1, Screen.height) - 0.5f); }
            if (art)
            {
                // 줌 아웃: 처음엔 얼굴을 화면 가운데에 크게, 천천히 시작해 끝에서 부드럽게 멈춤
                float z = Mathf.Clamp01((Mathf.Floor(age * 12f) / 12f - ZoomStart) / ZoomLen);   // 카메라도 12장/초
                float ze = z < 1 ? 1 - Mathf.Pow(1 - Ease(z), 1.6f) : 1;
                float sc = Mathf.Lerp(ZoomFrom, ZoomTo, ze) + (introDone ? Mathf.Sin(now * 0.12f) * 0.015f : 0);
                art.localScale = Vector3.one * sc;
                var focus = Vector2.Lerp(Face / GameSettings.UiScale, Vector2.zero, ze);   // UI 크기로 캔버스가 줄면 얼굴 자리도 같이 (2026-11-26)
                parallax = Vector2.Lerp(parallax, introDone ? -m * 22f : Vector2.zero, 4f * Time.unscaledDeltaTime);
                art.anchoredPosition = -focus * sc + parallax + jitter * 0.5f;
            }
            float atmo = Mathf.Clamp01((age - 7.0f) / 1f);
            foreach (var f in fogs)
            {
                var p = f.anchoredPosition; p.x += 26f * Time.unscaledDeltaTime;
                if (p.x > 1400) p.x = -1400;
                f.anchoredPosition = p;
                var ri = f.GetComponent<RawImage>(); var c = ri.color; c.a = 0.14f * atmo; ri.color = c;
            }
            foreach (var (rt, img, speed, ph) in motes)
            {
                var p = rt.anchoredPosition;
                p.y += speed * Time.unscaledDeltaTime; p.x += (Mathf.Sin(now * 1.3f + ph) * 14f + 10f) * Time.unscaledDeltaTime;
                if (p.y > 560) { p.y = -560; p.x = Random.Range(-960f, 960f); }
                rt.anchoredPosition = p;
                var c = img.color; c.a = atmo * (0.4f + 0.4f * Mathf.Sin(now * 3f + ph)); img.color = c;
            }

            if (kb != null && kb.escapeKey.wasPressedThisFrame && panel) ClosePanel();
        }

        void Burst()
        {
            if (!vfx || !title) return;
            vfx.TitleBurst(vfx.transform.InverseTransformPoint(title.TransformPoint(title.rect.center)));
        }

        static float Ease(float t) => t * t * (3 - 2 * t);
        /// <summary>셰이더 smoothstep과 같음 (Mathf.SmoothStep은 보간이라 다름)</summary>
        static float Edge(float e0, float e1, float x) => Ease(Mathf.Clamp01((x - e0) / (e1 - e0)));

        void FinishIntro()
        {
            introDone = true;
            if (!titleStamped) { titleStamped = true; stampAt = Time.unscaledTime; Burst(); }
            if (!vfxOn && vfx) { vfxOn = true; vfx.Begin(); }
            bool any = AnySlot();
            for (int i = 0; i < menuButtons.Count; i++)
            {
                menuButtons[i].gameObject.SetActive(true);
                StartCoroutine(SlideIn(menuButtons[i], 0.1f + i * 0.1f, i == 1 && !any ? 0.4f : 1f));
            }
        }

        IEnumerator SlideIn(RectTransform rt, float delay, float alpha)
        {
            var cg = rt.GetComponent<CanvasGroup>();
            if (!cg) cg = rt.gameObject.AddComponent<CanvasGroup>();
            var to = rt.anchoredPosition;
            cg.alpha = 0; rt.anchoredPosition = to + new Vector2(260, 0);
            yield return new WaitForSecondsRealtime(delay);
            for (float t = 0; t < 1f && rt; t += Time.unscaledDeltaTime / 0.35f)
            {
                float e = 1 - Mathf.Pow(1 - t, 3);
                cg.alpha = t * alpha; rt.anchoredPosition = to + new Vector2(260 * (1 - e), 0);
                yield return null;
            }
            if (rt) { cg.alpha = alpha; rt.anchoredPosition = to; }
        }

        void Enter(int slot)
        {
            if (loading) return;
            loading = true;
            Meta.BeginSlot(slot);
            StartCoroutine(EnterCo());
        }

        /// <summary>잿빛 라벤더 안개가 덮이고 마을로 (마을에서 같은 안개가 걷히며 깨어남)</summary>
        IEnumerator EnterCo()
        {
            // 2026-11-18 사용자: 처음엔 마을을 거치지 않고 바로 첫 전투(1장) → 판이 끝나면 마을에서 깨어나 뻐꾸기와 대화
            bool first = Meta.FirstRunPending;
            if (first) Meta.SelectedChapter = 1;
            var op = SceneManager.LoadSceneAsync(first ? "Run" : "Village");
            op.allowSceneActivation = false;
            for (float t = 0; t < 1f; t += Time.unscaledDeltaTime / 0.7f)
            {
                hazeImg.color = new Color(Haze.r, Haze.g, Haze.b, t);
                yield return null;
            }
            hazeImg.color = Haze;
            op.allowSceneActivation = true;
        }
    }

    /// <summary>마우스를 올리면 살짝 커짐</summary>
    public class HoverScale : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        public float amount = 1.04f;
        public Material fx;                                   // UIFlatFx면 _Hover도 올림 (금이 달아오르고 빛이 훑음)
        bool over;
        float h;
        public void OnPointerEnter(PointerEventData e) { over = true; Sfx.Play("ui_hover", 0.35f, 0.04f, 1); }
        public void OnPointerExit(PointerEventData e) => over = false;
        void OnDisable() => over = false;
        void Update()
        {
            transform.localScale = Vector3.Lerp(transform.localScale, Vector3.one * (over ? amount : 1f), 12f * Time.unscaledDeltaTime);
            h = Mathf.MoveTowards(h, over ? 1f : 0f, Time.unscaledDeltaTime * 6f);
            if (fx) fx.SetFloat("_Hover", h);
        }
    }

    /// <summary>설정 슬라이더: 막대를 누르거나 끌면 손잡이가 따라가고 0~1 값을 알림</summary>
    public class SliderDrag : MonoBehaviour, IPointerDownHandler, IDragHandler
    {
        public RectTransform knob;
        public System.Action<float> onChange;
        public void OnPointerDown(PointerEventData e) => Set(e);
        public void OnDrag(PointerEventData e) => Set(e);
        void Set(PointerEventData e)
        {
            var rt = (RectTransform)transform;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(rt, e.position, e.pressEventCamera, out var lp)) return;
            float v = Mathf.Clamp01(Mathf.InverseLerp(rt.rect.xMin, rt.rect.xMax, lp.x));
            knob.anchorMin = knob.anchorMax = new Vector2(v, 0.5f);
            knob.anchoredPosition = Vector2.zero;
            onChange?.Invoke(v);
        }
    }
}
