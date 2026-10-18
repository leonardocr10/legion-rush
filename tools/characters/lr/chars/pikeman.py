"""Piqueiro (Enemy_Pikeman): iron Spartan helmet with red crest, red scarf, dark armour, long pike with red tassel."""
from mathutils import Vector

from .. import anim, armor as A, humanoid as H, mesh as M, rig, weapons as W
from ..character import Build

V = Vector
NAME = "Pikeman"
STYLE = "spear_shield"


def build():
    P = H.Proportions(scale=1.0, width=1.1, head=1.1)
    arm = rig.build_armature(NAME + "_Rig", H.skeleton(P))
    b = Build(NAME, P, arm)
    b.height = 1.5

    b.add(H.torso(P, "red"))
    b.add(A.breastplate(P, "iron_dark", "red_dark", 0.62, 0.78, grow=0.008))
    b.add(A.fur_collar(P, "red", 0.04))
    for sx in (1, -1):
        pts = [(-0.13 * sx, -0.11, 0.79), (0.0, -0.15, 0.68), (0.14 * sx, -0.14, 0.58)]
        b.add(M.tube("Strap" + str(sx), pts, 0.011, 6, "leather_dark", "Spine,Chest", radii=[(0.018, 0.007)] * 3))
    for side in (1, -1):
        b.add(H.leg(P, side, "black_cloth"))
        b.add(H.boot(P, side, "boot", "leather_dark", height=0.18, size=1.05))
        b.add(A.knee_guard(P, side, "iron", "gold", 0.95))
        b.add(H.arm(P, side, "skin_tan", upper=0.058, fore=0.052))
        b.add(A.pauldron(P, side, "iron", "gold", 0.95, 2, spikes=1))
        b.add(A.vambrace(P, side, "leather_dark", "iron_dark", length=0.12))
        b.add(H.fist(P, side, "leather_dark"))
    b.add(A.belt(P, 0.57, "leather", "gold"))
    b.add(A.skirt(P, 0.56, 0.4, "red", None, 1.18, split_front=True))
    b.add(A.tabard(P, 0.555, 0.3, 0.075, "red", None, True, emblem=M.beast_emblem(0.11), emblem_pal="black_cloth"))
    b.add(H.head(P, "skin_tan"))
    b.add(A.helmet_shell(P, "iron", "gold", grow=0.03, brow=0.06, cheek=-0.1, back=-0.11, opening=0.5, ridge=False))
    b.add(A.crest(P, "red_crest", height=0.09, width=0.028))
    b.faces(iris="iris_brown", mustache="thick", beard="short", hair_pal="hair_dark", eye_scale=1.05,
            base_brow={"brow_in": -0.018, "brow_out": 0.008})

    b.action("Idle", anim.idle(STYLE))
    b.action("Walk", anim.locomotion(STYLE, run=False))
    b.action("Run", anim.locomotion(STYLE, run=True))
    b.action("SpearAttack", anim.spear_attack(STYLE, two_hand=True), impact=0.32)
    b.action("Thrust", anim.spear_thrust(STYLE, two_hand=True, lunge=1.2), impact=0.32)
    b.action("Hit", anim.hit(STYLE))
    b.action("Death", anim.death(STYLE, backward=True))
    b.action("Victory", anim.victory(STYLE))
    b.slot("Attack", "Thrust")
    b.slot("AttackAlt", "SpearAttack")

    b.pose("Idle")
    rh = b.socket("RightHandSocket")
    pike = W.spear("Pike", length=1.75, head_len=0.24, shaft="wood_dark", head="steel", collar="gold", tassel="red",
                   butt=0.3, head_w=0.08)
    b.hold(pike, "RightHandSocket", rh.translation, (0, 0.04, 1), (1, 0, 0), "Weapon_Pike")
    return b
