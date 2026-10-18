"""Character assembly: shared steps after a character module created its parts."""
import json
import os
import bpy
from mathutils import Vector, Matrix

from . import finalize, rig, render, palette
from . import humanoid as H

V = Vector


class Build:
    """Collects everything a character module produces."""

    def __init__(self, name, P, arm):
        self.name = name
        self.P = P
        self.arm = arm
        self.body = []
        self.face = []
        self.face_sets = {}
        self.weapons = []  # (obj, socket bone, weapon id)
        self.props = []  # (obj placed in world, prop id): standalone export only
        self.actions = {}
        self.meta = {"name": name, "clips": [], "slots": [], "sockets": []}
        self.height = 1.4

    def add(self, *items):
        for it in items:
            if isinstance(it, (list, tuple)):
                self.add(*it)
            elif it is not None:
                self.body.append(it)

    def faces(self, **kw):
        self.face = H.face(self.P, None, **kw)
        self.face_sets = {n: H.face(self.P, e, **kw) for n, e in H.EXPRESSIONS.items()}

    def action(self, name, keys_loop, impact=None, events=None):
        keys, loop = keys_loop
        act = rig.make_action(self.arm, name, keys, loop)
        self.actions[name] = act
        f0, f1 = act.frame_range
        info = {"name": name, "loop": bool(loop), "length": (f1 - f0) / 30.0, "impact": impact if impact else 0.0}
        self.meta["clips"].append(info)
        return act

    def slot(self, slot, clip):
        """Maps an Animator slot (Attack, AttackAlt, Skill, Block...) to a clip."""
        self.meta["slots"].append({"slot": slot, "clip": clip})

    def pose(self, action_name="Idle", frame=None):
        act = self.actions[action_name]
        self.arm.animation_data.action = act
        bpy.context.scene.frame_set(int(frame or act.frame_range[0]))
        bpy.context.view_layer.update()

    def socket(self, bone):
        pb = self.arm.pose.bones[bone]
        m = self.arm.matrix_world @ pb.matrix
        return m

    def hold(self, obj, bone, origin, axis_z, axis_x_hint, weapon_id):
        """Places a weapon built in local space (grip at origin) at a world frame and keeps it for parenting."""
        z = V(axis_z).normalized()
        x = V(axis_x_hint)
        x = (x - z * x.dot(z)).normalized()
        y = z.cross(x)
        m = Matrix((x, y, z)).transposed().to_4x4()
        m.translation = V(origin)
        obj.matrix_world = m
        self.weapons.append((obj, bone, weapon_id))
        return obj

    def socket_at(self, name, parent, world_pos, direction=(0, -1, 0)):
        """Adds a non deforming bone whose posed position (current pose) is world_pos."""
        import bpy as _bpy
        pb = self.arm.pose.bones[parent]
        posed = self.arm.matrix_world @ pb.matrix
        local = posed.inverted() @ V(world_pos)
        rest = pb.bone.matrix_local
        head = rest @ local
        act = self.arm.animation_data.action
        frame = _bpy.context.scene.frame_current
        _bpy.context.view_layer.objects.active = self.arm
        _bpy.ops.object.mode_set(mode="EDIT")
        eb = self.arm.data.edit_bones.new(name)
        eb.head = head
        eb.tail = head + V(direction) * 0.08
        eb.parent = self.arm.data.edit_bones[parent]
        eb.use_deform = False
        _bpy.ops.object.mode_set(mode="OBJECT")
        self.arm.pose.bones[name].rotation_mode = "QUATERNION"
        self.arm.animation_data.action = act
        _bpy.context.scene.frame_set(frame)
        _bpy.context.view_layer.update()

    def prop(self, obj, bone, prop_id):
        """Equipment that rides on the body (quiver, book, crown...): exported standalone as a prop and also
        merged into the skinned body (rigid on bone) so it costs no extra draw call."""
        import bpy as _bpy
        copy = _bpy.data.objects.new(obj.name + "Prop", obj.data.copy())
        for col in obj.users_collection:
            col.objects.link(copy)
        copy.matrix_world = obj.matrix_world.copy()
        copy.data.transform(copy.matrix_world)
        copy.matrix_world = Matrix()
        copy["bind"] = bone
        self.body.append(copy)
        self.props.append((obj, prop_id))
        obj.hide_render = True
        return obj

    def loose_prop(self, obj, prop_id):
        """Standalone equipment exported on its own (projectiles, spare parts)."""
        obj.hide_render = True
        self.props.append((obj, prop_id))
        return obj

    def hold_facing(self, obj, bone, origin, up, facing, weapon_id):
        up = V(up).normalized()
        f = V(facing).normalized()
        x = (-f).cross(up).normalized()
        return self.hold(obj, bone, origin, up, x, weapon_id)


def setup_scene():
    bpy.ops.wm.read_factory_settings(use_empty=True)
    sc = bpy.context.scene
    sc.render.fps = 30
    sc.unit_settings.system = "METRIC"
    sc.unit_settings.scale_length = 1.0


