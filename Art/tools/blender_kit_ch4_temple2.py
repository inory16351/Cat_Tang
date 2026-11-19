# 시간의 성전 가구 6종: 독립 기둥, 천사상, 등불, 장의자, 모래시계와 바닥 시계.
# 실행: "C:/Program Files/Blender Foundation/Blender 5.3/blender.exe" -b --factory-startup -P Art/tools/blender_kit_ch4_temple2.py
"""Standalone Blender 5.x temple furniture. Metres; Z up; +Y front.

Shares the first temple kit's plinths, tapered marble, stepped silver capitals,
gear rosettes and swallow-tail textiles. No imports from other project scripts.
Export grey role materials first; review colours are a temporary render pass.
The floor mosaic is a constrained planar subdivision, not stacked relief.
"""
import math
import random
from pathlib import Path

import bmesh
import bpy
from mathutils import Euler, Matrix, Vector
from mathutils.geometry import delaunay_2d_cdt

ROOT = Path(__file__).resolve().parents[2]
OUTPUT = ROOT / 'Game/Assets/Art/Characters/Ch4'
PREVIEW = ROOT / 'Art/tools/specs/blender_kit_ch4_temple2_preview.png'
TAU = math.tau
FACE = (math.pi / 2, 0, 0)
MATS, PARTS = {}, []
PALETTE = {
    'stone': (.49, .49, .48), 'stone_dark': (.135, .14, .155),
    'porcelain': (.91, .90, .85), 'silver': (.65, .69, .73),
    'iron_dark': (.035, .038, .045), 'dial_face': (.74, .76, .73),
    'dial_mark': (.055, .057, .065), 'starlight': (.79, .86, .95),
    'ribbon_red': (.55, .018, .038), 'red_lacquer': (.30, .012, .022),
    'gem_red': (.69, .025, .055), 'lamp_warm': (1, .69, .29),
    'wood_dark': (.085, .075, .072), 'wood': (.27, .25, .23),
    'velvet': (.16, .145, .16), 'glass': (.43, .53, .61),
}


def materials():
    for role in PALETTE:
        mat = bpy.data.materials.new(role)
        mat.use_nodes = True
        mat.diffuse_color = (.5, .5, .5, 1)
        shader = mat.node_tree.nodes['Principled BSDF']
        shader.inputs['Base Color'].default_value = (.5, .5, .5, 1)
        shader.inputs['Roughness'].default_value = .78
        MATS[role] = mat


def mesh(label, vertices, faces, role, bevel=False):
    data = bpy.data.meshes.new(label)
    data.from_pydata(vertices, [], faces)
    data.update()
    obj = bpy.data.objects.new(label, data)
    bpy.context.collection.objects.link(obj)
    data.materials.append(MATS[role])
    if bevel:
        mod = obj.modifiers.new('Carved softened edges', 'BEVEL')
        mod.width, mod.segments = .025, 2
        mod.limit_method, mod.angle_limit = 'ANGLE', math.radians(60)
        mod.use_clamp_overlap = True
    PARTS.append(obj)
    return obj


def place(obj, loc=(0, 0, 0), rot=(0, 0, 0)):
    obj.data.transform(Matrix.Translation(Vector(loc)) @ Euler(rot).to_matrix().to_4x4())
    return obj


def box(label, loc, size, role, bevel=False, rot=(0, 0, 0)):
    x, y, z = (s / 2 for s in size)
    v = [(-x,-y,-z),(x,-y,-z),(x,y,-z),(-x,y,-z),
         (-x,-y,z),(x,-y,z),(x,y,z),(-x,y,z)]
    f = [(0,3,2,1),(4,5,6,7),(0,1,5,4),(1,2,6,5),(2,3,7,6),(3,0,4,7)]
    return place(mesh(label, v, f, role, bevel), loc, rot)


def extrude(label, profile, back, front, role, bevel=False):
    n = len(profile)
    v = [(x,y,z) for y in (back,front) for x,z in profile]
    f = [tuple(reversed(range(n))), tuple(range(n,2*n))]
    f += [(j,(j+1)%n,(j+1)%n+n,j+n) for j in range(n)]
    return mesh(label,v,f,role,bevel)


def cylinder(label, loc, radius, depth, role, n=16, rot=(0,0,0), bevel=False):
    v = [(radius*math.cos(j*TAU/n),radius*math.sin(j*TAU/n),z)
         for z in (-depth/2,depth/2) for j in range(n)]
    f = [tuple(reversed(range(n))),tuple(range(n,2*n))]
    f += [(j,(j+1)%n,(j+1)%n+n,j+n) for j in range(n)]
    return place(mesh(label,v,f,role,bevel),loc,rot)


def lathe(label, profile, role, loc=(0,0,0), n=16, closed=False):
    v = [(r*math.cos(j*TAU/n),r*math.sin(j*TAU/n),z)
         for r,z in profile for j in range(n)]
    f = [] if closed else [tuple(reversed(range(n))),tuple(range((len(profile)-1)*n,len(profile)*n))]
    f += [(k*n+j,k*n+(j+1)%n,((k+1)%len(profile))*n+(j+1)%n,((k+1)%len(profile))*n+j)
          for k in range(len(profile) if closed else len(profile)-1) for j in range(n)]
    return place(mesh(label,v,f,role),loc)


