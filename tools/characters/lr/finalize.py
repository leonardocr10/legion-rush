"""Palette UVs, single atlas material, skinning, joins, facial shape keys and LODs."""
import os
import bpy
from mathutils import Vector

from . import palette, rig

V = Vector
ATLAS_MAT = "LR_CharacterAtlas"


def atlas_images(out_dir):
    """Writes the albedo + emission atlas PNGs (shared by every character) and returns the images."""
    os.makedirs(out_dir, exist_ok=True)
    albedo, emission = palette.atlas_pixels()
    imgs = []
    for nm, px in (("LR_CharacterAtlas", albedo), ("LR_CharacterAtlas_Emission", emission)):
        img = bpy.data.images.get(nm) or bpy.data.images.new(nm, palette.ATLAS_SIZE, palette.ATLAS_SIZE, alpha=False)
        img.pixels.foreach_set(px)
        img.filepath_raw = os.path.join(out_dir, nm + ".png")
        img.file_format = "PNG"
        img.save()
        imgs.append(img)
    return imgs


def atlas_material(albedo_img, emission_img):
    mat = bpy.data.materials.get(ATLAS_MAT)
    if mat:
        return mat
    mat = bpy.data.materials.new(ATLAS_MAT)
    mat.use_nodes = True
    nt = mat.node_tree
    bsdf = nt.nodes["Principled BSDF"]
    tex = nt.nodes.new("ShaderNodeTexImage")
    tex.image = albedo_img
    tex.interpolation = "Closest"
    em = nt.nodes.new("ShaderNodeTexImage")
    em.image = emission_img
    em.interpolation = "Closest"
    nt.links.new(tex.outputs["Color"], bsdf.inputs["Base Color"])
    nt.links.new(em.outputs["Color"], bsdf.inputs["Emission Color"])
    bsdf.inputs["Emission Strength"].default_value = 2.5
    bsdf.inputs["Roughness"].default_value = 0.55
    return mat


def bake_palette_uv(obj, mat):
    """Moves every face's UVs into its palette swatch and swaps all slots for the atlas material."""
    me = obj.data
    keys = []
    for m in me.materials:
        keys.append(m.name[4:] if m and m.name.startswith("pal_") else obj.get("pal", "blue"))
    if not me.uv_layers:
        me.uv_layers.new(name="UVMap")
    uv = me.uv_layers.active.data
    zs = [v.co.z for v in me.vertices]
    if "grad" in obj:
        z0, z1 = obj["grad"]
    else:
        z0, z1 = (min(zs), max(zs)) if zs else (0, 1)
    span = max(z1 - z0, 1e-5)
    for p in me.polygons:
        key = keys[p.material_index] if p.material_index < len(keys) else obj.get("pal", "blue")
        u0, v0, u1, v1 = palette.swatch_rect(key)
        for li in p.loop_indices:
            z = me.vertices[me.loops[li].vertex_index].co.z
            t = min(1.0, max(0.0, (z - z0) / span))
            uv[li].uv = ((u0 + u1) * 0.5, v0 + (v1 - v0) * t)
    me.materials.clear()
    me.materials.append(mat)
    for p in me.polygons:
        p.material_index = 0


def add_expression_keys(neutral_parts, expr_sets):
    """neutral_parts: list of objects. expr_sets: {name: [objects in the same order]}.
    Adds shape keys per part, then deletes the expression copies."""
    for i, obj in enumerate(neutral_parts):
        obj.shape_key_add(name="Basis", from_mix=False)
        for name, objs in expr_sets.items():
            src = objs[i]
            if len(src.data.vertices) != len(obj.data.vertices):
                raise RuntimeError(f"expression {name}: topology mismatch on {obj.name}")
            kb = obj.shape_key_add(name=name, from_mix=False)
            for v, sv in zip(kb.data, src.data.vertices):
                v.co = sv.co
    for objs in expr_sets.values():
        for o in objs:
            bpy.data.objects.remove(o, do_unlink=True)


def join_into(objs, name, active=None):
    objs = [o for o in objs if o is not None]
    base = active or objs[0]
    for o in bpy.context.view_layer.objects:
        o.select_set(False)
    for o in objs:
        o.select_set(True)
    bpy.context.view_layer.objects.active = base
    bpy.ops.object.join()
    base.name = name
    base.data.name = name
    return base


def lod(obj, name, ratio):
    me = obj.data.copy()
    new = bpy.data.objects.new(name, me)
    for c in obj.users_collection:
        c.objects.link(new)
    new.parent = obj.parent
    if me.shape_keys:
        new.shape_key_clear()
    dec = new.modifiers.new("dec", "DECIMATE")
    dec.ratio = ratio
    dec.delimit = {"UV", "MATERIAL", "SEAM"}
    dec.use_collapse_triangulate = True
    bpy.context.view_layer.objects.active = new
    for o in bpy.context.view_layer.objects:
        o.select_set(o == new)
    bpy.ops.object.modifier_apply(modifier="dec")
    return new


def tri_count(obj):
    return sum(len(p.vertices) - 2 for p in obj.data.polygons)


def finish_character(name, arm_obj, body_parts, face_parts, rigid_parts, mat, lod1_ratio=0.38):
    """body_parts/face_parts: skinned objects. rigid_parts: [(obj, bone)] weapons parented to bones."""
    for o in body_parts + face_parts:
        bake_palette_uv(o, mat)
        rig.skin(o, arm_obj)
    body = join_into(body_parts + face_parts, name + "_LOD0", active=face_parts[0] if face_parts else None)
    rig.attach(body, arm_obj)
    lod1 = lod(body, name + "_LOD1", lod1_ratio)
    lod1.parent = None
    rig.attach(lod1, arm_obj)
    weapons = []
    for o, bone in rigid_parts:
        bake_palette_uv(o, mat)
        rig.parent_to_bone(o, arm_obj, bone)
        weapons.append(o)
    return body, lod1, weapons
