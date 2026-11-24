# 받은 캐릭터 FBX(+ 기본색 텍스처)를 정면 · 3/4 · 옆 · 뒤 4장으로 렌더해 한 장으로 붙임 (모델 품질 확인용)
# 사용: blender -b --factory-startup -P blender_model_preview.py -- <model.fbx> <texture.png> <out.png>
import bpy, sys, math, os
from mathutils import Vector

argv = sys.argv[sys.argv.index("--") + 1:]
fbx, tex, out = argv[0], argv[1], argv[2]

bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.fbx(filepath=fbx)
meshes = [o for o in bpy.context.scene.objects if o.type == "MESH"]

img = bpy.data.images.load(tex)
mat = bpy.data.materials.new("Prev")
mat.use_nodes = True
nt = mat.node_tree
bsdf = nt.nodes.get("Principled BSDF")
ti = nt.nodes.new("ShaderNodeTexImage"); ti.image = img
nt.links.new(ti.outputs["Color"], bsdf.inputs["Base Color"])
bsdf.inputs["Roughness"].default_value = 0.8
for o in meshes:
    o.data.materials.clear(); o.data.materials.append(mat)

# 크기 · 위치
bpy.context.view_layer.update()
pts = [o.matrix_world @ Vector(c) for o in meshes for c in o.bound_box]
mn = Vector((min(p.x for p in pts), min(p.y for p in pts), min(p.z for p in pts)))
mx = Vector((max(p.x for p in pts), max(p.y for p in pts), max(p.z for p in pts)))
center = (mn + mx) / 2
h = max(mx.z - mn.z, mx.y - mn.y, mx.x - mn.x)

sc = bpy.context.scene
sc.render.engine = "BLENDER_EEVEE_NEXT" if "BLENDER_EEVEE_NEXT" in [e.identifier for e in bpy.types.RenderSettings.bl_rna.properties["engine"].enum_items] else "BLENDER_EEVEE"
sc.render.resolution_x = 420; sc.render.resolution_y = 560
sc.render.film_transparent = False
w = bpy.data.worlds.new("W"); sc.world = w; w.use_nodes = True
w.node_tree.nodes["Background"].inputs[0].default_value = (0.75, 0.75, 0.78, 1)
w.node_tree.nodes["Background"].inputs[1].default_value = 1.0
sun = bpy.data.objects.new("Sun", bpy.data.lights.new("Sun", "SUN")); sc.collection.objects.link(sun)
sun.data.energy = 3.0; sun.rotation_euler = (math.radians(50), 0, math.radians(30))

cam = bpy.data.objects.new("Cam", bpy.data.cameras.new("Cam")); sc.collection.objects.link(cam); sc.camera = cam
cam.data.type = "ORTHO"; cam.data.ortho_scale = h * 1.15

# 위가 Z인지 Y인지 (FBX 가져오기는 보통 Z 위)
up = Vector((0, 0, 1))
frames = []
base = os.path.splitext(out)[0]
for i, ang in enumerate([0, 45, 90, 180]):
    a = math.radians(ang)
    d = Vector((math.sin(a), -math.cos(a), 0)) * h * 3
    cam.location = center + d
    cam.rotation_euler = (center - cam.location).to_track_quat("-Z", "Y").to_euler()
    p = f"{base}_{i}.png"
    sc.render.filepath = p
    bpy.ops.render.render(write_still=True)
    frames.append(p)

# 붙이기
imgs = [bpy.data.images.load(p) for p in frames]
W, H = imgs[0].size
outimg = bpy.data.images.new("out", W * len(imgs), H)
px = [0.0] * (W * len(imgs) * H * 4)
for k, im in enumerate(imgs):
    src = list(im.pixels)
    for y in range(H):
        row = src[y * W * 4:(y + 1) * W * 4]
        o = (y * W * len(imgs) + k * W) * 4
        px[o:o + W * 4] = row
outimg.pixels = px
outimg.filepath_raw = out; outimg.file_format = "PNG"; outimg.save()
for p in frames: os.remove(p)
print("saved", out, "height", h)
