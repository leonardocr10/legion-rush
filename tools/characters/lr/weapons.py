"""Weapons and held props. Built in a local frame: grip at the origin, main axis +Z (blade/shaft up),
width along X, thickness along Y (shields: face toward -Y, up +Z). place() moves them into the world."""
import math
from mathutils import Vector, Matrix

from . import mesh as M
from . import finalize

V = Vector
PI = math.pi


def place(obj, origin, axis_z, axis_x_hint):
    """Orients a weapon built in local space: +Z -> axis_z, +X -> closest to axis_x_hint."""
    z = V(axis_z).normalized()
    x = V(axis_x_hint)
    x = (x - z * x.dot(z)).normalized()
    y = z.cross(x)
    m = Matrix((x, y, z)).transposed().to_4x4()
    m.translation = V(origin)
    obj.data.transform(m)
    obj.data.update()
    return obj


def combine(objs, name):
    return finalize.join_into(objs, name)


def sword(name="Sword", length=0.44, width=0.068, grip=0.1, guard=0.17, gem="gem_blue", blade="steel",
          guard_pal="gold", grip_pal="leather_dark", pommel="gold", s=1.0, fuller=True):
    L, W = length * s, width * s
    g0 = 0.035 * s
    blade_pts = [(-W / 2, g0), (W / 2, g0), (W / 2 * 0.96, g0 + L * 0.75), (0.0, g0 + L), (-W / 2 * 0.96, g0 + L * 0.75)]
    bl = M.extrude_poly(name + "Blade", blade_pts, 0.014 * s, blade, None, "XZ", 0.0, 1)
    # sharpen the edges: squash thickness toward the edges
    M.deform(bl, lambda p: V((p.x, p.y * (1 - 0.75 * (abs(p.x) / (W / 2)) ** 1.5), p.z)))
    parts = [bl]
    if fuller:
        parts.append(M.box(name + "Fuller", (W * 0.16, 0.016 * s, L * 0.55), (0, 0, g0 + L * 0.36), "steel_dark"))
    gd = M.tube(name + "Guard", [(-guard * s / 2, 0, 0.012 * s), (-guard * s / 4, 0, 0.0), (0, 0, -0.004 * s),
                                 (guard * s / 4, 0, 0.0), (guard * s / 2, 0, 0.012 * s)], 0, 8, guard_pal,
                radii=[(0.016 * s, 0.014 * s), (0.016 * s, 0.018 * s), (0.026 * s, 0.022 * s), (0.016 * s, 0.018 * s),
                       (0.016 * s, 0.014 * s)])
    for sx in (-1, 1):
        parts.append(M.ellipsoid(name + f"GuardTip{sx}", (sx * guard * s / 2, 0, 0.016 * s),
                                 (0.018 * s, 0.018 * s, 0.022 * s), guard_pal, None, 10, 6))
    parts.append(gd)
    if gem:
        parts.append(M.ellipsoid(name + "Gem", (0, -0.016 * s, 0.002 * s), (0.016 * s, 0.01 * s, 0.02 * s), gem, None,
                                 10, 6))
        parts.append(M.ellipsoid(name + "Gem2", (0, 0.016 * s, 0.002 * s), (0.016 * s, 0.01 * s, 0.02 * s), gem, None,
                                 10, 6))
    gr = M.tube(name + "Grip", [(0, 0, -0.01 * s), (0, 0, -grip * s * 0.5), (0, 0, -grip * s)], 0, 8, grip_pal,
                radii=[0.017 * s, 0.019 * s, 0.017 * s])
    parts.append(gr)
    parts.append(M.ellipsoid(name + "Pommel", (0, 0, -grip * s - 0.014 * s), (0.024 * s, 0.024 * s, 0.026 * s),
                             pommel, None, 12, 8))
    obj = combine(parts, name)
    M.smooth_shade(obj, 35)
    return obj


