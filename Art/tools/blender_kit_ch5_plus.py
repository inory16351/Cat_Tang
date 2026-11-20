# 5장 정예 인형: 뼈 부착용 균열 수정 장갑 7종과 강화 무기 5종, FBX 및 미리보기.
# 실행: "C:/Program Files/Blender Foundation/Blender 5.3/blender.exe" -b --factory-startup -P Art/tools/blender_kit_ch5_plus.py
"""Blender 5.x, metres, +Y front, +X right, +Z up.

Armor is authored about its bone attachment, weapons about their grip centre.
Every exported object has identity transforms. Disconnected hovering fragments
are closed islands in that same mesh. Export happens BEFORE preview assembly,
viewport recoloring, mannequins, labels or cameras are introduced.
"""
import math
import random
from pathlib import Path

import bmesh
import bpy
from mathutils import Euler, Matrix, Vector

ROOT = Path(__file__).resolve().parents[2]
OUTPUT = ROOT / 'Game/Assets/Art/Characters/Ch5'
PREVIEW = ROOT / 'Art/tools/specs/blender_kit_ch5_plus_preview.png'
TAU = math.tau
FRONT = (math.pi / 2, 0, 0)
MATS, PARTS = {}, []
PALETTE = {
    'void_stone': (.22, .13, .34), 'void_stone_dark': (.060, .035, .105),
    'violet': (.35, .13, .61), 'violet_dark': (.15, .045, .27),
    'indigo': (.07, .065, .24), 'night_sky': (.018, .018, .065),
    'crystal': (.56, .29, .94), 'magenta': (.98, .025, .47),
    'gold': (.86, .49, .10), 'gold_dark': (.35, .18, .045),
    'crack_gold': (1., .79, .26), 'mirror': (.64, .85, .96),
    'mirror_back': (.16, .30, .48), 'starlight': (.94, .95, 1.),
    'marble': (.80, .74, .91), 'marble_dark': (.39, .29, .52),
    'iron_dark': (.045, .037, .07),
}
ATTACH = {
    'ShoulderRift_R': (.30, 0, 1.15), 'ShoulderRift_L': (-.30, 0, 1.15),
    'BackRift': (0, 0, 1.15), 'HeadShards': (0, 0, 1.35),
    'HipShards': (0, 0, .80),
    'ArmShard_R': (.45, 0, .85), 'ArmShard_L': (-.45, 0, .85),
}
WEAPON_BOUNDS = {
    'ShardRiflePlus': (-.45, 1.05), 'StarStaffPlus': (-.40, 1.40),
    'RuinMaulPlus': (-.40, 1.30), 'RiftBladePlus': (-.18, .82),
    'RiftCenserPlus': (-.40, 1.30),
}


def materials():
    for role in PALETTE:
        mat = bpy.data.materials.new(role)
        mat.use_nodes = True
        mat.diffuse_color = (.5, .5, .5, 1)
        node = mat.node_tree.nodes['Principled BSDF']
        node.inputs['Base Color'].default_value = (.5, .5, .5, 1)
        node.inputs['Metallic'].default_value = 0
        node.inputs['Roughness'].default_value = .8
        MATS[role] = mat


def mesh(label, verts, faces, role, bevel=False):
    data = bpy.data.meshes.new(label)
    data.from_pydata(verts, [], faces)
    data.update()
    obj = bpy.data.objects.new(label, data)
    bpy.context.collection.objects.link(obj)
    data.materials.append(MATS[role])
    if bevel:
        mod = obj.modifiers.new('Chunky hard edge chamfer', 'BEVEL')
        mod.width, mod.segments = .02, 2
        mod.limit_method, mod.angle_limit = 'ANGLE', math.radians(35)
        mod.use_clamp_overlap = True
    PARTS.append(obj)
    return obj


def place(obj, loc=(0, 0, 0), rot=(0, 0, 0)):
    obj.data.transform(Matrix.Translation(Vector(loc)) @ Euler(rot).to_matrix().to_4x4())
    return obj


def box(label, loc, size, role, rot=(0, 0, 0), bevel=True):
    x, y, z = (s / 2 for s in size)
    verts = [(-x,-y,-z),(x,-y,-z),(x,y,-z),(-x,y,-z),
             (-x,-y,z),(x,-y,z),(x,y,z),(-x,y,z)]
    faces = [(0,3,2,1),(4,5,6,7),(0,1,5,4),(1,2,6,5),(2,3,7,6),(3,0,4,7)]
    return place(mesh(label, verts, faces, role, bevel), loc, rot)


def cylinder(label, loc, radius, depth, role, n=12, rot=(0,0,0), bevel=False):
    verts = [(radius*math.cos(TAU*j/n),radius*math.sin(TAU*j/n),z)
             for z in (-depth/2,depth/2) for j in range(n)]
    faces = [tuple(reversed(range(n))),tuple(range(n,2*n))]
    faces += [(j,(j+1)%n,(j+1)%n+n,j+n) for j in range(n)]
    return place(mesh(label,verts,faces,role,bevel),loc,rot)


def ring(label, loc, radius, width, depth, role='gold', rot=(0,0,0), n=16):
    profile = [(radius-width,-depth/2),(radius,-depth/2),
               (radius,depth/2),(radius-width,depth/2)]
    verts = [(r*math.cos(TAU*j/n),r*math.sin(TAU*j/n),z)
             for r,z in profile for j in range(n)]
    faces = [(k*n+j,k*n+(j+1)%n,((k+1)%4)*n+(j+1)%n,((k+1)%4)*n+j)
             for k in range(4) for j in range(n)]
    return place(mesh(label,verts,faces,role),loc,rot)


