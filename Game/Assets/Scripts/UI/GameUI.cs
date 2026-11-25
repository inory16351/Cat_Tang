// 화면 UI 총괄: 상단 HUD(체력 바 · 대시 · 골드 · 시간의 조각 · 방) + 하단 탄환 시계 HUD + 화면들
// 화면: 인벤토리(Tab) · 상인 · 판 결과 · 챕터 선택 · 마을 시설(기억의 방 · 총포상 · 탄약 공방 · 의상실)
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace CatTang
{
    public class GameUI : MonoBehaviour
    {
        public bool village;                    // 마을 씬이면 전투 HUD 숨김

        RectTransform root, hudRoot, screens;
        HudView hud;
        Image hpFill;
        RectTransform hpFillRt;
        float hpFillW;
        Text hpText, goldText, shardText, roomText, nextText, promptText, toastText, heatText, skillText;
        Text[] dash = new Text[3];
        Image dashBar;
        bool dashArt;
        static readonly Color DashPipEmpty = new Color(0.28f, 0.27f, 0.32f, 0.9f), DashPipOn = new Color(0.86f, 0.86f, 0.84f, 1f);   // 찬 칸도 살짝 눌러 HUD 톤에 맞춤
        readonly Image[] dashFill = new Image[3];
        readonly RectTransform[] dashCell = new RectTransform[3];
        readonly float[] dashPop = new float[3];
        readonly bool[] dashWasFull = new bool[3];
        GameObject dashGauge;
        static readonly Color DashOn = new Color(0.62f, 0.9f, 1f), DashOff = new Color(1f, 1f, 1f, 0.22f);
        GameObject prompt;
        PlayerController player;
        Coroutine toastCo;
        GameObject current;
        InventoryScreen inventory;
        /// <summary>열려 있는 인벤토리 (튜토리얼이 Tab 유도 후 기다림)</summary>
        public InventoryScreen InventoryUI => inventory;

        void Awake()
        {
            UIKit.Init();
            var cgo = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            cgo.transform.SetParent(transform, false);
            var canvas = cgo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = cgo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 1f;
            root = (RectTransform)cgo.transform;
            if (FindFirstObjectByType<EventSystem>() == null)
                new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));

            // 닷지 액션: 화면 전체 푸른 보랏빛 (시간이 느려지는 느낌)
            // 궁 준비: 화면 가장자리에서 일렁이는 불길 (열기 3칸이 차면)
            if (!village)
            {
                heatGlow = UIKit.Stretch("HeatGlow", root).gameObject.AddComponent<RawImage>();
                heatGlow.raycastTarget = false;
                heatGlow.color = new Color(1, 1, 1, 0);
                // 손 그림 테두리 불길 (Codex, FX/edge/edge_0~N)이 있으면 그걸, 없으면 코드로 만든 불길
                var drawn = new List<Texture2D>();
                for (int i = 0; i < 8; i++) { var t = Resources.Load<Texture2D>("FX/edge/edge_" + i); if (!t) break; drawn.Add(t); }
                heatFrames = drawn.Count > 0 ? drawn.ToArray() : new Texture2D[4];
                if (drawn.Count == 0) for (int i = 0; i < heatFrames.Length; i++) heatFrames[i] = MakeHeatFrame(17 + i * 31);
                heatGlow.texture = heatFrames[0];
            }
            dodgeTint = UIKit.Stretch("DodgeTint", root).gameObject.AddComponent<Image>();
            dodgeTint.color = new Color(0.35f, 0.45f, 1f, 0f);
            dodgeTint.raycastTarget = false;
            hudRoot = UIKit.Stretch("HUD", root);
            if (!village)
            {
                hud = new GameObject("HudView").AddComponent<HudView>();
                hud.transform.SetParent(transform, false);
                hud.lift = HudLift;                                   // 장전 HUD 밑에 경험치 바 자리
                hud.Build(hudRoot, 1080);
                HeadDash.Create(this);                                // 고양이 머리 위 남은 대시 '<<<'
                AimCursor.Create(this);                               // 조준 커서 '표적지'
            }
            BuildTop();
            if (!village) TraitHud.Build(hudRoot, new Vector2(30, -262));     // 활성 특성 목록 (마우스 올리면 효과)
            screens = UIKit.Stretch("Screens", root);
            toastText = UIKit.Label("Toast", root, new Vector2(0.5f, 1), new Vector2(0, -150), new Vector2(1200, 60), 38, TextAnchor.MiddleCenter, true, UIKit.Gold);
        }

        RawImage heatGlow;
        Texture2D[] heatFrames;
        float heatGlowA, heatFrameT;
        int heatFrame;

        /// <summary>가장자리 불길 한 장: 화면 끝에서 안쪽으로 뻗는 불꽃 혀 (노이즈로 들쭉날쭉), 바깥 진홍 → 안쪽 주황 · 노랑, 가운데는 투명</summary>
        static Texture2D MakeHeatFrame(int seed)
        {
            const int W = 640, H = 360;
            var rng = new System.Random(seed);
            // 값 노이즈 격자 (가장자리 둘레를 따라 불꽃 혀 크기가 달라지게)
            int GX = 48, GY = 27;
            var g = new float[GX + 1, GY + 1];
            for (int x = 0; x <= GX; x++) for (int y = 0; y <= GY; y++) g[x, y] = (float)rng.NextDouble();
            float N(float u, float v)
            {
                float fx = u * GX, fy = v * GY; int x0 = Mathf.Clamp((int)fx, 0, GX - 1), y0 = Mathf.Clamp((int)fy, 0, GY - 1);
                float tx = fx - x0, ty = fy - y0; tx = tx * tx * (3 - 2 * tx); ty = ty * ty * (3 - 2 * ty);
                return Mathf.Lerp(Mathf.Lerp(g[x0, y0], g[x0 + 1, y0], tx), Mathf.Lerp(g[x0, y0 + 1], g[x0 + 1, y0 + 1], tx), ty);
            }
            var tex = new Texture2D(W, H, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
            var px = new Color32[W * H];
            Color deep = new Color(0.55f, 0.03f, 0.05f), red = new Color(0.95f, 0.15f, 0.08f), orange = new Color(1f, 0.5f, 0.1f), yellow = new Color(1f, 0.85f, 0.35f);
            for (int y = 0; y < H; y++)
                for (int x = 0; x < W; x++)
                {
                    float u = x / (float)(W - 1), v = y / (float)(H - 1);
                    // 가장자리까지 거리 (화면 비율 보정, 0 = 끝)
                    float e = Mathf.Min(Mathf.Min(u, 1 - u) * 1.78f, Mathf.Min(v, 1 - v));
                    float n = N(u, v) * 0.6f + N(u * 2.7f % 1f, v * 2.7f % 1f) * 0.4f;
                    n = n * n;                                              // 대부분 짧고 가끔 길게 솟는 혀
                    float reach = 0.035f + 0.15f * n;                     // 불꽃 혀 길이
                    float f = Mathf.Clamp01(1 - e / reach);              // 1 = 끝, 0 = 혀 끝
                    float a = Mathf.SmoothStep(0, 1, f);
                    // 단계 색 (툰처럼 딱 끊김): 혀 끝 노랑 → 주황 → 빨강 → 바깥 진홍
                    Color c = f > 0.8f ? deep : f > 0.5f ? red : f > 0.25f ? orange : yellow;
                    c.a = a * (f > 0.25f ? 0.9f : 0.7f);
                    px[y * W + x] = c;
                }
            tex.SetPixels32(px);
            tex.Apply();
            return tex;
        }

        void UpdateHeatGlow()
        {
            if (!heatGlow) return;
            bool ready = player && player.rev.heat >= Revolver.HeatMax && !player.UltimateActive;
            heatGlowA = Mathf.MoveTowards(heatGlowA, ready ? 1 : 0, Time.unscaledDeltaTime * (ready ? 3f : 5f));
            if (heatGlowA <= 0.001f) { if (heatGlow.enabled) heatGlow.enabled = false; return; }
            heatGlow.enabled = true;
            // 일렁임: 불꽃 모양 4장을 번갈아 + 숨 쉬듯 밝기 · 살짝 커졌다 작아짐
            heatFrameT += Time.unscaledDeltaTime;
            if (heatFrameT > 0.11f) { heatFrameT = 0; heatFrame = (heatFrame + 1 + UnityEngine.Random.Range(0, 2)) % heatFrames.Length; heatGlow.texture = heatFrames[heatFrame]; }
            float t = Time.unscaledTime;
            float pulse = 0.78f + 0.22f * Mathf.Sin(t * 5.2f) * Mathf.Sin(t * 2.3f + 1f);
            heatGlow.color = new Color(1, 1, 1, heatGlowA * pulse * 0.85f);
            float s = 1.0f + 0.015f * Mathf.Sin(t * 3.7f);
            heatGlow.rectTransform.localScale = new Vector3(s, s, 1);
        }

        Image dodgeTint;
        Text dodgeText;
        float dodgeFlash, lastCritLeft;

        void UpdateDodge()
        {
            float left = player ? player.DodgeCritLeft : 0;
            if (left > lastCritLeft + 0.5f) dodgeFlash = 1f;                 // 방금 닷지 액션 발동
            lastCritLeft = left;
            // 느려진 동안(실시간 약 0.6초) 짙게 유지하다 풀림
            dodgeFlash = Mathf.MoveTowards(dodgeFlash, 0, Time.unscaledDeltaTime * 0.9f);
            var c = dodgeTint.color; c.a = 0.38f * Mathf.Clamp01(dodgeFlash * 1.6f); dodgeTint.color = c;
            if (!dodgeText) return;
            dodgeText.gameObject.SetActive(left > 0);
            if (left > 0) dodgeText.text = $"닷지!  치명타 ×{PlayerController.DodgeCritMul:0.0}  <size=20>{left:0.0}초</size>";
        }

        GameObject bossBar;
        Image bossFill, bossTrail;
        Text bossName;
        float bossShown = 1, bossTrailV = 1, bossLastHp, bossHold;
        Health bossHealth;

        void UpdateBossBar()
        {
            if (!bossBar) return;
            if (!bossHealth || !bossHealth.Alive)
            {
                bossHealth = null;
                foreach (var en in Enemy.All) if (en && en.health && en.health.IsBoss && en.health.Alive) { bossHealth = en.health; bossShown = bossTrailV = 1; bossLastHp = en.health.hp; break; }
            }
            bossBar.SetActive(bossHealth);
            if (!bossHealth) return;
            var rm = RunManager.Instance;
            var bossEnemy = bossHealth.GetComponent<Enemy>();
            bossName.text = bossEnemy && bossEnemy.brain ? bossEnemy.brain.DisplayName : rm && rm.Chapter != null ? rm.Chapter.BossName : "보스";
            float v = bossHealth.maxHp > 0 ? Mathf.Clamp01(bossHealth.hp / bossHealth.maxHp) : 0;
            bossShown = Mathf.MoveTowards(bossShown, v, Time.unscaledDeltaTime * 3f);
            if (bossHealth.hp < bossLastHp - 0.01f) bossHold = 0.4f;
            bossLastHp = bossHealth.hp;
            if (bossHold > 0) bossHold -= Time.unscaledDeltaTime; else bossTrailV = Mathf.MoveTowards(bossTrailV, v, Time.unscaledDeltaTime * 0.6f);
            if (bossTrailV < v) bossTrailV = v;
            bossFill.rectTransform.sizeDelta = new Vector2(952 * bossShown, 30);
            bossTrail.rectTransform.sizeDelta = new Vector2(952 * bossTrailV, 30);
        }

        void BuildTop()
        {
            // 체력 바 (이미지: 틀 + 안쪽 채움)
            var frame = UIKit.Img("HPFrame", hudRoot, "hp_frame", new Vector2(0, 1), new Vector2(24, -16), new Vector2(454, 221) * 0.95f);
            frame.rectTransform.pivot = new Vector2(0, 1);
            var hi = UIKit.HpInterior ?? new[] { 144, 47, 412, 101 };
            float sc = 0.95f;
            hpFill = UIKit.Img("HPFill", frame.transform, "hp_fill", new Vector2(0, 1), new Vector2(hi[0] * sc, -hi[1] * sc), new Vector2((hi[2] - hi[0]) * sc, (hi[3] - hi[1]) * sc), true, 1.6f);
            hpFillRt = hpFill.rectTransform;
            hpFillRt.pivot = new Vector2(0, 1);
            hpFillW = (hi[2] - hi[0]) * sc;
            hpText = UIKit.Label("HPText", frame.transform, new Vector2(0, 1), new Vector2((hi[0] + hi[2]) / 2f * sc, -(hi[1] + hi[3]) / 2f * sc), new Vector2(240, 40), 26, TextAnchor.MiddleCenter, true);
            hpText.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            // 대시 게이지: >> 기호 3칸 (찬 칸은 밝게, 차오르는 칸은 서서히) + 아래 가는 충전 바
            var dg = UIKit.MakeRect("DashGauge", hudRoot, new Vector2(0, 1), new Vector2(58, -196), new Vector2(240, 70), new Vector2(0, 0.5f));
            dashGauge = dg.gameObject;
            // 세피리아식 대시 칸: 체력 바 아래 작은 대시 아이콘(Codex: UI/dash_icon) 3개를 한 줄로.
            // 찬 칸 = 원래 색, 빈 칸 = 어두운 회색, 차오르는 칸 = 회색 위로 색이 아래에서 위로 차오름
            var sprIcon = UIKit.Spr("dash_icon");
            if (sprIcon)
            {
                dashArt = true;
                const float PipSize = 50, PipGap = 4;
                for (int i = 0; i < 3; i++)
                {
                    var cell = UIKit.MakeRect("Pip" + i, dg, new Vector2(0, 0.5f), new Vector2(i * (PipSize + PipGap), 0), new Vector2(PipSize, PipSize), new Vector2(0, 0.5f));
                    var bg = cell.gameObject.AddComponent<Image>(); bg.sprite = sprIcon; bg.preserveAspect = true; bg.raycastTarget = false;
                    bg.color = DashPipEmpty;
                    // HUD 다른 그림처럼 바닥에 진한 그림자를 깔아 붕 떠 보이지 않게
                    var shd = cell.gameObject.AddComponent<Shadow>(); shd.effectColor = new Color(0.05f, 0.03f, 0.08f, 0.75f); shd.effectDistance = new Vector2(3, -3);
                    dashFill[i] = UIKit.MakeRect("Fill", cell, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(PipSize, PipSize)).gameObject.AddComponent<Image>();
                    dashFill[i].sprite = sprIcon; dashFill[i].preserveAspect = true;
                    dashFill[i].type = Image.Type.Filled; dashFill[i].fillMethod = Image.FillMethod.Vertical; dashFill[i].fillOrigin = (int)Image.OriginVertical.Bottom;
                    dashFill[i].raycastTarget = false;
                    dashCell[i] = cell;
                }
            }
            for (int i = 0; i < 3; i++)
            {
                dash[i] = UIKit.Label("Dash" + i, dg, new Vector2(0, 0.5f), new Vector2(i * 70, 6), new Vector2(70, 56), 44, TextAnchor.MiddleCenter, true, DashOn);
                dash[i].rectTransform.pivot = new Vector2(0, 0.5f);
                dash[i].text = ">>";
                dash[i].gameObject.SetActive(!dashArt);
            }
            var barBg = UIKit.MakeRect("BarBg", dg, new Vector2(0, 0.5f), new Vector2(6, -26), new Vector2(198, 6), new Vector2(0, 0.5f)).gameObject.AddComponent<Image>();
            barBg.color = new Color(0, 0, 0, 0.45f);
            barBg.raycastTarget = false;
            barBg.gameObject.SetActive(!dashArt);
            dashBar = UIKit.MakeRect("Bar", barBg.transform, new Vector2(0, 0.5f), Vector2.zero, new Vector2(198, 6), new Vector2(0, 0.5f)).gameObject.AddComponent<Image>();
            dashBar.color = DashOn;
            dashBar.raycastTarget = false;
            dodgeText = UIKit.Label("DodgeCrit", hudRoot, new Vector2(0, 1), new Vector2(270, -196), new Vector2(420, 44), 26, TextAnchor.MiddleLeft, true, DashOn);
            dodgeText.rectTransform.pivot = new Vector2(0, 0.5f);
            dodgeText.gameObject.SetActive(false);
            BuildXpBar();
            // 골드 · 시간의 조각 (오른쪽 위)
            var coin = UIKit.Img("Coin", hudRoot, "coin", new Vector2(1, 1), new Vector2(-300, -60), new Vector2(64, 64));
            coin.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            goldText = UIKit.Label("Gold", hudRoot, new Vector2(1, 1), new Vector2(-180, -60), new Vector2(170, 50), 34, TextAnchor.MiddleLeft, true, UIKit.Gold);
            goldText.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            var shard = UIKit.Img("Shard", hudRoot, "shard", new Vector2(1, 1), new Vector2(-300, -130), new Vector2(52, 66));
            shard.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            shardText = UIKit.Label("Shards", hudRoot, new Vector2(1, 1), new Vector2(-180, -130), new Vector2(170, 50), 30, TextAnchor.MiddleLeft, true, UIKit.Blue);
            shardText.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            // 보스 체력 바 (화면 위쪽 가운데, 보스 방에서만)
            bossBar = UIKit.MakeRect("BossBar", hudRoot, new Vector2(0.5f, 1), new Vector2(0, -118), new Vector2(980, 86)).gameObject;
            var bFrame = UIKit.Img("Frame", bossBar.transform, "panel_plain", new Vector2(0.5f, 0.5f), new Vector2(0, -10), new Vector2(980, 50), true, 1.4f);
            bFrame.color = new Color(0.12f, 0.07f, 0.1f, 0.95f);
            bossTrail = UIKit.Img("Trail", bossBar.transform, "hp_fill", new Vector2(0, 0.5f), new Vector2(14, -10), new Vector2(952, 30), true, 1.6f);
            bossTrail.rectTransform.pivot = new Vector2(0, 0.5f);
            // 깎인 만큼 남는 흔적: 체력 바와 같은 모양(스프라이트 알파)을 순백으로 → 빨간 체력과 확실히 구분
            bossTrail.sprite = UIKit.WhiteSprite(bossTrail.sprite);
            bossTrail.color = Color.white;
            bossFill = UIKit.Img("Fill", bossBar.transform, "hp_fill", new Vector2(0, 0.5f), new Vector2(14, -10), new Vector2(952, 30), true, 1.6f);
            bossFill.rectTransform.pivot = new Vector2(0, 0.5f);
            bossName = UIKit.Label("Name", bossBar.transform, new Vector2(0.5f, 1), new Vector2(0, 6), new Vector2(900, 40), 30, TextAnchor.MiddleCenter, true, UIKit.Gold);
            bossBar.SetActive(false);
            roomText = UIKit.Label("Room", hudRoot, new Vector2(0.5f, 1), new Vector2(0, -40), new Vector2(900, 44), 28, TextAnchor.MiddleCenter, true, UIKit.Cream);
            float up = village ? 0 : HudLift - 4f;                       // 장전 HUD가 경험치 바만큼 올라간 만큼 같이
            nextText = UIKit.Label("Next", hudRoot, new Vector2(0.5f, 0), new Vector2(0, 285 + up), new Vector2(1000, 36), 22, TextAnchor.MiddleCenter);
            heatText = UIKit.Label("Heat", hudRoot, new Vector2(0.5f, 0), new Vector2(390, 40 + up), new Vector2(260, 40), 26, TextAnchor.MiddleLeft, true, UIKit.Gold);
            skillText = UIKit.Label("Skill", hudRoot, new Vector2(1, 0), new Vector2(-30, 30), new Vector2(420, 130), 20, TextAnchor.LowerRight);
            var help = UIKit.Label("Help", hudRoot, new Vector2(0, 0), new Vector2(26, 26), new Vector2(560, 150), 18, TextAnchor.LowerLeft, false, new Color(1, 1, 1, 0.55f));
            help.text = village ? "WASD 이동 · Space 대시 · E 들어가기 · Tab 인벤토리 · M 지도" : "WASD 이동 · 마우스 조준 · 좌클릭 사격 · Space 대시\n우클릭 총 스킬 · Q 불릿 파티\nE 상호작용 · Tab 인벤토리 · Esc 포기";
            // 상호작용 안내
            prompt = UIKit.Img("Prompt", hudRoot, "button", new Vector2(0.5f, 0), new Vector2(0, 340 + up), new Vector2(460, 84), true, 2.2f).gameObject;
            promptText = UIKit.Label("Text", prompt.transform, new Vector2(0.5f, 0.5f), new Vector2(0, 2), new Vector2(420, 60), 28, TextAnchor.MiddleCenter, true);
            prompt.SetActive(false);
            if (village)
            {
                foreach (var g in new GameObject[] { hpFill.transform.parent.gameObject, dashGauge, coin.gameObject, goldText.gameObject, nextText.gameObject, heatText.gameObject, skillText.gameObject })
                    g.SetActive(false);
            }
        }

        public void OnRunStart(PlayerController p)
        {
            player = p;
            hud?.Bind(p);
            CloseAll();
        }

        public void BindVillage(PlayerController p) { player = p; }

        void Update()
        {
            UpdateBossBar();
            if (!player) player = PlayerController.Instance;
            if (!player) return;
            var kb = Keyboard.current;
            if (kb != null && !StoryOverlay.HintOpen)            // 튜토리얼 안내가 떠 있으면 Tab · Esc로 화면이 닫히지 않게
            {
                if (kb.tabKey.wasPressedThisFrame)
                {
                    if (inventory) CloseAll();
                    else if (!current) OpenInventory();
                }
                if (kb.escapeKey.wasPressedThisFrame && current && !resultsOpen) CloseAll();
                else if (kb.escapeKey.wasPressedThisFrame && village && !current && !player.InputLocked && !leaving && !VillageMap.EscUsed) OpenVillageMenu();   // 깨어나는 중엔 안 열림
                else if (kb.escapeKey.wasPressedThisFrame && !village && !current && !resultsOpen && !player.InputLocked && !leaving && !MapView.EscUsed
                         && player.health.Alive) OpenGiveUp();                 // 판 안: 전투 포기하고 마을로 갈지 묻기
            }
            UpdateDodge();
            UpdateHeatGlow();
            var h = player.health;
            float f = Mathf.Clamp01(h.hp / Mathf.Max(1, h.maxHp));
            hpFillRt.sizeDelta = new Vector2(Mathf.Max(0.01f, hpFillW * f), hpFillRt.sizeDelta.y);
            hpFill.enabled = f > 0.01f;
            hpText.text = $"{Mathf.CeilToInt(h.hp)} / {Mathf.CeilToInt(h.maxHp)}";
            float dc = player.DashCharges, dp = player.DashRechargeProgress;
            if (dashArt)
            {
                // 칸마다 따로: 찬 칸 = 가득 · 밝게, 차오르는 칸 = 진행만큼 왼쪽부터 · 살짝 어둡게, 빈 칸 = 비움. 다 차는 순간 칸이 톡 튐
                for (int i = 0; i < 3; i++)
                {
                    bool full = dc >= i + 1;
                    bool filling = !full && i == Mathf.FloorToInt(dc);
                    dashFill[i].fillAmount = full ? 1f : filling ? dp : 0f;
                    dashFill[i].color = full ? DashPipOn
                        : filling && player.DashLocked ? Color.Lerp(new Color(0.62f, 0.66f, 0.7f, 0.85f), new Color(1f, 0.25f, 0.4f, 0.95f), 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 18f))   // 검표 펀치: 충전 멈춤 깜빡임
                        : new Color(0.62f, 0.66f, 0.7f, 0.85f);
                    if (filling && player.DashLocked) dashFill[i].fillAmount = Mathf.Max(0.15f, dp);
                    if (full && !dashWasFull[i]) dashPop[i] = 1f;
                    dashWasFull[i] = full;
                    dashPop[i] = Mathf.MoveTowards(dashPop[i], 0, Time.unscaledDeltaTime * 4f);
                    dashCell[i].localScale = Vector3.one * (1 + 0.2f * dashPop[i] * dashPop[i]);
                }
            }
            for (int i = 0; i < 3; i++)
            {
                bool full = dc >= i + 1;
                bool filling = !full && i == Mathf.FloorToInt(dc);
                float k = full ? 1f : filling ? dp * 0.6f : 0f;          // 차오르는 칸은 60%까지만 밝아짐 (다 차면 번쩍)
                dash[i].color = Color.Lerp(DashOff, DashOn, k);
                dash[i].transform.localScale = Vector3.one * (full ? 1f : 0.85f);
            }
            // 충전 바: 전체 대시 게이지 (칸 수 + 진행)
            UpdateXpBar();
            float total = Mathf.Clamp01((dc + (dc >= player.dashMax ? 0 : dp)) / Mathf.Max(1, player.dashMax));
            dashBar.rectTransform.sizeDelta = new Vector2(198 * total, 6);
            goldText.text = player.inv.gold.ToString();
            var rm = RunManager.Instance;
            shardText.text = rm ? $"{Meta.Shards} <size=22>(+{rm.shardsThisRun})</size>" : Meta.Shards.ToString();
            roomText.text = rm ? (rm.IsBossRoom ? $"{rm.chapter}장 · {rm.Chapter?.BossName}" : rm.IsMidBossRoom ? $"{rm.chapter}장 · 중간 보스" : $"{rm.chapter}장 · 스테이지 {rm.stageIndex + 1}/{RunManager.Stages} · 방 {rm.roomIndex + 1}/{rm.RoomCount}") + (rm.inCombat ? "" : "  <color=#FFD54A>· 다음 방 · 문으로</color>")
                                : "여관 앞 마을";

            var rev = player.rev;
            if (!village)
            {
                if (rev.reloading) nextText.text = "장전 중…";
                else
                {
                    int s = Mathf.Clamp(rev.next, 1, 12);
                    var b = rev.slots[s].bullet;
                    nextText.text = b == null ? $"다음 {s}시 · 빈 칸" :
                        $"다음 {s}시 · {b.Name} · 판정 {rev.JudgeTime(s)}시 · {(rev.Awakened(s) ? "<color=#FFD54A>활성화</color>" : "<color=#9AA0A6>비활성화</color>")}";
                }
                heatText.text = rev.heat >= Revolver.HeatMax ? "<color=#FFD54A>Q 불릿 파티!</color>" : $"열기 {rev.heat:0.0}/3";
                skillText.text = $"총 스킬 {(player.SkillCooldownLeft > 0 ? player.SkillCooldownLeft.ToString("0.0") + "초" : "준비")}";
            }
            var fcs = player.Focus;
            bool showPrompt = fcs && !current && !player.InputLocked;            // 깨어남 · 이야기 연출 중엔 숨김
            if (prompt.activeSelf != showPrompt) prompt.SetActive(showPrompt);
            if (showPrompt) promptText.text = $"<color=#FFD54A>E</color>  {fcs.prompt}";
        }

        // ---------- 대화 (마을 이야기, VillageStory) ----------
        /// <summary>화면 아래 대화 창: 이름 + 한 줄씩 (글자가 흘러나옴). E · 스페이스 · 엔터 · 클릭으로 넘김, 다 끝나면 done</summary>
        /// <summary>개발용 (IntroDemo · 스토리 녹화): 대사 · 안내 창을 1.3초마다 자동으로 넘김</summary>
        public static bool DevAutoAdvance;
        static float devNext;

        /// <summary>대사창 밑면 높이 = 경험치 바 밑면과 같게 (2026-11-25 사용자: 대화창을 경험치 UI 자리까지 내림)</summary>
        const float TalkRestY = XpBottom;

        public void Talk(string speaker, string[] lines, System.Action done)
        {
            var go = OpenScreen("Talk", false);
            StartCoroutine(TalkCo(go, speaker, lines, done));
        }

        // 대화 일러스트 (2026-11-23 사용자: 니케처럼 대사창 옆에 일러스트, 대화 중엔 HUD 숨김). Resources/UI/Portraits/<id>.png (투명 배경)
        static readonly Dictionary<string, string> PortraitId = new()
        {
            { "고양이", "cat" }, { "태엽 풀린 뻐꾸기", "cuckoo" }, { "모래시계 대장장이", "smith" }, { "실타래 재봉사", "tailor" }, { "빈 외투 보안관", "sheriff" }, { "괘종시계 문지기", "gatekeeper" },
        };
        static readonly Dictionary<string, Sprite> portraitCache = new();
        // 표정 · 자세 (2026-11-23 사용자: 하데스풍으로 상황에 맞는 표정과 자세): "고양이#두통::대사" → Portraits/cat_ache.png (없으면 cat.png)
        static readonly Dictionary<string, string> ExprId = new()
        {
            { "기본", "" }, { "두통", "ache" }, { "결의", "resolve" }, { "의문", "doubt" }, { "씩", "smirk" }, { "놀람", "shock" },
            { "쓸쓸", "wistful" }, { "다정", "warm" }, { "엄숙", "solemn" }, { "의미심장", "knowing" },
        };
        static Sprite Portrait(string who, string expr = null)
        {
            if (string.IsNullOrEmpty(who) || !PortraitId.TryGetValue(who, out var id)) return null;
            string key = id;
            if (!string.IsNullOrEmpty(expr) && ExprId.TryGetValue(expr, out var e) && e != "") key = id + "_" + e;
            if (portraitCache.TryGetValue(key, out var sp)) return sp;
            var tex = Resources.Load<Texture2D>("UI/Portraits/" + key);
            sp = tex ? Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0f)) : (key != id ? Portrait(who) : null);
            portraitCache[key] = sp;
            return sp;
        }
        /// <summary>"이름#표정::대사" → (이름, 표정, 대사). 화자 표시가 없으면 (fallback, null, 줄 전체)</summary>
        static (string who, string expr, string line) ParseLine(string raw, string fallback)
        {
            int k = raw.IndexOf("::", System.StringComparison.Ordinal);
            if (k <= 0) return (fallback, null, raw);
            string head = raw.Substring(0, k), expr = null;
            int h = head.IndexOf('#');
            if (h > 0) { expr = head.Substring(h + 1); head = head.Substring(0, h); }
            return (head, expr, raw.Substring(k + 2));
        }
        static string SpeakerOf(string raw, string fallback) => ParseLine(raw, fallback).who;

        // 대사 전환 연출 (2026-11-23 사용자: "대사 전환도 연출 효과 넣어서 자연스럽게")
        // 등장: 아래 어둠이 깔리고 일러스트가 양옆에서 미끄러져 들어오고 대사창이 살짝 튀어 오름
        // 화자 교대: 이름표가 톡 튀고, 말하는 쪽이 앞으로 나서며(밝게 · 크게 · 살짝 뜀) 듣는 쪽은 물러남(어둡게 · 작게), 대사창이 화자 쪽으로 살짝 쏠림
        // 표정 교체: 같은 자리에서 그림이 겹쳐 바뀜(0.18초) + 살짝 눌렸다 폄
        // 감정 강조: 두통 = 흔들림 + 붉은 번쩍 · 놀람 = 확 당김 · 결의 = 앞으로 밀기 · 엄숙 = 배경이 더 어두워짐
        // 퇴장: 일러스트가 양옆으로 빠지고 대사창이 내려가며 사라짐
        class Slot
        {
            public RectTransform holder; public Image front, back; public bool left; public Vector2 home;
            public float fade = 1f, hop, shake, push, squash, enter;
        }

        static float EaseOutBack(float t) { const float c = 1.6f; t -= 1; return 1 + (c + 1) * t * t * t + c * t * t; }

        IEnumerator TalkCo(GameObject go, string speaker, string[] lines, System.Action done)
        {
            bool hudWas = hudRoot && hudRoot.gameObject.activeSelf;
            if (hudRoot) hudRoot.gameObject.SetActive(false);
            // 아래쪽 어둠 (일러스트 · 대사창이 떠 보이게)
            var shade = UIKit.Stretch("Shade", go.transform).gameObject.AddComponent<Image>();
            shade.sprite = BottomShade(); shade.raycastTarget = false; shade.color = new Color(0.03f, 0.02f, 0.06f, 0);
            // 일러스트 무대: 고양이는 왼쪽, 상대는 오른쪽 (그림이 각자 가운데를 보게 그림)
            var who = new List<string>();
            foreach (var l in lines) { var n = SpeakerOf(l, speaker); if (!who.Contains(n) && Portrait(n)) who.Add(n); }
            who.Sort((a, b) => (b == "고양이").CompareTo(a == "고양이"));
            var stage = new Dictionary<string, Slot>();
            for (int i = 0; i < who.Count && i < 2; i++)
            {
                bool left = i == 0 && (who[i] == "고양이" || who.Count > 1);
                var sl = new Slot { left = left, home = new Vector2(left ? 360 : -360, -30), enter = -0.08f * i };
                sl.holder = UIKit.MakeRect("Portrait_" + i, go.transform, new Vector2(left ? 0 : 1, 0), sl.home, new Vector2(10, 10), new Vector2(0.5f, 0));
                Image Make(string n) { var rt = UIKit.MakeRect(n, sl.holder, new Vector2(0.5f, 0), Vector2.zero, new Vector2(600, 820), new Vector2(0.5f, 0)); var im = rt.gameObject.AddComponent<Image>(); im.raycastTarget = false; im.preserveAspect = true; return im; }
                sl.back = Make("Back"); sl.front = Make("Front");
                SetSprite(sl.front, Portrait(who[i])); sl.back.color = new Color(1, 1, 1, 0);
                stage[who[i]] = sl;
            }
            // 2026-11-25 사용자: 아래 장식 없앰 · 이름 명찰은 말하는 캐릭터 발밑(창 윗변)에 붙임
            var panelImg = UIKit.Panel("Panel", go.transform, new Vector2(0.5f, 0), new Vector2(0, TalkRestY), new Vector2(1320, 250), false);
            var panel = panelImg.rectTransform;
            var pg = panelImg.gameObject.AddComponent<CanvasGroup>(); pg.alpha = 0;
            var name = UIKit.Banner(speaker, panel, new Vector2(0, 1), new Vector2(250, 6), 440);
            var nameRt = name.transform.parent && name.transform.parent != panel ? (RectTransform)name.transform.parent : name.rectTransform;
            nameRt.pivot = new Vector2(0.5f, nameRt.pivot.y);
            // 일러스트 자리(화면 왼쪽 · 오른쪽 360px) → 창 기준 x. 창 너비 1320, 화면 1920 → 창 왼쪽 = 300
            float NameX(string who) => stage.TryGetValue(who ?? "", out var s0) ? (s0.left ? 360f - 300f + 40f : 1920f - 360f - 300f - 40f) : 250f + 220f;
            string firstWho = ParseLine(lines.Length > 0 ? lines[0] : "", speaker).who;
            float nameX = NameX(firstWho);
            nameRt.anchoredPosition = new Vector2(nameX, nameRt.anchoredPosition.y);
            var body = UIKit.Label("Line", panel, new Vector2(0.5f, 0.5f), new Vector2(0, -14), new Vector2(1160, 140), 28, TextAnchor.MiddleLeft, false, UIKit.Cream);
            var hint = UIKit.Label("Hint", panel, new Vector2(1, 0), new Vector2(-110, 30), new Vector2(200, 30), 18, TextAnchor.MiddleRight, false, new Color(1, 1, 1, 0.55f));
            hint.text = "E · 클릭";
            string active = null;
            float shadeTarget = 0.72f, namePop = 0, lean = 0, leanTarget = 0, flash = 0, closing = -1;
            var ache = new Color(1f, 0.55f, 0.55f);
            // 매 프레임 무대 갱신 (등장 · 교대 · 강조 · 퇴장)
            IEnumerator Animate()
            {
                float t0 = Time.unscaledTime;
                while (go)
                {
                    float dt = Time.unscaledDeltaTime, t = Time.unscaledTime - t0;
                    float open = Mathf.Clamp01(t / 0.35f), close = closing < 0 ? 0 : Mathf.Clamp01((Time.unscaledTime - closing) / 0.28f);
                    shade.color = new Color(0.03f, 0.02f, 0.06f, Mathf.Lerp(shade.color.a, shadeTarget * open * (1 - close), dt * 8f));
                    // 대사창: 아래에서 튀어 오름 → 화자 쪽으로 살짝 쏠림 → 퇴장 때 내려감
                    float pu = Mathf.Clamp01((t - 0.06f) / 0.32f);
                    lean = Mathf.Lerp(lean, leanTarget, dt * 9f);
                    panel.anchoredPosition = new Vector2(lean, Mathf.Lerp(-270, TalkRestY, EaseOutBack(pu)) - 280 * close * close);
                    pg.alpha = Mathf.Clamp01(pu * 2f) * (1 - close);
                    namePop = Mathf.MoveTowards(namePop, 0, dt * 5.5f);
                    float np = namePop > 0 ? 1 + Mathf.Sin(namePop * Mathf.PI) * 0.18f : 1;
                    nameRt.localScale = new Vector3(np, np, 1);
                    nameRt.anchoredPosition = new Vector2(Mathf.Lerp(nameRt.anchoredPosition.x, NameX(active ?? firstWho), dt * 14f), nameRt.anchoredPosition.y);
                    flash = Mathf.MoveTowards(flash, 0, dt * 3f);
                    foreach (var kv in stage)
                    {
                        var sl = kv.Value; bool on = kv.Key == active;
                        sl.enter += dt;
                        float e = Mathf.Clamp01(sl.enter / 0.42f), ee = EaseOutBack(e);
                        float off = (1 - ee) * 520 + close * close * 560;
                        sl.hop = Mathf.MoveTowards(sl.hop, 0, dt * 3.2f);
                        sl.shake = Mathf.MoveTowards(sl.shake, 0, dt * 2.2f);
                        sl.push = Mathf.MoveTowards(sl.push, 0, dt * 3f);
                        sl.squash = Mathf.MoveTowards(sl.squash, 0, dt * 6f);
                        float hopY = Mathf.Sin(sl.hop * Mathf.PI) * 26f;
                        var jit = sl.shake > 0 ? new Vector2(Mathf.Sin(Time.unscaledTime * 63f), Mathf.Sin(Time.unscaledTime * 51f)) * 10f * sl.shake : Vector2.zero;
                        float stepIn = on ? 18f : -10f;                                   // 말하는 쪽은 가운데로 반 걸음
                        sl.holder.anchoredPosition = sl.home + new Vector2((sl.left ? 1 : -1) * (stepIn - off), hopY + (on ? 0 : -14f)) + jit;
                        float sc = (on ? 1f : 0.9f) + Mathf.Sin(sl.push * Mathf.PI) * 0.06f;
                        var cur = sl.holder.localScale;
                        float k = Mathf.Lerp(cur.x, sc, dt * 10f);
                        sl.holder.localScale = new Vector3(k, k * (1 - 0.035f * Mathf.Sin(sl.squash * Mathf.PI)), 1);
                        var tint = on ? Color.white : new Color(0.36f, 0.33f, 0.43f, 1f);
                        if (on && flash > 0) tint = Color.Lerp(tint, ache, flash);
                        float a = Mathf.Clamp01(e * 1.6f) * (1 - close);
                        var c = Color.Lerp(sl.front.color, tint, dt * 9f); c.a = a;
                        sl.front.color = c;
                        sl.fade = Mathf.MoveTowards(sl.fade, 1, dt / 0.18f);           // 표정 교체: 뒤 그림이 사라지며 앞 그림이 드러남
                        var bc = c; bc.a = a * (1 - sl.fade); sl.back.color = bc;
                    }
                    yield return null;
                }
            }
            StartCoroutine(Animate());
            bool Next()
            {
                if (DevAutoAdvance && Time.unscaledTime >= devNext) { devNext = Time.unscaledTime + 1.3f; return true; }   // 개발용: 연출 녹화 때 자동 넘김
                var kb = Keyboard.current; var ms = Mouse.current;
                return (kb != null && (kb.eKey.wasPressedThisFrame || kb.spaceKey.wasPressedThisFrame || kb.enterKey.wasPressedThisFrame)) || (ms != null && ms.leftButton.wasPressedThisFrame);
            }
            void Finish() { if (hudRoot) hudRoot.gameObject.SetActive(hudWas); done?.Invoke(); }
            yield return new WaitForSecondsRealtime(0.28f);
            foreach (var raw in lines)
            {
                // "이름#표정::대사" = 이 줄의 화자 · 표정 (2026-11-23 스토리 기획서)
                var (whoNow, expr, line) = ParseLine(raw, speaker);
                if (whoNow != active)
                {
                    namePop = 1f;
                    if (active != null) Sfx.Play("ui_click_2", 0.35f, 0.08f, 1);
                    if (stage.TryGetValue(whoNow, out var ns)) { ns.hop = 1f; leanTarget = ns.left ? -14f : 14f; }
                }
                name.text = whoNow;
                active = whoNow;
                shadeTarget = expr == "엄숙" ? 0.86f : 0.72f;
                if (stage.TryGetValue(whoNow, out var sl))
                {
                    var sp = Portrait(whoNow, expr);
                    if (sp && sl.front.sprite != sp)
                    {
                        sl.back.sprite = sl.front.sprite; sl.back.rectTransform.sizeDelta = sl.front.rectTransform.sizeDelta;
                        SetSprite(sl.front, sp); sl.fade = 0; sl.squash = 1f;
                    }
                    if (expr == "두통") { sl.shake = 1f; flash = 0.8f; }
                    else if (expr == "놀람") { sl.push = 1f; sl.shake = 0.4f; }
                    else if (expr == "결의") sl.push = 1f;
                }
                // 글자가 흘러나옴 (줄 처음엔 살짝 비었다가 들어옴)
                body.color = new Color(UIKit.Cream.r, UIKit.Cream.g, UIKit.Cream.b, 0);
                int blip = -1;
                bool skip = false;                                    // 2026-11-25 사용자: 클릭하면 (글자가 흐르는 중이어도) 바로 다음 대사로
                for (float c = 0; c < line.Length; c += Time.unscaledDeltaTime * 42f)
                {
                    if (!go) { Finish(); yield break; }             // ESC로 닫힘
                    body.color = new Color(UIKit.Cream.r, UIKit.Cream.g, UIKit.Cream.b, Mathf.Min(1, body.color.a + Time.unscaledDeltaTime * 8f));
                    body.text = line.Substring(0, Mathf.Min(line.Length, (int)c + 1));
                    if ((int)c / 3 != blip) { blip = (int)c / 3; if (line[Mathf.Min(line.Length - 1, (int)c)] != ' ') Sfx.Play("talk", 0.4f, 0.12f, 1); }   // 글자 세 개마다 작은 소리
                    yield return null;
                    if (Next()) { skip = true; break; }
                }
                body.text = line; body.color = UIKit.Cream;
                if (skip) continue;
                yield return null;
                while (go && !Next()) { hint.color = new Color(1, 1, 1, 0.35f + 0.3f * Mathf.Sin(Time.unscaledTime * 5f)); yield return null; }
                if (!go) { Finish(); yield break; }
            }
            // 퇴장
            closing = Time.unscaledTime;
            yield return new WaitForSecondsRealtime(0.3f);
            if (current == go) CloseAll();
            Finish();
        }

        static void SetSprite(Image im, Sprite sp)
        {
            im.sprite = sp;
            if (sp) im.rectTransform.sizeDelta = new Vector2(820f * sp.rect.width / sp.rect.height, 820f);
        }

        /// <summary>아래에서 위로 옅어지는 어둠 (대사창 · 일러스트 뒤)</summary>
        static Sprite bottomShade;
        static Sprite BottomShade()
        {
            if (bottomShade) return bottomShade;
            var t = new Texture2D(4, 128, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            for (int y = 0; y < 128; y++) { float a = Mathf.Pow(1f - y / 127f, 1.6f); for (int x = 0; x < 4; x++) t.SetPixel(x, y, new Color(1, 1, 1, a)); }
            t.Apply();
            return bottomShade = Sprite.Create(t, new Rect(0, 0, 4, 128), new Vector2(0.5f, 0.5f));
        }

        public void Toast(string msg)
        {
            if (toastCo != null) StopCoroutine(toastCo);
            toastCo = StartCoroutine(ToastCo(msg));
        }

        IEnumerator ToastCo(string msg)
        {
            toastText.text = msg;
            toastText.color = UIKit.Gold;
            yield return new WaitForSecondsRealtime(1.8f);
            for (float a = 1; a > 0; a -= Time.unscaledDeltaTime * 2) { toastText.color = new Color(UIKit.Gold.r, UIKit.Gold.g, UIKit.Gold.b, a); yield return null; }
            toastText.text = "";
        }

        // ---------- 경험치 바 (세피리아식, 화면 아래 장전 HUD 밑 — 2026-11-17 사용자: 대시 칸 아래 → 장전 UI 아래) ----------
        // 그림 = Codex UI/xp_bar (놋쇠 틀 + 왼쪽 레벨 배지). 안쪽 빈 홈(XpSlot, 그림 비율)에 청록 채움을 왼쪽부터, 배지 가운데(XpBadge)에 레벨 숫자
        public static readonly Rect XpSlot = new(0.106f, 0.38f, 0.832f, 0.24f);       // x, y(아래부터), w, h — 그림 기준 비율 (docs/tools/ui_xp_cut.py로 잰 홈 안쪽)
        public static readonly Vector2 XpBadge = new(0.048f, 0.506f);
        static readonly Color XpColor = new(0.42f, 0.92f, 1f);
        const float XpW = 470f, XpBottom = 6f;
        static float XpH { get { var spr = UIKit.Spr("xp_bar"); return spr ? XpW * spr.rect.height / spr.rect.width : 44f; } }
        /// <summary>장전 HUD를 화면 아래에서 띄우는 높이 = 경험치 바 높이 + 여백</summary>
        static float HudLift => XpBottom + XpH + 2f;
        Image xpFill; RectTransform xpFillRt; Text xpLevel; float xpFillW, xpShown; int xpLastLevel;

        void BuildXpBar()
        {
            if (village) return;
            var spr = UIKit.Spr("xp_bar");
            float h = XpH;
            var root = UIKit.MakeRect("XpBar", hudRoot, new Vector2(0.5f, 0), new Vector2(0, XpBottom), new Vector2(XpW, h), new Vector2(0.5f, 0));
            if (!spr)
            {
                var bg = root.gameObject.AddComponent<Image>(); bg.color = new Color(0.1f, 0.07f, 0.13f, 0.85f); bg.raycastTarget = false;
            }
            if (spr)
            {
                var fr = UIKit.MakeRect("Frame", root, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(XpW, h)).gameObject.AddComponent<Image>();
                fr.sprite = spr; fr.raycastTarget = false;
                var shd = fr.gameObject.AddComponent<Shadow>(); shd.effectColor = new Color(0.05f, 0.03f, 0.08f, 0.75f); shd.effectDistance = new Vector2(3, -3);
            }
            // 채움: 틀 위, 홈 안쪽에 왼쪽부터 (홈이 불투명한 짙은 보라라 틀 밑에 깔면 안 보임)
            xpFillW = XpW * XpSlot.width;
            xpFill = UIKit.MakeRect("Fill", root, new Vector2(0, 0), new Vector2(XpW * XpSlot.x, h * XpSlot.y), new Vector2(0, h * XpSlot.height), new Vector2(0, 0)).gameObject.AddComponent<Image>();
            xpFill.color = XpColor; xpFill.raycastTarget = false;
            xpFillRt = xpFill.rectTransform;
            xpLevel = UIKit.Label("Level", root, new Vector2(0, 0), new Vector2(XpW * XpBadge.x, h * XpBadge.y), new Vector2(80, 50), 22, TextAnchor.MiddleCenter, true, UIKit.Gold);
            xpLevel.rectTransform.pivot = new Vector2(0.5f, 0.5f);
        }

        void UpdateXpBar()
        {
            var rm = RunManager.Instance;
            if (!xpFill || !rm) return;
            if (rm.level != xpLastLevel) { if (xpLastLevel > 0) xpShown = 0; xpLastLevel = rm.level; }
            xpShown = Mathf.MoveTowards(xpShown, rm.XpProgress, Time.unscaledDeltaTime * 1.5f);
            xpFillRt.sizeDelta = new Vector2(Mathf.Max(0, xpFillW * xpShown), xpFillRt.sizeDelta.y);
            xpFill.enabled = xpShown > 0.005f;
            xpLevel.text = rm.level.ToString();
        }

        /// <summary>창(보상 · 상점 · 보관함 · 결과 등)이 열려 있음 — 레벨 업 창은 닫힌 뒤에 뜸</summary>
        public bool ScreenOpen => current || resultsOpen;

        // ---------- 화면 열고 닫기 ----------
        bool resultsOpen;

        GameObject OpenScreen(string name, bool pause = true)
        {
            CloseAll();
            var rt = UIKit.Stretch(name, screens);
            current = rt.gameObject;
            if (player) player.InputLocked = true;
            if (pause) Time.timeScale = 0f;
            return current;
        }

        public void CloseAll()
        {
            if (resultsOpen) return;
            if (current) Destroy(current);
            current = null;
            inventory = null;
            if (player) player.InputLocked = false;
            Time.timeScale = 1f;
        }

        public void OpenInventory()
        {
            var go = OpenScreen("InventoryRoot");
            inventory = InventoryScreen.Open(go.transform, player);
        }

        // ---------- 상인 ----------
        public void OpenShop(Merchant m)
        {
            var go = OpenScreen("Shop");
            UIKit.Dim(go.transform);
            var panel = UIKit.Panel("Panel", go.transform, new Vector2(0.5f, 0.5f), new Vector2(0, 0), new Vector2(1400, 820), true);
            UIKit.Banner("떠돌이 상인", panel.transform, new Vector2(0.5f, 1), new Vector2(0, 10), 560);
            UIKit.CloseButton(panel.transform, CloseAll);
            var gold = UIKit.Label("Gold", panel.transform, new Vector2(0.5f, 1), new Vector2(0, -122), new Vector2(600, 40), 30, TextAnchor.MiddleCenter, true, UIKit.Gold);
            void Redraw()
            {
                for (int c = panel.transform.childCount - 1; c >= 0; c--)
                    if (panel.transform.GetChild(c).name.StartsWith("Card")) Destroy(panel.transform.GetChild(c).gameObject);
                gold.text = $"골드 {player.inv.gold}  ·  보관함 {player.inv.storage.Count(x => x != null)}/{Inventory.StorageSize}";
                for (int i = 0; i < m.stock.Count; i++)
                {
                    var it = m.stock[i];
                    int idx = i, price = Shop.PriceOf(it);
                    // 카드 2줄: 위 줄은 골드 글 아래, 아래 줄은 회복 · 새로고침 버튼 위 (서로 안 겹치게)
                    var card = UIKit.Panel("Card" + i, panel.transform, new Vector2(0.5f, 0.5f), new Vector2(-430 + (i % 3) * 430, 96 - (i / 3) * 254), new Vector2(400, 240), false, "tooltip", 2f);   // 3열이 패널(1400) 테두리 안에 들어가게 (2026-11-19)
                    var icon = UIKit.Img("Icon", card.transform, "slot", new Vector2(0, 0.5f), new Vector2(90, 20), new Vector2(130, 130));
                    icon.rectTransform.pivot = new Vector2(0.5f, 0.5f);
                    icon.enabled = false;                                // 네모 틀 없이 아이콘만
                    var inner = UIKit.MakeRect("I", icon.transform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(130, 130)).gameObject.AddComponent<Image>();
                    inner.preserveAspect = true;
                    inner.sprite = it.kind switch { ItemKind.Bullet => UIKit.BulletIcon(it.bullet), ItemKind.Gun => UIKit.GunIcon(it.gun), _ => UIKit.Spr("op_token") };
                    var nm = UIKit.Label("Name", card.transform, new Vector2(0, 1), new Vector2(165, -52), new Vector2(215, 56), 24, TextAnchor.MiddleLeft, true, new Color(0.3f, 0.18f, 0.1f));
                    nm.rectTransform.pivot = new Vector2(0, 0.5f);
                    nm.text = it.Name;
                    var ds = UIKit.Label("Desc", card.transform, new Vector2(0, 1), new Vector2(165, -98), new Vector2(215, 56), 18, TextAnchor.UpperLeft, false, new Color(0.3f, 0.2f, 0.12f));
                    ds.rectTransform.pivot = new Vector2(0, 1);
                    ds.text = it.kind switch
                    {
                        ItemKind.Bullet => $"{UIKit.GradeKo(it.bullet.Grade)} · 공격 {it.bullet.Damage}\n조건 {UIKit.CondKo(it.bullet)}",
                        ItemKind.Gun => $"{UIKit.GradeKo(it.gun.Grade)} · ×{it.gun.DamageMul:0.00}",
                        _ => "탄에 끼워 판정 시각을 바꿈"
                    };
                    foreach (var t in new[] { nm, ds }) foreach (var sh in t.GetComponents<Shadow>()) Destroy(sh);
                    var btn = UIKit.MakeButton("Buy", card.transform, new Vector2(0.5f, 0), new Vector2(55, 36), new Vector2(240, 64), m.sold[i] ? "판매됨" : $"{price} 골드", () =>
                    {
                        bool isGun = it.kind == ItemKind.Gun;
                        if (m.sold[idx] || player.inv.gold < price || (!isGun && !player.inv.HasSpace)) return;
                        player.inv.gold -= price;
                        m.sold[idx] = true;
                        if (isGun) { ShowGunPick(it.gun, () => OpenShop(m)); return; }   // 총은 교체 창 → 상점으로 돌아옴
                        player.inv.Add(it);
                        Redraw();
                    }, 24);
                    UIKit.SetInteractable(btn, !m.sold[i] && player.inv.gold >= price && (it.kind == ItemKind.Gun || player.inv.HasSpace));
                    CursorTip.Attach(card.gameObject, () => UIKit.ItemTip(it));
                }
                int healPct = Mathf.RoundToInt(HeartPickup.HealPct * 100);
                var heal = UIKit.MakeButton("CardHeal", panel.transform, new Vector2(0.5f, 0), new Vector2(-260, 52), new Vector2(420, 74),
                    m.heartsLeft > 0 ? $"하트 · 체력 {healPct}%  ({Merchant.HeartCost} 골드 · {m.heartsLeft}개)" : "하트 · 다 팔림", () =>
                {
                    if (m.heartsLeft <= 0 || player.inv.gold < Merchant.HeartCost || player.health.hp >= player.health.maxHp) return;
                    player.inv.gold -= Merchant.HeartCost;
                    m.heartsLeft--;
                    player.health.Heal(player.health.maxHp * HeartPickup.HealPct);
                    Redraw();
                }, 22);
                UIKit.SetInteractable(heal, m.heartsLeft > 0 && player.inv.gold >= Merchant.HeartCost && player.health.hp < player.health.maxHp);
                var reroll = UIKit.MakeButton("CardReroll", panel.transform, new Vector2(0.5f, 0), new Vector2(260, 52), new Vector2(420, 74), $"새로고침  ({m.RerollCost} 골드)", () =>
                {
                    if (player.inv.gold < m.RerollCost) return;
                    player.inv.gold -= m.RerollCost;
                    m.rerolls++;
                    m.Restock(player);
                    Redraw();
                }, 22);
                UIKit.SetInteractable(reroll, player.inv.gold >= m.RerollCost);
            }
            Redraw();
        }

        // ---------- 보상 고르기 ----------
        /// <summary>보상을 하나 고를 때마다 (판 2 튜토리얼이 들음)</summary>
        public static event System.Action RewardPicked;

        /// <param name="reroll">선택지를 새로 뽑는 함수 — 기억의 방 '다시 뽑기'를 해금했고 이번 판에 안 썼으면 '다시 뽑기' 버튼이 나옴</param>
        public void ShowRewardPick(string title, List<RewardOption> options, System.Action onDone, System.Func<List<RewardOption>> reroll = null)
        {
            var done0 = onDone; onDone = () => { RewardPicked?.Invoke(); done0?.Invoke(); };
            var go = OpenScreen("RewardPick");
            var rm = RunManager.Instance;
            if (reroll != null && rm && rm.RerollLeft)
            {
                bool used = false;
                UIKit.MakeButton("Reroll", go.transform, new Vector2(0.5f, 0.5f), new Vector2(0, -430), new Vector2(380, 66), "다시 뽑기 (1회)", () =>
                {
                    if (used || !rm.RerollLeft) return;
                    used = true; rm.RerollLeft = false;
                    Sfx.Play("ui_click_1", 0.7f);
                    CursorTip.Hide();
                    CloseAll();
                    ShowRewardPick(title, reroll(), done0);
                }, 22);
            }
            UIKit.Dim(go.transform, 0.78f);
            UIKit.Banner(title, go.transform, new Vector2(0.5f, 0.5f), new Vector2(0, 330), 760);
            for (int i = 0; i < options.Count; i++)
            {
                var o = options[i];
                float x = (i - (options.Count - 1) / 2f) * 470;
                var card = UIKit.Panel("Card" + i, go.transform, new Vector2(0.5f, 0.5f), new Vector2(x, -10), new Vector2(430, 520), false, "tooltip", 1.8f);
                var frame = UIKit.Img("Frame", card.transform, "slot", new Vector2(0.5f, 1), new Vector2(0, -120), new Vector2(170, 170));
                frame.rectTransform.pivot = new Vector2(0.5f, 0.5f);
                frame.enabled = false;                                   // 네모 틀 없이 아이콘만
                var ic = UIKit.MakeRect("Icon", frame.transform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(170, 170)).gameObject.AddComponent<Image>();
                ic.sprite = o.icon; ic.preserveAspect = true; ic.raycastTarget = false;
                var nm = UIKit.Label("Name", card.transform, new Vector2(0.5f, 1), new Vector2(0, -230), new Vector2(380, 80), 28, TextAnchor.UpperCenter, true, new Color(0.3f, 0.18f, 0.1f));
                nm.rectTransform.pivot = new Vector2(0.5f, 1);
                nm.text = o.label;
                // 설명 칸: 이름 아래 ~ 고르기 버튼 위 (버튼과 겹치지 않게 높이 95, 넘치면 글자가 줄어듦)
                var ds = UIKit.Label("Desc", card.transform, new Vector2(0.5f, 1), new Vector2(0, -305), new Vector2(370, 95), 18, TextAnchor.UpperCenter, false, new Color(0.32f, 0.22f, 0.14f));
                ds.rectTransform.pivot = new Vector2(0.5f, 1);
                ds.text = o.desc;
                foreach (var t in new[] { nm, ds }) foreach (var sh in t.GetComponents<Shadow>()) Destroy(sh);
                bool picked = false;
                UIKit.MakeButton("Pick", card.transform, new Vector2(0.5f, 0), new Vector2(0, 56), new Vector2(280, 76), "고르기", () =>
                {
                    if (picked) return;          // 두 번 눌림 방지
                    picked = true;
                    CursorTip.Hide();
                    CloseAll();
                    // 총: 교체 창이 열리고, 손을 고르거나 버린 뒤에 onDone (전에는 onDone이 바로 불려 교체 창이 닫혀 버림)
                    if (o.applyThen != null) { o.applyThen(onDone); return; }
                    o.apply?.Invoke();
                    onDone?.Invoke();
                }, 26);
                if (o.item != null) CursorTip.Attach(card.gameObject, () => UIKit.ItemTip(o.item));
            }
        }

        // ---------- 총 줍기: 오른손 교체 · 왼손 교체 · 버리기 ----------
        // 총은 양손 2자루만. 손에서 빠진 총 · 버린 총은 바닥에 떨어져 다시 주울 수 있다.
        public void ShowGunPick(GunDef g, System.Action after)
        {
            if (g == null || player == null) { after?.Invoke(); return; }
            var go = OpenScreen("GunPick");
            UIKit.Dim(go.transform, 0.8f);
            UIKit.Banner($"총을 주웠다 — {g.Name}", go.transform, new Vector2(0.5f, 0.5f), new Vector2(0, 380), 860);
            var rev = player.rev;
            var brown = new Color(0.3f, 0.18f, 0.1f);
            var body = new Color(0.32f, 0.22f, 0.14f);

            void Finish(GunDef dropped)
            {
                if (dropped != null) GunPickup.Drop(dropped, player.transform.position);
                CloseAll();
                after?.Invoke();
            }

            GameObject GunCard(string name, Vector2 pos, GunDef gun, string head, Color headCol, string text)
            {
                var card = UIKit.Panel(name, go.transform, new Vector2(0.5f, 0.5f), pos, new Vector2(500, 560), false, "tooltip", 1.8f);
                UIKit.Label("Head", card.transform, new Vector2(0.5f, 1), new Vector2(0, -42), new Vector2(460, 40), 24, TextAnchor.MiddleCenter, true, headCol).text = head;
                var frame = UIKit.Img("Frame", card.transform, "slot", new Vector2(0.5f, 1), new Vector2(0, -140), new Vector2(140, 140));
                frame.rectTransform.pivot = new Vector2(0.5f, 0.5f);
                frame.enabled = false;                                   // 네모 틀 없이 아이콘만
                var ic = UIKit.MakeRect("Icon", frame.transform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(140, 140)).gameObject.AddComponent<Image>();
                ic.sprite = UIKit.GunIcon(gun); ic.preserveAspect = true; ic.raycastTarget = false;
                var nm = UIKit.Label("Name", card.transform, new Vector2(0.5f, 1), new Vector2(0, -232), new Vector2(460, 40), 26, TextAnchor.MiddleCenter, true, brown);
                nm.text = gun != null ? $"{gun.Name}  <size=18>{UIKit.GradeKo(gun.Grade)} · ×{gun.DamageMul:0.00} · 사거리 {gun.Range:0}m</size>" : "-";
                var ds = UIKit.Label("Desc", card.transform, new Vector2(0.5f, 1), new Vector2(0, -268), new Vector2(450, 190), 18, TextAnchor.UpperLeft, false, body);
                ds.rectTransform.pivot = new Vector2(0.5f, 1);
                ds.supportRichText = true;
                ds.text = text;
                foreach (var t in new[] { nm, ds }) foreach (var sh in t.GetComponents<Shadow>()) Destroy(sh);
                return card.gameObject;
            }

            string Swap(GunDef cur, bool right) =>
                $"<b>지금</b>  {(cur != null ? UIKit.HandEffectLine(cur, right) : "-")}\n\n<color=#B8862B><b>바꾸면</b>  {UIKit.HandEffectLine(g, right)}</color>";

            // 왼쪽 = 왼손, 가운데 = 새 총, 오른쪽 = 오른손 (화면 배치 = 손 위치)
            var lc = GunCard("CardLeft", new Vector2(-540, -20), rev.leftGun, "왼손 · 짝수 칸", UIKit.Blue, Swap(rev.leftGun, false));
            var nc = GunCard("CardNew", new Vector2(0, -20), g, "새 총", UIKit.Gold, UIKit.GunHandText(g) + $"\n\n<i>{g.Desc}</i>");
            var rc = GunCard("CardRight", new Vector2(540, -20), rev.rightGun, "오른손 · 홀수 칸", UIKit.Gold, Swap(rev.rightGun, true));

            UIKit.MakeButton("TakeLeft", lc.transform, new Vector2(0.5f, 0), new Vector2(0, 56), new Vector2(340, 76), "왼손에 쥐기", () =>
            { var old = rev.leftGun; rev.leftGun = g; Toast(old != null ? $"{old.Name}을(를) 바닥에 내려놓음" : $"{g.Name} — 왼손"); Finish(old); }, 24);
            UIKit.MakeButton("TakeRight", rc.transform, new Vector2(0.5f, 0), new Vector2(0, 56), new Vector2(340, 76), "오른손에 쥐기", () =>
            { var old = rev.rightGun; rev.rightGun = g; Toast(old != null ? $"{old.Name}을(를) 바닥에 내려놓음" : $"{g.Name} — 오른손"); Finish(old); }, 24);
            UIKit.MakeButton("Leave", nc.transform, new Vector2(0.5f, 0), new Vector2(0, 56), new Vector2(340, 76), "버리기 (바닥에 둠)", () =>
            { Toast($"{g.Name}을(를) 바닥에 둠 — 다시 주울 수 있음"); Finish(g); }, 22);
        }

        // ---------- 판 결과 ----------
        public void ShowResults(bool win, int shards, int chapter)
        {
            var go = OpenScreen("Results");
            resultsOpen = true;
            UIKit.Dim(go.transform, 0.85f);
            var panel = UIKit.Panel("Panel", go.transform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1000, 620), true);
            var rm = RunManager.Instance;
            int before = rm ? rm.clearedAtStart : 0;
            bool all = Meta.ChaptersCleared >= Meta.ChapterCount;
            UIKit.Banner(win ? (all && before < Meta.ChapterCount ? "11:59를 넘었다" : $"{chapter}장 돌파") : "시간에 삼켜졌다", panel.transform, new Vector2(0.5f, 1), new Vector2(0, 10), 600);
            var t = UIKit.Label("Body", panel.transform, new Vector2(0.5f, 0.5f), new Vector2(0, 40), new Vector2(820, 300), 30, TextAnchor.MiddleCenter);
            string unlock = "";
            for (int f = 0; f < 4; f++)
                if (Meta.FacilityUnlockChapter[f] > before && Meta.FacilityUnlockChapter[f] <= Meta.ChaptersCleared && Meta.FacilityQuestKey[f] == null) unlock += $"\n<color=#FFD54A>{Meta.FacilityName[f]} 해금</color>";
            if (unlock != "") unlock = "\n" + unlock;
            string next = !all ? $"\n다음 판은 {Mathf.Min(Meta.ChapterCount, Meta.ChaptersCleared + 1)}장까지 갈 수 있다." : "";
            string pick = win && rm && !string.IsNullOrEmpty(rm.bossPick) ? $"\n최종 보상: <color=#FFD54A>{rm.bossPick}</color>" : "";
            t.text = (win ? $"{Tables.Chapter(chapter)?.BossName}을(를) 넘었다.{pick}{next}" : $"{chapter}장에서 쓰러졌다. 다시 여관 2층에서 눈을 뜬다.") +
                     $"\n\n시간의 조각 +{shards}  (보유 {Meta.Shards})\n골드는 사라진다.{unlock}";
            UIKit.MakeButton("Village", panel.transform, new Vector2(0.5f, 0), new Vector2(0, 90), new Vector2(380, 90), "마을로", () =>
            {
                resultsOpen = false;
                RunManager.ReturnToVillage();
            }, 30);
        }

        // ---------- 마을: ESC 메뉴 (계속하기 · 메인 로비로) ----------
        // 저장은 시설에서 살 때마다 바로 되므로 로비로 나가도 잃는 것 없음
        bool leaving;
        static readonly Color Haze = new(0.78f, 0.74f, 0.8f);         // 로비 → 마을 때와 같은 라벤더 안개

        public void OpenVillageMenu()
        {
            var go = OpenScreen("VillageMenu");
            UIKit.Dim(go.transform);
            var panel = UIKit.Panel("Panel", go.transform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(640, 520), true);
            UIKit.Banner("잠시 멈춤", panel.transform, new Vector2(0.5f, 1), new Vector2(0, 10), 440);
            UIKit.CloseButton(panel.transform, CloseAll);
            var info = UIKit.Label("Info", panel.transform, new Vector2(0.5f, 1), new Vector2(0, -150), new Vector2(540, 40), 22, TextAnchor.MiddleCenter, false, UIKit.Cream);
            info.text = $"슬롯 {Meta.ActiveSlot}  ·  넘은 장 {Meta.ChaptersCleared}  ·  시간의 조각 {Meta.Shards}";
            UIKit.MakeButton("Resume", panel.transform, new Vector2(0.5f, 0.5f), new Vector2(0, -10), new Vector2(400, 90), "계속하기", CloseAll, 30);
            UIKit.MakeButton("Lobby", panel.transform, new Vector2(0.5f, 0.5f), new Vector2(0, -120), new Vector2(400, 90), "메인 로비로", () => StartCoroutine(ToLobbyCo()), 30);
        }

        // ---------- 판: ESC = 전투 포기 확인 (2026-11-19 사용자) ----------
        // 예 = 판 끝(죽었을 때와 같은 처리), 이번 판에서 얻은 시간의 조각만 챙겨 마을로. 결과 창 없이 바로
        public void OpenGiveUp()
        {
            var go = OpenScreen("GiveUp");                                     // 게임 멈춤
            UIKit.Dim(go.transform);
            var panel = UIKit.Panel("Panel", go.transform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(760, 440), true);
            UIKit.Banner("잠시 멈춤", panel.transform, new Vector2(0.5f, 1), new Vector2(0, 10), 440);
            UIKit.CloseButton(panel.transform, CloseAll);
            var q = UIKit.Label("Question", panel.transform, new Vector2(0.5f, 0.5f), new Vector2(0, 50), new Vector2(700, 50), 30, TextAnchor.MiddleCenter, true, UIKit.Cream);
            q.text = "전투를 포기하고 마을로 이동하시겠습니까?";
            var rm = RunManager.Instance;
            var info = UIKit.Label("Info", panel.transform, new Vector2(0.5f, 0.5f), new Vector2(0, 0), new Vector2(700, 36), 22, TextAnchor.MiddleCenter, false, UIKit.Muted);
            info.text = rm ? $"이번 판에서 얻은 시간의 조각 {rm.ShardGainPreview}개만 가져갑니다" : "";
            UIKit.MakeButton("Yes", panel.transform, new Vector2(0.5f, 0.5f), new Vector2(-150, -100), new Vector2(260, 84), "예", () =>
            {
                if (leaving) return;
                leaving = true;
                CloseAll();
                RunManager.Instance?.GiveUp();
            }, 30);
            UIKit.MakeButton("No", panel.transform, new Vector2(0.5f, 0.5f), new Vector2(150, -100), new Vector2(260, 84), "아니오", CloseAll, 30);
        }

        IEnumerator ToLobbyCo()
        {
            if (leaving) yield break;
            leaving = true;
            CloseAll();
            if (player) player.InputLocked = true;
            PlayerPrefs.Save();
            var op = SceneManager.LoadSceneAsync("Lobby");
            op.allowSceneActivation = false;
            var hz = UIKit.Stretch("Haze", screens).gameObject.AddComponent<Image>();
            hz.raycastTarget = true;
            for (float t = 0; t < 1f; t += Time.unscaledDeltaTime / 0.7f)
            {
                hz.color = new Color(Haze.r, Haze.g, Haze.b, t);
                yield return null;
            }
            hz.color = Haze;
            Time.timeScale = 1f;
            op.allowSceneActivation = true;
        }

        // ---------- 마을: 시간의 문 (한 판 = 1장부터 한 목숨) ----------
        // 챕터를 고르지 않음. 늘 1장에서 출발해 보스를 넘을 때마다 다음 장으로 이어지고, 이번 판의 끝 = 넘은 장 + 1
        public void OpenChapterSelect()
        {
            Sfx.Play("fac_gate", 0.85f, 0f);
            var go = OpenScreen("Chapters");
            UIKit.Dim(go.transform);
            var panel = UIKit.Panel("Panel", go.transform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1300, 900), true);
            UIKit.Banner("시간의 문", panel.transform, new Vector2(0.5f, 1), new Vector2(0, 10), 520);
            UIKit.CloseButton(panel.transform, CloseAll);
            int cleared = Meta.ChaptersCleared, frontier = Mathf.Min(Meta.ChapterCount, cleared + 1);
            var head = UIKit.Label("Head", panel.transform, new Vector2(0.5f, 1), new Vector2(0, -130), new Vector2(1100, 40), 24, TextAnchor.MiddleCenter, true, UIKit.Cream);
            head.text = cleared >= Meta.ChapterCount ? "모든 장을 넘었다 · 1장부터 5장까지 한 목숨으로" : $"1장부터 한 목숨으로 · 이번 판은 <color=#FFD54A>{frontier}장</color>까지";
            for (int i = 0; i < Tables.Chapters.Count; i++)
            {
                var ch = Tables.Chapters[i];
                bool done = ch.Id <= cleared, goal = ch.Id == frontier && !done, locked = ch.Id > frontier;
                var row = UIKit.Panel("Row" + ch.Id, panel.transform, new Vector2(0.5f, 1), new Vector2(0, -190 - i * 112), new Vector2(1100, 100), false, "tooltip", 2.6f);
                if (locked) row.color = new Color(1, 1, 1, 0.45f);
                var nm = UIKit.Label("Name", row.transform, new Vector2(0, 0.5f), new Vector2(40, 12), new Vector2(760, 44), 28, TextAnchor.MiddleLeft, true, new Color(0.3f, 0.18f, 0.1f));
                nm.rectTransform.pivot = new Vector2(0, 0.5f);
                nm.text = $"{ch.Id}장 · {ch.Name}";
                var ds = UIKit.Label("Desc", row.transform, new Vector2(0, 0.5f), new Vector2(40, -24), new Vector2(760, 30), 18, TextAnchor.MiddleLeft, false, new Color(0.35f, 0.24f, 0.15f));
                ds.rectTransform.pivot = new Vector2(0, 0.5f);
                ds.text = locked ? $"{ch.Id - 1}장을 넘으면 다음 판부터 갈 수 있음" : $"{ch.Desc} · 보스: {ch.BossName}";
                var st = UIKit.Label("State", row.transform, new Vector2(1, 0.5f), new Vector2(-40, 0), new Vector2(260, 50), 24, TextAnchor.MiddleRight, true,
                    done ? new Color(0.25f, 0.45f, 0.2f) : goal ? new Color(0.65f, 0.2f, 0.1f) : new Color(0.4f, 0.32f, 0.25f));
                st.rectTransform.pivot = new Vector2(1, 0.5f);
                st.text = done ? "넘음 ✓" : goal ? "이번 목표" : "잠김";
                foreach (var t in new[] { nm, ds, st }) foreach (var sh in t.GetComponents<Shadow>()) Destroy(sh);
            }
            UIKit.MakeButton("Go", panel.transform, new Vector2(0.5f, 0), new Vector2(0, 70), new Vector2(420, 90), "출발 — 1장부터", () =>
            {
                Meta.SelectedChapter = 1;
                Time.timeScale = 1f;
                Sfx.Play("gate_depart", 0.9f, 0f);
                SceneManager.LoadSceneAsync("Run");
            }, 30);
        }

        // ---------- 마을 시설 (2026-11-18 사용자: 시설 레벨 없음 · 새 아이템 해금이 주요 성장) ----------
        // 총포상 · 탄약 공방 = 해금 묶음 구매(세피리아식) · 의상실 = 조건을 채운 코스튬 구매 · 기억의 방 = 능력치
        public void OpenFacility(Meta.Facility f)
        {
            Sfx.Play(f switch { Meta.Facility.MemoryRoom => "fac_memory", Meta.Facility.GunShop => "fac_gun", Meta.Facility.AmmoWorkshop => "fac_ammo", _ => "fac_wardrobe" }, 0.8f, 0f);   // 시설마다 열리는 소리 (2026-11-19)
            var go = OpenScreen("Facility_" + f);
            UIKit.Dim(go.transform);
            var panel = UIKit.Panel("Panel", go.transform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1500, 900), true);
            UIKit.Banner(Meta.FacilityName[(int)f], panel.transform, new Vector2(0.5f, 1), new Vector2(0, 10), 560);
            UIKit.CloseButton(panel.transform, CloseAll);
            var head = UIKit.Label("Head", panel.transform, new Vector2(0.5f, 1), new Vector2(0, -150), new Vector2(1300, 40), 26, TextAnchor.MiddleLeft, true, UIKit.Cream);
            // 내용 영역 = 스크롤 (2026-11-19 사용자: 기억의 방 세 번째 줄이 화면 밖으로 잘림 → 카드 크기는 그대로, 스크롤바로)
            var content = ScrollArea(panel.transform, new Vector2(0, -62), new Vector2(1300, 596), out var scroll);   // 위 글줄(지키는 존재 · 조각)과 안 겹치게
            void Redraw()
            {
                float keep = scroll.verticalNormalizedPosition;
                for (int c = content.childCount - 1; c >= 0; c--) { var old = content.GetChild(c).gameObject; old.SetActive(false); Destroy(old); }
                head.text = $"{Meta.FacilityNpc[(int)f]}  ·  시간의 조각 {Meta.Shards}";
                switch (f)
                {
                    case Meta.Facility.MemoryRoom: MemoryContent(content, Redraw); break;
                    case Meta.Facility.GunShop: GroupContent(content, "Gun", Redraw); break;   // 2026-11-25 사용자: 시작 총 고르기 없앰 — 묶음 해금만
                    case Meta.Facility.AmmoWorkshop: GroupContent(content, "Ammo", Redraw); break;
                    case Meta.Facility.Wardrobe: WardrobeContent(content, Redraw); break;
                }
                FitScroll(content, 596f);
                Canvas.ForceUpdateCanvases();
                scroll.verticalNormalizedPosition = keep;          // 사고 나서 다시 그려도 보던 자리 그대로
            }
            Redraw();
            scroll.verticalNormalizedPosition = 1f;
            if (!Meta.StorySeen(FacTutKey(f))) StartCoroutine(FacilityTutorial(f, content));
        }

        // ---------- 시설 첫 설명 (2026-11-24 사용자: 기억의 방 · 의상실이 같이 열리니 먼저 들어간 곳에서 그 시설 설명) ----------
        static string FacTutKey(Meta.Facility f) => "tut_fac_" + f;
        readonly System.Collections.Generic.List<RectTransform> wardrobeCards = new();

        System.Collections.IEnumerator FacilityTutorial(Meta.Facility f, RectTransform content)
        {
            var ov = StoryOverlay.I;
            if (!ov) yield break;
            yield return new WaitForSecondsRealtime(0.35f);
            switch (f)
            {
                case Meta.Facility.MemoryRoom:
                    yield return ov.HintAt("기억의 방", "시간의 조각으로 능력 강화\n더 강해진 기억으로 재도전", content);
                    break;
                case Meta.Facility.Wardrobe:
                    var row = wardrobeCards.FindAll(c => c).GetRange(0, Mathf.Min(4, wardrobeCards.FindAll(c => c).Count)).ToArray();
                    yield return ov.HintAt("의상실", "코스튬은 캐릭터에게 특별한 능력을 더해줍니다.\n새로운 의상을 통해 당신의 매력을 뽐내세요.", row.Length > 0 ? row : new[] { content });
                    break;
                default: yield break;
            }
            Meta.MarkStory(FacTutKey(f));
        }

        /// <summary>세로 스크롤 영역: 보이는 창(가림) + 내용 + 오른쪽 스크롤바(넘칠 때만). 반환 = 카드를 놓을 내용 (가운데 기준 좌표 그대로 씀)</summary>
        RectTransform ScrollArea(Transform parent, Vector2 pos, Vector2 size, out ScrollRect sr)
        {
            var view = UIKit.MakeRect("View", parent, new Vector2(0.5f, 0.5f), pos, size);
            view.gameObject.AddComponent<RectMask2D>();
            view.gameObject.AddComponent<Image>().color = new Color(0, 0, 0, 0);    // 빈 곳에서도 휠 · 끌기가 먹게
            var content = UIKit.MakeRect("Content", view, new Vector2(0.5f, 1f), Vector2.zero, size);
            sr = view.gameObject.AddComponent<ScrollRect>();
            sr.viewport = view; sr.content = content;
            sr.horizontal = false; sr.vertical = true;
            sr.movementType = ScrollRect.MovementType.Clamped;
            sr.scrollSensitivity = 45f;
            // 스크롤바: 패널 오른쪽 안쪽. 그림 = Codex(prompts/scrollbar.txt → ui_scroll_cut.py): 놋쇠 마개 홈 + 금빛 손잡이 + 가운데 줄무늬
            // 그림은 반 크기로 그림(ppu 배율 2) — 홈 48px → 24, 손잡이 36px → 18
            const float BarW = 24f, CapInset = 14f;
            var trackSpr = UIKit.Spr("scroll_track");
            var handleSpr = UIKit.Spr("scroll_handle");
            var gripSpr = UIKit.Spr("scroll_grip");
            var bar = UIKit.MakeRect("ScrollBar", parent, new Vector2(0.5f, 0.5f), pos + new Vector2(size.x / 2 + 28, 0), new Vector2(BarW, size.y));
            var tImg = bar.gameObject.AddComponent<Image>();
            if (trackSpr) { tImg.sprite = trackSpr; tImg.type = Image.Type.Sliced; tImg.pixelsPerUnitMultiplier = 2f; }
            else tImg.color = new Color(0f, 0f, 0f, 0.35f);
            var area = UIKit.MakeRect("Area", bar, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            area.anchorMin = Vector2.zero; area.anchorMax = Vector2.one;
            float inset = trackSpr ? CapInset : 0f;                                  // 손잡이가 위아래 놋쇠 마개를 덮지 않게
            area.offsetMin = new Vector2(3, inset); area.offsetMax = new Vector2(-3, -inset);
            var handle = UIKit.MakeRect("Handle", area, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            handle.anchorMin = Vector2.zero; handle.anchorMax = Vector2.one; handle.sizeDelta = Vector2.zero;
            var hImg = handle.gameObject.AddComponent<Image>();
            if (handleSpr) { hImg.sprite = handleSpr; hImg.type = Image.Type.Sliced; hImg.pixelsPerUnitMultiplier = 2f; }
            else hImg.color = UIKit.Gold;
            if (gripSpr)
            {
                var grip = UIKit.MakeRect("Grip", handle, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(gripSpr.rect.width / 2, gripSpr.rect.height / 2));
                var gImg = grip.gameObject.AddComponent<Image>();
                gImg.sprite = gripSpr; gImg.raycastTarget = false;
            }
            var sb = bar.gameObject.AddComponent<Scrollbar>();
            sb.handleRect = handle; sb.targetGraphic = hImg; sb.direction = Scrollbar.Direction.BottomToTop;
            sr.verticalScrollbar = sb;
            sr.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHide;
            return content;
        }

        /// <summary>가운데 기준으로 놓인 카드들(보이는 높이 viewH 기준 배치)을 내용 위쪽에 맞추고, 아래로 넘친 만큼 내용을 늘림</summary>
        static void FitScroll(RectTransform content, float viewH)
        {
            float bottom = -viewH / 2;
            foreach (RectTransform ch in content)
                if (ch.gameObject.activeSelf) bottom = Mathf.Min(bottom, ch.anchoredPosition.y - ch.sizeDelta.y * ch.pivot.y);
            float h = Mathf.Max(viewH, viewH / 2 - bottom + 24f);
            // 카드는 내용 가운데에 매달려 있음: 내용이 h로 커지면 가운데가 (h - viewH)/2만큼 내려가므로 그만큼 올려 위쪽 정렬 유지
            float shift = (h - viewH) / 2;
            foreach (RectTransform ch in content)
                if (ch.gameObject.activeSelf && ch.anchorMin.y == 0.5f && ch.anchorMax.y == 0.5f) ch.anchoredPosition += new Vector2(0, shift);
            content.sizeDelta = new Vector2(content.sizeDelta.x, h);
        }

        Image Card(Transform parent, string name, Vector2 pos, Vector2 size) => UIKit.Panel(name, parent, new Vector2(0.5f, 0.5f), pos, size, false, "tooltip", 2f);

        Text CardText(Transform card, string text, Vector2 pos, Vector2 size, int fs, bool title)
        {
            var t = UIKit.Label("T", card, new Vector2(0.5f, 1), pos, size, fs, TextAnchor.UpperCenter, title, new Color(0.3f, 0.18f, 0.1f));
            t.rectTransform.pivot = new Vector2(0.5f, 1);
            t.text = text;
            foreach (var sh in t.GetComponents<Shadow>()) Destroy(sh);
            return t;
        }

        // 기억의 방 갈래 트리 (2026-11-23 사용자 확정): 화력 · 생존 · 손재주 3갈래 × 4층 (아래 1층 → 위 4층 = 기술 노드 ◆)
        //   층 n = 장을 n개 넘으면 열림 · 위층은 이어진 아래 노드를 1단계 이상 찍어야 · 이어진 선은 아래 노드를 찍으면 금빛
        // 2026-11-23 사용자: "노드 슬롯이랑 아이콘 제대로 이미지로 뽑아서 레이아웃 가시성 좋게" →
        //   왼쪽 = 톱니 소켓(잠김 · 열림 · 금빛) 안에 노드 아이콘 + 이름 + 단계 눈금 / 오른쪽 = 고른 노드 설명 · 지금 → 다음 · 값 · 새기기
        //   그림: Resources/UI/Memory/slot_locked · slot · slot_gold · tech_locked · tech · tech_gold · icon_<노드 Id> (원본 Art/ui_src/)
        static readonly string[] MemoryBranches = { "화력", "생존", "손재주" };
        static readonly Color[] BranchColor = { new(1f, 0.55f, 0.4f), new(0.55f, 0.85f, 0.55f), new(0.55f, 0.78f, 1f) };
        static int memSel;
        static Sprite MemSpr(string n) => UIKit.Spr("Memory/" + n);

        void MemoryContent(Transform content, System.Action redraw)
        {
            const float treeX = -170, colGap = 285, pairGap = 74, floorGap = 140, f1y = -225, slot = 92, techSlot = 112;
            var pos = new Dictionary<int, Vector2>();
            if (Tables.MemoryNodes.Find(x => x.Id == memSel) == null) memSel = Tables.MemoryNodes.Count > 0 ? Tables.MemoryNodes[0].Id : 0;
            // 층 표시 (왼쪽): n층 · n장을 넘으면
            for (int f = 1; f <= 4; f++)
            {
                bool open = Meta.FloorOpen(f);
                var fl = UIKit.Label("Floor" + f, content, new Vector2(0.5f, 0.5f), new Vector2(-612, f1y + (f - 1) * floorGap), new Vector2(76, 60), 20, TextAnchor.MiddleCenter, true,
                    open ? UIKit.Cream : new Color(0.6f, 0.55f, 0.5f));
                fl.text = open ? $"{f}층" : $"{f}층\n<size=13>{f}장 클리어</size>";
            }
            for (int bi = 0; bi < MemoryBranches.Length; bi++)
            {
                var br = MemoryBranches[bi];
                float cx = treeX + (bi - 1) * colGap;
                var head = UIKit.Label("Branch" + bi, content, new Vector2(0.5f, 0.5f), new Vector2(cx, f1y + 3 * floorGap + 78), new Vector2(260, 34), 26, TextAnchor.MiddleCenter, true, BranchColor[bi]);
                head.text = br;
                for (int f = 1; f <= 4; f++)
                {
                    var row = Tables.MemoryNodes.FindAll(n => n.Branch == br && n.Floor == f);
                    for (int k = 0; k < row.Count; k++)
                        pos[row[k].Id] = new Vector2(cx + (row.Count == 1 ? 0 : (k == 0 ? -pairGap : pairGap)), f1y + (f - 1) * floorGap);
                }
            }
            // 이음선 (소켓 뒤): 아래 노드를 찍었으면 금빛
            foreach (var n in Tables.MemoryNodes)
            {
                if (n.Floor <= 1 || !pos.ContainsKey(n.Id)) continue;
                foreach (var m in Tables.MemoryNodes)
                {
                    bool parent = n.Req > 0 ? m.Id == n.Req : m.Branch == n.Branch && m.Floor == n.Floor - 1;
                    if (!parent || !pos.ContainsKey(m.Id)) continue;
                    Vector2 a = pos[m.Id] + new Vector2(0, slot / 2 - 6), b = pos[n.Id] - new Vector2(0, (n.IsTech ? techSlot : slot) / 2 - 6);
                    bool lit = Meta.Level(m.Id) >= 1;
                    var line = UIKit.MakeRect("Link", content, new Vector2(0.5f, 0.5f), (a + b) / 2, new Vector2(lit ? 7 : 5, (b - a).magnitude)).gameObject.AddComponent<Image>();
                    line.rectTransform.localRotation = Quaternion.FromToRotation(Vector3.up, b - a);
                    line.color = lit ? new Color(1f, 0.8f, 0.28f, 1f) : new Color(0.35f, 0.3f, 0.3f, 0.75f);
                    line.raycastTarget = false;
                }
            }
            // 소켓 + 아이콘 + 이름 + 단계 눈금
            foreach (var n in Tables.MemoryNodes)
            {
                if (!pos.TryGetValue(n.Id, out var at)) continue;
                var node = n;
                int lv = Meta.Level(n.Id);
                bool avail = Meta.NodeAvailable(n), max = lv >= n.MaxLevel, sel = n.Id == memSel;
                float size = n.IsTech ? techSlot : slot;
                string state = !avail ? "_locked" : max ? "_gold" : "";
                var sock = UIKit.MakeRect("Node" + n.Id, content, new Vector2(0.5f, 0.5f), at, new Vector2(size, size)).gameObject.AddComponent<Image>();
                sock.sprite = MemSpr((n.IsTech ? "tech" : "slot") + state) ?? UIKit.Spr("slot_round"); sock.preserveAspect = true;
                var btn = sock.gameObject.AddComponent<Button>();
                btn.onClick.AddListener(() => { memSel = node.Id; Sfx.Play("ui_click_1", 0.5f); redraw(); });
                var ic = UIKit.MakeRect("Icon", sock.transform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(size * 0.58f, size * 0.58f)).gameObject.AddComponent<Image>();
                ic.sprite = MemSpr("icon_" + n.Id); ic.preserveAspect = true; ic.raycastTarget = false;
                ic.color = !avail ? new Color(0.35f, 0.33f, 0.35f, 0.8f) : lv == 0 ? new Color(0.75f, 0.72f, 0.7f, 0.9f) : Color.white;
                if (!ic.sprite) ic.enabled = false;
                if (sel)   // 고른 노드: 노란 테두리 고리
                {
                    var ring = UIKit.MakeRect("Sel", sock.transform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(size + 18, size + 18)).gameObject.AddComponent<Image>();
                    ring.sprite = UIKit.Spr("slot_round"); ring.color = new Color(1f, 0.85f, 0.3f, 0.9f); ring.raycastTarget = false; ring.transform.SetAsFirstSibling();
                }
                var nm = UIKit.Label("Name", content, new Vector2(0.5f, 0.5f), at + new Vector2(0, -size / 2 - 12), new Vector2(150, 24), 17, TextAnchor.MiddleCenter, true,
                    avail ? UIKit.Cream : new Color(0.6f, 0.56f, 0.52f));
                nm.text = n.Name; nm.raycastTarget = false;
                if (!n.IsTech)   // 단계 눈금 ●●○
                {
                    var pip = UIKit.Label("Pips", content, new Vector2(0.5f, 0.5f), at + new Vector2(0, -size / 2 - 32), new Vector2(150, 18), 14, TextAnchor.MiddleCenter, false, new Color(1f, 0.82f, 0.35f));
                    pip.text = new string('●', lv) + new string('○', n.MaxLevel - lv); pip.raycastTarget = false;
                }
            }
            MemoryDetail(content, redraw);
        }

        /// <summary>오른쪽 설명 판: 고른 노드 그림 · 이름 · 갈래/층 · 설명 · 지금 → 다음 · 조건 · 새기기 버튼 + 아래 합계</summary>
        void MemoryDetail(Transform content, System.Action redraw)
        {
            var n = Tables.MemoryNodes.Find(x => x.Id == memSel);
            var panel = Card(content, "Detail", new Vector2(440, 10), new Vector2(400, 560));
            if (n == null) return;
            var brown = new Color(0.3f, 0.18f, 0.1f);
            int lv = Meta.Level(n.Id), bi = System.Array.IndexOf(MemoryBranches, n.Branch);
            bool floorOpen = Meta.FloorOpen(n.Floor), reqMet = Meta.ReqMet(n), max = lv >= n.MaxLevel;
            var big = UIKit.MakeRect("Big", panel.transform, new Vector2(0.5f, 1), new Vector2(0, -90), new Vector2(124, 124), new Vector2(0.5f, 0.5f)).gameObject.AddComponent<Image>();
            big.sprite = MemSpr((n.IsTech ? "tech" : "slot") + (max ? "_gold" : "")) ?? UIKit.Spr("slot_round"); big.preserveAspect = true;
            var bic = UIKit.MakeRect("Icon", big.transform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(78, 78)).gameObject.AddComponent<Image>();
            bic.sprite = MemSpr("icon_" + n.Id); bic.preserveAspect = true; if (!bic.sprite) bic.enabled = false;
            CardText(panel.transform, (n.IsTech ? "◆ " : "") + n.Name, new Vector2(0, -186), new Vector2(370, 40), 30, true);
            var sub = CardText(panel.transform, $"{n.Branch} · {n.Floor}층" + (n.IsTech ? " · 기술" : ""), new Vector2(0, -226), new Vector2(370, 26), 17, false);
            if (bi >= 0) sub.color = Color.Lerp(BranchColor[bi], brown, 0.55f);
            // 2026-11-23 사용자: "현재 Lv / 다음 Lv / Lv UP, 단계마다 말고 현재 레벨의 수치로 표기"
            if (n.IsTech)
            {
                CardText(panel.transform, n.Desc, new Vector2(0, -262), new Vector2(350, 70), 20, false);
                CardText(panel.transform, max ? "<b>해금 완료</b>" : "<b>미해금</b>", new Vector2(0, -336), new Vector2(370, 30), 20, false);
            }
            else
            {
                string stat = StatLabel(n.Stat);
                CardText(panel.transform, $"<b>현재 Lv {lv}</b> <size=15>/ {n.MaxLevel}</size>", new Vector2(0, -258), new Vector2(370, 30), 21, false);
                CardText(panel.transform, lv > 0 ? $"{stat} <b>{Fmt(n, lv)}</b>" : $"{stat} <b>+0</b>", new Vector2(0, -288), new Vector2(370, 28), 19, false);
                CardText(panel.transform, max ? "<color=#8D6E2F><b>MAX</b></color>" : $"<color=#2E7D32><b>다음 Lv {lv + 1}</b>   {stat} <b>{Fmt(n, lv + 1)}</b></color>", new Vector2(0, -326), new Vector2(370, 28), 19, false);
            }
            string why = !floorOpen ? $"{n.Floor}장 클리어 시 해금" : !reqMet ? $"선행: {ReqName(n)}" : "";
            if (why != "") { var w = CardText(panel.transform, why, new Vector2(0, -372), new Vector2(370, 28), 18, true); w.color = new Color(0.65f, 0.2f, 0.1f); }
            string label = !floorOpen || !reqMet ? "잠김" : max ? (n.IsTech ? "해금 완료" : "MAX") : n.IsTech ? $"해금  ({Meta.CostOf(n)} 조각)" : $"강화  ({Meta.CostOf(n)} 조각)";
            var b = UIKit.MakeButton("Buy", panel.transform, new Vector2(0.5f, 0), new Vector2(0, 62), new Vector2(320, 76), label, () =>
            {
                if (Meta.TryBuy(n)) { Sfx.Play("ui_buy", 0.85f, 0f); if (n.IsTech) Toast($"<color=#FFD54A>◆ {n.Name}</color> — {n.Desc}"); redraw(); }
                else Sfx.Play("ui_deny", 0.7f, 0f);
            }, 24);
            UIKit.SetInteractable(b, floorOpen && reqMet && !max && Meta.Shards >= Meta.CostOf(n));
            // 합계 (설명 판 아래)
            var sum = UIKit.Label("Sum", content, new Vector2(0.5f, 0.5f), new Vector2(440, -290), new Vector2(420, 40), 14, TextAnchor.MiddleCenter, false, UIKit.Cream);
            sum.text = $"공격 +{Meta.Stat("DamageMul") * 100:0}% · 치명 +{Meta.Stat("CritChance") * 100:0}% · 치명피해 +{Meta.Stat("CritDamage") * 100:0}% · 열기 +{Meta.Stat("HeatGain") * 100:0}%\n" +
                       $"체력 +{Meta.Stat("MaxHp"):0} · 방어 +{Meta.Stat("Defense"):0} · 회피 {Meta.Stat("DodgeCooldown") * 100:0}% · 이동 +{Meta.Stat("MoveSpeed") * 100:0}% · 장전 {Meta.Stat("ReloadTime") * 100:0}% · 발동 +{Meta.Stat("ProcChance") * 100:0}% · 조각 +{Meta.Stat("ShardGain") * 100:0}%";
        }

        static string StatLabel(string stat) => stat switch
        {
            "DamageMul" => "공격력", "CritChance" => "치명타 확률", "CritDamage" => "치명타 피해", "HeatGain" => "열기 획득",
            "MaxHp" => "최대 체력", "DodgeCooldown" => "회피 재사용", "Defense" => "방어", "MoveSpeed" => "이동 속도",
            "ShardGain" => "시간의 조각 획득", "ReloadTime" => "장전 시간", "ProcChance" => "효과 발동 확률", _ => stat,
        };

        static string ReqName(MemoryNodeDef n)
        {
            if (n.Req > 0) { var r = Tables.MemoryNodes.Find(x => x.Id == n.Req); return r != null ? r.Name + " Lv 1" : "하위 노드"; }
            return $"{n.Branch} {n.Floor - 1}층 노드 1개";
        }

        /// <summary>노드 값 표기: 단계 lv일 때 합계 (체력 · 방어는 수, 나머지는 %)</summary>
        static string Fmt(MemoryNodeDef n, int lv)
        {
            float v = n.ValuePerLevel * lv;
            return n.Stat is "MaxHp" or "Defense" ? $"{(v >= 0 ? "+" : "")}{v:0}" : $"{(v >= 0 ? "+" : "")}{v * 100:0}%";
        }

        static string ItemName(int id) => Tables.Bullets.TryGetValue(id, out var b) ? b.Name : Tables.Guns.TryGetValue(id, out var g) ? g.Name : id.ToString();

        /// <summary>해금 묶음 카드 (총포상 · 탄약 공방): 묶음 이름 · 안 아이템 · 가격 / 필요한 장</summary>
        void GroupContent(Transform content, string facility, System.Action redraw, float top = 160f, float height = 640f)
        {
            var groups = Tables.UnlockGroups.Where(g => g.Facility == facility).ToList();
            if (groups.Count == 0)                                          // 지금 있는 총탄 · 총은 기본 해금 (2026-11-18), 새 묶음은 앞으로 추가
            {
                var none = UIKit.Label("None", content, new Vector2(0.5f, 0.5f), new Vector2(0, top - 60), new Vector2(900, 60), 22, TextAnchor.MiddleCenter, false, UIKit.Cream);
                none.text = facility == "Gun" ? "지금 있는 총은 모두 손에 익었다 · 새 총은 아직 만드는 중" : "지금 있는 탄은 모두 손에 익었다 · 새 탄은 아직 빚는 중";
                return;
            }
            // 2026-11-25 사용자 "ui 레이아웃도 만들고": 묶음 = 큰 카드 2열 (앞으로 묶음이 늘면 아래로 줄이 늘고 스크롤)
            //   카드 = 묶음 이름 · 한 줄 설명 / 안 아이템 (아이콘 + 이름 · 등급 · 조건 · 특성 · 고유 효과, 총은 스탯 · 우클릭 스킬) / 해금 버튼 (조각) · 해금 완료 도장
            //   아이템 줄에 마우스를 올리면 전체 툴팁 (보상 카드와 같은 ItemTip)
            const float CardW = 630f, CardH = 576f, Gap = 16f;
            for (int i = 0; i < groups.Count; i++)
            {
                var g = groups[i];
                bool own = Meta.GroupOwned(g), avail = Meta.GroupAvailable(g);
                float x = (i % 2 == 0 ? -1 : 1) * (CardW + Gap) / 2, y = 0 - (i / 2) * (CardH + Gap);
                var card = Card(content, "Group" + g.Id, new Vector2(x, y), new Vector2(CardW, CardH));
                if (own) card.color = new Color(1f, 0.96f, 0.82f);
                var ct = card.transform;
                BundleText(ct, g.Name, new Vector2(28, -20), new Vector2(CardW - 56, 40), 30, TextAnchor.UpperLeft, true, Ink);
                BundleText(ct, g.Desc, new Vector2(30, -62), new Vector2(CardW - 60, 26), 17, TextAnchor.UpperLeft, false, InkSoft);
                var rule = UIKit.MakeRect("Rule", ct, new Vector2(0, 1), new Vector2(24, -96), new Vector2(CardW - 48, 2)).gameObject.AddComponent<Image>();
                rule.color = new Color(0.45f, 0.3f, 0.18f, 0.35f); rule.raycastTarget = false;
                // 안 아이템
                var bullets = g.Items.Where(id => Tables.Bullets.ContainsKey(id)).Select(id => Tables.Bullets[id]).ToList();
                var guns = g.Items.Where(id => Tables.Guns.ContainsKey(id)).Select(id => Tables.Guns[id]).ToList();
                float rowTop = -108f;
                if (guns.Count == 1 && bullets.Count == 0) GunBlock(ct, guns[0], rowTop, CardW);
                else
                {
                    int n = bullets.Count + guns.Count;
                    float rowH = Mathf.Min(122f, (CardH - 108f - 96f) / Mathf.Max(1, n));
                    int r = 0;
                    foreach (var b in bullets) BulletRow(ct, b, rowTop - (r++) * rowH, rowH, CardW);
                    foreach (var gn in guns) GunRow(ct, gn, rowTop - (r++) * rowH, rowH, CardW);
                }
                // 해금 버튼 / 도장
                if (own)
                {
                    var stamp = BundleText(ct, "해금 완료 · 이제 판에서 나온다", new Vector2(0, -(CardH - 70)), new Vector2(CardW, 44), 24, TextAnchor.MiddleCenter, true, new Color(0.72f, 0.46f, 0.08f));
                    stamp.rectTransform.anchoredPosition = new Vector2(0, -(CardH - 74));
                }
                else
                {
                    string label = !avail ? $"{g.ReqChapter}장 클리어 시" : $"해금  ·  시간의 조각 {g.Cost}";
                    var bt = UIKit.MakeButton("Buy", ct, new Vector2(0.5f, 0), new Vector2(0, 46), new Vector2(330, 58), label,
                        () => { if (Meta.TryBuyGroup(g)) { Sfx.Play("ui_buy", 0.85f, 0f); Toast($"'{g.Name}' 해금 — 이제 판에서 나온다"); redraw(); } else Sfx.Play("ui_deny", 0.7f, 0f); }, 21);
                    UIKit.SetInteractable(bt, avail && Meta.Shards >= g.Cost);
                    if (avail && Meta.Shards < g.Cost)
                        BundleText(ct, $"조각이 {g.Cost - Meta.Shards}개 모자람", new Vector2(0, -(CardH - 22)), new Vector2(CardW, 20), 15, TextAnchor.MiddleCenter, false, InkSoft)
                            .rectTransform.anchoredPosition = new Vector2(0, -(CardH - 20));
                }
            }
        }

        static readonly Color Ink = new(0.3f, 0.18f, 0.1f), InkSoft = new(0.42f, 0.32f, 0.24f);

        /// <summary>카드 안 글 (왼쪽 위 기준)</summary>
        Text BundleText(Transform card, string text, Vector2 topLeft, Vector2 size, int fs, TextAnchor al, bool title, Color c)
        {
            var t = UIKit.Label("T", card, new Vector2(0, 1), topLeft, size, fs, al, title, c);
            t.rectTransform.pivot = new Vector2(0, 1);
            t.rectTransform.anchoredPosition = topLeft;
            t.text = text;
            foreach (var sh in t.GetComponents<Shadow>()) Destroy(sh);
            return t;
        }

        static string Rgb(Color c) => ColorUtility.ToHtmlStringRGB(c);
        static Color OnPaper(Color c) => Color.Lerp(c, Color.black, 0.28f);

        Image BundleIcon(Transform card, Sprite spr, Vector2 topLeft, float size, string grade)
        {
            var back = UIKit.MakeRect("IconBack", card, new Vector2(0, 1), topLeft, new Vector2(size, size)).gameObject.AddComponent<Image>();
            var gc = UIKit.GradeColor(grade);
            back.color = new Color(gc.r, gc.g, gc.b, 0.16f);
            back.raycastTarget = false;
            var im = UIKit.MakeRect("Icon", back.transform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(size - 8, size - 8)).gameObject.AddComponent<Image>();
            im.sprite = spr; im.preserveAspect = true; im.raycastTarget = false;
            if (!spr) im.color = new Color(gc.r, gc.g, gc.b, 0.5f);
            return im;
        }

        /// <summary>아이템 줄 전체에 마우스 올리면 툴팁</summary>
        void RowTip(Transform card, float top, float h, float w, Item it)
        {
            var hit = UIKit.MakeRect("Row", card, new Vector2(0, 1), new Vector2(16, top), new Vector2(w - 32, h - 4)).gameObject.AddComponent<Image>();
            hit.color = new Color(0, 0, 0, 0);
            CursorTip.Attach(hit.gameObject, () => UIKit.ItemTip(it));
        }

        void BulletRow(Transform card, BulletDef b, float top, float h, float w)
        {
            RowTip(card, top, h, w, Item.Of(b));
            float icon = Mathf.Min(h - 10, 104);
            BundleIcon(card, UIKit.BulletIcon(b), new Vector2(26, top - 4), icon, b.Grade);
            float tx = 26 + icon + 16, tw = w - tx - 26;
            var gc = OnPaper(UIKit.GradeColor(b.Grade));
            BundleText(card, $"<color=#{Rgb(gc)}>{b.Name}</color>  <size=15><color=#{Rgb(InkSoft)}>{UIKit.GradeKo(b.Grade)} · 공격 {b.Damage} · {UIKit.CondKo(b)} · 연산 {b.OpSlots}칸</color></size>",
                new Vector2(tx, top - 4), new Vector2(tw, 30), 22, TextAnchor.UpperLeft, true, Ink);
            var traits = new List<string>();
            foreach (var id in new[] { b.Trait1, b.Trait2 })
                if (id != 0 && Tables.Traits.TryGetValue(id, out var tr)) traits.Add($"<color=#{Rgb(OnPaper(GunkataFx.TraitColor(id, Ink)))}>◆ {tr.Name}</color>");
            BundleText(card, string.Join("   ", traits), new Vector2(tx, top - 36), new Vector2(tw, 22), 16, TextAnchor.UpperLeft, true, Ink);
            var fx = new List<string>();
            foreach (var id in new[] { b.Effect1, b.Effect2 }) if (id != 0 && Tables.Effects.TryGetValue(id, out var e)) fx.Add(e.Desc);
            BundleText(card, string.Join(" · ", fx), new Vector2(tx, top - 60), new Vector2(tw, h - 66), 15, TextAnchor.UpperLeft, false, Ink);
        }

        static string SkillLine(GunDef g) => Tables.Skills.TryGetValue(g.SkillId, out var sk) && (sk.Action == "LanternField" || sk.Action == "SteamDash")
            ? $"<b>우클릭 · {sk.Name}</b> (재사용 {sk.Cooldown:0}초)\n{sk.Desc}" : "우클릭 · 백스텝 샷";

        static string GunStats(GunDef g) => $"공격 ×{g.DamageMul:0.00} · 연사 {1f / Mathf.Max(0.05f, g.FireInterval):0.0}발/초 · 장전 {g.ReloadTime:0.0}초 · 사거리 {g.Range:0}m"
            + (g.CritChance > 0 ? $" · 치명 +{g.CritChance * 100:0}% / 피해 +{g.CritDamage * 100:0}%" : "");

        void GunRow(Transform card, GunDef g, float top, float h, float w)
        {
            RowTip(card, top, h, w, Item.Of(g));
            float icon = Mathf.Min(h - 10, 104);
            BundleIcon(card, UIKit.GunIcon(g), new Vector2(26, top - 4), icon, g.Grade);
            float tx = 26 + icon + 16, tw = w - tx - 26;
            BundleText(card, $"<color=#{Rgb(OnPaper(UIKit.GradeColor(g.Grade)))}>{g.Name}</color>  <size=15><color=#{Rgb(InkSoft)}>{UIKit.GradeKo(g.Grade)} 총</color></size>",
                new Vector2(tx, top - 4), new Vector2(tw, 30), 22, TextAnchor.UpperLeft, true, Ink);
            BundleText(card, GunStats(g) + "\n" + SkillLine(g), new Vector2(tx, top - 36), new Vector2(tw, h - 40), 15, TextAnchor.UpperLeft, false, Ink);
        }

        /// <summary>총 한 자루 묶음: 큰 아이콘 + 이름 · 스탯 · 우클릭 스킬 · 설명</summary>
        void GunBlock(Transform card, GunDef g, float top, float w)
        {
            RowTip(card, top, 360, w, Item.Of(g));
            BundleIcon(card, UIKit.GunIcon(g), new Vector2((w - 230) / 2, top - 6), 230, g.Grade);
            float ty = top - 248;
            var gc = OnPaper(UIKit.GradeColor(g.Grade));
            BundleText(card, $"<color=#{Rgb(gc)}>{UIKit.GradeKo(g.Grade)} 총</color>  <size=15><color=#{Rgb(InkSoft)}>{GunStats(g)}</color></size>",
                new Vector2(28, ty), new Vector2(w - 56, 26), 18, TextAnchor.UpperCenter, true, Ink);
            BundleText(card, SkillLine(g), new Vector2(40, ty - 32), new Vector2(w - 80, 66), 16, TextAnchor.UpperCenter, false, Ink);
            BundleText(card, $"<i>{g.Desc}</i>", new Vector2(40, ty - 102), new Vector2(w - 80, 24), 15, TextAnchor.UpperCenter, false, InkSoft);
        }

        void WardrobeContent(Transform content, System.Action redraw)
        {
            int i = 0;
            wardrobeCards.Clear();
            foreach (var c in Tables.Costumes.Values)
            {
                var cos = c;
                bool un = Meta.CostumeUnlocked(c.Id), met = Meta.CostumeCondMet(c);
                var (now, goal) = Meta.CostumeProgress(c);
                // 카드 = 위 초상화(크롬 ChatGPT 그림 Resources/UI/Costumes/cos_<Id>, 2026-11-24) + 이름 · 효과 · 조건 · 버튼
                var card = Card(content, "Cos" + c.Id, new Vector2(-480 + (i % 4) * 320, 70 - (i / 4) * 455), new Vector2(300, 440));
                wardrobeCards.Add(card.rectTransform);
                var pic = UIKit.Icon("UI/Costumes/cos_" + c.Id);
                if (pic)
                {
                    var im = UIKit.MakeRect("Portrait", card.transform, new Vector2(0.5f, 1), new Vector2(0, -102), new Vector2(270, 180)).gameObject.AddComponent<Image>();
                    im.sprite = pic; im.preserveAspect = true; im.raycastTarget = false;
                    im.color = un ? Color.white : met ? new Color(0.7f, 0.66f, 0.62f) : new Color(0.16f, 0.13f, 0.13f);   // 안 연 옷 = 그림자 · 조건 채움 = 흐리게
                }
                CardText(card.transform, c.Name, new Vector2(0, -196), new Vector2(280, 36), 26, true);
                string eff = string.Join("\n", new[] { c.Effect1, c.Effect2, c.Effect3 }.Where(x => x != 0).Select(x => Tables.Effects.TryGetValue(x, out var e) ? e.Desc : ""));
                CardText(card.transform, eff == "" ? c.Desc : eff, new Vector2(0, -232), new Vector2(270, 100), 16, false);
                if (!un)
                    CardText(card.transform, $"<color={(met ? "#3F7A2E" : "#8A3A22")}>조건: {Meta.CostumeCondText(c)}{(goal > 1 ? $"  ({now}/{goal})" : met ? " ✓" : "")}</color>",
                        new Vector2(0, -334), new Vector2(280, 30), 16, false);
                Button b;
                if (!un) b = UIKit.MakeButton("Unlock", card.transform, new Vector2(0.5f, 0), new Vector2(0, 40), new Vector2(250, 60), met ? $"구매 {c.Cost} 조각" : "조건 미달성",
                    () => { if (Meta.TryUnlockCostume(cos)) { Sfx.Play("ui_buy", 0.85f, 0f); redraw(); } else Sfx.Play("ui_deny", 0.7f, 0f); }, 20);
                else b = UIKit.MakeButton("Wear", card.transform, new Vector2(0.5f, 0), new Vector2(0, 40), new Vector2(250, 60), Meta.SelectedCostume == c.Id ? "입는 중" : "입기", () => { Meta.SelectedCostume = cos.Id; redraw(); }, 20);
                UIKit.SetInteractable(b, un ? Meta.SelectedCostume != c.Id : met && Meta.Shards >= c.Cost);
                i++;
            }
        }
    }
}
