# 시계탑 전투 소품 11종을 만들고 FBX와 검토용 미리보기를 출력합니다.
# 실행: "C:/Program Files/Blender Foundation/Blender 5.3/blender.exe" -b --factory-startup -P Art/tools/blender_kit_ch4_props.py
"""Deterministic Blender 5.x clockwork props; metres, +Y front, grey role shaders."""
import math
import random
from pathlib import Path

import bmesh
import bpy
from mathutils import Euler, Matrix, Vector

ROOT = Path(__file__).resolve().parents[2]
OUTPUT = ROOT / 'Game/Assets/Art/Characters/Ch4'
PREVIEW = ROOT / 'Art/tools/specs/blender_kit_ch4_props_preview.png'
TAU = math.tau
MATS, PARTS = {}, []
PALETTE = {
    'steel': (.48, .59, .70), 'steel_dark': (.20, .28, .37),
    'iron': (.23, .29, .37), 'iron_dark': (.075, .105, .16),
    'silver': (.77, .84, .89), 'brass': (.68, .43, .15),
    'brass_dark': (.34, .21, .09), 'gold': (.98, .70, .24),
    'copper': (.53, .29, .17), 'navy_paint': (.075, .16, .30),
    'navy_dark': (.035, .065, .13), 'red_lacquer': (.75, .055, .08),
    'red_dark': (.27, .025, .045), 'gem_red': (.95, .10, .13),
    'wood': (.37, .22, .13), 'wood_dark': (.14, .075, .055),
    'stone': (.48, .49, .56), 'stone_dark': (.25, .28, .35),
    'dial_face': (.91, .86, .70), 'dial_mark': (.065, .085, .12),
    'glass': (.42, .72, .83), 'starlight': (.84, .94, 1.0),
    'night_sky': (.018, .035, .10), 'porcelain': (.94, .91, .84),
    'ribbon_red': (.80, .045, .10), 'velvet': (.34, .025, .065),
    'lamp_warm': (1.0, .79, .36),
}


def materials():
    for role in PALETTE:
        mat = bpy.data.materials.new(role)
        mat.use_nodes = True
        mat.diffuse_color = (.5, .5, .5, 1)
        bsdf = mat.node_tree.nodes.get('Principled BSDF')
        bsdf.inputs['Base Color'].default_value = (.5, .5, .5, 1)
        bsdf.inputs['Metallic'].default_value = 0
        bsdf.inputs['Roughness'].default_value = .8
        MATS[role] = mat


def mesh(label, verts, faces, role, bevel=False):
    data = bpy.data.meshes.new(label)
    data.from_pydata(verts, [], faces)
    data.update()
    obj = bpy.data.objects.new(label, data)
    bpy.context.collection.objects.link(obj)
    data.materials.append(MATS[role])
    if bevel:
        mod = obj.modifiers.new('Worn chamfer', 'BEVEL')
        mod.width, mod.segments = .02, 2
        mod.limit_method, mod.angle_limit = 'ANGLE', math.radians(70)
        mod.use_clamp_overlap = True
    PARTS.append(obj)
    return obj


def place(obj, loc=(0, 0, 0), rot=(0, 0, 0)):
    obj.data.transform(Matrix.Translation(Vector(loc)) @ Euler(rot).to_matrix().to_4x4())
    return obj


def box(label, loc, size, role, rot=(0, 0, 0), bevel=True):
    x, y, z = (v / 2 for v in size)
    v = [(-x,-y,-z),(x,-y,-z),(x,y,-z),(-x,y,-z),
         (-x,-y,z),(x,-y,z),(x,y,z),(-x,y,z)]
    f = [(0,3,2,1),(4,5,6,7),(0,1,5,4),(1,2,6,5),(2,3,7,6),(3,0,4,7)]
    return place(mesh(label, v, f, role, bevel), loc, rot)


def cylinder(label, loc, radius, depth, role, n=20, rot=(0,0,0), bevel=False):
    v = [(radius*math.cos(TAU*j/n), radius*math.sin(TAU*j/n), z)
         for z in (-depth/2, depth/2) for j in range(n)]
    f = [tuple(reversed(range(n))), tuple(range(n,2*n))]
    f += [(j,(j+1)%n,(j+1)%n+n,j+n) for j in range(n)]
    return place(mesh(label, v, f, role, bevel), loc, rot)


def lathe(label, profile, role, n=32, loc=(0,0,0), rot=(0,0,0), bevel=False):
    v = [(r*math.cos(TAU*j/n), r*math.sin(TAU*j/n), z)
         for r,z in profile for j in range(n)]
    f = [(k*n+j,k*n+(j+1)%n,((k+1)%len(profile))*n+(j+1)%n,
          ((k+1)%len(profile))*n+j) for k in range(len(profile)) for j in range(n)]
    return place(mesh(label,v,f,role,bevel),loc,rot)


def ring(label, radius, width, z, height, role, n=32, loc=(0,0,0), rot=(0,0,0)):
    return lathe(label,[(radius-width,z),(radius,z),(radius,z+height),
                        (radius-width,z+height)],role,n,loc,rot)


