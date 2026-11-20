# 5장 붕괴한 시간 감옥의 소품 13종과 검수용 미리보기를 만듭니다.
# 실행: "C:/Program Files/Blender Foundation/Blender 5.3/blender.exe" -b --factory-startup -P Art/tools/blender_kit_ch5_props.py
"""Deterministic, metre-scale Ch5 props. +Y front; grey role shaders in FBX.

Only the review render uses coloured viewport materials. Every exported asset
is one mesh, including its hovering fragments. No external assets are needed.
"""
import math
import random
from pathlib import Path

import bmesh
import bpy
from mathutils import Euler, Matrix, Vector

ROOT = Path(__file__).resolve().parents[2]
OUTPUT = ROOT / 'Game/Assets/Art/Characters/Ch5'
PREVIEW = ROOT / 'Art/tools/specs/blender_kit_ch5_props_preview.png'
TAU = math.tau
MATS, PARTS = {}, []
PALETTE = {
    'void_stone': (.29, .20, .43), 'void_stone_dark': (.075, .045, .15),
    'marble': (.76, .69, .88), 'marble_dark': (.35, .28, .48),
    'violet': (.47, .20, .76), 'violet_dark': (.14, .065, .25),
    'indigo': (.075, .09, .24), 'night_sky': (.012, .018, .055),
    'starlight': (.90, .97, 1.0), 'crack_gold': (1.0, .83, .25),
    'gold': (.94, .59, .12), 'gold_dark': (.40, .23, .065),
    'mirror': (.60, .83, .94), 'mirror_back': (.20, .29, .43),
    'crystal': (.64, .38, .93), 'magenta': (.96, .055, .46),
    'wood': (.37, .23, .42), 'wood_dark': (.105, .06, .16),
    'iron': (.29, .27, .37), 'iron_dark': (.07, .075, .12),
    'steel': (.42, .52, .68), 'brass': (.68, .43, .16),
    'dial_face': (.86, .81, .96), 'dial_mark': (.055, .035, .10),
    'sand': (.60, .48, .65), 'red_lacquer': (.52, .04, .17),
    'cloth': (.50, .35, .65), 'lamp_warm': (1.0, .72, .30),
    'porcelain': (.86, .84, .94), 'velvet': (.24, .075, .30),
}


def materials():
    for role in PALETTE:
        mat = bpy.data.materials.new(role)
        mat.use_nodes = True
        mat.diffuse_color = (.5, .5, .5, 1)
        shader = mat.node_tree.nodes.get('Principled BSDF')
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
        mod = obj.modifiers.new('Worn two-step chamfer', 'BEVEL')
        mod.width, mod.segments = .02, 2
        mod.limit_method, mod.angle_limit = 'ANGLE', math.radians(65)
        mod.use_clamp_overlap = True
    PARTS.append(obj)
    return obj


def place(obj, loc=(0, 0, 0), rot=(0, 0, 0)):
    obj.data.transform(Matrix.Translation(Vector(loc)) @ Euler(rot).to_matrix().to_4x4())
    return obj


def group_place(start, loc=(0, 0, 0), rot=(0, 0, 0)):
    for obj in PARTS[start:]:
        place(obj, loc, rot)


def roles(obj, names, indices):
    for name in names:
        obj.data.materials.append(MATS[name])
    for face, index in zip(obj.data.polygons, indices):
        face.material_index = index
    return obj


def box(label, loc, size, role, rot=(0, 0, 0), bevel=True):
    x, y, z = (v / 2 for v in size)
    verts = [(-x,-y,-z),(x,-y,-z),(x,y,-z),(-x,y,-z),
             (-x,-y,z),(x,-y,z),(x,y,z),(-x,y,z)]
    faces = [(0,3,2,1),(4,5,6,7),(0,1,5,4),(1,2,6,5),(2,3,7,6),(3,0,4,7)]
    return place(mesh(label, verts, faces, role, bevel), loc, rot)


def cylinder(label, loc, radius, depth, role, n=16, rot=(0,0,0), bevel=False):
    verts = [(radius*math.cos(TAU*j/n), radius*math.sin(TAU*j/n), z)
             for z in (-depth/2, depth/2) for j in range(n)]
    faces = [tuple(reversed(range(n))), tuple(range(n,2*n))]
    faces += [(j,(j+1)%n,(j+1)%n+n,j+n) for j in range(n)]
    return place(mesh(label, verts, faces, role, bevel), loc, rot)


def ring(label, radius, width, z, height, role, n=24, loc=(0,0,0), rot=(0,0,0)):
    profile = [(radius-width,z),(radius,z),(radius,z+height),(radius-width,z+height)]
    verts = [(r*math.cos(TAU*j/n),r*math.sin(TAU*j/n),zz) for r,zz in profile for j in range(n)]
    faces = [(k*n+j,k*n+(j+1)%n,((k+1)%4)*n+(j+1)%n,((k+1)%4)*n+j)
             for k in range(4) for j in range(n)]
    return place(mesh(label, verts, faces, role), loc, rot)


def tube(label, points, radius, role, n=4):
    verts = []
    for i,p in enumerate(points):
        t = (Vector(points[min(i+1,len(points)-1)])-Vector(points[max(0,i-1)])).normalized()
        ref = Vector((0,1,0)) if abs(t.y)<.9 else Vector((1,0,0))
        u = ref.cross(t).normalized(); w = t.cross(u)
        verts += [tuple(Vector(p)+radius*(math.cos(TAU*j/n)*u+math.sin(TAU*j/n)*w)) for j in range(n)]
    faces = [tuple(reversed(range(n))),tuple(range((len(points)-1)*n,len(points)*n))]
    faces += [(i*n+j,i*n+(j+1)%n,(i+1)*n+(j+1)%n,(i+1)*n+j)
              for i in range(len(points)-1) for j in range(n)]
    return mesh(label, verts, faces, role)


