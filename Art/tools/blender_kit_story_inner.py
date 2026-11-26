# 내면 세계 맵 '멈춘 1초' 키트 — 아무것도 없는 하얀 방, 2026-11-26
# 실행: "C:/Program Files/Blender Foundation/Blender 5.3/blender.exe" -b --factory-startup -P Art/tools/blender_kit_story_inner.py
# Blender (X,Y,Z) = Unity (X,Z,Y); +Y 북쪽, 정면 -Y, 남쪽 위 57도 카메라.
# 회색 Principled 역할 재질 FBX(Kit_I_*.fbx): Unity는 재질 NAME으로 재채색. PNG에만 PALETTE 적용.
# KEEP = Ground만 맵 원점. 나머지는 가로 가운데 / 바닥 Z=0. 단위 m.
# 거울 면: 가운데 높이 Z=1.67m, 최대 폭=1.40m, 하단=.49 / 상단=2.85m.
# 거울 면 앞면 Y=-.235m: mirror_edge 앞면(-.205)보다 남쪽으로 정확히 .03m.
# 기둥 지름 .90m는 몸통 기준, 2단 받침 최대 1.22m; 총 높이 7.50m.
# 일반 3~6재질보다 항목별 명시 역할을 우선: Mirror 9, MemWatch 8, MemShawl 2, MemRing 1.
# 바닥 밑면 없음. 빛 고리 .02m와 얕은 바닥 새김은 명세의 두께 예외.
# 5장 병실의 Part/geometry/halfplane/chunk/mesh/budget/recenter/FBX 프레임워크 계승.
import math
import random
from pathlib import Path

import bmesh
import bpy
from mathutils import Euler, Matrix, Vector

ROOT = Path(__file__).resolve().parents[2]
OUTPUT = ROOT / 'Game/Assets/Art/Characters/StoryInner'
PREVIEW = ROOT / 'Art/tools/specs/blender_kit_story_inner_preview.png'
TAU = math.tau
PALETTE = {
    'floor': (.97,.97,.96), 'floor_ivory': (.96,.94,.9),
    'floor_pale': (.9,.91,.92), 'floor_seam': (.82,.83,.86),
    'marble': (.98,.98,.97), 'marble_vein': (.84,.85,.88), 'white_shade': (.78,.8,.86),
    'gold': (1,.82,.38), 'brass': (.9,.7,.36), 'gold_dark': (.62,.42,.16),
    'mirror': (.82,.88,.95), 'mirror_edge': (.95,.97,1),
    'iron': (.3,.3,.34), 'glass': (.85,.92,.98), 'lamp_glow': (1,.85,.5),
    'watch_face': (1,.96,.86), 'dial_mark': (.2,.12,.06),
    'cloth': (.9,.86,.78), 'cloth_shade': (.72,.68,.62), 'steel': (.75,.78,.82),
    'shawl_red': (.8,.08,.1), 'shawl_red_dark': (.45,.04,.06), 'glow': (1,.95,.8),
}
ROLES = tuple(PALETTE)
HARD = {'marble','marble_vein','gold','brass','gold_dark','iron','steel'}
FONT_FILES = ('C:/Windows/Fonts/BOOKOSB.TTF', 'C:/Windows/Fonts/timesbd.ttf', 'C:/Windows/Fonts/georgiab.ttf')
ROMAN = ('XII','I','II','III','IIII','V','VI','VII','VIII','IX','X','XI')
MATS, PARTS, FONT = {}, [], None
OUTLINE = [(-22,-42),(22,-42),(22,42),(-22,42)]
CORE = [(-20,-40),(20,-40),(20,40),(-20,40)]
BATTLE_C, BATTLE_R = (0,3), 7.0
PANE_CENTRE, PANE_WIDTH = (0,-.235,1.67), 1.40
ANCHORS = {'Mirror': {'pane_centre': PANE_CENTRE, 'pane_width': PANE_WIDTH}}

def materials():
    for role in ROLES:
        mat = bpy.data.materials.new(role)
        mat.use_nodes = True
        mat.diffuse_color = (.5, .5, .5, 1)
        nodes = mat.node_tree.nodes
        nodes.clear()
        shader = nodes.new('ShaderNodeBsdfPrincipled')
        shader.inputs['Base Color'].default_value = (.5, .5, .5, 1)
        shader.inputs['Metallic'].default_value = 0
        shader.inputs['Roughness'].default_value = .8
        output = nodes.new('ShaderNodeOutputMaterial')
        mat.node_tree.links.new(shader.outputs['BSDF'], output.inputs['Surface'])
        MATS[role] = mat


class Part:
    """Batch solid components before the shared 0.02m/two-segment bevel."""

    def __init__(self, label, verts, faces, role, bevel=True):
        assert role in ROLES, role
        self.label, self.role = label, role
        self.verts = [Vector(v) for v in verts]
        self.faces = faces
        self.bevel = bevel and role in HARD
        PARTS.append(self)

    def transform(self, loc=(0, 0, 0), rot=(0, 0, 0), scale=(1, 1, 1)):
        matrix = (Matrix.Translation(Vector(loc)) @
                  Euler(rot).to_matrix().to_4x4() @
                  Matrix.Diagonal(Vector((*scale, 1))))
        self.verts = [matrix @ v for v in self.verts]
        return self


def move_group(start, loc=(0, 0, 0), rot=(0, 0, 0), scale=(1, 1, 1)):
    for part in PARTS[start:]:
        part.transform(loc, rot, scale)


def box(label, loc, size, role, rot=(0, 0, 0), bevel=True):
    x, y, z = (s/2 for s in size)
    return Part(label, [(-x,-y,-z), (x,-y,-z), (x,y,-z), (-x,y,-z),
                        (-x,-y,z), (x,-y,z), (x,y,z), (-x,y,z)],
                [(0,3,2,1), (4,5,6,7), (0,1,5,4), (1,2,6,5),
                 (2,3,7,6), (3,0,4,7)], role, bevel).transform(loc, rot)


def extrude(label, polygon, depth, role, y=0, bevel=True):
    """Solid polygon in XZ; extruded along Y around y."""
    n = len(polygon)
    verts = [(x, y+d, z) for d in (-depth/2, depth/2) for x, z in polygon]
    faces = [tuple(reversed(range(n))), tuple(range(n, 2*n))]
    faces += [(i, (i+1) % n, (i+1) % n+n, i+n) for i in range(n)]
    return Part(label, verts, faces, role, bevel)


def lathe(label, rings, role, n=12, bevel=True):
    verts = [(r*math.cos(j*TAU/n), r*math.sin(j*TAU/n), z)
             for z, r in rings for j in range(n)]
    faces = [tuple(reversed(range(n)))]
    faces += [(i*n+j, i*n+(j+1) % n, (i+1)*n+(j+1) % n, (i+1)*n+j)
              for i in range(len(rings)-1) for j in range(n)]
    faces.append(tuple(range((len(rings)-1)*n, len(rings)*n)))
    return Part(label, verts, faces, role, bevel)


