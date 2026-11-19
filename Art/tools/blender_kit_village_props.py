# 새벽에 사라지는 서부 마을 소품 15종, FBX와 검수용 미리보기를 만듭니다.
# 실행: "C:/Program Files/Blender Foundation/Blender 5.3/blender.exe" -b --factory-startup -P Art/tools/blender_kit_village_props.py
"""Deterministic village kit. Metres, Z up, +Y front, bottom-centred origins.

Only Blender's bundled Python is needed. All export shaders remain flat grey;
the warm role palette is assigned to viewport colours only during the preview.
"""

import math
import random
from collections import defaultdict
from pathlib import Path

import bmesh
import bpy
from mathutils import Euler, Matrix, Vector
from mathutils.bvhtree import BVHTree


ROOT = Path(__file__).resolve().parents[2]
OUTPUT = ROOT / 'Game/Assets/Art/Characters/Village'
PREVIEW = ROOT / 'Art/tools/specs/blender_kit_village_props_preview.png'
ROLES = ('wood', 'wood_dark', 'wood_light', 'wood_weathered', 'plank',
         'roof_shingle', 'roof_tin', 'adobe', 'adobe_light', 'stone',
         'stone_dark', 'rock', 'rock_dark', 'earth', 'root', 'sand',
         'sand_glow', 'brass', 'gold', 'gold_drip', 'iron', 'rust', 'canvas',
         'cloth_red', 'cloth_cream', 'paint_rose', 'paint_sage', 'glass',
         'window_lit', 'lantern_glow', 'clock_face', 'cactus', 'cactus_dark',
         'stardust')
HARD = {'wood', 'wood_dark', 'wood_light', 'wood_weathered', 'plank',
        'roof_shingle', 'roof_tin', 'stone', 'stone_dark', 'rock', 'rock_dark',
        'brass', 'gold', 'iron', 'rust', 'paint_rose', 'paint_sage'}
PALETTE = {
    'wood': (.49, .26, .105), 'wood_dark': (.20, .105, .055),
    'wood_light': (.77, .53, .28), 'wood_weathered': (.55, .43, .31),
    'stone': (.66, .59, .47), 'stone_dark': (.34, .30, .24),
    'rock': (.51, .40, .29), 'rock_dark': (.27, .22, .17),
    'root': (.30, .16, .065), 'sand': (.85, .70, .40),
    'sand_glow': (1.0, .71, .20), 'brass': (.69, .43, .13),
    'gold': (.98, .67, .16), 'gold_drip': (1.0, .49, .055),
    'iron': (.18, .19, .165), 'rust': (.59, .22, .09),
    'canvas': (.87, .78, .56), 'cloth_red': (.68, .16, .10),
    'cloth_cream': (.98, .88, .67), 'paint_rose': (.74, .36, .29),
    'paint_sage': (.48, .54, .29), 'glass': (.70, .76, .60),
    'window_lit': (1.0, .79, .35), 'lantern_glow': (1.0, .87, .45),
    'clock_face': (.98, .93, .74), 'cactus': (.39, .53, .20),
    'cactus_dark': (.18, .30, .11), 'stardust': (1.0, .96, .82),
}
MATS = {}
PARTS = []
TAU = math.tau


def materials():
    for role in ROLES:
        mat = bpy.data.materials.new(role)
        mat.use_nodes = True
        mat.diffuse_color = (.5, .5, .5, 1)
        nodes = mat.node_tree.nodes
        nodes.clear()
        shader = nodes.new('ShaderNodeBsdfPrincipled')
        shader.inputs['Base Color'].default_value = (.5, .5, .5, 1)
        shader.inputs['Roughness'].default_value = .8
        shader.inputs['Metallic'].default_value = 0
        output = nodes.new('ShaderNodeOutputMaterial')
        mat.node_tree.links.new(shader.outputs['BSDF'], output.inputs['Surface'])
        MATS[role] = mat


class Part:
    """Geometry first: batch disconnected solids before applying bevels."""

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


def box(label, loc, size, role, rot=(0, 0, 0), bevel=True):
    x, y, z = (s/2 for s in size)
    return Part(label, [(-x,-y,-z), (x,-y,-z), (x,y,-z), (-x,y,-z),
                        (-x,-y,z), (x,-y,z), (x,y,z), (-x,y,z)],
                [(0,3,2,1), (4,5,6,7), (0,1,5,4), (1,2,6,5),
                 (2,3,7,6), (3,0,4,7)], role, bevel).transform(loc, rot)


def lathe(label, rings, role, n=12, bevel=True):
    verts = [(r*math.cos(j*TAU/n), r*math.sin(j*TAU/n), z)
             for z, r in rings for j in range(n)]
    faces = [tuple(reversed(range(n)))]
    faces += [(i*n+j, i*n+(j+1) % n, (i+1)*n+(j+1) % n, (i+1)*n+j)
              for i in range(len(rings)-1) for j in range(n)]
    faces.append(tuple(range((len(rings)-1)*n, len(rings)*n)))
    return Part(label, verts, faces, role, bevel)


def cylinder(label, loc, radius, height, role, n=12, top=None,
             rot=(0, 0, 0), bevel=True):
    return lathe(label, [(-height/2, radius),
                        (height/2, radius if top is None else top)],
                 role, n, bevel).transform(loc, rot)


def tube(label, points, radius, role, n=6, bevel=True):
    assert radius >= .0199, (label, 'detail below 0.04m diameter')
    verts = []
    for i, p in enumerate(points):
        tangent = (Vector(points[min(i+1, len(points)-1)]) -
                   Vector(points[max(0, i-1)])).normalized()
        reference = Vector((0, 1, 0)) if abs(tangent.y) < .9 else Vector((1, 0, 0))
        u = reference.cross(tangent).normalized()
        v = tangent.cross(u)
        verts += [Vector(p)+radius*(u*math.cos(j*TAU/n)+v*math.sin(j*TAU/n))
                  for j in range(n)]
    faces = [tuple(reversed(range(n)))]
    faces += [(i*n+j, i*n+(j+1) % n, (i+1)*n+(j+1) % n, (i+1)*n+j)
              for i in range(len(points)-1) for j in range(n)]
    faces.append(tuple(range((len(points)-1)*n, len(points)*n)))
    return Part(label, verts, faces, role, bevel)


def rod(label, a, b, radius, role, n=6, bevel=True):
    return tube(label, [a, b], radius, role, n, bevel)


def beam(label, a, b, width, depth, role):
    a, b = Vector(a), Vector(b)
    obj = box(label, (0,0,0), (width,depth,(b-a).length), role)
    matrix = Matrix.Translation((a+b)/2) @ (b-a).to_track_quat('Z','Y').to_matrix().to_4x4()
    obj.verts = [matrix @ v for v in obj.verts]
    return obj


def extrude(label, polygon, depth, role, y=0, bevel=True):
    """Extrude a simple XZ outline along Y, including concave arrow boards."""
    n = len(polygon)
    verts = [(x,y+d,z) for d in (-depth/2, depth/2) for x,z in polygon]
    faces = [tuple(reversed(range(n))), tuple(range(n,2*n))]
    faces += [(i,(i+1) % n,(i+1) % n+n,i+n) for i in range(n)]
    return Part(label, verts, faces, role, bevel)


