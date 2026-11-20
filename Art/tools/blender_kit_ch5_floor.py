# 5장 붕괴 바닥 7종: 찢어진 기억 조각, 공허 구멍, 뒤틀린 능선과 거울 파편을 만듭니다.
# 실행: "C:/Program Files/Blender Foundation/Blender 5.3/blender.exe" -b --factory-startup -P Art/tools/blender_kit_ch5_floor.py
"""Seeded, metre-scale floor kit. No source geometry is borrowed from chapter 4.

patch_top UVs are assigned LAST, in final asset/world metres / 8. The runtime
material selects the remembered chapter; the FBX contains only grey role shaders.
The pit's .05 m is its lip height, excluding its explicitly requested floaters.
"""
import math
import random
from collections import Counter
from pathlib import Path

import bmesh
import bpy
from mathutils import Vector

ROOT = Path(__file__).resolve().parents[2]
OUTPUT = ROOT / 'Game/Assets/Art/Characters/Ch5'
PREVIEW = ROOT / 'Art/tools/specs/blender_kit_ch5_floor_preview.png'
BUILD_LOG = ROOT / 'Art/tools/specs/blender_kit_ch5_floor_build.log'
BUILD_EVENTS = []
MATS, PARTS = {}, []
PALETTE = {
    'patch_top': (.48, .40, .63), 'void_stone': (.30, .22, .43),
    'void_stone_dark': (.075, .054, .14), 'marble': (.68, .57, .82),
    'marble_dark': (.29, .18, .43), 'violet_dark': (.19, .09, .33),
    'indigo': (.055, .065, .18), 'night_sky': (.009, .013, .043),
    'starlight': (.88, .95, 1), 'crack_gold': (1, .64, .12),
    'mirror': (.64, .85, .96), 'mirror_back': (.14, .23, .36),
    'crystal': (.48, .30, .69),
}


def report(message):
    BUILD_EVENTS.append(message)
    print(message, flush=True)


def materials():
    for role in PALETTE:
        m = bpy.data.materials.new(role)
        m.use_nodes = True
        m.diffuse_color = (.5, .5, .5, 1)
        bsdf = m.node_tree.nodes['Principled BSDF']
        bsdf.inputs['Base Color'].default_value = (.5, .5, .5, 1)
        bsdf.inputs['Metallic'].default_value = 0
        bsdf.inputs['Roughness'].default_value = .8
        MATS[role] = m


def mesh(label, verts, faces, roles, bevel=False, smooth=False):
    data = bpy.data.meshes.new(label)
    data.from_pydata(verts, [], faces)
    data.update()
    obj = bpy.data.objects.new(label, data)
    bpy.context.collection.objects.link(obj)
    if isinstance(roles, str):
        roles = [roles] * len(faces)
    for role in dict.fromkeys(roles):
        data.materials.append(MATS[role])
    slots = {m.name: i for i, m in enumerate(data.materials)}
    for p, role in zip(data.polygons, roles):
        p.material_index = slots[role]
        p.use_smooth = smooth and p.normal.z > .5
    if bevel:
        mod = obj.modifiers.new('Broken stone edge chamfer', 'BEVEL')
        mod.width, mod.segments = .02, 2
        mod.limit_method, mod.angle_limit = 'ANGLE', math.radians(65)
        mod.use_clamp_overlap = True
    PARTS.append(obj)
    return obj


def ccw(poly):
    area = sum(a[0]*b[1]-b[0]*a[1] for a, b in zip(poly, poly[1:]+poly[:1]))
    return poly if area > 0 else list(reversed(poly))


def fit_xy(poly, sx, sy):
    lo = [min(p[j] for p in poly) for j in (0, 1)]
    hi = [max(p[j] for p in poly) for j in (0, 1)]
    return [((x-(lo[0]+hi[0])/2)*sx/(hi[0]-lo[0]),
             (y-(lo[1]+hi[1])/2)*sy/(hi[1]-lo[1])) for x, y in poly]


def torn_outline(n, sx, sy):
    """Unequal angular intervals, global lobes, deep bites AND a bent silhouette.

    Radial variation alone makes a noisy primitive. The subsequent shear, bend,
    displaced lobes and pinched waist deliberately remove rotational symmetry.
    """
    phase = random.uniform(-2, 2)
    poly = []
    for j in range(n):
        a = math.tau*(j+random.uniform(-.29, .29))/n
        r = (1 + .23*math.sin(a+phase) + .19*math.cos(3*a-.7)
             + random.uniform(-.18, .13))
        x, y = r*math.cos(a), r*math.sin(a)
        x, y = x + .23*y*y + .13*math.sin(2.1*y), y + .16*x*x-.11*x
        poly.append((x, y))
    return ccw(fit_xy(poly, sx, sy))


