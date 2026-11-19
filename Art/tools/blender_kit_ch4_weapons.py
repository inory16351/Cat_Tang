# 4장 태엽 인형용 손 무기 8종: 손잡이 중심 원점, FBX와 검수 이미지 생성.
# 실행: "C:/Program Files/Blender Foundation/Blender 5.3/blender.exe" -b --factory-startup -P Art/tools/blender_kit_ch4_weapons.py
"""Blender 5.x. Metres; grip centre = zero; working end +Z; front +Y.

Only this source file is required. Export shaders are neutral grey; review
colours, captions and origin crosses are applied AFTER all FBX exports.
The mallet's head reaches .38; the greatsword pommel is centred at -.30,
with its lower finial reaching -.45 to preserve the requested 2.80 length.
"""
import math
import random
from pathlib import Path

import bmesh
import bpy
from mathutils import Euler, Matrix, Vector

ROOT = Path(__file__).resolve().parents[2]
OUTPUT = ROOT / 'Game/Assets/Art/Characters/Ch4'
PREVIEW = ROOT / 'Art/tools/specs/blender_kit_ch4_weapons_preview.png'
TAU = math.tau
FRONT = (math.pi / 2, 0, 0)
MATS, PARTS = {}, []
# Deliberately achromatic brass/copper/wood in the review, with lacquer red.
PALETTE = {
    'steel': (.27, .29, .32), 'steel_dark': (.18, .19, .21),
    'iron': (.12, .13, .15), 'iron_dark': (.025, .028, .035),
    'silver': (.86, .88, .90), 'brass': (.61, .62, .63),
    'copper': (.33, .34, .36), 'wood': (.29, .30, .32),
    'wood_dark': (.055, .06, .07), 'dial_face': (.94, .94, .92),
    'dial_mark': (.018, .021, .027), 'red_lacquer': (.72, .018, .035),
}


def materials():
    for role in PALETTE:
        mat = bpy.data.materials.new(role)
        mat.use_nodes = True
        mat.diffuse_color = (.5, .5, .5, 1)
        shader = mat.node_tree.nodes['Principled BSDF']
        shader.inputs['Base Color'].default_value = (.5, .5, .5, 1)
        shader.inputs['Metallic'].default_value = 0
        shader.inputs['Roughness'].default_value = .8
        MATS[role] = mat


def mesh(label, verts, faces, role, bevel=False):
    data = bpy.data.meshes.new(label)
    data.from_pydata(verts, [], faces)
    data.update()
    obj = bpy.data.objects.new(label, data)
    bpy.context.collection.objects.link(obj)
    data.materials.append(MATS[role])
    if bevel:
        mod = obj.modifiers.new('Toy edge chamfer', 'BEVEL')
        mod.width, mod.segments = .02, 2
        mod.limit_method = 'ANGLE'
        mod.angle_limit = math.radians(35)
        mod.use_clamp_overlap = True
    PARTS.append(obj)
    return obj


def place(obj, loc=(0, 0, 0), rot=(0, 0, 0)):
    obj.data.transform(Matrix.Translation(Vector(loc)) @ Euler(rot).to_matrix().to_4x4())
    return obj


def box(label, loc, size, role, rot=(0, 0, 0), bevel=True):
    x, y, z = (s / 2 for s in size)
    v = [(-x,-y,-z),(x,-y,-z),(x,y,-z),(-x,y,-z),
         (-x,-y,z),(x,-y,z),(x,y,z),(-x,y,z)]
    f = [(0,3,2,1),(4,5,6,7),(0,1,5,4),(1,2,6,5),(2,3,7,6),(3,0,4,7)]
    return place(mesh(label, v, f, role, bevel), loc, rot)


def cylinder(label, loc, radius, depth, role, n=12, rot=(0, 0, 0), bevel=False):
    v = [(radius*math.cos(TAU*j/n), radius*math.sin(TAU*j/n), z)
         for z in (-depth/2, depth/2) for j in range(n)]
    f = [tuple(reversed(range(n))), tuple(range(n,2*n))]
    f += [(j,(j+1)%n,(j+1)%n+n,j+n) for j in range(n)]
    return place(mesh(label, v, f, role, bevel), loc, rot)


