"""Cavaleiro Montado (Ally_MountedKnight): armoured knight + white warhorse in blue/gold barding.

Rider and horse are separate skinned meshes on ONE armature: the rider's Hips is parented to the horse's
Saddle bone, so every clip animates both in sync (Generic rig, one Animator)."""
import math
from mathutils import Vector

from .. import anim, armor as A, humanoid as H, mesh as M, rig, weapons as W
from ..anim import merge
from ..character import Build

V = Vector
NAME = "MountedKnight"
STYLE = "spear_shield"
SADDLE_Z = 0.86
RIDER_DY = -0.02

LEGS = {"FrontLeft": (0.095, -0.25), "FrontRight": (-0.095, -0.25), "BackLeft": (0.095, 0.27),
        "BackRight": (-0.095, 0.27)}


def horse_bones():
    b = [{"name": "HorseBody", "head": (0, 0.12, 0.62), "tail": (0, -0.18, 0.62), "parent": "Root"},
         {"name": "HorseNeck", "head": (0, -0.3, 0.74), "tail": (0, -0.43, 0.96), "parent": "HorseBody"},
         {"name": "HorseHead", "head": (0, -0.43, 0.96), "tail": (0, -0.69, 0.84), "parent": "HorseNeck"},
         {"name": "HorseTail1", "head": (0, 0.37, 0.72), "tail": (0, 0.47, 0.57), "parent": "HorseBody"},
         {"name": "HorseTail2", "head": (0, 0.47, 0.57), "tail": (0, 0.51, 0.36), "parent": "HorseTail1"},
         {"name": "Saddle", "head": (0, RIDER_DY, SADDLE_Z - 0.04), "tail": (0, RIDER_DY, SADDLE_Z + 0.06),
          "parent": "HorseBody", "deform": False}]
    for n, (x, y) in LEGS.items():
        b.append({"name": n + "Upper", "head": (x, y, 0.56), "tail": (x, y, 0.32), "parent": "HorseBody"})
        b.append({"name": n + "Lower", "head": (x, y, 0.32), "tail": (x, y, 0.1), "parent": n + "Upper"})
        b.append({"name": n + "Hoof", "head": (x, y, 0.1), "tail": (x, y - 0.04, 0.0), "parent": n + "Lower"})
    return b


def rider_bones(P):
    bones = H.skeleton(P, cape=True)
    dz = SADDLE_Z - P.pelvis_z
    out = []
    for bn in bones:
        if bn["name"] == "Root":
            continue
        bn = dict(bn)
        bn["head"] = (bn["head"][0], bn["head"][1] + RIDER_DY, bn["head"][2] + dz)
        bn["tail"] = (bn["tail"][0], bn["tail"][1] + RIDER_DY, bn["tail"][2] + dz)
        if bn["parent"] == "Root":
            bn["parent"] = "Saddle"
        out.append(bn)
    return out


# ---------------------------------------------------------------- horse mesh