def solid(label, outline, depth, role, y=0, bevel=True, back=None):
    """Closed XZ extrusion; the +Y face has the primary material."""
    n = len(outline)
    verts = [(x,yy,z) for yy in (y-depth/2,y+depth/2) for x,z in outline]
    faces = [tuple(reversed(range(n))),tuple(range(n,2*n))]
    faces += [(j,(j+1)%n,(j+1)%n+n,j+n) for j in range(n)]
    obj = mesh(label,verts,faces,role,bevel)
    if back and back != role:
        obj.data.materials.append(MATS[back])
        obj.data.polygons[0].material_index = 1
        for poly in obj.data.polygons[2:]: poly.material_index = 1
    return obj


def tube(label, points, radius, role, n=6):
    verts = []
    for i,p in enumerate(points):
        tangent = (Vector(points[min(i+1,len(points)-1)])-
                   Vector(points[max(0,i-1)])).normalized()
        ref = Vector((0,1,0)) if abs(tangent.y)<.9 else Vector((1,0,0))
        u = ref.cross(tangent).normalized()
        w = tangent.cross(u)
        verts += [tuple(Vector(p)+radius*(math.cos(TAU*j/n)*u+math.sin(TAU*j/n)*w))
                  for j in range(n)]
    faces = [tuple(reversed(range(n))),tuple(range((len(points)-1)*n,len(points)*n))]
    faces += [(i*n+j,i*n+(j+1)%n,(i+1)*n+(j+1)%n,(i+1)*n+j)
              for i in range(len(points)-1) for j in range(n)]
    return mesh(label,verts,faces,role)


def shard(label, base, tip, radius, role='crystal', flat=1., n=5):
    """Irregular broad crystal prism, pointed end; contrasting actual facets.

    Flat mirror shards have silver front facets and dark blue backs. Crystal
    has lilac light facets and dark violet facets, so FLAT lighting still reads
    as a mineral volume. Widths describe the body, not the intentionally sharp tip.
    """
    direction = Vector(tip)-Vector(base)
    length = direction.length
    verts = [(radius*math.cos(TAU*j/n),radius*flat*math.sin(TAU*j/n),z)
             for z,scale in ((0,1),(.64*length,.78)) for j in range(n)]
    # Taper the upper prism; keep the pointed endpoint exactly at the given tip.
    for j in range(n):
        x,y,z = verts[n+j]
        verts[n+j] = (x*.78,y*.78,z)
    verts.append((0,0,length))
    faces = [tuple(reversed(range(n)))]
    faces += [(j,(j+1)%n,n+(j+1)%n,n+j) for j in range(n)]
    faces += [(n+j,n+(j+1)%n,2*n) for j in range(n)]
    obj = mesh(label,verts,faces,role)
    other = 'mirror_back' if role=='mirror' else ('violet' if role=='crystal' else role)
    if other != role:
        obj.data.materials.append(MATS[other])
        for j,p in enumerate(obj.data.polygons):
            if j in (0,1,2,n+1,n+2): p.material_index = 1
    rotation = direction.to_track_quat('Z','Y').to_matrix().to_4x4()
    obj.data.transform(Matrix.Translation(Vector(base)) @ rotation)
    return obj


def gem(label, loc, radius, length, role='magenta'):
    # Double-ended crystal: two closed pyramids joined by a broad hexagonal body.
    n = 6
    verts = [(0,0,-length/2)]
    verts += [(radius*math.cos(TAU*j/n),radius*math.sin(TAU*j/n),z)
              for z in (-length*.20,length*.20) for j in range(n)]
    verts += [(0,0,length/2)]
    faces = [(0,1+(j+1)%n,1+j) for j in range(n)]
    faces += [(1+j,1+(j+1)%n,1+n+(j+1)%n,1+n+j) for j in range(n)]
    faces += [(1+n+j,1+n+(j+1)%n,1+2*n) for j in range(n)]
    return place(mesh(label,verts,faces,role),loc)


def star(label, loc, radius, role='starlight', depth=.055):
    outline = []
    for j in range(10):
        a = TAU*j/10
        r = radius if j%2==0 else radius*.46
        outline.append((loc[0]+r*math.sin(a),loc[2]+r*math.cos(a)))
    return solid(label,outline,depth,role,loc[1],False)


def grip(band='magenta'):
    cylinder('Exact centred fist cylinder',(0,0,0),.045,.18,'iron_dark')
    for z in (-.068,.068):
        cylinder('Grip end band',(0,0,z),.052,.04,band)


def pin(loc, role='gold', radius=.025):
    return cylinder('Broad kintsugi rivet',loc,radius,.04,role,8,FRONT)


