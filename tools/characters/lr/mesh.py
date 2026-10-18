"""Low-level modeling helpers (bmesh based).

Conventions (Blender space): Z up, character faces -Y, +X is the character's LEFT side.
Every part is its own object while modeling. Custom properties on the object drive the later passes:
  obj["pal"]   palette key used for faces whose material is the default slot
  obj["bind"]  comma separated bone names; one name = rigid, several = blended by distance
  obj["grad"]  optional (zmin, zmax) world range for the swatch gradient
Faces may carry several palette keys through material slots named "pal_<key>".
"""
import math
import bmesh
import bpy
from mathutils import Vector, Matrix, Euler

from . import palette

V = Vector
TAU = math.tau
DETAIL = 0.75  # global segment multiplier (mobile budget)


def seg(n, lo=4):
    return max(lo, int(round(n * DETAIL)))


# ---------------------------------------------------------------- materials / objects

def pal_material(key):
    name = "pal_" + key
    mat = bpy.data.materials.get(name)
    if mat is None:
        mat = bpy.data.materials.new(name)
        rgb = palette.hex_to_linear(palette.COLORS[key][0])
        mat.diffuse_color = (*rgb, 1.0)
        mat.use_nodes = True
        bsdf = mat.node_tree.nodes.get("Principled BSDF")
        bsdf.inputs["Base Color"].default_value = (*rgb, 1.0)
        bsdf.inputs["Roughness"].default_value = 0.6
        if palette.COLORS[key][2]:
            bsdf.inputs["Emission Color"].default_value = (*rgb, 1.0)
            bsdf.inputs["Emission Strength"].default_value = 3.0
    return mat


def collection(name="Parts"):
    col = bpy.data.collections.get(name)
    if col is None:
        col = bpy.data.collections.new(name)
        bpy.context.scene.collection.children.link(col)
    return col


def obj_from_bm(name, bm, pal="blue", bind=None, smooth=True, sharp_angle=None, col=None):
    me = bpy.data.meshes.new(name)
    bm.normal_update()
    bm.to_mesh(me)
    bm.free()
    obj = bpy.data.objects.new(name, me)
    (col or collection()).objects.link(obj)
    me.materials.append(pal_material(pal))
    obj["pal"] = pal
    if bind:
        obj["bind"] = bind
    for p in me.polygons:
        p.use_smooth = smooth
    if smooth and sharp_angle is not None:
        me.set_sharp_from_angle(angle=math.radians(sharp_angle))
    return obj


def add_pal(obj, key):
    """Adds a palette material slot to obj and returns its index."""
    mat = pal_material(key)
    for i, m in enumerate(obj.data.materials):
        if m == mat:
            return i
    obj.data.materials.append(mat)
    return len(obj.data.materials) - 1


def set_faces_pal(obj, key, predicate):
    """Assigns palette key to faces whose center satisfies predicate(center_vector, normal)."""
    idx = add_pal(obj, key)
    for p in obj.data.polygons:
        if predicate(V(p.center), V(p.normal)):
            p.material_index = idx


# ---------------------------------------------------------------- transforms

def xform(obj, loc=(0, 0, 0), rot=(0, 0, 0), scale=(1, 1, 1), pivot=(0, 0, 0)):
    """Bakes a transform into the mesh data (rotation in degrees XYZ around pivot)."""
    m = (Matrix.Translation(V(loc)) @ Matrix.Translation(V(pivot)) @
         Euler([math.radians(a) for a in rot], "XYZ").to_matrix().to_4x4() @
         Matrix.Diagonal((*scale, 1.0)) @ Matrix.Translation(-V(pivot)))
    obj.data.transform(m)
    obj.data.update()
    return obj


def deform(obj, fn):
    """fn(Vector) -> Vector applied to every vertex."""
    for v in obj.data.vertices:
        v.co = fn(V(v.co))
    obj.data.update()
    return obj


def mirror_name(name):
    for a, b in (("Left", "\0"), ("Right", "Left"), ("\0", "Right"), (".L", "\1"), (".R", ".L"), ("\1", ".R")):
        name = name.replace(a, b)
    return name


