# 스토리 맵 3 '거울 같은 호숫가' 키트 — 맑은 한낮 호수 · 잔교 · 보트 · 피크닉, 2026-11-25
# 실행: "C:/Program Files/Blender Foundation/Blender 5.3/blender.exe" -b --factory-startup -P Art/tools/blender_kit_story_lake.py
# 회색 역할 재질 FBX 15조각과 여름 하늘 미리보기. Unity가 재질 이름으로 색을 적용한다.
# Blender (X,Y,Z) = Unity (X,Z,Y); 카메라는 남쪽(-Y)에서 북쪽을 본다.
import math
import random
from collections import defaultdict
from pathlib import Path

import bmesh
import bpy
from mathutils import Euler, Matrix, Vector

ROOT = Path(__file__).resolve().parents[2]
OUTPUT = ROOT / 'Game/Assets/Art/Characters/StoryLake'
PREVIEW = ROOT / 'Art/tools/specs/blender_kit_story_lake_preview.png'
TAU = math.tau
ROLES = (
    'grass', 'grass_light', 'grass_dark', 'flower_white', 'flower_yellow',
    'flower_pink', 'dirt', 'dirt_dark', 'sand', 'pebble', 'pebble_dark',
    'stone', 'stone_dark', 'moss', 'lakebed', 'wood', 'wood_dark', 'wood_light',
    'rope', 'iron', 'iron_dark', 'brass', 'gold', 'paint_white', 'paint_blue',
    'paint_red', 'clock_face', 'lantern_glow', 'cloth_red', 'cloth_white',
    'wicker', 'ivory', 'leaf', 'leaf_dark', 'leaf_light', 'bark', 'reed',
    'reed_tip', 'lily_pad', 'lily_pink', 'lily_core', 'shadow', 'shadow_rim',
    'hat_straw', 'ribbon',
)
HARD = {'stone', 'stone_dark', 'wood', 'wood_dark', 'wood_light', 'iron',
        'iron_dark', 'brass', 'gold', 'paint_white', 'paint_blue', 'paint_red',
        'clock_face', 'ivory', 'wicker'}
PALETTE = {  # Preview only; exported Principled materials remain flat grey.
    'grass': (.45,.72,.38), 'grass_light': (.62,.85,.45),
    'grass_dark': (.3,.52,.3), 'flower_white': (.97,.97,.92),
    'flower_yellow': (1,.85,.3), 'flower_pink': (1,.6,.75),
    'dirt': (.65,.5,.35), 'dirt_dark': (.45,.34,.24),
    'sand': (.92,.84,.65), 'pebble': (.7,.68,.66), 'pebble_dark': (.5,.48,.5),
    'stone': (.68,.68,.72), 'stone_dark': (.45,.45,.52), 'moss': (.4,.6,.35),
    'lakebed': (.3,.45,.5), 'wood': (.62,.44,.3), 'wood_dark': (.38,.26,.18),
    'wood_light': (.8,.66,.48), 'rope': (.85,.75,.55), 'iron': (.3,.32,.38),
    'iron_dark': (.16,.17,.22), 'brass': (.88,.68,.35), 'gold': (1,.82,.38),
    'paint_white': (.95,.95,.92), 'paint_blue': (.3,.5,.75),
    'paint_red': (.85,.3,.28), 'clock_face': (1,.96,.86),
    'lantern_glow': (1,.8,.45), 'cloth_red': (.88,.3,.3),
    'cloth_white': (.97,.95,.9), 'wicker': (.78,.6,.35), 'ivory': (.95,.92,.82),
    'leaf': (.38,.65,.35), 'leaf_dark': (.25,.45,.28),
    'leaf_light': (.6,.8,.42), 'bark': (.42,.32,.25), 'reed': (.6,.65,.38),
    'reed_tip': (.5,.35,.25), 'lily_pad': (.32,.62,.36),
    'lily_pink': (1,.7,.82), 'lily_core': (1,.85,.35),
    'shadow': (.08,.06,.12), 'shadow_rim': (1,.75,.45),
    'hat_straw': (.92,.8,.52), 'ribbon': (.95,.45,.55),
}
MATS = {}
PARTS = []

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
    """Batch solid components before the shared 0.025m/two-segment bevel."""

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
    """Solid polygon in XZ; +Y is its decorated/front surface."""
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


def cylinder(label, loc, radius, depth, role, n=12, rot=(0,0,0), top=None):
    return lathe(label, [(-depth/2, radius),
                        (depth/2, radius if top is None else top)], role, n).transform(loc, rot)


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


def rod(label, a, b, radius, role, n=6, bevel=True):
    return tube(label, [a,b], radius, role, n, bevel)


def beam(label, a, b, width, depth, role):
    a, b = Vector(a), Vector(b)
    part = box(label, (0,0,0), (width,depth,(b-a).length), role)
    matrix = Matrix.Translation((a+b)/2) @ (b-a).to_track_quat('Z','Y').to_matrix().to_4x4()
    part.verts = [matrix @ v for v in part.verts]
    return part


def hoop(label, loc, radius, band, depth, role, n=24, rot=(0,0,0)):
    """Open ring in XZ, axis Y; no disk hiding the negative space."""
    verts = [(r*math.cos(j*TAU/n), y, r*math.sin(j*TAU/n))
             for r,y in ((radius-band,-depth/2), (radius,-depth/2),
                         (radius,depth/2), (radius-band,depth/2)) for j in range(n)]
    faces = [(k*n+j, k*n+(j+1) % n, ((k+1) % 4)*n+(j+1) % n,
              ((k+1) % 4)*n+j) for k in range(4) for j in range(n)]
    return Part(label, verts, faces, role).transform(loc, rot)


def sector(label, inner, outer, start, end, depth, role, y=0, steps=4):
    angles = [start+(end-start)*j/steps for j in range(steps+1)]
    polygon = [(outer*math.cos(a), outer*math.sin(a)) for a in angles]
    polygon += [(inner*math.cos(a), inner*math.sin(a)) for a in reversed(angles)]
    return extrude(label, polygon, depth, role, y)


def lump(label, loc, size, role, detail=1):
    bm = bmesh.new()
    bmesh.ops.create_icosphere(bm, subdivisions=detail, radius=1)
    for v in bm.verts:
        v.co *= random.uniform(.91,1.09)
        v.co = Vector(loc)+Vector(tuple(v.co[i]*size[i]/2 for i in range(3)))
    bm.verts.ensure_lookup_table()
    result = Part(label, [tuple(v.co) for v in bm.verts],
                  [tuple(v.index for v in f.verts) for f in bm.faces], role)
    bm.free()
    return result


def scroll(label, loc, radius=.3, flip=1):
    x,y,z = loc
    points = []
    for j in range(11):
        t = j/10
        a, r = -.8+t*math.pi*1.9, radius*(1-.73*t)
        points.append((x+flip*r*math.cos(a),y,z+r*math.sin(a)))
    return tube(label, points, .025 if radius < .3 else .035, 'gold')


def numeral(text, loc, height=.28, role='gold', angle=0):
    """Bold mesh bars, no font dependency. I, V and X are actual silhouettes."""
    start = len(PARTS)
    pitch = height*.45
    for i, char in enumerate(text):
        x = (i-(len(text)-1)/2)*pitch
        w = max(.04,height*.15)
        if char == 'I':
            box('Numeral I', (x,0,0), (w,.065,height), role)
        elif char == 'X':
            for s in (-1,1):
                beam('Numeral X', (x-s*height*.18,0,-height/2),
                     (x+s*height*.18,0,height/2), w,.065,role)
        elif char == 'V':
            for s in (-1,1):
                beam('Numeral V', (x,0,-height/2),
                     (x+s*height*.21,0,height/2), w,.065,role)
    move_group(start, loc, (0,angle,0))