def shoulder():
    box('Broken obsidian shoulder saddle',(.11,0,.08),(.32,.28,.10),'void_stone_dark')
    box('Inset violet shoulder plate',(.11,0,.136),(.27,.23,.045),'violet_dark')
    # Two cross straps are the raised gold clamp, not a giant solid gold cap.
    for y in (-.105,.105):
        box('Shoulder clamp cross strap',(.11,y,.155),(.32,.045,.045),'gold')
        box('Outer clamp jaw',(.257,y,.095),(.045,.055,.12),'gold_dark')
    tube('Shoulder spreading gold seam',[(-.025,-.02,.166),(.06,.01,.166),
         (.10,-.03,.166),(.18,.025,.166),(.24,0,.166)],.022,'crack_gold',4)
    # All tips flare 20--50 degrees UP/OUT; no volume projects toward the face.
    bases = [(.04,-.075,.16),(.14,-.075,.16),(.22,-.035,.16),
             (.07,.045,.16),(.18,.065,.16)]
    vectors = [(.10,-.025,.25),(.15,-.05,.38),(.13,-.025,.22),
               (.08,.015,.18),(.13,.02,.29)]
    for i,(base,delta) in enumerate(zip(bases,vectors)):
        shard('Shoulder erupted violet crystal',base,Vector(base)+Vector(delta),.055 if i!=1 else .067)
    shard('Shoulder mirror splinter A',(.065,-.105,.17),(.14,-.17,.35),.05,'mirror',.50)
    shard('Shoulder mirror splinter B',(.215,.025,.17),(.31,.06,.32),.045,'mirror',.50)
    # Tallest spike ends at Z .54. Lowest chip vertex is exactly .12 above it.
    gem('Detached levitating shoulder crystal',(.29,-.125,.695),.038,.07,'crystal')


def back_rift():
    box('Back attachment bracket',(0,-.255,0),(.30,.06,.25),'gold')
    box('Recess in bracket',(0,-.292,0),(.22,.04,.17),'void_stone_dark')
    for x in (-.115,.115):
        tube('Back bracket standoff',[(x,-.25,-.065),(x,-.36,.065)],.033,'gold_dark')
    outline = [(-.14,-.15),(-.235,.02),(-.14,.20),(-.275,.36),(-.13,.43),
               (-.21,.65),(-.025,.85),(.075,.70),(.225,.78),(.15,.52),
               (.275,.39),(.14,.29),(.205,.105),(.085,-.025),(.13,-.15)]
    solid('Lightning shaped open void slab',outline,.10,'night_sky',-.38,True,'indigo')
    # Gold on BOTH sides: readable on the assembled front silhouette and back.
    for y in (-.44,-.32):
        tube('Raised gold rift perimeter',[(x,y,z) for x,z in outline+[outline[0]]],.022,'crack_gold',4)
    for x,z in ((-.08,-.015),(.07,.12),(-.12,.30),(.07,.40),(-.07,.52),(.02,.65),(-.03,.76)):
        gem('Back face starlight', (x,-.455,z),.026,.055,'starlight')
    spikes = [((-.23,-.39,.12),(-.43,-.48,.37),.07),
              ((-.22,-.39,.50),(-.40,-.47,.85),.075),
              ((-.10,-.39,.72),(-.24,-.48,1.13),.08),
              ((.06,-.39,.72),(.17,-.48,1.24),.085),
              ((.20,-.39,.60),(.38,-.46,.95),.07),
              ((.22,-.39,.26),(.43,-.48,.52),.065)]
    for a,b,r in spikes: shard('Rift edge violet eruption',a,b,r)
    for a,b in (((-.17,-.44,.02),(-.33,-.50,.19)),
                ((-.19,-.44,.57),(-.34,-.52,.77)),
                ((.15,-.44,.49),(.32,-.51,.69))):
        shard('Back mirror fragment',a,b,.065,'mirror',.48)
    gem('Back magenta fissure seed',(0,-.49,.24),.065,.14)


def head_shards():
    # Rear half only. The highest +Y vertex stays behind +.10, away from face.
    angles = [-math.pi/2+math.pi*j/16 for j in range(17)]
    arc = [(.36*math.sin(a),-.10-.23*math.cos(a),.62+.065*math.cos(a)) for a in angles]
    tube('Broken gold back crown segment',arc,.023,'gold',6)
    for i in range(6):
        a = -1.42+2.84*i/5
        base = Vector((.35*math.sin(a),-.10-.23*math.cos(a),.625+.065*math.cos(a)))
        height = (.19,.31,.40,.35,.29,.20)[i]
        tip = base+Vector((.075*math.sin(a),-.055,height))
        shard('Top back violet crown shard',base,tip,.057 if i in (0,5) else .065)
        if i in (1,4):
            shard('Crown silver shard',base+Vector((.045,0,-.015)),
                  base+Vector((.085,-.055,.16)),.04,'mirror',.5)
    for s in (-1,1):
        tube('Crown crack seam',[(s*.31,-.20,.65),(s*.25,-.25,.69),
             (s*.22,-.27,.65)],.02,'crack_gold',4)
        # Gold feet reach down to the upper skull; the crown never covers eyes.
        box('Crown skull clamp',(s*.32,-.12,.565),(.055,.08,.14),'gold_dark')
    gem('Central back magenta crown gem',(0,-.365,.70),.072,.16)
    box('Magenta crown gem socket',(0,-.345,.64),(.14,.07,.055),'gold')


def hip_shards():
    # Open horseshoe band: front centre is entirely empty, even the gold band.
    angles = [math.radians(58+244*j/24) for j in range(25)]
    points = [(.32*math.sin(a),.32*math.cos(a),.02) for a in angles]
    tube('Open cracked kintsugi belt',points,.04,'gold',6)
    for i,a in enumerate([math.radians(d) for d in (66,108,150,210,252,294)]):
        r=.32
        base=Vector((r*math.sin(a),r*math.cos(a),-.02))
        length=(.23,.29,.26,.30,.27,.22)[i]
        tip=base+Vector((.075*math.sin(a),.075*math.cos(a),-length))
        shard('Downward flared shard tasset',base,tip,.085,
              'mirror' if i%2==0 else 'crystal',.65)
        box('Tasset belt clamp',base+Vector((0,0,.035)),(.085,.085,.06),'gold_dark')
    tube('Belt spreading crack',[(-.18,-.27,.055),(-.10,-.31,.055),
         (-.035,-.29,.055),(.04,-.32,.055),(.13,-.29,.055)],.02,'crack_gold',4)
    gem('Belt back magenta clasp',(0,-.35,.025),.055,.095)


