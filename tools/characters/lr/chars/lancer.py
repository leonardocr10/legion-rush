"""Lanceiro Real (Ally_Spearman): plumed helmet, blue/steel armour, fleur-de-lis tabard, star shield and royal spear."""
from mathutils import Vector

from .. import anim, armor as A, humanoid as H, mesh as M, rig, weapons as W
from ..character import Build

V = Vector
NAME = "Spearman"
STYLE = "spear_shield"


def build():
    P = H.Proportions(scale=1.0, width=1.05, head=1.1)
    arm = rig.build_armature(NAME + "_Rig", H.skeleton(P, cape=True))
    b = Build(NAME, P, arm)
    b.height = 1.45

    b.add(H.torso(P, "blue"))
    b.add(A.breastplate(P, "steel", "gold", 0.62, 0.8, grow=0.008))
    for sx in (1, -1):
        pts = [(-0.13 * sx, -0.1, 0.8), (0.0, -0.145, 0.69), (0.14 * sx, -0.13, 0.58)]
        b.add(M.tube("Strap" + str(sx), pts, 0.011, 6, "leather", "Spine,Chest", radii=[(0.018, 0.007)] * 3))
    for side in (1, -1):
        b.add(H.leg(P, side, "steel_dark"))
        b.add(H.boot(P, side, "boot", "steel", height=0.17, size=1.04))
        b.add(A.knee_guard(P, side, "steel", "gold"))
        b.add(H.arm(P, side, "blue"))
        b.add(A.pauldron(P, side, "steel", "gold", 1.0, 2))
        b.add(A.vambrace(P, side, "steel", "gold"))
        b.add(H.fist(P, side, "leather"))
    b.add(A.belt(P, 0.57))
    b.add(A.skirt(P, 0.56, 0.42, "blue", "gold", 1.15, split_front=True))
    b.add(A.tabard(P, 0.555, 0.31, 0.07, "blue", "gold", True, emblem=M.fleur_de_lis(0.1)))
    b.add(A.cape(P, "blue", "gold", "blue_dark", length=0.55, width=0.21, flare=1.35, folds=2))
    b.add(H.head(P, "skin_tan"))
    b.add(A.helmet_shell(P, "steel", "gold", grow=0.03, brow=0.075, cheek=-0.07, back=-0.1, opening=0.6))
    b.add(A.plume(P, "blue_plume", "gold", height=0.16, length=0.3, thick=0.07))
    z = P.head_c.z + 0.075 * P.hs + 0.02
    orn = M.extrude_poly("HelmetOrn", M.star4(0.032, 0.011), 0.014, "gold", "Head", "XZ", 0.003, 1)
    M.xform(orn, loc=(0, P.surface_y(0, z) - 0.03, z))
    b.add(orn)
    b.faces(iris="iris_brown", mustache="thick", beard="short", hair_pal="hair_dark", eye_scale=1.15,
            base_brow={"brow_in": -0.012, "brow_out": 0.006})

    b.action("Idle", anim.idle(STYLE))
    b.action("Walk", anim.locomotion(STYLE, run=False))
    b.action("Run", anim.locomotion(STYLE, run=True))
    b.action("SpearThrust", anim.spear_thrust(STYLE), impact=0.32)
    b.action("Attack", anim.spear_attack(STYLE), impact=0.32)
    b.action("Defend", anim.spear_defend(STYLE))
    b.action("Charge", anim.spear_charge(STYLE))
    b.action("Hit", anim.hit(STYLE))
    b.action("Death", anim.death(STYLE, backward=True))
    b.action("Victory", anim.victory(STYLE))
    b.slot("Attack", "SpearThrust")
    b.slot("AttackAlt", "Attack")
    b.slot("Skill", "Charge")
    b.slot("Block", "Defend")

    b.pose("Idle")
    rh = b.socket("RightHandSocket")
    spear = W.spear("RoyalSpear", length=1.45, head_len=0.2, shaft="wood", head="steel", collar="gold",
                    stripes="blue", butt=0.28)
    b.hold(spear, "RightHandSocket", rh.translation, (0, 0.04, 1), (1, 0, 0), "Weapon_RoyalSpear")
    la = b.arm.pose.bones["LeftLowerArm"]
    center = (b.arm.matrix_world @ la.head).lerp(b.arm.matrix_world @ la.tail, 0.55) + V((0.07, -0.05, 0.02))
    shield = W.heater_shield("RoyalStarShield", 0.33, 0.42, "blue", "gold", emblem=M.star4(0.12, 0.035))
    b.hold_facing(shield, "LeftHandSocket", center, (0, 0.18, 1), (0.42, -1, 0), "Weapon_RoyalStarShield")
    return b