def ring(label, loc, outer, inner, depth, role, n=32, rot=(0,0,0)):
    v = [(r*math.cos(j*TAU/n),y,r*math.sin(j*TAU/n))
         for y,r in ((-depth/2,outer),(depth/2,outer),(depth/2,inner),(-depth/2,inner))
         for j in range(n)]
    f = [(k*n+j,k*n+(j+1)%n,((k+1)%4)*n+(j+1)%n,((k+1)%4)*n+j)
         for k in range(4) for j in range(n)]
    return place(mesh(label,v,f,role),loc,rot)


def beam(label, a, b, width, depth, role, bevel=False):
    vec = Vector(b)-Vector(a)
    obj = box(label,(0,0,0),(width,depth,vec.length),role,bevel)
    obj.data.transform(Matrix.Translation((Vector(a)+Vector(b))/2) @
                       vec.to_track_quat('Z','Y').to_matrix().to_4x4())
    return obj


def tube(label, points, radius, role, n=6):
    v = []
    for i,p in enumerate(points):
        t = (Vector(points[min(i+1,len(points)-1)])-Vector(points[max(i-1,0)])).normalized()
        ref = Vector((0,1,0)) if abs(t.y)<.9 else Vector((1,0,0))
        u = ref.cross(t).normalized(); w = t.cross(u)
        v += [tuple(Vector(p)+radius*(u*math.cos(j*TAU/n)+w*math.sin(j*TAU/n))) for j in range(n)]
    f = [tuple(reversed(range(n))),tuple(range((len(points)-1)*n,len(points)*n))]
    f += [(k*n+j,k*n+(j+1)%n,(k+1)*n+(j+1)%n,(k+1)*n+j)
          for k in range(len(points)-1) for j in range(n)]
    return mesh(label,v,f,role)


def gem(label, loc, size, role, rot=(0,0,0)):
    bpy.ops.mesh.primitive_ico_sphere_add(subdivisions=2,radius=1)
    obj = bpy.context.object; obj.name = label
    obj.data.materials.append(MATS[role]); PARTS.append(obj)
    obj.data.transform(Matrix.Diagonal((*size,1)))
    return place(obj,loc,rot)


def star(x,y,z,r=.085,role='silver'):
    p = [(x+math.sin(j*TAU/8)*r*(1 if j%2==0 else .28),
          z+math.cos(j*TAU/8)*r*(1 if j%2==0 else .28)) for j in range(8)]
    return extrude('Four point liturgical star',p,y-.025,y+.025,role)


def rosette(x,y,z,r=.18,angle=0):
    first = len(PARTS)
    p = [(math.sin(j*TAU/40)*r*(1 if j%4 in (1,2) else .84),
          z+math.cos(j*TAU/40)*r*(1 if j%4 in (1,2) else .84)) for j in range(40)]
    extrude('Gear rosette',p,y-.04,y+.04,'silver')
    cylinder('Rosette enamel centre',(0,y+.055,z),r*.53,.04,'dial_mark',12,FACE)
    star(0,y+.085,z,r*.37,'porcelain')
    for obj in PARTS[first:]: place(obj,(x,0,0),(0,0,angle))


def banner(x,y,top,w,length):
    p = [(x-w/2,top),(x+w/2,top),(x+w/2,top-length),
         (x+w*.20,top-length),(x,top-length+.20),
         (x-w*.22,top-length),(x-w/2,top-length)]
    obj = extrude('Pleated swallow tail sacred cloth',p,y-.03,y+.03,'ribbon_red')
    for v in obj.data.vertices:
        v.co.y += .025*math.cos((v.co.x-x)*TAU/w)
    for dx in (-w*.31,w*.31):
        box('Raised woven ribbon fold',(x+dx,y+.045,top-length*.43),
            (.04,.04,length*.80),'red_lacquer')
    box('Silver cloth suspension',(x,y,top+.025),(w+.12,.08,.07),'silver')
    star(x,y+.085,top-min(.29,length*.32),w*.22)


def candle(x,y,bottom,h,r=.055):
    cylinder('Ivory candle',(x,y,bottom+h/2),r,h,'porcelain',10)
    cylinder('Silver candle socket',(x,y,bottom+.025),r*1.26,.05,'silver',10)
    cylinder('Dark wax cup',(x,y,bottom+h+.006),r*.77,.018,'stone_dark',10)
    cylinder('Broad wax drip',(x-r*.7,y+r*.7,bottom+h-.055),.022,.13,'porcelain',6)
    lathe('Small candle flame',[(.022,bottom+h+.025),(.035,bottom+h+.065),
          (.006,bottom+h+.15)],'lamp_warm',(x,y,0),6)


def little_clock(loc, r, rot=(0,0,0), halo=False):
    """Clock on +Y plane; rotating the entire group also serves pew ends."""
    start = len(PARTS)
    ring('Silver dial molding',(0,0,0),r,r*.85,.06,'silver',32)
    if halo:
        ring('Open clock dial halo',(0,.015,0),r*.86,r*.61,.045,'dial_face',32)
    else:
        cylinder('Clock enamel',(0,.01,0),r*.85,.045,'dial_face',24,FACE)
    for j in range(12):
        a = -j*TAU/12
        p = Vector((math.sin(a)*r*.72,.052,math.cos(a)*r*.72))
        beam('Clock hour bar',p-Vector((math.sin(a),0,math.cos(a)))*.035,
             p+Vector((math.sin(a),0,math.cos(a)))*.035,.04,.04,'dial_mark')
    if not halo:
        for a,l in ((-.72,r*.48),(.78,r*.33)):
            beam('Silver dial hand',(0,.083,0),(math.sin(a)*l,.083,math.cos(a)*l),.04,.04,'silver')
        cylinder('Dial pivot',(0,.11,0),.04,.04,'red_lacquer',8,FACE)
    for obj in PARTS[start:]: place(obj,loc,rot)