def mirror_copy(obj, name=None):
    """Duplicate mirrored on X (character side swap); flips bind names and normals."""
    me = obj.data.copy()
    me.transform(Matrix.Diagonal((-1, 1, 1, 1)))
    me.flip_normals()
    new = bpy.data.objects.new(name or mirror_name(obj.name), me)
    for c in obj.users_collection:
        c.objects.link(new)
    for k in obj.keys():
        new[k] = obj[k]
    if "bind" in obj:
        new["bind"] = ",".join(mirror_name(b) for b in obj["bind"].split(","))
    if "grad" in obj:
        new["grad"] = obj["grad"]
    return new


def apply_modifiers(obj):
    dg = bpy.context.evaluated_depsgraph_get()
    ev = obj.evaluated_get(dg)
    me = bpy.data.meshes.new_from_object(ev, preserve_all_data_layers=True, depsgraph=dg)
    old = obj.data
    obj.modifiers.clear()
    obj.data = me
    bpy.data.meshes.remove(old)
    return obj


def subdivide(obj, levels=1, keep_sharp=False):
    mod = obj.modifiers.new("sub", "SUBSURF")
    mod.levels = levels
    mod.render_levels = levels
    mod.use_limit_surface = True
    if keep_sharp:
        mod.boundary_smooth = "PRESERVE_CORNERS"
    return apply_modifiers(obj)


def solidify(obj, thickness, offset=-1.0):
    mod = obj.modifiers.new("solid", "SOLIDIFY")
    mod.thickness = thickness
    mod.offset = offset
    mod.use_even_offset = True
    return apply_modifiers(obj)


def bevel(obj, width, segments=2, angle=35):
    mod = obj.modifiers.new("bevel", "BEVEL")
    mod.width = width
    mod.segments = segments
    mod.limit_method = "ANGLE"
    mod.angle_limit = math.radians(angle)
    mod.harden_normals = False
    return apply_modifiers(obj)


def smooth_shade(obj, sharp_angle=None):
    for p in obj.data.polygons:
        p.use_smooth = True
    if sharp_angle is not None:
        obj.data.set_sharp_from_angle(angle=math.radians(sharp_angle))
    return obj


def join(objs, name):
    objs = [o for o in objs if o is not None]
    base = objs[0]
    with bpy.context.temp_override(active_object=base, selected_editable_objects=objs, object=base):
        bpy.ops.object.join()
    base.name = name
    base.data.name = name
    return base


# ---------------------------------------------------------------- primitives

def _ring(bm, c, u, v, a, b, n, power=2.0, phase=0.0, wobble=None, arc=None):
    """Superellipse ring. arc=(start, end) radians for open shells."""
    verts = []
    count = n if arc is None else n + 1
    for i in range(count):
        if arc is None:
            t = phase + TAU * i / n
        else:
            t = arc[0] + (arc[1] - arc[0]) * i / n
        ct, st = math.cos(t), math.sin(t)
        e = 2.0 / power
        x = math.copysign(abs(ct) ** e, ct) * a
        y = math.copysign(abs(st) ** e, st) * b
        if wobble:
            k = wobble(t)
            x *= k
            y *= k
        verts.append(bm.verts.new(c + u * x + v * y))
    return verts


def loft(name, sections, n=16, cap0=True, cap1=True, pal="blue", bind=None, smooth=True,
         sharp=None, arc=None, power=2.0, col=None):
    """Lofts superellipse sections.

    Each section is a dict: c (center), a, b (radii along u, v), optional u, v axis vectors,
    p (superellipse power), w (callable t->scale), dz/dy offsets are baked into c.
    Default axes: u=X, v=Y (section lies in the horizontal plane, loft goes along Z).
    """
    n = seg(n, 5)
    bm = bmesh.new()
    rings = []
    for s in sections:
        u = V(s.get("u", (1, 0, 0))).normalized()
        v = V(s.get("v", (0, 1, 0))).normalized()
        rings.append(_ring(bm, V(s["c"]), u, v, s["a"], s["b"], n, s.get("p", power),
                           s.get("phase", 0.0), s.get("w"), arc))
    closed = arc is None
    for r0, r1 in zip(rings, rings[1:]):
        m = len(r0)
        for i in range(m if closed else m - 1):
            j = (i + 1) % m
            bm.faces.new((r0[i], r0[j], r1[j], r1[i]))
    if closed:
        for ring, flip, on in ((rings[0], True, cap0), (rings[-1], False, cap1)):
            if not on:
                continue
            ctr = sum((vv.co for vv in ring), V()) / len(ring)
            cv = bm.verts.new(ctr)
            for i in range(len(ring)):
                j = (i + 1) % len(ring)
                f = (ring[j], ring[i], cv) if flip else (ring[i], ring[j], cv)
                bm.faces.new(f)
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    return obj_from_bm(name, bm, pal, bind, smooth, sharp, col)


