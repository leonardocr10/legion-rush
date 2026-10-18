"""Heroic-cartoon humanoid: proportions, skeleton, body parts and the expressive face.

All characters share the skeleton layout (names + hierarchy). Proportions change per character,
which is fine because every clip only animates rotations (plus the Hips offset).
"""
import math
from mathutils import Vector

from . import mesh as M

V = Vector


class Proportions:
    def __init__(self, scale=1.0, width=1.0, female=False, head=1.0, hands=1.0, legs=1.0):
        s = scale
        w = width * s
        self.s, self.w, self.female, self.hs = s, w, female, head * s
        lg = legs
        self.ankle = V((0.088 * w, 0.0, 0.085 * s))
        self.knee = V((0.088 * w, -0.012 * s, 0.26 * s * lg))
        self.hip = V((0.083 * w, 0.0, 0.44 * s * lg))
        self.toe = V((0.09 * w, -0.13 * s, 0.03 * s))
        lift = 0.44 * s * lg - 0.44 * s
        self.pelvis_z = 0.47 * s + lift
        self.spine_z = 0.56 * s + lift
        self.chest_z = 0.67 * s + lift
        self.neck_z = 0.82 * s + lift
        self.headb_z = 0.86 * s + lift
        self.shoulder = V((0.185 * w, 0.01 * s, 0.775 * s + lift))
        self.elbow = V((0.25 * w, 0.025 * s, 0.6 * s + lift))
        self.wrist = V((0.285 * w, 0.0, 0.465 * s + lift))
        self.hand_end = V((0.298 * w, -0.012 * s, 0.39 * s + lift))
        hs = self.hs
        self.head_c = V((0.0, -0.008 * s, self.headb_z + 0.145 * hs))
        self.head_r = V((0.132 * hs, 0.128 * hs, 0.152 * hs))
        if female:
            self.head_r = V((0.128 * hs, 0.124 * hs, 0.146 * hs))
        self.hand_r = 0.052 * s * hands
        self.lift = lift

    def surface_y(self, x, z, extra=0.0):
        """Front surface of the head ellipsoid at (x, z)."""
        c, r = self.head_c, self.head_r
        k = 1.0 - (x / r.x) ** 2 - ((z - c.z) / r.z) ** 2
        return c.y - r.y * math.sqrt(max(0.0, k)) - extra


def skeleton(P, cape=False, extra=None):
    s = P.s
    b = []

    def add(name, head, tail, parent=None, deform=True):
        b.append({"name": name, "head": tuple(head), "tail": tuple(tail), "parent": parent, "deform": deform})

    add("Root", (0, 0, 0), (0, 0.12 * s, 0), None, False)
    add("Hips", (0, 0, P.pelvis_z), (0, 0, P.spine_z), "Root")
    add("Spine", (0, 0, P.spine_z), (0, 0, P.chest_z), "Hips")
    add("Chest", (0, 0, P.chest_z), (0, 0, P.neck_z), "Spine")
    add("Neck", (0, 0, P.neck_z), (0, 0, P.headb_z), "Chest")
    add("Head", (0, 0, P.headb_z), (0, 0, P.headb_z + 0.3 * P.hs), "Neck")
    for side, sx in (("Left", 1), ("Right", -1)):
        m = V((sx, 1, 1))

        def mm(v):
            return V((v.x * sx, v.y, v.z))
        add(side + "Shoulder", (0.04 * sx * P.w, 0.0, P.shoulder.z - 0.01), mm(P.shoulder), "Chest")
        add(side + "UpperArm", mm(P.shoulder), mm(P.elbow), side + "Shoulder")
        add(side + "LowerArm", mm(P.elbow), mm(P.wrist), side + "UpperArm")
        add(side + "Hand", mm(P.wrist), mm(P.hand_end), side + "LowerArm")
        add(side + "UpperLeg", mm(P.hip), mm(P.knee), "Hips")
        add(side + "LowerLeg", mm(P.knee), mm(P.ankle), side + "UpperLeg")
        add(side + "Foot", mm(P.ankle), mm(V((P.ankle.x, -0.07 * s, 0.03 * s))), side + "LowerLeg")
        add(side + "Toes", mm(V((P.ankle.x, -0.07 * s, 0.03 * s))), mm(P.toe), side + "Foot")
    if cape:
        z0 = P.shoulder.z + 0.02
        add("Cape1", (0, 0.13 * P.w, z0), (0, 0.15 * P.w, z0 - 0.2 * s), "Chest")
        add("Cape2", (0, 0.15 * P.w, z0 - 0.2 * s), (0, 0.17 * P.w, z0 - 0.42 * s), "Cape1")
        add("Cape3", (0, 0.17 * P.w, z0 - 0.42 * s), (0, 0.19 * P.w, z0 - 0.66 * s), "Cape2")
    # sockets (non deforming)
    rh = V((-P.hand_end.x, P.hand_end.y, P.hand_end.z))
    lh = V((P.hand_end.x, P.hand_end.y, P.hand_end.z))
    add("RightHandSocket", (-P.wrist.x - 0.006, -0.012 * s, P.wrist.z - 0.05 * s),
        (-P.wrist.x - 0.006, -0.1 * s, P.wrist.z - 0.05 * s), "RightHand", False)
    add("LeftHandSocket", (P.wrist.x + 0.006, -0.012 * s, P.wrist.z - 0.05 * s),
        (P.wrist.x + 0.006, -0.1 * s, P.wrist.z - 0.05 * s), "LeftHand", False)
    add("HeadSocket", (0, 0, P.head_c.z + P.head_r.z), (0, 0, P.head_c.z + P.head_r.z + 0.08), "Head", False)
    add("BackSocket", (0, 0.16 * P.w, P.chest_z + 0.05), (0, 0.16 * P.w, P.chest_z + 0.15), "Chest", False)
    for e in extra or []:
        add(*e)
    return b


