// 2장 클리어 뒤 마을 퀘스트 (2026-11-25 사용자 확정): 탄 → 총 순서
//   2장 클리어 = 꿈 없이 블랙아웃 → 깨어나면 광장 바늘이 다시 멈춤(조각 2개) → 뻐꾸기 "새로운 손님" → 대장장이 섬 다리
//   → 대장장이가 흐르는 모래를 부탁 → 다음 판 첫 중간 보스가 '흐르는 모래' 확정 드랍 (부탁을 들은 뒤에만 · 주우면 기록 → 죽어도 유지, SandPickup)
//   → 대장장이에게 건넴 → 조각 하나가 바늘로 · 탄약 공방 해금 → 대장장이가 보안관에게 보냄 → 보안관 섬 다리
//   → 결투 (연출 위주: 종이 울리기 전에 쏘면 다시 · 늦어도 다시, 패널티 없음) → 마지막 조각 · 바늘이 다시 돎 · 총포상 해금
//   해금 = 묶음을 살 수 있다는 안내만 (첫 묶음 무료 없음)
// 대장장이 · 보안관 대사 2026-11-25 사용자 확정 · 뻐꾸기 깨어남 대화(VillageStory "ch2")는 사용자 대사
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

namespace CatTang
{
    public static class Ch2Quest
    {
        public const string KeyAsk = "q2_ask", KeySand = "q2_sand", KeyAmmo = "q2_ammo", KeyGun = "q2_gun";
        const string Smith = "모래시계 대장장이", Sheriff = "빈 외투 보안관";
        public const string SandName = "흐르는 모래";

        public static bool Active => Meta.ChaptersCleared >= 2 && !Meta.StorySeen(KeyGun);
        /// <summary>광장 바늘이 다시 멈춰 있음 (2장 클리어 ~ 결투)</summary>
        public static bool ClockStopped => Active;
        /// <summary>바늘 둘레에 떠 있는 조각 수 (대장장이 1 · 보안관 1)</summary>
        public static int ShardsLeft => !ClockStopped ? 0 : Meta.StorySeen(KeyAmmo) ? 1 : 2;
        public static bool HasSand => Meta.StorySeen(KeySand) && !Meta.StorySeen(KeyAmmo);
        /// <summary>이번 판 중간 보스가 흐르는 모래를 떨어뜨림</summary>
        public static bool SandWanted => Meta.ChaptersCleared >= 2 && Meta.StorySeen(KeyAsk) && !Meta.StorySeen(KeySand) && !Meta.StorySeen(KeyAmmo);   // 대장장이에게 부탁을 들은 뒤에만 (2026-11-25 사용자)

        /// <summary>이 시설 문을 퀘스트가 가로챔 (대장장이 대화 · 보안관 결투)</summary>
        public static bool Handles(Meta.Facility f) =>
            (f == Meta.Facility.AmmoWorkshop && Meta.ChaptersCleared >= 2 && !Meta.StorySeen(KeyAmmo)) ||
            (f == Meta.Facility.GunShop && Meta.StorySeen(KeyAmmo) && !Meta.StorySeen(KeyGun));

        public static IEnumerator Door(GameUI ui, Meta.Facility f)
        {
            if (f == Meta.Facility.AmmoWorkshop) yield return SmithTalk(ui);
            else yield return Duel(ui);
        }

        // ---------- 판 안: 중간 보스 확정 드랍 ----------
        public static void TryDropSand(Vector3 at)
        {
            if (!SandWanted) return;
            SandPickup.Drop(at);
        }

        // ---------- 대장장이 ----------
        static IEnumerator Say(GameUI ui, string speaker, params string[] lines)
        {
            bool d = false;
            ui.Talk(speaker, lines, () => d = true);
            while (!d) yield return null;
            var p = PlayerController.Instance;
            if (p) p.InputLocked = true;   // 대화 창이 닫히면 조작이 풀림 → 연출이 이어지는 동안 다시 잠금 (끝에서 부른 쪽이 풂)
        }

        static Transform Spot(string name) { var g = GameObject.Find(name); return g ? g.transform : null; }

        static FacilityDoor DoorOf(Meta.Facility f)
        {
            foreach (var d in Object.FindObjectsByType<FacilityDoor>(FindObjectsSortMode.None)) if (d.facility == f) return d;
            return null;
        }

