# 그리니치 시간 천문대 소품 12종: FBX와 검토용 미리보기를 만듭니다.
# 실행: "C:/Program Files/Blender Foundation/Blender 5.3/blender.exe" -b --factory-startup -P Art/tools/blender_kit_ch5_obsprops.py
"""Observatory replacements. Metres, +Y front, neutral role shaders in FBX."""
import math
import random
import hashlib
from pathlib import Path

import bmesh
import bpy
from mathutils import Euler, Matrix, Vector

ROOT = Path(__file__).resolve().parents[2]
OUT = ROOT / 'Game/Assets/Art/Characters/Ch5'
PREVIEW = ROOT / 'Art/tools/specs/blender_kit_ch5_obsprops_preview.png'
NAMES = ('WarpedKeg', 'WarpedTrunk', 'WarpedSpring', 'CoverShardRock',
         'CoverMemoryPile', 'CoverBrokenColumn', 'FD_Saloon', 'FD_Rail',
         'FD_StationClock', 'FD_Bell', 'FD_DesertPillar', 'FD_MirrorFrame')
TAU = math.tau
MATS, PARTS = {}, []
PALETTE = {
    'void_stone': (.055, .12, .23), 'void_stone_dark': (.022, .046, .095),
    'marble': (.76, .85, .91), 'marble_dark': (.32, .44, .54),
    'indigo': (.035, .070, .18), 'night_sky': (.009, .022, .065),
    'starlight': (.91, .98, 1), 'crack_gold': (.045, .86, 1),
    'gold': (.94, .67, .25), 'gold_dark': (.33, .20, .065),
    'brass': (.64, .43, .17), 'steel': (.31, .46, .58),
    'dial_face': (.91, .87, .72), 'dial_mark': (.018, .035, .072),
    'wood': (.25, .15, .085), 'wood_dark': (.075, .041, .025),
    'cloth': (.09, .23, .39), 'mirror': (.39, .72, .88),
    'mirror_back': (.09, .19, .26), 'crystal': (.48, .83, .95),
    'red_lacquer': (.72, .035, .045),
}


def measure_originals():
    """Import the immutable original FBXs before building anything."""
    measured = {}
    for name in NAMES:
        bpy.ops.wm.read_factory_settings(use_empty=True)
        path = ROOT / 'Art/models/ch5_unwarped' / ('Kit_' + name + '.fbx')
        bpy.ops.import_scene.fbx(filepath=str(path), use_anim=False)
        objects = [o for o in bpy.context.scene.objects if o.type == 'MESH']
        assert len(objects) == 1, (name, len(objects))
        ob = objects[0]
        verts = [ob.matrix_world @ v.co for v in ob.data.vertices]
        lo, hi = [Vector(tuple(f(v[j] for v in verts) for j in range(3)))
                  for f in (min, max)]
        pivot = ob.matrix_world.translation.copy()
        measured[name] = (hi-lo, lo-pivot, hi-pivot)
        print('MEASURED', name, 'dimensions', tuple(hi-lo), 'origin', tuple(pivot),
              'bounds_from_origin', tuple(lo-pivot), tuple(hi-pivot), flush=True)
        assert pivot.length < 1e-4, (name, pivot)
    bpy.ops.wm.read_factory_settings(use_empty=True)
    return measured


def materials():
    for role in PALETTE:
        mat = bpy.data.materials.new(role)
        mat.use_nodes = True
        mat.diffuse_color = (.5, .5, .5, 1)
        bs = mat.node_tree.nodes['Principled BSDF']
        bs.inputs['Base Color'].default_value = (.5, .5, .5, 1)
        bs.inputs['Roughness'].default_value = .8
        MATS[role] = mat


def mesh(label, verts, faces, role, bevel=False):
    data = bpy.data.meshes.new(label)
    data.from_pydata(verts, [], faces)
    data.update()
    ob = bpy.data.objects.new(label, data)
    bpy.context.collection.objects.link(ob)
    roles = [role]*len(faces) if isinstance(role, str) else role
    for r in dict.fromkeys(roles):
        data.materials.append(MATS[r])
    slots = {m.name: i for i, m in enumerate(data.materials)}
    for p, r in zip(data.polygons, roles):
        p.material_index = slots[r]
    if bevel:
        mod = ob.modifiers.new('Two-step stone and metal chamfer', 'BEVEL')
        mod.width, mod.segments = .024, 2
        mod.limit_method, mod.angle_limit = 'ANGLE', math.radians(38)
        mod.use_clamp_overlap = True
    PARTS.append(ob)
    return ob


def place(ob, loc=(0, 0, 0), rot=(0, 0, 0)):
    ob.data.transform(Matrix.Translation(Vector(loc)) @ Euler(rot).to_matrix().to_4x4())
    return ob


def group(start, loc=(0, 0, 0), rot=(0, 0, 0)):
    for ob in PARTS[start:]:
        place(ob, loc, rot)


def poly(label, outline, depth, role, loc=(0, 0, 0), rot=(0, 0, 0), bevel=False,
         side=None):
    """Closed polygon in XY, extruded in Z. Explicitly closed disconnected parts."""
    pts = list(outline)
    if sum(a[0]*b[1]-a[1]*b[0] for a, b in zip(pts, pts[1:]+pts[:1])) < 0:
        pts.reverse()
    n = len(pts)
    verts = [(x, y, z) for z in (-depth/2, depth/2) for x, y in pts]
    faces = [tuple(reversed(range(n))), tuple(range(n, 2*n))]
    faces += [(j, (j+1)%n, (j+1)%n+n, j+n) for j in range(n)]
    roles = [side or role, role]+[side or role]*n
    return place(mesh(label, verts, faces, roles, bevel), loc, rot)


def box(label, loc, size, role, rot=(0, 0, 0), bevel=True):
    x, y, z = (d/2 for d in size)
    return poly(label, [(-x, -y), (x, -y), (x, y), (-x, y)], 2*z,
                role, loc, rot, bevel)


def cyl(label, loc, r, depth, role, n=20, rot=(0, 0, 0), bevel=False):
    return poly(label, [(r*math.cos(TAU*j/n), r*math.sin(TAU*j/n)) for j in range(n)],
                depth, role, loc, rot, bevel)


