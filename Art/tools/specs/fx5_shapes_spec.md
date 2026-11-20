# Chapter 5 stylized VFX shape meshes (Codex task)

Write a NEW script `Art/tools/blender_fx5_shapes.py` (Blender 5.3, background) and run it:
`"C:/Program Files/Blender Foundation/Blender 5.3/blender.exe" -b --factory-startup -P Art/tools/blender_fx5_shapes.py`
Short Korean header comment at the top (what it makes, how to run). Print `EXPORTED <name> <tris>` per mesh and `DONE` at the end.

Why: the game recreates the "5 nodes stylized VFX" technique in a Unity shader (`CatTang/StylizedVfx`): a scrolling noise/voronoi pattern on simple meshes, stepped colour ramp, alpha threshold driven by an age value (dissolve), fresnel rim. The meshes only need clean shape + good UVs; ALL the look comes from the shader. So: no materials needed (one default material is fine), smooth shading, sensible poly counts (200-2000 tris), and UVs laid out exactly as described because the shader scrolls the noise along U and dissolves along V.

## Coordinates / export
Blender Z-up; export each mesh to its own FBX `Game/Assets/Resources/FX/Meshes5/fx5_<name>.fbx` (create the folder) with
`axis_forward='-Z', axis_up='Y', apply_scale_options='FBX_SCALE_UNITS', bake_space_transform=True, object_types={'MESH'}, use_mesh_modifiers=True, mesh_smooth_type='FACE', add_leaf_bones=False, bake_anim=False`.
Origin at the effect centre on the ground (Z 0) unless noted. Unit size (radius 1 m) — the game scales them.

## UV rule (important)
- U (0..1) runs ALONG the main direction of motion / around the shape (around the ring, along the slash arc, around the tornado), so scrolling U makes the pattern flow.
- V (0..1) runs ACROSS the shape: 0 = inner edge / bottom / leading edge, 1 = outer edge / top / trailing edge.
- U may exceed 1 for long shapes (e.g. 0..3 around a tornado that wraps 3 times) so the noise repeats instead of stretching.

## Meshes
1. `dome` — hemisphere radius 1 (open bottom), 32 x 12 segments. U around (0..2), V from rim (0) to top (1). Normals outward.
2. `ring` — flat annulus on the ground, inner radius 0.55, outer radius 1.0, 64 segments around, 2 rings across. U around (0..3), V inner (0) → outer (1). Faces up.
3. `ring_tall` — a short open cylinder band (shock wall): radius 1, height 0.35, slightly flared outward at the top (top radius 1.12), 64 segments. U around (0..3), V bottom (0) → top (1).
4. `swirl` — a spinjitzu-style tornado: 3 ribbon strands, each a twisted band that spirals upward 1.5 turns from radius 0.35 at the bottom (Z 0) to radius 1.0 at the top (Z 2.2), band width 0.45 m, leaning slightly into the spiral direction, 120 segments along each strand, double-sided geometry is NOT needed (the shader renders both sides). U along the strand (0..3), V across the band.
5. `slash_arc` — a horizontal crescent blade trail for a giant scythe sweep: lies flat (in the XY plane at Z 1.0 height above origin? NO: put it at Z 0, the game positions it), sweeping 200 degrees counter-clockwise from -100 deg to +100 deg around the origin, inner radius 0.45, outer radius 1.0, and the band TAPERS: full width in the middle, both ends pinched to a point (crescent). 96 segments along, 4 across. U along the arc (0 = start/back of the swing, 1 = end/front), V inner (0) → outer (1). Faces up (+Z).
6. `spiral_ball` — a sphere radius 0.7 wrapped by 2 thick spiral bands (like the cyan ball in the reference: a ball made of swirling stripes): build it as a UV sphere 48x24 whose U = longitude*2 + latitude*1.5 (so stripes spiral when the shader thresholds a stripe pattern), V = latitude 0..1.
7. `spike_burst` — 10 thin cone spikes radiating outward horizontally from the centre (length 1.0, base radius 0.06), slightly random tilt up/down (seeded), each spike U along its length (0 = base, 1 = tip), V around.

Render ONE preview PNG of all meshes in a row (Workbench, flat colours, show the UV pattern by using a checker texture or by colouring faces by U) to `Art/tools/specs/blender_fx5_shapes_preview.png` and look at it; fix anything broken.