def hoop(label, loc, radius, band, depth, role, n=16, rot=(0,0,0)):
    """Open-centred ring, default in XY; rotate pi/2 about X for a dial."""
    verts = [(r*math.cos(j*TAU/n), r*math.sin(j*TAU/n), z)
             for r,z in ((radius-band,-depth/2), (radius,-depth/2),
                         (radius,depth/2), (radius-band,depth/2))
             for j in range(n)]
    faces = [(k*n+j,k*n+(j+1) % n,((k+1) % 4)*n+(j+1) % n,
              ((k+1) % 4)*n+j) for k in range(4) for j in range(n)]
    return Part(label, verts, faces, role).transform(loc, rot)


def sector(label, loc, inner, outer, start, end, height, role, steps=3):
    outline = [(outer*math.cos(a),outer*math.sin(a))
               for a in [start+(end-start)*i/steps for i in range(steps+1)]]
    outline += [(inner*math.cos(a),inner*math.sin(a))
                for a in [end-(end-start)*i/steps for i in range(steps+1)]]
    return extrude(label, outline, height, role).transform(loc, (math.pi/2,0,0))


def lump(label, loc, size, role):
    bm = bmesh.new()
    bmesh.ops.create_icosphere(bm, subdivisions=1, radius=1)
    bm.verts.ensure_lookup_table()
    verts = [tuple(Vector(loc)+Vector(tuple(v.co[i]*size[i]/2 for i in range(3))))
             for v in bm.verts]
    obj = Part(label, verts, [tuple(v.index for v in f.verts) for f in bm.faces],
               role, bevel=role in HARD)
    bm.free()
    return obj


def mote(loc, size=.055, role='stardust'):
    x,y,z = loc
    verts = [(x+size,y,z),(x,y+size,z),(x-size,y,z),(x,y-size,z),
             (x,y,z+size*1.5),(x,y,z-size*1.5)]
    return Part('Floating diamond grain', verts,
                [(j,(j+1) % 4,k) for j in range(4) for k in (4,5)], role, False)


def scroll(loc, size=.4, role='brass', flip=1):
    x,y,z = loc
    pts = []
    for i in range(10):
        t = i/9
        a, r = -.9+t*TAU*.8, size*(1-.76*t)
        pts.append((x+flip*r*math.cos(a),y,z+r*math.sin(a)))
    return tube('Forged scroll', pts, .025, role, n=5)


def vanish(center, spread, rise, roles, size=.2, fabric=False, spiral=False):
    """Four shrinking, increasingly sparse layers plus discrete upward trails.

    All grains are mesh geometry. There are no emitters, curves or animation
    dependencies in the exported object. A solid silhouette survives below.
    """
    x,y,z = center
    for layer, count in enumerate((7,5,3,2)):
        t = layer/3
        for j in range(count):
            angle = layer*1.4+j*TAU/count+random.uniform(-.2,.2)
            radius = spread*(.7-.35*t)*random.uniform(.55,1)
            if spiral:
                angle = layer*1.7+j*.6
            pos = (x+radius*math.cos(angle),y+radius*math.sin(angle),
                   z+rise*(.12+.82*t)+random.uniform(-.03,.03)*rise)
            s = max(.045,size*(1-.7*t)*random.uniform(.8,1.15))
            role = roles[(j+layer) % len(roles)]
            box('Detached fabric scrap' if fabric else 'Detached top fragment',
                pos, (s*1.5,s if fabric else s*.8,max(.04,s*(.22 if fabric else .7))),
                role, (random.uniform(-.5,.5),random.uniform(-.5,.5),angle))
            if j < 3:
                # Tapered grain spacing makes the direction of travel readable.
                for k in range(2):
                    mote((pos[0]-.04*k,pos[1],pos[2]-.10-.10*k),
                         max(.022,.038*(1-.35*t)), 'sand_glow')
        mote((x+spread*.45*math.cos(t*5),y+spread*.3*math.sin(t*5),
              z+rise*(.22+.74*t)), max(.025,size*.22))


def clock(loc, radius, melting=False, pocket=False):
    """Facing +Y; sagging cream dial, brass bezel, block numerals, gold runs."""
    x,y,z = loc
    n = 24
    outline = []
    for j in range(n):
        a = j*TAU/n
        sag = max(0,-math.sin(a))**3*radius*.36 if melting else 0
        outline.append((x+radius*math.cos(a),z+radius*math.sin(a)-sag))
    extrude('Cream dial with drooping lower edge',outline,.065,'clock_face',y)
    rim = [(px,y+.055,pz) for px,pz in outline]
    tube('Brass dial bezel',rim+[rim[0]],max(.025,radius*.08),'brass',n=5)
    for j in range(12):
        a = j*TAU/12
        px, pz = x+radius*.78*math.sin(a), z+radius*.78*math.cos(a)
        box('Raised block hour numeral',(px,y+.07,pz),
            (max(.04,radius*.09),.045,max(.05,radius*.18)),
            'gold',rot=(0,a,0))
    rod('Frozen minute hand',(x,y+.11,z),
        (x-radius*.15,y+.11,z+radius*.62),.023,'iron',n=4)
    rod('Frozen hour hand',(x,y+.12,z),
        (x+radius*.46,y+.12,z+radius*.18),.026,'iron',n=4)
    cylinder('Clock hand pivot',(x,y+.14,z),max(.045,radius*.10),.065,
             'gold',n=8,rot=(math.pi/2,0,0))
    tube('Dial hairline fracture',[(x-radius*.48,y+.046,z+radius*.56),
         (x-radius*.23,y+.046,z+radius*.27),(x-radius*.31,y+.046,z+.02)],
         .02,'iron',n=4)
    if melting:
        for dx,length in ((-.60,.48),(-.13,.78),(.43,.39)):
            px = x+radius*dx
            top = z-radius*(1+.20*(1-abs(dx)))
            pts = [(px,y+.03,top+.10),(px+.035,y+.08,top-.14),
                   (px-.025,y+.14,top-radius*length)]
            tube('Molten gold dial run',pts,max(.027,radius*.065),'gold_drip',n=6)
            lump('Heavy drip end',pts[-1],(.10,.09,.15),'gold_drip')
    if pocket:
        hoop('Pocket watch winding bow',(x,y,z+radius*1.18),radius*.23,
             .045,.07,'brass',n=12,rot=(math.pi/2,0,0))
        cylinder('Pocket watch crown',(x,y,z+radius*1.03),.075,.13,'gold',n=8)


