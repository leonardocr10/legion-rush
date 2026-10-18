"""Reusable armor and clothing pieces built around a Proportions instance."""
import math
from mathutils import Vector

from . import mesh as M

V = Vector
PI = math.pi


def breastplate(P, pal="steel", trim="gold", z0=0.6, z1=0.8, grow=0.014, emblem=None, emblem_pal="gold",
                back=False, name="Breastplate", chest=1.0):
    s, w = P.s, P.w
    L = P.lift
    torso_secs = [(0.58, 0.162, 0.122), (0.66, 0.185 * chest, 0.134 * chest), (0.735, 0.2 * chest, 0.138 * chest),
                  (0.79, 0.178 * chest, 0.12), (0.825, 0.11, 0.085)]

    def radius_at(z):
        for (za, aa, ba), (zb, ab, bb) in zip(torso_secs, torso_secs[1:]):
            if za <= z <= zb:
                t = (z - za) / (zb - za)
                return aa + (ab - aa) * t, ba + (bb - ba) * t
        return torso_secs[-1][1:] if z > torso_secs[-1][0] else torso_secs[0][1:]
    secs = []
    steps = 6
    for i in range(steps + 1):
        z = z0 + (z1 - z0) * i / steps
        a, b = radius_at(z)
        secs.append({"c": (0, 0.004 * s, z * s + L), "a": a * w + grow, "b": b * s + grow * 1.2, "p": 2.3})
    arc = (PI * 1.0 - 0.15, PI * 2.0 + 0.15) if not back else (-0.15, PI + 0.15)
    plate = M.loft(name, secs, 20, False, False, pal, "Spine,Chest", arc=arc)
    M.solidify(plate, 0.012 * s, 1.0)
    M.smooth_shade(plate, 60)
    parts = [plate]
    if trim:
        for z, nm in ((z1, "Top"), (z0, "Bottom")):
            a, b = radius_at(z)
            pts = M.ring_path((0, 0.004 * s, z * s + L), a * w + grow + 0.006, b * s + grow * 1.2 + 0.006, 20,
                              arc=(arc[0] + 0.05, arc[1] - 0.05))
            parts.append(M.tube(name + "Trim" + nm, pts, 0.009 * s, 8, trim, "Spine,Chest"))
    if emblem:
        zc = (z0 + z1) * 0.5 * s + L + 0.01
        a, b = radius_at((z0 + z1) * 0.5)
        y = -(b * s + grow * 1.2) - 0.008
        em = M.extrude_poly(name + "Emblem", emblem, 0.014 * s, emblem_pal, "Chest", "XZ", 0.003, 2)
        M.xform(em, loc=(0, y, zc))
        parts.append(em)
    return parts


def pauldron(P, side=1, pal="blue", trim="gold", size=1.0, layers=2, name=None, spikes=0, spike_pal="steel",
             tilt=28):
    s = P.s * size
    sd = "Left" if side > 0 else "Right"
    nm = name or ("Pauldron" + sd[0])
    sh = V((P.shoulder.x * side, P.shoulder.y, P.shoulder.z))
    parts = []
    for i in range(layers):
        r = (0.09 - 0.012 * i) * s
        drop = 0.035 * i * s
        prof = [(r * 1.02, -0.035 * s), (r, 0.0), (r * 0.92, 0.03 * s), (r * 0.7, 0.06 * s), (r * 0.35, 0.078 * s),
                (0.001, 0.082 * s)]
        dome = M.lathe(f"{nm}{i}", prof, 18, pal, f"{sd}UpperArm", rx=1.0, ry=1.12)
        M.solidify(dome, 0.01 * s, 1.0)
        ring = M.tube(f"{nm}{i}Rim", M.ring_path((0, 0, -0.035 * s), r * 1.04, r * 1.12 * 1.04, 20), 0.009 * s, 8,
                      trim, f"{sd}UpperArm", closed=True)
        for o in (dome, ring):
            M.xform(o, loc=sh + V((0.02 * side * s, 0.0, 0.03 * s - drop)), rot=(0, -tilt * side - 14 * i * side, 0))
        parts += [dome, ring]
    for k in range(spikes):
        sp = M.lathe(f"{nm}Spike{k}", [(0.018 * s, 0), (0.012 * s, 0.03 * s), (0.001, 0.06 * s)], 8, spike_pal,
                     f"{sd}UpperArm")
        ang = (k - (spikes - 1) / 2) * 0.5
        M.xform(sp, loc=sh + V((0.05 * side * s, math.sin(ang) * 0.05 * s, 0.1 * s)),
                rot=(math.degrees(ang) * -0.8, -40 * side, 0))
        parts.append(sp)
    return parts


