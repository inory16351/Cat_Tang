# 새벽에 사라지는 서부 마을 시설 5종: 사방 외벽, 지붕, 시계 장식과 검토용 이미지.
# 실행: "C:/Program Files/Blender Foundation/Blender 5.3/blender.exe" -b --factory-startup -P Art/tools/blender_kit_village_buildings.py
"""Deterministic village kit; stock Blender only. Metres, Z up, front +Y.

Run exports first with neutral role materials, then renders a coloured review.
Broken edges stay on the buildings; rising debris is spawned by the game.
The gate deliberately substitutes an open passage for the shops' closed door.
No shared helpers, input assets, textures, or other source files are modified.
"""

import math
import random
from pathlib import Path

import bmesh
import bpy
from mathutils import Euler, Matrix, Vector
from mathutils.bvhtree import BVHTree


ROOT = Path(__file__).resolve().parents[2]
OUTPUT = ROOT / 'Game/Assets/Art/Characters/Village'
PREVIEW = ROOT / 'Art/tools/specs/blender_kit_village_buildings_preview.png'
ROLES = ('wood', 'wood_dark', 'wood_light', 'wood_weathered', 'plank',
         'roof_shingle', 'roof_tin', 'stone', 'stone_dark', 'sand_glow',
         'brass', 'gold', 'gold_drip', 'iron', 'rust', 'canvas', 'cloth_red',
         'cloth_cream', 'paint_rose', 'paint_sage', 'glass', 'window_lit',
         'lantern_glow', 'clock_face')
SOFT = {'canvas', 'cloth_red', 'cloth_cream', 'glass', 'window_lit',
        'lantern_glow', 'sand_glow', 'clock_face'}
PALETTE = {
    'wood': (.46, .25, .12), 'wood_dark': (.21, .105, .055),
    'wood_light': (.76, .51, .28), 'wood_weathered': (.49, .40, .29),
    'plank': (.63, .38, .20), 'roof_shingle': (.37, .24, .18),
    'roof_tin': (.60, .53, .41), 'stone': (.60, .55, .45),
    'stone_dark': (.30, .27, .22), 'sand_glow': (1, .77, .29),
    'brass': (.72, .45, .13), 'gold': (1, .67, .17),
    'gold_drip': (.96, .47, .055), 'iron': (.16, .17, .15),
    'rust': (.54, .22, .10), 'canvas': (.81, .66, .43),
    'cloth_red': (.67, .16, .105), 'cloth_cream': (.96, .87, .67),
    'paint_rose': (.69, .36, .31), 'paint_sage': (.43, .51, .32),
    'glass': (.67, .69, .54), 'window_lit': (1, .79, .40),
    'lantern_glow': (1, .56, .16), 'clock_face': (.98, .91, .73),
}
MATS, PARTS = {}, []
WOOD = ('wood', 'wood_weathered', 'wood_light', 'wood', 'wood_dark')
TAU = math.tau


def materials():
    for role in ROLES:
        mat = bpy.data.materials.new(role)
        mat.use_nodes = True
        mat.diffuse_color = (.5, .5, .5, 1)
        nodes = mat.node_tree.nodes
        nodes.clear()
        shader = nodes.new('ShaderNodeBsdfPrincipled')
        shader.inputs['Base Color'].default_value = (.5, .5, .5, 1)
        shader.inputs['Roughness'].default_value = .8
        shader.inputs['Metallic'].default_value = 0
        out = nodes.new('ShaderNodeOutputMaterial')
        mat.node_tree.links.new(shader.outputs['BSDF'], out.inputs['Surface'])
        MATS[role] = mat


def mesh(label, verts, faces, role, bevel=True):
    data = bpy.data.meshes.new(label)
    data.from_pydata(verts, [], faces)
    data.materials.append(MATS[role])
    data.update()
    obj = bpy.data.objects.new(label, data)
    bpy.context.collection.objects.link(obj)
    if bevel and role not in SOFT:
        mod = obj.modifiers.new('Worn 0.02m edges / two segments', 'BEVEL')
        mod.width, mod.segments = .02, 2
        mod.limit_method, mod.angle_limit = 'ANGLE', math.radians(45)
    PARTS.append(obj)
    return obj


def transform(obj, loc=(0, 0, 0), rot=(0, 0, 0)):
    obj.data.transform(Matrix.Translation(Vector(loc)) @
                       Euler(rot).to_matrix().to_4x4())
    return obj


def box(label, loc, size, role, rot=(0, 0, 0), bevel=True):
    x, y, z = (s / 2 for s in size)
    return transform(mesh(label,
        [(-x,-y,-z), (x,-y,-z), (x,y,-z), (-x,y,-z),
         (-x,-y,z), (x,-y,z), (x,y,z), (-x,y,z)],
        [(0,3,2,1), (4,5,6,7), (0,1,5,4), (1,2,6,5),
         (2,3,7,6), (3,0,4,7)], role, bevel), loc, rot)


def lathe(label, loc, rings, role, n=10, rot=(0, 0, 0), bevel=True):
    verts = [(r*math.cos(j*TAU/n), r*math.sin(j*TAU/n), z)
             for z, r in rings for j in range(n)]
    faces = [tuple(reversed(range(n)))]
    faces += [(k*n+j, k*n+(j+1)%n, (k+1)*n+(j+1)%n, (k+1)*n+j)
              for k in range(len(rings)-1) for j in range(n)]
    faces.append(tuple(range((len(rings)-1)*n, len(rings)*n)))
    return transform(mesh(label, verts, faces, role, bevel), loc, rot)


def cylinder(label, loc, radius, depth, role, n=12, rot=(0, 0, 0), bevel=True):
    return lathe(label, loc, [(-depth/2,radius), (depth/2,radius)],
                 role, n, rot, bevel)


def rod(label, a, b, radius, role, n=6, bevel=True):
    a, b = Vector(a), Vector(b)
    obj = cylinder(label, (0,0,0), radius, (b-a).length, role, n, bevel=bevel)
    obj.data.transform(Matrix.Translation((a+b)/2) @
                       (b-a).to_track_quat('Z','Y').to_matrix().to_4x4())
    return obj


def tube(label, points, radius, role, n=5):
    verts = []
    for i, point in enumerate(points):
        tangent = (Vector(points[min(i+1,len(points)-1)]) -
                   Vector(points[max(0,i-1)])).normalized()
        ref = Vector((0,1,0)) if abs(tangent.y) < .9 else Vector((1,0,0))
        u = ref.cross(tangent).normalized()
        v = tangent.cross(u)
        verts += [tuple(Vector(point)+radius*(u*math.cos(j*TAU/n)+v*math.sin(j*TAU/n)))
                  for j in range(n)]
    faces = [tuple(reversed(range(n)))]
    faces += [(k*n+j,k*n+(j+1)%n,(k+1)*n+(j+1)%n,(k+1)*n+j)
              for k in range(len(points)-1) for j in range(n)]
    faces.append(tuple(range((len(points)-1)*n,len(points)*n)))
    return mesh(label,verts,faces,role,bevel=False)


def extrude(label, polygon, depth, role, y=0, bevel=True):
    n = len(polygon)
    verts = [(x,y+d,z) for d in (-depth/2,depth/2) for x,z in polygon]
    faces = [tuple(reversed(range(n))), tuple(range(n,2*n))]
    faces += [(j,(j+1)%n,(j+1)%n+n,j+n) for j in range(n)]
    return mesh(label,verts,faces,role,bevel)