def lantern(loc, scale=1):
    x,y,z = loc
    start = len(PARTS)
    cylinder('Lantern foot',(0,0,0),.24,.10,'brass',n=8)
    # Separate, solid glass panes leave a front opening onto the flame.
    for side in (-1,1):
        box('Side glass pane',(side*.20,0,.30),(.04,.34,.40),'glass',bevel=False)
    box('Back glass pane',(0,-.19,.30),(.36,.04,.40),'glass',bevel=False)
    lump('Visible warm flame',(0,.19,.24),(.26,.26,.36),'lantern_glow')
    for dx,dy in ((-.23,-.23),(.23,-.23),(-.23,.23),(.23,.23)):
        rod('Lantern iron corner',(dx,dy,.03),(dx,dy,.55),.025,'iron',n=4)
    cylinder('Gold lantern roof',(0,0,.59),.33,.18,'gold',n=4,top=.12)
    cylinder('Roof vent',(0,0,.74),.09,.13,'iron',n=8)
    for p in PARTS[start:]:
        p.transform((x,y,z),scale=(scale,scale,scale))


def crate(loc, size=(.8,.7,.7), lid=True):
    x,y,z = loc
    w,d,h = size
    for j in range(4):
        px = x+(j-1.5)*w/4
        for side in (-1,1):
            box('Crate face plank',(px,y+side*(d/2-.04),z+h/2),
                (w/4-.018,.08,h-.08),('wood_light','wood','wood_weathered')[j % 3])
        if lid:
            box('Crate lid plank',(px,y,z+h-.04),(w/4-.018,d-.05,.08),
                'wood_light' if j % 2 else 'wood_weathered')
    for side in (-1,1):
        box('Crate end panel',(x+side*(w/2-.04),y,z+h/2),(.08,d-.08,h-.10),'wood')
        for zz in (z+.07,z+h-.07):
            box('Dark crate frame',(x,y+side*d/2,zz),(w,.09,.10),'wood_dark')
        for xx in (x-w/2+.06,x+w/2-.06):
            box('Dark crate corner',(xx,y+side*d/2,z+h/2),(.11,.10,h),'wood_dark')
            box('Iron corner strap',(xx,y+side*(d/2+.025),z+h-.12),
                (.14,.05,.18),'iron')
    beam('Diagonal crate brace',(x-w*.39,y+d/2+.06,z+.17),
         (x+w*.39,y+d/2+.06,z+h-.17),.10,.07,'wood_dark')


def barrel(loc, radius=.43, height=1.12, keg=False):
    x,y,z = loc
    n = 12
    # Each stave is a curved, closed solid with a visible longitudinal seam.
    for j in range(n):
        angles = [j*TAU/n+.012,(j+1)*TAU/n-.012]
        verts = [(x+r*math.cos(a),y+r*math.sin(a),z+zz)
                 for zz,r in ((0,radius*.82),(.18*height,radius*.94),
                              (.5*height,radius),(.82*height,radius*.94),
                              (height,radius*.82)) for a in angles]
        verts += [(x,y,z),(x,y,z+height)]
        faces = [(i*2,i*2+1,i*2+3,i*2+2) for i in range(4)]
        faces += [(0,10,11,8,6,4,2),(1,3,5,7,9,11,10),(0,1,10),(8,11,9)]
        role = ('brass','wood_dark','gold')[j % 3] if keg else (
            'wood','wood_dark','wood_light','wood_weathered')[j % 4]
        Part('Bulging individual barrel stave',verts,faces,role)
    for zz in (.15*height,.50*height,.85*height):
        r = radius*(1.01 if zz == .50*height else .95)
        hoop('Barrel iron hoop',(x,y,z+zz),r,.055,.085,'iron',n=12)
    cylinder('Barrel inset lid',(x,y,z+height-.035),radius*.78,.07,
             'brass' if keg else 'wood_weathered',n=12)
    cylinder('Barrel bung',(x+radius*.25,y,z+height+.018),.07,.045,'wood_dark',n=8)


def centerpiece():
    for z,r,h,role in ((.15,2,.30,'stone_dark'),(.39,1.71,.18,'stone'),
                        (.57,1.36,.18,'stone_dark'),(.72,1.05,.12,'brass')):
        cylinder('Octagonal plaza step',(0,0,z),r,h,role,n=8)
    # Eight alternating raised iron and brass flutes, chunky enough at game scale.
    cylinder('Iron pole core',(0,0,2.25),.28,3,'iron',n=16)
    for j in range(8):
        a = j*TAU/8
        cylinder('Stopped shaft flute',(.29*math.cos(a),.29*math.sin(a),2.23),
                 .065,2.73,'brass' if j % 2 else 'iron',n=6)
    for z,r in ((.91,.49),(1.14,.38),(3.47,.40),(3.67,.55)):
        cylinder('Turned brass shaft collar',(0,0,z),r,.13,'brass',n=12)
    box('Four sided clock housing',(0,0,4.39),(1.78,1.78,1.48),'iron')
    for z in (3.69,5.10):
        box('Clock head cornice',(0,0,z),(2.02,2.02,.13),'brass')
    for side in range(4):
        start = len(PARTS)
        clock((0,.92,4.43),.62,melting=True)
        for x in (-.79,.79):
            box('Head gold corner pilaster',(x,.94,4.42),(.075,.09,1.24),'gold')
        scroll((-.62,.98,3.87),.16,'gold',-1)
        for p in PARTS[start:]:
            p.transform(rot=(0,0,side*math.pi/2))
    # Four hipped roof panels leave an open crown beneath the lifted finial.
    for j in range(4):
        surface = [(-.99,-1.04,5.20),(.99,-1.04,5.20),
                   (.23,-.30,5.63),(-.23,-.30,5.63)]
        verts = surface+[(x,y,z-.08) for x,y,z in surface]
        Part('Broken hipped clock roof panel',verts,
             [(0,1,2,3),(7,6,5,4),(0,4,5,1),(1,5,6,2),
              (2,6,7,3),(3,7,4,0)],'brass').transform(rot=(0,0,j*math.pi/2))
    cylinder('Lifted finial lower bell',(.12,0,5.88),.24,.26,'gold',n=8,top=.07)
    vanish((0,0,5.80),1.02,1.15,('brass','iron','gold_drip'),.28,spiral=True)
    hoop('Detached clock gear',(.37,.02,6.14),.22,.07,.09,'brass',n=12,
         rot=(.4,.2,0))


