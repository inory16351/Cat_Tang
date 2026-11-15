# Blender 배치 렌더: 모델(GLB/FBX)을 앞 · 옆 · 뒤 · 3/4 · 탑뷰(게임 카메라 50도)로 찍어 한 장으로 합친다
# 실행: blender -b -P render_turntable.py -- <model.glb> <out.png>
import bpy, sys, math, os
from mathutils import Vector

argv = sys.argv[sys.argv.index("--") + 1:]
model_path, out_path = argv[0], argv[1]
tmp_dir = os.path.join(os.path.dirname(out_path), "_views")
os.makedirs(tmp_dir, exist_ok=True)

bpy.ops.wm.read_factory_settings(use_empty=True)
ext = os.path.splitext(model_path)[1].lower()
if ext in (".glb", ".gltf"):
    bpy.ops.import_scene.gltf(filepath=model_path)
elif ext == ".fbx":
    bpy.ops.import_scene.fbx(filepath=model_path)
else:
    bpy.ops.wm.obj_import(filepath=model_path)

meshes = [o for o in bpy.context.scene.objects if o.type == "MESH"]
# 전체 경계 상자
mn = Vector((1e9, 1e9, 1e9)); mx = Vector((-1e9, -1e9, -1e9))
for o in meshes:
    for c in o.bound_box:
        w = o.matrix_world @ Vector(c)
        mn = Vector((min(mn[i], w[i]) for i in range(3)))
        mx = Vector((max(mx[i], w[i]) for i in range(3)))
center = (mn + mx) / 2
size = max(mx - mn)
tris = sum(len(o.data.polygons) for o in meshes)
print(f"MESHES {len(meshes)} FACES {tris} SIZE {tuple(round(v,3) for v in (mx-mn))}")

scene = bpy.context.scene
scene.render.engine = "BLENDER_EEVEE_NEXT" if "BLENDER_EEVEE_NEXT" in [e.identifier for e in bpy.types.RenderSettings.bl_rna.properties["engine"].enum_items] else "BLENDER_EEVEE"
scene.render.resolution_x = 512
scene.render.resolution_y = 512
scene.render.film_transparent = False
world = bpy.data.worlds.new("W"); scene.world = world
world.use_nodes = True
world.node_tree.nodes["Background"].inputs[0].default_value = (0.16, 0.14, 0.2, 1)
world.node_tree.nodes["Background"].inputs[1].default_value = 1.0

sun = bpy.data.objects.new("Sun", bpy.data.lights.new("Sun", "SUN"))
sun.data.energy = 3.0
sun.rotation_euler = (math.radians(50), 0, math.radians(30))
scene.collection.objects.link(sun)

cam_data = bpy.data.cameras.new("Cam")
cam_data.type = "ORTHO"
cam_data.ortho_scale = size * 1.25
cam = bpy.data.objects.new("Cam", cam_data)
scene.collection.objects.link(cam)
scene.camera = cam

def look_at(obj, target):
    d = target - obj.location
    obj.rotation_euler = d.to_track_quat("-Z", "Y").to_euler()

# glTF는 Y-up을 Z-up으로 바꿔 가져오고, 앞모습은 -Y 쪽을 본다
views = [("front", 0, 0), ("side", 90, 0), ("back", 180, 0), ("three_quarter", 35, 10), ("topdown_50", 20, 50)]
dist = size * 3
files = []
for name, yaw, pitch in views:
    y, p = math.radians(yaw), math.radians(pitch)
    off = Vector((math.sin(y) * math.cos(p), -math.cos(y) * math.cos(p), math.sin(p))) * dist
    cam.location = center + off
    look_at(cam, center)
    f = os.path.join(tmp_dir, f"{name}.png")
    scene.render.filepath = f
    bpy.ops.render.render(write_still=True)
    files.append(f)

# 한 장으로 합치기
imgs = [bpy.data.images.load(f) for f in files]
W = 512 * len(imgs); H = 512
sheet = bpy.data.images.new("sheet", W, H)
px = [0.0] * (W * H * 4)
for k, im in enumerate(imgs):
    src = list(im.pixels)
    for row in range(H):
        s = row * 512 * 4
        d = (row * W + k * 512) * 4
        px[d:d + 512 * 4] = src[s:s + 512 * 4]
sheet.pixels = px
sheet.filepath_raw = out_path
sheet.file_format = "PNG"
sheet.save()
print("SAVED", out_path)