        static IEnumerator SmithTalk(GameUI ui)
        {
            var p = PlayerController.Instance;
            if (p) p.InputLocked = true;
            var spot = Spot("NpcSpot_AmmoWorkshop");
            if (spot) CameraFollow.FocusOn(spot.position + Vector3.back * 1.5f, 0.8f);
            bool first = !Meta.StorySeen(KeyAsk);
            if (!HasSand)
            {
                if (first)
                    yield return Say(ui, Smith,
                        "...모래가 다 새기 전에 왔군.",
                        "고양이#의문::광장 시계가 또 멈췄어.",
                        "알고 있다. 시계 속 모래가 말라 버렸어.",
                        "흐르는 모래가 있어야 바늘이 다시 간다.",
                        "시간의 문 너머, 처음 길을 막아서는 큰 놈이 그걸 품고 있지.",
                        "가져오면 바늘을 채워 주마. 남는 모래로는 탄도 부어 주고.");
                else
                    yield return Say(ui, Smith,
                        "흐르는 모래는?",
                        "시간의 문 너머, 처음 길을 막아서는 큰 놈이다.");
                Meta.MarkStory(KeyAsk);
                CameraFollow.FocusOn(null, 0f);
                ui.Toast($"<color=#FFD54A>다음 판 첫 중간 보스에게서 {SandName}를 얻어 오자</color>");
                if (p) p.InputLocked = false;
                yield break;
            }
            // 모래를 가져옴 (부탁을 들은 뒤에만 떨어짐): 건네기 → 조각 하나가 바늘로 → 탄약 공방 → 보안관에게 보냄
            yield return Say(ui, Smith, "가져왔군.");
            if (Sfx.Has("ui_buy")) Sfx.Play("ui_buy", 0.7f, 0f);
            if (spot) FxBurst.Spawn(spot.position + Vector3.up * 1.6f, 1.8f, FxBurst.Kind.Gold, new Color(1f, 0.82f, 0.45f));
            yield return Say(ui, Smith,
                "...그래, 이 모래다.",
                "아직 따뜻하군. 누군가 오래 쥐고 있던 모래야.");
            CameraFollow.FocusOn(null, 0f);
            if (ClockQuest.I) yield return ClockQuest.I.RepairStep();
            Meta.MarkStory(KeyAmmo);
            ClockQuest.I?.Refresh();
            VillageManager.RefreshFacilityLabels();
            yield return Reveal(ui, Meta.Facility.AmmoWorkshop);
            if (spot) CameraFollow.FocusOn(spot.position + Vector3.back * 1.5f, 0.8f);
            yield return Say(ui, Smith,
                "바늘 한쪽은 채웠다. 나머지는 내 손으론 안 돼.",
                "서쪽 섬의 보안관을 찾아가.",
                "그 친구는 말보다 총으로 이야기하지.",
                "공방도 다시 열었다. 새 탄을 부으려면 조각을 가져와.");
            CameraFollow.FocusOn(null, 0f);
            foreach (var br in Object.FindObjectsByType<IslandBridge>(FindObjectsSortMode.None))
                if (br.reqStory == KeyAmmo && br.Pending) yield return br.Reconnect();
            CameraFollow.FocusOn(null, 0f);
            var gun = DoorOf(Meta.Facility.GunShop);
            if (p) p.InputLocked = false;
            if (gun) GuideArrow.Show(gun.transform, "보안관", keep: true);
        }

        /// <summary>열린 시설 문을 비추고 해금 알림 + 묶음 구매 안내</summary>
        static IEnumerator Reveal(GameUI ui, Meta.Facility f)
        {
            var door = DoorOf(f);
            if (door) CameraFollow.FocusOn(door.transform.position + Vector3.back * 1.5f, 0.7f);
            yield return new WaitForSecondsRealtime(0.8f);
            if (Sfx.Has("ui_buy")) Sfx.Play("ui_buy", 0.8f, 0f);
            string what = f == Meta.Facility.GunShop ? "새 총" : "새 총탄";
            ui.Toast($"<color=#FFD54A>{Meta.FacilityName[(int)f]} 해금</color> — 시간의 조각으로 {what} 묶음을 살 수 있다");
            yield return new WaitForSecondsRealtime(1.4f);
            CameraFollow.FocusOn(null, 0f);
        }

        // ---------- 보안관 결투 ----------
        const float DuelGap = 7f;          // 마주 선 거리
        static bool Clicked() => Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame;
        /// <summary>개발용: 결투 자동 진행 — 첫 판은 일찍 쏘고, 다음 판은 종 0.3초 뒤에 쏨</summary>
        public static bool DevAutoDuel;