def lathe(name, profile, n=20, pal="steel", bind=None, arc=None, axis_center=(0, 0, 0), smooth=True,
          sharp=None, close_top=True, col=None, rx=1.0, ry=1.0):
    """Revolves profile [(r, z), ...] around Z. arc=(a0, a1) radians gives a partial revolve.
    rx/ry squash the revolve into an ellipse."""
    secs = [{"c": (axis_center[0], axis_center[1], axis_center[2] + z), "a": max(r, 1e-4) * rx,
             "b": max(r, 1e-4) * ry} for r, z in profile]
    cap0 = profile[0][0] > 1e-3
    cap1 = close_top and profile[-1][0] > 1e-3
    return loft(name, secs, n, cap0, cap1, pal, bind, smooth, sharp, arc, 2.0, col)


def ellipsoid(name, c, r, pal="skin", bind=None, n=16, rings=10, power=2.0, col=None):
    """Ellipsoid (superellipsoid when power != 2) built as a loft so it subdivides cleanly."""
    secs = []
    rings = seg(rings, 5)
    for i in range(1, rings):
        phi = math.pi * i / rings - math.pi / 2
        s, z = math.cos(phi), math.sin(phi)
        e = 2.0 / power
        s = abs(s) ** e
        z = math.copysign(abs(z) ** e, z)
        secs.append({"c": (c[0], c[1], c[2] + z * r[2]), "a": max(1e-4, s * r[0]), "b": max(1e-4, s * r[1]),
                     "p": power})
    obj = loft(name, secs, n, True, True, pal, bind, True, None, None, power, col)
    return obj


def box(name, size, c=(0, 0, 0), pal="leather", bind=None, bevel_w=0.0, seg=2, col=None):
    bm = bmesh.new()
    bmesh.ops.create_cube(bm, size=1.0)
    for v in bm.verts:
        v.co = V((v.co.x * size[0], v.co.y * size[1], v.co.z * size[2])) + V(c)
    obj = obj_from_bm(name, bm, pal, bind, smooth=True, col=col)
    if bevel_w > 0:
        bevel(obj, bevel_w, seg, 30)
    smooth_shade(obj, 50)
    return obj


def tube(name, path, radius, n=8, pal="gold", bind=None, closed=False, col=None, radii=None, power=2.0,
         up=(0, 0, 1)):
    """Sweeps a circle along a polyline (list of points). radii optional per point."""
    pts = [V(p) for p in path]
    secs = []
    m = len(pts)
    for i, p in enumerate(pts):
        if closed:
            t = (pts[(i + 1) % m] - pts[i - 1])
        else:
            t = (pts[min(i + 1, m - 1)] - pts[max(i - 1, 0)])
        t.normalize()
        ref = V(up) if abs(t.dot(V(up))) < 0.95 else V((1, 0, 0))
        u = t.cross(ref).normalized()
        v = t.cross(u).normalized()
        r = radii[i] if radii else radius
        if isinstance(r, (tuple, list)):
            a, b = r
        else:
            a = b = r
        secs.append({"c": p, "u": u, "v": v, "a": a, "b": b, "p": power})
    if closed:
        secs.append(dict(secs[0]))
    obj = loft(name, secs, n, not closed, not closed, pal, bind, True, None, None, power, col)
    if closed:
        bm = bmesh.new()
        bm.from_mesh(obj.data)
        bmesh.ops.remove_doubles(bm, verts=bm.verts, dist=1e-5)
        bm.to_mesh(obj.data)
        bm.free()
    return obj


