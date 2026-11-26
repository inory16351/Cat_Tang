# 엔딩 스토리 맵 '자정의 시계판' 녹아내리는 섬 아랫면 키트, 2026-11-26
# 실행: "C:/Program Files/Blender Foundation/Blender 5.3/blender.exe" -b --factory-startup -P Art/tools/blender_kit_story_dial_under.py
# 축 규칙: Blender (X,Y,Z) = Unity (X,Z,Y), 미터 단위.
# KEEP: 모든 조각 원점은 섬 중심 / 섬 윗면 (0,0,0). 절대 재중심하지 않음.
# tips JSON: sand breakup starts and slump bottoms, Unity local (x,z,y).
"""Self-contained, seeded Blender 5.x kit. Grey FBX; coloured review only.

The eight-role ceiling takes precedence over redundant ornament colours:
Earthen roots use earth, dark metal uses rock_dark, sand ripples sand_glow.
All required shapes remain separate, solid geometry. No texture dependency.
"""
import json
import math
import random
from pathlib import Path

import bmesh
import bpy
from mathutils import Euler, Matrix, Vector
from mathutils.bvhtree import BVHTree

ROOT = Path(__file__).resolve().parents[2]
OUTPUT = ROOT / 'Game/Assets/Art/Characters/StoryDial'
PREVIEW = ROOT / 'Art/tools/specs/blender_kit_story_dial_under_preview.png'
OUTLINES = ROOT / 'Art/tools/specs/story_dial_outlines.json'
ROLES = (
    'wood', 'wood_dark', 'wood_light', 'wood_weathered', 'plank',
    'roof_shingle', 'roof_tin', 'adobe', 'adobe_light', 'stone', 'stone_dark',
    'rock', 'rock_dark', 'earth', 'root', 'sand', 'sand_glow', 'brass', 'gold',
    'gold_drip', 'iron', 'rust', 'canvas', 'cloth_red', 'cloth_cream',
    'paint_rose', 'paint_sage', 'glass', 'window_lit', 'lantern_glow',
    'clock_face', 'cactus', 'cactus_dark', 'stardust',
)
PALETTE = {
    'wood': (.43, .24, .12), 'wood_dark': (.18, .095, .055),
    'wood_light': (.72, .48, .25), 'wood_weathered': (.49, .41, .28),
    'plank': (.62, .36, .17), 'roof_shingle': (.48, .22, .14),
    'roof_tin': (.57, .53, .45), 'stone': (.66, .57, .43),
    'stone_dark': (.31, .28, .22), 'rock': (.49, .37, .28),
    'rock_dark': (.23, .19, .15), 'earth': (.49, .25, .13),
    'root': (.29, .15, .07), 'sand': (.83, .66, .42),
    'sand_glow': (1.0, .73, .26), 'brass': (.69, .44, .12),
    'gold': (.96, .66, .16), 'gold_drip': (1.0, .48, .10),
    'iron': (.16, .15, .13), 'rust': (.62, .25, .10),
    'paint_rose': (.70, .34, .27), 'paint_sage': (.48, .51, .29),
    'window_lit': (1.0, .86, .44), 'lantern_glow': (1.0, .58, .16),
    'clock_face': (.93, .85, .64), 'cactus': (.39, .47, .17),
    'cactus_dark': (.20, .29, .10), 'stardust': (1.0, .94, .72),
}
HARD = set(ROLES) - {'root', 'sand', 'sand_glow', 'gold_drip', 'stardust',
                      'cactus', 'cactus_dark', 'clock_face'}
MATS = {}
PARTS = []
TAU = math.tau
KEYS = ('Center', 'Gun', 'Ammo', 'Wardrobe', 'Cuckoo', 'Gate')
KIT_ROLES = ('sand', 'earth', 'rock', 'rock_dark', 'brass',
             'sand_glow', 'clock_face')
ROOT_ROLE, IRON_ROLE = 'earth', 'rock_dark'

def materials():
    for role in KIT_ROLES + ('stardust',):
        mat = bpy.data.materials.new(role)
        mat.use_nodes = True
        mat.diffuse_color = (.5, .5, .5, 1)
        mat.node_tree.nodes.clear()
        shader = mat.node_tree.nodes.new('ShaderNodeBsdfPrincipled')
        shader.inputs['Base Color'].default_value = (.5, .5, .5, 1)
        shader.inputs['Roughness'].default_value = .95
        shader.inputs['Metallic'].default_value = 0
        shader.inputs['Specular IOR Level'].default_value = .15
        output = mat.node_tree.nodes.new('ShaderNodeOutputMaterial')
        mat.node_tree.links.new(shader.outputs['BSDF'], output.inputs['Surface'])
        MATS[role] = mat


def mesh(label, vertices, faces, role, bevel=True):
    data = bpy.data.meshes.new(label)
    data.from_pydata(vertices, [], faces)
    data.materials.append(MATS[role])
    data.update()
    bm = bmesh.new()
    bm.from_mesh(data)
    bmesh.ops.recalc_face_normals(bm, faces=list(bm.faces))
    bm.to_mesh(data)
    bm.free()
    obj = bpy.data.objects.new(label, data)
    bpy.context.collection.objects.link(obj)
    if bevel and role in HARD:
        mod = obj.modifiers.new('Worn edges .025m / two segments', 'BEVEL')
        mod.width, mod.segments = .025, 2
        mod.limit_method, mod.angle_limit = 'ANGLE', math.radians(42)
    PARTS.append(obj)
    return obj


def transform(obj, loc=(0, 0, 0), rot=(0, 0, 0)):
    obj.data.transform(Matrix.Translation(Vector(loc)) @
                       Euler(rot).to_matrix().to_4x4())
    return obj


def box(label, loc, size, role, rot=(0, 0, 0), bevel=True):
    x, y, z = (v / 2 for v in size)
    vertices = [(-x,-y,-z), (x,-y,-z), (x,y,-z), (-x,y,-z),
                (-x,-y,z), (x,-y,z), (x,y,z), (-x,y,z)]
    faces = [(0,3,2,1), (4,5,6,7), (0,1,5,4), (1,2,6,5),
             (2,3,7,6), (3,0,4,7)]
    return transform(mesh(label, vertices, faces, role, bevel), loc, rot)