def ring(label, loc, radius, band, depth, role, n=24):
    verts = [(r*math.cos(j*TAU/n),y,r*math.sin(j*TAU/n))
             for r,y in ((radius-band,-depth/2),(radius,-depth/2),
                         (radius,depth/2),(radius-band,depth/2)) for j in range(n)]
    faces = [(k*n+j,k*n+(j+1)%n,((k+1)%4)*n+(j+1)%n,((k+1)%4)*n+j)
             for k in range(4) for j in range(n)]
    return transform(mesh(label,verts,faces,role),loc)


def scroll(loc, scale=.8, flip=1):
    x,y,z = loc
    points = []
    for i in range(15):
        t = i/14
        angle = -.65+t*TAU*1.1
        r = scale*(.45-.33*t)
        points.append((x+flip*r*math.cos(angle),y,z+r*math.sin(angle)))
    tube('Gold carved scroll',points,.045,'gold')


def dial(loc, radius, cracked=False, hour=10, minute=10):
    x,y,z = loc
    if cracked:
        # Two closed, physically separated halves expose a real lightning crack.
        for side in (-1,1):
            arc = [(x+radius*math.cos(a),z+radius*math.sin(a))
                   for a in [(-math.pi/2 if side==1 else math.pi/2)+j*math.pi/16
                             for j in range(17)]]
            zig = [(x+side*.045+dx*radius,z+zz*radius)
                   for dx,zz in ((0,1),(.06,.55),(-.12,.27),(.09,-.08),
                                 (-.07,-.5),(0,-1))]
            if side == 1:
                poly = arc+zig
            else:
                poly = arc+list(reversed(zig))
            extrude('Split ivory clock dial',poly,.12,'clock_face',y,False)
    else:
        cylinder('Ivory clock dial',loc,radius,.12,'clock_face',32,(math.pi/2,0,0))
    ring('Brass clock bezel',(x,y+.04,z),radius*1.12,radius*.12,.16,'brass',32)
    for i in range(12):
        a = i*TAU/12
        box('Gold numeral block',(x+radius*.81*math.sin(a),y+.13,
                                 z+radius*.81*math.cos(a)),
            (.075,.065,.16 if i%3==0 else .10),'gold',(0,a,0))
    for angle,length,width in (((hour+minute/60)*TAU/12,radius*.52,.085),
                               (minute*TAU/60,radius*.73,.065)):
        rod('Iron frozen clock hand',(x,y+.21,z),
            (x+math.sin(angle)*length,y+.21,z+math.cos(angle)*length),
            width/2,'iron')
    cylinder('Clock axle',(x,y+.235,z),.10,.09,'brass',10,(math.pi/2,0,0))


def drip(loc, length=.65):
    x,y,z = loc
    rod('Molten gold attached root',(x,y-.23,z),(x,y,z),.065,'gold_drip')
    extrude('Sagging molten dial gold',
            [(x-.15,z+.07),(x+.16,z+.02),(x+.12,z-.19),
             (x+.07,z-length+.09),(x,z-length),(x-.06,z-length+.10),
             (x-.08,z-.26)],.095,'gold_drip',y)


def lantern(loc, scale=1):
    x,y,z = loc
    box('Lantern glowing bulb',(x,y,z),(.26*scale,.24*scale,.44*scale),'lantern_glow')
    for zz in (-.28,.28):
        box('Lantern iron cap',(x,y,z+zz*scale),(.40*scale,.36*scale,.10*scale),'iron')
    for xx in (-.17,.17):
        for yy in (-.15,.15):
            rod('Lantern cage',(x+xx*scale,y+yy*scale,z-.23*scale),
                (x+xx*scale,y+yy*scale,z+.23*scale),max(.02,.025*scale),'brass')
    ring('Lantern hanging loop',(x,y,z+.41*scale),.12*scale,.045,.07,'iron',12)


def boardwalk(width, front, cx=0, gate=False):
    # Exactly 1.2m along the complete +Y frontage. The gate has no threshold
    # spanning its passage: only the two side landings receive boards/steps.
    intervals = [(-width/2,width/2)] if not gate else [(-4.35,-2.25),(2.25,4.35)]
    for lo,hi in intervals:
        n = max(1,round((hi-lo)/.48))
        for i in range(n):
            x = cx+lo+(i+.5)*(hi-lo)/n
            box('Front boardwalk / 1.2m deep',(x,front+.60,.14),
                ((hi-lo)/n-.025,1.2,.16),WOOD[i%len(WOOD)])
        box('Boardwalk front fascia',(cx+(lo+hi)/2,front+1.18,.12),
            (hi-lo,.09,.22),'wood_dark')
    for side in (-1,1):
        if gate:
            x,sw = side*3.3,1.9
        else:
            if side == -1:
                continue
            x,sw = 0,2.1
        box('Lower entrance step',(x,front+1.47,.045),(sw,.36,.09),'wood_weathered')
        box('Upper entrance step',(x,front+1.28,.11),(sw,.30,.10),'plank')


def door(front, width=1.45, height=2.5, double=False):
    box('Entrance dark reveal',(0,front+.025,.22+height/2),
        (width+.16,.16,height+.08),'wood_dark')
    n = 8 if double else 5
    for i in range(n):
        x = (i+.5)*width/n-width/2
        box('Closed entrance door board',(x,front+.14,.22+height/2),
            (width/n-.018,.15,height-.04),WOOD[(i+2)%len(WOOD)])
    # The leaf and threshold remain square and walkable. Its oversized carved
    # surround curls softly without tilting or narrowing the straight entrance.
    pts=[]
    for j in range(9):
        t=j/8
        pts.append((-width/2-.15-.065*math.sin(t*math.pi),front+.23,.22+t*height))
    for j in range(1,13):
        t=j/12
        pts.append((-width/2-.15+t*(width+.30),front+.23,
                    .22+height+.16*math.sin(t*math.pi)+.045*math.sin(t*TAU)))
    for j in range(1,9):
        t=j/8
        pts.append((width/2+.15+.065*math.sin(t*math.pi),front+.23,.22+(1-t)*height))
    tube('Wobbly carved entrance surround',pts,.105,'wood_light',6)
    for side in (-1,1):
        for zz in (.75,height-.25):
            box('Iron strap hinge',(side*(width/2-.22),front+.245,zz),
                (.42,.07,.095),'iron')
            cylinder('Hinge pin',(side*(width/2-.045),front+.25,zz),.055,.22,'iron',8)
        if double or side == 1:
            knobx = side*(.14 if double else width*.29)
            cylinder('Brass knob backplate',(knobx,front+.25,1.35),.10,.055,'brass',10,
                     (math.pi/2,0,0))
            cylinder('Brass door knob',(knobx,front+.32,1.35),.075,.11,'brass',10,
                     (math.pi/2,0,0))
    if double:
        box('Barn centre stile',(0,front+.24,.22+height/2),(.085,.09,height),'iron')
        for side in (-1,1):
            rod('Barn diagonal door brace',(side*.12,front+.25,.42),
                (side*(width/2-.14),front+.25,height+.04),.07,'wood_light')



