// Interactable.cs에서 분리 (씬에 저장하려면 파일 이름 = 클래스 이름)
using System.Collections.Generic;
using UnityEngine;

namespace CatTang
{
    /// <summary>마을의 챕터 문 (판 시작)</summary>
    public class ChapterGate : Interactable
    {
        public override void Interact(PlayerController p) => FindFirstObjectByType<GameUI>()?.OpenChapterSelect();
    }
}
