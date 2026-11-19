# 거울 세계 전투방의 대형 장애물 랜드마크 7종과 검토용 미리보기를 만듭니다.
# 실행: "C:/Program Files/Blender Foundation/Blender 5.3/blender.exe" -b --factory-startup -P Art/tools/blender_kit_ch3_landmarks.py
"""Seven deterministic, metre-scale landmarks; stock Blender 5.x only."""

import math
import random
from pathlib import Path

import bmesh
import bpy
from mathutils import Euler, Matrix, Vector
from mathutils.bvhtree import BVHTree


ROOT = Path(__file__).resolve().parents[2]
OUTPUT = ROOT / 'Game/Assets/Art/Characters/Ch3'
PREVIEW = ROOT / 'Art/tools/specs/blender_kit_ch3_landmarks_preview.png'
ROLES = ('sandstone', 'sandstone_light', 'sandstone_dark', 'rock', 'rock_dark',
         'sand', 'marble', 'marble_dark', 'gold', 'gold_dark', 'mirror',
         'mirror_back', 'crystal', 'crystal_core', 'water', 'water_foam',
         'cloud', 'cloud_shade', 'cloth_rose', 'stardust', 'wood', 'roof_tile',
         'window_lit', 'glaze_teal', 'coral', 'ivory', 'bronze', 'rose_gold',
         'gem_rose', 'gem_amber', 'crystal_rose', 'tile_cream')
HARD = {'sandstone', 'sandstone_light', 'sandstone_dark', 'rock', 'rock_dark',
        'marble', 'marble_dark', 'gold', 'gold_dark', 'glaze_teal', 'coral',
        'ivory', 'bronze', 'rose_gold', 'tile_cream'}
PALETTE = {
    'sandstone': (.67, .35, .31), 'sandstone_light': (.85, .58, .45),
    'sandstone_dark': (.43, .22, .24), 'rock': (.48, .35, .39),
    'rock_dark': (.24, .18, .29), 'sand': (.84, .65, .43),
    'marble': (.90, .88, .83), 'marble_dark': (.47, .43, .49),
    'gold': (.96, .62, .16), 'gold_dark': (.45, .26, .075),
    'mirror': (.43, .68, .79), 'mirror_back': (.24, .29, .37),
    'crystal': (.65, .83, .89), 'crystal_core': (.98, .99, 1.0),
    'stardust': (1.0, .86, .53),
    'glaze_teal': (.09, .49, .48), 'coral': (.88, .38, .33),
    'ivory': (.98, .92, .72), 'bronze': (.47, .29, .16),
    'rose_gold': (.84, .48, .35), 'gem_rose': (.91, .29, .52),
    'gem_amber': (1.0, .41, .07), 'crystal_rose': (.87, .53, .72),
    'tile_cream': (.87, .79, .61), 'window_lit': (1.0, .91, .48),
}
MATS = {}
PARTS = []


def materials():
    for role in ROLES:
        mat = bpy.data.materials.new(role)
        mat.use_nodes = True
        mat.diffuse_color = (.5, .5, .5, 1)
        nodes = mat.node_tree.nodes
        nodes.clear()
        shader = nodes.new('ShaderNodeBsdfPrincipled')
        shader.inputs['Base Color'].default_value = (.5, .5, .5, 1)
        shader.inputs['Metallic'].default_value = 0
        shader.inputs['Roughness'].default_value = .8
        output = nodes.new('ShaderNodeOutputMaterial')
        mat.node_tree.links.new(shader.outputs['BSDF'], output.inputs['Surface'])
        MATS[role] = mat


def mesh(label, verts, faces, role, bevel=True, smooth=False):
    data = bpy.data.meshes.new(label)
    data.from_pydata(verts, [], faces)
    data.materials.append(MATS[role])
    data.update()
    bm = bmesh.new()
    bm.from_mesh(data)
    bmesh.ops.recalc_face_normals(bm, faces=list(bm.faces))
    bm.to_mesh(data)
    bm.free()
    for face in data.polygons:
        face.use_smooth = smooth
    obj = bpy.data.objects.new(label, data)
    bpy.context.collection.objects.link(obj)
    if bevel and role in HARD:
        mod = obj.modifiers.new('Worn edges 0.025m', 'BEVEL')
        mod.width, mod.segments = .025, 2
        mod.limit_method, mod.angle_limit = 'ANGLE', math.radians(45)
    PARTS.append(obj)
    return obj


def transform(obj, loc=(0, 0, 0), rot=(0, 0, 0)):
    obj.data.transform(Matrix.Translation(Vector(loc)) @
                       Euler(rot).to_matrix().to_4x4())
    return obj


def box(label, loc, size, role, rot=(0, 0, 0), bevel=True):
    x, y, z = (s / 2 for s in size)
    verts = [(-x, -y, -z), (x, -y, -z), (x, y, -z), (-x, y, -z),
             (-x, -y, z), (x, -y, z), (x, y, z), (-x, y, z)]
    faces = [(0, 3, 2, 1), (4, 5, 6, 7), (0, 1, 5, 4),
             (1, 2, 6, 5), (2, 3, 7, 6), (3, 0, 4, 7)]
    return transform(mesh(label, verts, faces, role, bevel), loc, rot)


def lathe(label, rings, role, n=16, bevel=True, smooth=False):
    """Closed surface of revolution, rings = (z, radius). No zero radii."""
    verts = [(r * math.cos(j * math.tau / n), r * math.sin(j * math.tau / n), z)
             for z, r in rings for j in range(n)]
    faces = [tuple(reversed(range(n)))]
    faces += [(i*n+j, i*n+(j+1) % n, (i+1)*n+(j+1) % n, (i+1)*n+j)
              for i in range(len(rings)-1) for j in range(n)]
    faces.append(tuple(range((len(rings)-1)*n, len(rings)*n)))
    return mesh(label, verts, faces, role, bevel, smooth)


def cylinder(label, loc, radius, depth, role, n=12, top=None,
             rot=(0, 0, 0), bevel=True):
    obj = lathe(label, [(-depth/2, radius),
                       (depth/2, radius if top is None else top)], role, n, bevel)
    return transform(obj, loc, rot)


def tube(label, points, radius, role, n=6, bevel=True):
    verts = []
    for i, point in enumerate(points):
        tangent = (Vector(points[min(i+1, len(points)-1)]) -
                   Vector(points[max(0, i-1)])).normalized()
        reference = Vector((0, 1, 0)) if abs(tangent.y) < .9 else Vector((1, 0, 0))
        u = reference.cross(tangent).normalized()
        v = tangent.cross(u)
        verts += [tuple(Vector(point) + radius *
                        (u*math.cos(j*math.tau/n) + v*math.sin(j*math.tau/n)))
                  for j in range(n)]
    faces = [tuple(reversed(range(n)))]
    faces += [(i*n+j, i*n+(j+1) % n, (i+1)*n+(j+1) % n, (i+1)*n+j)
              for i in range(len(points)-1) for j in range(n)]
    faces.append(tuple(range((len(points)-1)*n, len(points)*n)))
    return mesh(label, verts, faces, role, bevel)