def cylinder(label, loc, radius, depth, role, n=12, rot=(0,0,0), top=None, bevel=True):
    return lathe(label, [(-depth/2, radius),
                        (depth/2, radius if top is None else top)], role, n, bevel).transform(loc, rot)


def ycyl(label, loc, radius, depth, role, n=16, bevel=False):
    """Disc whose axis is Y (faces the camera side -Y)."""
    return cylinder(label, loc, radius, depth, role, n, rot=(math.pi/2,0,0), bevel=bevel)


def tube(label, points, radius, role, n=5, bevel=False):
    verts = []
    for i, point in enumerate(points):
        tangent = (Vector(points[min(i+1, len(points)-1)]) -
                   Vector(points[max(0, i-1)])).normalized()
        ref = Vector((0,1,0)) if abs(tangent.y) < .9 else Vector((1,0,0))
        u = ref.cross(tangent).normalized()
        v = tangent.cross(u)
        verts += [tuple(Vector(point) + radius*(u*math.cos(j*TAU/n) +
                                                v*math.sin(j*TAU/n))) for j in range(n)]
    faces = [tuple(reversed(range(n)))]
    faces += [(i*n+j, i*n+(j+1) % n, (i+1)*n+(j+1) % n, (i+1)*n+j)
              for i in range(len(points)-1) for j in range(n)]
    faces.append(tuple(range((len(points)-1)*n, len(points)*n)))
    return Part(label, verts, faces, role, bevel)


def rod(label, a, b, radius, role, n=6, bevel=False):
    return tube(label, [a,b], radius, role, n, bevel)


def beam(label, a, b, width, depth, role, bevel=True):
    a, b = Vector(a), Vector(b)
    part = box(label, (0,0,0), (width,depth,(b-a).length), role, bevel=bevel)
    matrix = Matrix.Translation((a+b)/2) @ (b-a).to_track_quat('Z','Y').to_matrix().to_4x4()
    part.verts = [matrix @ v for v in part.verts]
    return part


def hoop(label, loc, radius, band, depth, role, n=24, rot=(0,0,0), bevel=False):
    """Open ring in XZ, axis Y."""
    verts = [(r*math.cos(j*TAU/n), y, r*math.sin(j*TAU/n))
             for r,y in ((radius-band,-depth/2), (radius,-depth/2),
                         (radius,depth/2), (radius-band,depth/2)) for j in range(n)]
    faces = [(k*n+j, k*n+(j+1) % n, ((k+1) % 4)*n+(j+1) % n,
              ((k+1) % 4)*n+j) for k in range(4) for j in range(n)]
    return Part(label, verts, faces, role, bevel).transform(loc, rot)


def flat_hoop(label, loc, radius, band, thick, role, n=32):
    """Horizontal ring lying on the floor (axis Z)."""
    return hoop(label, loc, radius, band, thick, role, n, rot=(math.pi/2,0,0))


def sector(label, inner, outer, start, end, depth, role, y=0, steps=4, bevel=True):
    angles = [start+(end-start)*j/steps for j in range(steps+1)]
    polygon = [(outer*math.cos(a), outer*math.sin(a)) for a in angles]
    polygon += [(inner*math.cos(a), inner*math.sin(a)) for a in reversed(angles)]
    return extrude(label, polygon, depth, role, y, bevel)


def lump(label, loc, size, role, detail=1):
    bm = bmesh.new()
    bmesh.ops.create_icosphere(bm, subdivisions=detail, radius=1)
    for v in bm.verts:
        v.co *= random.uniform(.93,1.07)
        v.co = Vector(loc)+Vector(tuple(v.co[i]*size[i]/2 for i in range(3)))
    bm.verts.ensure_lookup_table()
    result = Part(label, [tuple(v.co) for v in bm.verts],
                  [tuple(v.index for v in f.verts) for f in bm.faces], role, False)
    bm.free()
    return result


def ball(label, loc, r, role, detail=2):
    bm = bmesh.new()
    bmesh.ops.create_icosphere(bm, subdivisions=detail, radius=r)
    verts = [tuple(Vector(loc)+v.co) for v in bm.verts]
    faces = [tuple(v.index for v in f.verts) for f in bm.faces]
    bm.free()
    return Part(label, verts, faces, role, False)


def scroll(label, loc, radius=.3, flip=1, role='gold', y=0):
    x,_,z = loc
    points = []
    for j in range(11):
        t = j/10
        a, r = -.8+t*math.pi*1.9, radius*(1-.73*t)
        points.append((x+flip*r*math.cos(a),loc[1],z+r*math.sin(a)))
    return tube(label, points, max(.022, radius*.12), role, n=5)


def numeral(text, loc, height=.28, role='gold', angle=0, depth=.05):
    """Bold mesh bars, no font dependency. I, V and X silhouettes in XZ."""
    start = len(PARTS)
    pitch = height*.45
    for i, char in enumerate(text):
        x = (i-(len(text)-1)/2)*pitch
        w = max(.04,height*.15)
        if char == 'I':
            box('Numeral I', (x,0,0), (w,depth,height), role, bevel=False)
        elif char == 'X':
            for s in (-1,1):
                beam('Numeral X', (x-s*height*.18,0,-height/2),
                     (x+s*height*.18,0,height/2), w,depth,role,bevel=False)
        elif char == 'V':
            for s in (-1,1):
                beam('Numeral V', (x,0,-height/2),
                     (x+s*height*.21,0,height/2), w,depth,role,bevel=False)
    move_group(start, loc, (0,angle,0))


def slab(label, poly, top, thick, role):
    if len(poly) < 3:
        return None
    area = abs(sum(poly[i][0]*poly[(i+1) % len(poly)][1] -
                   poly[(i+1) % len(poly)][0]*poly[i][1] for i in range(len(poly))))
    if area < 1e-6:
        return None
    n = len(poly)
    verts = [(x,y,z) for z in (top-thick,top) for x,y in poly]
    faces = [tuple(reversed(range(n))),tuple(range(n,2*n))]
    faces += [(i,(i+1) % n,(i+1) % n+n,i+n) for i in range(n)]
    return Part(label,verts,faces,role,False)


def circle_poly(cx, cy, r, n=32, ccw=True):
    s = 1 if ccw else -1
    return [(cx+r*math.cos(s*j*TAU/n), cy+r*math.sin(s*j*TAU/n)) for j in range(n)]


def disc(label, xy, r, top, thick, role, n=32):
    return slab(label, circle_poly(xy[0], xy[1], r, n), top, thick, role)


def annulus(label, xy, r0, r1, top, thick, role, n=64):
    for j in range(n):
        a, b = j*TAU/n, (j+1)*TAU/n
        slab(label, [(xy[0]+r0*math.cos(a), xy[1]+r0*math.sin(a)), (xy[0]+r1*math.cos(a), xy[1]+r1*math.sin(a)),
                     (xy[0]+r1*math.cos(b), xy[1]+r1*math.sin(b)), (xy[0]+r0*math.cos(b), xy[1]+r0*math.sin(b))],
             top, thick, role)


