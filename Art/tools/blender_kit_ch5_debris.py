# 5장 공중 기억 파편 8종과 보스 시계 경기장 3종을 생성하고 FBX와 검토용 PNG를 저장합니다.
# 실행: "C:/Program Files/Blender Foundation/Blender 5.3/blender.exe" -b --factory-startup -P Art/tools/blender_kit_ch5_debris.py
"""Broken memories, modelled in metres, +Y front; deterministic Blender 5.x.

All export shaders are neutral grey. Review colours and layout are applied ONLY
after exports. Floating assets rotate about a volume-weighted centre of mass.
Dial intact faces occupy Z=.00-.08; three fractured leaves hinge DOWN by 2-5deg.
Their exceptional negative Z is intentional; the arena origin is its ground datum.
"""
import json
import hashlib
import math
import random
import warnings
import zipfile
from pathlib import Path

import bmesh
import bpy
from mathutils import Euler, Matrix, Vector

warnings.filterwarnings('ignore', category=DeprecationWarning)

ROOT = Path(__file__).resolve().parents[2]
OUT = ROOT / 'Game/Assets/Art/Characters/Ch5'
PREVIEW = ROOT / 'Art/tools/specs/blender_kit_ch5_debris_preview.png'
LOG = ROOT / 'Art/tools/specs/blender_kit_ch5_debris.log'
AUDIT = ROOT / 'Art/tools/specs/blender_kit_ch5_debris_audit.json'
ARCHIVE = ROOT / 'Art/tools/specs/blender_kit_ch5_debris_exports.zip'
TAU = math.tau
MATS, PARTS, EVENTS, RECORDS = {}, [], [], []
FBX_PAYLOADS = {}
PALETTE = {
    'void_stone': (.29,.18,.43), 'void_stone_dark': (.065,.037,.12),
    'marble': (.71,.61,.84), 'marble_dark': (.29,.22,.39),
    'violet': (.43,.18,.65), 'violet_dark': (.15,.055,.25),
    'indigo': (.045,.035,.14), 'starlight': (.91,.96,1),
    'crack_gold': (1,.76,.17), 'gold': (.96,.59,.13),
    'gold_dark': (.36,.20,.055), 'mirror': (.52,.79,.94),
    'mirror_back': (.10,.19,.28), 'crystal': (.59,.32,.85),
    'wood': (.39,.24,.30), 'wood_dark': (.135,.075,.15),
    'iron_dark': (.045,.055,.10), 'steel': (.42,.52,.64),
    'brass': (.67,.40,.12), 'dial_face': (.81,.75,.88),
    'dial_mark': (.065,.045,.11), 'sand': (.57,.43,.46),
    'lamp_warm': (1,.80,.44), 'magenta': (.81,.045,.47),
}


def report(s):
    EVENTS.append(s)
    print(s, flush=True)


def materials():
    for role in PALETTE:
        m = bpy.data.materials.new(role)
        m.use_nodes = True
        m.diffuse_color = (.5,.5,.5,1)
        bs = m.node_tree.nodes['Principled BSDF']
        bs.inputs['Base Color'].default_value = (.5,.5,.5,1)
        bs.inputs['Metallic'].default_value = 0
        bs.inputs['Roughness'].default_value = .8
        MATS[role] = m


def mesh(label, verts, faces, role, bevel=False):
    d = bpy.data.meshes.new(label)
    d.from_pydata(verts, [], faces)
    d.update()
    ob = bpy.data.objects.new(label, d)
    bpy.context.collection.objects.link(ob)
    roles = [role]*len(faces) if isinstance(role,str) else role
    for r in dict.fromkeys(roles): d.materials.append(MATS[r])
    slots = {m.name:i for i,m in enumerate(d.materials)}
    for p,r in zip(d.polygons,roles): p.material_index=slots[r]
    if bevel:
        mod=ob.modifiers.new('Worn hard-edge chamfer', 'BEVEL')
        mod.width=.025; mod.segments=2
        mod.limit_method='ANGLE'; mod.angle_limit=math.radians(36)
        mod.use_clamp_overlap=True
    PARTS.append(ob)
    return ob


def place(ob, loc=(0,0,0), rot=(0,0,0)):
    ob.data.transform(Matrix.Translation(Vector(loc)) @ Euler(rot).to_matrix().to_4x4())
    return ob


def transform_since(start, loc=(0,0,0), rot=(0,0,0)):
    for ob in PARTS[start:]: place(ob,loc,rot)


def poly(label, outline, low, high, role, bevel=False, side=None):
    pts=list(outline)
    area=sum(a[0]*b[1]-a[1]*b[0] for a,b in zip(pts,pts[1:]+pts[:1]))
    if area<0: pts.reverse()
    n=len(pts)
    v=[(x,y,z) for z in (low,high) for x,y in pts]
    f=[tuple(reversed(range(n))),tuple(range(n,2*n))]
    f += [(j,(j+1)%n,(j+1)%n+n,j+n) for j in range(n)]
    roles=[side or role,role]+[side or role]*n
    return mesh(label,v,f,roles,bevel)


def box(label, loc, size, role, rot=(0,0,0), bevel=True):
    x,y,z=(s/2 for s in size)
    return place(poly(label,[(-x,-y),(x,-y),(x,y),(-x,y)],-z,z,role,bevel),loc,rot)


def beam(label,a,b,width,depth,role,bevel=True):
    a,b=Vector(a),Vector(b)
    ob=box(label,(0,0,0),(width,depth,(b-a).length),role,bevel=bevel)
    ob.data.transform((b-a).to_track_quat('Z','Y').to_matrix().to_4x4())
    return place(ob,(a+b)/2)


def lathe(label, profile, role, n=24, loc=(0,0,0), rot=(0,0,0), bevel=False):
    v=[(r*math.cos(TAU*j/n),r*math.sin(TAU*j/n),z) for r,z in profile for j in range(n)]
    k=len(profile)
    f=[(i*n+j,i*n+(j+1)%n,((i+1)%k)*n+(j+1)%n,((i+1)%k)*n+j)
       for i in range(k) for j in range(n)]
    return place(mesh(label,v,f,role,bevel),loc,rot)


def ring(label,r,width,z,height,role,n=32,loc=(0,0,0),rot=(0,0,0),bevel=False):
    return lathe(label,[(r-width,z),(r,z),(r,z+height),(r-width,z+height)],role,n,loc,rot,bevel)


def cyl(label,loc,r,depth,role,n=16,rot=(0,0,0),bevel=False):
    pts=[(r*math.cos(TAU*j/n),r*math.sin(TAU*j/n)) for j in range(n)]
    return place(poly(label,pts,-depth/2,depth/2,role,bevel),loc,rot)


def tube(label,pts,r,role,n=6):
    v=[]
    for i,p in enumerate(pts):
        t=(Vector(pts[min(i+1,len(pts)-1)])-Vector(pts[max(0,i-1)])).normalized()
        ref=Vector((0,1,0)) if abs(t.y)<.85 else Vector((1,0,0))
        u=ref.cross(t).normalized(); w=t.cross(u)
        v += [tuple(Vector(p)+r*(math.cos(TAU*j/n)*u+math.sin(TAU*j/n)*w)) for j in range(n)]
    f=[tuple(reversed(range(n))),tuple(range(len(v)-n,len(v)))]
    f += [(i*n+j,i*n+(j+1)%n,(i+1)*n+(j+1)%n,(i+1)*n+j)
          for i in range(len(pts)-1) for j in range(n)]
    return mesh(label,v,f,role)


def arc(label,r,width,depth,a0,a1,role,loc=(0,0,0),rot=(0,0,0),n=28,bevel=False):
    angles=[a0+(a1-a0)*j/n for j in range(n+1)]
    pts=[(rr*math.cos(a),rr*math.sin(a)) for rr,aa in
         ((r,angles),(r-width,list(reversed(angles)))) for a in aa]
    return place(poly(label,pts,-depth/2,depth/2,role,bevel),loc,rot)


