# 5장 시간의 군주 보스의 후광·견갑·왕관·망토 파편과 공전 장식을 만듭니다.
# 실행: "C:/Program Files/Blender Foundation/Blender 5.3/blender.exe" -b --factory-startup -P Art/tools/blender_kit_ch5_bossparts.py
"""Bone-local, 2.2 m reference-scale boss regalia; +Y front, Z up.

All export shaders are flat grey role materials. Only the post-export Workbench
review changes viewport colours. Broken contours are authored zigzags, never
regular wedge cuts. No dependency on the Tripo mesh or other kit scripts.
"""
import json
import math
import random
from pathlib import Path

import bmesh
import bpy
from mathutils import Euler, Matrix, Vector

ROOT = Path(__file__).resolve().parents[2]
OUTPUT = ROOT / 'Game/Assets/Art/Characters/Ch5'
PREVIEW = ROOT / 'Art/tools/specs/blender_kit_ch5_bossparts_preview.png'
REPORT = PREVIEW.with_name('blender_kit_ch5_bossparts_audit.json')
LOG = PREVIEW.with_name('blender_kit_ch5_bossparts.log')
TAU = math.tau
MATS, PARTS, RECORDS = {}, [], []
LOG_LINES = []
PALETTE = {
    'void_stone_dark': (.075,.042,.14), 'iron_dark': (.12,.135,.19),
    'gold': (.96,.64,.17), 'gold_dark': (.38,.225,.075),
    'brass': (.66,.40,.15), 'dial_face': (.88,.83,.96),
    'dial_mark': (.065,.035,.12), 'crack_gold': (1,.85,.32),
    'crystal': (.63,.27,.96), 'violet_dark': (.24,.085,.40),
    'night_sky': (.028,.025,.09), 'mirror': (.63,.84,.98),
    'mirror_back': (.22,.28,.43), 'starlight': (.93,.98,1),
    'porcelain': (.79,.78,.91), 'sand': (.69,.49,.79),
    'magenta': (.98,.08,.52),
}


def emit(message):
    print(message,flush=True)
    LOG_LINES.append(message)


def materials():
    for role in PALETTE:
        mat = bpy.data.materials.new(role)
        mat.use_nodes = True
        mat.diffuse_color = (.5,.5,.5,1)
        shader = mat.node_tree.nodes['Principled BSDF']
        shader.inputs['Base Color'].default_value = (.5,.5,.5,1)
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
        mod = obj.modifiers.new('Two-step worn chamfer', 'BEVEL')
        mod.width, mod.segments = .02, 2
        mod.limit_method, mod.angle_limit = 'ANGLE', math.radians(55)
        mod.use_clamp_overlap = True
    PARTS.append(obj)
    return obj


def place(obj, loc=(0,0,0), rot=(0,0,0)):
    obj.data.transform(Matrix.Translation(Vector(loc)) @ Euler(rot).to_matrix().to_4x4())
    return obj


def group_place(start, loc=(0,0,0), rot=(0,0,0), scale=1):
    matrix = Matrix.Translation(Vector(loc)) @ Euler(rot).to_matrix().to_4x4() @ Matrix.Scale(scale,4)
    for obj in PARTS[start:]:
        obj.data.transform(matrix)


def roles(obj, extra, indices):
    for role in extra:
        obj.data.materials.append(MATS[role])
    for face, idx in zip(obj.data.polygons, indices):
        face.material_index = idx
    return obj


def box(label, loc, size, role, rot=(0,0,0), bevel=True):
    x,y,z = [s/2 for s in size]
    v = [(-x,-y,-z),(x,-y,-z),(x,y,-z),(-x,y,-z),
         (-x,-y,z),(x,-y,z),(x,y,z),(-x,y,z)]
    f = [(0,3,2,1),(4,5,6,7),(0,1,5,4),(1,2,6,5),(2,3,7,6),(3,0,4,7)]
    return place(mesh(label,v,f,role,bevel),loc,rot)


def slab(label, outline, depth, role, loc=(0,0,0), rot=(0,0,0), bevel=True):
    n = len(outline)
    v = [(x,y,z) for y in (-depth/2,depth/2) for x,z in outline]
    f = [tuple(reversed(range(n))),tuple(range(n,2*n))]
    f += [(j,(j+1)%n,(j+1)%n+n,j+n) for j in range(n)]
    return place(mesh(label,v,f,role,bevel),loc,rot)


def lathe(label, profile, role, n=16, loc=(0,0,0), rot=(0,0,0), bevel=False):
    # Profile is closed around its cross-section, including central cap points.
    v = [(r*math.cos(TAU*j/n),r*math.sin(TAU*j/n),z) for r,z in profile for j in range(n)]
    f = [(k*n+j,k*n+(j+1)%n,((k+1)%len(profile))*n+(j+1)%n,((k+1)%len(profile))*n+j)
         for k in range(len(profile)) for j in range(n)]
    return place(mesh(label,v,f,role,bevel),loc,rot)