def lathe(label, profile, role, n=20, loc=(0,0,0), rot=(0,0,0), bands=None):
    """Closed section: outer wall, lip, inner wall; no filled fake openings."""
    v = [(r*math.cos(TAU*j/n), r*math.sin(TAU*j/n), z)
         for r,z in profile for j in range(n)]
    f = [(k*n+j,k*n+(j+1)%n,((k+1)%len(profile))*n+(j+1)%n,
          ((k+1)%len(profile))*n+j) for k in range(len(profile)) for j in range(n)]
    obj = place(mesh(label, v, f, role), loc, rot)
    if bands:
        for k, name in bands.items():
            if MATS[name] not in list(obj.data.materials): obj.data.materials.append(MATS[name])
            idx = list(obj.data.materials).index(MATS[name])
            for p in obj.data.polygons[k*n:(k+1)*n]: p.material_index = idx
    return obj


def ring(label, loc, radius, width, depth, role, rot=(0,0,0), n=16):
    return lathe(label, [(radius-width,-depth/2),(radius,-depth/2),
                         (radius,depth/2),(radius-width,depth/2)],role,n,loc,rot)


def solid(label, outline, depth, role, y=0, bevel=True, back='steel'):
    """Extruded X/Z silhouette, including concave jaws and curved blades."""
    n = len(outline)
    v = [(x, yy, z) for yy in (y-depth/2,y+depth/2) for x,z in outline]
    f = [tuple(reversed(range(n))),tuple(range(n,2*n))]
    f += [(j,(j+1)%n,(j+1)%n+n,j+n) for j in range(n)]
    obj = mesh(label,v,f,role,bevel)
    if back and back != role:
        obj.data.materials.append(MATS[back])
        obj.data.polygons[0].material_index = 1
    return obj


def frame(label, outer, inner, depth, role, y=0):
    """Matched contours make an actual through-cutout, with bevelled edges."""
    n = len(outer)
    assert n == len(inner)
    v = [(x,yy,z) for yy,curve in ((y-depth/2,outer),(y+depth/2,outer),
                                  (y+depth/2,inner),(y-depth/2,inner)) for x,z in curve]
    f = [(k*n+j,k*n+(j+1)%n,((k+1)%4)*n+(j+1)%n,((k+1)%4)*n+j)
         for k in range(4) for j in range(n)]
    obj = mesh(label,v,f,role,True)
    obj.data.materials.append(MATS['steel'])
    for p in obj.data.polygons[3*n:]: p.material_index = 1
    return obj


def gear(label, loc, radius, hole, depth, role, teeth=10):
    outer,inner = [],[]
    for j in range(teeth*4):
        a = TAU*j/(teeth*4)
        r = radius if j%4 in (1,2) else radius*.81
        outer.append((loc[0]+r*math.sin(a),loc[2]+r*math.cos(a)))
        inner.append((loc[0]+hole*math.sin(a),loc[2]+hole*math.cos(a)))
    # Small gear teeth stay crisp; beveling every tooth wastes the triangle budget.
    n=len(outer)
    v=[(x,y,z) for y,curve in ((loc[1]-depth/2,outer),(loc[1]+depth/2,outer),
                              (loc[1]+depth/2,inner),(loc[1]-depth/2,inner)) for x,z in curve]
    f=[(k*n+j,k*n+(j+1)%n,((k+1)%4)*n+(j+1)%n,((k+1)%4)*n+j)
       for k in range(4) for j in range(n)]
    return mesh(label,v,f,role)


