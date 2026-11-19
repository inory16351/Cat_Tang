# 챕터 4 오르골 상인: 도자기 인형, 태엽 장식과 상인 소품을 생성합니다.
# 실행: "C:/Program Files/Blender Foundation/Blender 5.3/blender.exe" -b --factory-startup -P Art/tools/blender_kit_ch4_merchant.py
"""One deterministic, metre-scale hero mesh. +Y front, bottom-centre origin.

No external assets or add-ons. The exported role shaders remain neutral grey;
only the temporary Workbench review uses the warm concept palette.
"""
import math
import random
import tempfile
from pathlib import Path

import bmesh
import bpy
import numpy as np
from mathutils import Matrix, Vector

ROOT = Path(__file__).resolve().parents[2]
DEST = ROOT / 'Game/Assets/Art/Characters/Ch4/Kit_Ch4_Merchant.fbx'
PREVIEW = ROOT / 'Art/tools/specs/blender_kit_ch4_merchant_preview.png'
TAU = math.tau
PARTS, MATS = [], {}
PALETTE = {
    'porcelain': (.90, .79, .58), 'dial_face': (.75, .63, .43),
    'gold': (.94, .55, .13), 'brass': (.63, .35, .085),
    'velvet': (.25, .026, .060), 'red_dark': (.19, .025, .040),
    'wood_dark': (.23, .125, .075), 'dial_mark': (.15, .095, .065),
    'glass': (.57, .76, .77), 'lamp_warm': (1.0, .43, .055),
    'copper': (.52, .25, .10), 'silver': (1.0, .95, .79),
}


def mesh(name, verts, faces, role, smooth=False, bevel=0):
    data = bpy.data.meshes.new(name)
    data.from_pydata(verts, [], faces)
    data.update()
    obj = bpy.data.objects.new(name, data)
    bpy.context.collection.objects.link(obj)
    data.materials.append(MATS[role])
    for p in data.polygons:
        p.use_smooth = smooth
    if bevel:
        mod = obj.modifiers.new('Soft handmade hard edge', 'BEVEL')
        mod.width, mod.segments = bevel, 2
        mod.limit_method, mod.angle_limit = 'ANGLE', math.radians(40)
        mod.use_clamp_overlap = True
    PARTS.append(obj)
    return obj


def ellipsoid(name, c, scale, role, n=20, rings=12):
    v = [(c[0], c[1], c[2] - scale[2])]
    for k in range(1, rings):
        a = -math.pi / 2 + math.pi * k / rings
        for j in range(n):
            t = TAU * j / n
            v.append((c[0]+scale[0]*math.cos(a)*math.cos(t),
                      c[1]+scale[1]*math.cos(a)*math.sin(t), c[2]+scale[2]*math.sin(a)))
    v.append((c[0], c[1], c[2]+scale[2]))
    f = [(0, 1+(j+1)%n, 1+j) for j in range(n)]
    for k in range(rings-2):
        f += [(1+k*n+j, 1+k*n+(j+1)%n, 1+(k+1)*n+(j+1)%n, 1+(k+1)*n+j) for j in range(n)]
    f += [(len(v)-1, 1+(rings-2)*n+j, 1+(rings-2)*n+(j+1)%n) for j in range(n)]
    return mesh(name, v, f, role, True)


def tube(name, points, radius, role, n=8, closed=False):
    points = [Vector(p) for p in points]
    v, f = [], []
    count = len(points)
    for i, p in enumerate(points):
        prev = points[(i-1)%count] if closed else points[max(0, i-1)]
        nxt = points[(i+1)%count] if closed else points[min(count-1, i+1)]
        tangent = (nxt-prev).normalized()
        ref = Vector((0,1,0)) if abs(tangent.y)<.92 else Vector((1,0,0))
        u = tangent.cross(ref).normalized(); w = tangent.cross(u)
        r = radius[i] if isinstance(radius, list) else radius
        for j in range(n):
            v.append(tuple(p+r*(math.cos(TAU*j/n)*u+math.sin(TAU*j/n)*w)))
    for i in range(count if closed else count-1):
        f += [(i*n+j, i*n+(j+1)%n, ((i+1)%count)*n+(j+1)%n, ((i+1)%count)*n+j) for j in range(n)]
    if not closed:
        f += [tuple(reversed(range(n))), tuple(range((count-1)*n,count*n))]
    return mesh(name, v, f, role, True)


def bar(name, a, b, r, role, n=12):
    return tube(name, [a,b], r, role, n)


def disk(name, c, radius, depth, role, n=24, axis=(0,1,0), bevel=0):
    axis = Vector(axis).normalized()
    u = axis.cross(Vector((0,0,1)))
    if u.length < .1: u = axis.cross(Vector((0,1,0)))
    u.normalize(); w = axis.cross(u)
    v = [tuple(Vector(c)+axis*z+radius*(u*math.cos(TAU*j/n)+w*math.sin(TAU*j/n)))
         for z in (-depth/2,depth/2) for j in range(n)]
    f = [tuple(reversed(range(n))), tuple(range(n,2*n))]
    f += [(j,(j+1)%n,n+(j+1)%n,n+j) for j in range(n)]
    return mesh(name,v,f,role,True,bevel)


def ring(name,c,rx,rz,r,role,n=40):
    return tube(name,[(c[0]+rx*math.cos(TAU*j/n),c[1],c[2]+rz*math.sin(TAU*j/n)) for j in range(n)],r,role,8,True)


def plate(name, points, depth, role, bevel=.02):
    # A relief in the XZ plane. Every ornament has a closed, solid back.
    count=len(points)
    v=[(x,y+d,z) for d in (-depth/2,depth/2) for x,y,z in points]
    f=[tuple(reversed(range(count))),tuple(range(count,2*count))]
    f += [(j,(j+1)%count,(j+1)%count+count,j+count) for j in range(count)]
    return mesh(name,v,f,role,False,bevel)