def nave_column():
    for z,w,h,role in ((.12,1.2,.24,'stone_dark'),(.275,1.10,.07,'silver'),
                       (.405,1.04,.19,'stone_dark'),(.535,1.12,.07,'silver'),
                       (.585,1.02,.03,'stone')):
        box('Stepped square column plinth',(0,0,z),(w,w,h),role,True)
    lathe('Marble column foot',[(.50,.60),(.50,.69),(.45,.77),(.40,.86)],'porcelain',n=32)
    lathe('Foot dark reveal',[(.455,.72),(.455,.765)],'stone_dark',n=32)
    # Sixteen broad sculpted flutes; stone in the recess breaks the white mass.
    n = 64
    profile = [(.40,.84),(.385,1.04),(.36,5.28),(.385,5.38)]
    verts = [(r*(.95+.05*math.cos(j*TAU/4))*math.cos(j*TAU/n),
              r*(.95+.05*math.cos(j*TAU/4))*math.sin(j*TAU/n),z)
             for r,z in profile for j in range(n)]
    faces = [tuple(reversed(range(n))),tuple(range(3*n,4*n))]
    faces += [(k*n+j,k*n+(j+1)%n,(k+1)*n+(j+1)%n,(k+1)*n+j) for k in range(3) for j in range(n)]
    obj = mesh('Tapered fluted porcelain shaft',verts,faces,'porcelain')
    obj.data.materials.append(MATS['stone'])
    for i,p in enumerate(obj.data.polygons):
        if i>=2 and (i-2)%4==1: p.material_index = 1
    for z in (1.42,5.03):
        lathe('Silver shaft ring band',[(.401,z-.065),(.425,z-.035),(.425,z+.035),(.401,z+.065)],'silver',n=32)
    lathe('Stacked carved silver capital',[(.385,5.32),(.46,5.40),(.42,5.48),
          (.53,5.58),(.53,5.67),(.48,5.72),(.56,5.84)],'silver',n=32)
    cylinder('Capital pale clock tick seat',(0,0,5.90),.56,.13,'porcelain',32)
    for j in range(32):
        a = j*TAU/32
        box('Capital clock tick',(.56*math.cos(a),.56*math.sin(a),5.90),
            (.048,.045,.095 if j%4==0 else .055),'dial_mark',rot=(0,0,a))
    box('Capital square abacus',(0,0,6.025),(1.16,1.16,.12),'stone_dark',True)
    box('Capital silver crown',(0,0,6.115),(1.18,1.18,.06),'silver',True)
    for angle in (0,math.pi/2,math.pi,3*math.pi/2): rosette(0,.50,5.66,.17,angle)
    # Fracture varies in BOTH horizontal axes, avoiding a corrugated roof shape.
    rim = [(-.48,-.46),(0,-.46),(.48,-.46),(.48,0),
           (.48,.46),(0,.46),(-.48,.46),(-.48,0)]
    heights = (6.37,6.44,6.39,6.47,6.36,6.45,6.34,6.40)
    verts = [(x,y,6.145) for x,y in rim]
    verts += [(x*(.94 if j%3==0 else 1),y*(.88 if j%3==0 else 1),heights[j])
              for j,(x,y) in enumerate(rim)]
    verts.append((.08,-.09,6.5))
    faces = [tuple(reversed(range(8)))]
    faces += [(j,(j+1)%8,(j+1)%8+8,j+8) for j in range(8)]
    faces += [(8+j,8+(j+1)%8,16) for j in range(8)]
    obj = mesh('Fractured short marble impost',verts,faces,'stone',True)
    obj.data.materials.append(MATS['stone_dark'])
    obj.data.polygons[11].material_index = 1
    banner(0,.405,5.03,.45,1.8)
    # Textile turns over the upper band before falling down its front.
    box('Banner wrapped over band',(0,.17,5.098),(.45,.47,.045),'ribbon_red')