def window(label, u, y, z, width=1.3, height=1.55, shutters=False,
           trim='wood_light', side=0, tilt=0):
    """Rounded superellipse with uneven radii and curved, chunky mullions."""
    start=len(PARTS)
    outline=[]
    for j in range(28):
        a=j*TAU/28
        c,sn=math.cos(a),math.sin(a)
        r=1+.065*math.sin(3*a+.7)+.035*math.cos(5*a)
        outline.append((width/2*math.copysign(abs(c)**.55,c)*r,
                        height/2*math.copysign(abs(sn)**.55,sn)*r))
    extrude(label+' wobbly pane',outline,.10,'window_lit',0,False)
    points=[(x,.10,zz) for x,zz in outline]
    tube(label+' rounded frame',points+[points[0]],.085,trim,6)
    for axis in (0,1):
        points=[]
        for j in range(9):
            t=j/8-.5
            points.append((width*t if axis else .055*math.sin(t*TAU),.16,
                           height*t if not axis else .055*math.sin(t*TAU+.3)))
        tube(label+' curved mullion',points,.035,'wood_dark',5)
    if shutters:
        for sign in (-1,1):
            box(label+' tilted shutter',(sign*(width/2+.25),-.025,0),
                (.32,.13,height*.88),'paint_rose',(0,sign*.13,0))
    for obj in PARTS[start:]:
        transform(obj,(u,y,z),(0,tilt,side))


def mapped_block(label, loc, size, role, warp, segments=6):
    """Closed subdivided cuboid baked through a continuous deformation lattice."""
    vertices,faces,lookup=[],[],{}
    def v(i,j,k):
        key=(i,j,k)
        if key not in lookup:
            lookup[key]=len(vertices)
            vertices.append(warp((loc[0]+size[0]*(i-.5),
                                  loc[1]+size[1]*(j-.5),
                                  loc[2]+size[2]*(k/segments-.5))))
        return lookup[key]
    for k in range(segments):
        faces += [(v(0,0,k),v(1,0,k),v(1,0,k+1),v(0,0,k+1)),
                  (v(0,1,k+1),v(1,1,k+1),v(1,1,k),v(0,1,k)),
                  (v(0,0,k+1),v(0,1,k+1),v(0,1,k),v(0,0,k)),
                  (v(1,0,k),v(1,1,k),v(1,1,k+1),v(1,0,k+1))]
    faces += [(v(0,0,0),v(0,1,0),v(1,1,0),v(1,0,0)),
              (v(0,0,segments),v(1,0,segments),v(1,1,segments),v(0,1,segments))]
    return mesh(label,vertices,faces,role)


def deform_parts(start, warp):
    for obj in PARTS[start:]:
        for vertex in obj.data.vertices:
            vertex.co=warp(vertex.co)


def dream_volume(label, width, depth, height, center, bottom, yaw=0,
                 lean=.45, paint=None, holes=(), windows=(), ruin=False,
                 roof_role='roof_shingle', ground=False, passage_side=0):
    """Hollow planked room sharing a baked bend/taper/twist lattice.

    The straight door pocket fades into the bent facade. Upper rooms rotate
    independently; shingles and openings follow their walls, not flat planes.
    """
    cx,cy=center
    if ground:
        box(label+' straight stone footing',(cx,cy,.12),(width+.10,depth+.10,.24),'stone_dark')
        box(label+' ground floor',(cx,cy,.23),(width,depth,.14),'plank')
    phase=.55 if lean>0 else 2.1
    angle=math.radians(yaw)
    def warp(p):
        x,y,z=p
        t=max(0,min(1,z/height))
        doorway=1
        if ground and abs(x)<2.20 and y>depth/2-.35:
            doorway=max(0,(abs(x)-1.52)/.68)
        bulge=.105*math.sin(math.pi*t)-.055*t
        xx=x*(1+bulge)+lean*t*t
        yy=y*(1+.07*math.sin(math.pi*t))+.18*math.sin(math.pi*t+phase)*t
        twist=math.radians(5)*math.sin(t*math.pi/2)*(1 if lean>0 else -1)
        xx,yy=xx*math.cos(twist)-yy*math.sin(twist),xx*math.sin(twist)+yy*math.cos(twist)
        xx,yy=x+(xx-x)*doorway,y+(yy-y)*doorway
        zz=z+t*t*(.23*math.sin(x*.95+phase)+.16*math.sin(y*1.1)
                    -.20*(abs(x)/(width/2))**4)
        wx=cx+xx*math.cos(angle)-yy*math.sin(angle)
        if passage_side:
            # Smooth outward knee: the clock may bow but cannot enter the lane.
            wx=passage_side*(2.29+.08*math.log1p(math.exp((passage_side*wx-2.29)/.08)))
        return (wx,cy+xx*math.sin(angle)+yy*math.cos(angle),bottom+zz)
    def wall(length, origin, rotation, cuts, frontwall=False):
        n=math.ceil(length/.46)
        pitch=length/n
        c,sn=math.cos(rotation),math.sin(rotation)
        def wallwarp(p):
            x,y,z=p
            return warp((origin[0]+x*c-y*sn,origin[1]+x*sn+y*c,z))
        for i in range(n):
            u=-length/2+(i+.5)*pitch
            top=height
            if ruin and frontwall and i>=n-3:
                top-=(.32,.73,.48)[i-(n-3)]
            rectangles=[(u-pitch/2+.007,u+pitch/2-.007,0,top)]
            for hu,hz,hw,hh in cuts:
                result=[]
                for l,r,a,b in rectangles:
                    left,right=max(l,hu-hw/2),min(r,hu+hw/2)
                    low,high=max(a,hz),min(b,hz+hh)
                    if left>=right or low>=high:
                        result.append((l,r,a,b))
                    else:
                        result += [(l,left,a,b),(right,r,a,b),
                                   (left,right,a,low),(left,right,high,b)]
                rectangles=[q for q in result if q[1]-q[0]>.045 and q[3]-q[2]>.045]
            for l,r,a,b in rectangles:
                role=paint if paint and i%3 else WOOD[i%5]
                mapped_block(label+' bowed plank',((l+r)/2,0,(a+b)/2),
                             (r-l,.16,b-a),role,wallwarp)
    allholes=list(holes)+[(x,z-h/2,w*1.06,h*1.09) for x,z,w,h,*_ in windows]
    wall(width,(0,depth/2),0,allholes,True)
    wall(width,(0,-depth/2),math.pi,())
    for side in (-1,1):
        wall(depth,(side*width/2,0),-side*math.pi/2,())
    for x in (-width/2,width/2):
        for y in (-depth/2,depth/2):
            mapped_block(label+' bent corner post',(x,y,height/2),(.23,.23,height),
                         'wood_dark',warp,9)
    for zz in (.12,height-.08):
        for y in (-depth/2-.06,depth/2+.06):
            tube(label+' wax cornice',[warp((-width/2+i*width/18,y,zz)) for i in range(19)],
                 .105,'cloth_cream',6)
        for x in (-width/2,width/2):
            tube(label+' side cornice',[warp((x,-depth/2+i*depth/12,zz)) for i in range(13)],
                 .105,'wood_light',6)
    for index,item in enumerate(windows):
        x,z,w,h,*rest=item
        start=len(PARTS)
        window(label+' uneven window',x,depth/2+.08,z,w,h,trim='cloth_cream',
               tilt=rest[0] if rest else (-.08 if index%2 else .09))
        deform_parts(start,warp)
    cols,rows=max(3,math.ceil(width/.75)),max(3,math.ceil(depth/.8))
    for i in range(cols):
        for j in range(rows):
            if ruin and i>=cols-2 and j>=rows-2 and (i+j)%2==0:
                continue
            x=-width/2+(i+.5)*width/cols
            y=-depth/2+(j+.5)*depth/rows
            mapped_block(label+' melted roof shingle',(x,y,height+.035),
                (width/cols+.045,depth/rows+.045,.16),
                roof_role if (i+j)%5 else 'rust',warp,1)
    if ruin:
        for i in range(3):
            x=width/2-.7-i*.24
            a=warp((x,depth/2-.10,height-.15))
            b=warp((x-.20,depth/2-.12,height+.25+.13*(i%2)))
            snapped_plank(label+' attached roof splinter',a,b,.16,.13)
    return warp


