"""11:59 효과음 · 배경음악 생성 (audio-studio-mcp = Stable Audio 3 medium, 로컬 GPU)

실행: "C:/Project/audio-studio-mcp/.venv/Scripts/python.exe" Art/tools/gen_audio.py [sfx|bgm|all] [이름 ...]
  - 원본 WAV(후보 여러 개) → Art/audio_src/{sfx,bgm}/
  - 다듬은 OGG → Game/Assets/Resources/Audio/Sfx/<이름>[_n].ogg · Resources/Audio/<bgm 이름>.ogg
    효과음: 앞뒤 무음 자르기 · 모노 · 피크 −1dB / 배경음악: 끊김 없는 반복(크로스페이드) · −16 LUFS
  - 같은 시드라 다시 돌리면 같은 소리. 마음에 안 드는 건 SEED를 바꿔 그 이름만 다시: gen_audio.py sfx shot
모델 라이선스: Stability AI Community License (상업 = 무료 등록, 연 매출 100만 달러 미만). 생성물 소유권은 사용자.
"""
from __future__ import annotations

import asyncio
import json
import os
import shutil
import sys
from pathlib import Path

from mcp import Client, StdioServerParameters

ROOT = Path(__file__).resolve().parents[2]
SRC = ROOT / "Art/audio_src"
RES = ROOT / "Game/Assets/Resources/Audio"
SERVER = Path(r"C:\Project\audio-studio-mcp\.venv\Scripts\python.exe")

