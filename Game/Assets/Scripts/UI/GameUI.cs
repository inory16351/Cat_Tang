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
                hud.Build(hudRoot, 1080);
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
            bossName.text = rm && rm.Chapter != null ? rm.Chapter.BossName : "보스";
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
            nextText = UIKit.Label("Next", hudRoot, new Vector2(0.5f, 0), new Vector2(0, 285), new Vector2(1000, 36), 22, TextAnchor.MiddleCenter);
            heatText = UIKit.Label("Heat", hudRoot, new Vector2(0.5f, 0), new Vector2(390, 40), new Vector2(260, 40), 26, TextAnchor.MiddleLeft, true, UIKit.Gold);
            skillText = UIKit.Label("Skill", hudRoot, new Vector2(1, 0), new Vector2(-30, 30), new Vector2(420, 130), 20, TextAnchor.LowerRight);
            var help = UIKit.Label("Help", hudRoot, new Vector2(0, 0), new Vector2(26, 26), new Vector2(560, 150), 18, TextAnchor.LowerLeft, false, new Color(1, 1, 1, 0.55f));
            help.text = village ? "WASD 이동 · Space 대시 · E 들어가기 · Tab 인벤토리" : "WASD 이동 · 마우스 조준 · 좌클릭 사격 · Space 대시\nR 완벽 장전 · 우클릭 총 스킬 · Q 불릿 파티\nE 상호작용 · Tab 인벤토리";
            // 상호작용 안내
            prompt = UIKit.Img("Prompt", hudRoot, "button", new Vector2(0.5f, 0), new Vector2(0, 340), new Vector2(460, 84), true, 2.2f).gameObject;
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
            if (kb != null)
            {
                if (kb.tabKey.wasPressedThisFrame)
                {
                    if (inventory) CloseAll();
                    else if (!current) OpenInventory();
                }
                if (kb.escapeKey.wasPressedThisFrame && current && !resultsOpen) CloseAll();
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
                    dashFill[i].color = full ? DashPipOn : new Color(0.62f, 0.66f, 0.7f, 0.85f);
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
            float total = Mathf.Clamp01((dc + (dc >= player.dashMax ? 0 : dp)) / Mathf.Max(1, player.dashMax));
            dashBar.rectTransform.sizeDelta = new Vector2(198 * total, 6);
            goldText.text = player.inv.gold.ToString();
            var rm = RunManager.Instance;
            shardText.text = rm ? $"{Meta.Shards} <size=22>(+{rm.shardsThisRun})</size>" : Meta.Shards.ToString();
            roomText.text = rm ? (rm.IsBossRoom ? $"{rm.chapter}장 · {rm.Chapter?.BossName}" : $"{rm.chapter}장 · 스테이지 {rm.stageIndex + 1}/{RunManager.Stages} · 방 {rm.roomIndex + 1}/{rm.RoomCount}") + (rm.inCombat ? "" : "  <color=#FFD54A>· 다음 방 · 문으로</color>")
                                : "여관 앞 마을";

            var rev = player.rev;
            if (!village)
            {
                if (rev.reloading) nextText.text = player.ReloadProgress > 0.45f && player.ReloadProgress < 0.65f ? "<color=#FFD54A>지금 R!</color>" : "장전 중…";
                else
                {
                    int s = Mathf.Clamp(rev.next, 1, 12);
                    var b = rev.slots[s].bullet;
                    nextText.text = b == null ? $"다음 {s}시 · 빈 칸" :
                        $"다음 {s}시 · {b.Name} · 판정 {rev.JudgeTime(s)}시 · {(rev.Awakened(s) ? "<color=#FFD54A>깨어남</color>" : "<color=#9AA0A6>잠듦</color>")}";
                }
                heatText.text = rev.heat >= Revolver.HeatMax ? "<color=#FFD54A>Q 불릿 파티!</color>" : $"열기 {rev.heat:0.0}/3";
                skillText.text = $"총 스킬 {(player.SkillCooldownLeft > 0 ? player.SkillCooldownLeft.ToString("0.0") + "초" : "준비")}";
            }
            var fcs = player.Focus;
            bool showPrompt = fcs && !current;
            if (prompt.activeSelf != showPrompt) prompt.SetActive(showPrompt);
            if (showPrompt) promptText.text = $"<color=#FFD54A>E</color>  {fcs.prompt}";
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
            var gold = UIKit.Label("Gold", panel.transform, new Vector2(0.5f, 1), new Vector2(0, -150), new Vector2(600, 40), 30, TextAnchor.MiddleCenter, true, UIKit.Gold);
            void Redraw()
            {
                for (int c = panel.transform.childCount - 1; c >= 0; c--)
                    if (panel.transform.GetChild(c).name.StartsWith("Card")) Destroy(panel.transform.GetChild(c).gameObject);
                gold.text = $"골드 {player.inv.gold}  ·  보관함 {player.inv.storage.Count(x => x != null)}/{Inventory.StorageSize}";
                for (int i = 0; i < m.stock.Count; i++)
                {
                    var it = m.stock[i];
                    int idx = i, price = Shop.PriceOf(it);
                    var card = UIKit.Panel("Card" + i, panel.transform, new Vector2(0.5f, 0.5f), new Vector2(-500 + (i % 3) * 500, 120 - (i / 3) * 300), new Vector2(440, 270), false, "tooltip", 2f);
                    var icon = UIKit.Img("Icon", card.transform, "slot", new Vector2(0, 0.5f), new Vector2(90, 20), new Vector2(130, 130));
                    icon.rectTransform.pivot = new Vector2(0.5f, 0.5f);
                    icon.enabled = false;                                // 네모 틀 없이 아이콘만
                    var inner = UIKit.MakeRect("I", icon.transform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(130, 130)).gameObject.AddComponent<Image>();
                    inner.preserveAspect = true;
                    inner.sprite = it.kind switch { ItemKind.Bullet => UIKit.BulletIcon(it.bullet), ItemKind.Gun => UIKit.GunIcon(it.gun), _ => UIKit.Spr("op_token") };
                    var nm = UIKit.Label("Name", card.transform, new Vector2(0, 1), new Vector2(170, -60), new Vector2(250, 60), 24, TextAnchor.MiddleLeft, true, new Color(0.3f, 0.18f, 0.1f));
                    nm.rectTransform.pivot = new Vector2(0, 0.5f);
                    nm.text = it.Name;
                    var ds = UIKit.Label("Desc", card.transform, new Vector2(0, 1), new Vector2(170, -110), new Vector2(250, 60), 18, TextAnchor.UpperLeft, false, new Color(0.3f, 0.2f, 0.12f));
                    ds.rectTransform.pivot = new Vector2(0, 1);
                    ds.text = it.kind switch
                    {
                        ItemKind.Bullet => $"{UIKit.GradeKo(it.bullet.Grade)} · 공격 {it.bullet.Damage}\n조건 {UIKit.CondKo(it.bullet)}",
                        ItemKind.Gun => $"{UIKit.GradeKo(it.gun.Grade)} · ×{it.gun.DamageMul:0.00}",
                        _ => "탄에 끼워 판정 시각을 바꿈"
                    };
                    foreach (var t in new[] { nm, ds }) foreach (var sh in t.GetComponents<Shadow>()) Destroy(sh);
                    var btn = UIKit.MakeButton("Buy", card.transform, new Vector2(0.5f, 0), new Vector2(60, 40), new Vector2(260, 70), m.sold[i] ? "판매됨" : $"{price} 골드", () =>
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
                }
                var heal = UIKit.MakeButton("CardHeal", panel.transform, new Vector2(0.5f, 0), new Vector2(-260, 70), new Vector2(420, 80), $"체력 회복 40%  ({Merchant.HealCost} 골드)", () =>
                {
                    if (player.inv.gold < Merchant.HealCost) return;
                    player.inv.gold -= Merchant.HealCost;
                    player.health.Heal(player.health.maxHp * 0.4f);
                    Redraw();
                }, 22);
                UIKit.SetInteractable(heal, player.inv.gold >= Merchant.HealCost && player.health.hp < player.health.maxHp);
                var reroll = UIKit.MakeButton("CardReroll", panel.transform, new Vector2(0.5f, 0), new Vector2(260, 70), new Vector2(420, 80), $"새로고침  ({m.RerollCost} 골드)", () =>
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
        public void ShowRewardPick(string title, List<RewardOption> options, System.Action onDone)
        {
            var go = OpenScreen("RewardPick");
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
                var ds = UIKit.Label("Desc", card.transform, new Vector2(0.5f, 1), new Vector2(0, -310), new Vector2(370, 130), 18, TextAnchor.UpperCenter, false, new Color(0.32f, 0.22f, 0.14f));
                ds.rectTransform.pivot = new Vector2(0.5f, 1);
                ds.text = o.desc;
                foreach (var t in new[] { nm, ds }) foreach (var sh in t.GetComponents<Shadow>()) Destroy(sh);
                UIKit.MakeButton("Pick", card.transform, new Vector2(0.5f, 0), new Vector2(0, 56), new Vector2(280, 76), "고르기", () =>
                {
                    CloseAll();
                    o.apply?.Invoke();          // 총이면 여기서 교체 창이 열림
                    onDone?.Invoke();
                }, 26);
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
                nm.text = gun != null ? $"{gun.Name}  <size=18>{UIKit.GradeKo(gun.Grade)} · ×{gun.DamageMul:0.00} · {gun.FireInterval:0.00}초</size>" : "-";
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
                if (Meta.FacilityUnlockChapter[f] > before && Meta.FacilityUnlockChapter[f] <= Meta.ChaptersCleared) unlock += $"\n<color=#FFD54A>마을에 '{Meta.FacilityName[f]}'이(가) 열렸다</color>";
            if (unlock != "") unlock = "\n" + unlock;
            string next = !all ? $"\n다음 판은 {Mathf.Min(Meta.ChapterCount, Meta.ChaptersCleared + 1)}장까지 갈 수 있다." : "";
            t.text = (win ? $"{Tables.Chapter(chapter)?.BossName}을(를) 넘었다.{next}" : $"{chapter}장에서 쓰러졌다. 다시 여관 2층에서 눈을 뜬다.") +
                     $"\n\n시간의 조각 +{shards}  (보유 {Meta.Shards})\n골드는 사라진다.{unlock}";
            UIKit.MakeButton("Village", panel.transform, new Vector2(0.5f, 0), new Vector2(0, 90), new Vector2(380, 90), "마을로", () =>
            {
                resultsOpen = false;
                RunManager.ReturnToVillage();
            }, 30);
        }

        // ---------- 마을: 시간의 문 (한 판 = 1장부터 한 목숨) ----------
        // 챕터를 고르지 않음. 늘 1장에서 출발해 보스를 넘을 때마다 다음 장으로 이어지고, 이번 판의 끝 = 넘은 장 + 1
        public void OpenChapterSelect()
        {
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
                SceneManager.LoadSceneAsync("Run");
            }, 30);
        }

        // ---------- 마을 시설 ----------
        public void OpenFacility(Meta.Facility f)
        {
            var go = OpenScreen("Facility_" + f);
            UIKit.Dim(go.transform);
            var panel = UIKit.Panel("Panel", go.transform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1500, 900), true);
            UIKit.Banner(Meta.FacilityName[(int)f], panel.transform, new Vector2(0.5f, 1), new Vector2(0, 10), 560);
            UIKit.CloseButton(panel.transform, CloseAll);
            var head = UIKit.Label("Head", panel.transform, new Vector2(0.5f, 1), new Vector2(-200, -150), new Vector2(900, 40), 26, TextAnchor.MiddleLeft, true, UIKit.Cream);
            var content = UIKit.MakeRect("Content", panel.transform, new Vector2(0.5f, 0.5f), new Vector2(0, -40), new Vector2(1300, 640));
            void Redraw()
            {
                for (int c = content.childCount - 1; c >= 0; c--) Destroy(content.GetChild(c).gameObject);
                int lv = Meta.FacilityLevel(f);
                head.text = $"시설 {lv}/{Meta.FacilityMaxLevel}단계  ·  시간의 조각 {Meta.Shards}";
                var up = UIKit.MakeButton("Upgrade", content, new Vector2(1, 1), new Vector2(0, 100), new Vector2(380, 76),
                    lv >= Meta.FacilityMaxLevel ? "최대 단계" : $"시설 강화 ({Meta.FacilityUpgradeCost(f)} 조각)", () => { if (Meta.TryUpgradeFacility(f)) { Toast("시설이 넓어졌다"); Redraw(); } }, 22);
                up.GetComponent<RectTransform>().pivot = new Vector2(1, 0.5f);
                UIKit.SetInteractable(up, lv < Meta.FacilityMaxLevel && Meta.Shards >= Meta.FacilityUpgradeCost(f));
                switch (f)
                {
                    case Meta.Facility.MemoryRoom: MemoryContent(content, Redraw); break;
                    case Meta.Facility.GunShop: GunShopContent(content, Redraw); break;
                    case Meta.Facility.AmmoWorkshop: AmmoContent(content); break;
                    case Meta.Facility.Wardrobe: WardrobeContent(content, Redraw); break;
                }
            }
            Redraw();
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

        void MemoryContent(Transform content, System.Action redraw)
        {
            int i = 0;
            foreach (var n in Tables.MemoryNodes)
            {
                var node = n;
                int lv = Meta.Level(n.Id);
                bool avail = Meta.NodeAvailable(n);
                var card = Card(content, "Node" + n.Id, new Vector2(-430 + (i % 3) * 430, 130 - (i / 3) * 300), new Vector2(400, 270));
                CardText(card.transform, n.Name, new Vector2(0, -36), new Vector2(360, 40), 30, true);
                CardText(card.transform, $"{lv} / {n.MaxLevel}\n{n.Desc}", new Vector2(0, -82), new Vector2(340, 90), 20, false);
                string label = !avail ? "시설 단계 부족" : lv >= n.MaxLevel ? "최대" : $"새기기 ({Meta.CostOf(n)} 조각)";
                var b = UIKit.MakeButton("Buy", card.transform, new Vector2(0.5f, 0), new Vector2(0, 50), new Vector2(300, 70), label, () => { if (Meta.TryBuy(node)) redraw(); }, 22);
                UIKit.SetInteractable(b, avail && lv < n.MaxLevel && Meta.Shards >= Meta.CostOf(n));
                i++;
            }
        }

        void GunShopContent(Transform content, System.Action redraw)
        {
            int i = 0;
            foreach (var g in Tables.Guns.Values)
            {
                var gun = g;
                bool un = Meta.GunUnlocked(g.Id);
                var card = Card(content, "Gun" + g.Id, new Vector2(-480 + (i % 4) * 320, 140 - (i / 4) * 320), new Vector2(300, 300));
                var ic = UIKit.Img("Icon", card.transform, "slot", new Vector2(0.5f, 1), new Vector2(0, -80), new Vector2(110, 110));
                ic.rectTransform.pivot = new Vector2(0.5f, 0.5f);
                ic.enabled = false;                                      // 네모 틀 없이 아이콘만
                var inner = UIKit.MakeRect("I", ic.transform, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(110, 110)).gameObject.AddComponent<Image>();
                inner.sprite = UIKit.GunIcon(g); inner.preserveAspect = true; inner.color = un ? Color.white : new Color(0.3f, 0.3f, 0.3f);
                CardText(card.transform, $"{g.Name}\n<size=16>{UIKit.GradeKo(g.Grade)} · ×{g.DamageMul:0.00}</size>", new Vector2(0, -140), new Vector2(280, 60), 22, true);
                if (!un)
                {
                    var b = UIKit.MakeButton("Unlock", card.transform, new Vector2(0.5f, 0), new Vector2(0, 46), new Vector2(250, 64), $"해금 {Meta.GunUnlockCost(g)} 조각", () => { if (Meta.TryUnlockGun(gun)) redraw(); else Toast("조각이 모자라거나 시설 단계가 낮음"); }, 20);
                    UIKit.SetInteractable(b, Meta.Shards >= Meta.GunUnlockCost(g));
                }
                else
                {
                    // 시작 총은 한 자루: 고른 손에 들고 반대 손은 기본 총
                    var r = UIKit.MakeButton("R", card.transform, new Vector2(0.5f, 0), new Vector2(-62, 46), new Vector2(124, 60), Meta.StartGunRight == g.Id ? "오른 ✓" : "오른손", () => { Meta.StartGunRight = gun.Id; Meta.StartGunLeft = 2001; redraw(); }, 18);
                    var l = UIKit.MakeButton("L", card.transform, new Vector2(0.5f, 0), new Vector2(62, 46), new Vector2(124, 60), Meta.StartGunLeft == g.Id ? "왼 ✓" : "왼손", () => { Meta.StartGunLeft = gun.Id; Meta.StartGunRight = 2001; redraw(); }, 18);
                }
                i++;
            }
        }

        void AmmoContent(Transform content)
        {
            int lv = Meta.FacilityLevel(Meta.Facility.AmmoWorkshop);
            string[] lines =
            {
                "기본: 일반 · 희귀 탄이 보상과 상점에 나옴",
                (lv >= 1 ? "✓ " : "") + "1단계: 영웅 탄이 나오기 시작",
                (lv >= 2 ? "✓ " : "") + "2단계: 전설 탄이 나오기 시작",
                (lv >= 3 ? "✓ " : "") + "3단계: 판을 시작할 때 연산 1개를 들고 감"
            };
            var card = Card(content, "Info", new Vector2(0, 40), new Vector2(1000, 420));
            CardText(card.transform, "탄약 공방", new Vector2(0, -40), new Vector2(900, 50), 34, true);
            CardText(card.transform, string.Join("\n\n", lines), new Vector2(0, -110), new Vector2(860, 300), 24, false);
        }

        void WardrobeContent(Transform content, System.Action redraw)
        {
            int i = 0;
            foreach (var c in Tables.Costumes.Values)
            {
                var cos = c;
                bool un = Meta.CostumeUnlocked(c.Id);
                var card = Card(content, "Cos" + c.Id, new Vector2(-480 + (i % 4) * 320, 140 - (i / 4) * 320), new Vector2(300, 300));
                CardText(card.transform, c.Name, new Vector2(0, -34), new Vector2(280, 40), 26, true);
                string eff = string.Join("\n", new[] { c.Effect1, c.Effect2, c.Effect3 }.Where(x => x != 0).Select(x => Tables.Effects.TryGetValue(x, out var e) ? e.Desc : ""));
                CardText(card.transform, eff == "" ? c.Desc : eff, new Vector2(0, -80), new Vector2(270, 120), 17, false);
                Button b;
                if (!un) b = UIKit.MakeButton("Unlock", card.transform, new Vector2(0.5f, 0), new Vector2(0, 46), new Vector2(250, 64), $"해금 {c.Cost} 조각", () => { if (Meta.TryUnlockCostume(cos.Id, cos.Cost)) redraw(); }, 20);
                else b = UIKit.MakeButton("Wear", card.transform, new Vector2(0.5f, 0), new Vector2(0, 46), new Vector2(250, 64), Meta.SelectedCostume == c.Id ? "입는 중" : "입기", () => { Meta.SelectedCostume = cos.Id; redraw(); }, 20);
                UIKit.SetInteractable(b, un ? Meta.SelectedCostume != c.Id : Meta.Shards >= c.Cost);
                i++;
            }
        }
    }
}