def belt(P, z=0.565, pal="leather", buckle="gold", height=0.045, grow=0.012, name="Belt", buckle_size=1.0):
    s, w = P.s, P.w
    L = P.lift
    a, b = 0.168 * w + grow, 0.126 * s + grow
    zc = z * s + L
    band = M.loft(name, [{"c": (0, 0.004 * s, zc - height * s / 2), "a": a, "b": b, "p": 2.3},
                         {"c": (0, 0.004 * s, zc + height * s / 2), "a": a, "b": b, "p": 2.3}], 22, False, False, pal,
                  "Hips,Spine")
    M.solidify(band, 0.01 * s, 1.0)
    parts = [band]
    if buckle:
        bs = buckle_size
        outer = M.box(name + "Buckle", (0.075 * s * bs, 0.016 * s, 0.062 * s * bs), (0, -b - 0.006, zc), buckle,
                      "Hips,Spine", 0.004)
        inner = M.box(name + "BuckleIn", (0.042 * s * bs, 0.012 * s, 0.03 * s * bs), (0, -b - 0.012, zc), pal,
                      "Hips,Spine", 0.002)
        prong = M.box(name + "Prong", (0.008 * s, 0.01 * s, 0.034 * s * bs), (0, -b - 0.016, zc), buckle, "Hips,Spine")
        parts += [outer, inner, prong]
    return parts


def skirt(P, z_top=0.56, z_bot=0.36, pal="blue", hem="gold", flare=1.3, name="Skirt", split_front=False,
          bind="Hips,LeftUpperLeg,RightUpperLeg", top_a=0.17, top_b=0.128):
    s, w = P.s, P.w
    L = P.lift
    secs = []
    steps = 5
    for i in range(steps + 1):
        t = i / steps
        z = (z_top + (z_bot - z_top) * t) * s + L
        k = 1 + (flare - 1) * t ** 1.4
        secs.append({"c": (0, 0.006 * s + 0.01 * t * s, z), "a": top_a * w * k, "b": top_b * s * k, "p": 2.2})
    arc = (PI * 1.5 + 0.12, PI * 1.5 + PI * 2 - 0.12) if split_front else None
    sk = M.loft(name, secs, 24, False, False, pal, bind, arc=arc)
    M.solidify(sk, 0.012 * s, 1.0)
    parts = [sk]
    if hem:
        k = flare
        pts = M.ring_path((0, 0.016 * s, z_bot * s + L), top_a * w * k + 0.004, top_b * s * k + 0.004, 28,
                          arc=arc or (0, 2 * PI))
        parts.append(M.tube(name + "Hem", pts, 0.011 * s, 8, hem, bind, closed=arc is None))
    return parts