def angel_statue():
    for r,z,h,role in ((.8,.10,.20,'stone_dark'),(.77,.225,.05,'silver'),
                       (.71,.30,.10,'stone'),(.72,.375,.05,'silver'),(.66,.425,.05,'stone_dark')):
        cylinder('Round saint pedestal',(0,0,z),r,h,role,32,bevel=True)
    for j in range(12):
        a = j*TAU/12
        box('Pedestal pale dentil',(.715*math.cos(a),.715*math.sin(a),.285),
            (.09,.045,.075),'porcelain',rot=(0,0,a))
    # Knees project forwards; folded shins and robe trail back on the pedestal.
    for s in (-1,1):
        gem('Kneeling bent knee',(s*.19,.33,.66),(.20,.27,.19),'porcelain')
        box('Tucked shin under robe',(s*.17,-.12,.56),(.22,.55,.18),'stone',True)
    robe = [(-.30,.55),(.30,.55),(.31,.83),(.20,1.56),(.17,1.77),(-.17,1.77),(-.24,1.25)]
    extrude('Faceted kneeling white robe',robe,-.23,.22,'porcelain',True)
    for x in (-.20,-.07,.07,.20):
        beam('Deep vertical robe fold',(x,.24,.68),(x*.50,.24,1.51),.045,.05,'stone')
    lathe('Silver clockwork waist belt',[(.25,1.23),(.25,1.31)],'silver',n=12)
    cylinder('Angel neck',(0,.10,1.91),.085,.20,'stone',12)
    gem('Bowed doll head',(0,.29,2.13),(.205,.20,.25),'porcelain',(-.40,0,0))
    # Two simple lowered eyelids keep the doll's prayer readable at close range.
    for s in (-1,1):
        beam('Lowered eye',(s*.04,.480,2.13),(s*.12,.465,2.11),.04,.04,'stone_dark')
    little_clock((0,-.005,2.26),.35,halo=True)
    for s in (-1,1):
        gem('Silver shoulder joint',(s*.215,.025,1.70),(.13,.14,.15),'silver')
        beam('Upper praying arm',(s*.23,.15,1.69),(s*.28,.35,1.48),.135,.14,'porcelain',True)
        gem('Elbow hinge',(s*.28,.35,1.49),(.075,.075,.075),'silver')
        beam('Upward folded forearm',(s*.28,.36,1.49),(s*.065,.52,1.82),.12,.12,'porcelain',True)
        beam('Joined prayer palm',(s*.045,.53,1.80),(s*.035,.50,2.00),.07,.10,'porcelain',True)
    gem('Small chest ruby',(0,.255,1.64),(.065,.045,.085),'gem_red')
    # Tall swept wings, broad enough to survive the top-down game camera.
    for s in (-1,1):
        for j in range(6):
            out = .48+j*.042
            high = 2.98-j*.17
            p = [(s*.14,1.51),(s*(out-.10),high-.24),(s*out,high),
                 (s*(out+.075),high+.015),(s*(out+.075),high-.11),
                 (s*(out+.13),high-.15),(s*.24,1.18-j*.035)]
            extrude('Layered swept silver gear feather',p,-.40-j*.025,-.31-j*.025,'silver')
            # Smaller nested stone blades leave a strong silver tooth edge.
            q = [(s*.17,1.46),(s*(out-.06),high-.29),
                 (s*(out+.025),high-.16),(s*.26,1.24)]
            extrude('Inner carved stone feather',q,-.295-j*.025,-.25-j*.025,'stone')
        beam('Wing silver root',(s*.14,-.24,1.45),(s*.43,-.37,2.52),.065,.07,'silver')
    for x,y,h in ((-.42,.46,.22),(0,.62,.28),(.42,.46,.33)):
        candle(x,y,.45,h,.05)


def lantern_stand():
    for r,z,h,role in ((.35,.07,.14,'iron_dark'),(.31,.16,.04,'silver'),
                       (.27,.235,.11,'stone_dark'),(.22,.31,.04,'silver')):
        cylinder('Octagonal standing lantern base',(0,0,z),r,h,role,8,bevel=True)
    lathe('Tapered iron standing post',[(.12,.33),(.085,.47),(.055,2.84)],'iron_dark',(0,-.19,0),12)
    for z in (.44,.63,1.57,2.03,2.75):
        cylinder('Post silver ring',(0,-.19,z),.09,.065,'silver',12)
    tube('Curved lantern hanging crook',[(0,-.19,2.70),(0,-.19,3.05),
         (0,-.13,3.16),(0,.02,3.16),(0,.08,3.09),(0,.08,3.0)],.04,'iron_dark',8)
    ring('Lantern suspension loop',(0,.08,2.97),.075,.035,.045,'silver',16)
    cy = .08
    cylinder('Lantern cage bottom',(0,cy,2.18),.235,.09,'iron_dark',8,bevel=True)
    cylinder('Lantern lower silver reveal',(0,cy,2.235),.215,.035,'silver',8)
    cylinder('Warm lantern glass panels',(0,cy,2.465),.17,.43,'lamp_warm',8)
    for j in range(8):
        a = j*TAU/8
        box('Iron lantern cage upright',(.195*math.cos(a),cy+.195*math.sin(a),2.47),
            (.045,.045,.49),'iron_dark',rot=(0,0,a))
    cylinder('Upper lantern cage band',(0,cy,2.715),.235,.07,'iron_dark',8)
    lathe('Pointed lantern hood',[(.25,2.75),(.25,2.79),(.065,2.92)],'iron_dark',(0,cy,0),8)
    cylinder('Lantern silver eave molding',(0,cy,2.785),.252,.035,'silver',8)
    cylinder('Silver lantern finial',(0,cy,2.94),.05,.08,'silver',8)
    # Small tied ribbon immediately below the cage, not a long second banner.
    cylinder('Ribbon knot',(0,.08,2.105),.07,.08,'ribbon_red',8)
    for s in (-1,1):
        extrude('Short forked ribbon tail',[(s*.02,2.11),(s*.17,2.05),
                (s*.20,1.78),(s*.10,1.85),(s*.035,1.79)],.07,.115,'ribbon_red')
    box('Ribbon knot silver pin',(0,.135,2.095),(.075,.035,.065),'silver')