def diamond(name,x,y,z,w,h,role,depth=.05):
    return plate(name,[(x,y,z+h),(x+w,y,z),(x,y,z-h),(x-w,y,z)],depth,role)


def spindle(name,a,b,r,role):
    # Tapered doll segments, with a fuller middle and genuinely separate joints.
    a,b=Vector(a),Vector(b)
    obj=ellipsoid(name,(0,0,0),(r,r,(b-a).length/2),role,16,10)
    obj.data.transform(Matrix.Translation((a+b)/2) @ (b-a).to_track_quat('Z','Y').to_matrix().to_4x4())
    return obj


def scroll(name,c,rx,rz,side,role,r=.022,turns=1.15):
    pts=[]
    for j in range(37):
        t=j/36; a=-math.pi*.65+TAU*turns*t
        q=1-.87*t
        pts.append((c[0]+side*rx*q*math.cos(a),c[1]+.012*math.sin(t*math.pi),c[2]+rz*q*math.sin(a)))
    return tube(name,pts,[r*(1-.35*j/36) for j in range(37)],role,8)


def gear(name,x,y,z,r,teeth,role,phase=0):
    # Solid cog with deep teeth, an inset hub, and a warm dark axle.
    pts=[]
    for j in range(teeth):
        for da,rr in ((-.45,.78),(-.26,1),(.26,1),(.45,.78)):
            a=TAU*(j+da)/teeth+phase
            pts.append((x+r*rr*math.cos(a),y,z+r*rr*math.sin(a)))
    # Explicit four-level chamfer avoids the redundant internal bevel triangles
    # that a modifier creates at each small tooth. Tooth outlines remain intact.
    n=len(pts)
    v=[(x+(px-x)*scale,y+dy,z+(pz-z)*scale)
       for dy,scale in ((-.019,.94),(-.012,1.),(.012,1.),(.019,.94)) for px,_,pz in pts]
    f=[tuple(reversed(range(n))),tuple(range(3*n,4*n))]
    for k in range(3):
        f += [(k*n+j,k*n+(j+1)%n,(k+1)*n+(j+1)%n,(k+1)*n+j) for j in range(n)]
    mesh(name,v,f,role)
    disk(name+' recessed hub',(x,y+.025,z),r*.34,.025,'wood_dark',16)
    disk(name+' axle',(x,y+.042,z),r*.19,.025,'copper',12)
    if r>.09:
        for j in range(4):
            a=TAU*j/4+phase
            bar(name+' spoke',(x,y+.025,z),(x+r*.66*math.cos(a),y+.025,z+r*.66*math.sin(a)),.012,'brass',6)


def costume_surface(name, rows, role, start=0, end=TAU, segments=72, pleats=0, thickness=.018):
    # Flared, thick, closed cloth shell; angle zero is the +Y front.
    v=[]
    wrap=abs(end-start-TAU)<1e-6
    stride=segments if wrap else segments+1
    for inset in (0,thickness):
        for rx,ry,z in rows:
            for j in range(stride):
                a=start+(end-start)*j/segments
                wave=1+pleats*math.cos(a*20)
                v.append(((rx*wave-inset)*math.sin(a),(ry*wave-inset)*math.cos(a),z))
    count=stride*len(rows); f=[]
    for layer in (0,1):
        for k in range(len(rows)-1):
            for j in range(segments):
                q=layer*count+k*stride+j
                nxt=layer*count+k*stride+(j+1)%stride
                f.append((q,nxt,nxt+stride,q+stride))
    for k in (0,len(rows)-1):
        for j in range(segments):
            q=k*stride+j; nxt=k*stride+(j+1)%stride
            f.append((q,nxt,count+nxt,count+q))
    if not wrap:
        for j in (0,segments):
            for k in range(len(rows)-1):
                q=k*stride+j; f.append((q,q+stride,count+q+stride,count+q))
    return mesh(name,v,f,role,True)


def make_skirt_and_cape():
    costume_surface('Twenty sculpted ivory skirt pleats',[(.25,.20,1.105),(.40,.29,.93),(.58,.37,.68),(.60,.38,.61)],'porcelain',pleats=.045)
    costume_surface('Skirt ivory hem piping',[(.58,.37,.685),(.60,.38,.64)],'dial_face',pleats=.045)
    costume_surface('Wine underskirt peeking below pleats',[(.57,.355,.625),(.585,.365,.59)],'red_dark',pleats=.035)
    costume_surface('Open velvet overskirt',[(.26,.22,1.11),(.43,.32,.91),(.57,.385,.72)],'velvet',.57,TAU-.57,60,.025)
    costume_surface('Overskirt gold hem band',[(.548,.373,.755),(.57,.385,.72)],'gold',.57,TAU-.57,60,.025)
    for s in (-1,1):
        tube('Gold front opening embroidery',[(s*.144,.197,1.10),(s*.233,.273,.91),(s*.307,.325,.74)],.018,'gold')
    # Long cape drapes across the back and both hips, leaving the front open.
    costume_surface('Full velvet back cape',[(.32,.215,1.54),(.46,.265,1.40),(.44,.30,1.11),(.63,.39,.80),(.73,.44,.63)],'velvet',.90,TAU-.90,60,.018)
    costume_surface('Heavy gold cape hem',[(.712,.43,.665),(.73,.44,.63)],'gold',.90,TAU-.90,60)
    for j in range(19):
        a=.94+(TAU-1.88)*j/18
        x,y=.735*math.sin(a),.447*math.cos(a)
        spindle('Short cape fringe tassel',(x,y,.635),(x*1.025,y*1.025,.54),.025,'gold')
    # Broad shoulder mantle with a wavy/scalloped gold edge.
    costume_surface('Short scalloped shoulder mantle',[(.22,.19,1.615),(.39,.27,1.56),(.52,.30,1.43)],'velvet',.28,TAU-.28,64)
    edge=[]
    for j in range(97):
        a=.28+(TAU-.56)*j/96
        edge.append((.52*math.sin(a),.30*math.cos(a),1.435+.018*math.cos(a*10)))
    tube('Scalloped gold shoulder edging',edge,.025,'gold')
    for s in (-1,1):
        scroll('Shoulder gold embroidered volute',(s*.35,.252,1.495),.10,.063,s,'gold',.013)
        # Front segmented ivory lining beneath the gold shoulder edge.
        for j in range(4):
            x=s*(.26+j*.065)
            ellipsoid('Scalloped porcelain mantle lining',(x,.17,1.415),(.055,.092,.055),'porcelain',12,6)
    # Back ornament: two scrolls and a small central brass music medallion.
    for s in (-1,1):
        scroll('Back cape embroidery',(s*.21,-.337,1.10),.145,.19,s,'gold',.016)
        tube('Back cape seam',[(s*.12,-.25,1.42),(s*.20,-.33,1.04),(s*.31,-.412,.69)],.011,'red_dark',6)
    disk('Back mantle medallion',(0,-.29,1.47),.072,.035,'gold',20)
    diamond('Back medallion inset',0,-.315,1.47,.026,.042,'red_dark',.025)


