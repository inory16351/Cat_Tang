# 3장 거울 세계의 장식 벽 모듈 7종과 미리보기를 만듭니다.
# 실행: "C:/Program Files/Blender Foundation/Blender 5.3/blender.exe" -b --factory-startup -P Art/tools/blender_kit_ch3_walls.py
"""Deterministic, metre-scale Blender 5.x wall kit; stock Blender only."""

import math
import random
from pathlib import Path

import bmesh
import bpy
from mathutils import Euler, Matrix, Vector


ROOT = Path(__file__).resolve().parents[2]
OUTPUT = ROOT / 'Game/Assets/Art/Characters/Ch3'
PREVIEW = ROOT / 'Art/tools/specs/blender_kit_ch3_walls_preview.png'
PALETTE = {
    'sandstone': (.67, .32, .38), 'sandstone_light': (.94, .62, .59),
    'sandstone_dark': (.35, .14, .23), 'rock': (.45, .31, .40),
    'rock_dark': (.22, .17, .24), 'sand': (.83, .53, .39),
    'marble': (.91, .87, .78), 'marble_dark': (.43, .43, .51),
    'gold': (.94, .61, .18), 'gold_dark': (.43, .25, .07),
    'mirror': (.47, .71, .83), 'mirror_back': (.23, .23, .34),
    'crystal': (.71, .86, .96), 'crystal_core': (1, .97, .85),
    'cloud': (.88, .87, .98), 'cloud_shade': (.51, .48, .68),
    'wood': (.35, .19, .22), 'roof_tile': (.65, .22, .31),
    'window_lit': (1, .83, .43),
    'glaze_teal': (.12, .52, .54), 'coral': (.84, .35, .39),
    'ivory': (.98, .90, .71), 'bronze': (.49, .30, .16),
    'rose_gold': (.83, .47, .38), 'gem_rose': (.92, .36, .60),
    'gem_amber': (1, .62, .12), 'crystal_rose': (.90, .61, .81),
    'tile_cream': (.83, .76, .61),
}
HARD = {'sandstone', 'sandstone_light', 'sandstone_dark', 'rock',
        'rock_dark', 'marble', 'marble_dark', 'gold', 'gold_dark',
        'glaze_teal', 'coral', 'ivory', 'bronze', 'rose_gold', 'tile_cream'}
MATS = {}
PARTS = []


def setup_materials():
    for role in PALETTE:
        mat = bpy.data.materials.new(role)
        mat.use_nodes = True
        mat.diffuse_color = (.5, .5, .5, 1)
        nodes = mat.node_tree.nodes
        nodes.clear()
        shader = nodes.new('ShaderNodeBsdfPrincipled')
        shader.inputs['Base Color'].default_value = (.5, .5, .5, 1)
        shader.inputs['Metallic'].default_value = 0
        shader.inputs['Roughness'].default_value = .8
        out = nodes.new('ShaderNodeOutputMaterial')
        mat.node_tree.links.new(shader.outputs['BSDF'], out.inputs['Surface'])
        MATS[role] = mat


def mesh(label, verts, faces, role, bevel=True):
    data = bpy.data.meshes.new(label)
    data.from_pydata(verts, [], faces)
    data.update()
    obj = bpy.data.objects.new(label, data)
    bpy.context.collection.objects.link(obj)
    data.materials.append(MATS[role])
    if bevel and role in HARD:
        mod = obj.modifiers.new('Worn edges 3cm', 'BEVEL')
        mod.width, mod.segments = .03, 2
        mod.limit_method, mod.angle_limit = 'ANGLE', math.radians(65)
    PARTS.append(obj)
    return obj


def place(obj, loc=(0, 0, 0), rot=(0, 0, 0)):
    obj.data.transform(Matrix.Translation(Vector(loc)) @
                       Euler(rot).to_matrix().to_4x4())
    return obj


def box(label, loc, size, role, rot=(0, 0, 0), bevel=True):
    x, y, z = (s / 2 for s in size)
    verts = [(-x, -y, -z), (x, -y, -z), (x, y, -z), (-x, y, -z),
             (-x, -y, z), (x, -y, z), (x, y, z), (-x, y, z)]
    return place(mesh(label, verts, [(0, 3, 2, 1), (4, 5, 6, 7),
                 (0, 1, 5, 4), (1, 2, 6, 5), (2, 3, 7, 6), (3, 0, 4, 7)],
                 role, bevel), loc, rot)


def extrude(label, profile, y0, y1, role, bevel=True):
    """Closed X/Z polygon prism, including concave arch spandrels."""
    n = len(profile)
    verts = [(x, y, z) for y in (y0, y1) for x, z in profile]
    faces = [tuple(reversed(range(n))), tuple(range(n, 2 * n))]
    faces += [(i, (i + 1) % n, (i + 1) % n + n, i + n) for i in range(n)]
    return mesh(label, verts, faces, role, bevel)


def tube(label, points, radius, role, n=6):
    verts = []
    for i, point in enumerate(points):
        tangent = Vector(points[min(i + 1, len(points) - 1)]) - Vector(points[max(0, i - 1)])
        tangent.normalize()
        reference = Vector((1, 0, 0)) if abs(tangent.y) > .9 else Vector((0, 1, 0))
        side = reference.cross(tangent).normalized()
        other = tangent.cross(side)
        for j in range(n):
            a = j * math.tau / n
            verts.append(tuple(Vector(point) + radius * (side * math.cos(a) + other * math.sin(a))))
    faces = [tuple(reversed(range(n))), tuple(range(len(verts) - n, len(verts)))]
    faces += [(i * n + j, i * n + (j + 1) % n,
               (i + 1) * n + (j + 1) % n, (i + 1) * n + j)
              for i in range(len(points) - 1) for j in range(n)]
    return mesh(label, verts, faces, role)


def cylinder(label, loc, radius, depth, role, n=12, rot=(0, 0, 0)):
    verts = [(radius*math.cos(i*math.tau/n), radius*math.sin(i*math.tau/n), z)
             for z in (-depth/2, depth/2) for i in range(n)]
    faces = [tuple(reversed(range(n))), tuple(range(n, 2*n))]
    faces += [(i, (i+1)%n, (i+1)%n+n, i+n) for i in range(n)]
    return place(mesh(label, verts, faces, role), loc, rot)


def gem(label, x, y, z, radius, role):
    # A broad faceted cabochon, readable through the game's outline shader.
    verts = [(x+radius*math.cos(i*math.tau/8), y, z+radius*math.sin(i*math.tau/8))
             for i in range(8)] + [(x, y+.10, z), (x, y-.04, z)]
    faces = [(i, (i+1)%8, 8) for i in range(8)]
    faces += [((i+1)%8, i, 9) for i in range(8)]
    return mesh(label, verts, faces, role, False)


def scroll(x, y, z, size, role='gold', sign=1):
    points = []
    for i in range(12):
        angle = i*math.pi*1.65/11
        radius = size*(1-.73*i/11)
        points.append((x+sign*radius*math.cos(angle), y, z+radius*math.sin(angle)))
    tube('Carved spiral volute', points, .045, role, n=5)


