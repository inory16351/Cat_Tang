// 3장 (신기루 사막 = 거울 세계) 방 꾸미기 + 오브젝트 2종 (docs/Cat_Tang_레벨_디자인.md 챕터 3 · 수치 기준 (챕터 3))
// - 장소(스테이지)별 벽면 · 지형지물 · 바닥: 1 거울 호수 기슭(사암 기슭) → 2 거울 속 서부 마을(1장 건물 좌우 반전) → 3 떠도는 섬(섬 가장자리) → 4 뒤집힌 하늘(구름 둑 · 구름 바닥)
// - 기억 수정: 방마다 2~4, 쏘면 깨지며 반경 3m 빛가루 폭발 · 적 50 · 고양이 18 (주황 ↔ 노랑 테두리 DangerEdge)
// - 분수 샘: 방마다 1~2, 쏘면 반경 1.5m 원형 물기둥 벽이 3초 솟아 양쪽 탄을 막음, 다시 쓰려면 8초
// - 신기루 엄폐물(모래 바위 · 무너진 기둥 · 선 거울): 내구 40. 기억 수정 · 분수 샘은 몬스터 공격에도 발동(PropHit · 적 탄)
// 키트(Resources/Kit/Ch3)는 블렌더 + Codex 모델 (Art/tools/blender_kit_ch3_*.py, 명세 Art/tools/specs/kit_ch3_*_spec.md)
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CatTang
{
    public static class Ch3Kit
    {
        public static bool Is(ChapterDef ch) => ch != null && ch.Id == 3 && Kit.Has(ch, "MemoryCrystal");
        public static readonly Vector2 BossRoomSize = new(48, 46);          // 거울 제단 원형 지름 40m
        public static readonly Vector2 TheaterSize = new(36, 32);           // 중간 보스 거울 극장 (일반 방 28×24보다 크게)

        static int Stage => RunManager.Instance ? RunManager.Instance.stageIndex : 0;
        static bool BossRoom => RunManager.Instance && RunManager.Instance.IsBossRoom;
        static bool MidBossRoom => RunManager.Instance && RunManager.Instance.IsMidBossRoom;

        /// <summary>1장 키트를 빌려 좌우 반전해 쓰는 조각 이름 앞에 붙는 표시 (거울 속 서부 마을). Kit.Prefab이 Kit/Ch1에서 찾음</summary>
        public const string Ch1Mirror = "@1/";

        // ---------- 바닥 ----------
        public static string GroundName() => !BossRoom && Stage >= 3 ? "GroundCloud" : "Ground";

        public static readonly string[] GroundBits = { "Pebbles3", "CrystalBits", "Ripple" };
        public static float[] GroundWeights() => !BossRoom && Stage >= 3 ? new[] { 0f, 1f, 0f } : !BossRoom && Stage == 2 ? new[] { 0.8f, 1.4f, 0.4f } : new[] { 0.35f, 1.2f, 0.9f };

        // ---------- 벽면 (Kit.BuildingRow가 씀). 스테이지 1(호수 기슭)은 Cliff(사암 기슭)라 여기 안 옴 ----------
        public static (string[] names, float[] widths) Facades()
        {
            if (BossRoom || MidBossRoom) return (new[] { "Facade_Colonnade", "Facade_Arch", "Facade_MirrorFrame" }, new[] { 8f, 6f, 4f });
            switch (Stage)
            {
                case 1:   // 거울 속 서부 마을: 1장 건물 정면 8종을 좌우 반전 + 사이사이 거울 액자 벽
                    return (new[] { Ch1Mirror + "Facade_Store", Ch1Mirror + "Facade_Hotel", Ch1Mirror + "Facade_Narrow", Ch1Mirror + "Facade_Adobe", Ch1Mirror + "Facade_Bank", Ch1Mirror + "Facade_Blue", "Facade_MirrorFrame" },
                            new[] { 5f, 6f, 3f, 4f, 4f, 4f, 4f });
                case 2: return (new[] { "Facade_Island", "Facade_Colonnade" }, new[] { 6f, 8f });
                default:
                    if (Stage >= 3) return (new[] { "Facade_Cloud" }, new[] { 6f });
                    return (new[] { "Facade_Colonnade", "Facade_Arch", "Facade_MirrorFrame" }, new[] { 8f, 6f, 4f });
            }
        }

        /// <summary>떠도는 섬 · 뒤집힌 하늘의 벽은 키 작은 가장자리라 남쪽 벽을 덜 낮춤</summary>
        public static float SouthWallMul() => Stage == 2 || Stage >= 3 ? 0.85f : 0.6f;

        // ---------- 큰 지형지물 (RoomBuilder.AddShape가 씀) ----------
        public static string[] Landmarks()
        {
            switch (Stage)
            {
                case 0: return new[] { "LM_CrystalSpire", "LM_MirrorGate", "LM_Hourglass", "LM_ArchRuin" };   // 모래 언덕 뺌 (2026-11-18 사용자: 모래사장 말고 거울)
                case 1: return new[] { "LM_ArchRuin", "LM_Obelisk", "LM_MirrorGate", "LM_Hourglass" };
                case 2: return new[] { "LM_FloatRock", "LM_CrystalSpire", "LM_ArchRuin", "LM_Obelisk" };
                default: return new[] { "LM_FloatRock", "LM_MirrorGate", "LM_CrystalSpire", "LM_Hourglass" };
            }
        }

        // ---------- 소품 · 오브젝트 (RoomBuilder.KitProps 대신) ----------
        public static void Props(ChapterDef ch, RoomBuilder.RoomInfo info, float hw, float hd, System.Random rnd, bool boss)
        {
            var root = info.root;
            if (boss)
            {
                // 거울 제단 (거울 고래): 지름 40m 원형 테두리(Altar) + 보이지 않는 원형 경계 + 둘레에 떠 있는 거울 조각 8개(반사 빛줄기용, 고정 · 쏘면 15° 회전)
                Kit.Place(ch, "Altar", root, Vector3.zero, 0f, 1f, false);
                Ch2Kit.ArenaFence(root, Ch3Boss.ArenaR);
                for (int i = 0; i < 8; i++)
                {
                    float a = (22.5f + i * 45f) * Mathf.Deg2Rad;
                    var p = info.center + new Vector3(Mathf.Sin(a), 0, Mathf.Cos(a)) * 15.5f;
                    MirrorShard.SpawnAnchored(ch, info, p, i * 45f + 22.5f + 90f + rnd.Next(-25, 25));
                }
                // 모서리 바깥(원 밖)은 장식: 오벨리스크
                for (int sx = -1; sx <= 1; sx += 2)
                    for (int sz = -1; sz <= 1; sz += 2)
                        Kit.Place(ch, "LM_Obelisk", root, new Vector3(sx * (hw - 2.4f), 0, sz * (hd - 2.4f)), Quaternion.LookRotation(new Vector3(-sx, 0, -sz)).eulerAngles.y);
                return;
            }

            // 후보 자리 (가운데 · 문 앞 통로 · 막힌 곳 비움)
            var spots = new List<Vector3>();
            for (float x = -hw + 3f; x <= hw - 3f; x += 3f)
                for (float z = -hd + 3f; z <= hd - 3f; z += 3f)
                {
                    if (Mathf.Abs(x) < 3.2f && Mathf.Abs(z) < 3.2f) continue;
                    if (Mathf.Abs(x) < 2.6f || Mathf.Abs(z) < 2.6f) continue;
                    if (info.Blocked(info.center + new Vector3(x, 0, z), 1.6f)) continue;
                    spots.Add(new Vector3(x, 0, z));
                }
            for (int i = spots.Count - 1; i > 0; i--) { int j = rnd.Next(i + 1); (spots[i], spots[j]) = (spots[j], spots[i]); }
            int si = 0;
            Vector3 Next() => si < spots.Count ? spots[si++] + new Vector3((float)rnd.NextDouble() - 0.5f, 0, (float)rnd.NextDouble() - 0.5f) * 0.8f : Vector3.zero;

            // 기억 수정 2~4
            int crystals = rnd.Next(2, 5);
            for (int k = 0; k < crystals && si < spots.Count; k++)
            {
                var go = Kit.Place(ch, "MemoryCrystal", root, Next(), rnd.Next(360));
                if (go) MemoryCrystal.Make(go);
            }
            // 분수 샘 1~2
            int fountains = rnd.Next(1, 3);
            for (int k = 0; k < fountains && si < spots.Count; k++)
            {
                var go = Kit.Place(ch, "Fountain", root, Next(), rnd.Next(4) * 90f);
                if (go) FountainWall.Make(go);
            }
            // 신기루 엄폐물 3~5 (모래 바위 · 무너진 기둥 · 선 거울), 내구 40. 선 거울은 거울 너머 검객이 튀어나오는 자리이기도 함
            string[] kinds = Stage >= 3 ? new[] { "FallenPillar", "StandMirror", "StandMirror" } : new[] { "FallenPillar", "StandMirror", "StandMirror", "SandRock" };
            int cover = rnd.Next(3, 6);
            for (int k = 0; k < cover && si < spots.Count; k++)
            {
                string kind = kinds[rnd.Next(kinds.Length)];
                var go = Kit.Place(ch, kind, root, Next(), rnd.Next(4) * 90f + rnd.Next(-12, 12));
                if (go) Breakable.Make(go, CoverHp);
            }
        }

        public const float CoverHp = 40f;

        public static Health MakeHp(GameObject go, float hp = 1f)
        {
            var h = go.AddComponent<Health>();
            h.maxHp = hp;
            h.ResetHp(hp);
            return h;
        }
    }

    /// <summary>기억 수정 (3장): 쏘면(몬스터 공격 포함) 깨지며 반경 3m 빛가루 폭발 · 적 50 · 고양이 18, 근처 수정 · 폭발물 연쇄</summary>
    public class MemoryCrystal : MonoBehaviour
    {
        public const float Radius = 3f, EnemyDmg = 50f, PlayerDmg = 18f, Fuse = 0.18f;
        bool done;

        public static void Make(GameObject go)
        {
            Ch3Kit.MakeHp(go);
            go.AddComponent<MemoryCrystal>();
            if (!go.GetComponent<DangerEdge>()) DangerEdge.Add(go);
        }

        void Awake() => GetComponent<Health>().OnDeath += _ => { if (!done) { done = true; StartCoroutine(Shatter()); } };

        IEnumerator Shatter()
        {
            // 짧은 금 가는 떨림 → 터짐 (연쇄가 한 번에 다 터지지 않고 차례로 번지게)
            foreach (var c in GetComponentsInChildren<Collider>()) c.enabled = false;
            var at = transform.position;
            Enemy.Fx3("MirrorFlash", at + Vector3.up * 1f, 0.7f);
            for (float t = 0; t < Fuse; t += Time.deltaTime)
            {
                transform.position = at + Random.insideUnitSphere * 0.05f;
                yield return null;
            }
            transform.position = at;
            var ground = new Vector3(at.x, 0.02f, at.z);
            foreach (var en in Enemy.All.ToArray())
                if (en && en.health.Alive && Effects.Flat(en.transform.position - ground).magnitude <= Radius + 0.4f) en.health.TakeDamage(EnemyDmg);
            var p = PlayerController.Instance;
            if (p && p.health.Alive && Effects.Flat(p.transform.position - ground).magnitude <= Radius + 0.3f) p.health.TakeDamage(PlayerDmg);
            Enemy.Fx3("GlassBurst", ground + Vector3.up * 0.9f, 1.4f);
            Enemy.Fx3("Stardust", ground + Vector3.up * 0.4f, 1.6f);
            // 그을음 연기 없이 분홍 · 금빛 반짝임 (수정이라)
            FxBurst.Spawn(ground + Vector3.up * 0.8f, Radius * 1.1f, FxBurst.Kind.Explosion, new Color(1f, 0.72f, 0.92f));
            FxBurst.Spawn(ground + Vector3.up * 1.0f, Radius * 0.9f, FxBurst.Kind.Gold, new Color(1f, 0.9f, 0.6f));
            FxBurst.Spawn(ground + Vector3.up * 0.5f, Radius * 0.8f, FxBurst.Kind.Spark, new Color(0.8f, 0.95f, 1f));
            CameraFollow.Shake(0.13f);
            PropHit.Area(ground, Radius, EnemyDmg);               // 근처 수정 · 분수 · 엄폐물도
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// 분수 샘 (3장, 2026-11-18 사용자: 물기둥을 지속시키지 말고 물이 한 번 폭발하듯): 쏘면(몬스터 공격 포함) 물이 한 번 펑 솟구쳐 터짐.
    /// 반경 3m 안 적 피해 25 + 바깥으로 밀쳐냄, 적 탄은 물에 씻겨 사라짐, 고양이는 피해 없이 밀려남. 다시 쓰려면 6초.
    /// 분수 자체는 부서지지 않음(체력이 줄면 다시 채움). 겉모습 = 3D 물 WaterBurst (블렌더 물 메시 + CatTang/Water 셰이더) + 바닥 물결 고리
    /// </summary>
    public class FountainWall : MonoBehaviour
    {
        public const float Radius = 3f, EnemyDmg = 25f, EnemyPush = 9f, PlayerPush = 6f, Cooldown = 6f;
        static readonly Color Water = new(0.35f, 0.85f, 0.95f);
        Health hp;
        float readyAt;

        public static void Make(GameObject go)
        {
            var h = Ch3Kit.MakeHp(go, 1e6f);
            go.AddComponent<FountainWall>().hp = h;
        }

        void Awake()
        {
            if (!hp) hp = GetComponent<Health>();
            hp.OnDamaged += _ => { hp.ResetHp(hp.maxHp); if (Time.time >= readyAt) Burst(); };
        }

        void Burst()
        {
            readyAt = Time.time + Cooldown;
            var at = transform.position;
            var ground = new Vector3(at.x, 0.03f, at.z);
            // 겉모습 (2026-11-18 사용자: 구운 3D가 안 어울림 → 구운 프레임(움직임) + 게임 화면(그림체)을 같이 넣어 Codex가 다시 그린 시트 fx_c3water,
            // docs/images/fx_src/prompts/c3_water.txt → fx_sheet_cut.py). 프레임 아래 3%가 바닥 → 가운데를 위로 올려 밑동이 분수에 닿게. 없으면 구운 3D
            // → 2026-11-18 사용자: 그림도 물 같지 않음 → 진짜 3D 물 WaterBurst (블렌더 메시 + CatTang/Water 셰이더)가 먼저, 없을 때만 그림
            const float Size = 5.5f;
            if (!WaterBurst.Spawn(ground)
                && !FxFlipbook.Spawn("fx_c3water", ground + Vector3.up * Size * 0.47f, Vector3.zero, Size, 0.7f, false)
                && !FxFlipbook.Spawn("bk_c3water", ground + Vector3.up * Size * 0.4f, Vector3.zero, Size, 0.75f, false))
                Enemy.Fx3("MirrorFlash", ground + Vector3.up, 1.2f);
            RippleRing.Spawn(ground, 9f, 0f, Radius + 0.6f, 0.35f, Water);
            RippleRing.Spawn(ground, 6f, 0f, Radius * 0.8f, 0.2f, new Color(0.85f, 1f, 1f), 0.4f);
            CameraFollow.Shake(0.08f);
            // 적: 피해 + 밀쳐냄
            foreach (var en in Enemy.All.ToArray())
            {
                if (!en || !en.health.Alive) continue;
                var d = Effects.Flat(en.transform.position - ground);
                if (d.magnitude > Radius + 0.4f) continue;
                en.health.TakeDamage(EnemyDmg);
                en.health.Knockback((d.sqrMagnitude > 1e-4f ? d.normalized : Vector3.forward) * EnemyPush);
            }
            // 적 탄: 물에 씻겨 사라짐
            EnemyProjectile.WashAway(ground, Radius);
            // 고양이: 피해 없이 밀려남
            var p = PlayerController.Instance;
            if (p && p.health.Alive)
            {
                var d = Effects.Flat(p.transform.position - ground);
                if (d.magnitude <= Radius) p.health.Knockback((d.sqrMagnitude > 1e-4f ? d.normalized : Vector3.back) * PlayerPush);
            }
        }
    }
}
