# Directional grain, soft erosion and luminance variation baked in Blender.
# blender -b --factory-startup -P Art/tools/blender_slash_tex.py
import bpy, os

OUT = os.path.join(os.path.dirname(__file__), "..", "..", "Game", "Assets", "Resources", "FX", "Tex")
W, H = 512, 128

def reset():
    bpy.ops.wm.read_factory_settings(use_empty=True)
    sc = bpy.context.scene
    sc.render.engine = "CYCLES"
    sc.cycles.samples = 4
    sc.render.resolution_x, sc.render.resolution_y = W, H
    sc.render.resolution_percentage = 100
    sc.render.image_settings.file_format = "PNG"
    sc.render.image_settings.color_mode = "RGB"
    sc.view_settings.view_transform = "Raw"
    sc.render.dither_intensity = 0
    sc.render.film_transparent = False
    world = bpy.data.worlds.new("w"); world.use_nodes = True
    world.node_tree.nodes["Background"].inputs[0].default_value = (0, 0, 0, 1)
    sc.world = world
    cam = bpy.data.cameras.new("cam"); cam.type = "ORTHO"; cam.ortho_scale = 4.0
    co = bpy.data.objects.new("cam", cam); sc.collection.objects.link(co); sc.camera = co
    co.location = (0, 0, 5)
    bpy.ops.mesh.primitive_plane_add(size=1)
    pl = bpy.context.active_object; pl.scale = (4.0, 1.0, 1)
    return sc, pl

def flow_channel(nt, uv, kind):
    n = nt.nodes
    l = nt.links
    sep = n.new("ShaderNodeSeparateXYZ")
    l.new(uv, sep.inputs[0])
    # u를 원 둘레로 감아 이음매 없이: (cos 2πu, sin 2πu) · R, v는 z
    def math(op, a, b=None):
        mn = n.new("ShaderNodeMath"); mn.operation = op
        if isinstance(a, float): mn.inputs[0].default_value = a
        else: l.new(a, mn.inputs[0])
        if b is not None:
            if isinstance(b, float): mn.inputs[1].default_value = b
            else: l.new(b, mn.inputs[1])
        return mn.outputs[0]
    ang = math("MULTIPLY", sep.outputs["X"], 2 * 3.14159265)
    R = {"streak": 0.32, "erode": 0.85, "chunk": 0.48}[kind]
    cx = math("MULTIPLY", math("COSINE", ang), R)
    cy = math("MULTIPLY", math("SINE", ang), R)
    vz = math("MULTIPLY", sep.outputs["Y"], {"streak": 7.0, "erode": 2.2, "chunk": 1.2}[kind])
    comb = n.new("ShaderNodeCombineXYZ"); l.new(cx, comb.inputs[0]); l.new(cy, comb.inputs[1]); l.new(vz, comb.inputs[2])
    val = None
    if kind == "streak":
        # 폭 방향(z)으로 촘촘한 보로노이 경계 = 길이 방향으로 길게 늘어진 결
        vo = n.new("ShaderNodeTexVoronoi"); vo.voronoi_dimensions = "3D"; vo.feature = "DISTANCE_TO_EDGE"
        vo.inputs["Scale"].default_value = 2.2; vo.inputs["Randomness"].default_value = 1.0
        l.new(comb.outputs[0], vo.inputs["Vector"])
        cr = n.new("ShaderNodeMapRange"); cr.inputs[1].default_value = 0.0; cr.inputs[2].default_value = 0.22
        l.new(vo.outputs["Distance"], cr.inputs[0]); val = cr.outputs[0]
    elif kind == "erode":
        no = n.new("ShaderNodeTexNoise"); no.noise_dimensions = "3D"
        no.inputs["Scale"].default_value = 1.9; no.inputs["Detail"].default_value = 2.0; no.inputs["Roughness"].default_value = 0.55
        l.new(comb.outputs[0], no.inputs["Vector"])
        cr = n.new("ShaderNodeMapRange"); cr.inputs[1].default_value = 0.28; cr.inputs[2].default_value = 0.72
        l.new(no.outputs["Fac"], cr.inputs[0]); val = cr.outputs[0]
    else:
        vo = n.new("ShaderNodeTexVoronoi"); vo.voronoi_dimensions = "3D"; vo.feature = "F1"
        vo.inputs["Scale"].default_value = 1.6
        l.new(comb.outputs[0], vo.inputs["Vector"])
        cr = n.new("ShaderNodeMapRange"); cr.inputs[1].default_value = 0.05; cr.inputs[2].default_value = 0.85
        l.new(vo.outputs["Distance"], cr.inputs[0]); val = cr.outputs[0]
    return val


def main():
    os.makedirs(OUT, exist_ok=True)
    sc, plane = reset()
    material = bpy.data.materials.new("Slash flow")
    material.use_nodes = True
    tree = material.node_tree
    tree.nodes.clear()
    uv = tree.nodes.new("ShaderNodeTexCoord").outputs["UV"]
    combine = tree.nodes.new("ShaderNodeCombineColor")
    for kind, channel in (("streak", "Red"), ("erode", "Green"), ("chunk", "Blue")):
        tree.links.new(flow_channel(tree, uv, kind), combine.inputs[channel])
    emission = tree.nodes.new("ShaderNodeEmission")
    output = tree.nodes.new("ShaderNodeOutputMaterial")
    tree.links.new(combine.outputs[0], emission.inputs["Color"])
    tree.links.new(emission.outputs[0], output.inputs["Surface"])
    plane.data.materials.append(material)
    sc.render.filepath = os.path.join(OUT, "slash_flow.png")
    bpy.ops.render.render(write_still=True)


main()