def ribbon(label, points, width, thickness, role):
    # Broad upright rectangular ribbon, not a wire helix.
    verts = []
    for i,p in enumerate(points):
        t = (Vector(points[min(i+1,len(points)-1)])-Vector(points[max(0,i-1)])).normalized()
        side = Vector((-t.y,t.x,0)).normalized()
        verts += [tuple(Vector(p)+side*s*thickness/2+Vector((0,0,z*width/2)))
                  for s,z in ((-1,-1),(1,-1),(1,1),(-1,1))]
    faces = [(3,2,1,0),tuple(range(len(verts)-4,len(verts)))]
    faces += [(4*i+j,4*i+(j+1)%4,4*(i+1)+(j+1)%4,4*(i+1)+j)
              for i in range(len(points)-1) for j in range(4)]
    return mesh(label, verts, faces, role)


def slab(label, contour, depth, role, loc=(0,0,0), rot=(0,0,0), bevel=False):
    """Closed polygon in X/Z, extruded in Y; both broad faces are planar."""
    n = len(contour)
    verts = [(x,y,z) for y in (-depth/2,depth/2) for x,z in contour]
    faces = [tuple(reversed(range(n))),tuple(range(n,2*n))]
    faces += [(j,(j+1)%n,(j+1)%n+n,j+n) for j in range(n)]
    return place(mesh(label, verts, faces, role, bevel), loc, rot)


def glass(label, contour, depth=.045, loc=(0,0,0), rot=(0,0,0), two_sided=False):
    """One closed faceted shard. Extra gold is a rim edge, never a second face."""
    obj = slab(label,contour,depth,'mirror',loc,rot)
    indices = [0 if two_sided else 1,0]+[2 if j%3 else 3 for j in range(len(contour))]
    return roles(obj,['mirror_back','crack_gold','gold'],indices)


def octa(label, loc, size, role, rot=(0,0,0)):
    x,y,z = size
    verts = [(x/2,0,0),(0,y/2,0),(-x/2,0,0),(0,-y/2,0),(0,0,z/2),(0,0,-z/2)]
    faces = [(j,(j+1)%4,4) for j in range(4)]+[((j+1)%4,j,5) for j in range(4)]
    return place(mesh(label,verts,faces,role),loc,rot)


def rock(label, loc, size, role='void_stone', rot=(0,0,0)):
    x,y,z = size
    verts = []
    for zz,r in ((-.5,.72),(-.12,1),(.35,.78)):
        for j in range(7):
            a=TAU*j/7; rj=r*random.uniform(.87,1.05)
            verts.append((x*.5*rj*math.cos(a),y*.5*rj*math.sin(a),z*zz))
    verts.append((x*.05,-y*.05,z*.5))
    faces=[tuple(reversed(range(7)))]
    faces += [(k*7+j,k*7+(j+1)%7,(k+1)*7+(j+1)%7,(k+1)*7+j) for k in range(2) for j in range(7)]
    faces += [(14+j,14+(j+1)%7,21) for j in range(7)]
    obj=mesh(label,verts,faces,role,True)
    alt='void_stone_dark' if role=='void_stone' else 'violet'
    roles(obj,[alt],[1 if j%4==1 else 0 for j in range(len(faces))])
    return place(obj,loc,rot)


def crystal(loc, radius, height, rot=(0,0,0)):
    n=5
    verts=[(radius*math.cos(TAU*j/n),radius*math.sin(TAU*j/n),z)
           for z in (0,height*.64) for j in range(n)]
    verts.append((radius*.25,0,height))
    faces=[tuple(reversed(range(n)))]+[(j,(j+1)%n,(j+1)%n+n,j+n) for j in range(n)]
    faces += [(n+j,n+(j+1)%n,2*n) for j in range(n)]
    obj=mesh('Split lilac crystal',verts,faces,'crystal')
    roles(obj,['violet','starlight'],[1]+[j%2 for j in range(n)]+[2 if j==1 else 0 for j in range(n)])
    return place(obj,loc,rot)


def star(loc, radius=.14, role='gold', depth=.035):
    contour=[]
    for j in range(10):
        a=TAU*j/10+math.pi/2; r=radius if j%2==0 else radius*.43
        contour.append((r*math.cos(a),r*math.sin(a)))
    return slab('Inlaid five-point star',contour,depth,role,loc)


def rivet(loc, role='brass', radius=.04, front=False):
    return cylinder('Broad hex rivet',loc,radius,.04,role,n=6,rot=(math.pi/2,0,0) if front else (0,0,0))


def key(loc, scale=1, rot=(0,0,0)):
    start=len(PARTS)
    box('Key stem',(0,0,-.12*scale),(.10*scale,.12*scale,.32*scale),'brass')
    for s in (-1,1):
        ring('Open winding key bow',.16*scale,.065*scale,0,.07*scale,'gold',n=12,
             loc=(s*.135*scale,0,.065*scale),rot=(math.pi/2,0,0))
    rivet((0,.035*scale,.05*scale),'crack_gold',.06*scale,True)
    group_place(start,loc,rot)


def gear(loc, radius=.4, rot=(0,0,0)):
    n=12; contour=[]
    for j in range(n):
        for t,r in ((-.46,.78),(-.27,1),(.27,1),(.46,.78)):
            a=TAU*(j+t)/n
            contour.append((radius*r*math.cos(a),radius*r*math.sin(a)))
    start=len(PARTS)
    # Annular toothed wheel with an open centre and five broad spokes.
    count=len(contour)
    inner=[(.51*radius*math.cos(math.atan2(z,x)),.51*radius*math.sin(math.atan2(z,x))) for x,z in contour]
    verts=[(x,y,z) for y,c in ((-.055,contour),(.055,contour),(.055,inner),(-.055,inner)) for x,z in c]
    faces=[(k*count+j,k*count+(j+1)%count,((k+1)%4)*count+(j+1)%count,((k+1)%4)*count+j)
           for k in range(4) for j in range(count)]
    mesh('Memory of a toothed clock wheel',verts,faces,'brass')
    for j in range(5):
        a=TAU*j/5
        box('Five wheel spokes',(.28*radius*math.sin(a),0,.28*radius*math.cos(a)),
            (.09,.075,.58*radius),'gold_dark',rot=(0,a,0),bevel=False)
    cylinder('Wheel axle',(0,.015,0),radius*.20,.15,'gold',n=10,rot=(math.pi/2,0,0))
    group_place(start,loc,rot)


