"""Re-import the delivered FBXs and verify geometry, materials and open void seams."""
import importlib.util
import json
import hashlib
import math
from pathlib import Path

import bmesh
import bpy
from mathutils import Vector
from mathutils.bvhtree import BVHTree

ROOT=Path(__file__).resolve().parents[3]
spec=importlib.util.spec_from_file_location('debris',ROOT/'Art/tools/blender_kit_ch5_debris.py')
kit=importlib.util.module_from_spec(spec); spec.loader.exec_module(kit)
expected=json.loads(kit.AUDIT.read_text(encoding='utf-8'))
results=[]
for rec in expected:
    bpy.ops.wm.read_factory_settings(use_empty=True)
    path=kit.OUT/f"Kit_{rec['name']}.fbx"
    assert hashlib.sha256(path.read_bytes()).hexdigest()==rec['fbx_sha256'],('FBX rewritten after generation',rec['name'])
    bpy.ops.import_scene.fbx(filepath=str(path),use_anim=False)
    objects=list(bpy.context.scene.objects)
    assert len(objects)==1 and objects[0].type=='MESH',(path,objects)
    ob=objects[0]
    assert ob.name==rec['name'],(ob.name,rec['name'])
    ob.data.transform(ob.matrix_world); ob.matrix_world.identity()
    ob.data.update(); bpy.context.view_layer.update()
    count=kit.tri_count(ob)
    assert count==rec['triangles'],(ob.name,count,rec['triangles'])
    assert max(abs(a-b) for a,b in zip(ob.dimensions,rec['dimensions_m']))<.001,(ob.name,ob.dimensions)
    for m in ob.data.materials:
        assert m.name in kit.PALETTE,m.name
        bs=m.node_tree.nodes.get('Principled BSDF')
        assert bs and max(abs(v-.5) for v in bs.inputs['Base Color'].default_value[:3])<1e-5,(ob.name,m.name)
        assert not any(n.type=='TEX_IMAGE' for n in m.node_tree.nodes)
    bm=bmesh.new(); bm.from_mesh(ob.data)
    assert all(e.is_manifold for e in bm.edges),ob.name
    assert all(f.calc_area()>1e-10 for f in bm.faces),(ob.name,'degenerate face')
    bm.free()
    centroid,vol=kit.volume_centroid(ob)
    if ob.name.startswith('FD_') or ob.name=='BA_FloatingNumeral':
        assert centroid.length<2e-5,(ob.name,centroid)
    result=dict(name=ob.name,triangles=count,one_mesh=True,grey_role_shaders=True,
                closed=True,centroid_error_m=centroid.length if rec['name'].startswith('FD_') or rec['name']=='BA_FloatingNumeral' else None)
    if ob.name=='BA_DialRing':
        verts=[v.co.copy() for v in ob.data.vertices]
        faces=[list(p.vertices) for p in ob.data.polygons]
        tree=BVHTree.FromPolygons(verts,faces,all_triangles=True)
        def top(x,y):
            return tree.ray_cast(Vector((x,y,5)),Vector((0,0,-1)),10)[0]
        probes=0
        for i in range(12):
            left=kit.edge_path(i,1); right=kit.edge_path(i,-1)
            mid=[((a[0]+b[0])/2,(a[1]+b[1])/2) for a,b in zip(left,right)]
            for a,b in zip(mid,mid[1:]):
                for t in (.25,.5,.75):
                    x=a[0]*(1-t)+b[0]*t; y=a[1]*(1-t)+b[1]*t
                    assert top(x,y) is None,('bridged void seam',i,x,y,top(x,y))
                    probes+=1
        for x,y in ((0,0),(.3,0),(-.3,0),(0,.3),(0,-.3)):
            assert top(x,y) is None,('backing disc beneath central void',x,y)
        for i in range(12):
            a=math.tau*(i+.5)/12
            for r in (3.5,7,13,18):
                p=top(r*math.cos(a),r*math.sin(a))
                assert p is not None,('missing dial leaf',i,r)
                if i not in (2,6,9): assert -.0001<=p.z<=.0801,('intact top envelope',i,p.z)
        assert max(v.z for v in verts)<=.0801
        assert abs(max(math.hypot(v.x,v.y) for v in verts)-21)<.001
        result.update(open_void_probes=probes+5,radius_m=21,intact_top_envelope_m=[0,.08],tilted_leaves=3)
    results.append(result)
    print('VERIFIED',ob.name,count,flush=True)
(ROOT/'Art/tools/specs/blender_kit_ch5_debris_fbx_validation.json').write_text(json.dumps(results,indent=2),encoding='utf-8')
print('DONE',flush=True)
