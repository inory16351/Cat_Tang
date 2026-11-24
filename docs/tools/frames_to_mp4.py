# 연출 녹화 프레임(IntroDemo.Record: f0000_<ms>.png, 내용은 JPG) → MP4 (프레임 간격 = 파일 이름의 실제 시각) + 곡 · 효과음 입힘
# 실행: python docs/tools/frames_to_mp4.py <프레임 폴더> <출력.mp4> [곡 파일] [곡 시작 오프셋 초] [--log Editor.log]
#   --log: Unity Editor.log의 마지막 [RecordStart] 뒤 [MontageStart] · [MontageSfx] 줄을 읽어
#          곡은 몽타주 시작 시각에, '!' 효과음(Resources/Audio/Sfx/<이름>.ogg)은 그 시각에 섞음
import glob, os, re, subprocess, sys

args = [a for a in sys.argv[1:]]
log = None
if "--log" in args:
    i = args.index("--log"); log = args[i + 1]; del args[i:i + 2]
src, out = args[0], args[1]
music = args[2] if len(args) > 2 else None
off = float(args[3]) if len(args) > 3 else 0.0
SFX = r"C:/Project/Cat_Tang/Game/Assets/Resources/Audio/Sfx"

events = []
if log:
    lines = open(log, encoding="utf-8", errors="ignore").read().splitlines()
    starts = [i for i, l in enumerate(lines) if l.startswith("[RecordStart]")]
    if starts:
        lines = lines[starts[-1]:]
        t0 = float(re.search(r"t=([\d.]+)", lines[0]).group(1))
        for l in lines:
            m = re.match(r"\[MontageStart\] t=([\d.]+)", l)
            if m and music:
                off = float(m.group(1)) - t0
            m = re.match(r"\[MontageSfx\] (\S+) t=([\d.]+)", l)
            if m:
                events.append((m.group(1), float(m.group(2)) - t0))

files = sorted(glob.glob(os.path.join(src, "f*_*.png")))
ts = [int(re.search(r"_(\d+)\.png$", f).group(1)) / 1000 for f in files]
lst = os.path.join(src, "concat.txt")
with open(lst, "w", encoding="utf-8") as fp:
    for i, f in enumerate(files):
        d = (ts[i + 1] - ts[i]) if i + 1 < len(files) else 0.1
        fp.write(f"file '{os.path.abspath(f).replace(os.sep, '/')}'\nduration {max(0.001, d):.4f}\n")
    fp.write(f"file '{os.path.abspath(files[-1]).replace(os.sep, '/')}'\n")
cmd = ["ffmpeg", "-v", "error", "-y", "-f", "concat", "-safe", "0", "-i", lst]
audio = []
if music:
    cmd += ["-i", music]; audio.append(("m", off))
for name, t in events:
    cmd += ["-i", os.path.join(SFX, name + ".ogg")]; audio.append(("s", t))
if audio:
    parts, labels = [], []
    for k, (kind, t) in enumerate(audio):
        ms = max(0, int(round(t * 1000)))
        vol = 1.0 if kind == "m" else 0.9
        parts.append(f"[{k + 1}:a]adelay={ms}|{ms},volume={vol}[a{k}]"); labels.append(f"[a{k}]")
    fc = ";".join(parts) + ";" + "".join(labels) + f"amix=inputs={len(labels)}:normalize=0[aout]"
    cmd += ["-filter_complex", fc, "-map", "0:v", "-map", "[aout]", "-c:a", "aac", "-b:a", "192k", "-shortest"]
cmd += ["-vf", "fps=30,format=yuv420p", "-c:v", "libx264", "-crf", "18", "-preset", "medium", out]
subprocess.run(cmd, check=True)
print(out, len(files), "frames", f"{ts[-1]:.1f}s", "music@", round(off, 3), "sfx", [(n, round(t, 2)) for n, t in events])