def tabard(P, z_top=0.56, z_bot=0.32, width=0.1, pal="blue", trim="gold", front=True, name="Tabard", emblem=None,
           emblem_pal="gold", bind="Hips,LeftUpperLeg,RightUpperLeg", point=True):
    """Hanging cloth panel in front (or back) of the hips."""
    s = P.s
    L = P.lift
    sign = -1 if front else 1
    y0 = sign * 0.15 * s

    def f(u, v):
        x = (u - 0.5) * 2 * width * s * (1 + 0.15 * v)
        z = (z_top + (z_bot - z_top) * v) * s + L
        if point:
            z -= 0.05 * s * v * (1 - abs(u - 0.5) * 2) ** 1.0
        y = y0 + sign * (0.02 * s * v + 0.012 * s * math.cos((u - 0.5) * PI))
        return V((x, y, z))
    pan = M.plate(name, f, 6, 6, 0.01 * s, pal, bind)
    parts = [pan]
    if trim:
        pts = [f(u / 8, 1.0) for u in range(9)]
        pts = [V((p.x, p.y + sign * 0.004, p.z - 0.004)) for p in pts]
        parts.append(M.tube(name + "Trim", pts, 0.008 * s, 6, trim, bind))
        for u in (0.0, 1.0):
            side = [f(u, v / 6) for v in range(7)]
            parts.append(M.tube(name + f"Edge{int(u)}", [V((p.x, p.y + sign * 0.004, p.z)) for p in side], 0.007 * s, 6,
                                trim, bind))
    if emblem:
        em = M.extrude_poly(name + "Emblem", emblem, 0.008 * s, emblem_pal, bind.split(",")[0], "XZ", 0.002, 1)
        c = f(0.5, 0.42)
        M.xform(em, loc=(0, c.y + sign * 0.008, c.z))
        if not front:
            M.xform(em, rot=(0, 0, 180), pivot=(0, c.y + sign * 0.008, c.z))
        parts.append(em)
    return parts


def knee_guard(P, side=1, pal="steel", trim="gold", size=1.0, name=None):
    s = P.s * size
    sd = "Left" if side > 0 else "Right"
    k = V((P.knee.x * side, P.knee.y, P.knee.z))
    cap = M.ellipsoid(name or ("Knee" + sd[0]), (0, 0, 0), (0.058 * s, 0.03 * s, 0.062 * s), pal, f"{sd}LowerLeg", 14, 8)
    M.xform(cap, loc=k + V((0, -0.05 * s, 0.0)))
    rim = M.tube((name or ("Knee" + sd[0])) + "Rim", M.ring_path((0, 0, 0), 0.054 * s, 0.058 * s, 18, axis="Y"),
                 0.007 * s, 6, trim, f"{sd}LowerLeg", closed=True)
    M.xform(rim, loc=k + V((0, -0.062 * s, 0.0)))
    return [cap, rim]


def vambrace(P, side=1, pal="steel", trim="gold", length=0.11, r=0.058, name=None):
    s = P.s
    sd = "Left" if side > 0 else "Right"
    a = V((P.elbow.x * side, P.elbow.y, P.elbow.z))
    b = V((P.wrist.x * side, P.wrist.y, P.wrist.z))
    d = (b - a).normalized()
    p1 = b - d * 0.012 * s
    p0 = p1 - d * length * s
    nm = name or ("Vambrace" + sd[0])
    body = M.tube(nm, [p0, p0.lerp(p1, 0.5), p1], 0, 14, pal, f"{sd}LowerArm", radii=[r * s * 0.96, r * s, r * s * 1.08])
    parts = [body]
    if trim:
        for p, rr in ((p0, r * 0.98), (p1, r * 1.1)):
            u = d.orthogonal().normalized()
            v = d.cross(u)
            pts = [p + (u * math.cos(t) + v * math.sin(t)) * rr * s for t in [i * math.tau / 16 for i in range(16)]]
            parts.append(M.tube(nm + "Rim", pts, 0.007 * s, 6, trim, f"{sd}LowerArm", closed=True))
    return parts


