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
            root.anchoredPosition = new Vector2(0, 4);

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
            fxMat = new Material(Shader.Find("CatTang/FX"));
            SetHand(1, true);
        }

        public void Bind(PlayerController p)
        {
            if (player != null) { player.OnFired -= Fired; player.OnReloadStart -= ReloadStart; player.OnReloadEnd -= ReloadEnd; }
            player = p;
            p.OnFired += Fired;
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
                if (!player || !player.rev.reloading) break;            // 완벽 장전 등으로 일찍 끝남
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

        void ReloadEnd(bool perfect)
        {
            if (!spinning) { ShowLoaded(); SetHand(player ? player.rev.next : 1, false); }
            Fx(new Vector2(L.clockX, L.clockY), L.clockR * (perfect ? 3.4f : 2.9f), 2, perfect ? 0.6f : 0.4f);
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