        static IEnumerator Duel(GameUI ui)
        {
            var p = PlayerController.Instance;
            var ov = StoryOverlay.I;
            var spot = Spot("NpcSpot_GunShop");
            if (!p || !spot) yield break;
            p.InputLocked = true;
            CameraFollow.FocusOn(spot.position + Vector3.back * 1.5f, 0.8f);
            yield return Say(ui, Sheriff,
                "대장장이가 보냈나.",
                "내 얼굴은 필름과 함께 바랬어. 하지만 총은 아직 기억하지.",
                "바늘을 움직이고 싶다면, 한 번 겨뤄 보세.",
                "종이 울리면 뽑게. 먼저 뽑으면 처음부터야.");

            // 자리 잡기: 보안관 정면(마을 가운데 쪽)으로 DuelGap만큼 떨어져 마주 섬
            var sheriff = spot.childCount > 0 ? spot.GetChild(0) : spot;
            var sheriffRot = sheriff.rotation;
            Vector3 back0 = p.transform.position;
            Vector3 dir = DuelDir(spot.position);
            Vector3 stand = spot.position + dir * DuelGap;
            if (Physics.Raycast(stand + Vector3.up * 6f, Vector3.down, out var hit, 20f, ~0, QueryTriggerInteraction.Ignore)) stand.y = hit.point.y;
            if (ov) yield return ov.FadeBlack(0, 1, 0.35f);
            p.Teleport(stand);
            if (p.model) p.model.rotation = Quaternion.LookRotation(-dir);
            sheriff.rotation = Quaternion.LookRotation(dir);
            CameraFollow.FocusOn((spot.position + stand) * 0.5f, 0.85f);
            if (ov) yield return ov.FadeBlack(1, 0, 0.45f);
            p.poser?.SetAimWeight(0f);
            Music.Duck(0.15f, 0.6f);

            Vector3 chestS = sheriff.position + Vector3.up * 1.6f;
            int tries = 0;
            while (true)
            {
                tries++;
                while (Mouse.current != null && Mouse.current.leftButton.isPressed) yield return null;   // 대사 넘긴 클릭이 섞이지 않게
                if (ov) ov.SetCaption(tries == 1 ? "종이 울리면  <color=#FFD54A>좌클릭</color>" : "...");
                yield return new WaitForSecondsRealtime(0.6f);
                if (ov) ov.SetCaption("...");
                // 째깍 소리 사이로 종을 기다림
                float wait = Random.Range(2.0f, 4.4f), t = 0, nextTick = 0.5f;
                bool early = false;
                while (t < wait)
                {
                    if (Clicked() || (DevAutoDuel && tries == 1 && t > 1.2f)) { early = true; break; }
                    t += Time.unscaledDeltaTime;
                    if (t >= nextTick) { if (Sfx.Has("e4_tick_1")) Sfx.Play("e4_tick_1", 0.55f, 0f); nextTick += 0.8f; }
                    yield return null;
                }
                if (early)
                {
                    PlayerShot(p, p.transform.position + Vector3.up * 1.3f + (-dir + Vector3.up * 0.9f).normalized * 6f, false);
                    if (ov) ov.SetCaption("");
                    yield return Say(ui, Sheriff, "...성급하군. 다시.");
                    continue;
                }
                // 종: 섬광 → 지금!
                if (Sfx.Has("e4_bell")) Sfx.Play("e4_bell", 1f, 0f);
                if (ov) { ov.SetCaption("<color=#FFD54A>지금!</color>"); ov.SetWhite(0.35f); }
                float r = 0; bool shot = false;
                while (r < 1.3f)
                {
                    if (ov && r > 0.08f) ov.SetWhite(0);
                    if (Clicked() || (DevAutoDuel && r > 0.3f)) { shot = true; break; }
                    r += Time.unscaledDeltaTime;
                    yield return null;
                }
                if (ov) { ov.SetWhite(0); ov.SetCaption(""); }
                if (!shot)
                {
                    // 보안관이 먼저 뽑음 (피해 없음)
                    Tracer.Spawn(chestS + dir * 0.6f, p.transform.position + Vector3.up * 1.2f, false);
                    Sfx.Play("shot", 0.85f, 0f);
                    FxBurst.Spawn(p.transform.position + Vector3.up * 1.2f, 1.1f, FxBurst.Kind.Spark, new Color(1f, 0.9f, 0.7f));
                    CameraFollow.Shake(0.25f);
                    yield return new WaitForSecondsRealtime(0.5f);
                    yield return Say(ui, Sheriff, "느려. 한 번 더.");
                    continue;
                }
                // 이김: 슬로 → 모자가 튀고 외투가 휘청
                PlayerShot(p, chestS, true);
                FxBurst.Spawn(chestS, 1.8f, FxBurst.Kind.Crit, new Color(1f, 0.85f, 0.4f));
                CameraFollow.Shake(0.45f);
                Time.timeScale = 0.25f;
                var lean = sheriff.rotation;
                for (float k = 0; k < 1f; k += Time.unscaledDeltaTime / 0.5f)
                {
                    sheriff.rotation = lean * Quaternion.Euler(-18f * Mathf.Sin(k * Mathf.PI * 0.5f), 0, 0);
                    yield return null;
                }
                Time.timeScale = 1f;
                yield return new WaitForSecondsRealtime(0.6f);
                for (float k = 0; k < 1f; k += Time.unscaledDeltaTime / 0.6f) { sheriff.rotation = Quaternion.Slerp(sheriff.rotation, lean, k); yield return null; }
                sheriff.rotation = lean;
                break;
            }
            Music.Duck(0f, 0.6f);
            p.poser?.SetAimWeight(0f);
            yield return Say(ui, Sheriff,
                "......좋은 손이야.",
                "떠나는 기차를 붙잡으려던 그 손이군.",
                "고양이#의문::......어떻게 그걸.",
                "총포상을 다시 열지. 바늘도 이제 돌 거야.");

            // 원래 자리로 · 마지막 조각 → 바늘이 다시 돎 → 총포상 해금
            if (ov) yield return ov.FadeBlack(0, 1, 0.35f);
            sheriff.rotation = sheriffRot;
            var gunDoor = DoorOf(Meta.Facility.GunShop);
            p.Teleport(gunDoor ? gunDoor.transform.position + (dir * 2.5f) : back0);
            CameraFollow.FocusOn(null, 0f);
            if (ov) yield return ov.FadeBlack(1, 0, 0.45f);
            if (ClockQuest.I) yield return ClockQuest.I.RepairStep();
            Meta.MarkStory(KeyGun);
            if (ClockQuest.I) yield return ClockQuest.I.SpinAgain();
            ClockQuest.I?.Refresh();
            VillageManager.RefreshFacilityLabels();
            yield return Reveal(ui, Meta.Facility.GunShop);
            p.InputLocked = false;
        }