# 게임 톤: 녹아내리는 서부 + 시계 · 태엽 · 시간 (브롤 스타즈처럼 또렷하고 짧게, 카툰 쪽으로 과장)
# name: (소리를 내는 것, 어떻게, 재질, 공간, 길이 초, 후보 수 = 게임에서 무작위로 돌려 쓰는 개수)
SFX = {
    "shot":       ("old western six-shooter revolver", "single sharp gunshot crack with a short punchy tail, no echo repeats",
                   "brass and steel, gunpowder snap", "close mic, dry, video game sound effect", 1.2, 3),
    "reload":     ("revolver cylinder", "cylinder swings out, spins with fast ratcheting clicks like a clockwork gear",
                   "oiled steel, tiny brass clock gears", "close mic, dry", 1.4, 1),
    "reload_end": ("revolver cylinder", "snaps shut with one crisp metallic click and a hammer cock",
                   "steel", "close mic, dry", 0.8, 1),
    "dash":       ("cat gunslinger with a long coat", "very fast whoosh swish of a quick dash",
                   "cloth and air", "close, dry, cartoon game sound", 0.8, 2),
    "justdodge":  ("magical pocket watch", "time freezes: a bright glassy chime with a reversed swell and a deep tick",
                   "crystal and brass", "airy, slight reverb", 1.6, 1),
    "hit":        ("bullet hitting a creature", "short punchy flesh and cloth impact thump",
                   "soft body, fabric", "close mic, dry, game hit sound", 0.5, 3),
    "crit":       ("critical bullet hit", "heavy punchy impact with a bright metallic ring and crunchy bass",
                   "steel and bone, cartoon", "close mic, dry, game critical hit", 0.8, 2),
    "enemy_die":  ("clockwork monster", "collapses into dark smoke: a soft whoomp poof with a fading ghostly hiss",
                   "smoke, cloth, small springs", "close, dry", 1.2, 2),
    "player_hurt": ("small cat", "short pained yowl hiss as it gets hit, with a cushioned thud",
                   "cat voice, fur", "close mic, dry", 0.7, 2),
    "explosion":  ("gunpowder keg", "big explosion boom with wood debris crackle and sand hiss",
                   "wood, gunpowder, sand", "outdoor, short tail", 2.2, 2),
    "heart":      ("magical heart pickup", "warm sparkly rising chime, two bright notes",
                   "glass bells and soft synth", "clean, small reverb, game pickup", 0.9, 1),
    "levelup":    ("level up fanfare", "short triumphant ascending arpeggio with a shimmering clock chime at the end",
                   "brass, bells, music box", "clean, game UI", 1.8, 1),
    "door":       ("heavy old wooden saloon door", "swings open with a creak and a solid thump",
                   "dry wood, iron hinges", "indoor, close", 1.4, 1),
    "merchant":   ("dreamlike mystical merchant appearing", "magical shimmer rising out of the ground with a soft clock ticking",
                   "glass, sand, music box", "airy reverb", 2.0, 1),
    # ---- UI · 마을 시설 (2026-11-19) ----
    "ui_click":   ("small brass clock button", "one short crisp tick click", "brass, wood", "close mic, dry, UI sound", 0.5, 2),
    "ui_hover":   ("tiny pocket watch gear", "very soft subtle tick", "brass", "close mic, dry, quiet UI hover sound", 0.5, 1),
    "ui_close":   ("old leather book", "closes with a soft thump and a little paper flutter", "leather, paper", "close mic, dry, UI", 0.5, 1),
    "ui_buy":     ("golden time shard coins", "pleasant purchase: a sparkly coin clink with a short rising chime", "gold, glass", "clean, UI", 0.9, 1),
    "ui_deny":    ("small wooden clock", "short low dull double knock, error buzz feeling", "wood", "close mic, dry, UI error", 0.4, 1),
    "fac_memory": ("dreamy memory room", "opens with a soft magical music box chime and a gentle whoosh of sparkles", "music box, glass", "airy, soft reverb", 1.5, 1),
    "fac_ammo":   ("blacksmith ammo workshop", "a hammer strikes an anvil twice with a hiss of hot metal and clinking bullets", "steel, brass shells", "indoor workshop", 1.3, 1),
    "fac_gun":    ("western gun shop", "a lever-action rifle racks and a revolver hammer cocks, then a shop bell dings", "steel, wood, bell", "indoor, close", 1.3, 1),
    "fac_wardrobe": ("tailor wardrobe", "fabric swish, scissors snip twice and a soft wooden drawer slide", "cloth, steel scissors, wood", "indoor, close", 1.2, 1),
    "fac_gate":   ("giant grandfather clock gate", "deep resonant clock gong with slow ticking and a humming time portal", "brass, wood, magic", "large space reverb", 2.0, 1),
    "gate_depart": ("time portal", "swirling whoosh rising into a bright chime as you step through", "air, glass, magic", "airy reverb", 1.6, 1),
    "talk":       ("cute dialogue text blip", "single tiny soft plucked typewriter blip", "music box tine", "close, dry, game dialogue", 0.5, 3),
    # 건카타(불릿 파티) 도는 내내 반복 (끊김 없이 이어지게 다듬음)
    "gunkata_spin": ("twin revolvers spun rapidly around a whirling gunslinger", "continuous fast whirling whoosh with rhythmic metallic spinning clicks, steady, no start or end",
                   "steel, brass, swirling air", "close, dry, seamless loop for a game", 3.0, 1),
    "bridge":     ("broken floating stone bridge", "pieces float back together: rumbling stones clack into place with a magical shimmer", "stone, wood, magic", "outdoor, open air", 2.5, 1),
    # ---- 적 스킬 전용 모션에 맞춘 소리 (2026-11-19 사용자: 새 모션에 어울리는 효과음) — 공격이 나가는 박자에 냄 ----
    # 4장 시계 장치 속
    "e4_rifle":   ("toy soldier clockwork musket", "single sharp gunshot crack with a tiny springy click before it and a short metallic ring",
                   "brass, steel, clockwork", "close mic, dry, video game sound effect", 0.9, 2),
    "e4_drum":    ("small toy snare drum on a soldier's hip", "two quick crisp military drum taps, ra-tap", "wood, taut drum skin, snare wires", "close mic, dry", 0.8, 1),
    "e4_bayonet": ("bayonet charge", "fast forward whoosh ending in a sharp steel stab shing", "steel, air", "close mic, dry, game sound", 0.7, 1),
    "e4_bell":    ("small brass hand bell struck hard by a hammer", "one loud clang followed by a pulsing wobbling sonic ring that fades out",
                   "brass bell, iron hammer", "small room, short reverb", 1.6, 2),
    "e4_spin":    ("clockwork music box ballerina twirling", "fast spinning music box melody over a whirring wind-up spring, steady and continuous",
                   "music box tines, spring, brass", "close, slight reverb", 3.0, 1),
    "e4_finale":  ("music box finale", "bright sparkling music box chord burst ending with a ting", "metal tines, glass", "close, slight reverb", 1.0, 1),
    "e4_tick":    ("second hand of a giant clock", "single sharp heavy tick with a tiny electric zap", "brass, steel spring", "close mic, dry", 0.4, 2),
    "e4_stab":    ("thin needle blade", "very fast zip thrust with a thin metallic shing", "silver steel, air", "close mic, dry, game sound", 0.5, 2),
    "e4_scythe":  ("giant pendulum scythe", "heavy swinging whoosh with a deep tick and a humming metal tail", "iron, brass, air", "close mic, dry, game sound", 0.9, 2),
    "e4_whirl":   ("giant pendulum scythe spun in a full circle", "heavy whirling swoosh all the way around with a ringing metal sweep", "iron, air", "close mic, dry", 1.0, 1),
    "e4_wind":    ("clockwork wind-up key turned by a wrench", "rapid ratcheting cranking clicks of a spring being wound tight", "brass, steel spring", "close mic, dry", 1.0, 1),
    "e4_toss":    ("big steel wrench thrown end over end", "spinning whirring whoosh as it flies", "steel, air", "close mic, dry", 0.7, 1),
    "e4_clank":   ("heavy steel wrench", "lands with a loud metallic clank and a small bounce", "steel on stone", "close mic, dry", 0.6, 1),
    # 1~3장 새 모션
    "e_fan":      ("old western revolver fanned rapidly", "three very fast gunshots in a row, bang-bang-bang", "steel, gunpowder", "close mic, dry, game sound", 0.8, 1),
    "e_slam":     ("heavy brute", "smashes both fists into the ground: deep thud with rocks and dust crunching", "earth, stone, wood", "close, outdoor", 1.0, 2),
    "e_spin":     ("heavy club swung around in a full circle", "big whooshing swing all the way around", "wood, air", "close mic, dry", 0.8, 1),
    "e_whistle":  ("conductor's brass whistle", "one loud shrill whistle blast", "brass whistle", "outdoor, short reverb", 0.9, 1),
    "e_glass":    ("magic mirror shield", "cracks and shatters into glass shards with a shimmer", "glass, crystal", "close, short reverb", 1.0, 1),
    "e_dive":     ("figure diving into a magic mirror", "glassy swoosh with a liquid ripple", "glass, water, magic", "close, slight reverb", 0.8, 1),
    "e_conjure":  ("magic orbs of light gathering", "soft rising sparkling shimmer swell", "glass, chimes", "airy reverb", 1.5, 1),
    "e_bow":      ("stage magician", "theatrical flourish: a magic poof with a short sparkling glissando", "cloth, chimes, smoke", "small theater reverb", 1.0, 1),
    "e_steam":    ("steam locomotive valve", "big pressurized steam blast hiss burst", "steam, iron", "outdoor, close", 1.5, 1),
    # ---- 5장 '무너지는 시간의 끝' (보라 · 금 우주, 깨진 거울 · 균열) 2026-11-19 ----
    "e5_shard_shot":  ("crystal shard rifle", "sharp glassy gunshot crack with a bright crystalline ring tail", "glass, quartz, magic", "close mic, dry, game sound", 0.8, 2),
    "e5_shard_split": ("glass bullet hitting a wall", "shatters and splits into three: crisp glass crack with tinkling shards flying apart", "thin glass, crystal", "close, dry", 0.7, 2),
    "e5_star_place":  ("glowing star pinned in the air", "single bright twinkling ting with a soft magical shimmer", "glass bells, chimes", "airy, slight reverb", 0.6, 2),
    "e5_star_line":   ("beam of starlight connecting two stars", "fast rising electric zap that hums and sizzles along a line", "magic energy, glass", "close, slight reverb", 0.9, 1),
    "e5_star_burst":  ("constellation triangle exploding", "bright magical burst boom with sparkling glassy debris", "starlight, crystal", "medium reverb", 1.2, 1),
    "e5_giant_slam":  ("giant crystal rock golem", "smashes a huge hammer into the floor: deep heavy thud with cracking ground and glass splinters", "stone, crystal, glass", "close, short reverb", 1.2, 2),
    "e5_giant_dash":  ("heavy crystal rock golem charging", "fast rumbling stomp rush ending in a crunch", "stone, crystal", "close", 0.9, 1),
    "e5_rift_dive":   ("figure sinking into a crack in space", "reversed glassy whoosh pulled into a deep void hum", "void energy, glass", "close, slight reverb", 0.8, 1),
    "e5_rift_emerge": ("assassin bursting out of a crack in space", "glass shatter crack with a fast slicing whoosh", "glass, void energy, steel", "close, dry", 0.8, 1),
    "e5_slash":       ("purple void blade", "very fast slicing swish with a thin glassy shing", "steel, glass, air", "close mic, dry, game sound", 0.5, 2),
    "e5_rift_open":   ("crack opening in the air like a broken mirror", "creaking glass splintering with a low ominous void hum swelling", "glass, void", "close, slight reverb", 1.5, 2),
    "e5_rift_shatter": ("crack in space shattering", "loud glass explosion with many sharp shards scattering", "glass, crystal, magic", "close, short reverb", 1.0, 2),
    "e5_void_lob":    ("dark void orb tossed by a sorcerer", "soft whooshing throw with a warbling magical hum", "void energy, air", "close, dry", 0.7, 1),
    "e5_void_land":   ("dark void orb landing", "muffled magical impact thump with a short glassy crack", "void energy, glass, stone", "close, dry", 0.7, 1),
    "e5_phantom_split": ("mirror phantom splitting into reflections", "shimmering glassy swell with layered echoes", "mirror glass, magic", "airy reverb", 1.0, 1),
    "e5_phantom_break": ("mirror reflection clone breaking", "quick bright glass shatter with a ghostly sigh", "mirror glass", "close, short reverb", 0.7, 2),
    # ---- 5장 보스 '시간의 주인' (2026-11-20) ----
    "e5b_reap_wind":  ("giant reaper scythe raised high behind", "slow ominous rising whoosh with a deep creaking tick, building tension", "heavy steel, cloth, air", "large hall reverb", 1.2, 1),
    "e5b_reap_dash":  ("ghostly reaper dashing forward with a giant scythe", "violent fast ghostly swoosh with a reversed whisper and a heavy blade cut at the end", "steel, wind, spectral", "close, short reverb", 1.0, 2),
    "e5b_time_cut":   ("clock time being cut away", "sharp glassy clock tick that shatters and rewinds quickly, warped", "glass, brass clockwork", "close, slight reverb", 0.9, 1),
    "e5b_bell":       ("enormous cathedral clock bell", "one deep booming toll with a long shimmering metallic decay", "bronze bell", "huge reverb", 2.5, 1),
    "e5b_stop":       ("time freezing in a field", "deep descending whoomp with glassy crystallizing crackle and a stopped-clock tick", "glass, ice, clockwork", "airy reverb", 1.5, 1),
    # ---- 보스 등장 연출 (2026-11-22 사용자: 연출 효과음도 만들고 보스 브금과 부드럽게) — BossIntro가 있으면 쓰고 없으면 예전 소리 ----
    "intro_riser":    ("cinematic tension riser", "a rising whoosh with a ticking clock accelerating underneath, building suspense", "air, brass clock ticks, low strings", "cinematic, wide stereo", 1.8, 1),
    "intro_whoosh":   ("fast camera whip pan", "a quick airy swish whoosh with a short tail", "air", "close, dry, cinematic transition", 0.6, 2),
    "intro_impact":   ("cinematic freeze frame hit", "a huge low boom with a sharp clock tick and a glassy time-stop shimmer", "sub bass, glass, clockwork", "big, short reverb tail", 1.8, 1),
    "intro_stinger_1": ("western showdown stinger", "a dramatic twangy electric guitar chord sting with a whip crack and a ricochet", "electric guitar, whip, desert", "cinematic", 2.2, 1),
    "intro_stinger_2": ("steam train boss stinger", "a loud steam locomotive horn blast with a heavy iron clang and hiss", "steam, iron, brass horn", "big station hall", 2.2, 1),
    "intro_stinger_3": ("mirror lake magic stinger", "a shimmering glass chime chord swell with a deep whale call and crystal sparkle", "glass, crystal, whale song", "airy cavern reverb", 2.4, 1),
    "intro_stinger_4": ("clock tower noir stinger", "a single heavy cathedral bell toll with a dark low brass hit and ticking", "bronze bell, brass, clockwork", "tower reverb", 2.4, 1),
    "intro_stinger_5": ("end of time final boss stinger", "an ominous choir swell into a deep bell and a reversed clock chime, dark and epic", "choir, bell, glass, sub bass", "huge cathedral reverb", 2.8, 1),
    "intro_flip":     ("split-flap departure board", "rapid mechanical flap clatter settling into place", "plastic flaps, small motor", "close, dry", 1.0, 1),
    "intro_glitch":   ("digital glitch", "a stuttering corrupted static burst with a reversed tape stop", "digital noise, tape", "close, dry", 0.8, 1),
    "e5b_doom_orb":   ("three dark doom orbs summoned", "low ominous pulse with a magical crackle swelling", "dark magic energy", "medium reverb", 1.2, 1),
    "e5b_spear":      ("giant clock hand thrown like a spear", "fast piercing whoosh ending in a metallic impact", "steel, air", "close, dry", 0.8, 1),
    "e5b_sand":       ("swirling sand and time storm", "rushing whirling wind with hissing sand and ticking clocks inside, continuous", "sand, wind, brass", "outdoor, wide", 3.0, 1),
    "e5b_summon":     ("ghost of a past boss summoned from a broken clock", "eerie reversed choir-like swell into a glass crack", "spectral, glass", "large reverb", 2.0, 1),
    "e5b_beam":       ("watchful clock eye beam", "charging hum that fires a sharp laser zap", "magic energy", "close, slight reverb", 1.0, 1),
    "e5b_hourglass":  ("hourglass flipped over by a bullet", "crisp glass clink with sand pouring and a bright rewind chime", "glass, sand", "close, small reverb", 0.9, 1),
    "e5b_seal":       ("glowing clock number seal on the floor sealed", "bright locking chime with a stone thud", "stone, magic chime", "medium reverb", 1.0, 1),
    "e5b_phase":      ("time prison collapsing", "huge rumbling crack with shattering glass and a deep reversed boom", "stone, glass, cosmic", "huge reverb", 3.0, 1),
}
SEED = {}   # 이름: 시드 (기본 1159)
LOOPS = {"gunkata_spin"}   # 끊김 없이 반복되게 다듬을 효과음