def arc(label, r, width, depth, a0, a1, role, loc=(0, 0, 0), rot=(0, 0, 0), n=24):
    """Capped rectangular-section instrument arc, with chamfers in its profile."""
    # Eight-sided section gives an actual 2 cm bevel without a dense modifier.
    c = min(.02, width*.22, depth*.22)
    profile = [(r-width+c, -depth/2), (r-c, -depth/2), (r, -depth/2+c),
               (r, depth/2-c), (r-c, depth/2), (r-width+c, depth/2),
               (r-width, depth/2-c), (r-width, -depth/2+c)]
    angles = [a0+(a1-a0)*j/n for j in range(n+1)]
    verts = [(rr*math.cos(a), rr*math.sin(a), z) for a in angles for rr, z in profile]
    faces = [tuple(reversed(range(8))), tuple(range(8*n, 8*n+8))]
    faces += [(8*j+k, 8*j+(k+1)%8, 8*(j+1)+(k+1)%8, 8*(j+1)+k)
              for j in range(n) for k in range(8)]
    return place(mesh(label, verts, faces, role), loc, rot)


def ring(label, r, width, depth, role, loc=(0, 0, 0), rot=(0, 0, 0), n=24):
    # A cyclic 4-corner section is enough for small machined bands.
    profile = [(r-width, -depth/2), (r, -depth/2), (r, depth/2), (r-width, depth/2)]
    verts = [(rr*math.cos(TAU*j/n), rr*math.sin(TAU*j/n), z)
             for rr, z in profile for j in range(n)]
    faces = [(k*n+j, k*n+(j+1)%n, ((k+1)%4)*n+(j+1)%n, ((k+1)%4)*n+j)
             for k in range(4) for j in range(n)]
    return place(mesh(label, verts, faces, role), loc, rot)


def tube(label, points, radius, role, n=4):
    verts = []
    for i, p in enumerate(points):
        t = (Vector(points[min(i+1, len(points)-1)])-Vector(points[max(0, i-1)])).normalized()
        ref = Vector((0, 1, 0)) if abs(t.y) < .9 else Vector((1, 0, 0))
        u = ref.cross(t).normalized()
        w = t.cross(u)
        verts += [tuple(Vector(p)+radius*(math.cos(TAU*j/n)*u+math.sin(TAU*j/n)*w))
                  for j in range(n)]
    faces = [tuple(reversed(range(n))), tuple(range(len(verts)-n, len(verts)))]
    faces += [(i*n+j, i*n+(j+1)%n, (i+1)*n+(j+1)%n, (i+1)*n+j)
              for i in range(len(points)-1) for j in range(n)]
    return mesh(label, verts, faces, role)


def beam(label, a, b, width, depth, role, bevel=False):
    a, b = Vector(a), Vector(b)
    ob = box(label, (0, 0, 0), (width, depth, (b-a).length), role, bevel=bevel)
    ob.data.transform((b-a).to_track_quat('Z', 'Y').to_matrix().to_4x4())
    return place(ob, (a+b)/2)


def star(loc, r=.065, role='starlight'):
    # Thick, four-point astronomical glints; the smallest span is >= 4 cm.
    pts = []
    for j in range(8):
        a = TAU*j/8
        rr = r if j%2 == 0 else r*.35
        pts.append((rr*math.cos(a), rr*math.sin(a)))
    return poly('Raised astronomical star', pts, .045, role, loc)


def chart(loc, size, rot=(0, 0, 0), page=False):
    start = len(PARTS)
    w, h = size
    box('Chart page' if page else 'Recessed midnight star chart', (0, 0, 0),
        (w, h, .05), 'dial_face' if page else 'night_sky', bevel=False)
    pts = [(-.34*w, -.20*h, .052), (-.16*w, .12*h, .052),
           (.05*w, -.05*h, .052), (.26*w, .25*h, .052), (.36*w, -.23*h, .052)]
    tube('Raised constellation', pts, .021, 'steel')
    for p in pts:
        star(p, .045 if page else .058, 'dial_mark' if page else 'starlight')
    group(start, loc, rot)


def dial(loc, radius, rot=(0, 0, 0), cracked=True, ticks=24):
    start = len(PARTS)
    cyl('Dial metal back', (0, 0, -.025), radius, .12, 'steel', n=24)
    cyl('Cream instrument dial', (0, 0, .045), radius*.86, .075, 'dial_face', n=32)
    ring('Broad brass dial bezel', radius, radius*.13, .13, 'brass', n=32)
    for j in range(ticks):
        a = TAU*j/ticks
        rr = radius*.71
        box('24-hour raised tick', (rr*math.cos(a), rr*math.sin(a), .105),
            (.12*radius if j%6 else .20*radius, .042, .04), 'dial_mark',
            rot=(0, 0, a), bevel=False)
    a, length = .8, radius*.63
    box('Single standard-time hand', (length*.5*math.cos(a), length*.5*math.sin(a), .15),
        (length, .048, .045), 'brass', rot=(0, 0, a), bevel=False)
    cyl('Hand pivot', (0, 0, .18), .065, .05, 'gold', n=10)
    if cracked:
        tube('Cracked time dial', [(-radius*.60, radius*.56, .103), (-.05, .06, .103),
                                  (.10, -.04, .103), (radius*.58, -radius*.55, .103)],
             .023, 'crack_gold')
    group(start, loc, rot)


def telescope(loc, length, radius, rot=(0, 0, 0), broken=False):
    """Tube axis is local X. Front end is +X; openings have a real wall."""
    start = len(PARTS)
    n = 20
    verts = []
    for end, rr in ((-1, radius), (1, radius), (1, radius*.78), (-1, radius*.78)):
        for j in range(n):
            a = TAU*j/n
            # Irregular jagged rear lip, not a flat closed cylinder.
            x = end*length/2
            if end < 0 and broken:
                x += .13*math.sin(3*a)+.055*math.cos(7*a)
            verts.append((x, rr*math.sin(a), rr*math.cos(a)))
    faces = [(k*n+j, k*n+(j+1)%n, ((k+1)%4)*n+(j+1)%n, ((k+1)%4)*n+j)
             for k in range(4) for j in range(n)]
    roles = ['wood_dark']*n + ['brass']*n + ['mirror_back']*n + ['steel']*n
    mesh('Hollow refractor barrel with broken lip', verts, faces, roles)
    for x, rr, d in ((-.29*length, radius*1.10, .13), (.24*length, radius*1.12, .15),
                     (.48*length, radius*1.15, .14)):
        ring('Refractor brass collar', rr, .075, d, 'brass', (x, 0, 0), (0, math.pi/2, 0), n=20)
    cyl('Inset silver-blue objective lens', (length*.48, 0, 0), radius*.83, .05,
        'mirror', n=24, rot=(0, math.pi/2, 0))
    # Broad glass glint visible at the end, plus a focus rail on the top.
    beam('Lens light reflection', (length*.515, -.13*radius, -.60*radius),
         (length*.515, .25*radius, .58*radius), .045, .045, 'starlight')
    box('Focus rack', (0, 0, radius+.055), (length*.65, .09, .085), 'steel')
    cyl('Focus wheel', (-length*.20, radius*1.10, radius*.20), radius*.27, .09,
        'gold', n=12, rot=(math.pi/2, 0, 0))
    group(start, loc, rot)


