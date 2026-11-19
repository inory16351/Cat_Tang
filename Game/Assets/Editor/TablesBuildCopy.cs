// 빌드 후처리 (2026-11-19): 데이터 표(CSV)는 Tables.Read가 Application.dataPath/Data/Tables에서 파일로 읽음.
// 에디터에선 Assets/Data/Tables, 빌드에선 <게임>_Data/Data/Tables라서 빌드할 때 CSV를 그 자리로 복사해 줌 (안 하면 탄 · 총 · 챕터가 0개)
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace CatTang.EditorTools
{
    public class TablesBuildCopy : IPostprocessBuildWithReport
    {
        public int callbackOrder => 0;

        public void OnPostprocessBuild(BuildReport report)
        {
            if (report.summary.platform != BuildTarget.StandaloneWindows64 && report.summary.platform != BuildTarget.StandaloneWindows
                && report.summary.platform != BuildTarget.StandaloneOSX && report.summary.platform != BuildTarget.StandaloneLinux64) return;
            string exe = report.summary.outputPath;
            string dataDir = report.summary.platform == BuildTarget.StandaloneOSX
                ? Path.Combine(exe, "Contents")
                : Path.Combine(Path.GetDirectoryName(exe), Path.GetFileNameWithoutExtension(exe) + "_Data");
            string src = Path.Combine(Application.dataPath, "Data/Tables");
            string dst = Path.Combine(dataDir, "Data/Tables");
            Directory.CreateDirectory(dst);
            int n = 0;
            foreach (var f in Directory.GetFiles(src, "*.csv"))
            {
                File.Copy(f, Path.Combine(dst, Path.GetFileName(f)), true);
                n++;
            }
            Debug.Log($"[TablesBuildCopy] 데이터 표 {n}개 → {dst}");
        }
    }
}