# ---------------------------------------------------------------- body parts

def torso(P, pal="blue", name="Torso", chest=1.0, belly=1.0, sections=None, n=20):
    s, w = P.s, P.w
    L = P.lift
    secs = sections or [
        (0.43, 0.165, 0.118), (0.5, 0.172, 0.124), (0.58, 0.162 * belly, 0.122 * belly),
        (0.66, 0.185 * chest, 0.134 * chest), (0.735, 0.2 * chest, 0.138 * chest), (0.79, 0.178 * chest, 0.12),
        (0.825, 0.11, 0.085), (0.85, 0.062, 0.058), (0.89, 0.058, 0.056)]
    rings = [{"c": (0, 0.004 * s, z * s + L), "a": a * w, "b": b * s, "p": 2.3} for z, a, b in secs]
    return M.loft(name, rings, n, True, True, pal, "Hips,Spine,Chest,Neck")


def leg(P, side=1, pal="leather", name=None, thigh=0.074, calf=0.062):
    s, w = P.s, P.w
    hip, knee, ankle = V((P.hip.x * side, P.hip.y, P.hip.z + 0.02)), V((P.knee.x * side, P.knee.y, P.knee.z)), \
        V((P.ankle.x * side, P.ankle.y, P.ankle.z + 0.05))
    pts = [hip, hip.lerp(knee, 0.5), knee, knee.lerp(ankle, 0.45), ankle]
    radii = [thigh * s * 1.05, thigh * s, calf * s * 0.95, calf * s, calf * s * 0.82]
    nm = name or ("Leg" + ("L" if side > 0 else "R"))
    sd = "Left" if side > 0 else "Right"
    return M.tube(nm, pts, 0.0, 12, pal, f"{sd}UpperLeg,{sd}LowerLeg", radii=radii)