def tube(label, points, radius, role, n=6):
    v=[]
    for i,p in enumerate(points):
        t=(Vector(points[min(i+1,len(points)-1)])-Vector(points[max(0,i-1)])).normalized()
        ref=Vector((0,1,0)) if abs(t.y)<.9 else Vector((1,0,0))
        u=ref.cross(t).normalized(); w=t.cross(u)
        v += [tuple(Vector(p)+radius*(math.cos(TAU*j/n)*u+math.sin(TAU*j/n)*w)) for j in range(n)]
    f=[tuple(reversed(range(n))),tuple(range((len(points)-1)*n,len(points)*n))]
    f += [(i*n+j,i*n+(j+1)%n,(i+1)*n+(j+1)%n,(i+1)*n+j)
          for i in range(len(points)-1) for j in range(n)]
    return mesh(label,v,f,role)


def grip(role='wood_dark', band='brass'):
    cylinder('Fist grip: radius .045 length .18', (0,0,0), .045,.18,role,n=12)
    for z in (-.067,.067):
        cylinder('Grip end band',(0,0,z),.052,.04,band,n=12)


def ticks(loc, radius, count=12, length=.055, width=.032):
    x,y,z=loc
    for j in range(count):
        a=TAU*j/count
        box('Raised hour index',(x+radius*math.sin(a),y,z+radius*math.cos(a)),
            (width,.032,length if j%3 else length*1.3),'dial_mark',rot=(0,a,0),bevel=False)


def pin(loc, radius=.025, role='brass'):
    cylinder('Broad clock rivet',loc,radius,.035,role,n=8,rot=FRONT)


def rifle():
    grip()
    # Offset barrel leaves the cylindrical hand grip clear at the origin.
    stock=[(-.047,-.09),(.047,-.09),(.105,-.27),(.09,-.40),
           (-.12,-.40),(-.13,-.30),(-.075,-.15)]
    solid('Carved musket butt',stock,.13,'wood_dark',back='wood_dark')
    box('Butt plate',(-.015,0,-.376),(.23,.16,.048),'brass')
    solid('Stock cheek inlay',[(-.067,-.17),(.023,-.17),(.060,-.30),(-.08,-.30)],
          .032,'steel',y=.077)
    box('Walnut fore-end',(-.025,0,.33),(.15,.13,.49),'wood_dark')
    box('Fore-end front inset',(-.026,.073,.34),(.072,.035,.30),'steel')
    # Annular barrel gives a real muzzle bore looking along +Z.
    lathe('Long bored steel barrel',[(.055,.18),(.055,.95),(.024,.95),(.024,.18)],
          'steel',16,loc=(.025,-.046,0),bands={1:'silver',2:'iron_dark'})
    for z in (.24,.50,.87): cylinder('Brass barrel band',(.025,-.046,z),.067,.05,'brass')
    box('Barrel breech block',(.025,-.035,.16),(.15,.13,.14),'iron')
    gear('Gear shaped cocking hammer',(-.09,.082,.16),.083,.027,.045,'brass',8)
    tube('Hammer spur',[(-.105,.08,.17),(-.15,.08,.235),(-.13,.08,.27)],.024,'iron')
    ring('Red sling swivel',(-.10,.055,.43),.07,.031,.04,'red_lacquer',FRONT)
    box('Bayonet mounting lug',(.095,-.046,.895),(.09,.08,.065),'iron')
    box('Front sight',(.025,.032,.89),(.04,.05,.07),'silver')
    pin((-.025,.093,-.335))


def bell():
    grip()
    cylinder('Handle bottom button',(0,0,-.085),.052,.03,'brass')
    # Bell flares in +Z. Lip opening is .30 across, exterior .36.
    lathe('Hollow bell, open toward +Z',[(.047,.085),(.070,.11),(.085,.17),
          (.12,.225),(.17,.27),(.17,.30),(.135,.30),(.135,.275),
          (.09,.225),(.055,.17),(.035,.12),(.025,.10)],'copper',24,
          bands={4:'brass',5:'brass',6:'brass',7:'iron',8:'iron',9:'iron',10:'brass'})
    # Inner lip radius .15; revise the inner mouth vertices, without filling it.
    shell=PARTS[-1]
    for v in shell.data.vertices:
        if v.co.z >= .274 and math.hypot(v.co.x,v.co.y) < .14:
            r=math.hypot(v.co.x,v.co.y); v.co.x*=.15/r; v.co.y*=.15/r
    ring('Shoulder molding',(0,0,.132),.079,.03,.035,'brass',n=20)
    for j in range(8):
        a=TAU*j/8
        box('Bell tick band',(.108*math.cos(a),.108*math.sin(a),.216),
            (.035,.035,.045),'dial_mark',rot=(0,0,a),bevel=False)
    tube('Forward visible clapper',[(0,0,.12),(0,.025,.20),(0,.065,.265)],.017,'iron')
    cylinder('Clapper ball',(0,.065,.266),.035,.055,'silver',n=10)
    # A small red collar connects it visually to the dolls.
    cylinder('Red neck collar',(0,0,.09),.053,.035,'red_lacquer')


