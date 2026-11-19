# 시계탑 전투방의 대형 엄폐물 5종과 검토용 미리보기를 만듭니다.
# 실행: "C:/Program Files/Blender Foundation/Blender 5.3/blender.exe" -b --factory-startup -P Art/tools/blender_kit_ch4_landmarks.py
"""Standalone, deterministic Blender 5.x landmark kit. Metres; +Y front.

Only the five FBXs and the review PNG are written. Export shaders stay grey;
the review palette is applied only AFTER export and restored after rendering.
The cabinet needs ten roles to honour its explicit component material list.
"""
import math
import random
from pathlib import Path

import bmesh
import bpy
from mathutils import Euler, Matrix, Vector

ROOT = Path(__file__).resolve().parents[2]
OUTPUT = ROOT / 'Game/Assets/Art/Characters/Ch4'
PREVIEW = ROOT / 'Art/tools/specs/blender_kit_ch4_landmarks_preview.png'
TAU = math.tau
FRONT = (math.pi / 2, 0, 0)
MATS, PARTS = {}, []
# Desaturated review swatches: silver/black contrast, with red accents only.
PALETTE = {
    'steel': (.42, .49, .56), 'iron': (.23, .26, .30),
    'iron_dark': (.012, .018, .026), 'silver': (.80, .86, .91),
    'brass': (.58, .60, .62), 'brass_dark': (.27, .29, .32),
    'gold': (.82, .81, .75), 'copper': (.30, .28, .27),
    'red_lacquer': (.72, .025, .045), 'wood': (.22, .21, .20),
    'wood_dark': (.035, .03, .028), 'stone': (.49, .51, .54),
    'stone_dark': (.20, .22, .25), 'dial_face': (.91, .92, .90),
    'dial_mark': (.025, .03, .038), 'glass': (.24, .34, .39),
    'starlight': (.91, .96, 1.0), 'ribbon_red': (.82, .035, .06),
    'velvet': (.135, .12, .15),
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
        mod = obj.modifiers.new('Worn 3cm chamfer', 'BEVEL')
        mod.width, mod.segments = .03, 2
        mod.limit_method, mod.angle_limit = 'ANGLE', math.radians(70)
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


def cylinder(label, loc, radius, depth, role, n=24, rot=(0,0,0), bevel=False):
    v = [(radius*math.cos(TAU*j/n), radius*math.sin(TAU*j/n), z)
         for z in (-depth/2, depth/2) for j in range(n)]
    f = [tuple(reversed(range(n))), tuple(range(n,2*n))]
    f += [(j,(j+1)%n,(j+1)%n+n,j+n) for j in range(n)]
    return place(mesh(label,v,f,role,bevel),loc,rot)


def lathe(label, profile, role, n=32, loc=(0,0,0), rot=(0,0,0), bevel=False):
    v = [(r*math.cos(TAU*j/n),r*math.sin(TAU*j/n),z)
         for r,z in profile for j in range(n)]
    f = [(k*n+j,k*n+(j+1)%n,((k+1)%len(profile))*n+(j+1)%n,
          ((k+1)%len(profile))*n+j) for k in range(len(profile)) for j in range(n)]
    return place(mesh(label,v,f,role,bevel),loc,rot)


def ring(label, radius, width, z, height, role, n=32, loc=(0,0,0), rot=(0,0,0)):
    return lathe(label,[(radius-width,z),(radius,z),(radius,z+height),
                        (radius-width,z+height)],role,n,loc,rot)


def gear(label, root, tip, hole, depth, teeth, role, loc=(0,0,0), rot=(0,0,0)):
    """Closed annulus with broad square teeth; spokes are separate closed solids."""
    contour=[]
    half=tip*math.sin(math.pi/teeth)*.44
    for j in range(teeth):
        a=TAU*j/teeth
        for r,t in ((root,-half),(tip,-half),(tip,half),(root,half)):
            contour.append((r*math.cos(a)-t*math.sin(a),r*math.sin(a)+t*math.cos(a)))
    n=len(contour)
    inner=[(hole*math.cos(math.atan2(y,x)),hole*math.sin(math.atan2(y,x))) for x,y in contour]
    v=[(x,y,z) for z,curve in ((-depth/2,contour),(depth/2,contour),
                              (depth/2,inner),(-depth/2,inner)) for x,y in curve]
    f=[(k*n+j,k*n+(j+1)%n,((k+1)%4)*n+(j+1)%n,((k+1)%4)*n+j)
       for k in range(4) for j in range(n)]
    return place(mesh(label,v,f,role),loc,rot)


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


def ribbon(label, points, height, thickness, role):
    v=[]
    for i,p in enumerate(points):
        t=(Vector(points[min(i+1,len(points)-1)])-Vector(points[max(0,i-1)])).normalized()
        side=Vector((-t.y,t.x,0)).normalized()
        for s,z in ((-1,-1),(1,-1),(1,1),(-1,1)):
            v.append(tuple(Vector(p)+side*s*thickness/2+Vector((0,0,z*height/2))))
    f=[(3,2,1,0),tuple(range(len(v)-4,len(v)))]
    f += [(4*i+j,4*i+(j+1)%4,4*(i+1)+(j+1)%4,4*(i+1)+j)
          for i in range(len(points)-1) for j in range(4)]
    return mesh(label,v,f,role)


def beam(label, a, b, width, depth, role, bevel=True):
    delta=Vector(b)-Vector(a)
    obj=box(label,(0,0,0),(width,depth,delta.length),role,bevel=bevel)
    obj.data.transform(Matrix.Translation((Vector(a)+Vector(b))/2) @ delta.to_track_quat('Z','Y').to_matrix().to_4x4())
    return obj


def extrude_front(label, outline, y_back, y_front, role, bevel=False):
    """Extrude an X/Z polygon into a closed slab."""
    n=len(outline)
    v=[(x,y,z) for y in (y_back,y_front) for x,z in outline]
    f=[tuple(reversed(range(n))),tuple(range(n,2*n))]
    f += [(j,(j+1)%n,(j+1)%n+n,j+n) for j in range(n)]
    return mesh(label,v,f,role,bevel)


def rivet(loc, role='silver', radius=.055, rot=(0,0,0)):
    return cylinder('Broad octagonal rivet',loc,radius,.05,role,n=8,rot=rot)


def spoked_gear(radius, z, role, teeth=24, loc=(0,0,0), rot=(0,0,0), spoke_role='iron'):
    start=len(PARTS)
    gear('Cast square gear teeth',radius*.89,radius,radius*.67,.25,teeth,role,loc=(0,0,z))
    ring('Raised gear cheek',radius*.88,.10,z+.125,.045,'silver' if role=='steel' else 'brass_dark',n=40)
    for j in range(6):
        a=TAU*j/6
        box('Six thick cast spokes',(.47*radius*math.cos(a),.47*radius*math.sin(a),z),
            (.93*radius,.17,.22),spoke_role,rot=(0,0,a),bevel=False)
        rivet((.77*radius*math.cos(a),.77*radius*math.sin(a),z+.19),radius=.045)
    cylinder('Stepped gear hub',(0,0,z),.26*radius,.37,role,n=20,bevel=True)
    cylinder('Silver axle cap',(0,0,z+.23),.17*radius,.10,'silver',n=12,bevel=True)
    cylinder('Hexagonal axle recess',(0,0,z+.29),.075*radius,.035,spoke_role,n=6)
    for obj in PARTS[start:]: place(obj,loc,rot)


def roman(text, loc, height=.30, stroke=.05, angle=0):
    """Actual I/V/X numerals made of raised solid bars, never font objects."""
    start=len(PARTS)
    pitch=height*.42
    for k,symbol in enumerate(text):
        x=(k-(len(text)-1)/2)*pitch
        if symbol=='I':
            box('Raised Roman I',(x,0,0),(stroke,.055,height),'dial_mark',bevel=False)
        else:
            for s in (-1,1):
                tilt=s*(.37 if symbol=='V' else .43)
                offset=s*height*.095 if symbol=='V' else 0
                box('Raised Roman '+symbol,(x+offset,0,0),(stroke,.055,height),
                    'dial_mark',rot=(0,tilt,0),bevel=False)
    for obj in PARTS[start:]: place(obj,loc,(0,angle,0))


def clockwork():
    # The full 5 x 4 x .8 foundation is a real solid, not a collision proxy.
    box('Unbroken movement foundation',(0,0,.4),(5,4,.8),'iron_dark')
    box('Lower machined steel course',(0,0,.13),(4.94,3.94,.18),'steel')
    box('Movement top plate',(0,0,.78),(4.84,3.84,.10),'brass_dark')
    for y in (-1.91,1.91):
        for x in (-1.65,0,1.65):
            box('Inset foundation armor panel',(x,y,.46),(1.44,.10,.40),'steel')
            box('Dark recessed panel centre',(x,y*1.025,.46),(1.16,.035,.20),'iron_dark',bevel=False)
            for dx in (-.62,.62): rivet((x+dx,y*1.028,.46),'red_lacquer',.06,FRONT)
    for x in (-2.43,2.43):
        for y in (-1.25,0,1.25):
            box('Side armor panels',(x,y,.45),(.08,1.14,.43),'steel')
            rivet((x,y,.83),'silver',.065)
    for x in (-2.15,2.15):
        for y in (-1.62,1.62): rivet((x,y,.88),'red_lacquer',.085)
    # Both wheels share the tilted gear plane, with centres one pitch sum apart.
    tilt=(math.radians(20),0,0)
    centre=Vector((-.70,.05,1.67))
    spoked_gear(1.595,0,'brass',26,centre,tilt,'iron_dark')
    offset=Euler(tilt).to_matrix() @ Vector((2.12,.19,0))
    spoked_gear(.70,0,'steel',12,centre+offset,tilt,'iron_dark')
    for x,y,h in ((-.70,.05,1.5),(1.42,.24,1.52)):
        cylinder('Solid iron bearing pedestal',(x,y,(h+.8)/2),.29,h-.8,'iron',n=16)
    # Visible spring on the opposite/front side of the machinery.
    drum=(1.27,1.04,0)
    cylinder('Mainspring drum body',(drum[0],drum[1],1.18),.68,.69,'brass_dark',n=32,bevel=True)
    for z in (.88,1.47): ring('Drum rolled steel rim',.71,.10,z,.10,'steel',n=32,loc=drum)
    pts=[]
    for j in range(109):
        t=j/108; a=t*TAU*3.4; r=.12+.46*t
        pts.append((drum[0]+r*math.cos(a),drum[1]+r*math.sin(a),1.62))
    ribbon('Open coiled flat mainspring',pts,.19,.05,'steel')
    cylinder('Spring arbor',(drum[0],drum[1],1.71),.10,.22,'silver',n=12)
    # Broad, forked escapement silhouette rises above the tilted wheel.
    cylinder('Escapement bearing',(-.3,-1.12,1.46),.24,1.30,'brass_dark',n=16)
    anchor=[(-1.31,2.34),(-1.15,2.62),(-.49,2.88),(-.37,3.20),
            (-.19,3.20),(-.08,2.88),(.65,2.65),(.87,2.33),
            (.65,2.25),(.48,2.48),(-.28,2.68),(-1.04,2.43),(-1.10,2.25)]
    extrude_front('Silver escapement anchor',anchor,-1.18,-.98,'silver',True)
    cylinder('Anchor dark pivot',(-.28,-.94,2.76),.15,.10,'iron_dark',n=16,rot=FRONT)
    for x,z in ((-1.16,2.34),(.72,2.35),(-.28,2.76)):
        rivet((x,-.87,z),'red_lacquer',.07,FRONT)


def pendulum_case():
    # All ten roles below are explicitly requested for this particular piece.
    box('Wide solid stone plinth',(0,0,.13),(3.6,2.6,.26),'stone_dark')
    box('Stone plinth upper step',(0,0,.365),(3.34,2.35,.27),'stone_dark')
    box('Gold plinth fillet',(0,0,.51),(3.12,2.18,.065),'gold')
    box('Solid timber cabinet',(0,-.02,2.16),(2.68,1.93,3.26),'wood_dark')
    for z,size,role in ((.63,(2.98,2.12,.17),'wood'),(.76,(2.88,2.04,.09),'gold'),
                        (2.76,(2.89,2.10,.13),'wood'),(2.85,(2.95,2.14,.065),'gold'),
                        (3.83,(3.08,2.18,.17),'wood'),(3.94,(3.22,2.27,.08),'gold')):
        box('Stacked cabinet cornice',(0,0,z),size,role)
    # Deeply framed side panels give the visible top and side real carving.
    for x in (-1.36,1.36):
        for z,h in ((1.65,1.63),(3.32,.70)):
            box('Carved side raised panel',(x,-.02,z),(.085,1.62,h),'wood')
            box('Recessed side field',(x*1.035,-.02,z),(.045,1.29,h-.25),'wood_dark')
            for y in (-.53,.49): box('Side fluted panel ribs',(x*1.06,y,z),(.045,.065,h-.34),'gold',bevel=False)
    for x in (-1.19,1.19):
        box('Carved pilaster',(x,1.01,1.73),(.24,.19,1.91),'wood')
        for dx in (-.065,.065): box('Pilaster flutes',(x+dx,1.12,1.73),(.04,.04,1.55),'wood_dark',bevel=False)
        for z in (.88,2.59): box('Pilaster capital',(x,1.06,z),(.35,.25,.13),'gold')
    box('Window velvet reveal',(0,.993,1.74),(1.94,.075,1.75),'velvet')
    # A relief window: the neutral opaque export shader must not hide the bob.
    # Glazing is recessed behind its mechanical relief; velvet borders remain.
    box('Smoked glass window',(0,1.045,1.75),(1.67,.045,1.56),'glass',bevel=False)
    for x in (-.93,.93): box('Window gold side molding',(x,1.12,1.76),(.10,.12,1.90),'gold')
    for z in (.85,2.67): box('Window gold transom',(0,1.12,z),(1.96,.12,.10),'gold')
    cylinder('Steel pendulum rod',(0,1.16,1.95),.055,1.38,'steel',n=10)
    cylinder('Brass pendulum disc',(0,1.19,1.24),.48,.14,'brass',n=32,rot=FRONT,bevel=True)
    ring('Pendulum concentric engraving',.38,.055,-.02,.04,'gold',n=32,loc=(0,1.29,1.24),rot=FRONT)
    cylinder('Pendulum pivot boss',(0,1.31,1.24),.10,.065,'steel',n=12,rot=FRONT)
    for x in (-.76,.76):
        beam('Glass corner glints',(x,1.08,2.36),(x-.12,1.08,2.51),.045,.04,'gold',False)
    # Large readable dial on the cabinet head, all geometry facing +Y.
    cylinder('Clock gold bezel',(0,1.04,3.34),.64,.16,'gold',n=40,rot=FRONT,bevel=True)
    cylinder('Pale relief dial',(0,1.145,3.34),.57,.055,'dial_face',n=40,rot=FRONT)
    for j,txt in enumerate(('XII','I','II','III','IV','V','VI','VII','VIII','IX','X','XI')):
        a=TAU*j/12
        roman(txt,(.447*math.sin(a),1.19,3.34+.447*math.cos(a)),.135,.04,a)
    for a,length in ((-.85,.29),(1.05,.40)):
        box('Steel clock hand',(math.sin(a)*length/2,1.25,3.34+math.cos(a)*length/2),
            (.055,.05,length),'steel',rot=(0,a,0),bevel=False)
    rivet((0,1.30,3.34),'gold',.075,FRONT)
    # A shallow broken FRONT crest, rather than a full-depth gabled roof.
    # Its missing centre exposes the carved horizontal crown from above.
    box('Dark flat crown deck',(0,-.11,4.005),(2.93,1.88,.07),'wood_dark')
    for x in (-.76,.76):
        box('Crown inset carved field',(x,-.16,4.055),(1.19,1.44,.055),'wood')
        box('Crown recessed panel',(x,-.16,4.087),(.96,1.17,.025),'wood_dark',bevel=False)
        for y in (-.70,.37): box('Crown panel gold binding',(x,y,4.11),(1.02,.055,.045),'gold',bevel=False)
    outline=[(-1.43,3.99),(-1.43,4.09),(-.65,4.38),(-.39,4.33),
             (-.42,4.13),(-.23,4.19),(-.16,4.04),(.26,4.07),
             (.35,4.25),(.52,4.20),(.72,4.36),(1.43,4.09),(1.43,3.99)]
    extrude_front('Broken timber pediment',outline,.83,1.02,'wood',True)
    for s in (-1,1):
        beam('Broken pediment gold raking molding',(s*.46,1.055,4.30),(s*1.39,1.055,4.045),.07,.07,'gold')
    beam('Bent finial stem',(-.32,.91,4.10),(-.20,.91,4.38),.11,.12,'gold')
    cylinder('Cracked finial lower facet',(-.20,.91,4.47),.18,.12,'gold',n=8)
    cylinder('Shifted finial crown',(-.16,.93,4.565),.13,.07,'gold',n=8)
    box('Black finial fracture',(-.18,1.092,4.495),(.25,.045,.045),'wood_dark',rot=(0,.15,0),bevel=False)
    for x in (-.93,.93):
        for z in (.85,2.67): rivet((x,1.21,z),'gold',.05,FRONT)


def gear_stack():
    # Octagon fills 83% of the square footprint and remains solid up to .75 m.
    cylinder('Solid octagonal iron floor mount',(0,0,.275),2.2,.55,'iron_dark',n=8,bevel=True)
    cylinder('Octagonal stepped mount shoulder',(0,0,.62),2.07,.19,'iron',n=8,bevel=True)
    cylinder('Continuous dark gear backing',(0,0,.78),1.90,.28,'iron_dark',n=40)
    for j in range(8):
        a=TAU*j/8
        rivet((2.02*math.cos(a),2.02*math.sin(a),.59),'silver',.085)
    cylinder('Shared vertical iron axle',(0,0,1.55),.34,1.70,'iron',n=20,bevel=True)
    for radius,z,role,teeth,phase in ((2.04,.97,'brass',28,0),(1.60,1.57,'steel',22,.13),
                                      (1.16,2.17,'brass_dark',16,.26)):
        # Recessed solid web below each set of spokes closes the blocker body.
        cylinder('Recessed dark solid wheel web',(0,0,z-.08),radius*.72,.15,'iron_dark',n=32)
        spoked_gear(radius,z,role,teeth,rot=(0,0,phase),spoke_role='iron')
    cylinder('Top silver axle crown',(0,0,2.49),.28,.22,'silver',n=16,bevel=True)
    cylinder('Crown recessed hex',(0,0,2.59),.13,.02,'iron_dark',n=6)
    for x,y,r,lean in ((-1.38,1.32,.48,.83),(.08,1.82,.35,1.02),(1.39,.90,.40,.92)):
        start=len(PARTS)
        gear('Loose copper cog',r*.81,r,r*.38,.14,12,'copper')
        for j in range(4):
            a=TAU*j/4
            box('Loose cog spoke',(.48*r*math.cos(a),.48*r*math.sin(a),0),(.85*r,.10,.12),
                'copper',rot=(0,0,a),bevel=False)
        cylinder('Loose silver cog hub',(0,0,.09),r*.20,.08,'silver',n=12)
        for obj in PARTS[start:]: place(obj,(x,y,.61+r*.4),(lean,.20,.3))


def bell_frame():
    box('Full solid footing slab',(0,0,.17),(4.2,3,.34),'stone')
    box('Upper stone footing step',(0,0,.385),(4.00,2.8,.13),'stone')
    # Filled side buttresses join the low bell footprint; no open walk-throughs.
    for x in (-1.60,1.60):
        box('Stone side buttress',(x,0,.73),(.77,2.42,.60),'stone')
        box('Solid timber sill',(x,0,1.13),(.65,2.25,.30),'wood_dark')
        for s in (-1,1):
            a=(x,s*1.05,.65); b=(x,s*.10,3.55)
            beam('Heavy A-frame timber',a,b,.35,.40,'wood_dark')
            for t in (.12,.60,.91):
                p=Vector(a).lerp(Vector(b),t)
                box('Iron timber binding',p,(.42,.47,.14),'iron')
                rivet((p.x,p.y+.26,p.z),'brass',.06,FRONT)
        box('A-frame collar beam',(x,0,2.47),(.42,1.02,.22),'wood_dark')
        box('Front iron foot shoe',(x,1.13,.96),(.75,.12,.51),'iron')
        for dx in (-.23,.23): rivet((x+dx,1.22,.96),'brass',.065,FRONT)
    box('Heavy overhead crossbeam',(0,0,3.65),(4.02,.71,.49),'wood_dark')
    for x in (-1.87,1.87):
        box('Broad brass beam end cap',(x,0,3.65),(.24,.77,.55),'brass')
        rivet((x,.42,3.65),'iron_dark',.10,FRONT)
    for x in (-1.1,1.1):
        beam('Iron diagonal knee brace',(x,0,3.39),(x*1.42,0,2.76),.14,.35,'iron')
    box('Brass top crown rail',(0,0,3.965),(3.92,.55,.07),'brass')
    cylinder('Iron hanging stem',(0,0,3.22),.14,.52,'iron_dark',n=16)
    box('Bell suspension yoke',(0,0,3.33),(1.15,.43,.21),'iron')
    lathe('Hollow cast copper bell',[(1.0,.53),(1.0,.69),(.87,.88),(.66,1.40),
          (.53,2.31),(.45,2.67),(.29,2.86),(.14,2.93),(.11,2.82),
          (.34,2.61),(.42,2.27),(.55,1.39),(.76,.86),(.88,.65),(.88,.53)],
          'copper',n=40)
    ring('Heavy brass mouth lip',1.025,.14,.50,.16,'brass',n=40)
    ring('Bell shoulder ring',.51,.055,2.57,.10,'brass',n=32)
    ring('Dark engraved bell tick band',.762,.045,1.08,.19,'dial_mark',n=40)
    for z in (1.05,1.28): ring('Band edge molding',.78,.045,z,.045,'brass',n=40)
    for j in range(20):
        a=TAU*j/20
        box('Bell band raised tick',(.783*math.cos(a),.783*math.sin(a),1.19),(.065,.065,.14),
            'brass',rot=(0,0,a),bevel=False)
    tube('Visible swung clapper',[(0,0,2.63),(0,.51,1.13),(0,.85,.68)],.075,'iron_dark',8)
    cylinder('Clapper weight',(0,.86,.66),.18,.24,'iron_dark',n=16,bevel=True)
    tube('Red rope tied round clapper',[(.15,.89,.76),(.15,1.04,.76),(-.14,1.06,.70),
         (-.16,.88,.66),(.14,.88,.67),(.15,1.06,.62),(.30,1.23,.58),(.49,1.27,.52)],.047,'ribbon_red',6)
    for s in (-1,1):
        tube('Two short red knot tails',[(.12,1.08,.65),(.12+s*.16,1.16,.53),
             (.12+s*.22,1.22,.47)],.045,'ribbon_red',6)


def dial_fragment():
    # A true 5 m radius outer edge: chord 5.2 m, embedded broken circular slab.
    half=math.asin(2.6/5)
    arc=[(5*math.sin(-half+2*half*j/32),-1.6+5*math.cos(-half+2*half*j/32)) for j in range(33)]
    outline=arc+[(2.33,2.30),(2.46,1.95),(2.13,1.54),(2.27,.97),(1.89,.40),
                 (.94,.25),(.32,.47),(-.46,.23),(-1.57,.38),(-2.09,.81),
                 (-2.35,1.16),(-2.18,1.62),(-2.47,2.03)]
    extrude_front('Exposed six decimetre stone core',outline,-.47,.13,'stone_dark')
    # Inset facing reveals a dark core along both shattered edges.
    facing=[(x*.967,.30+(z-.30)*.975) for x,z in outline]
    extrude_front('Broken pale clock facing',facing,.13,.205,'dial_face')
    extrude_front('Rear broken clock facing',facing,-.51,-.47,'dial_face')
    # Two stacked arcuate moldings, separately modeled around the real circle.
    for radius,width,y0,y1 in ((5,.12,.14,.31),(4.81,.055,.205,.28)):
        top=[(radius*math.sin(-half+2*half*j/32),-1.6+radius*math.cos(-half+2*half*j/32)) for j in range(33)]
        inside=[((radius-width)*math.sin(-half+2*half*j/32),-1.6+(radius-width)*math.cos(-half+2*half*j/32)) for j in reversed(range(33))]
        extrude_front('Curved gold rim molding',top+inside,y0,y1,'gold')
    for a,text in ((-.43,'XI'),(-.15,'XII'),(.15,'I'),(.43,'II')):
        roman(text,(4.29*math.sin(a),.26,-1.6+4.29*math.cos(a)),.55,.085,a)
    for j in range(25):
        a=-half+.035+(2*half-.07)*j/24
        box('Giant dial minute tick',(4.64*math.sin(a),.26,-1.6+4.64*math.cos(a)),
            (.05,.055,.16 if j%5==0 else .09),'dial_mark',rot=(0,a,0),bevel=False)
    # Real filled rubble mound; details are broad chunks rather than gravel.
    n=16
    verts=[(2.6*math.cos(TAU*j/n),1.2*math.sin(TAU*j/n),0) for j in range(n)]
    verts += [(2.35*math.cos(TAU*j/n),1.03*math.sin(TAU*j/n),.32+random.random()*.18) for j in range(n)]
    verts.append((0,0,.65))
    faces=[tuple(reversed(range(n)))]
    faces += [(j,(j+1)%n,(j+1)%n+n,j+n) for j in range(n)]
    faces += [(n+j,n+(j+1)%n,2*n) for j in range(n)]
    mesh('Continuous filled rubble berm',verts,faces,'stone_dark')
    for j in range(16):
        a=TAU*j/16
        x=2.05*math.cos(a); y=.77*math.sin(a)
        size=random.uniform(.30,.60)
        obj=box('Large broken masonry chunk',(x,y,.35+size*.25),(size,size*.80,size*.65),
                'stone',rot=(random.uniform(-.2,.2),random.uniform(-.25,.25),random.uniform(-.7,.7)))
        # Clamp bottoms; the filled berm joins the stones into a solid footprint.
        for v in obj.data.vertices: v.co.z=max(.04,v.co.z)
    for z in (1.10,1.85,2.28):
        tube('Bent exposed rebar',[(2.05,-.04,z),(2.36,-.05,z-.13),(2.48,.02,z-.06)],.055,'iron',6)
    tube('Deep visible dial fracture',[(-.64,.241,3.22),(-.46,.241,2.82),(-.58,.241,2.44),
         (-.27,.241,1.91),(-.40,.241,1.43)],.032,'stone_dark',4)
    tube('Forked fracture',[(-.58,.241,2.44),(-1.02,.241,2.26),(-1.20,.241,1.94)],.027,'stone_dark',4)
    for x,z in ((-.48,2.76),(-.51,2.40),(-.31,1.94),(-1.04,2.21)):
        extrude_front('Star shard in fracture',[(x-.045,z),(x,z+.10),(x+.045,z),(x,z-.10)],.25,.30,'starlight')
    # Broad arrow-shaped minute-hand fragment leaning backwards onto the slab.
    hand=[(-.59,.46),(-.17,.40),(.82,2.39),(.99,2.32),(1.10,2.93),
          (.60,2.60),(.77,2.55),(-.12,.77)]
    obj=extrude_front('Huge broken steel minute hand',hand,.47,.63,'steel',True)
    for v in obj.data.vertices: v.co.y-=.11*(v.co.z-.4)
    beam('Minute hand inset iron spine',(-.30,.66,.74),(.80,.42,2.56),.07,.045,'iron',False)
    cylinder('Broken minute hand pivot',(-.34,.68,.65),.23,.11,'steel',n=20,rot=FRONT,bevel=True)
    cylinder('Hand pivot central hole',(-.34,.75,.65),.09,.04,'iron',n=12,rot=FRONT)


SPECS=[
    ('LM_Clockwork',(5.0,4.0,3.2),clockwork,7),
    ('LM_PendulumCase',(3.6,2.6,4.6),pendulum_case,10),
    ('LM_GearStack',(4.4,4.4,2.6),gear_stack,7),
    ('LM_BellFrame',(4.2,3.0,4.0),bell_frame,8),
    ('LM_DialFragment',(5.2,2.4,3.4),dial_fragment,8),
]


def triangles(obj):
    obj.data.calc_loop_triangles()
    return len(obj.data.loop_triangles)


def bounds(obj):
    return [Vector(tuple(fn(v.co[i] for v in obj.data.vertices) for i in range(3))) for fn in (min,max)]


def finish(name,dims,material_count):
    bpy.ops.object.select_all(action='DESELECT')
    for obj in PARTS:
        obj.select_set(True); bpy.context.view_layer.objects.active=obj
        for mod in list(obj.modifiers): bpy.ops.object.modifier_apply(modifier=mod.name)
        obj.select_set(False)
    for obj in PARTS: obj.select_set(True)
    bpy.context.view_layer.objects.active=PARTS[0]
    bpy.ops.object.join()
    obj=bpy.context.object; obj.name=name; obj.data.name=name+'_Mesh'
    # Silhouette-preserving material-aware reduction only if applied bevels
    # put the assembled mesh over budget. Never decimate a low-budget piece.
    if triangles(obj)>8000:
        mod=obj.modifiers.new('Preserve role boundaries under 8000 triangles','DECIMATE')
        mod.ratio=7880/triangles(obj); mod.delimit={'MATERIAL'}
        bpy.ops.object.modifier_apply(modifier=mod.name)
    assert triangles(obj)<=8000,(name,triangles(obj))
    low,high=bounds(obj); span=high-low; centre=(low+high)/2
    # Fit specified outer bounds; the modeled features already use metres.
    # The movement foundation itself remains the exact requested 5 x 4 x .8.
    if name=='LM_Clockwork':
        assert all(abs(span[i]-dims[i])<1e-4 for i in range(3)),(name,span)
    else:
        for v in obj.data.vertices:
            v.co.x=(v.co.x-centre.x)*dims[0]/span.x
            v.co.y=(v.co.y-centre.y)*dims[1]/span.y
            v.co.z=(v.co.z-low.z)*dims[2]/span.z
    bm=bmesh.new(); bm.from_mesh(obj.data)
    bmesh.ops.recalc_face_normals(bm,faces=list(bm.faces))
    assert not any(e.is_boundary or not e.is_manifold for e in bm.edges),name
    bm.to_mesh(obj.data); bm.free()
    for p in obj.data.polygons: p.use_smooth=False
    obj.data.update(); bpy.context.view_layer.update()
    used={obj.data.materials[p.material_index].name for p in obj.data.polygons}
    assert len(used)==material_count,(name,used)
    assert all(math.isfinite(c) for v in obj.data.vertices for c in v.co)
    low,high=bounds(obj)
    assert abs(low.z)<1e-5 and abs(high.z-dims[2])<1e-4,(name,low,high)
    assert abs(low.x+high.x)<1e-4 and abs(low.y+high.y)<1e-4
    assert all(abs(obj.dimensions[i]-dims[i])<1e-4 for i in range(3))
    assert obj.location.length<1e-8 and tuple(obj.scale)==(1,1,1)
    for mat in obj.data.materials:
        assert mat.name in PALETTE
        assert tuple(mat.diffuse_color)==(.5,.5,.5,1)
        assert tuple(mat.node_tree.nodes['Principled BSDF'].inputs['Base Color'].default_value)==(.5,.5,.5,1)
    obj['chapter']='Inside the Clockwork'; obj['front']='+Y'
    obj['triangle_count']=triangles(obj); obj['solid_combat_blocker']=True
    obj['origin']='Bottom centre'; obj['dimensions_m']=list(dims)
    path=OUTPUT/f'Kit_{name}.fbx'
    bpy.ops.export_scene.fbx(filepath=str(path),use_selection=True,
        axis_forward='-Z',axis_up='Y',apply_scale_options='FBX_SCALE_UNITS',
        bake_space_transform=True,object_types={'MESH'},use_mesh_modifiers=True,
        mesh_smooth_type='FACE',add_leaf_bones=False,bake_anim=False,use_custom_props=True)
    assert path.is_file() and path.stat().st_size>1024
    print(f'EXPORTED {name} {triangles(obj)} {tuple(round(d,4) for d in obj.dimensions)}',flush=True)
    return obj


def preview(objects):
    scene=bpy.context.scene
    scene.render.engine='BLENDER_WORKBENCH'
    scene.render.resolution_x,scene.render.resolution_y=1800,1000
    scene.render.resolution_percentage=100
    scene.render.image_settings.file_format='PNG'; scene.render.filepath=str(PREVIEW)
    scene.render.film_transparent=False
    scene.view_settings.view_transform='Standard'
    shade=scene.display.shading
    shade.light='FLAT'; shade.color_type='MATERIAL'
    shade.show_shadows=False; shade.show_cavity=True; shade.cavity_type='BOTH'
    shade.curvature_ridge_factor=1.45; shade.curvature_valley_factor=1.20
    shade.cavity_ridge_factor=1.3; shade.cavity_valley_factor=1.1
    shade.show_specular_highlight=False; shade.show_object_outline=True
    shade.background_type='WORLD'
    if scene.world is None: scene.world=bpy.data.worlds.new('Review background')
    scene.world.color=(.035,.042,.055)
    helpers=[]
    az=math.radians(18); el=math.radians(55)
    near=Vector((math.sin(az),math.cos(az),0))
    right=Vector((-math.cos(az),math.sin(az),0))
    camera=bpy.data.objects.new('Review camera',bpy.data.cameras.new('Review camera'))
    scene.collection.objects.link(camera); helpers.append(camera); scene.camera=camera
    camera.location=near*35*math.cos(el)+Vector((0,0,35*math.sin(el)))
    camera.rotation_euler=(-camera.location).to_track_quat('-Z','Y').to_euler()
    camera.data.type='ORTHO'; inv=camera.rotation_euler.to_matrix().transposed()

    def points(obj):
        return [inv @ (obj.matrix_world @ v.co) for v in obj.data.vertices]

    def label(body,x,y,size,role='silver',align='CENTER'):
        data=bpy.data.curves.new('Review caption','FONT')
        data.body=body; data.size=size; data.align_x=align
        data.materials.append(MATS[role])
        obj=bpy.data.objects.new('Review caption',data)
        scene.collection.objects.link(obj); helpers.append(obj)
        obj.rotation_euler=camera.rotation_euler
        obj.location=camera.rotation_euler.to_matrix() @ Vector((x,y,5))
        return obj

    try:
        for role,mat in MATS.items(): mat.diffuse_color=(*PALETTE[role],1)
        # Pack the real projected silhouettes with a generous .85 m gap.
        cursor=0
        for obj in objects:
            bpy.context.view_layer.update()
            pp=points(obj); lo=min(p.x for p in pp); hi=max(p.x for p in pp)
            obj.location=right*(cursor-lo)
            cursor+=hi-lo+.85
        bpy.context.view_layer.update()
        cloud=[p for obj in objects for p in points(obj)]
        x0=min(p.x for p in cloud); x1=max(p.x for p in cloud)
        y0=min(p.y for p in cloud); y1=max(p.y for p in cloud)
        baseline=y0-.56
        for obj,(_,dims,_,_) in zip(objects,SPECS):
            pp=points(obj); x=(min(p.x for p in pp)+max(p.x for p in pp))/2
            label(obj.name,x,baseline,.245)
            label(f'{dims[0]:.1f} x {dims[1]:.1f} x {dims[2]:.1f} m',x,baseline-.37,.19,'brass')
            label(f'{triangles(obj):,} tris',x,baseline-.67,.16,'steel')
        mid=(x0+x1)/2
        label('IV  /  INSIDE THE CLOCKWORK',mid,y1+2.02,.48)
        label('COMBAT LANDMARKS   /   SOLID COVER   /   REAL SCALE',mid,y1+1.38,.205,'brass')
        label('1 : 1 METRES     /     +Y FRONT     /     55 DEG ELEVATION',mid,baseline-1.53,.17,'steel')
        bpy.context.view_layer.update()
        cloud += [inv @ (o.matrix_world @ Vector(c)) for o in helpers if o.type=='FONT' for c in o.bound_box]
        x0=min(p.x for p in cloud); x1=max(p.x for p in cloud)
        y0=min(p.y for p in cloud); y1=max(p.y for p in cloud)
        camera.location+=camera.rotation_euler.to_matrix() @ Vector(((x0+x1)/2,(y0+y1)/2,0))
        camera.data.ortho_scale=max(x1-x0,(y1-y0)*1.8)*1.075
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
    for i,(name,dims,build,count) in enumerate(SPECS):
        PARTS=[]; random.seed(44051+i*7919)
        build(); objects.append(finish(name,dims,count))
    assert len(scene.objects)==5 and all(o.type=='MESH' for o in scene.objects)
    preview(objects)
    assert len(scene.objects)==5 and all(o.type=='MESH' for o in scene.objects)
    print('DONE',flush=True)


if __name__=='__main__':
    main()