def sun_disc(x, y, z, radius, role='gold'):
    # Join the rays into one carved plate so simplification cannot open isolated tips.
    n = 16
    outline = [(radius*(1 if i%2 == 0 else .58)*math.cos(i*math.tau/n),
                radius*(1 if i%2 == 0 else .58)*math.sin(i*math.tau/n))
               for i in range(n)]
    rings = ((.88, -.04), (.97, -.028), (1, 0), (.97, .028), (.88, .04))
    verts = [(x+dx*scale, y+depth, z+dz*scale)
             for scale, depth in rings for dx, dz in outline]
    faces = [tuple(reversed(range(n))), tuple(range(4*n, 5*n))]
    faces += [(k*n+i, k*n+(i+1)%n, (k+1)*n+(i+1)%n, (k+1)*n+i)
              for k in range(4) for i in range(n)]
    mesh('Continuous bevelled sunburst relief', verts, faces, role, False)
    cylinder('Raised sun disc', (x, y, z), radius*.54, .075, role,
             n=10, rot=(math.pi/2, 0, 0))


def mosaic(x0, x1, y, z, count, size=.17):
    for i in range(count):
        x = x0+(x1-x0)*(i+.5)/count
        box('Inlaid alternating mosaic tessera', (x, y, z), (size, .065, size),
            ('coral', 'ivory', 'glaze_teal')[i%3], rot=(0, math.pi/4, 0))


def coping_inlay(a, b, y0, y1, role):
    """Carved inlay follows a coping's sloping top, including its gold borders."""
    x0, x1 = a[0]+.08, b[0]-.08
    slope = (b[1]-a[1])/(b[0]-a[0])
    z0, z1 = a[1]+.08*slope, b[1]-.08*slope
    profile = [(x0, z0+.012), (x1, z1+.012), (x1, z1+.065), (x0, z0+.065)]
    extrude('Coping inset enamel panel', profile, y0+.18, y1-.18, role)
    for y in (y0+.15, y1-.15):
        extrude('Coping fine gold border', profile, y-.025, y+.025, 'gold')
    start = len(PARTS)
    radius = max(.08, min(.18, (x1-x0)*.31))
    sun_disc(0, 0, 0, radius, 'bronze')
    transform = (Matrix.Translation(((x0+x1)/2, (y0+y1)/2, (z0+z1)/2+.085)) @
                 Matrix.Rotation(-math.atan(slope), 4, 'Y') @
                 Matrix.Rotation(math.pi/2, 4, 'X'))
    for part in PARTS[start:]:
        part.data.transform(transform)


def band(label, outer, inner, y0, y1, role):
    """Closed planar ornamental ring, without a filled centre."""
    n = len(outer)
    assert n == len(inner)
    verts = [(x, y, z) for y, points in ((y0, outer), (y0, inner),
                                       (y1, outer), (y1, inner)) for x, z in points]
    faces = []
    for i in range(n):
        j = (i + 1) % n
        faces += [(i, j, n + j, n + i), (2*n+i, 3*n+i, 3*n+j, 2*n+j),
                  (i, 2*n+i, 2*n+j, j), (n+i, n+j, 3*n+j, 3*n+i)]
    return mesh(label, verts, faces, role)


def slab(label, x0, x1, y0, y1, z0, z1, role='sandstone', facets=True):
    """Clipped, irregular slab with a flat back, level sole and broad top facets."""
    cut = min((x1-x0) * .13, .18)
    outline = [(x0+cut, y0), (x1-cut, y0), (x1, y0+.15),
               (x1, y1-.15), (x1-cut, y1), (x0+cut, y1),
               (x0, y1-.15), (x0, y0+.15)]
    verts = [(x, y, z0) for x, y in outline]
    for i, (x, y) in enumerate(outline):
        verts.append((x, y, z1 - random.uniform(.03, .16)))
    verts.append(((x0+x1)/2 + random.uniform(-.08, .08), (y0+y1)/2, z1))
    faces = [tuple(reversed(range(8)))]
    faces += [(i, (i+1) % 8, (i+1) % 8+8, i+8) for i in range(8)]
    faces += [(8+i, 8+(i+1) % 8, 16) for i in range(8)]
    obj = mesh(label, verts, faces, role)
    if facets:
        low = 'sandstone_dark' if role == 'sandstone' else 'rock_dark'
        high = 'sandstone_light' if role == 'sandstone' else 'rock'
        obj.data.materials.append(MATS[low])
        obj.data.materials.append(MATS[high])
        for p in obj.data.polygons:
            p.material_index = 2 if p.index >= 9 else (1 if p.index in (0, 1, 2, 8) else 0)
    return obj


def crystal(loc, height, radius=.12, tilt=0, role='crystal', collar='gold'):
    n = 5
    verts = [(radius * math.cos(i*math.tau/n), radius * math.sin(i*math.tau/n), z)
             for z in (0, height*.67) for i in range(n)]
    verts.append((radius*.2, 0, height))
    faces = [tuple(reversed(range(n)))]
    faces += [(i, (i+1) % n, (i+1) % n+n, i+n) for i in range(n)]
    faces += [(n+i, n+(i+1) % n, n*2) for i in range(n)]
    obj = place(mesh('Five sided memory crystal', verts, faces, role), loc, (0, tilt, 0))
    obj.data.materials.append(MATS['crystal_core'])
    obj.data.polygons[-2].material_index = 1
    cylinder('Cast crystal socket', (loc[0], loc[1], loc[2]+.045),
             radius*1.30, .09, collar, n=8, rot=(0, tilt, 0))


def cluster(loc):
    x, y, z = loc
    for dx, dy, h, tilt in [(-.18, .02, .40, -.32), (0, 0, .66, .10), (.18, -.04, .34, .42)]:
        crystal((x+dx, y+dy, z), h, .105, tilt)