def gear(label, root, tip, hole, depth, teeth, role, loc=(0,0,0), rot=(0,0,0), bevel=False):
    """One annular mesh, with square radial teeth and a genuinely open centre."""
    contour = []
    half = tip * math.sin(math.pi / teeth) * .46
    for j in range(teeth):
        a = TAU*j/teeth
        for r,t in ((root,-half),(tip,-half),(tip,half),(root,half)):
            contour.append((r*math.cos(a)-t*math.sin(a),r*math.sin(a)+t*math.cos(a)))
    n = len(contour)
    inner = [(hole*math.cos(math.atan2(y,x)),hole*math.sin(math.atan2(y,x))) for x,y in contour]
    v = [(x,y,z) for z,curve in ((-depth/2,contour),(depth/2,contour),
                                (depth/2,inner),(-depth/2,inner)) for x,y in curve]
    f = [(k*n+j,k*n+(j+1)%n,((k+1)%4)*n+(j+1)%n,((k+1)%4)*n+j)
         for k in range(4) for j in range(n)]
    return place(mesh(label,v,f,role,bevel),loc,rot)


def tube(label, points, radius, role, n=6):
    v=[]
    for i,p in enumerate(points):
        t=(Vector(points[min(i+1,len(points)-1)])-Vector(points[max(i-1,0)])).normalized()
        ref=Vector((0,1,0)) if abs(t.y)<.9 else Vector((1,0,0))
        u=ref.cross(t).normalized(); w=t.cross(u)
        v += [tuple(Vector(p)+radius*(math.cos(TAU*j/n)*u+math.sin(TAU*j/n)*w)) for j in range(n)]
    f=[tuple(reversed(range(n))),tuple(range((len(points)-1)*n,len(points)*n))]
    f += [(i*n+j,i*n+(j+1)%n,(i+1)*n+(j+1)%n,(i+1)*n+j)
          for i in range(len(points)-1) for j in range(n)]
    return mesh(label,v,f,role)


def ribbon(label, points, width, thickness, role):
    """Rectangular vertical steel strip, not a round wire spring."""
    v=[]
    for i,p in enumerate(points):
        t=(Vector(points[min(i+1,len(points)-1)])-Vector(points[max(0,i-1)])).normalized()
        side=Vector((-t.y,t.x,0)).normalized()
        for s,z in ((-1,-1),(1,-1),(1,1),(-1,1)):
            v.append(tuple(Vector(p)+side*s*thickness/2+Vector((0,0,z*width/2))))
    f=[(3,2,1,0),tuple(range(len(v)-4,len(v)))]
    f += [(4*i+j,4*i+(j+1)%4,4*(i+1)+(j+1)%4,4*(i+1)+j)
          for i in range(len(points)-1) for j in range(4)]
    return mesh(label,v,f,role)


def rivet(loc, role='brass_dark', radius=.04, rot=(0,0,0)):
    cylinder('Broad rivet',loc,radius,.04,role,n=8,rot=rot)


def cut_at_floor(obj):
    """Cap a half-buried component so it cannot lift the rest of its prop."""
    bm=bmesh.new(); bm.from_mesh(obj.data)
    bmesh.ops.bisect_plane(bm,geom=list(bm.verts)+list(bm.edges)+list(bm.faces),
        dist=1e-6,plane_co=(0,0,0),plane_no=(0,0,1),clear_inner=True)
    edges=[e for e in bm.edges if e.is_boundary]
    if edges: bmesh.ops.holes_fill(bm,edges=edges,sides=0)
    bm.to_mesh(obj.data); bm.free(); obj.data.update()


def key(loc, scale=1, rot=(0,0,0), role='brass'):
    start=len(PARTS)
    box('Winding key stem',(0,0,-.16*scale),(.10*scale,.12*scale,.38*scale),role)
    for side in (-1,1):
        ring('Open round key bow',.19*scale,.065*scale,0,.08*scale,role,n=16,
             loc=(side*.145*scale,0,.09*scale),rot=(math.pi/2,0,0))
    cylinder('Key centre boss',(0,.035*scale,.05*scale),.085*scale,.11*scale,
             'gold',n=10,rot=(math.pi/2,0,0))
    for obj in PARTS[start:]:
        place(obj,loc,rot)


def clock(loc, radius, cracked=False):
    x,y,z=loc
    ring('Clock bezel',radius,.07,-.06,.13,'brass',n=32,loc=loc,rot=(math.pi/2,0,0))
    cylinder('Ivory dial',(x,y+.015,z),radius-.065,.06,'dial_face',n=32,rot=(math.pi/2,0,0))
    for j in range(12):
        a=TAU*j/12
        box('Raised hour tick',(x+.77*radius*math.sin(a),y+.065,z+.77*radius*math.cos(a)),
            (.045,.04,.11 if j%3 else .15),'dial_mark',rot=(0,a,0),bevel=False)
    for a,length in ((-.65,.49*radius),(1.0,.66*radius)):
        box('Clock hand',(x+math.sin(a)*length/2,y+.093,z+math.cos(a)*length/2),
            (.055,.045,length),'dial_mark',rot=(0,a,0),bevel=False)
    cylinder('Dial pivot',(x,y+.12,z),.07,.05,'gold',n=12,rot=(math.pi/2,0,0))
    if cracked:
        tube('Broken dial fracture',[(x-.1,y+.052,z+radius*.85),(x+.04,y+.052,z+.22),
                                    (x-.13,y+.052,z+.05),(x+.16,y+.052,z-.35)],.022,'stone_dark',n=4)


