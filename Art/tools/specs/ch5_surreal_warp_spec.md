# Chapter 5 — surrealist warp pass for walls & objects (Codex task)

Write a NEW script `Art/tools/blender_ch5_surreal_warp.py` (Blender 5.3, background) and run it:
`"C:/Program Files/Blender Foundation/Blender 5.3/blender.exe" -b --factory-startup -P Art/tools/blender_ch5_surreal_warp.py`
Short Korean header comment. Print `WARPED <name> <tris>` per file and `DONE` at the end.

Why: the user says chapter 5 looks plain and wants the WALLS and OBJECTS to feel distorted and twisted like the village buildings, SURREALIST (Dali melting clocks, Escher). Study `Art/tools/blender_kit_village_buildings.py` (`dream_volume` → `warp(p)`: baked bend / taper / twist / bulge / sagging top, door pocket kept straight) and apply the same kind of deformation as a POST-PROCESS to the already-exported chapter-5 FBX files.

## Input / output
- Process every `Game/Assets/Art/Characters/Ch5/Kit_*.fbx` whose name starts with: `Kit_Wall`, `Kit_Cover`, `Kit_LM_`, `Kit_Warped`, `Kit_MirrorShard`, `Kit_RiftCrack`, `Kit_Gate_`, `Kit_Portal_`, `Kit_Corridor_`, `Kit_RewardDoor`, `Kit_FD_`, `Kit_BA_HandSpire`, `Kit_BA_FloatingNumeral` (skip files that do not exist; skip `Kit_W_`, `Kit_EP_`, `Kit_BP_`, `Kit_Floor`, `Kit_FX_`, `Kit_BA_DialRing`, `Kit_Ch5_`).
- BEFORE the first warp, copy each original to `Art/models/ch5_unwarped/<same name>.fbx` (only if that backup does not exist yet) and ALWAYS read from the backup, so re-running the script never warps twice.
- Import with the default FBX importer, keep materials (names matter — the game recolours by material name), apply the warp to vertex positions, export back to the same `Game/Assets/Art/Characters/Ch5/<name>.fbx` with `axis_forward='-Z', axis_up='Y', apply_scale_options='FBX_SCALE_UNITS', bake_space_transform=True, object_types={'MESH'}, use_mesh_modifiers=True, mesh_smooth_type='FACE', add_leaf_bones=False, bake_anim=False`. Check the imported object transform convention so the exported file has the same orientation/origin/size as the input (compare bounding boxes before/after; size may grow at most ~12%).

## The warp (seeded per file name, deterministic)
1. Densify first: subdivide edges longer than ~0.25 m (bmesh, keep UVs/materials) so bending is smooth; cap the result at ~3x the original triangle count.
2. Normalised height t = (z - zmin) / height. Apply, in the piece's local space:
   - BEND / LEAN: x += lean * t^2 (lean = ±(0.06-0.14) * height), alternating sign per seed,
   - TWIST around the vertical axis through the centre: angle = ±(6-16 deg) * t,
   - BULGE: horizontal scale *(1 + 0.08 * sin(pi * t)) and a pinched waist for tall pieces,
   - MELT (Dali): the TOP edge sags in waves: z -= t^3 * (0.04-0.10) * height * (0.5 + 0.5 * sin(x * f + phase)); for pieces wider than 3 m also let one top corner droop further (melting clock),
   - WOBBLE: small low-frequency noise displacement (amplitude 0.02-0.04 * size).
3. KEEP: the bottom (t < 0.08) unchanged so pieces still sit on the floor, and for WALL pieces (`Kit_Wall*`, gates, portals, corridors, reward door) keep the footprint inside the original X range (walls tile side by side — clamp |x| at the bottom to the original half width) and keep any door / passage opening walkable (do not lean the middle third of gates/portals/corridors into the opening; corridors only get gentle wobble ± melt on top).
4. For FLOAT pieces (`Kit_FD_*`, `Kit_BA_FloatingNumeral`): use the bounding-box centre as pivot instead of the bottom; twist + bend the whole piece (no bottom lock).
5. Recalculate normals.

Render one preview: before/after side by side for 8 representative pieces (Workbench, flat material colours), to `Art/tools/specs/blender_ch5_surreal_warp_preview.png`, and look at it. The deformation must be clearly visible and dreamlike but must not break the pieces apart or flip normals.