def hammer():
    grip('wood_dark','brass')
    cylinder('Wood mallet shaft',(0,0,.17),.043,.24,'wood')
    cylinder('Grip butt',(0,0,-.105),.052,.03,'iron')
    cylinder('Lacquer wrap below head',(0,0,.253),.054,.065,'red_lacquer')
    box('Iron mallet head',(0,0,.33),(.18,.10,.10),'iron')
    for x in (-.10,.10):
        box('Brass striking cap',(x,0,.33),(.04,.13,.10),'brass')
        box('Inset striking face',(x*1.24,0,.33),(.035,.085,.075),'silver')
    gear('Clockwork mallet socket',(0,.063,.33),.045,.014,.03,'steel',6)
    pin((0,.085,.33),.018,'brass')


def needle_dagger():
    grip()
    box('Small straight crossguard',(0,0,.13),(.25,.10,.06),'brass')
    solid('Second hand needle',[(-.033,.15),(.033,.15),(.036,.43),
          (0,.53),(-.036,.43)],.045,'silver')
    frame('Hollow diamond second hand',[(0,.75),(.095,.58),(0,.43),(-.095,.58)],
          [(0,.668),(.048,.58),(0,.505),(-.048,.58)],.05,'silver')
    solid('Sharp needle apex',[(0,.75),(.028,.697),(-.028,.697)],.045,'silver',bevel=False)
    box('Dark second hand spine',(0,.027,.315),(.03,.03,.29),'steel',bevel=False)
    # Flattened counterweight disc has its exact centre at -.12.
    disc=cylinder('Red counterweight disc',(0,0,0),.070,.065,'red_lacquer',16,FRONT)
    for v in disc.data.vertices: v.co.z *= .03/.07
    place(disc,(0,0,-.12))
    pin((0,.046,-.12),.027,'iron_dark')
    pin((0,.064,.13),.03,'dial_mark')


def pendulum_scythe():
    cylinder('Long iron staff',(0,0,.60),.045,2.10,'iron',n=12)
    grip('iron','red_lacquer')
    cylinder('Butt ferrule',(0,0,-.4075),.065,.085,'brass')
    for z in (-.30,.30,.72,1.05):
        cylinder('Broad pole ring',(0,0,z),.058,.055,'brass')
    ring('Silver pendulum outer disc',(0,0,1.40),.35,.105,.095,'silver',FRONT,32)
    ring('Inset charcoal dial border',(0,.058,1.40),.302,.035,.035,'steel_dark',FRONT,24)
    ticks((0,.087,1.40),.288,12,.055)
    # Four curved spokes leave four ornate, genuinely open windows.
    for j in range(4):
        a=TAU*j/4
        pts=[]
        for r,twist in ((.045,0),(.13,.30),(.20,.24),(.25,.04)):
            pts.append((r*math.sin(a+twist),0,1.40+r*math.cos(a+twist)))
        tube('Curved pendulum tracery',pts,.023,'brass')
    cylinder('Pendulum central hub',(0,.025,1.40),.084,.13,'iron_dark',12,FRONT)
    cylinder('Red pendulum gem',(0,.105,1.40),.057,.045,'red_lacquer',8,FRONT,True)
    # Broad forged sickle: follows a .9 m sweep toward +X, edge on +Y.
    outer=[(.20,1.63),(.43,1.70),(.67,1.66),(.88,1.54),(1.04,1.34),
           (1.13,1.09),(1.10,.83),(.98,1.11),(.82,1.29),(.62,1.40),(.40,1.44),(.22,1.44)]
    solid('Pendulum scythe curved forge',outer,.09,'steel')
    edge=[(.22,1.44),(.40,1.44),(.62,1.40),(.82,1.29),(.98,1.11),(1.10,.83),
          (1.035,1.13),(.86,1.35),(.64,1.48),(.40,1.52),(.22,1.51)]
    solid('Bright continuous cutting bevel',edge,.035,'silver',y=.063,bevel=False)
    for x,z in ((.38,1.58),(.60,1.56),(.79,1.47)): pin((x,.065,z))