def pew():
    for x in (-1.22,1.22):
        for y in (-.34,.34):
            box('Iron pew foot',(x,y,.075),(.30,.27,.15),'iron_dark',True)
            box('Pew leg',(x,y,.30),(.15,.15,.42),'wood_dark',True)
    box('Carved seat structural rail',(0,0,.45),(2.78,.82,.15),'wood',True)
    box('Thick dark wooden seat',(0,.035,.55),(2.78,.87,.12),'wood_dark',True)
    box('Velvet seat cushion',(0,.10,.645),(2.48,.61,.10),'velvet',True)
    for y in (-.17,.37):
        box('Cushion silver piping',(0,y,.69),(2.43,.04,.04),'silver')
    box('Back board facing minus Y',(0,-.37,.88),(2.72,.18,.54),'wood_dark',True)
    for x in (-.87,0,.87):
        box('Carved wooden back panel',(x,-.262,.88),(.77,.055,.37),'wood',True)
        # Shallow pointed panel frame quotes the gothic arcade above the pew.
        tube('Panel pointed silver carving',[(x-.27,-.222,.73),(x-.27,-.222,.97),
             (x,-.222,1.055),(x+.27,-.222,.97),(x+.27,-.222,.73)],.022,'silver',4)
        box('Rear carved panel',(x,-.475,.87),(.73,.04,.32),'wood')
    box('Rounded back crest',(0,-.37,1.165),(2.82,.22,.07),'wood',True)
    for s in (-1,1):
        profile = [(-.49,.16),(.45,.16),(.45,.69),(.30,.80),
                   (-.02,.80),(-.04,1.03),(-.22,1.17),(-.44,1.03)]
        # Authored in XZ then rotate: the side panel runs along Y at each end.
        obj = extrude('Shaped wooden pew end',profile,-.075,.075,'wood_dark',True)
        place(obj,(s*1.36,0,0),(0,0,math.pi/2))
        obj = extrude('Side carved inset',[(-.36,.29),(.30,.29),(.30,.61),(-.36,.74)],-.02,.02,'wood')
        place(obj,(s*1.445,0,0),(0,0,math.pi/2))
        little_clock((s*1.46,-.235,.96),.24,(0,0,-s*math.pi/2))
        box('Pew end silver armrest',(s*1.36,.15,.805),(.17,.56,.06),'silver',True)


def reliquary():
    for z,w,h,role in ((.08,1.4,.16,'stone'),(.18,1.31,.04,'silver'),
                       (.265,1.20,.13,'stone_dark'),(.35,1.28,.04,'silver'),(.405,1.14,.07,'stone')):
        box('Stepped shrine pedestal',(0,0,z),(w,w,h),role,True)
    for x in (-.48,.48):
        for y in (-.48,.48):
            box('Corner pillar dark plinth',(x,y,.49),(.23,.23,.10),'stone_dark',True)
            lathe('Short porcelain shrine pillar',[(.105,.54),(.105,.61),(.065,.68),
                  (.062,1.00),(.09,1.08)],'porcelain',(x,y,0),12)
            cylinder('Pillar silver capital',(x,y,1.105),.115,.05,'silver',12)
            gem('Pillar pointed cap',(x,y,1.17),(.075,.075,.085),'porcelain')
    for z in (.52,1.87):
        cylinder('Hourglass silver disc',(0,0,z),.46,.085,'silver',32,bevel=True)
        cylinder('Hourglass dark disc fillet',(0,0,z+.055),.405,.035,'stone_dark',32)
        cylinder('Hourglass porcelain disc inset',(0,0,z+.079),.33,.025,'porcelain',24)
    # Four twisted, open supports leave the glass silhouette clearly visible.
    for j in range(4):
        a = math.pi/4+j*TAU/4
        points = []
        for k in range(15):
            t = k/14
            r = .405+.023*math.sin(t*TAU*2)
            angle = a+.065*math.cos(t*TAU*2)
            points.append((r*math.cos(angle),r*math.sin(angle),.59+t*1.23))
        tube('Twisted silver hourglass support',points,.037,'silver',6)
    lower = [(.16,.61),(.26,.68),(.30,.83),(.28,.99),(.16,1.10),(.055,1.18)]
    upper = [(.055,1.18),(.16,1.26),(.28,1.39),(.30,1.55),(.26,1.72),(.16,1.79)]
    for profile in (lower,upper):
        # Real sealed thin shells. The review uses material alpha for glass.
        lathe('Faceted glass hourglass bulb',profile+[(r-.018,z) for r,z in reversed(profile)],'glass',n=24,closed=True)
    lathe('Upper starlight sand reservoir',[(.025,1.205),(.09,1.30),(.20,1.49),(.21,1.59)],'starlight',n=16)
    lathe('Lower starlight sand heap',[(.17,.65),(.21,.70),(.15,.77),(.025,.96)],'starlight',n=16)
    cylinder('Narrow falling luminous sand',(0,0,1.105),.023,.31,'starlight',8)
    for j in range(7):
        gem('Falling star sand grain',(random.uniform(-.07,.07),random.uniform(-.07,.07),.93+j*.05),
            (.025,.025,.035),'starlight')
    cylinder('Small hourglass crown',(0,0,1.965),.065,.07,'silver',12)
    box('Cloth draped across shrine step',(0,.39,.453),(.38,.49,.035),'ribbon_red')
    banner(0,.64,.43,.38,.35)