def warped_keg():
    cyl('Squat blue pressure canister', (0, 0, .47), .41, .84, 'indigo', n=24, bevel=True)
    for z in (.10, .47, .83):
        ring('Heavy chronometer brass band', .445, .055, .10, 'brass', (0, 0, z), n=24)
    for j in range(8):
        a = TAU*j/8
        box('Brass panel separator', (.416*math.cos(a), .416*math.sin(a), .47),
            (.05, .055, .60), 'gold_dark', rot=(0, 0, a), bevel=False)
        cyl('Top collar bolt', (.37*math.cos(a), .37*math.sin(a), .91), .043, .055,
            'steel', n=6)
    ring('Small red danger ring', .425, .035, .055, 'red_lacquer', (0, 0, .755), n=24)
    cyl('Sloped dial support', (0, .035, .93), .30, .12, 'brass', n=24)
    dial((0, .035, 1.00), .29, rot=(-.28, 0, 0))
    for x, y in ((-.32, -.13), (.32, -.11)):
        cyl('Steel pressure valve', (x, y, .97), .065, .22, 'steel', n=10)
        ring('Open steel valve handwheel', .105, .04, .045, 'steel', (x, y, 1.09), n=12)
        box('Valve wheel crossbar', (x, y, 1.09), (.16, .042, .04), 'brass', bevel=False)
    tube('Escaping cyan time fracture', [(-.18, .388, .86), (-.06, .443, .72),
         (-.14, .448, .56), (.035, .448, .43), (-.045, .43, .22), (.05, .39, .08)],
         .027, 'crack_gold')
    box('Detached navy pressure panel', (-.36, .18, .97), (.15, .11, .17),
        'indigo', rot=(.2, -.25, -.25))


def warped_trunk():
    box('Closed astronomer instrument chest', (0, 0, .33), (1.26, .72, .58), 'wood_dark')
    box('Blue cloth instrument-case inset', (0, .371, .33), (1.05, .045, .37), 'cloth')
    box('Dark wood sealed lid', (0, 0, .665), (1.30, .77, .14), 'wood_dark')
    chart((0, 0, .76), (1.01, .56))
    for y in (-.35, .35):
        box('Lid brass border', (0, y, .76), (1.28, .055, .06), 'brass', bevel=False)
    for x in (-.60, .60):
        box('Lid end brass border', (x, 0, .76), (.055, .70, .06), 'brass', bevel=False)
        for y in (-.325, .325):
            for z in (.10, .63):
                box('Protective brass corner', (x, y, z), (.18, .16, .15), 'brass')
    for z in (.09, .54):
        box('Case front wood molding', (0, .40, z), (1.18, .06, .07), 'wood', bevel=False)
    box('Central instrument clasp', (0, .425, .51), (.18, .07, .23), 'gold')
    box('Clasp key slot', (0, .47, .51), (.045, .04, .07), 'dial_mark', bevel=False)
    for x in (-.64, .64):
        box('Case carrying handle', (x, 0, .35), (.07, .28, .075), 'steel')
    # Pages float at the back, leaving the lid chart unobscured.
    chart((-.35, -.16, 1.02), (.37, .27), rot=(.16, -.18, -.22), page=True)
    chart((.32, -.23, 1.09), (.34, .25), rot=(-.17, .16, .28), page=True)
    tube('Cyan clasp fracture', [(.07, .468, .67), (.02, .47, .58), (.08, .47, .52)],
         .023, 'crack_gold')


def warped_spring():
    box('Portland stone spring pedestal', (0, 0, .105), (.96, .96, .21), 'marble')
    box('Navy inset base panel', (0, 0, .235), (.84, .84, .08), 'void_stone')
    # Readable ratchet teeth around a solid brass wheel.
    pts = []
    for j in range(16):
        for t, r in ((0, .36), (.18, .44), (.67, .44), (.86, .36)):
            a = TAU*(j+t)/16
            pts.append((r*math.cos(a), r*math.sin(a)))
    poly('Sixteen-tooth winding ratchet', pts, .10, 'brass', (0, 0, .325))
    ring('Ratchet inner channel', .29, .055, .045, 'gold_dark', (0, 0, .39), n=24)
    cyl('Vertical winding arbor', (0, 0, .85), .065, 1.04, 'brass', n=12)
    # A broad metal strip with four open, separated coils.
    points = []
    for j in range(113):
        t = j/112
        a = TAU*4*t
        # Nearly constant radius protects the gaps in the 55-degree game view.
        # A widening cone hid the front turns behind one another in the preview.
        r = .285+.025*t
        points.append((r*math.cos(a), r*math.sin(a), .43+.94*t))
    points += [(.41, .05, 1.39), (.42, .26, 1.40), (.23, .37, 1.41)]
    verts = []
    for i, p in enumerate(points):
        t = (Vector(points[min(i+1, len(points)-1)])-Vector(points[max(0, i-1)])).normalized()
        s = Vector((-t.y, t.x, 0)).normalized()
        verts += [tuple(Vector(p)+s*a*.0225+Vector((0, 0, b*.05)))
                  for a, b in ((-1, -1), (1, -1), (1, 1), (-1, 1))]
    faces = [(3, 2, 1, 0), tuple(range(len(verts)-4, len(verts)))]
    faces += [(4*i+j, 4*i+(j+1)%4, 4*(i+1)+(j+1)%4, 4*(i+1)+j)
              for i in range(len(points)-1) for j in range(4)]
    mesh('Four separated blued steel clock-spring turns', verts, faces, 'steel')
    cyl('Winding key hub', (0, 0, 1.43), .10, .12, 'gold', n=12)
    for s in (-1, 1):
        ring('Winding key open bow', .14, .05, .065, 'brass', (s*.115, 0, 1.46),
             (math.pi/2, 0, 0), n=12)
    box('Ratchet pawl', (.30, .20, .41), (.22, .09, .08), 'steel', rot=(0, 0, -.5))
    for x in (-.35, .35):
        for y in (-.35, .35):
            cyl('Mounting bolt', (x, y, .29), .05, .06, 'gold', n=6)
    tube('Cracked stone base', [(-.43, .25, .215), (-.28, .17, .28),
         (-.23, .36, .28), (-.12, .42, .28)], .024, 'crack_gold')