def cylinder(label, loc, radius, depth, role, n=12, rot=(0,0,0)):
    vertices = [(radius*math.cos(j*TAU/n), radius*math.sin(j*TAU/n), z)
                for z in (-depth/2, depth/2) for j in range(n)]
    faces = [tuple(reversed(range(n))), tuple(range(n,2*n))]
    faces += [(j,(j+1)%n,(j+1)%n+n,j+n) for j in range(n)]
    return transform(mesh(label, vertices, faces, role), loc, rot)


def tube(label, points, radii, role, n=5):
    """Closed tapered polygonal strand; .04 m minimum diameter."""
    if isinstance(radii, (float, int)):
        radii = [radii]*len(points)
    vertices = []
    for i, p in enumerate(points):
        tangent = (Vector(points[min(i+1,len(points)-1)]) -
                   Vector(points[max(0,i-1)])).normalized()
        reference = Vector((0,1,0)) if abs(tangent.y) < .9 else Vector((1,0,0))
        u = reference.cross(tangent).normalized()
        v = tangent.cross(u)
        vertices += [tuple(Vector(p)+max(.02,radii[i])*(
            u*math.cos(j*TAU/n)+v*math.sin(j*TAU/n))) for j in range(n)]
    faces = [tuple(reversed(range(n)))]
    faces += [(i*n+j,i*n+(j+1)%n,(i+1)*n+(j+1)%n,(i+1)*n+j)
              for i in range(len(points)-1) for j in range(n)]
    faces.append(tuple(range((len(points)-1)*n,len(points)*n)))
    return mesh(label, vertices, faces, role)


def ring(label, loc, inner, outer, depth, role, start=0, end=TAU,
         n=32, rot=(0,0,0)):
    """Annular solid, optionally an open broken arc; local XY plane."""
    closed = abs(end-start-TAU) < 1e-6
    count = n if closed else n+1
    vertices = [(r*math.cos(start+(end-start)*j/n),
                 r*math.sin(start+(end-start)*j/n), z)
                for r,z in ((inner,-depth/2),(outer,-depth/2),
                            (outer,depth/2),(inner,depth/2)) for j in range(count)]
    faces = [(k*count+j,k*count+(j+1)%count,
              ((k+1)%4)*count+(j+1)%count,((k+1)%4)*count+j)
             for k in range(4) for j in range(n)]
    if not closed:
        faces += [(0,count,2*count,3*count),
                  (count-1,4*count-1,3*count-1,2*count-1)]
    return transform(mesh(label, vertices, faces, role), loc, rot)


def gear(loc, radius=.5, teeth=10, rot=(0,0,0)):
    start = len(PARTS)
    ring('Brass gear open hub', (0,0,0), radius*.33, radius*.78,
         .12, 'brass', n=teeth*2)
    for i in range(teeth):
        a = i*TAU/teeth
        box('Broad gear tooth', (radius*.86*math.cos(a),radius*.86*math.sin(a),0),
            (radius*.33,radius*.25,.12), 'brass', rot=(0,0,a))
    for obj in PARTS[start:]:
        transform(obj,loc,rot)


def apply_modifiers(obj):
    bpy.ops.object.select_all(action='DESELECT')
    obj.select_set(True)
    bpy.context.view_layer.objects.active = obj
    for mod in list(obj.modifiers):
        bpy.ops.object.modifier_apply(modifier=mod.name)


def bounds(obj):
    return [Vector(tuple(fn(v.co[k] for v in obj.data.vertices) for k in range(3)))
            for fn in (min,max)]


def triangles(obj):
    obj.data.calc_loop_triangles()
    return len(obj.data.loop_triangles)


def inset_outline(points, distance=.15):
    """Exact parallel-edge polygon offset, including concave miter corners."""
    points = [Vector(p) for p in points]
    area = sum(p.x * points[(i+1) % len(points)].y -
               points[(i+1) % len(points)].x * p.y for i, p in enumerate(points))
    assert abs(area) > 1, 'Degenerate input outline'
    winding = 1 if area > 0 else -1
    result = []
    for i, p in enumerate(points):
        before = (p-points[i-1]).normalized()
        after = (points[(i+1) % len(points)]-p).normalized()
        n0 = winding * Vector((-before.y, before.x))
        n1 = winding * Vector((-after.y, after.x))
        shift = (n0+n1) * (distance/(1+n0.dot(n1)))
        assert abs(shift.dot(n0)-distance) < 1e-6
        assert abs(shift.dot(n1)-distance) < 1e-6
        result.append(p+shift)
    return result


def protect(obj):
    """Keep small grain fragments and sculpted flow details through decimation."""
    group = obj.vertex_groups.get('Protected_mineral_strata')
    if group is None:
        group = obj.vertex_groups.new(name='Protected_mineral_strata')
    group.add(list(range(len(obj.data.vertices))),1,'REPLACE')
    return obj


def sand_chunk(center, size, role):
    """Six sharp, asymmetric vertices, eight planar faces; never a bead."""
    assert .06 <= size <= .15
    points = [(-.50,-.18,-.16),(.50,.12,.08),(-.12,-.50,.10),
              (.18,.50,-.14),(-.17,.05,.50),(.10,-.14,-.50)]
    obj = mesh('Angular crumbling sand fragment',
               [tuple(size*c for c in p) for p in points],
               [(0,2,4),(2,1,4),(1,3,4),(3,0,4),
                (2,0,5),(1,2,5),(3,1,5),(0,3,5)],role,False)
    transform(obj,center,tuple(random.uniform(-math.pi,math.pi) for _ in range(3)))
    return protect(obj)