def floor_dial():
    """One watertight 60 mm slab, all mosaic colours at exactly the same Z.

    CDT constraints partition nested circles, star, strokes and hands. Material
    precedence is determined by original face IDs, so even crossed clock hands
    have a single top surface and no coplanar overlays or z-fighting.
    """
    vertices, polygons, roles = [], [], []

    def region(points, role):
        points = list(points)
        # CDT's original-face membership requires counterclockwise boundaries.
        if sum(a[0]*b[1]-b[0]*a[1] for a,b in zip(points,points[1:]+points[:1]))<0:
            points.reverse()
        first = len(vertices)
        vertices.extend(Vector((x,y)) for x,y in points)
        polygons.append(list(range(first,len(vertices))))
        roles.append(role)

    def circle(r,role,n=96):
        region([(r*math.cos(j*TAU/n),r*math.sin(j*TAU/n)) for j in range(n)],role)

    def stroke(a,b,width,role):
        a,b = Vector(a),Vector(b)
        normal = Vector((-(b-a).y,(b-a).x)).normalized()*width/2
        region([a-normal,b-normal,b+normal,a+normal],role)

    circle(3,'silver')
    circle(2.82,'dial_mark')
    circle(2.75,'porcelain')
    circle(2.15,'silver')
    circle(2.10,'porcelain')
    # Broad faint eight-point star, alternating long and short rays.
    region([(math.sin(j*TAU/16)*(2.03 if j%2==0 else 1.35),
             math.cos(j*TAU/16)*(2.03 if j%2==0 else 1.35)) for j in range(16)],'stone')
    # Eight pale kites split the large field and keep the star a quiet mosaic.
    for j in range(8):
        a = j*TAU/8
        u,v = Vector((math.sin(a),math.cos(a))),Vector((math.cos(a),-math.sin(a)))
        region([u*.28,u*1.65+v*.08,u*1.86,u*1.65-v*.08],'porcelain')
    numerals = ('XII','I','II','III','IV','V','VI','VII','VIII','IX','X','XI')
    glyph = {'I': [((0,-.13),(0,.13))],
             'V': [((-.055,.13),(0,-.13)),((0,-.13),(.055,.13))],
             'X': [((-.055,-.13),(.055,.13)),((-.055,.13),(.055,-.13))]}
    for hour,letters in enumerate(numerals):
        # XII is towards -Y, upright from the above-front review camera.
        # Positive angles run clockwise when seen from above (+Z).
        a = math.pi+hour*TAU/12
        u,v = Vector((math.cos(a),-math.sin(a))),Vector((math.sin(a),math.cos(a)))
        centre = v*2.455
        for k,ch in enumerate(letters):
            offset = (k-(len(letters)-1)/2)*.155
            for p,q in glyph[ch]:
                stroke(centre+u*(offset+p[0])+v*p[1],centre+u*(offset+q[0])+v*q[1],.055,'dial_mark')
    for j in range(48):
        a = j*TAU/48
        u = Vector((math.sin(a),math.cos(a)))
        stroke(u*2.86,u*(2.96 if j%4==0 else 2.92),.045,'dial_mark')
    for a,length,width in ((-.85,1.74,.11),(.78,1.22,.15)):
        a += math.pi
        u,v = Vector((math.sin(a),math.cos(a))),Vector((math.cos(a),-math.sin(a)))
        region([-u*.30-v*width,u*(length-.20)-v*width*.55,u*length,
                u*(length-.20)+v*width*.55,-u*.30+v*width],'dial_mark')
        stroke(u*.12,u*(length-.17),width*.65,'silver')
    circle(.16,'silver',24)
    circle(.09,'gem_red',16)
    v2,edges,faces,origv,orige,origf = delaunay_2d_cdt(vertices,[],polygons,0,1e-7,True)
    verts = [(v.x,v.y,.06) for v in v2]
    top_count = len(faces)
    # Recover the actual outer CDT boundary, including any new split vertices.
    counts = {}
    for face in faces:
        for a,b in zip(face,face[1:]+face[:1]):
            key = tuple(sorted((a,b))); counts[key] = counts.get(key,0)+1
    outer = sorted({i for edge,count in counts.items() if count==1 for i in edge},
                   key=lambda i:math.atan2(v2[i].y,v2[i].x))
    bottom = len(verts)
    verts += [(v2[i].x,v2[i].y,0) for i in outer]
    all_faces = list(faces)+[tuple(reversed(range(bottom,bottom+len(outer))))]
    all_faces += [(outer[j],bottom+j,bottom+(j+1)%len(outer),outer[(j+1)%len(outer)])
                  for j in range(len(outer))]
    obj = mesh('Flush clock mosaic slab',verts,all_faces,'silver')
    slots = {'silver':0}
    for role in dict.fromkeys(roles):
        if role not in slots:
            slots[role] = len(obj.data.materials); obj.data.materials.append(MATS[role])
    slots['stone_dark'] = len(obj.data.materials)
    obj.data.materials.append(MATS['stone_dark'])
    obj.data.polygons[top_count].material_index = slots['stone_dark']
    for j in range(top_count):
        assert origf[j], 'Unassigned mosaic triangle'
        obj.data.polygons[j].material_index = slots[roles[max(origf[j])]]