def warped_keg():
    # Stave cross sections rotate progressively: an actual eight-degree twist.
    for j in range(14):
        verts=[]
        for z,r in ((.06,.34),(.28,.43),(.71,.44),(.94,.35)):
            for rr,t in ((r-.075,-.47),(r,-.47),(r,.47),(r-.075,.47)):
                a=TAU*(j+t)/14+math.radians(8)*z/.94
                verts.append((rr*math.cos(a),rr*math.sin(a),z))
        faces=[(3,2,1,0),(12,13,14,15)]+[(4*k+i,4*k+(i+1)%4,4*(k+1)+(i+1)%4,4*(k+1)+i) for k in range(3) for i in range(4)]
        obj=mesh('Fourteen twisted separate staves',verts,faces,'wood',True)
        roles(obj,['wood_dark'],[1,1]+[0 if i%4==1 else 1 for i in range(12)])
    cylinder('Dark lid grooves',(0,0,.935),.35,.05,'wood_dark',n=14)
    for j in range(5):
        x=(j-2)*.125
        length=2*math.sqrt(max(.005,.33**2-x*x))
        box('Lid timber panels',(x,0,.968),(.109,length,.04),'wood',rot=(0,0,.14),bevel=False)
    for z,r in ((.15,.395),(.48,.456),(.83,.403)):
        ring('Black iron binding hoop',r,.055,z,.11,'iron_dark',n=28)
        ring('Violet hoop border',r+.004,.025,z+.085,.025,'violet',n=28)
        for a in (math.pi*.25,math.pi*.5,math.pi*.75):
            rivet((r*math.cos(a),(r+.01)*math.sin(a),z+.055),'gold',.035,True)
    tube('Wide leaking front fracture',[(-.08,.355,.92),(.05,.43,.76),(-.035,.465,.58),(.12,.45,.40),(.045,.37,.12)],.037,'crack_gold')
    tube('Branch across top',[(-.08,.35,.94),(-.17,.16,.998),(-.09,-.06,.998),(-.25,-.24,.998)],.025,'crack_gold')
    star((-.20,.43,.68),.145)
    cylinder('Fuse socket',(.13,-.02,.99),.09,.075,'brass',n=10)
    tube('Magenta fuse wick',[(.13,-.02,1.01),(.15,.015,1.10),(.23,.03,1.14)],.035,'magenta',n=6)
    octa('White fuse spark',(.24,.03,1.16),(.14,.10,.13),'starlight')
    for loc,rot in (((-.35,.04,1.055),(.12,-.23,-.5)),((.35,-.13,1.01),(.2,.15,.45)),((-.08,-.36,1.04),(-.15,.12,-.1))):
        slab('Hovering jagged stave splinter',[(-.05,-.09),(.05,-.08),(.04,.04),(.005,.13),(-.045,.065)],.10,'wood',loc,rot,True)
        octa('Splinter gold fracture',Vector(loc)+Vector((0,.025,-.065)),(.07,.07,.08),'crack_gold')


def warped_trunk():
    start=len(PARTS)
    box('Stopped leather trunk body',(0,0,.36),(1.27,.74,.64),'violet_dark')
    for y in (-.38,.38):
        box('Leather inset',(0,y,.37),(1.10,.045,.41),'violet')
        for z in (.12,.58): box('Front and back timber slats',(0,y*1.06,z),(1.29,.06,.075),'wood')
    for x in (-.64,.64):
        box('End timber slat',(x,0,.37),(.065,.75,.09),'wood')
        box('Raised brass end handle',(x*1.035,0,.43),(.06,.32,.11),'brass')
        box('Handle grip recess',(x*1.075,0,.43),(.025,.19,.05),'violet_dark',bevel=False)
    box('Unsealed gold light slit',(0,0,.691),(1.25,.74,.02),'crack_gold',bevel=False)
    box('Slightly lifted lid',(0,-.012,.80),(1.28,.76,.20),'violet_dark',rot=(math.radians(2),0,0))
    box('Violet lid panel',(0,-.02,.91),(1.03,.59,.045),'violet')
    for x in (-.41,.41):
        box('Lid wood runners',(x,0,.94),(.09,.79,.055),'wood')
        box('Cloth belt across lid',(x/2,0,.961),(.10,.77,.04),'cloth',bevel=False)
        box('Cloth belt front',(x/2,.427,.44),(.10,.04,.68),'cloth',bevel=False)
        box('Brass belt buckle',(x/2,.46,.55),(.15,.045,.13),'brass')
    for x in (-.57,.57):
        for y in (-.33,.33):
            for z in (.10,.84):
                if x>.5 and y>.3 and z>.8: continue
                box('Broad brass corner binding',(x,y,z),(.19,.15,.16),'brass')
                rivet((x,y+(.085 if y>0 else -.085),z),'gold',.035,True)
    box('Big central brass lock',(0,.458,.65),(.19,.075,.22),'brass')
    box('Black lock keyhole',(0,.503,.66),(.05,.025,.08),'dial_mark',bevel=False)
    # Frozen luggage clock, deliberately large enough to read at game scale.
    tube('Tag hanger',[(.44,.45,.82),(.49,.47,.70),(.40,.48,.60)],.025,'cloth')
    cylinder('Luggage clock bezel',(.40,.49,.47),.155,.06,'brass',n=16,rot=(math.pi/2,0,0))
    cylinder('Frozen luggage dial',(.40,.53,.47),.123,.035,'dial_face',n=16,rot=(math.pi/2,0,0))
    for a,l in ((-.65,.09),(1.2,.075)):
        box('Stopped luggage clock hands',(.40+math.sin(a)*l/2,.556,.47+math.cos(a)*l/2),(.04,.025,l),'dial_mark',rot=(0,a,0),bevel=False)
    group_place(start,(0,0,0),(0,0,math.radians(5)))
    box('One detached corner cap',(.66,.37,.94),(.20,.17,.15),'brass',rot=(.15,.1,.2))
    octa('Exposed corner light',(.57,.32,.84),(.12,.09,.08),'crack_gold')
    for loc in ((-.58,-.24,1.01),(.58,-.35,.77)):
        glass('Hovering leather-glass chip',[(-.055,-.06),(.065,-.03),(.025,.08)],loc=loc,rot=(.1,.2,0))