def shard(label,loc,size,role='crystal'):
    sx,sy,sz=size
    n=5
    angles=[TAU*(j+random.uniform(-.12,.12))/n for j in range(n)]
    v=[(sx*.5*math.cos(a),sy*.5*math.sin(a),random.uniform(-.10,.10)*sz) for a in angles]
    v += [(sx*.29*math.cos(a),sy*.29*math.sin(a),sz*.53) for a in angles]
    v += [(sx*.16,-sy*.12,sz),(-sx*.07,0,-sz*.22)]
    f=[(j,(j+1)%n,(j+1)%n+n,j+n) for j in range(n)]
    f += [(j+n,(j+1)%n+n,2*n) for j in range(n)]
    f += [(j,2*n+1,(j+1)%n) for j in range(n)]
    return place(mesh(label,v,f,[role]*n+['mirror' if role=='crystal' else role]*n+['void_stone_dark']*n),loc)


def star(loc,r=.12):
    x,y,z=loc
    pts=[]
    for j in range(8):
        a=TAU*j/8; rr=r if j%2==0 else r*.25
        pts.append((x+rr*math.cos(a),y+rr*math.sin(a)))
    poly('Four-point star inlay',pts,z,z+.045,'starlight')


def rock(loc=(0,0,0),sx=2.4,sy=1.65,depth=.65,n=15):
    """Three unequal rings; triangular faces, deep asymmetric bites, tapered root."""
    outline=[]
    for j in range(n):
        a=TAU*(j+random.uniform(-.23,.23))/n
        rr=random.uniform(.76,1.04)
        outline.append((sx*.5*rr*math.cos(a),sy*.5*rr*math.sin(a)))
    v=[(x,y,0) for x,y in outline]
    v += [(x*random.uniform(.86,1.12),y*random.uniform(.84,1.12),-depth*random.uniform(.3,.57)) for x,y in outline]
    v += [(x*.37+.10,y*.32-.08,-depth*random.uniform(.86,1.12)) for x,y in outline]
    v += [(.06,-.05,-depth*1.2),(.04,-.02,.005)]
    f=[(j,(j+1)%n,3*n+1) for j in range(n)]
    roles=[('marble_dark' if j%4==0 else 'void_stone') for j in range(n)]
    for k in range(2):
        for j in range(n):
            a=k*n+j; b=k*n+(j+1)%n; c=(k+1)*n+(j+1)%n; d=(k+1)*n+j
            f.extend([(a,b,c),(a,c,d)])
            roles.extend([random.choice(['void_stone_dark','void_stone','marble_dark'])]*2)
    for j in range(n):
        f.append((2*n+j,3*n,2*n+(j+1)%n)); roles.append('void_stone_dark')
    ob=place(mesh('Jagged suspended island root',v,f,roles),loc)
    x,y,z=loc
    for j in (1,5,10):
        j=j%n; xx,yy=outline[j]
        tube('Gold-lit root fracture drip',[(x+xx,y+yy,z+.025),(x+xx*.92+.04,y+yy*.92,z-depth*.23),
             (x+xx*.67,y+yy*.6,z-depth*.55),(x+xx*.64+.05,y+yy*.57,z-depth*.78)],.025,'crack_gold')
    return outline,ob


def detached(sx,sy,z=0,count=2,outline=None):
    # Surface-to-surface separation from the root is .12-.25m, not an orbiting halo.
    for j in range(count):
        side=-1 if j%2 else 1
        if outline:
            a=(0,math.pi,.94)[j]
            direction=Vector((math.cos(a),math.sin(a)))
            edge=max(outline,key=lambda p:Vector(p).dot(direction))
            x,y=Vector(edge)+direction*.31
        else:
            x,y=side*(sx*.49+.24),(-.17+j*.34)*sy
        rock((x,y,z+.12),.28,.25,.22,n=7)
        shard('Detached mirrored fracture splinter',(x,y,z+.14),(.12,.15,.22),'mirror')


def tiles(sx,sy,z=.015,nx=5,ny=3):
    """Clipped irregular floor shards; broad seams and alternating readable faces."""
    for j in range(ny):
        for i in range(nx):
            x=(i-(nx-1)/2)*sx/nx; y=(j-(ny-1)/2)*sy/ny
            if (i in (0,nx-1) and j in (0,ny-1)): continue
            w=sx/nx*.9; h=sy/ny*.87
            pts=[(-w*.5,-h*.4),(-w*.19,-h*.52),(w*.44,-h*.42),
                 (w*.52,h*.13),(w*.34,h*.48),(-w*.42,h*.46),(-w*.53,h*.04)]
            pts=[(a*random.uniform(.87,1.05),b*random.uniform(.87,1.05)) for a,b in pts]
            poly('Broken floor tile',[(x+a,y+b) for a,b in pts],z-.07,z+random.uniform(0,.025),
                 ['marble','void_stone','marble_dark'][(i+2*j)%3],True,side='void_stone_dark')
    tube('Lightning floor inlay',[(-sx*.38,sy*.10,z+.04),(-sx*.18,sy*.06,z+.04),
         (-sx*.09,-sy*.08,z+.04),(sx*.10,0,z+.04),(sx*.26,-sy*.18,z+.04),(sx*.40,-sy*.16,z+.04)],.026,'crack_gold')


def root(sx=2.5,sy=1.65,depth=.6,tiled=True,count=2):
    outline,_=rock(sx=sx,sy=sy,depth=depth)
    if tiled: tiles(sx*.83,sy*.80)
    detached(sx,sy,count=count,outline=outline)


def jagged_board(label,loc,width,height,thick,role,rot=(0,0,0),bevel=True):
    # Top / bottom fractures differ; manufactured side edges remain recognisable.
    w=width/2; h=height/2
    outline=[(-w,-h*random.uniform(.65,.96)),(-w*random.uniform(.37,.65),-h*random.uniform(.79,1.0)),
             (-w*random.uniform(.03,.20),-h*random.uniform(.50,.81)),(w*random.uniform(.25,.51),-h),
             (w,-h*random.uniform(.65,.91)),(w,h*random.uniform(.64,.91)),
             (w*random.uniform(.47,.70),h*random.uniform(.53,.86)),(w*random.uniform(.07,.27),h),
             (-w*random.uniform(.12,.31),h*random.uniform(.58,.87)),(-w*random.uniform(.52,.76),h*random.uniform(.80,.99)),
             (-w,h*random.uniform(.61,.88))]
    ob=poly(label,outline,-thick/2,thick/2,role,bevel)
    return place(ob,loc,rot)


def saloon():
    root(2.45,1.55,.58,False)
    for i in range(7):
        # Model in XY: deliberately torn end grain at the porch's front.
        jagged_board('Splintered porch plank',(-1.00+i*.33,.05,.10),.30,1.42,.11,
                     'wood' if i%2 else 'wood_dark')
        for y in (-.39,.51): cyl('Porch nail',(-1+i*.33,y,.17),.026,.035,'brass',n=6)
    for x,h in ((-.94,1.95),(.94,1.73)):
        jagged_board('Snapped door jamb',(x,-.30,h/2+.10),.19,h,.21,'wood_dark',(math.pi/2,0,0))
        box('Jamb brass collar',(x,-.16,.36),(.25,.08,.18),'brass')
    for side in (-1,1):
        start=len(PARTS)
        for i in range(3):
            jagged_board('Swinging door broken stave',(side*(.18+i*.22),0,.87),.205,1.05,.10,'wood',(math.pi/2,0,0),False)
        for z in (.50,1.14): box('Saloon door crossrail',(side*.40,.075,z),(.67,.07,.11),'wood_dark')
        for z in (.54,1.11): cyl('Door hinge',(side*.77,-.01,z),.05,.23,'brass',n=10)
        transform_since(start,(0,-.18,0),(0,0,side*.21))
    start=len(PARTS)
    jagged_board('Crooked six-relief sign',(0,0,0),2.16,.43,.16,'wood_dark',(math.pi/2,0,0))
    for i,h in enumerate((.20,.27,.17,.26,.22,.19)):
        box('Abstract block sign relief',(-.84+i*.33,.115,.015),(.20,.065,h),'gold',rot=(0,0,.05),bevel=False)
    for x in (-.91,.92): cyl('Sign forged pin',(x,.13,0),.04,.055,'brass',n=8,rot=(math.pi/2,0,0))
    transform_since(start,(0,-.28,1.93),(0,-.08,0))
    tube('Gold fracture across saloon door',[(-.55,.01,.72),(-.30,.02,.78),(-.18,.03,.65),(.08,.02,.85),(.42,.01,.97)],.026,'crack_gold')
    # Folded metal star and a violet mirror sliver connect the western memory to ch5.
    star((-.75,.47,.18),.17)
    shard('Porch memory crystal',(.89,.46,.15),(.18,.18,.40))