def arm_shard():
    ring('Obsidian forearm open cuff',(0,0,0),.11,.045,.18,'void_stone_dark',
         rot=(0,math.pi/2,0),n=12)
    ring('Gold forearm clamp',(.04,0,0),.123,.035,.045,'gold',
         rot=(0,math.pi/2,0),n=12)
    for y,z,length in ((-.075,.065,.19),(0,.105,.25),(.075,.065,.14)):
        base=(.035,y,z)
        # Towards elbow (-X on the right arm), swept up off the hand surface.
        shard('Swept bracer crystal',base,(-length*.78,y-.025,z+length*.62),.047)
    shard('Bracer mirror tooth',(.055,-.08,.045),(-.08,-.14,.14),.045,'mirror',.5)
    tube('Cuff gold fracture',[(-.085,.075,.07),(-.035,.09,.04),
         (.015,.075,.07),(.065,.085,.06)],.02,'crack_gold',4)
    gem('Bracer magenta clamp stone',(.065,.06,.10),.039,.08)


def rifle_plus():
    grip()
    stock=[(-.055,-.09),(.055,-.09),(.15,-.29),(.13,-.45),(-.15,-.45),(-.17,-.31)]
    solid('Broken violet rifle stock',stock,.18,'violet_dark')
    box('Heavy rifle butt plate',(-.01,0,-.425),(.28,.20,.05),'gold')
    solid('Stock mirror inset',[(-.09,-.21),(.035,-.23),(.075,-.36),(-.11,-.36)],
          .04,'mirror',.105,True,'mirror_back')
    tube('Wide stock kintsugi crack',[(-.11,.135,-.20),(-.025,.135,-.27),
         (-.06,.135,-.33),(.05,.135,-.38)],.025,'crack_gold',4)
    box('Rifle obsidian receiver',(0,-.025,.19),(.21,.19,.20),'void_stone_dark')
    box('Silver breech plate',(0,.085,.19),(.15,.045,.13),'mirror')
    # Five mirror barrel shards fan out into separate, unmistakable muzzle tips.
    for i,(x,top) in enumerate(((-.18,.94),(-.095,1.00),(0,1.05),(.095,.99),(.18,.93))):
        base_x=x*.45
        outline=[(base_x-.035,.29),(base_x+.035,.29),(x+.041,top-.12),
                 (x+.012,top),(x-.04,top-.065)]
        solid('Fan of five mirror muzzle shards',outline,.075,'mirror',
              -.025+(i%2)*.075,False,'mirror_back')
    for z,r in ((.33,.14),(.59,.16),(.79,.185)):
        ring('Rifle gold barrel binding',(0,0,z),r,.04,.05,n=16)
    for i,z in enumerate((-.17,-.26,-.34)):
        shard('Violet stock eruption',(-.12,-.035,z),(-.30-i*.025,-.07,z+.07),.05)
    for x,z in ((.17,.245),(-.145,.45)):
        gem('Additional magenta ammunition crystal',(x,.085,z),.065,.20)
        box('Ammo gold clamp',(x,.055,z-.075),(.11,.10,.045),'gold')
    star('Rifle gold star sight',(0,.23,.73),.075,'gold',.045)
    box('Star sight bridge',(0,.16,.73),(.05,.15,.05),'gold_dark')
    shard('Detached muzzle crystal',(.29,.015,.92),(.34,.01,1.02),.046)
    gem('Detached stock mirror chip',(-.27,.035,-.40),.045,.08,'mirror')


def star_staff_plus():
    cylinder('Indigo staff',(0,0,.25),.045,1.20,'indigo')
    grip()
    cylinder('Staff butt ferrule',(0,0,-.37),.068,.06,'gold')
    for z in (-.25,.25,.48,.70):
        cylinder('Staff kintsugi band',(0,0,z),.065,.05,'gold')
    gem('Staff larger magenta head socket',(0,0,.82),.09,.19)
    centre=(0,0,1.115)
    for i,yaw in enumerate((0,math.radians(58),math.radians(-58))):
        ring('Three crossed armillary rings',centre,.285,.04,.045,
             rot=(math.pi/2,0,yaw),n=24)
    positions=[(-.205,.035,1.23),(.165,.025,1.29),(.21,.04,1.03),
               (-.14,.04,.94),(-.04,-.045,1.34)]
    for i,p in enumerate(positions):
        tube('Constellation gold link',[p,positions[(i+1)%5]],.021,'gold_dark',6)
        gem('Five crystal constellation orbs',p,.052,.105,'crystal')
    star('Larger luminous central star',(0,.12,1.115),.155,depth=.065)
    gem('Star magenta heart',(0,.17,1.115),.045,.085)
    shard('Staff violet rupture',(-.025,-.015,.50),(-.18,-.06,.72),.055)
    shard('Staff silver rupture',(.02,-.01,.58),(.13,-.06,.75),.045,'mirror',.5)
    tube('Shaft wide gold crack',[(0,.05,.30),(.028,.05,.40),
         (-.022,.05,.49),(.02,.05,.59)],.02,'crack_gold',4)
    gem('Detached armillary mirror chip',(-.35,0,1.21),.045,.10,'mirror')
    gem('Detached armillary crystal',(.35,0,1.14),.045,.12,'crystal')