def mirror_shard():
    # Rubble keeps the base shallow; the complete front AND back stay planar.
    for loc,size in (((-.63,0,.14),(1.15,.28,.28)),((.42,0,.13),(1.10,.28,.26)),((.0,0,.24),(.80,.25,.36))):
        rock('Narrow rubble shoe',loc,size)
    outline=[(-.91,.20),(.62,.24),(1.00,.91),(.65,1.20),(.80,2.06),(.27,1.87),(.13,2.38),(-.45,2.16),(-.85,1.32)]
    slab('Dark structural mirror thickness',outline,.12,'mirror_back')
    cx=sum(x for x,z in outline)/len(outline); cz=sum(z for x,z in outline)/len(outline)
    inset=[(cx+(x-cx)*.91,cz+(z-cz)*.94) for x,z in outline]
    for side in (-1,1):
        slab('Flat bullet reflector on both sides',inset,.025,'mirror',loc=(0,side*.075,0))
        for j in range(len(outline)):
            # Broken frame: leave two short, intentional gaps at fracture points.
            a=Vector((outline[j][0],side*.077,outline[j][1]))
            b=Vector((outline[(j+1)%len(outline)][0],side*.077,outline[(j+1)%len(outline)][1]))
            if j in (2,6): b=a+(b-a)*.72
            tube('Thin interrupted gold frame',[a,b],.035,'gold')
    for x,z,s in ((-.90,.12,.30),(-.72,.14,.46),(.76,.11,.36),(.93,.10,.23)):
        crystal((x,0,z),.09,s,(0,-.20 if x<0 else .22,0))
    for loc,c in (((-1.10,0,1.40),[(-.11,-.16),(.11,-.07),(.04,.23)]),((1.11,0,.83),[(-.10,-.15),(.13,.06),(-.05,.20)])):
        glass('Detached two-sided mirror shard',c,.08,loc,(0,.12,.03),True)
    tube('Gold fracture on base',[(-.76,.15,.13),(-.45,.15,.25),(-.12,.15,.17),(.11,.15,.31)],.025,'crack_gold')


def warped_spring():
    box('Square fractured spring plinth',(0,0,.065),(.94,.94,.13),'void_stone_dark')
    box('Bright magenta warning belt',(0,0,.145),(.90,.90,.07),'magenta')
    box('Violet base top',(0,0,.195),(.85,.85,.04),'violet')
    for x in (-.34,.34):
        for y in (-.34,.34): rivet((x,y,.24),'brass',.05)
    tube('Base crack',[(-.42,.18,.223),(-.17,.12,.223),(-.24,-.04,.223),(.02,-.20,.223),(.15,-.42,.223)],.026,'crack_gold')
    cylinder('Vertical brass arbor',(0,0,.75),.065,1.04,'brass',n=12,rot=(0,.10,0))
    pts=[]
    for j in range(181):
        t=j/180; a=TAU*5*t; r=.12+.235*t
        pts.append((r*math.cos(a)+.065*t,r*math.sin(a),.32+.86*t))
    # Five well-separated turns; the loose tang extends the last turn.
    ribbon('Five gold ribbon turns',pts,.12,.04,'gold')
    ribbon('Unwinding free tang',[pts[-1],(.46,.10,1.20),(.46,.30,1.22),(.31,.42,1.24)],.12,.04,'gold')
    box('Retaining warning catch',(.37,.05,.83),(.10,.16,.20),'magenta',rot=(0,.12,0))
    key((.06,0,1.32),.72,(0,.23,-.15))
    for loc in ((-.38,.11,1.0),(.36,-.27,.54)):
        box('Detached gold spring chip',loc,(.10,.11,.12),'gold',rot=(.2,.15,.3))
        octa('Lit spring fracture',loc,(.045,.06,.15),'crack_gold')


def cover_shard_rock():
    rock('Large split cover boulder',(-.12,-.04,.58),(1.55,1.10,1.16))
    rock('Dark rear buttress',(.43,-.20,.43),(.76,.76,.86),'void_stone_dark')
    outline=[(-.56,-.31),(.30,-.30),(.50,.12),(.14,.44),(-.47,.27)]
    slab('Inset mirror dark socket',outline,.08,'mirror_back',loc=(-.10,.49,.58),rot=(.13,.04,.06))
    slab('Big front mirror facet',[(x*.87,z*.85) for x,z in outline],.045,'mirror',loc=(-.10,.54,.58),rot=(.13,.04,.06))
    tube('Rock gold lightning',[(-.47,.53,.99),(-.24,.57,.86),(-.33,.59,.60),(-.06,.59,.42),(-.17,.48,.16)],.035,'crack_gold')
    tube('Top crack branch',[(-.47,.39,1.04),(-.29,.14,1.13),(-.09,.05,1.08),(.04,-.19,1.03)],.028,'crack_gold')
    for loc,r,h,rot in (((.60,.16,.28),.14,.74,(0,.28,0)),((.75,-.08,.28),.12,.51,(.1,.35,0)),((.50,.37,.25),.10,.48,(-.2,.22,0))):
        crystal(loc,r,h,rot)
    for loc,s in (((-.40,-.05,1.25),.19),((.10,-.20,1.27),.16),((-.63,.13,1.13),.14)):
        rock('Hovering cover pebble',loc,(s,s*.85,s*.65))