def heater_shield(name="Shield", w=0.34, h=0.42, face="blue", rim="gold", emblem=None, emblem_pal="gold",
                  back="wood_dark", s=1.0, bulge=0.035, rim_r=0.016, boss=None):
    W, H = w * s, h * s
    outline = M.heater_shield(W, H)
    body = M.extrude_poly(name + "Body", outline, 0.028 * s, face, None, "XZ", 0.004, 2)
    M.set_faces_pal(body, back, lambda c, n: n.y > 0.5)
    parts = [body]
    path = [(x, -0.012 * s, z) for x, z in M.offset_poly(outline, -0.004 * s)]
    parts.append(M.tube(name + "Rim", path, rim_r * s, 8, rim, closed=True))
    if emblem:
        em = M.extrude_poly(name + "Emblem", emblem, 0.018 * s, emblem_pal, None, "XZ", 0.004, 2)
        M.xform(em, loc=(0, -0.02 * s, 0.02 * s))
        parts.append(em)
    if boss:
        parts.append(M.ellipsoid(name + "Boss", (0, -0.03 * s, 0.02 * s), (0.035 * s, 0.02 * s, 0.035 * s), boss, None,
                                 12, 8))
    # strap on the back
    parts.append(M.box(name + "Strap", (0.03 * s, 0.02 * s, 0.18 * s), (0, 0.03 * s, 0), "leather"))
    obj = combine(parts, name)
    # curve the shield (convex toward the front)
    M.deform(obj, lambda p: V((p.x, p.y + bulge * (2 * p.x / W) ** 2 * s, p.z)))
    M.smooth_shade(obj, 40)
    return obj


def spear(name="Spear", length=1.25, head_len=0.2, shaft="wood", head="steel", collar="gold", s=1.0, tassel=None,
          stripes=None, butt=0.12, head_w=0.07):
    """Grip at origin; shaft runs from -butt*length to the tip (+Z)."""
    L = length * s
    z0 = -butt * L
    z1 = L - butt * L - head_len * s
    parts = [M.tube(name + "Shaft", [(0, 0, z0), (0, 0, (z0 + z1) / 2), (0, 0, z1)], 0.016 * s, 8, shaft)]
    if stripes:
        n = 6
        for i in range(n):
            za = z0 + (z1 - z0) * (i + 0.2) / n
            parts.append(M.tube(name + f"Stripe{i}", [(0, 0, za), (0, 0, za + 0.035 * s)], 0.018 * s, 8, stripes))
    hw = head_w * s
    hl = head_len * s
    pts = [(-hw * 0.18, 0), (hw * 0.18, 0), (hw * 0.5, hl * 0.28), (hw * 0.32, hl * 0.6), (0, hl), (-hw * 0.32, hl * 0.6),
           (-hw * 0.5, hl * 0.28)]
    hd = M.extrude_poly(name + "Head", pts, 0.022 * s, head, None, "XZ", 0.0, 1)
    M.deform(hd, lambda p: V((p.x, p.y * (1 - 0.7 * min(1.0, abs(p.x) / (hw * 0.5))), p.z)))
    M.xform(hd, loc=(0, 0, z1))
    parts.append(hd)
    hd2 = M.extrude_poly(name + "Head2", pts, 0.022 * s, head, None, "XZ", 0.0, 1)
    M.deform(hd2, lambda p: V((p.x, p.y * (1 - 0.7 * min(1.0, abs(p.x) / (hw * 0.5))), p.z)))
    M.xform(hd2, rot=(0, 0, 90))
    M.xform(hd2, loc=(0, 0, z1))
    M.xform(hd2, scale=(0.55, 1, 0.92), pivot=(0, 0, z1))
    parts.append(hd2)
    if collar:
        parts.append(M.lathe(name + "Collar", [(0.02 * s, -0.04 * s), (0.026 * s, -0.02 * s), (0.024 * s, 0.0),
                                               (0.016 * s, 0.012 * s)], 10, collar, axis_center=(0, 0, z1)))
        parts.append(M.lathe(name + "Butt", [(0.016 * s, 0.0), (0.022 * s, -0.02 * s), (0.001, -0.05 * s)], 10, collar,
                             axis_center=(0, 0, z0)))
    if tassel:
        parts.append(M.tube(name + "Tassel", [(0, -0.02 * s, z1 - 0.03 * s), (0.01 * s, -0.03 * s, z1 - 0.1 * s),
                                              (0.015 * s, -0.035 * s, z1 - 0.16 * s)], 0, 8, tassel,
                            radii=[0.014 * s, 0.022 * s, 0.006 * s]))
    obj = combine(parts, name)
    M.smooth_shade(obj, 35)
    return obj