def rod(label, a, b, radius, role, n=6, bevel=True):
    return tube(label, [a, b], radius, role, n, bevel)


def extrude(label, polygon, depth, role, y=0, bevel=True):
    n = len(polygon)
    verts = [(x, y+d, z) for d in (-depth/2, depth/2) for x, z in polygon]
    faces = [tuple(reversed(range(n))), tuple(range(n, 2*n))]
    faces += [(j, (j+1) % n, (j+1) % n+n, j+n) for j in range(n)]
    return mesh(label, verts, faces, role, bevel)


def hoop(label, loc, radius, band, depth, role, n=32):
    verts = [(r*math.cos(j*math.tau/n), y, r*math.sin(j*math.tau/n))
             for r, y in ((radius-band, -depth/2), (radius, -depth/2),
                          (radius, depth/2), (radius-band, depth/2))
             for j in range(n)]
    faces = [(k*n+j, k*n+(j+1) % n, ((k+1) % 4)*n+(j+1) % n,
              ((k+1) % 4)*n+j) for k in range(4) for j in range(n)]
    return transform(mesh(label, verts, faces, role), loc)


def lump(label, loc, size, role, detail=1):
    bm = bmesh.new()
    bmesh.ops.create_icosphere(bm, subdivisions=detail, radius=1)
    for vert in bm.verts:
        vert.co *= random.uniform(.89, 1.11)
        vert.co = Vector(loc) + Vector(tuple(vert.co[i]*size[i]/2 for i in range(3)))
    bm.verts.ensure_lookup_table()
    obj = mesh(label, [tuple(v.co) for v in bm.verts],
               [tuple(v.index for v in f.verts) for f in bm.faces], role)
    bm.free()
    return obj


def mound(label, width, depth, height, role='sand', dune=False):
    """Solid continuous ground footprint; asymmetric crest and smooth top."""
    n = 24
    verts = []
    for ring, (r, z) in enumerate(((1, 0), (.93, .14), (.69, .55),
                                  (.36, .88), (.09, 1))):
        for j in range(n):
            a = j*math.tau/n
            ripple = 1 + .035*math.sin(3*a+.4) + .025*math.cos(5*a)
            shift = (1-r)*(.53 if dune else .13)
            x = width/2*(r*math.cos(a)*ripple + shift)
            y = depth/2*(r*math.sin(a)*ripple - shift*.30)
            zz = height*z*(1 + .045*math.sin(2*a)) if ring else 0
            verts.append((x, y, zz))
    faces = [tuple(reversed(range(n)))]
    faces += [(i*n+j, i*n+(j+1) % n, (i+1)*n+(j+1) % n, (i+1)*n+j)
              for i in range(4) for j in range(n)]
    verts.append((width*(.265 if dune else .065), -depth*.03, height))
    faces += [(4*n+j, 4*n+(j+1) % n, len(verts)-1) for j in range(n)]
    return mesh(label, verts, faces, role, bevel=False, smooth=True)


def crystal(label, a, b, radius, core=False, role='crystal'):
    """Hexagonal prism, a single pointed termination, an actual inner core.

    The inner core is exposed in a recessed split in the +Y prism facet;
    it therefore reads in opaque material previews without shader tricks.
    """
    a, b = Vector(a), Vector(b)
    length = (b-a).length
    verts = [(r*math.cos(j*math.tau/6), r*math.sin(j*math.tau/6), z)
             for z, r in ((0, radius*.77), (length*.74, radius))
             for j in range(6)]
    verts.append((radius*.13, -radius*.08, length))
    faces = [tuple(reversed(range(6)))]
    faces += [(j, (j+1) % 6, (j+1) % 6+6, j+6) for j in range(6)]
    faces += [(j+6, (j+1) % 6+6, 12) for j in range(6)]
    if core:
        # Replace the forward facet with a recessed slit capped by the core.
        index = 2  # j=1, normal +Y
        outer = faces[index]
        mid = sum((Vector(verts[k]) for k in outer), Vector())/4
        inner = []
        for k in outer:
            v = mid + (Vector(verts[k])-mid)*.68
            v.y -= radius*.12
            inner.append(len(verts))
            verts.append(tuple(v))
        faces[index] = tuple(inner)
        for j in range(4):
            faces.append((outer[j], outer[(j+1) % 4], inner[(j+1) % 4], inner[j]))
    obj = mesh(label, verts, faces, role, bevel=False)
    if core:
        obj.data.materials.append(MATS['crystal_core'])
        obj.data.polygons[2].material_index = 1
    matrix = Matrix.Translation(a) @ (b-a).to_track_quat('Z', 'Y').to_matrix().to_4x4()
    obj.data.transform(matrix)
    if core:
        inside = lathe(label+' inner luminous prism',
                       [(length*.14, radius*.33), (length*.78, radius*.36),
                        (length*.91, radius*.025)], 'crystal_core', n=6, bevel=False)
        inside.data.transform(matrix)
    return obj


def octahedron(label, loc, radius, role='stardust'):
    x, y, z = loc
    verts = [(x+radius,y,z), (x,y+radius,z), (x-radius,y,z),
             (x,y-radius,z), (x,y,z+radius*1.6), (x,y,z-radius*1.6)]
    return mesh(label, verts, [(i, (i+1) % 4, p) for p in (4, 5) for i in range(4)],
                role, bevel=False)


def ring_sector(label, loc, inner, outer, start, end, depth, role,
                horizontal=False, steps=3):
    """A closed, bevelled masonry wedge with readable joints between sectors."""
    angles = [start+(end-start)*i/steps for i in range(steps+1)]
    polygon = [(outer*math.cos(a), outer*math.sin(a)) for a in angles]
    polygon += [(inner*math.cos(a), inner*math.sin(a)) for a in reversed(angles)]
    obj = extrude(label, polygon, depth, role)
    rotation = (math.pi/2, 0, 0) if horizontal else (0, 0, 0)
    return transform(obj, loc, rotation)


def fluted_column(label, loc, radius, height, role='marble'):
    """Eight broad concave flutes, stopped short of the shaft's end collars."""
    n = 32
    rings = [(0, .98), (.10, 1), (height-.12, .87), (height, .86)]
    verts = []
    for z, taper in rings:
        for j in range(n):
            # Four points per flute retain a round arris and a recessed trough.
            flute = (1, .83, .74, .83)[j % 4]
            r = radius*taper*flute
            a = j*math.tau/n
            verts.append((r*math.cos(a), r*math.sin(a), z))
    faces = [tuple(reversed(range(n)))]
    faces += [(i*n+j, i*n+(j+1) % n, (i+1)*n+(j+1) % n, (i+1)*n+j)
              for i in range(3) for j in range(n)]
    faces.append(tuple(range(3*n, 4*n)))
    obj = mesh(label, verts, faces, role)
    obj.data.materials.append(MATS['marble_dark'])
    for i, face in enumerate(obj.data.polygons[1:-1]):
        if i % 4 == 1:
            face.material_index = 1
    return transform(obj, loc)


