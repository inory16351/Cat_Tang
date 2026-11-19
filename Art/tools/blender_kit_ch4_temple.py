# 시계탑 성전 모듈 6종: 아케이드, 장미창, 제단과 장식물을 생성합니다.
# 실행: "C:/Program Files/Blender Foundation/Blender 5.3/blender.exe" -b --factory-startup -P Art/tools/blender_kit_ch4_temple.py
"""Blender 5.x, deterministic temple kit. Metres, Z up, front/room = +Y.

Only this file is a source dependency. All export geometry has grey role
materials; the coloured contact sheet is a separate, reversible review pass.
Wall structural front is Y=0; relief projects into +Y, backing into -Y.
"""
import math
import random
from pathlib import Path

import bmesh
import bpy
from mathutils import Euler, Matrix, Vector

ROOT = Path(__file__).resolve().parents[2]
OUTPUT = ROOT / 'Game/Assets/Art/Characters/Ch4'
PREVIEW = ROOT / 'Art/tools/specs/blender_kit_ch4_temple_preview.png'
TAU = math.tau
FACE = (math.pi / 2, 0, 0)
MATS, PARTS = {}, []
# Intentionally monochrome review colours, except lit wicks and small textiles.
PALETTE = {
    'stone': (.49, .49, .48), 'stone_dark': (.135, .14, .155),
    'porcelain': (.91, .90, .85), 'silver': (.65, .69, .73),
    'iron_dark': (.035, .038, .045), 'dial_face': (.74, .76, .73),
    'dial_mark': (.055, .057, .065), 'night_sky': (.009, .014, .035),
    'starlight': (.79, .86, .95), 'ribbon_red': (.55, .018, .038),
    'red_lacquer': (.30, .012, .022), 'lamp_warm': (1, .69, .29),
    'brass': (.40, .32, .20),
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
    v = [(x, y, z) for y in (back, front) for x, z in profile]
    f = [tuple(reversed(range(n))), tuple(range(n, 2*n))]
    f += [(j,(j+1)%n,(j+1)%n+n,j+n) for j in range(n)]
    return mesh(label, v, f, role, bevel)


def cylinder(label, loc, radius, depth, role, n=16, rot=(0,0,0), bevel=False):
    v = [(radius*math.cos(j*TAU/n),radius*math.sin(j*TAU/n),z)
         for z in (-depth/2,depth/2) for j in range(n)]
    f = [tuple(reversed(range(n))),tuple(range(n,2*n))]
    f += [(j,(j+1)%n,(j+1)%n+n,j+n) for j in range(n)]
    return place(mesh(label,v,f,role,bevel),loc,rot)


def lathe(label, profile, role, loc=(0,0,0), n=16):
    """Closed solid revolution, with explicit end caps (no axial zero rings)."""
    v = [(r*math.cos(j*TAU/n),r*math.sin(j*TAU/n),z)
         for r,z in profile for j in range(n)]
    f = [tuple(reversed(range(n))),tuple(range((len(profile)-1)*n,len(profile)*n))]
    f += [(k*n+j,k*n+(j+1)%n,(k+1)*n+(j+1)%n,(k+1)*n+j)
          for k in range(len(profile)-1) for j in range(n)]
    return place(mesh(label,v,f,role),loc)


def ring(label, loc, outer, inner, depth, role, n=48):
    v = [(r*math.cos(j*TAU/n),y,r*math.sin(j*TAU/n))
         for y,r in ((-depth/2,outer),(depth/2,outer),(depth/2,inner),(-depth/2,inner))
         for j in range(n)]
    f = [(k*n+j,k*n+(j+1)%n,((k+1)%4)*n+(j+1)%n,((k+1)%4)*n+j)
         for k in range(4) for j in range(n)]
    return place(mesh(label,v,f,role),loc)


def beam(label, a, b, width, depth, role, bevel=False):
    vec = Vector(b)-Vector(a)
    obj = box(label,(0,0,0),(width,depth,vec.length),role,bevel)
    obj.data.transform(Matrix.Translation((Vector(a)+Vector(b))/2) @
                       vec.to_track_quat('Z','Y').to_matrix().to_4x4())
    return obj


def tube(label, points, radius, role, n=5):
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


def star(x,y,z,r=.085,role='starlight'):
    p = [(x+math.sin(j*TAU/8)*r*(1 if j%2==0 else .28),
          z+math.cos(j*TAU/8)*r*(1 if j%2==0 else .28)) for j in range(8)]
    extrude('Four point star relief',p,y-.025,y+.025,role)


def rosette(x,y,z,r=.18):
    # Small liturgical gear medallion, never a freestanding machine part.
    p = [(x+math.sin(j*TAU/40)*r*(1 if j%4 in (1,2) else .84),
          z+math.cos(j*TAU/40)*r*(1 if j%4 in (1,2) else .84)) for j in range(40)]
    extrude('Gear rosette',p,y-.04,y+.04,'silver')
    cylinder('Rosette enamel centre',(x,y+.055,z),r*.53,.04,'dial_mark',n=12,rot=FACE)
    star(x,y+.085,z,r*.37,'porcelain')


def roman_marks(x,y,z,r,scale=1):
    numerals = ('XII','I','II','III','IV','V','VI','VII','VIII','IX','X','XI')
    for hour,letters in enumerate(numerals):
        a = hour*TAU/12
        # +Y front projects world -X to screen right. Numerals therefore run
        # clockwise in this frame and glyph strokes read correctly from +Y.
        u = Vector((-math.cos(a),0,-math.sin(a)))
        v = Vector((-math.sin(a),0,math.cos(a)))
        centre = Vector((x,y,z))+v*r
        for k,ch in enumerate(letters):
            dx = (k-(len(letters)-1)/2)*.095*scale
            strokes = {'I': [((0,-.10),(0,.10))],
                       'V': [((-.035,.10),(0,-.10)),((0,-.10),(.035,.10))],
                       'X': [((-.035,-.10),(.035,.10)),((-.035,.10),(.035,-.10))]}[ch]
            for p,q in strokes:
                beam('Roman numeral bar',centre+u*(dx+p[0]*scale)+v*p[1]*scale,
                     centre+u*(dx+q[0]*scale)+v*q[1]*scale,.04*scale,.045,'dial_mark')


def clock(x,y,z,r,rose=False):
    cylinder('Recess shadow',(x,y-.05,z),r*1.09,.15,'stone_dark',n=48,rot=FACE)
    cylinder('Ivory dial glass',(x,y+.045,z),r*.97,.07,'dial_face',n=48,rot=FACE)
    ring('Silver outer archivolt',(x,y+.09,z),r*1.07,r*.98,.15,'silver')
    ring('Dark numeral seat',(x,y+.13,z),r*.98,r*.75,.06,'porcelain')
    ring('Inner dial fillet',(x,y+.17,z),r*.75,r*.715,.07,'silver')
    roman_marks(x,y+.20,z,r*.865,max(.68,r*.72))
    if rose:
        for j in range(12):
            a = j*TAU/12
            def pt(rad,offset=0):
                return (x+rad*math.sin(a+offset),y+.14,z+rad*math.cos(a+offset))
            beam('Rose radial stone tracery',pt(r*.23),pt(r*.72),.07,.10,'stone_dark')
            # Twelve gothic lancet petals, all bounded inside the numeral ring.
            tube('Lobed rose petal',[pt(r*.25),pt(r*.42,-.15),pt(r*.62,-.14),
                 pt(r*.70),pt(r*.62,.14),pt(r*.42,.15),pt(r*.25)],.033,'stone_dark',n=4)
        ring('Rose central oculus',(x,y+.19,z),r*.25,r*.20,.08,'silver',n=32)
        cylinder('Oculus dark glass',(x,y+.15,z),r*.20,.06,'night_sky',n=24,rot=FACE)
    for angle,length,width in ((-.83,r*.56,.075),(.64,r*.39,.10)):
        a = Vector((x,y+.28,z)); b = a+Vector((math.sin(angle)*length,0,math.cos(angle)*length))
        beam('Hand dark outline',a,b,width+.055,.055,'dial_mark')
        beam('Silver clock hand',a+Vector((0,.035,0)),b+Vector((0,.035,0)),width,.05,'silver')
    cylinder('Clock hand pivot',(x,y+.36,z),r*.065,.07,'silver',n=12,rot=FACE)
    cylinder('Small red pivot jewel',(x,y+.405,z),max(.04,r*.025),.035,'red_lacquer',n=8,rot=FACE)


def column(x,y,top=3.2,diameter=.45,base=.0):
    r = diameter/2
    box('Square dark plinth',(x,y,base+.13),(.70,.70,.26),'stone_dark',True)
    box('Plinth silver reveal',(x,y,base+.28),(.59,.59,.055),'silver')
    lathe('Marble column foot',[(r*1.3,base+.3),(r*1.3,base+.40),
          (r,base+.49)],'porcelain',(x,y,0))
    lathe('Slender tapered marble shaft',[(r,base+.44),(r*.90,top-.43),
          (r*.96,top-.37)],'porcelain',(x,y,0),n=16)
    # Broad dark flutes make the white shaft legible from a game camera.
    for dx in (-r*.53,r*.53):
        beam('Recessed shaft flute',(x+dx,y+r*.87,base+.61),
             (x+dx*.90,y+r*.82,top-.50),.041,.025,'stone')
    lathe('Stacked carved silver capital',[(r*1.02,top-.38),(r*1.25,top-.31),
          (r*1.1,top-.25),(r*1.38,top-.16),(r*1.38,top-.10)],'silver',(x,y,0))
    box('Capital square abacus',(x,y,top-.05),(.70,.68,.10),'stone_dark',True)
    rosette(x,y+r*1.33,top-.23,r*.64)


def banner(x,y,top,w,length,torn=False):
    # Closed pleated cloth with a real swallow-tail silhouette and thick folds.
    profile = [(x-w/2,top),(x+w/2,top),(x+w/2,top-length),
               (x+w*.20,top-length+.12 if torn else top-length),
               (x,top-length+(.29 if torn else .23)),
               (x-w*.22,top-length+.07 if torn else top-length),
               (x-w/2,top-length)]
    obj = extrude('Swallow tail sacred banner',profile,y-.035,y+.035,'ribbon_red')
    # Variable Y displacement preserves thickness and adds broad cloth folds.
    for v in obj.data.vertices:
        v.co.y += .035*math.cos((v.co.x-x)*TAU/w)
    for dx in (-w*.31,w*.31):
        box('Raised woven ribbon fold',(x+dx,y+.045,top-length*.42),
            (.043,.04,length*.81),'red_lacquer')
    box('Silver banner crossbar',(x,y,top+.04),(w+.16,.085,.08),'silver')
    star(x,y+.09,top-.32,w*.22,'silver')


def lantern(x,y,z,s=1):
    cylinder('Lantern dark foot',(x,y,z),.17*s,.10*s,'iron_dark',n=8)
    cylinder('Small warm glass',(x,y,z+.22*s),.11*s,.34*s,'lamp_warm',n=8)
    for dx,dy in ((-.12,0),(.12,0),(0,-.12),(0,.12)):
        box('Lantern cage upright',(x+dx*s,y+dy*s,z+.22*s),(.043*s,.043*s,.39*s),'iron_dark')
    lathe('Lantern pointed hood',[(.20*s,z+.40*s),(.20*s,z+.44*s),
          (.055*s,z+.59*s)],'iron_dark',(x,y,0),n=8)
    cylinder('Lantern silver finial',(x,y,z+.62*s),.045*s,.08*s,'silver',n=8)


def candle(x,y,bottom,h,r=.06):
    cylinder('Ivory candle',(x,y,bottom+h/2),r,h,'porcelain',n=10)
    cylinder('Candle silver socket',(x,y,bottom+.025),r*1.26,.05,'silver',n=10)
    cylinder('Dark wax cup',(x,y,bottom+h+.006),r*.77,.018,'stone_dark',n=10)
    for j in range(2):
        a = .5+j*1.8
        cylinder('Broad wax drip',(x+math.sin(a)*r*.92,y+math.cos(a)*r*.92,bottom+h-.065-j*.035),
                 .021,.15+j*.04,'porcelain',n=6)
    # Tiny flame cone, not a bulb that overwhelms the pale silhouette.
    lathe('Candle flame',[(.022,bottom+h+.025),(.035,bottom+h+.065),
          (.006,bottom+h+.15)],'lamp_warm',(x,y,0),n=6)


def pointed(cx,half,spring,top,steps=12):
    left = []
    for i in range(steps+1):
        t = i/steps
        # Vertical at the spring, nonzero slope at the crown: a true pointed
        # join between the two curved sides, rather than a rounded ellipse.
        left.append((cx-half*(1-t*t), spring+(top-spring)*t))
    return left+[(2*cx-x,z) for x,z in reversed(left[:-1])]


def arch_ribbon(label,inner,outer,back,front,role):
    # Individual closed voussoirs avoid overlapping boolean seams.
    for i in range(len(inner)-1):
        extrude(label,[inner[i],inner[i+1],outer[i+1],outer[i]],back,front,role)


def colonnade():
    box('Continuous arcade foundation',(0,-.35,.11),(6,.9,.22),'stone_dark',True)
    for x in (-2.65,0,2.65):
        # Ends remain exactly at +/-3 including the capital and square plinth.
        column(x,-.31,3.20)
    for cx in (-1.325,1.325):
        # 2.4 m openings, elliptical crowns at 4.6 m.
        curve = [(cx+1.2*math.cos(j*math.pi/24),3.2+1.4*math.sin(j*math.pi/24)) for j in range(25)]
        extrude('Night behind arcade',[(cx-1.2,.22),(cx+1.2,.22)]+curve,
                -.40,-.30,'night_sky')
        # Genuine open arcade silhouette: solid stone only ABOVE the intrados.
        extrude('Cut out upper arcade wall',curve+[(cx-1.325,5.28),(cx+1.325,5.28)],
                -.8,-.03,'stone')
        outer = [(cx+1.32*math.cos(j*math.pi/24),3.2+1.57*math.sin(j*math.pi/24)) for j in range(25)]
        arch_ribbon('Carved ivory arch stones',curve,outer,-.24,.08,'porcelain')
        tube('Silver arch fillet',[(x,.10,z+.035) for x,z in outer],.035,'silver',n=4)
        for j in range(18):
            x = random.uniform(cx-1.08,cx+1.08)
            z = random.uniform(1.15,4.35)
            if z < 3.2+1.4*math.sqrt(max(0,1-((x-cx)/1.2)**2))-.16:
                star(x,-.235,z,random.uniform(.055,.095))
        # Stone rail, bulbous white balusters, and dark recess below the railing.
        box('Balustrade low sill',(cx,-.22,.32),(2.42,.62,.20),'stone',True)
        box('Balustrade handrail',(cx,-.20,.86),(2.42,.66,.12),'stone',True)
        box('White rail fillet',(cx,.14,.89),(2.40,.045,.045),'porcelain')
        for k in range(6):
            xx = cx+(k-2.5)*.36
            lathe('Carved white baluster',[(.095,.42),(.095,.47),(.06,.52),
                  (.085,.62),(.045,.73),(.075,.80)],'porcelain',(xx,-.20,0),n=8)
        rosette(cx,.10,4.97,.14)
    box('Dark carved frieze band',(0,-.31,5.40),(6,.89,.30),'stone_dark',True)
    box('Ivory frieze inset',(0,.15,5.40),(5.80,.065,.19),'porcelain')
    for j in range(43):
        box('Clock tick frieze',( -2.84+j*5.68/42,.20,5.40),
            (.047,.055,.16 if j%3==0 else .10),'dial_mark')
    for z,w,d,h,role in ((5.65,6,.96,.14,'silver'),(5.83,6,1,.22,'stone'),
                           (5.97,6,1,.06,'porcelain')):
        box('Continuous stepped cornice',(0,-.30,z),(w,d,h),role,True)
    banner(0,.16,5.13,.60,2.40)
    for x in (-2.65,2.65):
        beam('Lantern wall arm',(x,-.10,2.39),(x,.38,2.39),.065,.065,'iron_dark')
        beam('Lantern bracket brace',(x,-.11,2.12),(x,.32,2.39),.05,.05,'silver')
        lantern(x,.38,2.40,.72)


def rose_wall():
    box('Solid cathedral wall',(0,-.42,3),(6,.84,6),'stone_dark')
    # Ashlar seams are geometry gaps over an uninterrupted blocker backing.
    for row in range(8):
        cuts = [-3]+[x for x in [-2.55+(row%2)*.63+j*1.26 for j in range(5)] if -2.9<x<2.9]+[3]
        for j,(a,b) in enumerate(zip(cuts,cuts[1:])):
            box('Dressed stone ashlar',((a+b)/2,-.005,(row+.5)*.75),
                (b-a-.025,.09,.725),'stone' if (row+j)%5 else 'porcelain')
    box('Lower black dado',(0,.08,.39),(6,.20,.78),'stone_dark',True)
    box('Dado marble reveal',(0,.19,.75),(6,.075,.065),'porcelain')
    for x in (-2.66,2.66):
        box('Layered pilaster foot',(x,-.10,.18),(.68,1,.36),'stone_dark',True)
        box('Tall white pilaster',(x,-.06,2.90),(.48,.72,5.20),'porcelain',True)
        box('Pilaster dark recessed face',(x,.315,2.88),(.28,.045,4.73),'stone_dark')
        box('Pilaster inner silver line',(x,.345,2.88),(.075,.035,4.50),'silver')
        for z in (.46,5.36,5.51):
            box('Pilaster carved molding',(x,-.02,z),(.63,.84,.12),'silver',True)
        rosette(x,.38,5.15,.15)
    for z,h,d,role in ((5.69,.18,.96,'stone_dark'),(5.84,.12,1,'silver'),(5.96,.08,1,'porcelain')):
        box('Rose wall cornice',(0,-.28,z),(6,d,h),role,True)
    clock(0,.12,3.60,1.70,rose=True)
    # Pointed altar recess: dark interior and layered stone archivolts.
    inner = pointed(0,.92,1.33,2.08,8)
    outer = pointed(0,1.04,1.36,2.20,8)
    extrude('Shadow in shallow altar niche',[(-.92,.12),(.92,.12)]+list(reversed(inner)),
            .055,.11,'night_sky')
    arch_ribbon('Niche ivory archivolt',inner,outer,.10,.23,'porcelain')
    for x in (-.98,.98):
        box('Niche side jamb',(x,.16,.72),(.12,.18,1.24),'porcelain')
    box('Altar dark pedestal',(0,.28,.43),(1.52,.65,.62),'stone_dark',True)
    box('Altar carved inset',(0,.625,.43),(1.20,.045,.36),'stone')
    box('Altar slab silver rim',(0,.28,.79),(1.91,.80,.12),'silver',True)
    box('Altar marble top',(0,.28,.87),(1.84,.75,.08),'porcelain',True)
    box('Draped altar cloth top',(0,.30,.925),(.60,.72,.045),'ribbon_red')
    banner(0,.705,.91,.58,.65)
    for side in (-1,1):
        for j in range(3):
            candle(side*(.53+j*.11),.32-(j%2)*.18,.92,.28+.12*(j%2),.047)
    star(0,.24,1.60,.16,'silver')


def angel(x,y,bottom,side):
    # Abstract kneeling figures: bowed heads, prayer hands, robes and swept
    # wings. Gear-tooth feather ends quote the clock motif without machinery.
    box('Kneeling robe base',(x,y,bottom+.16),(.62,.70,.32),'stone',True)
    body = [(x-.26,bottom+.25),(x+.26,bottom+.25),(x+.19,bottom+1.11),
            (x-.08,bottom+1.22),(x-.24,bottom+.85)]
    extrude('Angular white angel robe',body,y-.23,y+.22,'porcelain',True)
    for dx in (-.14,0,.14):
        beam('Carved robe fold',(x+dx,y+.235,bottom+.31),
             (x+dx*.55,y+.235,bottom+.97),.045,.045,'silver')
    bpy.ops.mesh.primitive_ico_sphere_add(subdivisions=2,radius=.205,location=(0,0,0))
    obj = bpy.context.object; obj.name = 'Bowed faceted angel head'
    obj.data.materials.append(MATS['porcelain']); PARTS.append(obj)
    place(obj,(x-side*.08,y+.13,bottom+1.35))
    ring('Thin saint halo',(x-side*.08,y-.02,bottom+1.41),.29,.255,.05,'silver',n=24)
    beam('Angel folded arm',(x+side*.15,y+.24,bottom+.99),
         (x-side*.18,y+.46,bottom+1.05),.13,.14,'porcelain',True)
    beam('Praying hands',(x-side*.18,y+.46,bottom+1.05),
         (x-side*.22,y+.40,bottom+1.24),.10,.11,'porcelain')
    for wing_side in (-1,1):
        # Feather fan rises out of the shoulder; each stepped blade stays broad.
        for j in range(5):
            out = .36+j*.10
            high = 1.93-j*.16
            p = [(x+wing_side*.10,bottom+.79),
                 (x+wing_side*(out-.08),bottom+high-.22),
                 (x+wing_side*out,bottom+high),
                 (x+wing_side*(out+.10),bottom+high+.04),
                 (x+wing_side*(out+.10),bottom+high-.10),
                 (x+wing_side*(out+.16),bottom+high-.15),
                 (x+wing_side*.23,bottom+.66)]
            extrude('Sweeping gear tooth feather',p,y-.27-j*.025,y-.20-j*.025,
                    'porcelain' if j%2==0 else 'silver')


def clock_altar():
    for r,z,h,role in ((2.20,.12,.24,'stone_dark'),(2.04,.29,.10,'porcelain'),
                       (1.93,.41,.18,'stone'),(1.78,.52,.06,'porcelain'),
                       (1.65,.60,.10,'stone_dark')):
        obj = cylinder('Round stepped sanctuary dais',(0,0,z),r,h,role,n=48,bevel=True)
        for v in obj.data.vertices: v.co.y *= 3.2/4.4
    box('Tall dark clock stele',(0,-.31,1.86),(1.68,.86,2.42),'stone_dark',True)
    for x in (-.80,.80):
        box('Stele ivory edge',(x,.15,1.90),(.09,.075,2.30),'porcelain')
    extrude('Peaked stele crown',[(-.84,3.04),(0,3.40),(.84,3.04)],-.74,.12,'stone_dark',True)
    tube('Stele crown silver edge',[(-.83,.15,3.04),(0,.15,3.38),(.83,.15,3.04)],.04,'silver')
    clock(0,.20,2.30,.80)
    box('Stele low carved panel',(0,.15,1.04),(1.35,.075,.41),'stone')
    rosette(0,.22,1.04,.16)
    for side in (-1,1): angel(side*1.29,-.10,.65,side)
    for x,y,h in ((-1.47,.78,.35),(-1.20,1,.48),(-.91,1.10,.29),
                  (1.47,.78,.42),(1.20,1,.30),(.91,1.10,.50)):
        candle(x,y,.46,h,.07)
    box('Cloth across front step',(0,1.14,.475),(.66,.60,.045),'ribbon_red')
    banner(0,1.43,.49,.64,.42)
    for x in (-.50,.50):
        box('Front dais inset',(x,1.49,.20),(.16,.045,.16),'silver',rot=(0,0,0))


def arch_ruin():
    box('Continuous low rubble blocker',(0,0,.27),(5,2.4,.54),'stone_dark',True)
    for row in range(2):
        for j in range(7):
            x = -2.10+j*.70+(row%2)*.10
            box('Low wall rubble ashlar',(x,.62,.45+row*.22),(.65,.86,.24),
                'stone' if j%3 else 'porcelain',True,rot=(0,0,random.uniform(-.07,.07)))
    for x in (-1.85,1.85): column(x,-.14,3.10,.48,.54)
    inner = pointed(0,1.85,3.10,4.58,12)
    outer = pointed(0,2.20,3.10,4.80,12)
    for j in list(range(15))+[22,23]:
        p,q = inner[j],inner[j+1]; a,b = outer[j],outer[j+1]
        # Recessed mortar line between individually beveled voussoirs.
        centre = Vector(((p[0]+q[0]+a[0]+b[0])/4,(p[1]+q[1]+a[1]+b[1])/4))
        contour = [tuple(centre+(Vector(v)-centre)*.96) for v in (p,q,b,a)]
        extrude('Surviving pointed voussoir',contour,-.64,.35,
                'porcelain' if j%2 else 'stone',True)
    # A small dial in the pointed keystone, rather than a gear mechanism.
    clock(0,.38,4.45,.25)
    banner(-1.84,.30,3.02,.49,1.88,True)
    for j,(x,y,z,s) in enumerate(((.95,.20,.86,.57),(1.45,.42,1.03,.51),
              (1.97,.50,.89,.55),(.52,.67,.76,.47),(-.68,-.55,.66,.49),
              (1.03,-.59,.70,.53),(1.77,-.61,.73,.47))):
        box('Fallen arch block',(x,y,z),(s,.58,s*.64),'stone' if j%2 else 'stone_dark',
            True,rot=(.12*(-1)**j,.16,random.uniform(-.55,.55)))
    # Visible dark fracture faces at both broken ends.
    extrude('Broken arch scar',[(.82,4.13),(1.04,4.28),(1.23,4.13),(.98,4.00)],
            -.65,.36,'stone_dark')


def banner_pillar():
    box('Pillar square footing',(0,0,.13),(.8,.8,.26),'stone_dark',True)
    column(0,-.04,3.54,.36,.02)
    banner(0,.24,3.27,.50,2.20)
    lantern(0,-.04,3.56,.97)


def candles():
    box('Candle stand lower step',(0,0,.10),(1,.8,.20),'stone_dark',True)
    box('Candle stand silver reveal',(0,0,.215),(.90,.69,.045),'silver')
    box('Candle stand upper step',(0,-.05,.30),(.80,.56,.13),'stone',True)
    for x,y,h in ((-.28,-.15,.56),(-.06,-.17,.585),(.20,-.17,.43),
                  (-.30,.13,.30),(-.08,.14,.41),(.16,.13,.25),(.31,.06,.35)):
        candle(x,y,.365,h,.06)
    # Tiny clock-hand votive ornament, separate from the warm wick tips.
    cylinder('Stand ornament dark seat',(0,.405,.12),.095,.025,'dial_mark',n=12,rot=FACE)
    beam('Votive clock hand',(0,.43,.12),(-.09,.43,.18),.04,.025,'silver')
    beam('Votive short hand',(0,.43,.12),(.065,.43,.17),.04,.025,'silver')


SPECS = [
    ('Wall_Colonnade',(6,None,6),colonnade,8000),
    ('Wall_RoseDial',(6,None,6),rose_wall,8000),
    ('LM_ClockAltar',(4.4,3.2,3.4),clock_altar,8000),
    ('LM_ArchRuin',(5,2.4,4.8),arch_ruin,8000),
    ('Deco_BannerPillar',(.8,.8,4.2),banner_pillar,3000),
    ('Deco_Candles',(1,.8,1.1),candles,3000),
]


def triangles(obj):
    obj.data.calc_loop_triangles()
    return len(obj.data.loop_triangles)


def bounds(obj):
    return [Vector(tuple(fn(v.co[i] for v in obj.data.vertices) for i in range(3)))
            for fn in (min,max)]


def finish(name,dims,budget):
    bpy.ops.object.select_all(action='DESELECT')
    for obj in PARTS:
        obj.select_set(True)
        bpy.context.view_layer.objects.active = obj
        for mod in list(obj.modifiers): bpy.ops.object.modifier_apply(modifier=mod.name)
        obj.select_set(False)
    for obj in PARTS: obj.select_set(True)
    bpy.context.view_layer.objects.active = PARTS[0]
    bpy.ops.object.join()
    obj = bpy.context.object
    obj.name, obj.data.name = name,name+'_Mesh'
    obj.location = (0,0,0)
    count = triangles(obj)
    if count>budget:
        mod = obj.modifiers.new('Preserve materials within triangle budget','DECIMATE')
        mod.ratio = (budget-120)/count
        mod.delimit = {'MATERIAL'}
        bpy.ops.object.modifier_apply(modifier=mod.name)
    # Landmarks/decor meet their exact envelopes; architectural walls are never
    # rescaled so their 2.4 m openings and modular seam lines remain authored.
    low,high = bounds(obj)
    if name.startswith('Wall_'):
        # Bevel miters can overshoot a seam by about a millimetre. Snap only
        # the perimeter, leaving every internal architectural dimension intact.
        for v in obj.data.vertices:
            if abs(abs(v.co.x)-dims[0]/2)<.003:
                v.co.x = math.copysign(dims[0]/2,v.co.x)
            if abs(v.co.z-dims[2])<.003: v.co.z = dims[2]
    else:
        span,centre = high-low,(high+low)/2
        for v in obj.data.vertices:
            v.co.x = (v.co.x-centre.x)*dims[0]/span.x
            v.co.y = (v.co.y-centre.y)*dims[1]/span.y
            v.co.z = (v.co.z-low.z)*dims[2]/span.z
    bm = bmesh.new(); bm.from_mesh(obj.data)
    bmesh.ops.recalc_face_normals(bm,faces=list(bm.faces))
    assert not any(e.is_boundary or not e.is_manifold for e in bm.edges),name
    bm.to_mesh(obj.data); bm.free()
    for p in obj.data.polygons: p.use_smooth = False
    obj.data.update(); bpy.context.view_layer.update()
    low,high = bounds(obj)
    assert abs(low.x+dims[0]/2)<1e-4 and abs(high.x-dims[0]/2)<1e-4,(name,low,high)
    assert abs(low.z)<1e-4 and abs(high.z-dims[2])<1e-4,(name,low,high)
    if dims[1]: assert abs(high.y-low.y-dims[1])<1e-4,(name,low,high)
    assert triangles(obj)<=budget,(name,triangles(obj))
    assert all(math.isfinite(c) for v in obj.data.vertices for c in v.co)
    areas = {}
    for p in obj.data.polygons:
        role = obj.data.materials[p.material_index].name
        assert role in PALETTE
        areas[role] = areas.get(role,0)+p.area
    assert len(areas)>=(6 if budget==8000 else 4),(name,areas)
    assert max(areas.values())/sum(areas.values())<=.55,(name,areas)
    # Complete surface area includes buried backs; report it to aid review.
    print('MATERIALS',name,{k:round(v/sum(areas.values()),3) for k,v in areas.items()},flush=True)
    for mat in obj.data.materials:
        assert tuple(mat.diffuse_color)==(.5,.5,.5,1)
        assert tuple(mat.node_tree.nodes['Principled BSDF'].inputs['Base Color'].default_value)==(.5,.5,.5,1)
    assert tuple(obj.location)==(0,0,0)
    obj['front'] = '+Y'
    obj['origin_rule'] = 'Bottom centre of structural front Y=0' if name.startswith('Wall_') else 'Bottom centre'
    obj['chapter'], obj['triangle_count'] = 'Cathedral of Time',triangles(obj)
    obj['solid_blocker'] = True
    if name.startswith('Wall_'): obj['module_width_m'] = 6.0
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
    if scene.world is None: scene.world = bpy.data.worlds.new('Temple review world')
    scene.world.color = (.025,.029,.040)
    helpers = []
    try:
        for role,mat in MATS.items(): mat.diffuse_color = (*PALETTE[role],1)
        camera = bpy.data.objects.new('Review camera',bpy.data.cameras.new('Review camera'))
        scene.collection.objects.link(camera); helpers.append(camera); scene.camera = camera
        el,az = math.radians(35),math.radians(-8)
        near = Vector((math.sin(az),math.cos(az),0))
        camera.location = near*35*math.cos(el)+Vector((0,0,35*math.sin(el)))
        camera.rotation_euler = (-camera.location).to_track_quat('-Z','Y').to_euler()
        camera.data.type = 'ORTHO'
        rotation = camera.rotation_euler.to_matrix(); inv = rotation.transposed()
        right = rotation @ Vector((1,0,0))
        # Two side-by-side rows, true metre scale. Smaller props share the
        # right-hand column; they are never enlarged relative to the walls.
        positions = [(-7,-2.9),(0,-2.9),(-6.2,4.6),(-.5,4.6),(5.4,-1.0),(5.4,4.6)]
        for obj,(x,y) in zip(objects,positions): obj.location = right*x+near*y
        bpy.context.view_layer.update()
        def screen_points(obj):
            return [inv @ (obj.matrix_world @ v.co) for v in obj.data.vertices]
        # Fit the two lower landmarks below the upper row's captions. This
        # uses actual projected silhouettes, so changing an arch cannot cause
        # the next review to obscure another piece or its label.
        upper_bottom = min(p.y for obj in objects[:2] for p in screen_points(obj))
        lower_top = max(p.y for obj in objects[2:4] for p in screen_points(obj))
        shift = max(0,(lower_top-upper_bottom+1.13)/math.sin(el))
        for obj in objects[2:4]: obj.location += near*shift
        bpy.context.view_layer.update()

        def caption(body,x,y,size,role='silver'):
            data = bpy.data.curves.new('Preview typography','FONT')
            data.body,data.align_x,data.size = body,'CENTER',size
            data.materials.append(MATS[role])
            label = bpy.data.objects.new('Review label',data)
            scene.collection.objects.link(label); helpers.append(label)
            label.rotation_euler = camera.rotation_euler
            label.location = rotation @ Vector((x,y,0))
            return label

        review = []
        for obj,(_,dims,_,_) in zip(objects,SPECS):
            pts = [inv @ (obj.matrix_world @ v.co) for v in obj.data.vertices]
            review += pts
            x = (min(p.x for p in pts)+max(p.x for p in pts))/2
            y = min(p.y for p in pts)-.30
            caption(obj.name,x,y,.22)
            caption(f'{dims[0]:g} m  /  1:1',x,y-.27,.15,'stone')
            review += [Vector((x-1.5,y-.4,0)),Vector((x+1.5,y+.2,0))]
        top = max(p.y for p in review)
        left,right_edge = min(p.x for p in review),max(p.x for p in review)
        title_x = (left+right_edge)/2
        caption('IV  /  CATHEDRAL OF TIME',title_x,top+.83,.40,'porcelain')
        caption('TEMPLE KIT     |     SIX MESHES     |     METRES     |     +Y FRONT',title_x,top+.40,.18)
        review += [Vector((left,top+1.5,0)),Vector((right_edge,top+1.5,0))]
        lo = Vector((min(p.x for p in review),min(p.y for p in review),0))
        hi = Vector((max(p.x for p in review),max(p.y for p in review),0))
        camera.location += rotation @ ((lo+hi)/2)
        camera.data.ortho_scale = max(hi.x-lo.x,(hi.y-lo.y)*1.8)*1.08
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
        PARTS = []; random.seed(44071+i*7919)
        build(); objects.append(finish(name,dims,budget))
    assert len(bpy.context.scene.objects)==6
    assert all(o.type=='MESH' for o in bpy.context.scene.objects)
    preview(objects)
    assert len(bpy.context.scene.objects)==6 and all(o.type=='MESH' for o in bpy.context.scene.objects)
    print('DONE',flush=True)


if __name__=='__main__':
    main()