def rail():
    root(2.65,1.64,.56,False)
    for i,x in enumerate((-.80,0,.80)):
        jagged_board('Three shattered rail sleepers',(x,0,.09),.33,1.58,.19,'wood_dark',rot=(0,0,(-.04,.02,.08)[i]))
        for y in (-.49,.49):
            box('Rail chair plate',(x,y,.225),(.31,.32,.07),'iron_dark',bevel=False)
            for xx in (-.095,.095): cyl('Track spike',(x+xx,y+.10,.28),.035,.07,'brass',n=6)
    for side in (-1,1):
        y=side*.49
        pts=[(-1.2,y,.29),(-.85,y,.29),(-.2,y+.025,.30),(.43,y+.09,.38),(.84,y+.14,.67),(1.08,y+.11,1.03)]
        for k,rr in ((-.04,.09),(.10,.065)):
            tube('Bent rail flange' if k<0 else 'Bent polished rail head',[(x,yy,z+k) for x,yy,z in pts],rr,'steel',n=8)
        for a,b in zip(pts,pts[1:]):
            aa=(a[0],a[1],a[2]+.025); bb=(b[0],b[1],b[2]+.025)
            beam('I-rail dark web',aa,bb,.065,.16,'iron_dark',False)
        jagged_board('Frayed snapped rail end',(1.08,y+.11,1.07),.17,.24,.09,'steel',rot=(.2,.9,0),bevel=False)
    cyl('Station post shoe',(-.88,-.56,.34),.20,.20,'iron_dark',n=12,bevel=True)
    cyl('Snapped fluted lamp post',(-.88,-.56,.79),.095,.80,'iron_dark',n=12,rot=(0,-.13,0),bevel=True)
    for z in (.45,.67,1.03): ring('Post brass collar',.12,.035,z,.08,'brass',n=12,loc=(-.88,-.56,0))
    jagged_board('Torn metal post lip',(-.93,-.56,1.20),.18,.27,.13,'steel',rot=(math.pi/2,0,0),bevel=False)
    # The lamp crown remains as a broken, tethered memory fragment on the island.
    start=len(PARTS)
    cyl('Lamp roof',(0,0,.48),.26,.10,'brass',n=6,bevel=True)
    cyl('Lamp foot',(0,0,.03),.21,.07,'iron_dark',n=6,bevel=True)
    for x in (-.15,.15):
        for y in (-.15,.15): beam('Lamp cage',(x,y,.07),(x*.86,y*.86,.45),.05,.05,'iron_dark',False)
    shard('Fractured station lamp lens',(0,0,.12),(.24,.24,.25),'lamp_warm')
    transform_since(start,(.23,-.62,.34),(0,.50,-.20))
    tube('Railway gold rupture',[(-.58,-.70,.18),(-.43,-.38,.20),(-.50,-.08,.17),(-.22,.24,.19),(-.29,.59,.17)],.028,'crack_gold')


def clock_half(label,r,side,offset):
    # Vertical XZ dial, front normal +Y. Both cut boundaries have a jagged tooth line.
    ang=[-math.pi/2+math.pi*j/24 for j in range(25)]
    outer=[(side*r*math.cos(a),r*math.sin(a)) for a in ang]
    cut=[(side*.035,-r*.78),(-side*.10,-r*.43),(side*.08,-r*.17),(-side*.07,r*.12),
         (side*.11,r*.36),(-side*.04,r*.65)]
    outline=outer+list(reversed(cut))
    ob=poly(label,outline,-.06,.055,'dial_face',False,side='void_stone_dark')
    # +Y front is the lower cap after the 90deg rotation.
    ob.data.polygons[0].material_index=next(i for i,m in enumerate(ob.data.materials) if m.name=='dial_face')
    return place(ob,offset,(math.pi/2,0,0))


def station_clock():
    root(1.92,1.20,.47)
    start=len(PARTS)
    radius=.93
    for side in (-1,1):
        dx=side*.10; dz=.035 if side>0 else -.035
        clock_half('Two genuinely separated cracked dial halves',radius,side,(dx,0,dz))
        a0,a1=(-math.pi/2+.025,math.pi/2-.025) if side>0 else (math.pi/2+.025,3*math.pi/2-.025)
        arc('Broken brass clock case',1.01,.10,.19,a0,a1,'brass',loc=(dx,-.025,dz),rot=(math.pi/2,0,0),n=26)
        arc('Raised gold bezel',.94,.045,.055,a0,a1,'gold',loc=(dx,.10,dz),rot=(math.pi/2,0,0),n=26)
    for j in range(48):
        a=TAU*j/48; side=1 if math.sin(a)>0 else -1
        x=.81*math.sin(a)+side*.10; z=.81*math.cos(a)+(.035 if side>0 else -.035)
        box('Engraved clock minute batons',(x,.095,z),(.045,.045,.10 if j%4==0 else .045),'dial_mark',rot=(0,a,0),bevel=False)
    for a,length in ((-.60,.64),(1.12,.74)):
        beam('Frozen spear clock hands',(.10,.15,0),(.10+math.sin(a)*length,.15,math.cos(a)*length),.065,.055,'dial_mark',False)
    cyl('Clock hand pivot',(.10,.19,.025),.105,.075,'gold',n=12,rot=(math.pi/2,0,0),bevel=True)
    tube('Broken dial gold seam',[(-.06,.08,-.71),(-.17,.08,-.43),(.01,.08,-.17),(-.14,.08,.12),(.03,.08,.36),(-.12,.08,.65)],.025,'crack_gold')
    transform_since(start,(0,0,1.17),(math.radians(22),0,-.09))
    # A side-supported hanging clock still has a root for consistent floating silhouettes.
    beam('Bent wrought bracket upright',(-.83,-.23,.12),(-.92,-.29,2.30),.13,.13,'iron_dark')
    tube('Broken bracket arch',[(-.92,-.29,2.15),(-.93,-.29,2.43),(-.75,-.29,2.56),(-.19,-.29,2.56),(.04,-.29,2.46)],.065,'iron_dark',n=8)
    ring('Hanging clock bail',.17,.055,0,.08,'brass',n=16,loc=(-.05,-.20,2.35),rot=(math.pi/2,0,0))
    tube('Bracket flourish',[(-.90,-.28,1.85),(-.53,-.28,2.11),(-.44,-.28,2.31),(-.56,-.28,2.39)],.045,'gold_dark')