def scroll(label, loc, scale=1, flip=1, role='gold'):
    points = []
    for i in range(12):
        t = i/11
        angle = -.6+t*math.pi*1.75
        r = scale*(.24-.16*t)
        points.append((loc[0]+flip*r*math.cos(angle), loc[1],
                       loc[2]+r*math.sin(angle)))
    tube(label, points, max(.04, scale*.045), role, n=5, bevel=False)


def capital(label, loc, width=.84):
    x, y, z = loc
    cylinder(label+' gold astragal', (x,y,z), width*.37, .10, 'gold', n=12)
    cylinder(label+' carved bell', (x,y,z+.12), width*.34, .18,
             'ivory', n=8, top=width*.48)
    box(label+' ivory abacus', (x,y,z+.27), (width,width,.12), 'ivory')
    box(label+' gold crown line', (x,y,z+.35),
        (width*1.04,width*1.04,.06), 'gold')
    for side in (-1,1):
        scroll(label+' volute', (x+side*width*.27,y+width*.48,z+.17),
               scale=width*.60, flip=side)


def sun_disc(label, loc, radius, role='gold', rays=8):
    x, y, z = loc
    cylinder(label+' boss', loc, radius*.60, .07, role, n=12,
             rot=(math.pi/2,0,0))
    for i in range(rays):
        a = i*math.tau/rays
        u = Vector((math.cos(a),math.sin(a)))
        v = Vector((-u.y,u.x))
        points = [radius*.68*u+radius*.19*v, radius*u,
                  radius*.68*u-radius*.19*v]
        extrude(label+' ray', [(x+p.x,z+p.y) for p in points], .06, role, y=y)


def mosaic(label, loc, columns, rows, pitch, roles=('tile_cream','glaze_teal'),
           vertical=False):
    """Raised tile faces over a darker bed give real recessed grout joints."""
    x, y, z = loc
    for row in range(rows):
        for column in range(columns):
            dx = (column-(columns-1)/2)*pitch
            dy = (row-(rows-1)/2)*pitch
            pos = (x+dx,y,z+dy) if vertical else (x+dx,y+dy,z)
            size = (pitch-.04,.06,pitch-.04) if vertical else (pitch-.04,pitch-.04,.06)
            box(label+' tessera', pos, size, roles[(row+column) % len(roles)])


def star(label, loc, radius, role):
    x, y, z = loc
    polygon = [(x+radius*(1 if i % 2 == 0 else .46)*math.sin(i*math.pi/5),
                z+radius*(1 if i % 2 == 0 else .46)*math.cos(i*math.pi/5))
               for i in range(10)]
    return extrude(label, polygon, .07, role, y=y)


def crescent(label, loc, radius, role):
    x, y, z = loc
    # Both arcs share their tips, forming a solid crescent-shaped relief.
    outer = [(x+radius*math.cos(a),z+radius*math.sin(a))
             for a in [math.pi/3+i*(4*math.pi/3)/12 for i in range(13)]]
    inner = [(x+radius*.39-radius*.71*math.cos(a),z+radius*.866*math.sin(a))
             for a in [-math.pi/2+i*math.pi/12 for i in range(1,12)]]
    return extrude(label, outer+inner, .07, role, y=y)


def crystal_collar(label, a, b, radius, role='gold'):
    a, b = Vector(a), Vector(b)
    obj = lathe(label, [(0,radius*.90),(.07,radius*1.08),(.18,radius*.95)],
                role, n=6)
    obj.data.transform(Matrix.Translation(a) @
                       (b-a).to_track_quat('Z','Y').to_matrix().to_4x4())


def arch_ruin():
    # A continuous ruined footing makes the arcade an obstacle, not a doorway.
    box('Continuous buried stylobate', (0, 0, .10), (5, 1.8, .20), 'sandstone_dark')
    box('Stylobate ivory molding', (0,0,.25), (4.94,1.76,.10), 'ivory')
    box('Stylobate gold stringcourse', (0,0,.325), (4.86,1.70,.05), 'gold')
    mosaic('Footing mosaic walk', (0,.58,.38), 15, 1, .28)
    for x in (-1.85, 0, 1.85):
        box('Square dark column base', (x, 0, .48), (.86, 1.12, .34), 'marble_dark')
        mosaic('Column base teal inlay', (x,.57,.49), 3, 1, .24,
               ('glaze_teal','ivory'), vertical=True)
        cylinder('Column torus block', (x, 0, .72), .40, .18, 'ivory', n=12)
        cylinder('Column lower astragal', (x,0,.85), .34, .08, 'gold', n=12)
        fluted_column('Fluted marble shaft', (x,0,.89), .32, 2.08)
        capital('Ionic ruin capital', (x,0,3.0), .84)
    def wedge(cx, start, end, role, broken=False):
        steps = 3
        outer = [(cx+1.22*math.cos(t), 3.31+1.22*math.sin(t))
                 for t in [start+(end-start)*i/steps for i in range(steps+1)]]
        inner = [(cx+.90*math.cos(t), 3.31+.90*math.sin(t))
                 for t in [end-(end-start)*i/steps for i in range(steps+1)]]
        if broken:
            angle = end
            # Alternating radii and angles produce a real saw-tooth fracture.
            jagged = [(cx+r*math.cos(angle+offset), 3.31+r*math.sin(angle+offset))
                      for r, offset in ((1.15,.07),(1.08,-.07),(.99,.055))]
            outer += jagged
        extrude('Jagged arch end' if broken else 'Sandstone arch voussoir',
                outer+inner, .78, role)
    for i in range(7):
        wedge(-.925, i*math.pi/7+.016, (i+1)*math.pi/7-.016,
              'sandstone' if i % 2 == 0 else 'coral')
    for i in range(4):
        wedge(.925, i*.40+.016, (i+1)*.40-.016,
              'coral' if i % 2 == 0 else 'sandstone', i == 3)
    extrude('Oversize gold keystone', [(-1.10,4.29),(-1.18,4.60),
                                      (-.67,4.60),(-.75,4.29)], .88, 'gold')
    octahedron('Keystone amber heart',(-.925,.51,4.43),.14,'gem_amber')
    for cx, stop in ((-.925,math.pi),(.925,1.60)):
        ring_sector('Arch inner ivory archivolt',(cx,.425,3.31),.88,.95,
                    .01,stop-.01,.08,'ivory',steps=12)
        ring_sector('Arch outer gold archivolt',(cx,.425,3.31),1.17,1.23,
                    .01,stop-.01,.08,'gold',steps=12)
    box('Broken relief frieze bed',(-1.12,0,4.59),(2.38,.64,.27),'sandstone_light')
    box('Frieze ivory cornice',(-1.12,0,4.77),(2.48,.74,.08),'ivory')
    box('Frieze gold crown line',(-1.12,0,4.82),(2.48,.74,.04),'gold')
    for x in (-1.94,-1.40,-.86,-.32):
        sun_disc('Frieze sun relief',(x,.38,4.59),.16,'gold',rays=6)
        start = len(PARTS)
        sun_disc('Cornice carved solar rosette',(0,0,0),.21,'coral',rays=6)
        for obj in PARTS[start:]:
            transform(obj,(x,0,4.88),(math.pi/2,0,0))
    for x, y, size, angle in ((-1.10,.50,(.70,.56,.48),.3),
                               (1.02,.43,(.88,.66,.54),-.3),
                               (1.48,-.38,(.64,.72,.42),.6),
                               (-.60,-.45,(.62,.64,.39),-.2)):
        box('Fallen arch rubble', (x,y,.42), size,
            random.choice(('sandstone','sandstone_light','coral')),
            (0,.16,angle))