def cape(P, pal="blue", trim="gold", inner=None, length=0.62, width=0.24, flare=1.5, name="Cape", collar=None,
         collar_pal="fur", emblem=None, emblem_pal="gold", folds=3, z_top=None):
    """Cape hanging from the shoulders, skinned to Cape1..3 (+Chest at the top)."""
    s, w = P.s, P.w
    zt = (z_top or (P.shoulder.z + 0.03))

    def f(u, v):
        ang = (u - 0.5) * PI * 0.95
        wid = width * w * (1 + (flare - 1) * v ** 1.2)
        x = math.sin(ang) * wid
        y = 0.1 * w + math.cos(ang) * 0.06 * w * (1 - v * 0.4) + 0.06 * s * v + 0.012 * s * math.sin(
            u * folds * 2 * PI) * v
        z = zt - length * s * v
        return V((x, y, z))
    cp = M.plate(name, f, 14, 10, 0.012 * s, pal, "Chest,Cape1,Cape2,Cape3")
    if inner:
        M.set_faces_pal(cp, inner, lambda c, n: n.y < -0.3)
    parts = [cp]
    if trim:
        pts = [f(u / 16, 1.0) + V((0, 0.006, 0.0)) for u in range(17)]
        parts.append(M.tube(name + "Hem", pts, 0.012 * s, 6, trim, "Cape3,Cape2"))
        for u in (0.0, 1.0):
            pts = [f(u, v / 10) + V((0, 0.004, 0)) for v in range(11)]
            parts.append(M.tube(name + f"Side{int(u)}", pts, 0.009 * s, 6, trim, "Chest,Cape1,Cape2,Cape3"))
    if collar:
        pts = M.ring_path((0, 0.02 * s, zt - 0.005), 0.15 * w, 0.115 * s, 20, arc=(-0.25, PI + 0.25))
        parts.append(M.tube(name + "Collar", pts, 0.042 * s * collar, 10, collar_pal, "Chest",
                            radii=[0.03 * s * collar + 0.016 * s * collar * math.sin(i / 20 * PI) for i in range(21)]))
    if emblem:
        em = M.extrude_poly(name + "Emblem", emblem, 0.008 * s, emblem_pal, "Cape2", "XZ", 0.002, 1)
        c = f(0.5, 0.45)
        M.xform(em, rot=(0, 0, 180))
        M.xform(em, loc=(0, c.y + 0.012, c.z))
        parts.append(em)
    return parts


