"""Preview renders: turnaround (front/side/back/perspective) and animation strips."""
import math
import os
import bpy
from mathutils import Vector, Euler

V = Vector


def setup_scene(res=(520, 720), bg=(0.06, 0.09, 0.2)):
    sc = bpy.context.scene
    for eng in ("BLENDER_EEVEE_NEXT", "BLENDER_EEVEE", "BLENDER_WORKBENCH"):
        try:
            sc.render.engine = eng
            break
        except TypeError:
            continue
    sc.render.resolution_x, sc.render.resolution_y = res
    sc.render.resolution_percentage = 100
    sc.render.film_transparent = False
    sc.view_settings.view_transform = "Standard"
    sc.view_settings.look = "None"
    world = sc.world or bpy.data.worlds.new("World")
    sc.world = world
    world.use_nodes = True
    bgn = world.node_tree.nodes["Background"]
    bgn.inputs["Color"].default_value = (*bg, 1)
    bgn.inputs["Strength"].default_value = 1.0
    if hasattr(sc, "eevee"):
        try:
            sc.eevee.taa_render_samples = 16
        except AttributeError:
            pass
    for nm, rot, energy, color in (("Key", (50, 0, -35), 3.2, (1.0, 0.96, 0.9)),
                                   ("Fill", (60, 0, 140), 1.2, (0.7, 0.8, 1.0)),
                                   ("Rim", (20, 0, 180), 2.0, (0.8, 0.85, 1.0))):
        lt = bpy.data.lights.get(nm) or bpy.data.lights.new(nm, "SUN")
        lt.energy = energy
        lt.color = color
        ob = bpy.data.objects.get(nm) or bpy.data.objects.new(nm, lt)
        if ob.name not in sc.collection.objects:
            sc.collection.objects.link(ob)
        ob.rotation_euler = Euler([math.radians(a) for a in rot])
    cam_data = bpy.data.cameras.get("PreviewCam") or bpy.data.cameras.new("PreviewCam")
    cam = bpy.data.objects.get("PreviewCam") or bpy.data.objects.new("PreviewCam", cam_data)
    if cam.name not in sc.collection.objects:
        sc.collection.objects.link(cam)
    sc.camera = cam
    return cam


def _aim(cam, target, yaw_deg, pitch_deg, dist):
    yaw, pitch = math.radians(yaw_deg), math.radians(pitch_deg)
    # yaw 0 = looking at the character's face (camera at -Y)
    d = V((math.sin(yaw) * math.cos(pitch), -math.cos(yaw) * math.cos(pitch), math.sin(pitch)))
    cam.location = target + d * dist
    look = (target - cam.location).normalized()
    cam.rotation_euler = look.to_track_quat("-Z", "Y").to_euler()


def turnaround(out_dir, prefix, height=1.4, center_z=None, ortho_scale=None, views=None):
    cam = setup_scene()
    os.makedirs(out_dir, exist_ok=True)
    cz = center_z if center_z is not None else height * 0.5
    target = V((0, 0, cz))
    views = views or [("front", 0, 4, True), ("side", 90, 4, True), ("back", 180, 4, True),
                      ("perspective", 35, 16, False)]
    paths = []
    for name, yaw, pitch, ortho in views:
        cam.data.type = "ORTHO" if ortho else "PERSP"
        cam.data.ortho_scale = ortho_scale or height * 1.25
        cam.data.lens = 50
        _aim(cam, target, yaw, pitch, 6.0 if ortho else height * 2.6)
        p = os.path.join(out_dir, f"{prefix}_{name}.png")
        bpy.context.scene.render.filepath = p
        bpy.ops.render.render(write_still=True)
        paths.append(p)
    return paths


def anim_strip(out_dir, arm_obj, action, frames=6, height=1.4, yaw=35, res=(300, 380), ortho_scale=None):
    sc = bpy.context.scene
    cam = setup_scene(res)
    cam.data.type = "ORTHO"
    cam.data.ortho_scale = ortho_scale or height * 1.35
    _aim(cam, V((0, 0, height * 0.5)), yaw, 10, 6.0)
    arm_obj.animation_data.action = action
    f0, f1 = action.frame_range
    paths = []
    for i in range(frames):
        f = f0 + (f1 - f0) * i / max(1, frames - 1)
        sc.frame_set(int(round(f)))
        p = os.path.join(out_dir, f"anim_{action.name}_{i}.png")
        sc.render.filepath = p
        bpy.ops.render.render(write_still=True)
        paths.append(p)
    return paths