def cover_memory_pile():
    rock('Void mortar fusing four memories',(0,-.06,.48),(1.65,1.15,.94))
    box('Sandstone memory block',(.44,.20,.35),(.70,.71,.63),'sand',rot=(0,.10,-.14))
    box('Sandstone shadow course',(.45,.21,.18),(.73,.74,.08),'marble_dark',rot=(0,.10,-.14))
    # Broken saloon sign, two chunky boards with jagged ends.
    for z,dx in ((.68,0),(.91,-.08)):
        contour=[(-.79,-.095),(.48,-.095),(.64,-.03),(.51,.03),(.60,.10),(-.69,.10),(-.79,.04)]
        slab('Broken saloon sign plank',contour,.13,'wood',loc=(dx,.45,z),rot=(0,-.10,-.07),bevel=True)
    for x in (-.60,.31):
        box('Old saloon sign black batten',(x,.53,.79),(.07,.05,.46),'wood_dark',rot=(0,-.10,0))
        rivet((x,.57,.95),'gold',.04,True)
    star((-.20,.565,.84),.16,'gold')
    # Steel bench end: large curved arm, two legs and two remnant seat slats.
    for x in (-.64,-.22):
        box('Station bench feet',(x,-.35,.17),(.15,.22,.34),'steel',rot=(0,.1,0))
        box('Station bench seat slats',(x,-.30,.55),(.16,.72,.10),'steel',rot=(.08,0,0))
    tube('Recognisable curved station bench arm',[(-.65,-.56,.39),(-.65,-.55,.86),(-.64,-.39,1.12),(-.63,-.16,1.18),(-.62,.02,1.03)],.07,'steel',n=6)
    gear((.42,.17,1.04),.36,(.12,-.17,-.10))
    tube('Memory pile leaking seam',[(-.68,.24,.30),(-.38,.35,.47),(-.11,.53,.50),(.13,.56,.32),(.44,.57,.43),(.74,.41,.30)],.034,'crack_gold')
    tube('Fused crown fracture',[(-.30,-.25,.94),(-.08,-.05,1.00),(.13,-.11,1.04),(.30,-.06,1.14)],.03,'crack_gold')
    for loc in (((-.17,-.21,1.30)),((.65,-.14,.98))):
        rock('Hovering memory fragment',loc,(.18,.15,.17))


def cover_broken_column():
    box('Column dark square footing',(0,0,.09),(1.24,1.24,.18),'marble_dark')
    box('Column pale stepped footing',(0,0,.205),(1.12,1.12,.09),'marble')
    ring('Footing gold molding',.53,.07,.25,.07,'gold',n=20)
    for k in range(3):
        z=.35+k*.39
        cylinder('Separate marble column drum',(.015*k,0,z+.15),.42,.34,'marble',n=16,bevel=True)
        ring('Drum dark joint',.43,.045,z-.025,.05,'marble_dark',n=16,loc=(.015*k,0,0))
        for j in range(8):
            a=TAU*j/8+.04*k
            box('Eight violet fluting insets',(.015*k+.413*math.cos(a),.413*math.sin(a),z+.15),(.07,.075,.27),'violet',rot=(0,0,a),bevel=False)
    # Lower fracture plane ends at 1.47; suspended drum starts at 1.77.
    cylinder('Sheared lower fracture crown',(.04,0,1.43),.43,.08,'crack_gold',n=16)
    start=len(PARTS)
    cylinder('Hovering sheared top drum',(0,0,.16),.44,.32,'marble',n=16,bevel=True)
    cylinder('Gold fracture underside',(0,0,-.025),.43,.05,'crack_gold',n=16)
    for j in range(8):
        a=TAU*j/8
        box('Suspended violet fluting',(.43*math.cos(a),.43*math.sin(a),.15),(.08,.07,.27),'violet',rot=(0,0,a),bevel=False)
    box('Broken capital dark abacus',(0,0,.38),(1.00,.97,.11),'marble_dark')
    box('Recessed capital pale centre',(0,0,.45),(.61,.61,.045),'marble')
    for j in range(4):
        slab('Four broken L-shaped capital moldings',[(.11,.55),(.55,.55),(.55,.09),(.42,.14),(.42,.40),(.11,.40)],
             .10,'gold',loc=(0,0,.48),rot=(math.pi/2,0,TAU*j/4))
    ornament=star((0,0,0),.22,'crack_gold',.025)
    place(ornament,(0,0,.49),(math.pi/2,0,0))
    group_place(start,(-.07,-.025,1.84),(math.radians(7),math.radians(-6),.08))
    # Preserve a real 30 cm air gap, measured between the actual tilted bounds.
    lowest=min(v.co.z for obj in PARTS[start:] for v in obj.data.vertices)
    for obj in PARTS[start:]: place(obj,(0,0,1.77-lowest))
    tube('Main marble split',[(.25,.365,1.42),(.10,.44,1.23),(.21,.41,1.04),(.07,.44,.86),(.18,.42,.65)],.026,'crack_gold')
    for loc,sz in (((.56,.12,1.98),(.22,.19,.16)),((-.53,.21,2.12),(.17,.20,.13))):
        box('Detached capital fragment',loc,sz,'gold',rot=(.1,.2,.23))