def helmet_dome(P, pal="steel", trim="gold", grow=0.016, brow_z=0.04, cheek=True, neck=True, name="Helmet",
                ridge=True, cheek_depth=0.105, rx=1.0):
    hs = P.hs
    c, r = P.head_c, P.head_r
    z0 = c.z + brow_z * hs
    parts = []
    prof = []
    steps = 8
    for i in range(steps + 1):
        t = i / steps
        z = z0 + (c.z + r.z + grow - z0) * t
        k = max(0.0, 1 - ((z - c.z) / (r.z + grow)) ** 2)
        prof.append((math.sqrt(k) * (r.x + grow) + 0.002, z - c.z))
    prof[-1] = (0.001, prof[-1][1])
    dome = M.lathe(name, prof, 22, pal, "Head", axis_center=(c.x, c.y, c.z), rx=rx, ry=(r.y + grow) / (r.x + grow))
    parts.append(dome)
    band_r = prof[0][0]
    parts.append(M.tube(name + "Band", M.ring_path((c.x, c.y, z0), band_r * rx + 0.004,
                                                   band_r * (r.y + grow) / (r.x + grow) + 0.004, 24),
                        0.011 * hs, 8, trim, "Head", closed=True))
    if ridge:
        pts = M.ring_path((c.x, c.y, c.z), r.y + grow + 0.004, r.z + grow + 0.004, 16, arc=(-PI * 0.95, -0.05),
                          axis="X")
        pts = [(p[0], p[1], p[2]) for p in pts]
        # ring_path axis X: (y=cos*a, z=sin*b) -> want arc over the top from front (-y) to back (+y)
        pts = [(0.0, c.y + math.cos(t) * (r.y + grow + 0.005), c.z + math.sin(t) * (r.z + grow + 0.005) * 0.98)
               for t in [PI * 0.95 - PI * 0.85 * i / 14 for i in range(15)]]
        pts = [(x, c.y - (y - c.y), z) for x, y, z in pts]
        parts.append(M.tube(name + "Ridge", pts, 0.012 * hs, 8, trim, "Head", radii=None))
    if neck:
        secs = []
        for i in range(4):
            t = i / 3
            z = z0 - (0.13 * hs) * t
            secs.append({"c": (c.x, c.y + 0.01 * t, z), "a": band_r * rx * (1 + 0.08 * t), "b": band_r * (r.y + grow) /
                         (r.x + grow) * (1 + 0.1 * t), "p": 2.2})
        nk = M.loft(name + "Neck", secs, 18, False, False, pal, "Head", arc=(0.35, PI - 0.35))
        M.solidify(nk, 0.01 * hs, 1.0)
        parts.append(nk)
        bot = M.ring_path((c.x, c.y + 0.01, z0 - 0.13 * hs), band_r * rx * 1.08 + 0.002,
                          band_r * (r.y + grow) / (r.x + grow) * 1.1 + 0.002, 16, arc=(0.35, PI - 0.35))
        parts.append(M.tube(name + "NeckRim", bot, 0.008 * hs, 6, trim, "Head"))
    if cheek:
        for side in (1, -1):
            secs = []
            for i in range(4):
                t = i / 3
                z = z0 - cheek_depth * hs * t
                secs.append({"c": (c.x, c.y, z), "a": (band_r * rx) * (1 - 0.06 * t), "b": band_r * (r.y + grow) /
                             (r.x + grow) * (1 - 0.1 * t), "p": 2.2})
            a0 = -0.95 if side > 0 else PI - 0.2
            a1 = 0.2 if side > 0 else PI + 0.95
            ck = M.loft(name + "Cheek" + ("L" if side > 0 else "R"), secs, 8, False, False, pal, "Head", arc=(a0, a1))
            M.solidify(ck, 0.01 * hs, 1.0)
            parts.append(ck)
    return parts


def plume(P, pal="blue_plume", holder="gold", height=0.14, length=0.22, thick=0.05, name="Plume", z_off=0.0):
    hs = P.hs
    c, r = P.head_c, P.head_r
    top = c.z + r.z + 0.016 + z_off
    pts = [(0, c.y - 0.01, top - 0.01), (0, c.y + 0.0, top + height * 0.55 * hs),
           (0, c.y + 0.05 * hs, top + height * hs), (0, c.y + 0.12 * hs, top + height * 1.02 * hs),
           (0, c.y + 0.19 * hs, top + height * 0.7 * hs), (0, c.y + length * hs, top + height * 0.2 * hs)]
    radii = [(thick * 0.45 * hs, thick * 0.5 * hs), (thick * 0.6 * hs, thick * 0.9 * hs), (thick * 0.66 * hs, thick * hs),
             (thick * 0.6 * hs, thick * 0.95 * hs), (thick * 0.45 * hs, thick * 0.7 * hs), (thick * 0.1 * hs, thick * 0.2 * hs)]
    pl = M.tube(name, pts, 0, 12, pal, "Head", radii=radii, up=(1, 0, 0))
    M.subdivide(pl, 1)
    parts = [pl]
    if holder:
        h = M.lathe(name + "Holder", [(0.024 * hs, -0.01), (0.03 * hs, 0.012 * hs), (0.022 * hs, 0.03 * hs)], 12,
                    holder, "Head", axis_center=(0, c.y - 0.005, top - 0.012))
        parts.append(h)
    return parts