def bow(name="Bow", h=0.62, depth=0.12, wood="gold", grip="leather_dark", string="cream", s=1.0, gem=None,
        tips="gold", recurve=0.035):
    """Bow in the XZ plane? No: limbs along Z, belly bulging toward +Y (away from the archer), string at y=0."""
    H = h * s
    pts = []
    radii = []
    n = 12
    for i in range(n + 1):
        t = i / n * 2 - 1  # -1 bottom .. 1 top
        z = t * H / 2
        y = depth * s * (1 - t * t) - recurve * s * max(0.0, abs(t) - 0.75) * 4
        pts.append((0, -y, z))
        radii.append((0.012 * s + 0.01 * s * (1 - abs(t)), 0.016 * s + 0.01 * s * (1 - abs(t))))
    limb = M.tube(name + "Limb", pts, 0, 8, wood, radii=radii)
    parts = [limb]
    parts.append(M.tube(name + "Grip", [(0, -depth * s, -0.05 * s), (0, -depth * s, 0.05 * s)], 0.026 * s, 10, grip))
    tip_y = -(depth * s * 0 - recurve * s)
    st = M.tube(name + "String", [(0, pts[0][1], pts[0][2]), (0, 0.0, 0.0), (0, pts[-1][1], pts[-1][2])], 0.003 * s, 4,
                string)
    parts.append(st)
    if gem:
        parts.append(M.ellipsoid(name + "Gem", (0, -depth * s - 0.02 * s, 0.07 * s), (0.02 * s, 0.014 * s, 0.03 * s),
                                 gem, None, 10, 6))
        parts.append(M.ellipsoid(name + "Gem2", (0, -depth * s - 0.02 * s, -0.07 * s), (0.02 * s, 0.014 * s, 0.03 * s),
                                 gem, None, 10, 6))
    for sz in (-1, 1):
        parts.append(M.ellipsoid(name + f"Tip{sz}", pts[0 if sz < 0 else -1], (0.016 * s, 0.016 * s, 0.022 * s), tips,
                                 None, 8, 6))
    obj = combine(parts, name)
    # shift so the grip is at the origin
    M.xform(obj, loc=(0, depth * s, 0))
    M.smooth_shade(obj, 40)
    return obj


def arrow(name="Arrow", length=0.36, shaft="wood", head="steel", fletch="blue", s=1.0, glow=None):
    L = length * s
    parts = [M.tube(name + "Shaft", [(0, 0, 0), (0, 0, L)], 0.0055 * s, 6, shaft)]
    hd = M.lathe(name + "Head", [(0.012 * s, 0.0), (0.004 * s, 0.03 * s), (0.001, 0.045 * s)], 6, head,
                 axis_center=(0, 0, L))
    M.xform(hd, scale=(1, 0.4, 1), pivot=(0, 0, L))
    parts.append(hd)
    for k in range(3):
        f = M.extrude_poly(name + f"Fletch{k}", [(0, 0), (0.022 * s, 0.01 * s), (0.02 * s, 0.07 * s), (0, 0.075 * s)],
                           0.003 * s, fletch, None, "XZ")
        M.xform(f, loc=(0, 0, 0.012 * s), rot=(0, 0, k * 120))
        parts.append(f)
    if glow:
        parts.append(M.ellipsoid(name + "Glow", (0, 0, L + 0.02 * s), (0.012 * s, 0.012 * s, 0.03 * s), glow, None, 8, 6))
    obj = combine(parts, name)
    M.smooth_shade(obj, 40)
    return obj