def boot(P, side=1, pal="boot", cuff_pal=None, name=None, height=0.17, size=1.0, cuff=True, toe_up=0.0):
    s, w = P.s * size, P.w
    x = P.ankle.x * side
    sd = "Left" if side > 0 else "Right"
    nm = name or ("Boot" + sd[0])
    parts = []
    # shaft
    shaft = M.loft(nm + "Shaft", [
        {"c": (x, 0.005 * s, 0.07 * s), "a": 0.068 * s, "b": 0.07 * s},
        {"c": (x, 0.004 * s, 0.11 * s), "a": 0.064 * s, "b": 0.066 * s},
        {"c": (x, 0.0, height * s), "a": 0.07 * s, "b": 0.07 * s}], 14, False, True, pal,
        f"{sd}LowerLeg,{sd}Foot")
    parts.append(shaft)
    # foot (lofted along -Y)
    secs = []
    for y, a, h, z in ((0.07, 0.055, 0.06, 0.055), (0.03, 0.068, 0.085, 0.06), (-0.04, 0.07, 0.075, 0.05),
                       (-0.1, 0.066, 0.06, 0.042), (-0.145, 0.05, 0.045, 0.036 + toe_up), (-0.165, 0.025, 0.03, 0.034 + toe_up)):
        secs.append({"c": (x, y * s, z * s), "a": a * s, "b": h * s * 0.75, "u": (1, 0, 0), "v": (0, 0, 1), "p": 2.6})
    foot = M.loft(nm + "Foot", secs, 14, True, True, pal, f"{sd}Foot,{sd}Toes")
    # flatten the sole
    M.deform(foot, lambda p: V((p.x, p.y, max(p.z, 0.0))))
    parts.append(foot)
    sole = M.loft(nm + "Sole", [
        {"c": (x, y * s, 0.012 * s), "a": a * s * 1.04, "b": 0.012 * s, "u": (1, 0, 0), "v": (0, 0, 1), "p": 3}
        for y, a in ((0.075, 0.05), (0.03, 0.068), (-0.06, 0.07), (-0.13, 0.058), (-0.168, 0.03))], 14, True, True,
        "leather_dark", f"{sd}Foot,{sd}Toes")
    parts.append(sole)
    if cuff:
        c = M.lathe(nm + "Cuff", [(0.072, height - 0.035), (0.088, height - 0.03), (0.092, height),
                                  (0.084, height + 0.012), (0.068, height + 0.01)], 16, cuff_pal or pal,
                    f"{sd}LowerLeg", axis_center=(x, 0, 0), close_top=False)
        M.xform(c, scale=(s, s, s), pivot=(x, 0, 0))
        parts.append(c)
    return parts


def arm(P, side=1, pal="blue", name=None, upper=0.06, fore=0.052, puff=1.0):
    s = P.s
    sd = "Left" if side > 0 else "Right"
    sh = V((P.shoulder.x * side, P.shoulder.y, P.shoulder.z))
    el = V((P.elbow.x * side, P.elbow.y, P.elbow.z))
    wr = V((P.wrist.x * side, P.wrist.y, P.wrist.z))
    start = sh + (sh - el).normalized() * 0.02
    pts = [start, sh.lerp(el, 0.35), el, el.lerp(wr, 0.5), wr]
    radii = [upper * s * puff * 1.1, upper * s * puff, fore * s * 1.02, fore * s, fore * s * 0.88]
    return M.tube(name or ("Arm" + sd[0]), pts, 0, 12, pal, f"{sd}UpperArm,{sd}LowerArm", radii=radii)


def fist(P, side=1, pal="leather", name=None, open_hand=False):
    s = P.s
    sd = "Left" if side > 0 else "Right"
    wr = V((P.wrist.x * side, P.wrist.y, P.wrist.z))
    he = V((P.hand_end.x * side, P.hand_end.y, P.hand_end.z))
    d = (he - wr).normalized()
    c = wr + d * 0.045 * s
    r = P.hand_r
    palm = M.ellipsoid((name or "Hand" + sd[0]), (0, 0, 0), (r * 0.95, r * 1.05, r * 1.1), pal, sd + "Hand", 12, 8, 2.4)
    M.xform(palm, loc=c)
    thumb = M.ellipsoid((name or "Hand" + sd[0]) + "Thumb", (0, 0, 0), (r * 0.42, r * 0.42, r * 0.62), pal,
                        sd + "Hand", 10, 6)
    M.xform(thumb, loc=c + V((-0.02 * side * s, -0.04 * s, 0.012 * s)), rot=(20, -25 * side, 0))
    return [palm, thumb]