def windpump():
    levels = ((.12,1.22),(2.0,.96),(3.8,.66),(5.5,.42))
    for sx in (-1,1):
        for sy in (-1,1):
            beam('Tapered lattice tower leg',(sx*1.22,sy*1.22,.12),
                 (sx*.42,sy*.42,5.5),.19,.20,'wood')
            box('Tower stone footing',(sx*1.22,sy*1.22,.11),(.44,.44,.22),'stone')
    for (z0,r0),(z1,r1) in zip(levels,levels[1:]):
        for side in (-1,1):
            for a,b in (((-r0,side*r0,z0),(r1,side*r1,z1)),
                        ((r0,side*r0,z0),(-r1,side*r1,z1))):
                beam('Lattice face diagonal',a,b,.105,.12,
                     'iron' if side == -1 else 'wood_light')
            beam('Lattice side diagonal',(side*r0,-r0,z0),(side*r1,r1,z1),
                 .11,.11,'wood_dark')
            beam('Tower cross tie',(-r0,side*r0,z0),(r0,side*r0,z0),.13,.15,'wood_weathered')
    cylinder('Pump lift rod',(0,0,2.86),.06,5.65,'iron',n=8)
    cylinder('Windmill yaw bearing',(0,0,5.63),.27,.32,'brass',n=10)
    center = Vector((0,.36,6.60))
    cylinder('Wheel spindle',tuple(center),.22,.80,'iron',n=12,rot=(math.pi/2,0,0))
    hoop('Windwheel open inner rim',tuple(center),.76,.07,.09,'iron',n=16,
         rot=(math.pi/2,0,0))
    for j in range(12):
        a = j*TAU/12+.15
        u = Vector((math.cos(a),0,math.sin(a)))
        v = Vector((-math.sin(a),0,math.cos(a)))
        tip = 1.65 if math.sin(a) < .75 else 1.16
        rod('Radial windwheel spoke',center+u*.20,center+u*min(tip,1.30),.035,'iron',n=4)
        polygon = [center+u*.77-v*.08,center+u*tip-v*.16,
                   center+u*tip+v*.32,center+u*.77+v*.08]
        extrude('Broad tapered fan blade',[(p.x,p.z) for p in polygon],.08,
                'rust' if j % 2 else 'iron',y=.43)
    cylinder('Brass rotor cap',(0,.86,6.60),.27,.15,'brass',n=12,rot=(math.pi/2,0,0))
    beam('Tail boom',(0,-.1,6.56),(0,-1.82,6.72),.10,.12,'iron')
    start = len(PARTS)
    extrude('Rose arrow tail vane',[(-.65,6.41),(.52,6.41),(.80,6.72),
            (.52,7.08),(-.65,7.08),(-.40,6.72)],.10,'paint_rose')
    for p in PARTS[start:]:
        p.transform((0,-1.58,0),rot=(0,0,1.10))
    box('Trough underside',(0,1.19,.16),(2.24,.79,.17),'wood_dark')
    for side in (-1,1):
        box('Trough raised side',(0,1.19+side*.37,.36),(2.24,.10,.45),'wood_dark')
        box('Trough end',(side*1.07,1.19,.36),(.10,.70,.45),'wood_weathered')
    box('Glasslike still sand water',(0,1.19,.40),(2.03,.61,.075),'sand_glow',bevel=False)
    for x in (-.65,.55):
        hoop('Sand water ripple',(x,1.19,.444),.17,.04,.04,'glass',n=12)
    vanish((0,.43,7.57),1.30,1.36,('rust','iron','paint_rose'),.27)


def water_tower():
    for sx in (-1,1):
        for sy in (-1,1):
            a,b = (sx*1.65,sy*1.65,.14),(sx*1.25,sy*1.25,4.64)
            beam('Water tank timber leg',a,b,.30,.30,'wood_dark')
            box('Stone leg shoe',(a[0],a[1],.14),(.60,.60,.28),'stone')
    for side in (-1,1):
        for zz in (.60,2.45):
            r0,r1 = 1.62-(zz-.14)*.09,1.62-(zz+1.75-.14)*.09
            for flip in (-1,1):
                beam('Tank front X brace',(flip*r0,side*r0,zz),
                     (-flip*r1,side*r1,zz+1.75),.16,.15,'wood_light')
                beam('Tank side iron X brace',(side*r0,flip*r0,zz),
                     (side*r1,-flip*r1,zz+1.75),.10,.11,'iron')
    for j in range(10):
        box('Overhanging platform board',((j-4.5)*.41,0,4.55),(.39,4.10,.18),
            ('wood_weathered','wood_light','wood')[j % 3])
    for j in range(20):
        a,b = j*TAU/20+.009,(j+1)*TAU/20-.009
        sector('Separate tank stave',(0,0,5.76),1.46,1.70,a,b,2.22,
               ('wood','wood_light','wood_dark','wood_weathered')[j % 4],steps=1)
    cylinder('Tank interior dark floor',(0,0,4.72),1.47,.12,'wood_dark',n=20)
    for z in (4.90,5.75,6.59):
        hoop('Tank iron retaining hoop',(0,0,z),1.75,.08,.13,'iron',n=20)
    # A segmented cone leaves real open gaps along its dissolving upper edge.
    for j in range(10):
        a,b = j*TAU/10+.025,(j+1)*TAU/10-.025
        verts = [(r*math.cos(t),r*math.sin(t),z)
                 for z,r in ((6.94,1.87),(7.47,.52),(7.39,.50),(6.86,1.83))
                 for t in (a,b)]
        Part('Lifting conical roof panel',verts,
             [(0,1,3,2),(2,3,5,4),(4,5,7,6),(6,7,1,0),(0,2,4,6),(1,7,5,3)],
             'wood_weathered' if j % 2 else 'wood')
    for x in (-.36,.36):
        beam('Front ladder stile',(x,1.92,.13),(x,1.92,6.78),.10,.12,'wood_dark')
    for j in range(19):
        rod('Ladder rung',(-.36,1.94,.35+j*.33),(.36,1.94,.35+j*.33),.04,'iron',n=6)
    tube('Upward leaking crack',[(.62,1.60,5.97),(.77,1.55,6.23),
         (.65,1.61,6.46),(.87,1.49,6.72)],.035,'wood_dark',n=4)
    for j in range(14):
        t = j/13
        mote((.80+.17*math.sin(t*6),1.62-.20*t,6.24+2.2*t),
             max(.025,.06*(1-t*.65)),'sand_glow')
    vanish((0,0,7.55),1.85,1.37,('wood_light','wood','iron'),.34,spiral=True)