def build_horse():
    parts = []
    # barrel (lofted along -Y), white coat
    secs = []
    for y, a, bb, z in ((0.42, 0.09, 0.1, 0.66), (0.36, 0.15, 0.17, 0.64), (0.2, 0.17, 0.2, 0.63), (0.0, 0.165, 0.2, 0.62),
                        (-0.2, 0.165, 0.2, 0.63), (-0.33, 0.15, 0.19, 0.65), (-0.4, 0.1, 0.13, 0.69)):
        secs.append({"c": (0, y, z), "a": a, "b": bb, "u": (1, 0, 0), "v": (0, 0, 1), "p": 2.2})
    parts.append(M.loft("HorseBodyMesh", secs, 18, True, True, "horse_white", "HorseBody,HorseNeck,HorseTail1"))
    # neck + head
    parts.append(M.tube("HorseNeckMesh", [(0, -0.3, 0.7), (0, -0.37, 0.84), (0, -0.43, 0.96), (0, -0.47, 1.02)], 0, 14,
                        "horse_white", "HorseBody,HorseNeck,HorseHead",
                        radii=[(0.11, 0.13), (0.085, 0.11), (0.075, 0.09), (0.07, 0.08)]))
    hsecs = []
    for t, (a, bb) in enumerate(((0.068, 0.085), (0.074, 0.098), (0.068, 0.088), (0.06, 0.074), (0.058, 0.066),
                                 (0.044, 0.046))):
        k = t / 5
        y = -0.43 - 0.27 * k
        z = 1.03 - 0.18 * k
        hsecs.append({"c": (0, y, z), "a": a, "b": bb, "u": (1, 0, 0), "v": (0, -0.55, 0.83), "p": 2.3})
    parts.append(M.loft("HorseHeadMesh", hsecs, 14, True, True, "horse_white", "HorseHead"))
    for sx in (1, -1):
        ear = M.lathe(f"HorseEar{sx}", [(0.022, 0), (0.018, 0.03), (0.001, 0.07)], 8, "horse_white", "HorseHead")
        M.xform(ear, rot=(-15, 18 * sx, 0))
        M.xform(ear, loc=(0.045 * sx, -0.44, 1.07))
        parts.append(ear)
        eye = M.ellipsoid(f"HorseEye{sx}", (0.058 * sx, -0.52, 1.0), (0.012, 0.018, 0.016), "pupil", "HorseHead", 8, 6)
        parts.append(eye)
        nos = M.ellipsoid(f"HorseNostril{sx}", (0.022 * sx, -0.705, 0.86), (0.01, 0.01, 0.012), "hoof", "HorseHead", 6, 4)
        parts.append(nos)
    # mane (white-grey tufts along the neck) and blue tail
    for i in range(6):
        t = i / 5
        y = -0.33 - 0.14 * t
        z = 0.86 + 0.2 * t
        tuft = M.ellipsoid(f"Mane{i}", (0, y + 0.06, z + 0.03), (0.03, 0.06, 0.05), "horse_grey",
                           "HorseNeck" if i < 5 else "HorseHead", 8, 6)
        parts.append(tuft)
    parts.append(M.tube("HorseTailMesh", [(0, 0.38, 0.72), (0, 0.46, 0.62), (0, 0.5, 0.48), (0, 0.5, 0.33)], 0, 10,
                        "blue_plume", "HorseTail1,HorseTail2",
                        radii=[(0.03, 0.03), (0.05, 0.045), (0.055, 0.05), (0.015, 0.02)]))
    # legs with blue/gold guards and dark hooves
    for n, (x, y) in LEGS.items():
        front = y < 0
        top = 0.6
        pts = [(x, y, top), (x, y, 0.44), (x, y + (0.012 if front else -0.02), 0.32), (x, y, 0.2), (x, y, 0.1)]
        parts.append(M.tube(n + "LegMesh", pts, 0, 10, "horse_white", f"HorseBody,{n}Upper,{n}Lower",
                            radii=[0.075, 0.06, 0.042, 0.036, 0.038]))
        parts.append(M.lathe(n + "HoofMesh", [(0.046, 0.0), (0.048, 0.03), (0.04, 0.07)], 10, "hoof", n + "Hoof",
                             axis_center=(x, y - 0.006, 0.0)))
        parts.append(M.lathe(n + "Guard", [(0.044, 0.08), (0.05, 0.11), (0.046, 0.15), (0.04, 0.17)], 10, "blue",
                             n + "Lower", axis_center=(x, y, 0.0), close_top=False))
        parts.append(M.tube(n + "GuardRim", M.ring_path((x, y, 0.08), 0.048, 0.048, 12), 0.008, 6, "gold", n + "Lower",
                            closed=True))
    # caparison (blue barding with gold trim and fleur-de-lis on the flanks)
    def cap(u, v):
        ang = math.pi * (0.05 + 0.9 * u)  # around the barrel, top half and sides
        y = -0.36 + 0.78 * v
        prof = 0.215 if abs(y) < 0.3 else 0.18
        x = math.cos(ang) * 0.215
        z = 0.63 + math.sin(ang) * prof * 1.08
        hang = max(0.0, 0.18 - math.sin(ang) * 0.18)
        return V((x * (1 + 0.1 * hang), y, z - hang * 1.4))
    cp = M.plate("Caparison", cap, 12, 10, 0.012, "blue", "HorseBody")
    parts.append(cp)
    for u in (0.0, 1.0):
        pts = [cap(u, v / 10) + V((0.006 if u < 0.5 else -0.006, 0, -0.004)) for v in range(11)]
        parts.append(M.tube(f"CapTrim{int(u)}", pts, 0.012, 6, "gold", "HorseBody"))
    for sx in (1, -1):
        em = M.extrude_poly(f"CapFleur{sx}", M.fleur_de_lis(0.13), 0.008, "gold", "HorseBody", "YZ", 0.002, 1)
        M.xform(em, loc=(0.235 * sx, 0.12, 0.5))
        parts.append(em)
    # saddle + chanfron (face armour)
    parts.append(M.box("SaddleSeat", (0.2, 0.24, 0.05), (0, RIDER_DY, 0.82), "leather_dark", "HorseBody", 0.02))
    parts.append(M.box("SaddleBack", (0.18, 0.04, 0.08), (0, RIDER_DY + 0.12, 0.86), "leather_dark", "HorseBody", 0.015))
    parts.append(M.tube("SaddleRim", M.ring_path((0, RIDER_DY, 0.845), 0.105, 0.125, 16), 0.008, 6, "gold", "HorseBody",
                        closed=True))
    ch = M.plate("Chanfron", lambda u, v: V(((u - 0.5) * 0.13 * (1 - 0.35 * v), -0.5 - 0.2 * v - 0.085 * math.cos(
        (u - 0.5) * 2.4) * 0.6, 1.06 - 0.15 * v)), 6, 6, 0.01, "blue", "HorseHead")
    parts.append(ch)
    parts.append(M.lathe("ChanfronCrest", [(0.016, 0), (0.001, 0.06)], 8, "gold", "HorseHead", axis_center=(0, -0.46, 1.07)))
    # bridle
    parts.append(M.tube("Bridle", M.ring_path((0, -0.62, 0.9), 0.066, 0.075, 14, axis="Y"), 0.008, 6, "leather_dark",
                        "HorseHead", closed=True))
    return parts