# 배경음악: 이름: (프롬프트, BPM, 길이 초). 끊김 없이 반복되게 끝을 앞에 겹침
BGM = {
    "bgm_village": ("Dreamy calm instrumental for a floating village at dawn under a starry sky: soft spaghetti-western "
                    "acoustic guitar, music box melody, gentle ticking clock percussion, warm pads, melancholic and hopeful",
                    80, 120),
    "bgm_stage_1": ("Upbeat spaghetti western action instrumental for a melting surreal desert town: twangy electric "
                    "guitar, whistling melody, galloping drums, ticking clock percussion, slightly warped and wobbly "
                    "tape-melt effect, adventurous and catchy video game battle music",
                    124, 120),
    "bgm_midboss_1": ("Tense spaghetti western showdown instrumental against a clockwork bounty hunter on a main street: "
                      "suspenseful twangy guitar, harmonica stabs, snare rolls, ticking clock, quickening rhythm, "
                      "video game mini-boss battle music",
                      132, 120),
    "bgm_boss_1":  ("Intense spaghetti western boss battle instrumental: aggressive twangy guitar riffs, dramatic trumpet "
                    "mariachi melody, pounding toms, heavy ticking clock and bell hits, epic and tense video game boss music",
                    140, 120),
    "bgm_stage_2": ("Steampunk train station action instrumental where time is frozen: driving chugging rhythm like a "
                    "steam locomotive, western guitar, brass, clockwork percussion, whistle accents, energetic video game "
                    "battle music",
                    128, 120),
    "bgm_midboss_2": ("Tense mini-boss battle instrumental in a frozen train station waiting hall against a clockwork "
                      "station master: ticking station clock, ringing platform bells, tight snare, staccato brass, "
                      "western guitar, urgent video game mini-boss music",
                      136, 120),
    "bgm_boss_2":  ("Epic boss battle instrumental against a giant stopped locomotive: heavy industrial percussion, train "
                    "chug rhythm, distorted western guitar, blaring brass, steam hiss hits, ticking clock, intense and "
                    "relentless video game boss music",
                    146, 120),
    "bgm_stage_3": ("Dreamy mysterious action instrumental for a mirage desert mirror world: shimmering glassy synths, "
                    "reverb western guitar, middle-eastern hand drums, ethereal choir pads, sparkling bells, adventurous "
                    "video game battle music",
                    118, 120),
    "bgm_midboss_3": ("Playful sinister mini-boss battle instrumental in a mirror theater against a mirage magician: "
                      "circus waltz feel, twisted music box, pizzicato strings, glassy bells, theatrical brass hits, "
                      "mysterious video game mini-boss music",
                      128, 120),
    "bgm_boss_3":  ("Majestic intense boss battle instrumental against a giant mirror whale in the sky: soaring orchestral "
                    "strings, ethereal choir, crystal glass percussion, deep whale-like synth swells, western guitar "
                    "accents, epic video game boss music",
                    136, 120),
}