def false_front(warp, width, height, paint):
    start=len(PARTS)
    for i in range(11):
        x=-width/2+(i+.5)*width/11
        top=height+.32+.55*(1-(x/(width/2))**2)+.13*math.sin(i*1.2)
        if i>=9:
            top-=.38
        box('Wavering broken western false front',(x,0,(height-.18+top)/2),
            (width/11-.015,.18,top-height+.18),paint)
    deform_parts(start,warp)


def balcony(warp, width, depth):
    start=len(PARTS)
    for i in range(13):
        x=-width/2+(i+.5)*width/13
        box('Cantilever balcony deck',(x,depth/2+.47,.04),(width/13-.012,.99,.15),WOOD[i%5])
    for i in range(11):
        x=-width/2+i*width/10
        box('Wobbly balcony spindle',(x,depth/2+.94,.51),(.09,.10,.88),'wood_light')
    tube('Sagging balcony handrail',[(-width/2+i*width/16,depth/2+.94,
          1.02-.13*math.cos(i*math.pi/8)) for i in range(17)],.075,'wood_dark',6)
    for x in (-width/2+.12,width/2-.12):
        rod('Balcony cantilever knee',(x,depth/2,-.62),(x,depth/2+.76,-.02),.095,'wood_dark')
    deform_parts(start,warp)


def chimney(x, y, bottom, top, radius=.42, curl=.58):
    """Overlapping masonry courses spiral around a continuous curling core."""
    def spine(t):
        a=t*TAU*1.12
        return Vector((x+curl*(math.cos(a)-1)*t,
                       y+curl*math.sin(a)*t,bottom+(top-bottom)*t))
    tube('Corkscrew continuous flue',[tuple(spine(i/32)) for i in range(33)],radius*.81,'stone_dark',8)
    rows=math.ceil((top-bottom)/.28)
    for i in range(rows):
        t=(i+.5)/rows
        center=spine(t)
        tangent=(spine(min(1,t+.01))-spine(max(0,t-.01))).normalized()
        quat=tangent.to_track_quat('Z','Y')
        for j in range(4):
            a=j*TAU/4+i*.19
            offset=quat @ Vector((radius*.58*math.cos(a),radius*.58*math.sin(a),0))
            obj=box('Helical staggered brick',tuple(center+offset),
                    (radius*.99,radius*.90,(top-bottom)/rows+.04),
                    ('stone','stone_dark','rust')[(i+j)%3],(0,0,a))
            obj.data.transform(Matrix.Translation(center+offset) @ quat.to_matrix().to_4x4() @
                               Matrix.Translation(-(center+offset)))
    end=spine(1)
    tangent=(spine(1)-spine(.99)).normalized()
    lip=ring('Open rust corkscrew lip',(0,0,0),radius*1.14,.13,.19,'rust',16)
    transform(lip,tuple(end),tangent.to_track_quat('Y','Z').to_euler())


def inn():
    f=2.4
    boardwalk(10.4,f)
    dream_volume('Inn ground saloon',9.8,6.20,3.40,(0,-.70),.22,
        lean=.45,holes=((0,0,1.65,2.67),),
        windows=((-3.15,1.88,1.5,1.65),(3.15,1.94,1.30,1.85)),ground=True)
    upper=dream_volume('Inn tilted second floor',7.45,4.70,2.68,(-.75,-.77),3.48,
        yaw=11,lean=-.53,paint='paint_rose',
        windows=((-2.40,1.33,1.25,1.57),(-.30,1.45,1.05,1.40),(1.80,1.30,1.20,1.65)))
    balcony(upper,7.15,4.70)
    attic=dream_volume('Inn cantilever cuckoo loft',3.90,3.38,1.66,(.62,-1.08),6.07,
        yaw=-12,lean=.42,paint='wood_weathered',windows=((-1.0,.80,.72,.92),),ruin=True)
    false_front(lambda p: attic((p[0],p[1]+1.75,p[2])),3.9,1.52,'paint_rose')
    start=len(PARTS)
    extrude('Cuckoo house crest',[(-.70,.86),(.70,.86),(.70,1.82),(0,2.32),(-.70,1.82)],
            .30,'wood_dark',1.92)
    for side in (-1,1):
        rod('Cuckoo gold rake',(side*.79,2.13,1.80),(0,2.13,2.38),.07,'gold')
        box('Cuckoo tiny door',(side*.13,2.13,1.88),(.22,.08,.29),'wood_light')
        rod('Attached cuckoo weight chain',(side*.55,2.18,1.0),(side*.55,2.18,.24),.025,'brass')
        lathe('Cuckoo pine cone weight',(side*.55,2.18,.19),[(0,.07),(.16,.16),(.35,.07)],'wood_dark',8)
    dial((0,2.12,1.20),.39,hour=6,minute=30)
    deform_parts(start,attic)
    door(f)
    porch(f,2.35,3.05)
    for x in (-1.06,1.06):
        lantern((x,f+.40,2.55),.70)
    chimney(-2.75,-1.30,5.25,10.45,.43,.63)


def gunshop():
    f=1.9
    boardwalk(8.5,f)
    dream_volume('Gunshop swollen storefront',8.0,5.10,3.58,(0,-.65),.22,
        lean=-.36,holes=((0,0,1.65,2.67),),
        windows=((-2.60,1.74,2.10,1.80),(2.65,1.95,1.12,1.52)),ground=True,roof_role='roof_tin')
    upper=dream_volume('Gunshop offset gun room',5.0,3.70,2.10,(-.92,-.65),3.65,
        yaw=-12,lean=.48,paint='wood_light',windows=((-1.45,1.04,1.0,1.2),(1.30,1.0,.85,1.15)),
        ruin=True,roof_role='roof_tin')
    false_front(lambda p: upper((p[0],p[1]+1.91,p[2])),5.0,1.96,'wood_light')
    for i in range(14):
        x=-3.9+(i+.5)*7.8/14
        z=3.35+.16*math.sin(x*1.2)
        role='canvas' if i%2==0 else 'cloth_red'
        box('Striped awning canopy',(x,f+.67,z),(7.8/14+.015,1.35,.09),role,(-.12,0,0))
        extrude('Awning scalloped valance',[(x-.29,z-.09),(x+.29,z-.09),
            (x+.29,z-.30),(x+.13,z-.40),(x-.12,z-.43),(x-.29,z-.30)],.09,role,f+1.29,False)
    for x in (-3.80,3.80):
        box('Awning grounded iron post',(x,f+1.1,1.65),(.12,.12,2.9),'iron')
    rod('Gun sign cantilever mounting spar',upper((1.30,1.85,1.48)),(.80,f+.1,5.10),.09,'wood_dark')
    sy,sz=sign_bracket(.80,f-.02,5.28,.70)
    # Cream relief plaque separates the iron barrel from the weathered wall.
    outline=[(-1.34,-.91),(1.19,-.84),(1.37,-.48),(1.29,.66),
             (.84,.82),(-1.19,.74),(-1.43,.36)]
    start=len(PARTS)
    extrude('Gunshop wobbly cream sign plaque',outline,.18,'cloth_cream')
    tube('Gunshop gilt sign border',[(x,.12,z) for x,z in outline]+[(outline[0][0],.12,outline[0][1])],
         .055,'gold',6)
    revolver_sign(.16,.19,.38)
    for obj in PARTS[start:]:
        transform(obj,(.55,sy,4.73),(0,-.07,0))
    for i in range(3):
        rifle((-3.17+i*.54,f+.39,1.66),.84)
    box('Gun display rack',(-2.63,f+.44,1.35),(1.95,.16,.12),'wood_light')
    door(f)
    lantern((1.02,f+.40,2.52),.70)
    chimney(-2.30,-1.15,4.65,9.35,.40,.63)