def make_legs_and_arms():
    for s in (-1,1):
        x=s*.255
        ellipsoid('Hip porcelain joint',(x,0,.70),(.09,.085,.09),'porcelain',16,8)
        spindle('Short porcelain thigh',(x,0,.69),(x+s*.018,.015,.53),.079,'porcelain')
        ellipsoid('Golden knee ball',(x+s*.018,.045,.53),(.075,.078,.075),'gold',16,8)
        disk('Knee inset medallion',(x+s*.018,.118,.53),.051,.021,'lamp_warm',16)
        spindle('Tapered porcelain shin',(x+s*.02,0,.485),(x+s*.05,.025,.215),.079,'porcelain')
        tube('Shin inset seam',[(x+s*.025,.077,.46),(x+s*.04,.079,.35),(x+s*.05,.062,.23)],.009,'dial_face',6)
        # Boots are flat-bottomed, with a rounded toe pointing along +Y.
        bx=x+s*.05
        outline=[(-.105,-.095),(-.11,.10),(-.086,.205),(.086,.205),(.11,.10),(.105,-.095)]
        v=[(bx+xx,yy,z) for z in (0,.055) for xx,yy in outline]
        f=[tuple(reversed(range(6))),tuple(range(6,12))]+[(j,(j+1)%6,(j+1)%6+6,j+6) for j in range(6)]
        mesh('Flat gold boot sole',v,f,'gold',False,.02)
        ellipsoid('Wine ankle boot rounded upper',(bx,.055,.102),(.105,.158,.083),'velvet',16,8)
        spindle('Wine ankle boot shaft',(bx,-.015,.09),(bx,-.012,.225),.086,'velvet')
        disk('Gold boot cuff',(bx,0,.215),.099,.045,'gold',20,axis=(0,0,1),bevel=.02)
        diamond('Ivory boot vamp',bx,.199,.115,.072,.064,'porcelain',.028)
        diamond('Dark wood toe diamond',bx,.221,.112,.027,.034,'wood_dark',.022)
        for ss in (-1,1):
            plate('Boot wine ribbon',[(bx,.13,.19),(bx+ss*.10,.12,.16),(bx+ss*.09,.13,.125),(bx,.15,.17)],.032,'velvet',.012)
        shoulder=(s*.385,.0,1.465); elbow=(s*.57,.015,1.285); wrist=(s*.72,.035,1.115)
        for nm,p,r in (('Shoulder',shoulder,.086),('Elbow',elbow,.067),('Wrist',wrist,.057)):
            ellipsoid(nm+' gold ball joint',p,(r,r,r),'gold',16,8)
            disk(nm+' ivory socket',(p[0],p[1]+r*.88,p[2]),r*.60,.02,'dial_face',12)
        spindle('Porcelain upper arm',Vector(shoulder).lerp(Vector(elbow),.17),Vector(shoulder).lerp(Vector(elbow),.79),.072,'porcelain')
        spindle('Porcelain forearm',Vector(elbow).lerp(Vector(wrist),.18),Vector(elbow).lerp(Vector(wrist),.85),.077,'porcelain')
        axis=(Vector(wrist)-Vector(elbow)).normalized()
        disk('Flared gold wrist cuff',Vector(wrist)-axis*.035,.095,.055,'gold',20,axis,bevel=.02)
        hand=Vector(wrist)+axis*.11
        ellipsoid('Clenched porcelain fist',hand,(.088,.075,.098),'porcelain',20,10)
        ellipsoid('Folded porcelain thumb',hand+Vector((-s*.055,.047,.019)),(.043,.044,.056),'porcelain',12,8)
        for k in range(2):
            tube('Recessed finger crease',[(hand.x+s*(.005+k*.023),hand.y+.070,hand.z-.024),(hand.x+s*(.017+k*.023),hand.y+.069,hand.z-.045)],.008,'dial_face',6)