def ruin_maul_plus():
    cylinder('Dark two handed haft',(0,0,.18),.046,1.10,'iron_dark')
    grip()
    cylinder('Maul gold pommel',(0,0,-.365),.085,.07,'gold')
    for z in (-.24,.22,.47,.65):
        cylinder('Haft gold collar',(0,0,z),.072,.05,'gold')
    for z in (.29,.40,.53): pin((0,.06,z))
    left=[(-.405,.67),(-.065,.67),(-.115,.81),(-.03,.91),
          (-.13,1.02),(-.075,1.18),(-.35,1.18),(-.405,1.10)]
    right=[(-.015,.67),(.405,.67),(.405,1.10),(.34,1.18),
           (-.015,1.18),(-.07,1.02),(.025,.91),(-.055,.81)]
    solid('Fractured maul left block',left,.50,'void_stone',back='void_stone_dark')
    solid('Fractured maul right block',right,.50,'void_stone',back='void_stone_dark')
    for y in (-.265,.265):
        tube('Wide glowing maul fracture',[(-.04,y,.69),(-.083,y,.81),
             (0,y,.91),(-.10,y,1.02),(-.045,y,1.16)],.032,'crack_gold',4)
    for x in (-.29,.29):
        for y in (-.265,.265):
            box('Maul binding face',(x,y,.92),(.075,.045,.51),'gold')
        for z in (.695,1.155):
            box('Maul binding crossbar',(x,0,z),(.075,.55,.05),'gold')
    # Broken marble capital retained from the normal weapon.
    box('Embedded capital top',(.16,.31,1.07),(.30,.17,.075),'marble')
    box('Embedded capital shadow molding',(.16,.31,.995),(.24,.15,.065),'marble_dark')
    box('Embedded capital stump',(.16,.31,.89),(.20,.14,.15),'marble')
    for x in (.105,.215):
        box('Capital broad flute',(x,.39,.89),(.04,.04,.12),'marble_dark',bevel=False)
    # A full crown around the THROUGH fracture; tips also emerge sideways.
    for i,a in enumerate([TAU*j/6 for j in range(6)]):
        base=(-.04,.22*math.sin(a),.94+.16*math.cos(a))
        tip=(-.10+.07*math.sin(a),.43*math.sin(a),.94+.31*math.cos(a))
        shard('Maul crack crystal burst',base,tip,.065)
    shard('Maul side silver splinter',(-.34,-.10,1.12),(-.51,-.14,1.26),.065,'mirror',.5)
    # Visible air gap under the detached block; its unchamfered apex is +1.30.
    gem('Hovering broken maul cap',(.16,-.025,1.265),.095,.07,'void_stone')
    box('Hovering cap gold scar',(.16,.058,1.265),(.11,.04,.04),'crack_gold',bevel=False)
    gem('Hovering magenta maul core',(-.23,.10,1.265),.048,.07)
    gem('Maul fissure magenta seed',(-.05,.30,.94),.065,.15)


def rift_blade_plus():
    grip()
    for z in (-.035,.035):
        ring('Violet handle wrap',(0,0,z),.05,.03,.035,'violet_dark',n=12)
    gem('Blade magenta pommel',(0,0,-.14),.065,.08)
    crescent=[(-.22,.16),(-.17,.08),(-.07,.105),(0,.12),(.10,.10),
              (.20,.09),(.245,.18),(.16,.15),(.07,.17),(-.07,.17),(-.15,.14)]
    solid('Larger gold crescent guard',crescent,.10,'gold')
    blade=[(-.07,.18),(.085,.18),(.18,.32),(.26,.53),(.26,.71),
           (.145,.82),(.16,.60),(.09,.41),(-.015,.28)]
    solid('Long curved mirror blade',blade,.09,'mirror',bevel=True,back='mirror_back')
    # Exact tip is a broad facet, not a bevel-clipped silhouette endpoint.
    solid('Mirror cutting tip',[(.145,.82),(.16,.64),(.225,.70)],.06,'mirror',bevel=False,back='mirror_back')
    tube('Widened blade kintsugi seam',[(0,.065,.23),(.085,.065,.33),
         (.068,.065,.43),(.17,.065,.53),(.15,.065,.63),(.19,.065,.71)],.023,'crack_gold',4)
    for i,(x,z) in enumerate(((-.025,.27),(.055,.39),(.115,.52),(.16,.65))):
        shard('Jagged violet back spine',(x,-.01,z),(x-.13,-.015,z+.12),.05)
    solid('Bright mirror reflection',[(.14,.44),(.20,.57),(.20,.66),(.17,.57)],
          .035,'starlight',.065,False)
    gem('Guard magenta gem',(.13,.075,.14),.047,.085)
    shard('Detached blade spine chip',(-.15,-.01,.51),(-.19,-.01,.62),.045)
    gem('Detached blade mirror fragment',(.33,0,.73),.045,.105,'mirror')