def gear_floor():
    # Body lives below zero. Only the shootable axle exceeds the 3 cm relief.
    gear('24 integrated walkable square teeth',3.55,4.0,3.18,.115,24,'brass_dark',loc=(0,0,-.0625))
    ring('Flush brass rim',3.56,.37,-.12,.12,'brass',n=96)
    cylinder('Recess foundation',(0,0,-.075),3.20,.09,'steel',n=72)
    for j in range(6):
        a=TAU*j/6
        # Steel lands at floor level surround the genuinely lower navy wells.
        steel_angles=[a-.12+k*.08 for k in range(4)]
        outline=[(r*math.cos(t),r*math.sin(t)) for r,ts in
                 ((.82,steel_angles),(3.18,list(reversed(steel_angles)))) for t in ts]
        m=len(outline)
        verts=[(x,y,z) for z in (-.03,0) for x,y in outline]
        faces=[tuple(reversed(range(m))),tuple(range(m,2*m))]
        faces += [(k,(k+1)%m,(k+1)%m+m,k+m) for k in range(m)]
        mesh('Flush steel lands between recessed windows',verts,faces,'steel')
        # Recessed wedge windows alternate with the broad steel plate sectors.
        angles=[a+.12+k*(math.pi/3-.24)/10 for k in range(11)]
        curve=[(r*math.cos(t),r*math.sin(t)) for r,ts in ((1.05,angles),(3.12,list(reversed(angles)))) for t in ts]
        n=len(curve)
        v=[(x,y,z) for z in (-.04,-.012) for x,y in curve]
        f=[tuple(reversed(range(n))),tuple(range(n,2*n))]
        f += [(k,(k+1)%n,(k+1)%n+n,k+n) for k in range(n)]
        mesh('Six inset navy windows',v,f,'navy_paint')
        box('Six radial iron spokes',(1.95*math.cos(a),1.95*math.sin(a),.006),
            (2.45,.21,.048),'iron',rot=(0,0,a),bevel=False)
    ring('Seventy percent radius gold line',2.82,.045,0,.025,'gold',n=72)
    for j in range(12):
        a=TAU*j/12
        box('Twelve raised dial ticks',(2.98*math.cos(a),2.98*math.sin(a),.015),
            (.21,.07,.03),'dial_mark',rot=(0,0,a),bevel=False)
        rivet((3.38*math.cos(a),3.38*math.sin(a),.0),'gold',.055)
    cylinder('Shootable brass axle',(0,0,.175),.8,.35,'brass',n=40,bevel=True)
    ring('Reverse direction red band',.813,.045,.12,.13,'red_lacquer',n=40)
    cylinder('Axle gold cap',(0,0,.365),.77,.03,'gold',n=40)
    ring('Axle cap engraving',.58,.055,.38,.025,'brass_dark',n=32)
    cylinder('Axle centre bolt',(0,0,.41),.16,.06,'iron',n=6)


def wind_spring():
    box('Square dark base',(0,0,.07),(1,1,.14),'iron_dark')
    box('Red warning base band',(0,0,.15),(.93,.93,.08),'red_lacquer')
    box('Brass base top',(0,0,.205),(.87,.87,.04),'brass_dark')
    for x in (-.35,.35):
        for y in (-.35,.35): rivet((x,y,.25),'brass',.055)
    cylinder('Vertical winding arbor',(0,0,.73),.075,1.02,'brass',n=12)
    pts=[]
    for j in range(241):
        t=j/240; a=TAU*5*t; r=.105+.33*t
        pts.append((r*math.cos(a),r*math.sin(a),.36+.62*t))
    ribbon('Five loaded turns of flat steel mainspring',pts,.12,.04,'steel')
    # Red retaining strap makes the stored-energy silhouette legible.
    box('Retaining latch',(.38,0,.77),(.10,.12,.43),'iron')
    box('Retaining red catch',(.42,.015,.96),(.12,.17,.12),'red_lacquer')
    key((0,0,1.23),.70,(0,.30,-.25))


def gear_wheel():
    start=len(PARTS)
    gear('Sixteen teeth',.66,.79,.49,.30,16,'brass_dark')
    ring('Brass spoked wheel rim',.685,.19,-.16,.32,'brass',n=48)
    for j in range(5):
        a=TAU*j/5
        box('Five iron spokes',(.32*math.cos(a),.32*math.sin(a),0),(.55,.11,.22),
            'iron',rot=(0,0,a))
    for side in (-1,1):
        cylinder('Gold hub on both faces',(0,0,side*.19),.18,.08,'gold',n=16,bevel=True)
        cylinder('Hex axle cap',(0,0,side*.235),.085,.04,'steel',n=6)
        cylinder('Rolling red rim dot',(.58,0,side*.17),.085,.04,'red_lacquer',n=12)
    for obj in PARTS[start:]: place(obj,(0,0,.8),(0,math.pi/2,0))
    # Small separate chocks become disconnected components of the joined mesh.
    for y in (-.61,.61):
        v=[(-.24,y-.13,0),(.24,y-.13,0),(.24,y+.13,0),(-.24,y+.13,0),
           (-.24,y,.22),(.24,y,.22)]
        mesh('Wood wedge chock',v,[(0,3,2,1),(0,1,5,4),(3,4,5,2),(0,4,3),(1,2,5)],'wood_dark',True)