def ammoworkshop():
    f=2.4
    boardwalk(8.6,f)
    dream_volume('Workshop bent barn',8.05,6.22,3.88,(0,-.71),.22,
        lean=.42,holes=((0,0,2.86,3.16),),windows=((-2.92,1.97,1.12,1.45),),
        ground=True,roof_role='roof_tin')
    upper=dream_volume('Workshop skewed forge loft',5.10,4.12,2.13,(-.87,-.92),4.0,
        yaw=13,lean=-.50,paint='wood_weathered',windows=((-1.25,1.10,1.0,1.2),),
        ruin=True,roof_role='roof_tin')
    false_front(lambda p: upper((p[0],p[1]+2.12,p[2])),5.1,1.95,'wood_weathered')
    dream_volume('Workshop projecting side room',2.30,2.82,1.50,(2.02,-1.24),3.55,
        yaw=-11,lean=.24,paint='paint_rose',windows=((0,.75,.86,.93),),roof_role='roof_tin')
    door(f,2.65,3.0,True)
    porch(f,3.18,3.62)
    for zz in (4.25,5.80):
        rod('Hourglass sign wall spar',upper((.65,2.06,zz-4.0)),(0,f-.20,zz),.10,'wood_dark')
    box('Hourglass sign carved backing',(0,f-.20,5.06),(1.65,.20,2.40),'wood_dark')
    hourglass((0,f+.16,5.05),.88)
    crate((-2.36,f+.56,.22))
    crate((2.33,f+.56,.22))
    ax,ay=3.10,.60
    box('Anvil timber block',(ax,ay,.65),(.72,.68,1.0),'wood_dark')
    extrude('Forge iron anvil',[(ax-.50,1.13),(ax+.50,1.13),(ax+.23,1.42),
        (ax+.36,1.63),(ax+.80,1.86),(ax-.60,1.86),(ax-.26,1.60),(ax-.20,1.38)],.48,'iron',ay)
    lantern((-1.69,f+.40,2.85),.70)
    chimney(-2.24,-1.24,4.25,10.20,.53,.76)


def wardrobe():
    f=1.9
    boardwalk(7.5,f)
    dream_volume('Tailor bowed sage shop',7.15,5.18,3.65,(0,-.69),.22,
        lean=-.32,paint='paint_sage',holes=((0,0,1.61,2.67),(-2.22,.60,2.45,2.38)),
        windows=((2.37,1.97,1.0,1.45),),ground=True)
    upper=dream_volume('Tailor offset sewing room',4.6,3.70,2.16,(.56,-.83),3.58,
        yaw=12,lean=.44,paint='paint_rose',windows=((-1.26,1.10,1.0,1.30),(1.20,1.0,.78,1.15)),ruin=True)
    balcony(upper,4.38,3.70)
    false_front(lambda p: upper((p[0],p[1]+1.91,p[2])),4.6,2.0,'paint_sage')
    x=-2.20
    plan=[(-1.19,0),(-1.19,.36),(-.88,.83),(.88,.83),(1.19,.36),(1.19,0)]
    def baywarp(p):
        xx,yy,zz=p
        return (xx+.07*math.sin(zz*1.6),yy+.06*math.sin(zz*1.9),zz+.04*math.sin(xx*2))
    start=len(PARTS)
    for z in (.78,3.40):
        n=len(plan)
        verts=[(x+u,f+v,z+dz) for dz in (-.10,.10) for u,v in plan]
        faces=[tuple(reversed(range(n))),tuple(range(n,2*n))]
        faces += [(i,(i+1)%n,(i+1)%n+n,i+n) for i in range(n)]
        mesh('Bay bowed plinth canopy',verts,faces,'cloth_cream')
    box('Bay warm backdrop',(x,f+.07,2.05),(2.24,.09,2.45),'window_lit')
    for u,v in plan[1:-1]:
        tube('Bay soft crooked mullion',[(x+u+.05*math.sin(j*.7),f+v,.83+j*2.55/8)
             for j in range(9)],.065,'cloth_cream',6)
    for side in (-1,1):
        box('Bay angled glass',(x+side*1.03,f+.59,2.08),(.07,.58,2.4),'glass',
            (0,0,side*math.radians(34)))
    tube('Bay sagging display rail',[(x-1.14+i*2.28/12,f+.85,1.03+.08*math.sin(i*.4))
         for i in range(13)],.065,'cloth_cream',6)
    deform_parts(start,baywarp)
    mannequin((x-.43,f+.48,.88),'cloth_cream')
    mannequin((x+.43,f+.48,.88),'cloth_red')
    sy,sz=sign_bracket(2.46,f,3.34,.78)
    for zz in (-.42,.42):
        cylinder('Spool broad wooden flange',(2.46,sy,sz+zz),.44,.13,'wood_light',12)
    cylinder('Spool red thread',(2.46,sy,sz),.30,.76,'cloth_red',12)
    for j in range(5):
        cylinder('Spool winding band',(2.46,sy,sz-.29+j*.145),.33,.05,'cloth_red',12)
    rod('Spool sewing needle',(2.12,sy+.31,sz-.53),(2.88,sy+.31,sz+.72),.035,'iron')
    ring('Needle eye',(2.86,sy+.31,sz+.69),.09,.04,.06,'iron',12)
    door(f,1.4)
    porch(f,2.0,3.03)
    lantern((.97,f+.38,2.50),.65)
    chimney(1.76,-1.57,4.70,9.38,.39,.62)


def timegate():
    # Four stacked clock volumes. All bridging geometry is above Z=6.
    # Lower inner edges stay outside +/-2.25: 4.5m of completely empty passage.
    for side in (-1,1):
        case_start=len(PARTS)
        x=side*3.29
        box('Clock stone plinth',(x,-.20,.19),(2.04,2.60,.38),'stone_dark')
        lower=dream_volume('Gate lower grandfather case',1.72,2.18,3.53,(x,-.20),.38,
            lean=side*.16,paint='wood_weathered',windows=((0,1.77,.92,2.57),),
            roof_role='roof_tin',passage_side=side)
        upper=dream_volume('Gate shifted clock chamber',1.68,1.96,2.12,(x+side*.15,-.32),3.78,
            yaw=-side*10,lean=-side*.18,paint='wood_light',ruin=True,passage_side=side)
        start=len(PARTS)
        dial((0,1.10,1.12),.48,hour=11,minute=59)
        extrude('Grandfather pointed hood',[(-.85,1.91),(.85,1.91),(.67,2.34),
                (0,2.75),(-.80,2.29)],.20,'wood_dark',.92)
        for ss in (-1,1):
            rod('Clock hood gilt rake',(ss*.82,1.08,2.22),(0,1.08,2.76),.065,'gold')
        deform_parts(start,upper)
        start=len(PARTS)
        rod('Lantern pendulum shaft',(0,1.20,2.89),(0,1.20,.94),.045,'brass')
        lantern((0,1.25,.96),1.0)
        ring('Pendulum brass halo',(0,1.28,.96),.43,.065,.10,'brass',20)
        deform_parts(start,lower)
        box('Clock side landing canopy',(x,.99,3.46),(1.90,.62,.15),'roof_tin')
        # The cornice tube has thickness beyond its centreline. Contain its
        # complete surface, including the .105m round molding, outside the lane.
        for obj in PARTS[case_start:]:
            for v in obj.data.vertices:
                a=side*v.co.x
                v.co.x=side*(2.28+.055*math.log1p(math.exp((a-2.28)/.055)))
    boardwalk(8.7,.80,gate=True)
    points=[]
    for i in range(33):
        t=i/32
        xx=-3.30+6.60*t
        zz=6.13+1.72*math.sin(t*math.pi)+.16*math.sin(t*TAU)
        points.append((xx,-.26,zz))
    tube('Gate bent arch backbone',points,.29,'wood_dark',8)
    tube('Gate gold inner arch molding',[(x,.50,z-.14) for x,y,z in points],.08,'gold',6)
    for i in range(21):
        t=(i+.5)/21
        xx=-3.30+6.60*t
        zz=6.13+1.72*math.sin(t*math.pi)+.16*math.sin(t*TAU)
        tall=.45+(.08,.31,.17,.36)[i%4]
        box('Gate ragged crest timber',(xx,-.24,zz+tall/2),(.34,1.44,tall+.25),
            WOOD[i%5],(0,-.35*math.cos(t*math.pi),0))
    dial((0,.70,8.38),1.24,True,hour=11,minute=59)
    for x,length in ((-.72,.58),(-.20,.89),(.42,.70),(.82,.40)):
        zz=8.38-math.sqrt(max(0,1.24**2-x*x))
        drip((x,.94,zz+.13),length)
    chimney(-3.20,-.55,5.28,10.60,.36,.45)