def hood(P, pal="blue", trim="gold", inner="blue_dark", grow=0.03, point=0.08, back_drop=0.18, name="Hood",
         open_front=0.62, tip_back=0.06):
    """Hood around the head with a face opening and a soft point at the back."""
    hs = P.hs
    c, r = P.head_c, P.head_r
    nu, nv = 18, 12
    opening = PI * open_front  # angular half-width of the face opening

    def f(u, v):
        # u around (0 = front center, wraps), v from bottom (neck) to top
        ang = -PI / 2 + opening / 2 + (2 * PI - opening) * u
        phi = -PI * 0.38 + PI * 0.88 * v
        k = math.cos(phi)
        x = math.cos(ang) * (r.x + grow) * k * (1 + 0.15 * max(0.0, -math.sin(phi)))
        y = c.y + math.sin(ang) * (r.y + grow) * k * 1.05
        z = c.z + math.sin(phi) * (r.z + grow) + 0.01
        # pointed tip at the back top
        back = max(0.0, math.sin(ang))
        z += point * hs * (v ** 3) * back
        y += tip_back * hs * (v ** 2) * back
        # drape down onto the shoulders at the bottom
        if v < 0.3:
            z -= back_drop * hs * (0.3 - v) * 2.0
            x *= 1 + 0.6 * (0.3 - v)
            y += 0.05 * (0.3 - v) * back
        return V((x, y, z))
    hd = M.plate(name, f, nu, nv, 0.014 * hs, pal, "Head,Neck,Chest")
    if inner:
        M.set_faces_pal(hd, inner, lambda cc, n: (cc - V((0, c.y, c.z))).dot(n) < 0)
    parts = [hd]
    if trim:
        pts = [f(0.0, v / nv) for v in range(nv + 1)][::-1] + [f(1.0, v / nv) for v in range(nv + 1)]
        pts = [V((p.x, p.y - 0.004, p.z)) for p in pts]
        parts.append(M.tube(name + "Trim", pts[1:-1], 0.008 * hs, 6, trim, "Head"))
    return parts


def helmet_shell(P, pal="steel", trim="gold", grow=0.018, brow=0.075, cheek=-0.085, back=-0.11, opening=0.62,
                 flare=0.03, name="Helmet", ridge=True, ridge_pal=None, nu=40, nv=10, trim_r=0.011, top_k=1.0,
                 peak=0.0, peak_back=0.0):
    """One continuous helmet shell: dome + cheek guards + neck guard, with a face opening framed by a trim.
    brow/cheek/back are z offsets of the lower edge (relative to the head center) at the front, cheeks and back.
    opening: half-angle (rad) of the face opening measured from the front."""
    hs = P.hs
    c, r = P.head_c, P.head_r
    R = V((r.x + grow, r.y + grow, (r.z + grow) * top_k))

    def bottom(a):
        a = abs(a)
        if a < opening:
            return brow * hs
        if a < opening + 0.22:
            t = (a - opening) / 0.22
            t = t * t * (3 - 2 * t)
            return brow * hs + (cheek * hs - brow * hs) * t
        t = min(1.0, (a - opening - 0.22) / (math.pi - opening - 0.22))
        return cheek * hs + (back * hs - cheek * hs) * t

    def f(u, v):
        a = -math.pi + 2 * math.pi * u
        zb = bottom(a)
        zt = R.z
        z = zb + (zt - zb) * (1 - (1 - v) ** 1.0)
        # spherical radius at that height (clamped so the guards keep hugging the jaw)
        q = max(-0.98, min(1.0, z / R.z))
        k = math.sqrt(max(0.0, 1 - q * q)) if q > -0.2 else math.sqrt(1 - 0.04)
        k = max(k, 0.0)
        k *= 1 + flare / max(R.x, 1e-3) * (1 - v) ** 3
        x = math.sin(a) * R.x * k
        y = c.y - math.cos(a) * R.y * k
        if peak:
            t = max(0.0, (z / R.z - 0.35) / 0.65)
            z += peak * hs * t ** 2.2
            y += peak_back * hs * t ** 2.2
        return V((x, y, c.z + z))
    sh = M.plate(name, f, nu, nv, 0.012 * hs, pal, "Head", weld=True)
    M.smooth_shade(sh, 70)
    parts = [sh]
    if trim:
        pts = [f(i / nu, 0.0) for i in range(nu)]
        out = []
        for p in pts:
            d = V((p.x, p.y - c.y, 0)).normalized() * 0.006
            out.append(p + d + V((0, 0, 0.004)))
        parts.append(M.tube(name + "Trim", out, trim_r * hs, 8, trim, "Head", closed=True))
    if ridge:
        pts = []
        for i in range(15):
            t = i / 14
            ang = math.pi * (0.06 + 0.82 * t)
            pts.append((0.0, c.y - math.cos(ang) * (R.y + 0.006), c.z + math.sin(ang) * (R.z + 0.006) * 0.99 +
                        (brow * hs if t < 0.08 else 0) * 0))
        pts = [p for p in pts if p[2] >= c.z + brow * hs]
        parts.append(M.tube(name + "Ridge", pts, 0.013 * hs, 8, ridge_pal or trim, "Head",
                            radii=[(0.016 * hs, 0.011 * hs)] * len(pts)))
    return parts