def gear(label,r,teeth,loc,role='brass',chipped=False):
    n=teeth*4; outline=[]; inner=[]
    for j in range(n):
        a=TAU*j/n
        rr=r*(1 if j%4 in (1,2) else .86)
        if chipped and j in (2,3,4,5,6): rr*= (.78,.66,.81,.67,.89)[j-2]
        outline.append((rr*math.cos(a),rr*math.sin(a)))
        inner.append((r*.58*math.cos(a),r*.58*math.sin(a)))
    v=[(x,y,z) for z,curve in ((-.10,outline),(.10,outline),(.10,inner),(-.10,inner)) for x,y in curve]
    f=[(k*n+j,k*n+(j+1)%n,((k+1)%4)*n+(j+1)%n,((k+1)%4)*n+j) for k in range(4) for j in range(n)]
    place(mesh(label,v,f,[role]*n+['gold']*n+['gold_dark']*n+['gold_dark']*n),loc)
    x,y,z=loc
    ring('Gear inner machined rim',r*.62,.065,z+.09,.06,'gold_dark',n=24,loc=(x,y,0))
    for j in range(4):
        a=TAU*j/4+.3
        beam('Four substantial gear spokes',(x,y,z),(x+r*.61*math.cos(a),y+r*.61*math.sin(a),z),.105,.13,'gold_dark',False)
        cyl('Gear spoke rivet',(x+r*.73*math.cos(a),y+r*.73*math.sin(a),z+.135),.045,.06,'steel',n=8)
    cyl('Axle hexagonal collar',(x,y,z+.11),r*.22,.13,'brass',n=8,bevel=True)
    cyl('Black axle',(x,y,z+.20),r*.085,.11,'iron_dark',n=12)


def gears():
    root(2.60,1.73,.52,tiled=True)
    start=len(PARTS)
    gear('Large fourteen-tooth memory gear',.75,14,(-.46,.07,.34),'brass',True)
    gear('Medium interlocked gear',.55,11,(.66,.30,.42),'gold_dark')
    gear('Small third interlocked gear',.39,9,(.36,-.54,.48),'brass')
    transform_since(start,(0,0,.11),(.20,-.10,.03))
    tube('Chipped gear glowing fault',[(-.30,.34,.70),(-.48,.45,.67),(-.61,.34,.65),(-.85,.43,.64)],.027,'crack_gold')
    beam('Dangling pendulum stem',(.10,.57,.28),(.20,.85,-.45),.08,.08,'gold_dark')
    cyl('Pendulum bob',(.21,.88,-.52),.30,.14,'brass',n=24,rot=(math.pi/2,0,0),bevel=True)
    cyl('Pendulum inset medallion',(.21,.97,-.52),.21,.06,'gold_dark',n=16,rot=(math.pi/2,0,0))
    cyl('Pendulum centre star jewel',(.21,1.01,-.52),.085,.07,'crystal',n=6,rot=(math.pi/2,0,0))


def mirror_frame():
    root(1.88,1.21,.51)
    start=len(PARTS)
    # Broken pointed arch: three concentric open bands with one missing section.
    for rr,width,dep,role in ((1,.16,.17,'gold_dark'),(.91,.08,.19,'gold'),(.80,.055,.12,'brass')):
        ob=arc('Three-layer broken oval mirror molding',rr,width,dep,-1.38,4.55,role,n=32)
        for v in ob.data.vertices: v.co.x*=.71; v.co.y*=1.04
    for side in (-1,1):
        tube('Baroque curling frame vine',[(side*.49,-.72,.13),(side*.79,-.52,.13),(side*.86,-.27,.13),
             (side*.76,-.14,.13),(side*.64,-.27,.13),(side*.70,-.39,.13)],.055,'gold',n=8)
        for y in (-.45,.12,.61):
            poly('Raised laurel leaf',[(side*.68,y-.13),(side*.83,y-.03),(side*.72,y+.13),(side*.63,y+.02)],.11,.18,'brass',True)
    for y in (-.97,.97):
        star((0,y,.16),.18)
        cyl('Mirror crest cabochon',(0,y,.15),.11,.09,'crystal',n=8,bevel=True)
    fragments=[ [(-.56,-.48),(-.19,-.65),(-.09,-.15),(-.43,.17),(-.61,-.06)],
                [(-.17,-.70),(.32,-.70),(.50,-.41),(.09,-.17)],
                [(.11,-.11),(.57,-.34),(.59,.28),(.25,.14)],
                [(-.49,.23),(-.12,-.08),(.12,.24),(-.18,.73),(-.42,.58)],
                [(-.09,.77),(.17,.25),(.53,.36),(.37,.73),(.04,.91)]]
    for i,pts in enumerate(fragments):
        poly('Retained shattered silver-blue glass',pts,-.055,.055,'mirror',False,side='mirror_back')
    poly('Glass shard floating out of its fracture',[(.10,-.11),(.39,-.24),(.27,.10)],.20,.26,'mirror',False,side='mirror_back')
    tube('Gold fracture in glass',[(-.10,-.62,.065),(-.04,-.18,.065),(.17,.21,.065),(.02,.65,.065)],.025,'crack_gold')
    # Tilt away from the front so the mirror's face reads from the high game camera.
    transform_since(start,(0,-.05,1.07),(math.radians(-64),0,.07))
    for x,y,s in ((-.24,.40,.65),(.22,.30,.55),(.01,.70,.37)):
        rock((x,y,.05),s,s*.70,.09,n=9)
        pts=[(x+s*.48*math.cos(TAU*j/9),y+s*.32*math.sin(TAU*j/9)) for j in range(9)]
        poly('Desert sand spilling from broken mirror',pts,.065,.13,'sand',True)
    for x,y,z in ((-.30,.87,-.05),(-.22,.87,-.28),(-.13,.86,-.48)):
        shard('Drifting sand clod',(x,y,z),(.12,.10,.13),'sand')


def pillar():
    root(2.00,1.62,.64)
    start=len(PARTS)
    n=18
    # Irregular jagged crown; alternating fluted panels, layered plinth and collars.
    v=[]
    for k in range(3):
        for j in range(n):
            a=TAU*j/n; rr=(.57 if j%2 else .64)*(1 if k<2 else random.uniform(.86,1.04))
            z=(.11,.98,1.36+random.uniform(-.25,.16))[k]
            v.append((rr*math.cos(a),rr*math.sin(a),z))
    f=[tuple(reversed(range(n))),tuple(range(2*n,3*n))]
    roles=['void_stone','sand']
    for k in range(2):
        for j in range(n):
            f.append((k*n+j,k*n+(j+1)%n,(k+1)*n+(j+1)%n,(k+1)*n+j))
            roles.append('sand' if j%3 else 'void_stone')
    mesh('Tilted broken sandstone pillar drum',v,f,roles,True)
    for r,z,h,role in ((.78,.04,.13,'void_stone'),(.70,.17,.12,'sand'),(.65,.29,.07,'gold_dark'),(.66,.88,.08,'sand')):
        ring('Pillar carved molding',r,.19,z,h,role,n=18,bevel=True)
    for j in range(9):
        a=TAU*j/9
        box('Relief pillar band block',(.59*math.cos(a),.59*math.sin(a),.69),(.12,.07,.19),'gold_dark',rot=(0,0,a+math.pi/2),bevel=False)
    tube('Glowing stepped pillar fault',[(.10,.62,.18),(.19,.61,.45),(.03,.63,.62),(.16,.61,.86),(.08,.59,1.19)],.030,'crack_gold')
    for x,y,z,s in ((.21,.07,1.25,.71),(-.21,.13,1.29,.47),(.40,.02,1.12,.43),(-.05,-.18,1.23,.57)):
        ob=shard('Crystal growing through fractured crown',(x,y,z),(.26,.28,s))
        # Tilt facets apart rather than stacking identical crystals.
        place(ob,(0,0,0),(0,.08*x,.06*y))
    transform_since(start,(0,0,.08),(.16,-.24,-.12))