def cylinder(label, loc, radius, depth, role, n=16, rot=(0,0,0), bevel=True):
    v = [(radius*math.cos(TAU*j/n),radius*math.sin(TAU*j/n),z)
         for z in (-depth/2,depth/2) for j in range(n)]
    f = [tuple(reversed(range(n))),tuple(range(n,2*n))]
    f += [(j,(j+1)%n,(j+1)%n+n,j+n) for j in range(n)]
    return place(mesh(label,v,f,role,bevel),loc,rot)


def ring(label, radius, width, height, role, loc=(0,0,0), rot=(0,0,0), n=20, bevel=False):
    return lathe(label,[(radius-width,-height/2),(radius,-height/2),
                       (radius,height/2),(radius-width,height/2)],role,n,loc,rot,bevel)


def tube(label, points, radius, role, n=6):
    v = []
    for i,p in enumerate(points):
        t = (Vector(points[min(i+1,len(points)-1)])-Vector(points[max(i-1,0)])).normalized()
        ref = Vector((0,1,0)) if abs(t.y)<.9 else Vector((1,0,0))
        u = ref.cross(t).normalized(); w = t.cross(u)
        v += [tuple(Vector(p)+radius*(math.cos(TAU*j/n)*u+math.sin(TAU*j/n)*w)) for j in range(n)]
    f = [tuple(reversed(range(n))),tuple(range(len(v)-n,len(v)))]
    f += [(k*n+j,k*n+(j+1)%n,(k+1)*n+(j+1)%n,(k+1)*n+j)
          for k in range(len(points)-1) for j in range(n)]
    return mesh(label,v,f,role)


def shard(label, loc, width, depth, height, role, rot=(0,0,0)):
    # Off-axis apex and a chipped shoulder, with large readable facets.
    v = [(-width*.47,-depth*.4,0),(width*.45,-depth*.5,.018),
         (width*.55,depth*.34,0),(-width*.4,depth*.5,.025),
         (-width*.36,-depth*.37,height*.58),(width*.29,-depth*.40,height*.73),
         (width*.33,depth*.31,height*.64),(-width*.30,depth*.40,height*.53),
         (-width*.08,depth*.1,height)]
    f = [(3,2,1,0)]
    f += [(j,(j+1)%4,(j+1)%4+4,j+4) for j in range(4)]
    f += [(4+j,4+(j+1)%4,8) for j in range(4)]
    obj = mesh(label,v,f,role,True)
    if role == 'crystal': roles(obj,['violet_dark','mirror'],[1,0,0,1,0,0,2,0,0])
    return place(obj,loc,rot)


def arc(label, inner, outer, a0, a1, depth, role, y=0, jagged=False, n=6):
    """XZ arc with independently serrated fracture boundaries at both ends."""
    angles = [a0+(a1-a0)*j/n for j in range(n+1)]
    curve = [(outer*math.sin(a),outer*math.cos(a)) for a in angles]
    jag = .050 / ((inner+outer)/2)
    # Four unequal radial fracture steps, not a straight radial cap.
    if jagged:
        for r,da in ((.79,-jag),(.46,jag*1.25),(.21,-jag*.85)):
            rr = inner+(outer-inner)*r
            curve.append((rr*math.sin(a1+da),rr*math.cos(a1+da)))
    curve += [(inner*math.sin(a),inner*math.cos(a)) for a in reversed(angles)]
    if jagged:
        for r,da in ((.18,jag),(.55,-jag*1.35),(.83,jag*.90)):
            rr = inner+(outer-inner)*r
            curve.append((rr*math.sin(a0+da),rr*math.cos(a0+da)))
    return slab(label,curve,depth,role,loc=(0,y,0))