def crystal_spire():
    mound('Solid mineral mound', 3.6, 3.4, .72, 'rock_dark')
    # The ceremonial ring is buried to its inner edge in the mineral matrix.
    for i in range(12):
        a, b = i*math.tau/12+.022, (i+1)*math.tau/12-.022
        ring_sector('Excavated sandstone ring block',(0,0,.29),1.13,1.70,
                    a,b,.26,'sandstone' if i % 2 else 'sandstone_light',
                    horizontal=True)
        ring_sector('Ring outer gold rim',(0,0,.43),1.61,1.70,
                    a,b,.07,'gold',horizontal=True)
        ring_sector('Ring teal tile inset',(0,0,.44),1.30,1.53,
                    a+.065,b-.065,.07,'glaze_teal',horizontal=True,steps=1)
        angle = (a+b)/2
        octahedron('Ring amber rivet',(1.19*math.cos(angle),1.19*math.sin(angle),.46),
                   .07,'gem_amber')
    for loc, size in (((-.72,.35,.57),(1.2,1.25,.85)),
                      ((.80,-.34,.54),(1.40,1.05,.92)),
                      ((-.36,-.72,.36),(1.4,.85,.64))):
        lump('Rose matrix boulder', loc, size,
             random.choice(('rock','sandstone','sandstone_dark')))
    specs = [((-.38,-.05,.66),(-.50,-.13,4.6),.53),
             ((.43,-.10,.69),(.91,-.21,4.08),.46),
             ((-.88,.02,.46),(-1.45,.06,3.01),.36),
             ((.56,.40,.54),(1.49,.62,2.89),.35),
             ((-.46,.55,.49),(-.93,1.03,2.77),.34),
             ((.01,-.61,.50),(.12,-1.18,3.10),.36),
             ((.84,-.42,.43),(1.46,-.79,2.24),.29),
             ((-.68,-.59,.45),(-1.31,-1.00,2.05),.30),
             ((.02,.59,.60),(.05,1.12,2.35),.29)]
    for i, (a,b,r) in enumerate(specs):
        crystal('Giant hexagonal crystal %02d' % i, a,b,r, core=i<5,
                role='crystal_rose' if i in (1,3,6,8) else 'crystal')
        crystal_collar('Hexagonal bronze socket',a,b,r,'bronze')
        collar = Vector(a)+(Vector(b)-Vector(a)).normalized()*.16
        crystal_collar('Gold crystal root petals',collar,b,r*.93)
    for i, (x,y,z) in enumerate(((-1.17,.34,3.75),(.23,.63,4.16),
                                  (1.20,.36,3.57),(-.92,-.66,4.14),
                                  (.31,-.83,3.69),(.99,-.57,4.36))):
        octahedron('Floating stardust %d' % i, (x,y,z), .095)


def broken_bulb():
    """Thick upper bulb with a jagged, truly missing patch facing +Y."""
    n = 16
    rings = [(1.87,.16),(2.14,.45),(2.48,.73),(2.84,.84),
             (3.15,.72),(3.36,.40)]
    verts = [(r*math.cos(j*math.tau/n), r*math.sin(j*math.tau/n),z)
             for thickness in (0,.065) for z, radius in rings
             for j in range(n) for r in (radius-thickness,)]
    count = n*len(rings)
    # The stepped outline is deliberately asymmetric, not a clean cutout.
    missing = {(2,3),(2,4),(3,2),(3,3),(3,4),(3,5),(4,3)}
    outer = []
    for i in range(len(rings)-1):
        for j in range(n):
            if (i,j) not in missing:
                outer.append((i*n+j,i*n+(j+1) % n,(i+1)*n+(j+1) % n,(i+1)*n+j))
    faces = outer + [tuple(k+count for k in reversed(face)) for face in outer]
    # Close shell walls around the hole and both bulb rims.
    edges = {}
    for face in outer:
        for a,b in zip(face, face[1:]+face[:1]):
            key = tuple(sorted((a,b)))
            edges.setdefault(key, []).append((a,b))
    for uses in edges.values():
        if len(uses) == 1:
            a,b = uses[0]
            faces.append((b,a,a+count,b+count))
    return mesh('Cracked open upper crystal bulb', verts, faces, 'crystal', bevel=False)