def lozenge(label, xy, length, width, angle, top, thick, role):
    c, s = math.cos(angle), math.sin(angle)
    pts = [(length/2,0),(0,width/2),(-length/2,0),(0,-width/2)]
    return slab(label, [(xy[0]+x*c-y*s, xy[1]+x*s+y*c) for x,y in pts], top, thick, role)


def halfplane(poly, a, b, right=True):
    if not poly:
        return []
    def distance(p):
        return (b[0]-a[0])*(p[1]-a[1])-(b[1]-a[1])*(p[0]-a[0])
    out, p = [], poly[-1]
    dp = distance(p)
    for q in poly:
        dq = distance(q)
        ip = dp <= 1e-9 if right else dp >= -1e-9
        iq = dq <= 1e-9 if right else dq >= -1e-9
        if ip != iq:
            t = dp/(dp-dq)
            out.append((p[0]+t*(q[0]-p[0]), p[1]+t*(q[1]-p[1])))
        if iq:
            out.append(q)
        p, dp = q, dq
    return out


def clip_convex(poly, clipper):
    """Clip poly by a convex CCW clipper polygon."""
    for i, a in enumerate(clipper):
        poly = halfplane(poly, a, clipper[(i+1) % len(clipper)], right=False)
        if len(poly) < 3:
            return []
    return poly


def outside_circle(poly, c, r, n=64):
    """Pieces of convex poly outside the circle (clockwise circle edges, like the lake clearing)."""
    circle = [(c[0]+r*math.cos(-j*TAU/n), c[1]+r*math.sin(-j*TAU/n)) for j in range(n)]
    pieces, remain = [], poly
    for k,a in enumerate(circle):
        b = circle[(k+1) % n]
        out = halfplane(remain,a,b,False)
        if len(out) >= 3:
            pieces.append(out)
        remain = halfplane(remain,a,b)
        if len(remain) < 3:
            break
    return pieces


def ring_skin(label, rings, role, cap_top=True, cap_bottom=True):
    """Closed skin through rings of equal point counts (bottom to top)."""
    n = len(rings[0])
    verts = [p for ring in rings for p in ring]
    faces = []
    if cap_bottom:
        faces.append(tuple(reversed(range(n))))
    faces += [(i*n+j, i*n+(j+1) % n, (i+1)*n+(j+1) % n, (i+1)*n+j)
              for i in range(len(rings)-1) for j in range(n)]
    if cap_top:
        faces.append(tuple(range((len(rings)-1)*n, len(rings)*n)))
    return Part(label, verts, faces, role, False)


def frame(label, cx, cz, w, h, y, bar, depth, role, bevel=True):
    """Rectangular molding frame in XZ (centre cx,cz)."""
    box(label, (cx, y, cz+h/2-bar/2), (w, depth, bar), role, bevel=bevel)
    box(label, (cx, y, cz-h/2+bar/2), (w, depth, bar), role, bevel=bevel)
    box(label, (cx-w/2+bar/2, y, cz), (bar, depth, h-2*bar), role, bevel=bevel)
    box(label, (cx+w/2-bar/2, y, cz), (bar, depth, h-2*bar), role, bevel=bevel)


# ---------------------------------------------------------------- text numerals
def load_font():
    global FONT
    for path in FONT_FILES:
        if Path(path).is_file():
            try:
                FONT = bpy.data.fonts.load(path)
                print('FONT ' + path, flush=True)
                return
            except Exception as exc:
                print('font failed', path, exc)
    FONT = None


def text_mesh(text, offset=0.0, extrude=.5):
    cu = bpy.data.curves.new('num_text', 'FONT')
    cu.body = text
    if FONT:
        cu.font = FONT
    cu.align_x, cu.align_y = 'CENTER', 'CENTER'
    cu.size, cu.extrude, cu.offset = 1.0, extrude, offset
    cu.resolution_u = 3
    ob = bpy.data.objects.new('num_text', cu)
    bpy.context.collection.objects.link(ob)
    dg = bpy.context.evaluated_depsgraph_get()
    ev = ob.evaluated_get(dg)
    me = ev.to_mesh()
    verts = [v.co.copy() for v in me.vertices]
    faces = [tuple(p.vertices) for p in me.polygons]
    ev.to_mesh_clear()
    bpy.data.objects.remove(ob)
    bpy.data.curves.remove(cu)
    return verts, faces


def text_numeral(text, height, top_role, rim_role='gold_dark'):
    """Flat Arabic numeral lying on the floor, readable from the south. Origin centre, bottom z 0."""
    base_v, base_f = text_mesh(text, 0.0)
    xs = [v.x for v in base_v]; ys = [v.y for v in base_v]
    s = height/(max(ys)-min(ys))
    cx, cy = (max(xs)+min(xs))/2, (max(ys)+min(ys))/2
    rim_v, rim_f = text_mesh(text, .035)
    def place(vs, z0, thick):
        return [((v.x-cx)*s, (v.y-cy)*s, z0+(v.z+.5)*thick) for v in vs]
    Part('Gold numeral outline', place(rim_v, 0, .03), rim_f, rim_role, False)
    Part('Raised numeral', place(base_v, .02, .045), base_f, top_role, False)


def ribbon_solid(label, points, width, role, thick=.04):
    n = len(points)
    verts = [(x+dx,y,z+dz) for dz in (-thick/2,thick/2)
             for x,y,z in points for dx in (-width/2,width/2)]
    faces = []
    for i in range(n-1):
        a, b, k = i*2, (i+1)*2, n*2
        faces += [(a,b,b+1,a+1),(a+k+1,b+k+1,b+k,a+k),
                  (a,a+k,b+k,b),(a+1,b+1,b+k+1,a+k+1)]
    faces += [(0,1,2*n+1,2*n),(2*n-2,4*n-2,4*n-1,2*n-1)]
    return Part(label,verts,faces,role,False)



# ---------------------------------------------------------------- chunk floor / inner world
def surface(label, poly, role, z=0):
    """Single upward surface; Ground deliberately has no underside or side skirt."""
    if len(poly) >= 3:
        area = sum(p[0]*poly[(i+1)%len(poly)][1]-poly[(i+1)%len(poly)][0]*p[1]
                   for i,p in enumerate(poly))
        if abs(area) > 1e-8:
            Part(label, [(x,y,z) for x,y in poly],
                 [tuple(range(len(poly))) if area > 0 else tuple(reversed(range(len(poly))))],
                 role, False)


def chunk_cells():
    """The ward's half-plane Voronoi cells, enlarged to approximately five metres."""
    seeds = [(-22+i*5+random.uniform(-.7,.7), -42+j*5+random.uniform(-.7,.7))
             for i in range(10) for j in range(18)]
    for i,s in enumerate(seeds):
        cell = list(OUTLINE)
        for t in seeds:
            if t is s:
                continue
            m = ((s[0]+t[0])/2,(s[1]+t[1])/2)
            d = (-(t[1]-s[1]),t[0]-s[0])
            cell = halfplane(cell,m,(m[0]+d[0],m[1]+d[1]),False)
            if len(cell) < 3:
                break
        if len(cell) >= 3:
            yield i,cell