def make_chest():
    ellipsoid('Porcelain torso casing',(0,-.035,1.355),(.255,.20,.275),'porcelain',24,14)
    # The opaque back of the hemispherical crystal carries the visible mechanism.
    ellipsoid('Pale blue glass rear chamber',(0,.176,1.355),(.222,.05,.238),'glass',24,12)
    ring('Chest inner brass gasket',(0,.228,1.355),.216,.238,.022,'brass')
    gear('Lower twelve tooth drive gear',.005,.247,1.272,.130,12,'gold',.08)
    gear('Upper right eight tooth gear',-.104,.249,1.434,.096,8,'brass',.13)
    gear('Upper left nine tooth gear',.104,.247,1.457,.100,9,'gold',.03)
    # Crystal hemisphere: a thick visible perimeter and a clear central lens.
    verts=[]; faces=[]; n=48
    for k in range(5):
        a=.08+k*.16
        for j in range(n):
            t=TAU*j/n
            verts.append((.222*math.cos(a)*math.cos(t),.227+.115*math.sin(a),1.355+.238*math.cos(a)*math.sin(t)))
    for k in range(4):
        for j in range(n): faces.append((k*n+j,k*n+(j+1)%n,(k+1)*n+(j+1)%n,(k+1)*n+j))
    dome=mesh('Curved glass dome perimeter',verts,faces,'glass',True)
    mod=dome.modifiers.new('Crystal shell thickness','SOLIDIFY'); mod.thickness=.008
    verts=[]; faces=[]; n=32
    for k in range(7):
        a=.72+(math.pi/2-.72)*k/7
        for j in range(n):
            t=TAU*j/n
            verts.append((.222*math.cos(a)*math.cos(t),.227+.115*math.sin(a),1.355+.238*math.cos(a)*math.sin(t)))
    for k in range(6):
        for j in range(n): faces.append((k*n+j,k*n+(j+1)%n,(k+1)*n+(j+1)%n,(k+1)*n+j))
    verts.append((0,.342,1.355))
    faces += [(6*n+j,6*n+(j+1)%n,len(verts)-1) for j in range(n)]
    lens=mesh('Optically clear central glass lens',verts,faces,'glass',True)
    group=lens.vertex_groups.new(name='Clear lens - review transparency')
    group.add(list(range(len(verts))),1.0,'REPLACE')
    # Heart-shaped front bezel: cusp at neckline, pointed tip above belt.
    pts=[]
    for j in range(81):
        t=TAU*j/80
        x=16*math.sin(t)**3/17
        z=(13*math.cos(t)-5*math.cos(2*t)-2*math.cos(3*t)-math.cos(4*t))/17
        pts.append((x*.242,.307,1.377+z*.257))
    tube('Continuous gold heart shaped chest bezel',pts,.025,'gold',8)
    tube('Glass curved reflected highlight',[(.145,.303,1.50),(.170,.315,1.47),(.180,.322,1.43)],.016,'silver',8)
    ellipsoid('Glass lower reflection',(.182,.294,1.36),(.016,.012,.022),'silver',12,6)
    # Collar and wide wine bow.
    spindle('Thin porcelain neck',(0,0,1.56),(0,0,1.765),.072,'porcelain')
    for j in range(10):
        a=TAU*j/10
        obj=ellipsoid('Ivory frilled collar petal',(.105*math.sin(a),.095*math.cos(a),1.646),(.04,.046,.067),'porcelain',12,6)
    for s in (-1,1):
        plate('Sculpted velvet bow loop',[(s*.02,.28,1.645),(s*.166,.258,1.69),(s*.182,.264,1.675),(s*.176,.27,1.58),(s*.03,.29,1.614)],.055,'velvet')
        plate('Velvet fishtail bow ribbon',[(s*.03,.29,1.624),(s*.103,.29,1.61),(s*.151,.28,1.48),(s*.091,.29,1.505),(s*.061,.29,1.477)],.035,'velvet',.012)
        tube('Bow folded ridge',[(s*.04,.317,1.635),(s*.10,.305,1.643),(s*.157,.298,1.663)],.011,'red_dark',6)
    ellipsoid('Copper amber jewel setting',(0,.325,1.626),(.061,.031,.074),'copper',20,10)
    ring('Gold amber jewel rim',(0,.35,1.626),.048,.061,.014,'gold',24)
    ellipsoid('Warm amber cabochon',(0,.36,1.626),(.040,.026,.053),'lamp_warm',20,10)
    ellipsoid('Amber cabochon reflection',(.011,.383,1.647),(.010,.006,.016),'silver',12,6)