class MeltBody:
    """72-point lip and eight progressively sagging body courses."""
    def __init__(self, key, data, depth):
        self.key, self.depth = key,depth
        self.edge = [Vector(p) for p in data['outline']]
        self.outline = inset_outline(data['outline'])
        area = sum(p.x*self.edge[(i+1)%72].y-self.edge[(i+1)%72].x*p.y
                   for i,p in enumerate(self.edge))
        self.winding = 1 if area > 0 else -1
        self.tip_centers = []
        self.columns = []
        self.rim_strands = []
        self.phase = random.uniform(0,TAU)
        self.lean = Vector((random.choice((-1,1))*random.uniform(1.3,2.2),
                            random.uniform(-2.0,-.9),0))
        self.rings = []
        # Top, rounded lip, wavy sand/earth seam, then eight body courses.
        levels = [(-.05,1),(-.28,1),(-.70,.98),
                  (-depth*.17,.995),(-depth*.30,.925),(-depth*.44,.76),
                  (-depth*.57,.54),(-depth*.70,.39),
                  (-depth*.81,.24),(-depth*.88,.13)]
        for k,(z,scale) in enumerate(levels):
            course = []
            for j,p in enumerate(self.outline):
                a = j*TAU/72
                wave = math.sin(3*a+self.phase)+.42*math.sin(7*a-self.phase)
                t = k/(len(levels)-1)
                sag = .5+.5*math.cos(a-self.phase)
                radial = (1 if k < 2 else
                          1+t*(.25*math.sin(2*a+self.phase)+.12*math.sin(5*a+k*.3)))
                xy = p*scale*radial
                if k == 1:
                    xy += p.normalized()*.20
                co = Vector((xy.x,xy.y,z)) + self.lean*t*t
                if k >= 2:
                    co.z += ((.13 if k == 2 else .20+.15*t)*wave
                             - (0 if k == 2 else 1.35*t*sag))
                if key == 'Center' and co.z <= -4.65:
                    # Smoothly contract before -5: the northward clock hands
                    # at -6.5 have clear space outside radius 5.
                    limit = 3.65 if co.z <= -5 else 3.65+3*(-co.z-5)**2
                    r = math.hypot(co.x,co.y)
                    if r > limit:
                        co.x *= limit/r
                        co.y *= limit/r
                course.append(co)
            self.rings.append(course)
        self.make_solid()
        self.bottom_lobes()

    def edge_point(self, j):
        """True outline and outward edge normal, independent of winding."""
        j %= 72
        i = int(j)
        p = self.edge[i].lerp(self.edge[(i+1)%72],j-i)
        tangent = (self.edge[(i+1)%72]-self.edge[i]).normalized()
        normal = self.winding*Vector((tangent.y,-tangent.x,0))
        return Vector((p.x,p.y,0)),normal

    def outside_distance(self, p):
        """Signed nearest polygon distance; positive means outside."""
        q = Vector((p.x,p.y))
        distance,inside = float('inf'),False
        for i,a in enumerate(self.edge):
            b = self.edge[(i+1)%72]
            d = b-a
            t = max(0,min(1,(q-a).dot(d)/d.length_squared))
            distance = min(distance,(q-a-d*t).length)
            if (a.y > q.y) != (b.y > q.y):
                if q.x < a.x+(q.y-a.y)*(b.x-a.x)/(b.y-a.y):
                    inside = not inside
        return -distance if inside else distance

    def point(self, j, course, offset=0):
        """Bilinear sampling on the authored skin; outward offset in metres."""
        j %= 72
        a,b = int(j),int(j+1)%72
        f = j-int(j)
        k = max(0,min(len(self.rings)-2,int(course)))
        t = max(0,min(1,course-k))
        p0 = self.rings[k][a].lerp(self.rings[k][b],f)
        p1 = self.rings[k+1][a].lerp(self.rings[k+1][b],f)
        p = p0.lerp(p1,t)
        radial = Vector((p.x,p.y,0)).normalized()
        return p+radial*offset

    def make_solid(self):
        n = 72
        verts = [tuple(p) for course in self.rings for p in course]
        faces, roles = [],[]
        bands = ('sand','sand','earth','earth','rock','rock',
                 'rock_dark','rock_dark','earth')
        for k in range(len(self.rings)-1):
            for j in range(n):
                a,b = k*n+j,k*n+(j+1)%n
                c,d = b+n,a+n
                faces += [(a,b,c),(a,c,d)]
                # Continuous strata, with a few wide inclusions, never a
                # per-face random checkerboard or a monochrome inverted cone.
                role = bands[k]
                if 3 <= k <= 7 and (j+int(self.phase*4))%23 in (0,1,2):
                    role = 'earth' if k >= 5 else 'rock'
                roles += [role,role]
        faces.append(tuple(reversed(range((len(self.rings)-1)*n,len(verts)))))
        roles.append('rock_dark')
        # A solid cap is necessary for manifold FBX. These broad mineral
        # regions are hidden by the actual island floor once placed in Unity.
        previous = list(range(n))
        for layer,scale in enumerate((.80,.59,.34)):
            current = []
            for j,p in enumerate(self.outline):
                w = scale+.016*math.sin(3*j*TAU/n+self.phase+layer)
                current.append(len(verts))
                verts.append((p.x*w,p.y*w,-.05))
            for j in range(n):
                faces.append((previous[j],previous[(j+1)%n],
                              current[(j+1)%n],current[j]))
                roles.append(('sand','earth','rock')[layer])
            previous = current
        faces.append(tuple(previous))
        roles.append('rock_dark')
        obj = mesh('Exact 72-point lip and flowing mineral strata',verts,faces,'sand',False)
        for role in ('earth','rock','rock_dark'):
            obj.data.materials.append(MATS[role])
        for p,role in zip(obj.data.polygons,roles):
            p.material_index = ('sand','earth','rock','rock_dark').index(role)
            p.use_smooth = False
        group = obj.vertex_groups.new(name='Lip_exact_inset_Z_minus_005')
        group.add(list(range(n)),1,'REPLACE')
        body_group = obj.vertex_groups.new(name='Melt_body_skin')
        body_group.add(list(range(len(self.rings)*n)),1,'REPLACE')
        protected = obj.vertex_groups.new(name='Protected_mineral_strata')
        protected.add(list(range(len(verts))),1,'REPLACE')

    def bottom_lobes(self):
        # Three broken earth shelves continue the closed bottom course.
        for i in range(3):
            j = (i*24+self.phase*5)%72
            p = self.point(j,7.6,-.65)
            end = Vector((self.lean.x+(i-1)*.95,
                          self.lean.y+.38*math.sin(i*2+self.phase),
                          -self.depth+(0,.38,.16)[i]))
            mid = p.lerp(end,.55)+Vector((.15*math.sin(i),0,.07))
            first = len(PARTS)
            normal = Vector((p.x,p.y,0)).normalized()
            sand_volume('Three-way broken body ledge',
                        [(p,.90,.55),(mid,.72,.42),(end,.55,.27)],normal,
                        'earth' if i%2 else 'rock_dark')
            for obj in PARTS[first:]:
                group = obj.vertex_groups.new(name='Protected_mineral_strata')
                group.add(list(range(len(obj.data.vertices))),1,'REPLACE')


