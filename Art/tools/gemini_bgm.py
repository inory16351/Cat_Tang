# Gemini(음악 만들기)로 받은 배경음악 MP3 → 게임 OGG (끝 무음 자르기 · −16 LUFS · 48kHz · 스테레오)
#   python Art/tools/gemini_bgm.py bgm_stage_5 [bgm_midboss_5 ...]
#   원본: Art/audio_src/gemini/<이름>.mp3 → Game/Assets/Resources/Audio/<이름>.ogg (Music.PlayFor가 이름으로 찾음)
# 받는 법: 내장 브라우저 Gemini '음악 만들기' → 트랙 다운로드 → 오디오만 → ~/Downloads/<guid>.tmp 를 바로 위 원본 이름으로 복사
import os
import subprocess
import sys

ROOT = os.path.normpath(os.path.join(os.path.dirname(__file__), '..', '..'))
SRC = os.path.join(ROOT, 'Art', 'audio_src', 'gemini')
OUT = os.path.join(ROOT, 'Game', 'Assets', 'Resources', 'Audio')

for name in sys.argv[1:]:
    src = os.path.join(SRC, name + '.mp3')
    dst = os.path.join(OUT, name + '.ogg')
    # 끝 무음: 뒤집어서 앞 무음을 자르고 다시 뒤집음 → 마지막 1.5초는 짧게 페이드 아웃 (반복 이음새가 툭 끊기지 않게)
    dur = float(subprocess.run(['ffprobe', '-v', 'error', '-show_entries', 'format=duration', '-of', 'csv=p=0', src], capture_output=True, text=True).stdout.strip())
    af = ('areverse,silenceremove=start_periods=1:start_threshold=-50dB:start_silence=0.1,areverse,'
          f'afade=t=out:st={max(0.0, dur - 2.0):.2f}:d=1.5,loudnorm=I=-16:TP=-1.5:LRA=11')
    subprocess.run(['ffmpeg', '-y', '-v', 'error', '-i', src, '-af', af, '-ar', '48000', '-ac', '2', '-c:a', 'libvorbis', '-q:a', '6', dst], check=True)
    print(name, '→', dst)