def cliff_shore():
    # Ten overlapping, rounded stones with individually carved planar facets.
    rng = random.Random(34117)

    def boulder(label, footprint, bottom, radius, flatten, role):
        bm = bmesh.new()
        bmesh.ops.create_icosphere(bm, subdivisions=2, radius=1)
        rotation = Euler(tuple(rng.uniform(-math.pi, math.pi) for _ in range(3))).to_matrix()
        points = [rotation @ (v.co * rng.uniform(.89, 1.12) +
                  Vector(tuple(rng.uniform(-.045, .045) for _ in range(3))))
                  for v in bm.verts]
        bm.free()
        bm = bmesh.new()
        for point in points:
            bm.verts.new(point)
        bmesh.ops.convex_hull(bm, input=list(bm.verts))
        for vertex in list(bm.verts):
            if not vertex.link_faces:
                bm.verts.remove(vertex)
        # Shave two oblique faces and a broad buried sole, retaining round shoulders.
        for normal, distance in [((.32, .17, 1), .87),
                                 ((-.8, .5, .24), .88), ((0, 0, -1), .65)]:
            normal = Vector(normal).normalized()
            bmesh.ops.bisect_plane(
                bm, geom=list(bm.verts) + list(bm.edges) + list(bm.faces),
                plane_co=normal * distance, plane_no=normal,
                dist=.000001, clear_outer=True)
            boundary = [edge for edge in bm.edges if edge.is_boundary]
            if boundary:
                bmesh.ops.holes_fill(bm, edges=boundary)
        low = Vector(tuple(min(v.co[i] for v in bm.verts) for i in range(3)))
        high = Vector(tuple(max(v.co[i] for v in bm.verts) for i in range(3)))
        x0, x1, y0, y1 = footprint
        for vertex in bm.verts:
            unit = (vertex.co - low)
            vertex.co = (x0 + unit.x / (high.x-low.x) * (x1-x0),
                         y0 + unit.y / (high.y-low.y) * (y1-y0),
                         bottom + unit.z / (high.z-low.z) * radius * 2 * flatten)
        bmesh.ops.recalc_face_normals(bm, faces=list(bm.faces))
        bm.verts.ensure_lookup_table()
        bm.verts.index_update()
        verts = [tuple(v.co) for v in bm.verts]
        faces = [tuple(v.index for v in face.verts) for face in bm.faces]
        assert 40 <= len(faces) <= 120, (label, len(faces))
        bm.free()
        obj = mesh(label, verts, faces, role)
        obj.data.materials.append(MATS['sandstone_light'])
        obj.data.materials.append(MATS['sandstone_dark'])
        for face in obj.data.polygons:
            face.use_smooth = False
            if face.normal.z > .55:
                face.material_index = 1
            elif face.normal.z < -.35 or (face.center.z < .52 and face.normal.z < .2):
                face.material_index = 2
        return obj

    # Low curved ends blend into neighbours; the offset crown rests across two stones.
    stones = [
        ('Left tapered bank', (-3, -1.42, -.55, .69), 0, 1.05, .74),
        ('Broad left shoulder', (-2.06, .12, -.60, .87), 0, 1.55, .86),
        ('Central supporting boulder', (-.65, 1.97, -.58, 1.00), 0, 1.40, .78),
        ('High rounded crown', (-.85, 1.75, -.52, .81), 1.35, 1.42, .76),
        ('Right tapered bank', (1.03, 3, -.56, .75), 0, 1.18, .80),
        ('Front left resting stone', (-2.67, -.62, .27, 1.18), 0, .90, .78),
        ('Front ledge boulder', (-1.12, .98, .44, 1.27), 0, 1.04, .78),
        ('Front right resting stone', (.56, 2.66, .34, 1.23), 0, .88, .73),
        ('Small left waterline stone', (-3, -1.98, .66, 1.34), 0, .50, .65),
        ('Small right waterline stone', (1.98, 3, .64, 1.40), 0, .56, .71),
    ]
    rocks = [boulder(*stone, ('sandstone', 'sandstone_light', 'rock', 'sandstone_dark')[i%4])
             for i, stone in enumerate(stones)]

    # A shallow, curved sand drift closes the foot continuously across the tile.
    verts = []
    sections, sides = 17, 7
    for i in range(sections):
        x = -3 + 6 * i / (sections-1)
        swell = math.sin(math.pi * i / (sections-1))
        height = .09 + .19 * swell + .018 * math.sin(i * 1.7) * swell
        back = .26 + .055 * math.sin(i * 1.1)
        front = 1.30 + .10 * swell
        verts.extend((x, y, z) for y, z in [
            (back, 0), (front, 0), (front-.035, height*.16),
            (front-.19, height*.52), (back+.48, height*.89),
            (back+.20, height), (back-.04, height*.44)])
    faces = [tuple(reversed(range(sides))),
             tuple(range((sections-1)*sides, sections*sides))]
    faces += [(i*sides+j, i*sides+(j+1)%sides,
               (i+1)*sides+(j+1)%sides, (i+1)*sides+j)
              for i in range(sections-1) for j in range(sides)]
    mesh('Rounded sand drift', verts, faces, 'sand', False)

    # Sample both edges against the crown so the narrow raised vein hugs its facets.
    crown = rocks[3]
    verts = []
    samples = 23
    for i in range(samples):
        t = i / (samples-1)
        x, z = -.25 + 1.20*t, 2.15 + 1.00*t
        for side in (-1, 1):
            hit, point, normal, _ = crown.ray_cast(
                Vector((x + side*.025, 3, z - side*.025)), Vector((0, -1, 0)))
            assert hit, ('Gold vein missed crown', i, side)
            verts.extend((tuple(point-normal*.008), tuple(point+normal*.018)))
    end = (samples-1)*4
    faces = [(0, 1, 3, 2), (end, end+2, end+3, end+1)]
    faces += [(i*4+a, (i+1)*4+a, (i+1)*4+b, i*4+b)
              for i in range(samples-1) for a, b in ((0, 2), (2, 3), (3, 1), (1, 0))]
    vein = mesh('Surface following diagonal gold vein', verts, faces, 'gold')
    vein.modifiers[0].width = .02

    def shore_cluster(loc, support=None):
        for index, (dx, dy, height, lean) in enumerate([
                (-.14, .015, .28, -.23), (0, 0, .48, .08), (.14, -.025, .24, .28)]):
            x, y, z = loc[0]+dx, loc[1]+dy, loc[2]
            if support is not None:
                hit, point, _, _ = support.ray_cast(Vector((x, y, 4)), Vector((0, 0, -1)))
                assert hit, 'Ledge crystal needs a stone beneath it'
                z = point.z - .025
            radius, n = .085, 6
            verts = [(radius*math.cos(j*math.tau/n), radius*math.sin(j*math.tau/n), zz)
                     for zz in (0, height*.70) for j in range(n)]
            verts.append((.012, 0, height))
            faces = [tuple(reversed(range(n)))]
            faces += [(j, (j+1)%n, (j+1)%n+n, j+n) for j in range(n)]
            faces += [(n+j, n+(j+1)%n, n*2) for j in range(n)]
            role = 'crystal_rose' if index == 0 else 'crystal'
            obj = place(mesh('Hexagonal shore crystal', verts, faces, role),
                        (x, y, z), (rng.uniform(-.12, .12), lean, rng.uniform(-.3, .3)))
            obj.data.materials.append(MATS['crystal_core'])
            obj.data.polygons[-2].material_index = 1
            cylinder('Bronze shore crystal collar', (x, y, z+.04), .115, .08, 'bronze', n=8)

    shore_cluster((-1.80, 1.23, .12))
    shore_cluster((1.70, 1.25, .12))
    shore_cluster((-.03, .89, 0), rocks[6])

    # A carved column remnant lodged in the front shoulder, with its broken end exposed.
    start = len(PARTS)
    n = 32
    verts = [((.31 if i%2 == 0 else .266)*math.cos(i*math.tau/n),
              (.31 if i%2 == 0 else .266)*math.sin(i*math.tau/n), z)
             for z in (-.50, .50) for i in range(n)]
    faces = [tuple(reversed(range(n))), tuple(range(n, 2*n))]
    faces += [(i, (i+1)%n, (i+1)%n+n, i+n) for i in range(n)]
    mesh('Half buried fluted marble column drum', verts, faces, 'marble')
    cylinder('Gold band around fallen drum', (0, 0, .19), .325, .12, 'gold', n=16)
    cylinder('Gilt drum binding', (0, 0, .29), .326, .11, 'gold', n=16)
    cylinder('Dark broken column end', (0, 0, -.505), .23, .05, 'marble_dark', n=12)
    transform = Matrix.Translation((-1.24, .99, 1.45)) @ Euler((.15, 1.18, -.12)).to_matrix().to_4x4()
    for part in PARTS[start:]:
        part.data.transform(transform)

    extrude('Broken marble tiled step', [(1.22, .16), (2.62, .16), (2.62, .35),
            (2.30, .48), (1.22, .48)], .90, 1.39, 'marble')
    for i in range(4):
        box('Surviving teal step tile', (1.40+i*.25, 1.12, .50),
            (.22, .39, .07), 'glaze_teal' if i != 2 else 'ivory')
    box('Step gold nosing', (1.86, 1.355, .40), (1.22, .07, .075), 'gold')