def inset(poly, distance):
    """Capped unequal mitres; preserves a torn contour without spike artefacts."""
    poly = ccw(poly)
    result = []
    for i, p in enumerate(poly):
        a = Vector(p)-Vector(poly[i-1])
        b = Vector(poly[(i+1) % len(poly)])-Vector(p)
        a.normalize(); b.normalize()
        na, nb = Vector((-a.y, a.x)), Vector((-b.y, b.x))
        v = na+nb
        if v.length < .001:
            v = na
        v.normalize()
        delta = v * min(distance/max(.30, v.dot(na)), distance*1.8)
        result.append(tuple(Vector(p)+delta))
    return result


def solid(label, poly, top, thickness, role, side='void_stone_dark', bevel=True):
    poly = ccw(poly); n = len(poly)
    z = top if callable(top) else lambda x, y: top
    verts = [(x, y, z(x,y)-thickness) for x,y in poly]
    verts += [(x, y, z(x,y)) for x,y in poly]
    faces = [tuple(reversed(range(n))), tuple(range(n, 2*n))]
    faces += [(i,(i+1)%n,(i+1)%n+n,i+n) for i in range(n)]
    return mesh(label, verts, faces, [side, role]+[side]*n, bevel)


def seam(label, points, width=.047, role='crack_gold', height=.009, closed=False):
    """A flat irregular vein, never a cylinder or an even rectangular strip.

    Both banks breathe independently. Segment lengths come from the caller's
    bent fracture path; random width changes make bulges and narrow throats.
    """
    points = [Vector(p) for p in points]
    n = len(points); verts = []
    for i,p in enumerate(points):
        prev = points[(i-1)%n] if closed or i else p
        nxt = points[(i+1)%n] if closed or i<n-1 else p
        tangent = nxt-prev
        bank = Vector((-tangent.y,tangent.x,0)).normalized()
        left, right = random.uniform(.43,.78), random.uniform(.43,.80)
        a, b = p-bank*width*left, p+bank*width*right
        verts.extend([tuple(a),tuple(b),tuple(b+Vector((0,0,height))),
                      tuple(a+Vector((0,0,height)))])
    faces = []
    steps = n if closed else n-1
    for i in range(steps):
        ni = (i+1)%n
        faces += [(i*4+j,i*4+(j+1)%4,ni*4+(j+1)%4,ni*4+j) for j in range(4)]
    if not closed:
        faces += [(3,2,1,0), tuple(range(4*(n-1),4*n))]
    return mesh(label, verts, faces, role)


def crack(points, top, width=.045):
    # A dark mortise border reads as an inset, with the gold below its bevel lip.
    seam('Recessed fracture bed', [(x,y,top(x,y)+.001) for x,y in points],
         width*1.85, 'void_stone_dark', .002)
    seam('Gold fracture inlay', [(x,y,top(x,y)+.003) for x,y in points],
         width, 'crack_gold', .002)


def star(x, y, z, size=.055):
    # A small asymmetrical, closed crystal flake, not a circle or regular star.
    poly = [(x-size*.60,y-size*.08),(x-size*.12,y-size*.45),
            (x+size*.51,y-size*.16),(x+size*.26,y+size*.37),
            (x-size*.17,y+size*.55)]
    solid('White star flake', poly, z+.006, .008, 'starlight', 'indigo', False)


def eroded_slab(label, outline, top, thickness=.035, top_role='patch_top', erosion=.72):
    """One ungridded old-floor polygon inside broad exposed fracture shoulders.

    The shoulders are part of the slab, not decoration pasted over an intact
    board. They keep the remembered material near half the visible footprint.
    """
    outline = ccw(outline); n = len(outline)
    cx = sum(x for x,y in outline)/n; cy = sum(y for x,y in outline)/n
    core = []
    for x,y in outline:
        shrink = erosion+random.uniform(-.045,.035)
        core.append((cx+(x-cx)*shrink,cy+(y-cy)*shrink))
    v = [(x,y,top(x,y)-thickness) for x,y in outline]
    v += [(x,y,top(x,y)-.012) for x,y in outline]
    v += [(x,y,top(x,y)) for x,y in core]
    f = [tuple(reversed(range(n))), tuple(range(2*n,3*n))]
    roles = ['void_stone_dark', top_role]
    for i in range(n):
        j = (i+1)%n
        f += [(i,j,j+n,i+n), (i+n,j+n,j+2*n,i+2*n)]
        roles += ['void_stone_dark', ('void_stone','marble_dark','mirror_back')[i%3]]
    mesh(label, v, f, roles, True)
    # The outer rim follows every bite, never a regular closed border primitive.
    seam('Torn perimeter gold', [(x,y,top(x,y)-.009) for x,y in outline],
         .046, height=.012, closed=True)
    return core