def cover_shard_rock():
    # Staggered navy ashlar supports with two torn, pale cornice fragments.
    for x, y, z, w in ((-.47, -.10, .18, .78), (.35, -.12, .18, .85),
                        (-.56, -.07, .48, .59), (.12, -.04, .48, .71),
                        (.56, -.09, .48, .44), (-.20, -.15, .77, .71)):
        box('Broken navy observatory ashlar', (x, y, z), (w, .74, .27),
            'void_stone' if x < .3 else 'void_stone_dark', rot=(0, .025, -.035))
    outline = [(-.91, -.35), (.36, -.35), (.52, -.19), (.34, .04),
               (.55, .24), (.15, .38), (-.89, .33)]
    poly('Jagged pale cornice slab', outline, .22, 'marble', (0, .02, .87),
         rot=(.07, -.09, .02), bevel=True, side='marble_dark')
    box('Cornice projecting front molding', (-.25, .45, .91), (1.19, .20, .16),
        'marble', rot=(0, -.09, .02))
    box('Upper broken cornice lip', (-.30, .28, 1.06), (1.12, .27, .10),
        'marble', rot=(.06, -.09, .02))
    box('Meridian strip through low masonry', (.21, .39, .44), (.085, .05, .78), 'brass')
    box('Offset meridian on torn cornice', (.14, .16, 1.035), (.085, .70, .045),
        'brass', rot=(0, -.09, .02), bevel=False)
    tube('Cyan split across masonry face', [(-.53, .34, .74), (-.32, .38, .62),
         (-.40, .38, .39), (-.14, .38, .27), (-.20, .38, .07)], .028, 'crack_gold')
    tube('Cornice top fracture', [(-.58, -.20, 1.01), (-.33, .02, 1.04),
         (-.41, .20, 1.08), (-.20, .34, 1.12)], .025, 'crack_gold')
    box('Hovering pale cornice end', (.67, .14, 1.19), (.30, .39, .18),
        'marble', rot=(.12, -.17, -.18))
    box('Detached navy brick', (-.77, -.31, 1.10), (.22, .20, .18),
        'void_stone', rot=(.12, .22, .14))


def book(loc, size, role, rot):
    start = len(PARTS)
    w, h, d = size
    box('Thick atlas page block', (0, 0, 0), (w*.91, h*.93, d*.70), 'dial_face')
    for z in (-d*.43, d*.43):
        box('Bound atlas cover', (0, 0, z), (w, h, d*.16), role, bevel=False)
    box('Dark stained wooden book spine', (0, h*.46, 0), (w, h*.12, d), 'wood')
    for x in (-w*.30, w*.30):
        box('Brass book spine band', (x, h*.53, 0), (.055, .045, d*.86), 'brass', bevel=False)
    group(start, loc, rot)


def cover_memory_pile():
    book((-.32, .04, .18), (1.03, .78, .28), 'cloth', (0, 0, -.16))
    book((-.28, .04, .43), (.97, .74, .24), 'wood_dark', (.04, .04, .17))
    book((-.39, -.03, .67), (.83, .62, .22), 'cloth', (.08, -.12, -.23))
    book((.54, -.17, .43), (.60, .54, .23), 'wood', (0, .9, .2))
    telescope((-.03, -.21, 1.01), 1.10, .18, rot=(.10, -.21, -.30))
    dial((.38, .42, .66), .36, rot=(-.98, .12, -.16), cracked=True)
    # A rolled blue star chart with a short unfurled tail.
    start = len(PARTS)
    cyl('Rolled midnight star chart', (0, 0, 0), .125, .60, 'night_sky', n=16,
        rot=(0, math.pi/2, 0))
    for x in (-.30, .30):
        cyl('Visible parchment roll end', (x, 0, 0), .095, .045, 'dial_face', n=12,
            rot=(0, math.pi/2, 0))
        cyl('Hollow roll core', (x*1.08, 0, 0), .045, .045, 'wood_dark', n=10,
            rot=(0, math.pi/2, 0))
    chart((0, .17, -.055), (.54, .26))
    group(start, (-.39, .45, .86), (.15, .05, .13))
    box('Fallen telescope mounting shoe', (.50, -.27, .17), (.48, .42, .22), 'void_stone')
    tube('Fractured clock edge', [(.68, .52, .75), (.65, .60, .61), (.73, .57, .47)],
         .025, 'crack_gold')
    chart((-.40, -.05, 1.33), (.32, .25), rot=(.18, .14, .34), page=True)


def column_drum(loc, height, radius=.43, rot=(0, 0, 0)):
    start = len(PARTS)
    n = 64
    # Actual concave fluting profile rather than painted strips on a cylinder.
    outline = []
    for j in range(n):
        a = TAU*j/n
        rr = radius*(.91 if j%4 in (1, 2) else 1)
        outline.append((rr*math.cos(a), rr*math.sin(a)))
    ob = poly('Eight carved concave marble flutes', outline, height, 'marble', bevel=False)
    ob.data.materials.append(MATS['marble_dark'])
    for p in ob.data.polygons:
        if p.index >= 2 and (p.index-2)%4 == 1:
            p.material_index = 1
    group(start, loc, rot)


def cover_broken_column():
    box('Navy column plinth', (0, 0, .105), (1.30, 1.30, .21), 'void_stone')
    box('Pale column foot molding', (0, 0, .265), (1.14, 1.14, .13), 'marble')
    ring('Base brass molding', .52, .085, .10, 'brass', (0, 0, .38), n=32)
    column_drum((0, 0, .93), 1.03)
    ring('Brass drum cincture', .455, .045, .13, 'brass', (0, 0, .77), n=32)
    # A broken crown; separate floating drum has a conspicuous 0.25 m air gap.
    poly('Ragged column fracture', [(-.41, -.23), (-.31, -.37), (.22, -.40),
         (.43, -.11), (.34, .25), (.02, .44), (-.30, .31)], .15, 'marble_dark',
         (0, 0, 1.47), rot=(.11, -.13, 0))
    tube('Cold cyan split on front', [(.16, .39, 1.47), (.03, .44, 1.25),
         (.15, .43, 1.06), (.04, .43, .88), (.12, .43, .61)], .028, 'crack_gold')
    start = len(PARTS)
    column_drum((0, 0, .12), .24)
    box('Broken gold capital abacus', (-.10, 0, .36), (.94, .88, .15), 'gold')
    box('Capital navy recess', (-.10, 0, .45), (.73, .66, .06), 'void_stone')
    for y in (-.34, .34):
        box('Gold capital molding', (-.15, y, .51), (.78, .13, .10), 'gold')
    for x in (-.45, .25):
        box('Gold capital end molding', (x, 0, .51), (.12, .58, .10), 'brass')
    group(start, (-.10, -.03, 1.85), (.08, -.13, .08))
    box('Suspended gold capital chip', (.48, .18, 2.20), (.19, .23, .15),
        'gold', rot=(.20, .14, -.14))