def crown(P, pal="gold", gem="gem_blue", spikes=5, height=0.07, grow=0.012, z_off=0.05, name="Crown"):
    hs = P.hs
    c, r = P.head_c, P.head_r
    z = c.z + z_off * hs
    k = math.sqrt(max(0.0, 1 - (z_off * hs / r.z) ** 2))
    a, b = r.x * k + grow, r.y * k + grow
    parts = []
    band = M.loft(name + "Band", [{"c": (0, c.y, z - 0.008), "a": a, "b": b},
                                  {"c": (0, c.y, z + 0.035 * hs), "a": a * 1.04, "b": b * 1.04}], 24, False, False,
                  pal, "Head")
    M.solidify(band, 0.012 * hs, 1.0)
    parts.append(band)
    for i, zz in enumerate((z - 0.008, z + 0.035 * hs)):
        k2 = 1.0 if i == 0 else 1.04
        parts.append(M.tube(name + f"Rim{i}", M.ring_path((0, c.y, zz), a * k2 + 0.005, b * k2 + 0.005, 24), 0.008 * hs,
                            6, pal, "Head", closed=True))
    for i in range(spikes):
        ang = -math.pi / 2 + (i - (spikes - 1) / 2) * (math.pi * 2 / spikes)
        x, y = math.cos(ang) * a * 1.03, c.y + math.sin(ang) * b * 1.03
        sp = M.extrude_poly(name + f"Spike{i}", [(-0.03 * hs, 0), (0.03 * hs, 0), (0.012 * hs, height * hs * 0.7),
                                                 (0, height * hs), (-0.012 * hs, height * hs * 0.7)],
                            0.012 * hs, pal, "Head", "XZ", 0.003, 1)
        M.xform(sp, rot=(-8, 0, math.degrees(ang) + 90))
        M.xform(sp, loc=(x, y, z + 0.03 * hs))
        parts.append(sp)
        parts.append(M.ellipsoid(name + f"Ball{i}", (x * 1.0, y, z + 0.03 * hs + height * hs + 0.008),
                                 (0.011 * hs, 0.011 * hs, 0.011 * hs), pal, "Head", 8, 6))
    gm = M.extrude_poly(name + "Gem", M.diamond(0.04 * hs, 0.06 * hs), 0.016 * hs, gem, "Head", "XZ", 0.003, 1)
    M.xform(gm, loc=(0, c.y - b * 1.04 - 0.012, z + 0.018 * hs))
    parts.append(gm)
    return parts