async def call(c: Client, name: str, args: dict) -> dict:
    if c is None:   # 같은 프로세스에서 도구 함수를 바로 부름 (direct)
        import audio_studio_mcp.server as S
        return await getattr(S, name)(**args)
    r = await c.call_tool(name, args)
    text = "".join(getattr(x, "text", "") for x in r.content)
    if r.is_error:
        raise RuntimeError(f"{name} failed: {text[:400]}")
    return json.loads(text)


async def make_sfx(c: Client, name: str) -> None:
    src, act, mat, space, secs, n = SFX[name]
    g = await call(c, "generate_audio", {
        "audio_type": "sfx", "duration_seconds": secs, "variations": n, "seed": SEED.get(name, 1159),
        "sound_source": src, "sound_action": act, "sound_material": mat, "sound_space": space,
        "title": name, "original_idea": f"11:59 game SFX: {name}"})
    (RES / "Sfx").mkdir(parents=True, exist_ok=True)
    for old in (RES / "Sfx").glob(f"{name}_*.ogg"):
        old.unlink()
    for i, cand in enumerate(g["candidates"]):
        stem = name if n == 1 else f"{name}_{i + 1}"
        loop = name in LOOPS
        p = await call(c, "process_audio", {
            "input_audio": cand["path"], "trim_silence": not loop, "silence_threshold_db": -45, "mono": True,
            "normalize": "peak", "peak_ceiling_db": -1, "fade_out_ms": 0 if loop else 30,
            "loop": loop, "loop_crossfade_ms": 250,
            "format": "ogg", "title": stem, **({"sample_rate": 48000} if shutil.which("ffmpeg") else {})})   # ffmpeg 없으면 원래 샘플레이트 그대로 (Unity는 상관없음)
        shutil.copy(p["path"], RES / "Sfx" / f"{stem}.ogg")
    print(f"sfx {name}: {n}개, {g.get('wall_seconds')}초", flush=True)