def wrench():
    cylinder('Iron tool core',(0,0,.02),.044,.48,'iron',12)
    grip('iron','brass')
    cylinder('Red rubber grip sleeve',(0,0,0),.052,.16,'red_lacquer',12)
    for z in (-.065,.065): cylinder('Grip sleeve end cuff',(0,0,z),.056,.035,'iron_dark',12)
    cylinder('Wrench butt',(0,0,-.225),.070,.05,'steel')
    ring('Butt hanging eye',(0,.035,-.187),.049,.03,.04,'brass',FRONT,12)
    outline=[(-.07,.09),(.07,.09),(.082,.52),(.18,.61),(.18,.78),
             (.12,.86),(.12,.95),(.025,.95),(.025,.76),(-.10,.76),
             (-.10,.95),(-.205,.95),(-.24,.83),(-.24,.69),(-.15,.56),(-.09,.50)]
    solid('Forged open jaw wrench',outline,.12,'steel')
    box('Long recessed handle panel',(0,.069,.33),(.075,.032,.35),'iron_dark')
    box('Handle panel silver spine',(0,.089,.33),(.032,.03,.25),'silver',bevel=False)
    box('Sliding jaw carriage',(-.053,.015,.69),(.20,.155,.095),'brass')
    box('Movable silver jaw',(-.106,0,.835),(.072,.14,.23),'silver')
    cylinder('Adjuster screw',(-.048,.108,.611),.048,.15,'iron_dark',12,(0,math.pi/2,0))
    for x in (-.107,-.067,-.027,.013):
        cylinder('Coarse adjustment threads',(x,.108,.611),.055,.023,'silver',10,(0,math.pi/2,0))
    # Integrated rear rack with three visible negative notches between teeth.
    for z in (.38,.46,.54): box('Back gear rack tooth',(-.105,0,z),(.07,.11,.042),'iron')
    pin((.105,.083,.72),.028,'dial_mark')