def dome_patch(label, a0, a1, p0, p1, role, n=5, m=4):
    """Spherical double-walled plate; dome slit is a true missing sector."""
    verts = []
    for r in (1.56, 1.43):
        for k in range(m+1):
            p = p0+(p1-p0)*k/m
            for j in range(n+1):
                a = a0+(a1-a0)*j/n
                verts.append((r*math.sin(p)*math.cos(a),
                              r*math.sin(p)*math.sin(a), r*math.cos(p)))
    layer = (m+1)*(n+1)
    faces = []
    roles = []
    for k in range(m):
        for j in range(n):
            q = k*(n+1)+j
            faces += [(q, q+1, q+n+2, q+n+1),
                      (layer+q+n+1, layer+q+n+2, layer+q+1, layer+q)]
            roles += [role, 'void_stone_dark']
    for k in range(m):
        for j in (0, n):
            q = k*(n+1)+j
            faces.append((q, q+n+1, layer+q+n+1, layer+q))
            roles.append('brass')
    for j in range(n):
        for k in (0, m):
            q = k*(n+1)+j
            faces.append((q, layer+q, layer+q+1, q+1))
            roles.append('steel')
    return mesh(label, verts, faces, roles)


def fd_saloon():
    start = len(PARTS)
    for j in range(3):
        a = .18+j*.73
        dome_patch('Navy curved observatory dome plate', a, a+.69, .32, 1.47,
                   'void_stone' if j != 1 else 'indigo')
    for j in range(4):
        a = .165+j*.73
        pts = [(1.59*math.sin(p)*math.cos(a), 1.59*math.sin(p)*math.sin(a),
                1.59*math.cos(p)) for p in [.31+k*1.17/12 for k in range(13)]]
        tube('Curved dome brass structural rib', pts, .045, 'brass', n=4)
    # Broad machined slit edge and pale stone springing course.
    a = .13
    pts = [(1.63*math.sin(p)*math.cos(a), 1.63*math.sin(p)*math.sin(a),
            1.63*math.cos(p)) for p in [.29+k*1.22/12 for k in range(13)]]
    tube('Heavy open observation-slit jamb', pts, .075, 'gold', n=4)
    arc('Dome base pale springing course', 1.60, .18, .16, .12, 2.33,
        'marble', (0, 0, .10), n=20)
    tube('Time fracture in dome panel', [(.42, 1.12, 1.06), (.33, 1.27, .88),
         (.48, 1.32, .73), (.42, 1.44, .51)], .03, 'crack_gold')
    dome_patch('Detached upper dome tile', .87, 1.22, .04, .25, 'void_stone', n=3, m=2)
    group(start, (0, 0, 0), (.30, -.34, -.50))


def fd_rail():
    telescope((0, 0, 0), 2.65, .47, rot=(.10, -.28, .10), broken=True)
    box('Detached lens collar segment', (-.84, .43, .65), (.38, .19, .14),
        'brass', rot=(.20, .10, -.17))
    tube('Time fracture on telescope casing', [(-1.0, .37, .36), (-.74, .43, .32),
         (-.61, .44, .20), (-.32, .47, .16)], .029, 'crack_gold')
    # Small cream scale on the focusing carriage gives a fifth broad role.
    box('Cream focus scale', (-.30, .36, .46), (.60, .065, .11), 'dial_face',
        rot=(0, -.20, .1), bevel=False)
    for x in (-.5, -.34, -.18):
        box('Focus scale marks', (x, .403, .47), (.045, .04, .08), 'dial_mark', bevel=False)


def fd_station_clock():
    start = len(PARTS)
    # A torn 240-degree sector, not an intact station clock or clock on a post.
    angles = [math.radians(-49)+math.radians(239)*j/40 for j in range(41)]
    outline = [(-.16, -.23)]+[(1.26*math.cos(a), 1.26*math.sin(a)) for a in angles]+[(-.31, -.05)]
    poly('Broken 24-hour gate-clock dial wedge', outline, .17, 'dial_face',
         side='void_stone', bevel=True)
    arc('Gold gate-clock bezel arc', 1.34, .20, .16, angles[0], angles[-1], 'gold',
        (0, 0, .06), n=32)
    arc('Navy inset dial chapter ring', 1.10, .22, .045, angles[0], angles[-1], 'indigo',
        (0, 0, .11), n=26)
    ring('Inner steel dial engraving', .72, .055, .045, 'steel', (0, 0, .11), n=24)
    # Marks use 360/24 spacing: the absent 8 marks were lost with the missing wedge.
    for j in range(24):
        a = TAU*j/24
        aa = a if a <= math.pi*1.10 else a-TAU
        if angles[0]+.05 < aa < angles[-1]-.05:
            box('One of the 24 hour blocks', (.95*math.cos(a), .95*math.sin(a), .15),
                (.19 if j%6 == 0 else .12, .055, .06), 'dial_mark', rot=(0, 0, a), bevel=False)
    a, l = 1.05, .82
    box('Single brass Greenwich hour hand', (l*.5*math.cos(a), l*.5*math.sin(a), .225),
        (l, .075, .06), 'brass', rot=(0, 0, a))
    cyl('Clock hand boss', (0, 0, .245), .13, .12, 'gold', n=16)
    tube('Split cream dial', [(-.93, .74, .115), (-.64, .57, .115), (-.46, .27, .115),
         (-.18, .31, .115), (.10, .12, .115)], .027, 'crack_gold')
    poly('Hovering cream dial fragment', [(0, 0), (.32, -.12), (.42, .21), (.20, .30)],
         .16, 'dial_face', (-.75, -.35, .26), rot=(.1, .15, -.1), side='steel')
    # Tilt the face towards +Y and upwards so it reads in the top-down game camera.
    group(start, (0, 0, 0), (math.radians(-54), -.10, .12))