async def make_bgm(c: Client, name: str) -> None:
    prompt, bpm, secs = BGM[name]
    g = await call(c, "generate_audio", {
        "audio_type": "music", "prompt": prompt, "bpm": bpm, "duration_seconds": secs, "variations": 1,
        "seed": SEED.get(name, 1159), "title": name, "original_idea": f"11:59 game BGM: {name}"})
    p = await call(c, "process_audio", {
        "input_audio": g["candidates"][0]["path"], "normalize": "loudness", "target_lufs": -16,
        "loop": True, "loop_crossfade_ms": 1500, "format": "ogg", "sample_rate": 48000, "title": name + "_loop"})
    shutil.copy(p["path"], RES / f"{name}.ogg")
    print(f"bgm {name}: {secs}초, {g.get('wall_seconds')}초 걸림", flush=True)


def low_memory_loader() -> None:
    """2026-11-19: 공식 로더는 fp32 모델(9.2GB) + 체크포인트 전체(9.2GB)를 한꺼번에 CPU 메모리에 올려
    페이징 파일이 작은 PC(12GB)에서 access violation으로 죽음. 같은 결과를 내되 모델을 먼저 fp16으로 줄이고
    가중치를 텐서 하나씩 읽어 채움 → 최대 약 9GB. vendor 코드는 그대로 두고 여기서만 바꿔 끼움."""
    import torch
    from safetensors import safe_open
    import stable_audio_3.model as sa3_model
    from stable_audio_3.factory import create_diffusion_cond_from_config

    def load(model_config, ckpt_path, device="cuda", model_half=False):
        model = create_diffusion_cond_from_config(model_config)
        if model_half:
            model.to(torch.float16)                         # 원래도 마지막에 fp16으로 바꿈 — 먼저 바꿔 메모리 절반
        msd = model.state_dict()
        with safe_open(ckpt_path, "pt", device="cpu") as f:
            for k in f.keys():
                key = k
                if key not in msd:                           # 공식 remap_state_dict_keys와 같은 규칙
                    parts = k.split(".")
                    for i in range(1, len(parts)):
                        cand = ".".join(parts[:i]) + "." + ".".join(parts[i + 1:])
                        if cand in msd:
                            key = cand
                            break
                t = f.get_tensor(k)
                if key in msd and msd[key].shape == t.shape:
                    msd[key].copy_(t)
                del t
        model.to(device).eval().requires_grad_(False)
        if model_half:
            model.to(torch.float16)
        return model

    sa3_model.load_diffusion_cond = load