def bell_sector(a0,a1):
    # Closed shell with hollow interior, crown, thick flared mouth, and torn lips.
    profile=[(.12,1.36),(.25,1.32),(.35,1.22),(.39,.96),(.47,.73),(.63,.53),
             (.68,.48),(.68,.39),(.59,.38),(.54,.49),(.39,.73),(.31,1.02),(.25,1.19),(.12,1.25)]
    steps=20; k=len(profile)
    v=[]
    for i in range(steps+1):
        for j,(r,z) in enumerate(profile):
            a=a0+(a1-a0)*i/steps
            if i==0: a += (.0,.028,-.024,.049,-.018,.031,-.022,.01,.0,.02,-.02,.04,-.01,.0)[j]
            if i==steps: a += (.01,-.02,.02,-.047,.018,-.028,.018,-.01,0,-.02,.02,-.03,.01,0)[j]
            v.append((r*math.cos(a),r*math.sin(a),z))
    f=[]; roles=[]
    for i in range(steps):
        for j in range(k):
            f.append((i*k+j,i*k+(j+1)%k,(i+1)*k+(j+1)%k,(i+1)*k+j))
            roles.append('gold_dark' if j>=8 else ('gold' if j in (4,6) else 'brass'))
    f += [tuple(reversed(range(k))),tuple(range(steps*k,(steps+1)*k))]; roles+=['crack_gold','crack_gold']
    return mesh('Hollow bell shell with irregular open fracture',v,f,roles)


def bell():
    root(2.02,1.45,.55)
    start=len(PARTS)
    body_start=len(PARTS)
    bell_sector(.12,math.pi+.035); bell_sector(math.pi+.15,TAU-.015)
    for z,r in ((.57,.60),(1.13,.37)):
        arc('Broken bell decorative collar',r,.045,.055,.16,3.15,'gold',loc=(0,0,z),n=20)
        arc('Broken bell decorative collar',r,.045,.055,3.31,6.23,'gold',loc=(0,0,z),n=20)
    # Put the main open fracture on the +Y face, visible from the game camera.
    transform_since(body_start,rot=(0,0,math.pi/2))
    ring('Bell crown lifting loop',.16,.065,0,.09,'brass',n=16,loc=(0,0,1.48),rot=(math.pi/2,0,0))
    beam('Iron clapper stem',(0,0,1.24),(.12,.09,.40),.075,.075,'iron_dark',False)
    cyl('Bell clapper bob',(.12,.09,.36),.13,.20,'iron_dark',n=12,bevel=True)
    for x in (-.73,.73):
        jagged_board('Broken bell yoke upright',(x,0,.87),.18,1.53,.18,'wood_dark',(math.pi/2,0,0))
        box('Yoke iron clasp',(x,.11,1.36),(.24,.075,.22),'iron_dark')
    jagged_board('Torn wooden bell cross-yoke',(0,0,1.70),1.73,.25,.23,'wood_dark',(math.pi/2,0,0))
    for x in (-.68,.68): cyl('Yoke bolt',(x,.17,1.70),.07,.10,'brass',n=8,rot=(math.pi/2,0,0))
    for j in range(8):
        a=TAU*j/8+.18
        cyl('Bell skirt raised stud',(.44*math.cos(a),.44*math.sin(a),.83),.047,.07,'gold',n=8)
    tube('Molten bell fault',[(-.07,.64,.45),(-.06,.51,.69),(-.07,.41,.90),(-.05,.36,1.13)],.026,'crack_gold')
    transform_since(start,(0,0,.12),(-.22,.12,.13))


def island():
    root(2.80,2.21,.83,tiled=False,count=3)
    tiles(2.60,2.03,.02,nx=6,ny=5)
    # Broad geometric inlays and starlight, sparse enough to remain a filler island.
    arc('Fragment of embedded clock circle',.78,.055,.05,.3,2.75,'gold_dark',loc=(0,0,.07),n=22)
    arc('Fractured inner clock arc',.63,.045,.05,3.1,4.6,'gold',loc=(0,0,.075),n=12)
    for x,y,r in ((-.74,.34,.11),(.59,-.46,.15),(.21,.65,.09)):
        star((x,y,.10),r)
    for x,y,z in ((-.87,-.57,.15),(.76,.53,.20),(.52,.71,.30)):
        shard('Small island starlight dust',(x,y,z),(.08,.08,.12),'starlight')


ROMANS = ('I','II','III','IV','V','VI','VII','VIII','IX','X','XI','XII')


def numeral(label,text,center,height,width,role,depth=.055):
    """Roman shapes made from solid blocks, never text curves in exported assets."""
    start=len(PARTS); advance=height*.43; total=advance*len(text)
    def bar(a,b):
        dx=b[0]-a[0]; dy=b[1]-a[1]
        box(label,((a[0]+b[0])/2,(a[1]+b[1])/2,depth/2),
            (width,math.hypot(dx,dy),depth),role,rot=(0,0,-math.atan2(dx,dy)),bevel=False)
    for i,c in enumerate(text):
        x=(i+.5)*advance-total/2
        if c=='I':
            box(label,(x,0,depth/2),(width,height,depth),role,bevel=False)
            for y in (-height*.46,height*.46): box('Roman block serif',(x,y,depth/2),(width*2.1,width,depth),role,bevel=False)
        elif c=='V':
            for side in (-1,1): bar((x+side*advance*.35,height*.48),(x,-height*.46))
        else:
            for side in (-1,1): bar((x-side*advance*.32,-height*.48),(x+side*advance*.32,height*.48))
    transform_since(start,center)


def edge_path(i,side):
    """Shared jagged radial boundary. Parallel offsets create measured open seams.

    Gap width varies .34-.98m, so each neighbour has the same fracture contour
    rather than independent teeth which could accidentally collide.
    """
    a=TAU*i/12
    t=Vector((-math.sin(a),math.cos(a)))
    u=Vector((math.cos(a),math.sin(a)))
    result=[]
    for j,r in enumerate((.82,2.6,4.0,5.8,7.2,9.1,10.9,12.5,14.0,15.8,17.3,18.7,20.1,21)):
        wiggle=(0,.19,-.21,.68,.07,-.60,.81,-.14,.70,-.59,.12,-.72,.35,0)[j]*(.83+.16*math.sin(i*2.3))
        gap=(.40,.52,.66,.43,.81,.48,.94,.66)[(j+i)%8]
        p=u*r+t*(wiggle+side*gap/2)
        if j==13: p=p.normalized()*21
        result.append(tuple(p))
    return result