def clock(loc, radius, hands=True):
    start = len(PARTS)
    cylinder('Clock iron back', (0,-.045,0), radius,.13,'iron',n=24,rot=(math.pi/2,0,0))
    cylinder('Cream enamel dial', (0,.045,0),radius*.86,.07,'clock_face',n=24,rot=(math.pi/2,0,0))
    hoop('Brass clock bezel',(0,.075,0),radius,.13*radius,.15,'brass')
    hoop('Gold inner bezel',(0,.12,0),radius*.84,.045,.06,'gold')
    for j in range(12):
        a = j*TAU/12
        box('Gold hour index',(radius*.69*math.sin(a),.13,radius*.69*math.cos(a)),
            (.045,.05,radius*.12),'gold',rot=(0,a,0))
    if hands:
        beam('Minute hand',(0,.19,0),(-radius*.32,.19,radius*.55),.065,.055,'iron')
        beam('Hour hand',(0,.20,0),(radius*.44,.20,radius*.18),.09,.06,'iron')
        cylinder('Clock pivot',(0,.24,0),.07,.06,'gold',n=8,rot=(math.pi/2,0,0))
    move_group(start,loc)


# ======================= 거울 같은 호숫가 =======================
# This is the exact Unity invisible-edge-wall polygon. Never grow or bevel it.
OUTLINE = [
    (-17.0,-26.0), (-18.5,-15.0), (-17.0,-5.0), (-13.0,1.5),
    (-7.0,4.0), (0.0,4.6), (6.0,4.2), (12.0,2.0),
    (16.5,-4.0), (18.0,-15.0), (16.0,-26.0),
]


def inside(pt, poly=OUTLINE):
    x, y = pt
    result = False
    for i, (x1,y1) in enumerate(poly):
        x2,y2 = poly[(i+1) % len(poly)]
        if (y1 > y) != (y2 > y) and x < (x2-x1)*(y-y1)/(y2-y1)+x1:
            result = not result
    return result


def edge_distance(pt):
    p = Vector(pt)
    distances = []
    for i, a in enumerate(OUTLINE):
        a, b = Vector(a), Vector(OUTLINE[(i+1) % len(OUTLINE)])
        d = b-a
        t = max(0, min(1, (p-a).dot(d)/d.length_squared))
        distances.append((p-(a+t*d)).length)
    return min(distances)


def clip(poly, axis, value, above):
    """Half-plane clipping keeps surface tiles strictly inside OUTLINE."""
    out = []
    if not poly:
        return out
    a = poly[-1]
    for b in poly:
        ia = (a[axis] >= value) if above else (a[axis] <= value)
        ib = (b[axis] >= value) if above else (b[axis] <= value)
        if ia != ib:
            t = (value-a[axis])/(b[axis]-a[axis])
            out.append(tuple(a[k]+t*(b[k]-a[k]) for k in range(2)))
        if ib:
            out.append(b)
        a = b
    return out


def clipped_rect(x0, x1, y0, y1, poly=OUTLINE):
    for axis, value, above in ((0,x0,True),(0,x1,False),(1,y0,True),(1,y1,False)):
        poly = clip(poly,axis,value,above)
    return poly


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


def blob(x, y, radius, n=8):
    phase = random.uniform(0,TAU)
    return [(x+radius*r*math.cos(a),y+radius*r*math.sin(a))
            for a,r in [(phase+j*TAU/n+random.uniform(-.07,.07),
                         random.uniform(.78,1.0)) for j in range(n)]]


def flat_ring(label, xy, radius, band, top, role, n=64):
    # Preserve the ground's inlaid rings without bevel/decimation drift.
    p = hoop(label,(xy[0],xy[1],top-.02),radius,band,.04,role,n,
             rot=(math.pi/2,0,0))
    p.bevel = False
    return p



def halfplane(poly, a, b, right=True):
    """Clip against a directed edge; OUTLINE is clockwise, interior on right."""
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


def within_outline(poly):
    for i, a in enumerate(OUTLINE):
        poly = halfplane(poly, a, OUTLINE[(i+1) % len(OUTLINE)])
    return poly


def ribbon_solid(label, points, width, role, thick=.045):
    """Chunky rectangular ribbon, with closed edges, for willow leaves and ties."""
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


def petal(label, loc, length, width, rise, role, angle=0):
    # A six-sided pointed, folded petal with a real 4cm underside.
    verts = [(0,0,0),(-width/2,length*.48,rise*.65),(0,length,rise),
             (width/2,length*.48,rise*.65),(0,length*.45,rise*.9)]
    verts += [(x,y,z-.04) for x,y,z in verts[:4]]
    faces = [(0,1,4),(1,2,4),(2,3,4),(3,0,4),(5,8,7,6),
             (0,5,6,1),(1,6,7,2),(2,7,8,3),(3,8,5,0)]
    return Part(label,verts,faces,role,False).transform(loc,(0,0,angle))


def coil(loc, radius=.34, turns=2.5):
    x,y,z = loc
    points = []
    for j in range(43):
        t = j/42
        a, r = turns*TAU*t, radius*(1-.74*t)
        points.append((x+r*math.cos(a),y+r*math.sin(a),z+.045*t))
    tube('Laid mooring rope coil',points,.03,'rope',n=5)


def reed_cluster(loc=(0,0,0), count=10):
    start = len(PARTS)
    for j in range(count):
        a = j*TAU/count+random.uniform(-.15,.15)
        r, h = random.uniform(.12,.42),random.uniform(.8,1.6)
        x,y = r*math.cos(a),r*math.sin(a)
        dx,dy = .23*math.cos(a),.23*math.sin(a)
        tube('Curved reed stalk',[(x,y,0),(x+dx*.3,y+dy*.3,h*.6),
                                  (x+dx,y+dy,h)],.023,'reed',n=5)
        if j % 2 == 0:
            rod('Velvet cattail',(x+dx*.75,y+dy*.75,h*.82),
                (x+dx,y+dy,h+.1),.055,'reed_tip',n=6,bevel=False)
        ribbon_solid('Reed lanceolate blade',[(x,y,.13),(x+dx*.8,y+dy*.8,h*.6),
                     (x+dx*2,y+dy*2,h*.76)],.09,
                     ('leaf','leaf_dark','leaf_light')[j % 3])
    move_group(start,loc)