def mirror_chip(x, y, top, sx=.32, sy=.20):
    outline = [(a+x,b+y) for a,b in torn_outline(7,sx,sy)]
    solid('Loose mirror chip', outline, lambda a,b: top(a,b)+.020,
          .016, 'mirror', 'mirror_back')


def patch_a():
    outline = torn_outline(11,3.55,2.75)
    # Two degrees along the short axis fits a genuinely walkable thin patch.
    top = lambda x,y: .094+y*math.tan(math.radians(2.0))
    core = eroded_slab('One torn remembered floor',outline,top,.036,erosion=.71)
    a, b = core[1], core[7]
    crack([a,(a[0]*.52,.25),(.12,.09),(-.18,-.16),(b[0]*.57,b[1]*.60),b],top)
    crack([core[4],(-.45,.36),(-.18,-.16),(.30,-.25),core[9]],top,.042)
    mirror_chip(-.46,-.52,top,.34,.18)
    star(.40,.32,top(.40,.32)+.011,.065)


def patch_b():
    outer = torn_outline(14,2.57,2.17)
    centre = (.04,-.03)
    ends = (0,5,10)
    branches = {}
    for i in ends:
        x,y = outer[i]
        # Shared meandering fractures; irregular mitre offsets open real gaps.
        dx,dy = x-centre[0],y-centre[1]
        branches[i] = [centre,(centre[0]+.32*dx-.07*dy,centre[1]+.32*dy+.07*dx),
                       (centre[0]+.58*dx+.085*dy,centre[1]+.58*dy-.085*dx),
                       (centre[0]+.80*dx-.035*dy,centre[1]+.80*dy+.035*dx),(x,y)]
    for branch in branches.values():
        # Stop under the perimeter: a thick ribbon cap must not poke outside it.
        end=Vector(branch[-2]).lerp(Vector(branch[-1]),.62)
        clipped=branch[:-1]+[tuple(end)]
        seam('Void at bottom of separated fracture',[(x,y,.005) for x,y in clipped],
             .19,'night_sky',.003)
    for k,(start,end) in enumerate(((0,5),(5,10),(10,14))):
        a,b = start%14,end%14
        arc = [outer[j%14] for j in range(start,end+1)]
        poly = ccw(arc+list(reversed(branches[b][:-1]))+branches[a][1:-1])
        poly = inset(poly,.052)
        # Tilt around the narrow dimension of each fragment, without tall walls.
        directions = [(math.cos(t*math.pi/90),math.sin(t*math.pi/90)) for t in range(90)]
        direction = min(directions,key=lambda d:max(x*d[0]+y*d[1] for x,y in poly)
                        -min(x*d[0]+y*d[1] for x,y in poly))
        slope = math.tan(math.radians((5,7,3)[k]))
        lowest = min(x*direction[0]+y*direction[1] for x,y in poly)
        lift = .08 if k==2 else 0
        def top(x,y,d=direction,m=lowest,s=slope,l=lift):
            return .034+l+s*(x*d[0]+y*d[1]-m)
        core = eroded_slab('Independent tilted memory %d'%k,poly,top,.034,erosion=.69)
        c = (sum(x for x,y in core)/len(core),sum(y for x,y in core)/len(core))
        crack([core[1],(c[0]-.08,c[1]+.04),(c[0]+.10,c[1]-.07),core[len(core)//2]],top,.042)
        if k==1:
            mirror_chip(c[0]+.10,c[1],top,.20,.12)
    for i, t in ((0,1),(0,3),(5,2),(10,1),(10,3)):
        x,y = branches[i][t]
        star(x,y,.012,.045)


def skin_solid(label, rows, roles, bottom=.0, smooth=False, bevel=False):
    """Closed grid surface with a flat underside. Rows may be deeply warped."""
    nx,ny = len(rows),len(rows[0]); n = nx*ny
    verts = [p for row in rows for p in row]
    verts += [(p[0],p[1],bottom if not callable(bottom) else bottom(*p)) for p in verts[:n]]
    faces=[]; mats=[]
    for i in range(nx-1):
        for j in range(ny-1):
            a = i*ny+j
            faces += [(a,a+ny,a+ny+1,a+1),(a+n+1,a+n+ny+1,a+n+ny,a+n)]
            mats += [roles(i,j),'void_stone_dark']
    border = list(range(ny))+[i*ny+ny-1 for i in range(1,nx)]
    border += list(range((nx-1)*ny+ny-2,(nx-1)*ny-1,-1))
    border += [i*ny for i in range(nx-2,0,-1)]
    for a,b in zip(border,border[1:]+border[:1]):
        faces.append((a,b,b+n,a+n)); mats.append('void_stone_dark')
    obj = mesh(label,verts,faces,mats,bevel,smooth)
    return obj,[verts[i] for i in border]


def patch_c():
    rows=[]
    across=(-1,-.76,-.51,0,.48,.73,1)
    n=34
    for i in range(n):
        t=i/(n-1); x=-2.07+4.14*t
        centre=.053*x*x+.035*math.sin(4.1*t+.3)
        # Unequal lobes, a long deep waist, and differently scalloped banks.
        wl=.40+.17*math.sin(5.8*t+.3)+.09*math.cos(14*t)
        wr=.46+.18*math.sin(5.1*t+1.3)-.075*math.cos(17*t+.5)
        row=[]
        for s in across:
            xx=x+.10*s*math.sin(7.3*t+.9)+.055*s*s*math.cos(12*t)
            yy=centre+s*(wl if s<0 else wr)
            zz=.072+.049*math.sin(2.65*math.pi*t+.35)+.006*s*math.sin(5*t)
            row.append((xx,yy,zz))
        rows.append(row)
    obj,border=skin_solid('Bent waving old floor',rows,
        lambda i,j:'patch_top' if j in (2,3) else ('marble_dark' if j%2 else 'void_stone'),
        bottom=lambda x,y,z:z-.021,smooth=True)
    seam('Warped perimeter seam',[(x,y,z+.002) for x,y,z in border],
         .044, height=.009, closed=True)
    # Two fissures follow, rather than bridge over, the wave's actual height.
    for k in (9,23):
        path=[]
        for j in range(1,6):
            p=rows[k+(j%3)-1][j]
            path.append((p[0],p[1],p[2]+.003))
        seam('Wave-following gold fault',path,.044,height=.004)
    for k,j in ((6,2),(27,4)):
        x,y,z=rows[k][j]; star(x,y,z+.014,.055)


def void_pit():
    outer=torn_outline(17,2.38,1.98)
    inner=[]
    for i,(x,y) in enumerate(outer):
        a=math.atan2(y,x)
        r=.64+.11*math.sin(2*a+.6)+.055*math.sin(i*1.37+.5)
        inner.append((x*r+.055*math.sin(i)+.04,y*r-.045*math.cos(i*1.3)))
    solid('Continuous void well bottom',outer,.005,.005,'night_sky','indigo',False)
    for i in range(len(outer)):
        j=(i+1)%len(outer)
        a,b,c,d=outer[i],outer[j],inner[j],inner[i]
        # Each lip chunk has a different torn midpoint and asymmetric break.
        mid=((a[0]+b[0])/2+.025*math.sin(i*2.7),(a[1]+b[1])/2+.035*math.cos(i))
        poly=inset(ccw([a,mid,b,c,d]),.014+random.random()*.009)
        h=random.uniform(.033,.049)
        solid('Broken low hole lip',poly,h,h-.005,
              'void_stone' if i%3 else 'marble_dark')
        p=Vector(d); q=Vector(c); u=p.lerp(q,.23); v=p.lerp(q,.68)
        v.x+=.012*math.sin(i*2)
        seam('Jagged glowing inner hole rim',[(p.x,p.y,h),(u.x,u.y,h+.001),
             (v.x,v.y,h),(q.x,q.y,h)],.041,height=.006)
    nebula=[(-.73,-.19,.008),(-.50,-.04,.008),(-.29,-.10,.008),
            (-.07,.07,.008),(.13,.04,.008),(.35,.23,.008),(.61,.24,.008)]
    seam('Faint torn violet nebula',nebula,.23,'violet_dark',.002)
    seam('Nebula dark fold',[(x+.02,y+.015,.011) for x,y,z in nebula],.065,'indigo',.001)
    for x,y,size in ((-.52,.29,.06),(-.24,-.39,.047),(.10,.37,.065),
                     (.45,-.26,.045),(.62,.08,.06),(-.09,.10,.052),
                     (-.63,-.22,.045),(.28,-.08,.042)):
        star(x,y,.014,size)
    for x,y,z,s in ((-.30,.19,.17,.28),(.43,-.08,.235,.22)):
        poly=[(a+x,b+y) for a,b in torn_outline(7,s,s*.70)]
        solid('Detached lip floating above void',poly,z,.035,'void_stone','void_stone_dark')
        crack([poly[0],(x-.025,y+.02),poly[3]],lambda a,b,z=z:z,.041)


def slab_tilted():
    outline=torn_outline(12,2.17,1.77)
    ymin=min(y for x,y in outline)
    top=lambda x,y:.055+(y-ymin)*math.tan(math.radians(15.3))
    core=eroded_slab('Uplifted fractured violet marble',outline,top,.055,
                     top_role='marble',erosion=.72)
    centre=(-.10,-.05)
    spokes=[]
    for i in (0,2,3,5,7,9,11):
        x,y=core[i]; cx,cy=centre
        path=[centre,(cx+(x-cx)*.27+.045,cy+(y-cy)*.27-.035),
              (cx+(x-cx)*.61-.04,cy+(y-cy)*.61+.045),(x,y)]
        crack(path,top,.044); spokes.append(path)
    # Unequal polygonal web crosslinks: no circular rings or regular spokes.
    for i in (0,2,3,5):
        a=spokes[i][2]; b=spokes[(i+1)%len(spokes)][1]
        crack([a,((a[0]+b[0])*.5+.045,(a[1]+b[1])*.5-.06),b],top,.041)
    for x,y,sx,sy in ((-.28,.35,.34,.21),(.32,.10,.29,.17),(-.23,-.38,.22,.15)):
        mirror_chip(x,y,top,sx,sy)


def shard_spike():
    base=torn_outline(15,1.37,1.37)
    top=lambda x,y:.04
    core=eroded_slab('Cracked lopsided impact star',base,top,.04,
                     top_role='void_stone',erosion=.71)
    for i in (0,2,5,7,10,12):
        a=core[i]
        crack([a,(a[0]*.56+.055,a[1]*.5-.025),(.015,-.02)],top,.043)
    specs=[(-.24,.15,.36,.86,-.44),(.16,.08,.31,.69,.55),
           (.33,-.19,.28,.48,-.16),(-.24,-.26,.27,.43,.82),
           (-.04,.35,.25,.60,-.10)]
    for k,(x,y,w,h,yaw) in enumerate(specs):
        # Hand-torn 7-corner silhouettes with displaced tips and bitten sides.
        profile=[(-.46*w,.045),(.34*w,.052),(.58*w,h*.40),(.22*w,h*.71),
                 (.39*w,h),(-.20*w,h*.86),(-.56*w,h*.31)]
        verts=[]
        for depth in (-.029,.029):
            for u,z in profile:
                lean=.13*z*(1 if k%2 else -1)
                verts.append((x+u*math.cos(yaw)-depth*math.sin(yaw)+lean,
                              y+u*math.sin(yaw)+depth*math.cos(yaw)+.12*z,z))
        n=len(profile)
        faces=[tuple(reversed(range(n))),tuple(range(n,2*n))]
        faces += [(i,(i+1)%n,(i+1)%n+n,i+n) for i in range(n)]
        mesh('Jagged upright two-sided mirror',verts,faces,
             ['mirror_back','mirror']+['crystal' if i%3==0 else 'mirror_back' for i in range(n)],True)
        foot=[(a+x,b+y) for a,b in torn_outline(7,w*.85,w*.63)]
        solid('Faceted crystal root',foot,.093,.055,'crystal','mirror_back')
        seam('Split light in mirror',[(x-.05,y+.038,.13),
             (x+.01,y+.05,.24),(x-.035,y+.063,.35)],.040,height=.006)
    for x,y in ((-.46,-.05),(.41,.32),(.01,-.43)):
        star(x,y,.047,.055)


def warp_ridge():
    def centre(x): return .115*math.sin(2.15*x+.3)+.045*math.cos(4.4*x)
    def gap(x): return .050+.018*math.sin(5.1*x+1.4)+.009*math.cos(11*x)
    def crest(x): return .205+.030*math.sin(1.7*x+.2)+.009*math.cos(5*x)
    for side in (-1,1):
        # Opposite banks tear at different places, with unequal fragment lengths.
        cuts=((-1.47,-1.12,-.35,.02,.78,1.19,1.47) if side<0 else
              (-1.47,-.87,-.54,.24,.61,1.04,1.47))
        for k in range(len(cuts)-1):
            rows=[]
            for i in range(4):
                t=i/3
                x=cuts[k]*(1-t)+cuts[k+1]*t
                row=[]
                for j,s in enumerate((0,.36,1)):
                    # Staggered torn breaks make irregular slab ends too.
                    xx=x+.065*math.sin(j*2.3+k*1.7+side)*(1 if i==0 else -1 if i==3 else .3)
                    xx+=.010 if i==0 else -.010 if i==3 else 0
                    width=.34+.075*math.sin(3.8*xx+side)+.045*math.cos(8.3*xx+1)
                    yy=centre(xx)+side*(gap(xx)+s*width)
                    zz=crest(xx)*(1-s**1.35)+.027*s+.008*math.sin(6*xx+j)
                    row.append((xx,yy,zz))
                rows.append(row)
            obj,border=skin_solid('Buckled torn bank',rows,
                lambda i,j,k=k:'void_stone' if (j+k)%3 else 'marble_dark',
                bottom=0,smooth=True,bevel=True)
            p=[(row[0][0],row[0][1],row[0][2]+.002) for row in rows]
            seam('Broken molten crest seam',p,.043,height=.009)
    path=[(-1.47+2.94*i/27,0,0) for i in range(28)]
    path=[(x,centre(x),.094+.018*math.sin(x*3)) for x,y,z in path]
    seam('Deep narrow sky slit',path,.117,'night_sky',.005)
    for x in (-.81,.13,.88): star(x,centre(x),.12,.047)


SPECS=[
    ('FloorPatch_A',(3.6,2.8,.14),patch_a,3000),
    ('FloorPatch_B',(2.6,2.2,.18),patch_b,3000),
    ('FloorPatch_C',(4.2,1.6,.12),patch_c,3000),
    ('FloorVoidPit',(2.4,2.0,.25),void_pit,3000),
    ('FloorSlabTilted',(2.2,1.8,.60),slab_tilted,4000),
    ('FloorShardSpike',(1.4,1.4,.90),shard_spike,4000),
    ('FloorWarpRidge',(3.0,.9,.25),warp_ridge,3000),
]


def triangles(obj):
    obj.data.calc_loop_triangles()
    return len(obj.data.loop_triangles)


def bounds(obj):
    return [Vector([fn(v.co[i] for v in obj.data.vertices) for i in range(3)]) for fn in (min,max)]


def visible_materials(obj):
    """Actual occlusion-aware 55-degree sample, including the broad shoulders.

    Counting raw polygon area would count the concealed sky UNDER the pit lip.
    This checks the user's visible-surface requirement from the game camera.
    """
    toward=Vector((.12,.56,.82)).normalized()
    right=Vector((-toward.y,toward.x,0)).normalized()
    up=toward.cross(right).normalized()
    points=[Vector((v.co.dot(right),v.co.dot(up))) for v in obj.data.vertices]
    lo=[min(p[j] for p in points) for j in (0,1)]
    hi=[max(p[j] for p in points) for j in (0,1)]
    counts=Counter()
    for ix in range(88):
        for iy in range(72):
            x=lo[0]+(hi[0]-lo[0])*(ix+.5)/88
            y=lo[1]+(hi[1]-lo[1])*(iy+.5)/72
            hit,pos,normal,index=obj.ray_cast(right*x+up*y+toward*10,-toward)
            if hit:
                counts[obj.data.materials[obj.data.polygons[index].material_index].name]+=1
    fractions={role:round(n/sum(counts.values()),3) for role,n in counts.items()}
    assert max(fractions.values())<=.56,(obj.name,'visible role dominance',fractions)
    return fractions


def finish(name,dims,budget):
    bpy.ops.object.select_all(action='DESELECT')
    for obj in PARTS:
        obj.select_set(True); bpy.context.view_layer.objects.active=obj
        for mod in list(obj.modifiers): bpy.ops.object.modifier_apply(modifier=mod.name)
        obj.select_set(False)
    for obj in PARTS: obj.select_set(True)
    bpy.context.view_layer.objects.active=PARTS[0]
    bpy.ops.object.join()
    obj=bpy.context.object; obj.name=name; obj.data.name=name+'_Mesh'
    low,high=bounds(obj); span=high-low; mid=(high+low)/2
    for v in obj.data.vertices:
        v.co.x=(v.co.x-mid.x)*dims[0]/span.x
        v.co.y=(v.co.y-mid.y)*dims[1]/span.y
        # Pit heights are physical: its lip is <= .055 and floaters .1-.2 above.
        v.co.z=(v.co.z-low.z)*(1 if name=='FloorVoidPit' else dims[2]/span.z)
    bm=bmesh.new(); bm.from_mesh(obj.data)
    # Clamp-overlap bevels on the very thin pit lips can leave coincident
    # middle edges. Weld only floating-point duplicates, never actual gaps.
    bmesh.ops.remove_doubles(bm,verts=list(bm.verts),dist=1e-6)
    bmesh.ops.dissolve_degenerate(bm,edges=list(bm.edges),dist=1e-7)
    bmesh.ops.recalc_face_normals(bm,faces=list(bm.faces))
    bad=[e for e in bm.edges if not e.is_manifold]
    assert not bad,(name,'non-manifold edges',len(bad))
    bm.to_mesh(obj.data); bm.free(); obj.data.update()
    uv=obj.data.uv_layers.new(name='WorldMetresDiv8')
    for p in obj.data.polygons:
        for index in p.loop_indices:
            co=obj.data.vertices[obj.data.loops[index].vertex_index].co
            uv.data[index].uv=(co.x/8,co.y/8)
    bpy.context.view_layer.update()
    low,high=bounds(obj)
    assert abs(low.z)<1e-5 and abs(low.x+high.x)<1e-5 and abs(low.y+high.y)<1e-5
    assert obj.location.length<1e-7 and all(abs(s-1)<1e-7 for s in obj.scale)
    assert all(math.isfinite(c) for v in obj.data.vertices for c in v.co)
    assert all(abs(obj.dimensions[i]-dims[i])<1e-4 for i in (0,1))
    assert triangles(obj)<=budget,(name,triangles(obj),budget)
    degenerate=[(p.index,obj.data.materials[p.material_index].name,
                 [tuple(obj.data.vertices[i].co) for i in p.vertices])
                for p in obj.data.polygons if p.area<=1e-10]
    assert not degenerate,(name,'degenerate face',degenerate[:3])
    used={obj.data.materials[p.material_index].name for p in obj.data.polygons}
    assert len(used)>=4,(name,used)
    for p in obj.data.polygons:
        if obj.data.materials[p.material_index].name=='patch_top':
            for i in p.loop_indices:
                co=obj.data.vertices[obj.data.loops[i].vertex_index].co
                assert (uv.data[i].uv-Vector((co.x/8,co.y/8))).length<1e-6
    for m in obj.data.materials:
        assert m.name in PALETTE
        assert tuple(m.diffuse_color)==(.5,.5,.5,1)
        assert tuple(m.node_tree.nodes['Principled BSDF'].inputs['Base Color'].default_value)==(.5,.5,.5,1)
        assert len(m.node_tree.nodes)==2
    fractions=visible_materials(obj)
    obj['chapter']='The Collapsing End of Time'; obj['front']='+Y'
    obj['origin']='bottom centre'; obj['triangle_count']=triangles(obj)
    obj['patch_uv']='final asset/world metres divided by 8; no normalization'
    obj['collision']=name in {'FloorSlabTilted','FloorShardSpike'}
    if name=='FloorVoidPit': obj['lip_height_m']=.05; obj['floating_chunks']=2
    report(f'CHECKED {name} visible_roles {fractions}')
    dest=OUTPUT/f'Kit_{name}.fbx'
    bpy.ops.export_scene.fbx(filepath=str(dest),use_selection=True,
        axis_forward='-Z',axis_up='Y',apply_scale_options='FBX_SCALE_UNITS',
        bake_space_transform=True,object_types={'MESH'},use_mesh_modifiers=True,
        mesh_smooth_type='FACE',add_leaf_bones=False,bake_anim=False,use_custom_props=True)
    assert dest.is_file() and dest.stat().st_size>1024
    report(f'EXPORTED {name} {triangles(obj)} {tuple(round(d,4) for d in obj.dimensions)}')
    return obj


def preview(objects):
    scene=bpy.context.scene; scene.render.engine='BLENDER_WORKBENCH'
    scene.render.resolution_x,scene.render.resolution_y=1800,1000
    scene.render.resolution_percentage=100
    scene.render.image_settings.file_format='PNG'; scene.render.filepath=str(PREVIEW)
    scene.view_settings.view_transform='Standard'
    shade=scene.display.shading; shade.light='FLAT'; shade.color_type='MATERIAL'
    shade.show_shadows=False; shade.show_cavity=True; shade.cavity_type='BOTH'
    shade.curvature_ridge_factor=1.45; shade.curvature_valley_factor=1.15
    shade.show_specular_highlight=False; shade.show_object_outline=True
    shade.background_type='WORLD'
    scene.world=bpy.data.worlds.new('Preview background'); scene.world.color=(.020,.025,.046)
    helpers=[]; saved=[(o,o.matrix_world.copy()) for o in objects]
    try:
        for role,m in MATS.items(): m.diffuse_color=(*PALETTE[role],1)
        cam=bpy.data.objects.new('Preview only camera',bpy.data.cameras.new('Preview only camera'))
        scene.collection.objects.link(cam); helpers.append(cam); scene.camera=cam
        el=math.radians(55); az=math.radians(12)
        cam.location=Vector((math.sin(az)*math.cos(el),math.cos(az)*math.cos(el),math.sin(el)))*30
        cam.rotation_euler=(-cam.location).to_track_quat('-Z','Y').to_euler()
        cam.data.type='ORTHO'; cam.data.ortho_scale=19.2
        rotation=cam.rotation_euler.to_matrix(); inverse=rotation.transposed()

        def caption(body,x,y,size=.18,role='starlight'):
            data=bpy.data.curves.new('Preview caption','FONT'); data.body=body
            data.size=size; data.align_x='CENTER'; data.materials.append(MATS[role])
            label=bpy.data.objects.new('Preview caption',data); scene.collection.objects.link(label)
            label.location=rotation @ Vector((x,y,3)); label.rotation_euler=cam.rotation_euler
            helpers.append(label)

        # Equal review scales preserve the meaningful size comparison of pieces.
        layout=[(-6.65,2.30),(-2.25,2.30),(2.25,2.30),(6.65,2.30),
                (-5.8,-2.18),(0,-2.18),(5.8,-2.18)]
        notes=['ONE TORN MEMORY / TWO INLAID FAULTS','THREE TILTS / OPEN STARRED GAPS',
               'BENT PLAN / WAVING SURFACE','SHATTERED LIP / TWO HOVERING CHUNKS',
               '15 DEG UPLIFT / MARBLE WEB','FIVE TWO-SIDED MIRROR SHARDS','BUCKLED BANKS / OPEN VOID CREST']
        for i,(obj,(x,y)) in enumerate(zip(objects,layout)):
            points=[inverse @ v.co for v in obj.data.vertices]
            midx=(min(p.x for p in points)+max(p.x for p in points))/2
            midy=(min(p.y for p in points)+max(p.y for p in points))/2
            obj.location=rotation @ Vector((x-midx,y-midy,0))
            low=y+(min(p.y for p in points)-midy)
            caption(obj.name,x,low-.30,.21)
            caption(notes[i],x,low-.53,.12,'crystal')
            d=obj.dimensions
            caption(f'{d.x:.1f} x {d.y:.1f} x {d.z:.2f} m   /   {triangles(obj)} tris',x,low-.74,.115,'mirror')
        caption('CHAPTER 05   /   SHATTERED MEMORY FLOORS',0,4.78,.31,'crack_gold')
        caption('TORN + MELTED SILHOUETTES     |     55 DEG ABOVE +Y     |     TRUE RELATIVE SIZES',0,4.43,.14,'mirror')
        caption('PATCH_TOP = PREVIOUS CHAPTER GROUND AT RUNTIME     /     WORLD METRES UV / 8',0,-4.90,.15,'crystal')
        PREVIEW.parent.mkdir(parents=True,exist_ok=True)
        bpy.ops.render.render(write_still=True)
        assert PREVIEW.is_file() and PREVIEW.stat().st_size>1024
        report(f'PREVIEW {PREVIEW}')
    finally:
        scene.camera=None
        for obj in helpers: bpy.data.objects.remove(obj,do_unlink=True)
        for obj,matrix in saved: obj.matrix_world=matrix
        for m in MATS.values(): m.diffuse_color=(.5,.5,.5,1)


def main():
    global PARTS
    BUILD_EVENTS.clear()
    BUILD_LOG.unlink(missing_ok=True)
    bpy.ops.wm.read_factory_settings(use_empty=True)
    for m in list(bpy.data.materials): bpy.data.materials.remove(m)
    scene=bpy.context.scene; scene.unit_settings.system='METRIC'; scene.unit_settings.scale_length=1
    materials(); OUTPUT.mkdir(parents=True,exist_ok=True)
    objects=[]
    for i,(name,dims,build,budget) in enumerate(SPECS):
        PARTS=[]; random.seed(551701+i*7919)
        build(); objects.append(finish(name,dims,budget))
    assert len(scene.objects)==7 and all(o.type=='MESH' for o in scene.objects)
    preview(objects)
    assert len(scene.objects)==7 and all(o.type=='MESH' for o in scene.objects)
    assert all(tuple(m.diffuse_color)==(.5,.5,.5,1) for m in MATS.values())
    report('DONE')
    BUILD_LOG.write_text('\n'.join(BUILD_EVENTS)+'\n',encoding='utf-8')


if __name__=='__main__':
    main()
