// 하단 HUD (높이 25%): 바탕 + 열기 게이지 3칸 + 실린더 2개(탄피 6발씩) + 시계 바늘 + 벡터 셰이더 이펙트
// 레이어 이미지 · 좌표: Resources/HUD (docs/tools/hud_anim.py 결과)
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace CatTang
{
    [Serializable] public class HudSlot { public int slot; public string side; public float x, y, r; }
    [Serializable] public class HudCell { public int x0, x1, y0, y1; }
    [Serializable]
    public class HudLayout
    {
        public float width, height, clockX, clockY, clockR, cylLX, cylLY, cylRX, cylRY;
        public float cylLPivotX, cylLPivotY, cylRPivotX, cylRPivotY, handPivotX, handPivotY;
        public HudSlot[] slots;
        public HudCell[] cells;
    }

    public class HudView : MonoBehaviour
    {
        public float heightRatio = 0.25f;
        public float lift = 4f;                          // 화면 아래에서 띄우는 높이 (밑에 경험치 바가 들어감, GameUI.HudLift)

        HudLayout L;
        RectTransform root, cylL, cylR, hand;
        readonly RawImage[] caps = new RawImage[13];
        readonly RawImage[] cellFill = new RawImage[3];
        Texture2D texFull;
        Material fxMat;
        float shownHeat, handAngle;
        PlayerController player;
        bool spinning;
        Coroutine handTween;

        public void Build(RectTransform parent, float canvasHeight)
        {
            var json = Resources.Load<TextAsset>("HUD/hud_layout");
            L = JsonUtility.FromJson<HudLayout>(json.text);
            float scale = canvasHeight * heightRatio / L.height;

            root = NewRect("HUD", parent);
            root.anchorMin = root.anchorMax = new Vector2(0.5f, 0);
            root.pivot = new Vector2(0.5f, 0);
            root.sizeDelta = new Vector2(L.width, L.height);
            root.localScale = Vector3.one * scale;
            root.anchoredPosition = new Vector2(0, lift);

            Img("BaseEmpty", root, "HUD/base_gauge_empty_toon", new Vector2(L.width / 2, L.height / 2), new Vector2(0.5f, 0.5f));
            texFull = Resources.Load<Texture2D>("HUD/base_gauge_full_toon");
            for (int i = 0; i < 3; i++)
            {
                var c = L.cells[i];
                var ri = NewRect("HeatCell" + (i + 1), root).gameObject.AddComponent<RawImage>();
                ri.texture = texFull;
                ri.raycastTarget = false;
                var rt = ri.rectTransform;
                rt.pivot = new Vector2(0, 1);
                rt.anchoredPosition = ToUi(c.x0, c.y0);
                cellFill[i] = ri;
                SetCell(i, 0);
            }
            cylL = Cylinder("CylinderLeft", "HUD/cylinder_left_toon", L.cylLX, L.cylLY, L.cylLPivotX, L.cylLPivotY, "left");
            cylR = Cylinder("CylinderRight", "HUD/cylinder_right_toon", L.cylRX, L.cylRY, L.cylRPivotX, L.cylRPivotY, "right");
            var handTex = Resources.Load<Texture2D>("HUD/hand_toon");
            hand = Img("Hand", root, "HUD/hand_toon", new Vector2(L.clockX, L.clockY),
                new Vector2(L.handPivotX / handTex.width, 1 - L.handPivotY / handTex.height)).rectTransform;
            var fxShader = Shader.Find("CatTang/FX");                 // 빌드에서 빠지면 null → 이펙트만 안 나오게 (예전엔 여기서 예외 → GameUI.Awake 중단)
            fxMat = fxShader ? new Material(fxShader) : null;
            SetHand(1, true);
            MiniMapView.Create(parent);                                  // 오른쪽 아래 상시 미니맵 (MiniMapView.cs)
        }

        public void Bind(PlayerController p)
        {
            if (player != null) { player.OnFired -= Fired; player.OnReloadStart -= ReloadStart; player.OnReloadEnd -= ReloadEnd; player.OnSlotPowerUp -= PowerUp; }
            player = p;
            p.OnFired += Fired;
            p.OnSlotPowerUp += PowerUp;
            p.OnReloadStart += ReloadStart;
            p.OnReloadEnd += ReloadEnd;
            ShowLoaded();
            cylL.localRotation = cylR.localRotation = Quaternion.identity;
            SetHand(1, true);
        }

        void ShowLoaded()
        {
            for (int s = 1; s <= 12; s++)
            {
                var b = player != null ? player.rev.slots[s].bullet : null;
                caps[s].enabled = b != null;
                if (b != null) caps[s].color = b.Id == 1000 ? Color.white : Color.Lerp(Color.white, BulletLook.Shown(b, player.rev.Awakened(s)), 0.65f);
            }
        }

        // ---------- 만들기 도우미 ----------
        // 이미지 좌표(왼쪽 위 원점, y 아래로) → root 가운데 기준 UI 좌표
        Vector2 ToUi(float x, float y) => new Vector2(x - L.width / 2, L.height / 2 - y);

        RectTransform NewRect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return (RectTransform)go.transform;
        }

        RawImage Img(string name, Transform parent, string res, Vector2 imgPos, Vector2 pivot)
        {
            var tex = Resources.Load<Texture2D>(res);
            var ri = NewRect(name, parent).gameObject.AddComponent<RawImage>();
            ri.texture = tex;
            ri.raycastTarget = false;
            var rt = ri.rectTransform;
            rt.sizeDelta = new Vector2(tex.width, tex.height);
            rt.pivot = pivot;
            rt.anchoredPosition = parent == root ? ToUi(imgPos.x, imgPos.y) : imgPos;
            return ri;
        }

        RectTransform Cylinder(string name, string res, float cx, float cy, float px, float py, string side)
        {
            var tex = Resources.Load<Texture2D>(res);
            var ri = Img(name, root, res, new Vector2(cx, cy), new Vector2(px / tex.width, 1 - py / tex.height));
            var capTex = Resources.Load<Texture2D>("HUD/cartridge_toon");
            foreach (var s in L.slots)
            {
                if (s.side != side) continue;
                var cap = NewRect("Cartridge" + s.slot, ri.transform).gameObject.AddComponent<RawImage>();
                cap.texture = capTex;
                cap.raycastTarget = false;
                cap.rectTransform.sizeDelta = new Vector2(capTex.width, capTex.height);
                // 실린더 피벗 기준 위치 (이미지 좌표 → UI)
                var rtp = ri.rectTransform;
                cap.rectTransform.anchorMin = cap.rectTransform.anchorMax = rtp.pivot;
                cap.rectTransform.anchoredPosition = new Vector2(s.x - cx, -(s.y - cy));
                caps[s.slot] = cap;
            }
            return ri.rectTransform;
        }

        void SetCell(int i, float f)
        {
            var c = L.cells[i];
            float w = (c.x1 - c.x0 + 1) * Mathf.Clamp01(f), h = c.y1 - c.y0 + 1;
            var ri = cellFill[i];
            ri.enabled = f > 0.001f;
            ri.rectTransform.sizeDelta = new Vector2(w, h);
            ri.uvRect = new Rect(c.x0 / L.width, 1 - (c.y0 + h) / L.height, w / L.width, h / L.height);
        }

        void SetHand(float hour, bool instant)
        {
            float target = -30f * hour;
            if (instant) { handAngle = target; hand.localRotation = Quaternion.Euler(0, 0, handAngle); return; }
            if (handTween != null) StopCoroutine(handTween);
            handTween = StartCoroutine(TweenHand(target));
        }

        IEnumerator TweenHand(float target)
        {
            float from = handAngle, t = 0;
            while (t < 1)
            {
                t += Time.unscaledDeltaTime / 0.08f;
                handAngle = Mathf.Lerp(from, target, 1 - Mathf.Pow(1 - Mathf.Clamp01(t), 3));
                hand.localRotation = Quaternion.Euler(0, 0, handAngle);
                yield return null;
            }
        }

        // ---------- 이펙트 (UI 위 벡터 셰이더) ----------
        void Fx(Vector2 imgPos, float size, int mode, float life, Transform parent = null)
        {
            if (!fxMat) return;
            var ri = NewRect("FX", parent ? parent : root).gameObject.AddComponent<RawImage>();
            ri.material = new Material(fxMat);
            ri.material.SetFloat("_Mode", mode);
            ri.material.SetFloat("_Seed", UnityEngine.Random.Range(0f, 100f));
            ri.raycastTarget = false;
            ri.rectTransform.sizeDelta = Vector2.one * size;
            ri.rectTransform.anchoredPosition = parent ? imgPos : ToUi(imgPos.x, imgPos.y);
            StartCoroutine(RunFx(ri, life));
        }

        IEnumerator RunFx(RawImage ri, float life)
        {
            float t = 0;
            while (t < 1)
            {
                t += Time.unscaledDeltaTime / life;
                ri.material.SetFloat("_Progress", t);
                yield return null;
            }
            Destroy(ri.material);
            Destroy(ri.gameObject);
        }

        HudSlot SlotInfo(int s) { foreach (var x in L.slots) if (x.slot == s) return x; return null; }

        // ---------- 이벤트 ----------
        void Fired(int slot, bool awake)
        {
            if (spinning) return;
            caps[slot].enabled = false;
            var si = SlotInfo(slot);
            Fx(new Vector2(si.x, si.y), si.r * 2 * 3.6f, 0, 0.32f);
            SetHand(Mathf.Clamp(player.rev.reversed ? slot - 1 : slot + 1, 1, 12), false);
        }

        /// <summary>칸 탄의 공격력이 영구로 오름 (레테의 물결, 2026-11-18 사용자): 그 칸이 금빛으로 번쩍 + '공격력 +n' 글자가 떠오름</summary>
        void PowerUp(int slot, int amount)
        {
            var si = SlotInfo(slot);
            if (si == null) return;
            Fx(new Vector2(si.x, si.y), si.r * 2 * 5f, 0, 0.5f);
            StartCoroutine(PowerText(ToUi(si.x, si.y), amount));
        }

        IEnumerator PowerText(Vector2 at, int amount)
        {
            float k = 1f / Mathf.Max(0.01f, root.localScale.x);                 // HUD는 화면 높이에 맞춰 줄여 둬서, 글자는 그만큼 키움
            var t = UIKit.Label("PowerUp", root, new Vector2(0.5f, 0.5f), at, new Vector2(260, 40) * k, Mathf.RoundToInt(24 * k), TextAnchor.MiddleCenter, true, new Color(1f, 0.85f, 0.35f));
            t.text = $"공격력 +{amount}";
            var ol = t.gameObject.AddComponent<Outline>(); ol.effectColor = new Color(0.15f, 0.08f, 0.02f, 0.9f); ol.effectDistance = new Vector2(2, -2) * k;
            var rt = t.rectTransform;
            float life = 0, dur = 1.1f;
            while (life < dur)
            {
                life += Time.unscaledDeltaTime;
                float u = life / dur;
                rt.anchoredPosition = at + new Vector2(0, (40 + 50 * u) * k);
                rt.localScale = Vector3.one * (u < 0.12f ? Mathf.Lerp(0.5f, 1.15f, u / 0.12f) : Mathf.Lerp(1.15f, 1f, Mathf.Clamp01((u - 0.12f) / 0.2f)));
                var c = t.color; c.a = u < 0.7f ? 1 : 1 - (u - 0.7f) / 0.3f; t.color = c;
                yield return null;
            }
            Destroy(t.gameObject);
        }

        void ReloadStart(float dur)
        {
            StartCoroutine(Spin(dur));
        }

        IEnumerator Spin(float dur)
        {
            spinning = true;
            for (int s = 1; s <= 12; s++) caps[s].enabled = false;
            float t = 0;
            int loadedR = 0, loadedL = 0;
            var order = new[] { 1, 11, 9, 7, 5, 3 };                  // 맨 위부터 반시계로 지나가는 순서 대략
            var orderL = new[] { 2, 12, 10, 8, 6, 4 };
            while (t < 1)
            {
                t += Time.deltaTime / dur;
                float e = Mathf.SmoothStep(0, 1, Mathf.Clamp01(t));
                float ang = 720f * e;
                cylL.localRotation = cylR.localRotation = Quaternion.Euler(0, 0, -ang);
                handAngle = -360f * 2 * e - 30f * e;
                hand.localRotation = Quaternion.Euler(0, 0, handAngle);
                int want = Mathf.Clamp(Mathf.FloorToInt(t * 6.5f), 0, 6);
                while (loadedR < want) { Load(order[loadedR], cylR); loadedR++; }
                while (loadedL < want) { Load(orderL[loadedL], cylL); loadedL++; }
                if (!player || !player.rev.reloading) break;            // 즉시 장전 등으로 일찍 끝남
                yield return null;
            }
            ShowLoaded();
            cylL.localRotation = cylR.localRotation = Quaternion.identity;
            SetHand(player ? player.rev.FirstSlot : 1, true);
            spinning = false;
        }

        void Load(int slot, RectTransform cyl)
        {
            caps[slot].enabled = true;
            Fx(caps[slot].rectTransform.anchoredPosition, caps[slot].rectTransform.sizeDelta.x * 2.2f, 1, 0.3f, cyl);
        }

        void ReloadEnd()
        {
            if (!spinning) { ShowLoaded(); SetHand(player ? player.rev.next : 1, false); }
            Fx(new Vector2(L.clockX, L.clockY), L.clockR * 2.9f, 2, 0.4f);
        }

        void Update()
        {
            if (!player || L == null) return;
            if (!spinning && !player.rev.reloading && player.rev.next == 1) ShowLoaded();   // 인벤토리에서 바꾼 것 반영
            shownHeat = Mathf.MoveTowards(shownHeat, player.rev.heat, Time.unscaledDeltaTime * 2.5f);
            for (int i = 0; i < 3; i++) SetCell(i, shownHeat - i);
        }
    }
}