def quiver(name="Quiver", h=0.3, r=0.055, body="blue", trim="gold", strap="leather", arrows=5, fletch="blue",
           s=1.0, arrow_head="steel"):
    """Upright quiver, bottom at origin."""
    H = h * s
    parts = [M.lathe(name + "Body", [(r * s * 0.85, 0.0), (r * s, 0.04 * s), (r * s * 1.02, H * 0.6), (r * s * 1.1, H),
                                     (r * s * 1.0, H + 0.004)], 14, body, close_top=False, rx=1.0, ry=0.8)]
    parts[0].data.polygons[0].use_smooth = True
    for zz in (0.03 * s, H * 0.5, H - 0.012 * s):
        k = 0.88 + 0.22 * zz / H
        parts.append(M.tube(name + f"Band{zz:.2f}", M.ring_path((0, 0, zz), r * s * k * 1.04, r * s * k * 0.84, 16),
                            0.008 * s, 6, trim, closed=True))
    for i in range(arrows):
        a = i / max(1, arrows) * 2 * PI
        ox, oy = math.cos(a) * r * s * 0.45, math.sin(a) * r * s * 0.35
        top = H + 0.07 * s + 0.02 * s * (i % 2)
        parts.append(M.tube(name + f"Shaft{i}", [(ox, oy, H * 0.4), (ox, oy, top)], 0.0055 * s, 5, "wood"))
        for k in range(3):
            f = M.extrude_poly(name + f"F{i}{k}", [(0, 0), (0.02 * s, 0.01 * s), (0.018 * s, 0.06 * s), (0, 0.065 * s)],
                               0.003 * s, fletch, None, "XZ")
            M.xform(f, loc=(ox, oy, top - 0.066 * s), rot=(0, 0, k * 120 + i * 30))
            parts.append(f)
    obj = combine(parts, name)
    M.smooth_shade(obj, 40)
    return obj


def staff(name="Staff", length=1.2, shaft="wood_dark", metal="gold", crystal="glow_blue", s=1.0, crown="ring",
          butt=0.35, crystal_size=1.0):
    """Grip at origin, head on +Z."""
    L = length * s
    z0 = -butt * L
    z1 = L - butt * L
    parts = [M.tube(name + "Shaft", [(0, 0, z0), (0, 0, z1 * 0.5), (0, 0, z1)], 0.017 * s, 8, shaft,
                    radii=[0.015 * s, 0.017 * s, 0.019 * s])]
    for zz in (z1 * 0.35, z1 * 0.7, z0 * 0.5):
        parts.append(M.tube(name + f"Band{zz:.2f}", [(0, 0, zz - 0.012 * s), (0, 0, zz + 0.012 * s)], 0.022 * s, 8,
                            metal))
    parts.append(M.lathe(name + "Foot", [(0.018 * s, 0.0), (0.024 * s, -0.025 * s), (0.001, -0.07 * s)], 8, metal,
                         axis_center=(0, 0, z0)))
    cs = crystal_size * s
    head_z = z1 + 0.11 * s
    parts.append(M.lathe(name + "Cup", [(0.02 * s, -0.02 * s), (0.04 * s, 0.02 * s), (0.03 * s, 0.045 * s)], 10, metal,
                         axis_center=(0, 0, z1)))
    if crown == "ring":
        ring = M.tube(name + "Ring", M.ring_path((0, 0, 0), 0.085 * cs, 0.1 * cs, 22, axis="Y"), 0.013 * s, 8, metal,
                      closed=True)
        M.xform(ring, loc=(0, 0, head_z))
        parts.append(ring)
        for sx in (-1, 1):
            parts.append(M.lathe(name + f"Spike{sx}", [(0.012 * s, 0), (0.001, 0.05 * s)], 6, metal,
                                 axis_center=(0, 0, 0)))
            M.xform(parts[-1], loc=(sx * 0.09 * cs, 0, head_z + 0.06 * cs), rot=(0, -sx * 35, 0))
        parts.append(M.lathe(name + "Top", [(0.014 * s, 0), (0.001, 0.06 * s)], 6, metal))
        M.xform(parts[-1], loc=(0, 0, head_z + 0.1 * cs))
    elif crown == "claw":
        for k in range(3):
            a = k * 2 * PI / 3
            pts = [(math.cos(a) * 0.03 * cs, math.sin(a) * 0.03 * cs, z1 + 0.03 * s),
                   (math.cos(a) * 0.075 * cs, math.sin(a) * 0.075 * cs, head_z),
                   (math.cos(a) * 0.04 * cs, math.sin(a) * 0.04 * cs, head_z + 0.1 * cs)]
            parts.append(M.tube(name + f"Claw{k}", pts, 0, 6, metal, radii=[0.012 * s, 0.01 * s, 0.003 * s]))
    cr = M.ellipsoid(name + "Crystal", (0, 0, head_z), (0.042 * cs, 0.042 * cs, 0.07 * cs), crystal, None, 6, 6, 1.2)
    parts.append(cr)
    obj = combine(parts, name)
    M.smooth_shade(obj, 35)
    obj["tip"] = head_z
    return obj