def cuff_ring(P, side, at="wrist", pal="steel", r=0.06, h=0.05, name=None, flare=1.15):
    s = P.s
    sd = "Left" if side > 0 else "Right"
    a = V((P.elbow.x * side, P.elbow.y, P.elbow.z))
    b = V((P.wrist.x * side, P.wrist.y, P.wrist.z))
    d = (b - a).normalized()
    center = b - d * 0.01 * s if at == "wrist" else a.lerp(b, 0.5)
    p0 = center - d * h * s * 0.5
    p1 = center + d * h * s * 0.5
    return M.tube(name or ("Cuff" + sd[0] + at), [p0, p0.lerp(p1, 0.5), p1], 0, 14, pal, f"{sd}LowerArm",
                  radii=[r * s, r * s * 1.03, r * s * flare])


# ---------------------------------------------------------------- head & face

def head(P, pal="skin", ears=True, nose=True, chin=1.0):
    c, r = P.head_c, P.head_r
    parts = []
    secs = []
    rings = 12
    for i in range(1, rings):
        phi = math.pi * i / rings - math.pi / 2
        cs, sn = math.cos(phi), math.sin(phi)
        jaw = 1.0 + (0.06 * chin if sn < -0.2 else 0.0) * min(1.0, (-sn - 0.2) * 3)
        secs.append({"c": (c.x, c.y - (0.012 * jaw if sn < 0 else 0), c.z + sn * r.z),
                     "a": max(1e-4, (abs(cs) ** 0.92) * r.x * (jaw if sn < 0.3 else 1.0)),
                     "b": max(1e-4, (abs(cs) ** 0.92) * r.y), "p": 2.2})
    hd = M.loft("Head", secs, 20, True, True, pal, "Head")
    parts.append(hd)
    if ears:
        for side in (1, -1):
            e = M.ellipsoid("Ear" + ("L" if side > 0 else "R"), (0, 0, 0), (0.016, 0.032, 0.042), pal, "Head", 10, 6)
            M.xform(e, loc=(c.x + side * r.x * 0.97, c.y + 0.012, c.z - 0.005), rot=(0, 0, -15 * side))
            parts.append(e)
    if nose:
        z = c.z - 0.028 * P.hs
        n = M.ellipsoid("Nose", (0, 0, 0), (0.022 * P.hs, 0.02 * P.hs, 0.025 * P.hs), pal, "Head", 10, 6)
        M.xform(n, loc=(0, P.surface_y(0, z) + 0.004, z))
        parts.append(n)
    return parts


NEUTRAL = dict(brow_in=-0.004, brow_out=0.004, lid=0.12, mouth_w=0.034, mouth_open=0.004, curve=0.0,
               teeth=0.0, pupil=1.0, look=0.0)

EXPRESSIONS = {
    "Happy": dict(brow_in=0.01, brow_out=0.008, lid=0.22, mouth_w=0.04, mouth_open=0.016, curve=0.014, teeth=0.6),
    "Angry": dict(brow_in=-0.02, brow_out=0.008, lid=0.2, mouth_w=0.036, mouth_open=0.012, curve=-0.008, teeth=0.8),
    "Attack": dict(brow_in=-0.02, brow_out=0.01, lid=0.15, mouth_w=0.038, mouth_open=0.036, curve=-0.006, teeth=0.5),
    "Surprised": dict(brow_in=0.02, brow_out=0.016, lid=0.0, mouth_w=0.022, mouth_open=0.03, curve=0.0, pupil=0.75),
    "Pain": dict(brow_in=0.014, brow_out=-0.01, lid=0.62, mouth_w=0.036, mouth_open=0.016, curve=-0.012, teeth=0.9),
    "Victory": dict(brow_in=0.012, brow_out=0.01, lid=0.3, mouth_w=0.044, mouth_open=0.03, curve=0.02, teeth=0.7),
    "Defeat": dict(brow_in=0.014, brow_out=-0.012, lid=0.95, mouth_w=0.03, mouth_open=0.004, curve=-0.014),
}