def gear_chunk(radius=.2, hole=.08, depth=.065, teeth=15, loc=(0,0,0), rot=(0,0,0)):
    start = len(PARTS)
    # 240 degrees remain. Radial zigzag fracture caps connect inner and outer rim.
    a0,a1 = math.radians(-122),math.radians(118)
    curve = []
    count = round(teeth*2/3)
    for j in range(count):
        a = a0+(a1-a0)*j/count
        step = (a1-a0)/count
        for t,r in ((0,.80),(.20,.80),(.27,1),(.72,1),(.80,.80)):
            aa = a+t*step
            curve.append((radius*r*math.sin(aa),radius*r*math.cos(aa)))
    curve += [(radius*.67*math.sin(a1+.13),radius*.67*math.cos(a1+.13)),
              (radius*.54*math.sin(a1-.15),radius*.54*math.cos(a1-.15))]
    curve += [(hole*math.sin(a1-(a1-a0)*j/24),hole*math.cos(a1-(a1-a0)*j/24)) for j in range(25)]
    curve += [(radius*.53*math.sin(a0+.18),radius*.53*math.cos(a0+.18)),
              (radius*.69*math.sin(a0-.10),radius*.69*math.cos(a0-.10))]
    slab('Mutilated brass tooth rim',curve,depth,'brass')
    arc('Recessed dark gear web',hole+.005,radius*.71,a0+.05,a1-.06,.045,'gold_dark',y=depth*.55,n=22)
    arc('Raised intact gold rim',radius*.65,radius*.82,a0+.06,a1-.07,.04,'gold',y=depth*.80,n=22)
    for a in (-.9,0,.94):
        r = radius*.54
        box('Heavy radial gear rib',(r*math.sin(a),depth*.9,r*math.cos(a)),
            (.048,.05,radius*.45),'iron_dark',rot=(0,a,0))
    # Broken cap seam is broad enough to survive the gameplay camera.
    pts = [(radius*r*math.sin(a1+da),depth*.65,radius*r*math.cos(a1+da))
           for r,da in ((.89,0),(.70,.11),(.53,-.12),(.42,.03))]
    tube('Gold light at gear fracture',pts,.021,'crack_gold',4)
    group_place(start,loc,rot)


ROMAN = ['XII','I','II','III','IV','V','VI','VII','VIII','IX','X','XI']


def numeral(word, angle):
    # Relief strokes only: never FONT objects. Stroke section >= 4 cm.
    pitch = .062
    for i,ch in enumerate(word):
        # +X is screen-left when looking at a +Y-facing dial.
        x = -(i-(len(word)-1)/2)*pitch
        strokes = [(0,0)] if ch == 'I' else [(-.018,-.30),(.018,.30)]
        if ch == 'X': strokes = [(0,-.38),(0,.38)]
        for dx,tilt in strokes:
            obj = box('Roman numeral relief stroke',(x+dx,.156,.95),(.04,.042,.145),
                      'dial_mark',rot=(0,tilt,0),bevel=False)
            place(obj,rot=(0,angle,0))


def halo():
    for j in range(12):
        start = len(PARTS)
        a = -TAU*j/12
        half = math.radians(12.6 if j not in (2,5,9) else 11.2)
        arc('Heavy brass dial segment',.785,1.115,a-half,a+half,.17,'brass',jagged=True)
        arc('Dark undercut moulding',.805,1.09,a-half+.012,a+half-.012,.055,'gold_dark',y=.095)
        arc('Inset ivory dial plate',.842,1.055,a-half+.026,a+half-.026,.045,'dial_face',y=.126,n=5)
        for inner,outer in ((.775,.827),(1.073,1.128)):
            arc('Raised gold archivolt',inner,outer,a-half+.01,a+half-.01,.06,'gold',y=.10)
        numeral(ROMAN[j],a)
        if j in (1,3,6,8,10):
            pts = [(r*math.sin(a+da),.159,r*math.cos(a+da))
                   for r,da in ((.79,-.09),(.90,-.055),(.96,-.085),(1.03,-.06),(1.12,-.075))]
            tube('Branching halo gold fracture',pts,.021,'crack_gold',4)
        if j in (2,5,9):
            # Move the entire segment, including its dial and numerals.
            distance = {2:.13,5:.10,9:.16}[j]
            group_place(start,(distance*math.sin(a),-.045 if j==5 else .035,distance*math.cos(a)),
                        (0,(-.035 if j==2 else .025),.025 if j==9 else -.015))
    for j in range(4):
        a = j*math.pi/2
        outline = [(-.105,1.04),(-.075,1.19),(0,1.39),(.064,1.20),(.11,1.07),(0,1.12)]
        slab('Four cardinal spear points',outline,.09,'gold',rot=(0,a,0))
        shard('Inset cardinal crystal',(0,0,1.12),.075,.06,.16,'crystal') if j==0 else None
    # 11:59: hands lie screen-left of twelve, viewed from +Y.
    for angle,length,width,y in ((math.radians(6),.86,.064,.205),
                                  (math.radians(.5),.65,.10,.27)):
        contour = [(-width*.5,-.22),(-width*.85,-.15),(-width*.45,-.08),
                   (-width*.45,length-.15),(-width,length-.16),(0,length),
                   (width,length-.16),(width*.45,length-.15),(width*.45,-.08),(width*.8,-.15),(width*.5,-.22)]
        slab('Frozen 11:59 clock hand',contour,.05,'gold_dark',loc=(0,y,0),rot=(0,angle,0))
    cylinder('Gold hand pivot',(0,.31,0),.11,.065,'gold',n=16,rot=(math.pi/2,0,0))
    cylinder('Violet pivot cabochon',(0,.355,0),.061,.04,'crystal',n=8,rot=(math.pi/2,0,0))