def ribbon(label, points, width, role, thickness=.14):
    """Thick, closed, face-hugging stream: no sheet thinner than .1m."""
    verts = []
    for i,p in enumerate(points):
        radial = Vector((p.x,p.y,0)).normalized()
        across = Vector((-radial.y,radial.x,0))
        w = width*(1-.58*i/(len(points)-1))
        verts += [tuple(p+across*s*w/2+radial*d)
                  for d,s in ((-thickness/2,-1),(-thickness/2,1),
                              (thickness/2,1),(thickness/2,-1))]
    faces = [(3,2,1,0)]
    for i in range(len(points)-1):
        faces += [(i*4+j,i*4+(j+1)%4,(i+1)*4+(j+1)%4,(i+1)*4+j)
                  for j in range(4)]
    faces.append(tuple(range(len(verts)-4,len(verts))))
    return mesh(label,verts,faces,role,False)


def sand_volume(label, sections, normal, role, fluted=True):
    """Flat backed sand sheet: fluted front and a jagged, wide broken edge."""
    across = Vector((-normal.y,normal.x,0))
    n = 15
    verts = []
    for k,(p,width,thickness) in enumerate(sections):
        # Thirteen front samples cut six longitudinal channels. Rear corners
        # close a trapezoidal section, with no elliptical liquid cross section.
        for j in range(13):
            u = j/6-1
            depth = thickness*((.42 if j%2 == 0 else .34) if fluted else
                               .43-.08*abs(u)+.015*math.sin(j*2.1+k))
            edge = (0 if k < len(sections)-1 else
                    -.045-(j%3)*.055)
            verts.append(tuple(p+across*(width*u/2)+normal*depth+Vector((0,0,edge))))
        verts += [tuple(p+across*width/2-normal*thickness/2),
                  tuple(p-across*width/2-normal*thickness/2)]
    faces = [tuple(reversed(range(n)))]
    faces += [(k*n+j,k*n+(j+1)%n,(k+1)*n+(j+1)%n,(k+1)*n+j)
              for k in range(len(sections)-1) for j in range(n)]
    faces.append(tuple(range(len(verts)-n,len(verts))))
    obj = mesh(label,verts,faces,role,False)
    return obj


def dune_ripples(sections, normal, role):
    """Horizontal 0.05-0.10m dune crests resting on the slump's ledges."""
    across = Vector((-normal.y,normal.x,0))
    for k,(p,width,thickness) in enumerate(sections[1:-1:2]):
        points = [p+across*(width*.46*u)+normal*(thickness*.43)+
                  Vector((0,0,.025*math.sin(j*1.7+k)))
                  for j,u in enumerate((-1,-.66,-.33,0,.33,.66,1))]
        tube('Horizontal dune ripple / collapsing shelf',points,
             random.uniform(.025,.05),role,n=4)


