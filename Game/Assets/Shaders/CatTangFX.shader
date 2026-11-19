// 벡터 + 셰이더 VFX (docs/tools/fx_shader.py와 같은 식)
// 도형을 거리장으로 정의하고 픽셀마다 계단 램프 색 · 잉크 테두리 · 노이즈 디졸브.
// _Progress 0→1 하나로 진행. _Mode: 0 폭발, 1 금빛 고리(장전 팝), 2 빛줄기 고리(장전 끝/완벽 장전)
// 월드(빌보드 쿼드)와 UI(RawImage) 모두 사용 가능.
Shader "CatTang/FX"
{
    Properties
    {
        [PerRendererData] _MainTex ("Texture", 2D) = "white" {}
        _Progress ("Progress", Range(0,1)) = 0
        _Mode ("Mode", Float) = 0
        _Seed ("Seed", Float) = 3
        _Tint ("Tint", Color) = (1,1,1,1)
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" "CanUseSpriteAtlas"="True" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off
        Pass
        {
            Tags { "LightMode" = "UniversalForward" }   // Vivid(후처리 뒤 RenderObjects)에서도 그려지게
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            float _Progress, _Mode, _Seed;
            float4 _Tint;

            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; float4 color : COLOR; };
            struct v2f { float4 pos : SV_POSITION; float2 uv : TEXCOORD0; float4 color : COLOR; };

            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.color = v.color;
                return o;
            }

            float hash(float2 p) { return frac(sin(dot(p, float2(127.1, 311.7)) + _Seed * 17.3) * 43758.5453); }
            float vnoise(float2 p)
            {
                float2 i = floor(p), f = frac(p);
                float2 u = f * f * (3 - 2 * f);
                return lerp(lerp(hash(i), hash(i + float2(1,0)), u.x), lerp(hash(i + float2(0,1)), hash(i + float2(1,1)), u.x), u.y);
            }
            float fbm(float2 p) { float s = 0, a = 0.5; for (int k = 0; k < 4; k++) { s += a * vnoise(p); p *= 2.03; a *= 0.5; } return s / 0.9375; }
            float easeOut(float t) { return 1 - pow(1 - t, 3); }
            float easeBack(float t) { t -= 1; return t * t * (2.7 * t + 1.7) + 1; }

            static const float3 INK = float3(0.07, 0.05, 0.10);
            static const float3 WHITE = float3(1.0, 0.97, 0.87);
            static const float3 YEL = float3(1.0, 0.84, 0.29);
            static const float3 ORG = float3(0.97, 0.55, 0.16);
            static const float3 RED = float3(0.84, 0.23, 0.17);
            static const float3 GOLD = float3(1.0, 0.91, 0.59);
            static const float3 SMOKE_L = float3(0.55, 0.48, 0.63);
            static const float3 SMOKE_D = float3(0.38, 0.31, 0.45);

            // 위로 덮어 그리기 (알파 합성)
            void over(inout float4 acc, float3 c, float a) { acc.rgb = lerp(acc.rgb, c, a); acc.a = a + acc.a * (1 - a); }

            float4 explosion(float2 uv, float t)
            {
                float2 p = uv * 2 - 1;
                float r = length(p), th = atan2(p.x, -p.y);
                float n1 = fbm(uv * 6), n2 = fbm(uv * 6 + 7.3);
                float4 acc = 0;
                // 연기
                if (t > 0.38)
                {
                    float tt = (t - 0.38) / 0.62;
                    float f = -1;
                    for (int i = 0; i < 6; i++)
                    {
                        float a = i * 1.0472 + (hash(float2(i, 3)) - 0.5) * 0.8;
                        float rr = 0.12 + 0.32 * easeOut(tt);
                        float2 c = float2(rr * sin(a), -rr * cos(a) - 0.25 * tt);
                        float pr = (0.16 + 0.12 * tt) * (0.85 + 0.3 * hash(float2(i, 9)));
                        f = max(f, 1 - length(p - c) / pr);
                    }
                    f += (n2 - 0.5) * 0.35 - smoothstep(0.3, 1.0, tt) * 1.1;
                    if (f > 0) over(acc, f < 0.09 ? INK : (p.y < 0.1 - p.x * 0.3 ? SMOKE_L : SMOKE_D), 1);
                }
                // 불꽃 별
                if (t < 0.8)
                {
                    float grow = easeBack(saturate(t / 0.28));
                    float R = 0.62 * grow * (1 - 0.55 * smoothstep(0.35, 0.8, t));
                    float spikes = 1 + 0.28 * cos(10 * th + 1.3 + t * 2.5) + 0.22 * (n1 - 0.5);
                    float field = 1 - r / (R * spikes + 1e-4);
                    float burn = smoothstep(0.3, 0.8, t) * 1.05;
                    float I = field + (n1 - 0.5) * 0.35 * smoothstep(0.2, 0.6, t) - burn;
                    float glow = saturate((I + 0.25) * 2) * 0.45 * (1 - smoothstep(0.4, 0.8, t));
                    over(acc, YEL, glow * (I <= 0));
                    if (I > 0)
                    {
                        float k = I / max(1e-4, 1 - burn * 0.6);
                        float3 c = k > 0.62 ? WHITE : k > 0.42 ? YEL : k > 0.24 ? ORG : k > 0.07 ? RED : INK;
                        over(acc, c, 1);
                    }
                }
                // 불똥 줄기
                if (t > 0.06 && t < 0.85)
                {
                    float tt = (t - 0.06) / 0.79;
                    float r0 = 0.3 + 0.55 * easeOut(tt), r1 = r0 + 0.28 * (1 - tt);
                    float ang = frac(th * 9 / 6.2832 + 0.37);
                    float wid = 0.06 * (1 - tt) + 0.015;
                    if (abs(ang - 0.5) < wid && r > r0 && r < r1) over(acc, abs(ang - 0.5) < wid * 0.45 ? YEL : INK, 1);
                }
                return acc;
            }

            float4 goldRing(float2 uv, float t)
            {
                float2 p = uv * 2 - 1;
                float r = length(p);
                float n = fbm(uv * 7 + 2.1);
                float4 acc = 0;
                float Rr = 0.35 + 0.5 * easeOut(t);
                float w = 0.13 * (1 - t) + 0.02;
                float f = 1 - abs(r - Rr) / w + (n - 0.5) * 0.9 * t - smoothstep(0.5, 1.0, t) * 0.8;
                float glow = saturate((f + 0.6) * 1.2) * 0.4 * (1 - t);
                over(acc, GOLD, glow * (f <= 0));
                if (f > 0) over(acc, f < 0.22 ? INK : (f > 0.6 ? WHITE : GOLD), 1);
                return acc;
            }

            float4 rays(float2 uv, float t)
            {
                float2 p = uv * 2 - 1;
                float r = length(p), th = atan2(p.x, -p.y);
                float n = fbm(uv * 7 + 5.7);
                float4 acc = 0;
                float ang = frac(th * 12 / 6.2832);
                float ln = 0.16 * sin(3.1416 * saturate(t));
                float r0 = 0.8 + 0.04 * easeOut(t);
                float wid = 0.09 * (1 - t) + 0.03;
                float f = (wid - abs(ang - 0.5)) / wid;
                if (r > r0 && r < r0 + ln && f > 0)
                {
                    f += (n - 0.5) * 0.6 * t - smoothstep(0.55, 1.0, t);
                    if (f > 0) over(acc, f < 0.25 ? INK : (f > 0.7 ? WHITE : GOLD), 1);
                }
                float band = 1 - abs(r - r0 * 0.98) / (0.03 * (1 - t) + 0.005);
                if (band > 0 && band - smoothstep(0.3, 0.8, t) * 1.2 + (n - 0.5) * 0.5 > 0) over(acc, WHITE, 0.9);
                return acc;
            }

            // ---------- 상태 · 효과 이펙트 (3~14) ----------
            static const float3 ICE = float3(0.62, 0.86, 1.0);
            static const float3 ICE_D = float3(0.33, 0.55, 0.86);
            static const float3 SAND = float3(0.93, 0.78, 0.52);
            static const float3 PINK = float3(1.0, 0.55, 0.78);
            static const float3 VIO = float3(0.66, 0.46, 0.95);

            // 잉크 테두리 칠하기: f > 0 안쪽, 가장자리는 잉크
            void inked(inout float4 acc, float f, float edge, float3 c, float3 hi, float hiT)
            {
                if (f <= 0) return;
                over(acc, f < edge ? INK : (f > hiT ? hi : c), 1);
            }
            float segDist(float2 p, float2 a, float2 b) { float2 pa = p - a, ba = b - a; float h = saturate(dot(pa, ba) / dot(ba, ba)); return length(pa - ba * h); }

            // 3 시간에 가둠: 시계판 고리 + 눈금 + 도는 바늘 (반복)
            float4 clockRing(float2 uv, float t)
            {
                float2 p = uv * 2 - 1; float r = length(p), th = atan2(p.x, p.y);
                float4 acc = 0;
                float pulse = 0.82 + 0.04 * sin(t * 6.2832 * 2);
                inked(acc, 1 - abs(r - pulse) / 0.07, 0.3, ICE, WHITE, 0.75);
                float tick = frac(th * 12 / 6.2832 + 0.5);
                if (abs(tick - 0.5) < 0.05 && r > pulse - 0.2 && r < pulse - 0.08) over(acc, ICE_D, 1);
                float ha = t * 6.2832;
                float2 hd = float2(sin(ha), cos(ha));
                inked(acc, 1 - segDist(p, 0, hd * 0.6) / 0.05, 0.35, WHITE, WHITE, 1);
                over(acc, ICE, saturate(0.25 - r * 0.25) * 0.6);
                return acc;
            }
            // 4 정지(기절): 머리 위를 도는 별 셋
            float4 stars(float2 uv, float t)
            {
                float2 p = uv * 2 - 1; float4 acc = 0;
                for (int k = 0; k < 3; k++)
                {
                    float a = t * 6.2832 + k * 2.094;
                    float2 c = float2(sin(a) * 0.62, cos(a) * 0.22);
                    float2 q = p - c; float s = 0.22 * (0.8 + 0.2 * cos(a));
                    float d = (abs(q.x) + abs(q.y) * 2.2) * (abs(q.x) * 2.2 + abs(q.y));
                    inked(acc, 1 - d / (s * s * 1.2), 0.3, YEL, WHITE, 0.7);
                }
                return acc;
            }
            // 5 둔화: 발밑 모래 소용돌이
            float4 sandSwirl(float2 uv, float t)
            {
                float2 p = uv * 2 - 1; float r = length(p), th = atan2(p.x, p.y);
                float4 acc = 0;
                float sp = frac((th / 6.2832) * 3 + r * 1.6 - t * 1.5);
                float band = 1 - abs(sp - 0.5) / 0.16;
                float f = band * smoothstep(0.95, 0.75, r) * smoothstep(0.15, 0.3, r) + (fbm(uv * 8) - 0.5) * 0.4;
                inked(acc, f - 0.2, 0.15, SAND, WHITE, 0.75);
                return acc;
            }
            // 6 화상: 일렁이는 불꽃
            float4 flame(float2 uv, float t)
            {
                float2 p = uv * 2 - 1; p.y += 0.25; float4 acc = 0;
                float n = fbm(float2(uv.x * 4, uv.y * 3 - t * 3));
                float w = 0.55 * (1 - saturate((p.y + 0.6) / 1.5)) + 0.05;
                float f = 1 - abs(p.x + (n - 0.5) * 0.35 * (p.y + 0.6)) / w - saturate(-p.y - 0.55) * 4;
                f += (n - 0.5) * 0.5;
                if (f > 0) over(acc, f < 0.12 ? INK : f > 0.7 ? WHITE : f > 0.45 ? YEL : f > 0.25 ? ORG : RED, 1);
                return acc;
            }
            // 7 추억 표식: 분홍 마름모 (Tint.a로 겹 수 0~1)
            float4 mark(float2 uv, float t)
            {
                float2 p = uv * 2 - 1; float4 acc = 0;
                float pulse = 1 + 0.08 * sin(t * 6.2832 * 2);
                float d = (abs(p.x) + abs(p.y)) / (0.7 * pulse);
                inked(acc, 1 - d, 0.18, PINK, WHITE, 0.72);
                float inner = (abs(p.x) + abs(p.y)) / 0.28;
                if (inner < 1) over(acc, WHITE, 0.9);
                return acc;
            }
            // 8 방어력 감소: 금 간 방패
            float4 crackShield(float2 uv, float t)
            {
                float2 p = uv * 2 - 1; float4 acc = 0;
                float sh = max(abs(p.x) * 1.15 + max(p.y, 0) * 0.15, (p.y < -0.1 ? length(float2(p.x, p.y + 0.1) * float2(1.1, 1.3)) : 0));
                float f = 1 - max(sh / 0.75, abs(p.y - 0.05) / 0.85);
                inked(acc, f, 0.2, VIO, WHITE, 0.8);
                float crack = segDist(p, float2(0.05, 0.75), float2(-0.1, 0.2));
                crack = min(crack, segDist(p, float2(-0.1, 0.2), float2(0.12, -0.15)));
                crack = min(crack, segDist(p, float2(0.12, -0.15), float2(-0.02, -0.6)));
                if (crack < 0.05 && f > 0) over(acc, INK, 1);
                acc.a *= 1 - smoothstep(0.7, 1, t);
                return acc;
            }
            // 9 처형: 붉은 X 베기
            float4 slashX(float2 uv, float t)
            {
                float2 p = uv * 2 - 1; float4 acc = 0;
                float g1 = saturate(t / 0.3), g2 = saturate((t - 0.12) / 0.3);
                float fade = 1 - smoothstep(0.55, 1, t);
                float2 a1 = float2(-0.8, 0.8), b1 = lerp(a1, float2(0.8, -0.8), g1);
                float2 a2 = float2(0.8, 0.8), b2 = lerp(a2, float2(-0.8, -0.8), g2);
                float w = 0.12 * fade + 0.02;
                inked(acc, 1 - segDist(p, a1, b1) / w, 0.3, RED, WHITE, 0.7);
                if (g2 > 0) inked(acc, 1 - segDist(p, a2, b2) / w, 0.3, RED, WHITE, 0.7);
                return acc;
            }
            // 10 지연 피해: 바깥에서 조여 드는 고리 (끝에 터짐)
            float4 converge(float2 uv, float t)
            {
                float2 p = uv * 2 - 1; float r = length(p), th = atan2(p.x, p.y);
                float4 acc = 0;
                float R = lerp(0.95, 0.12, easeOut(t));
                float dash = step(0.5, frac(th * 8 / 6.2832 + t));
                inked(acc, (1 - abs(r - R) / 0.06) * (0.4 + 0.6 * dash), 0.3, VIO, WHITE, 0.75);
                if (t > 0.85) inked(acc, 1 - r / (0.5 * (t - 0.85) / 0.15 + 0.01), 0.2, WHITE, WHITE, 1);
                return acc;
            }
            // 11 치명타: 뾰족한 금빛 별 폭발
            float4 critStar(float2 uv, float t)
            {
                float2 p = uv * 2 - 1; float r = length(p), th = atan2(p.x, p.y);
                float4 acc = 0;
                float grow = easeBack(saturate(t / 0.3));
                float sp = 0.45 + 0.55 * pow(abs(cos(th * 4)), 6);
                float f = 1 - r / (0.9 * grow * sp + 1e-4) - smoothstep(0.4, 1, t) * 1.2;
                if (f > 0) over(acc, f < 0.12 ? INK : f > 0.55 ? WHITE : YEL, 1);
                return acc;
            }
            // 12 방어구 막음: 푸른 육각 방패 번쩍
            float4 shieldFlash(float2 uv, float t)
            {
                float2 p = abs(uv * 2 - 1);
                float hex = max(p.x * 0.866 + p.y * 0.5, p.y);
                float4 acc = 0;
                float R = 0.6 + 0.25 * easeOut(t);
                inked(acc, (1 - abs(hex - R) / 0.06) - t * 0.9, 0.25, ICE, WHITE, 0.7);
                over(acc, ICE, saturate(1 - hex / R) * 0.25 * (1 - t));
                return acc;
            }
            // 13 밀쳐냄: 먼지 뭉치
            float4 dust(float2 uv, float t)
            {
                float2 p = uv * 2 - 1; float4 acc = 0; float f = -1;
                for (int k = 0; k < 5; k++)
                {
                    float a = k * 1.2566 + hash(float2(k, 1)) * 0.6;
                    float2 c = float2(sin(a), cos(a) * 0.5) * (0.2 + 0.5 * easeOut(t));
                    f = max(f, 1 - length(p - c) / (0.28 * (1 - t * 0.6)));
                }
                f += (fbm(uv * 7) - 0.5) * 0.4 - t * 0.8;
                // 흙먼지 = 적이 사라질 때 검은 안개(bk_mist)와 같은 색 (2026-11-18 사용자): 회보라 몸 · 아래쪽 어두운 면 · 보라 테두리
                // bk_mist 그림의 sRGB 색 (0.38, 0.31, 0.44) · (0.31, 0.25, 0.31) · (0.38, 0.25, 0.5)를 리니어로
                const float3 MIST = float3(0.12, 0.078, 0.16), MIST_D = float3(0.078, 0.051, 0.078), MIST_E = float3(0.12, 0.051, 0.21);
                // 색을 따로 준 먼지(흰색이 아닌 _Tint: 3장 빛가루 · 밀쳐냄 등)는 예전처럼 밝은 연기에 물들임
                bool plain = min(_Tint.r, min(_Tint.g, _Tint.b)) > 0.99;
                if (f > 0) over(acc, plain ? (f < 0.12 ? MIST_E : (p.y < 0.05 - p.x * 0.3 ? MIST : MIST_D)) : (f < 0.12 ? INK : SMOKE_L), 1);
                return acc;
            }
            // 14 튕김 · 관통 불꽃: 짧은 방사 선
            float4 spark(float2 uv, float t)
            {
                float2 p = uv * 2 - 1; float r = length(p), th = atan2(p.x, p.y);
                float4 acc = 0;
                float ang = frac(th * 7 / 6.2832 + hash(float2(1, 2)));
                float r0 = 0.15 + 0.6 * easeOut(t), r1 = r0 + 0.3 * (1 - t);
                if (abs(ang - 0.5) < 0.07 * (1 - t) + 0.02 && r > r0 && r < r1) over(acc, abs(ang - 0.5) < 0.035 ? WHITE : YEL, 1);
                inked(acc, 1 - r / (0.3 * (1 - t)), 0.3, YEL, WHITE, 0.6);
                return acc;
            }

            float4 pick(float2 uv, float t)
            {
                float m = _Mode;
                if (m < 0.5) return explosion(uv, t);
                if (m < 1.5) return goldRing(uv, t);
                if (m < 2.5) return rays(uv, t);
                if (m < 3.5) return clockRing(uv, t);
                if (m < 4.5) return stars(uv, t);
                if (m < 5.5) return sandSwirl(uv, t);
                if (m < 6.5) return flame(uv, t);
                if (m < 7.5) return mark(uv, t);
                if (m < 8.5) return crackShield(uv, t);
                if (m < 9.5) return slashX(uv, t);
                if (m < 10.5) return converge(uv, t);
                if (m < 11.5) return critStar(uv, t);
                if (m < 12.5) return shieldFlash(uv, t);
                if (m < 13.5) return dust(uv, t);
                return spark(uv, t);
            }

            float4 frag (v2f i) : SV_Target
            {
                float t = saturate(_Progress);
                float4 c = pick(i.uv, t);
                // 탄 색으로 물들이기 (잉크 검정은 그대로, 밝은 면만)
                float lum = dot(c.rgb, float3(0.3, 0.59, 0.11));
                float3 tinted = lerp(c.rgb, c.rgb * _Tint.rgb * 1.35 + _Tint.rgb * 0.1, 0.75);
                c.rgb = lerp(c.rgb, tinted, smoothstep(0.15, 0.35, lum) * step(0.01, 1.0 - min(_Tint.r, min(_Tint.g, _Tint.b)) + 0.0001));
                c.a *= i.color.a;
                return c;
            }
            ENDCG
        }
    }
}