def hourglass(loc=(0,0,0), scale=1, rot=(0,0,0)):
    start = len(PARTS)
    for sign in (-1,1):
        z = sign*.148
        cylinder('Hourglass crown foot',(0,0,z),.108,.054,'gold',n=12)
        ring('Hourglass stepped dark moulding',.098,.04,.04,'gold_dark',loc=(0,0,sign*.115),n=12)
        # Broken glass is an open-front porcelain shell, revealing the sand.
        # Each curved panel is individually capped, so exports remain manifold.
        profile = [(.029,.018),(.047,.052),(.079,.096),(.071,.121)]
        for panel in range(6):
            if panel in (1,2):
                continue  # genuinely missing front third, no fake transparency
            a0 = panel*TAU/6; a1 = (panel+1)*TAU/6-.045
            verts = []
            for offset in (0,-.04):
                for r,zz in profile:
                    for a in (a0,(a0+a1)/2,a1):
                        rr = max(.009,r+offset)
                        verts.append((rr*math.cos(a),rr*math.sin(a),sign*zz))
            faces = []
            for base in (0,12):
                faces += [(base+k*3+j,base+k*3+j+1,base+(k+1)*3+j+1,base+(k+1)*3+j)
                          for k in range(3) for j in range(2)]
            perimeter = [0,1,2,5,8,11,10,9,6,3]
            faces += [(a,b,b+12,a+12) for a,b in zip(perimeter,perimeter[1:]+perimeter[:1])]
            mesh('Fractured porcelain glass bulb',verts,faces,'porcelain')
        # Sand mounds exposed through missing panes.
        cylinder('Sand reservoir',(0,0,sign*.095),.045,.047,'sand',n=12,bevel=False)
        shard('Hourglass sand mound',(0,0,sign*.054),.075,.075,.045,'sand',rot=(0,math.pi if sign<0 else 0,0))
    for j in range(3):
        a = TAU*j/3+.30
        x,y = .097*math.cos(a),.097*math.sin(a)
        if j == 1:
            tube('Snapped frame lower prong',[(x,y,-.123),(x+.006,y,-.062),(x-.02,y,-.023)],.021,'gold',6)
            tube('Detached upper frame prong',[(x+.025,y+.01,.043),(x+.012,y,.085),(x,y,.123)],.021,'gold',6)
        else:
            tube('Hourglass gold pillar',[(x,y,-.123),(x*1.13,y*1.13,0),(x,y,.123)],.021,'gold',6)
    tube('Frozen sand stream',[(0,0,-.038),(0,0,.035)],.021,'sand',6)
    tube('Bulb jagged gold crack',[(-.06,.052,.114),(-.042,.058,.083),(-.056,.052,.068),(-.023,.029,.025)],.021,'crack_gold',4)
    group_place(start,loc,rot,scale)


def pendant(loc):
    """A deliberately chunky 0.19 m charm; no scaled-down micro details."""
    start = len(PARTS)
    for sign in (-1,1):
        cylinder('Pendant gold end cap',(0,0,sign*.075),.068,.04,'gold',n=10)
        # Glass has a broad pearl-like facet; exposed sand sits in front.
        obj = shard('Pendant porcelain bulb',(-.007,-.012,sign*.020),.075,.06,.042,
                    'porcelain',rot=(0,math.pi if sign<0 else 0,0))
        cylinder('Pendant visible sand',(0,.018,sign*.025),.025,.04,'sand',n=8,bevel=False)
    for x in (-.055,.055):
        tube('Pendant thick frame',[(x,0,-.059),(x*1.05,0,0),(x,0,.059)],.021,'gold',6)
    tube('Pendant split gold collar',[(-.04,.035,.069),(-.013,.040,.038),(.015,.03,.020)],.021,'crack_gold',4)
    group_place(start,loc,(0,.13,0))


def armour_shell(label, outline, depth, role, loc=(0,0,0)):
    """Closed convex-front carapace, with a raised angular central ridge."""
    n = len(outline)
    v = [(x,y,z) for y in (-depth/2,depth/2) for x,z in outline]
    v.append((.10,depth*.95,.025))
    f = [tuple(reversed(range(n)))]
    f += [(n+j,n+(j+1)%n,2*n) for j in range(n)]
    f += [(j,(j+1)%n,n+(j+1)%n,n+j) for j in range(n)]
    return place(mesh(label,v,f,role,True),loc)