def wagon():
    for j in range(9):
        box('Wagon bed plank',((j-4)*.43,0,1.03),(.41,1.72,.16),
            ('wood','wood_light','wood_weathered')[j % 3])
    for y in (-.88,.88):
        box('Wagon sideboard',(0,y,1.32),(3.90,.13,.45),'wood')
        box('Painted sideboard trim',(0,y,1.55),(3.96,.16,.08),'paint_rose')
        for x in (-1.65,-.55,.55,1.65):
            box('Wagon sideboard batten',(x,y*1.06,1.32),(.11,.09,.51),'wood_dark')
    for x in (-1.25,1.25):
        rod('Wagon iron axle',(x,-1.09,.65),(x,1.09,.65),.09,'iron',n=8)
        for y in (-1.06,1.06):
            hoop('Wheel iron rim',(x,y,.65),.65,.085,.14,'iron',n=16,rot=(math.pi/2,0,0))
            hoop('Wheel wooden felloe',(x,y,.65),.57,.12,.16,'wood_dark',n=16,rot=(math.pi/2,0,0))
            for j in range(8):
                a = j*TAU/8
                rod('Wheel wood spoke',(x,y,.65),(x+.49*math.cos(a),y,.65+.49*math.sin(a)),
                    .042,'wood_light',n=4)
            cylinder('Wheel brass hub',(x,y,.65),.15,.23,'brass',n=10,rot=(math.pi/2,0,0))
    # Solid cloth strips follow the barrel vault; upper seam has torn gaps.
    for ix in range(6):
        x0,x1 = -1.90+ix*.63,-1.90+(ix+1)*.63-.025
        for ia in range(8):
            if ia in (3,4) and ix in (1,4):
                continue
            a,b = .09+ia*(math.pi-.18)/8,.09+(ia+1)*(math.pi-.18)/8-.012
            verts = [(x,r*math.cos(t),1.61+r*math.sin(t))
                     for r in (1.02,1.08) for x,t in ((x0,a),(x1,a),(x1,b),(x0,b))]
            role = 'cloth_red' if (ix,ia) in ((1,2),(3,5),(4,1),(0,6)) else (
                'canvas' if (ix+ia) % 3 else 'cloth_cream')
            Part('Canvas patchwork vault',verts,
                 [(0,3,2,1),(4,5,6,7),(0,1,5,4),(1,2,6,5),(2,3,7,6),(3,0,4,7)],role,False)
    for x in (-1.91,-.65,.65,1.91):
        pts = [(x,1.10*math.cos(a),1.61+1.10*math.sin(a))
               for a in [.05+j*(math.pi-.1)/12 for j in range(13)]]
        tube('Canvas supporting wood bow',pts,.035,'wood_dark',n=5)
    box('Open rear tailboard',(-2.15,0,1.0),(.49,1.72,.14),'wood_dark')
    crate((-2.10,-.35,1.10),(.48,.57,.51))
    lantern((-2.12,.53,1.12),.57)
    beam('Wagon drawbar',(1.75,0,.90),(2.65,0,.54),.17,.17,'wood_dark')
    vanish((0,0,2.83),1.48,1.12,('canvas','cloth_red','cloth_cream'),.29,fabric=True)


def well():
    for row in range(3):
        for j in range(12):
            a = (j+(row % 2)*.5)*TAU/12+.016
            sector('Individual well masonry block',(0,0,.21+row*.31),.72,1.13,
                   a,a+TAU/12-.032,.29,'stone' if (j+row) % 3 else 'stone_dark',steps=2)
    for j in range(12):
        a = j*TAU/12+.018
        sector('Broad coping stone',(0,0,1.01),.70,1.22,a,a+TAU/12-.036,.18,
               'stone_dark' if j % 3 else 'stone',steps=2)
    cylinder('Still sand deep inside well',(0,0,.23),.69,.05,'sand_glow',n=16,bevel=False)
    for x in (-1.0,1.0):
        box('Crank roof timber upright',(x,0,1.87),(.19,.22,2.24),'wood')
        box('Brass post shoe',(x,0,1.10),(.23,.26,.18),'brass')
    box('Roof ridge watch beam',(0,0,2.84),(2.40,.28,.22),'wood_dark')
    for side in (-1,1):
        for j in range(4):
            y = side*(.15+j*.23)
            z = 2.83-abs(y)*.36
            box('Low pitched crank roof board',(0,y,z),(2.45,.24,.09),
                'wood_light' if j % 2 else 'wood_weathered',rot=(-side*.34,0,0))
    rod('Wood winding spindle',(-1.17,0,2.04),(1.22,0,2.04),.12,'wood_dark',n=10)
    rod('Iron crank arm',(1.22,0,2.04),(1.22,.30,1.84),.055,'iron',n=6)
    rod('Crank handle',(1.22,.30,1.84),(1.38,.30,1.84),.065,'wood',n=8)
    for j in range(5):
        hoop('Wound spindle rope',(-.22+j*.10,0,2.04),.16,.045,.07,'canvas',n=12,
             rot=(0,math.pi/2,0))
    rod('Bucket suspension rope',(0,.10,2.02),(0,.10,1.30),.027,'canvas',n=5)
    lathe('Hanging wooden bucket',[(.93,.22),(1.32,.29)],'wood_dark',n=10).transform((0,.10,0))
    for z in (1.00,1.26):
        hoop('Bucket iron hoop',(0,.10,z),.29,.045,.06,'iron',n=10)
    tube('Bucket bail',[(-.25,.10,1.26),(-.20,.10,1.48),(0,.10,1.53),(.20,.10,1.48),(.25,.10,1.26)],
         .025,'iron',n=5)
    clock((-.10,.38,2.94),.44,melting=True,pocket=True)
    vanish((-.10,0,3.39),.65,.57,('brass','wood_light','gold_drip'),.12,spiral=True)


def lamp():
    cylinder('Lamp octagonal plinth',(0,0,.09),.28,.18,'iron',n=8)
    cylinder('Brass plinth trim',(0,0,.22),.21,.10,'brass',n=10)
    lathe('Turned street lamp post',[(.26,.14),(.47,.10),(2.42,.065),(2.56,.12)],'iron',n=10)
    for z in (.48,1.90,2.47):
        cylinder('Lamp brass collar',(0,0,z),.12,.10,'brass',n=10)
    tube('Gooseneck bracket',[(0,0,2.48),(.08,0,2.68),(.31,0,2.70),(.40,0,2.55)],
         .035,'iron',n=6)
    scroll((.16,0,2.43),.17,'brass')
    lantern((.25,0,2.35),.78)
    vanish((.22,0,3.01),.34,.34,('gold','iron','glass'),.070)
    for p in ((-.12,.20,2.78),(.48,.08,2.95),(.03,-.18,3.10)):
        mote(p,.036)


def cactus():
    lathe('Saguaro central trunk',[(0,.28),(.30,.31),(2.28,.25),(2.43,.13)],
          'cactus',n=12,bevel=False)
    for j in range(10):
        a = j*TAU/10
        tube('Deep central cactus rib',[(.29*math.cos(a),.29*math.sin(a),.14),
             (.30*math.cos(a),.30*math.sin(a),1.28),(.245*math.cos(a),.245*math.sin(a),2.30)],
             .035,'cactus_dark',n=4,bevel=False)
    for side,tip in ((-1,1.82),(1,2.08)):
        pts = [(side*.17,0,1.00),(side*.47,0,1.0),(side*.62,0,1.20),
               (side*.62,0,tip-.12)]
        tube('Upright bent saguaro arm',pts,.17,'cactus',n=10,bevel=False)
        lump('Rounded arm crown',(side*.62,0,tip-.10),(.32,.32,.32),'cactus')
        for dy in (-.14,.14):
            tube('Arm vertical rib',[(side*.32,dy,1.04),(side*.58,dy,1.18),
                 (side*.62,dy,tip-.12)],.030,'cactus_dark',n=4,bevel=False)
    for loc,size,role in (((-.42,.28,.16),(.55,.56,.32),'rock'),
                           ((.42,-.13,.12),(.49,.43,.24),'rock_dark'),
                           ((.05,.40,.09),(.36,.31,.18),'sand')):
        lump('Cactus foot rock',loc,size,role)
    vanish((0,0,2.46),.58,.67,('cactus','cactus_dark','sand'),.15)


