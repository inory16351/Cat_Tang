# 꿈3 곡 30초 편집 (2026-11-26 사용자: "시간도 다른 꿈처럼 30초 정도로") — 원본 Art/audio_src/gemini/story_dream3.mp3 (56.7초, 보존)
#   조용한 도입 0~4.0 + 큰 구간 9.84~24.24 + 잦아듦 44.64~끝(마지막 째깍 54.5 포함), 이음새 0.4초 크로스페이드 → 약 29.7초
#   (이음 지점은 약 0.4초 박자 격자 0.24 + 0.4k 위), −16 LUFS · 48kHz OGG → Game/Assets/Resources/Audio/story_dream3.ogg
#   출력 시각 = 도입 0~4.0 · 큰 구간 3.6~18.0 (원본 t → 3.6 + t − 9.84) · 잦아듦 17.6~29.7 (원본 t → 17.6 + t − 44.64)
#   예전 56.7초 판 = Art/audio_src/gemini/story_dream3_full56.ogg
import os, subprocess
ROOT = os.path.normpath(os.path.join(os.path.dirname(__file__), '..', '..'))
src = os.path.join(ROOT, 'Art', 'audio_src', 'gemini', 'story_dream3.mp3')
dst = os.path.join(ROOT, 'Game', 'Assets', 'Resources', 'Audio', 'story_dream3.ogg')
X = 0.4
fc = ('[0:a]atrim=0:4.0,asetpts=PTS-STARTPTS[a];'
      '[0:a]atrim=9.84:24.24,asetpts=PTS-STARTPTS[b];'
      '[0:a]atrim=44.64,asetpts=PTS-STARTPTS[c];'
      f'[a][b]acrossfade=d={X}:c1=tri:c2=tri[ab];[ab][c]acrossfade=d={X}:c1=tri:c2=tri,'
      'loudnorm=I=-16:TP=-1.5:LRA=11[out]')
subprocess.run(['ffmpeg', '-y', '-v', 'error', '-i', src, '-filter_complex', fc, '-map', '[out]', '-ar', '48000', '-ac', '2', '-c:a', 'libvorbis', '-q:a', '6', dst], check=True)
print(dst, subprocess.run(['ffprobe', '-v', 'error', '-show_entries', 'format=duration', '-of', 'csv=p=0', dst], capture_output=True, text=True).stdout.strip())