def ring_path(c, a, b, n=24, z=0.0, arc=(0, TAU), axis="Z"):
    pts = []
    closed = abs(arc[1] - arc[0] - TAU) < 1e-6
    cnt = n if closed else n + 1
    for i in range(cnt):
        t = arc[0] + (arc[1] - arc[0]) * i / n
        if axis == "Z":
            pts.append((c[0] + math.cos(t) * a, c[1] + math.sin(t) * b, c[2] + z))
        elif axis == "Y":
            pts.append((c[0] + math.cos(t) * a, c[1] + z, c[2] + math.sin(t) * b))
        else:
            pts.append((c[0] + z, c[1] + math.cos(t) * a, c[2] + math.sin(t) * b))
    return pts


def extrude_poly(name, pts, depth, pal="steel", bind=None, plane="XZ", bevel_w=0.0, seg=2, center=0.5,
                 col=None, sharp=40, subdiv=False):
    """Extrudes a 2D polygon (counter-clockwise points) by depth.
    plane XZ: polygon in X (right) / Z (up), extruded along Y. plane XY: lies flat, extruded along Z.
    plane YZ: polygon in Y/Z, extruded along X."""
    bm = bmesh.new()

    def to3(p, d):
        if plane == "XZ":
            return V((p[0], d, p[1]))
        if plane == "XY":
            return V((p[0], p[1], d))
        return V((d, p[0], p[1]))

    d0 = -depth * center
    front = [bm.verts.new(to3(p, d0)) for p in pts]
    back = [bm.verts.new(to3(p, d0 + depth)) for p in pts]
    bm.faces.new(front)
    bm.faces.new(list(reversed(back)))
    k = len(pts)
    for i in range(k):
        j = (i + 1) % k
        bm.faces.new((front[i], front[j], back[j], back[i]))
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    # triangulate n-gons into something subdivision/bevel friendly
    ngons = [f for f in bm.faces if len(f.verts) > 4]
    bmesh.ops.triangulate(bm, faces=ngons, quad_method="BEAUTY", ngon_method="BEAUTY")
    obj = obj_from_bm(name, bm, pal, bind, smooth=True, col=col)
    if bevel_w > 0:
        bevel(obj, bevel_w, seg, 30)
    if subdiv:
        subdivide(obj, 1)
    smooth_shade(obj, sharp)
    return obj


def plate(name, sections_fn, nu, nv, thickness, pal="blue", bind=None, col=None, sharp=None, weld=False):
    """Open surface grid from fn(u, v) -> Vector with u,v in [0,1], then solidified.
    weld merges coincident vertices (closed loops in u, poles)."""
    bm = bmesh.new()
    grid = [[bm.verts.new(sections_fn(i / nu, j / nv)) for i in range(nu + 1)] for j in range(nv + 1)]
    for j in range(nv):
        for i in range(nu):
            q = (grid[j][i], grid[j][i + 1], grid[j + 1][i + 1], grid[j + 1][i])
            try:
                bm.faces.new(q)
            except ValueError:
                pass
    if weld:
        bmesh.ops.remove_doubles(bm, verts=bm.verts, dist=1e-5)
        bmesh.ops.dissolve_degenerate(bm, edges=bm.edges, dist=1e-6)
        bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    obj = obj_from_bm(name, bm, pal, bind, True, None, col)
    if thickness > 0:
        solidify(obj, thickness, 0.0)
    smooth_shade(obj, sharp)
    return obj


# ---------------------------------------------------------------- 2D shapes

def star4(r_out, r_in, n_extra=0, round_tips=0.0):
    """Four point star (gold emblem on shields/breastplates). CCW points."""
    pts = []
    for i in range(8):
        a = math.pi / 2 + i * math.pi / 4
        r = r_out if i % 2 == 0 else r_in
        pts.append((math.cos(a) * r, math.sin(a) * r))
    return list(reversed(pts)) if _signed_area(pts) < 0 else pts


def cross_pattee(r, arm_w, flare):
    """Cross with flared arms (shield emblem). CCW."""
    h = arm_w / 2
    out = []
    for k in range(4):
        a = k * math.pi / 2
        ca, sa = math.cos(a), math.sin(a)

        def rot(x, y):
            return (x * ca - y * sa, x * sa + y * ca)
        out += [rot(h, -h), rot(r, -flare), rot(r * 1.04, 0), rot(r, flare)]
    return out