def snapped_plank(label, a, b, width=.23, depth=.15, role='wood_light'):
    """A capped, chunky splinter; its unbroken end is embedded in a support."""
    a,b = Vector(a),Vector(b)
    length = (b-a).length
    obj = extrude(label,[(-width/2,0),(width/2,0),(width/2,length-.16),
                        (width*.12,length-.16),(width*.12,length),
                        (-width*.14,length-.08),(-width/2,length-.08)],
                  depth,role)
    obj.data.transform(Matrix.Translation(a) @
                       (b-a).to_track_quat('Z','Y').to_matrix().to_4x4())
    return obj


def porch(front, width=2.5, height=3.05):
    box('Little entrance awning',(0,front+.63,height),(width,1.30,.16),
        'roof_shingle',(-math.radians(7),0,0))
    box('Porch carved front fascia',(0,front+1.23,height-.10),(width,.14,.24),'wood_light')
    for side in (-1,1):
        x = side*(width/2-.14)
        box('Porch post',(x,front+1.13,(height+.22)/2),(.13,.14,height-.22),'wood_dark')
        rod('Porch angled bracket',(x,front+1.12,height-.65),
            (x,front+.67,height-.10),.065,'wood_light')


def sign_bracket(x, front, z, reach=1):
    box('Iron hanging sign wall plate',(x,front+.13,z-.22),(.16,.13,.74),'iron')
    rod('Iron sign bracket',(x,front+.16,z),(x,front+reach,z),.055,'iron')
    rod('Iron bracket diagonal',(x,front+.18,z-.46),(x,front+reach*.80,z),.045,'iron')
    for yy in (front+reach*.5,front+reach*.91):
        rod('Hanging sign chain',(x,yy,z-.03),(x,yy,z-.37),.025,'iron')
    return front+reach*.75,z-.60


def rifle(loc, scale=1):
    x,y,z = loc
    box('Rifle walnut stock',(x-.10*scale,y,z-.42*scale),
        (.16*scale,.12,.46*scale),'wood_dark',(0,-.15,0))
    box('Rifle fore stock',(x,y,z),(.13*scale,.13,.55*scale),'wood_dark')
    rod('Rifle iron barrel',(x,y,z-.1*scale),(x,y,z+.85*scale),.035*scale,'iron')
    box('Rifle receiver',(x,y+.045,z-.18*scale),(.22*scale,.09,.17*scale),'iron')
    ring('Rifle trigger guard',(x-.02*scale,y+.09,z-.33*scale),.095*scale,.04,.06,'iron',10)


def revolver_sign(x, y, z):
    # True revolver silhouette with barrel, six-shot cylinder and angled grip.
    poly = [(-1.18,.25),(.42,.25),(.60,.12),(.63,-.10),(.42,-.25),
            (.62,-.88),(.19,-1.0),(-.04,-.28),(-.44,-.19),(-.44,.04),(-1.18,.04)]
    extrude('Oversize revolver iron silhouette',[(x+a,z+c) for a,c in poly],
            .20,'iron',y)
    extrude('Revolver wood grip',[(x+.12,z-.24),(x+.40,z-.27),
            (x+.55,z-.82),(x+.25,z-.90)],.245,'wood',y)
    box('Revolver long barrel',(x-.54,y,z+.16),(1.44,.25,.19),'iron')
    box('Revolver gold barrel inlay',(x-.54,y+.15,z+.18),(1.32,.055,.045),'gold')
    cylinder('Revolver fluted cylinder',(x-.13,y+.10,z-.005),.265,.20,'iron',12,(math.pi/2,0,0))
    for i in range(6):
        a = i*TAU/6
        cylinder('Revolver cylinder brass chamber',(x-.13+.17*math.cos(a),y+.22,z+.17*math.sin(a)),
                 .045,.04,'gold',6,(math.pi/2,0,0))
    ring('Revolver gold trigger guard',(x+.19,y+.13,z-.30),.22,.055,.10,'gold',16)
    box('Revolver hammer',(x+.46,y,z+.22),(.17,.15,.19),'iron',(0,.25,0))
    cylinder('Grip gold rivet',(x+.34,y+.15,z-.60),.065,.05,'gold',8,(math.pi/2,0,0))


def bullet(loc, scale=1):
    lathe('Brass cartridge',loc,[(0,.095*scale),(.07*scale,.115*scale),
          (.14*scale,.095*scale),(.48*scale,.095*scale),
          (.60*scale,.07*scale),(.72*scale,max(.020,.022*scale))],'brass',8)


def crate(loc):
    x,y,z = loc
    box('Cartridge crate dark interior',(x,y,z+.30),(.85,.67,.54),'wood_dark')
    for zz in (z+.14,z+.47):
        for yy in (y-.36,y+.36):
            box('Crate slat',(x,yy,zz),(.97,.10,.22),'wood_light')
        for xx in (x-.45,x+.45):
            box('Crate end slat',(xx,y,zz),(.10,.70,.22),'wood_weathered')
    for xx in (-.31,.31):
        box('Crate iron band',(x+xx,y+.42,z+.32),(.065,.06,.59),'iron')
    for j in range(2):
        for i in range(4):
            bullet((x+(i-1.5)*.20,y+(j-.5)*.24,z+.46),.65)