def ground():
    # A uniform two-metre border dissolves into white fog in Unity.
    for poly in ([(-22,-42),(-20,-42),(-20,42),(-22,42)],
                 [(20,-42),(22,-42),(22,42),(20,42)],
                 [(-20,-42),(20,-42),(20,-40),(-20,-40)],
                 [(-20,40),(20,40),(20,42),(-20,42)]):
        surface('Quiet fog border',poly,'floor')
    for i,cell in chunk_cells():
        cell = clip_convex(cell,CORE)
        inset = list(cell)
        # Partition each cell into .025m edge strips and a plain centre.
        # Adjacent strips form .05m seams, without any coplanar underlay.
        for k,p in enumerate(cell):
            q = cell[(k+1)%len(cell)]
            dx,dy = q[0]-p[0],q[1]-p[1]
            length = math.hypot(dx,dy)
            if length < 1e-6:
                continue
            a = (p[0]-dy/length*.025,p[1]+dx/length*.025)
            b = (a[0]+dx,a[1]+dy)
            surface('Fine quiet seam',halfplane(inset,a,b,True),'floor_seam')
            inset = halfplane(inset,a,b,False)
        role = random.choices(('floor','floor_ivory','floor_pale'),weights=(8,1,1))[0]
        surface('Unpatterned five metre chunk',inset,role)
    for radius in (7.0,6.78):
        for j in range(128):
            a,b = j*TAU/128,(j+1)*TAU/128
            surface('Almost erased clock circle',
                    [(r*math.cos(t),3+r*math.sin(t))
                     for r,t in ((radius-.018,a),(radius+.018,a),
                                 (radius+.018,b),(radius-.018,b))], 'floor_seam',.002)
    for j in range(12):
        a = j*TAU/12
        length = .52 if j == 0 else .26
        r = 6.48
        c,s = math.cos(a),math.sin(a)
        poly = [(x*c+y*s,3-x*s+y*c) for x,y in
                ((-.018,r-length/2),(.018,r-length/2),
                 (.018,r+length/2),(-.018,r+length/2))]
        surface('Faint twelve hour incisions',poly,'gold' if j == 0 else 'floor_seam',.003)


def hollow_moulding(label, rings, role, n=36, square=False):
    """Open-topped moulding with a real inner wall, no cap across the opening."""
    outer = []
    for z,r in rings:
        outer.append([(r*math.cos(a)/(max(abs(math.cos(a)),abs(math.sin(a))) if square else 1),
                       r*math.sin(a)/(max(abs(math.cos(a)),abs(math.sin(a))) if square else 1),z)
                      for a in (j*TAU/n for j in range(n))])
    inner = [[(.30*math.cos(j*TAU/n),.30*math.sin(j*TAU/n),z)
              for j in range(n)] for z in (rings[-1][0],rings[0][0])]
    path = outer + inner
    verts = [p for ring in path for p in ring]
    faces = [(k*n+j,k*n+(j+1)%n,((k+1)%len(path))*n+(j+1)%n,
              ((k+1)%len(path))*n+j) for k in range(len(path)) for j in range(n)]
    return Part(label,verts,faces,role,False)


def pillar():
    box('Lower marble plinth',(0,0,.11),(1.22,1.22,.22),'marble')
    box('Plinth vein band',(0,0,.24),(1.16,1.16,.06),'marble_vein')
    box('Second marble step',(0,0,.345),(1.05,1.05,.15),'marble')
    lathe('Turned base torus',[(.42,.49),(.46,.53),(.53,.51),(.59,.45),(.63,.45)],
          'marble',n=36,bevel=False)
    # Twelve genuinely concave flutes, not dark rods glued onto a cylinder.
    n = 72
    profile = (.45,.432,.398,.39,.398,.432)
    rings = [[(profile[j%6]*taper*math.cos(j*TAU/n),
               profile[j%6]*taper*math.sin(j*TAU/n),z) for j in range(n)]
             for z,taper in ((.58,1),(2.3,1),(5.5,.94),(6.9,.90))]
    for j in range(n):
        verts = [ring[j] for ring in rings]+[ring[(j+1)%n] for ring in rings]
        faces = [(k,k+1,len(rings)+k+1,len(rings)+k) for k in range(len(rings)-1)]
        Part('Recessed shaft flute',verts,faces,
             'white_shade' if j%6 in (2,3) else 'marble',False)
    flat_hoop('Single quiet gold collar',(0,0,3.75),.468,.06,.045,'gold',n=36)
    hollow_moulding('Neck bead',[(6.83,.42),(6.90,.46),(6.97,.46)],'marble_vein')
    hollow_moulding('Doric echinus',[(6.97,.44),(7.06,.46),(7.16,.55),(7.25,.56)],'marble')
    hollow_moulding('Open cut square abacus',[(7.25,.56),(7.50,.56)],'marble',square=True)
    hollow_moulding('Inner cut shade',[(7.40,.301),(7.50,.301)],'white_shade')


def arch_poly(radius, bottom, spring=2.15, steps=24):
    return [(-radius,bottom),(radius,bottom)]+[
        (radius*math.cos(j*math.pi/steps),spring+radius*math.sin(j*math.pi/steps))
        for j in range(steps+1)]


def arch_band(label, outer, inner, bottom, y, depth, role, spring=2.15):
    # A continuous open arch profile; side mouldings meet the semicircle exactly.
    box(label,(-((outer+inner)/2),y,(bottom+spring)/2),
        (outer-inner,depth,spring-bottom),role,bevel=False)
    box(label,(((outer+inner)/2),y,(bottom+spring)/2),
        (outer-inner,depth,spring-bottom),role,bevel=False)
    box(label,(0,y,bottom+(outer-inner)/2),(inner*2,depth,outer-inner),role,bevel=False)
    sector(label,inner,outer,0,math.pi,depth,role,y=y,steps=32,bevel=False).transform((0,0,spring))


def clock_dial(label, radius, centre, flat=False):
    """Shared chunky stopped dial, noon is +Z (upright) / +Y (flat)."""
    start = len(PARTS)
    ycyl(label+' face',(0,0,0),radius,.035,'watch_face',n=32)
    for j in range(12):
        a = j*TAU/12
        box(label+' hour mark',(radius*.79*math.sin(a),-.025,radius*.79*math.cos(a)),
            (.03,.03,.055 if j%3 == 0 else .035),'dial_mark',rot=(0,a,0),bevel=False)
    for length,angle in ((radius*.49,(11+59/60)*TAU/12),
                         (radius*.70,59*TAU/60)):
        beam(label+' stopped 11:59 hand',(0,-.05,0),
             (length*math.sin(angle),-.05,length*math.cos(angle)),.03,.03,'gold_dark',False)
    ycyl(label+' pivot',(0,-.07,0),.033,.03,'gold',n=8)
    move_group(start,centre,(-math.pi/2,0,0) if flat else (0,0,0))


