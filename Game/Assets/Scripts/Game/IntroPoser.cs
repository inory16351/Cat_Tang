// 등장 연출 전용 런타임 자세 (2026-11-22): 클립을 따로 못 만드는 모델(주인공 컨트롤러를 그대로 쓰는 거울 고양이 등)에
// 휴머노이드 근육 값을 지금 애니메이션 위에 더해 연기를 시킴. 표기는 Editor/EnemyMotions와 같음 ("RA.FB=.6 RF=-.9 ...", 값 = idle에서 더하는 근육값).
// 키 사이는 사인 가감속, 끝나면 0.25초 동안 원래 자세로 풀림. 리볼버 돌리기(Spin) 같이 뼈가 아닌 물체 회전도 붙일 수 있음.
using System.Collections.Generic;
using UnityEngine;

namespace CatTang
{
    [DefaultExecutionOrder(20000)]   // KataPoser 등 다른 자세 코드보다 늦게
    public class IntroPoser : MonoBehaviour
    {
        Animator an;
        HumanPoseHandler handler;
        HumanPose pose;
        readonly List<(float t, Dictionary<int, float> d, float root)> keys = new();
        float t, end;
        readonly List<(Transform tr, Quaternion baseRot, float t0, float t1, float turns, Vector3 axis)> spins = new();

        static Dictionary<string, int> index;

        /// <summary>e의 모델에 시간표 (시각, 자세 문자열)를 재생. 휴머노이드가 아니면 null</summary>
        public static IntroPoser Play(Enemy e, params (float t, string pose)[] tl)
        {
            var an = e ? e.GetComponentInChildren<Animator>() : null;
            if (!an || !an.isHuman || !an.avatar) return null;
            var old = an.GetComponent<IntroPoser>(); if (old) Destroy(old);
            var p = an.gameObject.AddComponent<IntroPoser>();
            p.Init(an, tl);
            return p;
        }

        /// <summary>물체(리볼버 등)를 t0 ~ t1 동안 turns바퀴 돌림 (가속 → 감속). 끝나면 원래 회전</summary>
        public void Spin(Transform tr, float t0, float t1, float turns, Vector3 axis)
        {
            if (tr) spins.Add((tr, tr.localRotation, t0, t1, turns, axis));
        }

        void Init(Animator a, (float t, string pose)[] tl)
        {
            an = a;
            handler = new HumanPoseHandler(an.avatar, an.transform);
            if (index == null)
            {
                index = new Dictionary<string, int>();
                for (int i = 0; i < HumanTrait.MuscleCount; i++) index[HumanTrait.MuscleName[i]] = i;
            }
            foreach (var (time, s) in tl) keys.Add((time, Parse(s, out float root), root));
            end = keys.Count > 0 ? keys[keys.Count - 1].t : 0;
        }

        void LateUpdate()
        {
            t += Time.deltaTime;
            float w = 1f - Mathf.Clamp01((t - end) / 0.25f);
            if (keys.Count > 0 && w > 0)
            {
                handler.GetHumanPose(ref pose);
                int k = 0;
                while (k < keys.Count - 1 && keys[k + 1].t <= t) k++;
                var a = keys[k];
                var b = k + 1 < keys.Count ? keys[k + 1] : a;
                float u = b.t > a.t ? Mathf.Clamp01((t - a.t) / (b.t - a.t)) : 1f;
                u = 0.5f - 0.5f * Mathf.Cos(u * Mathf.PI);                   // 가감속
                foreach (var kv in a.d) { float bv = b.d.TryGetValue(kv.Key, out var x) ? x : 0f; pose.muscles[kv.Key] += Mathf.Lerp(kv.Value, bv, u) * w; }
                foreach (var kv in b.d) if (!a.d.ContainsKey(kv.Key)) pose.muscles[kv.Key] += kv.Value * u * w;
                pose.bodyPosition += Vector3.up * Mathf.Lerp(a.root, b.root, u) * w;
                handler.SetHumanPose(ref pose);
            }
            for (int i = 0; i < spins.Count; i++)
            {
                var s = spins[i];
                if (!s.tr) continue;
                float su = Mathf.Clamp01((t - s.t0) / Mathf.Max(0.01f, s.t1 - s.t0));
                float ang = 360f * s.turns * (su * su * (3 - 2 * su));
                s.tr.localRotation = s.baseRot * Quaternion.AngleAxis(t >= s.t1 ? 0 : ang, s.axis);
            }
            if (t > end + 0.3f && AllSpinsDone()) Destroy(this);
        }

        bool AllSpinsDone() { foreach (var s in spins) if (t < s.t1) return false; return true; }

        void OnDestroy()
        {
            foreach (var s in spins) if (s.tr) s.tr.localRotation = s.baseRot;
            handler?.Dispose();
        }

        // ---------- 표기 해석 (Editor/EnemyMotions.Muscle과 같은 표) ----------
        static Dictionary<int, float> Parse(string s, out float root)
        {
            var d = new Dictionary<int, float>();
            root = 0;
            if (string.IsNullOrEmpty(s)) return d;
            foreach (var tok in s.Split(' '))
            {
                if (tok.Length == 0) continue;
                var kv = tok.Split('=');
                if (kv.Length != 2) continue;
                float val = float.Parse(kv[1], System.Globalization.CultureInfo.InvariantCulture);
                if (kv[0] == "ROOT") { root = val; continue; }
                var m = Muscle(kv[0]);
                if (m == null || !index.TryGetValue(m, out int idx)) { Debug.LogWarning("[IntroPoser] 모르는 약어 " + kv[0]); continue; }
                if (m.EndsWith("Front-Back")) val = -val;                    // 유니티 Front-Back은 + = 뒤 → 표는 + = 앞 (Ch2Motion.MirrorFrontBack과 같은 뜻)
                if (m.EndsWith("Foot Up-Down")) val = -val;                  // FT: − = 발끝 세움
                d[idx] = val;
            }
            return d;
        }

        static string Muscle(string a)
        {
            switch (a)
            {
                case "SP.FB": return "Spine Front-Back";
                case "SP.LR": return "Spine Left-Right";
                case "SP.TW": return "Spine Twist Left-Right";
                case "CH.FB": return "Chest Front-Back";
                case "CH.LR": return "Chest Left-Right";
                case "CH.TW": return "Chest Twist Left-Right";
                case "HD.ND": return "Head Nod Down-Up";
                case "HD.TN": return "Head Turn Left-Right";
                case "HD.TL": return "Head Tilt Left-Right";
            }
            if (a.Length < 2) return null;
            string side = a[0] == 'R' ? "Right " : "Left ";
            return a.Substring(1) switch
            {
                "A.FB" => side + "Arm Front-Back", "A.DU" => side + "Arm Down-Up", "A.TW" => side + "Arm Twist In-Out",
                "F" => side + "Forearm Stretch", "F.TW" => side + "Forearm Twist In-Out",
                "H.DU" => side + "Hand Down-Up", "H.IO" => side + "Hand In-Out",
                "S.DU" => side + "Shoulder Down-Up", "S.FB" => side + "Shoulder Front-Back",
                "L.FB" => side + "Upper Leg Front-Back", "L.IO" => side + "Upper Leg In-Out",
                "K" => side + "Lower Leg Stretch", "FT" => side + "Foot Up-Down",
                _ => null,
            };
        }
    }
}