def turned_capital(x, z):
    n = 16
    profile = [(.30, z-.23), (.37, z-.18), (.37, z-.10), (.45, z-.04), (.45, z)]
    verts = [(x+r*math.cos(i*math.tau/n), .53+r*math.sin(i*math.tau/n), zz)
             for r, zz in profile for i in range(n)]
    faces = [tuple(reversed(range(n))), tuple(range((len(profile)-1)*n, len(profile)*n))]
    faces += [(k*n+i, k*n+(i+1)%n, (k+1)*n+(i+1)%n, (k+1)*n+i)
              for k in range(len(profile)-1) for i in range(n)]
    mesh('Carved ivory spreading capital', verts, faces, 'ivory')
    cylinder('Gold astragal below capital', (x, .53, z-.25), .315, .10, 'gold', n=16)
    for sign in (-1, 1):
        scroll(x+sign*.23, .94, z-.12, .15, 'ivory', sign)
    box('Capital square abacus', (x, .53, z-.035), (.88, .85, .07), 'marble')


def column(x, height, intact):
    box('Square dark marble plinth', (x, .53, .12), (.84, .84, .24), 'marble_dark')
    box('White column foot', (x, .53, .30), (.68, .68, .12), 'marble')
    for dx in (-.19, .19):
        for dy in (-.19, .19):
            box('Teal glazed plinth tile', (x+dx, .53+dy, .255),
                (.34, .34, .065), 'glaze_teal')
    cylinder('Lower gold astragal', (x, .53, .43), .32, .10, 'gold', n=16)
    n = 32
    top = height-.23 if intact else height
    verts = []
    for k, z in enumerate((.36, top)):
        for i in range(n):
            a = i*math.tau/n
            r = (.30 if i%2 == 0 else .255) * (1 if k == 0 else .94)
            zz = z if k == 0 or intact else z + .12*math.sin(a*3) - .06*math.cos(a*5)
            verts.append((x+r*math.cos(a), .53+r*math.sin(a), zz))
    faces = [tuple(reversed(range(n))), tuple(range(n, n*2))]
    faces += [(i, (i+1)%n, (i+1)%n+n, i+n) for i in range(n)]
    shaft = mesh('Fluted white shaft' if intact else 'Jagged broken fluted shaft', verts, faces, 'marble')
    shaft.data.materials.append(MATS['marble_dark'])
    for face in shaft.data.polygons:
        if face.index > 1 and face.index%4 == 0:
            face.material_index = 1
    if intact:
        turned_capital(x, height)


def wall_colonnade():
    for row in range(3):
        cuts = [-4, -2.7, -1.1, .3, 1.9, 3.1, 4] if row%2 else [-4, -3.3, -1.8, -.2, 1.2, 2.7, 4]
        for i, (a, b) in enumerate(zip(cuts, cuts[1:])):
            lo, hi = a+(.014 if a > -4 else 0), b-(.014 if b < 4 else 0)
            box('Offset mixed stone colonnade backing', ((lo+hi)/2, -.42, .23+row*.46),
                (hi-lo, .66, .44), ('sandstone', 'rock', 'sandstone_light', 'sandstone_dark')[(i+row)%4])
    box('Continuous colonnade sole', (0, -.42, .055), (8, .66, .11), 'marble_dark')
    box('Mosaic frieze bed', (0, -.045, 1.16), (8, .09, .32), 'ivory')
    mosaic(-4, 4, .028, 1.16, 24, .19)
    box('Frieze separating gold line', (0, -.015, 1.365), (8, .12, .07), 'gold')
    for i, (x, h) in enumerate(zip((-3, -1, 1, 3), (4.2, 2.2, 4.2, 3.0))):
        column(x, h, i in (0, 2))
    crown = [(-3.58, 4.51), (-3.42, 4.64), (-2.62, 4.61), (-1.95, 4.48),
             (-1.10, 4.64), (-.48, 4.50), (.30, 4.61), (.78, 4.49), (1.36, 4.64),
             (1.61, 4.49), (1.39, 4.35)]
    extrude('Broken partial architrave', [(-3.58, 4.2), (1.55, 4.2)]+list(reversed(crown)),
            .12, .97, 'marble')
    for i in range(1, 8):
        coping_inlay(crown[i], crown[i+1], .12, .97,
                     'glaze_teal' if i%2 else 'coral')
    box('Architrave gold band', (-1.04, 1.002, 4.25), (5.01, .065, .075), 'gold')
    for i, x in enumerate((-3.15, -2.25, -1.35, -.45, .45, 1.23)):
        box('Architrave inset relief panel', (x, .994, 4.38),
            (.65 if i < 5 else .38, .08, .20), 'ivory' if i%2 else 'sandstone_light')
        sun_disc(x, 1.065, 4.38, .105, 'gold')
    for i in range(17):
        box('Carved architrave dentil', (-3.40+i*.285, .99, 4.18),
            (.15, .13, .15), 'ivory' if i%2 else 'marble')
    for x, y, size in [(-2.1, 1.03, .55), (-.6, 1.04, .64), (2.0, 1.00, .48),
                       (3.6, .69, .43)]:
        slab('Dark marble fallen rubble', x-size/2, x+size/2, y-.22, y+.22,
             0, random.uniform(.25, .43), 'marble_dark', False)


def arch_path(radius, bottom=.10, spring=2.1, n=20):
    return [(-radius, bottom), (-radius, spring)] + [
        (radius*math.cos(math.pi-i*math.pi/n), spring+radius*math.sin(math.pi-i*math.pi/n))
        for i in range(1, n+1)] + [(radius, bottom)]


def wall_arch():
    # The rear wall closes the niche at Y=.60; facade stands at Y=1.0.
    top = [(-3, 4.14), (-2.50, 4.14), (-2.24, 4.40), (-1.43, 4.50),
           (-.82, 4.28), (.14, 4.39), (.55, 4.19), (1.18, 4.37),
           (1.68, 4.18), (2.10, 4.31), (2.50, 4.14), (3, 4.14)]
    extrude('Continuous flat recess backing', [(-3, 0), (3, 0)] + list(reversed(top)),
            -.7505098, .60, 'rock')
    opening = arch_path(1.3, bottom=0)
    profile = [(-3, 0)] + opening + [(3, 0)] + list(reversed(top))
    extrude('Rose arch piers and spandrel', profile, .60, 1.0, 'sandstone')
    panel = arch_path(1.20, bottom=.10)
    extrude('Recess mirror backing', panel, .602, .614, 'mirror_back', False)
    extrude('Recess reflective face', panel, .615, .625, 'mirror', False)
    # Voussoirs emphasize real recess depth; the gold inlay remains a thin line.
    for i in range(12):
        a, b = i*math.pi/12+.012, (i+1)*math.pi/12-.012
        p = [(r*math.cos(t), 2.1+r*math.sin(t))
             for r, t in ((1.32,a), (1.54,a), (1.54,b), (1.32,b))]
        extrude('Alternating carved voussoir', p, .94, 1.045, 'coral' if i%2 else 'sandstone')
    for radius, thickness, front, role in [(1.28, .075, .73, 'gold'),
                                            (1.205, .055, .79, 'rose_gold'),
                                            (1.15, .045, .84, 'gold')]:
        band('Stepped recessed mirror molding', arch_path(radius), arch_path(radius-thickness),
             front-.09, front, role)
    extrude('Gold keystone', [(-.13, 3.32), (.13, 3.32), (.21, 3.69), (-.21, 3.69)],
            1.01, 1.06, 'gold')
    gem('Amber keystone cabochon', 0, 1.06, 3.53, .125, 'gem_amber')
    for sign in (-1, 1):
        for row in range(5):
            lo, hi = sorted((sign*1.55, sign*3))
            box('Arch pier offset facing stone', ((lo+hi)/2, 1.015, .32+row*.65),
                (hi-lo-.035, .075, .61),
                ('sandstone_light', 'sandstone_dark', 'sandstone', 'marble_dark', 'coral')[row])
        box('Pier molded marble foot', (sign*2.28, .80, .13), (1.44, .62, .26), 'marble')
        box('Pier gold foot line', (sign*2.28, 1.065, .29), (1.40, .09, .07), 'gold')
        for i in range(3):
            for j in range(3):
                box('Teal spandrel diamond', (sign*(1.78+i*.36), 1.072, 2.75+j*.34),
                    (.23, .09, .23), 'glaze_teal' if (i+j)%2 == 0 else 'ivory',
                    rot=(0, math.pi/4, 0))
    box('Arch relief frieze bed', (0, 1.015, 3.94), (6, .09, .30), 'ivory')
    box('Relief lower gold molding', (0, 1.055, 3.75), (6, .10, .075), 'gold')
    for i in range(9):
        sun_disc(-2.66+i*.665, 1.10, 3.94, .125, 'rose_gold' if i%2 else 'gold')
    for i, (a, b) in enumerate(zip(top, top[1:])):
        lo = a[0]+(.012 if i else 0)
        hi = b[0]-(.012 if i < len(top)-2 else 0)
        extrude('Mixed broken crown stone', [(lo, a[1]-.08), (hi, b[1]-.08),
                (hi, b[1]+.06), (lo, a[1]+.06)], -.7505098, 1.04,
                ('sandstone_light', 'coral', 'marble', 'sandstone_dark')[i%4])
        coping_inlay((lo, a[1]+.06), (hi, b[1]+.06), -.7505098, 1.04,
                     'glaze_teal' if i%3 else 'ivory')