def mirror():
    box('Lower marble stair',(0,0,.075),(1.90,.84,.15),'marble')
    box('Stair veining',(0,0,.165),(1.76,.72,.03),'marble_vein')
    box('Upper marble stair',(0,0,.24),(1.68,.62,.12),'marble')
    for x in (-.76,.76):
        box('Mirror side foot',(x,-.04,.34),(.25,.72,.14),'marble')
        box('Foot gold shoe',(x,-.12,.42),(.21,.29,.04),'brass',bevel=False)
    extrude('Gold arch backing',arch_poly(.89,.34),.18,'gold',y=0,bevel=False)
    arch_band('Deep carved groove',.86,.74,.39,-.115,.08,'gold_dark')
    arch_band('Brass inner moulding',.77,.705,.435,-.163,.07,'brass')
    # Edge front = -.205; pane front = -.235, exact .03m southward reveal.
    extrude('Pale pane edge',arch_poly(.735,.455),.04,'mirror_edge',y=-.185,bevel=False)
    extrude('Unbroken blue white mirror',arch_poly(.70,.49),.03,'mirror',y=-.22,bevel=False)
    # Raised winding gold vines and leaf pairs, kept outside the mirror aperture.
    for side in (-1,1):
        pts = [(side*(.815+.038*math.sin(j*math.pi/2)), -.18,
                .48+j*.137) for j in range(13)]
        tube('Climbing gold vine',pts,.027,'gold',n=5)
        for j in range(6):
            z = .65+j*.245
            x = side*(.815+.038*math.sin((z-.48)/.137*math.pi/2))
            extrude('Carved vine leaf',[(x,z),(x+side*.085,z+.075),
                    (x+side*.075,z+.145),(x+side*.015,z+.095)],.045,
                    'gold',y=-.19,bevel=False)
    crown = [(.815*math.cos(a),-.18,2.15+.815*math.sin(a))
             for a in (j*math.pi/24 for j in range(25))]
    tube('Arch crown vine',crown,.03,'gold',n=5)
    for j in range(1,12):
        a = j*math.pi/12
        x,z = .815*math.cos(a),2.15+.815*math.sin(a)
        rod('Crown leaf stem',(x,-.18,z),(.88*math.cos(a),-.18,2.15+.88*math.sin(a)),
            .025,'gold',n=5)
    # Clock supported by a short gold neck, exact overall height 3.40m.
    box('Crest neck',(0,-.015,3.04),(.16,.14,.20),'gold',bevel=False)
    ycyl('Crest clock case',(0,-.055,3.17),.20,.12,'gold_dark',n=32)
    hoop('Crest gold bezel',(0,-.13,3.17),.20,.04,.05,'gold',n=32)
    clock_dial('Crest clock',.16,(0,-.14,3.17))
    ball('Crest finial',(0,-.05,3.37),.03,'gold',detail=1)


def mem_hand():
    # Build in XY, axis toward +Y. The whole silhouette is 1.8 x .32m.
    poly = [(-.085,-.40),(.085,-.40),(.07,.88),(.16,.88),
            (0,1.18),(-.16,.88),(-.07,.88)]
    def bend(part):
        for v in part.verts:
            v.z += .10*((v.y+.62)/1.8)**2
        return part
    for cut in outside_circle(poly,(0,-.18),.07,n=20):
        bend(slab('Long minute hand',cut,.07,.055,'gold'))
    channel = [(-.03,-.22),(.03,-.22),(.03,.83),(0,.98),(-.03,.83)]
    for cut in outside_circle(channel,(0,-.18),.07,n=20):
        bend(slab('Inset central brass channel',cut,.086,.03,'brass'))
    start = len(PARTS)
    flat_hoop('Counterweight open ring',(0,-.46,.043),.16,.045,.055,'gold',n=20)
    flat_hoop('Dark axis hole',(0,-.18,.072),.105,.035,.045,'gold_dark',n=20)
    for p in PARTS[start:]:
        bend(p)
    move_group(0,rot=(0,0,math.radians(-32)))


def mem_lantern():
    cylinder('Heavy lantern foot',(0,0,.055),.235,.11,'iron',n=12,bevel=False)
    flat_hoop('Foot brass band',(0,0,.12),.218,.045,.04,'brass',n=12)
    cylinder('Glass seat',(0,0,.16),.205,.06,'iron',n=4,rot=(0,0,math.pi/4),bevel=False)
    for x in (-.145,.145):
        for y in (-.145,.145):
            rod('Four iron corner posts',(x,y,.18),(x,y,.55),.025,'iron',n=5)
    # Four solid glass panes, with a central peephole on the south-facing pane.
    # Role materials stay opaque grey in FBX; the opening exposes the flame there too.
    for x in (-.145,.145):
        box('Side glass',(x,0,.365),(.03,.27,.33),'glass',bevel=False)
    box('Back glass',(0,.145,.365),(.27,.03,.33),'glass',bevel=False)
    for x in (-.112,.112):
        box('Front glass sides',(x,-.145,.365),(.065,.03,.33),'glass',bevel=False)
    for z in (.225,.505):
        box('Front glass lintel',(0,-.145,z),(.16,.03,.05),'glass',bevel=False)
    cylinder('Wick cup',(0,0,.23),.075,.07,'brass',n=10,bevel=False)
    lathe('Warm flame',[(.255,.055),(.31,.075),(.40,.048),(.46,.016),(.48,.0)],
          'lamp_glow',n=10,bevel=False)
    # A second tongue is just behind the front aperture, not pasted outside the glass.
    lump('Visible flame heart',(0,-.115,.35),(.105,.06,.18),'lamp_glow',detail=1)
    lathe('Pitched iron hood',[(.55,.225),(.59,.235),(.68,.10),(.71,.085)],
          'iron',n=8,bevel=False)
    flat_hoop('Roof brass binding',(0,0,.57),.228,.035,.035,'brass',n=12)
    hoop('Carry handle',(0,.12,.762),.12,.032,.04,'iron',n=20)
    for x in (-.09,.09):
        rod('Handle mounting ears',(x,.12,.66),(x,.12,.73),.025,'iron',n=5)
    move_group(0,rot=(math.radians(-12),math.radians(-8),math.radians(12)))
    # Rigid tilt then fit height: finish() seats the lowest foot on Z=0.
    zs = [v.z for p in PARTS for v in p.verts]
    move_group(0,scale=(1,1,.85/(max(zs)-min(zs))))