def rift_crack():
    # Long vertical lightning tear; narrow sections remain readable from above.
    contour=[(-.20,-.97),(.22,-.60),(.03,-.38),(.53,-.06),(.27,.12),(.57,.49),
             (.29,.94),(.11,.51),(-.14,.36),(.02,.13),(-.43,-.08),(-.19,-.33),(-.52,-.54)]
    slab('Eight-centimetre void tear',contour,.08,'night_sky')
    for side in (-1,1):
        pts=[(x,side*.055,z) for x,z in contour]+[(contour[0][0],side*.055,contour[0][1])]
        tube('Raised six-centimetre jagged gold rim',pts,.03,'crack_gold',n=4)
        for x,z in ((-.16,-.70),(-.26,-.48),(-.08,-.30),(-.13,-.10),(.14,.01),(.12,.24),(.28,.43),(.29,.69)):
            octa('Stars inside both rift faces',(x,side*.052,z),(.055,.025,.06),'starlight')
    for j,(x,z,a) in enumerate(((-.43,-.52,-.5),(.16,-.61,.3),(-.30,-.11,-.7),(.47,-.03,.5),(-.06,.36,-.45),(.53,.50,.55),(.30,.86,.3))):
        # All seven are actual closed angular shards, not cards or round spikes.
        glass('Seven broken glass teeth',[(-.07,-.10),(.19,-.035),(.10,.18)],.055,
              (x,(-1 if j%2 else 1)*.085,z),(0,a,.21*(-1 if j%2 else 1)))


def shard_bits():
    locations=[(-.40,-.39,.28,.1),(.07,-.37,.31,-.5),(.38,-.22,.24,.6),(-.28,.03,.30,-.8),(.15,.05,.34,.4),(-.38,.38,.22,.6),(.29,.38,.27,-.2)]
    for i,(x,y,s,a) in enumerate(locations):
        glass('Seven flat floor mirror shards',[(-s*.55,-s*.30),(s*.5,-s*.20),(s*.22,s*.48),(-s*.32,s*.31)],.04,
              (x,y,.025+(i%3)*.008),(math.pi/2,0,a))
    for x,y,s in ((-.48,.20,.10),(.45,.15,.13),(.02,.44,.11),(-.03,-.07,.12)):
        rock('Scattered violet pebble',(x,y,.045),(s,s*.9,.09),'void_stone_dark')
    octa('One glint among the shards',(.08,.12,.09),(.06,.06,.06),'starlight')


def floor_path(label, points, width, z, height, role):
    # Rectangular paths keep their entire upper surface exactly at z+height.
    verts=[]
    for i,p in enumerate(points):
        prev=Vector(points[max(0,i-1)]); nxt=Vector(points[min(len(points)-1,i+1)])
        t=(nxt-prev).normalized(); side=Vector((-t.y,t.x))*width/2
        verts += [(p[0]+s*side.x,p[1]+s*side.y,zz) for zz,s in ((z,-1),(z,1),(z+height,1),(z+height,-1))]
    faces=[(3,2,1,0),tuple(range(len(verts)-4,len(verts)))]
    faces += [(4*i+j,4*i+(j+1)%4,4*(i+1)+(j+1)%4,4*(i+1)+j) for i in range(len(points)-1) for j in range(4)]
    return mesh(label,verts,faces,role)


def crack_glow():
    paths=[[(0,0),(.28,.12),(.55,.02),(.81,.29),(1.12,.26),(1.47,.55)],
           [(0,0),(-.27,.23),(-.53,.17),(-.66,.58),(-.98,.79),(-1.43,1.13)],
           [(0,0),(.11,-.30),(.36,-.42),(.25,-.73),(.57,-1.02),(.47,-1.46)],
           [(0,0),(-.35,-.15),(-.51,-.44),(-.90,-.41),(-1.45,-.70)],
           [(.28,.12),(.29,.45),(.62,.76),(.56,1.10),(.75,1.46)],
           [(-.51,-.44),(-.38,-.80),(-.68,-1.15)],
           [(.81,.29),(1.05,-.02),(1.42,-.16)]]
    for p in paths:
        bank=floor_path('Dark chipped crack banks',p,.105,0,.010,'void_stone_dark')
        # Dark upper banks, violet fracture depth/undersides. The gold overlays
        # their middle, so hidden bank tops should not dominate the role audit.
        roles(bank,['void_stone'],[0,0]+[1 if j%4 in (0,1) else 0 for j in range((len(p)-1)*4)])
        floor_path('Raised thin gold lightning',p,.047,.010,.009,'crack_gold')
    # Central gap is genuinely a dark angular inset, not a large gold disk.
    contour=[(-.18,-.10),(.02,-.17),(.19,-.02),(.11,.16),(-.08,.14)]
    slab('Dark opening into the starry void',contour,.008,'night_sky',loc=(0,0,.012),rot=(math.pi/2,0,0))
    for loc in ((-.08,-.04,.025),(.08,.025,.025),(-.015,.09,.025)):
        octa('Three tiny floor-gap stars',loc,(.04,.04,.010),'starlight')


def star_dust():
    for j in range(35):
        a=random.uniform(0,TAU); r=math.sqrt(random.random())*.45
        x,y=r*math.cos(a),r*math.sin(a)
        s=random.uniform(.045,.085)
        octa('Faceted gold and white star dust',(x,y,s*.25),(s,s*.8,s*.5),'starlight' if j%2 else 'gold',rot=(0,0,a))
    for x,y in ((.13,.06),(.04,.13)):
        crystal((x,y,.01),.045,.09,(.13,-.23,0))
    for loc in ((-.10,.04,.025),(.16,-.12,.025),(-.20,-.17,.025)):
        octa('Dark violet dust facets',loc,(.10,.085,.05),'violet')


def fx_glass_shard():
    glass('Single four-sided particle',[(-.125,-.045),(.045,-.060),(.125,.025),(-.040,.060)],.04)