def make_belt_and_goods():
    costume_surface('Dark timber waist belt',[(.287,.242,1.16),(.293,.247,1.075)],'wood_dark',segments=48)
    for s in (-1,1):
        plate('Golden belt keeper',[(s*.18,.246,1.17),(s*.225,.231,1.16),(s*.224,.236,1.07),(s*.18,.252,1.075)],.035,'gold')
        scroll('Buckle gold acanthus scroll',(s*.133,.292,1.188),.10,.057,s,'gold',.022,.9)
    diamond('Ornate diamond gold buckle',0,.304,1.133,.091,.121,'gold',.065)
    diamond('Dark inset buckle diamond',0,.346,1.135,.049,.073,'wood_dark',.025)
    diamond('Ivory buckle centre',0,.366,1.135,.027,.043,'dial_face',.025)
    # Pouch on the concept's left (world +X), flattened naturally against hip.
    ellipsoid('Leather dark wood coin pouch',(.345,.333,.966),(.154,.097,.171),'wood_dark',24,12)
    tube('Pouch gold stitched border',[(.225,.388,.89),(.237,.406,.844),(.345,.431,.821),(.452,.40,.85),(.473,.365,.942)],.012,'copper',6)
    for j in range(5):
        x=.255+j*.044
        plate('Gathered pouch mouth',[(x-.027,.33,1.096),(x-.037,.332,1.18),(x+.023,.337,1.17),(x+.024,.341,1.097)],.055,'wood_dark',.012)
    bar('Gold pouch neck cord',(.246,.390,1.094),(.439,.390,1.094),.015,'gold',8)
    ellipsoid('Pouch cord knot',(.341,.401,1.092),(.024,.020,.024),'gold',12,6)
    for s in (-1,1):
        tube('Tied pouch cord',[(.342,.408,1.09),(.342+s*.065,.417,1.043),(.342+s*.032,.415,1.035),(.342,.408,1.09)],.013,'gold',6)
    for x,y,z in ((.30,.427,.934),(.365,.429,.958),(.365,.434,.985)):
        disk('Visible gold coin',(x,y,z),.043,.022,'gold',20,axis=(0,.5,1),bevel=.008)
    # Portable balance hanging from opposite hip, not an ambiguous lantern.
    x,y,z=-.359,.427,1.077
    ring('Balance hanging loop',(x,y,z+.056),.037,.043,.014,'gold',24)
    bar('Scale upright',(x,y,z-.20),(x,y,z+.015),.019,'gold',10)
    ellipsoid('Scale central foot',(x,y,z-.21),(.047,.027,.018),'gold',12,6)
    bar('Balance left beam',(x-.125,y,z-.020),(x,y,z+.008),.018,'gold',10)
    bar('Balance right beam',(x,y,z+.008),(x+.125,y,z-.020),.018,'gold',10)
    disk('Balance beam fulcrum',(x,y+.018,z+.006),.035,.025,'gold',16)
    disk('Balance fulcrum inset',(x,y+.035,z+.006),.013,.015,'wood_dark',12)
    for s in (-1,1):
        px=x+s*.125
        ellipsoid('Balance beam knob',(px,y,z-.021),(.025,.024,.025),'gold',12,6)
        for dx,dy in ((-.052,0),(.052,0),(0,-.036)):
            bar('Balance suspended chain',(px,y,z-.033),(px+dx,y+dy,z-.155),.010,'gold',6)
        # Open concave gold bowl, profile revolved around Z.
        v=[]; f=[]; profile=[(.002,-.201),(.035,-.192),(.065,-.167),(.065,-.152),(.053,-.157),(.027,-.178),(.002,-.182)]
        n=20
        for r,dz in profile:
            for j in range(n): v.append((px+r*math.cos(TAU*j/n),y+r*math.sin(TAU*j/n),z+dz))
        for k in range(len(profile)):
            for j in range(n): f.append((k*n+j,k*n+(j+1)%n,((k+1)%len(profile))*n+(j+1)%n,((k+1)%len(profile))*n+j))
        mesh('Concave hanging balance pan',v,f,'gold',True)


def make_head():
    head=ellipsoid('Large round porcelain doll head',(0,0,1.999),(.351,.277,.365),'porcelain',48,28)
    ellipsoid('Porcelain back hair cap',(0,-.126,2.059),(.373,.23,.325),'dial_face',28,16)
    # Face marks follow the exact ellipsoid surface, sunk halfway into it.
    def surface(x,z,offset=0):
        q=max(.001,1-(x/.351)**2-((z-1.999)/.365)**2)
        return .277*math.sqrt(q)+offset
    def face_line(name,xz,r,role,offset=.001):
        return tube(name,[(x,surface(x,z,offset),z) for x,z in xz],r,role,6)
    for s in (-1,1):
        pts=[]
        for j in range(19):
            t=j/18
            pts.append((s*(.073+.182*t),1.944+.038*math.sin(math.pi*t)-.003*t))
        face_line('Closed smiling gold eyelid',pts,.016,'gold')
        for k in range(2):
            x=s*(.221+k*.022); z=1.962-k*.008
            face_line('Gold outer eyelash',[(x,z),(x+s*.024,z+.021)],.011,'gold')
    smile=[(.052*math.cos(math.pi+math.pi*j/20),1.885+.033*math.sin(math.pi+math.pi*j/20)) for j in range(21)]
    face_line('Small peaceful smile',smile,.010,'dial_mark',-.012)
    # Deliberate old porcelain fractures, thin and irregular, with forked ends.
    cutters=[face_line('Temporary recessed smile cutter',smile,.014,'dial_mark',-.001)]
    for pts in [
        [(-.025,2.315),(-.052,2.243),(-.018,2.203),(-.032,2.16),(.032,2.124),(.047,2.08),(.085,2.062)],
        [(-.018,2.203),(-.106,2.215),(-.15,2.263)],
        [(.047,2.08),(.106,2.089),(.139,2.151)],
        [(-.244,1.846),(-.195,1.863),(-.174,1.91)],
        [(-.205,1.878),(-.219,1.919)],
        [(.135,1.674),(.153,1.746),(.204,1.795),(.186,1.84),(.218,1.882)],
        [(.204,1.795),(.263,1.793),(.278,1.826)],
    ]:
        cutters.append(face_line('Temporary porcelain trench cutter',pts,.011,'dial_mark',-.001))
        face_line('Inset time worn porcelain crack',pts,.0065,'dial_mark',-.008)
    # Boolean-carved trenches, with dark inlay beneath the original face surface.
    # Cutters are construction tools and never enter the joined/exported mesh.
    cv,cf=[],[]
    for cutter in cutters:
        offset=len(cv)
        cv.extend(tuple(v.co) for v in cutter.data.vertices)
        cf.extend(tuple(i+offset for i in p.vertices) for p in cutter.data.polygons)
        PARTS.remove(cutter); bpy.data.objects.remove(cutter,do_unlink=True)
    cutter=mesh('Combined temporary crack cutter',cv,cf,'dial_mark')
    bm=bmesh.new(); bm.from_mesh(cutter.data)
    bmesh.ops.recalc_face_normals(bm,faces=list(bm.faces)); bm.to_mesh(cutter.data); bm.free()
    head.data.materials.append(MATS['dial_mark'])
    mod=head.modifiers.new('Actually recessed porcelain fractures','BOOLEAN')
    mod.operation='DIFFERENCE'; mod.solver='EXACT'; mod.object=cutter; mod.use_self=True
    bpy.context.view_layer.objects.active=head
    bpy.ops.object.modifier_apply(modifier=mod.name)
    PARTS.remove(cutter); bpy.data.objects.remove(cutter,do_unlink=True)
    # Broad swept locks travel from the parting to outward rolled volutes.
    for s in (-1,1):
        pts=[]
        for j in range(24):
            t=j/23
            pts.append((s*(.034+.35*t),.11+.061*math.sin(math.pi*t),2.291-.19*t-.027*math.sin(math.pi*t)))
        tube('Swept thick porcelain fringe',pts,[.064+.015*math.sin(math.pi*j/23) for j in range(24)],'porcelain',10)
        # Spiral is a substantial 3D roll, not a line drawn on a sphere.
        pts=[]
        for j in range(53):
            t=j/52; a=-2.4+TAU*1.18*t; r=.143*(1-.84*t)
            pts.append((s*(.356+r*math.cos(a)),.155+.025*math.sin(t*math.pi),2.103+r*math.sin(a)))
        tube('Large temple spiral hair roll',pts,[.064-.038*j/52 for j in range(53)],'porcelain',10)
        pts=[]
        for j in range(43):
            t=j/42; a=1.75+TAU*1.12*t; r=.109*(1-.84*t)
            pts.append((s*(.343+r*math.cos(a)),.111+.025*math.sin(t*math.pi),1.803+r*math.sin(a)))
        tube('Lower porcelain corkscrew hair roll',pts,[.056-.031*j/42 for j in range(43)],'porcelain',10)
        spindle('Side hair connecting roll',(s*.35,-.075,2.03),(s*.365,-.04,1.83),.074,'dial_face')
    # A fully modelled coiffure behind the head, arranged in sculpted waves.
    for i in range(5):
        x=(i-2)*.12
        pts=[]
        for j in range(27):
            t=j/26
            pts.append((x+.025*math.sin(t*TAU),-.267-.05*math.sin(math.pi*t),2.29-.47*t))
        tube('Back swept porcelain hair wave',pts,.054,'porcelain',8)
    for s in (-1,1):
        scroll('Back nape hair spiral',(s*.175,-.26,1.839),.10,.10,s,'porcelain',.043,1.1)