def ellipse(rx, rz, z, n=32):
    return [(rx*math.cos(i*math.tau/n), z+rz*math.sin(i*math.tau/n)) for i in range(n)]


def wall_mirrorframe():
    for row in range(5):
        cuts = [-2, -.96, .32, 1.25, 2] if row%2 else [-2, -1.45, -.24, .95, 2]
        for i, (a, b) in enumerate(zip(cuts, cuts[1:])):
            lo, hi = a+(.012 if a > -2 else 0), b-(.012 if b < 2 else 0)
            box('Mirror wall mixed dressed block', ((lo+hi)/2, .1, .33+row*.66),
                (hi-lo, 1.6, .64), ('sandstone', 'sandstone_light', 'rock', 'sandstone_dark')[(row+i)%4])
    box('Mirror wall bottom molding', (0, .1, .07), (4, 1.6, .14), 'marble_dark')
    crown = [(-2, 3.32), (-1.55, 3.32), (-1.10, 3.60), (-.48, 3.45),
             (.22, 3.55), (.80, 3.38), (1.35, 3.51), (1.65, 3.32), (2, 3.32)]
    for i, (a, b) in enumerate(zip(crown, crown[1:])):
        extrude('Broken mirror wall crown', [(a[0], 3.22), (b[0], 3.22), b, a],
                -.7, .9, ('ivory', 'sandstone_light', 'marble_dark')[i%3])
        coping_inlay(a, b, -.7, .9, 'coral' if i%2 else 'glaze_teal')
    mosaic(-1.9, 1.9, .94, 3.20, 12, .15)
    extrude('Oval dull backboard', ellipse(1.13, 1.23, 1.80), .91, .96, 'mirror_back', False)
    extrude('Oval reflective face', ellipse(1.08, 1.18, 1.80), .965, .99, 'mirror', False)
    for rx, rz, thickness, rear, front in [(1.31, 1.41, .10, .93, 1.045),
                                           (1.21, 1.31, .075, 1.04, 1.13),
                                           (1.135, 1.235, .065, 1.12, 1.205)]:
        band('Stepped carved gold oval profile', ellipse(rx, rz, 1.80),
             ellipse(rx-thickness, rz-thickness, 1.80), rear, front, 'gold')
    band('Rose gold raised oval bead', ellipse(1.24, 1.34, 1.80), ellipse(1.185, 1.285, 1.80),
         1.10, 1.17, 'rose_gold')
    for i in range(8):
        a = (i+.5)*math.tau/8
        gem('Rose gemstone frame stud', 1.255*math.cos(a), 1.14,
            1.80+1.355*math.sin(a), .092, 'gem_rose')
    for sign in (-1, 1):
        for z in (1.05, 2.47):
            points = [(sign*(1.13+.24*math.sin(t)), 1.13, z+.28*math.cos(t))
                      for t in (0, .65, 1.3, 1.95, 2.6, 3.25, 3.9, 4.55)]
            tube('Gilt scroll flourish', points, .045, 'gold')
            scroll(sign*1.49, 1.13, z, .18, 'rose_gold', sign)
    # Small fan-shaped sun crest above the oval.
    extrude('Gold sun centre', ellipse(.14, .14, 3.21, 12), 1.04, 1.17, 'gold')
    gem('Sun crest amber jewel', 0, 1.14, 3.21, .11, 'gem_amber')
    for i in range(7):
        a = i*math.pi/6
        tip = (.36*math.cos(a), 3.21+.36*math.sin(a))
        p = [(.18*math.cos(a-.18), 3.21+.18*math.sin(a-.18)), tip,
             (.18*math.cos(a+.18), 3.21+.18*math.sin(a+.18))]
        extrude('Sun crest ray', p, 1.07, 1.13, 'gold')
    box('White marble projecting shelf', (0, .91, .43), (2.86, .66, .18), 'marble')
    box('Gilt shelf edge molding', (0, 1.205, .42), (2.80, .07, .07), 'gold')
    mosaic(-1.30, 1.30, 1.205, .29, 9, .10)
    for x in (-.94, .94):
        extrude('Carved shelf corbel', [(x-.08, .10), (x+.08, .10), (x+.16, .4),
                (x-.16, .4)], .73, 1.02, 'ivory')
        scroll(x, 1.05, .27, .11)