def finish(b, out_dir, weapons_dir, preview_dir, lod1_ratio=0.38, render_previews=True, strips=True):
    """Bakes UVs/material, skins, joins, LODs, parents weapons in the idle pose, renders and exports."""
    tex_dir = os.path.join(out_dir, "..", "..", "..", "Textures")
    albedo, emission = finalize.atlas_images(os.path.normpath(tex_dir))
    mat = finalize.atlas_material(albedo, emission)
    # the face carries shape keys: add them per part first
    if b.face:
        finalize.add_expression_keys(b.face, b.face_sets)
    for o in b.body + b.face:
        finalize.bake_palette_uv(o, mat)
        rig.skin(o, b.arm)
    body = finalize.join_into(b.body + b.face, b.name + "_LOD0", active=b.face[0] if b.face else None)
    body.parent = b.arm
    mod = body.modifiers.new("Armature", "ARMATURE")
    mod.object = b.arm
    lod1 = finalize.lod(body, b.name + "_LOD1", lod1_ratio)
    lod2 = finalize.lod(body, b.name + "_LOD2", lod1_ratio * 0.36)
    for lo in (lod1, lod2):
        lo.parent = b.arm
        mod = lo.modifiers.new("Armature", "ARMATURE")
        mod.object = b.arm
    b.pose("Idle")
    for obj, bone, _ in b.weapons:
        finalize.bake_palette_uv(obj, mat)
        world = obj.matrix_world.copy()
        obj.parent = b.arm
        obj.parent_type = "BONE"
        obj.parent_bone = bone
        bpy.context.view_layer.update()
        obj.matrix_world = world
    bpy.context.view_layer.update()

    stats = {"LOD0_tris": finalize.tri_count(body), "LOD1_tris": finalize.tri_count(lod1), "LOD2_tris": finalize.tri_count(lod2),
             "weapons": {wid: finalize.tri_count(o) for o, _, wid in b.weapons},
             "bones": len(b.arm.data.bones), "shape_keys": [k.name for k in body.data.shape_keys.key_blocks]
             if body.data.shape_keys else []}
    b.meta["stats"] = {k: v for k, v in stats.items() if k != "weapons"}
    b.meta["stats"]["shape_keys"] = ",".join(stats["shape_keys"])
    b.meta["weapons"] = [{"id": wid, "socket": bone, "node": o.name} for o, bone, wid in b.weapons]
    b.meta["sockets"] = [bn.name for bn in b.arm.data.bones if not bn.use_deform and bn.name != "Root"]

    os.makedirs(out_dir, exist_ok=True)
    if render_previews:
        b.pose("Idle")
        render.turnaround(preview_dir, b.name, height=b.height)
        if strips:
            for name, act in b.actions.items():
                render.anim_strip(os.path.join(preview_dir, "anim"), b.arm, act, 6, b.height)
        b.pose("Idle")

    export_fbx(b, [body, lod1, lod2], os.path.join(out_dir, b.name + ".fbx"))
    os.makedirs(weapons_dir, exist_ok=True)
    for obj, bone, wid in b.weapons:
        export_weapon(obj, os.path.join(weapons_dir, wid + ".fbx"))
    for obj, pid in b.props:
        finalize.bake_palette_uv(obj, mat)
        obj.data.transform(obj.matrix_world)
        obj.matrix_world = Matrix()
        xs = [v.co for v in obj.data.vertices]
        lo = V((min(p.x for p in xs), min(p.y for p in xs), min(p.z for p in xs)))
        hi = V((max(p.x for p in xs), max(p.y for p in xs), max(p.z for p in xs)))
        obj.data.transform(Matrix.Translation(-V(((lo.x + hi.x) / 2, (lo.y + hi.y) / 2, lo.z))))
        export_weapon(obj, os.path.join(weapons_dir, pid + ".fbx"))
    with open(os.path.join(out_dir, b.name + ".json"), "w", encoding="utf-8") as fh:
        json.dump(b.meta, fh, indent=2)
    return body, lod1


def _select(objs):
    for o in bpy.context.view_layer.objects:
        o.select_set(False)
    for o in objs:
        o.select_set(True)
    bpy.context.view_layer.objects.active = objs[0]


def export_fbx(b, meshes, path):
    b.arm.animation_data.action = None
    for pb in b.arm.pose.bones:
        pb.rotation_quaternion = (1, 0, 0, 0)
        pb.location = (0, 0, 0)
    objs = [b.arm] + meshes + [o for o, _, _ in b.weapons]
    _select(objs)
    bpy.ops.export_scene.fbx(
        filepath=path, use_selection=True, object_types={"ARMATURE", "MESH"},
        apply_scale_options="FBX_SCALE_ALL", axis_forward="Z", axis_up="Y",
        use_mesh_modifiers=True, mesh_smooth_type="OFF", use_tspace=False,
        add_leaf_bones=False, primary_bone_axis="Y", secondary_bone_axis="X",
        use_armature_deform_only=False, armature_nodetype="NULL",
        bake_anim=True, bake_anim_use_all_bones=True, bake_anim_use_nla_strips=False,
        bake_anim_use_all_actions=True, bake_anim_force_startend_keying=True, bake_anim_step=1.0,
        bake_anim_simplify_factor=0.6, path_mode="STRIP", embed_textures=False)


def export_weapon(obj, path):
    me = obj.data.copy()
    tmp = bpy.data.objects.new(obj.name, me)
    bpy.context.scene.collection.objects.link(tmp)
    obj.name = obj.name + "_held"
    tmp.name = tmp.name.replace(".001", "")
    _select([tmp])
    bpy.ops.export_scene.fbx(
        filepath=path, use_selection=True, object_types={"MESH"}, apply_scale_options="FBX_SCALE_ALL",
        axis_forward="Z", axis_up="Y", mesh_smooth_type="OFF", bake_anim=False, path_mode="STRIP")
    bpy.data.objects.remove(tmp, do_unlink=True)
    obj.name = obj.name[:-5]