def face(P, expr=None, iris="iris_brown", brow_pal="brow_dark", mustache=None, beard=None, hair_pal="hair_dark",
         lashes=False, eye_scale=1.0, base_brow=None, lid_pal="skin", brow_w=1.0, mouth_pal="mouth"):
    """Builds face features for one expression. Same topology for every expression (shape keys)."""
    e = dict(NEUTRAL)
    if base_brow:
        e.update(base_brow)
    if expr:
        e.update(expr)
    hs = P.hs
    c, r = P.head_c, P.head_r
    parts = []
    eye_z = c.z + 0.004 * hs
    ex, ez = 0.024 * hs * eye_scale, 0.03 * hs * eye_scale
    for side in (1, -1):
        sd = "L" if side > 0 else "R"
        x = 0.05 * hs * side * (1.0 + 0.1 * (eye_scale - 1))
        y = P.surface_y(x, eye_z) + 0.001
        yaw = -14 * side
        white = M.ellipsoid("EyeWhite" + sd, (0, 0, 0), (ex, 0.012 * hs, ez), "eye_white", "Head", 12, 8)
        M.xform(white, loc=(x, y, eye_z), rot=(0, 0, yaw))
        ir = M.ellipsoid("Iris" + sd, (0, 0, 0), (ex * 0.68, 0.006 * hs, ez * 0.66), iris, "Head", 12, 6)
        M.xform(ir, loc=(x - 0.004 * side * hs + e["look"], y - 0.0075 * hs, eye_z - 0.002 * hs), rot=(0, 0, yaw))
        pk = e["pupil"]
        pu = M.ellipsoid("Pupil" + sd, (0, 0, 0), (ex * 0.34 * pk, 0.004 * hs, ez * 0.36 * pk), "pupil", "Head", 10, 6)
        M.xform(pu, loc=(x - 0.004 * side * hs + e["look"], y - 0.011 * hs, eye_z - 0.002 * hs), rot=(0, 0, yaw))
        hl = M.ellipsoid("EyeHL" + sd, (0, 0, 0), (ex * 0.16, 0.003 * hs, ez * 0.16), "eye_white", "Head", 8, 4)
        M.xform(hl, loc=(x + 0.006 * hs - 0.004 * side * hs, y - 0.0135 * hs, eye_z + 0.008 * hs), rot=(0, 0, yaw))
        # eyelid: skin dome that slides down over the eye (lid=1 closed)
        lid = e["lid"]
        lz = eye_z + ez * (2.08 - 2.0 * lid)
        # open lids shrink and retreat inside the head so they never read as bumps above the eyes
        k = min(1.0, lid / 0.3)
        ly = y + 0.004 + 0.035 * (1.0 - k)
        lz -= ez * 0.6 * (1.0 - k)
        ks = 0.35 + 0.65 * k
        ld = M.ellipsoid("Lid" + sd, (0, 0, 0), (ex * 1.16 * ks, 0.015 * hs * ks, ez * 1.1 * ks), lid_pal, "Head", 12, 8)
        M.xform(ld, loc=(x, ly, lz), rot=(0, 0, yaw))
        parts += [white, ir, pu, hl, ld]
        if lashes:
            lash = M.tube("Lash" + sd, [(x - side * ex * 0.95, 0, 0), (x, 0, 0), (x + side * ex * 1.15, 0, 0)], 0, 6,
                          "brow_dark", "Head", radii=[0.003 * hs, 0.005 * hs, 0.004 * hs])
            M.deform(lash, lambda p, x=x, lz=lz: V((p.x, P.surface_y(p.x, lz - ez * 1.05) - 0.006,
                                                    lz - ez * 1.0 + 0.005 * hs * (1 - abs(p.x - x) / ex))))
            parts.append(lash)
        # brow
        bz = eye_z + 0.048 * hs
        xi, xo = 0.016 * hs * side, 0.088 * hs * side
        bp = []
        for t in (0.0, 0.33, 0.66, 1.0):
            bx = xi + (xo - xi) * t
            arch = 0.008 * hs * math.sin(t * math.pi)
            bzz = bz + e["brow_in"] * hs * (1 - t) + e["brow_out"] * hs * t + arch
            bp.append((bx, P.surface_y(bx, bzz) - 0.006 * hs, bzz))
        brow = M.tube("Brow" + sd, bp, 0, 8, brow_pal, "Head",
                      radii=[(0.013 * hs * brow_w, 0.01 * hs * brow_w), (0.015 * hs * brow_w, 0.011 * hs * brow_w),
                             (0.012 * hs * brow_w, 0.009 * hs * brow_w), (0.007 * hs * brow_w, 0.006 * hs * brow_w)])
        parts.append(brow)
    # mouth (closed outline, constant vertex count)
    mz = c.z - 0.078 * hs
    w = e["mouth_w"] * hs
    op = e["mouth_open"] * hs
    cv = e["curve"] * hs
    n = 10
    top, bot = [], []
    for i in range(n + 1):
        t = -1 + 2 * i / n
        k = 1 - t * t
        top.append((t * w, op * 0.35 * k + cv * t * t))
        bot.append((t * w, -op * k + cv * t * t - 0.0025 * hs * k))
    outline = top[::-1] + bot[1:-1]
    mouth = M.extrude_poly("Mouth", outline, 0.014, mouth_pal, "Head", "XZ", 0, 1, 0.5, sharp=None)
    M.deform(mouth, lambda p: V((p.x, P.surface_y(p.x, mz + p.z) + 0.004 + p.y, mz + p.z)))
    parts.append(mouth)
    th = e["teeth"] * max(0.0, op * 0.45)
    teeth = M.box("Teeth", (w * 1.1, 0.006, max(th, 0.0004)), (0, 0, 0), "teeth", "Head")
    M.deform(teeth, lambda p: V((p.x, P.surface_y(p.x, mz + p.z) + 0.006 + p.y, mz + op * 0.2 + p.z - th * 0.4)))
    parts.append(teeth)
    if mustache:
        parts += _mustache(P, mustache, hair_pal, e)
    if beard:
        parts += _beard(P, beard, hair_pal)
    return parts