def old_bell():
    box('Stone frame slab',(0,0,.07),(1.6,1.6,.14),'stone')
    box('Inset slab course',(0,0,.16),(1.47,1.42,.07),'stone_dark')
    for x in (-.65,.65):
        box('Dark timber bell post',(x,0,1.23),(.22,.28,2.08),'wood_dark')
        for z in (.32,1.63,2.10):
            box('Iron post band',(x,0,z),(.26,.32,.11),'iron')
            rivet((x,.18,z),'brass_dark',.045,rot=(math.pi/2,0,0))
    box('Timber top crossbeam',(0,0,2.28),(1.59,.40,.24),'wood')
    box('Iron hanging yoke',(0,0,2.025),(.75,.30,.18),'iron_dark')
    cylinder('Bell suspension stem',(0,0,1.89),.11,.31,'iron_dark',n=12)
    lathe('Hollow old bronze bell',[(.55,.4),(.55,.51),(.46,.68),(.35,1.18),
          (.29,1.51),(.20,1.65),(.09,1.69),(.07,1.59),(.20,1.51),(.26,1.20),
          (.37,.68),(.47,.49),(.47,.4)],'copper',n=32,bevel=True)
    ring('Heavy brass bell lip',.57,.10,.40,.12,'brass',n=32)
    ring('Bell shoulder gold molding',.31,.045,1.43,.08,'gold',n=24)
    ring('Bell tick band lower rail',.445,.04,.73,.045,'brass_dark',n=32)
    for j in range(16):
        a=TAU*j/16
        box('Raised gold bell ticks',(.433*math.cos(a),.433*math.sin(a),.79),
            (.055,.05,.14),'gold',rot=(0,0,a),bevel=False)
    tube('Visible clapper rod',[(0,0,1.5),(0,.24,.54),(0,.46,.31)],.045,'iron_dark')
    cylinder('Visible brass clapper',(0,.46,.28),.115,.16,'brass',n=12,bevel=True)


def gear_crate():
    box('Crate dark inner seams',(0,0,.58),(1.42,1.04,1.16),'wood_dark')
    for y in (-.53,.53):
        for j in range(5):
            box('Separate timber plank',(-.55+j*.275,y,.60),(.255,.09,1.08),'wood')
        for z in (.15,1.04):
            box('Crate iron strap',(0,y*1.07,z),(1.43,.08,.10),'iron')
            for x in (-.59,-.30,0,.30,.59): rivet((x,y*1.16,z),'brass_dark',.04,(math.pi/2,0,0))
    for x in (-.70,.70):
        box('Side wood panel',(x,0,.60),(.08,1.03,1.08),'wood')
        for y in (-.5,.5): box('Iron corner bracket',(x,y,.60),(.12,.14,1.20),'iron')
    for j in range(4): box('Top planks',(0,-.38+j*.25,1.14),(1.34,.23,.12),'wood')
    box('Navy front stencil plate',(-.1,.607,.67),(.65,.045,.42),'navy_paint')
    gear('Raised gear stencil',.125,.18,.075,.045,8,'dial_mark',loc=(-.1,.65,.67),rot=(math.pi/2,0,0))
    # Half-sunk gear at the right-hand side, tilted into the crate.
    spare=gear('Spare brass gear',.28,.36,.15,.10,12,'brass',loc=(.64,-.03,.27),rot=(0,1.2,.15))
    cut_at_floor(spare)
    cylinder('Spare gear hub',(.67,-.03,.27),.09,.16,'gold',n=12,rot=(0,math.pi/2,0))


def broken_pillar():
    box('Wide stone foot',(0,0,.11),(1.30,1.23,.22),'stone_dark')
    for j in range(3):
        box('Lower masonry block',(0,0,.34+j*.29),(1.05,1.02,.26),
            'stone_dark' if j%2==0 else 'stone')
    # Unequal crown heights create a true broken silhouette, not a flat top.
    v=[(-.52,-.5,1.0),(.52,-.5,1.0),(.52,.5,1.0),(-.52,.5,1.0),
       (-.52,-.5,2.17),(.52,-.5,1.91),(.52,.5,2.24),(-.52,.5,2.02),
       (0,-.5,2.04),(0,.5,1.89)]
    f=[(0,3,2,1),(0,1,5,8,4),(1,2,6,5),(2,3,7,9,6),(3,0,4,7),
       (4,8,9,7),(8,5,6,9)]
    mesh('Jagged broken stone column',v,f,'stone',True)
    box('Clock housing dark inset',(0,.51,1.47),(.95,.07,1.01),'stone_dark')
    clock((0,.57,1.47),.47,True)
    tube('Stone fracture',[(.44,.515,2.18),(.31,.515,1.97),(.38,.515,1.84)],.025,'stone_dark',n=4)
    gear('Exposed brass crown gear',.26,.34,.105,.12,12,'brass',loc=(-.12,-.04,2.23),rot=(-.96,.14,.3))
    cylinder('Exposed gear axle',(-.12,-.04,2.23),.08,.28,'iron',n=8,rot=(math.pi/2,0,0))
    ribbon('Bent exposed flat spring',[(.22,-.12,2.01),(.28,-.13,2.32),(.38,-.06,2.48),
            (.32,.06,2.56),(.14,.12,2.51),(.06,.04,2.41)],.09,.045,'steel')
    for x,y,s in ((-.55,.48,.21),(.51,.43,.23),(-.47,-.46,.20),(.51,-.48,.17)):
        box('Foot rubble',(x,y,.08),(s,s*.9,.16),'stone_dark' if x>0 else 'stone',rot=(.10,.12,random.uniform(-.4,.4)))