# ---------------------------------------------------------------- clips

RIDE = {"LeftUpperLeg": (-70, -34, 0), "RightUpperLeg": (-70, 34, 0), "LeftLowerLeg": (80, 0, 0),
        "RightLowerLeg": (80, 0, 0), "LeftFoot": (-14, 0, 0), "RightFoot": (-14, 0, 0)}


def rider(pose):
    out = dict(pose)
    out.update(RIDE)
    out.pop("_hips", None)
    out.pop("_hipsrot", None)
    return out


def horse_gait(ph, amp, phases, knee=30, bob=0.0, pitch=0.0):
    p = {}
    for n, off in phases.items():
        q = (ph + off) % 1.0
        s = math.cos(q * math.tau)
        lift = max(0.0, math.sin(q * math.tau))
        front = n.startswith("Front")
        p[n + "Upper"] = (-amp * s, 0, 0)
        p[n + "Lower"] = ((knee if front else -knee * 0.8) * lift, 0, 0)
        p[n + "Hoof"] = ((20 if front else -10) * lift, 0, 0)
    p["HorseBody"] = (pitch * math.sin(ph * math.tau), 0, 0)
    p["_loc:HorseBody"] = (0, 0, bob * abs(math.sin(ph * math.tau * 2)))
    p["HorseNeck"] = (-6 * math.sin(ph * math.tau * 2), 0, 0)
    p["HorseHead"] = (8 * math.sin(ph * math.tau * 2 + 1), 0, 0)
    p["HorseTail1"] = (10 + 6 * math.sin(ph * math.tau), 0, 8 * math.sin(ph * math.tau))
    return p


WALK = {"BackLeft": 0.0, "FrontLeft": 0.25, "BackRight": 0.5, "FrontRight": 0.75}
GALLOP = {"BackLeft": 0.0, "BackRight": 0.1, "FrontLeft": 0.42, "FrontRight": 0.52}