def fd_bell():
    # Three crossed, intentionally incomplete armillary great circles.
    arc('Equatorial brass armillary arc', 1.27, .13, .12, -.20, 4.9, 'brass',
        rot=(.25, .10, .08), n=30)
    arc('Polar brass armillary arc', 1.16, .12, .12, -.6, 4.6, 'brass',
        rot=(1.28, .15, .40), n=28)
    arc('Ecliptic golden armillary arc', 1.32, .10, .10, .40, 5.25, 'gold',
        rot=(.84, -.82, -.30), n=28)
    # Broad navy degree scale occupies a meaningful part of the equatorial arc.
    arc('Navy equatorial degree scale', 1.29, .065, .045, .2, 3.7, 'indigo',
        loc=(0, 0, .075), rot=(.25, .10, .08), n=18)
    for j in range(12):
        a = .3+j*.27
        start = len(PARTS)
        box('Armillary degree tick', (1.22*math.cos(a), 1.22*math.sin(a), .13),
            (.12, .045, .04), 'dial_face', rot=(0, 0, a), bevel=False)
        group(start, (0, 0, 0), (.25, .10, .08))
    beam('Steel polar spindle', (-.16, -.16, -.83), (.16, .16, .86), .075, .075, 'steel')
    bpy.ops.mesh.primitive_ico_sphere_add(subdivisions=2, radius=.32, location=(0, 0, 0))
    sphere = bpy.context.object
    sphere.name = 'Small faceted celestial crystal globe'
    sphere.data.materials.append(MATS['crystal'])
    PARTS.append(sphere)
    ring('Globe brass equator', .33, .045, .055, 'brass', n=20)
    cyl('North polar cap', (.15, .15, .83), .13, .14, 'gold', n=12)
    star((.06, -.02, .34), .10)
    box('Detached armillary index tab', (-1.04, .22, .92), (.25, .13, .17),
        'steel', rot=(.4, .2, -.2))
    tube('Fractured cold ring end', [(.96, -.62, .0), (1.09, -.53, .09),
         (1.04, -.41, .17)], .035, 'crack_gold')


def fd_desert_pillar():
    # Architectural pilaster: broad backplate, carved channels and brick return.
    box('Broken pilaster pale backplate', (0, 0, 0), (1.34, .33, 2.1), 'marble_dark')
    for x in (-.50, -.25, 0, .25, .50):
        box('Five pilaster carved raised flutes', (x, .24, -.05), (.16, .25, 1.68), 'marble')
    for z, w, d in ((-.98, 1.65, .56), (-.78, 1.47, .48), (.83, 1.5, .49)):
        box('Pilaster stepped stone molding', (-.06, .02, z), (w, d, .16), 'marble')
    for x, z in ((.74, -.59), (.81, -.16), (.73, .27), (.80, .70)):
        box('Navy brick corner return', (x, -.13, z), (.53, .68, .37), 'void_stone')
    box('Straight prime meridian brass inlay', (-.24, .39, -.29), (.095, .055, 1.28), 'brass')
    box('Displaced prime meridian above fracture', (-.34, .42, .59), (.095, .055, .36), 'brass')
    # Ragged capital and its hovering lip give a broken top silhouette.
    poly('Sheared pilaster capital fragment', [(-.76, -.22), (.35, -.22), (.59, .02),
         (.27, .30), (-.67, .31)], .21, 'marble', (-.04, 0, 1.14),
         rot=(.08, -.13, .03), bevel=True, side='gold')
    tube('Prime meridian time rupture', [(-.54, .38, .35), (-.32, .41, .21),
         (-.12, .40, .31), (.14, .41, .10), (.49, .40, .18)], .032, 'crack_gold')
    box('Hovering navy return brick', (1.03, -.21, 1.08), (.35, .39, .24),
        'void_stone', rot=(.15, .16, .08))
    box('Pilaster gold corner trim', (-.72, .27, -.89), (.12, .12, .38), 'gold')
    group(0, (0, 0, 0), (0, -.22, -.10))


def fd_mirror_frame():
    start = len(PARTS)
    # Two cartographic fields share a latitude seam, with no hidden colour cards.
    upper = [(-1.064, -.25), (.9413, -.25), (.87, .09), (1.06, .62),
             (.49, 1.05), (-.70, .97), (-1.02, .45)]
    lower = [(-1.08, -.91), (.67, -1.03), (1.00, -.53),
             (.9413, -.25), (-1.064, -.25)]
    poly('Midnight northern celestial chart field', upper, .18, 'night_sky',
         side='void_stone', bevel=True)
    poly('Indigo southern celestial chart field', lower, .18, 'indigo',
         side='void_stone', bevel=True)
    tube('Steel latitude division', [(-1.02, -.25, .13), (.90, -.25, .13)], .025, 'steel')
    # A pale engraved border keeps the chart from reading as a dark rectangle.
    for a, b in (((-.96, -.75, .13), (-.91, .70, .13)),
                 ((-.88, .81, .13), (.41, .89, .13)),
                 ((-.91, -.80, .13), (.51, -.89, .13))):
        tube('Steel chart neatline', [a, b], .026, 'steel')
    stars = [(-.65, .46, .15), (-.21, .71, .15), (.28, .53, .15),
             (.65, .18, .15), (.18, -.05, .15), (-.36, -.26, .15),
             (-.57, -.62, .15), (.20, -.65, .15), (.58, -.39, .15)]
    for indices in ((0, 1, 2, 3, 4, 5, 0), (5, 6, 7, 8, 4)):
        tube('Raised constellation stick lines', [stars[i] for i in indices], .025, 'steel')
    for i, p in enumerate(stars):
        star(p, .11 if i in (1, 4, 7) else .075)
    # A surviving L of a machined gold frame, not an intact mirror frame.
    box('Gold chart frame left stile', (-1.10, -.05, .10), (.15, 1.91, .18), 'gold')
    box('Gold chart frame lower corner', (-.50, -.98, .10), (1.35, .15, .18), 'gold')
    box('Frame corner navy inset', (-1.08, -.93, .22), (.23, .23, .06), 'indigo')
    star((-1.08, -.93, .28), .075, 'brass')
    # Broken latitude arc and a cyan rupture are separate, legible motifs.
    arc('Brass celestial coordinate arc', .73, .045, .045, -.5, 2.9, 'brass',
        (0, 0, .13), n=16)
    tube('Chart panel cyan fissure', [(.83, .66, .15), (.58, .46, .15),
         (.65, .29, .15), (.50, .10, .15)], .03, 'crack_gold')
    poly('Hovering star chart corner', [(0, 0), (.39, -.12), (.30, .27)], .16,
         'night_sky', (1.05, .88, .27), rot=(.2, -.1, .2), side='steel')
    star((1.20, .95, .39), .08)
    group(start, (0, 0, 0), (math.radians(-52), -.12, -.12))


