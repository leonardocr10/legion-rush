"""Soldado inimigo (Enemy_Soldier): red peaked helmet, dark iron pauldrons, red tunic, red star shield and sword."""
from mathutils import Vector

from .. import anim, armor as A, humanoid as H, mesh as M, rig, weapons as W
from ..character import Build

V = Vector
NAME = "EnemySoldier"
STYLE = "sword_shield"


def build():
    P = H.Proportions(scale=1.0, width=1.08, head=1.12)
    arm = rig.build_armature(NAME + "_Rig", H.skeleton(P))
    b = Build(NAME, P, arm)
    b.height = 1.45

    b.add(H.torso(P, "red"))
    for sx in (1, -1):
        pts = [(-0.13 * sx, -0.1, 0.8), (0.0, -0.145, 0.69), (0.14 * sx, -0.13, 0.58)]
        b.add(M.tube("Strap" + str(sx), pts, 0.011, 6, "leather_dark", "Spine,Chest", radii=[(0.018, 0.007)] * 3))
    for side in (1, -1):
        b.add(H.leg(P, side, "black_cloth"))
        b.add(H.boot(P, side, "boot", "leather_dark", height=0.17, size=1.05))
        b.add(H.arm(P, side, "red"))
        b.add(A.pauldron(P, side, "iron", "iron_dark", 1.0, 2))
        b.add(A.vambrace(P, side, "leather_dark", "iron_dark"))
        b.add(H.fist(P, side, "leather"))
    b.add(A.belt(P, 0.57, "leather", "gold"))
    b.add(A.skirt(P, 0.56, 0.4, "red", "red_dark", 1.18, split_front=True))
    b.add(A.tabard(P, 0.555, 0.32, 0.065, "red_dark", None, True))
    b.add(H.head(P, "skin_tan"))
    b.add(A.helmet_shell(P, "red", "iron_dark", grow=0.03, brow=0.07, cheek=-0.07, back=-0.1, opening=0.6,
                         peak=0.07, peak_back=0.02, ridge_pal="red_dark"))
    b.faces(iris="iris_brown", mustache="thick", beard="goatee", hair_pal="hair_dark", eye_scale=1.1,
            base_brow={"brow_in": -0.018, "brow_out": 0.008})

    b.action("Idle", anim.idle(STYLE))
    b.action("Walk", anim.locomotion(STYLE, run=False))
    b.action("Run", anim.locomotion(STYLE, run=True))
    b.action("Attack", anim.sword_attack(STYLE), impact=0.27)
    b.action("Block", anim.shield_block(STYLE))
    b.action("Hit", anim.hit(STYLE))
    b.action("Death", anim.death(STYLE, backward=True))
    b.action("Victory", anim.victory(STYLE))
    b.slot("Block", "Block")

    b.pose("Idle")
    rh = b.socket("RightHandSocket")
    sword = W.sword("SoldierSword", length=0.42, width=0.07, gem=None, guard=0.16, guard_pal="gold")
    b.hold(sword, "RightHandSocket", rh.translation, (0.32, -0.55, -0.77), (0.1, -0.8, 0.5), "Weapon_EnemySword")
    la = b.arm.pose.bones["LeftLowerArm"]
    center = (b.arm.matrix_world @ la.head).lerp(b.arm.matrix_world @ la.tail, 0.55) + V((0.07, -0.05, 0.02))
    shield = W.heater_shield("SoldierShield", 0.34, 0.42, "red", "steel", emblem=M.star4(0.1, 0.03))
    b.hold_facing(shield, "LeftHandSocket", center, (0, 0.18, 1), (0.42, -1, 0), "Weapon_EnemyShield")
    return b
