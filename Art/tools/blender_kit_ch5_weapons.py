# 5장 시간 감옥 무기 7종: 손잡이 중심 원점, 개별 FBX와 검수 미리보기 생성.
# 실행: "C:/Program Files/Blender Foundation/Blender 5.3/blender.exe" -b --factory-startup -P Art/tools/blender_kit_ch5_weapons.py
"""Blender 5.x; metres, grip centre zero, working end +Z, front +Y.

Self-contained procedural geometry. Export materials are neutral grey; review
colours, captions, camera and red origin crosses exist only AFTER exporting.
The pistol pommel is centred at -.10, with its cap reaching -.12: .42 overall.
Detached fragments remain closed mesh islands inside their weapon's ONE mesh.
"""
import math
import random
from pathlib import Path

import bmesh
import bpy
from mathutils import Euler, Matrix, Vector

ROOT = Path(__file__).resolve().parents[2]
OUTPUT = ROOT / 'Game/Assets/Art/Characters/Ch5'
PREVIEW = ROOT / 'Art/tools/specs/blender_kit_ch5_weapons_preview.png'
TAU = math.tau
FRONT = (math.pi / 2, 0, 0)
MATS, PARTS = {}, []
PALETTE = {
    'void_stone': (.19, .125, .32), 'void_stone_dark': (.07, .045, .12),
    'marble': (.78, .72, .90), 'marble_dark': (.38, .29, .51),
    'violet': (.40, .19, .64), 'violet_dark': (.16, .055, .28),
    'indigo': (.085, .09, .29), 'night_sky': (.025, .025, .095),
    'starlight': (.92, .94, 1.0), 'crack_gold': (1.0, .76, .24),
    'gold': (.88, .53, .13), 'gold_dark': (.39, .23, .075),
    'mirror': (.57, .79, .91), 'mirror_back': (.17, .29, .45),
    'crystal': (.55, .35, .90), 'magenta': (.92, .035, .45),
    'wood_dark': (.10, .065, .13), 'iron_dark': (.045, .04, .075),
    'dial_face': (.88, .84, .69), 'dial_mark': (.055, .04, .10),
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
        mod = obj.modifiers.new('Chunky edge chamfer', 'BEVEL')
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


def cylinder(label, loc, radius, depth, role, n=12, rot=(0,0,0), bevel=False):
    v = [(radius*math.cos(TAU*j/n), radius*math.sin(TAU*j/n), z)
         for z in (-depth/2,depth/2) for j in range(n)]
    f = [tuple(reversed(range(n))),tuple(range(n,2*n))]
    f += [(j,(j+1)%n,(j+1)%n+n,j+n) for j in range(n)]
    return place(mesh(label,v,f,role,bevel),loc,rot)


def lathe(label, profile, role, n=16, loc=(0,0,0), rot=(0,0,0)):
    """Closed annular profile; true open rings rather than filled discs."""
    v = [(r*math.cos(TAU*j/n),r*math.sin(TAU*j/n),z)
         for r,z in profile for j in range(n)]
    f = [(k*n+j,k*n+(j+1)%n,((k+1)%len(profile))*n+(j+1)%n,
          ((k+1)%len(profile))*n+j) for k in range(len(profile)) for j in range(n)]
    return place(mesh(label,v,f,role),loc,rot)


def ring(label, loc, radius, width, depth, role='gold', rot=(0,0,0), n=16):
    return lathe(label,[(radius-width,-depth/2),(radius,-depth/2),
                        (radius,depth/2),(radius-width,depth/2)],role,n,loc,rot)


def solid(label, outline, depth, role, y=0, bevel=True, back=None):
    """Closed extruded X/Z polygon; front (+Y) and back can use distinct roles."""
    n = len(outline)
    v = [(x,yy,z) for yy in (y-depth/2,y+depth/2) for x,z in outline]
    f = [tuple(reversed(range(n))),tuple(range(n,2*n))]
    f += [(j,(j+1)%n,(j+1)%n+n,j+n) for j in range(n)]
    obj = mesh(label,v,f,role,bevel)
    if back and back != role:
        obj.data.materials.append(MATS[back])
        obj.data.polygons[0].material_index = 1
        for poly in obj.data.polygons[2:]: poly.material_index = 1
    return obj


def tube(label, points, radius, role, n=6):
    """Faceted solid bars, including deliberately angular lightning inlays."""
    v = []
    for i,p in enumerate(points):
        t = (Vector(points[min(i+1,len(points)-1)])-
             Vector(points[max(0,i-1)])).normalized()
        ref = Vector((0,1,0)) if abs(t.y)<.9 else Vector((1,0,0))
        u = ref.cross(t).normalized()
        w = t.cross(u)
        v += [tuple(Vector(p)+radius*(math.cos(TAU*j/n)*u+math.sin(TAU*j/n)*w))
              for j in range(n)]
    f = [tuple(reversed(range(n))),tuple(range((len(points)-1)*n,len(points)*n))]
    f += [(i*n+j,i*n+(j+1)%n,(i+1)*n+(j+1)%n,(i+1)*n+j)
          for i in range(len(points)-1) for j in range(n)]
    return mesh(label,v,f,role)


def gem(label, loc, radius, length, role, n=6):
    """Two sharp ends and two broad rings: readable low-poly crystal."""
    v = [(0,0,-length/2)]
    v += [(radius*math.cos(TAU*j/n),radius*math.sin(TAU*j/n),z)
          for z in (-length*.19,length*.19) for j in range(n)]
    v += [(0,0,length/2)]
    f = [(0,1+(j+1)%n,1+j) for j in range(n)]
    f += [(1+j,1+(j+1)%n,1+n+(j+1)%n,1+n+j) for j in range(n)]
    f += [(1+n+j,1+n+(j+1)%n,1+2*n) for j in range(n)]
    return place(mesh(label,v,f,role),loc)


def orb(label, loc, radius, role='crystal'):
    # An octagonal bipyramid with a broad equator, not a micro-detail sphere.
    v = [(0,0,-radius),(0,0,radius)]
    v += [(radius*math.cos(TAU*j/8),radius*math.sin(TAU*j/8),0) for j in range(8)]
    f = [(0,2+(j+1)%8,2+j) for j in range(8)]
    f += [(1,2+j,2+(j+1)%8) for j in range(8)]
    return place(mesh(label,v,f,role),loc)


def star(label, loc, radius, role, depth=.045):
    outline = []
    for j in range(10):
        a = TAU*j/10
        r = radius if j%2==0 else radius*.47
        outline.append((loc[0]+r*math.sin(a),loc[2]+r*math.cos(a)))
    return solid(label,outline,depth,role,y=loc[1],bevel=False)


def grip(band='gold'):
    cylinder('Fist grip .045 radius .18 length',(0,0,0),.045,.18,'iron_dark')
    for z in (-.068,.068):
        cylinder('Grip boundary band',(0,0,z),.052,.04,band)


def pin(loc, radius=.026, role='gold'):
    return cylinder('Broad rivet',loc,radius,.035,role,n=8,rot=FRONT)


def chain(label, x, y, top, count=2, radius=.044):
    for i in range(count):
        # Overlap links spatially but retain each actual through-opening.
        ring(label,(x,y,top-i*.064),radius,.03,.035,'gold',
             rot=(math.pi/2,0,(i%2)*math.pi/3),n=12)


def shard_rifle():
    grip()
    stock = [(-.047,-.09),(.047,-.09),(.12,-.26),(.11,-.40),
             (-.13,-.40),(-.14,-.28),(-.075,-.15)]
    solid('Violet fractured rifle stock',stock,.15,'violet_dark')
    box('Gold butt plate',(-.015,0,-.38),(.25,.17,.04),'gold')
    solid('Stock mirror inset',[(-.07,-.19),(.02,-.20),(.065,-.33),(-.09,-.33)],
          .035,'mirror',y=.083,back='mirror_back')
    tube('Stock gold fracture',[(-.08,.108,-.19),(-.025,.108,-.24),
         (-.055,.108,-.29),(.025,.108,-.35)],.02,'crack_gold',4)
    box('Violet receiver',(0,-.045,.175),(.18,.17,.17),'violet_dark')
    box('Breech mirror plate',(0,.052,.18),(.12,.035,.11),'mirror')
    # Exactly three long angular glass shards. Their staggered points share
    # the +Z firing direction; only the longest reaches the .90 muzzle plane.
    for x,y,top,w in ((-.09,-.045,.86,.065),(.09,-.045,.875,.065),(0,.025,.90,.056)):
        outline = [(x-w/2,.235),(x+w/2,.24),(x+w*.40,top-.09),
                   (x+.012,top),(x-w*.42,top-.045)]
        solid('One of three mirror barrel shards',outline,.08,'mirror',y=y,
              bevel=False,back='mirror_back')
    for z in (.29,.53,.765):
        ring('Open gold shard binding',(0,-.03,z),.133,.035,.045,n=16)
    box('Ammo cell mount',(.135,0,.225),(.09,.11,.14),'gold_dark')
    gem('Magenta crystal ammo cell',(.182,.045,.245),.073,.22,'magenta')
    box('Ammo gold socket',(.182,.045,.16),(.13,.12,.04),'gold')
    box('Star sight riser',(0,.125,.68),(.045,.13,.045),'gold')
    star('Gold five point sight',(0,.20,.68),.07,'gold',.04)
    pin((-.068,.065,.17))


def star_staff():
    cylinder('Indigo staff',(0,0,.235),.039,1.13,'indigo')
    grip('magenta')
    cylinder('Staff butt ferrule',(0,0,-.325),.06,.05,'gold')
    for z in (-.23,.24,.49,.65):
        cylinder('Staff ring band',(0,0,z),.055,.045,'gold')
    gem('Magenta head socket',(0,0,.752),.082,.19,'magenta')
    cylinder('Armillary plinth',(0,0,.82),.075,.045,'gold_dark')
    centre = (0,0,1.025)
    # Both diameter .45; the rotated ring supplies depth without closing the
    # open windows around the miniature constellation.
    ring('Front armillary ring',centre,.225,.035,.04,rot=FRONT,n=24)
    ring('Crossed armillary ring',centre,.225,.035,.04,
         rot=(math.pi/2,0,math.radians(70)),n=24)
    for p in ((-.14,.025,1.10),(.14,.025,1.09),(.025,.025,.875)):
        tube('Straight constellation connector',[(0,.025,1.025),p],.019,'gold',6)
        orb('Constellation crystal orb',p,.047)
    star('Chunky central starlight star',(0,.075,1.025),.112,'starlight',.055)
    pin((0,.116,1.025),.028,'magenta')


def ruin_maul():
    cylinder('Iron two handed haft',(0,0,.145),.041,.95,'iron_dark')
    grip()
    cylinder('Heavy gold pommel',(0,0,-.32),.073,.06,'gold')
    cylinder('Second iron grip',(0,0,.32),.048,.22,'iron_dark')
    for z in (-.22,.205,.435,.565):
        cylinder('Maul haft cuff',(0,0,z),.06,.045,'gold')
    for z in (-.18,.28,.39,.52): pin((0,.054,z),.025)
    # Together the two interlocking blocks fill a .70 x .45 x .45 envelope.
    # The narrow zigzag gap exposes a gold seam instead of a painted texture.
    left = [(-.35,.615),(-.065,.615),(-.10,.74),(-.035,.84),
            (-.11,.93),(-.07,1.065),(-.30,1.065),(-.35,1.01)]
    right = [(-.02,.615),(.35,.615),(.35,1.005),(.29,1.065),
             (-.025,1.065),(-.065,.93),(.01,.84),(-.055,.74)]
    solid('Broken left obsidian block',left,.45,'void_stone',back='void_stone_dark')
    solid('Broken right obsidian block',right,.45,'void_stone',back='void_stone_dark')
    tube('Gold through the head fracture',[(-.043,.205,.63),(-.078,.205,.74),
         (-.012,.205,.84),(-.088,.205,.93),(-.047,.205,1.045)],.025,'crack_gold',4)
    tube('Branching head fracture',[(-.014,.233,.84),(.095,.233,.89),
         (.16,.233,.845),(.30,.233,.88)],.021,'crack_gold',4)
    for x in (-.25,.25):
        for y in (-.235,.235):
            box('Gold head binding front and rear',(x,y,.84),(.065,.04,.45),'gold')
        for z in (.635,1.045):
            box('Gold head binding top and bottom',(x,0,z),(.065,.49,.04),'gold')
    # Stacked broken architectural capital embedded in the +Y striking face.
    box('Marble capital abacus',(.13,.292,.96),(.30,.15,.065),'marble')
    box('Marble capital echinus',(.13,.292,.897),(.24,.14,.065),'marble_dark')
    box('Marble capital broken column',(.13,.28,.80),(.19,.13,.14),'marble')
    box('Marble capital bottom molding',(.13,.28,.72),(.24,.14,.04),'marble')
    for x in (.075,.185):
        box('Broad capital fluting',(x,.355,.80),(.035,.035,.12),'marble_dark',bevel=False)
    # Two visibly detached fragments, still included in the joined mesh.
    gem('Hovering top pebble A',(-.20,-.025,1.12),.052,.06,'void_stone',5)
    gem('Hovering top pebble B',(.17,-.065,1.12),.042,.06,'marble',5)


def rift_blade():
    grip()
    # Interrupted violet wraps leave the central iron contact visible.
    for z in (-.038,.038):
        ring('Violet handle wrap',(0,0,z),.048,.032,.03,'violet_dark',n=12)
    gem('Magenta pommel gem',(0,0,-.115),.06,.07,'magenta')
    crescent = [(-.19,.145),(-.15,.075),(-.06,.10),(0,.115),(.08,.10),
                (.18,.085),(.215,.16),(.14,.135),(.065,.15),(-.065,.15),(-.13,.125)]
    solid('Small gold crescent guard',crescent,.085,'gold')
    blade = [(-.065,.165),(.075,.165),(.16,.28),(.22,.46),(.22,.61),
             (.13,.70),(.135,.51),(.075,.36),(-.02,.265)]
    solid('Curved mirror shard blade',blade,.07,'mirror',bevel=False,back='mirror_back')
    tube('Jagged blade crack',[(-.006,.044,.205),(.075,.044,.29),
         (.055,.044,.365),(.145,.044,.45),(.126,.044,.535),(.16,.044,.61)],
         .017,'crack_gold',4)
    solid('Broad reflected blade facet',[(.035,.25),(.06,.31),(.095,.34),(.085,.28)],
          .032,'starlight',y=.043,bevel=False)


def rift_censer():
    cylinder('Violet censer upper staff',(0,-.055,.555),.039,.72,'violet_dark')
    # Return the lower staff to the fist centre; no off-axis hand anchor.
    tube('Staff transition below the hook',[(0,0,.09),(0,-.055,.23),(0,-.055,.90)],
         .039,'violet_dark',8)
    cylinder('Censer lower centred shaft',(0,0,-.20),.039,.24,'violet_dark')
    grip('magenta')
    cylinder('Gold censer butt',(0,0,-.325),.06,.05,'gold')
    for z in (-.23,.29,.54,.79):
        cylinder('Gold staff band',(0,-.055 if z>0 else 0,z),.054,.045,'gold')
    tube('Swept gold lantern crook',[(0,-.055,.88),(-.065,-.055,1.06),
         (-.025,-.055,1.12),(.055,-.055,1.12),(.12,-.055,1.12),
         (.22,-.055,1.055)],.03,'gold',8)
    chain('Upper suspension chain',.22,-.055,1.05,1,.042)
    # Four corner bars leave a clean front window around the dark rift core.
    cx, cy = .22, -.055
    for z in (.67,.98):
        ring('Lantern octagonal rim',(cx,cy,z),.18,.035,.04,'gold',n=8)
    for dx,dy in ((-.128,-.105),(.128,-.105),(-.128,.105),(.128,.105)):
        tube('Cage vertical gold bar',[(cx+dx,cy+dy,.68),(cx+dx,cy+dy,.97)],.021,'gold',6)
    for dx in (-.12,.12):
        tube('Lantern crown brace',[(cx+dx,cy,.98),(cx,cy,1.025)],.021,'gold_dark',6)
    core = [(cx-.015,.705),(cx-.095,.81),(cx-.052,.845),(cx-.07,.90),
            (cx+.035,.952),(cx+.085,.86),(cx+.05,.815),(cx+.072,.77)]
    solid('Floating jagged night sky rift',core,.085,'night_sky',y=cy,bevel=False)
    # Closed outline of the jagged core, .04 thick, slightly inset in its face.
    tube('Glowing outline of crack crystal',[(x,cy+.049,z) for x,z in core+[core[0]]],
         .02,'crack_gold',4)
    for dx,z in ((-.025,.81),(.015,.88),(.027,.778)):
        solid('Starlight crystal speck',[(cx+dx,z+.021),(cx+dx+.018,z),
              (cx+dx,z-.021),(cx+dx-.018,z)],.032,'starlight',y=cy+.065,bevel=False)
    chain('Short chain under the cage',cx,cy,.623,2,.042)
    gem('Magenta tassel bead',(cx,cy,.47),.067,.13,'magenta')


def hourglass(x, y, z):
    for dz in (-.15,.15):
        cylinder('Hourglass gold end cap',(x,y,z+dz),.125,.04,'gold',n=8)
    for dx,dy in ((-.095,-.065),(.095,-.065),(-.095,.065),(.095,.065)):
        tube('Hourglass frame pillar',[(x+dx,y+dy,z-.13),(x+dx,y+dy,z+.13)],.018,'gold',6)
    # Two closed faceted bulbs meet at the narrow waist, with pale time sand.
    for sign in (-1,1):
        v = [(x,y,z+sign*.012),(x,y,z+sign*.13)]
        v += [(x+.078*math.cos(TAU*j/8),y+.078*math.sin(TAU*j/8),z+sign*.095)
              for j in range(8)]
        f = [(0,2+j,2+(j+1)%8) for j in range(8)]
        f += [(1,2+(j+1)%8,2+j) for j in range(8)]
        mesh('Faceted crystal hourglass bulb',v,f,'crystal')
    gem('White time sand',(x,y+.073,z-.065),.035,.075,'starlight')


def time_scythe():
    cylinder('Long indigo snath',(0,0,.675),.042,2.37,'indigo')
    grip('magenta')
    cylinder('Second iron hand grip',(0,0,.64),.051,.22,'iron_dark')
    for z in (-.43,-.24,.30,.51,.77,1.10,1.43,1.67):
        cylinder('Gold snath ring',(0,0,z),.059,.045,'gold')
    for z in (.565,.715): cylinder('Second grip magenta band',(0,0,z),.056,.035,'magenta')
    gem('Gold spike butt',(0,0,-.525),.082,.15,'gold')
    cylinder('Gold spike socket',(0,0,-.455),.075,.05,'gold_dark')
    # The .75 clock bezel reaches the exact +2.30 envelope.
    zc = 1.925
    cylinder('Clock rear indigo body',(0,-.015,zc),.35,.13,'indigo',n=32,rot=FRONT)
    ring('Large gold clock bezel',(0,.025,zc),.375,.05,.15,rot=FRONT,n=32)
    cylinder('Ivory clock face',(0,.093,zc),.325,.045,'dial_face',n=32,rot=FRONT)
    for j in range(12):
        a = TAU*j/12
        box('Raised clock hour tick',( .271*math.sin(a),.134,zc+.271*math.cos(a)),
            (.035,.035,.066 if j%3 else .083),'dial_mark',rot=(0,a,0),bevel=False)
    tube('Clock minute hand',[(0,.17,zc),(.115,.17,zc+.199)],.019,'dial_mark',4)
    tube('Clock hour hand',[(0,.17,zc),(-.151,.17,zc+.05)],.025,'dial_mark',4)
    pin((0,.197,zc),.047,'gold')
    pin((0,.222,zc),.026,'magenta')
    # A 1.40 m lateral sweep from the root (.15) to its sharp +X tip (1.55).
    blade = [(.15,2.14),(.45,2.24),(.78,2.20),(1.08,2.03),(1.34,1.77),
             (1.50,1.43),(1.55,1.10),(1.40,1.36),(1.20,1.58),(.94,1.76),
             (.65,1.86),(.39,1.87),(.15,1.87)]
    solid('Huge violet swept scythe blade',blade,.12,'violet_dark',back='mirror_back')
    edge = [(.15,1.87),(.39,1.87),(.65,1.86),(.94,1.76),(1.20,1.58),
            (1.40,1.36),(1.55,1.10),(1.44,1.43),(1.235,1.65),(.96,1.83),
            (.66,1.935),(.39,1.945),(.15,1.945)]
    solid('Continuous gold scythe cutting edge',edge,.035,'gold',y=.068,bevel=False)
    tube('Scythe lightning crack',[(.36,.069,2.07),(.56,.069,2.115),
         (.65,.069,2.055),(.82,.069,2.07),(.89,.069,1.98),(1.06,.069,1.925),
         (1.07,.069,1.845),(1.24,.069,1.73),(1.39,.069,1.48)],.021,'crack_gold',4)
    solid('Mirror shard embedded in scythe',[(.44,2.15),(.63,2.16),(.78,2.12),(.62,2.125)],
          .035,'mirror',y=.067,bevel=False)
    for x,z in ((.43,2.015),(.81,1.93),(1.13,1.735)): pin((x,.09,z),.027,'gold_dark')
    tube('Hourglass suspension hook',[(-.24,0,1.68),(-.43,0,1.57)],.022,'gold',6)
    chain('Hourglass chain',-.43,0,1.55,2,.045)
    hourglass(-.43,0,1.27)


def mirror_pistol():
    grip()
    solid('Violet curved pistol grip',[(-.073,.075),(.052,.075),(.075,-.09),
          (.045,-.12),(-.068,-.12),(-.085,-.075)],.085,'violet_dark',y=-.035)
    # The fist contact remains a centred dark cylinder above the violet panel.
    cylinder('Exposed pistol hand contact',(0,0,0),.045,.115,'iron_dark')
    box('Pistol pommel cap',(0,0,-.10),(.14,.13,.04),'gold')
    solid('Silver blue revolver frame',[(-.155,.095),(.035,.095),(.055,.21),
          (-.075,.245),(-.16,.205)],.14,'mirror',back='mirror_back')
    cylinder('Gold revolver cylinder',(-.07,.045,.16),.078,.11,'gold',n=10)
    for a in (.3,1.4,2.5):
        x = -.07+.076*math.cos(a)
        y = .045+.076*math.sin(a)
        tube('Broad cylinder flute',[(x,y,.124),(x,y,.194)],.016,'gold_dark',4)
    # Open muzzle is parallel +Z; dark internal wall and bright square sight.
    lathe('Hollow mirror pistol muzzle',[(.058,.215),(.058,.30),
          (.025,.30),(.025,.215)],'mirror',n=12,loc=(-.075,-.018,0))
    box('Pistol front sight',(-.075,.044,.268),(.035,.055,.045),'gold')
    # Trigger bow is a real open, chunky gold loop beside the cylindrical grip.
    outline = [(.05,.13),(.135,.13),(.185,.08),(.18,-.015),(.105,-.035),(.055,.005)]
    inner = [(.082,.095),(.122,.095),(.148,.067),(.145,.015),(.11,.002),(.087,.025)]
    n = len(outline)
    v = [(x,y,z) for y,curve in ((-.022,outline),(.022,outline),
         (.022,inner),(-.022,inner)) for x,z in curve]
    f = [(k*n+j,k*n+(j+1)%n,((k+1)%4)*n+(j+1)%n,((k+1)%4)*n+j)
         for k in range(4) for j in range(n)]
    mesh('Open gold pistol trigger guard',v,f,'gold',True)
    tube('Pistol trigger',[(.083,.015,.105),(.111,.015,.058)],.016,'gold_dark',4)
    solid('Revolver hammer',[(-.15,.135),(-.205,.175),(-.205,.225),(-.145,.205)],
          .08,'violet_dark')
    gem('Magenta hammer gem',(-.178,.055,.20),.034,.065,'magenta',5)
    tube('Mirror frame gold crack',[(-.15,.082,.19),(-.12,.082,.205),
         (-.098,.082,.177),(-.042,.082,.21)],.017,'crack_gold',4)
    pin((-.125,.09,.125),.023,'mirror_back')


SPECS = [
    ('ShardRifle',shard_rifle,-.40,.90,3500),
    ('StarStaff',star_staff,-.35,1.25,3500),
    ('RuinMaul',ruin_maul,-.35,1.15,3500),
    ('RiftBlade',rift_blade,-.15,.70,3500),
    ('RiftCenser',rift_censer,-.35,1.15,3500),
    ('TimeScythe',time_scythe,-.60,2.30,5000),
    ('MirrorPistol',mirror_pistol,-.12,.30,3500),
]


def triangles(obj):
    obj.data.calc_loop_triangles()
    return len(obj.data.loop_triangles)


def bounds(obj):
    return [Vector(tuple(fn(v.co[i] for v in obj.data.vertices) for i in range(3)))
            for fn in (min,max)]


def recalc(obj):
    bm = bmesh.new()
    bm.from_mesh(obj.data)
    bmesh.ops.recalc_face_normals(bm,faces=list(bm.faces))
    bm.to_mesh(obj.data)
    bm.free()
    obj.data.update()


def finish(name, low_z, high_z, budget):
    bpy.ops.object.select_all(action='DESELECT')
    for part in PARTS:
        part.select_set(True)
        bpy.context.view_layer.objects.active = part
        recalc(part)
        for mod in list(part.modifiers): bpy.ops.object.modifier_apply(modifier=mod.name)
        part.select_set(False)
    for part in PARTS: part.select_set(True)
    bpy.context.view_layer.objects.active = PARTS[0]
    bpy.ops.object.join()
    obj = bpy.context.object
    obj.name, obj.data.name = name, name+'_Mesh'
    assert obj.location.length < 1e-7, name+' grip moved'
    # Chamfers are the only potentially expensive geometry. Delimit materials
    # so an emergency budget reduction cannot merge the gold/mirror boundaries.
    if triangles(obj)>budget:
        mod = obj.modifiers.new('Preserve silhouette and material seams','DECIMATE')
        mod.ratio = (budget-120)/triangles(obj)
        mod.delimit = {'MATERIAL'}
        bpy.ops.object.modifier_apply(modifier=mod.name)
    recalc(obj)
    bm = bmesh.new()
    bm.from_mesh(obj.data)
    assert not any(e.is_boundary or not e.is_manifold for e in bm.edges),name+' open mesh'
    bm.free()
    for p in obj.data.polygons: p.use_smooth = False
    obj.data.update()
    bpy.context.view_layer.update()
    lo, hi = bounds(obj)
    assert abs(lo.z-low_z)<.00015 and abs(hi.z-high_z)<.00015,(name,lo.z,hi.z)
    assert triangles(obj)<=budget,(name,triangles(obj))
    areas = {}
    for p in obj.data.polygons:
        role = obj.data.materials[p.material_index].name
        areas[role] = areas.get(role,0)+p.area
    assert len(areas)>=4,(name,areas)
    assert max(areas.values())/sum(areas.values())<=.55,(name,areas)
    assert all(math.isfinite(c) for v in obj.data.vertices for c in v.co)
    for mat in obj.data.materials:
        assert mat.name in PALETTE
        assert tuple(mat.diffuse_color)==(.5,.5,.5,1)
        assert tuple(mat.node_tree.nodes['Principled BSDF'].inputs['Base Color'].default_value)==(.5,.5,.5,1)
    obj['grip_centre_m'] = [0.0,0.0,0.0]
    obj['grip_radius_m'], obj['grip_length_m'] = .045,.18
    obj['long_axis'], obj['front'] = '+Z','+Y'
    obj['triangle_count'] = triangles(obj)
    dest = OUTPUT/f'Kit_W_{name}.fbx'
    bpy.ops.export_scene.fbx(filepath=str(dest),use_selection=True,
        axis_forward='-Z',axis_up='Y',apply_scale_options='FBX_SCALE_UNITS',
        bake_space_transform=True,object_types={'MESH'},use_mesh_modifiers=True,
        mesh_smooth_type='FACE',add_leaf_bones=False,bake_anim=False,use_custom_props=True)
    assert dest.is_file() and dest.stat().st_size>1024
    print(f'EXPORTED {name} {triangles(obj)} {tuple(round(d,4) for d in obj.dimensions)}',flush=True)
    return obj


def preview(objects):
    scene = bpy.context.scene
    scene.render.engine = 'BLENDER_WORKBENCH'
    scene.render.resolution_x,scene.render.resolution_y = 1800,1000
    scene.render.resolution_percentage = 100
    scene.render.image_settings.file_format = 'PNG'
    scene.render.filepath = str(PREVIEW)
    scene.render.film_transparent = False
    scene.view_settings.view_transform = 'Standard'
    shade = scene.display.shading
    shade.light, shade.color_type = 'FLAT','MATERIAL'
    shade.show_shadows, shade.show_cavity = False,True
    shade.cavity_type = 'SCREEN'
    shade.curvature_ridge_factor, shade.curvature_valley_factor = 1.2,1.1
    shade.show_specular_highlight = False
    shade.background_type = 'WORLD'
    scene.world = bpy.data.worlds.new('Indigo review backdrop')
    scene.world.color = (.035,.04,.065)
    helpers = []
    positions = []
    try:
        for role,mat in MATS.items(): mat.diffuse_color = (*PALETTE[role],1)
        # Seen from +Y, screen-right is -X. Allocate the FULL lateral silhouette
        # of the boss scythe before placing the next weapon. All origins Z=0.
        cursor = 0
        for obj in objects:
            lo,hi = bounds(obj)
            width = max(hi.x-lo.x,.62)
            origin_screen_x = cursor+width/2+(hi.x+lo.x)/2
            positions.append(origin_screen_x)
            cursor += width+.30
        total = cursor-.30
        positions = [x-total/2 for x in positions]
        for obj,x in zip(objects,positions): obj.location = (-x,0,0)
        cam = bpy.data.objects.new('Review camera',bpy.data.cameras.new('Review camera'))
        scene.collection.objects.link(cam)
        helpers.append(cam)
        scene.camera = cam
        target = Vector((0,0,.88))
        cam.location = target+Vector((0,16,3.3))
        cam.rotation_euler = (target-cam.location).to_track_quat('-Z','Y').to_euler()
        cam.data.type, cam.data.ortho_scale = 'ORTHO',max(total+.7,7.05)
        toward_camera = (cam.location-target).normalized()

        def caption(body, x, z, size=.08, role='starlight'):
            data = bpy.data.curves.new('Preview text','FONT')
            data.body, data.size, data.align_x = body,size,'CENTER'
            data.materials.append(MATS[role])
            obj = bpy.data.objects.new('PREVIEW ONLY caption',data)
            scene.collection.objects.link(obj)
            helpers.append(obj)
            obj.location = (-x,.40,z)
            obj.rotation_euler = cam.rotation_euler

        # Preview red gets its own non-exported material, not a game role.
        red = bpy.data.materials.new('PREVIEW ONLY origin red')
        red.diffuse_color = (1,.035,.045,1)
        for obj,x,spec in zip(objects,positions,SPECS):
            for size in ((.105,.018,.016),(.016,.018,.105)):
                cross = box('PREVIEW ONLY grip origin',Vector((-x,0,0))+toward_camera*.40,
                            size,'magenta',bevel=False)
                cross.data.materials.clear()
                cross.data.materials.append(red)
                helpers.append(cross)
            title = {'ShardRifle':'SHARD\nRIFLE','StarStaff':'STAR\nSTAFF',
                     'RuinMaul':'RUIN\nMAUL','RiftBlade':'RIFT\nBLADE',
                     'RiftCenser':'RIFT\nCENSER','TimeScythe':'TIME\nSCYTHE',
                     'MirrorPistol':'MIRROR\nPISTOL'}[obj.name]
            caption(title,x,-.76,.083)
            caption(f'{spec[3]-spec[2]:.2f} m  /  {triangles(obj):,} tri',x,-.98,.049,'gold')
        caption('C H A P T E R   V   /   S H A T T E R E D   T I M E',0,2.71,.125)
        caption('HAND-HELD ARMOURY   /   TRUE SCALE   /   +Z WORKING END',0,2.51,.061,'gold')
        caption('+  RED CROSSES = GRIP ORIGINS   /   COMMON HAND HEIGHT',0,-1.17,.061,'magenta')
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
    scene = bpy.context.scene
    scene.unit_settings.system, scene.unit_settings.scale_length = 'METRIC',1
    materials()
    OUTPUT.mkdir(parents=True,exist_ok=True)
    objects = []
    for i,(name,build,lo,hi,budget) in enumerate(SPECS):
        PARTS = []
        random.seed(55081+i*7919)
        build()
        objects.append(finish(name,lo,hi,budget))
    assert len(scene.objects)==7 and all(o.type=='MESH' for o in scene.objects)
    preview(objects)
    assert len(scene.objects)==7 and all(o.type=='MESH' for o in scene.objects)
    assert all(o.location.length<1e-7 for o in objects)
    print('DONE',flush=True)


if __name__ == '__main__':
    main()