def fx_glass_shard2():
    glass('Single triangular particle',[(-.090,-.090),(.090,-.035),(-.025,.090)],.04)


SPECS=[
    ('WarpedKeg',(1.0,1.0,1.2),warped_keg,5000),
    ('WarpedTrunk',(1.4,.9,1.0),warped_trunk,5000),
    ('MirrorShard',(2.5,.3,2.4),mirror_shard,5000),
    ('WarpedSpring',(1.0,1.0,1.5),warped_spring,5000),
    ('CoverShardRock',(1.8,1.2,1.3),cover_shard_rock,5000),
    ('CoverMemoryPile',(1.8,1.4,1.4),cover_memory_pile,5000),
    ('CoverBrokenColumn',(1.4,1.4,2.4),cover_broken_column,5000),
    ('RiftCrack',(1.6,.3,2.0),rift_crack,5000),
    ('ShardBits',(1.2,1.2,.12),shard_bits,5000),
    ('CrackGlow',(3.0,3.0,.03),crack_glow,5000),
    ('StarDust',(1.0,1.0,.1),star_dust,5000),
    ('FX_GlassShard',(.25,.04,.12),fx_glass_shard,80),
    ('FX_GlassShard2',(.18,.04,.18),fx_glass_shard2,80),
]
CENTRED={'RiftCrack','FX_GlassShard','FX_GlassShard2'}
NO_COLLISION={'ShardBits','CrackGlow','StarDust','FX_GlassShard','FX_GlassShard2'}


def triangles(obj):
    obj.data.calc_loop_triangles()
    return len(obj.data.loop_triangles)


def bounds(obj):
    return [Vector(tuple(fn(v.co[i] for v in obj.data.vertices) for i in range(3))) for fn in (min,max)]


def finish(name,dims,budget):
    bpy.ops.object.select_all(action='DESELECT')
    for obj in PARTS:
        if name=='RiftCrack' and obj.name.startswith(('Eight-centimetre','Raised six-centimetre','Stars inside')):
            group=obj.vertex_groups.new(name='Rift fixed core depth')
            group.add(list(range(len(obj.data.vertices))),1.0,'REPLACE')
        obj.select_set(True); bpy.context.view_layer.objects.active=obj
        for mod in list(obj.modifiers): bpy.ops.object.modifier_apply(modifier=mod.name)
        obj.select_set(False)
    for obj in PARTS: obj.select_set(True)
    bpy.context.view_layer.objects.active=PARTS[0]
    if len(PARTS)>1: bpy.ops.object.join()
    obj=bpy.context.object; obj.name=name; obj.data.name=name+'_Mesh'
    # Never silently decimate recognisable silhouettes or material boundaries.
    assert triangles(obj)<=budget,(name,triangles(obj),budget)
    low,high=bounds(obj); span=high-low; mid=(low+high)/2
    fixed_depth=obj.vertex_groups.get('Rift fixed core depth')
    for v in obj.data.vertices:
        original_z=v.co.z
        v.co.x=(v.co.x-mid.x)*dims[0]/span.x
        v.co.y=(v.co.y-mid.y)*dims[1]/span.y
        v.co.z=(v.co.z-(mid.z if name in CENTRED else low.z))*dims[2]/span.z
        if fixed_depth and any(g.group==fixed_depth.index for g in v.groups):
            v.co.y=v.co.y*span.y/dims[1]+mid.y
        if name=='CoverBrokenColumn':
            # Piecewise vertical fitting protects the specified gap while keeping
            # the exact overall 2.4 m height and the flat footing at zero.
            if original_z<=1.47:
                v.co.z=(original_z-low.z)*1.40/(1.47-low.z)
            elif original_z>=1.77:
                v.co.z=1.70+(original_z-1.77)*.70/(high.z-1.77)
            else:
                v.co.z=1.40+(original_z-1.47)
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
    # Whole-surface check also catches accidentally unassigned details.
    fraction=max(areas.values())/sum(areas.values())
    assert fraction<=.56,(name,'dominant material',round(fraction,3),areas)
    assert all(math.isfinite(c) for v in obj.data.vertices for c in v.co)
    for mat in obj.data.materials:
        assert mat.name in PALETTE
        assert tuple(mat.diffuse_color)==(.5,.5,.5,1)
        shader=mat.node_tree.nodes['Principled BSDF']
        assert tuple(shader.inputs['Base Color'].default_value)==(.5,.5,.5,1)
        assert len(mat.node_tree.nodes)==2
    low,high=bounds(obj)
    assert all(abs(obj.dimensions[i]-dims[i])<1e-4 for i in range(3)),(name,obj.dimensions)
    assert abs(low.x+high.x)<1e-5 and abs(low.y+high.y)<1e-5
    assert abs(low.z+high.z if name in CENTRED else low.z)<1e-5
    assert obj.location.length<1e-6 and all(abs(s-1)<1e-6 for s in obj.scale)
    obj['chapter']='The Collapsing End of Time'; obj['front']='+Y'
    obj['triangle_count']=triangles(obj); obj['collision']=name not in NO_COLLISION
    obj['origin']='centre' if name in CENTRED else 'bottom centre'
    if name in {'WarpedKeg','WarpedSpring'}: obj['interactive']='Shootable explosive'
    if name=='WarpedTrunk': obj['interactive']='Pushable / shootable explosive'; obj['lid_gap_m']=.02
    if name=='MirrorShard': obj['interactive']='Two-sided bullet reflector'; obj['rotation_axis']='Z'
    if name=='RiftCrack': obj['interactive']='Room rift'; obj['void_thickness_m']=.08
    if name=='CoverBrokenColumn':
        assert not any(1.40001<v.co.z<1.69999 for v in obj.data.vertices)
        obj['hover_gap_m']=.30
    if name=='CrackGlow':
        # Bank tops .01, light tops .019; only the three stars exceed .02.
        assert max(v.co.z for p in obj.data.polygons if obj.data.materials[p.material_index].name=='crack_gold' for v in (obj.data.vertices[i] for i in p.vertices))<=.02001
    dest=OUTPUT/f'Kit_{name}.fbx'
    bpy.ops.export_scene.fbx(filepath=str(dest),use_selection=True,
        axis_forward='-Z',axis_up='Y',apply_scale_options='FBX_SCALE_UNITS',
        bake_space_transform=True,object_types={'MESH'},use_mesh_modifiers=True,
        mesh_smooth_type='FACE',add_leaf_bones=False,bake_anim=False,use_custom_props=True)
    assert dest.is_file() and dest.stat().st_size>1024
    print(f'EXPORTED {name} {triangles(obj)} {tuple(round(d,4) for d in obj.dimensions)}',flush=True)
    return obj