def axe(name="Axe", length=0.62, head_w=0.26, head_h=0.3, shaft="wood_dark", blade="steel", metal="iron", s=1.0,
        double=True, ring="gold"):
    """Grip at origin, head on +Z."""
    L = length * s
    z1 = L * 0.72
    z0 = -L * 0.28
    parts = [M.tube(name + "Shaft", [(0, 0, z0), (0, 0, z1 * 0.5), (0, 0, z1 + 0.04 * s)], 0.02 * s, 8, shaft,
                    radii=[0.02 * s, 0.021 * s, 0.022 * s])]
    for zz in (z1 * 0.2, -0.05 * s):
        parts.append(M.tube(name + f"Wrap{zz:.2f}", [(0, 0, zz - 0.03 * s), (0, 0, zz + 0.03 * s)], 0.024 * s, 8,
                            "leather_dark"))
    parts.append(M.box(name + "Socket", (0.06 * s, 0.05 * s, 0.11 * s), (0, 0, z1), metal, None, 0.008))
    hw, hh = head_w * s, head_h * s
    sides = (-1, 1) if double else (1,)
    for sx in sides:
        pts = [(0.025 * s, -hh * 0.22), (hw * 0.45, -hh * 0.42), (hw * 0.55, -hh * 0.2), (hw * 0.58, 0.0),
               (hw * 0.55, hh * 0.2), (hw * 0.45, hh * 0.42), (0.025 * s, hh * 0.22)]
        pts = [(x * sx, y) for x, y in pts]
        if sx < 0:
            pts.reverse()
        b = M.extrude_poly(name + f"Blade{sx}", pts, 0.032 * s, blade, None, "XZ", 0.003, 1)
        M.deform(b, lambda p, sx=sx: V((p.x, p.y * (1 - 0.8 * max(0.0, (abs(p.x) / (hw * 0.58)) - 0.5) * 2), p.z)))
        M.xform(b, loc=(0, 0, z1))
        parts.append(b)
    parts.append(M.lathe(name + "Ring", [(0.022 * s, -0.02 * s), (0.03 * s, 0.0), (0.022 * s, 0.02 * s)], 10, ring,
                         axis_center=(0, 0, z0)))
    parts.append(M.lathe(name + "Top", [(0.02 * s, 0.0), (0.001, 0.05 * s)], 8, metal, axis_center=(0, 0, z1 + 0.05 * s)))
    obj = combine(parts, name)
    M.smooth_shade(obj, 35)
    return obj


def book(name="Book", w=0.16, h=0.2, d=0.05, cover="blue", trim="gold", emblem=None, s=1.0):
    parts = [M.box(name + "Cover", (w * s, d * s, h * s), (0, 0, 0), cover, None, 0.006),
             M.box(name + "Pages", (w * s * 0.92, d * s * 0.7, h * s * 0.94), (0.008 * s, 0, 0), "cream", None, 0.002)]
    for zz in (-h * s * 0.42, h * s * 0.42):
        parts.append(M.box(name + f"Corner{zz:.2f}", (w * s * 1.02, d * s * 1.04, 0.022 * s), (0, 0, zz), trim, None,
                           0.003))
    if emblem:
        em = M.extrude_poly(name + "Emblem", emblem, 0.006 * s, trim, None, "XZ")
        M.xform(em, loc=(0, -d * s / 2 - 0.003, 0))
        parts.append(em)
    obj = combine(parts, name)
    M.smooth_shade(obj, 40)
    return obj