def rim_overflows(body):
    """Arc-length spacing, with 60% of the folds on the actual -Y half."""
    sides = {True:[],False:[]}
    for i,a in enumerate(body.edge):
        b = body.edge[(i+1)%72]
        cuts = [0,1]
        if a.y*b.y < 0:
            cuts.insert(1,-a.y/(b.y-a.y))
        for lo,hi in zip(cuts,cuts[1:]):
            length = (b-a).length*(hi-lo)
            south = a.lerp(b,(lo+hi)/2).y < 0
            sides[south].append((i+lo,i+hi,length))
    # Start each half at the equator. The north half crosses outline index 0;
    # retaining index order would incorrectly put its end margins at the pole.
    for south,edges in sides.items():
        for k,(lo,hi,length) in enumerate(edges):
            if abs(lo%72-edges[k-1][1]%72) > 1e-6:
                sides[south] = edges[k:]+edges[:k]
                break
    lengths = {s:sum(e[2] for e in edges) for s,edges in sides.items()}
    total = sum(lengths.values())
    candidates = []
    for count in range(math.ceil(total/2.5),math.floor(total/1.5)+1):
        if count%5:
            continue
        counts = {True:count*3//5,False:count*2//5}
        # Treat the two cross-equator gaps separately. Requiring equal spacing
        # within each half would make exact 60/40 impossible on Cuckoo.
        for boundary_gap in (2.3,2.1,2.5,1.9,1.7,1.5):
            target = total-2*boundary_gap
            low = max(1.5*(counts[True]-1),target-2.5*(counts[False]-1),
                      target-lengths[False]+1.0)
            high = min(2.5*(counts[True]-1),target-1.5*(counts[False]-1),
                       lengths[True]-1.0)
            if low > high:
                continue
            south_span = max(low,min(high,lengths[True]*(counts[True]-1)/counts[True]))
            spans = {True:south_span,False:target-south_span}
            spacing = {s:spans[s]/(counts[s]-1) for s in sides}
            margins = {s:(lengths[s]-spans[s])/2 for s in sides}
            score = abs(total/count-2)+abs(boundary_gap-2.3)*.1
            candidates.append((score,counts,spacing,margins))
    assert candidates, (body.key,'cannot space rim folds')
    _,counts,spacing,margins = min(candidates,key=lambda item:item[0])
    assert all(1.5-1e-6 <= v <= 2.5+1e-6 for v in spacing.values())
    assert 1.5-1e-6 <= sum(margins.values()) <= 2.5+1e-6
    lobes = []
    for south,edges in sides.items():
        for index in range(counts[south]):
            target = margins[south]+index*spacing[south]
            for lo,hi,length in edges:
                if target <= length:
                    j = lo+(hi-lo)*target/length
                    break
                target -= length
            edge,normal = body.edge_point(j)
            width = random.uniform(.85,1.5)
            sag = random.uniform(1.0,3.0)
            role = 'sand' if index%3 else 'earth'
            end = edge+normal*.27+Vector((0,0,-.05-sag))
            sections = []
            for t,out,w,thick in ((0,-.20,.88,.22),(.12,.01,1,.55),
                                   (.29,.01,.98,.55),(.32,.12,.88,.46),
                                   (.53,.12,.88,.46),(.56,.21,.73,.34),
                                   (.77,.21,.73,.34),(.80,.27,.57,.22),
                                   (1,.27,.52,.18)):
                p = edge+normal*out+Vector((0,0,-.05-sag*t))
                sections.append((p,width*w,thick))
            fold = sand_volume('Stepped crumbling rim sand slump',sections,normal,role,
                               fluted=False)
            # Tight convex outline bends need a narrower fold: a straight
            # tangent section otherwise exceeds the .5m overhang at its sides.
            across = Vector((-normal.y,normal.x,0))
            for attempt in range(8):
                protrusion = max(body.outside_distance(v.co) for v in fold.data.vertices)
                if protrusion <= .49:
                    break
                for v in fold.data.vertices:
                    v.co -= across*((v.co-edge).dot(across)*.18)
            group = fold.vertex_groups.new(name='Protected_mineral_strata')
            group.add(list(range(15)),1,'REPLACE')
            assert .1 <= protrusion <= .5, (body.key,'rim bulge',protrusion)
            # Match narrowed shelves, including their ripple crests.
            narrowed = max(abs((v.co-edge).dot(across)) for v in fold.data.vertices)
            ripple_sections = [(p,min(w,narrowed*2),thick) for p,w,thick in sections]
            dune_ripples(ripple_sections,normal,'sand_glow' if index%4 == 0 else 'sand')
            end.z = min(v.co.z for v in fold.data.vertices)
            body.tip_centers.append(end.copy())
            lobes.append(dict(j=j,end=end,normal=normal,role=role,south=south))
    assert sum(l['end'].y < 0 for l in lobes)*5 == len(lobes)*3
    body.lobes = lobes
    return lobes


def hanging_strand(body, p, end, width, role, label, normal):
    points = [p.lerp(end,k/12)+normal*(.055*math.sin(math.pi*k/12))
              for k in range(13)]
    return sand_column(body,points,width,role,label,normal)


def sand_column(body, path, width, role, label, normal):
    """Solid upper 2/3, blunt torn termination, discrete lower 1/3 only."""
    assert len(path) == 13
    across = Vector((-normal.y,normal.x,0))
    n,verts = 16,[]
    for k,p in enumerate(path[:9]):
        radius = width*.5*(1-.68*k/8)
        for j in range(n):
            a = j*TAU/n
            # Alternating ridges form actual vertical grooves, flat shaded.
            r = radius*(1 if j%2 == 0 else .84)
            z = -.04*(j%3) if k == 8 else 0
            verts.append(tuple(p+across*(r*math.cos(a))+
                               normal*(r*.76*math.sin(a))+Vector((0,0,z))))
    faces = [tuple(reversed(range(n)))]
    faces += [(k*n+j,k*n+(j+1)%n,(k+1)*n+(j+1)%n,(k+1)*n+j)
              for k in range(8) for j in range(n)]
    faces.append(tuple(range(8*n,9*n)))
    obj = mesh(label,verts,faces,role,False)
    # Preserve the entire blunt, serrated breakup rim: budget reduction must
    # never turn it into a single pointed vertex or erase its minimum width.
    group = obj.vertex_groups.new(name='Protected_mineral_strata')
    group.add(list(range(8*n,9*n)),1,'REPLACE')
    obj.data.materials.append(MATS['sand_glow' if role != 'sand_glow' else 'sand'])
    for face in obj.data.polygons[1:-1]:
        if (face.index-1)%n in (3,11):
            face.material_index = 1
    # The emitter is precisely where the unbroken sand stops, never at a bead.
    breakup = path[8].copy()
    body.tip_centers.append(breakup)
    count = random.randint(6,14)
    chunks = []
    for i in range(count):
        # Convex spacing increases towards the bottom; last grains scatter
        # sideways and get smaller, with no continuous pointed tail.
        t = (i+1)/(count+1)
        u = t**1.65
        pos = path[8].lerp(path[-1],u)
        scatter = (.05+width*.42*u)
        pos += across*random.uniform(-scatter,scatter)+normal*random.uniform(-scatter,scatter)
        size = random.uniform(.11,.15)*(1-u)+random.uniform(.06,.085)*u
        sand_chunk(pos,size,'sand' if i%4 else 'sand_glow')
        chunks.append((pos,size))
    body.columns.append(dict(start=path[0].copy(),breakup=breakup,
                             end=path[-1].copy(),chunks=chunks))
    return path


def drips(body, lobes):
    count = 30 if body.key == 'Center' else 20
    free_count = 20 if body.key == 'Center' else 14
    chosen = []
    # Match the folds' south bias without clustering the free strands together.
    for south,number in ((True,round(free_count*.6)),(False,free_count-round(free_count*.6))):
        pool = [l for l in lobes if l['south'] == south]
        chosen += [pool[int((i+.5)*len(pool)/number)] for i in range(number)]
    for i,lobe in enumerate(chosen):
        p,normal = lobe['end'],lobe['normal']
        width = random.uniform(.35,.9)
        length = (random.uniform(7.5,10) if i%4 == 0 else
                  random.uniform(2.0,7.2))
        # Outer north rim remains outside the central hand corridor, r=5..9.
        # No widening of the contracted Center body below -5 is necessary.
        end = p+normal*.07+Vector((0,0,-length))
        role = ('sand_glow' if i%10 == 6 else lobe['role'])
        points = hanging_strand(body,p,end,width,role,
                                'Outside rim fluted pouring sand column',normal)
        assert all(0 <= body.outside_distance(q) <= .6 for q in points)
        body.rim_strands.append(points)
    for i in range(count-free_count):
        j = (i*72/(count-free_count)+body.phase*2)%72
        start,finish = random.uniform(1.6,2.5),random.uniform(7.1,8.8)
        points = [body.point(j+.85*math.sin(k*.56+i),
                             start+(finish-start)*k/12,.18) for k in range(13)]
        role = ('sand','earth','sand','sand','earth','sand_glow')[i%6]
        normal = Vector((points[0].x,points[0].y,0)).normalized()
        sand_column(body,points,random.uniform(.35,.9),role,
                    'Skin-hugging grooved sand stream',normal)
    body.strand_count = count
    assert len(body.rim_strands) >= count/2


def melt_tongues(body):
    count = 5 if body.key == 'Center' else 4
    for i in range(count):
        # Broad tongues fan to either side; most sag towards the camera.
        j = (21+i*32/(count-1)+random.uniform(-1,1))%72
        p = body.point(j,3.0,-.08)
        edge,normal = body.edge_point(j)
        across = Vector((-normal.y,normal.x,0))
        width,length = random.uniform(1.5,3),random.uniform(4,8)
        side = random.uniform(-.9,.9)
        end = p+normal*random.uniform(.5,1.2)+across*side+Vector((0,0,-length))
        sections = []
        for t,w,thick in ((0,.88,.75),(.12,1,1.0),(.32,.94,1.05),
                           (.55,.76,.90),(.77,.53,.64),(1,.30,.38)):
            center = p.lerp(end,t)+normal*(.32*math.sin(math.pi*t))
            sections.append((center,width*w,thick))
        role = ('earth','sand','sand','earth','sand')[i]
        obj = sand_volume('Broad vertically rilled sand tongue',sections,normal,role)
        if body.key == 'Center':
            # Tongues belong to the body: preserve the r<=4 lower hand clearance.
            for v in obj.data.vertices:
                if v.co.z <= -4.5:
                    limit = 3.65 if v.co.z <= -5 else 3.65+3*(-v.co.z-5)**2
                    radius = math.hypot(v.co.x,v.co.y)
                    if radius > limit:
                        v.co.x *= limit/radius
                        v.co.y *= limit/radius
            group = obj.vertex_groups.new(name='Melt_body_skin')
            group.add(list(range(len(obj.data.vertices))),1,'REPLACE')
            radius = math.hypot(end.x,end.y)
            if end.z <= -5 and radius > 3.45:
                end.x *= 3.45/radius
                end.y *= 3.45/radius
        for branch in range(2+i%2):
            offset = (branch-(1+i%2)/2)*width*.22
            start = end+across*offset+Vector((0,0,.20))
            tip = start+across*(offset*.35)+normal*.10+Vector((0,0,-random.uniform(1.0,2.7)))
            hanging_strand(body,start,tip,random.uniform(.35,.55),role,
                           'Scattering branch from split sand tongue',normal)
    body.tongue_count = count


def skin_details(body):
    # Four rootlets curve along the skin; their 0.04m terminal diameter is real.
    for i in range(4):
        j = (body.phase*9+i*17.3)%72
        points = [body.point(j+1.1*math.sin(k*.9+i),2+k*.65,.07)
                  for k in range(7)]
        tube('Thin exposed earthen root',points,[.09,.08,.07,.06,.05,.03,.02],
             ROOT_ROLE,n=6)
    for i in range(6):
        j = (body.phase*4+i*12+3)%72
        start,finish = 2.2,random.uniform(6.0,8.4)
        points = [body.point(j+.48*math.sin(k*1.7+i),
                             start+(finish-start)*k/9,.075) for k in range(10)]
        ribbon('Glowing vertical time-sand fissure',points,
               random.uniform(.085,.15),'sand_glow',thickness=.10)
        if i%2 == 0:
            branch = [points[3],body.point(j+1,4.1,.075),body.point(j+2,4.6,.075)]
            ribbon('Branching sand fissure',branch,.08,'sand_glow',thickness=.10)


def skin_frame(body,j,course):
    p = body.point(j,course,.015)
    horizontal = body.point(j+.3,course)-body.point(j-.3,course)
    x = horizontal.normalized()
    up = (body.point(j,course-.15)-body.point(j,course+.15)).normalized()
    normal = x.cross(up).normalized()
    radial = Vector((p.x,p.y,0)).normalized()
    if normal.dot(radial) < 0:
        x = -x
        normal = -normal
    up = normal.cross(x).normalized()
    basis = Matrix(((x.x,up.x,normal.x),(x.y,up.y,normal.y),(x.z,up.z,normal.z)))
    return p,basis.to_euler()


def clock_fragment(loc,rot,radius,index):
    start = len(PARTS)
    # Thick partial porcelain disc; bottom vertices elongate and sag.
    n = 24
    a0,a1 = -.34,5.05
    angles = [a0+(a1-a0)*j/n for j in range(n+1)]
    polygon = [(0,0)]+[(radius*math.cos(a),radius*math.sin(a)) for a in angles]
    count = len(polygon)
    verts = [(x,y,z) for z in (-.065,.065) for x,y in polygon]
    faces = [tuple(reversed(range(count))),tuple(range(count,2*count))]
    faces += [(j,(j+1)%count,(j+1)%count+count,j+count) for j in range(count)]
    mesh('Half-melted broken clock-face',verts,faces,'clock_face',False)
    ring('Broken brass dial bezel',(0,0,0),radius*.88,radius*1.02,.15,
         'brass',start=a0,end=a1,n=24)
    for i in range(9):
        a = a0+.18+i*(a1-a0-.36)/8
        tube('Dark clock hour marker',[(radius*.66*math.cos(a),radius*.66*math.sin(a),.12),
             (radius*.82*math.cos(a),radius*.82*math.sin(a),.12)],.025,IRON_ROLE,n=4)
    tube('Crooked dial hand',[(0,-radius*.36,.14),(0,0,.14),
                              (radius*.48,radius*.13,.14)],.035,IRON_ROLE,n=5)
    for obj in PARTS[start:]:
        for v in obj.data.vertices:
            y = v.co.y
            sag = max(0,-y-radius*.12)
            v.co.y -= .68*(sag/radius)**1.5*radius
            v.co.x += .13*(sag/radius)**2*radius
            v.co.z += .18*(sag/radius)**2
        transform(obj,loc,rot)


def embedded_clocks(body):
    # South-facing placement ensures ornaments read from both review angles;
    # the fourth gear remains on the north/back so the lower view is ornate too.
    for i,j in enumerate((26,40,53,7)):
        j = (j+body.phase*1.7)%72
        p,rot = skin_frame(body,j,(3.5,4.8,3.8,5.1)[i])
        start = len(PARTS)
        radius = random.uniform(.53,.88)
        gear((0,0,0),radius,teeth=10+i%3)
        for obj in PARTS[start:]:
            for v in obj.data.vertices:
                sag = max(0,-v.co.y-radius*.25)
                v.co.y -= .55*sag*sag/radius
                v.co.x += .14*sag
            transform(obj,p,rot)
    for i,j in enumerate((32,48,65)):
        p,rot = skin_frame(body,(j+body.phase)%72,3.0+i*.75)
        clock_fragment(p,rot,random.uniform(.55,.77),i)
    for i,j in enumerate((37,57)):
        points = [body.point(j,3.5,.17),body.point(j+.8,4.2,.18),
                  body.point(j+1.7,5.3,.20),body.point(j+2,6.3,.23)]
        tube('Bent elongated embedded clock hand',points,[.065,.06,.045,.025],
             IRON_ROLE,n=4)
        # Recognisable wide arrowhead, 0.05m finite thickness.
        p,rot = skin_frame(body,j,3.5)
        transform(mesh('Clock hand broad arrowhead',
                       [(-.16,0,-.025),(.16,0,-.025),(0,.32,-.025),
                        (-.16,0,.025),(.16,0,.025),(0,.32,.025)],
                       [(2,1,0),(3,4,5),(0,1,4,3),(1,2,5,4),(2,0,3,5)],
                       IRON_ROLE,False),p,rot)


def visible_materials(obj, elevation=57):
    """Village framework's BVH-occluded projected surface-area regression."""
    obj.data.calc_loop_triangles()
    verts = [v.co.copy() for v in obj.data.vertices]
    tris = list(obj.data.loop_triangles)
    tree = BVHTree.FromPolygons(verts,[tuple(t.vertices) for t in tris],all_triangles=True)
    a = math.radians(elevation)
    view = Vector((0,-math.cos(a),math.sin(a)))
    distance = max(obj.dimensions)*4+2
    areas = {}
    for index,t in enumerate(tris):
        facing = t.normal.dot(view)
        if facing <= 0 or t.area < 1e-10:
            continue
        center = sum((verts[k] for k in t.vertices),Vector())/3
        _,_,hit,_ = tree.ray_cast(center+view*distance,-view,distance+.005)
        if hit == index:
            role = obj.data.materials[t.material_index].name
            areas[role] = areas.get(role,0)+t.area*facing
    total = sum(areas.values())
    assert total > 0,obj.name
    return {role:area/total for role,area in areas.items()}


def drop_unused_materials(obj):
    slots = list(obj.data.materials)
    used = sorted({p.material_index for p in obj.data.polygons})
    names,indices = [],[]
    for old in used:
        if slots[old].name not in names:
            names.append(slots[old].name)
        indices.append((old,names.index(slots[old].name)))
    remap = dict(indices)
    poly_indices = [remap[p.material_index] for p in obj.data.polygons]
    obj.data.materials.clear()
    for role in names:
        obj.data.materials.append(MATS[role])
    for p,index in zip(obj.data.polygons,poly_indices):
        p.material_index = index


def finish(name,budget,body,tips,seed):
    # KEEP authored origin: no origin_set, no bounding-box recentering.
    for obj in PARTS:
        apply_modifiers(obj)
    bpy.ops.object.select_all(action='DESELECT')
    for obj in PARTS:
        obj.select_set(True)
    bpy.context.view_layer.objects.active = PARTS[0]
    bpy.ops.object.join()
    obj = bpy.context.object
    obj.name,obj.data.name = name,name+'_Mesh'
    if triangles(obj) > budget:
        mod = obj.modifiers.new('Preserve strata and role boundaries / budget','DECIMATE')
        mod.ratio = (budget-150)/triangles(obj)
        mod.delimit = {'MATERIAL'}
        # Collapse can erase coplanar material boundaries even with delimit;
        # protect the entire body and simplify only ornamental triangles.
        mod.vertex_group = 'Protected_mineral_strata'
        mod.invert_vertex_group = True
        bpy.ops.object.modifier_apply(modifier=mod.name)
    drop_unused_materials(obj)
    obj.data.update()
    bpy.context.view_layer.update()
    assert triangles(obj) <= budget,(name,triangles(obj),budget)
    assert obj.location.length < 1e-7,name+' origin is not island datum'
    assert all(math.isfinite(c) for v in obj.data.vertices for c in v.co),name
    bm = bmesh.new()
    bm.from_mesh(obj.data)
    bmesh.ops.recalc_face_normals(bm,faces=list(bm.faces))
    assert all(e.is_manifold for e in bm.edges),name+' has an open fragment'
    bm.to_mesh(obj.data)
    bm.free()
    obj.data.update()
    lo,hi = bounds(obj)
    assert hi.z <= -.05+1e-6,(name,'pierces floor',hi.z)
    assert abs(hi.z+.05) < 1e-6,(name,'missing top lip')
    group = obj.vertex_groups['Lip_exact_inset_Z_minus_005'].index
    lip = [v for v in obj.data.vertices if any(g.group == group for g in v.groups)]
    assert len(lip) == 72,(name,'72-point lip changed',len(lip))
    for p in body.outline:
        assert min((v.co-Vector((p.x,p.y,-.05))).length for v in lip) < 1e-5
    if body.key == 'Center':
        group = obj.vertex_groups['Melt_body_skin'].index
        for v in obj.data.vertices:
            if v.co.z < -5 and any(g.group == group for g in v.groups):
                assert math.hypot(v.co.x,v.co.y) <= 4+1e-6
    assert 6 <= len(obj.data.materials) <= 8,(name,len(obj.data.materials))
    assert {m.name for m in obj.data.materials} == set(KIT_ROLES),name
    assert not any(m.name == 'gold_drip' for m in obj.data.materials)
    for mat in obj.data.materials:
        assert mat.name in ROLES and tuple(mat.diffuse_color) == (.5,.5,.5,1)
        nodes = mat.node_tree.nodes
        assert tuple(nodes['Principled BSDF'].inputs['Base Color'].default_value) == (.5,.5,.5,1)
        assert len(nodes) == 2 and not any(n.type.startswith('TEX_') for n in nodes)
    for angle in (57,-40):
        coverage = visible_materials(obj,angle)
        assert len(coverage) >= 6,(name,angle,coverage)
        assert max(coverage.values()) <= .55,(name,'dominant visible role',angle,coverage)
        print('VISIBLE '+name+' '+str(angle)+' '+str({k:round(v,3) for k,v in
              sorted(coverage.items(),key=lambda item:-item[1])}),flush=True)
    obj['front'],obj['origin_rule'] = '-Y south','KEEP island centre and top surface'
    obj['triangle_count'],obj['seed'] = triangles(obj),seed
    obj['terrain_surface_z'],obj['lip_inset_m'] = 0.0,.15
    destination = OUTPUT / ('Kit_D_'+name+'.fbx')
    bpy.ops.export_scene.fbx(
        filepath=str(destination),use_selection=True,
        axis_forward='-Z',axis_up='Y',apply_scale_options='FBX_SCALE_UNITS',
        bake_space_transform=True,object_types={'MESH'},use_mesh_modifiers=True,
        mesh_smooth_type='FACE',add_leaf_bones=False,bake_anim=False,
    )
    assert destination.is_file() and destination.stat().st_size > 1024,destination
    sidecar = OUTPUT / ('Kit_D_'+name+'_tips.json')
    sidecar.write_text(json.dumps({'tips':tips},ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
    saved_tips = json.loads(sidecar.read_text(encoding='utf-8'))['tips']
    assert len(saved_tips) == len(body.tip_centers)
    assert len(saved_tips) == len(body.lobes)+len(body.columns)
    for tip,p in zip(saved_tips,body.tip_centers):
        assert (Vector((tip[0],tip[2],tip[1]))-p).length < 1e-5
    for column in body.columns:
        assert 6 <= len(column['chunks']) <= 14
        assert all(.06 <= size <= .15 for _,size in column['chunks'])
        # Lower third begins at the authored 2/3 sample along every column.
        assert column['end'].z < column['breakup'].z < column['start'].z
    assert body.strand_count == (30 if body.key == 'Center' else 20)
    assert 3 <= body.tongue_count <= 5
    print(f'EXPORTED {name} {triangles(obj)} tris, {len(tips)} tips, '
          f'{len(body.lobes)} sand slumps, {len(body.rim_strands)} outside columns, '
          f'{body.tongue_count} tongues, Z={lo.z:.3f}..{hi.z:.3f}',flush=True)
    return obj


def preview(objects,data):
    """Two complete map placements, one render: rotate each panel into view.

    Rotating the maps around X is exactly equivalent to south-facing cameras
    at -40 and +57 degrees. All review transforms and colours are restored.
    """
    scene = bpy.context.scene
    scene.render.engine = 'BLENDER_WORKBENCH'
    scene.render.resolution_x,scene.render.resolution_y = 3200,1600
    scene.render.resolution_percentage = 100
    scene.render.image_settings.file_format = 'PNG'
    scene.render.filepath = str(PREVIEW)
    scene.render.film_transparent = False
    scene.view_settings.view_transform = 'Standard'
    shading = scene.display.shading
    shading.light,shading.color_type = 'FLAT','MATERIAL'
    shading.show_shadows,shading.show_cavity = False,True
    shading.cavity_type = 'BOTH'
    shading.curvature_ridge_factor,shading.curvature_valley_factor = 1.5,1.2
    shading.show_specular_highlight = False
    shading.background_type,shading.background_color = 'WORLD',(.07,.07,.18)
    if scene.world is None:
        scene.world = bpy.data.worlds.new('Review world')
    scene.world.color = (.07,.07,.18)
    saved = [(o.location.copy(),o.rotation_euler.copy()) for o in objects]
    helpers = []
    def caption(text,x,z,size):
        font = bpy.data.curves.new('Review caption','FONT')
        font.body,font.align_x,font.size = text,'CENTER',size
        font.materials.append(MATS['stardust'])
        obj = bpy.data.objects.new('Caption '+text,font)
        scene.collection.objects.link(obj)
        obj.location = (x,-90,z)
        obj.rotation_euler = (math.pi/2,0,0)
        helpers.append(obj)
    try:
        for role,mat in MATS.items():
            mat.diffuse_color = (*PALETTE.get(role,(.6,.5,.4)),1)
        for angle,offset,title in ((-40,-43,'UNDERSIDE  /  SOUTH -40 deg'),
                                   (57,43,'GAME CAMERA  /  SOUTH +57 deg')):
            rotation = Matrix.Rotation(math.radians(angle),4,'X')
            panel = []
            for original,key in zip(objects,KEYS):
                obj = original
                if angle == 57:
                    obj = original.copy()
                    obj.data = original.data
                    scene.collection.objects.link(obj)
                    helpers.append(obj)
                x,y = data[key]['center']
                obj.rotation_euler = (math.radians(angle),0,0)
                obj.location = rotation @ Vector((x,y,0))+Vector((offset,0,0))
                panel.append((obj,key))
            # Force dependency updates before calculating projected extents.
            bpy.context.view_layer.update()
            coords = [o.matrix_world @ v.co for o,_ in panel for v in o.data.vertices]
            shift = -(max(p.z for p in coords)+min(p.z for p in coords))/2
            for obj,key in panel:
                obj.location.z += shift
                bpy.context.view_layer.update()
                z = max((obj.matrix_world @ v.co).z for v in obj.data.vertices)
                caption(key,obj.location.x+(2.5 if key == 'Cuckoo' and angle < 0 else 0),z+1.0,.88)
            caption(title,offset,36,1.35)
        caption('MIDNIGHT DIAL  /  FLOWING AND CRUMBLING SAND',0,40,1.55)
        caption('72-POINT INSET LIPS   |   METRES   |   KEEP ISLAND ORIGIN',0,-39,.88)
        camera = bpy.data.objects.new('Review camera',bpy.data.cameras.new('Review camera'))
        scene.collection.objects.link(camera)
        helpers.append(camera)
        scene.camera = camera
        camera.data.type,camera.data.ortho_scale = 'ORTHO',174
        camera.location = (0,-200,1)
        camera.rotation_euler = (Vector((0,0,1))-camera.location).to_track_quat('-Z','Y').to_euler()
        PREVIEW.parent.mkdir(parents=True,exist_ok=True)
        bpy.ops.render.render(write_still=True)
        assert PREVIEW.is_file() and PREVIEW.stat().st_size > 1024
        print('PREVIEW '+str(PREVIEW),flush=True)
    finally:
        scene.camera = None
        for helper in helpers:
            bpy.data.objects.remove(helper,do_unlink=True)
        for obj,(loc,rot) in zip(objects,saved):
            obj.location,obj.rotation_euler = loc,rot
        for mat in MATS.values():
            mat.diffuse_color = (.5,.5,.5,1)


def main():
    global PARTS
    bpy.ops.wm.read_factory_settings(use_empty=True)
    for mat in list(bpy.data.materials):
        bpy.data.materials.remove(mat)
    scene = bpy.context.scene
    scene.unit_settings.system,scene.unit_settings.scale_length = 'METRIC',1.0
    data = json.loads(OUTLINES.read_text(encoding='utf-8'))
    assert set(data) == set(KEYS)
    assert all(len(data[key]['outline']) == 72 for key in KEYS)
    materials()
    OUTPUT.mkdir(parents=True,exist_ok=True)
    objects = []
    for i,key in enumerate(KEYS):
        PARTS = []
        seed = 73117+i*7919
        random.seed(seed)
        depth = 7 if key == 'Center' else (8.6,9.4,9.8,8.8,9.1)[i-1]
        body = MeltBody(key,data[key],depth)
        drips(body,rim_overflows(body))
        melt_tongues(body)
        tips = [[round(p.x,6),round(p.z,6),round(p.y,6)] for p in body.tip_centers]
        skin_details(body)
        embedded_clocks(body)
        objects.append(finish('Under_'+key,26000 if key == 'Center' else 20000,
                              body,tips,seed))
    assert len(scene.objects) == 6 and all(obj.type == 'MESH' for obj in scene.objects)
    preview(objects,data)
    assert len(scene.objects) == 6 and all(obj.type == 'MESH' for obj in scene.objects)
    assert all(obj.location.length < 1e-7 for obj in objects)
    print('DONE: six FBX, six tips JSON, two-view preview',flush=True)


if __name__ == '__main__':
    main()