def lion_pauldron(P, side=1, pal="gold", trim="gold_dark", base="blue", size=1.0, name=None):
    """Big royal pauldron: blue dome + gold lion-head plate (mane ring, muzzle, brow)."""
    s = P.s * size
    sd = "Left" if side > 0 else "Right"
    nm = name or ("Lion" + sd[0])
    parts = pauldron(P, side, base, pal, size * 1.15, 2, nm)
    sh = V((P.shoulder.x * side, P.shoulder.y, P.shoulder.z))
    c = sh + V((0.095 * side * s, -0.005 * s, 0.035 * s))
    face = M.ellipsoid(nm + "Face", (0, 0, 0), (0.028 * s, 0.05 * s, 0.05 * s), pal, f"{sd}UpperArm", 12, 8)
    M.xform(face, loc=c)
    mane = M.tube(nm + "Mane", M.ring_path((0, 0, 0), 0.056 * s, 0.058 * s, 14, axis="X"), 0.016 * s, 8, pal,
                  f"{sd}UpperArm", closed=True)
    M.xform(mane, loc=c - V((0.006 * side * s, 0, 0)))
    muzzle = M.ellipsoid(nm + "Muzzle", (0, 0, 0), (0.022 * s, 0.026 * s, 0.018 * s), trim, f"{sd}UpperArm", 10, 6)
    M.xform(muzzle, loc=c + V((0.024 * side * s, 0, -0.015 * s)))
    parts += [face, mane, muzzle]
    for e in (1, -1):
        eye = M.ellipsoid(nm + f"Eye{e}", (0, 0, 0), (0.008 * s, 0.01 * s, 0.006 * s), "iron_dark", f"{sd}UpperArm", 6, 4)
        M.xform(eye, loc=c + V((0.026 * side * s, e * 0.018 * s, 0.014 * s)))
        parts.append(eye)
    return parts


def fur_collar(P, pal="fur", r=0.045, name="FurCollar"):
    s, w = P.s, P.w
    z = P.shoulder.z + 0.035
    pts = M.ring_path((0, 0.015 * s, z), 0.165 * w, 0.125 * s, 22)
    radii = [r * s * (1 + 0.18 * math.sin(i * 2.7)) for i in range(len(pts))]
    t = M.tube(name, pts, 0, 10, pal, "Chest", closed=True, radii=radii)
    return [t]


def crest(P, pal="red_crest", height=0.07, width=0.026, grow=0.03, name="Crest", front=0.12, back=0.95):
    """Mohawk crest along the helmet ridge (Roman/Spartan style)."""
    hs = P.hs
    c, r = P.head_c, P.head_r
    pts, radii = [], []
    n = 12
    for i in range(n + 1):
        t = i / n
        ang = math.pi * (front + (back - front) * t)
        R = (r.y + grow, r.z + grow)
        lift = height * hs * (0.55 + 0.45 * math.sin(t * math.pi))
        pts.append((0.0, c.y - math.cos(ang) * (R[0] + lift * 0.45), c.z + math.sin(ang) * (R[1] + lift * 0.45)))
        radii.append((width * hs * (0.7 + 0.3 * math.sin(t * math.pi)), lift * 0.55))
    cr = M.tube(name, pts, 0, 10, pal, "Head", radii=radii, up=(1, 0, 0))
    return [cr]


def horns(P, pal="cream", size=1.0, name="Horn", z_off=0.06):
    hs = P.hs * size
    c, r = P.head_c, P.head_r
    parts = []
    for side in (1, -1):
        x0 = side * (r.x + 0.02)
        pts = [(x0, c.y, c.z + z_off * P.hs), (x0 + side * 0.05 * hs, c.y, c.z + z_off * P.hs + 0.03 * hs),
               (x0 + side * 0.085 * hs, c.y - 0.01 * hs, c.z + z_off * P.hs + 0.09 * hs),
               (x0 + side * 0.08 * hs, c.y - 0.02 * hs, c.z + z_off * P.hs + 0.15 * hs)]
        parts.append(M.tube(name + ("L" if side > 0 else "R"), pts, 0, 10, pal, "Head",
                            radii=[0.03 * hs, 0.026 * hs, 0.018 * hs, 0.004 * hs]))
    return parts