def _mustache(P, kind, pal, e):
    hs = P.hs
    c = P.head_c
    z0 = c.z - 0.056 * hs
    out = []
    for side in (1, -1):
        if kind == "handlebar":
            ctrl = [(0.004, 0.0), (0.03, -0.004), (0.058, -0.016), (0.074, -0.04), (0.07, -0.06)]
            radii = [0.011, 0.016, 0.014, 0.01, 0.006]
        elif kind == "thick":
            ctrl = [(0.004, 0.0), (0.03, -0.006), (0.056, -0.022), (0.066, -0.048)]
            radii = [0.013, 0.017, 0.014, 0.008]
        else:  # thin
            ctrl = [(0.004, 0.0), (0.028, -0.004), (0.05, -0.016)]
            radii = [0.008, 0.009, 0.005]
        pts = []
        for x, dz in ctrl:
            xx = x * hs * side
            zz = z0 + dz * hs + e["curve"] * hs * 0.4 * (x / 0.07)
            pts.append((xx, P.surface_y(xx, zz) - 0.008 * hs, zz))
        out.append(M.tube("Mustache" + ("L" if side > 0 else "R"), pts, 0, 8, pal, "Head",
                          radii=[(rr * hs, rr * hs * 0.8) for rr in radii]))
    return out


def _beard(P, kind, pal):
    hs = P.hs
    c, r = P.head_c, P.head_r
    out = []
    if kind == "goatee":
        z = c.z - r.z * 0.86
        g = M.ellipsoid("Goatee", (0, 0, 0), (0.03 * hs, 0.024 * hs, 0.036 * hs), pal, "Head", 12, 8)
        M.xform(g, loc=(0, P.surface_y(0, z + 0.02 * hs) + 0.008, z))
        out.append(g)
    elif kind in ("full", "short", "big"):
        depth = {"short": 0.03, "full": 0.07, "big": 0.11}[kind]

        def f(u, v):
            ang = (u - 0.5) * math.pi * 1.15  # 0 = front
            side = min(1.0, abs(ang) / 1.25)
            side = side * side * (3 - 2 * side)
            top = c.z + (-0.108 + 0.1 * side) * hs
            bottom = c.z - r.z * 0.92 - depth * hs * (1 - 0.6 * side)
            z = top + (bottom - top) * v
            q = max(-1.0, min(1.0, (z - c.z) / r.z))
            k = max(0.55, math.sqrt(1 - q * q)) * (1 - 0.55 * v ** 2.2)
            x = math.sin(ang) * (r.x * 1.13) * k
            y = c.y - math.cos(ang) * (r.y * 1.15) * max(k, 0.5) - 0.02 * hs * v
            return V((x, y, z))
        bd = M.plate("Beard", f, 16, 7, 0.018 * hs, pal, "Head")
        out.append(bd)
    return out