def rift_censer_plus():
    cylinder('Censer lower staff',(0,0,-.235),.044,.27,'violet_dark')
    tube('Censer swept upper staff',[(0,0,.09),(0,-.07,.27),(0,-.07,1.03)],.044,'violet_dark',8)
    grip()
    cylinder('Censer gold butt',(0,0,-.37),.07,.06,'gold')
    for z in (-.25,.30,.57,.84):
        cylinder('Censer staff gold band',(0,-.07 if z>0 else 0,z),.061,.05,'gold')
    tube('Gold lantern hook',[(0,-.07,1.02),(-.065,-.07,1.20),
         (-.015,-.07,1.27),(.09,-.07,1.27),(.22,-.07,1.20),(.26,-.07,1.15)],.03,'gold',8)
    ring('Lantern suspension link',(.26,-.07,1.15),.052,.032,.04,rot=FRONT,n=12)
    cx,cy=.26,-.07
    for z in (.70,1.095):
        ring('Larger cage octagonal rim',(cx,cy,z),.215,.04,.05,n=8)
    for dx,dy in ((-.15,-.14),(.15,-.14),(-.15,.14),(.15,.14)):
        tube('Cage open vertical bar',[(cx+dx,cy+dy,.70),(cx+dx,cy+dy,1.095)],.025,'gold',6)
    for dx in (-.15,.15):
        tube('Lantern top brace',[(cx+dx,cy,1.095),(cx,cy,1.15)],.023,'gold_dark',6)
    outline=[(.20,.745),(.13,.85),(.19,.90),(.16,1.00),(.27,1.08),
             (.35,.97),(.30,.91),(.34,.82)]
    solid('Jagged lantern night sky core',outline,.11,'night_sky',cy,False)
    for y in (cy-.065,cy+.065):
        tube('Core wide gold outline',[(x,y,z) for x,z in outline+[outline[0]]],.022,'crack_gold',4)
    for x,z in ((.23,.82),(.26,.96),(.29,.88)):
        gem('Rift core starlight',(x,cy+.08,z),.025,.055,'starlight')
    for a,b in (((.19,cy,.88),(-.015,cy-.02,1.04)),
                ((.31,cy,.91),(.56,cy+.02,1.06)),
                ((.24,cy,1.00),(.30,cy-.03,1.22)),
                ((.24,cy,.80),(.14,cy-.015,.60))):
        shard('Violet core spikes through cage bars',a,b,.065)
    shard('Cage mirror splinter',(.36,cy,.80),(.52,cy-.04,.76),.055,'mirror',.50)
    ring('Cage lower chain link',(cx,cy,.63),.048,.03,.04,rot=FRONT,n=12)
    gem('Magenta censer tassel',(cx,cy,.51),.075,.16)
    gem('Additional magenta hook gem',(.02,0,1.24),.045,.12)
    gem('Detached rift crystal',(.56,cy,1.18),.045,.12,'crystal')
    gem('Detached cage mirror chip',(.52,cy,.67),.042,.09,'mirror')


def triangles(obj):
    obj.data.calc_loop_triangles()
    return len(obj.data.loop_triangles)


def bounds(obj):
    return [Vector(tuple(fn(v.co[i] for v in obj.data.vertices) for i in range(3)))
            for fn in (min,max)]


def recalc(obj):
    bm=bmesh.new()
    bm.from_mesh(obj.data)
    bmesh.ops.recalc_face_normals(bm,faces=list(bm.faces))
    bm.to_mesh(obj.data)
    bm.free()
    obj.data.update()


def finish(name):
    bpy.ops.object.select_all(action='DESELECT')
    for part in PARTS:
        part.select_set(True)
        bpy.context.view_layer.objects.active=part
        recalc(part)
        for mod in list(part.modifiers): bpy.ops.object.modifier_apply(modifier=mod.name)
        part.select_set(False)
    for part in PARTS: part.select_set(True)
    bpy.context.view_layer.objects.active=PARTS[0]
    if len(PARTS)>1: bpy.ops.object.join()
    obj=bpy.context.object
    obj.name, obj.data.name = name,name+'_Mesh'
    budget=3000 if name in ATTACH else 4500
    # Geometry is designed under budget; fail instead of quietly damaging facets.
    assert triangles(obj)<=budget,(name,triangles(obj))
    recalc(obj)
    bm=bmesh.new()
    bm.from_mesh(obj.data)
    assert not any(e.is_boundary or not e.is_manifold for e in bm.edges),name+' open mesh'
    bm.free()
    for poly in obj.data.polygons: poly.use_smooth=False
    bpy.context.view_layer.update()
    assert obj.location.length<1e-7 and obj.rotation_euler.to_matrix()==Matrix.Identity(3)
    assert tuple(obj.scale)==(1.,1.,1.)
    assert all(math.isfinite(c) for v in obj.data.vertices for c in v.co)
    areas={}
    for poly in obj.data.polygons:
        role=obj.data.materials[poly.material_index].name
        areas[role]=areas.get(role,0)+poly.area
    assert len(areas)>=4,(name,areas)
    assert max(areas.values())/sum(areas.values())<=.55,(name,areas)
    for mat in obj.data.materials:
        assert mat.name in PALETTE
        assert tuple(mat.diffuse_color)==(.5,.5,.5,1)
        assert tuple(mat.node_tree.nodes['Principled BSDF'].inputs['Base Color'].default_value)==(.5,.5,.5,1)
    lo,hi=bounds(obj)
    if name in ATTACH:
        obj['attachment_world_m']=ATTACH[name]
        obj['origin_convention']='bone attachment; local zero'
        if name=='HeadShards': assert hi.y<=.10
        if name=='ShoulderRift_R': assert lo.x>=-.051 and hi.x<=.351,(lo,hi)
        prefix='Kit_EP_'
    else:
        low,high=WEAPON_BOUNDS[name]
        assert abs(lo.z-low)<.00015 and abs(hi.z-high)<.00015,(name,lo.z,hi.z)
        obj['origin_convention']='exact grip centre'
        obj['grip_centre_m']=[0.,0.,0.]
        obj['grip_radius_m'],obj['grip_length_m']=.045,.18
        prefix='Kit_W_'
    obj['front'],obj['long_axis']='+Y','+Z'
    obj['triangle_count']=triangles(obj)
    destination=OUTPUT/(prefix+name+'.fbx')
    bpy.ops.export_scene.fbx(filepath=str(destination),use_selection=True,
        axis_forward='-Z',axis_up='Y',apply_scale_options='FBX_SCALE_UNITS',
        bake_space_transform=True,object_types={'MESH'},use_mesh_modifiers=True,
        mesh_smooth_type='FACE',add_leaf_bones=False,bake_anim=False,use_custom_props=True)
    assert destination.is_file() and destination.stat().st_size>1024
    print(f'EXPORTED {name} {triangles(obj)} {tuple(round(d,4) for d in obj.dimensions)}',flush=True)
    return obj