def wall():
    # Four staggered courses; cut end blocks terminate exactly at tile boundaries.
    for row in range(4):
        cuts = [-2, -1, 0, 1, 2] if row%2 == 0 else [-2, -1.5, -.5, .5, 1.5, 2]
        for i, (a, b) in enumerate(zip(cuts, cuts[1:])):
            lo, hi = a + (.012 if a > -2 else 0), b - (.012 if b < 2 else 0)
            z0, z1 = (row*.69+.008 if row else 0), (row+1)*.69-.012
            if (row, i) in {(1, 1), (2, 2), (3, 1)}:
                extrude('Chipped rose block', [(lo, z0), (hi, z0), (hi, z1-.12),
                        (hi-.14, z1), (lo, z1)], -.7, .94,
                        ('sandstone', 'sandstone_light', 'rock', 'sandstone_dark')[(row+i)%4])
            else:
                box('Staggered rose block', ((lo+hi)/2, .15, (z0+z1)/2),
                    (hi-lo, 1.64, z1-z0),
                    ('sandstone', 'sandstone_light', 'rock', 'sandstone_dark')[(row+i)%4])
    box('Wall marble lower plinth', (0, .15, .10), (4, 1.8, .20), 'marble_dark')
    box('Lower stacked ivory molding', (0, .15, .24), (4, 1.74, .08), 'ivory')
    box('Lower gold molding', (0, .15, .32), (4, 1.72, .08), 'gold')
    for x in (-1.46, 0, 1.46):
        box('Recessed wall relief bed', (x, .975, 1.42), (.77, .07, .88), 'marble_dark')
        for dx in (-.39, .39):
            box('Raised panel side molding', (x+dx, 1.01, 1.42), (.07, .08, .94), 'ivory')
        for z in (.95, 1.89):
            box('Raised panel lintel molding', (x, 1.01, z), (.85, .08, .07), 'ivory')
        sun_disc(x, 1.01, 1.42, .27, 'gold')
        gem('Rose panel centre', x, .95, 1.42, .10, 'gem_rose')
    box('Wall mosaic frieze bed', (0, .96, 2.29), (4, .12, .30), 'ivory')
    mosaic(-2, 2, 1.01, 2.29, 13, .16)
    box('Frieze upper gold line', (0, .15, 2.51), (4, 1.72, .07), 'gold')
    # Matching end courses surround an eroded, uneven central crown.
    heights = [2.84, 2.84, 3.00, 2.88, 2.96, 2.78, 2.90, 2.84, 2.84]
    for i in range(8):
        a, b = -2+i*.5, -1.5+i*.5
        extrude('Broken molded wall coping', [(a, 2.64), (b, 2.64),
                (b, heights[i+1]), (a, heights[i])], -.75, 1.05,
                ('marble', 'sandstone_light', 'ivory', 'sandstone_dark')[i%4])
        coping_inlay((a, heights[i]), (b, heights[i+1]), -.75, 1.05,
                     'coral' if i%2 else 'glaze_teal')
    for x in (-1.25, .68):
        tube('Inset branching stone fissure', [(x, .975, .48), (x-.10, .975, .63),
             (x-.04, .975, .78), (x-.17, .975, .90)], .023, 'sandstone_dark', n=4)