def hourglass():
    mound('Wide bank of spilled sand', 3.6, 3.0, .87)
    start = len(PARTS)
    for z in (.28,3.53):
        cylinder('Bronze disc lower molding', (0,0,z-.10), .98, .10, 'bronze', n=20)
        cylinder('Heavy gold hourglass disc', (0,0,z), 1.02, .14, 'gold', n=20)
        cylinder('Ivory disc inset', (0,0,z+.10), .86, .07, 'ivory', n=16)
        cylinder('Rose gold stepped boss', (0,0,z+.15), .58, .07, 'rose_gold', n=12)
        for i in range(12):
            a = i*math.tau/12
            u = Vector((math.cos(a),math.sin(a),0))
            v = Vector((-u.y,u.x,0))
            vertices = []
            for h in (-.04,.04):
                for p in (.96*u+.13*v,1.16*u,.96*u-.13*v):
                    vertices.append((p.x,p.y,z+h))
            mesh('Disc sun ray edge', vertices,
                 [(0,2,1),(3,4,5),(0,1,4,3),(1,2,5,4),(2,0,3,5)],
                 'gold' if i % 2 else 'rose_gold')
            octahedron('Disc rose stud',(.76*u.x,.76*u.y,z+.16),.085,'gem_rose')
    for i in range(4):
        angle = math.pi/4+i*math.pi/2
        cx, cy = .91*math.cos(angle), .91*math.sin(angle)
        for phase, role in ((0,'bronze'),(math.pi,'rose_gold')):
            points = []
            for j in range(13):
                t = j/12
                twist = phase+t*math.tau*1.5
                points.append((cx+.065*math.cos(twist),cy+.065*math.sin(twist),
                               .40+3.01*t))
            tube('Braided '+role+' support',points,.06,role,n=5,bevel=False)
        for z in (.46,1.87,3.37):
            cylinder('Post gold cuff',(cx,cy,z),.13,.12,'gold',n=8)
    bulb = lathe('Lower hourglass crystal bulb', [(.46,.58),(.64,.78),(.93,.83),
                 (1.20,.69),(1.51,.42),(1.84,.16)], 'crystal', n=16, bevel=False)
    bulb.data.materials.append(MATS['crystal_rose'])
    bulb.data.materials.append(MATS['crystal_core'])
    for i, face in enumerate(bulb.data.polygons[1:-1]):
        if i % 16 in (3,4):
            face.material_index = 2
        elif i % 4 == 0:
            face.material_index = 1
    lathe('Sand trapped in lower bulb', [(.48,.50),(.80,.61),(1.24,.11)],
          'crystal_core', n=12, bevel=False)
    broken_bulb()
    lathe('Exposed luminous upper sand heart',[(2.06,.12),(2.36,.30),(2.75,.19)],
          'crystal_core',n=10,bevel=False)
    tube('Upper bulb dark split',[(.25,.70,2.52),(.36,.70,2.77),(.17,.67,3.03)],
         .025,'crystal_rose',n=4,bevel=False)
    cylinder('Gold neck collar', (0,0,1.86), .235, .14, 'gold', n=12)
    tilt = Matrix.Translation(Vector((-.25,-.05,.29))) @ Euler(
        (0,math.radians(20),math.radians(-7))).to_matrix().to_4x4()
    for obj in PARTS[start:]:
        obj.data.transform(tilt)
    pour = [tuple(tilt @ Vector(p)) for p in ((.05,.74,2.81),(.10,.98,2.53),
            (.18,1.10,2.13),(.27,1.12,1.67),(.37,1.08,1.15),(.44,1.02,.66))]
    tube('Frozen chunky curved sand stream', pour, .16, 'sand', n=8, bevel=False)
    for p in ((-.22,.72,3.00),(.29,.86,2.80)):
        q = tilt @ Vector(p)
        octahedron('Detached crystal splinter', q, .10, 'crystal')
    plaque_start = len(PARTS)
    box('Spilled sand marble tablet',(0,0,0),(1.22,.72,.16),'marble')
    box('Tablet inset face',(0,0,.09),(1.04,.56,.06),'glaze_teal')
    # A small dial carved into the exposed face of the tilted tablet.
    dial_start = len(PARTS)
    sun_disc('Tablet solar seal',(0,.02,0),.23,'gold',rays=6)
    for obj in PARTS[dial_start:]:
        obj.data.transform(Matrix.Translation(Vector((0,0,.16))) @
                           Euler((math.pi/2,0,0)).to_matrix().to_4x4())
    tablet_matrix = Matrix.Translation(Vector((-.77,.78,.62))) @ Euler(
        (math.radians(12),math.radians(-12),-.23)).to_matrix().to_4x4()
    for obj in PARTS[plaque_start:]:
        obj.data.transform(tablet_matrix)


