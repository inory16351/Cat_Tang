You are writing ONE Blender 5.x Python script: `Art/tools/blender_kit_story_inner.py` (create this file; do not edit any other file).
It runs headless: `"C:/Program Files/Blender Foundation/Blender 5.3/blender.exe" -b --factory-startup -P Art/tools/blender_kit_story_inner.py`
After writing it, RUN it once with that command and fix errors until it exports every piece and renders the preview. Then look at the preview PNG and fix anything clearly broken (floating parts, missing pieces, wrong facing, pieces unreadable on a white background).

## Base it on the chapter-5 story map script
Read `Art/tools/blender_kit_story_ward.py` FIRST and copy its whole framework into the new script (one self-contained script, read files as UTF-8):
materials(), Part, the geometry helpers, OUTLINE / chunk-floor helpers, SPECS table, KEEP handling, assembled_mesh, triangles, bounds, finish() (decimate to budget, recenter except KEEP, drop unused materials, FBX export with EXACTLY the same export options), preview() and main().
Changes:
- OUTPUT = ROOT / 'Game/Assets/Art/Characters/StoryInner', files `Kit_I_<Name>.fbx`.
- PREVIEW = ROOT / 'Art/tools/specs/blender_kit_story_inner_preview.png', preview world colour very pale grey-white (.93,.93,.95) with a soft key light from the south-east so white pieces still read by their shading.
- ROLES / PALETTE come from the spec below (materials flat grey Principled named by role; Unity recolours by material NAME).
- Header comment in Korean: "내면 세계 맵 '멈춘 1초' 키트 — 아무것도 없는 하얀 방, 2026-11-26", how to run, axis rule, and the mirror pane centre height / width the spec asks for.

## The full spec (Korean) — follow it exactly
Read `Art/tools/specs/kit_story_inner_spec.md` (UTF-8). It lists the scene, palette roles, coordinates, KEEP/origin rules and all 8 pieces with sizes and triangle budgets.
This map is deliberately almost empty and white (user requirement: "an empty white room"). Keep the floor quiet and low-contrast. The Pillar and Mirror still get carved detail (flutes, mouldings, gold vine frame) — no plain single-colour blocks; each built piece uses 3-6 role materials. Chunky: nothing thinner than ~0.03 m. Seeded slight irregularity.
The four memory props (MemHand, MemLantern, MemWatch, MemShawl) must read clearly from a top-down 57-degree camera at about 1 m size.