def pauldron():
    # Origin is the shoulder joint; outward is +X. Right is a true mesh mirror.
    outline = [(-.17,-.10),(-.13,.13),(.03,.20),(.25,.16),(.40,.04),
               (.43,-.07),(.35,-.06),(.31,-.17),(.23,-.105),(.13,-.19),(.04,-.12),(-.07,-.16)]
    armour_shell('Main dark shoulder carapace',outline,.36,'void_stone_dark')
    tube('Cast gold shoulder crest',[(x,.205,z) for x,z in outline[:6]],.024,'gold',6)
    for layer,(dx,dz,s) in enumerate(((.015,-.02,1),(.06,-.10,.86),(.10,-.19,.68))):
        contour = [(-.12*s+dx,.11*s+dz),(.11*s+dx,.15*s+dz),(.32*s+dx,.03*s+dz),
                   (.37*s+dx,-.06*s+dz),(.25*s+dx,-.15*s+dz),(.17*s+dx,-.09*s+dz),
                   (.07*s+dx,-.18*s+dz),(-.06*s+dx,-.12*s+dz)]
        armour_shell('Overlapping iron shoulder lames',contour,.10,'iron_dark',loc=(0,.22+layer*.045,0))
        pts = [(x,.28+layer*.045,z) for x,z in contour[2:7]]
        tube('Broad scalloped gold shoulder trim',pts,.024,'gold',6)
    # Clear violet inset under the crystal rupture.
    slab('Exposed violet inner mantle',[(-.08,-.04),(.04,.12),(.23,.08),(.32,-.03),(.16,-.12)],
         .05,'violet_dark',loc=(0,.355,.025))
    gear_chunk(.235,.10,.07,12,loc=(.125,-.015,.16),rot=(.15,0,-.11))
    tube('Shoulder crystal fissure',[(-.02,.16,.15),(.07,.19,.11),(.11,.21,.15),(.23,.16,.10)],.023,'crack_gold',4)
    for x,y,z,w,h,lean in ((.025,.13,.14,.115,.27,-.28),(.14,.12,.15,.11,.30,.14),(.24,.10,.10,.10,.24,.40)):
        shard('Three shoulder rupture crystals',(x,y,z),w,.09,h,'crystal',rot=(.10,lean,0))
    # Three chunky interlocking links (4 cm bar sections).
    for j in range(3):
        ring('Hanging shoulder chain link',.052,.04,.045,'gold',loc=(.26,.33,-.22-j*.065),
             rot=(math.pi/2,0,0 if j%2==0 else math.pi/2),n=10)
    pendant((.26,.34,-.475))


def crown():
    # Separate cracked castings around an oval hood band, origin at its bottom.
    for j in range(7):
        a = TAU*j/7
        start = len(PARTS)
        arc('Cracked crown band casting',.245,.315,a-.40,a+.40,.085,'gold_dark',jagged=True,n=5)
        arc('Crown band ivory inlay',.264,.302,a-.34,a+.34,.04,'violet_dark',y=.056,n=4)
        for r0,r1 in ((.235,.275),(.305,.345)):
            arc('Crown band gold lip',r0,r1,a-.38,a+.38,.045,'gold',y=.025,n=5)
        # Rotate XZ arc onto horizontal XY and lift its extrusion above Z=0.
        group_place(start,(0,0,.045),(math.pi/2,0,0))
    heights = [.45,.23,.36,.17,.41,.26,.32]
    for j,h in enumerate(heights):
        a = TAU*j/7+.04
        x,y = .288*math.sin(a),.288*math.cos(a)
        start = len(PARTS)
        shard('Seven uneven crown spires',(0,0,0),.145,.105,h,
              'crystal' if j in (1,3,5) else 'gold',rot=(0,random.uniform(-.14,.14),0))
        shard('Inset contrasting spire facet',(0,.053,.045),.075,.043,h*.52,
              'gold_dark' if j in (1,3,5) else 'crystal')
        group_place(start,(x,y,.08),(math.radians(-7),0,-a))
    tube('Broad glowing crown band fracture',[(-.27,.10,.02),(-.24,.16,.064),(-.28,.19,.095)],.021,'crack_gold',4)


