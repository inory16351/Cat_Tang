// 배경음악: Resources/Audio/<이름> (bgm_village · bgm_stage_N · bgm_midboss_N · bgm_boss_N, Art/tools/gen_audio.py). 파일이 없으면 조용히 넘어감.
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
        float duck = 1f, duckTarget = 1f, duckSpeed = 1f;
        bool fading;

        /// <summary>음악을 잠깐 줄임 (보스 등장 연출: 0.3 → 정지 순간 0 → 1). dur초 동안</summary>
        public static void Duck(float to, float dur)
        {
            var m = Inst;
            m.duckTarget = Mathf.Clamp01(to);
            m.duckSpeed = dur <= 0.001f ? 999f : Mathf.Abs(m.duckTarget - m.duck) / dur;
            if (dur <= 0.001f) m.duck = m.duckTarget;
        }

        // ---- 스토리 연출 전용 곡 (2026-11-23 사용자: "두 영상 연출 다 효과음 빼고 전용 노래, 재생되는 동안 인게임 노래 나오게 하지 말고") ----
        AudioSource story;
        Coroutine storyFade;

        /// <summary>연출 전용 곡: 인게임 곡은 일시정지(진행도 멈춤) · Resources/Audio/{name}을 처음부터 한 번</summary>
        public static void Story(string name, float fadeIn = 0.4f)
        {
            var m = Inst;
            var clip = Resources.Load<AudioClip>("Audio/" + name);
            foreach (var s in new[] { m.a, m.b }) if (s && s.isPlaying) s.Pause();
            if (!m.story) { m.story = m.gameObject.AddComponent<AudioSource>(); m.story.playOnAwake = false; m.story.loop = false; m.story.ignoreListenerPause = true; }
            if (m.storyFade != null) m.StopCoroutine(m.storyFade);
            if (!clip) { m.story.Stop(); return; }
            m.story.clip = clip; m.story.time = 0; m.story.volume = 0; m.story.Play();
            m.storyFade = m.StartCoroutine(m.FadeStory(Volume, fadeIn, false));
        }

        /// <summary>연출 곡을 줄여 끄고 인게임 곡을 멈췄던 자리에서 다시</summary>
        public static void StoryEnd(float fadeOut = 1.2f)
        {
            if (!inst) return;
            var m = inst;
            if (m.storyFade != null) m.StopCoroutine(m.storyFade);
            if (m.story && m.story.isPlaying) m.storyFade = m.StartCoroutine(m.FadeStory(0, fadeOut, true));
            else m.ResumeGame();
        }

        IEnumerator FadeStory(float to, float dur, bool stopAfter)
        {
            float from = story.volume;
            for (float t = 0; t < dur; t += Time.unscaledDeltaTime) { story.volume = Mathf.Lerp(from, to, t / dur); yield return null; }
            story.volume = to;
            if (stopAfter) { story.Stop(); ResumeGame(); }
            storyFade = null;
        }

        void ResumeGame()
        {
            if (a && a.clip) { a.UnPause(); if (!a.isPlaying) a.Play(); }
            Duck(1f, 1f);   // 연출 전에 줄여 둔 음량도 되돌림 (꿈 → 마을에서 곡이 0으로 남던 문제)
        }

        /// <summary>지금 곡을 처음부터 (등장 연출이 끝나며 보스 곡이 시작되게)</summary>
        public static void Restart()
        {
            if (inst && inst.a && inst.a.clip) { inst.a.time = 0; if (!inst.a.isPlaying) inst.a.Play(); }
        }

        void Update()
        {
            duck = Mathf.MoveTowards(duck, duckTarget, duckSpeed * Time.unscaledDeltaTime);
            if (!fading && a && a.isPlaying) a.volume = Volume * duck;
        }

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

        /// <summary>음악 음량을 바로 바꿈 (설정 화면)</summary>
        public static void SetVolume(float v)
        {
            Volume = Mathf.Clamp01(v);
            if (inst && inst.a && inst.a.isPlaying) inst.a.volume = Volume;
            if (inst && inst.story && inst.story.isPlaying && inst.storyFade == null) inst.story.volume = Volume;
        }

        /// <summary>장별 곡: bgm_{kind}_{장} (stage · midboss · boss, 2026-11-19 audio-studio로 생성). 없으면 1장 곡, 중간 보스 곡이 없으면 보스 곡</summary>
        public static void PlayFor(string kind, int chapter)
        {
            // 곡이 없는 장은 1장 곡으로 (2026-11-20: 1 ~ 5장 맵 · 중간 보스 · 보스 곡 전부 있음 — 4장은 Gemini 3곡 추가)
            foreach (var n in new[] { $"bgm_{kind}_{chapter}", $"bgm_{kind}_1", kind == "midboss" ? $"bgm_boss_{chapter}" : null, kind == "midboss" ? "bgm_boss_1" : null })
                if (n != null && Resources.Load<AudioClip>("Audio/" + n)) { Play(n); return; }
            Play($"bgm_{kind}");
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
            fading = true;
            for (float t = 0; t < 1; t += Time.unscaledDeltaTime / 1.2f)
            {
                if (from) from.volume = Volume * duck * (1 - t);
                if (next) to.volume = Volume * duck * t;
                yield return null;
            }
            if (from) { from.Stop(); from.volume = 0; }
            if (next) { to.volume = Volume * duck; (a, b) = (to, a); }
            fading = false;
        }
    }
}