def cloth_patch(label, width, length, role, shade, columns=12, rows=8, z=.035):
    """A closed continuous quilted solid; no gaps between adjacent fabric strips."""
    top = []
    for j in range(rows+1):
        y = -length/2+length*j/rows
        for i in range(columns+1):
            x = -width/2+width*i/columns
            zz = z+.027*math.sin(i*math.pi*.8+j*.55)+.014*math.sin(j*1.7)
            top.append((x,y,zz))
    n = len(top)
    verts = top+[(x,y,zz-.032) for x,y,zz in top]
    for j in range(rows):
        for i in range(columns):
            a = j*(columns+1)+i
            f = (a,a+1,a+columns+2,a+columns+1)
            Part(label,[verts[k] for k in f]+[verts[k+n] for k in f],
                 [(0,1,2,3),(4,7,6,5),(0,4,5,1),(1,5,6,2),(2,6,7,3),(3,7,4,0)],
                 shade if (i+j//3)%5 == 1 else role,False)


def mem_watch():
    cloth_patch('Unfurled tool cloth',.9,.6,'cloth','cloth_shade')
    # Face tilted only by perspective: points north so south-side camera sees the dial.
    cylinder('Pocket watch case',(-.10,-.015,.12),.275,.10,'gold',n=32,bevel=False)
    flat_hoop('Case chased rim',(-.10,-.015,.18),.274,.035,.04,'gold',n=32)
    clock_dial('Pocket watch',.235,(-.10,-.015,.17),flat=True)
    # North hinge, opened 105 degrees: face and lid both read from the south.
    start = len(PARTS)
    cylinder('Open brass lid',(-.10,-.26,0),.263,.045,'brass',n=28,bevel=False)
    flat_hoop('Lid embossed rim',(-.10,-.26,-.024),.24,.035,.03,'brass',n=28)
    move_group(start,loc=(0,.245,.14),rot=(math.radians(-105),0,0))
    rod('Pocket lid hinge',(-.20,.245,.14),(0,.245,.14),.03,'gold_dark',n=8)
    flat_hoop('Winding bow',(-.10,-.32,.13),.06,.03,.04,'gold',n=12)
    cylinder('Winding crown',(-.10,-.285,.13),.044,.07,'gold_dark',n=8,
             rot=(math.pi/2,0,0),bevel=False)
    # Two separate tools, offset east: screwdriver plus an open V of tweezers.
    rod('Screwdriver steel shaft',(.30,-.23,.07),(.30,.10,.07),.016,'steel',n=5)
    box('Screwdriver handle',(.30,-.18,.085),(.075,.17,.065),'gold_dark',bevel=False)
    box('Screwdriver blade',(.30,.13,.07),(.035,.08,.03),'steel',bevel=False)
    for x in (.365,.425):
        beam('Tweezers open prong',(.395,-.23,.075),(x,.18,.075),.03,.03,'steel',False)
    box('Tweezers grip',(.395,-.18,.085),(.075,.12,.03),'gold_dark',bevel=False)


def mem_shawl():
    # One continuous thick fabric surface, with curved diagonal folds and a
    # relaxed uneven perimeter, rather than a row of identical accordion bars.
    nx,ny = 16,9
    grid = []
    for j in range(ny+1):
        row = []
        for i in range(nx+1):
            u,v = i/nx,j/ny
            x = (u-.5)*(1-.045*math.cos(v*TAU))+.014*math.sin(v*5+u*3)
            y = (v-.5)*.55+.022*math.sin(u*8+v*2)
            z = (.085+.038*math.sin(u*math.pi*7+v*1.8)+
                 .022*math.cos(v*6-u*3)+.022*u*v+random.uniform(-.004,.004))
            row.append((x,y,z))
        grid.append(row)
    for j in range(ny):
        for i in range(nx):
            top = [grid[j][i],grid[j][i+1],grid[j+1][i+1],grid[j+1][i]]
            verts = top+[(x,y,z-.035) for x,y,z in top]
            Part('Flowing folded red cloth',verts,
                 [(0,1,2,3),(4,7,6,5),(0,4,5,1),(1,5,6,2),(2,6,7,3),(3,7,4,0)],
                 'shawl_red_dark' if math.cos((i+.5)/nx*math.pi*7+(j+.5)/ny*1.8)>.75
                 else 'shawl_red',False)
    for j,side in ((0,-1),(ny,1)):
        for i in range(nx+1):
            x,y,z = grid[j][i]
            length = random.uniform(.06,.085)
            tube('Relaxed short fringe',[(x,y,z-.012),
                 (x+.009,y+side*length*.5,max(.02,z-.045)),
                 (x-.01,y+side*length,max(.018,z-.055))],
                 .016,'shawl_red_dark',n=4)
    # Exact footprint 1.1 x .7m including fringe and uneven fold edges.
    xs = [v.x for p in PARTS for v in p.verts]
    ys = [v.y for p in PARTS for v in p.verts]
    move_group(0,scale=(1.1/(max(xs)-min(xs)),.7/(max(ys)-min(ys)),1))


def mem_ring():
    # Spec exception: .02m physical thickness, lower face .02 above the floor.
    flat_hoop('Outer memory light',(0,0,.03),1.10,.035,.02,'glow',n=24)
    flat_hoop('Inner memory light',(0,0,.03),.96,.03,.02,'glow',n=24)
    for j in range(12):
        a = j*TAU/12
        cylinder('Twelve memory points',(1.03*math.sin(a),1.03*math.cos(a),.03),
                 .03,.02,'glow',n=5,bevel=False)


# ---------------------------------------------------------------- export (identical ward mesh and FBX framework)
KEEP = {'Ground'}
# Explicit role lists in each spec item override the general 3~6 count.
MIN_ROLES = {'Ground':5,'Pillar':4,'Mirror':9,'MemHand':3,
             'MemLantern':4,'MemWatch':8,'MemShawl':2,'MemRing':1}
MAX_ROLES = dict(MIN_ROLES)
SPECS = [
    ('Ground',ground,30000), ('Pillar',pillar,3000), ('Mirror',mirror,8000),
    ('MemHand',mem_hand,2000), ('MemLantern',mem_lantern,2000),
    ('MemWatch',mem_watch,2500), ('MemShawl',mem_shawl,2500), ('MemRing',mem_ring,600),
]

def triangles(obj):
    obj.data.calc_loop_triangles()
    return len(obj.data.loop_triangles)


def bounds(obj):
    return [Vector(tuple(fn(v.co[i] for v in obj.data.vertices) for i in range(3))) for fn in (min, max)]


def assembled_mesh(name):
    objects = []
    for bevel in (True, False):
        verts, faces, indices = [], [], []
        for p in PARTS:
            if p.bevel != bevel:
                continue
            offset = len(verts)
            verts.extend(p.verts)
            faces.extend(tuple(offset + i for i in f) for f in p.faces)
            indices.extend([ROLES.index(p.role)] * len(p.faces))
        if not verts:
            continue
        data = bpy.data.meshes.new(name + '_parts')
        data.from_pydata(verts, [], faces)
        for role in ROLES:
            data.materials.append(MATS[role])
        for f, index in zip(data.polygons, indices):
            f.material_index = index
        bm = bmesh.new()
        bm.from_mesh(data)
        bmesh.ops.dissolve_degenerate(bm, dist=1e-7, edges=list(bm.edges))
        bmesh.ops.recalc_face_normals(bm, faces=list(bm.faces))
        bm.to_mesh(data)
        bm.free()
        obj = bpy.data.objects.new(name + '_parts', data)
        bpy.context.collection.objects.link(obj)
        bpy.context.view_layer.objects.active = obj
        obj.select_set(True)
        if bevel:
            mod = obj.modifiers.new('Worn edges', 'BEVEL')
            mod.width, mod.segments = .02, 2
            mod.limit_method, mod.angle_limit = 'ANGLE', math.radians(45)
            mod.use_clamp_overlap = True
            bpy.ops.object.modifier_apply(modifier=mod.name)
        obj.select_set(False)
        objects.append(obj)
    for obj in objects:
        obj.select_set(True)
    bpy.context.view_layer.objects.active = objects[0]
    if len(objects) > 1:
        bpy.ops.object.join()
    obj = bpy.context.object
    obj.name, obj.data.name = name, name + '_Mesh'
    return obj


def finish(name, budget):
    bpy.ops.object.select_all(action='DESELECT')
    obj = assembled_mesh(name)
    if name in ('Ground','Pillar'):
        # Volume-based normal recalculation is ambiguous on disconnected, zero-
        # thickness surfaces. The floor is explicitly a one-sided upward sheet.
        bm = bmesh.new()
        bm.from_mesh(obj.data)
        bm.normal_update()
        if name == 'Ground':
            reverse = [f for f in bm.faces if f.normal.z < 0]
        else:
            # The open flute strips are also sheets. Face their visible skin
            # radially outward so Unity backface culling cannot erase a flute.
            reverse = []
            for f in bm.faces:
                c = f.calc_center_median()
                if (.64 < c.z < 6.8 and ROLES[f.material_index] in ('marble','white_shade')
                        and f.normal.dot(Vector((c.x,c.y,0))) < 0):
                    reverse.append(f)
        bmesh.ops.reverse_faces(bm,faces=reverse)
        bm.to_mesh(obj.data)
        bm.free()
        obj.data.update()
    raw = triangles(obj)
    for attempt in range(4):
        if triangles(obj) <= budget:
            break
        mod = obj.modifiers.new('Budget', 'DECIMATE')
        mod.ratio, mod.delimit = (budget * .95) / triangles(obj), {'MATERIAL'}
        bpy.ops.object.modifier_apply(modifier=mod.name)
    assert triangles(obj) <= budget, (name, triangles(obj), budget)
    if name not in KEEP:
        lo, hi = bounds(obj)
        mid = (hi + lo) / 2
        for v in obj.data.vertices:
            v.co.x -= mid.x
            v.co.y -= mid.y
            v.co.z -= lo.z
    obj.data.update()
    used = {obj.data.materials[f.material_index].name for f in obj.data.polygons}
    for index in reversed(range(len(obj.material_slots))):
        if obj.data.materials[index].name not in used:
            obj.data.materials.pop(index=index)
    need = MIN_ROLES.get(name, 3)
    assert len(used) >= need, (name, 'needs role materials', need, used)
    assert len(used) <= MAX_ROLES.get(name, 6), (name, 'too many role materials', used)
    assert all(math.isfinite(c) for v in obj.data.vertices for c in v.co), name
    for mat in obj.data.materials:
        shader=next(n for n in mat.node_tree.nodes if n.type=='BSDF_PRINCIPLED')
        assert tuple(shader.inputs['Base Color'].default_value)==(.5,.5,.5,1), mat.name
    destination = OUTPUT / ('Kit_I_' + name + '.fbx')
    bpy.ops.export_scene.fbx(
        filepath=str(destination), use_selection=True,
        axis_forward='-Z', axis_up='Y', apply_scale_options='FBX_SCALE_UNITS',
        bake_space_transform=True, object_types={'MESH'}, use_mesh_modifiers=True,
        mesh_smooth_type='FACE', add_leaf_bones=False, bake_anim=False,
    )
    assert destination.is_file() and destination.stat().st_size > 1024, destination
    print(f'EXPORTED {name} {triangles(obj)} {tuple(round(d, 3) for d in obj.dimensions)} roles {len(used)} raw {raw}', flush=True)
    return obj


def preview(objects):
    """Left: sparse full map. Right: mirror/pillar and four enlarged memories.

    Every display copy is created after export. No stage, label or preview colour
    can leak into the grey role-material FBX files.
    """
    scene = bpy.context.scene
    scene.render.engine = 'CYCLES'
    scene.cycles.samples = 48
    scene.cycles.use_denoising = True
    scene.view_settings.view_transform = 'Standard'
    scene.view_settings.look = 'None'
    scene.view_settings.exposure = -.30
    scene.render.resolution_x,scene.render.resolution_y = 3200,2100
    scene.render.resolution_percentage = 100
    scene.render.image_settings.file_format = 'PNG'
    scene.render.filepath = str(PREVIEW)
    if scene.world is None:
        scene.world = bpy.data.worlds.new('Pale grey white limbo')
    scene.world.color = (.93,.93,.95)
    scene.world.use_nodes = True
    bg = scene.world.node_tree.nodes['Background']
    bg.inputs['Color'].default_value = (.93,.93,.95,1)
    bg.inputs['Strength'].default_value = .55
    # Only after every FBX is written, apply the exact preview palette.
    for role,mat in MATS.items():
        mat.diffuse_color = (*PALETTE[role],1)
        shader = next(n for n in mat.node_tree.nodes if n.type == 'BSDF_PRINCIPLED')
        shader.inputs['Base Color'].default_value = (*PALETTE[role],1)
        if role in ('glow','lamp_glow'):
            shader.inputs['Emission Color'].default_value = (*PALETTE[role],1)
            shader.inputs['Emission Strength'].default_value = .03 if role == 'glow' else .3
    by = {o.name:o for o in objects}
    for obj in objects:
        obj.hide_render = True
    review = []
    def copy(name, loc, scale=1, angle=0):
        ob = bpy.data.objects.new('Review '+name,by[name].data)
        scene.collection.objects.link(ob)
        ob.location,ob.scale = loc,(scale,)*3
        ob.rotation_euler.z = angle
        review.append(ob)
        return ob
    def label(text, loc, size=.22, align='CENTER'):
        cu = bpy.data.curves.new('Review caption','FONT')
        cu.body,cu.align_x,cu.size,cu.extrude = text,align,size,.001
        ob = bpy.data.objects.new('Caption '+text,cu)
        scene.collection.objects.link(ob)
        ob.location = loc
        cu.materials.append(MATS['iron'])
        review.append(ob)
        return ob
    # Entire map at uniform 1:6.25 display scale; true placement coordinates.
    scale,ox,oy = .16,-8.0,1.0
    def map_point(x,y,z=0):
        return (ox+x*scale,oy+y*scale,z*scale)
    copy('Ground',map_point(0,0),scale)
    for x,y in ((-17,-35),(18,-31),(-18,-17),(16,-12),(-16,0),
                (19,6),(-18,18),(16,23),(-16,34),(18,36)):
        copy('Pillar',map_point(x,y),scale)
    copy('Mirror',map_point(0,31),scale)
    for name,(x,y) in zip(('MemHand','MemLantern','MemWatch','MemShawl'),
                           ((-6,15),(-2.2,18),(2.2,18),(6,15))):
        copy('MemRing',map_point(x,y,.02),scale)
        copy(name,map_point(x,y,.04),scale)
    label('THE PAUSED SECOND',(-8,9.6,.01),.34)
    label('44 x 84 m  /  FULL MAP',(-8,-6.7,.01),.20)
    label('No walls. No ceiling. Only white.',(-8,-7.25,.01),.18)
    # Large architectural details on the upper right; each is grounded.
    copy('Mirror',(-1.4,5.1,0),1.45)
    copy('Pillar',(3.0,5.1,0),.74)
    label('Mirror  /  3.40 m',(-1.4,4.15,.01),.23)
    label('Pillar  /  7.50 m',(3.0,4.15,.01),.23)
    label('CARVED IN THE QUIET',(1.0,9.6,.01),.34)
    # Two rows of memories at a readable scale, seen from the same 57 degrees.
    for name,loc,s in (
        ('MemHand',(-1.7,.7,0),1.8),
        ('MemLantern',(3.2,.7,0),2.3),
        ('MemWatch',(-1.7,-3.7,0),2.65),
        ('MemShawl',(3.2,-3.7,0),2.5)):
        ob = copy(name,loc,s)
        # Keep bottom recenter unchanged. MemRing is placed .02 above its mount floor.
        label(name,(loc[0],loc[1]-1.75,.01),.24)
    copy('MemRing',(6.55,-3.6,.02),1.20)
    label('MemRing',(6.55,-5.45,.01),.20)
    label('FOUR MEMORIES',(1.0,2.95,.01),.28)
    label('South view / 57 degrees',(1.0,-7.25,.01),.18)
    # Matte white review floor catches soft contact shadows under the closeups.
    stage_mat = bpy.data.materials.new('Preview white only')
    stage_mat.use_nodes = True
    shader = stage_mat.node_tree.nodes.get('Principled BSDF')
    shader.inputs['Base Color'].default_value = (.93,.93,.95,1)
    shader.inputs['Roughness'].default_value = .95
    mesh = bpy.data.meshes.new('Preview stage')
    mesh.from_pydata([(-200,-200,-.015),(200,-200,-.015),
                      (200,200,-.015),(-200,200,-.015)],[],[(0,1,2,3)])
    mesh.materials.append(stage_mat)
    stage = bpy.data.objects.new('Preview shadow catcher',mesh)
    scene.collection.objects.link(stage)
    # South-east = +X / -Y. A broad soft key sculpts the white surfaces.
    for name,loc,energy,size in (
        ('South east soft key',(8,-10,18),2900,9),
        ('North west soft fill',(-10,7,15),650,12)):
        light = bpy.data.objects.new(name,bpy.data.lights.new(name,'AREA'))
        scene.collection.objects.link(light)
        light.location = loc
        light.data.energy,light.data.shape,light.data.size = energy,'DISK',size
        light.rotation_euler = (Vector((-2,1,0))-light.location).to_track_quat('-Z','Y').to_euler()
    cam = bpy.data.objects.new('Cam',bpy.data.cameras.new('Cam'))
    scene.collection.objects.link(cam)
    scene.camera = cam
    cam.data.type = 'ORTHO'
    a = math.radians(57)
    cam.location = Vector((0,-60*math.cos(a),60*math.sin(a)))
    cam.rotation_euler = (-cam.location).to_track_quat('-Z','Y').to_euler()
    bpy.context.view_layer.update()
    inv = cam.matrix_world.inverted()
    corners = [inv@(ob.matrix_world@Vector(c)) for ob in review for c in ob.bound_box]
    left,right = min(v.x for v in corners),max(v.x for v in corners)
    low,high = min(v.y for v in corners),max(v.y for v in corners)
    cam.location += cam.rotation_euler.to_matrix()@Vector(((left+right)/2,(low+high)/2,0))
    aspect = scene.render.resolution_x/scene.render.resolution_y
    cam.data.ortho_scale = max(right-left,(high-low)*aspect)*1.10
    PREVIEW.parent.mkdir(parents=True,exist_ok=True)
    bpy.ops.render.render(write_still=True)
    assert PREVIEW.is_file() and PREVIEW.stat().st_size > 1024
    print('PREVIEW '+str(PREVIEW),flush=True)


def validate(objects):
    """Export invariants: dimensions, datums, palette roles and no underside."""
    by = {ob.name:ob for ob in objects}
    assert len(by) == 8
    for name,_,budget in SPECS:
        obj = by[name]
        assert triangles(obj) <= budget
        lo,hi = bounds(obj)
        if name not in KEEP:
            assert abs(lo.z) < 1e-5, (name,'floor datum',lo.z)
            assert abs(lo.x+hi.x) < 1e-5 and abs(lo.y+hi.y) < 1e-5, (name,'centre')
    lo,hi = bounds(by['Ground'])
    assert (lo.x,lo.y,hi.x,hi.y) == (-22,-42,22,42)
    assert all(p.normal.z > .99 for p in by['Ground'].data.polygons), 'Ground underside'
    assert abs(by['Pillar'].dimensions.z-7.5) < .002
    assert abs(by['Mirror'].dimensions.x-1.9) < .002
    assert abs(by['Mirror'].dimensions.z-3.4) < .004
    assert abs(by['MemLantern'].dimensions.z-.85) < .004
    assert abs(by['MemShawl'].dimensions.x-1.1) < .002
    assert abs(by['MemShawl'].dimensions.y-.7) < .002
    assert abs(by['MemRing'].dimensions.x-2.2) < .002
    assert abs(by['MemRing'].dimensions.z-.02) < .002
    print('VALIDATED eight budgets, ground surface, dimensions and floor origins',flush=True)


def main():
    global PARTS
    bpy.ops.wm.read_factory_settings(use_empty=True)
    for mat in list(bpy.data.materials):
        bpy.data.materials.remove(mat)
    scene = bpy.context.scene
    scene.unit_settings.system,scene.unit_settings.scale_length = 'METRIC',1.0
    materials()
    load_font()
    OUTPUT.mkdir(parents=True,exist_ok=True)
    out = []
    for i,(name,build,budget) in enumerate(SPECS):
        PARTS = []
        random.seed(61126+i*977)
        build()
        out.append(finish(name,budget))
    validate(out)
    print('ANCHOR Mirror',ANCHORS['Mirror'],flush=True)
    print('MOUNT MemRing: place its bottom-origin at floor + .02m',flush=True)
    preview(out)
    print('DONE',flush=True)


if __name__ == '__main__':
    main()