SPECS = [
    ('Deco_NaveColumn',(1.2,1.2,6.5),nave_column,6000),
    ('Deco_AngelStatue',(1.6,1.6,3.0),angel_statue,6000),
    ('Deco_LanternStand',(.7,.7,3.2),lantern_stand,3000),
    ('Deco_Pew',(3.,1.,1.2),pew,3000),
    ('Deco_Reliquary',(1.4,1.4,2.0),reliquary,6000),
    ('Deco_FloorDial',(6.,6.,.06),floor_dial,3000),
]


def triangles(obj):
    obj.data.calc_loop_triangles()
    return len(obj.data.loop_triangles)


def bounds(obj):
    return [Vector(tuple(fn(v.co[i] for v in obj.data.vertices) for i in range(3))) for fn in (min,max)]


def finish(name,dims,budget):
    bpy.ops.object.select_all(action='DESELECT')
    for obj in PARTS:
        obj.select_set(True); bpy.context.view_layer.objects.active = obj
        for mod in list(obj.modifiers): bpy.ops.object.modifier_apply(modifier=mod.name)
        # Clamped bevel miters on a jagged stone can collapse tiny edges.
        clean = bmesh.new(); clean.from_mesh(obj.data)
        bmesh.ops.remove_doubles(clean,verts=list(clean.verts),dist=1e-7)
        bmesh.ops.dissolve_degenerate(clean,edges=list(clean.edges),dist=1e-7)
        clean.to_mesh(obj.data); clean.free()
        obj.select_set(False)
    for obj in PARTS: obj.select_set(True)
    bpy.context.view_layer.objects.active = PARTS[0]
    if len(PARTS)>1: bpy.ops.object.join()
    obj = bpy.context.object; obj.name, obj.data.name = name,name+'_Mesh'
    obj.location = (0,0,0)
    # Keep silhouettes and role boundaries while simplifying buried bevels.
    count = triangles(obj)
    if count>budget:
        mod = obj.modifiers.new('Preserve material regions within budget','DECIMATE')
        mod.ratio = (budget-100)/count; mod.delimit = {'MATERIAL'}
        bpy.ops.object.modifier_apply(modifier=mod.name)
    low,high = bounds(obj); span,centre = high-low,(high+low)/2
    for v in obj.data.vertices:
        v.co.x = (v.co.x-centre.x)*dims[0]/span.x
        v.co.y = (v.co.y-centre.y)*dims[1]/span.y
        v.co.z = (v.co.z-low.z)*dims[2]/span.z
    bm = bmesh.new(); bm.from_mesh(obj.data)
    bmesh.ops.recalc_face_normals(bm,faces=list(bm.faces))
    assert all(e.is_manifold for e in bm.edges), (name,'non-manifold geometry')
    assert all(f.calc_area()>1e-10 for f in bm.faces),(name,'degenerate face')
    bm.to_mesh(obj.data); bm.free()
    for p in obj.data.polygons: p.use_smooth = False
    obj.data.update(); bpy.context.view_layer.update()
    low,high = bounds(obj)
    assert all(abs((high-low)[i]-dims[i])<1e-4 for i in range(3)),(name,low,high)
    assert abs(low.z)<1e-5 and abs(high.x+low.x)<1e-5 and abs(high.y+low.y)<1e-5
    assert triangles(obj)<=budget,(name,triangles(obj),budget)
    assert all(math.isfinite(c) for v in obj.data.vertices for c in v.co)
    areas = {}
    for p in obj.data.polygons:
        role = obj.data.materials[p.material_index].name
        assert role in PALETTE
        areas[role] = areas.get(role,0)+p.area
    assert len(areas)>=4,(name,areas)
    assert max(areas.values())/sum(areas.values())<=.55,(name,areas)
    print('MATERIALS',name,{k:round(v/sum(areas.values()),3) for k,v in areas.items()},flush=True)
    if name=='Deco_FloorDial':
        # All upward faces are flush; no raised hands, ticks or centre jewel.
        assert all(abs(v.co.z)<1e-6 or abs(v.co.z-.06)<1e-6 for v in obj.data.vertices)
        assert all(abs(p.center.z-.06)<1e-5 for p in obj.data.polygons if p.normal.z>.9)
    for mat in obj.data.materials:
        assert tuple(mat.diffuse_color)==(.5,.5,.5,1)
        assert tuple(mat.node_tree.nodes['Principled BSDF'].inputs['Base Color'].default_value)==(.5,.5,.5,1)
    obj['front'],obj['origin_rule'] = '+Y','Bottom centre'
    obj['chapter'],obj['triangle_count'] = 'Cathedral of Time',triangles(obj)
    obj['solid_blocker'] = name!='Deco_FloorDial'
    destination = OUTPUT/f'Kit_{name}.fbx'
    bpy.ops.export_scene.fbx(filepath=str(destination),use_selection=True,
        axis_forward='-Z',axis_up='Y',apply_scale_options='FBX_SCALE_UNITS',
        bake_space_transform=True,object_types={'MESH'},use_mesh_modifiers=True,
        mesh_smooth_type='FACE',add_leaf_bones=False,bake_anim=False,use_custom_props=True)
    assert destination.is_file() and destination.stat().st_size>1024
    print(f'EXPORTED {name} {triangles(obj)} {tuple(round(d,4) for d in obj.dimensions)}',flush=True)
    return obj