        /// <summary>보안관에게서 DuelGap 떨어진 빈 땅 방향: 섬 바닥(GroundCollider) 위 · 사이에 막힌 것 없음. 북동(30°)부터 훑음 (서쪽은 총포상 건물, 동쪽은 섬 끝 · 다리)</summary>
        static Vector3 DuelDir(Vector3 from)
        {
            foreach (int a in new[] { 30, 0, 210, 60, 330, 90, 180, 270, 120, 150, 240, 300 })
            {
                var d = Quaternion.Euler(0, a, 0) * Vector3.forward;
                var p = from + d * DuelGap;
                bool ground = Physics.Raycast(p + Vector3.up * 6f, Vector3.down, out var h, 20f, ~0, QueryTriggerInteraction.Ignore) && h.collider.name == "GroundCollider";
                bool clear = true;
                foreach (var rh in Physics.RaycastAll(from + Vector3.up * 0.8f, d, DuelGap, ~0, QueryTriggerInteraction.Ignore))
                    if (!rh.collider.GetComponentInParent<PlayerController>()) { clear = false; break; }   // 고양이 자신은 무시
                if (ground && clear) return d;
            }
            return Quaternion.Euler(0, 30, 0) * Vector3.forward;
        }

        /// <summary>결투 한 발: 오른손 총구 섬광 + 궤적 + 반동</summary>
        static void PlayerShot(PlayerController p, Vector3 to, bool awakened)
        {
            p.poser?.SetAimWeight(1f);   // 뽑아 듦
            var muz = p.rightMuzzle ? p.rightMuzzle.position : p.transform.position + Vector3.up * 1.2f;
            Tracer.Spawn(muz, to, awakened);
            Sfx.Play("shot", 1f, 0f);
            var d = to - muz; d.y = 0;
            if (d.sqrMagnitude > 0.01f) GunkataFx.Burst(muz, d.normalized, 0.8f, false);
            p.poser?.Recoil(true, 1.4f);
        }

        /// <summary>개발용: 퀘스트 기록 지우기 (2장 클리어 직후 상태로)</summary>
        public static void DevReset()
        {
            foreach (var k in new[] { KeyAsk, KeySand, KeyAmmo, KeyGun, "bridge.Bridge_Gun" }) PlayerPrefs.DeleteKey(Meta.StoryPrefKey(k));
            PlayerPrefs.Save();
        }
    }
}