def cliff_island():
    # A deliberately narrow top perimeter; all hanging rock stays behind Y=.2.
    rim = [(-3, -2.12, 1.1), (-2.14, -1.35, 1.45), (-1.03, -.12, .95),
           (-.14, .80, 1.6), (1.13, 2.06, 1.1), (2.04, 3, 1.38)]
    for i, (a, b, h) in enumerate(rim):
        slab('Irregular island rim', a, b, -.42, .6, 0, h,
             ('rock', 'sandstone', 'sandstone_light', 'sandstone_dark')[i%4])
        x = (a+b)/2
        box('Island rim gold inset', (x, .56, h*.48), (b-a-.15, .07, .075), 'gold')
        for j in range(2):
            box('Broken rim mosaic tile', (x+(j-.5)*.25, .20, h-.08),
                (.22, .34, .08), 'glaze_teal' if (i+j)%2 else 'ivory',
                rot=(0, .09*(-1)**i, .08*(-1)**j))
        if i in (1, 3, 5):
            sun_disc(x, .55, h*.67, .17, 'bronze')
    outline = [(-3, -1.3), (-1.9, -1.3), (-.5, -1.3), (1.2, -1.3), (3, -1.3),
               (3, -.55), (2.3, .18), (.9, .18), (-.7, .18), (-2.25, .18), (-3, -.5)]
    n = len(outline)
    verts = [(x, y, 0) for x, y in outline]
    for scale, depth in ((.91, -.72), (.69, -1.65), (.40, -2.72)):
        verts += [(x*scale+.10*(1-scale), -.65+(y+.65)*scale,
                   depth+random.uniform(-.20, .14)) for x, y in outline]
    verts += [(-.2, -.7, -4)]
    faces = [tuple(reversed(range(n)))]
    for layer in range(3):
        for i in range(n):
            a, b = layer*n+i, layer*n+(i+1)%n
            faces += [(a, b, a+n), (b, b+n, a+n)]
    faces += [(3*n+i, 3*n+(i+1)%n, 4*n) for i in range(n)]
    obj = mesh('Three fractured geological island strata', verts, faces, 'sandstone')
    for role in ('rock', 'rock_dark', 'sandstone_dark'):
        obj.data.materials.append(MATS[role])
    for p in obj.data.polygons:
        p.material_index = min(2, max(0, (p.index-1)//(2*n)))
        if p.index%9 == 0:
            p.material_index = 3
    for loc, h, tilt in [((-2.3, .35, 1.05), .54, -.2), ((.27, .32, 1.52), .72, .24),
                         ((2.4, .34, 1.27), .46, -.25)]:
        crystal(loc, h, .13, tilt)
    for i, (x, z, h) in enumerate([(-1.88, -1.34, .94), (-.70, -2.31, .65),
                                     (.58, -1.85, 1.10), (1.72, -1.16, .72)]):
        crystal((x, -.16, z), h, .13, math.pi+(.16 if i%2 else -.20),
                'crystal_rose' if i%2 else 'crystal', 'bronze')
    tube('Gold vein across exposed island stratum', [(-1.70, .06, -.26),
         (-1.30, -.02, -.63), (-1.03, -.06, -.89), (-.66, -.16, -1.20)],
         .035, 'gold', n=5)


def puff(label, loc, radii, role, shade=False):
    """Low-poly round lobes with clipped, closed, flattened bottoms."""
    n, rings = 12, 6
    verts = []
    # Avoid coincident polar vertices; one top point closes each sphere.
    for k in range(rings):
        a = -.75 + (math.pi/2+.75)*k/rings
        for i in range(n):
            t = i*math.tau/n
            verts.append((loc[0]+radii[0]*math.cos(a)*math.cos(t),
                          loc[1]+radii[1]*math.cos(a)*math.sin(t),
                          loc[2]+radii[2]*math.sin(a)))
    verts.append((loc[0], loc[1], loc[2]+radii[2]))
    faces = [tuple(reversed(range(n)))]
    faces += [(k*n+i, k*n+(i+1)%n, (k+1)*n+(i+1)%n, (k+1)*n+i)
              for k in range(rings-1) for i in range(n)]
    faces += [((rings-1)*n+i, (rings-1)*n+(i+1)%n, rings*n) for i in range(n)]
    obj = mesh(label, verts, faces, role, False)
    for p in obj.data.polygons:
        p.use_smooth = p.index != 0
    return obj


def wall_cloud():
    # Three depth rows, flattened along Y to keep the bank tileable as a wall.
    for row, y in enumerate((-.60, -.04, .52)):
        for i, x in enumerate((-1.66, 0, 1.66)):
            rz = (1.42, 1.55, 1.38)[i]
            z = (1.62, 1.86, 1.36)[row] + (.08 if i == 0 else 0)
            puff('Puffy cloud bank upper', (x, y, z), (1.40, .50, rz),
                 'cloud_shade' if row == 0 else 'cloud')
            puff('Separate flattened lower cloud shade', (x, y-.015, .39),
                 (1.39, .50, .39/.68163876), 'cloud_shade', True)
    start = len(PARTS)
    box('Upside down coral house body', (0, 0, .7), (2, 1.6, 1.4), 'coral')
    for x in (-.94, .94):
        for y in (-.74, .74):
            for i in range(4):
                box('Ivory house corner quoin', (x, y, .18+i*.32),
                    (.20 if i%2 else .27, .20, .22), 'ivory')
    for z in (.09, 1.33):
        box('Ivory house cornice', (0, 0, z), (2.09, 1.68, .10), 'ivory')
        box('Gold house cornice line', (0, 0, z+.075), (2.06, 1.66, .05), 'gold')
    # The exposed underside becomes the visible ceiling of the inverted house.
    for x in (-.55, 0, .55):
        for y in (-.32, .32):
            box('Inverted house coffer inlay', (x, y, .005), (.49, .55, .045),
                'glaze_teal' if x else 'ivory')
    decoration = len(PARTS)
    sun_disc(0, 0, 0, .34)
    gem('Inverted ceiling amber boss', 0, .025, 0, .13, 'gem_amber')
    floor_transform = Matrix.Translation((0, 0, -.025)) @ Matrix.Rotation(-math.pi/2, 4, 'X')
    for part in PARTS[decoration:]:
        part.data.transform(floor_transform)
    extrude('Downward gabled tile roof', [(-1.16, 1.4), (1.16, 1.4), (0, 2.25)],
            -.89, .89, 'roof_tile', False)
    for x in (-.57, .57):
        tube('Broad roof raised tile seam', [(x*1.9, .915, 1.43), (x, .915, 1.83),
             (0, .915, 2.25)], .035, 'gold')
    for row in range(4):
        a, b = row/4, (row+1)/4-.025
        half0, half1 = 1.16*(1-a), 1.16*(1-b)
        extrude('Layered gable tile course', [(-half0, 1.4+.85*a), (half0, 1.4+.85*a),
                (half1, 1.4+.85*b), (-half1, 1.4+.85*b)], .89, .94,
                'roof_tile' if row%2 else 'coral')
    tube('Upside down gold roof ridge', [(0, -.86, 2.26), (0, .93, 2.26)], .045, 'gold')
    box('Inverted wood door', (0, .823, .39), (.34, .065, .72), 'wood')
    box('Door carved inset', (0, .867, .39), (.22, .04, .52), 'rose_gold')
    for x in (-.53, .53):
        box('Ivory window surround', (x, .835, .82), (.59, .09, .65), 'ivory')
        box('Warm lit window', (x, .89, .82), (.43, .05, .49), 'window_lit')
        box('Window gold vertical mullion', (x, .935, .82), (.05, .04, .50), 'gold')
        box('Window gold horizontal mullion', (x, .935, .82), (.44, .04, .05), 'gold')
        for sign in (-1, 1):
            box('Teal glazed window shutter', (x+sign*.37, .86, .82),
                (.16, .09, .58), 'glaze_teal', rot=(0, 0, sign*.18))
            for z in (.65, .83, 1.01):
                box('Shutter ivory cross slat', (x+sign*.37, .925, z), (.14, .045, .045), 'ivory')
        box('Gold window pediment', (x, .88, .44), (.68, .19, .08), 'gold')
    tube('Tiny upside down weathervane mast', [(0, .92, 2.24), (0, .92, 2.88)], .035, 'gold')
    extrude('Weathervane pierced arrow', [(-.30, 2.67), (.19, 2.67), (.19, 2.77),
            (.37, 2.64), (.19, 2.53), (.19, 2.61), (-.30, 2.61)], .89, .95, 'gold')
    for sign in (-1, 1):
        x = sign*1.27
        tube('Curled inverted lantern bracket', [(sign*.98, .78, 1.20),
             (x, .78, 1.20), (x, .78, 1.07)], .04, 'gold')
        cylinder('Inverted glowing lantern glass', (x, .78, .85), .14, .32, 'window_lit', n=4)
        for z in (.67, 1.03):
            cylinder('Lantern bronze stepped cap', (x, .78, z), .19, .09, 'bronze', n=4)
        for dx, dy in ((-.14, 0), (.14, 0), (0, -.14), (0, .14)):
            box('Lantern gold cage rib', (x+dx, .78+dy, .85), (.05, .05, .34), 'gold')
    transform = Matrix.Translation((0, .02, 5.0)) @ Matrix.Rotation(math.radians(190), 4, 'Y')
    for obj in PARTS[start:]:
        obj.data.transform(transform)


SPECS = [
    ('Cliff_Shore', 6, cliff_shore),
    ('Wall_Colonnade', 8, wall_colonnade),
    ('Wall_Arch', 6, wall_arch),
    ('Wall_MirrorFrame', 4, wall_mirrorframe),
    ('Wall', 4, wall),
    ('Cliff_Island', 6, cliff_island),
    ('Wall_Cloud', 6, wall_cloud),
]

# Original exported envelopes. Keep the lake-level origin fixed during calibration.
ENVELOPES = {
    'Cliff_Shore': ((-3, -.600000024, 0), (3, 1.399999976, 3.508399963)),
    'Wall_Colonnade': ((-4, -.75, 0), (4, 1.259999990, 4.639999866)),
    'Wall_Arch': ((-3, -.750509799, 0), (3, 1.159999967, 4.559999943)),
    'Wall_MirrorFrame': ((-2, -.699999988, 0), (2, 1.240000010, 3.599999905)),
    'Wall': ((-2, -.75, 0), (2, 1.049999952, 3)),
    'Cliff_Island': ((-3, -1.297008753, -4), (3, .600000024, 2.213182926)),
    'Wall_Cloud': ((-3, -1.114860773, 0), (3, 1.019860744, 5.168438435)),
}
MASONRY_WALLS = {'Wall', 'Wall_Arch', 'Wall_Colonnade', 'Wall_MirrorFrame'}


def triangles(obj):
    obj.data.calc_loop_triangles()
    return len(obj.data.loop_triangles)


def bounds(obj):
    return [Vector(tuple(fn(v.co[i] for v in obj.data.vertices) for i in range(3)))
            for fn in (min, max)]


def validate_tile_ends(obj, width):
    """Compare the occupied end profiles just inside both adjoining planes."""
    low, high = bounds(obj)
    for j in range(32):
        y = low.y+(high.y-low.y)*(j+.5)/32
        for k in range(96):
            z = low.z+(high.z-low.z)*(k+.5)/96
            left = obj.ray_cast(Vector((-width/2-.01, y, z)), Vector((1, 0, 0)), distance=.035)[0]
            right = obj.ray_cast(Vector((width/2+.01, y, z)), Vector((-1, 0, 0)), distance=.035)[0]
            assert left == right, ('Mismatched tile ends', obj.name, y, z)


def finish(name, width):
    bpy.ops.object.select_all(action='DESELECT')
    for part in PARTS:
        bpy.context.view_layer.objects.active = part
        part.select_set(True)
        for modifier in list(part.modifiers):
            bpy.ops.object.modifier_apply(modifier=modifier.name)
        part.select_set(False)
    for part in PARTS:
        part.select_set(True)
    bpy.context.view_layer.objects.active = PARTS[0]
    bpy.ops.object.join()
    obj = bpy.context.object
    obj.name, obj.data.name = name, name + '_Mesh'
    # Only simplify if the applied two-segment bevels exceed the game budget.
    # Material boundaries and silhouette are retained by the collapse solver.
    if triangles(obj) > 7950:
        end_group = None
        if name in MASONRY_WALLS:
            end_group = obj.vertex_groups.new(name='Preserve adjoining tile ends')
            end_group.add([v.index for v in obj.data.vertices if abs(v.co.x) > width/2-.14],
                          1, 'REPLACE')
        mod = obj.modifiers.new('Wall triangle budget', 'DECIMATE')
        mod.ratio, mod.delimit = 7700 / triangles(obj), {'MATERIAL'}
        if end_group is not None:
            mod.vertex_group, mod.invert_vertex_group = end_group.name, True
            mod.vertex_group_factor = 1000
        bpy.ops.object.modifier_apply(modifier=mod.name)
        if end_group is not None:
            obj.vertex_groups.remove(obj.vertex_groups['Preserve adjoining tile ends'])
    assert triangles(obj) <= 8000, (name, triangles(obj))
    # Restore original bounds after bevels, scaling above and below lake level
    # separately so the floating island retains its authored origin.
    low, high = bounds(obj)
    target_low, target_high = (Vector(v) for v in ENVELOPES[name])
    assert abs(low.y-target_low.y) < .10 and abs(high.y-target_high.y) < .10, (name, low, high)
    assert abs(high.z-target_high.z) < .20, (name, high.z)
    for v in obj.data.vertices:
        v.co.x = (v.co.x-(low.x+high.x)/2) * width/(high.x-low.x)
        v.co.y = target_low.y+(v.co.y-low.y)*(target_high.y-target_low.y)/(high.y-low.y)
        if target_low.z == 0:
            v.co.z = (v.co.z-low.z)*target_high.z/(high.z-low.z)
        else:
            v.co.z *= target_low.z/low.z if v.co.z < -.00001 else target_high.z/high.z
        if abs(v.co.z) < .00001:
            v.co.z = 0
    bm = bmesh.new()
    bm.from_mesh(obj.data)
    bmesh.ops.recalc_face_normals(bm, faces=list(bm.faces))
    assert not any(e.is_boundary or not e.is_manifold for e in bm.edges), name
    bm.to_mesh(obj.data)
    bm.free()
    obj.data.update()
    obj.location, obj.rotation_euler, obj.scale = (0, 0, 0), (0, 0, 0), (1, 1, 1)
    bpy.context.view_layer.update()
    low, high = bounds(obj)
    assert (low-target_low).length < 1e-5 and (high-target_high).length < 1e-5, (name, low, high)
    assert abs(low.x+width/2) < 1e-5 and abs(high.x-width/2) < 1e-5, name
    assert 1.2 <= high.y-low.y <= 2.25, (name, low, high)
    if name == 'Cliff_Island':
        assert abs(low.z+4) < 1e-5
        assert all(v.co.y <= .20001 for v in obj.data.vertices if v.co.z < -.01)
    else:
        assert abs(low.z) < .015, (name, low.z)
    assert all(math.isfinite(c) for v in obj.data.vertices for c in v.co)
    if name in MASONRY_WALLS:
        validate_tile_ends(obj, width)
    assert len({face.material_index for face in obj.data.polygons}) >= 5, name
    for mat in obj.data.materials:
        assert mat.name in PALETTE
        assert tuple(mat.diffuse_color) == (.5, .5, .5, 1)
        assert tuple(mat.node_tree.nodes['Principled BSDF'].inputs['Base Color'].default_value) == (.5, .5, .5, 1)
    area_by_role = {}
    view = Vector((0, math.cos(math.radians(55)), math.sin(math.radians(55))))
    for face in obj.data.polygons:
        role = obj.data.materials[face.material_index].name
        area_by_role[role] = area_by_role.get(role, 0)+face.area*max(0, face.normal.dot(view))
    dominant = max(area_by_role, key=area_by_role.get)
    share = area_by_role[dominant]/sum(area_by_role.values())
    assert share <= .55, (name, dominant, share)
    print(f'DETAIL {name} {len(area_by_role)} roles, largest projected region {dominant} {share:.1%}', flush=True)
    obj['front'], obj['tile_width_m'], obj['triangle_count'] = '+Y', width, triangles(obj)
    destination = OUTPUT / f'Kit_{name}.fbx'
    bpy.ops.export_scene.fbx(
        filepath=str(destination), use_selection=True,
        axis_forward='-Z', axis_up='Y', apply_scale_options='FBX_SCALE_UNITS',
        bake_space_transform=True, object_types={'MESH'}, use_mesh_modifiers=True,
        mesh_smooth_type='FACE', add_leaf_bones=False, bake_anim=False,
    )
    assert destination.is_file() and destination.stat().st_size > 1024
    print(f'EXPORTED {name} {triangles(obj)} {tuple(round(d, 4) for d in obj.dimensions)}', flush=True)
    return obj


def preview(objects):
    """Review-only colours, translations and camera, strictly after all exports."""
    scene = bpy.context.scene
    scene.render.engine = 'BLENDER_WORKBENCH'
    scene.render.resolution_x, scene.render.resolution_y = 1600, 900
    scene.render.resolution_percentage = 100
    scene.render.image_settings.file_format = 'PNG'
    scene.render.filepath = str(PREVIEW)
    scene.view_settings.view_transform = 'Standard'
    shade = scene.display.shading
    shade.light, shade.color_type = 'FLAT', 'MATERIAL'
    shade.show_shadows, shade.show_cavity = False, True
    shade.cavity_type = 'BOTH'
    shade.curvature_ridge_factor, shade.curvature_valley_factor = 1.3, 1
    shade.show_specular_highlight = False
    shade.background_type, shade.background_color = 'WORLD', (.045, .055, .08)
    if scene.world is None:
        scene.world = bpy.data.worlds.new('Review background')
    scene.world.color = (.045, .055, .08)
    helpers = []
    try:
        for role, mat in MATS.items():
            mat.diffuse_color = (*PALETTE[role], 1)
        total = sum(s[1] for s in SPECS)+1.0*(len(SPECS)-1)
        x = -total/2
        for obj, (_, width, _) in zip(objects, SPECS):
            obj.location.x = x+width/2
            x += width+1
        camera = bpy.data.objects.new('Review camera', bpy.data.cameras.new('Review camera'))
        scene.collection.objects.link(camera)
        helpers.append(camera)
        scene.camera = camera
        camera.data.type, camera.data.ortho_scale = 'ORTHO', total+1.5
        target = Vector((0, 0, .8))
        elevation = math.radians(55)
        camera.location = target+Vector((0, 50*math.cos(elevation), 50*math.sin(elevation)))
        camera.rotation_euler = (target-camera.location).to_track_quat('-Z', 'Y').to_euler()
        for obj in objects:
            font = bpy.data.curves.new('Review caption', 'FONT')
            font.body, font.align_x, font.size = obj.name, 'CENTER', .32
            font.materials.append(MATS['crystal_core'])
            label = bpy.data.objects.new('Review label', font)
            scene.collection.objects.link(label)
            label.location = (obj.location.x, 2.9, -1.7)
            label.rotation_euler = camera.rotation_euler
            helpers.append(label)
        PREVIEW.parent.mkdir(parents=True, exist_ok=True)
        bpy.ops.render.render(write_still=True)
        assert PREVIEW.is_file() and PREVIEW.stat().st_size > 1024
        print(f'PREVIEW {PREVIEW}', flush=True)
    finally:
        scene.camera = None
        for helper in helpers:
            bpy.data.objects.remove(helper, do_unlink=True)
        for obj in objects:
            obj.location = (0, 0, 0)
        for mat in MATS.values():
            mat.diffuse_color = (.5, .5, .5, 1)


def main():
    global PARTS
    bpy.ops.wm.read_factory_settings(use_empty=True)
    for mat in list(bpy.data.materials):
        bpy.data.materials.remove(mat)
    scene = bpy.context.scene
    scene.unit_settings.system, scene.unit_settings.scale_length = 'METRIC', 1
    setup_materials()
    OUTPUT.mkdir(parents=True, exist_ok=True)
    objects = []
    for i, (name, width, build) in enumerate(SPECS):
        PARTS = []
        random.seed(34117+i*7919)
        build()
        objects.append(finish(name, width))
    assert len(scene.objects) == 7 and all(o.type == 'MESH' for o in scene.objects)
    preview(objects)
    assert len(scene.objects) == 7 and all(o.type == 'MESH' for o in scene.objects)
    print('DONE', flush=True)


if __name__ == '__main__':
    main()