def preview(objects):
    global PARTS
    scene=bpy.context.scene
    scene.render.engine='BLENDER_WORKBENCH'
    scene.render.resolution_x,scene.render.resolution_y=1800,1000
    scene.render.resolution_percentage=100
    scene.render.image_settings.file_format='PNG'
    scene.render.filepath=str(PREVIEW)
    scene.render.film_transparent=False
    scene.view_settings.view_transform='Standard'
    shade=scene.display.shading
    shade.light,shade.color_type='FLAT','MATERIAL'
    shade.show_shadows,shade.show_cavity=False,True
    shade.cavity_type='BOTH'
    shade.curvature_ridge_factor,shade.curvature_valley_factor=1.3,1.15
    shade.show_specular_highlight=False
    shade.background_type='WORLD'
    scene.world=bpy.data.worlds.new('Elite kit review backdrop')
    scene.world.color=(.025,.030,.052)
    for role,mat in MATS.items(): mat.diffuse_color=(*PALETTE[role],1)
    for obj in objects: obj.hide_render=True
    PARTS=[]
    dummy=bpy.data.materials.new('PREVIEW ONLY neutral doll')
    dummy.diffuse_color=(.29,.31,.34,1)
    face=bpy.data.materials.new('PREVIEW ONLY doll face')
    face.diffuse_color=(.075,.085,.105,1)

    def grey(obj,mat=dummy):
        obj.data.materials.clear()
        obj.data.materials.append(mat)
        return obj

    def sphere(loc,scale,mat=dummy):
        bpy.ops.mesh.primitive_uv_sphere_add(segments=20,ring_count=12,location=loc)
        obj=bpy.context.object
        obj.name='PREVIEW ONLY reference doll'
        obj.scale=scale
        grey(obj,mat)
        for p in obj.data.polygons: p.use_smooth=True
        return obj

    # Projected screen positions; the second entire assembly is yawed 145 deg.
    # Camera +Y sees screen-right at -X.
    doll_positions=(-3.75,-1.65)
    for screen_x,yaw in zip(doll_positions,(0,math.radians(145))):
        existing=set(scene.objects)
        sphere((0,0,1.65),(.42,.42,.42))
        # True .6-wide capsule, bottom .45, top 1.25, depth .45.
        grey(cylinder('PREVIEW ONLY capsule core',(0,0,.85),.30,.20,'iron_dark',20))
        # Ellipsoid caps meet the cylinder tangentially; scale Y to torso depth.
        sphere((0,0,.75),(.30,.225,.30))
        sphere((0,0,.95),(.30,.225,.30))
        for s in (-1,1):
            arm=tube('PREVIEW ONLY down out arm',[(s*.30,0,1.15),(s*.45,0,.85),(s*.50,0,.65)],.105,'iron_dark',12)
            grey(arm)
            sphere((s*.45,0,.85),(.11,.11,.11))
            grey(cylinder('PREVIEW ONLY short leg',(s*.16,0,.30),.115,.33,'iron_dark',12))
            grey(box('PREVIEW ONLY foot',(s*.16,.07,.115),(.24,.34,.19),'iron_dark'))
            sphere((s*.145,.390,1.70),(.045,.035,.06),face)
        # Small eyes make the FRONT unmistakable and expose any face obstruction.
        mouth=box('PREVIEW ONLY mouth',(0,.417,1.53),(.075,.03,.032),'iron_dark',bevel=False)
        grey(mouth,face)
        bpy.context.view_layer.update()
        root=Matrix.Translation(Vector((-screen_x,0,0))) @ Matrix.Rotation(yaw,4,'Z')
        for obj in set(scene.objects)-existing: obj.matrix_world=root @ obj.matrix_world
        for obj in objects:
            if obj.name not in ATTACH: continue
            inst=bpy.data.objects.new('PREVIEW ONLY '+obj.name,obj.data)
            scene.collection.objects.link(inst)
            inst.matrix_world=root @ Matrix.Translation(Vector(ATTACH[obj.name]))
        cylinder('PREVIEW ONLY doll plinth',(-screen_x,0,-.035),.83,.07,'indigo',40)

    weapon_positions=(.05,1.05,2.25,3.40,4.45)
    weapons=[obj for obj in objects if obj.name in WEAPON_BOUNDS]
    for obj,screen_x in zip(weapons,weapon_positions):
        inst=bpy.data.objects.new('PREVIEW ONLY '+obj.name,obj.data)
        scene.collection.objects.link(inst)
        inst.location=(-screen_x,0,1.)
        cylinder('PREVIEW ONLY weapon plinth',(-screen_x,0,.02),.40,.055,'indigo',24)

    camera=bpy.data.objects.new('PREVIEW ONLY camera',bpy.data.cameras.new('Review camera'))
    scene.collection.objects.link(camera)
    scene.camera=camera
    target=Vector((-.10,0,1.12))
    camera.location=target+Vector((0,12,12*math.tan(math.radians(55))))
    camera.rotation_euler=(target-camera.location).to_track_quat('-Z','Y').to_euler()
    camera.data.type,camera.data.ortho_scale='ORTHO',10.60
    rotation=camera.rotation_euler.to_matrix()
    toward=(camera.location-target).normalized()

    def caption(body,x,y,size=.10,role='starlight'):
        data=bpy.data.curves.new('PREVIEW ONLY caption','FONT')
        data.body,data.size,data.align_x=body,size,'CENTER'
        data.materials.append(MATS[role])
        obj=bpy.data.objects.new('PREVIEW ONLY caption',data)
        scene.collection.objects.link(obj)
        obj.location=target+rotation @ Vector((x,y,0))+toward*3
        obj.rotation_euler=camera.rotation_euler

    caption('C H A P T E R   V   /   V I O L E T   R I F T   E L I T E S',0,2.29,.155)
    caption('7 BONE ATTACHMENTS  +  5 GRIP-CENTRED WEAPONS     /     2.2 m REFERENCE DOLL',0,2.02,.075,'gold')
    caption('KINTSUGI CLAMPS   /   SHATTERED MIRROR   /   ERUPTING CRYSTAL',0,1.78,.072,'crystal')
    for x,title,sub in ((-3.75,'FRONT / EQUIPPED','FACE CLEAR  /  ALL 7 PIECES'),
                        (-1.65,'BACK / THREE-QUARTER','RIFT RISING ABOVE THE HEAD')):
        caption(title,x,-1.28,.115)
        caption(sub,x,-1.48,.057,'gold')
    titles=['SHARD\nRIFLE +','STAR\nSTAFF +','RUIN\nMAUL +','RIFT\nBLADE +','RIFT\nCENSER +']
    for obj,x,title in zip(weapons,weapon_positions,titles):
        caption(title,x,-1.28,.093)
        lo,hi=WEAPON_BOUNDS[obj.name]
        caption(f'{hi-lo:.2f} m / {triangles(obj):,} tri',x,-1.63,.052,'gold')
        # Grip cross sits in screen plane but at the projected true grip height.
        centre=Vector((-x,0,1.))+toward*.20
        for size in ((.10,.016,.02),(.016,.10,.02)):
            box('PREVIEW ONLY magenta grip origin',centre,size,'magenta',
                rot=camera.rotation_euler,bevel=False)
    caption('55 DEG TOP-DOWN REVIEW     /     MAGENTA MARKS = WEAPON GRIPS AT Z 1.0     /     ONE MESH PER FBX',0,-2.12,.067,'mirror')
    PREVIEW.parent.mkdir(parents=True,exist_ok=True)
    bpy.ops.render.render(write_still=True)
    assert PREVIEW.is_file() and PREVIEW.stat().st_size>1024
    print(f'PREVIEW {PREVIEW}',flush=True)
    scene.camera=None
    for obj in list(scene.objects):
        if obj not in objects: bpy.data.objects.remove(obj,do_unlink=True)
    for obj in objects: obj.hide_render=False
    for mat in MATS.values(): mat.diffuse_color=(.5,.5,.5,1)