def float_rock():
    mound('Grounded island shadow mound', 4.0, 3.4, .60)
    for loc, size in (((-.90,.31,.25),(1.0,.92,.51)),
                      ((.68,-.59,.22),(1.1,.89,.44))):
        lump('Ground rock',loc,size,'rock')
    for i,(a,b,r) in enumerate((((-1.15,-.32,.23),(-1.46,-.42,.94),.18),
                                ((.95,.53,.25),(1.20,.65,.96),.20))):
        crystal('Ground crystal shard',a,b,r,core=True,
                role='crystal_rose' if i else 'crystal')
        crystal_collar('Ground crystal bronze socket',a,b,r,'bronze')
    n = 13
    outline = [(math.cos(j*math.tau/n)*random.uniform(.91,1.04),
                math.sin(j*math.tau/n)*random.uniform(.91,1.04)) for j in range(n)]
    verts = [(.20,.05,1.4)]
    strata = [(1.88,.68,.57),(2.02,.91,.73),(2.28,1.12,.90),
              (2.39,1.26,1.01),(2.77,1.76,1.40),(2.90,1.66,1.33),
              (3.10,1.74,1.39)]
    # Successive ledges follow one outline, exposing wide geological seams.
    wobble = [random.uniform(-.075,.075) for _ in outline]
    for ring,(z,rx,ry) in enumerate(strata):
        for j,(x,y) in enumerate(outline):
            verts.append((x*rx+(6-ring)*.018,y*ry,
                          z+(wobble[j] if ring < 6 else 0)))
    faces = [(0,1+j,1+(j+1) % n) for j in range(n)]
    for ring in range(len(strata)-1):
        for j in range(n):
            a,b = 1+ring*n+j,1+ring*n+(j+1) % n
            faces += [(a,b,b+n),(a,b+n,a+n)]
    faces.append(tuple(range(1+6*n,1+7*n)))
    cone = mesh('Hovering stratified jagged rock cone',verts,faces,'rock_dark')
    for role in ('sandstone_dark','rock','sandstone_light'):
        cone.data.materials.append(MATS[role])
    for i,face in enumerate(cone.data.polygons[n:-1]):
        face.material_index = (1,2,1,2,0,3)[i//(2*n)]
    # Separate flat sandstone slab makes the top surface unambiguous.
    v = [(x*1.73,y*1.38,z) for z in (3.075,3.20) for x,y in outline]
    f = [tuple(reversed(range(n))),tuple(range(n,2*n))]
    f += [(j,(j+1) % n,(j+1) % n+n,j+n) for j in range(n)]
    mesh('Flat sandstone island cap',v,f,'sandstone')
    box('Island mosaic recessed mortar bed',(-.11,.11,3.23),
        (2.20,1.67,.09),'marble_dark')
    mosaic('Island tiled sanctuary',(-.11,.11,3.30),6,4,.34)
    for y in (-.72,.94):
        box('Sanctuary gold boundary',(-.11,y,3.30),(2.2,.055,.07),'gold')
    for x,y,h in ((-.87,-.06,.64),(.45,-.52,.45)):
        box('Tiny ruined column foot',(x,y,3.25),(.48,.46,.15),'marble')
        obj = fluted_column('Tiny broken fluted column',(x,y,3.31),.19,h)
        for vert in obj.data.vertices:
            if vert.co.z > 3.31+h*.8:
                vert.co.z += random.uniform(-.12,.04)
        cylinder('Tiny column gold capital',(x,y,3.31+h),.25,.12,'gold',n=8)
        box('Tiny capital ivory abacus',(x,y,3.41+h),(.48,.45,.08),'ivory')
    for i,(a,b,r) in enumerate((((.21,.28,3.16),(.34,.37,4.20),.24),
                   ((.53,.31,3.15),(.94,.40,3.86),.20),
                   ((-.02,.48,3.15),(-.30,.65,3.83),.17))):
        crystal('Island crystal',a,b,r,core=True,
                role='crystal_rose' if i == 1 else 'crystal')
        crystal_collar('Island crystal gold socket',a,b,r)
    for loc,size in (((-1.20,.30,1.14),(.43,.39,.45)),
                      ((.93,.30,1.04),(.42,.43,.39)),
                      ((-.44,-.65,.96),(.34,.32,.33))):
        lump('Loose floating stone',loc,size,'rock_dark')


def mirror_gate():
    box('Lower carved marble plinth',(0,0,.12),(5,1.4,.24),'marble')
    box('Ivory plinth molding',(0,0,.285),(4.91,1.34,.09),'ivory')
    box('Recessed dark riser',(0,0,.43),(4.72,1.18,.22),'marble_dark')
    mosaic('Teal tiled riser',(0,.61,.43),18,1,.25,
           ('glaze_teal','glaze_teal','ivory'),vertical=True)
    box('Riser gold line',(0,0,.57),(4.82,1.28,.06),'gold')
    box('Upper white marble step',(0,0,.665),(4.65,1.18,.13),'marble')
    center = (0,0,2.85)
    hoop('Great circular gold portal frame',center,2.10,.35,.32,'gold',n=32)
    hoop('Raised bronze outer rim',(0,.19,2.85),2.09,.09,.10,'bronze',n=32)
    hoop('Rose gold annular molding',(0,.23,2.85),1.98,.12,.09,'rose_gold',n=32)
    hoop('Ivory annular inlay',(0,.28,2.85),1.87,.065,.07,'ivory',n=32)
    hoop('Inner gold lip',(0,.31,2.85),1.80,.065,.075,'gold',n=32)
    # A closed thin disc has outward mirror material on BOTH circular faces.
    cylinder('Double sided round mirror',center,1.76,.10,'mirror',n=48,
             rot=(math.pi/2,0,0),bevel=False)
    cylinder('Round mirror dull back',(0,-.08,2.85),1.77,.05,'mirror_back',n=48,
             rot=(math.pi/2,0,0),bevel=False)
    for i in range(16):
        angle = i*math.tau/16
        # Lower rays are buried in the plinth; no spikes outside its footprint.
        if math.sin(angle) < -.86:
            continue
        r, tip, half = 2.065, 2.35 if i % 2 == 0 else 2.27, .095
        u,v = Vector((math.cos(angle),math.sin(angle))),Vector((-math.sin(angle),math.cos(angle)))
        pts = [r*u+half*v,tip*u,r*u-half*v]
        extrude('Chunky sun ray',[(p.x,2.85+p.y) for p in pts],.22,
                'gold' if i % 2 == 0 else 'rose_gold')
        cylinder('Rim jewel gold setting',(1.95*u.x,.32,2.85+1.95*u.y),
                 .13,.07,'gold',n=8,rot=(math.pi/2,0,0))
        octahedron('Rim rose cabochon',(1.95*u.x,.39,2.85+1.95*u.y),.095,'gem_rose')
    for side in (-1,1):
        for z in (1.48,2.85,4.24):
            x = side*(1.95 if z == 2.85 else 1.42)
            scroll('Portal curling filigree',(x,.33,z),.78,flip=side)
    for x in (-2.10,2.10):
        cylinder('Lantern gold pedestal',(x,.12,.88),.16,.36,'gold',n=8)
        cylinder('Lantern luminous chamber',(x,.12,1.23),.17,.32,'window_lit',n=8)
        for dx,dy in ((-.15,-.15),(.15,-.15),(.15,.15),(-.15,.15)):
            rod('Lantern gold mullion',(x+dx,.12+dy,1.05),(x+dx,.12+dy,1.40),
                .035,'gold',n=4)
        roof = lathe('Lantern stepped gold finial',[(1.39,.25),(1.47,.25),
                     (1.61,.08),(1.72,.035)],'gold',n=8)
        transform(roof,(x,.12,0))
        octahedron('Lantern rose crown',(x,.12,1.75),.075,'gem_rose')


def obelisk():
    box('Obelisk lower white step',(0,0,.20),(2.4,2.4,.40),'marble')
    box('Lower step gold reveal',(0,0,.415),(2.28,2.28,.07),'gold')
    box('Obelisk upper white step',(0,0,.60),(1.98,1.98,.40),'marble')
    box('Upper step ivory molding',(0,0,.775),(2.04,2.04,.08),'ivory')
    box('Gold shaft shoe',(0,0,.85),(1.49,1.49,.10),'gold')
    verts = [(x*r,y*r,z) for z,r in ((.88,.66),(5.22,.38))
             for x,y in ((-1,-1),(1,-1),(1,1),(-1,1))]
    faces = [(0,3,2,1),(4,5,6,7),(0,1,5,4),(1,2,6,5),(2,3,7,6),(3,0,4,7)]
    mesh('Tapering rose sandstone obelisk',verts,faces,'sandstone')
    def front(z):
        return .66-(z-.88)*(.28/4.34)+.008

    for side in range(4):
        start = len(PARTS)
        # Every face carries recessed tiled panels and broad astronomical bands.
        for z in (1.38,2.13,3.65,4.98):
            w = front(z)*2+.04
            box('Relief band stone bed',(0,front(z)+.005,z),
                (w,.10,.28),'sandstone_light')
            for dz in (-.18,.18):
                box('Band gold separator',(0,front(z+dz)+.04,z+dz),
                    (w,.08,.05),'gold')
            crescent('Carved crescent',(0,front(z)+.09,z),.12,'ivory')
            for x in (-w*.32,w*.32):
                star('Astral band star',(x,front(z)+.09,z),.10,'gold')
        for z in (1.76,2.61,3.10,4.17):
            y = front(z)
            box('Inlay recessed bronze bed',(0,y+.01,z),(.53,.075,.34),'bronze')
            mosaic('Hieroglyph teal tablet',(0,y+.065,z),2,1,.23,
                   ('glaze_teal','coral'),vertical=True)
            rod('Glyph raised stem',(-.12,y+.115,z-.08),(-.12,y+.115,z+.08),
                .025,'ivory',n=4,bevel=False)
            rod('Glyph cross stroke',(-.19,y+.115,z+.025),(-.045,y+.115,z+.025),
                .025,'ivory',n=4,bevel=False)
            octahedron('Glyph amber punctuation',(.115,y+.12,z),.055,'gem_amber')
        for obj in PARTS[start:]:
            obj.data.transform(Matrix.Rotation(side*math.pi/2,4,'Z'))
    tip = [(-.39,-.39,5.20),(.39,-.39,5.20),(.39,.39,5.20),
           (-.39,.39,5.20),(0,0,6.00)]
    mesh('Gold pyramidion',tip,[(0,3,2,1),(0,1,4),(1,2,4),(2,3,4),(3,0,4)],'gold')
    z = 4.54
    cylinder('Inset clock gold dial',(0,.452,z),.39,.08,'gold',n=24,
             rot=(math.pi/2,0,0))
    cylinder('Clock ivory dial',(0,.51,z),.32,.04,'ivory',n=24,
             rot=(math.pi/2,0,0))
    hoop('Clock layered bronze bezel',(0,.51,z),.39,.055,.07,'bronze',n=24)
    for i in range(12):
        a = i*math.tau/12
        rod('Clock raised hour mark',(.25*math.sin(a),.548,z+.25*math.cos(a)),
            (.29*math.sin(a),.548,z+.29*math.cos(a)),.022,'gold_dark',n=4,bevel=False)
    rod('Clock minute hand',(0,.565,z),(-.06,.565,z+.23),.025,'gold',n=4)
    rod('Clock hour hand',(0,.569,z),(.17,.569,z+.07),.03,'gold',n=4)
    cylinder('Clock hand pin',(0,.585,z),.075,.055,'gold',n=8,
             rot=(math.pi/2,0,0))
    octahedron('Clock amber heart',(0,.64,z),.065,'gem_amber')
    crack = [(-.51,2.34),(-.24,2.56),(-.31,2.77),(.06,3.04),(.14,3.32),(.46,3.52)]
    tube('Diagonal dark fracture',[(x,front(z)+.075,z) for x,z in crack],.026,
         'sandstone_dark',n=4,bevel=False)
    tube('Crack branch',[(-.24,front(2.56)+.075,2.56),(.02,front(2.63)+.075,2.63),
                         (.14,front(2.78)+.075,2.78)],.022,'sandstone_dark',n=4,bevel=False)
    for i,(a,b,r) in enumerate((((.68,.69,.42),(.96,.91,1.65),.22),
                                ((.87,.51,.39),(1.15,.55,1.16),.18))):
        crystal('Obelisk corner crystal',a,b,r,core=True,
                role='crystal_rose' if i else 'crystal')
        crystal_collar('Obelisk crystal gold socket',a,b,r)


def dune():
    mound('Smooth wind sculpted dune',5.6,4.0,1.15,dune=True)
    # The lower half of this oval mirror is genuinely buried inside the dune.
    start = len(PARTS)
    hoop('Buried ornate oval mirror', (0,0,0), .91,.13,.16,'gold',n=28)
    cylinder('Buried mirror glass',(0,0,0),.79,.065,'mirror',n=32,
             rot=(math.pi/2,0,0),bevel=False)
    cylinder('Buried mirror back',(0,-.055,0),.80,.045,'mirror_back',n=32,
             rot=(math.pi/2,0,0))
    hoop('Oval engraved dark gold trim',(0,.10,0),.88,.035,.035,'gold_dark',n=28)
    hoop('Oval ivory inner molding',(0,.11,0),.81,.055,.06,'ivory',n=28)
    for i in range(7):
        a = i*math.pi/6
        x,z = .93*math.cos(a),.93*math.sin(a)
        cylinder('Buried gem gold setting',(x,.10,z),.13,.08,'rose_gold',n=8,
                 rot=(math.pi/2,0,0))
        octahedron('Buried frame gem',(x,.18,z),.095,'gem_rose')
    for side in (-1,1):
        scroll('Buried mirror acanthus',(side*.73,.10,.33),.8,flip=side)
    matrix = Matrix.Translation(Vector((.55,.50,.93))) @ Euler(
        (math.radians(24),math.radians(-24),math.radians(8))).to_matrix().to_4x4()
    oval = Matrix.Diagonal(Vector((1.12,1,.74,1)))
    for obj in PARTS[start:]:
        obj.data.transform(matrix @ oval)
    for loc,size in (((-1.90,.47,.25),(1.03,.81,.53)),
                      ((1.77,-.91,.20),(.85,.76,.41)),
                      ((-1.38,-1.05,.17),(.69,.62,.37))):
        lump('Dune foot sandstone rock',loc,size,
             random.choice(('sandstone','sandstone_dark','rock')),detail=1)
    start = len(PARTS)
    # A broken sanctuary pavement protrudes from the dune's windward flank.
    outline = [(-1.57,-1.10),(1.12,-1.10),(1.55,-.68),(1.20,-.10),
               (1.49,.82),(.85,1.10),(-1.57,1.10)]
    vertices = [(x,y,z) for z in (-.10,.10) for x,y in outline]
    n = len(outline)
    faces = [tuple(reversed(range(n))),tuple(range(n,2*n))]
    faces += [(i,(i+1) % n,(i+1) % n+n,i+n) for i in range(n)]
    mesh('Broken exposed marble pavement',vertices,faces,'marble')
    mosaic('Buried teal sanctuary floor',(-.16,0,.15),8,6,.34,
           ('glaze_teal','tile_cream','coral'))
    for y in (-1.04,1.04):
        box('Pavement gold border',(-.17,y,.15),(2.72,.06,.07),'gold')
    tube('Slab inset fracture',[(-1.25,-1.03,.19),(-.82,-.21,.19),
         (-.98,.05,.19),(-.30,.98,.19)],.027,'marble_dark',n=4,bevel=False)
    slab_matrix = Matrix.Translation(Vector((-.90,.08,1.00))) @ Euler(
        (math.radians(8),math.radians(13),-.10)).to_matrix().to_4x4()
    for obj in PARTS[start:]:
        obj.data.transform(slab_matrix)
    start = len(PARTS)
    fluted_column('Half buried broken column stub',(0,0,0),.49,.70)
    capital('Fallen carved ivory capital',(0,0,.68),1.42)
    for side in (-1,1):
        star('Capital petal relief',(side*.34,.65,.82),.15,'gold')
    fallen = Matrix.Translation(Vector((1.74,-.50,1.02))) @ Euler(
        (math.radians(28),math.radians(-64),.24)).to_matrix().to_4x4()
    for obj in PARTS[start:]:
        obj.data.transform(fallen)
    for i,(a,b,r) in enumerate((((-1.66,.97,.25),(-1.83,1.10,.97),.20),
                                ((1.54,.65,.44),(1.77,.74,1.20),.23),
                                ((1.88,.55,.27),(2.15,.58,.79),.16))):
        crystal('Dune memory crystal sprout',a,b,r,core=True,
                role='crystal_rose' if i == 1 else 'crystal')
        crystal_collar('Dune sprout gold collar',a,b,r)


SPECS = [
    ('LM_ArchRuin',(5.0,1.8,4.6),arch_ruin),
    ('LM_CrystalSpire',(3.6,3.4,4.6),crystal_spire),
    ('LM_Hourglass',(3.6,3.0,4.2),hourglass),
    ('LM_FloatRock',(4.0,3.4,4.2),float_rock),
    ('LM_MirrorGate',(5.0,1.4,5.2),mirror_gate),
    ('LM_Obelisk',(2.4,2.4,6.0),obelisk),
    ('LM_Dune',(5.6,4.0,1.7),dune),
]


def bounds(obj):
    return [Vector(tuple(fn(v.co[i] for v in obj.data.vertices) for i in range(3)))
            for fn in (min,max)]


def triangles(obj):
    obj.data.calc_loop_triangles()
    return len(obj.data.loop_triangles)


def visible_materials(obj):
    """Measure projected colour regions from the review camera, with occlusion."""
    obj.data.calc_loop_triangles()
    verts = [v.co.copy() for v in obj.data.vertices]
    tris = list(obj.data.loop_triangles)
    tree = BVHTree.FromPolygons(verts, [tuple(t.vertices) for t in tris],
                               all_triangles=True)
    angle = math.radians(55)
    view = Vector((0,math.cos(angle),math.sin(angle)))
    distance = max(obj.dimensions)*3
    areas = {}
    for index, tri in enumerate(tris):
        facing = tri.normal.dot(view)
        if facing <= 0:
            continue
        center = sum((verts[i] for i in tri.vertices),Vector())/3
        _, _, hit, _ = tree.ray_cast(center+view*distance,-view,distance+.01)
        if hit == index:
            role = obj.data.materials[tri.material_index].name
            areas[role] = areas.get(role,0)+tri.area*facing
    total = sum(areas.values())
    assert total > 0, obj.name+' has no visible faces'
    return {role:area/total for role,area in areas.items()}


def finish(name, dims):
    bpy.ops.object.select_all(action='DESELECT')
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
    obj.name, obj.data.name = name, name+'_Mesh'
    # Only bevel tessellation may need reduction; preserve material regions.
    if triangles(obj) > 8000:
        mod = obj.modifiers.new('Landmark triangle budget','DECIMATE')
        mod.ratio, mod.delimit = 7800/triangles(obj), {'MATERIAL'}
        bpy.ops.object.modifier_apply(modifier=mod.name)
    assert triangles(obj) <= 8000, (name,triangles(obj))
    lo, hi = bounds(obj)
    span, center = hi-lo, (hi+lo)/2
    for v in obj.data.vertices:
        v.co.x = (v.co.x-center.x)*dims[0]/span.x
        v.co.y = (v.co.y-center.y)*dims[1]/span.y
        v.co.z = (v.co.z-lo.z)*dims[2]/span.z
    bm = bmesh.new()
    bm.from_mesh(obj.data)
    bmesh.ops.recalc_face_normals(bm,faces=list(bm.faces))
    assert all(e.is_manifold for e in bm.edges), name+' has an open/nonmanifold edge'
    bm.to_mesh(obj.data)
    bm.free()
    obj.data.update()
    bpy.context.view_layer.update()
    lo, hi = bounds(obj)
    assert abs(lo.z) < 1e-5 and abs(hi.z-dims[2]) < 1e-5, name
    assert all(abs(obj.dimensions[i]-dims[i]) < 1e-4 for i in range(3)), name
    assert abs(lo.x+hi.x) < 1e-5 and abs(lo.y+hi.y) < 1e-5, name
    assert obj.location.length < 1e-6, name+' origin moved'
    assert all(math.isfinite(c) for v in obj.data.vertices for c in v.co), name
    for mat in obj.data.materials:
        assert mat.name in ROLES and tuple(mat.diffuse_color) == (.5,.5,.5,1)
        assert tuple(mat.node_tree.nodes['Principled BSDF'].inputs['Base Color'].default_value) == (.5,.5,.5,1)
        assert not any(n.type == 'TEX_IMAGE' for n in mat.node_tree.nodes)
    coverage = visible_materials(obj)
    assert len(coverage) >= 6, (name,coverage)
    print('VISIBLE '+name+' '+str({role:round(share,3)
          for role,share in sorted(coverage.items(),key=lambda item:-item[1])}),flush=True)
    assert max(coverage.values()) <= .56, (name,'dominant material',coverage)
    obj['chapter'],obj['front'] = 'Mirage Desert / Mirror World','+Y'
    obj['footprint_and_height_m'],obj['triangle_count'] = list(dims),triangles(obj)
    obj['solid_obstacle'] = True
    destination = OUTPUT / ('Kit_'+name+'.fbx')
    bpy.ops.export_scene.fbx(
        filepath=str(destination), use_selection=True,
        axis_forward='-Z', axis_up='Y', apply_scale_options='FBX_SCALE_UNITS',
        bake_space_transform=True, object_types={'MESH'}, use_mesh_modifiers=True,
        mesh_smooth_type='FACE', add_leaf_bones=False, bake_anim=False,
    )
    assert destination.is_file() and destination.stat().st_size > 1024, destination
    print(f'EXPORTED {name} {triangles(obj)} {tuple(round(d,4) for d in obj.dimensions)}',flush=True)
    return obj


def preview(objects):
    """Review-only colours and transforms, applied strictly after all exports."""
    scene = bpy.context.scene
    scene.render.engine = 'BLENDER_WORKBENCH'
    scene.render.resolution_x,scene.render.resolution_y = 1600,900
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
    shading.background_type,shading.background_color = 'WORLD',(.035,.043,.063)
    if scene.world is None:
        scene.world = bpy.data.worlds.new('Review world')
    scene.world.color = (.035,.043,.063)
    old_locations = [obj.location.copy() for obj in objects]
    helpers = []
    try:
        for role,mat in MATS.items():
            mat.diffuse_color = (*PALETTE.get(role,(.5,.5,.5)),1)
        gap = .85
        total = sum(dims[0] for _,dims,_ in SPECS)+gap*(len(objects)-1)
        x = -total/2
        for obj,(_,dims,_) in zip(objects,SPECS):
            obj.location.x = x+dims[0]/2
            x += dims[0]+gap
        camera = bpy.data.objects.new('Review camera',bpy.data.cameras.new('Review camera'))
        scene.collection.objects.link(camera)
        helpers.append(camera)
        scene.camera = camera
        camera.data.type,camera.data.ortho_scale = 'ORTHO',total+1.0
        target = Vector((0,0,1.5))
        a = math.radians(55)
        camera.location = target+Vector((0,35*math.cos(a),35*math.sin(a)))
        camera.rotation_euler = (target-camera.location).to_track_quat('-Z','Y').to_euler()
        for obj in objects:
            data = bpy.data.curves.new('Review caption','FONT')
            data.body,data.align_x,data.size = obj.name,'CENTER',.24
            data.materials.append(MATS['crystal_core'])
            caption = bpy.data.objects.new('Caption '+obj.name,data)
            scene.collection.objects.link(caption)
            caption.location = (obj.location.x,2.75,-.20)
            caption.rotation_euler = camera.rotation_euler
            helpers.append(caption)
        PREVIEW.parent.mkdir(parents=True,exist_ok=True)
        bpy.ops.render.render(write_still=True)
        assert PREVIEW.is_file() and PREVIEW.stat().st_size > 1024
        print('PREVIEW '+str(PREVIEW),flush=True)
    finally:
        scene.camera = None
        for obj in helpers:
            bpy.data.objects.remove(obj,do_unlink=True)
        for obj,loc in zip(objects,old_locations):
            obj.location = loc
        for mat in MATS.values():
            mat.diffuse_color = (.5,.5,.5,1)


def main():
    global PARTS
    bpy.ops.wm.read_factory_settings(use_empty=True)
    for mat in list(bpy.data.materials):
        bpy.data.materials.remove(mat)
    scene = bpy.context.scene
    scene.unit_settings.system,scene.unit_settings.scale_length = 'METRIC',1.0
    materials()
    OUTPUT.mkdir(parents=True,exist_ok=True)
    exported = []
    for i,(name,dims,build) in enumerate(SPECS):
        PARTS = []
        random.seed(33181+i*7919)
        build()
        exported.append(finish(name,dims))
    assert len(scene.objects) == 7 and all(obj.type == 'MESH' for obj in scene.objects)
    preview(exported)
    assert len(scene.objects) == 7 and all(obj.type == 'MESH' for obj in scene.objects)
    print('DONE',flush=True)


if __name__ == '__main__':
    main()
