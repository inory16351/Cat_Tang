# 챕터 5 거울 허상: 뾰족한 두건, 거울 조각 로브, 흰 장갑과 회전 파편을 만듭니다.
# 실행: "C:/Program Files/Blender Foundation/Blender 5.3/blender.exe" -b --factory-startup -P Art/tools/blender_kit_ch5_phantom.py
"""Deterministic primitive-built phantom, +Y front, metres; four flat FBX meshes.

Role shaders are neutral grey. Review colours and review instances exist only
after export, and are removed after the three-panel Workbench render.
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
DEST = ROOT / 'Game/Assets/Art/Characters/Ch5/Kit_Ch5_MirrorPhantom.fbx'
PREVIEW = ROOT / 'Art/tools/specs/blender_kit_ch5_phantom_preview.png'
TAU = math.tau
MATS, PARTS = {}, []
PALETTE = {
    'mirror': (.66, .83, .94), 'mirror_back': (.15, .25, .39),
    'violet_dark': (.105, .032, .22), 'violet': (.32, .16, .49),
    'indigo': (.015, .021, .075), 'gold': (.83, .49, .12),
    'crack_gold': (1., .77, .20), 'magenta': (1., .025, .49),
    'starlight': (.91, .97, 1.), 'marble': (.94, .89, 1.),
    'marble_dark': (.44, .35, .57),
}


def mesh(name, verts, faces, role, bevel=0, smooth=False):
    data = bpy.data.meshes.new(name)
    data.from_pydata(verts, [], faces)
    data.update()
    obj = bpy.data.objects.new(name, data)
    bpy.context.collection.objects.link(obj)
    data.materials.append(MATS[role])
    for p in data.polygons:
        p.use_smooth = smooth
    if bevel:
        mod = obj.modifiers.new('Toy edge chamfer', 'BEVEL')
        mod.width, mod.segments = bevel, 2
        mod.limit_method, mod.angle_limit = 'ANGLE', math.radians(38)
        mod.use_clamp_overlap = True
    PARTS.append(obj)
    return obj


def tube(name, pts, radius, role, n=6, closed=False):
    pts = [Vector(p) for p in pts]
    verts, faces = [], []
    for i, p in enumerate(pts):
        a = pts[(i-1) % len(pts)] if closed else pts[max(i-1, 0)]
        b = pts[(i+1) % len(pts)] if closed else pts[min(i+1, len(pts)-1)]
        axis = (b-a).normalized()
        ref = Vector((0, 1, 0)) if abs(axis.y) < .9 else Vector((1, 0, 0))
        u = axis.cross(ref).normalized()
        v = axis.cross(u)
        verts.extend(tuple(p+radius*(u*math.cos(TAU*j/n)+v*math.sin(TAU*j/n))) for j in range(n))
    for k in range(len(pts) if closed else len(pts)-1):
        nxt = (k+1) % len(pts)
        faces.extend((k*n+j, k*n+(j+1) % n, nxt*n+(j+1) % n, nxt*n+j) for j in range(n))
    if not closed:
        faces.extend([tuple(reversed(range(n))), tuple(range(len(verts)-n, len(verts)))])
    return mesh(name, verts, faces, role)


def oval(name, center, scale, role, n=20, rings=10):
    # UV sphere aligned with Z; transforms are baked directly into mesh data.
    c = Vector(center)
    verts = [tuple(c+Vector((0, 0, -scale[2])))]
    for k in range(1, rings):
        a = -math.pi/2+math.pi*k/rings
        verts.extend((c.x+scale[0]*math.cos(a)*math.cos(TAU*j/n),
                      c.y+scale[1]*math.cos(a)*math.sin(TAU*j/n),
                      c.z+scale[2]*math.sin(a)) for j in range(n))
    verts.append(tuple(c+Vector((0, 0, scale[2]))))
    faces = [(0, 1+(j+1) % n, 1+j) for j in range(n)]
    for k in range(rings-2):
        faces.extend((1+k*n+j, 1+k*n+(j+1) % n, 1+(k+1)*n+(j+1) % n, 1+(k+1)*n+j) for j in range(n))
    faces.extend((len(verts)-1, 1+(rings-2)*n+j, 1+(rings-2)*n+(j+1) % n) for j in range(n))
    return mesh(name, verts, faces, role, smooth=True)


def plate(name, points, depth=.05, role='mirror', normal=(0, 1, 0), bevel=.02):
    """Closed prism: silver front, dark reverse and thick chamfered edges."""
    normal = Vector(normal).normalized()
    points = [Vector(p) for p in points]
    n = len(points)
    verts = [tuple(p+normal*d) for d in (-depth/2, depth/2) for p in points]
    faces = [tuple(reversed(range(n))), tuple(range(n, 2*n))]
    faces.extend((j, (j+1) % n, (j+1) % n+n, j+n) for j in range(n))
    obj = mesh(name, verts, faces, role, bevel)
    obj.data.materials.append(MATS['mirror_back'])
    for p in obj.data.polygons:
        p.material_index = 0 if p.index == 1 else 1
    if bevel:
        obj.modifiers[0].material = 1
    return obj


def diamond(name, x, y, z, w, h, role, depth=.04):
    return plate(name, [(x, y, z+h), (x-w, y, z), (x, y, z-h), (x+w, y, z)], depth, role, bevel=.008)


def facet_plate(name, points, ridge, role='mirror', normal=(0, 1, 0),
                accents=('mirror', 'mirror', 'mirror_back')):
    """A thick shard with a shallow, explicitly triangulated front ridge."""
    points = [Vector(p) for p in points]
    normal = Vector(normal).normalized()
    n = len(points)
    verts = [tuple(p-normal*.035) for p in points]+[tuple(p) for p in points]
    verts.append(tuple(ridge))
    faces = [tuple(reversed(range(n)))]
    faces += [(j, (j+1) % n, (j+1) % n+n, j+n) for j in range(n)]
    faces += [(n+j, n+(j+1) % n, 2*n) for j in range(n)]
    obj = mesh(name, verts, faces, role)
    roles = list(dict.fromkeys([role, 'mirror_back', *accents]))
    for r in roles[1:]:
        obj.data.materials.append(MATS[r])
    for p in obj.data.polygons:
        p.material_index = roles.index('mirror_back') if p.index <= n else roles.index(accents[(p.index-n-1) % len(accents)])
    return obj


def hood():
    # Six-sided pointed opening; the apex leans BACK instead of rounding over.
    opening = [(0,.410,2.005),(-.170,.410,1.830),(-.355,.410,1.535),
               (0,.410,1.315),(.355,.410,1.535),(.170,.410,1.830)]
    outer = [(0,-.100,2.110),(-.240,.190,1.920),(-.470,.185,1.455),
             (0,.245,1.240),(.470,.185,1.455),(.240,.190,1.920)]
    rear = [(0,-.285,2.060),(-.190,-.350,1.875),(-.390,-.305,1.460),
            (0,-.250,1.265),(.390,-.305,1.460),(.190,-.350,1.875)]
    verts = opening+outer+rear+[(0,-.400,1.620)]
    faces, roles = [], []
    for j in range(6):
        k = (j+1) % 6
        faces.extend([(j,k,6+k),(j,6+k,6+j), (6+j,6+k,12+k),
                      (6+j,12+k,12+j), (12+j,12+k,18)])
        roles.extend([('mirror','mirror_back','mirror','mirror_back','mirror_back')[j % 5],
                      'mirror', 'mirror_back' if j % 2 else 'mirror',
                      'mirror' if j in (0,4) else 'mirror_back',
                      'violet_dark' if j in (1,4) else 'mirror_back'])
    shell = mesh('Pointed folded mirror cowl', verts, faces, 'mirror')
    for role in ('mirror_back', 'violet_dark'):
        shell.data.materials.append(MATS[role])
    for p,r in zip(shell.data.polygons,roles):
        p.material_index = ['mirror','mirror_back','violet_dark'].index(r)
    # Opening returns deeply into the hood. Side walls remain visible around
    # the small mask, including from the elevated game camera.
    well = [(x*.91,.235,1.645+(z-1.645)*.92) for x,y,z in opening]
    wall = mesh('Recessed violet hood lining', opening+well,
                [(j,(j+1)%6,(j+1)%6+6,j+6) for j in range(6)], 'violet_dark')
    plate('Deep indigo face shadow', well, .035, 'indigo', bevel=0)
    # A broad raised ribbon, rather than a circular helmet gasket.
    rim_outer = [(x*1.075,.425,1.645+(z-1.645)*1.075) for x,y,z in opening]
    rim_inner = [(x*.965,.428,1.645+(z-1.645)*.965) for x,y,z in opening]
    rim = mesh('Raised gold face opening band', rim_outer+rim_inner,
               [(j,(j+1)%6,(j+1)%6+6,j+6) for j in range(6)], 'gold')
    solid = rim.modifiers.new('Raised trim thickness','SOLIDIFY')
    solid.thickness = .025
    # Flat shield face, with angular cheek planes and one fine fracture.
    mask = [(0,.306,1.888),(-.195,.306,1.675),(-.135,.306,1.512),
            (0,.306,1.405),(.135,.306,1.512),(.195,.306,1.675)]
    facet_plate('Angular diamond shield mask', mask, (0,.328,1.657),
                accents=('mirror','mirror_back','mirror','mirror','mirror','mirror'))
    for s in (-1,1):
        diamond('Diamond eye socket',s*.090,.341,1.683,.052,.080,'indigo',.018)
        diamond('Magenta diamond eye',s*.090,.360,1.685,.036,.061,'magenta',.020)
    tube('Single mask fracture',[(.010,.345,1.858),(-.025,.352,1.767),
                                (.017,.359,1.730),(-.035,.359,1.613),
                                (.010,.351,1.474)],.010,'crack_gold',4)
    tube('Cowl left broken seam',[(-.285,.232,1.803),(-.307,.239,1.708),
                                 (-.365,.228,1.692),(-.408,.217,1.548)],.018,'crack_gold',4)
    tube('Cowl rear broken seam',[(.080,-.315,2.000),(.185,-.355,1.900),
                                 (.140,-.376,1.824),(.265,-.340,1.700)],.018,'crack_gold',4)
    diamond('Gold diamond chin clasp',0,.448,1.293,.064,.096,'gold',.055)
    diamond('Chin clasp inset',0,.481,1.293,.024,.048,'crack_gold',.012)


def cape():
    # An angular violet underskirt ends above the separated, long shard tips.
    n=10
    verts=[(r*math.sin(TAU*j/n),r*.72*math.cos(TAU*j/n),z)
           for r,z in ((.175,1.350),(.310,.965),(.245,.640)) for j in range(n)]
    mesh('Faceted violet inner robe',verts,
         [(k*n+j,k*n+(j+1)%n,(k+1)*n+(j+1)%n,(k+1)*n+j)
          for k in range(2) for j in range(n)]+[tuple(range(20,30))], 'violet_dark')
    # Five front plates and two rear plates; each continuous vertical panel
    # becomes a different-length spear tip, with visible negative-space splits.
    tails=[(-.310,.105,.605,.087),(-.175,.235,.465,.094),
           (0,.300,.350,.105),(.172,.228,.535,.091),(.316,.100,.665,.085),
           (-.170,-.195,.555,.115),(.165,-.205,.425,.112)]
    for i,(x,y,low,w) in enumerate(tails):
        pts=[(x*.65-w*.68,y*.65,1.245),(x*.65+w*.68,y*.65,1.245),
             (x+w,y,.825),(x+w*.52,y+.012,low+.185),
             (x*.90,y+.018,low),(x-w*.72,y,.735)]
        normal=(0,1 if y>0 else -1,0)
        ridge=(x,y+normal[1]*.052,.900)
        facet_plate('Long split robe shard %d'%i,pts,ridge,normal=normal,
                    accents=('mirror','mirror','mirror_back','mirror','mirror','mirror'))
        if i in (0,2,4,6):
            tube('Robe gold fracture %d'%i,[(x-.018,y+.058,1.102),
                 (x+.026,y+.066,.964),(x-.017,y+.060,.902),
                 (x+.010,y+.049,.765)],.014,'crack_gold',4)
    # Two overlapping tiers spread across the shoulders, distinctly shorter
    # than the robe. Each panel is a solid, gold-edged pointed cape leaf.
    for tier in range(2):
        for j in range(8):
            a=TAU*(j+.5)/8
            normal=Vector((math.sin(a),math.cos(a),0))
            tangent=Vector((math.cos(a),-math.sin(a),0))
            def p(w,r,z):
                return normal*r+tangent*w+Vector((0,0,z))
            top=1.365-tier*.115
            r=.190+tier*.050
            pts=[p(-.065,r,top),p(.065,r,top),
                 p(.145,.420+tier*.018,top-.185),
                 p(.035,.445+tier*.020,top-.270),
                 p(-.145,.420+tier*.018,top-.210)]
            facet_plate('Layered shoulder capelet %d %d'%(tier,j),pts,
                        p(0,.330,top-.115),role='violet_dark',normal=normal,
                        accents=('violet','violet_dark','mirror_back','violet_dark','mirror'))
            tube('Gold capelet hem %d %d'%(tier,j),
                 [pts[k]+normal*.015 for k in (2,3,4)],.014,'gold',4)
    # A small four-point star lies over the front robe below the chin clasp.
    pts=[]
    for j in range(8):
        a=TAU*j/8
        r=1 if j%2==0 else .30
        pts.append((math.sin(a)*.073*r,.387,1.035+math.cos(a)*.112*r))
    plate('Gold chest star',pts,.026,'gold',bevel=.006)
    diamond('Chest star silver heart',0,.409,1.035,.022,.033,'mirror',.012)


def hand(side):
    # Local U runs from wrist to fingertips. N points toward the viewer and
    # upward, so these are offering palms rather than fists hanging from cuffs.
    u=Vector((side*.940,0,.342))
    v=Vector((side*.212,.785,-.583)).normalized()
    normal=u.cross(v).normalized()*side
    origin=Vector((side*.620,.120,1.150))
    basis=Matrix(((u.x,v.x,normal.x,0),(u.y,v.y,normal.y,0),
                  (u.z,v.z,normal.z,0),(0,0,0,1)))
    def lobe(name,c,scale):
        obj=oval(name,c,scale,'marble',12,7)
        obj.data.transform(Matrix.Translation(origin) @ basis)
        return obj
    lobe('Open white mitten palm',(.018,0,0),(.102,.078,.039))
    # Three joined rounded lobes retain the broad mitten silhouette.
    for j,(w,length) in enumerate(((-.048,.063),(0,.074),(.047,.057))):
        lobe('Upturned glove finger lobe %d'%j,(.092,w,.022),(length,.032,.034))
    lobe('Raised open glove thumb',(-.030,.087,.055),(.041,.042,.067))
    # Gold hollow wrist band is perpendicular to the outward wrist axis.
    verts=[]
    n=12
    for along,r in ((-.098,.052),(-.098,.073),(-.048,.073),(-.048,.052)):
        verts += [tuple(origin+u*along+(v*math.cos(TAU*j/n)+normal*math.sin(TAU*j/n))*r) for j in range(n)]
    cuff=mesh('Gold open glove cuff',verts,
              [(k*n+j,k*n+(j+1)%n,((k+1)%4)*n+(j+1)%n,((k+1)%4)*n+j)
               for k in range(4) for j in range(n)],'gold')
    cuff.data.materials.append(MATS['indigo'])
    for p in cuff.data.polygons:
        if p.index//n==3:
            p.material_index=1
    crease=[origin+basis.to_3x3() @ Vector(q) for q in
            ((-.004,-.042,.036),(-.023,0,.043),(-.004,.040,.036))]
    tube('Soft glove palm fold',crease,.007,'marble_dark',4)


def halo():
    # Broken, thin elliptical halo. It stays with the hood in Body, leaving
    # Orbit's original radius, six shards and spin origin unchanged.
    tilt=math.radians(24)
    for segment,(start,end) in enumerate(((12,136),(153,268),(288,349))):
        pts=[]
        for j in range(18):
            a=math.radians(start+(end-start)*j/17)
            x,y=.615*math.cos(a),.475*math.sin(a)
            pts.append((x,y*math.cos(tilt),1.770+y*math.sin(tilt)+x*.105))
        tube('Broken tilted gold halo %d'%segment,pts,.014,'gold',5)


def orbit():
    for j in range(6):
        a = TAU*j/6+math.radians(30)
        radial = Vector((math.sin(a), math.cos(a), 0))
        c = radial*.85+Vector((0,0,1.0))
        up = (Vector((0,0,1))*.86+radial*.51).normalized()
        # Present broad facets to both the front and elevated game cameras.
        # Purely tangential blades disappear edge-on at two ring positions.
        facing = (Vector((0,.90,.60))+radial*.20).normalized()
        tangent = up.cross(facing).normalized()
        normal = tangent.cross(up).normalized()
        pts = [c+up*.124, c+tangent*.065+up*.018,
               c-up*.111, c-tangent*.052-up*.025]
        plate('Orbit kite shard %d' % j, pts, .045, normal=normal)
        tube('Orbit gold broken edge %d' % j, [pts[0]+normal*.025,pts[1]+normal*.025,pts[2]+normal*.025], .020, 'crack_gold')


def triangles(obj):
    obj.data.calc_loop_triangles()
    return len(obj.data.loop_triangles)


def join(name, origin):
    bpy.ops.object.select_all(action='DESELECT')
    for obj in PARTS:
        bpy.context.view_layer.objects.active = obj
        obj.select_set(True)
        for mod in list(obj.modifiers):
            bpy.ops.object.modifier_apply(modifier=mod.name)
        obj.select_set(False)
    for obj in PARTS:
        obj.select_set(True)
    bpy.context.view_layer.objects.active = PARTS[0]
    bpy.ops.object.join()
    obj = bpy.context.object
    obj.name = name
    obj.data.name = name+'_Mesh'
    # Collapse redundant tessellation while preserving material boundaries.
    # The cowl and robe stay deliberately flat; only gloves are smooth.
    budget = {'Body': 3850, 'Hand_R': 620, 'Hand_L': 620, 'Orbit': 650}[name]
    count = triangles(obj)
    if count > budget:
        mod = obj.modifiers.new('Static silhouette triangle budget', 'DECIMATE')
        mod.ratio = budget/count
        mod.delimit = {'MATERIAL'}
        mod.use_collapse_triangulate = True
        bpy.ops.object.modifier_apply(modifier=mod.name)
    if name == 'Body':
        # Chamfering shortens a sharp tip; restore the authored hover clearance.
        low = min(v.co.z for v in obj.data.vertices)
        for v in obj.data.vertices:
            if v.co.z < .70:
                v.co.z = .70-(.70-v.co.z)*(.70-.35)/(.70-low)
    bm = bmesh.new()
    bm.from_mesh(obj.data)
    bmesh.ops.recalc_face_normals(bm, faces=list(bm.faces))
    bm.to_mesh(obj.data)
    bm.free()
    origin = Vector(origin)
    obj.data.transform(Matrix.Translation(-origin))
    obj.location = origin
    obj['front'] = '+Y'
    obj['static_mesh'] = True
    obj['triangle_count'] = triangles(obj)
    PARTS.clear()
    return obj


def export(objects):
    bpy.context.view_layer.update()
    assert [o.name for o in objects] == ['Body','Hand_R','Hand_L','Orbit']
    assert len(bpy.context.scene.objects) == 4
    assert all(o.parent is None and o.type == 'MESH' for o in objects)
    counts = {o.name: triangles(o) for o in objects}
    print(f'AUTHORED_TRIANGLES {counts}', flush=True)
    assert sum(counts.values()) < 6000, counts
    body = objects[0]
    assert abs(min(v.co.z for v in body.data.vertices)-.35) < .001
    assert 2.09 < max(v.co.z for v in body.data.vertices) < 2.13
    assert body.location.length < 1e-7
    assert tuple(objects[-1].location) == (0.,0.,1.)
    for obj in objects:
        assert all(math.isfinite(c) for v in obj.data.vertices for c in v.co)
        assert not obj.modifiers
        assert all(m.name in PALETTE for m in obj.data.materials)
    for mat in MATS.values():
        assert tuple(mat.node_tree.nodes['Principled BSDF'].inputs['Base Color'].default_value) == (.5,.5,.5,1.)
    bpy.ops.object.select_all(action='DESELECT')
    for obj in objects:
        obj.select_set(True)
    DEST.parent.mkdir(parents=True, exist_ok=True)
    bpy.ops.export_scene.fbx(filepath=str(DEST),use_selection=True,
        axis_forward='-Z',axis_up='Y',apply_scale_options='FBX_SCALE_UNITS',
        bake_space_transform=True,object_types={'MESH'},use_mesh_modifiers=True,
        mesh_smooth_type='FACE',add_leaf_bones=False,bake_anim=False,use_custom_props=True)
    assert DEST.is_file() and DEST.stat().st_size > 1024
    dims = {o.name: tuple(round(d,4) for d in o.dimensions) for o in objects}
    print(f'EXPORTED Ch5_MirrorPhantom {[o.name for o in objects]} {sum(triangles(o) for o in objects)} {dims}', flush=True)
    for o in objects:
        print(f'MESH {o.name} {triangles(o)} origin={tuple(round(v,4) for v in o.location)}', flush=True)


def preview(objects):
    scene = bpy.context.scene
    scene.render.engine = 'BLENDER_WORKBENCH'
    scene.render.resolution_x, scene.render.resolution_y = 600, 1000
    scene.render.resolution_percentage = 100
    scene.render.image_settings.file_format = 'PNG'
    scene.render.film_transparent = False
    scene.view_settings.view_transform = 'Standard'
    shading = scene.display.shading
    shading.light, shading.color_type = 'FLAT', 'MATERIAL'
    shading.show_shadows = False
    shading.show_cavity, shading.cavity_type = True, 'BOTH'
    shading.curvature_ridge_factor, shading.curvature_valley_factor = .7, .85
    shading.cavity_ridge_factor, shading.cavity_valley_factor = .6, .8
    shading.show_specular_highlight = False
    shading.show_object_outline = True
    shading.object_outline_color = (.015,.019,.035)
    shading.background_type = 'WORLD'
    scene.world = bpy.data.worlds.new('Phantom review world')
    scene.world.color = (.041,.049,.080)
    camera = bpy.data.objects.new('Review camera', bpy.data.cameras.new('Review camera'))
    scene.collection.objects.link(camera)
    scene.camera = camera
    camera.data.type = 'ORTHO'
    copies=[]
    for obj in objects:
        obj.hide_render = True
        copy = obj.copy()
        copy.name = 'Review '+obj.name
        scene.collection.objects.link(copy)
        copy.hide_render = False
        copies.append(copy)
    for role,mat in MATS.items():
        mat.diffuse_color = (*PALETTE[role],1)
    arrays=[]
    try:
        for title,az,el in [('FRONT / +Y',0,0),('THREE QUARTER',-38,18),('GAME VIEW / 55 DEG',-22,55)]:
            az,el = math.radians(az),math.radians(el)
            target=Vector((0,0,1.16))
            camera.location=target+Vector((8*math.sin(az)*math.cos(el),8*math.cos(az)*math.cos(el),8*math.sin(el)))
            camera.rotation_euler=(target-camera.location).to_track_quat('-Z','Y').to_euler()
            rotation=camera.rotation_euler.to_matrix()
            inv=rotation.transposed()
            pts=[inv @ (o.matrix_world @ v.co) for o in copies for v in o.data.vertices]
            xmin,xmax=min(p.x for p in pts),max(p.x for p in pts)
            ymin,ymax=min(p.y for p in pts),max(p.y for p in pts)
            old=inv @ target
            camera.location += rotation @ Vector(((xmin+xmax)/2-old.x,(ymin+ymax)/2-old.y,0))
            camera.data.ortho_scale=max((xmax-xmin)/.6,ymax-ymin)*1.19
            data=bpy.data.curves.new('View caption','FONT')
            data.body=title
            data.align_x='CENTER'
            data.size=.072
            data.materials.append(MATS['starlight'])
            caption=bpy.data.objects.new('View caption',data)
            scene.collection.objects.link(caption)
            caption.rotation_euler=camera.rotation_euler
            caption.location=camera.location+rotation @ Vector((0,-camera.data.ortho_scale*.425,-7))
            bpy.context.view_layer.update()
            with tempfile.TemporaryDirectory(prefix='phantom_review_') as tmp:
                scene.render.filepath=str(Path(tmp)/'view.png')
                bpy.ops.render.render(write_still=True)
                rendered=bpy.data.images.load(scene.render.filepath,check_existing=False)
                pixels=np.array(rendered.pixels[:],dtype=np.float32)
                bpy.data.images.remove(rendered)
            assert pixels.size == 600*1000*4
            arrays.append(pixels.reshape((1000,600,4)))
            bpy.data.objects.remove(caption,do_unlink=True)
        output=bpy.data.images.new('Mirror phantom three view review',width=1800,height=1000,alpha=True)
        output.pixels.foreach_set(np.concatenate(arrays,axis=1).ravel())
        output.filepath_raw=str(PREVIEW)
        output.file_format='PNG'
        PREVIEW.parent.mkdir(parents=True,exist_ok=True)
        output.save()
        print(f'PREVIEW {PREVIEW}',flush=True)
    finally:
        for copy in copies:
            bpy.data.objects.remove(copy,do_unlink=True)
        for obj in objects:
            obj.hide_render=False
        scene.camera=None
        bpy.data.objects.remove(camera,do_unlink=True)
        for mat in MATS.values():
            mat.diffuse_color=(.5,.5,.5,1)
        scene.render.resolution_x=1800


def main():
    bpy.ops.wm.read_factory_settings(use_empty=True)
    random.seed(550731)
    scene=bpy.context.scene
    scene.unit_settings.system='METRIC'
    scene.unit_settings.scale_length=1
    for role in PALETTE:
        mat=bpy.data.materials.new(role)
        mat.use_nodes=True
        mat.diffuse_color=(.5,.5,.5,1)
        shader=mat.node_tree.nodes['Principled BSDF']
        shader.inputs['Base Color'].default_value=(.5,.5,.5,1)
        shader.inputs['Roughness'].default_value=.7
        MATS[role]=mat
    hood()
    cape()
    halo()
    objects=[join('Body',(0,0,0))]
    for side,name in ((1,'Hand_R'),(-1,'Hand_L')):
        hand(side)
        objects.append(join(name,(side*.62,.12,1.15)))
    orbit()
    objects.append(join('Orbit',(0,0,1)))
    export(objects)
    preview(objects)
    assert len(scene.objects)==4
    print('DONE',flush=True)


if __name__=='__main__':
    main()