def preview(objects):
    scene=bpy.context.scene; scene.render.engine='BLENDER_WORKBENCH'
    scene.render.resolution_x,scene.render.resolution_y=1800,1000
    scene.render.resolution_percentage=100
    scene.render.image_settings.file_format='PNG'; scene.render.filepath=str(PREVIEW)
    scene.view_settings.view_transform='Standard'
    shade=scene.display.shading; shade.light='FLAT'; shade.color_type='MATERIAL'
    shade.show_shadows=False; shade.show_cavity=True; shade.cavity_type='BOTH'
    shade.curvature_ridge_factor=1.5; shade.curvature_valley_factor=1.2
    shade.show_specular_highlight=False; shade.show_object_outline=True
    shade.background_type='WORLD'
    if scene.world is None: scene.world=bpy.data.worlds.new('Review background')
    scene.world.color=(.023,.027,.055)
    helpers=[]
    saved=[(obj,obj.matrix_world.copy()) for obj in objects]
    try:
        for role,mat in MATS.items(): mat.diffuse_color=(*PALETTE[role],1)
        camera=bpy.data.objects.new('Review only camera',bpy.data.cameras.new('Review only camera'))
        scene.collection.objects.link(camera); helpers.append(camera); scene.camera=camera
        az,el=math.radians(19),math.radians(55)
        near=Vector((math.sin(az),math.cos(az),0))
        camera.location=near*(30*math.cos(el))+Vector((0,0,30*math.sin(el)))
        camera.rotation_euler=(-camera.location).to_track_quat('-Z','Y').to_euler()
        camera.data.type='ORTHO'; camera.data.ortho_scale=19.8
        rotation=camera.rotation_euler.to_matrix(); inv=rotation.transposed()

        def caption(body,x,y,size=.19,role='starlight'):
            data=bpy.data.curves.new('Review text','FONT'); data.body=body
            data.size=size; data.align_x='CENTER'; data.materials.append(MATS[role])
            label=bpy.data.objects.new('Review caption',data); scene.collection.objects.link(label)
            label.location=rotation @ Vector((x,y,2)); label.rotation_euler=camera.rotation_euler
            helpers.append(label)

        # Regular rows, with small floor/particle pieces enlarged only for review.
        rows=[([0,1,2,3],[-6.75,-2.25,2.25,6.75],1.85),
              ([4,5,6,7],[-6.75,-2.25,2.25,6.75],-1.55),
              ([8,9,10,11,12],[-7.3,-3.65,0,3.65,7.3],-4.45)]
        for indices,xs,baseline in rows:
            for index,x in zip(indices,xs):
                obj=objects[index]; dims=SPECS[index][1]
                scale=(2.20 if index>=8 else 2.45)/max(dims)
                obj.scale=(scale,)*3
                bpy.context.view_layer.update()
                points=[inv @ (obj.matrix_world @ v.co) for v in obj.data.vertices]
                middle=(min(p.x for p in points)+max(p.x for p in points))/2
                low=min(p.y for p in points)
                obj.location+=rotation @ Vector((x-middle,baseline-low,0))
                caption(obj.name,x,baseline-.28,.20)
                caption(f'{dims[0]:g} x {dims[1]:g} x {dims[2]:g} m  |  {triangles(obj)} tris',x,baseline-.52,.12,'cloth')
        caption('CHAPTER 05  /  THE COLLAPSING END OF TIME',0,4.90,.29,'gold')
        caption('COMBAT PROPS  /  13 MESHES     +Y FRONT     55 DEG CAMERA     INDIVIDUAL REVIEW SCALES',0,4.57,.13,'cloth')
        PREVIEW.parent.mkdir(parents=True,exist_ok=True)
        bpy.ops.render.render(write_still=True)
        assert PREVIEW.is_file() and PREVIEW.stat().st_size>1024
        print(f'PREVIEW {PREVIEW}',flush=True)
    finally:
        scene.camera=None
        for obj in helpers: bpy.data.objects.remove(obj,do_unlink=True)
        for obj,matrix in saved: obj.matrix_world=matrix
        for mat in MATS.values(): mat.diffuse_color=(.5,.5,.5,1)


def main():
    global PARTS
    bpy.ops.wm.read_factory_settings(use_empty=True)
    for mat in list(bpy.data.materials): bpy.data.materials.remove(mat)
    scene=bpy.context.scene; scene.unit_settings.system='METRIC'; scene.unit_settings.scale_length=1
    materials(); OUTPUT.mkdir(parents=True,exist_ok=True)
    objects=[]
    for i,(name,dims,build,budget) in enumerate(SPECS):
        PARTS=[]; random.seed(55031+i*7919)
        build(); objects.append(finish(name,dims,budget))
    assert len(scene.objects)==13 and all(o.type=='MESH' for o in scene.objects)
    preview(objects)
    assert len(scene.objects)==13 and all(o.type=='MESH' for o in scene.objects)
    assert all(tuple(m.diffuse_color)==(.5,.5,.5,1) for m in MATS.values())
    print('DONE',flush=True)


if __name__=='__main__':
    main()