def cape_shards():
    # Back semicircle Y <= 0; every torn tongue has distinct notches and folds.
    for j in range(14):
        a = -math.pi/2+math.pi*j/13+random.uniform(-.05,.05)
        r = .55+random.uniform(-.035,.09)
        h = random.uniform(.19,.37)
        w = random.uniform(.12,.19)
        z = random.uniform(.005,.47-h)
        contour = [(-w*.5,h),(.12*w,h*.94),(.52*w,h*.83),(.31*w,h*.52),
                   (.47*w,h*.22),(.11*w,h*.32),(.20*w,0),(-.17*w,h*.16),
                   (-.42*w,h*.07),(-.30*w,h*.52)]
        start = len(PARTS)
        obj = slab('Fourteen torn cape tongues',contour,.05,
                   'night_sky' if j%2==0 else 'violet_dark',bevel=True)
        # Large central fold is contrasting fabric, not a stack of flat chips.
        slab('Broad angular cape fold',[(0,h*.87),(.23*w,h*.68),(.06*w,h*.20),(-.18*w,h*.44)],
             .045,'violet_dark' if j%2==0 else 'night_sky',loc=(0,.035,0))
        if j%3==0:
            tube('Luminous torn hem',[(x,.036,zz) for x,zz in contour[5:9]],.021,'crack_gold',4)
        group_place(start,(r*math.sin(a),-r*math.cos(a),z),
                    (random.uniform(-.3,.3),random.uniform(-.24,.24),-a+random.uniform(-.2,.2)))
    for j in range(8):
        a = -1.5+3*j/7
        r = random.uniform(.64,.81)
        w,h = random.uniform(.07,.13),random.uniform(.10,.21)
        contour = [(-w*.5,0),(-w*.36,h*.62),(.10*w,h),(.5*w,h*.43),(.19*w,h*.22)]
        obj = slab('Drifting mirror cape splinter',contour,.045,'mirror',
                   loc=(r*math.sin(a),-r*math.cos(a),random.uniform(.06,.28)),
                   rot=(random.uniform(-.4,.4),.3,-a),bevel=True)
        roles(obj,['mirror_back','gold'],[1,0]+[2 if k%2==0 else 1 for k in range(5)])
    for j in range(7):
        a = random.uniform(-1.45,1.45); r = random.uniform(.48,.83)
        shard('White stardust',(r*math.sin(a),-r*math.cos(a),random.uniform(.10,.44)),
              .045,.045,.065,'starlight',rot=(0,.4,.3))


SPECS = [
    ('Halo',halo,6000,'ring centre'),
    ('Pauldron_L',pauldron,3500,'shoulder joint'),
    ('Pauldron_R',pauldron,3500,'shoulder joint'),
    ('Crown',crown,2500,'band centre bottom'),
    ('CapeShards',cape_shards,3000,'ground centre'),
    ('Orbit_Hourglass',hourglass,1200,'centre'),
    ('Orbit_Gear',gear_chunk,1200,'centre'),
]


def triangles(obj):
    obj.data.calc_loop_triangles()
    return len(obj.data.loop_triangles)


def recalc(obj):
    bm = bmesh.new(); bm.from_mesh(obj.data)
    bmesh.ops.remove_doubles(bm,verts=list(bm.verts),dist=.000001)
    bmesh.ops.recalc_face_normals(bm,faces=list(bm.faces))
    bm.to_mesh(obj.data); bm.free(); obj.data.update()