def make_crown_and_key():
    # Horizontal music-box cylinder, with mechanical pins around its full barrel.
    z=2.397
    disk('Porcelain music pin cylinder',(0,-.012,z),.139,.51,'dial_face',32,axis=(1,0,0),bevel=.02)
    for s in (-1,1):
        disk('Wine cylinder end cap',(s*.252,-.012,z),.145,.041,'red_dark',28,axis=(1,0,0),bevel=.02)
        disk('Heavy gold cylinder rim',(s*.279,-.012,z),.145,.022,'gold',28,axis=(1,0,0),bevel=.01)
        disk('Cylinder end bearing',(s*.293,-.012,z),.082,.025,'brass',20,axis=(1,0,0),bevel=.01)
    for j in range(7):
        x=-.205+j*.068
        for k in range(9):
            a=TAU*k/9+(j%3)*.19
            normal=Vector((0,math.cos(a),math.sin(a)))
            p=Vector((x,-.012,z))+normal*.141
            disk('Raised dark wood music pin',p,.0115,.023,'wood_dark',8,axis=normal,bevel=0)
    # Crank extends to image-right, including the offset bend and large knob.
    tube('Gold music cylinder crank',[(-.298,-.012,z),(-.344,-.012,z),(-.344,-.012,z+.053),(-.427,-.012,z+.053)],.024,'gold',8)
    ellipsoid('Gold music crank knob',(-.446,-.012,z+.053),(.047,.047,.047),'gold',16,10)
    # A band encircles the coiffure; sculpted front scrolls sit above the fringe.
    pts=[(.30*math.sin(TAU*j/64),.205*math.cos(TAU*j/64),2.275) for j in range(64)]
    tube('Gold tiara encircling band',pts,.027,'gold',8,True)
    for s in (-1,1):
        scroll('Tiara wide gold volute',(s*.245,.208,2.252),.13,.077,-s,'gold',.035,.98)
        tube('Tiara rising filigree',[(s*.30,.204,2.254),(s*.17,.258,2.303),(s*.09,.276,2.31),(0,.281,2.244)],.026,'gold',8)
    diamond('Tiara pointed front gold crest',0,.267,2.312,.093,.132,'gold',.062)
    diamond('Tiara wine diamond inlay',0,.306,2.315,.035,.062,'red_dark',.024)
    for x,h in ((-.16,.063),(0,.10),(.16,.063)):
        diamond('Crown upper golden spike',x,-.018,2.5,.030,h,'gold',.048)
    # Back arbor and the broad figure-eight key are welded visually at overlap.
    # Its diagonal bow clears the head on the concept's right side.
    disk('Rear winding arbor',(0,-.275,1.44),.09,.16,'brass',20)
    bar('Long gold winding key shaft',(0,-.35,1.44),(-.46,-.39,1.63),.043,'gold',12)
    for x,zz in ((-.59,1.82),(-.715,1.623)):
        ring('Thick open figure eight winding key bow',(x,-.39,zz),.137,.145,.049,'gold',36)
    bar('Figure eight key central bridge',(-.56,-.39,1.70),(-.68,-.39,1.73),.055,'gold',12)


def triangles(obj):
    obj.data.calc_loop_triangles()
    return len(obj.data.loop_triangles)


