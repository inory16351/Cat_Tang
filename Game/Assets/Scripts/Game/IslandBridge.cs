// 마을 섬 사이 다리 (2026-11-18 사용자: 장(맵)을 넘을 때마다 섬 · 컨텐츠가 조금씩 해금, 섬 여러 개가 이어진 마을)
// 잠김 = 끊긴 다리(조각이 허공에 떠 있음) + 양 끝 막음 + 건너편 NPC 숨김
// 해금 = 성한 다리. 처음 열리는 순간(그 장을 넘고 처음 마을에 왔을 때)엔 VillageStory가 Reconnect로 다리가 이어지는 연출을 틀고 NPC가 나타남
using System.Collections;
using UnityEngine;

namespace CatTang
{
    public class IslandBridge : MonoBehaviour
    {
        public int reqChapter;                       // 이 장 보스를 넘으면 열림
        public string reqStory;                      // + 이 이야기 기록이 있어야 열림 (빈 값 = 장만). 보안관 섬 = 대장장이가 보낸 뒤 (Ch2Quest)
        public GameObject whole, broken;             // 성한 다리 · 끊긴 다리 모델
        public Collider[] blockers;                  // 잠겼을 때 다리 입구를 막는 벽
        public GameObject[] revealOnOpen;            // 열리면 나타나는 것 (건너편 섬의 NPC)

        public bool Open => Meta.ChaptersCleared >= reqChapter && (string.IsNullOrEmpty(reqStory) || Meta.StorySeen(reqStory));
        string Key => "bridge." + name;
        /// <summary>열렸지만 아직 이어지는 연출을 안 봄 → VillageStory가 Reconnect를 부름</summary>
        public bool Pending => reqChapter > 0 && Open && !Meta.StorySeen(Key);      // 처음부터 열린 다리(문 섬)는 연출 없음

        void Awake() => Apply(Open && !Pending);

        void Apply(bool open)
        {
            if (whole) whole.SetActive(open);
            if (broken) broken.SetActive(!open);
            foreach (var b in blockers) if (b) b.enabled = !open;
            foreach (var r in revealOnOpen) if (r) r.SetActive(open);
        }

        /// <summary>끊긴 다리 조각이 흩어지며 사라지고 성한 다리가 끝에서부터 이어짐 → 건너편 NPC가 모래 속에서 나타남</summary>
        public IEnumerator Reconnect()
        {
            Meta.MarkStory(Key);
            CameraFollow.FocusOn(transform.position, 2.4f);
            yield return new WaitForSecondsRealtime(0.6f);
            Sfx.Play("bridge", 0.9f, 0f);
            if (broken)
            {
                var b0 = broken.transform.localScale;
                for (float t = 0; t < 1f; t += Time.unscaledDeltaTime / 0.6f)
                {
                    broken.transform.localScale = b0 * (1f - t * t);
                    yield return null;
                }
                broken.SetActive(false);
                broken.transform.localScale = b0;
            }
            if (whole)
            {
                whole.SetActive(true);
                var w0 = whole.transform.localScale;
                for (float t = 0; t < 1f; t += Time.unscaledDeltaTime / 1.1f)
                {
                    float k = Mathf.SmoothStep(0, 1, t);
                    whole.transform.localScale = new Vector3(w0.x, w0.y * Mathf.Min(1f, k * 1.3f), w0.z * Mathf.Max(0.02f, k));
                    yield return null;
                }
                whole.transform.localScale = w0;
            }
            foreach (var b in blockers) if (b) b.enabled = false;
            foreach (var r in revealOnOpen)
            {
                if (!r) continue;
                r.SetActive(true);
                var s0 = r.transform.localScale;
                for (float t = 0; t < 1f; t += Time.unscaledDeltaTime / 0.45f)
                {
                    r.transform.localScale = s0 * (1f + 0.15f * Mathf.Sin(t * Mathf.PI)) * Mathf.Min(1f, t * 1.6f);
                    yield return null;
                }
                r.transform.localScale = s0;
            }
            yield return new WaitForSecondsRealtime(0.5f);
            CameraFollow.FocusOn(null, 0f);
        }
    }
}