def hourglass(loc, scale=1):
    x,y,z = loc
    for dz in (-.9,.9):
        obj = ring('Hourglass open brass end ring',(0,0,0),.62*scale,
                   .105*scale,.13,'brass',20)
        transform(obj,(x,y,z+dz*scale),(math.pi/2,0,0))
    for xx in (-.49,.49):
        for yy in (-.25,.25):
            rod('Hourglass brass column',(x+xx*scale,y+yy*scale,z-.86*scale),
                (x+xx*scale,y+yy*scale,z+.86*scale),.045*scale,'brass')
    # Separate rear glass half-bulbs leave a real open front cutaway so the
    # sand remains visible even with the required opaque grey Principled BSDF.
    for side in (-1,1):
        rings = [(side*v*scale,r*scale) for v,r in
                 ((.03,.06),(.25,.25),(.57,.44),(.78,.40))]
        rings.sort()
        count = 10
        verts = [(x+r*math.cos(math.pi+j*math.pi/count),
                  y+r*math.sin(math.pi+j*math.pi/count),z+zz)
                 for zz,r in rings for j in range(count+1)]
        # Give the half glass shell .045m thickness with Blender solidify.
        faces = [(k*(count+1)+j,k*(count+1)+j+1,(k+1)*(count+1)+j+1,
                  (k+1)*(count+1)+j) for k in range(len(rings)-1) for j in range(count)]
        obj = mesh('Hourglass rear glass bulb',verts,faces,'glass',False)
        mod = obj.modifiers.new('Thick glass cutaway','SOLIDIFY')
        mod.thickness = .045
    lathe('Upper frozen sand',(x,y+.015,z),[(.17*scale,.05*scale),
          (.53*scale,.29*scale),(.66*scale,.30*scale)],'sand_glow',12,bevel=False)
    lathe('Lower sand mound',(x,y+.015,z),[(-.77*scale,.35*scale),
          (-.67*scale,.29*scale),(-.40*scale,.025*scale)],'sand_glow',12,bevel=False)
    rod('Frozen hourglass sand stream',(x,y+.04,z-.40*scale),
        (x,y+.04,z+.17*scale),.026*scale,'sand_glow')


def mannequin(loc, role):
    x,y,z = loc
    cylinder('Dress form wood foot',(x,y,z+.05),.31,.10,'wood',10)
    rod('Dress form wooden stand',(x,y,z+.1),(x,y,z+.64),.055,'wood')
    lathe('Tailored dress-form silhouette',(x,y,z+.54),
          [(0,.38),(.18,.34),(.48,.18),(.69,.23),(.89,.32),
           (1.04,.21),(1.10,.09),(1.24,.09)],role,12,bevel=False)
    cylinder('Mannequin neck cap',(x,y,z+1.81),.12,.07,'wood',10)
    ring('Dress form waist tape',(x,y+.23,z+1.06),.20,.045,.06,'gold',12)


SPECS = (
    ('Inn',(11,8,10.70),inn),
    ('GunShop',(9,7,9.60),gunshop),
    ('AmmoWorkshop',(9,8,10.45),ammoworkshop),
    ('Wardrobe',(8,7,9.63),wardrobe),
    ('TimeGate',(9,4,10.85),timegate),
)


def triangles(obj):
    obj.data.calc_loop_triangles()
    return len(obj.data.loop_triangles)


def bounds(obj):
    return (Vector(tuple(min(v.co[i] for v in obj.data.vertices) for i in range(3))),
            Vector(tuple(max(v.co[i] for v in obj.data.vertices) for i in range(3))))


def material_coverage(obj):
    """Occlusion-aware projected visible area from the 55-degree review view."""
    obj.data.calc_loop_triangles()
    verts = [v.co.copy() for v in obj.data.vertices]
    tris = list(obj.data.loop_triangles)
    tree = BVHTree.FromPolygons(verts,[tuple(t.vertices) for t in tris],all_triangles=True)
    view = Vector((0,math.cos(math.radians(55)),math.sin(math.radians(55))))
    distance = max(obj.dimensions)*3
    areas = {}
    for index,t in enumerate(tris):
        facing = t.normal.dot(view)
        if facing <= 0:
            continue
        center = sum((verts[i] for i in t.vertices),Vector())/3
        _,_,hit,_ = tree.ray_cast(center+view*distance,-view,distance+.01)
        if hit == index:
            role = obj.data.materials[t.material_index].name
            areas[role] = areas.get(role,0)+t.area*facing
    total = sum(areas.values())
    assert total > 0,obj.name
    return {role:area/total for role,area in areas.items()}


def check_gate_passage(obj):
    """No triangle may cross the walking envelope, including disconnected FX."""
    obj.data.calc_loop_triangles()
    limits=((0,-2.249,1),(0,2.249,-1),(1,-3.0,1),(1,3.0,-1),
            (2,.025,1),(2,5.995,-1))
    for tri in obj.data.loop_triangles:
        poly=[obj.data.vertices[i].co.copy() for i in tri.vertices]
        for axis,edge,sign in limits:
            if not poly:
                break
            clipped=[]
            for a,b in zip(poly,poly[1:]+poly[:1]):
                da,db=sign*(a[axis]-edge),sign*(b[axis]-edge)
                if da>=0:
                    clipped.append(a)
                if (da>=0)!=(db>=0):
                    clipped.append(a+(b-a)*(da/(da-db)))
            poly=clipped
        if len(poly)>=3:
            area=sum(((poly[j]-poly[0]).cross(poly[j+1]-poly[0])).length
                     for j in range(1,len(poly)-1))
            assert area<1e-8,('Triangle inside gate passage',tri.index,poly)
    # Check a densely sampled ray grid through the full gate depth.
    tree = BVHTree.FromObject(obj,bpy.context.evaluated_depsgraph_get())
    for i in range(29):
        x = -2.249+i*4.498/28
        for j in range(39):
            z = .025+j*5.97/38
            hit,_,_,_ = tree.ray_cast(Vector((x,3,z)),Vector((0,-1,0)),6)
            assert hit is None,('Blocked gate passage',x,z,hit)


def contain_footprint(target):
    xhalf,depth=target[0]/2,target[1]
    back=-1.50 if depth==4 else -depth/2
    protected=('Front boardwalk','Boardwalk front','Lower entrance','Upper entrance',
               'Closed entrance','Entrance dark','Wobbly carved entrance','Iron strap',
               'Hinge pin','Brass knob','Brass door','Barn centre','Barn diagonal')
    # A soft knee keeps the footprint fixed without changing the central door.
    verts=[v for obj in PARTS if not obj.name.startswith(protected) for v in obj.data.vertices]
    xmin=min(v.co.x for v in verts); xmax=max(v.co.x for v in verts)
    ymin=min(v.co.y for v in verts)
    for v in verts:
        for sign,extent in ((-1,-xmin),(1,xmax)):
            val=sign*v.co.x
            edge=xhalf*.76
            if val>edge and extent>xhalf-.05:
                t=(val-edge)/(extent-edge)
                v.co.x=sign*(edge+(xhalf-.06-edge)*(t*.82+.18*math.sin(t*math.pi/2)))
        if v.co.y<back+.7 and ymin<back+.04:
            t=(back+.7-v.co.y)/(back+.7-ymin)
            v.co.y=back+.7-(.66)*(t*.82+.18*math.sin(t*math.pi/2))