def arena():
    # No backing disc: all twelve winding radial gaps really show through to void.
    for i in range(12):
        start=len(PARTS); a=TAU*(i+.5)/12
        left=edge_path(i,1); right=edge_path(i+1,-1)
        outer_a=math.atan2(left[-1][1],left[-1][0]); outer_b=math.atan2(right[-1][1],right[-1][0])
        while outer_b<outer_a: outer_b+=TAU
        outer=[(21*math.cos(outer_a+(outer_b-outer_a)*j/6),21*math.sin(outer_a+(outer_b-outer_a)*j/6)) for j in range(1,6)]
        outline=left+outer+list(reversed(right))
        poly('Twelve torn clock-dial leaf foundations',outline,.0,.025,'void_stone_dark')
        # Four broad top strata with broken radial edges. Every leaf includes marble.
        for j in range(len(left)-1):
            p,q=left[j],left[j+1]; r,s=right[j+1],right[j]
            for k in range(2):
                t0=k/2; t1=(k+1)/2
                def interp(a,b,t): return (a[0]*(1-t)+b[0]*t,a[1]*(1-t)+b[1]*t)
                pts=[interp(p,s,t0),interp(q,r,t0),interp(q,r,t1),interp(p,s,t1)]
                cx=sum(p[0] for p in pts)/4; cy=sum(p[1] for p in pts)/4
                pts=[(cx+(x-cx)*.994,cy+(y-cy)*.994) for x,y in pts]
                role='marble' if (j+k+i)%3!=0 else 'void_stone'
                if j in (4,5,6,7): role='void_stone' if k==0 else 'marble_dark'
                poly('Two-tone segmented dial stone face',pts,.025,.044,role,side='marble_dark')
        # Long arcs stop short of each torn boundary; no band bridges open void.
        aa=TAU*i/12+.038; bb=TAU*(i+1)/12-.038
        def boundary_angle(path,radius):
            for p,q in zip(path,path[1:]):
                rp,rq=math.hypot(*p),math.hypot(*q)
                if rp<=radius<=rq:
                    t=(radius-rp)/(rq-rp)
                    return math.atan2(p[1]*(1-t)+q[1]*t,p[0]*(1-t)+q[0]*t)
            return math.atan2(path[-1][1],path[-1][0])
        for r,w,h,role in ((20.86,.24,.060,'gold'),(20.30,.10,.065,'gold_dark'),(16.2,.10,.064,'gold'),
                           (10.5,.07,.058,'crack_gold'),(4.7,.10,.061,'gold_dark')):
            ar=boundary_angle(left,r-w/2)+.012; br=boundary_angle(right,r-w/2)-.012
            while br<ar: br+=TAU
            arc('Broken concentric dial rim',r,w,.026,ar,br,role,loc=(0,0,h-.013),n=10)
        # Roman reliefs set radially; all non-tilted top geometry <= .08m.
        ns=len(PARTS)
        numeral('Large Roman hour relief',ROMANS[i],(0,0,.045),1.52,.16,'dial_mark',.03)
        transform_since(ns,(18.25*math.cos(a),18.25*math.sin(a),0),(0,0,a-math.pi/2))
        for j in range(5):
            t=aa+(bb-aa)*(j+.5)/5
            box('Clock minute relief blocks',(19.95*math.cos(t),19.95*math.sin(t),.060),(.50,.10,.028),
                'dial_mark',rot=(0,0,t),bevel=False)
        # Lightning-shaped seams visibly branch into the stone, leaving the gaps clear.
        for radius in (6.5,12.4):
            path=[]
            for d,da in ((-1.8,-.08),(-.6,.01),(.1,-.03),(.8,.06),(2,.03)):
                t=a+da; path.append(((radius+d)*math.cos(t),(radius+d)*math.sin(t),.059))
            tube('Branching gold floor fracture',path,.021,'crack_gold',n=4)
            p=path[2]; tube('Gold crack side branch',[p,(p[0]+.5*math.cos(a+.6),p[1]+.5*math.sin(a+.6),.059),
                 (p[0]+1.15*math.cos(a+1),p[1]+1.15*math.sin(a+1),.059)],.021,'crack_gold',n=4)
        for r in (8.8,14.6):
            ns=len(PARTS)
            star((0,0,.031),.28)
            # Star caps would otherwise exceed the arena relief envelope.
            transform_since(ns,(r*math.cos(a),r*math.sin(a),0),(0,0,a))
        if i in (2,6,9):
            angle=math.radians({2:2.2,6:3.3,9:4.6}[i])
            # Hinge along the left jagged boundary's nominal radius; tilt below 0.
            axis=Vector((math.cos(TAU*i/12),math.sin(TAU*i/12),0))
            mat=Matrix.Rotation(-angle,4,axis)
            for ob in PARTS[start:]: ob.data.transform(Matrix.Translation((0,0,-.035)) @ mat)


def chain(a,b,links=9,r=.16):
    a,b=Vector(a),Vector(b)
    for j in range(links):
        t=j/(links-1); p=a.lerp(b,t); p.z-=.65*math.sin(math.pi*t)
        rot=(math.pi/2,0,0) if j%2 else (math.pi/2,math.pi/2,0)
        ring('Open interlocked iron chain link',r,.055,-.025,.055,'iron_dark',n=12,loc=p,rot=rot)


def hand_spire():
    # Main clock-hand length is exactly 9m along its tilted longitudinal axis.
    # It enters the ground at local (0,0,0); lower socket rock stays above datum.
    start=len(PARTS)
    outline=[(-.20,0),(-.48,.55),(-.39,.82),(-.67,1.17),(-.51,1.61),(-.28,1.86),
             (-.24,6.94),(-.68,7.19),(-.46,7.55),(-.20,8.03),(.025,9),
             (.27,8.18),(.57,7.62),(.73,7.26),(.30,7.03),(.28,1.92),
             (.58,1.47),(.42,1.14),(.56,.81),(.24,.39)]
    # XY silhouette becomes upright XZ, front side +Y, then leans 27deg.
    poly('Nine-metre broken clock-hand blade',outline,-.19,.19,'gold_dark',True,side='void_stone_dark')
    # Huge broad face inlay is broken into two discontinuous tapered panels.
    poly('Blade gold inset lower',[(-.13,.71),(-.17,1.73),(-.14,4.08),(.15,4.00),(.18,1.75),(.28,1.43)],.19,.27,'gold',True)
    poly('Blade gold inset upper',[(-.13,4.37),(-.16,7.10),(-.35,7.31),(.015,8.64),(.35,7.34),(.17,7.10),(.13,4.46)],.19,.27,'gold',True)
    tube('Hand fractured gold vein',[(-.28,4.15,.21),(-.04,4.28,.21),(.05,4.15,.21),(.29,4.31,.21)],.055,'crack_gold')
    for y in (1.28,2.48,3.25,5.23,6.27):
        cyl('Clock-hand steel fastening',(0,y,.29),.11,.10,'steel',n=8,bevel=True)
    for x,y in ((-.23,7.32),(.22,7.33)):
        poly('Broken hand spear notch',[(x-.11,y),(x+.10,y-.11),(x+.10,y+.27)],.23,.31,'gold')
    for ob in PARTS[start:]: ob.data.transform(Matrix.Diagonal((1,1,-1,1)))
    transform_since(start,(0,0,0),(math.pi/2,math.radians(27),-.13))
    # Collar and roots: no centering operation is allowed to move the entry pivot.
    rock((0,0,.22),2.20,1.84,.18,n=17)
    for i in range(6):
        a=TAU*i/6
        shard('Entry fracture rock shard',(.70*math.cos(a),.61*math.sin(a),.10),(.30,.33,.49),'void_stone')
    for a,b in (((1.24,.4,3.05),(-.68,.75,.20)),((1.82,.18,4.08),(1.04,-.63,.22))): chain(a,b,links=12)
    tube('Entry impact seam',[(-.81,-.29,.27),(-.44,-.15,.34),(-.09,-.31,.34),(.28,-.07,.33),(.82,-.12,.28)],.035,'crack_gold')
    # One torn-off clock-hand chip close to the broken middle section.
    shard('Clock-hand floating torn chip',(2.30,.19,4.14),(.28,.23,.43),'gold')


def floating_numeral():
    root(2.27,1.81,.67,tiled=False)
    outline=[(-1.02,-.60),(-.72,-.79),(-.35,-.70),(-.12,-.86),(.23,-.74),(.74,-.70),
             (.90,-.31),(1.09,-.11),(.92,.15),(.95,.58),(.46,.77),(.20,.66),(-.12,.83),(-.57,.67),(-.99,.37),(-.86,.08),(-1.05,-.17)]
    poly('Jagged floating Roman numeral slab',outline,.03,.18,'marble',True,side='void_stone')
    # Decorative inset frame follows the broken slab instead of hiding it in a box.
    arc('Remembered dial gold outer arc',1.02,.09,.075,.12,2.91,'gold',loc=(0,-.13,.21),n=25)
    arc('Remembered dial dark inner arc',.88,.055,.065,3.40,5.90,'gold_dark',loc=(0,.12,.215),n=22)
    ns=len(PARTS)
    numeral('Large raised XII shaped blocks','XII',(0,0,.19),.94,.115,'gold',.13)
    transform_since(ns,rot=(0,0,math.pi))
    tube('Numeral slab branching fracture',[(-.94,-.20,.215),(-.60,-.24,.215),(-.40,-.07,.215),(-.20,-.22,.215),(.16,-.13,.215),(.44,-.31,.215),(.88,-.26,.215)],.029,'crack_gold')
    for x,y in ((-.69,.39),(.69,.35),(-.62,-.53),(.59,-.50)):
        cyl('Dial anchoring jewel',(x,y,.22),.065,.075,'crystal',n=6,bevel=True)
    for x,y,z in ((-.96,-.47,.17),(.90,.35,.13),(.20,.73,.17)):
        shard('Numeral mirrored edge splinter',(x,y,z),(.16,.16,.24),'mirror')
    for x,y in ((-.71,-.41),(.70,.44)):
        ring('Numeral inset rivet collar',.12,.045,.21,.07,'gold_dark',n=16,loc=(x,y,0),bevel=True)