def spring_barrel():
    box('Dark cradle lower',(0,0,.07),(1.05,.85,.14),'iron_dark')
    for x in (-.40,.40): box('Cradle riser',(x,0,.19),(.16,.85,.24),'iron_dark')
    cylinder('Steel spring drum',(0,0,.76),.53,.91,'steel',n=32,bevel=True)
    for z in (.33,1.18):
        gear('Fine toothed brass rim',.54,.58,.43,.11,24,'brass',loc=(0,0,z))
    cylinder('Barrel top',(0,0,1.225),.51,.07,'brass_dark',n=32)
    ring('Top brass crown',.48,.09,1.26,.04,'brass',n=32)
    ring('Navy drum belt',.542,.025,.56,.35,'navy_paint',n=32)
    for j in range(12):
        a=TAU*j/12
        cylinder('Gold belt rivets',(.55*math.cos(a),.55*math.sin(a),.73),.045,.035,
                 'gold',n=8,rot=(math.pi/2,0,a-math.pi/2))
    cylinder('Top hex arbor',(0,0,1.25),.12,.10,'iron',n=6)
    box('Dark spring outlet slot',(0,.548,.45),(.30,.04,.13),'iron_dark',bevel=False)
    ribbon('Loose spring end',[(0,.55,.46),(.06,.64,.46),(.21,.64,.47),(.27,.53,.49),
                             (.21,.44,.53),(.10,.44,.55)],.09,.04,'steel')
    ring('Top silver winding groove',.32,.045,1.264,.025,'steel',n=24)


def scroll(x,y,z,side=1):
    tube('Gold corner scroll',[(x,y,z),(x+side*.13,y,z+.05),(x+side*.17,y,z+.16),
         (x+side*.10,y,z+.23),(x+side*.03,y,z+.20),(x+side*.04,y,z+.13)],.025,'gold',n=5)


def music_box():
    for x in (-.98,.98):
        for y in (-.66,.66):
            cylinder('Gold ornate foot',(x,y,.105),.14,.21,'gold',n=8,bevel=True)
    box('Music box timber body',(0,0,.55),(2.24,1.68,.72),'wood')
    for z,s in ((.23,(2.38,1.80,.12)),(.88,(2.38,1.80,.10))):
        box('Stepped dark molding',(0,0,z),s,'wood_dark')
    box('Top gold fillet',(0,0,.95),(2.38,1.80,.045),'gold')
    box('Velvet turntable bed',(0,.15,.981),(1.96,1.25,.04),'velvet')
    for y in (-.855,.855):
        for x in (-.67,0,.67):
            box('Dark carved panel frame',(x,y,.56),(.61,.06,.43),'wood_dark')
            box('Deep red inset panel',(x,y*1.045,.56),(.47,.045,.30),'velvet')
        for x in (-1.06,1.06):
            scroll(x,y*1.07,.33,-1 if x>0 else 1)
    for x in (-1.13,1.13):
        box('End panel',(x,0,.56),(.05,1.3,.42),'wood_dark')
        box('End velvet inlay',(x*1.025,0,.56),(.045,1.08,.27),'velvet')
    # A shallow rear display lid leaves the central rotating doll dais clear.
    # Lid angle is measured from its closed, forward horizontal position.
    start=len(PARTS)
    box('Open lid carved shell',(0,.27,0),(2.13,.54,.10),'wood_dark')
    box('Lid gold mirror border',(0,.27,-.064),(1.98,.44,.045),'gold')
    box('Recessed glass mirror',(0,.27,-.093),(1.79,.31,.035),'glass')
    for x in (-.99,.99):
        box('Lid corner gold binding',(x,.27,-.10),(.07,.51,.05),'gold')
    for obj in PARTS[start:]: place(obj,(0,-.79,.87),(math.radians(110),0,0))
    for x in (-.76,.76): cylinder('Lid hinge',(x,-.78,.90),.07,.30,'brass',n=12,rot=(0,math.pi/2,0))
    start=len(PARTS)
    cylinder('Round brass doll turntable',(0,.15,1.045),.55,.09,'brass',n=40,bevel=True)
    ring('Turntable gold perimeter',.54,.055,1.085,.015,'gold',n=40,loc=(0,.15,0))
    cylinder('Turntable velvet centre',(0,.15,1.094),.44,.012,'velvet',n=32)
    for j in range(8):
        a=TAU*j/8
        rivet((.49*math.cos(a),.15+.49*math.sin(a),1.10),'gold',.025)
    for obj in PARTS[start:]:
        group=obj.vertex_groups.new(name='Merchant turntable')
        group.add(list(range(len(obj.data.vertices))),1.0,'REPLACE')
    key((1.21,0,.62),.70,(0,math.pi/2,0),'gold')
    for x in (-.99,.99):
        cylinder('Lantern foot',(x,.71,1.01),.12,.08,'gold',n=8)
        cylinder('Warm lantern light',(x,.71,1.14),.09,.20,'lamp_warm',n=8)
        cylinder('Lantern roof',(x,.71,1.27),.13,.06,'gold',n=8)
        for dx in (-.07,.07): box('Lantern upright',(x+dx,.71,1.14),(.04,.12,.22),'brass_dark',bevel=False)
    # Broad folded bow loops and two tails, all visible on +Y front.
    for s in (-1,1):
        ring('Red bow loop',.15,.065,0,.07,'ribbon_red',n=12,
             loc=(s*.14,.948,.58),rot=(math.pi/2,0,s*.2))
        box('Red ribbon tail',(s*.11,.94,.36),(.12,.065,.29),'ribbon_red',rot=(0,s*.22,0))
    cylinder('Bow knot',(0,1.0,.58),.09,.09,'red_lacquer',n=10,rot=(math.pi/2,0,0))


