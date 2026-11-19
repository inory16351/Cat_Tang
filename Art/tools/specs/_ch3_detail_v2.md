## DETAIL PASS v2 (chapter 3) — read together with `_ch3_common.md`
The user saw the first version in game and said: "too many objects are a single flat color; carve them more ornately / richer, like high-end 3D".
In game every object is seen from ~55 deg above at small size with a cel shader and black outlines, so richness must come from MANY DISTINCT COLOR REGIONS and CARVED GEOMETRY that reads at a glance — not from tiny noise.

### Rules for every piece in this pass
- Every piece uses AT LEAST 5 different role materials (big landmarks and walls 6-8), and no single material may cover more than ~55% of the visible surface. Break large plain surfaces into bands, panels, insets and trims of different materials.
- Carve: moldings (stacked bevelled bands) at bases and tops, fluting on columns, recessed panels, relief friezes (repeating raised motifs: sun discs, waves, stars, crescent moons, clock dials), chamfered / bevelled edges everywhere (bevel 0.02-0.06, 2-3 segments), scroll / curl ornaments in gold, studs and rivets, cracks rendered as inset grooves in a darker material.
- Mosaic and inlays: rows of small square or diamond tiles in `glaze_teal` / `coral` / `ivory` set into stone; gem studs (`gem_rose`, small octahedra/cabochons) on gold frames and crests; thin `gold` lines separating bands.
- Gold is the signature: ornate frames with layered profiles, filigree curls, sun-ray crests, finials, and gold caps on crystal bases.
- Crystals: multi-tone — outer `crystal`, inner `crystal_core`, a few `crystal_rose` prisms mixed in, gold or bronze collar where they meet rock.
- Stone: mix `sandstone`, `sandstone_light`, `sandstone_dark`, `rock` per block / boulder (not one material for the whole wall); blocks slightly offset and rotated so joints read as carved grooves.
- Still chunky and cel-shaded: no detail thinner than ~0.04 m, no noisy micro-displacement. Triangle budget: roughly 2x the previous limit is fine (up to ~6000 for props, ~8000 for walls / landmarks / MerchantMirror).

### New role material names allowed in this pass (in addition to the common list)
`glaze_teal` (glazed teal tile), `coral` (warm coral tile/paint), `ivory`, `bronze`, `rose_gold`, `gem_rose` (pink gem), `gem_amber` (amber gem), `crystal_rose` (pink crystal), `tile_cream`.
Everything else stays as in `_ch3_common.md` (grey materials named by role, same coordinates, same FBX paths and export settings, same footprints / heights / origins / piece names). Re-render the preview PNG at the end.