def finish():
    bpy.ops.object.select_all(action='DESELECT')
    for part in PARTS:
        part.select_set(True); bpy.context.view_layer.objects.active=part
        for mod in list(part.modifiers): bpy.ops.object.modifier_apply(modifier=mod.name)
        part.select_set(False)
    before=sum(triangles(p) for p in PARTS)
    # Spend triangles on the face and volutes. Low-relief embroidery and hidden
    # back surfaces can simplify much more aggressively than organic silhouettes.
    priorities=[]
    for p in PARTS:
        name=p.name.lower(); weight=1.0
        if 'doll head' in name: weight=4.5
        elif any(s in name for s in ('spiral hair','corkscrew hair','thick porcelain fringe')): weight=3.0
        elif any(s in name for s in ('hair wave','hair cap','hair spiral')): weight=1.5
        elif any(s in name for s in ('smiling','eyelash','peaceful smile','porcelain crack')): weight=2.6
        elif any(s in name for s in ('shin','thigh','upper arm','forearm','fist','thumb','ball joint')): weight=1.8
        elif 'winding key bow' in name: weight=3.4
        elif 'knee' in name: weight=2.7
        elif 'hem' in name: weight=2.5
        elif any(s in name for s in ('skirt pleats','back cape','shoulder mantle','coin pouch')): weight=2.1
        elif 'tooth' in name and 'gear' in name: weight=2.0
        elif 'central glass lens' in name: weight=.3
        priorities.append((p,triangles(p),weight))
    lo,hi=0.,1.
    for _ in range(32):
        mid=(lo+hi)/2
        total=sum(t*(1. if t<=80 else min(1.,mid*w)) for _,t,w in priorities)
        if total>19750: hi=mid
        else: lo=mid
    for p,t,w in priorities:
        ratio=1. if t<=80 else min(1.,lo*w)
        if ratio<.999 and t>24:
            bpy.context.view_layer.objects.active=p
            mod=p.modifiers.new('Allocated silhouette detail','DECIMATE')
            mod.ratio=ratio; mod.delimit={'MATERIAL'}
            bpy.ops.object.modifier_apply(modifier=mod.name)
    for part in PARTS: part.select_set(True)
    bpy.context.view_layer.objects.active=PARTS[0]
    bpy.ops.object.join()
    obj=bpy.context.object
    obj.name='Ch4_Merchant'; obj.data.name='Ch4_Merchant_Mesh'
    joined=triangles(obj)
    if joined>19950:
        mod=obj.modifiers.new('Retain hero silhouette within 20k triangles','DECIMATE')
        mod.ratio=19850/joined; mod.delimit={'MATERIAL'}
        bpy.ops.object.modifier_apply(modifier=mod.name)
    low=min(v.co.z for v in obj.data.vertices); high=max(v.co.z for v in obj.data.vertices)
    # Uniform scaling, so the authored proportion and round joints are preserved.
    factor=2.6/(high-low)
    for v in obj.data.vertices:
        v.co.x*=factor; v.co.y*=factor; v.co.z=(v.co.z-low)*factor
    bm=bmesh.new(); bm.from_mesh(obj.data)
    bmesh.ops.remove_doubles(bm,verts=list(bm.verts),dist=.00001)
    bmesh.ops.recalc_face_normals(bm,faces=list(bm.faces))
    bm.to_mesh(obj.data); bm.free(); obj.data.update()
    bpy.context.view_layer.update()
    obj['front']='+Y'; obj['height_m']=2.6; obj['static_mesh']=True
    obj['character']='Chapter 4 clockwork music-box doll merchant'
    obj['triangle_count']=triangles(obj)
    obj['glass_representation']='Full glass hemisphere, clear central lens and reflection inlay'
    assert triangles(obj)<=20000,triangles(obj)
    assert abs(obj.dimensions.z-2.6)<1e-5
    assert all(math.isfinite(x) for v in obj.data.vertices for x in v.co)
    assert obj.location.length<1e-6
    assert len(bpy.context.scene.objects)==1
    for mat in obj.data.materials:
        assert mat.name in PALETTE
        assert tuple(mat.node_tree.nodes['Principled BSDF'].inputs['Base Color'].default_value)==(.5,.5,.5,1)
    DEST.parent.mkdir(parents=True,exist_ok=True)
    bpy.ops.export_scene.fbx(filepath=str(DEST),use_selection=True,
        axis_forward='-Z',axis_up='Y',apply_scale_options='FBX_SCALE_UNITS',
        bake_space_transform=True,object_types={'MESH'},use_mesh_modifiers=True,
        mesh_smooth_type='FACE',add_leaf_bones=False,bake_anim=False,use_custom_props=True)
    assert DEST.is_file() and DEST.stat().st_size>1024
    print(f'EXPORTED Ch4_Merchant {triangles(obj)} {tuple(round(d,4) for d in obj.dimensions)}',flush=True)
    print(f'AUTHORED_TRIANGLES {before}',flush=True)
    return obj