def greatsword():
    grip()
    cylinder('Long grip extension',(0,0,-.17),.044,.18,'wood_dark')
    for z in (-.18,-.10): cylinder('Grip winding band',(0,0,z),.051,.035,'brass')
    # Centre -.30, lower finial -.45, tip +2.35: exact 2.80 m envelope.
    frame('Pommel gem mounting',[(0,-.15),(.12,-.30),(0,-.45),(-.12,-.30)],
          [(0,-.215),(.064,-.30),(0,-.385),(-.064,-.30)],.09,'brass')
    solid('Pommel lower finial',[(0,-.45),(.034,-.393),(-.034,-.393)],.08,'brass',bevel=False)
    solid('Red pommel gem',[(0,-.20),(.08,-.30),(0,-.40),(-.08,-.30)],.115,'red_lacquer',back='red_lacquer')
    solid('Hour hand broad blade',[(-.10,.19),(.10,.19),(.18,.44),(.12,1.35),
          (.20,1.57),(0,1.80),(-.20,1.57),(-.12,1.35),(-.18,.44)],.09,'silver')
    solid('Recessed dark steel fuller',[(-.055,.31),(.055,.31),(.072,.50),
          (.045,1.37),(0,1.60),(-.045,1.37),(-.072,.50)],.032,'steel',y=.055,bevel=False)
    # Spade lobes taper into a sharp clock-hand point; inset follows the same
    # contour, leaving a large through-cutout rather than a painted black inset.
    outer=[(0,2.35),(.12,2.13),(.265,1.98),(.31,1.84),(.27,1.70),
           (.17,1.65),(.07,1.72),(0,1.58),(-.07,1.72),(-.17,1.65),
           (-.27,1.70),(-.31,1.84),(-.265,1.98),(-.12,2.13)]
    inner=[(x*.69,1.89+(z-1.89)*.66) for x,z in outer]
    frame('Open ornate spade tip',outer,inner,.085,'silver')
    solid('Sharp hour hand apex',[(0,2.35),(.045,2.265),(-.045,2.265)],.075,'silver',bevel=False)
    solid('Swept brass crossguard',[(-.43,.19),(-.35,.12),(-.15,.18),(0,.16),
          (.15,.18),(.35,.12),(.43,.19),(.32,.27),(.13,.25),(-.13,.25),(-.32,.27)],.12,'brass')
    gear('Crossguard small clock rim',(0,.015,.22),.19,.12,.14,'brass',10)
    cylinder('Crossguard ivory dial',(0,.092,.22),.133,.045,'dial_face',24,FRONT)
    ticks((0,.132,.22),.097,8,.037,.03)
    tube('Hour dial hands',[(0,.155,.29),(0,.155,.22),(.055,.155,.19)],.016,'dial_mark',4)
    pin((0,.166,.22),.022,'red_lacquer')
    for s in (-1,1): pin((s*.32,.075,.20),.028,'dial_mark')


def rapier():
    grip('wood_dark','brass')
    cylinder('Pommel stem',(0,0,-.12),.038,.10,'brass')
    ring('Red ribbon pommel ring',(0,0,-.19),.06,.03,.045,'red_lacquer',FRONT,16)
    # Shallow hollow cup, opening toward the fist (-Z).
    lathe('Swept hollow iron cup',[(.15,.09),(.15,.13),(.125,.19),(.07,.23),
          (.027,.24),(.027,.20),(.045,.19),(.09,.16),(.115,.12),(.115,.09)],
          'iron_dark',20,bands={1:'steel',2:'steel',3:'brass',4:'brass'})
    gear('Front cup gear rim',(0,.117,.135),.133,.087,.045,'brass',10)
    # A graceful knuckle bow along one side, clear of the grip cylinder.
    tube('Swept knuckle guard',[(.09,0,.15),(.18,0,.10),(.19,0,-.025),
          (.14,0,-.12),(.035,0,-.15)],.023,'iron_dark')
    solid('Minute hand slender blade',[(-.024,.22),(.024,.22),(.020,1.27),
          (0,1.34),(-.020,1.27)],.04,'silver',bevel=False)
    solid('Minute hand arrow point',[(0,1.45),(.072,1.25),(0,1.29),(-.072,1.25)],
          .045,'silver',bevel=False)
    box('Minute hand inset',(0,.027,.73),(.03,.03,.64),'steel',bevel=False)
    pin((0,.155,.135),.025,'red_lacquer')


SPECS = [
    ('Rifle',rifle,-.40,.95), ('Bell',bell,-.10,.30),
    ('Hammer',hammer,-.12,.38), ('NeedleDagger',needle_dagger,-.15,.75),
    ('PendulumScythe',pendulum_scythe,-.45,1.75), ('Wrench',wrench,-.25,.95),
    ('Greatsword',greatsword,-.45,2.35), ('Rapier',rapier,-.25,1.45),
]


def triangles(obj):
    obj.data.calc_loop_triangles()
    return len(obj.data.loop_triangles)


def bounds(obj):
    return [Vector(tuple(fn(v.co[i] for v in obj.data.vertices) for i in range(3))) for fn in (min,max)]