def preview(objects):
    scene = bpy.context.scene
    scene.render.engine = 'BLENDER_WORKBENCH'
    scene.render.resolution_x,scene.render.resolution_y = 1800,1000
    scene.render.resolution_percentage = 100
    scene.render.image_settings.file_format = 'PNG'
    scene.render.filepath = str(PREVIEW)
    scene.view_settings.view_transform = 'Standard'
    shade = scene.display.shading
    shade.light,shade.color_type = 'FLAT','MATERIAL'
    shade.show_shadows,shade.show_cavity = False,True
    shade.cavity_type = 'BOTH'
    shade.curvature_ridge_factor,shade.curvature_valley_factor = 1.45,1.25
    shade.show_specular_highlight = False
    shade.background_type = 'WORLD'
    if scene.world is None: scene.world = bpy.data.worlds.new('Temple furniture review')
    scene.world.color = (.025,.029,.040)
    helpers = []
    try:
        for role,mat in MATS.items(): mat.diffuse_color = (*PALETTE[role],.20 if role=='glass' else 1)
        camera = bpy.data.objects.new('Review camera',bpy.data.cameras.new('Review camera'))
        scene.collection.objects.link(camera); helpers.append(camera); scene.camera = camera
        el,az = math.radians(45),math.radians(-12)
        near = Vector((math.sin(az),math.cos(az),0))
        camera.location = near*35*math.cos(el)+Vector((0,0,35*math.sin(el)))
        camera.rotation_euler = (-camera.location).to_track_quat('-Z','Y').to_euler()
        camera.data.type = 'ORTHO'
        rotation = camera.rotation_euler.to_matrix(); inv = rotation.transposed()
        right = rotation @ Vector((1,0,0))

        def projected(obj):
            return [inv @ (obj.matrix_world @ v.co) for v in obj.data.vertices]

        def caption(body,x,y,size,role='silver'):
            data = bpy.data.curves.new('Review typography','FONT')
            data.body,data.align_x,data.size = body,'CENTER',size
            data.materials.append(MATS[role])
            label = bpy.data.objects.new('Review label',data)
            scene.collection.objects.link(label); helpers.append(label)
            label.rotation_euler = camera.rotation_euler
            label.location = rotation @ Vector((x,y,0))
            return label

        # Every base centre is on ONE straight floor line. No rescaling of props.
        cursor,review = 0,[]
        for obj,(name,dims,_,_) in zip(objects,SPECS):
            pts = projected(obj)
            lo,hi = min(p.x for p in pts),max(p.x for p in pts)
            width = max(hi-lo,2.15)
            x = cursor+width/2
            obj.location = right*(x-(lo+hi)/2)
            bpy.context.view_layer.update()
            pts = projected(obj); review += pts
            y = min(p.y for p in pts)-.34
            caption(name.removeprefix('Deco_'),x,y,.22,'porcelain')
            caption(' x '.join(f'{d:g}' for d in dims)+' m',x,y-.27,.145)
            review += [Vector((cursor,y-.42,0)),Vector((cursor+width,y+.20,0))]
            cursor += width+.60
        top = max(p.y for p in review)
        cx = (min(p.x for p in review)+max(p.x for p in review))/2
        caption('IV  /  CATHEDRAL OF TIME',cx,top+.85,.43,'porcelain')
        caption('TEMPLE FURNITURE     |     SIX MESHES     |     TRUE METRE SCALE     |     +Y FRONT',cx,top+.42,.18)
        review += [Vector((0,top+1.4,0)),Vector((cursor-.60,top+1.4,0))]
        lo = Vector((min(p.x for p in review),min(p.y for p in review),0))
        hi = Vector((max(p.x for p in review),max(p.y for p in review),0))
        camera.location += rotation @ ((lo+hi)/2)
        camera.data.ortho_scale = max(hi.x-lo.x,(hi.y-lo.y)*1.8)*1.07
        PREVIEW.parent.mkdir(parents=True,exist_ok=True)
        bpy.ops.render.render(write_still=True)
        assert PREVIEW.is_file() and PREVIEW.stat().st_size>1024
        print(f'PREVIEW {PREVIEW}',flush=True)
    finally:
        scene.camera = None
        for obj in helpers: bpy.data.objects.remove(obj,do_unlink=True)
        for obj in objects: obj.location = (0,0,0)
        for mat in MATS.values(): mat.diffuse_color = (.5,.5,.5,1)


def main():
    global PARTS
    bpy.ops.wm.read_factory_settings(use_empty=True)
    for mat in list(bpy.data.materials): bpy.data.materials.remove(mat)
    bpy.context.scene.unit_settings.system = 'METRIC'
    bpy.context.scene.unit_settings.scale_length = 1
    materials(); OUTPUT.mkdir(parents=True,exist_ok=True)
    objects = []
    for i,(name,dims,build,budget) in enumerate(SPECS):
        PARTS = []; random.seed(44072+i*7919)
        build(); objects.append(finish(name,dims,budget))
    assert len(bpy.context.scene.objects)==6
    assert all(o.type=='MESH' for o in bpy.context.scene.objects)
    preview(objects)
    assert len(bpy.context.scene.objects)==6 and all(o.type=='MESH' for o in bpy.context.scene.objects)
    print('DONE',flush=True)


if __name__=='__main__':
    main()
