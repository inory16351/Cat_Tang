// 5장 최종 보스 '시간의 주인' 대낫 연출 (2026-11-20 사용자: 마지막 보스는 압도적인 포스 — 참고 그림 · 영상: 거대한 소용돌이 베기,
//   흰 심지 → 뜨거운 몸통 띠, 칼날 뒤를 휩쓰는 검은 잉크 연기 띠, 흩날리는 검은 깃털 · 조각, 휘두른 결을 따라 날리는 불티, 연기로 흩어지며 사라짐)
// 5장 색 규칙(자홍 · 보라 = 해로움, 금 = 고양이에게 이로움)에 맞춰 불꽃 주황 대신 흰빛 → 자홍 → 짙은 보라 + 검은 잉크.
// 겹(위에서 아래로 그림): ① 낫 섬광 리본(FlashRibbon, 보스 코드가 따로 부름) ② 흰 심지 호(SlashCore) ③ 자홍 몸통 호 두 겹(조금 늦게 · 크게)
//   ④ 검은 잉크 연기 호(Void, 가장 크게 · 늦게 · 오래, 칼날 뒤를 휩씀) ⑤ 휘두른 호를 따라 접선 방향으로 날리는 불티(DashFx 불티) ⑥ 검은 깃털 · 보라 조각(DashFx, 돌며 떨어짐)
//   ⑦ 바닥 충격 고리 + 카메라 흔들림. 큰 순간에만 씀 (대낫 베기 · 유령 돌진 베기 끝 · 3페이즈 진입)
using UnityEngine;

namespace CatTang
{
    public static class ReaperFx
    {
        static readonly Color Ember = new(1f, 0.45f, 0.85f), EmberHot = new(1f, 0.85f, 0.95f), Feather = new(0.22f, 0.12f, 0.32f), Shard = new(0.55f, 0.25f, 0.95f);

        /// <summary>대낫 큰 베기. at = 보스 발밑, dir = 벤 방향(호의 가운데), radius = 베기 반지름, cw = 시계 방향으로 휘두르는지</summary>
        public static void BigSlash(Vector3 at, Vector3 dir, float radius, bool cw = true)
        {
            dir.y = 0; if (dir.sqrMagnitude < 1e-4f) dir = Vector3.forward; dir.Normalize();
            float h = 1.1f;
            // ②~④ 호 겹: 심지는 빠르고 작게, 몸통은 조금 늦게 크게, 잉크 연기는 가장 크고 오래 (칼날 뒤를 휩쓰는 검은 띠)
            StylizedFx.Slash(at, dir, radius * 0.92f, StylizedFx.SlashCore, h, cw, 0.22f);
            StylizedFx.Slash(at, dir, radius * 1.0f, StylizedFx.SlashStreak, h + 0.15f, cw, 0.3f);
            StylizedFx.Slash(at, dir, radius * 1.12f, StylizedFx.Magenta.With(sweepTime: 0.3f, tailFade: 1.6f), h + 0.35f, cw, 0.42f);
            StylizedFx.Slash(at, dir, radius * 1.3f, StylizedFx.Void.With(sweepTime: 0.35f, tailFade: 1.2f, opacity: 0.85f), h + 0.6f, cw, 0.7f);
            // ⑤ 불티: 호를 따라 고르게, 날아가는 방향 = 호의 접선(휘두른 쪽) + 바깥
            var side = Vector3.Cross(Vector3.up, dir) * (cw ? 1f : -1f);
            for (int i = 0; i < 26; i++)
            {
                float a = Mathf.Lerp(-80f, 80f, i / 25f) + Random.Range(-6f, 6f);
                var radial = Quaternion.Euler(0, a, 0) * dir;
                var tangent = Vector3.Cross(Vector3.up, radial) * (cw ? -1f : 1f);
                var p = at + radial * radius * Random.Range(0.75f, 1.1f) + Vector3.up * Random.Range(0.5f, 1.6f);
                var v = (tangent * Random.Range(6f, 12f) + radial * Random.Range(1f, 4f) + Vector3.up * Random.Range(-0.5f, 2f));
                float s = Random.Range(0.06f, 0.12f);
                DashFx.Emit(DashFx.M("fx_spike_0"), FxMesh.Kind.Spike, p, v, new Vector3(s, s, s * 5f), Random.Range(0.25f, 0.45f), Random.value < 0.35f ? EmberHot : Ember, false, 2.5f, 0f, i * 0.004f);
            }
            // ⑥ 검은 깃털 · 보라 조각: 휘두른 쪽으로 흩날리며 돌다 천천히 떨어짐
            for (int i = 0; i < 14; i++)
            {
                float a = Random.Range(-70f, 70f);
                var radial = Quaternion.Euler(0, a, 0) * dir;
                var p = at + radial * radius * Random.Range(0.5f, 1f) + Vector3.up * Random.Range(0.6f, 2f);
                var v = side * Random.Range(-1f, 4f) + radial * Random.Range(2f, 6f) + Vector3.up * Random.Range(1f, 4f);
                bool feather = i % 2 == 0;
                float s = feather ? Random.Range(0.28f, 0.42f) : Random.Range(0.16f, 0.26f);
                DashFx.Emit(DashFx.M(feather ? "fx_shard_1" : "fx_glass_0"), FxMesh.Kind.Ball, p, v, feather ? new Vector3(s * 0.35f, s * 0.12f, s) : Vector3.one * s,
                    Random.Range(0.7f, 1.1f), feather ? Feather : Shard, true, 2.2f, Random.Range(200f, 520f));
            }
            // ⑦ 바닥 충격 + 흔들림
            StylizedFx.Shockwave(at + Vector3.up * 0.05f, radius * 0.9f, StylizedFx.Void, 0.5f);
            CameraFollow.Shake(0.28f);
        }

