// 배경음악: Resources/Audio/<이름> (bgm_stage · bgm_boss · bgm_village). 파일이 없으면 조용히 넘어감.
// Gemini로 만든 곡을 내려받아 Assets/Resources/Audio/에 bgm_stage.mp3 등으로 넣으면 바로 재생됨.
using System.Collections;
using UnityEngine;

namespace CatTang
{
    public class Music : MonoBehaviour
    {
        static Music inst;
        AudioSource a, b;
        string current;
        public static float Volume = 0.55f;

        static Music Inst
        {
            get
            {
                if (inst) return inst;
                var go = new GameObject("Music");
                DontDestroyOnLoad(go);
                inst = go.AddComponent<Music>();
                inst.a = go.AddComponent<AudioSource>();
                inst.b = go.AddComponent<AudioSource>();
                foreach (var s in new[] { inst.a, inst.b }) { s.loop = true; s.playOnAwake = false; s.volume = 0; }
                return inst;
            }
        }

        public static void Play(string name)
        {
            var m = Inst;
            if (m.current == name) return;
            var clip = Resources.Load<AudioClip>("Audio/" + name);
            m.current = name;
            if (!clip) { m.StartCoroutine(m.Fade(null)); return; }
            m.StartCoroutine(m.Fade(clip));
        }

        IEnumerator Fade(AudioClip next)
        {
            var from = a.isPlaying ? a : null;
            var to = b;
            if (next)
            {
                to.clip = next;
                to.volume = 0;
                to.Play();
            }
            for (float t = 0; t < 1; t += Time.unscaledDeltaTime / 1.2f)
            {
                if (from) from.volume = Volume * (1 - t);
                if (next) to.volume = Volume * t;
                yield return null;
            }
            if (from) { from.Stop(); from.volume = 0; }
            if (next) { to.volume = Volume; (a, b) = (to, a); }
        }
    }
}