# ---------------------------------------------------------------- hair

def hair_long(P, pal="hair_brown", length=0.22, bangs=True, side_locks=True, back=True, volume=1.0, name="Hair"):
    """Long wavy hair as clumps (works under a hood): back mass, front locks over the shoulders, swept bangs."""
    hs = P.hs
    c, r = P.head_c, P.head_r
    parts = []
    if back:
        secs = []
        for i in range(7):
            t = i / 6
            z = c.z + 0.02 * hs - t * (r.z + length * hs)
            k = (1 - 0.35 * t) * volume
            secs.append({"c": (0, c.y + 0.03 * hs + 0.04 * hs * t, z), "a": (r.x * 1.05) * k, "b": r.y * 0.85 * k,
                         "p": 2.0, "w": (lambda tt: 1 + 0.06 * math.sin(tt * 5))})
        bk = M.loft(name + "Back", secs, 18, True, True, pal, "Head,Neck,Chest")
        parts.append(bk)
    if side_locks:
        for side in (1, -1):
            sd = "L" if side > 0 else "R"
            x0 = side * r.x * 0.86
            pts = [(x0, c.y - 0.02 * hs, c.z - 0.0 * hs), (x0 * 1.08, c.y - 0.03 * hs, c.z - 0.09 * hs),
                   (x0 * 1.12, c.y - 0.045 * hs, c.z - 0.17 * hs), (x0 * 1.18, c.y - 0.03 * hs, c.z - 0.25 * hs),
                   (x0 * 1.1, c.y - 0.04 * hs, c.z - 0.32 * hs)]
            radii = [(0.03 * hs, 0.026 * hs), (0.036 * hs, 0.03 * hs), (0.032 * hs, 0.028 * hs),
                     (0.03 * hs, 0.026 * hs), (0.008 * hs, 0.008 * hs)]
            parts.append(M.tube(name + "Lock" + sd, pts, 0, 8, pal, "Head,Neck,Chest", radii=radii))
    if bangs:
        for k, (x0, x1, drop) in enumerate(((0.035, -0.1, 0.055), (0.095, 0.03, 0.05))):
            pts = []
            for i in range(5):
                t = i / 4
                x = (x0 + (x1 - x0) * t) * hs
                z = c.z + (0.12 - drop * math.sin(t * math.pi * 0.9) - 0.02 * t) * hs
                pts.append((x, P.surface_y(x, z) - 0.008 * hs, z))
            parts.append(M.tube(name + f"Bang{k}", pts, 0, 8, pal, "Head",
                                radii=[(0.03 * hs, 0.016 * hs), (0.034 * hs, 0.018 * hs), (0.028 * hs, 0.016 * hs),
                                       (0.018 * hs, 0.011 * hs), (0.006 * hs, 0.005 * hs)]))
    return parts


def hair_cap(P, pal="hair_dark", grow=0.008, front=0.06, name="HairCap"):
    """Short hair hugging the skull (for characters with crowns or bare heads)."""
    hs = P.hs
    c, r = P.head_c, P.head_r
    secs = []
    for i in range(8):
        t = i / 7
        phi = -0.35 + (math.pi / 2 + 0.35) * t
        z = c.z + math.sin(phi) * (r.z + grow)
        k = math.cos(phi)
        secs.append({"c": (0, c.y + 0.01 * hs * (1 - t), z), "a": max(1e-3, (r.x + grow) * k),
                     "b": max(1e-3, (r.y + grow) * k), "p": 2.2})
    cap = M.loft(name, secs, 20, False, True, pal, "Head")
    # open the face: push the front bottom vertices up behind the forehead
    zf = c.z + front * hs

    def fn(p):
        if p.y < c.y - r.y * 0.35 and p.z < zf:
            return V((p.x, p.y + 0.03 * hs, zf - (zf - p.z) * 0.15))
        return p
    M.deform(cap, fn)
    return [cap]