BUILDERS = (warped_keg, warped_trunk, warped_spring, cover_shard_rock,
            cover_memory_pile, cover_broken_column, fd_saloon, fd_rail,
            fd_station_clock, fd_bell, fd_desert_pillar, fd_mirror_frame)
SUBJECTS = ('CHRONOMETER CANISTER', 'ASTRONOMER INSTRUMENT CASE', 'OBSERVATORY CLOCK SPRING',
            'FALLEN MERIDIAN MASONRY', 'INSTRUMENTS & STAR ATLASES', 'BROKEN FLUTED COLUMN',
            'OBSERVATORY DOME / SLIT', 'GREAT REFRACTOR SECTION', '24-HOUR GATE CLOCK',
            'SHATTERED ARMILLARY', 'PRIME MERIDIAN PILASTER', 'CELESTIAL CHART PANEL')


def bounds(ob):
    return [Vector(tuple(f(v.co[j] for v in ob.data.vertices) for j in range(3)))
            for f in (min, max)]


def centroid(ob):
    ob.data.calc_loop_triangles()
    total, centre = 0., Vector((0, 0, 0))
    for tr in ob.data.loop_triangles:
        a, b, c = (ob.data.vertices[i].co for i in tr.vertices)
        vol = a.dot(b.cross(c))/6
        total += vol
        centre += (a+b+c)*(vol/4)
    assert total > 1e-7, (ob.name, total)
    return centre/total


def finish(name, measured):
    bpy.ops.object.select_all(action='DESELECT')
    for ob in PARTS:
        bpy.context.view_layer.objects.active = ob
        ob.select_set(True)
        for mod in list(ob.modifiers):
            bpy.ops.object.modifier_apply(modifier=mod.name)
        ob.select_set(False)
    for ob in PARTS:
        ob.select_set(True)
    bpy.context.view_layer.objects.active = PARTS[0]
    bpy.ops.object.join()
    ob = bpy.context.object
    # Some operators create a real location; join bakes it into the active local space.
    bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
    ob.name, ob.data.name = name, name+'_Mesh'
    target = measured[name][0]
    lo, hi = bounds(ob)
    span, mid = hi-lo, (hi+lo)/2
    for v in ob.data.vertices:
        for j in range(3):
            v.co[j] = (v.co[j]-mid[j])*target[j]/span[j]
    bm = bmesh.new()
    bm.from_mesh(ob.data)
    bmesh.ops.recalc_face_normals(bm, faces=list(bm.faces))
    bmesh.ops.triangulate(bm, faces=list(bm.faces))
    bmesh.ops.recalc_face_normals(bm, faces=list(bm.faces))
    assert all(e.is_manifold for e in bm.edges), (name, 'open/nonmanifold surface')
    bm.to_mesh(ob.data)
    bm.free()
    ob.data.update()
    if name.startswith('FD_'):
        ob.data.transform(Matrix.Translation(-centroid(ob)))
        ob['origin_rule'] = 'volume-weighted centre, same as original debris'
        assert centroid(ob).length < 1e-5
    else:
        lo, hi = bounds(ob)
        ob.data.transform(Matrix.Translation(Vector((0, 0, -lo.z))))
        ob['origin_rule'] = 'bottom centre, +Y front'
    ob.data.update()
    bpy.context.view_layer.update()
    ob.data.calc_loop_triangles()
    count = len(ob.data.loop_triangles)
    budget = 3000 if name.startswith('FD_') else 5000
    assert count <= budget, (name, count, budget)
    assert all(abs(ob.dimensions[j]-target[j]) < 1e-4 for j in range(3)), (name, ob.dimensions, target)
    assert ob.location.length < 1e-6 and tuple(ob.scale) == (1, 1, 1)
    area = {}
    for p in ob.data.polygons:
        p.use_smooth = False
        role = ob.data.materials[p.material_index].name
        weight = max(0, p.normal.dot(Vector((.22, .53, .819))))
        area[role] = area.get(role, 0)+p.area*weight
    assert len([a for a in area.values() if a > 1e-5]) >= 5, (name, area)
    fraction = max(area.values())/sum(area.values())
    assert fraction <= .55, (name, 'dominant visible material', fraction, area)
    for mat in ob.data.materials:
        assert mat.name in PALETTE
        assert tuple(mat.diffuse_color) == (.5, .5, .5, 1)
        assert tuple(mat.node_tree.nodes['Principled BSDF'].inputs['Base Color'].default_value) == (.5, .5, .5, 1)
    ob['original_dimensions_m'] = list(target)
    ob['front'] = '+Y'
    path = OUT / ('Kit_'+name+'.fbx')
    bpy.ops.export_scene.fbx(filepath=str(path), use_selection=True,
        axis_forward='-Z', axis_up='Y', apply_scale_options='FBX_SCALE_UNITS',
        bake_space_transform=True, object_types={'MESH'}, use_mesh_modifiers=True,
        mesh_smooth_type='FACE', add_leaf_bones=False, bake_anim=False, use_custom_props=True)
    assert path.is_file() and path.stat().st_size > 1024
    print('EXPORTED', name, count, tuple(round(d, 6) for d in ob.dimensions), flush=True)
    print('AUDIT', name, 'roles', sorted(area), 'max_visible_role', round(fraction, 3),
          'origin', ob['origin_rule'], 'closed=True', flush=True)
    return ob, count


def verify_exports(objects, measured):
    """Round-trip every delivered FBX: dimensions, pivot, shader names and topology."""
    for original, count in objects:
        name = original.name
        before = set(bpy.data.objects)
        bpy.ops.import_scene.fbx(filepath=str(OUT / ('Kit_'+name+'.fbx')), use_anim=False)
        imported = [o for o in bpy.data.objects if o not in before]
        meshes = [o for o in imported if o.type == 'MESH']
        assert len(meshes) == 1, (name, 'round-trip object count')
        ob = meshes[0]
        ob.data.calc_loop_triangles()
        assert len(ob.data.loop_triangles) == count, (name, 'round-trip triangle count')
        verts = [ob.matrix_world @ v.co for v in ob.data.vertices]
        lo, hi = [Vector(tuple(f(v[j] for v in verts) for j in range(3))) for f in (min, max)]
        assert ob.matrix_world.translation.length < 1e-5
        assert all(abs((hi-lo)[j]-measured[name][0][j]) < 1e-4 for j in range(3))
        # Imported materials receive .001 suffixes while the originals are alive.
        assert all(m.name.split('.')[0] in PALETTE for m in ob.data.materials)
        if name.startswith('FD_'):
            ob.data.transform(ob.matrix_world)
            assert centroid(ob).length < 2e-5, (name, 'round-trip float pivot')
        else:
            assert abs(lo.z) < 1e-5
        for other in imported:
            bpy.data.objects.remove(other, do_unlink=True)
        print('VERIFIED', name, 'FBX round-trip', flush=True)