def finish(name,budget,origin):
    bpy.ops.object.select_all(action='DESELECT')
    for part in PARTS:
        bpy.context.view_layer.objects.active = part
        part.select_set(True)
        recalc(part)
        for mod in list(part.modifiers): bpy.ops.object.modifier_apply(modifier=mod.name)
        part.select_set(False)
    for part in PARTS: part.select_set(True)
    bpy.context.view_layer.objects.active = PARTS[0]
    if len(PARTS)>1: bpy.ops.object.join()
    obj = bpy.context.object
    obj.name, obj.data.name = name,name+'_Mesh'
    if triangles(obj)>budget:
        mod = obj.modifiers.new('Preserve material borders and silhouette','DECIMATE')
        mod.ratio = (budget-40)/triangles(obj)
        mod.delimit = {'MATERIAL'}
        bpy.ops.object.modifier_apply(modifier=mod.name)
    recalc(obj)
    if name == 'Crown':
        # Keep the specified bottom-centre pivot exact after bevel/decimation.
        bottom = min(v.co.z for v in obj.data.vertices)
        obj.data.transform(Matrix.Translation((0,0,-bottom)))
    for p in obj.data.polygons: p.use_smooth = False
    bpy.context.view_layer.update()
    assert obj.location.length<1e-7 and tuple(obj.scale)==(1,1,1),name
    assert triangles(obj)<=budget,(name,triangles(obj))
    bm = bmesh.new(); bm.from_mesh(obj.data)
    assert not any(not e.is_manifold for e in bm.edges),name+' non-manifold component'
    bm.free()
    areas = {}
    for p in obj.data.polygons:
        role = obj.data.materials[p.material_index].name
        areas[role] = areas.get(role,0)+p.area
    shares = {role:round(area/sum(areas.values()),4) for role,area in areas.items()}
    assert len(areas)>=4,(name,shares)
    assert max(shares.values())<=.55,(name,shares)
    assert all(math.isfinite(c) for v in obj.data.vertices for c in v.co)
    for mat in obj.data.materials:
        assert mat.name in PALETTE
        assert tuple(mat.diffuse_color)==(.5,.5,.5,1)
        assert tuple(mat.node_tree.nodes['Principled BSDF'].inputs['Base Color'].default_value)==(.5,.5,.5,1)
    obj['reference_body_height_m'] = 2.2
    obj['origin_description'],obj['front'] = origin,'+Y'
    obj['triangle_count'] = triangles(obj)
    dest = OUTPUT/f'Kit_BP_{name}.fbx'
    bpy.ops.export_scene.fbx(filepath=str(dest),use_selection=True,
        axis_forward='-Z',axis_up='Y',apply_scale_options='FBX_SCALE_UNITS',
        bake_space_transform=True,object_types={'MESH'},use_mesh_modifiers=True,
        mesh_smooth_type='FACE',add_leaf_bones=False,bake_anim=False,use_custom_props=True)
    assert dest.is_file() and dest.stat().st_size>1024
    bounds = [[round(fn(v.co[i] for v in obj.data.vertices),5) for i in range(3)] for fn in (min,max)]
    RECORDS.append(dict(name=name,triangles=triangles(obj),budget=budget,
                        dimensions_m=[round(d,5) for d in obj.dimensions],bounds_m=bounds,
                        origin=origin,material_surface_fractions=shares,fbx=str(dest.relative_to(ROOT))))
    emit(f'EXPORTED {name} {triangles(obj)} {tuple(round(d,4) for d in obj.dimensions)}')
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
    shade.light,shade.color_type = 'FLAT','MATERIAL'
    shade.show_shadows,shade.show_cavity = False,True
    shade.cavity_type = 'BOTH'
    shade.curvature_ridge_factor,shade.curvature_valley_factor = 1.25,1.15
    shade.show_specular_highlight = False
    shade.background_type = 'WORLD'
    scene.world = bpy.data.worlds.new('Preview indigo')
    scene.world.color = (.024,.030,.054)
    helpers = []
    try:
        for role,mat in MATS.items(): mat.diffuse_color = (*PALETTE[role],1)
        layout = [(2.0,-1.0,1.3),(-.05,-1.0,.65),(-1.4,-1.0,.65),(-2.85,-1.0,.52),
                  (1.6,1.25,.025),(-.6,1.25,.27),(-2.55,1.25,.30)]
        for obj,loc in zip(objects,layout): obj.location = loc
        cam = bpy.data.objects.new('PREVIEW ONLY camera',bpy.data.cameras.new('Review camera'))
        scene.collection.objects.link(cam); helpers.append(cam); scene.camera = cam
        target = Vector((.60,0,.65))
        cam.location = target+Vector((0,12,12*math.tan(math.radians(55))))
        cam.rotation_euler = (target-cam.location).to_track_quat('-Z','Y').to_euler()
        cam.data.type,cam.data.ortho_scale = 'ORTHO',9.3
        toward = (cam.location-target).normalized()
        # Preview-only helpers are created after all exports and removed below.
        gray = bpy.data.materials.new('PREVIEW ONLY capsule grey')
        gray.diffuse_color = (.39,.42,.48,1)
        start = len(PARTS)
        # True capsule: 1.56 straight length plus two .32 m hemispheres = 2.2 m.
        profile = []
        for j in range(7):
            a = -math.pi/2+math.pi/2*j/6
            profile.append((max(.00001,.32*math.cos(a)),.32+.32*math.sin(a)))
        for j in range(7):
            a = math.pi/2*j/6
            profile.append((max(.00001,.32*math.cos(a)),1.88+.32*math.sin(a)))
        cap = lathe('PREVIEW ONLY 2.2 m capsule',profile,'iron_dark',n=32,loc=(3.92,-1,0))
        cap.data.materials.clear(); cap.data.materials.append(gray)
        for face in cap.data.polygons: face.use_smooth = True
        helpers.append(cap)

        def caption(body,loc,size=.10,role='starlight'):
            data = bpy.data.curves.new('Review label','FONT')
            data.body,data.size,data.align_x = body,size,'CENTER'
            data.materials.append(MATS[role])
            label = bpy.data.objects.new('PREVIEW ONLY label',data)
            scene.collection.objects.link(label); helpers.append(label)
            label.location = loc
            label.rotation_euler = cam.rotation_euler
            return label

        # Camera-plane labels are positioned below each footprint in screen space.
        caption('L O R D   O F   T I M E   /   B O S S   R E G A L I A',(.6,-1.98,1.97),.155)
        caption('2.2 m REFERENCE  /  BONE-LOCAL ORIGINS  /  +Y FRONT  /  5.6 m BOSS = 2.545x',
                (.6,-1.80,1.88),.072,'gold')
        caption('2.2 m\nREFERENCE',(3.92,-.50,-.10),.09)
        labels = ['HALO / 11:59','PAULDRON L','PAULDRON R','CROWN','CAPE SHARDS','ORBIT HOURGLASS','ORBIT GEAR']
        for obj,loc,label,rec in zip(objects,layout,labels,RECORDS):
            x,y,z = loc
            ly = -.33 if y<0 else 1.90
            lz = -.10
            caption(label,(x,ly,lz),.09)
            caption(f'{rec["triangles"]:,} TRI',(x,ly+.16,lz-.10),.062,'gold')
            # Magenta cross denotes the attachment pivot, without altering FBX.
            p = Vector(loc)+toward*.36
            for size in ((.09,.018,.018),(.018,.018,.09)):
                helpers.append(box('PREVIEW ONLY origin',p,size,'magenta',bevel=False))
        caption('BROKEN DIAL  /  RUPTURED ARMOUR  /  VOID-TORN HEM',(.6,2.50,.05),.09,'gold')
        PREVIEW.parent.mkdir(parents=True,exist_ok=True)
        bpy.ops.render.render(write_still=True)
        assert PREVIEW.is_file() and PREVIEW.stat().st_size>1024
        emit(f'PREVIEW {PREVIEW}')
    finally:
        scene.camera = None
        for helper in helpers: bpy.data.objects.remove(helper,do_unlink=True)
        for obj in objects: obj.location = (0,0,0)
        for mat in MATS.values(): mat.diffuse_color = (.5,.5,.5,1)