def fence():
    # Exact tile interval is [-2,2]. Terminal faces of BOTH rails remain at
    # x=-2 and x=2; finish() never recentres or rescales this piece in X.
    for x in (-1.80,0,1.80):
        h = 1.39 if x > 1 else 1.64
        box('Split fence post',(x,0,h/2),(.28,.30,h),'wood_weathered')
        for z in (.54,1.14):
            pts = [(x+dx,y,z+dz) for dx,y,dz in
                   ((-.16,-.17,-.04),(.16,-.17,.04),(.16,.17,.09),(-.16,.17,.01),(-.16,-.17,.05))]
            tube('Iron wire post wrap',pts,.022,'iron',n=4)
    for z in (.52,1.10):
        box('Continuous repeatable rail',(0,.025,z),(4,.18,.17),'wood_dark',bevel=False)
        box('Weathered rail face',(0,.124,z+.025),(4,.055,.065),'wood_light',bevel=False)
    # Split endgrain chips stay inside the 4m module footprint.
    vanish((1.74,0,1.43),.19,.52,('wood_weathered','wood_light','wood_dark'),.095)


def barrels():
    for x,y,z,r,h in ((-.47,.25,0,.44,1.12),(.48,.25,0,.44,1.03),
                       (-.03,-.45,0,.42,1.15)):
        barrel((x,y,z),r,h)
    barrel((.10,-.18,1.17),.27,.56,keg=True)
    cylinder('Keg detached brass lid',(.12,-.16,1.91),.235,.07,'brass',n=12,rot=(.12,.18,0))
    vanish((.1,-.18,1.79),.35,.38,('brass','wood_dark','gold'),.085)


def crates():
    crate((-.52,.08,0),(.97,1.12,.86))
    crate((.53,.08,0),(.97,1.12,.75))
    crate((-.06,.04,.88),(1.05,.99,.76),lid=False)
    for j in range(4):
        box('Floating crate lid board',(-.06+(j-1.5)*.255,.04,1.83+j*.025),
            (.24,.96,.075),'wood_light' if j % 2 else 'wood_dark',rot=(.10,.08,-.04))
    for j in range(7):
        a = j*TAU/7
        rod('Floating straw',(math.cos(a)*.30,math.sin(a)*.28,1.78),
            (math.cos(a)*.45,math.sin(a)*.40,1.96),.022,'sand',n=4,bevel=False)
    vanish((-.06,.02,1.84),.52,.32,('wood_light','wood_dark','sand'),.09)


def bench():
    for j in range(4):
        box('Bench seat slat',(0,-.22+j*.15,.49),(2.08,.13,.10),
            ('wood_light','wood','wood_weathered','wood')[j])
    for x in (-.78,.78):
        for side in (-1,1):
            # S-shaped forged side supports and wide feet.
            pts = [(x,side*.30,.05),(x,side*.20,.16),(x,side*.12,.29),(x,side*.22,.46)]
            tube('Iron scroll bench leg',pts,.045,'iron',n=6)
            box('Bench foot',(x,side*.28,.04),(.25,.23,.08),'iron')
        beam('Bench back upright',(x,-.22,.33),(x,-.33,.97),.07,.08,'iron')
        scroll((x,.16,.36),.12,'brass')
    for z in (.75,.93):
        box('Bench backrest slat',(0,-.31,z),(2.10,.10,.14),
            'wood_weathered' if z < .8 else 'wood_light')
    start = len(PARTS)
    clock((0,0,0),.14,pocket=True)
    for p in PARTS[start:]:
        p.transform((.42,.04,.56),rot=(math.pi/2,0,.25))
    vanish((.77,-.29,1.01),.22,.16,('wood_light','wood_weathered','brass'),.050)


def signpost():
    cylinder('Signpost shoe',(0,0,.10),.23,.20,'stone',n=8)
    box('Tall sign timber',(0,0,1.46),(.19,.22,2.87),'wood_dark')
    for j,(z,angle,role) in enumerate(((1.52,0,'wood_light'),(1.98,math.pi/2,'paint_rose'),
                                      (2.42,math.pi,'paint_sage'),(3.04,-.45,'wood'))):
        start = len(PARTS)
        # Clock-hand spear tips continue each arrow's outline.
        extrude('Directional western arrow',[(-.70,z-.13),(.34,z-.13),(.63,z),
                (.34,z+.13),(-.70,z+.13)],.11,role,y=.10)
        extrude('Clock hand arrow tip',[(.26,z-.075),(.48,z-.075),(.77,z),
                (.48,z+.075),(.26,z+.075),(.35,z)],.065,'iron',y=.18)
        box('Arrow gold edging',(-.17,.17,z+.11),(.96,.05,.04),'gold')
        for xx in (-.52,.08):
            cylinder('Arrow brass bolt',(xx,.195,z),.045,.055,'brass',n=6,rot=(math.pi/2,0,0))
        # Large symbolic clock marks read better than sub-pixel lettering.
        hoop('Board little clock emblem',(-.28,.19,z),.09,.025,.045,'brass',n=12,rot=(math.pi/2,0,0))
        for p in PARTS[start:]:
            p.transform((.06 if j == 3 else 0,0,.06 if j == 3 else 0),rot=(0,0,angle))
    vanish((.12,0,3.10),.59,.25,('wood','paint_sage','iron'),.10)


def hitch_post():
    for x in (-.98,.98):
        box('Hitch upright',(x,0,.49),(.22,.27,.98),'wood_weathered')
        cylinder('Hitch brass post cap',(x,0,1.01),.17,.10,'brass',n=8)
        box('Iron post shoe',(x,0,.12),(.25,.30,.23),'iron')
    box('Hitching top rail',(0,0,.83),(2.37,.22,.22),'wood')
    box('Worn rail upper face',(0,0,.945),(2.37,.23,.05),'wood_light')
    # Open horseshoe, not a closed decorative ring.
    pts = [(-.40+.16*math.cos(a),.18,.66+.18*math.sin(a))
           for a in [.15+i*(math.pi-.30)/10+math.pi for i in range(11)]]
    tube('Forgotten brass horseshoe',pts,.04,'brass',n=6)
    for dx in (-.55,-.25):
        cylinder('Horseshoe nail',(dx,.22,.66),.025,.05,'iron',n=6,rot=(math.pi/2,0,0))
    pts = []
    for j in range(65):
        a = j*TAU*3/64
        pts.append((.48+.24*math.cos(a),.10+.06*j/64,.66+.22*math.sin(a)))
    tube('Coiled hitch rope',pts,.027,'canvas',n=5,bevel=False)
    tube('Rope loose end',[(.72,.16,.66),(.83,.17,.47),(.76,.19,.27)],.027,'canvas',n=5,bevel=False)
    vanish((.96,0,1.09),.17,.17,('wood_weathered','brass','wood_light'),.050)