def ground():
    # Exact boundary is never bevelled or simplified. Walkable top is Z=0.
    slab('Exact meadow skirt',OUTLINE,-.8,.7,'dirt_dark')
    # Shared jittered cell corners form a continuous, irregular 2-5m patchwork.
    nodes = {(i,j):(-21+i*3.3+(random.uniform(-.65,.65) if i not in (0,13) else 0),
                    -29+j*3.3+(random.uniform(-.65,.65) if j not in (0,11) else 0))
             for i in range(14) for j in range(12)}
    edges = {}
    def jagged_edge(a,b):
        key = tuple(sorted((a,b)))
        if key not in edges:
            p,q = Vector(nodes[key[0]]),Vector(nodes[key[1]])
            d = q-p
            n = Vector((-d.y,d.x)).normalized()
            edges[key] = [tuple(p+d*t+n*random.uniform(-.28,.28)) for t in (1/3,2/3)]
        return edges[key] if a == key[0] else list(reversed(edges[key]))
    circle = [(8*math.cos(-j*TAU/64),-10+8*math.sin(-j*TAU/64)) for j in range(64)]
    for i in range(13):
        for j in range(11):
            corners = [(i,j),(i,j+1),(i+1,j+1),(i+1,j)]
            poly = []
            for k,a in enumerate(corners):
                poly += [nodes[a]]+jagged_edge(a,corners[(k+1) % 4])
            poly = within_outline(poly)
            # Partition, rather than overlay, so the clearing has no z fighting.
            remain = poly
            role = ('grass','grass_light','grass_dark')[(i+2*j) % 3]
            for k,a in enumerate(circle):
                b = circle[(k+1) % len(circle)]
                outside = halfplane(remain,a,b,False)
                slab('Jagged meadow patch',outside,0,.8,role)
                remain = halfplane(remain,a,b)
                if not remain:
                    break
            slab('Short clearing grass',remain,0,.8,'grass_light')
    # A continuous 1.6m path skirts the east side of the eight-metre clearing.
    waypoints = [(0,-25),(3,-23),(7.7,-20),(10,-16),(10.5,-11),
                 (10,-6),(7.5,-2),(4,1),(2,4)]
    points = []
    for a,b in zip(waypoints,waypoints[1:]):
        for j in range(5):
            t = j/5
            points.append((a[0]*(1-t)+b[0]*t,a[1]*(1-t)+b[1]*t))
    points.append(waypoints[-1])
    for j,(a,b) in enumerate(zip(points,points[1:])):
        d = (Vector(b)-Vector(a)).normalized()
        n = Vector((-d.y,d.x))*.8
        p,q = Vector(a),Vector(b)
        slab('Winding dirt path',within_outline([tuple(p-n),tuple(p+n),tuple(q+n),tuple(q-n)]),
             .006,.06,'dirt')
        if j % 2 == 0:
            for sign in (-1,1):
                v = (p+q)/2+n*sign*1.07
                lump('Path dark edge stone',(v.x,v.y,.035),(.24,.29,.12),'dirt_dark')
    for index in (2,8,15,22,29,36):
        x,y = points[index]
        slab('Flat stepping slab',blob(x,y,.57,7),.055,.1,'stone')
    # Only low white dots mark the battle boundary; no reeds/flowers in its disc.
    for j in range(72):
        a = j*TAU/72
        cylinder('Faint clearing flower dot',(8*math.cos(a),-10+8*math.sin(a),.025),
                 .065,.05,'flower_white',n=5).bevel=False
    clusters = []
    for attempt in range(4000):
        if len(clusters) == 25:
            break
        x,y = random.uniform(-16,16),random.uniform(-24,2)
        if not inside((x,y)) or edge_distance((x,y)) < 1.8 or math.hypot(x,y+10) < 8.6:
            continue
        if min(math.hypot(x-px,y-py) for px,py in points) < 1.5:
            continue
        if any(math.hypot(x-px,y-py) < 1.3 for px,py in clusters):
            continue
        clusters.append((x,y))
        for k in range(5):
            dx,dy = random.uniform(-.36,.36),random.uniform(-.36,.36)
            h = random.uniform(.13,.27)
            rod('Short flower stem',(x+dx,y+dy,0),(x+dx,y+dy,h),.021,'leaf',n=4,bevel=False)
            cylinder('Meadow flower head',(x+dx,y+dy,h),.095,.055,
                     ('flower_white','flower_yellow','flower_pink')[len(clusters) % 3],n=5).bevel=False
    assert len(clusters) == 25
    # North shore wedges run from the exact walk boundary down into the lake.
    shore = OUTLINE[3:8]
    for a,b in zip(shore,shore[1:]):
        d = Vector(b)-Vector(a)
        out = Vector((-d.y,d.x)).normalized()
        count = math.ceil(d.length/.8)
        for j in range(count):
            p,q = Vector(a)+d*j/count,Vector(a)+d*(j+1)/count
            for k in range(3):
                t,u = k/3,(k+1)/3
                corners = [(p+out*1.5*t,-.6*t),(q+out*1.5*t,-.6*t),
                           (q+out*1.5*u,-.6*u),(p+out*1.5*u,-.6*u)]
                verts = [(v.x,v.y,z+dz) for dz in (-.12,0) for v,z in corners]
                Part('Sloping beach',verts,[(3,2,1,0),(4,5,6,7),(0,1,5,4),
                     (1,2,6,5),(2,3,7,6),(3,0,4,7)],
                     ('sand','pebble','pebble_dark')[(j+k) % 3],False)
    for j in range(10):
        a,b = shore[j//3 if j < 9 else 3],shore[min(4,j//3+1)]
        t = (j % 3+.5)/3
        x,y = a[0]*(1-t)+b[0]*t,a[1]*(1-t)+b[1]*t+.6
        lump('Half sunk shore rock',(x,y,-.18),(1.2,.95,.8),'stone' if j % 2 else 'stone_dark')
        lump('Rock moss cap',(x,y,.11),(.8,.68,.17),'moss')
    # West, south and east are solid, readable hedge banks, outside the arena.
    for i,a in enumerate(OUTLINE):
        if 3 <= i <= 6:
            continue
        b = OUTLINE[(i+1) % len(OUTLINE)]
        d = Vector(b)-Vector(a)
        out = Vector((-d.y,d.x)).normalized()
        count = math.ceil(d.length/1.4)
        for j in range(count):
            v = Vector(a)+d*(j+.5)/count+out*.2
            h = random.uniform(.6,1.2)
            lump('Raised grassy bank',(v.x,v.y,h*.25),(1.9,1.8,h*1.3),'grass_dark')
            lump('Dark hedge crown',(v.x,v.y,h*.66),(1.45,1.4,h*.72),'leaf_dark')
    # Unity supplies the translucent water at Z=-.15; no water plane is exported.
    box('Lake bed',(0,37,-1.26),(120,66,.12),'lakebed',bevel=False)
    for j in range(36):
        x,y = random.uniform(-57,57),random.uniform(7,65)
        slab('Submerged sediment',blob(x,y,random.uniform(1.5,3.8)),
             -1.19,.05,'pebble_dark' if j % 2 else 'moss')
    box('Far shore footing',(0,67,-.4),(120,10,.8),'dirt_dark',bevel=False)
    for j in range(34):
        lump('Rolling far bank',(-59+j*3.6,67+random.uniform(-1,1),.65),
             (7,9,random.uniform(2.4,4.6)),'grass_dark' if j % 2 else 'grass')
    for j in range(9):
        x,y = -53+j*13,68+random.uniform(-1,1)
        rod('Far tree trunk',(x,y,0),(x,y,3.5),.33,'bark',n=6,bevel=False)
        radius = random.uniform(2,4)
        for k in range(3):
            lump('Round distant tree crown',(x+(k-1)*radius*.6,y,3.7+(k % 2)*.9),
                 (radius*1.7,radius*1.6,radius*1.6),'leaf_dark' if k % 2 else 'leaf',detail=2)
    for x in (-35,8,39):
        y = 65.8
        box('Far white cottage',(x,y,1.1),(2,1.8,1.8),'paint_white')
        extrude('Cottage gabled red roof',[(-1.2,1.9),(1.2,1.9),(0,2.8)],
                2.1,'paint_red').transform((x,y,0))
        box('Cottage blue door',(x,y-.93,.8),(.45,.06,1.1),'paint_blue')
        for dx in (-.65,.65):
            box('Cottage window',(x+dx,y-.94,1.3),(.36,.05,.42),'wood_dark',bevel=False)
    for x,y in ((-14,4.8),(-10,6),(-6.8,5.8),(-3,6.4),(6.5,6),(10.8,5.3),(14.5,4.8)):
        reed_cluster((x+random.uniform(-.25,.25),y+random.uniform(-.2,.2),-.45),random.randint(8,12))


def jetty():
    for j in range(80):
        y = .125+j*.25
        width = 3 if y < 18 else 4.4
        depth = .232+random.uniform(-.006,.006)
        # Built-in chamfers keep every walkable top exactly at Z=0, while
        # avoiding the shared bevel's many triangles on eighty repeated boards.
        verts = [(x,y+dy,z) for z,inset in ((-.14,.015),(-.125,0),(-.015,0),(0,.015))
                 for x,dy in ((-width/2+inset,-depth/2+inset),
                              (width/2-inset,-depth/2+inset),
                              (width/2-inset,depth/2-inset),
                              (-width/2+inset,depth/2-inset))]
        faces = [(3,2,1,0),(12,13,14,15)]
        faces += [(k*4+i,k*4+(i+1) % 4,(k+1)*4+(i+1) % 4,(k+1)*4+i)
                  for k in range(3) for i in range(4)]
        Part('Crosswise chamfered deck plank',verts,faces,
             ('wood','wood_light','wood','wood_light','wood_dark')[j % 5],False)
        if j % 5 == 0:
            lump('Deck wood grain knot',(random.uniform(-1,1),y,-.018),
                 (.13,.07,.04),'wood_dark')
            for x in (-1.23,1.23):
                cylinder('Deck iron nail',(x,y,-.004),.035,.04,'iron_dark',n=5).bevel=False
    for x in (-1.4,1.4):
        box('Continuous edge beam',(x,10,-.23),(.2,20,.28),'wood_dark')
        for j in range(9):
            y = j*2.5
            box('Pier driven post',(x,y,-.87),(.23,.23,2.26),'wood_dark')
            box('Weathered post top',(x,y,.26),(.28,.28,.12),'wood_light')
            for z in (-.1,.12):
                hoop('Iron post band',(x,y,z),.18,.045,.07,'iron_dark',n=8,
                     rot=(math.pi/2,0,math.pi/8)).bevel=False
            if j % 2 == 0:
                for z in (.01,.07):
                    hoop('Rope post lashing',(x,y,z),.21,.04,.05,'rope',n=10,
                         rot=(math.pi/2,0,0)).bevel=False
    # Three low sides leave the shore approach completely open.
    for x in (-2.12,2.12):
        for y in (18.1,19.05,19.9):
            box('White platform rail post',(x,y,.39),(.12,.12,.78),'paint_white')
            cylinder('Brass rail cap',(x,y,.82),.09,.08,'brass',n=8).bevel=False
        for z in (.28,.69):
            box('Side platform rail',(x,19,z),(.09,1.9,.09),'paint_white')
    for x in (-1.06,0,1.06):
        box('End railing post',(x,19.9,.39),(.12,.12,.78),'paint_white')
        cylinder('End brass post cap',(x,19.9,.82),.09,.08,'brass',n=8).bevel=False
    for z in (.28,.69):
        box('North platform railing',(0,19.9,z),(4.24,.09,.09),'paint_white')
    for y in (18.35,19.05):
        beam('East ladder stile',(2.25,y,.18),(2.55,y,-1.85),.09,.09,'wood')
    for j in range(6):
        z = -.15-j*.29
        x = 2.25+(.18-z)*.148
        beam('Ladder tread',(x,18.35,z),(x,19.05,z),.09,.09,'wood_light')
    coil((-.9,18.8,.045),.36)
    coil((.8,17.3,.045),.32)
    hoop('Blue and white life ring',(0,19.79,.42),.32,.1,.1,'paint_blue',n=20)
    for a in (0,math.pi/2,math.pi,math.pi*1.5):
        sector('Life ring white quadrant',.22,.33,a-.19,a+.19,.12,'paint_white',
               y=19.77,steps=2).transform((0,0,.42))
    tube('Life ring hanging rope',[(-.18,19.78,.65),(0,19.78,.77),(.18,19.78,.65)],.025,'rope',n=5)


def pier_clock():
    lathe('Dark clock plinth',[(0,.38),(.12,.38),(.18,.32)],'stone_dark',n=12)
    lathe('Carved stone base',[(.18,.32),(.31,.29),(.42,.19)],'stone',n=12)
    lathe('Clock post iron core',[(.42,.13),(3.6,.11)],'iron',n=10,bevel=False)
    for j in range(8):
        a = j*TAU/8
        rod('Cast iron flute',(.12*math.cos(a),.12*math.sin(a),.48),
            (.1*math.cos(a),.1*math.sin(a),3.55),.025,'iron_dark',n=5,bevel=False)
    for z in (.45,.62,2.9,3.55):
        cylinder('Gold clock collar',(0,0,z),.18,.08,'gold',n=10).bevel=False
    for side in (-1,1):
        start = len(PARTS)
        clock((0,0,0),.55,hands=False)
        for p in PARTS[start:]:
            if p.label == 'Gold hour index':
                p.role = 'iron_dark'
        beam('Minute hand twelve',(0,.18,0),(0,.18,.39),.045,.045,'iron_dark')
        # +Y-facing local dial: its viewer sees local -X as three o'clock.
        beam('Hour hand three',(0,.2,0),(-.27,.2,0),.065,.045,'iron_dark')
        cylinder('Gold hand pin',(0,.23,0),.055,.05,'gold',n=8,rot=(math.pi/2,0,0)).bevel=False
        move_group(start,(0,side*.06,3.62),(0,0,0 if side == 1 else math.pi))
    cylinder('Gold sun centre',(0,0,4.32),.14,.1,'gold',n=12,rot=(math.pi/2,0,0)).bevel=False
    for j in range(9):
        a = j*TAU/9
        beam('Sun crest ray',(.18*math.sin(a),0,4.32+.18*math.cos(a)),
             (.29*math.sin(a),0,4.32+.29*math.cos(a)),.045,.07,'gold')
    for s in (-1,1):
        scroll('Pier clock crest scroll',(s*.29,0,4.17),.17,s)


def boat():
    # Ring shell with an OPEN interior; no solid cap across the cockpit.
    outline = [(-.4,-1.6),(.4,-1.6),(.59,-1.1),(.65,-.35),(.58,.65),
               (.38,1.28),(0,1.6),(-.38,1.28),(-.58,.65),(-.65,-.35),(-.59,-1.1)]
    n = len(outline)
    rings = [(0,.48),(.16,.82),(.28,.95),(.4,1),(.48,1),(.48,.88),(.18,.65)]
    roles = ['paint_white','paint_white','paint_blue','paint_white','wood_dark','wood_light']
    for k in range(len(rings)-1):
        za,sa = rings[k]; zb,sb = rings[k+1]
        verts = [(x*s,y*s,z) for z,s in ((za,sa),(zb,sb)) for x,y in outline]
        faces = [(j,(j+1) % n,(j+1) % n+n,j+n) for j in range(n)]
        Part('Boat open hull strake',verts,faces,roles[k],False)
    slab('Interior boat floor',[(x*.65,y*.65) for x,y in outline],.18,.18,'wood_dark')
    for j in range(7):
        x = -.39+j*.13
        poly = clip([(px*.65,py*.65) for px,py in outline],0,x,True)
        poly = clip(poly,0,x+.115,False)
        slab('Inside longitudinal planking',poly,.205,.04,'wood_light' if j % 2 else 'wood')
    for y in (-.62,.58):
        box('Rowboat thwart seat',(0,y,.43),(1.04,.24,.09),'wood_light')
        for x in (-.44,.44):
            box('Seat knee',(x,y,.34),(.09,.2,.16),'wood_dark')
    for j in (-1,1):
        start = len(PARTS)
        rod('Oar shaft',(-1.05,j*.33,.56),(.9,j*.33,.56),.035,'wood',n=7,bevel=False)
        box('Oar red paddle',(-1.12,j*.33,.56),(.48,.19,.06),'paint_red')
        box('Oar pale grip',(.92,j*.33,.56),(.3,.075,.075),'wood_light')
        move_group(start,rot=(0,0,j*.28))
        hoop('Brass oarlock',(j*.62,j*.3,.5),.075,.025,.06,'brass',n=8)
    coil((0,1.1,.37),.19,2)
    tube('Short mooring end',[(0,1.1,.37),(.16,1.35,.45),(.2,1.66,.28),(.36,1.8,.12)],.025,'rope',n=5)


def buoy():
    for rings,role in (([(0,.14),(.08,.3),(.26,.35)],'paint_red'),
                       ([(.26,.35),(.41,.28)],'paint_white'),
                       ([(.41,.28),(.5,.19)],'paint_red')):
        lathe('Banded float',rings,role,n=12,bevel=False)
    cylinder('Dark bell deck',(0,0,.52),.23,.06,'iron_dark',n=10).bevel=False
    for j in range(4):
        a = j*TAU/4
        rod('Bell cage',(.17*math.cos(a),.17*math.sin(a),.54),
            (.1*math.cos(a),.1*math.sin(a),.81),.025,'iron',n=5,bevel=False)
    lathe('Brass buoy bell',[(.58,.1),(.62,.09),(.72,.045),(.76,.03)],'brass',n=10,bevel=False)
    lump('Bell clapper',(0,0,.58),(.06,.06,.07),'iron_dark')
    hoop('Buoy lifting ring',(0,0,.83),.07,.025,.04,'iron_dark',n=10).bevel=False


def picnic():
    # Shared wavy corner positions keep all checks stitched together.
    verts = {(i,j):(-1.2+i*.3+(.025*math.sin(j*1.7) if i in (0,8) else 0),
                    -.9+j*.3+(.025*math.sin(i*1.3) if j in (0,6) else 0),
                    .04+.012*math.sin(i*1.2+j*.9))
             for i in range(9) for j in range(7)}
    for i in range(8):
        for j in range(6):
            corners = [verts[i,j],verts[i+1,j],verts[i+1,j+1],verts[i,j+1]]
            p = [(x,y,z-.04) for x,y,z in corners]+corners
            Part('Wavy gingham cloth check',p,[(3,2,1,0),(4,5,6,7),(0,1,5,4),
                 (1,2,6,5),(2,3,7,6),(3,0,4,7)],'cloth_red' if (i+j) % 2 else 'cloth_white',False)
    box('Wicker picnic basket',(-.65,.4,.25),(.68,.45,.4),'wicker')
    box('Basket dark rim',(-.65,.4,.45),(.72,.49,.07),'wood_dark')
    box('Basket hinged lid',(-.65,.4,.51),(.7,.47,.07),'wicker')
    for j in range(6):
        x = -.95+j*.12
        for y in (.165,.635):
            box('Basket vertical weave',(x,y,.25),(.045,.04,.36),'wood_light',bevel=False)
        box('Basket lid weave',(x,.4,.555),(.04,.42,.04),'wood_light',bevel=False)
    for z in (.13,.24,.35):
        for y in (.15,.65):
            box('Basket horizontal weave',(-.65,y,z),(.66,.04,.045),'wood',bevel=False)
    tube('Basket arched handle',[(-.95,.4,.48),(-.93,.4,.79),(-.65,.4,.91),
                                (-.37,.4,.79),(-.35,.4,.48)],.04,'wood_dark',n=6)
    box('Basket brass latch',(-.65,.12,.43),(.09,.04,.13),'brass',bevel=False)
    for x,y in ((-.16,-.42),(.35,.25)):
        lathe('Ivory tea cup',[(0,.07),(.04,.075),(.2,.11),(.22,.115),
               (.22,.072),(.06,.042)],'ivory',n=12,bevel=False).transform((x,y,.06))
        hoop('Teacup handle',(x+.13,y,.2),.085,.04,.05,'ivory',n=10).bevel=False
        cylinder('Tea cup blue saucer',(x,y,.075),.17,.045,'paint_blue',n=12).bevel=False
    box('Small blue book',(.66,.47,.11),(.45,.53,.12),'paint_blue',rot=(0,0,-.18))
    box('Book cream page block',(.66,.45,.115),(.38,.5,.055),'ivory',rot=(0,0,-.18),bevel=False)
    cylinder('Picnic plate',(.15,-.12,.075),.27,.05,'ivory',n=16).bevel=False
    for dx,dy in ((-.1,-.035),(.08,.04)):
        cylinder('Lemon slice rind',(.15+dx,-.12+dy,.12),.105,.045,'flower_yellow',n=10).bevel=False
        for j in range(5):
            a = j*TAU/5
            rod('Lemon pith segment',(.15+dx,-.12+dy,.146),
                (.15+dx+.08*math.cos(a),-.12+dy+.08*math.sin(a),.146),
                .02,'ivory',n=4,bevel=False)
    start = len(PARTS)
    cylinder('Picnic straw hat brim',(0,0,.07),.36,.06,'hat_straw',n=20).bevel=False
    lathe('Picnic straw hat crown',[(.09,.2),(.25,.18),(.29,.12)],'hat_straw',n=16,bevel=False)
    cylinder('Picnic hat ribbon',(0,0,.14),.205,.055,'ribbon',n=16).bevel=False
    ribbon_solid('Hat ribbon ends',[(.2,0,.13),(.34,.16,.09),(.43,.25,.06)],.1,'ribbon')
    move_group(start,(.72,-.48,.05),rot=(0,.08,.25))


def willow():
    # Root flare and branched, slightly leaning trunk support an airy curtain.
    lathe('Willow root flare',[(0,.48),(.25,.36),(1.3,.28),(3.8,.19),(5.15,.08)],
          'bark',n=9,bevel=False)
    for j in range(7):
        a = j*TAU/7
        tube('Spreading willow root',[(0,0,.27),(.58*math.cos(a),.58*math.sin(a),.12),
             (1.05*math.cos(a),1.05*math.sin(a),.05)],.1,'wood_dark',n=5)
    for j in range(9):
        a = j*TAU/9
        x,y = 1.7*math.cos(a),1.5*math.sin(a)
        tube('Willow sweeping bough',[(0,0,2.9),(x*.4,y*.4,4.5),
             (x,y,5.7),(x*1.3,y*1.3,5.55)],.105,'bark',n=6)
    for j in range(11):
        a = j*TAU/11
        r = 1.5 if j else .2
        lump('Rounded willow crown',(r*math.cos(a),r*math.sin(a),5.7+random.uniform(-.1,.5)),
             (2.9,2.7,1.7),('leaf_dark','leaf','leaf_light')[j % 3],detail=2)
    for j in range(58):
        a = j*TAU/58+random.uniform(-.04,.04)
        r = random.uniform(1.8,2.75)
        x,y = r*math.cos(a),r*math.sin(a)
        bottom = random.uniform(1.45,2.3)
        top = random.uniform(5.25,6.25)
        points = [(x*.57,y*.57,top),(x*.9,y*.9,top-.45),
                  (x*1.05,y*1.05,4.2),(x*1.07+.09*math.sin(j),y*1.07,2.95),
                  (x*1.03,y*1.03,bottom)]
        ribbon_solid('Drooping willow curtain',points,random.uniform(.13,.23),
                     ('leaf','leaf_light','leaf_dark')[j % 3],thick=.055)
        # Chunky pointed leaves make the long curtains read at the game camera.
        for k in range(3):
            z = bottom+.35+k*.73
            petal('Willow curtain lance leaf',(x*1.05,y*1.05,z),.24,.12,-.1,
                  ('leaf_light','leaf_dark','leaf')[j % 3],a+(k % 2)*math.pi)


def reed_clump():
    reed_cluster(count=11)


def lily_base():
    # Explicit missing wedge, with two radial edges, rather than a full disk.
    angles = [.2+j*(TAU-.4)/23 for j in range(24)]
    rim = [(r*math.cos(a),r*math.sin(a)) for a in angles
           for r in (random.uniform(.582,.6),)]
    tones = ('lily_pad','leaf','lily_pad','leaf_light')
    for j,(a,b) in enumerate(zip(rim,rim[1:])):
        slab('Notched lily pad tonal sector',[(0,0),a,b],.045,.045,tones[j % 4])
    for j in range(7):
        a = .5+j*(TAU-1)/6
        rod('Dark lily vein',(0,0,.048),(.52*math.cos(a),.52*math.sin(a),.048),
            .021,'leaf_dark',n=4,bevel=False)
    # Three lighter rim patches also break the pad's broad green surface.
    for a in (1.2,2.9,4.5):
        petal('Lily pad rim lobe',(.22*math.cos(a),.22*math.sin(a),.05),
              .33,.28,0,'leaf',a-math.pi/2)


def lily_pad():
    lily_base()
    for j in range(5):
        a = j*TAU/5
        petal('Closed bud sepal',(0,0,.075),.15,.12,.13,'leaf',a)
    # Closed pointed bud, not an open rosette. Base at .06, tip at .29.
    lathe('Closed pink water lily bud',[(.065,.055),(.12,.1),(.21,.065),(.29,.005)],
          'lily_pink',n=8,bevel=False)
    for j in range(3):
        a = j*TAU/3
        rod('Bud pale petal fold',(.06*math.cos(a),.06*math.sin(a),.16),
            (.018*math.cos(a),.018*math.sin(a),.257),.02,'cloth_white',n=4,bevel=False)


def lily_flower():
    lily_base()
    for count,length,width,z,rise,role in ((9,.31,.18,.085,.09,'lily_pink'),
                                          (7,.22,.14,.12,.12,'cloth_white')):
        for j in range(count):
            a = j*TAU/count+(0 if count == 9 else .25)
            petal('Open pointed lily petal',(0,0,z),length,width,rise,role,a)
    cylinder('Golden lily heart',(0,0,.18),.095,.09,'lily_core',n=10).bevel=False
    for j in range(6):
        a = j*TAU/6
        lump('Lily golden stamen',(.06*math.cos(a),.06*math.sin(a),.24),
             (.05,.05,.06),'lily_core')


def skip_stone():
    # Five real mineral facets, all within the exact 16x12x4cm envelope.
    n = 8
    verts = [(random.uniform(-.003,.003),random.uniform(-.002,.002),.04)]
    verts += [(.08*math.cos(j*TAU/n),.06*math.sin(j*TAU/n),random.uniform(.022,.028)) for j in range(n)]
    verts += [(.075*math.cos(j*TAU/n),.055*math.sin(j*TAU/n),0) for j in range(n)]
    for j in range(n):
        role = ('stone','stone','pebble','stone','pebble_dark','stone','sand','pebble')[j]
        Part('Skipping stone mineral facet',verts,[(0,j+1,(j+1) % n+1)],role,False)
    Part('Skipping stone dark underside',verts,
         [(n+j+1,n+(j+1) % n+1,(j+1) % n+1,j+1) for j in range(n)]+
         [tuple(range(n+1,2*n+1))],'stone_dark',False)


def summer_head(z, woman=False, lean=0):
    start = len(PARTS)
    cylinder('Silhouette neck',(0,0,z-.15),.065,.16,'shadow',n=8)
    lump('Faceless summer head',(0,0,z),(.27,.25,.31),'shadow',detail=2)
    if woman:
        cylinder('Shadow wide hat brim',(0,0,z+.135),.34,.05,'shadow',n=20).bevel=False
        hoop('Straw hat brim edge',(0,0,z+.135),.35,.045,.05,'hat_straw',n=20,
             rot=(math.pi/2,0,0)).bevel=False
        lathe('Shadow summer hat crown',[(z+.15,.19),(z+.27,.155),(z+.3,.09)],
              'shadow',n=12,bevel=False)
        cylinder('Ribbon hat band',(0,0,z+.19),.192,.045,'ribbon',n=12).bevel=False
        ribbon_solid('Fluttering hat ribbon',[(.11,.15,z+.19),(.15,.36,z+.08),
                     (.08,.56,z+.04),(.17,.73,z+.11)],.11,'ribbon')
        lump('Hat brass ribbon pin',(.18,.05,z+.2),(.055,.055,.055),'brass')
    else:
        lump('Summer flat cap crown',(0,0,z+.14),(.35,.31,.14),'shadow',detail=2)
        box('Flat cap peak',(0,-.16,z+.105),(.3,.19,.05),'shadow',rot=(.07,0,0),bevel=False)
        box('Thin cap edge band',(0,-.25,z+.105),(.29,.04,.04),'shadow_rim',bevel=False)
    if lean:
        # Rotate about neck base; negative lean means towards world -X.
        pivot = Vector((0,0,z-.18))
        matrix = Matrix.Translation(pivot) @ Euler((0,lean,0)).to_matrix().to_4x4() @ Matrix.Translation(-pivot)
        for p in PARTS[start:]:
            p.verts = [matrix @ v for v in p.verts]


def waistcoat_details(z, front=-1):
    # The explicit all-shadow bodies override the general 55% colour rule.
    # Five roles are still present as visible warm edging and period accessories.
    cylinder('Thin waistcoat hem',(0,0,z),.205,.04,'shadow_rim',n=12).transform(scale=(1,.65,1))
    for dz in (.13,.26,.39):
        lump('Ivory waistcoat button',(0,front*.151,z+dz),(.045,.045,.045),'ivory')
    tube('Small watch fob chain',[(.04,front*.16,z+.29),(.1,front*.18,z+.17),
                                (.17,front*.15,z+.23)],.02,'brass',n=5)
    box('Dark watch fob',(.17,front*.15,z+.22),(.055,.04,.08),'iron_dark',bevel=False)


def silhouette_body(height, woman=False):
    """Summer memory adaptation of the platform silhouette: intact legs, no ghost tail."""
    if woman:
        lathe('Knee length summer dress',[(.49,.36),(.55,.35),(.94,.18),
              (1.13,.2),(1.29,.22)],'shadow',n=12,bevel=False).transform(scale=(1,.7,1))
        cylinder('Thin summer dress hem',(0,0,.51),.363,.045,'shadow_rim',n=12).transform(scale=(1,.7,1))
        for x in (-.105,.105):
            rod('Woman lower leg',(x,0,.51),(x,-.015,.09),.055,'shadow',n=7,bevel=False)
            lump('Woman summer shoe',(x,-.065,.065),(.14,.23,.12),'shadow')
        summer_head(1.48,True)
    else:
        for x in (-.11,.11):
            tube('Relaxed trouser leg',[(x,0,.86),(x*.98,.015,.47),(x*1.08,-.015,.1)],.085,'shadow',n=8)
            lump('Summer shoe',(x*1.08,-.075,.075),(.18,.29,.15),'shadow')
        lathe('Fitted summer waistcoat',[(.82,.2),(1.08,.17),(1.35,.23),(1.44,.2)],
              'shadow',n=12,bevel=False).transform(scale=(1,.66,1))
        summer_head(1.64)
        waistcoat_details(.86)
    return height/(1.8 if woman else 1.85)


def normalize_height(start, height):
    values = [v.z for p in PARTS[start:] for v in p.verts]
    lo,hi = min(values),max(values)
    for p in PARTS[start:]:
        for v in p.verts:
            v.z = (v.z-lo)*height/(hi-lo)


def shadow_young():
    start = len(PARTS)
    silhouette_body(1.85)
    for s in (-1,1):
        a,b,c = (s*.22,0,1.38),(s*.32,-.02,1.17),(s*.3,-.09,.93)
        rod('Rolled shirt upper sleeve',a,b,.085,'shadow',n=8,bevel=False)
        cylinder('Rolled sleeve cuff',(s*.32,-.02,1.17),.09,.07,'shadow',n=8)
        rod('Relaxed forearm',b,c,.058,'shadow',n=7,bevel=False)
        lump('Relaxed hand',c,(.11,.1,.15),'shadow')
    normalize_height(start,1.85)


def shadow_woman():
    start = len(PARTS)
    silhouette_body(1.7,True)
    rod('Woman resting arm',(.21,0,1.26),(.31,-.06,.89),.057,'shadow',n=7,bevel=False)
    # Anatomical right is -X for a woman facing south.
    tube('Stone skipping raised right arm',[(-.21,0,1.27),(-.35,-.08,1.1),
         (-.47,-.24,1.24)],.06,'shadow',n=7)
    lump('Stone holding hand',(-.47,-.24,1.24),(.12,.11,.12),'shadow')
    lump('Held skipping stone',(-.5,-.27,1.29),(.14,.1,.06),'stone')
    normalize_height(start,1.7)


def seated_body(woman=False):
    # Seat contact = (0,0,0), front +Y, knees over the north edge.
    lump('Seated hips',(0,-.035,.12),(.42,.35,.24),'shadow',detail=2)
    if woman:
        lathe('Seated summer dress bodice',[(.13,.23),(.35,.17),(.61,.22)],
              'shadow',n=12,bevel=False).transform(scale=(1,.7,1))
        # Draped skirt over both thighs, with an intact warm hem over the knees.
        extrude('Seated draped knee skirt',[(-.24,.19),(.24,.19),(.29,-.02),(-.29,-.02)],
                .56,'shadow',y=.23,bevel=False)
        beam('Seated dress warm hem',(-.29,.51,.035),(.29,.51,.035),.045,.045,'shadow_rim')
        for x in (-.11,.11):
            rod('Woman dangling shin',(x,.46,.03),(x,.49,-.52),.05,'shadow',n=7,bevel=False)
            lump('Woman dangling shoe',(x,.56,-.55),(.14,.23,.12),'shadow')
        summer_head(.83,True,lean=-.28)
    else:
        lathe('Seated waistcoat',[(.15,.21),(.4,.18),(.68,.23)],
              'shadow',n=12,bevel=False).transform(scale=(1,.68,1))
        waistcoat_details(.2,front=1)
        for x in (-.12,.12):
            rod('Seated thigh',(x,.015,.15),(x,.43,.09),.09,'shadow',n=8,bevel=False)
            rod('Dangling trouser shin',(x,.43,.09),(x,.47,-.52),.075,'shadow',n=8,bevel=False)
            lump('Dangling summer shoe',(x,.54,-.55),(.18,.27,.14),'shadow')
        summer_head(.88)
    for s in (-1,1):
        tube('Deck supporting arm',[(s*.21,0,.6),(s*.32,-.035,.3),
             (s*.34,-.08,.07)],.06 if woman else .07,'shadow',n=7)
        lump('Hand resting on deck',(s*.35,-.07,.045),(.13,.17,.09),'shadow')
    # The heads above were authored facing -Y. Turn only the head and hat north.
    # Limbs and torso are already positioned in their north-facing sitting pose.


def shadow_young_sit():
    start = len(PARTS)
    seated_body(False)
    for p in PARTS[start:]:
        if p.label in ('Summer flat cap crown','Flat cap peak','Thin cap edge band'):
            for v in p.verts:
                v.x, v.y = -v.x,-v.y


def shadow_woman_sit():
    start = len(PARTS)
    seated_body(True)
    # Her leftward head lean remains -X; ribbon trails back toward the camera.
    for p in PARTS[start:]:
        if p.label == 'Fluttering hat ribbon':
            for v in p.verts:
                v.y = -v.y


SPECS = (
    # name, optional dimensions, centered, builder, triangle budget
    ('Ground', None, False, ground, 45000),
    ('Jetty', None, False, jetty, 9000),
    ('PierClock', None, False, pier_clock, 4500),
    ('Boat', None, False, boat, 4500),
    ('Buoy', None, False, buoy, 1500),
    ('Picnic', None, False, picnic, 4000),
    ('Willow', None, False, willow, 6000),
    ('ReedClump', None, False, reed_clump, 1200),
    ('LilyPad', None, False, lily_pad, 700),
    ('LilyFlower', None, False, lily_flower, 900),
    ('SkipStone', (.16,.12,.04), False, skip_stone, 200),
    ('ShadowYoungL', None, False, shadow_young, 2500),
    ('ShadowWomanL', None, False, shadow_woman, 2800),
    ('ShadowYoungSit', None, False, shadow_young_sit, 2500),
    ('ShadowWomanSit', None, False, shadow_woman_sit, 2800),
)

# These three pieces have explicit attachment anchors in the piece specification.
# All other non-Ground objects use the inherited centre/lowest-point convention.
ATTACHMENT_ANCHORS = {'Jetty': (0,0,0), 'ShadowYoungSit': (0,0,0),
                      'ShadowWomanSit': (0,0,0)}

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
            mod.width, mod.segments = .025, 2
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


def finish(name, centered, budget):
    bpy.ops.object.select_all(action='DESELECT')
    obj = assembled_mesh(name)
    if triangles(obj) > budget:
        mod = obj.modifiers.new('Budget', 'DECIMATE')
        mod.ratio, mod.delimit = (budget * .96) / triangles(obj), {'MATERIAL'}
        bpy.ops.object.modifier_apply(modifier=mod.name)
    # Material boundaries can constrain collapse. Recheck the actual result.
    for attempt in range(3):
        if triangles(obj) <= budget:
            break
        mod = obj.modifiers.new('Budget remainder', 'DECIMATE')
        mod.ratio, mod.delimit = (budget * .94) / triangles(obj), {'MATERIAL'}
        bpy.ops.object.modifier_apply(modifier=mod.name)
    assert triangles(obj) <= budget, (name, triangles(obj), budget)
    lo, hi = bounds(obj)
    mid = (hi + lo) / 2
    # Ground alone retains world coordinates; props are XY-centred and grounded.
    keep_origin = name in ('Ground',)
    if not keep_origin:
        for v in obj.data.vertices:
            v.co.x -= mid.x
            v.co.y -= mid.y
            v.co.z -= mid.z if centered else lo.z
        if name in ATTACHMENT_ANCHORS:
            # Restore the requested shore-end/seat pivot after the shared pass.
            # This is local mesh geometry; FBX object transforms stay identity.
            shift = Vector((mid.x,mid.y,mid.z if centered else lo.z))
            shift -= Vector(ATTACHMENT_ANCHORS[name])
            for v in obj.data.vertices:
                v.co += shift
    obj.data.update()
    used = {obj.data.materials[f.material_index].name for f in obj.data.polygons}
    for index in reversed(range(len(obj.material_slots))):
        if obj.data.materials[index].name not in used:
            obj.data.materials.pop(index=index)
    assert len(used) >= 5, (name, 'Needs at least five visible role materials', used)
    lo, hi = bounds(obj)
    if name == 'Ground':
        assert any(abs(v.co.z) < 1e-6 for v in obj.data.vertices)
    elif name not in ATTACHMENT_ANCHORS:
        assert abs(lo.z) < 1e-5 and abs(lo.x+hi.x) < 1e-5 and abs(lo.y+hi.y) < 1e-5
    if name in ('LilyPad','LilyFlower'):
        assert hi.z-lo.z <= .3+1e-5
    if name == 'SkipStone':
        assert all(abs((hi-lo)[i]-d) < 1e-5 for i,d in enumerate((.16,.12,.04)))
    destination = OUTPUT / ('Kit_L_' + name + '.fbx')
    bpy.ops.export_scene.fbx(
        filepath=str(destination), use_selection=True,
        axis_forward='-Z', axis_up='Y', apply_scale_options='FBX_SCALE_UNITS',
        bake_space_transform=True, object_types={'MESH'}, use_mesh_modifiers=True,
        mesh_smooth_type='FACE', add_leaf_bones=False, bake_anim=False,
    )
    assert destination.is_file() and destination.stat().st_size > 1024, destination
    print(f'EXPORTED {name} {triangles(obj)} {tuple(round(d, 3) for d in obj.dimensions)}', flush=True)
    return obj


def preview(objects):
    scene = bpy.context.scene
    scene.render.engine = 'BLENDER_WORKBENCH'
    scene.view_settings.view_transform = 'Standard'
    scene.view_settings.look = 'None'
    scene.render.resolution_x, scene.render.resolution_y = 1800, 1000
    scene.render.resolution_percentage = 100
    scene.render.image_settings.file_format = 'PNG'
    scene.render.filepath = str(PREVIEW)
    PREVIEW.parent.mkdir(parents=True, exist_ok=True)
    shading = scene.display.shading
    shading.light, shading.color_type = 'STUDIO', 'MATERIAL'
    shading.show_cavity, shading.show_object_outline = True, True
    shading.object_outline_color = (0,0,0)
    shading.background_type = 'WORLD'
    shading.show_shadows = True
    shading.show_specular_highlight = False
    shading.cavity_type = 'BOTH'
    if scene.world is None:
        scene.world = bpy.data.worlds.new('Review world')
    scene.world.color = (.55, .75, .95)
    for role, mat in MATS.items():
        mat.diffuse_color = (*PALETTE[role], 1)
    # Ground retains its export coordinates but is excluded from the contact sheet.
    for obj in objects:
        obj.hide_render = obj.name == 'Ground'
    objects = [o for o in objects if o.name != 'Ground']
    row1 = [next(o for o in objects if o.name == name) for name in ('Jetty','Willow','PierClock')]
    row2 = [next(o for o in objects if o.name == name) for name in ('Boat','Picnic','Buoy','ReedClump')]
    row3 = [o for o in objects if o not in row1+row2]
    # Contact-sheet transforms happen AFTER every FBX export, and do not alter it.
    for obj in objects:
        if obj.name == 'Jetty':
            obj.rotation_euler.z = -math.pi/2
        factor = (8 if obj.name == 'SkipStone' else 1.8 if obj in row3 else
                  1.6 if obj in row2 else 1)
        obj.scale = (factor,)*3
    bpy.context.view_layer.update()
    def lay(row, y, gap):
        # Object.dimensions are local-axis extents; rotated props need world bounds.
        extents = []
        for obj in row:
            corners = [obj.matrix_world @ Vector(c) for c in obj.bound_box]
            xs,ys,zs = ([c[k] for c in corners] for k in range(3))
            extents.append((min(xs),max(xs),(min(ys)+max(ys))/2,min(zs)))
        total = sum(hi-lo for lo,hi,_,_ in extents) + gap * (len(row) - 1)
        x = -total / 2
        for obj,(lo,hi,cy,zlo) in zip(row,extents):
            obj.location = (x-lo,y-cy,-zlo)
            x += hi-lo+gap
        return total
    lay(row1, 8, 2)
    lay(row2, -1, 2.2)
    lay(row3, -7, 1.6)
    bpy.context.view_layer.update()
    labels = []
    for obj in objects:
        corners = [obj.matrix_world @ Vector(c) for c in obj.bound_box]
        x = (min(v.x for v in corners)+max(v.x for v in corners))/2
        y = min(v.y for v in corners)-.65
        font = bpy.data.curves.new(obj.name+'_Label','FONT')
        font.body, font.align_x, font.size = obj.name, 'CENTER', .32
        font.extrude = 0
        label = bpy.data.objects.new(obj.name+'_Label',font)
        scene.collection.objects.link(label)
        label.location, label.rotation_euler = (x,y,.02),(math.radians(35),0,0)
        font.materials.append(MATS['iron_dark'])
        labels.append(label)
    cam = bpy.data.objects.new('Cam', bpy.data.cameras.new('Cam'))
    scene.collection.objects.link(cam)
    scene.camera = cam
    cam.data.type = 'ORTHO'
    a = math.radians(55)
    target = Vector((0,0,0))
    cam.location = target + Vector((0,-60*math.cos(a),60*math.sin(a)))
    cam.rotation_euler = (target - cam.location).to_track_quat('-Z', 'Y').to_euler()
    # Fit transformed bounds in camera space, allowing for the tall post tops.
    bpy.context.view_layer.update()
    inv = cam.matrix_world.inverted()
    corners = [inv @ (obj.matrix_world @ Vector(c)) for obj in objects+labels for c in obj.bound_box]
    left,right = min(v.x for v in corners),max(v.x for v in corners)
    low,high = min(v.y for v in corners),max(v.y for v in corners)
    center = Vector(((left+right)/2,(low+high)/2,0))
    cam.location += cam.rotation_euler.to_matrix() @ center
    aspect = scene.render.resolution_x/scene.render.resolution_y
    cam.data.ortho_scale = max(right-left,(high-low)*aspect)*1.12
    bpy.ops.render.render(write_still=True)
    assert PREVIEW.is_file() and PREVIEW.stat().st_size > 1024
    print('PREVIEW ' + str(PREVIEW), flush=True)


def main():
    global PARTS
    bpy.ops.wm.read_factory_settings(use_empty=True)
    for mat in list(bpy.data.materials):
        bpy.data.materials.remove(mat)
    scene = bpy.context.scene
    scene.unit_settings.system, scene.unit_settings.scale_length = 'METRIC', 1.0
    materials()
    OUTPUT.mkdir(parents=True, exist_ok=True)
    out = []
    for i, (name, dims, centered, build, budget) in enumerate(SPECS):
        PARTS = []
        random.seed(51150 + i * 977)
        build()
        out.append(finish(name, centered, budget))
    preview(out)
    print('DONE', flush=True)


if __name__ == '__main__':
    main()