def cog_bits():
    for i,(x,y,r,z) in enumerate(((-.36,-.35,.20,.025),(.31,-.30,.22,.025),
            (-.15,.10,.24,.025),(.31,.31,.19,.025),(-.36,.37,.17,.025),(.02,.09,.15,.085))):
        role=('brass','steel','iron_dark','brass_dark','steel','brass')[i]
        if i in (1,4): ring('Scattered open washer',r,.065,z-.025,.05,role,n=16,loc=(x,y,0))
        else: gear('Scattered cog',r*.77,r,r*.36,.05,8,role,loc=(x,y,z))
    ring('Stacked silver washer',.12,.045,.105,.015,'silver',n=16,loc=(.02,.09,0))


def screw_bits():
    for i,(x,y,a) in enumerate(((-.33,-.30,.3),(.05,-.29,-.6),(.31,-.20,1.1),
            (-.28,.08,-.3),(.13,.08,.8),(-.2,.35,1.7),(.31,.34,-.7))):
        start=len(PARTS)
        cylinder('Lying bolt shaft',(0,0,.045),.025,.26,'steel',n=8,rot=(0,math.pi/2,0))
        cylinder('Bolt head',(-.14,0,.045),.05,.055,'brass_dark' if i%2 else 'steel_dark',n=6,rot=(0,math.pi/2,0))
        for t in (-.07,.01,.09):
            ring('Broad bolt thread',.038,.015,t,.04,'steel' if i%2 else 'silver',n=8,
                 loc=(0,0,.045),rot=(0,math.pi/2,0))
        for obj in PARTS[start:]: place(obj,(x,y,0),(0,0,a))
    tube('Bent nail',[(.24,.01,.035),(.37,.0,.035),(.43,.09,.04)],.022,'brass_dark',n=6)
    cylinder('Nail head',(.24,.01,.035),.045,.04,'steel',n=8,rot=(0,math.pi/2,0))


def floor_grate():
    panel=box('Night sky glimpsed below grate',(0,0,.003),(1.9,1.9,.006),'night_sky',bevel=False)
    panel.data.materials.append(MATS['steel_dark'])
    for polygon in panel.data.polygons:
        if polygon.normal.z < .5: polygon.material_index=1
    for s in (-1,1):
        box('Grate iron frame X',(0,s*.94,.015),(2,.12,.03),'iron_dark',bevel=False)
        box('Grate iron frame Y',(s*.94,0,.015),(.12,1.76,.03),'iron_dark',bevel=False)
    for j in range(7):
        t=-.75+j*.25
        box('Steel grate bar',(t,0,.019),(.055,1.78,.022),'steel',bevel=False)
    for y in (-.66,0,.66): box('Iron transverse bar',(0,y,.017),(1.8,.055,.026),'iron',bevel=False)
    for x,y in ((-.625,-.38),(.375,.34),(-.125,.73),(.625,-.72)):
        cylinder('Tiny star seen through opening',(x,y,.009),.025,.006,'starlight',n=6)
    for x in (-.94,.94):
        for y in (-.94,.94): cylinder('Flush corner bolt',(x,y,.028),.04,.004,'brass_dark',n=8)


SPECS=[
    ('GearFloor',(8,8,.56),gear_floor,5000),
    ('WindSpring',(1,1,1.5),wind_spring,5000),
    # Axis X => axial thickness is X; radial rolling extent is Y/Z.
    ('GearWheel',(.5,1.6,1.6),gear_wheel,5000),
    ('OldBell',(1.6,1.6,2.4),old_bell,5000),
    ('GearCrate',(1.6,1.2,1.2),gear_crate,5000),
    ('BrokenClockPillar',(1.4,1.4,2.6),broken_pillar,5000),
    ('SpringBarrel',(1.2,1.2,1.3),spring_barrel,5000),
    ('MusicBox',(2.6,2.0,1.4),music_box,8000),
    ('CogBits',(1.2,1.2,.12),cog_bits,5000),
    ('ScrewBits',(1,1,.1),screw_bits,5000),
    ('FloorGrate',(2,2,.03),floor_grate,5000),
]


def triangles(obj):
    obj.data.calc_loop_triangles()
    return len(obj.data.loop_triangles)


def bounds(obj):
    return [Vector(tuple(fn(v.co[i] for v in obj.data.vertices) for i in range(3))) for fn in (min,max)]