def audit_exports():
    """Round-trip the actual FBXs in clean scenes, including shader checks."""
    for rec in RECORDS:
        bpy.ops.wm.read_factory_settings(use_empty=True)
        # Materials are global datablocks; remove orphans to preserve exact names.
        for mat in list(bpy.data.materials): bpy.data.materials.remove(mat)
        bpy.ops.import_scene.fbx(filepath=str(ROOT/rec['fbx']),use_anim=False)
        imported = list(bpy.context.scene.objects)
        assert len(imported)==1 and imported[0].type=='MESH',(rec['name'],'object count')
        obj = imported[0]
        assert obj.name==rec['name'],(obj.name,rec['name'])
        assert obj.location.length<.00001,(obj.name,'origin')
        assert triangles(obj)==rec['triangles'],(obj.name,'FBX triangles')
        world = [obj.matrix_world@v.co for v in obj.data.vertices]
        bounds = [[fn(v[i] for v in world) for i in range(3)] for fn in (min,max)]
        assert all(abs(a-b)<.0001 for aa,bb in zip(bounds,rec['bounds_m']) for a,b in zip(aa,bb)),(obj.name,bounds)
        for mat in obj.data.materials:
            assert mat.name in PALETTE,(obj.name,mat.name)
            shader = mat.node_tree.nodes.get('Principled BSDF')
            assert shader is not None
            assert all(abs(c-.5)<.00001 for c in shader.inputs['Base Color'].default_value[:3]),(obj.name,mat.name,'shader grey')
            assert not any(node.type=='TEX_IMAGE' for node in mat.node_tree.nodes)
        rec['fbx_roundtrip_verified'] = True
        emit(f'VERIFIED {obj.name}: one mesh, grey role shaders, correct origin/bounds/triangles')


def main():
    global PARTS
    bpy.ops.wm.read_factory_settings(use_empty=True)
    for mat in list(bpy.data.materials): bpy.data.materials.remove(mat)
    scene = bpy.context.scene
    scene.unit_settings.system,scene.unit_settings.scale_length = 'METRIC',1
    materials(); OUTPUT.mkdir(parents=True,exist_ok=True)
    objects = []
    for i,(name,build,budget,origin) in enumerate(SPECS):
        PARTS = []
        # Both pauldrons get the same seed so the mesh mirror is exact.
        random.seed(51159+(1 if name.startswith('Pauldron') else i)*7919)
        if name == 'Pauldron_R':
            left = objects[-1]
            right = bpy.data.objects.new('Mirrored finished shoulder',left.data.copy())
            scene.collection.objects.link(right)
            right.data.transform(Matrix.Diagonal((-1,1,1,1)))
            PARTS = [right]
        else:
            build()
        objects.append(finish(name,budget,origin))
    assert len(scene.objects)==7 and all(o.type=='MESH' for o in scene.objects)
    left,right = objects[1:3]
    assert len(left.data.vertices)==len(right.data.vertices)
    assert all(abs(a.co.x+b.co.x)<1e-6 and abs(a.co.y-b.co.y)<1e-6 and abs(a.co.z-b.co.z)<1e-6
               for a,b in zip(left.data.vertices,right.data.vertices)),'Shoulder mirror mismatch'
    preview(objects)
    assert len(scene.objects)==7 and all(o.type=='MESH' for o in scene.objects)
    assert all(o.location.length<1e-7 for o in objects)
    audit_exports()
    REPORT.write_text(json.dumps(RECORDS,indent=2),encoding='utf-8')
    emit('DONE')
    LOG.write_text('\n'.join(LOG_LINES)+'\n',encoding='utf-8')


if __name__ == '__main__':
    main()
