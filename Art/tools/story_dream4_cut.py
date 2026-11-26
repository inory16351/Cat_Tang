# 꿈4 '마지막 장' 곡: Gemini 원곡 62.6초(Art/audio_src/gemini/story_dream4.mp3)를 컷 길이(약 29.8초)에 맞게 세 토막 잇기
#   A 0~14.5 (오르골 → 피아노: 어린 날 · 병상) + B 35~45.5 (현악 · 기타 절정: 책 속 · 노을 길) + C 53~60.3 (높은 화음이 길게 사라짐: 그녀 · 11:59:59)
#   토막 사이 1.5초 크로스페이드 → −16 LUFS 48kHz OGG → Resources/Audio/story_dream4.ogg
import subprocess
src = "Art/audio_src/gemini/story_dream4.mp3"
dst = "Game/Assets/Resources/Audio/story_dream4.ogg"
fc = ("[0:a]atrim=0:14.5,asetpts=PTS-STARTPTS[a];[0:a]atrim=35:45.5,asetpts=PTS-STARTPTS[b];[0:a]atrim=53:60.3,asetpts=PTS-STARTPTS[c];"
      "[a][b]acrossfade=d=1.5[ab];[ab][c]acrossfade=d=1.5,afade=t=out:st=28.3:d=1.0,loudnorm=I=-16:TP=-1.5:LRA=11[o]")
subprocess.run(["ffmpeg", "-y", "-v", "error", "-i", src, "-filter_complex", fc, "-map", "[o]", "-ar", "48000", "-ac", "2", "-c:a", "libvorbis", "-q:a", "6", dst], check=True)
print("ok", dst)
