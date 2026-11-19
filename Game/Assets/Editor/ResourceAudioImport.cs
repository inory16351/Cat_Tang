// Resources/Audio 소리 가져오기 (2026-11-19, Art/tools/gen_audio.py로 만든 것):
//  배경음악(bgm_*) = 2분짜리라 스트리밍(메모리에 통째로 풀지 않음) · Vorbis
//  효과음(Sfx/) = 짧아서 불러올 때 풀어 둠(사격 · 타격이 잦아도 지연 없음) · 미리 불러옴
using UnityEditor;
using UnityEngine;

namespace CatTang.EditorTools
{
    public class ResourceAudioImport : AssetPostprocessor
    {
        void OnPreprocessAudio()
        {
            var p = assetPath.Replace('\\', '/');
            if (!p.Contains("/Resources/Audio/")) return;
            var imp = (AudioImporter)assetImporter;
            var s = imp.defaultSampleSettings;
            bool bgm = System.IO.Path.GetFileName(p).StartsWith("bgm_");
            s.loadType = bgm ? AudioClipLoadType.Streaming : AudioClipLoadType.DecompressOnLoad;
            s.compressionFormat = AudioCompressionFormat.Vorbis;
            s.quality = bgm ? 0.6f : 0.8f;
            s.preloadAudioData = !bgm;
            imp.defaultSampleSettings = s;
            imp.loadInBackground = bgm;
        }
    }
}