def preview(obj):
    scene=bpy.context.scene
    scene.render.engine='BLENDER_WORKBENCH'
    scene.render.resolution_x=600; scene.render.resolution_y=1000
    scene.render.resolution_percentage=100
    scene.render.image_settings.file_format='PNG'
    scene.render.film_transparent=False
    scene.view_settings.view_transform='Standard'
    shade=scene.display.shading
    shade.light='FLAT'; shade.color_type='MATERIAL'
    shade.show_shadows=False; shade.show_cavity=True; shade.cavity_type='BOTH'
    shade.curvature_ridge_factor=.55; shade.curvature_valley_factor=.8
    shade.cavity_ridge_factor=.45; shade.cavity_valley_factor=.8
    shade.show_specular_highlight=False
    shade.show_object_outline=True; shade.object_outline_color=(.22,.13,.09)
    shade.background_type='WORLD'
    scene.world=bpy.data.worlds.new('Warm parchment review world')
    scene.world.color=(.24,.215,.19)
    camera=bpy.data.objects.new('Temporary review camera',bpy.data.cameras.new('Temporary review camera'))
    scene.collection.objects.link(camera); scene.camera=camera
    camera.data.type='ORTHO'; camera.data.ortho_scale=3.28
    arrays=[]; labels=[]
    original_data=obj.data
    obj.data=original_data.copy()
    # Solid Workbench cannot display transmission. Remove ONLY the optically
    # clear centre in the review copy, keeping the exported hemisphere intact.
    group_index=obj.vertex_groups['Clear lens - review transparency'].index
    bm=bmesh.new(); bm.from_mesh(obj.data)
    deform=bm.verts.layers.deform.active
    clear=[v for v in bm.verts if deform and v[deform].get(group_index,0)>.5]
    bmesh.ops.delete(bm,geom=clear,context='VERTS')
    bm.to_mesh(obj.data); bm.free(); obj.data.update()
    for role,mat in MATS.items(): mat.diffuse_color=(*PALETTE[role],1)
    try:
        for title,az,el in [('FRONT',0,7),('FRONT RIGHT / GAME VIEW',-35,55),('BACK',180,12)]:
            az,el=math.radians(az),math.radians(el)
            target=Vector((0,0,1.30))
            camera.location=target+Vector((8*math.sin(az)*math.cos(el),8*math.cos(az)*math.cos(el),8*math.sin(el)))
            camera.rotation_euler=(target-camera.location).to_track_quat('-Z','Y').to_euler()
            # Fit in this narrow 600 x 1000 panel, including hands and rear key.
            inv=camera.rotation_euler.to_matrix().transposed()
            pts=[inv @ v.co for v in obj.data.vertices]
            xmin,xmax=min(p.x for p in pts),max(p.x for p in pts)
            ymin,ymax=min(p.y for p in pts),max(p.y for p in pts)
            center=camera.rotation_euler.to_matrix() @ Vector(((xmin+xmax)/2,(ymin+ymax)/2,0))
            camera.location+=center
            # Re-centre without changing view direction or distance.
            old=inv @ target
            camera.location-=camera.rotation_euler.to_matrix() @ Vector((old.x,old.y,0))
            camera.data.ortho_scale=max((xmax-xmin)/.6,ymax-ymin)*1.15
            data=bpy.data.curves.new('Review caption','FONT'); data.body=title
            data.align_x='CENTER'; data.size=.055 if len(title)>10 else .075
            data.materials.append(MATS['silver'])
            label=bpy.data.objects.new('Review caption',data); scene.collection.objects.link(label)
            label.rotation_euler=camera.rotation_euler
            label.location=camera.location+camera.rotation_euler.to_matrix() @ Vector((0,-camera.data.ortho_scale*.453,-7))
            labels.append(label)
            bpy.context.view_layer.update()
            # Background Blender may release Render Result's CPU pixel buffer.
            # A temporary lossless render is reliable on both Windows and Linux.
            with tempfile.TemporaryDirectory(prefix='merchant_review_') as tmp:
                scene.render.filepath=str(Path(tmp)/'view.png')
                bpy.ops.render.render(write_still=True)
                rendered=bpy.data.images.load(scene.render.filepath,check_existing=False)
                pixels=np.array(rendered.pixels[:],dtype=np.float32)
                bpy.data.images.remove(rendered)
            if pixels.size != 600*1000*4:
                raise RuntimeError('Workbench did not supply expected review pixels')
            arrays.append(pixels.reshape((1000,600,4)))
            bpy.data.objects.remove(label,do_unlink=True); labels.remove(label)
        merged=np.concatenate(arrays,axis=1)
        output=bpy.data.images.new('Merchant three view review',width=1800,height=1000,alpha=True)
        output.pixels.foreach_set(merged.ravel())
        output.filepath_raw=str(PREVIEW); output.file_format='PNG'
        PREVIEW.parent.mkdir(parents=True,exist_ok=True)
        output.save()
        scene.render.resolution_x=1800
        print(f'PREVIEW {PREVIEW}',flush=True)
    finally:
        review_data=obj.data; obj.data=original_data; bpy.data.meshes.remove(review_data)
        for label in labels: bpy.data.objects.remove(label,do_unlink=True)
        scene.camera=None; bpy.data.objects.remove(camera,do_unlink=True)
        for mat in MATS.values(): mat.diffuse_color=(.5,.5,.5,1)


def main():
    bpy.ops.wm.read_factory_settings(use_empty=True)
    random.seed(440271)
    scene=bpy.context.scene
    scene.unit_settings.system='METRIC'; scene.unit_settings.scale_length=1
    for role in PALETTE:
        mat=bpy.data.materials.new(role); mat.use_nodes=True
        mat.diffuse_color=(.5,.5,.5,1)
        bsdf=mat.node_tree.nodes['Principled BSDF']
        bsdf.inputs['Base Color'].default_value=(.5,.5,.5,1)
        bsdf.inputs['Roughness'].default_value=.65
        if role=='glass':
            bsdf.inputs['Roughness'].default_value=.16
            bsdf.inputs['Transmission Weight'].default_value=1
            bsdf.inputs['Alpha'].default_value=.16
            mat.surface_render_method='DITHERED'
        MATS[role]=mat
    make_skirt_and_cape(); make_legs_and_arms(); make_chest()
    make_belt_and_goods(); make_head(); make_crown_and_key()
    obj=finish(); preview(obj)
    assert len(scene.objects)==1 and scene.objects[0].name=='Ch4_Merchant'
    print('DONE',flush=True)


if __name__=='__main__':
    main()