SPECS=[
    ('FD_Saloon',saloon,3.0,5000),('FD_Rail',rail,3.0,5000),
    ('FD_StationClock',station_clock,3.0,5000),('FD_Gears',gears,3.0,5000),
    ('FD_MirrorFrame',mirror_frame,2.7,5000),('FD_DesertPillar',pillar,2.8,5000),
    ('FD_Bell',bell,2.8,5000),('FD_Island',island,3.0,5000),
    ('BA_DialRing',arena,None,11999),('BA_HandSpire',hand_spire,None,8000),
    ('BA_FloatingNumeral',floating_numeral,2.5,5000),
]


def tri_count(ob):
    ob.data.calc_loop_triangles()
    return len(ob.data.loop_triangles)


def volume_centroid(ob):
    ob.data.calc_loop_triangles(); total=0; centre=Vector((0,0,0))
    for tr in ob.data.loop_triangles:
        a,b,c=(ob.data.vertices[i].co for i in tr.vertices)
        vol=a.dot(b.cross(c))/6
        total+=vol; centre+=(a+b+c)*(vol/4)
    assert total>1e-8,(ob.name,total)
    return centre/total,total


def finish(name,target,budget):
    bpy.ops.object.select_all(action='DESELECT')
    for ob in PARTS:
        bpy.context.view_layer.objects.active=ob; ob.select_set(True)
        for mod in list(ob.modifiers): bpy.ops.object.modifier_apply(modifier=mod.name)
        ob.select_set(False)
    for ob in PARTS: ob.select_set(True)
    bpy.context.view_layer.objects.active=PARTS[0]
    bpy.ops.object.join()
    ob=bpy.context.object; ob.name=name; ob.data.name=name+'_Mesh'
    bm=bmesh.new(); bm.from_mesh(ob.data)
    bmesh.ops.recalc_face_normals(bm,faces=list(bm.faces)); bm.to_mesh(ob.data); bm.free()
    # Bake triangulation explicitly so the audit and FBX match.
    mod=ob.modifiers.new('Explicit game triangles','TRIANGULATE')
    bpy.ops.object.modifier_apply(modifier=mod.name)
    bpy.context.view_layer.update()
    if target:
        scale=target/max(ob.dimensions)
        ob.data.transform(Matrix.Scale(scale,4))
        centre,_=volume_centroid(ob)
        ob.data.transform(Matrix.Translation(-centre))
        ob['origin_rule']='volume-weighted centre of mass'
        ob['placement_height_m']='2.5-5.0'; ob['floating_detached_chunks']=3 if name=='FD_Island' else 2
    else:
        ob['origin_rule']='ground entry point' if name=='BA_HandSpire' else 'ground datum at disc centre'
    ob.data.update(); bpy.context.view_layer.update()
    n=tri_count(ob)
    # Only simplify a chamfer-heavy over-budget model; never add fake tessellation.
    if n>budget:
        mod=ob.modifiers.new('Preserve silhouette within game budget','DECIMATE')
        mod.ratio=(budget-100)/n
        mod.delimit={'MATERIAL','SHARP'}
        bpy.ops.object.modifier_apply(modifier=mod.name)
        n=tri_count(ob)
    # Clamped chamfers can leave near-coincident corners. Remove those before
    # delivering explicit triangles; FBX float quantisation must not flatten faces.
    bm=bmesh.new(); bm.from_mesh(ob.data)
    bmesh.ops.dissolve_degenerate(bm,dist=1e-5,edges=list(bm.edges))
    bmesh.ops.triangulate(bm,faces=list(bm.faces))
    bmesh.ops.recalc_face_normals(bm,faces=list(bm.faces))
    bm.to_mesh(ob.data); bm.free(); ob.data.update()
    n=tri_count(ob)
    if target:
        # Simplification can slightly alter the volume distribution of bevels.
        centre,_=volume_centroid(ob)
        ob.data.transform(Matrix.Translation(-centre)); ob.data.update()
    assert n<=budget,(name,n,budget)
    if target: assert n>=2500,(name,n,'insufficient readable detail')
    areas={}; visible={}
    for p in ob.data.polygons:
        p.use_smooth=False
        role=ob.data.materials[p.material_index].name
        areas[role]=areas.get(role,0)+p.area
        # Top + two side projections of the game camera, excludes hidden undersides.
        weight=max(0,p.normal.dot(Vector((0,.574,.819))))+max(0,p.normal.dot(Vector((.406,.406,.819))))
        visible[role]=visible.get(role,0)+p.area*weight
    assert len(areas)>=4,(name,areas)
    fraction=max(visible.values())/sum(visible.values())
    assert fraction<=.55,(name,fraction)
    bm=bmesh.new(); bm.from_mesh(ob.data)
    boundary=sum(e.is_boundary for e in bm.edges); nonmanifold=sum(not e.is_manifold for e in bm.edges)
    bm.free()
    assert boundary==0 and nonmanifold==0,(name,boundary,nonmanifold)
    assert all(math.isfinite(c) for v in ob.data.vertices for c in v.co)
    for m in ob.data.materials:
        assert m.name in PALETTE and tuple(m.diffuse_color)==(.5,.5,.5,1)
        assert tuple(m.node_tree.nodes['Principled BSDF'].inputs['Base Color'].default_value)==(.5,.5,.5,1)
    centre,volume=volume_centroid(ob)
    if target: assert centre.length<1e-5,(name,centre)
    ob['front']='+Y'; ob['chapter']=5; ob['triangles']=n
    if name=='BA_DialRing':
        ob['radius_m']=21.; ob['tilted_segment_indices']='2,6,9'; ob['tilt_degrees']='2.2,3.3,4.6'
        ob['intact_top_z_m']='0.0-0.08'; ob['gap_width_m']='0.40-0.94; open to void'
    path=OUT/f'Kit_{name}.fbx'
    pending=OUT/f'.Kit_{name}.debris-build.fbx'
    ob.update_tag(refresh={'OBJECT','DATA'})
    bpy.context.view_layer.update()
    bpy.ops.export_scene.fbx(filepath=str(pending),use_selection=True,axis_forward='-Z',axis_up='Y',
        apply_scale_options='FBX_SCALE_UNITS',bake_space_transform=True,object_types={'MESH'},
        use_mesh_modifiers=True,mesh_smooth_type='FACE',add_leaf_bones=False,bake_anim=False,
        use_custom_props=True)
    # Commit only the file just built, after checking its binary geometry payload.
    from io_scene_fbx import parse_fbx
    tree,_=parse_fbx.parse(str(pending))
    export_objects=next(c for c in tree.elems if c.id==b'Objects')
    geometry=[c for c in export_objects.elems if c.id==b'Geometry']
    assert len(geometry)==1,(name,'FBX mesh count')
    indices=next(c for c in geometry[0].elems if c.id==b'PolygonVertexIndex').props[0]
    assert len(indices)==3*n,(name,'FBX triangle payload',len(indices),3*n)
    FBX_PAYLOADS[path.name]=pending.read_bytes()
    pending.replace(path)
    assert path.is_file() and path.stat().st_size>1024
    dims=tuple(round(d,4) for d in ob.dimensions)
    RECORDS.append(dict(name=name,triangles=n,dimensions_m=dims,materials=sorted(areas),
        visible_material_fraction=round(fraction,4),visible_areas={r:round(a,4) for r,a in visible.items()},
        boundary_edges=boundary,nonmanifold_edges=nonmanifold,origin_rule=ob['origin_rule'],
        volume_centroid=[round(c,7) for c in centre],volume_m3=round(volume,4),
        fbx_sha256=hashlib.sha256(FBX_PAYLOADS[path.name]).hexdigest()))
    report(f'EXPORTED {name} {n} {dims}')
    report(f'AUDIT {name} roles={len(areas)} largest_visible_role={fraction:.1%} closed={nonmanifold==0}')
    return ob