def cycle(rider_keys_fn, dur, amp, phases, knee, bob, pitch, n=10, bounce=0.0):
    keys = []
    for i in range(n + 1):
        ph = i / n
        pose = merge(rider(rider_keys_fn(ph)), horse_gait(ph, amp, phases, knee, bob, pitch))
        if bounce:
            pose["Spine"] = (pose.get("Spine", (0, 0, 0))[0] + bounce * math.sin(ph * math.tau * 2), 0, 0)
        keys.append((dur * ph, pose))
    return keys


def clips(b):
    base = anim.stance(STYLE)
    idle_r = lambda ph: merge(base, {"Chest": (1.5 * math.sin(ph * math.tau), 0, 0)})
    idle = []
    for i in range(5):
        ph = i / 4
        h = {"HorseNeck": (3 * math.sin(ph * math.tau), 0, 0), "HorseHead": (-4 * math.sin(ph * math.tau), 0, 4 * math.sin(ph * math.tau)),
             "HorseTail1": (8, 0, 12 * math.sin(ph * math.tau)), "_loc:HorseBody": (0, 0, 0.004 * math.sin(ph * math.tau))}
        idle.append((2.4 * ph, merge(rider(idle_r(ph)), h)))
    b.action("Idle", (idle, True))
    b.action("Walk", (cycle(lambda ph: base, 1.1, 18, WALK, 40, 0.012, 1.5), True))
    b.action("Run", (cycle(lambda ph: base, 0.55, 34, GALLOP, 70, 0.05, 6, bounce=4), True))
    charge_r = dict(anim.spear_charge(STYLE)[0][0][1])
    b.action("Charge", (cycle(lambda ph: charge_r, 0.5, 38, GALLOP, 75, 0.06, 7, bounce=5), True))
    thrust, _ = anim.spear_thrust(STYLE, lunge=0.3)
    att = []
    for t, p in thrust:
        att.append((t, merge(rider(p), {"HorseNeck": (-6, 0, 0), "HorseTail1": (12, 0, 0)})))
    b.action("Attack", (att, False), impact=0.32)
    # skill: rear up and swing the lance in an arc
    rear = merge(rider(merge(base, {"RightUpperArm": (-150, 30, 0), "RightLowerArm": (-20, 0, 0), "Spine": (-10, 0, 0)})),
                 {"HorseBody": (-28, 0, 0), "_loc:HorseBody": (0, 0.08, 0.12), "FrontLeftUpper": (-70, 0, 0),
                  "FrontRightUpper": (-60, 0, 0), "FrontLeftLower": (90, 0, 0), "FrontRightLower": (80, 0, 0),
                  "BackLeftUpper": (20, 0, 0), "BackRightUpper": (22, 0, 0), "HorseNeck": (-20, 0, 0),
                  "HorseHead": (10, 0, 0), "HorseTail1": (30, 0, 0)})
    swing = merge(rear, {"RightUpperArm": (40, -60, -40), "Spine": (14, 0, -30)})
    idle0 = idle[0][1]
    b.action("Skill", ([(0, idle0), (0.3, rear), (0.55, swing), (0.75, rear), (1.1, idle0)], False), impact=0.55)
    hitp = merge(rider(merge(base, {"Spine": (-12, 0, 6), "Head": (-12, 0, 0)})),
                 {"HorseNeck": (-14, 0, 8), "HorseHead": (-10, 0, 0), "_loc:HorseBody": (0, 0.03, 0)})
    b.action("Hit", ([(0, idle0), (0.1, hitp), (0.45, idle0)], False))
    fall = merge(rider(merge(base, {"Spine": (-20, 0, 0), "LeftUpperArm": (-60, -60, 0), "RightUpperArm": (-60, 60, 0)})),
                 {"HorseBody": (0, 82, 0), "_loc:HorseBody": (0.25, 0, -0.36), "FrontLeftUpper": (-30, 0, 0),
                  "BackLeftUpper": (20, 0, 0), "FrontRightUpper": (-50, 0, 0), "BackRightUpper": (30, 0, 0),
                  "HorseNeck": (10, 0, 20), "HorseHead": (10, 0, 0)})
    stagger = merge(hitp, {"HorseBody": (0, 20, 0), "_loc:HorseBody": (0.05, 0, -0.05)})
    b.action("Death", ([(0, idle0), (0.25, stagger), (0.7, fall), (1.2, fall)], False))
    vic_up = merge(rear, {"RightUpperArm": (-10, -30, 20), "Head": (-14, 0, 0)})
    b.action("Victory", ([(0, idle0), (0.4, vic_up), (0.9, merge(vic_up, {"HorseBody": (8, 0, 0)})), (1.4, vic_up),
                          (1.8, idle0)], True))
    b.slot("AttackAlt", "Charge")


