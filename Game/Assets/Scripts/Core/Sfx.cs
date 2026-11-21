// 효과음 (2026-11-19): Resources/Audio/Sfx/<이름>.ogg 또는 <이름>_1 · _2 … (여러 개면 무작위). Art/tools/gen_audio.py가 만듦.
// 내려다보는 카메라라 위치 감쇠 없이 2D로, 너무 잦은 소리(사격 · 타격)는 같은 이름이 짧은 간격 안에 몇 번까지만 겹치게.
// 전체 음량 = AudioListener(설정), 효과음 크기 = Volume. 파일이 없으면 조용히 넘어감.
using System.Collections.Generic;
using UnityEngine;

namespace CatTang
{
    public class Sfx : MonoBehaviour
    {
        public static float Volume = 0.8f;
        const int Voices = 16;

        static Sfx inst;
        readonly List<AudioSource> pool = new();
        static readonly Dictionary<string, AudioClip[]> clips = new();
        static readonly Dictionary<string, (float t, int n)> recent = new();

        static Sfx Inst
        {
            get
            {
                if (inst) return inst;
                var go = new GameObject("Sfx");
                DontDestroyOnLoad(go);
                inst = go.AddComponent<Sfx>();
                for (int i = 0; i < Voices; i++)
                {
                    var s = go.AddComponent<AudioSource>();
                    s.playOnAwake = false; s.spatialBlend = 0f;
                    inst.pool.Add(s);
                }
                return inst;
            }
        }

        /// <summary>그 이름 소리 파일이 있는지 (연출 효과음이 아직 없으면 예전 소리로)</summary>
        public static bool Has(string name) => Load(name).Length > 0;

        static AudioClip[] Load(string name)
        {
            if (clips.TryGetValue(name, out var c)) return c;
            var list = new List<AudioClip>();
            var one = Resources.Load<AudioClip>("Audio/Sfx/" + name);
            if (one) list.Add(one);
            for (int i = 1; i <= 8; i++)
            {
                var v = Resources.Load<AudioClip>($"Audio/Sfx/{name}_{i}");
                if (v) list.Add(v); else if (i > 1) break;
            }
            return clips[name] = list.ToArray();
        }

        /// <summary>계속 반복하는 소리(건카타 회전음 등). 반환한 소스를 StopLoop로 끔. 파일이 없으면 null</summary>
        public static AudioSource Loop(string name, float vol = 1f)
        {
            var set = Load(name);
            if (set.Length == 0) return null;
            var src = Inst.gameObject.AddComponent<AudioSource>();
            src.playOnAwake = false; src.spatialBlend = 0f; src.loop = true;
            src.clip = set[0]; src.volume = 0f;
            src.Play();
            Inst.StartCoroutine(Inst.FadeTo(src, Mathf.Clamp01(vol) * Volume, 0.12f, false));
            return src;
        }

        /// <summary>반복 소리를 fade초에 걸쳐 줄이고 지움</summary>
        public static void StopLoop(AudioSource src, float fade = 0.3f)
        {
            if (!src || !inst) return;
            inst.StartCoroutine(inst.FadeTo(src, 0f, fade, true));
        }

        System.Collections.IEnumerator FadeTo(AudioSource src, float to, float dur, bool destroy)
        {
            float from = src ? src.volume : 0f;
            for (float t = 0; t < dur && src; t += Time.unscaledDeltaTime)
            {
                src.volume = Mathf.Lerp(from, to, t / dur);
                yield return null;
            }
            if (!src) yield break;
            src.volume = to;
            if (destroy) { src.Stop(); Destroy(src); }
        }

        /// <summary>vol = 이 소리 크기(0~1), pitch ± 무작위 폭, maxOverlap = 0.06초 안에 같은 소리 최대 몇 번</summary>
        public static void Play(string name, float vol = 1f, float pitchJitter = 0.06f, int maxOverlap = 3)
        {
            var set = Load(name);
            if (set.Length == 0) return;
            float now = Time.unscaledTime;
            if (recent.TryGetValue(name, out var r) && now - r.t < 0.06f)
            {
                if (r.n >= maxOverlap) return;
                recent[name] = (r.t, r.n + 1);
            }
            else recent[name] = (now, 1);

            var m = Inst;
            AudioSource src = null;
            foreach (var s in m.pool) if (!s.isPlaying) { src = s; break; }
            if (!src) { src = m.pool[0]; foreach (var s in m.pool) if (s.time > src.time) src = s; }   // 다 차면 가장 오래 울린 것을 끊음
            src.pitch = 1f + Random.Range(-pitchJitter, pitchJitter);
            src.PlayOneShot(set[Random.Range(0, set.Length)], Mathf.Clamp01(vol) * Volume);
        }
    }
}