def finish(name, target):
    bpy.ops.object.select_all(action='DESELECT')
    # Apply the identical bevel on all rigid parts before joining. A collapse
    # pass removes excess bevel tessellation while respecting material borders.
    for obj in PARTS:
        obj.select_set(True)
        bpy.context.view_layer.objects.active = obj
        for mod in list(obj.modifiers):
            bpy.ops.object.modifier_apply(modifier=mod.name)
        obj.select_set(False)
    for obj in PARTS:
        obj.select_set(True)
    bpy.context.view_layer.objects.active = PARTS[0]
    bpy.ops.object.join()
    obj = bpy.context.object
    obj.name,obj.data.name = name,name+'_Mesh'
    # Recalculate first, so decimation works consistently on closed submeshes.
    bm = bmesh.new()
    bm.from_mesh(obj.data)
    bmesh.ops.recalc_face_normals(bm,faces=list(bm.faces))
    assert all(e.is_manifold for e in bm.edges),(name,'open authored mesh')
    bm.to_mesh(obj.data)
    bm.free()
    raw = triangles(obj)
    if raw > 15850:
        mod = obj.modifiers.new('Building 16000 triangle budget','DECIMATE')
        mod.ratio,mod.delimit = 15650/raw,{'MATERIAL'}
        mod.use_collapse_triangulate = True
        bpy.ops.object.modifier_apply(modifier=mod.name)
    assert triangles(obj)<=16000,(name,triangles(obj))
    bm = bmesh.new()
    bm.from_mesh(obj.data)
    bmesh.ops.recalc_face_normals(bm,faces=list(bm.faces))
    assert all(e.is_manifold for e in bm.edges),(name,'open mesh edge')
    bm.to_mesh(obj.data)
    bm.free()
    obj.data.update()
    bpy.context.view_layer.update()
    lo,hi = bounds(obj)
    # Footprint origin is authored at (0,0,0); do not recentre or
    # nonuniformly scale the 1.2m boardwalk / 4.5m gate passage.
    assert obj.location.length < 1e-6,name
    assert abs(lo.z)<.025 and 9.0 < hi.z <= 11.0,(name,lo,hi)
    assert all(target[i]-.35 < obj.dimensions[i] <= target[i]+.04 for i in range(2)),(name,obj.dimensions)
    assert all(math.isfinite(c) for v in obj.data.vertices for c in v.co),name
    assert all(mat.name in ROLES for mat in obj.data.materials),name
    for mat in obj.data.materials:
        assert tuple(mat.diffuse_color)==(.5,.5,.5,1)
        shader = mat.node_tree.nodes['Principled BSDF']
        assert tuple(shader.inputs['Base Color'].default_value)==(.5,.5,.5,1)
        assert not any(n.type.startswith('TEX_') for n in mat.node_tree.nodes)
    coverage = material_coverage(obj)
    assert sum(v>.002 for v in coverage.values())>=6,(name,coverage)
    assert max(coverage.values())<=.55,(name,'material dominance',coverage)
    if name=='TimeGate':
        check_gate_passage(obj)
    obj['front'] = '+Y'
    obj['origin'] = 'Bottom centre of building footprint, metres'
    obj['approx_size_m'] = list(target)
    obj['triangle_count'] = triangles(obj)
    obj['stacked_volumes'] = {'Inn':3,'GunShop':2,'AmmoWorkshop':3,'Wardrobe':2,'TimeGate':4}[name]
    obj['geometry_style'] = 'Baked continuous bend / taper / twist, attached roof ruins'
    obj['tallest_point'] = 'Connected corkscrew masonry chimney'
    obj['vanishing_layers'] = 0
    obj['walkthrough'] = name=='TimeGate'
    obj['boardwalk_depth_m'] = 1.2
    destination = OUTPUT / ('Kit_V_'+name+'.fbx')
    bpy.ops.export_scene.fbx(
        filepath=str(destination),use_selection=True,
        axis_forward='-Z',axis_up='Y',apply_scale_options='FBX_SCALE_UNITS',
        bake_space_transform=True,object_types={'MESH'},use_mesh_modifiers=True,
        mesh_smooth_type='FACE',add_leaf_bones=False,bake_anim=False,
    )
    assert destination.is_file() and destination.stat().st_size>1024,destination
    print('VISIBLE '+name+' '+str({k:round(v,3) for k,v in
          sorted(coverage.items(),key=lambda item:-item[1])}),flush=True)
    print(f'EXPORTED {name} {triangles(obj)} {tuple(round(d,4) for d in obj.dimensions)}',flush=True)
    return obj


def preview(objects):
    """Only viewport colours change, strictly after the final neutral FBX."""
    scene = bpy.context.scene
    scene.render.engine = 'BLENDER_WORKBENCH'
    scene.render.resolution_x,scene.render.resolution_y = 1600,900
    scene.render.resolution_percentage = 100
    scene.render.image_settings.file_format = 'PNG'
    scene.render.filepath = str(PREVIEW)
    scene.render.film_transparent = False
    scene.render.use_file_extension = True
    scene.view_settings.view_transform = 'Standard'
    shading = scene.display.shading
    shading.light,shading.color_type = 'FLAT','MATERIAL'
    shading.show_shadows,shading.show_cavity = False,True
    shading.cavity_type = 'BOTH'
    shading.curvature_ridge_factor,shading.curvature_valley_factor = 1.55,1.15
    shading.show_specular_highlight = False
    shading.show_object_outline = True
    shading.object_outline_color = (.055,.035,.02)
    shading.background_type = 'WORLD'
    if scene.world is None:
        scene.world = bpy.data.worlds.new('Review world')
    scene.world.color = (.045,.052,.075)
    locations = [obj.location.copy() for obj in objects]
    helpers = []
    try:
        for role,mat in MATS.items():
            mat.diffuse_color = (*PALETTE[role],1)
        gap = 1.75
        widths = [obj.dimensions.x for obj in objects]
        total = sum(widths)+gap*(len(objects)-1)
        x = -total/2
        for obj,width in zip(objects,widths):
            lo,hi = bounds(obj)
            obj.location.x = x-lo.x
            x += width+gap
        data = bpy.data.cameras.new('Review camera')
        cam = bpy.data.objects.new('Review camera',data)
        scene.collection.objects.link(cam)
        helpers.append(cam)
        scene.camera = cam
        cam.data.type,cam.data.ortho_scale = 'ORTHO',total+2
        angle = math.radians(55)
        target = Vector((0,0,4.0))
        cam.location = target+Vector((0,45*math.cos(angle),45*math.sin(angle)))
        cam.rotation_euler = (target-cam.location).to_track_quat('-Z','Y').to_euler()
        for obj in objects:
            data = bpy.data.curves.new('Review caption','FONT')
            data.body,data.align_x,data.size = obj.name,'CENTER',.42
            data.materials.append(MATS['clock_face'])
            caption = bpy.data.objects.new('Review caption '+obj.name,data)
            scene.collection.objects.link(caption)
            caption.location = (obj.location.x,5.25,-.05)
            caption.rotation_euler = cam.rotation_euler
            helpers.append(caption)
        PREVIEW.parent.mkdir(parents=True,exist_ok=True)
        bpy.ops.render.render(write_still=True)
        assert PREVIEW.is_file() and PREVIEW.stat().st_size>1024
        print('PREVIEW '+str(PREVIEW),flush=True)
    finally:
        scene.camera = None
        for obj in helpers:
            bpy.data.objects.remove(obj,do_unlink=True)
        for obj,loc in zip(objects,locations):
            obj.location = loc
        for mat in MATS.values():
            mat.diffuse_color = (.5,.5,.5,1)


def main():
    global PARTS
    bpy.ops.wm.read_factory_settings(use_empty=True)
    for mat in list(bpy.data.materials):
        bpy.data.materials.remove(mat)
    scene = bpy.context.scene
    scene.unit_settings.system,scene.unit_settings.scale_length = 'METRIC',1
    materials()
    OUTPUT.mkdir(parents=True,exist_ok=True)
    objects = []
    for i,(name,size,build) in enumerate(SPECS):
        PARTS = []
        random.seed(115911+7919*i)
        build()
        contain_footprint(size)
        objects.append(finish(name,size))
    assert len(scene.objects)==5 and all(obj.type=='MESH' for obj in scene.objects)
    preview(objects)
    assert len(scene.objects)==5 and all(obj.type=='MESH' for obj in scene.objects)
    print('DONE',flush=True)


if __name__=='__main__':
    main()