def build():
    P = H.Proportions(scale=0.9, width=1.05, head=1.12)
    bones = [{"name": "Root", "head": (0, 0, 0), "tail": (0, 0.12, 0), "parent": None, "deform": False}]
    bones += horse_bones() + rider_bones(P)
    arm = rig.build_armature(NAME + "_Rig", bones)
    b = Build(NAME, P, arm)
    b.height = 1.75

    # rider (built standing at the origin, then moved onto the saddle like its bones)
    rider_parts = []

    def add(*items):
        for it in items:
            if isinstance(it, (list, tuple)):
                add(*it)
            elif it is not None:
                rider_parts.append(it)
    add(H.torso(P, "blue"))
    add(A.breastplate(P, "steel", "gold", 0.6, 0.8, grow=0.008, emblem=M.fleur_de_lis(0.06)))
    for side in (1, -1):
        add(H.leg(P, side, "steel_dark"))
        add(H.boot(P, side, "steel", "gold", height=0.17, size=1.0))
        add(A.knee_guard(P, side, "steel", "gold"))
        add(H.arm(P, side, "blue"))
        add(A.pauldron(P, side, "steel", "gold", 1.0, 2))
        add(A.vambrace(P, side, "steel", "gold"))
        add(H.fist(P, side, "steel_dark"))
    add(A.belt(P, 0.57))
    add(A.skirt(P, 0.56, 0.44, "blue", "gold", 1.2, split_front=True))
    add(A.cape(P, "blue", "gold", "blue_dark", length=0.55, width=0.22, flare=1.4, emblem=M.fleur_de_lis(0.13)))
    add(H.head(P, "skin_tan"))
    add(A.helmet_shell(P, "steel", "gold", grow=0.03, brow=0.075, cheek=-0.07, back=-0.1, opening=0.6))
    add(A.plume(P, "blue_plume", "gold", height=0.16, length=0.28, thick=0.07))
    face_kw = dict(iris="iris_brown", mustache="thick", beard="short", hair_pal="hair_dark", eye_scale=1.15,
                   base_brow={"brow_in": -0.012, "brow_out": 0.006})
    b.faces(**face_kw)
    shift = (0, RIDER_DY, SADDLE_Z - P.pelvis_z)
    for o in rider_parts + b.face + [o for objs in b.face_sets.values() for o in objs]:
        M.xform(o, loc=shift)
    b.add(rider_parts)
    b.add(build_horse())

    clips(b)

    b.pose("Idle")
    rh = b.socket("RightHandSocket")
    lance = W.spear("RoyalLance", length=1.7, head_len=0.24, shaft="blue", head="steel", collar="gold",
                    stripes="gold", butt=0.25, head_w=0.08)
    b.hold(lance, "RightHandSocket", rh.translation, (0, 0.04, 1), (1, 0, 0), "Weapon_RoyalLance")
    la = b.arm.pose.bones["LeftLowerArm"]
    center = (b.arm.matrix_world @ la.head).lerp(b.arm.matrix_world @ la.tail, 0.55) + V((0.06, -0.05, 0.02))
    shield = W.heater_shield("KnightShield", 0.3, 0.38, "blue", "gold", emblem=M.cross_pattee(0.1, 0.045, 0.045))
    b.hold_facing(shield, "LeftHandSocket", center, (0, 0.18, 1), (0.42, -1, 0), "Weapon_KnightShield")
    return b