def finish(name, low_z, high_z):
    bpy.ops.object.select_all(action='DESELECT')
    for obj in PARTS:
        obj.select_set(True)
        bpy.context.view_layer.objects.active=obj
        # Silhouette prisms may have either contour winding. Correct it BEFORE
        # beveling: inward normals make a bevel grow over the surface insets.
        bm=bmesh.new(); bm.from_mesh(obj.data)
        bmesh.ops.recalc_face_normals(bm,faces=list(bm.faces))
        bm.to_mesh(obj.data); bm.free(); obj.data.update()
        for mod in list(obj.modifiers): bpy.ops.object.modifier_apply(modifier=mod.name)
        obj.select_set(False)
    for obj in PARTS: obj.select_set(True)
    bpy.context.view_layer.objects.active=PARTS[0]
    bpy.ops.object.join()
    obj=bpy.context.object
    obj.name=name; obj.data.name=name+'_Mesh'
    # Do not centre bounds or fit dimensions: either would move the hand anchor.
    assert obj.location.length < 1e-7
    if triangles(obj)>2980:
        mod=obj.modifiers.new('Silhouette and material preserving budget','DECIMATE')
        mod.ratio=2880/triangles(obj); mod.delimit={'MATERIAL'}
        bpy.ops.object.modifier_apply(modifier=mod.name)
    bm=bmesh.new(); bm.from_mesh(obj.data)
    bmesh.ops.recalc_face_normals(bm,faces=list(bm.faces))
    assert not any(e.is_boundary or not e.is_manifold for e in bm.edges),name+' open mesh'
    bm.to_mesh(obj.data); bm.free()
    for p in obj.data.polygons: p.use_smooth=False
    obj.data.update(); bpy.context.view_layer.update()
    lo,hi=bounds(obj)
    assert abs(lo.z-low_z)<.0001 and abs(hi.z-high_z)<.0001,(name,lo.z,hi.z)
    assert triangles(obj)<=3000,(name,triangles(obj))
    used={obj.data.materials[p.material_index].name for p in obj.data.polygons}
    assert len(used)>=4,(name,used)
    areas={}
    for p in obj.data.polygons:
        role=obj.data.materials[p.material_index].name
        areas[role]=areas.get(role,0)+p.area
    assert max(areas.values())/sum(areas.values())<=.55,(name,areas)
    assert all(math.isfinite(c) for v in obj.data.vertices for c in v.co)
    for mat in obj.data.materials:
        assert mat.name in PALETTE
        assert tuple(mat.diffuse_color)==(.5,.5,.5,1)
        assert tuple(mat.node_tree.nodes['Principled BSDF'].inputs['Base Color'].default_value)==(.5,.5,.5,1)
    obj['grip_centre_m']=[0.0,0.0,0.0]
    obj['grip_radius_m']=.045; obj['grip_length_m']=.18
    obj['long_axis']='+Z'; obj['front']='+Y'; obj['triangle_count']=triangles(obj)
    dest=OUTPUT/f'Kit_W_{name}.fbx'
    bpy.ops.export_scene.fbx(filepath=str(dest),use_selection=True,
        axis_forward='-Z',axis_up='Y',apply_scale_options='FBX_SCALE_UNITS',
        bake_space_transform=True,object_types={'MESH'},use_mesh_modifiers=True,
        mesh_smooth_type='FACE',add_leaf_bones=False,bake_anim=False,use_custom_props=True)
    assert dest.is_file() and dest.stat().st_size>1024
    print(f'EXPORTED {name} {triangles(obj)} {tuple(round(d,4) for d in obj.dimensions)}',flush=True)
    return obj