def finish(name,dims,budget):
    bpy.ops.object.select_all(action='DESELECT')
    for obj in PARTS:
        obj.select_set(True)
        bpy.context.view_layer.objects.active=obj
        for mod in list(obj.modifiers): bpy.ops.object.modifier_apply(modifier=mod.name)
        obj.select_set(False)
    for obj in PARTS: obj.select_set(True)
    bpy.context.view_layer.objects.active=PARTS[0]
    bpy.ops.object.join()
    obj=bpy.context.object
    obj.name=name; obj.data.name=name+'_Mesh'
    if triangles(obj)>budget:
        mod=obj.modifiers.new('Keep material silhouettes within budget','DECIMATE')
        mod.ratio=(budget-60)/triangles(obj)
        mod.delimit={'MATERIAL'}
        bpy.ops.object.modifier_apply(modifier=mod.name)
    assert triangles(obj)<=budget,(name,triangles(obj))
    low,high=bounds(obj); span=high-low; centre=(low+high)/2
    if name!='GearFloor':
        for v in obj.data.vertices:
            v.co.x=(v.co.x-centre.x)*dims[0]/span.x
            v.co.y=(v.co.y-centre.y)*dims[1]/span.y
            v.co.z=(v.co.z-low.z)*dims[2]/span.z
        # Keep the merchant standing surface exactly 1.1 m; no Z scaling here.
        if name=='MusicBox':
            # Body and lantern bounds already give 1.4 m within rounding; rebuild
            # the vertical mapping piecewise to protect the gameplay anchor.
            old_anchor=(1.1-low.z)*dims[2]/span.z
            for v in obj.data.vertices:
                z=v.co.z
                v.co.z=z*1.1/old_anchor if z<=old_anchor else 1.1+(z-old_anchor)*.3/(1.4-old_anchor)
            # Footprint fitting must not turn the 1.1 m round dais into an oval.
            group_index=obj.vertex_groups['Merchant turntable'].index
            anchor_x=-centre.x*dims[0]/span.x
            anchor_y=(.15-centre.y)*dims[1]/span.y
            for v in obj.data.vertices:
                if any(g.group==group_index for g in v.groups):
                    v.co.x=anchor_x+(v.co.x-anchor_x)*span.x/dims[0]
                    v.co.y=anchor_y+(v.co.y-anchor_y)*span.y/dims[1]
    bm=bmesh.new(); bm.from_mesh(obj.data)
    bmesh.ops.recalc_face_normals(bm,faces=list(bm.faces))
    assert not any(e.is_boundary or not e.is_manifold for e in bm.edges),name
    bm.to_mesh(obj.data); bm.free()
    for p in obj.data.polygons: p.use_smooth=False
    obj.data.update(); bpy.context.view_layer.update()
    used={obj.data.materials[p.material_index].name for p in obj.data.polygons}
    assert len(used)>=4,(name,used)
    areas={}
    for p in obj.data.polygons:
        role=obj.data.materials[p.material_index].name
        areas[role]=areas.get(role,0)+p.area
    assert max(areas.values())/sum(areas.values())<=.56,(name,areas)
    assert all(math.isfinite(c) for v in obj.data.vertices for c in v.co)
    for mat in obj.data.materials:
        assert mat.name in PALETTE
        assert tuple(mat.diffuse_color)==(.5,.5,.5,1)
        assert tuple(mat.node_tree.nodes['Principled BSDF'].inputs['Base Color'].default_value)==(.5,.5,.5,1)
    low,high=bounds(obj)
    if name=='GearFloor':
        assert abs(low.z+.12)<1e-5
        for v in obj.data.vertices:
            if math.hypot(v.co.x,v.co.y)>.82: assert v.co.z<=.03001
    else:
        assert abs(low.z)<1e-5 and abs(high.z-dims[2])<1e-5
        assert all(abs(obj.dimensions[i]-dims[i])<1e-4 for i in (0,1))
    obj['chapter']='Inside the Clockwork'; obj['front']='+Y'
    obj['triangle_count']=triangles(obj)
    obj['collision']=name not in {'CogBits','ScrewBits','FloorGrate'}
    if name=='GearFloor': obj['interactive']='Shoot red axle to reverse Z rotation'; obj['floor_top_m']=0.0
    if name=='WindSpring': obj['interactive']='Shootable explosive mainspring'
    if name=='GearWheel': obj['roll_axis']='X'; obj['roll_direction']='Y'
    if name=='MusicBox': obj['merchant_standing_surface_m']=1.1; obj['lid_open_degrees']=110
    dest=OUTPUT/f'Kit_{name}.fbx'
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
    scene.render.image_settings.file_format='PNG'; scene.render.filepath=str(PREVIEW)
    scene.view_settings.view_transform='Standard'
    shade=scene.display.shading
    shade.light='FLAT'; shade.color_type='MATERIAL'
    shade.show_shadows=False; shade.show_cavity=True; shade.cavity_type='BOTH'
    shade.curvature_ridge_factor=1.5; shade.curvature_valley_factor=1.2
    shade.show_specular_highlight=False
    shade.background_type='WORLD'
    if scene.world is None: scene.world=bpy.data.worlds.new('Review background')
    scene.world.color=(.035,.045,.07)
    helpers=[]
    # Review-only scale doubles the small pieces; GearFloor stays at true scale.
    layout=[(-6,0,1),(-.4,4.7,1.5),(3.2,4.7,1.5),(6.8,4.7,1.5),
            (-.4,0,1.5),(3.2,0,1.5),(6.8,0,1.5),(-.4,-4.7,1.5),
            (3.2,-4.7,1.5),(6.8,-4.7,1.5),(-6,-6.4,1.25)]
    try:
        for role,mat in MATS.items(): mat.diffuse_color=(*PALETTE[role],1)
        azimuth=math.radians(35)
        near=Vector((math.sin(azimuth),math.cos(azimuth),0))
        right=Vector((-math.cos(azimuth),math.sin(azimuth),0))
        for obj,(x,y,s) in zip(objects,layout):
            obj.location=right*x-near*y
            obj.scale=(s,s,s)
        camera=bpy.data.objects.new('Review camera',bpy.data.cameras.new('Review camera'))
        scene.collection.objects.link(camera); helpers.append(camera); scene.camera=camera
        target=Vector((0,0,.3)); el=math.radians(55)
        camera.location=target+near*(32*math.cos(el))+Vector((0,0,32*math.sin(el)))
        camera.rotation_euler=(target-camera.location).to_track_quat('-Z','Y').to_euler()
        camera.data.type='ORTHO'
        bpy.context.view_layer.update()
        inverse=camera.rotation_euler.to_matrix().transposed()

        def projected_vertices(obj):
            return [inverse @ (obj.matrix_world @ v.co) for v in obj.data.vertices]

        # Space rows by their real projected silhouettes, including tall crowns.
        row_top=5.0
        for indices in ((1,2,3),(4,5,6),(7,8,9)):
            highest=max(p.y for i in indices for p in projected_vertices(objects[i]))
            shift=(row_top-highest)/math.sin(el)
            for i in indices: objects[i].location-=near*shift
            bpy.context.view_layer.update()
            row_top=min(p.y for i in indices for p in projected_vertices(objects[i]))-.95
        grate_top=min(p.y for p in projected_vertices(objects[0]))-.95
        shift=(grate_top-max(p.y for p in projected_vertices(objects[10])))/math.sin(el)
        objects[10].location-=near*shift
        bpy.context.view_layer.update()
        review_points=[]
        for obj in objects:
            review_points.extend(obj.matrix_world @ v.co for v in obj.data.vertices)
        for obj,(x,y,s),(_,dims,_,_) in zip(objects,layout,SPECS):
            data=bpy.data.curves.new('Preview caption','FONT')
            suffix='1:1' if obj.name=='GearFloor' else f'{s:g}x review'
            data.body=obj.name+'\n'+suffix; data.size=.20; data.align_x='CENTER'
            data.materials.append(MATS['silver'])
            label=bpy.data.objects.new('Preview label',data); scene.collection.objects.link(label)
            corners=projected_vertices(obj)
            label_x=(min(v.x for v in corners)+max(v.x for v in corners))/2
            label_y=min(v.y for v in corners)-.24
            label.location=camera.rotation_euler.to_matrix() @ Vector((label_x,label_y,0))
            label.rotation_euler=camera.rotation_euler; helpers.append(label)
            review_points.extend([label.location-right*1.5-Vector((0,0,.5)),
                                  label.location+right*1.5+Vector((0,0,.3))])
        # Fit every object and caption in camera space; no cropped review tiles.
        inverse=camera.rotation_euler.to_matrix().transposed()
        projected=[inverse @ p for p in review_points]
        lo=Vector(tuple(min(p[i] for p in projected) for i in range(3)))
        hi=Vector(tuple(max(p[i] for p in projected) for i in range(3)))
        middle=(lo+hi)/2
        shift=camera.rotation_euler.to_matrix() @ Vector((middle.x,middle.y,0))
        camera.location+=shift
        camera.data.ortho_scale=max(hi.x-lo.x,(hi.y-lo.y)*1.8)*1.07
        PREVIEW.parent.mkdir(parents=True,exist_ok=True)
        bpy.ops.render.render(write_still=True)
        assert PREVIEW.is_file() and PREVIEW.stat().st_size>1024
        print(f'PREVIEW {PREVIEW}',flush=True)
    finally:
        scene.camera=None
        for obj in helpers: bpy.data.objects.remove(obj,do_unlink=True)
        for obj in objects: obj.location=(0,0,0); obj.scale=(1,1,1)
        for mat in MATS.values(): mat.diffuse_color=(.5,.5,.5,1)


def main():
    global PARTS
    bpy.ops.wm.read_factory_settings(use_empty=True)
    for mat in list(bpy.data.materials): bpy.data.materials.remove(mat)
    scene=bpy.context.scene
    scene.unit_settings.system='METRIC'; scene.unit_settings.scale_length=1
    materials(); OUTPUT.mkdir(parents=True,exist_ok=True)
    objects=[]
    for i,(name,dims,build,budget) in enumerate(SPECS):
        PARTS=[]; random.seed(44017+i*7919)
        build(); objects.append(finish(name,dims,budget))
    assert len(scene.objects)==11 and all(o.type=='MESH' for o in scene.objects)
    preview(objects)
    assert len(scene.objects)==11 and all(o.type=='MESH' for o in scene.objects)
    print('DONE',flush=True)


if __name__=='__main__':
    main()
