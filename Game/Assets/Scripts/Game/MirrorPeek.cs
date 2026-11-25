// 3장 거울 마을: 섬 거울 앞에서 E → 카메라가 거울로 다가가 거울 속을 크게 봄 (2026-11-25, 위에서 보는 화면에선 거울이 작아서)
// E · 클릭 · 이동 키로 그만 봄. 씬에 저장하려면 파일 이름 = 클래스 이름
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

namespace CatTang
{
    public class MirrorPeek : Interactable
    {
        MirrorView view;
        protected override void OnEnable() { base.OnEnable(); prompt = "거울 들여다보기"; radius = 3.6f; view = GetComponent<MirrorView>(); }
        public override bool Available => Ch3Quest.Flipped && !Ch3Quest.Busy && view && !view.Broken;
        public override void Interact(PlayerController p) { if (Available && Ch3Quest.I) Ch3Quest.I.StartCoroutine(Peek(p)); }

        static bool Quit()
        {
            var kb = Keyboard.current; var m = Mouse.current;
            return (kb != null && (kb.eKey.wasPressedThisFrame || kb.escapeKey.wasPressedThisFrame || kb.wKey.wasPressedThisFrame || kb.aKey.wasPressedThisFrame || kb.sKey.wasPressedThisFrame || kb.dKey.wasPressedThisFrame))
                || (m != null && (m.leftButton.wasPressedThisFrame || m.rightButton.wasPressedThisFrame));
        }

        IEnumerator Peek(PlayerController p)
        {
            Ch3Quest.Busy = true;
            p.InputLocked = true;
            var c = view.GlassCenter;
            CameraFollow.FocusOn(c + Vector3.down * 1.2f + Vector3.back * 0.6f, 0.36f);
            var ov = StoryOverlay.I;
            if (ov) ov.SetCaption("<size=26>E · 클릭  그만 보기</size>");
            yield return null;
            float t = 0;
            while (t < 30f && !(t > 0.35f && Quit())) { t += Time.unscaledDeltaTime; yield return null; }
            if (ov) ov.SetCaption("");
            CameraFollow.FocusOn(null, 0f);
            yield return new WaitForSecondsRealtime(0.15f);
            p.InputLocked = false;
            Ch3Quest.Busy = false;
        }
    }
}
