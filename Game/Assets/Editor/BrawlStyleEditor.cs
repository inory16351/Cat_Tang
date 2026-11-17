// 에디터(씬 화면 · 플레이 전 게임 화면)에서도 기본 그림체(브롤 스타즈풍, 툰 셰이더 전역 _CatTangBrawl = 1)로 보이게 (2026-11-17 확정)
// 캐릭터 · 적 구분(_BrawlActor)은 플레이 중에만 표시되므로, 에디터에선 전부 배경 쪽 값으로 보임
using UnityEditor;
using UnityEngine;

namespace CatTang.EditorTools
{
    [InitializeOnLoad]
    static class BrawlStyleEditor
    {
        static BrawlStyleEditor() => EditorApplication.delayCall += () => Shader.SetGlobalFloat("_CatTangBrawl", 1f);
    }
}