        /// <summary>유령 돌진 베기 끝 (미끄러져 멈춘 자리): 잉크 연기 폭발 + 깃털 · 불티 · 충격 고리</summary>
        public static void DashEnd(Vector3 at, Vector3 dir)
        {
            dir.y = 0; if (dir.sqrMagnitude < 1e-4f) dir = Vector3.forward; dir.Normalize();
            StylizedFx.Slash(at, -dir, 4.2f, StylizedFx.Void.With(sweepTime: 0.3f, tailFade: 1.2f, opacity: 0.85f), 1.6f, false, 0.6f);
            StylizedFx.Shockwave(at + Vector3.up * 0.05f, 4.5f, StylizedFx.Magenta, 0.4f);
            StylizedFx.Burst(at + Vector3.up * 1.2f, 2.2f, StylizedFx.Void);
            for (int i = 0; i < 18; i++)
            {
                var o = Random.onUnitSphere; o.y = Mathf.Abs(o.y) * 0.8f + 0.2f;
                float s = Random.Range(0.06f, 0.11f);
                DashFx.Emit(DashFx.M("fx_spike_0"), FxMesh.Kind.Spike, at + Vector3.up * 1f, (o * Random.Range(5f, 10f) + dir * 3f), new Vector3(s, s, s * 5f), Random.Range(0.25f, 0.4f), Random.value < 0.35f ? EmberHot : Ember, false, 3f);
            }
            for (int i = 0; i < 10; i++)
            {
                var o = Random.onUnitSphere; o.y = Mathf.Abs(o.y);
                float s = Random.Range(0.28f, 0.42f);
                DashFx.Emit(DashFx.M("fx_shard_1"), FxMesh.Kind.Ball, at + Vector3.up * 1.2f, o * Random.Range(2f, 5f) + Vector3.up * 2f, new Vector3(s * 0.35f, s * 0.12f, s),
                    Random.Range(0.8f, 1.2f), Feather, true, 2.4f, Random.Range(200f, 500f));
            }
            CameraFollow.Shake(0.22f);
        }
    }
}
