// 다국어 개발 메뉴 (2026-11-26): 플레이 중 화면에 나왔는데 사전(Resources/Lang/<언어>.json)에 없던 한국어 글을 파일로
using UnityEditor;
using UnityEngine;

namespace CatTang.EditorTools
{
    public static class LocMenu
    {
        [MenuItem("CatTang/번역/못 찾은 문구 저장")]
        static void DumpMisses()
        {
            var path = System.IO.Path.Combine(Application.dataPath, "../../Art/tools/specs/loc_misses_" + Loc.Lang + ".txt");
            System.IO.File.WriteAllText(path, Loc.DumpMisses());
            Debug.Log($"[Loc] 못 찾은 문구 {Loc.Misses.Count}개 → {path}");
        }

        [MenuItem("CatTang/번역/언어: 한국어")] static void Ko() => Loc.Lang = "ko";
        [MenuItem("CatTang/번역/언어: English")] static void En() => Loc.Lang = "en";
    }
}