async def run_all(c, kind: str, only: list[str]) -> None:
    if kind in ("sfx", "all"):
        for n in only or SFX:
            if n in SFX: await make_sfx(c, n)
    if kind in ("bgm", "all"):
        for n in only or BGM:
            if n in BGM: await make_bgm(c, n)
    await call(c, "unload_models", {})


async def main(kind: str, only: list[str]) -> None:
    out = SRC / ("sfx" if kind == "sfx" else "bgm" if kind == "bgm" else "")
    out.mkdir(parents=True, exist_ok=True)
    if DIRECT:
        # 2026-11-19: MCP 서버 안에서는 작업 스레드가 모델 파일(safetensors)을 읽다 access violation으로 죽어서,
        # 기본은 같은 프로세스에서 메인 스레드로 모델을 먼저 올리고 도구 함수를 바로 부름
        os.environ["AUDIO_STUDIO_OUTPUT_DIR"] = str(out)
        await run_all(None, kind, only)
    else:
        params = StdioServerParameters(command=str(SERVER), args=["-m", "audio_studio_mcp"],
                                       env={**os.environ, "AUDIO_STUDIO_OUTPUT_DIR": str(out), "PYTHONIOENCODING": "utf-8", "PYTHONFAULTHANDLER": "1"})
        async with Client(params, read_timeout_seconds=1800) as c:
            await run_all(c, kind, only)
    print("DONE", flush=True)


if __name__ == "__main__":
    sys.stdout.reconfigure(errors="replace")
    args = [a for a in sys.argv[1:] if a != "--mcp"]
    DIRECT = "--mcp" not in sys.argv[1:]          # --mcp = 예전처럼 MCP 서버를 띄워서
    kind = args[0] if args else "all"
    if DIRECT:
        # 모델은 비동기 루프 밖, 메인 스레드에서 먼저 올림 (루프 안에서 올리면 safetensors 읽다 access violation)
        os.environ["AUDIO_STUDIO_OUTPUT_DIR"] = str(SRC / ("sfx" if kind == "sfx" else "bgm" if kind == "bgm" else ""))
        import audio_studio_mcp.server as S
        backend = S.ready_backend()
        low_memory_loader()
        backend._load()
    asyncio.run(main(kind, args[1:]))