def tumbleweed():
    # Interlaced great-circle twigs form a sphere with real negative space.
    for j in range(13):
        a,b = j*TAU/13,j*1.79
        rotation = Euler((a,b,j*.37)).to_matrix()
        pts = []
        for k in range(19):
            t = k*TAU/18
            r = .41+random.uniform(-.025,.025)
            v = rotation @ Vector((r*math.cos(t),r*math.sin(t),0))
            pts.append(tuple(v+Vector((0,0,.53))))
        tube('Crossing tumbleweed strand',pts,.022,
             ('wood_weathered','root','wood_light')[j % 3],n=4)
        for k in (3,8,13):
            p = Vector(pts[k])
            q = p+Vector((.11*math.cos(a+k),.09*math.sin(a+k),.10))
            rod('Forked tumbleweed twig',p,q,.02,'root' if j % 2 else 'wood_light',n=4)
    for j in range(9):
        a = j*TAU/9
        mote((.39*math.cos(a),.39*math.sin(a),.11+j*.013),.025,'sand_glow')
    vanish((0,0,.94),.22,.09,('root','wood_weathered','wood_light'),.045)


def grass_tuft():
    for j in range(25):
        a = j*2.39996
        r = .035+.16*(j % 5)/4
        h = .22+.18*random.random()
        x,y = r*math.cos(a),r*math.sin(a)
        # Thick blade ribbon with a chunky pointed end.
        blade = extrude('Dry folded grass blade',[(0,0),(-.026,h*.45),
                        (.13,h),(.07,h*.48),(.04,0)],.04,
                        ('sand','cactus_dark','wood_light')[j % 3],bevel=False)
        blade.transform((x,y,0),rot=(0,0,a))
    # Exactly three tiny stardust sparkles; other rising grains are sand_glow.
    for p in ((-.11,.05,.44),(.14,-.03,.47),(.02,.15,.41)):
        mote(p,.022)
    for layer,count in enumerate((4,3,2,1)):
        box('Detached dry grass tip',(.10*math.cos(layer),.10*math.sin(layer),
            .36+layer*.038),(.055,.045,.04),
            ('sand','wood_light','cactus_dark')[layer % 3],
            rot=(.2,layer*.3,layer),bevel=False)
        for j in range(count):
            a = j*2.4+layer
            mote((.14*math.cos(a),.12*math.sin(a),.34+layer*.045),
                 .022,'sand_glow')


SPECS = [
    ('Centerpiece',(4,4,7),centerpiece),
    ('Windpump',(4,4,9),windpump),
    ('WaterTower',(5,5,9),water_tower),
    ('Wagon',(5,2.5,4),wagon),
    ('Well',(2.6,2.6,4),well),
    ('Lamp',(.8,.8,3.4),lamp),
    ('Cactus',(1.6,1.2,3.2),cactus),
    ('Fence',(4,.4,2),fence),
    ('Barrels',(2,1.6,2.2),barrels),
    ('Crates',(2.2,1.6,2.2),crates),
    ('Bench',(2.2,.8,1.2),bench),
    ('Signpost',(1.6,1.2,3.4),signpost),
    ('HitchPost',(2.4,.6,1.3),hitch_post),
    ('Tumbleweed',(1,1,1),tumbleweed),
    ('GrassTuft',(.8,.8,.5),grass_tuft),
]


def triangles(obj):
    obj.data.calc_loop_triangles()
    return len(obj.data.loop_triangles)


def bounds(obj):
    return [Vector(tuple(fn(v.co[i] for v in obj.data.vertices) for i in range(3)))
            for fn in (min,max)]


def material_coverage(obj):
    """Occlusion-aware projected surface shares at the 55-degree game view."""
    obj.data.calc_loop_triangles()
    verts = [v.co.copy() for v in obj.data.vertices]
    tris = list(obj.data.loop_triangles)
    tree = BVHTree.FromPolygons(verts,[tuple(t.vertices) for t in tris],all_triangles=True)
    angle = math.radians(55)
    view = Vector((0,math.cos(angle),math.sin(angle)))
    distance = max(obj.dimensions)*4
    areas = defaultdict(float)
    for i,tri in enumerate(tris):
        facing = tri.normal.dot(view)
        if facing <= 0:
            continue
        center = sum((verts[j] for j in tri.vertices),Vector())/3
        _,_,hit,_ = tree.ray_cast(center+view*distance,-view,distance+.01)
        if hit == i:
            areas[obj.data.materials[tri.material_index].name] += tri.area*facing
    total = sum(areas.values())
    assert total > 0, obj.name
    return {role:area/total for role,area in areas.items()}


def assembled_mesh(name):
    objects = []
    for bevel in (True,False):
        verts,faces,indices = [],[],[]
        for part in PARTS:
            if part.bevel != bevel:
                continue
            offset = len(verts)
            verts.extend(part.verts)
            faces.extend(tuple(offset+i for i in face) for face in part.faces)
            indices.extend([ROLES.index(part.role)]*len(part.faces))
        if not verts:
            continue
        data = bpy.data.meshes.new(name+'_parts')
        data.from_pydata(verts,[],faces)
        for role in ROLES:
            data.materials.append(MATS[role])
        for face,index in zip(data.polygons,indices):
            face.material_index = index
        # Normals are recalculated before bevel so concave outlines behave too.
        bm = bmesh.new()
        bm.from_mesh(data)
        bmesh.ops.recalc_face_normals(bm,faces=list(bm.faces))
        bm.to_mesh(data)
        bm.free()
        obj = bpy.data.objects.new(name+'_parts',data)
        bpy.context.collection.objects.link(obj)
        bpy.context.view_layer.objects.active = obj
        obj.select_set(True)
        if bevel:
            mod = obj.modifiers.new('Worn edges 0.025m two segments','BEVEL')
            mod.width,mod.segments = .025,2
            mod.limit_method,mod.angle_limit = 'ANGLE',math.radians(45)
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
    obj.name,obj.data.name = name,name+'_Mesh'
    return obj