def preview(objects):
    scene=bpy.context.scene
    scene.render.engine='BLENDER_WORKBENCH'
    scene.render.resolution_x,scene.render.resolution_y=1800,1000
    scene.render.resolution_percentage=100
    scene.render.image_settings.file_format='PNG'
    scene.render.filepath=str(PREVIEW)
    scene.render.film_transparent=False
    scene.view_settings.view_transform='Standard'
    shade=scene.display.shading
    shade.light='FLAT'; shade.color_type='MATERIAL'
    shade.show_shadows=False; shade.show_cavity=True; shade.cavity_type='SCREEN'
    # World cavity's default radius washes out these centimetre-scale insets.
    shade.curvature_ridge_factor=1.1; shade.curvature_valley_factor=1.1
    shade.show_specular_highlight=False
    shade.background_type='WORLD'
    scene.world=bpy.data.worlds.new('Neutral review background')
    scene.world.color=(.085,.09,.105)
    helpers=[]
    try:
        for role,mat in MATS.items(): mat.diffuse_color=(*PALETTE[role],1)
        # Front camera +Y: screen-right is -X. All grip centres share Z=0.
        screen_positions=[-2.78,-2.05,-1.40,-.76,.48,1.39,2.24,3.05]
        for obj,x in zip(objects,screen_positions): obj.location=(-x,0,0)
        # The scythe blade points local +X (screen-left); reserve that gap.
        cam=bpy.data.objects.new('Review camera',bpy.data.cameras.new('Review camera'))
        scene.collection.objects.link(cam); helpers.append(cam); scene.camera=cam
        target=Vector((-.08,0,.90))
        cam.location=target+Vector((0,14,3.0))
        cam.rotation_euler=(target-cam.location).to_track_quat('-Z','Y').to_euler()
        cam.data.type='ORTHO'; cam.data.ortho_scale=7.3
        toward_camera=(cam.location-target).normalized()

        def caption(body, x, z, size=.085, role='silver', align='CENTER'):
            data=bpy.data.curves.new('Preview text','FONT')
            data.body=body; data.size=size; data.align_x=align
            data.materials.append(MATS[role])
            label=bpy.data.objects.new('Preview caption',data)
            scene.collection.objects.link(label); helpers.append(label)
            label.location=(-x,.32,z); label.rotation_euler=cam.rotation_euler

        for obj,x,(_,_,low,high) in zip(objects,screen_positions,SPECS):
            # Cross sits just in front of each grip, at its projected true origin.
            for size in ((.105,.018,.016),(.016,.018,.105)):
                cross=box('PREVIEW ONLY grip origin',Vector((-x,0,0))+toward_camera*.22,
                          size,'red_lacquer',bevel=False)
                helpers.append(cross)
            title={'NeedleDagger':'NEEDLE\nDAGGER','PendulumScythe':'PENDULUM\nSCYTHE'}.get(obj.name,obj.name.upper())
            caption(title,x,-.61,.083)
            caption(f'{high-low:.2f} m  /  {triangles(obj):,} tri',x,-.83,.053,'brass')
        caption('C H A P T E R   I V   /   C L O C K W O R K   A R M O U R Y',0,2.73,.125)
        caption('HAND-HELD WEAPONS   |   TRUE SCALE   |   +Z WORKING END',0,2.53,.063,'brass')
        caption('+   GRIP ORIGINS   /   COMMON HAND HEIGHT',0,-1.02,.065,'red_lacquer')
        PREVIEW.parent.mkdir(parents=True,exist_ok=True)
        bpy.ops.render.render(write_still=True)
        assert PREVIEW.is_file() and PREVIEW.stat().st_size>1024
        print(f'PREVIEW {PREVIEW}',flush=True)
    finally:
        scene.camera=None
        for obj in helpers: bpy.data.objects.remove(obj,do_unlink=True)
        for obj in objects: obj.location=(0,0,0)
        for mat in MATS.values(): mat.diffuse_color=(.5,.5,.5,1)


def main():
    global PARTS
    bpy.ops.wm.read_factory_settings(use_empty=True)
    for mat in list(bpy.data.materials): bpy.data.materials.remove(mat)
    scene=bpy.context.scene
    scene.unit_settings.system='METRIC'; scene.unit_settings.scale_length=1
    materials(); OUTPUT.mkdir(parents=True,exist_ok=True)
    objects=[]
    for i,(name,build,lo,hi) in enumerate(SPECS):
        PARTS=[]; random.seed(44081+i*7919)
        build(); objects.append(finish(name,lo,hi))
    assert len(scene.objects)==8 and all(o.type=='MESH' for o in scene.objects)
    preview(objects)
    assert len(scene.objects)==8 and all(o.type=='MESH' for o in scene.objects)
    print('DONE',flush=True)


if __name__=='__main__':
    main()
