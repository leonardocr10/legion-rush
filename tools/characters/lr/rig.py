"""Armature creation, skinning and posing helpers.

Bone names follow Unity's humanoid naming so the rig can be mapped as Humanoid later, but the game
imports it as Generic (cheaper for crowds). Poses are authored as rotations in the *rest-aligned
armature frame* of each bone (degrees, XYZ):
  X+  limb pointing down swings BACK, pointing up leans FORWARD   (character faces -Y)
  Y+  limb pointing down swings toward the character's RIGHT (-X)
  Z+  yaw toward the character's LEFT (+X)
"""
import math
import bpy
from mathutils import Vector, Matrix, Euler, Quaternion

V = Vector

LOC_BONES = {"Hips", "HorseBody"}
SOCKETS = ("RightHandSocket", "LeftHandSocket", "HeadSocket", "BackSocket", "ProjectileSpawn", "VFXSocket")


def build_armature(name, bones):
    """bones: list of dict(name, head, tail, parent=None, deform=True, roll=0)."""
    arm = bpy.data.armatures.new(name)
    arm.display_type = "STICK"
    obj = bpy.data.objects.new(name, arm)
    bpy.context.scene.collection.objects.link(obj)
    bpy.context.view_layer.objects.active = obj
    bpy.ops.object.mode_set(mode="EDIT")
    eb = arm.edit_bones
    for b in bones:
        e = eb.new(b["name"])
        e.head = V(b["head"])
        e.tail = V(b["tail"])
        e.use_deform = b.get("deform", True)
        e.align_roll(V(b.get("roll_axis", (0, -1, 0))))
    for b in bones:
        if b.get("parent"):
            eb[b["name"]].parent = eb[b["parent"]]
            eb[b["name"]].use_connect = False
    bpy.ops.object.mode_set(mode="OBJECT")
    for pb in obj.pose.bones:
        pb.rotation_mode = "QUATERNION"
    return obj


def _seg_dist(p, a, b):
    ab = b - a
    t = max(0.0, min(1.0, (p - a).dot(ab) / max(ab.length_squared, 1e-12)))
    return (a + ab * t - p).length


def skin(obj, arm_obj, power=5.0, max_influences=2):
    """Creates vertex groups from obj['bind'] (one bone = rigid, several = blended by distance)."""
    bones = [b.strip() for b in obj["bind"].split(",")]
    for b in bones:
        if b not in arm_obj.data.bones:
            raise KeyError(f"{obj.name}: bone '{b}' missing")
    groups = {b: obj.vertex_groups.get(b) or obj.vertex_groups.new(name=b) for b in bones}
    if len(bones) == 1:
        groups[bones[0]].add([v.index for v in obj.data.vertices], 1.0, "REPLACE")
        return
    segs = {b: (arm_obj.data.bones[b].head_local.copy(), arm_obj.data.bones[b].tail_local.copy()) for b in bones}
    for v in obj.data.vertices:
        p = V(v.co)
        ws = []
        for b in bones:
            d = _seg_dist(p, *segs[b])
            ws.append((1.0 / (d + 0.004) ** power, b))
        ws.sort(reverse=True)
        ws = ws[:max_influences]
        tot = sum(w for w, _ in ws)
        for w, b in ws:
            if w / tot > 0.02:
                groups[b].add([v.index], w / tot, "REPLACE")


def attach(obj, arm_obj):
    mod = obj.modifiers.new("Armature", "ARMATURE")
    mod.object = arm_obj
    obj.parent = arm_obj


def parent_to_bone(obj, arm_obj, bone):
    """Rigid child of a bone (weapons). Keeps the current world placement."""
    world = obj.matrix_world.copy()
    obj.parent = arm_obj
    obj.parent_type = "BONE"
    obj.parent_bone = bone
    obj.matrix_world = world


# ---------------------------------------------------------------- posing

def _basis(pb, euler_deg):
    rest = pb.bone.matrix_local.to_3x3()
    r = Euler([math.radians(a) for a in euler_deg], "XYZ").to_matrix()
    return (rest.inverted() @ r @ rest).to_quaternion()


def mirror_pose(pose):
    out = {}
    for k, val in pose.items():
        if k.startswith("_"):
            if k == "_hips" or k.startswith("_loc:"):
                out[k] = (-val[0], val[1], val[2])
            elif k == "_hipsrot":
                out[k] = (val[0], -val[1], -val[2])
            else:
                out[k] = val
            continue
        name = k.replace("Left", "\0").replace("Right", "Left").replace("\0", "Right")
        out[name] = (val[0], -val[1], -val[2])
    return out


def apply_pose(arm_obj, pose):
    """pose: {bone: (x, y, z) degrees, '_hips': (x, y, z) offset in meters}. Unlisted bones reset."""
    for pb in arm_obj.pose.bones:
        pb.rotation_quaternion = Quaternion()
        pb.location = V()
    for name, val in pose.items():
        if name.startswith("_loc:"):
            pb = arm_obj.pose.bones.get(name[5:])
            if pb is not None:
                pb.location = pb.bone.matrix_local.to_3x3().inverted() @ V(val)
            continue
        if name == "_hips":
            pb = arm_obj.pose.bones["Hips"]
            rest = pb.bone.matrix_local.to_3x3()
            pb.location = rest.inverted() @ V(val)
            continue
        if name.startswith("_"):
            continue
        pb = arm_obj.pose.bones.get(name)
        if pb is None:
            continue
        pb.rotation_quaternion = _basis(pb, val)
    if "_hipsrot" in pose:
        pb = arm_obj.pose.bones["Hips"]
        pb.rotation_quaternion = _basis(pb, pose["_hipsrot"]) @ pb.rotation_quaternion


def key_pose(arm_obj, frame):
    for pb in arm_obj.pose.bones:
        pb.keyframe_insert("rotation_quaternion", frame=frame, group=pb.name)
        if pb.name in LOC_BONES:
            pb.keyframe_insert("location", frame=frame, group=pb.name)


def make_action(arm_obj, name, keys, loop=False, fps=30):
    """keys: list of (time_seconds, pose). Creates an action with Bezier keys.
    Loops get the first pose repeated at the end."""
    act = bpy.data.actions.new(name)
    act.use_fake_user = True
    arm_obj.animation_data_create()
    arm_obj.animation_data.action = act
    frames = []
    prev = {}
    for t, pose in keys:
        f = round(t * fps) + 1
        apply_pose(arm_obj, pose)
        # keep quaternions in the same hemisphere as the previous key (no long-way flips)
        for pb in arm_obj.pose.bones:
            q = pb.rotation_quaternion
            p = prev.get(pb.name)
            if p is not None and p.dot(q) < 0:
                pb.rotation_quaternion = -q
            prev[pb.name] = pb.rotation_quaternion.copy()
        key_pose(arm_obj, f)
        frames.append(f)
    for fc in act.fcurves:
        fc.update()
        for kp in fc.keyframe_points:
            kp.interpolation = "BEZIER"
            kp.handle_left_type = "AUTO_CLAMPED"
            kp.handle_right_type = "AUTO_CLAMPED"
    act.frame_range = (frames[0], frames[-1])
    act.use_frame_range = True
    act["loop"] = 1 if loop else 0
    return act