def finish(name, dims):
    bpy.ops.object.select_all(action='DESELECT')
    obj = assembled_mesh(name)
    budget = 12000 if name in {'Centerpiece','Windpump','WaterTower'} else 6000
    if triangles(obj) > budget:
        # Collapse surplus bevel tessellation while keeping material boundaries.
        mod = obj.modifiers.new('Budget preserving material regions','DECIMATE')
        mod.ratio = (budget-180)/triangles(obj)
        mod.delimit = {'MATERIAL'}
        bpy.ops.object.modifier_apply(modifier=mod.name)
    assert triangles(obj) <= budget, (name,triangles(obj))
    lo,hi = bounds(obj)
    span,center = hi-lo,(hi+lo)/2
    for v in obj.data.vertices:
        # Fence rail terminals must remain x=+-2, even after beveling the posts.
        if name != 'Fence':
            v.co.x = (v.co.x-center.x)*dims[0]/span.x
        v.co.y = (v.co.y-center.y)*dims[1]/span.y
        v.co.z = (v.co.z-lo.z)*dims[2]/span.z
    bm = bmesh.new()
    bm.from_mesh(obj.data)
    bmesh.ops.recalc_face_normals(bm,faces=list(bm.faces))
    assert all(edge.is_manifold for edge in bm.edges), name+' has an open edge'
    bm.to_mesh(obj.data)
    bm.free()
    obj.data.update()
    bpy.context.view_layer.update()
    lo,hi = bounds(obj)
    assert all(abs(obj.dimensions[i]-dims[i]) < 1e-4 for i in range(3)), (name,obj.dimensions)
    assert abs(lo.z) < 1e-5 and abs(lo.x+hi.x) < 1e-5 and abs(lo.y+hi.y) < 1e-5, name
    assert obj.location.length < 1e-6, name+' misplaced origin'
    assert all(math.isfinite(c) for v in obj.data.vertices for c in v.co), name
    if name == 'Fence':
        for side in (-1,1):
            end = [v.co for v in obj.data.vertices if abs(v.co.x-side*2) < 1e-5]
            assert len(end) >= 8, 'Fence rail terminals lost'
        left = sorted((round(v.co.y,5),round(v.co.z,5)) for v in obj.data.vertices
                      if abs(v.co.x+2) < 1e-5)
        right = sorted((round(v.co.y,5),round(v.co.z,5)) for v in obj.data.vertices
                       if abs(v.co.x-2) < 1e-5)
        assert left == right, 'Fence endpoints do not tile'
    used = {obj.data.materials[f.material_index].name for f in obj.data.polygons}
    assert {'sand_glow','stardust'} <= used, (name,'missing vanishing grains')
    minimum = 6 if budget == 12000 else 5
    assert len(used) >= minimum, (name,used)
    coverage = material_coverage(obj)
    assert len(coverage) >= minimum, (name,'insufficient visible roles',coverage)
    print('VISIBLE '+name+' '+str({r:round(s,3) for r,s in
          sorted(coverage.items(),key=lambda item:-item[1])}),flush=True)
    assert max(coverage.values()) <= .555, (name,'dominant visible material',coverage)
    for mat in obj.data.materials:
        assert mat.name in ROLES
        assert tuple(mat.diffuse_color) == (.5,.5,.5,1)
        shader = next(n for n in mat.node_tree.nodes if n.type == 'BSDF_PRINCIPLED')
        assert tuple(shader.inputs['Base Color'].default_value) == (.5,.5,.5,1)
        assert len(mat.node_tree.nodes) == 2, 'Unexpected shader texture or effect'
    # Remove unused slots so each imported asset exposes just its own roles.
    for index in reversed(range(len(obj.material_slots))):
        if obj.data.materials[index].name not in used:
            obj.data.materials.pop(index=index)
    obj['front'],obj['origin_convention'] = '+Y','bottom center'
    obj['dimensions_m'],obj['triangle_count'] = list(dims),triangles(obj)
    obj['seed'],obj['vanish_layers'] = 71039+SPECS.index(next(s for s in SPECS if s[0] == name))*1009,4
    destination = OUTPUT / ('Kit_V_'+name+'.fbx')
    bpy.ops.export_scene.fbx(
        filepath=str(destination),use_selection=True,
        axis_forward='-Z',axis_up='Y',apply_scale_options='FBX_SCALE_UNITS',
        bake_space_transform=True,object_types={'MESH'},use_mesh_modifiers=True,
        mesh_smooth_type='FACE',add_leaf_bones=False,bake_anim=False,
    )
    assert destination.is_file() and destination.stat().st_size > 1024, destination
    print(f'EXPORTED {name} {triangles(obj)} {tuple(round(d,4) for d in obj.dimensions)}',flush=True)
    return obj


def preview(objects):
    """Only runs AFTER export. Restore viewport colours and locations afterward."""
    scene = bpy.context.scene
    scene.render.engine = 'BLENDER_WORKBENCH'
    scene.render.resolution_x,scene.render.resolution_y = 1600,900
    scene.render.resolution_percentage = 100
    scene.render.image_settings.file_format = 'PNG'
    scene.render.filepath = str(PREVIEW)
    scene.render.film_transparent = False
    scene.view_settings.view_transform = 'Standard'
    shading = scene.display.shading
    shading.light,shading.color_type = 'FLAT','MATERIAL'
    shading.show_shadows,shading.show_cavity = False,True
    shading.cavity_type = 'BOTH'
    shading.curvature_ridge_factor,shading.curvature_valley_factor = 1.5,1.1
    shading.show_specular_highlight = False
    shading.show_object_outline = True
    shading.background_type = 'WORLD'
    if scene.world is None:
        scene.world = bpy.data.worlds.new('Review world')
    scene.world.color = (.055,.045,.035)
    old_locations = [o.location.copy() for o in objects]
    helpers = []
    try:
        for role,mat in MATS.items():
            mat.diffuse_color = (*PALETTE.get(role,(.52,.46,.35)),1)
        gap = .60
        total = sum(dims[0] for _,dims,_ in SPECS)+gap*(len(objects)-1)
        cursor = -total/2
        for obj,(_,dims,_) in zip(objects,SPECS):
            obj.location.x = cursor+dims[0]/2
            cursor += dims[0]+gap
        target = Vector((0,0,2.5))
        a = math.radians(55)
        camera = bpy.data.objects.new('Review camera',bpy.data.cameras.new('Review camera'))
        scene.collection.objects.link(camera)
        helpers.append(camera)
        scene.camera = camera
        camera.data.type,camera.data.ortho_scale = 'ORTHO',total+1.5
        camera.location = target+Vector((0,40*math.cos(a),40*math.sin(a)))
        camera.rotation_euler = (target-camera.location).to_track_quat('-Z','Y').to_euler()
        for obj in objects:
            data = bpy.data.curves.new('Review caption','FONT')
            data.body,data.align_x,data.size = obj.name,'CENTER',.24
            data.materials.append(MATS['clock_face'])
            caption = bpy.data.objects.new('Caption '+obj.name,data)
            scene.collection.objects.link(caption)
            caption.location = (obj.location.x,2.8,-.20)
            caption.rotation_euler = camera.rotation_euler
            helpers.append(caption)
        PREVIEW.parent.mkdir(parents=True,exist_ok=True)
        bpy.ops.render.render(write_still=True)
        assert PREVIEW.is_file() and PREVIEW.stat().st_size > 1024
        print('PREVIEW '+str(PREVIEW),flush=True)
    finally:
        scene.camera = None
        for helper in helpers:
            bpy.data.objects.remove(helper,do_unlink=True)
        for obj,loc in zip(objects,old_locations):
            obj.location = loc
        for mat in MATS.values():
            mat.diffuse_color = (.5,.5,.5,1)


def main():
    global PARTS
    bpy.ops.wm.read_factory_settings(use_empty=True)
    for mat in list(bpy.data.materials):
        bpy.data.materials.remove(mat)
    scene = bpy.context.scene
    scene.unit_settings.system,scene.unit_settings.scale_length = 'METRIC',1.0
    materials()
    OUTPUT.mkdir(parents=True,exist_ok=True)
    exported = []
    for i,(name,dims,build) in enumerate(SPECS):
        PARTS = []
        random.seed(71039+i*1009)
        build()
        exported.append(finish(name,dims))
    assert len(scene.objects) == len(SPECS) == 15
    assert all(obj.type == 'MESH' for obj in scene.objects)
    preview(exported)
    assert len(scene.objects) == 15 and all(obj.type == 'MESH' for obj in scene.objects)
    print('DONE',flush=True)


if __name__ == '__main__':
    main()
