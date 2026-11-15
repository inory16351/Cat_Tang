// 에디터가 열려 있을 때, 프로젝트 폴더에 ShaderTestRequest.txt가 생기면
// 셰이더 비교 씬을 다시 만들고 스크린샷을 찍은 뒤 ShaderTestDone.txt를 남긴다.
// (Claude가 에디터를 직접 조작하지 않고 결과를 받기 위한 장치. 파일 내용 = 스크린샷 저장 폴더)
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace CatTang.EditorTools
{
    [InitializeOnLoad]
    static class ShaderTestWatcher
    {
        const string RequestFile = "ShaderTestRequest.txt";
        const string DoneFile = "ShaderTestDone.txt";
        static double _next;

        static ShaderTestWatcher()
        {
            EditorApplication.update += Tick;
        }

        static void Tick()
        {
            if (EditorApplication.timeSinceStartup < _next) return;
            _next = EditorApplication.timeSinceStartup + 1.0;
            if (!File.Exists(RequestFile) || EditorApplication.isCompiling || EditorApplication.isPlaying) return;

            string outDir = File.ReadAllText(RequestFile).Trim();
            File.Delete(RequestFile);
            try
            {
                if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                {
                    File.WriteAllText(DoneFile, "cancelled");
                    return;
                }
                AssetDatabase.Refresh();
                ShaderTestBuilder.BuildAndRenderTo(string.IsNullOrEmpty(outDir) ? "ShaderTestOut" : outDir);
                File.WriteAllText(DoneFile, "ok " + System.DateTime.Now.ToString("HH:mm:ss"));
            }
            catch (System.Exception e)
            {
                File.WriteAllText(DoneFile, "error " + e);
                Debug.LogException(e);
            }
        }
    }
}