def preview(objects, measured):
    scene = bpy.context.scene
    scene.render.engine = 'BLENDER_WORKBENCH'
    scene.render.resolution_x, scene.render.resolution_y = 1800, 1000
    scene.render.resolution_percentage = 100
    scene.render.image_settings.file_format = 'PNG'
    scene.render.filepath = str(PREVIEW)
    scene.view_settings.view_transform = 'Standard'
    shade = scene.display.shading
    shade.light, shade.color_type = 'FLAT', 'MATERIAL'
    shade.show_shadows = False
    shade.show_cavity, shade.cavity_type = True, 'BOTH'
    shade.curvature_ridge_factor, shade.curvature_valley_factor = 1.35, 1.1
    shade.show_specular_highlight = False
    shade.show_object_outline = True
    shade.background_type = 'WORLD'
    if scene.world is None:
        scene.world = bpy.data.worlds.new('Observatory review backdrop')
    scene.world.color = (.014, .023, .041)
    for role, mat in MATS.items():
        mat.diffuse_color = (*PALETTE[role], 1)
    camera = bpy.data.objects.new('Preview only camera', bpy.data.cameras.new('Review camera'))
    scene.collection.objects.link(camera)
    scene.camera = camera
    direction = Vector((.21, .534, .819)).normalized()
    camera.location = direction*35
    camera.rotation_euler = (-camera.location).to_track_quat('-Z', 'Y').to_euler()
    camera.data.type, camera.data.ortho_scale = 'ORTHO', 19.8
    rotation = camera.rotation_euler.to_matrix()
    inv = rotation.transposed()
    helpers = [camera]
    type_mat = bpy.data.materials.new('Review typography only')
    type_mat.diffuse_color = (.75, .84, .90, 1)
    cyan = bpy.data.materials.new('Review cyan only')
    cyan.diffuse_color = (.10, .69, .81, 1)

    def caption(body, x, y, size=.17, mat=type_mat):
        data = bpy.data.curves.new('Review label', 'FONT')
        data.body, data.size = body, size
        data.align_x, data.align_y = 'CENTER', 'TOP'
        data.materials.append(mat)
        label = bpy.data.objects.new('Review label', data)
        scene.collection.objects.link(label)
        label.location = rotation @ Vector((x, y, 3))
        label.rotation_euler = camera.rotation_euler
        helpers.append(label)

    caption('GREENWICH  /  OBSERVATORY OF TIME', 0, 5.22, .32)
    caption('CHAPTER 05    /    INSTRUMENTS, FALLEN STONE & DRIFTING FRAGMENTS', 0, 4.78, .15, cyan)
    for i, (ob, tris) in enumerate(objects):
        cx, cy = -7.15+(i%4)*4.77, 3.20-(i//4)*3.18
        projected = [inv @ v.co for v in ob.data.vertices]
        lo, hi = [Vector(tuple(f(v[j] for v in projected) for j in range(3))) for f in (min, max)]
        scale = min(3.42/(hi.x-lo.x), 2.17/(hi.y-lo.y))
        ob.scale = (scale,)*3
        ob.location = rotation @ Vector((cx-scale*(hi.x+lo.x)/2, cy-scale*(hi.y+lo.y)/2, 0))
        caption(ob.name, cx, cy-1.20, .195)
        caption(SUBJECTS[i], cx, cy-1.46, .125, cyan)
        dims = measured[ob.name][0]
        caption('%.2f x %.2f x %.2f m   /   %d tris' % (*dims, tris), cx, cy-1.66, .12)
    caption('BRASS  /  PORTLAND STONE  /  NAVY  /  CYAN TIME LIGHT', 0, -5.16, .13, cyan)
    # Review transforms and viewport colours happen strictly after delivery exports.
    hashes = {name: hashlib.sha256((OUT/('Kit_'+name+'.fbx')).read_bytes()).digest() for name in NAMES}
    try:
        bpy.ops.render.render(write_still=True)
        assert PREVIEW.is_file() and PREVIEW.stat().st_size > 1024
    finally:
        scene.camera = None
        for helper in helpers:
            bpy.data.objects.remove(helper, do_unlink=True)
        for ob, _ in objects:
            ob.location = (0, 0, 0)
            ob.scale = (1, 1, 1)
        for mat in MATS.values():
            mat.diffuse_color = (.5, .5, .5, 1)
    assert all(hashlib.sha256((OUT/('Kit_'+name+'.fbx')).read_bytes()).digest() == h
               for name, h in hashes.items()), 'Preview changed an FBX'
    print('PREVIEW', str(PREVIEW), flush=True)


def main():
    global PARTS
    measured = measure_originals()
    for mat in list(bpy.data.materials):
        bpy.data.materials.remove(mat)
    scene = bpy.context.scene
    scene.unit_settings.system, scene.unit_settings.scale_length = 'METRIC', 1
    materials()
    OUT.mkdir(parents=True, exist_ok=True)
    # Guard explicitly excluded floating pieces against accidental rewrites.
    protected = {n: hashlib.sha256((OUT/('Kit_'+n+'.fbx')).read_bytes()).digest()
                 for n in ('FD_Gears', 'FD_Island') if (OUT/('Kit_'+n+'.fbx')).exists()}
    objects = []
    for i, (name, build) in enumerate(zip(NAMES, BUILDERS)):
        PARTS = []
        random.seed(55120+i*991)
        build()
        objects.append(finish(name, measured))
    verify_exports(objects, measured)
    assert len(scene.objects) == 12 and all(o.type == 'MESH' for o in scene.objects)
    preview(objects, measured)
    assert all(hashlib.sha256((OUT/('Kit_'+n+'.fbx')).read_bytes()).digest() == h
               for n, h in protected.items())
    print('DONE', flush=True)


if __name__ == '__main__':
    main()
