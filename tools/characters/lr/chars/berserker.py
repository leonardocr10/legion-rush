"""Berserker (Enemy_Berserker): muscular brute, red beard, red crested helmet, spiked shoulder, tattered red kilt,
heavy double axe."""
import math
from mathutils import Vector

from .. import anim, armor as A, humanoid as H, mesh as M, rig, weapons as W
from ..anim import merge, stance
from ..character import Build

V = Vector
NAME = "Berserker"
STYLE = "two_hand"

MUSCLE_TORSO = [(0.43, 0.17, 0.12), (0.5, 0.176, 0.126), (0.58, 0.168, 0.13), (0.66, 0.205, 0.15),
                (0.735, 0.225, 0.155), (0.79, 0.2, 0.13), (0.825, 0.12, 0.09), (0.85, 0.07, 0.065),
                (0.89, 0.066, 0.062)]


def heavy_attack():
    base = stance(STYLE)
    up = merge(base, {"RightUpperArm": (-165, 20, 0), "RightLowerArm": (-50, 0, 0), "LeftUpperArm": (-150, -20, 0),
                      "LeftLowerArm": (-60, 0, 0), "Spine": (-14, 0, 0), "Chest": (-6, 0, 0), "_hips": (0, 0.03, 0.01)})
    chop = merge(base, {"RightUpperArm": (-60, -10, 0), "RightLowerArm": (-10, 0, 0), "RightHand": (40, 0, 0),
                        "LeftUpperArm": (-60, 10, 0), "LeftLowerArm": (-20, 0, 0), "Spine": (28, 0, 0),
                        "Chest": (12, 0, 0), "LeftUpperLeg": (-34, 6, 0), "LeftLowerLeg": (34, 0, 0),
                        "RightUpperLeg": (12, -4, 0), "_hips": (0, -0.08, -0.08)})
    return [(0, base), (0.28, up), (0.4, chop), (0.6, chop), (0.95, base)], False


def rage():
    base = stance(STYLE)
    roar = merge(base, {"RightUpperArm": (-40, 70, 0), "RightLowerArm": (-90, 0, 0), "LeftUpperArm": (-40, -70, 0),
                        "LeftLowerArm": (-90, 0, 0), "Spine": (-14, 0, 0), "Chest": (-10, 0, 0), "Head": (-18, 0, 0),
                        "LeftUpperLeg": (-6, 14, 0), "RightUpperLeg": (-6, -14, 0), "LeftLowerLeg": (20, 0, 0),
                        "RightLowerLeg": (20, 0, 0), "_hips": (0, 0, -0.06)})
    shake = merge(roar, {"Chest": (0, 0, 6), "Head": (0, 0, -8)})
    return [(0, base), (0.25, roar), (0.4, shake), (0.55, roar), (0.7, shake), (0.85, roar), (1.2, base)], False


def build():
    P = H.Proportions(scale=1.06, width=1.2, head=1.05, hands=1.2)
    arm = rig.build_armature(NAME + "_Rig", H.skeleton(P))
    b = Build(NAME, P, arm)
    b.height = 1.55

    b.add(H.torso(P, "skin_tan", sections=MUSCLE_TORSO))
    # pecs and abs as soft bulges
    for sx in (1, -1):
        b.add(M.ellipsoid(f"Pec{sx}", (0.075 * sx * P.w, -0.14 * P.s, 0.735 * P.s), (0.085, 0.04, 0.06), "skin_tan",
                          "Chest", 12, 8))
    for row in range(2):
        for sx in (1, -1):
            b.add(M.ellipsoid(f"Ab{row}{sx}", (0.035 * sx, -0.128 * P.s, (0.64 - 0.05 * row) * P.s),
                              (0.03, 0.018, 0.024), "skin_tan", "Spine", 8, 6))
    for sx in (1, -1):
        pts = [(-0.16 * sx * P.w, -0.12, 0.83 * P.s), (0.0, -0.18, 0.7 * P.s), (0.16 * sx * P.w, -0.16, 0.58 * P.s)]
        b.add(M.tube("Strap" + str(sx), pts, 0.014, 6, "red", "Spine,Chest", radii=[(0.022, 0.008)] * 3))
    ring = M.tube("StrapRing", M.ring_path((0, 0, 0), 0.028, 0.028, 12, axis="Y"), 0.008, 6, "gold", "Chest",
                  closed=True)
    M.xform(ring, loc=(0, -0.185, 0.7 * P.s))
    b.add(ring)
    for side in (1, -1):
        b.add(H.leg(P, side, "black_cloth", thigh=0.085, calf=0.07))
        b.add(H.boot(P, side, "boot", "leather_dark", height=0.18, size=1.12))
        b.add(H.arm(P, side, "skin_tan", upper=0.074, fore=0.066))
        b.add(A.vambrace(P, side, "leather_dark", "iron", length=0.12, r=0.072))
        b.add(H.fist(P, side, "skin_tan"))
    b.add(A.pauldron(P, 1, "iron", "iron_dark", 1.25, 1, spikes=3, spike_pal="steel"))
    b.add(A.belt(P, 0.57, "leather_dark", "gold", height=0.065, buckle_size=1.4))
    kilt = A.skirt(P, 0.56, 0.38, "red", None, 1.25, split_front=True)
    zb = 0.38 * P.s

    def tatter(p):
        # torn hem: zig-zag the bottom of the kilt
        if p.z < zb + 0.03:
            a = math.atan2(p.y, p.x)
            return V((p.x, p.y, p.z - 0.03 * (0.5 + 0.5 * math.sin(a * 9))))
        return p
    M.deform(kilt[0], tatter)
    b.add(kilt)
    b.add(H.head(P, "skin_tan", chin=1.4))
    b.add(A.helmet_shell(P, "red", "gold", grow=0.03, brow=0.075, cheek=-0.06, back=-0.09, opening=0.62, ridge=False))
    b.add(A.crest(P, "red_crest", height=0.08, width=0.03))
    b.faces(iris="iris_brown", mustache="thick", beard="big", hair_pal="hair_red", eye_scale=1.0, brow_pal="hair_red",
            brow_w=1.3, base_brow={"brow_in": -0.022, "brow_out": 0.01, "mouth_open": 0.03, "teeth": 0.8,
                                   "curve": -0.006})

    b.action("Idle", anim.idle(STYLE))
    b.action("Walk", anim.locomotion(STYLE, run=False))
    b.action("Run", anim.locomotion(STYLE, run=True, weight=1.1))
    b.action("HeavyAttack", heavy_attack(), impact=0.4)
    b.action("Rage", rage())
    b.action("Hit", anim.hit(STYLE))
    b.action("Death", anim.death(STYLE, backward=False))
    b.action("Victory", anim.victory(STYLE))
    b.slot("Attack", "HeavyAttack")
    b.slot("Skill", "Rage")

    b.pose("Idle")
    rh = b.socket("RightHandSocket")
    axe = W.axe("BerserkerAxe", length=0.75, head_w=0.3, head_h=0.32, s=1.35)
    b.hold(axe, "RightHandSocket", rh.translation, (0.15, -0.35, 0.92), (0, 1, 0), "Weapon_BerserkerAxe")
    return b
