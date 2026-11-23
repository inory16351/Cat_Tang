// 보스 등장 연출 (2026-11-21 사용자): 메인 · 중간 보스 10명 전부.
// - 보스마다 다른 카메라 워킹 + 그 컷에 맞춘 보스 동작 · 이펙트(비트) → 마지막엔 시간을 멈추고 일러스트 + 영어 타이포 카드
// - 카드는 보스 테마별로 10종 (현상수배 · 녹아내림 · 발차판 · 기차 띠 · 극장 커튼 · 거울 금 · 느와르 사선 · 흑백 반쪽 · 글리치 · 시계판) — BossIntroCards.cs
// - 길이: 메인 6초 · 중간 4초 / 첫 만남은 스킵 불가, 이후엔 스페이스로 스킵 (슬롯별 기록)
// - 이름: 연출엔 영어 타이포, 체력바엔 한글 (BossBrain.DisplayName)
// 일러스트: Resources/UI/BossIntro/<id>.png (투명 배경, ChatGPT). 없으면 타이포만.
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace CatTang
{
    public enum IntroShot
    {
        Wide, LowRise, Orbit, Crash, Dutch, Top,
        Feet, Showdown, Chase, JumpCut, Stutter, ClockSweep, OverShoulder, Rise, Snap, Pullback, Spiral, LookUp, Push,
        Vertigo, HeroLow, HeroOrbit, QuickDraw, Portrait,
        BehindBoss, Profile, WormEye, HighOrbit,
        Duel,
        ShotDown,
    }
    public enum IntroCut { Hard, Flash, Whip, Wipe }
    public enum IntroCard { Wanted, Melt, Board, Rail, Curtain, Shatter, NoirSlash, TwinSplit, Glitch, Dial }

    public struct IntroBeat { public int shot; public float u; public System.Action<BossIntro> fx; }

    public class BossIntroDef
    {
        public string id, titleEn, nameEn, titleKo, nameKo;
        public bool main;
        public Color accent, accent2;
        public (IntroShot shot, float dur, IntroCut cut)[] shots;
        public IntroBeat[] beats;
        public IntroCard card;
        public bool glitch, noir;
        public bool faceLeft;              // 마지막 컷에서 보스를 화면 왼쪽 1/3에 둠 (정지 카드 일러스트가 얼굴을 안 가리게, 2026-11-22 사용자)
        public bool bust;                  // 거대한 보스: 일러스트를 상반신만 크게 (2026-11-21 사용자)
        public bool emerge;                // 처음엔 바닥 아래에 숨었다가 비트(Emerge)로 솟아오름 (고래)
        // 2026-11-22 사용자: "등장하는 장면에서부터 시작해서 보스의 등장이라는 걸 명확하게" (모든 보스) → 시작 위치를 비켜 두고 비트 Enter로 들어옴
        public float entryUp;              // 위로 (+ = 하늘에서 떨어짐 · 내려옴), 여러 마리면 뒤 녀석일수록 3m씩 더 높이
        public float entryBack;            // 고양이 반대쪽으로 (멀리서 달려 들어옴)
        public string KoLine => string.IsNullOrEmpty(titleKo) ? nameKo : titleKo + " · " + nameKo;
        public int Chapter => id[2] - '0';
    }

    public partial class BossIntro : MonoBehaviour
    {
        // ---------- 보스 표 (이름 · 칭호 2026-11-21 사용자 확정) ----------
        static Color C(string hex) { ColorUtility.TryParseHtmlString("#" + hex, out var c); return c; }
        static IntroBeat At(int shot, float u, System.Action<BossIntro> fx) => new IntroBeat { shot = shot, u = u, fx = fx };

        public static BossIntroDef Def(int chapter, bool mid) => (chapter, mid) switch
        {
            // 1장 중간: 서부 대치 — 하늘에서 떨어짐 → 옆모습 장전 → 정면에서 카메라를 쏨, 맞은 카메라가 날아가 바닥에 떨어진 채 포스터
            (1, true) => new BossIntroDef
            {
                id = "ch1_mid", entryUp = 9f, titleEn = "OUTLAW OF THE WASTELAND", nameEn = "WINDER JACK", titleKo = "황야의 무법자", nameKo = "와인더 잭",
                accent = C("E39A3B"), accent2 = C("5A2A14"), card = IntroCard.Wanted,
                shots = new[] { (IntroShot.Feet, 0.8f, IntroCut.Hard), (IntroShot.Profile, 0.7f, IntroCut.Hard), (IntroShot.ShotDown, 1.9f, IntroCut.Hard) },
                beats = new[]
                {
                    At(0, 0.0f, x => Telegraph.Ring(x.Feet, 1.6f, 0.5f, Telegraph.Danger)),                         // 떨어질 자리 표시
                    At(0, 0.05f, x => { x.Snd("e_dive", 0.6f); x.Enter(0.42f, "drop");
                                        x.Later(0.42f, () => { x.Snd("e_slam_1"); x.Shake(0.45f); GunkataFx.Swirl(x.Feet, 1.8f, 1.2f, 0.6f); GunkataFx.Smoke(x.Feet, 2.4f, 1.4f);
                                                               x.Ring(6, 1.2f, q => GunkataFx.Smoke(q, 1.2f, 1.1f)); StylizedFx.Shockwave(x.Feet, 3f, StylizedFx.Gold, 0.4f); }); }),
                    At(1, 0.0f, x => x.Act("Intro")),
                    At(1, 0.35f, x => x.Snd("reload", 0.8f)),
                    // 정면 총격 → 카메라가 맞고 날아가 바닥에 떨어짐 (ShotDown)
                    At(2, 0.0f, x => { x.Act("Snipe"); x.Snd("reload", 0.6f); }),
                    At(2, BossIntro.ShotHit - 0.02f, x => { x.Snd("e_bow"); x.Snd("shot_1"); GunkataFx.MuzzleFlash(x.Chest + x.Fw * 1.2f, x.Fw, 2.6f);
                                                            GunkataFx.Streak(x.Chest + x.Fw * 1.4f, x.Fw, 3f, 0.12f, new Color(1f, 0.85f, 0.5f), 60f); }),
                    At(2, BossIntro.ShotHit, x => { x.Snd("shot_2", 0.7f); x.Snd("e_glass", 0.7f); x.Snd("dash", 0.7f); x.Flash(Color.white, 1f); x.Shake(0.6f); }),
                    At(2, BossIntro.ShotLand, x => { x.Snd("e_slam_2", 0.8f); x.Shake(0.5f); GunkataFx.Smoke(x.CamPos - x.Fw * 0.7f + x.Side * 1.1f, 0.7f, 0.7f); }),   // 먼지는 옆으로 비켜서 (보스를 가리지 않게)
                },
            },
            // 1장 보스: 녹아내리는 거인 — 틸트업 → 궤도 → 얼굴에서 확 빠지며 내려찍기
            (1, false) => new BossIntroDef
            {
                id = "ch1_boss", bust = true, emerge = true, titleEn = "THE BURNING CLOCKTOWER", nameEn = "DALIOS", titleKo = "불타는 시계탑", nameKo = "달리오스", main = true,
                accent = C("F0702A"), accent2 = C("4A1608"), card = IntroCard.Melt,
                shots = new[] { (IntroShot.LowRise, 1.4f, IntroCut.Hard), (IntroShot.BehindBoss, 1.2f, IntroCut.Flash), (IntroShot.Pullback, 1.6f, IntroCut.Hard) },
                beats = new[]
                {
                    At(0, 0.0f, x => x.Act("Intro")),
                    At(0, 0.02f, x => { x.Snd("e_steam", 0.6f); x.Shake(0.15f); ExtraFx.GroundWave(x.Feet, x.Radius + 2f, new Color(1f, 0.55f, 0.15f)); }),
                    At(0, 0.15f, x => { x.Enter(0.95f, "rise"); x.Snd("explosion", 0.8f); x.Snd("e_slam_1"); x.Shake(0.5f);
                                        StylizedFx.GroundBlast(x.Feet, 4.5f, StylizedFx.BlastRed); x.Ring(6, x.Radius + 0.8f, p => GunkataFx.Smoke(p, 1.4f, 1.6f)); }),
                    At(0, 0.6f, x => x.Ring(8, x.Radius * 0.7f, p => GunkataFx.Spark(p + Vector3.up * x.Size * 0.6f, 1.2f))),
                    At(1, 0.35f, x => { x.Snd("e_spin"); StylizedFx.Slash(x.Chest, x.Side, x.Size * 0.7f, StylizedFx.Gold, 1.2f); }),
                    At(2, 0.12f, x => { x.Snd("e_slam_1"); x.Snd("explosion_1", 0.7f); x.Shake(0.6f); x.Slow(0.4f);
                                        StylizedFx.GroundBlast(x.Front(0.5f), 4f, StylizedFx.BlastRed); StylizedFx.Shockwave(x.Feet, x.Size * 1.2f, StylizedFx.Gold, 0.6f); }),
                },
            },
            // 2장 중간: 호루라기 · 신호등 — 기울인 슬라이드(빨강/초록 섬광) → 크래시
            (2, true) => new BossIntroDef
            {
                id = "ch2_mid", entryBack = 9f, titleEn = "LORD OF THE PLATFORM", nameEn = "CONDUCTOR VAN", titleKo = "승강장의 지배자", nameKo = "컨덕터 반",
                accent = C("D83A2E"), accent2 = C("3A0E0A"), card = IntroCard.Board,
                shots = new[] { (IntroShot.HighOrbit, 1.0f, IntroCut.Hard), (IntroShot.QuickDraw, 1.4f, IntroCut.Wipe) },
                beats = new[]
                {
                    At(0, 0.0f, x => x.Act("Intro")),
                    At(0, 0.0f, x => { x.Enter(0.75f, "charge"); x.Snd("e_steam", 0.7f); }),
                    At(0, 0.25f, x => x.Snd("e_whistle")),
                    At(0, 0.35f, x => x.Flash(new Color(1f, 0.15f, 0.1f), 0.45f)),
                    At(0, 0.7f, x => x.Flash(new Color(0.2f, 1f, 0.4f), 0.4f)),
                    At(1, 0.2f, x => { x.Snd("e_slam_2"); x.Shake(0.35f); x.Slow(0.45f); GunkataFx.Flare(x.Head + x.Fw * 0.6f, 2.4f, new Color(1f, 0.2f, 0.15f), true); }),
                },
            },
            // 2장 보스: 기관차 — 와이드 → 돌진하는 보스 앞에서 뒷걸음 → 크래시 + 석탄 폭발
            (2, false) => new BossIntroDef
            {
                id = "ch2_boss", bust = true, entryBack = 14f, titleEn = "THE DERAILED TERMINUS", nameEn = "LOCOMOS", titleKo = "탈선한 종착역", nameKo = "로코모스", main = true,
                accent = C("C8733A"), accent2 = C("2E1A10"), card = IntroCard.Rail,
                shots = new[] { (IntroShot.Wide, 1.0f, IntroCut.Hard), (IntroShot.Chase, 1.6f, IntroCut.Whip), (IntroShot.Crash, 1.6f, IntroCut.Flash) },
                beats = new[]
                {
                    At(0, 0.0f, x => x.Act("Intro")),
                    At(0, 0.0f, x => { x.Enter(0.9f, "charge"); x.Snd("e5_giant_dash"); x.Snd("e_steam", 0.7f); x.Shake(0.2f); }),
                    At(0, 0.15f, x => { x.Snd("e_steam"); GunkataFx.Smoke(x.Head, 2.6f, 1.6f); GunkataFx.Smoke(x.Head + x.Side, 2f, 1.4f); }),
                    At(1, 0.0f, x => { x.Snd("e5_giant_dash"); x.Lunge(3.2f, 1.2f, true); x.Shake(0.15f); }),
                    At(2, 0.18f, x => { x.Snd("explosion_2"); x.Snd("e_slam_1", 0.6f); x.Shake(0.55f); x.Slow(0.45f); GunkataFx.Explosion(x.Front(1.2f), 2.6f, false); }),
                },
            },
            // 3장 중간: 마술사 (2026-11-22 Hyper3D 일루시오 기준) — 빈 무대에 연기가 터지며 인사하는 채로 나타남 →
            // 낮게 돌면 양옆에 거울 분신 둘이 깜빡이며 따라 하다 깨짐 → 나선으로 파고들며 카드를 카메라에 내던짐
            (3, true) => new BossIntroDef
            {
                id = "ch3_mid", titleEn = "DIRECTOR OF REFLECTIONS", nameEn = "ILLUSIO", titleKo = "반사의 연출가", nameKo = "일루시오",
                accent = C("7FD6F5"), accent2 = C("12324A"), card = IntroCard.Curtain,
                shots = new[] { (IntroShot.LowRise, 1.0f, IntroCut.Hard), (IntroShot.HeroOrbit, 0.7f, IntroCut.Flash), (IntroShot.Portrait, 0.7f, IntroCut.Hard) },   // 끝 = 얼굴 왼쪽, 카드 일러는 오른쪽
                beats = new[]
                {
                    At(0, 0.0f, x => { x.Hide(true); x.Act("Intro"); x.Snd("e_conjure", 0.6f); }),
                    At(0, 0.25f, x => { x.Hide(false); x.Flash(Color.white, 0.6f); x.Snd("e_conjure"); x.Snd("explosion", 0.4f); x.Shake(0.2f);
                                        GunkataFx.Smoke(x.Feet + Vector3.up * 0.5f, 3.2f, 1.4f); x.Ring(8, x.Radius + 0.9f, p => GunkataFx.Smoke(p, 1.5f, 1.3f));
                                        GunkataFx.Flare(x.Chest, 2.2f, Color.white, true); }),
                    At(0, 0.7f, x => x.Ring(10, x.Radius * 0.6f, p => GunkataFx.Spark(p + Vector3.up * x.Size * Random.Range(0.3f, 1f), 0.8f))),
                    At(1, 0.05f, x => { x.Doubles(2.4f, 0.75f); x.Snd("e_glass", 0.6f); }),
                    At(2, 0.2f, x => { x.Snd("e_fan"); x.Snd("e_slam_2", 0.6f); x.Slow(0.5f); x.Shake(0.3f);
                                       StylizedFx.Burst(x.Chest + x.Fw * 0.9f, 1.4f, StylizedFx.Glass); GunkataFx.Swirl(x.Chest + x.Fw * 0.6f, 0.8f, 3f, 0.4f); }),
                },
            },
            // 3장 보스: 고래 — 부감 파문 → 수면 위로 솟구침 → 낮은 3/4 정면으로 파고듦
            (3, false) => new BossIntroDef
            {
                id = "ch3_boss", titleEn = "MEMORY OF THE LAKE", nameEn = "MOBY GLASS", titleKo = "호수의 기억", nameKo = "모비 글래스", main = true, emerge = true, faceLeft = true,
                accent = C("9AD4F2"), accent2 = C("16304A"), card = IntroCard.Shatter,
                shots = new[] { (IntroShot.Top, 1.3f, IntroCut.Hard), (IntroShot.Rise, 1.5f, IntroCut.Whip), (IntroShot.HeroLow, 1.4f, IntroCut.Flash) },   // 끝 = 머리 쪽 3/4 정면, 얼굴을 화면 왼쪽 1/3 (2026-11-23 사용자: 일러스트 옆에 앞얼굴)
                beats = new[]
                {
                    At(0, 0.15f, x => { x.Snd("e3b_song", 0.5f); ExtraFx.GroundWave(x.Feet, x.Radius + 2f, new Color(0.7f, 0.9f, 1f)); }),
                    At(0, 0.6f, x => ExtraFx.GroundWave(x.Feet, x.Radius + 4f, new Color(0.7f, 0.9f, 1f))),
                    At(1, 0.12f, x => { x.Emerge(0.55f); x.Snd("e3b_dive"); x.Shake(0.4f); WaterBurst.Spawn(x.Feet, 2.2f); StylizedFx.Burst(x.Feet + Vector3.up, 3f, StylizedFx.Glass); }),
                    At(2, 0.2f, x => { x.Act("Attack"); x.Snd("e3b_song"); x.Snd("e3b_shards", 0.7f); x.Slow(0.45f); x.Shake(0.3f); StylizedFx.Shockwave(x.Feet, x.Radius + 4f, StylizedFx.Glass, 0.7f); }),
                },
            },
            // 4장 중간: 처형인 — 점멸 점프컷 4번 → 크래시 + 붉은 X자 베기
            (4, true) => new BossIntroDef
            {
                id = "ch4_mid", titleEn = "GUILLOTINE OF THE TOWER", nameEn = "GUILLOTINE", titleKo = "시계탑의 단두대", nameKo = "기로틴", noir = true,
                accent = C("E0262A"), accent2 = C("0A0A0C"), card = IntroCard.NoirSlash,
                shots = new[] { (IntroShot.JumpCut, 1.0f, IntroCut.Hard), (IntroShot.Crash, 1.4f, IntroCut.Flash) },
                beats = new[]
                {
                    At(0, 0.0f, x => { x.Act("Intro"); x.Hide(true); }),
                    At(0, 0.25f, x => { x.Hide(false); x.Flicker(0.2f); x.Flash(new Color(0.9f, 0.1f, 0.12f), 0.6f); x.Snd("e4m_glitch_1"); x.Shake(0.25f);
                                        StylizedFx.Burst(x.Chest, 1.4f, StylizedFx.BlastRed); }),
                    At(1, 0.2f, x => { x.Snd("e4_scythe_1"); x.Snd("e4m_glitch_1", 0.6f); x.Shake(0.4f); x.Slow(0.4f);
                                       StylizedFx.Slash(x.Chest, (x.Side + Vector3.up * 0.4f).normalized, x.Size * 0.9f, StylizedFx.BlastRed, 1f);
                                       StylizedFx.Slash(x.Chest, (x.Side - Vector3.up * 0.4f).normalized, x.Size * 0.9f, StylizedFx.BlastRed, 1f, true); }),
                },
            },
            // 4장 보스: 쌍둥이 (2026-11-22 사용자: "너무 가만히 서 있음, 컷 전환도 더") — 시계바늘처럼 돌며 둘이 내리꽂혀 착지 →
            // 크로노가 대검을 휘두르는 근접 컷 → 템포가 레이피어를 찌르는 근접 컷 → 옆에서 둘이 서로 자리를 바꾸며 교차 베기(느린 화면) → 정면 결정 자세
            (4, false) => new BossIntroDef
            {
                id = "ch4_boss", bust = true, entryUp = 12f, titleEn = "GUARDIANS OF TIME", nameEn = "CHRONO & TEMPO", titleKo = "시간의 수문장", nameKo = "크로노 & 템포", main = true, noir = true,
                accent = C("D9D9DE"), accent2 = C("B3141A"), card = IntroCard.TwinSplit,
                shots = new[] { (IntroShot.ClockSweep, 1.0f, IntroCut.Hard), (IntroShot.Duel, 0.55f, IntroCut.Whip), (IntroShot.Duel, 0.5f, IntroCut.Hard),
                                (IntroShot.Profile, 0.8f, IntroCut.Flash), (IntroShot.Push, 1.35f, IntroCut.Whip) },   // 끝 = 낮게 다가가는 정면, 둘 다 크게 (Crash는 둘 사이로 너무 붙고 Pullback은 너무 작아짐)
                beats = new[]
                {
                    At(0, 0.0f, x => { x.Act("Intro"); x.Snd("e4_tick_1"); }),
                    At(0, 0.05f, x => x.MarkLandings(2.2f, 0.6f)),
                    At(0, 0.2f, x => { x.Enter(0.5f, "drop"); x.Snd("e_dive", 0.6f);
                                       x.Later(0.5f, () => { x.Snd("e_slam_1"); x.Snd("e4_bell_1", 0.6f); x.Shake(0.55f);
                                                             x.EachFeet(p => { StylizedFx.Shockwave(p, x.Radius + 2f, StylizedFx.BlastRed, 0.5f); StylizedFx.GroundBlast(p, 3f, StylizedFx.BlastRed); }); }); }),
                    At(0, 0.45f, x => x.Snd("e4_tick_2")), At(0, 0.85f, x => x.Snd("e4_tick_1")),
                    // 크로노: 대검 휘두르기
                    At(1, 0.0f, x => { x.Focus(x.Twin(true)); x.Act(x.Twin(true), "SwingR"); x.Snd("e4_wind", 0.7f); }),
                    At(1, 0.55f, x => { x.Snd("e4_scythe_1"); x.Snd("e4_clank", 0.5f); x.Shake(0.35f);
                                        var c = x.FocusChest; StylizedFx.Slash(c + x.Fw * 0.6f, (x.Side + Vector3.up * 0.25f).normalized, x.Size * 0.8f, StylizedFx.BlastRed, 0.8f);
                                        StylizedFx.Slash(c + x.Fw * 0.6f, (x.Side - Vector3.up * 0.35f).normalized, x.Size * 0.6f, StylizedFx.SlashCore, 0.6f, true); }),
                    // 템포: 레이피어 찌르기
                    At(2, 0.0f, x => { x.Focus(x.Twin(false)); x.Act(x.Twin(false), "Stab"); x.Snd("e4m_blink_1", 0.6f); x.Flicker(0.06f); }),
                    At(2, 0.45f, x => { x.Snd("e4_stab_1"); x.Shake(0.3f);
                                        var c = x.FocusChest; StylizedFx.Slash(c + x.Fw * 0.8f, x.Fw, x.Size * 0.7f, StylizedFx.SlashCore, 0.6f);
                                        }),
                    // 옆에서 자리 바꾸며 교차 베기
                    At(3, 0.02f, x => { x.Act(x.Twin(true), "Whirl"); x.Act(x.Twin(false), "Stab"); x.Swap(0.6f); x.Snd("e4_whirl", 0.8f); x.Snd("dash", 0.6f); }),
                    At(3, 0.42f, x => { x.Snd("e4_bell_2"); x.Snd("e4_scythe_2"); x.Snd("e4_stab_2", 0.7f); x.Shake(0.5f); x.Slow(0.3f); x.Flash(x.Accent2, 0.5f);
                                        StylizedFx.Slash(x.Chest, (x.Fw + Vector3.up * 0.5f).normalized, x.Size * 1.1f, StylizedFx.BlastRed, 0.9f);
                                        StylizedFx.Slash(x.Chest, (x.Fw - Vector3.up * 0.5f).normalized, x.Size * 1.1f, StylizedFx.SlashCore, 0.9f, true);
                                        x.Later(0.09f, () => x.Slow(1f)); }),
                    // 정면 결정 자세
                    At(4, 0.0f, x => { x.FaceFront(); x.Act("IntroPose"); x.Snd("e4_tick_2"); }),
                    At(4, 0.24f, x => { x.Snd("e4_bell_2"); x.Snd("e4_scythe_2", 0.7f); x.Shake(0.5f); x.Slow(0.45f);
                                        StylizedFx.Slash(x.Chest, x.Side, x.Size * 1.1f, StylizedFx.BlastRed, 1f);
                                        StylizedFx.Slash(x.Chest, Vector3.Cross(x.Side, Vector3.up), x.Size * 1.1f, StylizedFx.SlashCore, 1f, true);
                                        StylizedFx.Shockwave(x.Feet, x.Radius + 3f, StylizedFx.BlastRed, 0.6f); }),
                },
            },
            // 5장 중간: 거울에서 나온 나 (2026-11-22 사용자: 맥크리 '최고의 플레이' 참고) — 고양이 어깨 너머 거울 속 나 →
            // 낮게 돌며 모자 챙을 내렸다 올리고 리볼버를 손가락으로 돌림 → 정면 낮은 앵글에서 두 자루를 번개처럼 뽑아 카메라를 겨눔
            (5, true) => new BossIntroDef
            {
                id = "ch5_mid", titleEn = "", nameEn = "UNKNOWN", titleKo = "", nameKo = "미지의 존재", glitch = true, faceLeft = true,
                accent = C("E8E4F4"), accent2 = C("2A1E48"), card = IntroCard.Glitch,
                shots = new[] { (IntroShot.OverShoulder, 0.75f, IntroCut.Hard), (IntroShot.HeroOrbit, 0.95f, IntroCut.Flash), (IntroShot.QuickDraw, 1.0f, IntroCut.Hard) },
                beats = new[]
                {
                    At(0, 0.0f, x =>
                    {
                        var ps = x.Perform(
                            (0f, ""), (0.55f, ""),
                            (0.85f, "RA.FB=.5 RA.DU=.78 RF=-.95 RH.DU=.25 HD.ND=-.38 SP.TW=.1 CH.FB=.05"),                       // 모자 챙을 잡고 고개 숙임
                            (1.1f, "RA.FB=.5 RA.DU=.82 RF=-.95 RH.DU=.25 HD.ND=-.42 SP.TW=.12"),
                            (1.28f, "RA.FB=.35 RA.DU=.35 RF=-.35 RA.TW=.2 HD.ND=.12 HD.TL=-.1 LA.FB=-.1 LF=-.4 SP.TW=-.1"),     // 고개 들며 팔을 내려 리볼버 돌림
                            (1.62f, "RA.FB=.38 RA.DU=.38 RF=-.3 RA.TW=.25 HD.ND=.12 HD.TL=-.12 LA.FB=-.05 LF=-.45 SP.TW=-.12"),
                            (1.75f, "RA.FB=1.0 LA.FB=1.0 RA.DU=.25 LA.DU=.25 RF=.45 LF=.45 SP.FB=.18 CH.FB=.05 LL.FB=.4 LK=-.4 RK=-.25 HD.ND=-.06 ROOT=-.09"),   // 두 자루 뽑아 겨눔
                            (1.82f, "RA.FB=1.0 LA.FB=1.0 RA.DU=.32 LA.DU=.3 RF=.45 LF=.45 SP.FB=.12 CH.FB=-.05 LL.FB=.4 LK=-.4 RK=-.25 ROOT=-.09"),
                            (3.2f, "RA.FB=1.0 LA.FB=1.0 RA.DU=.25 LA.DU=.25 RF=.45 LF=.45 SP.FB=.18 LL.FB=.4 LK=-.4 RK=-.25 HD.ND=-.06 ROOT=-.09"));
                        if (ps) ps.Spin(x.Part("Gun_R"), 1.22f, 1.62f, 3f, Vector3.right);
                    }),
                    At(0, 0.0f, x => x.Hide(true)),
                    At(0, 0.45f, x => { x.Hide(false); x.Flicker(0.3f); x.Flash(new Color(0.85f, 0.9f, 1f), 0.6f); x.Snd("e5_phantom_break_1"); x.Snd("e_glass"); x.Shake(0.25f);
                                        StylizedFx.Burst(x.Chest, 1.8f, StylizedFx.Glass); StylizedFx.Shockwave(x.Feet, 3f, StylizedFx.Glass, 0.45f); }),
                    At(0, 0.6f, x => x.Snd("e5_phantom_split", 0.6f)),
                    At(1, 0.1f, x => x.Snd("reload_end", 0.8f)),
                    At(1, 0.5f, x => { x.Snd("gunkata_spin", 0.6f); GunkataFx.Swirl(x.Chest + x.Side * 0.4f, 0.5f, 2f, 0.4f); }),
                    At(2, 0.15f, x => { x.Snd("shot_1"); x.Snd("shot_2", 0.8f); x.Shake(0.4f); x.Slow(0.35f); x.Flash(Color.white, 0.5f);
                                        GunkataFx.MuzzleFlash(x.Chest + x.Fw * 0.9f + x.Side * 0.25f, x.Fw, 1.6f); GunkataFx.MuzzleFlash(x.Chest + x.Fw * 0.9f - x.Side * 0.25f, x.Fw, 1.6f); }),
                    At(2, 0.32f, x => { x.Snd("e5_phantom_break_1"); StylizedFx.Burst(x.Chest - x.Fw * (x.Radius + 1.2f), 1.6f, StylizedFx.Glass); StylizedFx.Shockwave(x.Feet, 2.5f, StylizedFx.Glass, 0.4f); }),
                },
            },
            // 5장 보스: 시간의 주인 — 부감 공허 돔 → 째깍째깍 끊기는 궤도 → 느린 돌리 + 대낫
            (5, false) => new BossIntroDef
            {
                id = "ch5_boss", bust = true, entryUp = 9f, titleEn = "RULER OF ETERNITY", nameEn = "LORD OF TIME", titleKo = "영원의 지배자", nameKo = "시간의 주인", main = true,
                accent = C("A45CF0"), accent2 = C("E9B84A"), card = IntroCard.Dial,
                shots = new[] { (IntroShot.WormEye, 1.2f, IntroCut.Hard), (IntroShot.Stutter, 1.5f, IntroCut.Hard), (IntroShot.Vertigo, 1.5f, IntroCut.Whip) },
                beats = new[]
                {
                    At(0, 0.0f, x => x.Act("Intro")),
                    At(0, 0.0f, x => { x.Enter(1.05f, "descend"); x.Snd("e5b_summon"); Telegraph.Ring(x.Feet, x.Radius + 2.5f, 1.05f, new Color(0.7f, 0.35f, 1f)); }),   // 소환 원
                    At(0, 0.85f, x => { x.Snd("e5b_bell", 0.7f); x.Shake(0.3f); StylizedFx.Dome(x.Feet, x.Radius + 2f, 0.7f, StylizedFx.Void); }),
                    At(1, 0.0f, x => x.Snd("e5b_hourglass", 0.7f)),
                    At(2, 0.35f, x => { x.Snd("e5b_time_cut"); x.Snd("e5b_reap_wind", 0.7f); x.Shake(0.5f); x.Slow(0.4f);
                                        ReaperFx.BigSlash(x.Front(0.5f), x.Fw, x.Size * 1.3f, 110f, true); }),
                },
            },
            _ => null,
        };

        // ---------- 실행 ----------
        /// <summary>연출 중 (Enemy.Update · TwinDirector가 멈춤, 플레이어 입력 잠김)</summary>
        public static bool Playing { get; private set; }
        static BossIntro inst;

        /// <summary>보스를 막 만든 뒤 RunManager 코루틴에서 yield return</summary>
        public static IEnumerator Play(Enemy boss, int chapter, bool mid)
        {
            var def = Def(chapter, mid);
            if (def == null || !boss || Playing) yield break;
            if (!inst) inst = new GameObject("BossIntro").AddComponent<BossIntro>();
            yield return inst.Run(boss, def);
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() { Playing = false; inst = null; }

        Camera cam;
        float savedFov;
        bool skippable, skipped;
        readonly List<Canvas> hidden = new();
        readonly List<Enemy> targets = new();
        readonly List<float> groundY = new();
        readonly List<Vector3> groundPos = new();
        bool entryDone = true;
        BossIntroDef def;

        // 장면 기준
        Vector3 B, F, S;      // 보스 중심(발) · 보스 → 고양이 · 옆
        float H, R;           // 키 · 반지름
        float Reach;          // 보스 중심에서 고양이 쪽 앞 끝까지 (긴 보스의 머리)
        float shakeAmt;

        IEnumerator Run(Enemy boss, BossIntroDef d)
        {
            def = d;
            Playing = true;
            skipped = false;
            shakeAmt = 0;
            skippable = Meta.IntroSeen(d.id);
            var p = PlayerController.Instance;
            if (p) p.InputLocked = true;

            targets.Clear(); groundY.Clear();
            foreach (var e in Enemy.All) if (e && e.kind == Enemy.Kind.Boss && e.health && e.health.Alive) targets.Add(e);
            if (!targets.Contains(boss)) targets.Add(boss);
            foreach (var e in targets) groundY.Add(e.transform.position.y);
            modelPose.Clear();
            foreach (var e in targets)
            {
                if (!e) continue;
                var m = e.transform.Find("Model"); if (m) modelPose[m] = (m.localPosition, m.localRotation);
                foreach (var an in e.GetComponentsInChildren<Animator>())
                {
                    if (an.transform != e.transform) modelPose[an.transform] = (an.transform.localPosition, an.transform.localRotation);
                    if (an.applyRootMotion) { an.applyRootMotion = false; rootMotion.Add(an); }   // 연출 중 휘두르기 · 돌기 동작이 모델을 돌려 등을 보이지 않게
                }
            }
            Frame(p);
            GatherOccluders();
            groundPos.Clear(); foreach (var e in targets) groundPos.Add(e.transform.position);
            entryDone = !(d.emerge || d.entryUp != 0 || d.entryBack != 0);
            if (d.emerge) foreach (var e in targets) e.transform.position += Vector3.down * (H * 1.3f);   // 솟아오를 때까지 바닥 아래
            for (int i = 0; i < targets.Count; i++)
                targets[i].transform.position += Vector3.up * (d.entryUp + (d.entryUp > 0 ? i * 3f : 0)) - F * d.entryBack;

            cam = Camera.main;
            savedFov = cam ? cam.fieldOfView : 40f;
            CameraFollow.Hold = true;
            HideHud(true);
            BuildUi();
            Music.Duck(0.3f, 0.4f);                          // 연출 동안 보스 곡을 낮춤 → 정지 순간 침묵 → 끝나며 곡을 처음부터 크게
            if (Sfx.Has("intro_riser")) Snd("intro_riser", 0.8f);

            if (cam)
            {
                var shots = d.shots;
                for (int i = 0; i < shots.Length && !skipped; i++)
                {
                    var next = i + 1 < shots.Length ? shots[i + 1].cut : IntroCut.Flash;
                    yield return RunShot(shots[i].shot, shots[i].dur, i, shots[i].cut, next, i == shots.Length - 1);
                }
                if (!skipped) yield return Freeze();
            }

            Time.timeScale = 1f;
            RestoreGround();
            RestoreOccluders();
            Music.Restart();
            Music.Duck(1f, skipped ? 0.4f : 0.9f);
            yield return ReturnCamera(skipped ? 0f : 0.4f);
            Cleanup();
            Meta.MarkIntroSeen(d.id);
            yield return null;                               // 스킵한 스페이스가 같은 프레임에 대시로 먹히지 않게
            if (p) p.InputLocked = false;
            foreach (var e in targets) if (e && e.brain) e.brain.HoldPatterns(1.0f);
        }

        void Frame(PlayerController p)
        {
            B = Vector3.zero; H = 1.2f; R = 0.8f;
            foreach (var e in targets) B += e.transform.position;
            B /= Mathf.Max(1, targets.Count);
            var pp = p ? p.transform.position : B + Vector3.back * 10f;
            F = Effects.Flat(pp - B);
            F = F.sqrMagnitude > 0.01f ? F.normalized : Vector3.back;
            S = Vector3.Cross(Vector3.up, F);
            foreach (var e in targets)
            {
                var to = Effects.Flat(pp - e.transform.position);
                if (to.sqrMagnitude > 0.01f) e.transform.rotation = Quaternion.LookRotation(to);
                // 크기 = 팩토리가 정한 몸 캡슐 (스킨 메시 bounds는 헐렁하고 굽기는 배율이 들쭉날쭉)
                var col = e.GetComponent<CapsuleCollider>();
                float h = col ? col.height : 3f, rad = col ? col.radius : 1f;
                var an = e.GetComponentInChildren<Animator>();                      // 휴머노이드면 머리 뼈 높이가 더 정확 (캡슐은 실제 모델보다 큰 경우가 많음)
                var headBone = an && an.isHuman && !e.GetComponentInChildren<HoverFloat>() ? an.GetBoneTransform(HumanBodyBones.Head) : null;   // 떠 있는 사신은 리그 머리 뼈가 낮게 잡혀 캡슐을 믿음
                if (headBone) h = Mathf.Clamp((headBone.position.y - e.transform.position.y) * 1.12f + 0.15f, h * 0.65f, h * 1.3f);   // 리그가 이상한 모델(떠 있는 사신)은 캡슐의 65% 밑으로는 안 내려감
                H = Mathf.Max(H, h);
                R = Mathf.Max(R, rad + 0.55f * Effects.Flat(e.transform.position - B).magnitude);   // 쌍둥이: 벌어진 거리를 다 넣으면 너무 멀어짐
            }
            H = Mathf.Clamp(H, 1.6f, 14f);
            R = Mathf.Clamp(R, 0.8f, 10f);
            // 앞 끝까지 거리 (고양이 쪽 F 방향으로 모델이 얼마나 뻗었나): 긴 보스(고래)의 머리를 겨눌 때
            Reach = R;
            foreach (var e in targets)
            {
                if (!e) continue;
                foreach (var r in e.GetComponentsInChildren<Renderer>())
                {
                    if (r is ParticleSystemRenderer || r is TrailRenderer || r is LineRenderer) continue;
                    var bb = r.bounds;
                    float ext = Mathf.Abs(bb.extents.x * F.x) + Mathf.Abs(bb.extents.z * F.z);
                    Reach = Mathf.Max(Reach, Vector3.Dot(Effects.Flat(bb.center - B), F) + ext);
                }
            }
        }

        void RestoreGround()
        {
            if (!entryDone) { for (int i = 0; i < targets.Count && i < groundPos.Count; i++) if (targets[i]) targets[i].transform.position = groundPos[i]; entryDone = true; return; }
            for (int i = 0; i < targets.Count && i < groundY.Count; i++)
                if (targets[i]) { var q = targets[i].transform.position; q.y = groundY[i]; targets[i].transform.position = q; }
        }

        // ---------- 비트에서 쓰는 도구 (BossIntroDef.beats 람다) ----------
        public Vector3 Feet => B;
        public Vector3 Chest => B + Vector3.up * H * 0.55f;
        public Vector3 Head => B + Vector3.up * H * 0.85f;
        public Vector3 Fw => F;
        public const float ShotHit = 0.36f;                 // ShotDown 컷에서 총알이 카메라에 맞는 때 (컷 진행도)
        public const float ShotLand = 0.66f;                // ShotDown 컷에서 카메라가 바닥에 떨어지는 때
        public Vector3 CamPos => cam ? cam.transform.position : B + F * 6f;
        public Vector3 Side => S;
        public float Size => H;
        public float Radius => R;
        public Vector3 Front(float d) => B + F * (R + d);
        public void Act(string anim) { foreach (var e in targets) if (e) e.Anim(anim); }
        public Color Accent2 => def.accent2;
        /// <summary>쌍둥이 중 크로노(시침, true) · 템포(분침, false)의 번호. 없으면 0</summary>
        public int Twin(bool hour)
        {
            for (int i = 0; i < targets.Count; i++)
                if (targets[i] && (hour ? (Component)targets[i].GetComponent<Ch4HourBrain>() : targets[i].GetComponent<Ch4MinuteBrain>())) return i;
            return 0;
        }
        /// <summary>이 컷 동안 카메라가 보스 하나만 봄 (Duel 컷). 컷이 바뀌면 풀림</summary>
        public void Focus(int idx)
        {
            focus = idx; focusH = H;
            if (idx < 0 || idx >= targets.Count || !targets[idx]) return;
            var an = targets[idx].GetComponentInChildren<Animator>();
            var head = an && an.isHuman ? an.GetBoneTransform(HumanBodyBones.Head) : null;
            if (head) focusH = Mathf.Clamp((head.position.y - targets[idx].transform.position.y) * 1.12f + 0.15f, 1.6f, H);
        }
        int focus = -1;
        float focusH;
        readonly Dictionary<Transform, (Vector3 pos, Quaternion rot)> modelPose = new();
        readonly List<Animator> rootMotion = new();
        public Vector3 FocusChest => (focus >= 0 && focus < targets.Count && targets[focus] ? targets[focus].transform.position : B) + Vector3.up * H * 0.55f;
        public void EachFeet(System.Action<Vector3> fx) { foreach (var e in targets) if (e) fx(e.transform.position); }
        /// <summary>모두 고양이 쪽을 봄</summary>
        public void FaceFront()
        {
            var p = PlayerController.Instance;
            if (entryDone) for (int i = 0; i < targets.Count && i < groundPos.Count; i++) if (targets[i]) targets[i].transform.position = groundPos[i];
            foreach (var kv in modelPose) if (kv.Key) kv.Key.localRotation = kv.Value.rot;   // 휘두르기 동작으로 돌아간 모델 방향만 되돌림 (위치는 X — 처음 프레임 값은 발 맞춤 전이라 2m 떠 버림)
            foreach (var e in targets) if (e)
            {
                var to = Effects.Flat((p ? p.transform.position : e.transform.position + F) - e.transform.position);
                if (to.sqrMagnitude > 0.01f) e.transform.rotation = Quaternion.LookRotation(to);
            }
        }
        /// <summary>둘이 서로 자리를 바꾸며 스쳐 지나감 (한 녀석은 앞으로, 한 녀석은 뒤로 둥글게 비켜 감)</summary>
        public void Swap(float dur) { if (targets.Count >= 2) StartCoroutine(SwapRun(dur)); }
        IEnumerator SwapRun(float dur)
        {
            Transform a = targets[0] ? targets[0].transform : null, b = targets[1] ? targets[1].transform : null;
            if (!a || !b) yield break;
            Vector3 pa = groundPos.Count >= 2 ? groundPos[0] : a.position, pb = groundPos.Count >= 2 ? groundPos[1] : b.position;   // 앞 컷 루트 모션으로 밀렸어도 원래 자리 기준
            var run = Effects.Flat(pb - pa).normalized;
            // 스쳐 지나가 서로 어깨를 맞댄 자리에서 멈춤 (8m 벌어져 있으면 마지막 정면 컷에 둘이 화면 양 끝으로 밀려남)
            var mid = (pa + pb) * 0.5f; float half = Mathf.Min(Effects.Flat(pb - pa).magnitude * 0.5f, 2.3f);
            Vector3 ea = mid + run * half, eb = mid - run * half;
            if (run.sqrMagnitude > 0.01f) { a.rotation = Quaternion.LookRotation(run); b.rotation = Quaternion.LookRotation(-run); }
            float arc = Mathf.Max(1.2f, R * 0.35f), nextPuff = 0;
            for (float t = 0; t <= dur; t += Time.deltaTime)
            {
                float u = Mathf.Clamp01(t / dur), e = EaseInOut(u), bow = Mathf.Sin(Mathf.PI * u) * arc;
                a.position = Vector3.Lerp(pa, ea, e) + F * bow;
                b.position = Vector3.Lerp(pb, eb, e) - F * bow;
                if (t >= nextPuff) { nextPuff = t + 0.05f; GunkataFx.Smoke(a.position, 1f, 0.6f); GunkataFx.Smoke(b.position, 1f, 0.6f); }
                yield return null;
            }
            a.position = ea; b.position = eb;
            if (groundPos.Count >= 2) { groundPos[0] = ea; groundPos[1] = eb; }
        }
        public void Act(int idx, string anim) { if (idx < targets.Count && targets[idx]) targets[idx].Anim(anim); }
        public void Snd(string n, float vol = 0.9f) => Sfx.Play(n, vol, 0f);
        public void Shake(float a) => shakeAmt = Mathf.Max(shakeAmt, a);
        public void Slow(float s) => Time.timeScale = s;
        public void Flash(Color c, float a = 0.9f) { if (flash) StartCoroutine(FadeImg(flash, a, 0f, 0.18f, c)); }
        public void Flicker(float d) => StartCoroutine(GlitchFlicker(d));
        /// <summary>보스를 안 보이게 / 다시 보이게 (연기 속 등장). 끝나면 Cleanup이 되돌림</summary>
        public void Hide(bool hide)
        {
            foreach (var e in targets) if (e) foreach (var r in e.GetComponentsInChildren<Renderer>())
            {
                if (hide) { if (r.enabled) { r.enabled = false; flickered.Add(r); } }
                else if (flickered.Remove(r)) r.enabled = true;
            }
        }
        /// <summary>보스 모델을 양옆에 복제해 같은 동작을 따라 하며 깜빡이다 life초 뒤 유리처럼 깨짐 (거울 분신)</summary>
        public void Doubles(float dist, float life)
        {
            if (targets.Count == 0 || !targets[0]) return;
            var m = targets[0].transform.Find("Model");
            if (!m) return;
            var src = m.GetComponentInChildren<Animator>();
            for (int sgn = -1; sgn <= 1; sgn += 2)
            {
                var c = Instantiate(m.gameObject, m.position + S * sgn * dist, m.rotation);
                c.transform.localScale = m.lossyScale;
                foreach (var mb in c.GetComponentsInChildren<MonoBehaviour>()) mb.enabled = false;
                var an = c.GetComponentInChildren<Animator>();
                if (an && src) { var st = src.GetCurrentAnimatorStateInfo(0); an.Play(st.fullPathHash, 0, st.normalizedTime); }
                doubles.Add(c);
                StartCoroutine(DoubleRun(c, life));
            }
        }
        readonly List<GameObject> doubles = new();
        IEnumerator DoubleRun(GameObject c, float life)
        {
            var rs = c.GetComponentsInChildren<Renderer>();
            for (float t = 0; t < life && c; t += Time.deltaTime)
            {
                bool on = t < 0.12f ? Random.value > 0.5f : Random.value > 0.1f;
                foreach (var r in rs) if (r) r.enabled = on;
                yield return null;
            }
            if (!c) yield break;
            StylizedFx.Burst(c.transform.position + Vector3.up * H * 0.5f, 1.5f, StylizedFx.Glass);
            Snd("e_glass", 0.7f);
            Destroy(c);
        }
        /// <summary>첫 보스에 연출 전용 근육 자세 (IntroPoser). 휴머노이드가 아니면 null</summary>
        public IntroPoser Perform(params (float t, string pose)[] tl) => targets.Count > 0 ? IntroPoser.Play(targets[0], tl) : null;
        /// <summary>보스 안에서 이름으로 부품 찾기 (리볼버 등)</summary>
        public Transform Part(string name)
        {
            foreach (var e in targets) if (e) foreach (var t in e.GetComponentsInChildren<Transform>(true)) if (t.name == name) return t;
            return null;
        }
        public void Ring(int n, float radius, System.Action<Vector3> fx)
        {
            for (int i = 0; i < n; i++) fx(B + Quaternion.Euler(0, 360f * i / n, 0) * F * radius);
        }
        /// <summary>보스(들)를 고양이 쪽으로 dist만큼 밀고 나옴 (고양이와 3.5m는 남김). 카메라 기준점도 같이 움직임</summary>
        public void Lunge(float dist, float dur, bool smoke) => StartCoroutine(LungeRun(dist, dur, smoke));
        IEnumerator LungeRun(float dist, float dur, bool smoke)
        {
            var p = PlayerController.Instance;
            if (p) dist = Mathf.Min(dist, Mathf.Max(0, Effects.Flat(p.transform.position - B).magnitude - R - 3.5f));
            float done = 0, nextPuff = 0;
            for (float t = 0; t < dur; t += Time.deltaTime)
            {
                float want = dist * EaseInOut(Mathf.Clamp01(t / dur)), step = want - done; done = want;
                foreach (var e in targets) if (e) e.transform.position += F * step;
                if (smoke && t >= nextPuff) { nextPuff = t + 0.08f; GunkataFx.Smoke(B - F * R + S * Random.Range(-R, R), 1.1f, 0.9f); }
                yield return null;
            }
        }
        /// <summary>바닥 아래 숨었던 보스가 솟아오름 (emerge)</summary>
        public void Emerge(float dur) => Enter(dur, "rise");
        /// <summary>비켜 둔 시작 위치에서 제자리로 들어옴. mode: drop(가속 낙하) · descend(천천히 내려앉음) · charge(달려 들어와 멈춤) · rise(솟구쳤다 내려앉음)</summary>
        public void Enter(float dur, string mode) => StartCoroutine(EnterRun(dur, mode));
        IEnumerator EnterRun(float dur, string mode)
        {
            var from = new List<Vector3>();
            foreach (var e in targets) from.Add(e ? e.transform.position : Vector3.zero);
            float nextPuff = 0;
            for (float t = 0; t <= dur; t += Time.deltaTime)
            {
                float u = Mathf.Clamp01(t / dur);
                float k = mode switch { "drop" => u * u, "descend" => EaseOut(u), "charge" => EaseOut(u), _ => Back(u, 1.4f) };
                for (int i = 0; i < targets.Count && i < groundPos.Count; i++)
                    if (targets[i]) targets[i].transform.position = Vector3.LerpUnclamped(from[i], groundPos[i], k);
                if (mode == "charge" && t >= nextPuff) { nextPuff = t + 0.07f; foreach (var e in targets) if (e) GunkataFx.Smoke(e.transform.position - F * R + S * Random.Range(-R, R), 1.2f, 0.9f); }
                yield return null;
            }
            for (int i = 0; i < targets.Count && i < groundPos.Count; i++) if (targets[i]) targets[i].transform.position = groundPos[i];
            entryDone = true;
        }
        /// <summary>보스마다 내려앉을 자리에 예고 원</summary>
        public void MarkLandings(float radius, float dur) { foreach (var g in groundPos) Telegraph.Ring(g, radius, dur, Telegraph.Danger); }
        /// <summary>s초 뒤 (게임 시간) 실행 — 착지 순간 이펙트 등</summary>
        public void Later(float s, System.Action a) => StartCoroutine(LaterRun(s, a));
        IEnumerator LaterRun(float s, System.Action a) { yield return new WaitForSeconds(s); a?.Invoke(); }
        IEnumerator EmergeRun(float dur) { yield return EnterRun(dur, "rise"); }

        // ---------- 가림막 숨기기: 카메라와 보스 사이 소품 · 고양이를 잠깐 끔 ----------
        readonly List<Renderer> nearby = new();
        readonly HashSet<Renderer> hiddenOcc = new();
        readonly HashSet<Renderer> playerRends = new();

        void GatherOccluders()
        {
            nearby.Clear(); hiddenOcc.Clear(); playerRends.Clear();
            var mine = new HashSet<Renderer>();
            foreach (var e in targets) if (e) foreach (var x in e.GetComponentsInChildren<Renderer>(true)) mine.Add(x);
            var pl = PlayerController.Instance;
            if (pl) foreach (var x in pl.GetComponentsInChildren<Renderer>(true)) playerRends.Add(x);
            float reach = R + H * 3.5f + 14f;
            foreach (var x in FindObjectsByType<Renderer>(FindObjectsSortMode.None))
            {
                if (!x.enabled || mine.Contains(x) || x is ParticleSystemRenderer || x is TrailRenderer || x is LineRenderer) continue;
                var b = x.bounds;
                if (b.size.x > 14f || b.size.z > 14f || b.size.y < 0.25f) continue;          // 바닥 · 큰 벽 · 바닥 장식은 그대로
                if (Effects.Flat(b.center - B).magnitude > reach) continue;
                nearby.Add(x);
            }
        }

        void UpdateOccluders(Vector3 camPos, Vector3 look, bool keepPlayer, bool hidePlayer = false)
        {
            var to = look - camPos; float dist = to.magnitude;
            if (dist < 0.01f) return;
            var ray = new Ray(camPos, to / dist);
            float limit = Mathf.Max(0.5f, dist - R * 0.8f);
            foreach (var x in nearby)
            {
                if (!x) continue;
                var b = x.bounds; b.Expand(0.4f);
                bool block = ((b.Contains(camPos) || (b.IntersectRay(ray, out float d) && d < limit)) && !(keepPlayer && playerRends.Contains(x))) || (hidePlayer && playerRends.Contains(x));
                if (block && x.enabled) { x.enabled = false; hiddenOcc.Add(x); }
                else if (!block && hiddenOcc.Contains(x)) { x.enabled = true; hiddenOcc.Remove(x); }
            }
        }

        void RestoreOccluders()
        {
            foreach (var x in hiddenOcc) if (x) x.enabled = true;
            hiddenOcc.Clear(); nearby.Clear();
        }

        bool SkipPressed()
        {
            if (!skippable || skipped) return false;
            var kb = Keyboard.current;
            if (kb != null && kb.spaceKey.wasPressedThisFrame) { skipped = true; StartCoroutine(SkipPress()); }
            return skipped;
        }

        // ---------- 컷 ----------
        struct Pose { public Vector3 pos, look, up; public float fov, roll; }

        static float EaseInOut(float u) => u < 0.5f ? 4 * u * u * u : 1 - Mathf.Pow(-2 * u + 2, 3) / 2;
        static float EaseOut(float u) => 1 - Mathf.Pow(1 - u, 3);
        static float Expo(float u) => u >= 1 ? 1 : 1 - Mathf.Pow(2, -10 * u);
        static float Back(float u, float s = 1.70158f) { u = Mathf.Clamp01(u); return 1 + (s + 1) * Mathf.Pow(u - 1, 3) + s * Mathf.Pow(u - 1, 2); }

        // 하늘이 비어 있는 맵이라 이 컷들 말고는 최소 10° 내려다봄
        static bool LowShot(IntroShot s) => s is IntroShot.LowRise or IntroShot.Top or IntroShot.Feet or IntroShot.Chase or IntroShot.ClockSweep
                                                 or IntroShot.Rise or IntroShot.LookUp or IntroShot.Push or IntroShot.OverShoulder
                                                 or IntroShot.HeroLow or IntroShot.HeroOrbit or IntroShot.QuickDraw or IntroShot.Vertigo or IntroShot.Portrait
                                                 or IntroShot.BehindBoss or IntroShot.Profile or IntroShot.WormEye or IntroShot.Duel or IntroShot.ShotDown;

        int subCut;   // 점프컷 · 끊김 컷의 지금 칸 (바뀌면 효과음)

        // 2026-11-22 사용자: "줌 · 달리 · 극단적 화각 변경 · 로우 앵글 · 가감속을 전체적으로 더" → 화각 폭을 넓히고(광각 ↔ 망원),
        // 궤도에도 달리인을 섞고, 움직이는 컷은 SpeedRamp(빠르게 → 느리게 → 빠르게)로 시간을 다시 매김
        static bool Ramped(IntroShot s) => s is not (IntroShot.JumpCut or IntroShot.Stutter or IntroShot.Snap or IntroShot.Crash or IntroShot.LookUp
                                                    or IntroShot.HeroLow or IntroShot.QuickDraw or IntroShot.ShotDown);
        static float SpeedRamp(float u) => u + 0.55f * Mathf.Sin(2f * Mathf.PI * u) / (2f * Mathf.PI);   // 속도 1.55 → 0.45 → 1.55

        Pose Eval(IntroShot s, float u, int i)
        {
            float k = H, sg = i % 2 == 0 ? 1 : -1;
            var up = Vector3.up;
            var pp = PlayerController.Instance ? PlayerController.Instance.transform.position : B + F * 10f;
            if (Ramped(s)) u = SpeedRamp(u);
            switch (s)
            {
                case IntroShot.Wide:           // 고양이 등 뒤 높은 곳에서 보스 쪽으로 크게 밀고 들어감 (광각 → 표준)
                {
                    float e = EaseInOut(u);
                    var a = pp + F * 7f + up * (k * 1.1f + 5f) + S * 3f * sg;
                    var b = pp + F * 2f + up * (k * 0.6f + 2f) + S * 1f * sg;
                    return new Pose { pos = Vector3.Lerp(a, b, e), look = B + up * k * 0.55f, up = up, fov = Mathf.Lerp(64, 40, e) };
                }
                case IntroShot.LowRise:        // 땅바닥 광각에서 머리까지 틸트업 + 망원으로 조여 듦
                {
                    float e = EaseInOut(u);
                    var a = B + F * (R + k * 0.6f) + S * sg * k * 0.2f + up * 0.15f;
                    var b = B + F * (R + k * 1.1f) + S * sg * k * 0.45f + up * (k * 0.25f);
                    return new Pose { pos = Vector3.Lerp(a, b, e), look = B + up * Mathf.Lerp(k * 0.08f, k * 0.9f, e), up = up, fov = Mathf.Lerp(78, 44, e) };
                }
                case IntroShot.Orbit:          // 돌면서 다가감 (궤도 + 달리인)
                {
                    float e = EaseInOut(u);
                    var dir = Quaternion.Euler(0, Mathf.Lerp(-95f, 30f, e) * sg, 0) * F;
                    return new Pose { pos = B + dir * (R + k * Mathf.Lerp(1.6f, 1.05f, e)) + up * k * Mathf.Lerp(0.25f, 0.6f, e), look = B + up * k * 0.6f, up = up,
                                      fov = Mathf.Lerp(54, 38, e), roll = Mathf.Lerp(-8, 4, e) * sg };
                }
                case IntroShot.Dutch:          // 기울인 화면으로 옆으로 미끄러지며 다가감
                {
                    float e = EaseInOut(u);
                    var pos = B + F * (R + k * Mathf.Lerp(1.7f, 1.15f, e)) + S * Mathf.Lerp(-1f, 1f, e) * k * 0.9f * sg + up * k * 0.4f;
                    return new Pose { pos = pos, look = B + up * k * 0.62f + S * Mathf.Lerp(-0.3f, 0.3f, e) * sg, up = up, fov = Mathf.Lerp(48, 34, e), roll = 16 * sg };
                }
                case IntroShot.Top:            // 바로 위 부감에서 돌며 확 떨어져 내려옴
                {
                    float e = EaseOut(u);
                    var h = Mathf.Lerp(k * 3.2f + 9f, k * 1.5f + 2.5f, e);
                    var spin = Quaternion.Euler(0, Mathf.Lerp(-100f, 20f, e) * sg, 0);
                    var pos = B + up * h + spin * F * Mathf.Lerp(0.3f, k * 0.9f, e);
                    return new Pose { pos = pos, look = B + up * k * 0.3f, up = spin * -F, fov = Mathf.Lerp(66, 45, e) };
                }
                case IntroShot.Feet:           // 서부식: 땅바닥에서 발 · 다리를 옆으로 훑음
                {
                    float e = EaseInOut(u);
                    var pos = B + F * (R + 1.6f + k * 0.3f) + S * sg * Mathf.Lerp(-1.3f, 1.3f, e) + up * 0.15f;
                    return new Pose { pos = pos, look = B + up * Mathf.Min(0.7f, k * 0.15f) + S * sg * Mathf.Lerp(-0.4f, 0.4f, e), up = up, fov = Mathf.Lerp(26, 36, e) };
                }
                case IntroShot.Showdown:       // 서부식 대치: 눈높이 초근접, 망원을 더 조이며 옆으로
                {
                    float e = EaseInOut(u);
                    var pos = B + F * (R + k * 1.1f) + up * k * 0.82f + S * sg * Mathf.Lerp(0.5f, -0.5f, e) * k * 0.4f;
                    return new Pose { pos = pos, look = B + up * k * 0.82f, up = up, fov = Mathf.Lerp(30, 17, e) };
                }
                case IntroShot.Chase:          // 돌진하는 보스 바로 앞에서 낮게 광각으로 뒷걸음
                {
                    float e = EaseOut(u);
                    var pos = B + F * (R + k * Mathf.Lerp(0.6f, 2.0f, e)) + up * k * 0.25f + S * sg * k * 0.2f;
                    return new Pose { pos = pos, look = B + up * k * 0.5f, up = up, fov = Mathf.Lerp(82, 50, e), roll = Mathf.Sin(u * 40f) * 1.5f };
                }
                case IntroShot.JumpCut:        // 점멸: 네 각도를 뚝뚝 끊어 보여 줌 (화각도 칸마다 극단적으로)
                {
                    int j = Mathf.Min(3, Mathf.FloorToInt(u * 4)); float v = u * 4 - j;
                    float[] yaw = { -50, 65, -15, 22 }, dist = { 1.6f, 1.15f, 2.0f, 1.0f }, hgt = { 0.2f, 0.95f, 0.45f, 0.75f }, fov = { 46, 28, 66, 22 };
                    if (j != subCut) { subCut = j; Snd(j % 2 == 0 ? "e4m_blink_1" : "e4m_blink_2", 0.7f); Flash(def.accent, 0.35f); Flicker(0.08f); }
                    var dir = Quaternion.Euler(0, yaw[j] * sg, 0) * F;
                    return new Pose { pos = B + dir * (R + k * dist[j] * (1 - 0.12f * EaseOut(v))) + up * k * hgt[j], look = B + up * k * 0.6f, up = up, fov = fov[j] * (1 - 0.08f * v), roll = (j - 1.5f) * 5f };
                }
                case IntroShot.Stutter:        // 째깍째깍: 궤도를 일곱 칸으로 끊어서 움직임 (칸마다 조금씩 조임)
                {
                    const int steps = 7;
                    int j = Mathf.Min(steps - 1, Mathf.FloorToInt(u * steps));
                    if (j != subCut) { subCut = j; Snd(j % 2 == 0 ? "e4_tick_1" : "e4_tick_2", 0.8f); Shake(0.06f); Flash(def.accent, 0.12f); }
                    float e = EaseInOut((float)j / (steps - 1));
                    var dir = Quaternion.Euler(0, Mathf.Lerp(60f, -30f, e) * sg, 0) * F;
                    return new Pose { pos = B + dir * (R + k * Mathf.Lerp(1.6f, 1.2f, e)) + up * k * Mathf.Lerp(0.3f, 0.65f, e), look = B + up * k * 0.62f, up = up, fov = Mathf.Lerp(50, 34, e) };
                }
                case IntroShot.ClockSweep:     // 바로 위에서 화면이 시계바늘처럼 한 바퀴 돌며 떨어짐
                {
                    float e = EaseInOut(u);
                    var h = Mathf.Lerp(k * 3f + 8f, k * 1.5f + 3.5f, EaseOut(u));
                    var hand = Quaternion.Euler(0, -360f * e, 0) * -F;
                    return new Pose { pos = B + up * h + F * 0.05f, look = B, up = hand, fov = Mathf.Lerp(72, 46, e) };
                }
                case IntroShot.OverShoulder:   // 고양이 어깨 너머에서 시작해 보스로 초점을 옮기며 파고듦
                {
                    float e = EaseInOut(u);
                    float gap = Effects.Flat(pp - B).magnitude;
                    var a = pp + F * 1.6f + S * 0.9f + up * 1.5f;
                    var b = pp - F * (gap * 0.3f) + S * 0.7f + up * 1.9f;
                    return new Pose { pos = Vector3.Lerp(a, b, e), look = Vector3.Lerp(pp + up - F * 2f, B + up * k * 0.6f, EaseInOut(Mathf.Clamp01(u * 1.4f))), up = up, fov = Mathf.Lerp(44, 28, e) };
                }
                case IntroShot.Rise:           // 수면 가까이 광각으로 낮게, 솟구치는 걸 올려다봄
                {
                    var pos = B + F * (R + k * 1.6f) + S * sg * k * 0.5f + up * 0.3f;
                    return new Pose { pos = pos, look = B + up * k * Mathf.Lerp(0.2f, 0.85f, EaseOut(u)), up = up, fov = Mathf.Lerp(72, 52, EaseInOut(u)) };
                }
                case IntroShot.Snap:           // 단계 줌: 멈췄다가 두 번에 걸쳐 뚝 당김
                {
                    var pos = B + F * Mathf.Max(R + k * 2.6f, 5.5f) + up * k * 0.7f + S * sg * k * 0.04f - F * u * 0.4f;
                    float fov = u < 0.2f ? 55 : u < 0.45f ? 34 : 24;
                    return new Pose { pos = pos, look = B + up * k * 0.78f, up = up, fov = fov };
                }
                case IntroShot.Pullback:       // 얼굴 망원에서 확 빠지며 광각으로 전신 공개 (끝은 정면)
                {
                    float e = EaseOut(u);
                    var a = B + F * (R + k * 0.6f) + up * k * 0.8f;
                    var b = B + F * (R * 0.8f + k * 2.3f) + up * k * 1.0f + S * sg * k * 0.08f;
                    return new Pose { pos = Vector3.Lerp(a, b, e), look = Vector3.Lerp(B + up * k * 0.8f, B + up * k * 0.45f, e), up = up, fov = Mathf.Lerp(22, 60, e) };
                }
                case IntroShot.Spiral:         // 나선: 돌면서 기울기를 풀며 망원으로 파고듦 (끝은 정면)
                {
                    float e = EaseInOut(u);
                    var dir = Quaternion.Euler(0, Mathf.Lerp(70f, 0f, e) * sg, 0) * F;
                    return new Pose { pos = B + dir * (R + k * Mathf.Lerp(2.2f, 1.2f, e)) + up * k * Mathf.Lerp(0.3f, 0.7f, e), look = B + up * k * 0.7f, up = up,
                                      fov = Mathf.Lerp(62, 30, e), roll = Mathf.Lerp(-18f, 0f, e) * sg };
                }
                case IntroShot.LookUp:         // 낮은 곳에서 올려다보며 확 당김
                {
                    float e = Expo(Mathf.Clamp01(u / 0.5f)) * 0.85f + 0.15f * u;
                    var a = B + F * (R + k * 2.6f) + up * 0.3f;
                    var b = B + F * (R + k * 1.9f) + up * k * 0.25f;
                    return new Pose { pos = Vector3.Lerp(a, b, e), look = B + up * k * 0.65f, up = up, fov = Mathf.Lerp(70, 40, e) };
                }
                case IntroShot.Push:           // 낮고 느리게 다가감 (압도)
                {
                    float e = EaseInOut(u);
                    var a = B + F * (R + k * 2.4f) + up * k * 0.3f;
                    var b = B + F * (R + k * 1.4f) + up * k * 0.45f;
                    bool two = targets.Count > 1;                                       // 쌍둥이: 둘 다 들어오게 덜 조임
                    return new Pose { pos = Vector3.Lerp(a, b, e), look = B + up * k * (two ? 0.62f : 0.72f), up = up, fov = Mathf.Lerp(56, two ? 34 : 28, e) };
                }
                case IntroShot.Vertigo:        // 버티고(돌리 줌): 보스 크기는 그대로 두고 카메라는 물러나며 화각을 조여 배경이 빨려 들어옴
                {
                    float e = EaseInOut(u);
                    float d = R + k * Mathf.Lerp(1.15f, 3.4f, e), frame = k * 1.35f;
                    float fov = 2f * Mathf.Atan(frame / (2f * d)) * Mathf.Rad2Deg;
                    return new Pose { pos = B + F * d + up * k * Mathf.Lerp(0.25f, 0.4f, e), look = B + up * k * 0.6f, up = up, fov = fov };
                }
                case IntroShot.HeroLow:        // 낮은 3/4 정면으로 확 당김 (고래처럼 앞뒤로 긴 보스용): 몸 가운데가 아니라 머리(앞 끝)를 겨눔
                {
                    float e = Expo(Mathf.Clamp01(u / 0.5f)) * 0.85f + 0.15f * u;
                    var head = B + F * (Reach * 0.72f) + up * k * 0.5f;           // 앞 끝에서 조금 안쪽 = 눈 · 입 언저리
                    var dir = Quaternion.Euler(0, 34f * sg, 0) * F;
                    var a = head + dir * (k * 2.6f) - up * k * 0.3f;
                    var b = head + dir * (k * 1.55f) - up * k * 0.15f;
                    var pose = new Pose { pos = Vector3.Lerp(a, b, e), look = head, up = up, fov = Mathf.Lerp(66, 42, e) };
                    if (def.faceLeft) ShiftLeft(ref pose, e);
                    return pose;
                }
                case IntroShot.HeroOrbit:      // 허리 높이로 가까이 돌며 손(총) → 얼굴로 시선을 올림 (최고의 플레이식)
                {
                    float e = EaseInOut(u);
                    var dir = Quaternion.Euler(0, Mathf.Lerp(-115f, -25f, e) * sg, 0) * F;
                    return new Pose { pos = B + dir * (R + k * Mathf.Lerp(1.35f, 1.1f, e)) + up * k * Mathf.Lerp(0.3f, 0.5f, e), look = B + up * k * Mathf.Lerp(0.5f, 0.7f, e), up = up,
                                      fov = Mathf.Lerp(52, 38, e), roll = Mathf.Lerp(10f, -3f, e) * sg };
                }
                case IntroShot.QuickDraw:      // 정면 낮은 앵글: 뽑는 순간 망원으로 확 조이며 다가감
                {
                    float e = Expo(Mathf.Clamp01(u / 0.3f)) * 0.8f + 0.2f * u;
                    var a = B + F * (R + k * 3.2f) + up * k * 0.3f;
                    var b = B + F * (R + k * 1.9f) + up * k * 0.42f;
                    var pose = new Pose { pos = Vector3.Lerp(a, b, e), look = B + up * k * 0.62f, up = up, fov = Mathf.Lerp(60, 24, e), roll = Mathf.Lerp(-6f, 0f, e) };
                    if (def.faceLeft) ShiftLeft(ref pose, e);
                    return pose;
                }
                case IntroShot.Duel:           // 쌍둥이 하나만: 바깥쪽 낮은 3/4에서 가까이, 휘두르는 쪽으로 조여 듦
                {
                    k = focus >= 0 ? focusH : H;
                    float e = EaseOut(u);
                    var mid = Vector3.zero; int n = 0;
                    foreach (var t in targets) if (t) { mid += t.transform.position; n++; }
                    mid = n > 0 ? mid / n : B;
                    float out_ = Vector3.Dot(B - mid, S) >= 0 ? 1f : -1f;              // 다른 녀석 반대편(바깥)에서
                    var dir = Quaternion.Euler(0, Mathf.Lerp(48f, 28f, e) * -out_, 0) * F;
                    var pos = B + dir * (k * Mathf.Lerp(1.5f, 1.05f, e)) + up * k * Mathf.Lerp(0.2f, 0.35f, e);
                    return new Pose { pos = pos, look = B + up * k * 0.55f, up = up, fov = Mathf.Lerp(58, 40, e), roll = Mathf.Lerp(8f, 2f, e) * out_ };
                }
                // 2026-11-22 사용자: "카메라 무빙이 너무 유사, 앵글이 더 다각도" → 정면 쪽이 아닌 각도 넷
                case IntroShot.BehindBoss:     // 보스 등 뒤 낮은 곳에서 어깨 위로 올라가며 고양이 쪽을 봄 (보스 실루엣이 앞에)
                {
                    float e = EaseInOut(u);
                    var pos = B - F * (R + k * Mathf.Lerp(1.4f, 0.9f, e)) + S * sg * k * Mathf.Lerp(0.9f, 0.45f, e) + up * k * Mathf.Lerp(0.45f, 1.1f, e);
                    return new Pose { pos = pos, look = B + F * k * 1.8f + up * k * Mathf.Lerp(0.5f, 0.25f, e), up = up, fov = Mathf.Lerp(62, 46, e), roll = Mathf.Lerp(6f, -2f, e) * sg };
                }
                case IntroShot.Profile:        // 완전 옆모습, 망원으로 나란히 미끄러짐
                {
                    float e = EaseInOut(u);
                    var pos = B + S * sg * (R + k * 2.3f) + up * k * Mathf.Lerp(0.35f, 0.55f, e) + F * Mathf.Lerp(-1f, 1f, e) * k * 0.5f;
                    return new Pose { pos = pos, look = B + up * k * 0.55f + F * Mathf.Lerp(-0.4f, 0.4f, e) * k * 0.3f, up = up, fov = Mathf.Lerp(36, 26, e) };
                }
                case IntroShot.WormEye:        // 바로 발밑 바닥에서 거의 수직으로 올려다봄, 천천히 돎
                {
                    float e = EaseInOut(u);
                    var pos = B + F * (R + k * Mathf.Lerp(0.3f, 0.5f, e)) + S * sg * k * 0.15f + up * 0.1f;
                    return new Pose { pos = pos, look = B + up * k * Mathf.Lerp(0.95f, 1.2f, e) - F * k * 0.15f, up = up, fov = Mathf.Lerp(86, 70, e), roll = Mathf.Lerp(-12f, 12f, e) * sg };
                }
                case IntroShot.HighOrbit:      // 높은 새의 눈으로 크게 돌며 내려옴
                {
                    float e = EaseInOut(u);
                    var dir = Quaternion.Euler(0, Mathf.Lerp(-140f, -20f, e) * sg, 0) * F;
                    return new Pose { pos = B + dir * (R + k * Mathf.Lerp(2.4f, 1.7f, e)) + up * k * Mathf.Lerp(2.3f, 1.4f, e), look = B + up * k * 0.4f, up = up, fov = Mathf.Lerp(52, 40, e) };
                }
                case IntroShot.Portrait:       // 얼굴로 파고들며 보스를 화면 왼쪽 1/3에 둠 (오른쪽은 카드 일러스트 자리, 2026-11-22 사용자)
                {
                    float e = Expo(Mathf.Clamp01(u / 0.45f)) * 0.85f + 0.15f * u;
                    var dir = Quaternion.Euler(0, Mathf.Lerp(35f, 14f, e) * sg, 0) * F;
                    var pos = B + dir * (R + k * Mathf.Lerp(2.0f, 1.0f, e)) + up * k * Mathf.Lerp(0.5f, 0.78f, e);
                    var head = B + up * k * 0.78f;
                    var right = Vector3.Cross(Vector3.up, (head - pos).normalized).normalized;
                    return new Pose { pos = pos, look = head + right * k * Mathf.Lerp(0.1f, 0.42f, e), up = up, fov = Mathf.Lerp(50, 32, e), roll = Mathf.Lerp(-8f, 0f, e) * sg };
                }
                // 2026-11-23 사용자: 와인더 잭 마지막 — 정면에서 총을 쏘면 카메라가 맞은 것처럼 빠르게 뒤로 날아가 바닥에 떨어지고 포스터
                //  0 ~ ShotHit: 정면 눈높이로 살짝 다가가며 망원 (총구가 카메라를 봄) → 맞음
                //  ~ 0.66: 뒤로 튕겨 날아감 (처음이 가장 빠름 · 위로 살짝 솟았다 떨어지는 포물선 · 하늘을 보며 한 바퀴 가까이 구름 · 광각으로 벌어짐)
                //  ~ 0.76: 바닥에 쿵 → 한 번 튀었다 옆으로 누움 / 끝: 누운 채 멀리 선 보스를 봄 (렌즈가 천천히 초점을 잡듯 화각이 조금 좁아짐)
                case IntroShot.ShotDown:
                {
                    float d = Mathf.Clamp(Effects.Flat(pp - B).magnitude * 0.3f, 2.4f, 3.6f);   // 너무 멀면 누운 화면에서 보스가 점처럼 작아짐
                    var p0 = B + F * (R + k * 1.5f) + up * k * 0.58f;
                    var gunLook = B + up * k * 0.55f;
                    float gy = B.y + 0.16f;
                    const float hitU = ShotHit, flyEnd = 0.66f, landEnd = 0.76f;
                    if (u < hitU)
                    {
                        float e = EaseOut(u / hitU);
                        return new Pose { pos = p0 - F * k * 0.3f * e, look = gunLook, up = up, fov = Mathf.Lerp(40, 30, e) };
                    }
                    var start = p0 - F * k * 0.3f;
                    if (u < flyEnd)
                    {
                        float w = (u - hitU) / (flyEnd - hitU);
                        float e = 1 - Mathf.Pow(1 - w, 2.2f);                                   // 맞는 순간이 가장 빠름
                        var pos = start + F * d * e;
                        pos.y = Mathf.Lerp(start.y, gy, w * w) + Mathf.Sin(Mathf.PI * w) * 1.1f;
                        float pitch = Mathf.Sin(Mathf.PI * Mathf.Min(1, w * 1.2f)) * 55f;         // 하늘을 보며 날아감
                        var dir = Quaternion.AngleAxis(-pitch, S) * -F;
                        return new Pose { pos = pos, look = pos + dir, up = up, fov = Mathf.Lerp(30, 76, e), roll = -300f * e };
                    }
                    var rest = start + F * d; rest.y = gy;
                    var lieLook = B + up * k * 0.4f;
                    if (u < landEnd)
                    {
                        float w = (u - flyEnd) / (landEnd - flyEnd);
                        var pos = rest + up * 0.28f * Mathf.Sin(Mathf.PI * w);                  // 한 번 튐
                        return new Pose { pos = pos, look = Vector3.Lerp(pos - F, lieLook, EaseOut(w)), up = up, fov = Mathf.Lerp(76, 70, w), roll = Mathf.Lerp(-300f, -294f, EaseOut(w)) };
                    }
                    {
                        float w = (u - landEnd) / (1 - landEnd);
                        return new Pose { pos = rest, look = lieLook, up = up, fov = Mathf.Lerp(70, 36, EaseInOut(w)), roll = Mathf.Lerp(-294f, -292f, w) };
                    }
                }
                default:                       // Crash: 정면에서 얼굴로 광각 → 망원으로 확 당김
                {
                    float e = Expo(Mathf.Clamp01(u / 0.4f)) * 0.85f + 0.15f * u;
                    var a = B + F * (R + k * 2.1f) + up * k * 0.65f + S * k * 0.35f * sg;
                    var b = B + F * (R + k * 1.3f) + up * k * 0.8f + S * k * 0.03f * sg;
                    return new Pose { pos = Vector3.Lerp(a, b, e), look = B + up * k * 0.68f, up = up, fov = Mathf.Lerp(70, 28, e) };
                }
            }
        }

        /// <summary>보스를 화면 왼쪽 1/3로 보냄 (오른쪽은 정지 카드 일러스트 자리)</summary>
        static void ShiftLeft(ref Pose p, float e)
        {
            var fwd = p.look - p.pos; float d = fwd.magnitude;
            var right = Vector3.Cross(Vector3.up, fwd / d).normalized;
            float half = Mathf.Tan(p.fov * 0.5f * Mathf.Deg2Rad) * d * (16f / 9f);
            p.look += right * half * 0.34f * e;
        }

        const float CutLen = 0.13f;

        IEnumerator RunShot(IntroShot s, float dur, int i, IntroCut cutIn, IntroCut cutOut, bool last)
        {
            subCut = -1;
            focus = -1;
            var fired = new bool[def.beats?.Length ?? 0];
            bool whooshed = false;
            if (i == 0) StartCoroutine(Letterbox(true));
            if (cutIn == IntroCut.Flash) Flash(def.noir ? def.accent : Color.white);
            for (float t = 0; t < dur; t += Time.unscaledDeltaTime)
            {
                if (SkipPressed()) yield break;
                float u = t / dur;

                // 이 컷의 비트 (보스 동작 · 이펙트 · 소리)
                if (targets.Count > 0)                           // 보스가 움직이면(등장 돌진 · 돌진 비트) 카메라 기준점도 수평으로 따라감
                {
                    Vector3 avg = Vector3.zero; int n = 0;
                    if (focus >= 0 && focus < targets.Count && targets[focus]) { avg = targets[focus].transform.position; n = 1; }
                    else foreach (var e in targets) if (e) { avg += e.transform.position; n++; }
                    if (n > 0) { avg /= n; B = new Vector3(avg.x, B.y, avg.z); }
                }
                for (int b = 0; b < fired.Length; b++)
                    if (!fired[b] && def.beats[b].shot == i && u >= def.beats[b].u) { fired[b] = true; def.beats[b].fx?.Invoke(this); }

                var pose = Eval(s, u, i);
                if (!LowShot(s))
                {
                    float flat = Effects.Flat(pose.look - pose.pos).magnitude;
                    pose.pos.y = Mathf.Max(pose.pos.y, pose.look.y + flat * 0.18f);
                }
                shakeAmt = Mathf.MoveTowards(shakeAmt, 0, Time.unscaledDeltaTime * 0.8f);

                // 전환: 휙 돌리기 (나가는 컷 끝 · 들어오는 컷 처음)
                float yaw = 0;
                float tail = dur - t, head = t;
                if (cutOut == IntroCut.Whip && !last && tail < CutLen)
                {
                    float w = 1 - tail / CutLen; yaw = 55f * w * w; streaks.alpha = w;
                    if (!whooshed) { whooshed = true; Snd(Sfx.Has("intro_whoosh") ? "intro_whoosh" : "dash", 0.6f); }
                }
                else if (cutIn == IntroCut.Whip && i > 0 && head < CutLen) { float w = 1 - head / CutLen; yaw = -55f * w * w; streaks.alpha = w; }
                else streaks.alpha = 0;
                if (streaks.alpha > 0) AnimateStreaks();
                // 사선 닦기
                if (cutOut == IntroCut.Wipe && !last && tail < CutLen) SetWipe(-1 + (1 - tail / CutLen));
                else if (cutIn == IntroCut.Wipe && i > 0 && head < CutLen) SetWipe(head / CutLen);
                else SetWipe(-2);

                var rot = Quaternion.LookRotation(pose.look - pose.pos, pose.up) * Quaternion.Euler(0, yaw, pose.roll);
                cam.transform.SetPositionAndRotation(pose.pos + Random.insideUnitSphere * shakeAmt * (H * 0.06f), rot);
                UpdateOccluders(pose.pos, pose.look, s == IntroShot.OverShoulder, last);
                cam.fieldOfView = pose.fov;
                yield return null;
            }
            streaks.alpha = 0;
            SetWipe(-2);
        }

        IEnumerator ReturnCamera(float dur)
        {
            if (!cam) yield break;
            var p0 = cam.transform.position; var r0 = cam.transform.rotation; float f0 = cam.fieldOfView;
            for (float t = 0; t < dur; t += Time.unscaledDeltaTime)
            {
                float e = EaseInOut(t / dur);
                if (CameraFollow.GetFollowPose(out var fp, out var fr))
                    cam.transform.SetPositionAndRotation(Vector3.Lerp(p0, fp, e), Quaternion.Slerp(r0, fr, e));
                cam.fieldOfView = Mathf.Lerp(f0, savedFov, e);
                yield return null;
            }
            cam.fieldOfView = savedFov;
            if (CameraFollow.GetFollowPose(out var pos, out var rot)) cam.transform.SetPositionAndRotation(pos, rot);
        }

        IEnumerator GlitchFlicker(float dur)
        {
            var models = new List<Renderer>();
            foreach (var e in targets) if (e) foreach (var r in e.GetComponentsInChildren<Renderer>()) if (r.enabled) { models.Add(r); flickered.Add(r); }
            for (float t = 0; t < dur; t += Time.unscaledDeltaTime)
            {
                bool on = Random.value > 0.25f;
                foreach (var r in models) if (r) r.enabled = on;
                yield return null;
            }
            foreach (var r in models) if (r) { r.enabled = true; flickered.Remove(r); }
        }
        readonly HashSet<Renderer> flickered = new();

        static Color Alpha(Color c, float a) { c.a = a; return c; }

        // ---------- 공통 UI (레터박스 · 섬광 · 휙 줄무늬 · 사선 닦기 · 스킵 안내) ----------
        Canvas canvas;
        RectTransform root, letterTop, letterBot, wipe, freezeRoot;
        Image flash;
        CanvasGroup streaks;
        readonly List<RectTransform> streakLines = new();

        void BuildUi()
        {
            if (canvas) Destroy(canvas.gameObject);
            UIKit.Init();
            var go = new GameObject("BossIntroCanvas", typeof(Canvas), typeof(CanvasScaler));
            go.transform.SetParent(transform, false);
            canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 900;
            var sc = go.GetComponent<CanvasScaler>();
            sc.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            sc.referenceResolution = new Vector2(1920, 1080);
            sc.matchWidthOrHeight = 0.5f;
            root = (RectTransform)go.transform;

            var sg = new GameObject("Streaks", typeof(RectTransform), typeof(CanvasGroup));
            sg.transform.SetParent(root, false);
            Full((RectTransform)sg.transform);
            streaks = sg.GetComponent<CanvasGroup>(); streaks.alpha = 0;
            for (int i = 0; i < 26; i++)
            {
                var l = Rect("L", sg.transform, new Color(1, 1, 1, Random.Range(0.25f, 0.7f)));
                l.sizeDelta = new Vector2(Random.Range(300, 900), Random.Range(2, 7));
                l.anchoredPosition = new Vector2(Random.Range(-1100f, 1100f), Random.Range(-540f, 540f));
                streakLines.Add(l);
            }

            wipe = Rect("Wipe", root, new Color(0.04f, 0.03f, 0.05f, 1));
            wipe.sizeDelta = new Vector2(2200, 2600);
            wipe.localRotation = Quaternion.Euler(0, 0, -16);
            SetWipe(-2);

            letterTop = Rect("LetterTop", root, Color.black); letterTop.anchorMin = new Vector2(0, 1); letterTop.anchorMax = new Vector2(1, 1); letterTop.pivot = new Vector2(0.5f, 1); letterTop.sizeDelta = Vector2.zero;
            letterBot = Rect("LetterBot", root, Color.black); letterBot.anchorMin = new Vector2(0, 0); letterBot.anchorMax = new Vector2(1, 0); letterBot.pivot = new Vector2(0.5f, 0); letterBot.sizeDelta = Vector2.zero;

            freezeRoot = new GameObject("Freeze", typeof(RectTransform)).GetComponent<RectTransform>();
            freezeRoot.SetParent(root, false); Full(freezeRoot);

            flash = Rect("Flash", root, new Color(1, 1, 1, 0)).GetComponent<Image>(); Full(flash.rectTransform);

            if (skippable) BuildSkipPrompt();
        }

        // ---------- 건너뛰기 안내 (2026-11-22 사용자: 게임에 나오는 것처럼 아이콘이랑 같이 오른쪽 위에) ----------
        RectTransform skipRoot, skipCap;
        CanvasGroup skipGroup;
        Image skipFill;

        void BuildSkipPrompt()
        {
            var g = new GameObject("SkipPrompt", typeof(RectTransform), typeof(CanvasGroup));
            skipRoot = (RectTransform)g.transform; skipRoot.SetParent(root, false);
            skipRoot.anchorMin = skipRoot.anchorMax = skipRoot.pivot = new Vector2(1, 1);
            skipRoot.anchoredPosition = new Vector2(-40, -34); skipRoot.sizeDelta = new Vector2(360, 54);
            skipGroup = g.GetComponent<CanvasGroup>(); skipGroup.alpha = 0;

            // 키캡: 아래 두께(어두운 판) + 윗면 + 테두리 + 누르면 차는 띠
            var basePlate = Rect("CapBase", skipRoot, new Color(0.03f, 0.03f, 0.05f, 0.95f)).GetComponent<Image>();
            basePlate.sprite = Rounded(); basePlate.type = Image.Type.Sliced;
            var br = basePlate.rectTransform; br.anchorMin = br.anchorMax = br.pivot = new Vector2(1, 0.5f); br.sizeDelta = new Vector2(124, 48); br.anchoredPosition = new Vector2(0, -4);
            var cap = Rect("Cap", skipRoot, new Color(0.17f, 0.17f, 0.2f, 0.95f)).GetComponent<Image>();
            cap.sprite = Rounded(); cap.type = Image.Type.Sliced;
            skipCap = cap.rectTransform; skipCap.anchorMin = skipCap.anchorMax = skipCap.pivot = new Vector2(1, 0.5f); skipCap.sizeDelta = new Vector2(124, 46); skipCap.anchoredPosition = Vector2.zero;
            var ol = cap.gameObject.AddComponent<Outline>(); ol.effectColor = new Color(1, 1, 1, 0.55f); ol.effectDistance = new Vector2(1.5f, -1.5f);
            skipFill = Rect("Fill", skipCap, Alpha(def.accent, 0.0f)).GetComponent<Image>(); skipFill.sprite = Rounded(); skipFill.type = Image.Type.Sliced;
            Full(skipFill.rectTransform);
            var key = new GameObject("Key", typeof(RectTransform), typeof(Text)).GetComponent<Text>();
            key.transform.SetParent(skipCap, false); Full(key.rectTransform);
            key.font = UIKit.BodyBold ? UIKit.BodyBold : UIKit.Title; key.text = "SPACE"; key.fontSize = 22; key.alignment = TextAnchor.MiddleCenter; key.color = Color.white; key.raycastTarget = false;
            // 글자: 건너뛰기 + ▶▶
            var lab = new GameObject("Label", typeof(RectTransform), typeof(Text)).GetComponent<Text>();
            lab.transform.SetParent(skipRoot, false);
            var lr = lab.rectTransform; lr.anchorMin = lr.anchorMax = lr.pivot = new Vector2(1, 0.5f); lr.sizeDelta = new Vector2(220, 46); lr.anchoredPosition = new Vector2(-140, 0);
            lab.font = UIKit.Title; lab.text = "건너뛰기  ▶▶"; lab.fontSize = 26; lab.alignment = TextAnchor.MiddleRight; lab.color = new Color(1, 1, 1, 0.92f); lab.raycastTarget = false;
            var sh = lab.gameObject.AddComponent<Shadow>(); sh.effectColor = new Color(0, 0, 0, 0.7f); sh.effectDistance = new Vector2(2, -2);
            StartCoroutine(SkipPulse());
        }

        IEnumerator SkipPulse()
        {
            for (float t = 0; skipGroup; t += Time.unscaledDeltaTime)
            {
                skipGroup.alpha = Mathf.Clamp01(t / 0.3f) * (0.82f + 0.18f * Mathf.Sin(t * 4f));
                yield return null;
            }
        }

        /// <summary>스페이스를 누른 순간: 키캡이 눌리며 한 번 차오름</summary>
        IEnumerator SkipPress()
        {
            if (!skipCap) yield break;
            for (float t = 0; t < 0.12f; t += Time.unscaledDeltaTime)
            {
                float u = t / 0.12f;
                skipCap.anchoredPosition = new Vector2(0, -3f * Mathf.Sin(u * Mathf.PI));
                if (skipFill) skipFill.color = Alpha(def.accent, 0.8f * (1 - u));
                yield return null;
            }
        }

        static Sprite roundedSpr;
        static Sprite Rounded()
        {
            if (roundedSpr) return roundedSpr;
            const int n = 64; const float rad = 16f;
            var tex = new Texture2D(n, n, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            var px = new Color32[n * n];
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    float dx = Mathf.Max(0, Mathf.Max(rad - x - 0.5f, x + 0.5f - (n - rad))), dy = Mathf.Max(0, Mathf.Max(rad - y - 0.5f, y + 0.5f - (n - rad)));
                    float a = Mathf.Clamp01(rad - Mathf.Sqrt(dx * dx + dy * dy) + 0.5f);
                    px[y * n + x] = new Color32(255, 255, 255, (byte)(a * 255));
                }
            tex.SetPixels32(px); tex.Apply();
            return roundedSpr = Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), 100, 0, SpriteMeshType.FullRect, new Vector4(20, 20, 20, 20));
        }

        static RectTransform Rect(string name, Transform parent, Color c)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var img = go.GetComponent<Image>(); img.color = c; img.raycastTarget = false;
            return (RectTransform)go.transform;
        }

        static void Full(RectTransform r) { r.anchorMin = Vector2.zero; r.anchorMax = Vector2.one; r.offsetMin = r.offsetMax = Vector2.zero; }

        /// <summary>-1 = 왼쪽 밖 · 0 = 화면 덮음 · 1 = 오른쪽 밖 (그 밖 = 숨김)</summary>
        void SetWipe(float x)
        {
            if (!wipe) return;
            wipe.gameObject.SetActive(x >= -1.01f && x <= 1.01f);
            wipe.anchoredPosition = new Vector2(x * 2300f, 0);
        }

        void AnimateStreaks()
        {
            foreach (var l in streakLines)
            {
                var p = l.anchoredPosition; p.x -= 9000f * Time.unscaledDeltaTime;
                if (p.x < -1500) p.x += 3000;
                l.anchoredPosition = p;
            }
        }

        IEnumerator Letterbox(bool on)
        {
            float from = letterTop.sizeDelta.y, to = on ? 118f : 0f;
            for (float t = 0; t < 0.22f; t += Time.unscaledDeltaTime)
            {
                float h = Mathf.Lerp(from, to, EaseOut(t / 0.22f));
                letterTop.sizeDelta = letterBot.sizeDelta = new Vector2(0, h);
                yield return null;
            }
            letterTop.sizeDelta = letterBot.sizeDelta = new Vector2(0, to);
        }

        IEnumerator FadeImg(Image img, float a0, float a1, float dur, Color c)
        {
            for (float t = 0; t < dur && img; t += Time.unscaledDeltaTime)
            {
                img.color = new Color(c.r, c.g, c.b, Mathf.Lerp(a0, a1, t / dur));
                yield return null;
            }
            if (img) img.color = new Color(c.r, c.g, c.b, a1);
        }

        void HideHud(bool hide)
        {
            if (hide)
            {
                hidden.Clear();
                foreach (var c in FindObjectsByType<Canvas>(FindObjectsSortMode.None))
                    if (c.isRootCanvas && c.enabled && c.renderMode == RenderMode.ScreenSpaceOverlay && c.transform.root != transform) { c.enabled = false; hidden.Add(c); }
            }
            else
            {
                foreach (var c in hidden) if (c) c.enabled = true;
                hidden.Clear();
            }
        }

        void Cleanup()
        {
            Time.timeScale = 1f;
            StopAllCoroutines();
            RestoreGround();
            if (canvas) Destroy(canvas.gameObject);
            canvas = null; streakLines.Clear();
            HideHud(false);
            RestoreOccluders();
            if (cam) cam.fieldOfView = savedFov;
            CameraFollow.Hold = false;
            Music.Duck(1f, 0.3f);
            foreach (var g in doubles) if (g) Destroy(g);
            doubles.Clear();
            foreach (var r in flickered) if (r) r.enabled = true;
            flickered.Clear();
            foreach (var an in rootMotion) if (an) an.applyRootMotion = true;
            rootMotion.Clear();
            Playing = false;
        }

        void OnDestroy() { if (Playing) { Time.timeScale = 1f; CameraFollow.Hold = false; HideHud(false); Playing = false; } }
    }

    /// <summary>글자를 오른쪽으로 기울임 (영어 타이포 이탤릭 느낌)</summary>
    public class SkewText : BaseMeshEffect
    {
        public float skew = 0.2f;
        public override void ModifyMesh(VertexHelper vh)
        {
            if (!IsActive()) return;
            var v = new UIVertex();
            for (int i = 0; i < vh.currentVertCount; i++)
            {
                vh.PopulateUIVertex(ref v, i);
                v.position.x += v.position.y * skew;
                vh.SetUIVertex(v, i);
            }
        }
    }
}