def preview(objects):
    scene=bpy.context.scene
    scene.render.engine='BLENDER_WORKBENCH'
    scene.render.resolution_x=1800; scene.render.resolution_y=1000
    scene.render.resolution_percentage=100
    scene.render.image_settings.file_format='PNG'; scene.render.filepath=str(PREVIEW)
    scene.view_settings.view_transform='Standard'
    shade=scene.display.shading
    shade.light='FLAT'; shade.color_type='MATERIAL'
    shade.show_shadows=False; shade.show_cavity=True; shade.cavity_type='BOTH'
    shade.curvature_ridge_factor=1.5; shade.curvature_valley_factor=1.1
    shade.show_specular_highlight=False; shade.show_object_outline=True
    shade.background_type='WORLD'; scene.world.color=(.018,.022,.039)
    for r,m in MATS.items(): m.diffuse_color=(*PALETTE[r],1)
    # Camera-space rectangular grid; front stays +Y on every model.
    camera=bpy.data.objects.new('Preview camera',bpy.data.cameras.new('Preview camera'))
    scene.collection.objects.link(camera); scene.camera=camera
    near=Vector((.24,.971,0)).normalized(); el=math.radians(55)
    camera.location=near*35*math.cos(el)+Vector((0,0,35*math.sin(el)))
    camera.rotation_euler=(-camera.location).to_track_quat('-Z','Y').to_euler()
    camera.data.type='ORTHO'; camera.data.ortho_scale=22.1
    rotation=camera.rotation_euler.to_matrix(); inverse=rotation.transposed()
    helpers=[camera]
    grey=bpy.data.materials.new('Preview scale grey'); grey.diffuse_color=(.43,.46,.50,1)
    white=bpy.data.materials.new('Preview typography'); white.diffuse_color=(.80,.85,.95,1)
    def caption(body,x,y,size=.16):
        d=bpy.data.curves.new('Review label','FONT'); d.body=body
        d.size=size; d.align_x='CENTER'; d.align_y='TOP'; d.materials.append(white)
        ob=bpy.data.objects.new('Review label',d); scene.collection.objects.link(ob)
        ob.location=rotation @ Vector((x,y,1))
        ob.rotation_euler=camera.rotation_euler; helpers.append(ob)
    for i,ob in enumerate(objects):
        col=i%4; row=i//4
        cx=-8.2+col*5.45; cy=3.62-row*3.68
        scale=.075 if ob.name=='BA_DialRing' else (.37 if ob.name=='BA_HandSpire' else 1)
        ob.scale=(scale,)*3
        bpy.context.view_layer.update()
        projected=[inverse @ (ob.matrix_world @ v.co) for v in ob.data.vertices]
        lo=Vector(tuple(min(p[j] for p in projected) for j in range(3)))
        hi=Vector(tuple(max(p[j] for p in projected) for j in range(3)))
        # Align all silhouette centres inside their row; captions do not overlap roots.
        ob.location += rotation @ Vector((cx-(lo.x+hi.x)/2-.20,cy-(lo.y+hi.y)/2,0))
        bottom=cy-(hi.y-lo.y)/2
        suffix='42 m diameter / display 0.075x' if i==8 else ('9 m hand / display 0.37x' if i==9 else 'metres / 1:1')
        caption(ob.name,cx,cy-1.38,.18)
        caption(suffix,cx,cy-1.62,.13)
        # A true 2.2m capsule, with the SAME display scale as the local asset.
        # Unlike a sphere-on-cylinder stack, the poles use hemispherical profiles.
        radius=.22; h=2.2; profiles=[]
        for j in range(7):
            a=-math.pi/2+(math.pi/2)*j/6
            profiles.append((max(.001,radius*math.cos(a)),radius+radius*math.sin(a)))
        for j in range(7):
            a=(math.pi/2)*j/6
            profiles.append((max(.001,radius*math.cos(a)),h-radius+radius*math.sin(a)))
        cap=lathe('Preview-only 2.2m capsule',profiles,'void_stone',n=16)
        # lathe() closes through its axis: hemispherical caps have no open borders.
        cap.data.materials.clear(); cap.data.materials.append(grey)
        for p in cap.data.polygons: p.use_smooth=True
        cap.scale=(scale,)*3
        cap.location=rotation @ Vector((cx+1.88,cy-.93,0))
        helpers.append(cap)
    caption('CHAPTER 05  /  DRIFTING MEMORIES',0,5.80,.29)
    caption('BROKEN ROOTS  +  GOLD FRACTURES  +  CENTRED FLOAT PIVOTS',0,5.40,.13)
    caption('Grey capsule = 2.2 m\nRole colours are preview only',8.10,-3.40,.18)
    caption('11 meshes / individual FBX\nDial: 12 open fractured leaves',8.10,-4.10,.15)
    try:
        bpy.ops.render.render(write_still=True)
        assert PREVIEW.is_file() and PREVIEW.stat().st_size>1024
        report(f'PREVIEW {PREVIEW.relative_to(ROOT)}')
    finally:
        scene.camera=None
        for helper in helpers: bpy.data.objects.remove(helper,do_unlink=True)
        for ob in objects: ob.location=(0,0,0); ob.scale=(1,1,1)
        for m in MATS.values(): m.diffuse_color=(.5,.5,.5,1)


def main():
    global PARTS
    bpy.ops.wm.read_factory_settings(use_empty=True)
    for m in list(bpy.data.materials): bpy.data.materials.remove(m)
    scene=bpy.context.scene
    scene.unit_settings.system='METRIC'; scene.unit_settings.scale_length=1
    if scene.world is None: scene.world=bpy.data.worlds.new('Review backdrop')
    materials(); OUT.mkdir(parents=True,exist_ok=True)
    objects=[]
    for i,(name,build,target,budget) in enumerate(SPECS):
        PARTS=[]; random.seed(55031+i*7919)
        build(); objects.append(finish(name,target,budget))
    assert len(scene.objects)==11 and all(ob.type=='MESH' for ob in scene.objects)
    preview(objects)
    assert len(scene.objects)==11 and all(ob.type=='MESH' for ob in scene.objects)
    AUDIT.write_text(json.dumps(RECORDS,indent=2),encoding='utf-8')
    # Preserve the reviewed bytes even if another asset task watches the FBX folder.
    with zipfile.ZipFile(ARCHIVE,'w',zipfile.ZIP_DEFLATED) as archive:
        for filename,payload in FBX_PAYLOADS.items(): archive.writestr(filename,payload)
        archive.writestr(AUDIT.name,json.dumps(RECORDS,indent=2))
    for filename,payload in FBX_PAYLOADS.items():
        path=OUT/filename
        if path.read_bytes()!=payload:
            pending=OUT/('.'+filename+'.restore')
            pending.write_bytes(payload); pending.replace(path)
            report(f'RESTORED validated export {filename}')
    report('DONE')
    LOG.write_text('\n'.join(EVENTS)+'\n',encoding='utf-8')


if __name__=='__main__': main()