def heater_shield(w, h, straight=0.38, top_curve=0.05, n=10):
    """Heater shield outline centered at origin (CCW): curved top, straight sides, pointed bottom."""
    top, tip = h / 2, -h / 2
    shoulder = top - (top - tip) * straight
    pts = []
    for i in range(n + 1):
        x = w / 2 - w * i / n
        pts.append((x, top + top_curve * (1 - (2 * x / w) ** 2)))
    pts.append((-w / 2, (top + shoulder) / 2))
    pts.append((-w / 2, shoulder))
    ctrl = (-w / 2, tip + (shoulder - tip) * 0.35)
    for i in range(1, n + 1):
        t = i / n
        x = (1 - t) ** 2 * (-w / 2) + 2 * (1 - t) * t * ctrl[0]
        y = (1 - t) ** 2 * shoulder + 2 * (1 - t) * t * ctrl[1] + t * t * tip
        pts.append((x, y))
    right = [(-x, y) for x, y in reversed(pts[n + 1:-1])]
    pts += right
    pts.append((w / 2, (top + shoulder) / 2))
    if _signed_area(pts) < 0:
        pts.reverse()
    return pts


def fleur_de_lis(size=1.0):
    """Fleur-de-lis outline (height ~ size, centered). CCW."""
    right = [(0.0, 0.56), (0.07, 0.44), (0.12, 0.3), (0.115, 0.17), (0.075, 0.07), (0.13, 0.11), (0.22, 0.2),
             (0.33, 0.23), (0.41, 0.17), (0.42, 0.06), (0.37, -0.03), (0.39, -0.12), (0.31, -0.07), (0.24, -0.02),
             (0.17, -0.02), (0.2, -0.05), (0.2, -0.11), (0.1, -0.11), (0.09, -0.2), (0.16, -0.28), (0.22, -0.4),
             (0.17, -0.46), (0.08, -0.38), (0.04, -0.44)]
    pts = [(x * size, y * size) for x, y in right]
    left = [(-x, y) for x, y in reversed(pts[1:])]
    out = pts + [(0.0, -0.4 * size)] + left
    if _signed_area(out) < 0:
        out.reverse()
    return out


def beast_emblem(size=1.0):
    """Red army emblem: horned, spiky beast head silhouette. CCW."""
    right = [(0.0, 0.3), (0.06, 0.18), (0.14, 0.26), (0.22, 0.5), (0.24, 0.24), (0.34, 0.14), (0.27, 0.06),
             (0.36, -0.06), (0.24, -0.08), (0.2, -0.24), (0.1, -0.2), (0.06, -0.42)]
    pts = [(x * size, y * size) for x, y in right]
    left = [(-x, y) for x, y in reversed(pts[1:])]
    out = pts + [(0.0, -0.32 * size)] + left
    if _signed_area(out) < 0:
        out.reverse()
    return out


def diamond(w, h):
    return [(0, h / 2), (-w / 2, 0), (0, -h / 2), (w / 2, 0)]


def offset_poly(pts, d):
    """Crude polygon offset (positive = grow) using vertex normals."""
    out = []
    k = len(pts)
    sign = 1 if _signed_area(pts) > 0 else -1
    for i in range(k):
        p0, p1, p2 = V((*pts[i - 1], 0)), V((*pts[i], 0)), V((*pts[(i + 1) % k], 0))
        e0 = (p1 - p0).normalized()
        e1 = (p2 - p1).normalized()
        n0 = V((e0.y, -e0.x, 0)) * sign
        n1 = V((e1.y, -e1.x, 0)) * sign
        nn = (n0 + n1)
        if nn.length < 1e-6:
            nn = n0
        nn.normalize()
        cosh = max(0.35, nn.dot(n0))
        q = p1 + nn * (d / cosh)
        out.append((q.x, q.y))
    return out


def _signed_area(pts):
    a = 0.0
    for i in range(len(pts)):
        x0, y0 = pts[i - 1]
        x1, y1 = pts[i]
        a += x0 * y1 - x1 * y0
    return a / 2