def main():
    global PARTS
    bpy.ops.wm.read_factory_settings(use_empty=True)
    for mat in list(bpy.data.materials): bpy.data.materials.remove(mat)
    scene=bpy.context.scene
    scene.unit_settings.system,scene.unit_settings.scale_length='METRIC',1
    materials()
    OUTPUT.mkdir(parents=True,exist_ok=True)
    specs=[('ShoulderRift_R',shoulder),('ShoulderRift_L',None),
           ('BackRift',back_rift),('HeadShards',head_shards),('HipShards',hip_shards),
           ('ArmShard_R',arm_shard),('ArmShard_L',None),
           ('ShardRiflePlus',rifle_plus),('StarStaffPlus',star_staff_plus),
           ('RuinMaulPlus',ruin_maul_plus),('RiftBladePlus',rift_blade_plus),
           ('RiftCenserPlus',rift_censer_plus)]
    objects=[]
    for i,(name,build) in enumerate(specs):
        PARTS=[]
        random.seed(550921+i*7919)
        if build:
            build()
        else:
            # Mirror the ALREADY bevelled topology for genuinely exact symmetry.
            source=next(obj for obj in objects if obj.name==name[:-1]+'R')
            obj=bpy.data.objects.new('Exact reflected armor',source.data.copy())
            scene.collection.objects.link(obj)
            obj.data.transform(Matrix.Diagonal((-1,1,1,1)))
            PARTS=[obj]
        objects.append(finish(name))
    for prefix in ('ShoulderRift_','ArmShard_'):
        right=next(obj for obj in objects if obj.name==prefix+'R')
        left=next(obj for obj in objects if obj.name==prefix+'L')
        assert len(right.data.vertices)==len(left.data.vertices)
        for r,l in zip(right.data.vertices,left.data.vertices):
            assert (Vector((-r.co.x,r.co.y,r.co.z))-l.co).length<1e-6
    assert len(scene.objects)==12 and all(obj.type=='MESH' for obj in scene.objects)
    preview(objects)
    assert len(scene.objects)==12 and all(obj.type=='MESH' for obj in scene.objects)
    assert all(obj.location.length<1e-7 for obj in objects)
    print('DONE',flush=True)


if __name__=='__main__':
    main()
