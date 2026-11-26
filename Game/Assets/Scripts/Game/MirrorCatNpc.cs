// 거울 속 고양이 NPC (Ch3Quest.cs) — 씬에 저장하려면 파일 이름 = 클래스 이름
using UnityEngine;

namespace CatTang
{
    /// <summary>광장에 나와 선 거울 속 고양이: E로 말 걸기 (퀘스트 중엔 힌트, 끝난 뒤엔 판마다 다른 한 마디)</summary>
    public class MirrorCatNpc : Interactable
    {
        protected override void OnEnable() { base.OnEnable(); prompt = Ch3Quest.MirrorCat; radius = 2.8f; }
        public override bool Available => !Ch3Quest.Busy && !Ch4Quest.Busy && Meta.StorySeen(Ch3Quest.KeyIntro);
        public override void Interact(PlayerController p)
        {
            if (!Available) return;
            if (Ch4Quest.InProgress && Ch4Quest.I) { Ch4Quest.I.StartCoroutine(Ch4Quest.CatHint(Ch3Quest.UIRef)); return; }   // 4장 멈춘 마을 동안은 그 힌트
            if (Ch3Quest.I) Ch3Quest.I.StartCoroutine(Ch3Quest.PuddleTalk(Ch3Quest.UIRef));
        }
    }
}
