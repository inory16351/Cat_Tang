// 빌드 메뉴 (2026-11-23 사용자: "맥북 빌드 뽑고 싶은데 가능하게 셋팅")
// - Cat_Tang/빌드/윈도우  → Builds/Windows/1159.exe
// - Cat_Tang/빌드/맥(Intel + Apple Silicon) → Builds/macOS/1159.app  (+ 처음 실행 도우미 .command)
// 윈도우에서 맥 빌드: Unity Hub에서 이 에디터(6000.3.19f1)에 'Mac Build Support (Mono)' 모듈이 있어야 함. IL2CPP는 맥에서만 → Mono.
// 윈도우에서 뽑은 .app은 서명이 없어서, 맥에서 처음 한 번 '처음 실행.command'(격리 속성 지우기 + 임시 서명)를 돌려야 열림.
// 명령줄: Unity.exe -batchmode -quit -projectPath Game -executeMethod CatTang.EditorTools.BuildMenu.BuildMac
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace CatTang.EditorTools
{
    public static class BuildMenu
    {
        const string Name = "1159";
        static string Root => Path.GetFullPath(Path.Combine(Application.dataPath, "../Builds"));

        static string[] Scenes()
        {
            var list = new System.Collections.Generic.List<string>();
            foreach (var s in EditorBuildSettings.scenes) if (s.enabled) list.Add(s.path);
            return list.ToArray();
        }

        [MenuItem("Cat_Tang/빌드/윈도우")]
        public static void BuildWindows() => Build(BuildTarget.StandaloneWindows64, Path.Combine(Root, "Windows", Name + ".exe"));

        [MenuItem("Cat_Tang/빌드/맥 (Intel + Apple Silicon)")]
        public static void BuildMac()
        {
            var t = BuildTarget.StandaloneOSX;
            if (!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.Standalone, t))
            {
                const string msg = "이 에디터에 'Mac Build Support (Mono)' 모듈이 없습니다.\nUnity Hub → 설치 → 6000.3.19f1 → 모듈 추가 → Mac Build Support (Mono)를 설치한 뒤 다시 해 주세요.";
                if (Application.isBatchMode) Debug.LogError("[Build] " + msg); else EditorUtility.DisplayDialog("맥 빌드", msg, "확인");
                return;
            }
            // 인텔 + 애플 실리콘 둘 다 (Universal). 윈도우에서는 Mono만 됨
            EditorUserBuildSettings.SetPlatformSettings(BuildPipeline.GetBuildTargetName(t), "Architecture", "x64ARM64");
            PlayerSettings.SetScriptingBackend(UnityEditor.Build.NamedBuildTarget.Standalone, ScriptingImplementation.Mono2x);
            if (PlayerSettings.GetApplicationIdentifier(UnityEditor.Build.NamedBuildTarget.Standalone).Contains("Unity-Technologies"))
                PlayerSettings.SetApplicationIdentifier(UnityEditor.Build.NamedBuildTarget.Standalone, "com.cattang.game1159");
            string app = Path.Combine(Root, "macOS", Name + ".app");
            if (!Build(t, app)) return;
            WriteMacHelper(Path.GetDirectoryName(app));
        }

        static bool Build(BuildTarget target, string path)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            var opt = new BuildPlayerOptions { scenes = Scenes(), locationPathName = path, target = target, targetGroup = BuildTargetGroup.Standalone };
            var r = BuildPipeline.BuildPlayer(opt);
            var s = r.summary;
            Debug.Log($"[Build] {target} {s.result} · {s.totalSize / (1024f * 1024f):0} MB · {s.totalTime.TotalSeconds:0}초 · 오류 {s.totalErrors} → {path}");
            if (s.result != BuildResult.Succeeded) return false;
            if (!Application.isBatchMode) EditorUtility.RevealInFinder(path);
            return true;
        }

        /// <summary>맥에서 처음 한 번 실행: 다운로드 격리 속성 제거 · 실행 권한 · 임시(ad-hoc) 서명 → 이후엔 1159.app 더블클릭</summary>
        static void WriteMacHelper(string dir)
        {
            string cmd = Path.Combine(dir, "처음 실행.command");
            string body =
                "#!/bin/bash\n" +
                "# 윈도우에서 만든 빌드라 서명이 없음 → 이 파일을 한 번 실행하면 1159.app이 열림\n" +
                "cd \"$(dirname \"$0\")\"\n" +
                "xattr -cr \"" + Name + ".app\"\n" +
                "chmod +x \"" + Name + ".app/Contents/MacOS/\"*\n" +
                "codesign --force --deep -s - \"" + Name + ".app\"\n" +
                "open \"" + Name + ".app\"\n";
            File.WriteAllText(cmd, body.Replace("\r\n", "\n"), new System.Text.UTF8Encoding(false));
            File.WriteAllText(Path.Combine(dir, "읽어 주세요.txt"),
                "1. 이 폴더를 통째로 맥에 복사 (zip으로 묶어 옮기면 실행 권한이 유지됨)\n" +
                "2. 터미널에서 한 번:  bash \"처음 실행.command\"   (또는 우클릭 → 열기)\n" +
                "3. 그 뒤로는 1159.app 더블클릭\n" +
                "· '확인되지 않은 개발자' 경고가 뜨면: 시스템 설정 → 개인정보 보호 및 보안 → '그래도 열기'\n",
                new System.Text.UTF8Encoding(false));
        }
    }
}
