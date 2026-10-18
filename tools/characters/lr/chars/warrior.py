"""Guerreiro (Ally_Warrior): blue/gold armour, plumed steel helmet, sword and heater shield."""
from mathutils import Vector

from .. import anim, armor as A, humanoid as H, mesh as M, rig, weapons as W
from ..character import Build

V = Vector
NAME = "Warrior"
STYLE = "sword_shield"


def build():
    P = H.Proportions(scale=1.0, width=1.07, head=1.12)
    arm = rig.build_armature(NAME + "_Rig", H.skeleton(P))
    b = Build(NAME, P, arm)
    b.height = 1.42

    b.add(H.torso(P, "blue"))
    b.add(A.breastplate(P, "steel", "gold", 0.61, 0.8, grow=0.008, emblem=M.star4(0.046, 0.014)))
    for side in (1, -1):
        b.add(H.leg(P, side, "cloth_brown"))
        b.add(H.boot(P, side, "boot", "leather", height=0.17, size=1.05))
        b.add(A.knee_guard(P, side))
        b.add(H.arm(P, side, "blue"))
        b.add(A.pauldron(P, side, "blue", "gold", 1.0, 2))
        b.add(A.vambrace(P, side, "steel", "gold"))
        b.add(H.fist(P, side, "leather"))
    b.add(A.belt(P, 0.57))
    b.add(A.skirt(P, 0.56, 0.41, "blue", "gold", 1.16, split_front=True))
    b.add(H.head(P, "skin"))
    b.add(A.helmet_shell(P, "steel", "gold", grow=0.03, brow=0.078, cheek=-0.075, back=-0.1, opening=0.58))
    b.add(A.plume(P, "blue_plume", "gold", height=0.17, length=0.27, thick=0.075))
    z = P.head_c.z + 0.078 * P.hs + 0.022
    y = P.surface_y(0, z) - 0.03
    orn = M.extrude_poly("HelmetBrowGem", [(0, 0.045), (0.032, 0.0), (0, -0.03), (-0.032, 0.0)], 0.016, "gold", "Head",
                         "XZ", 0.003, 1)
    M.xform(orn, loc=(0, y, z))
    b.add(orn)
    b.add(M.ellipsoid("HelmetGem", (0, y - 0.01, z + 0.002), (0.014, 0.008, 0.017), "gem_blue", "Head"))
    b.faces(iris="iris_brown", mustache="thick", beard="goatee", hair_pal="hair_dark", eye_scale=1.2,
            base_brow={"brow_in": -0.014, "brow_out": 0.006})

    # clips
    b.action("Idle", anim.idle(STYLE))
    b.action("Walk", anim.locomotion(STYLE, run=False))
    b.action("Run", anim.locomotion(STYLE, run=True))
    b.action("Attack", anim.sword_attack(STYLE), impact=0.27)
    b.action("AttackCombo", anim.sword_combo(STYLE), impact=0.27)
    b.action("Block", anim.shield_block(STYLE))
    b.action("Hit", anim.hit(STYLE))
    b.action("Death", anim.death(STYLE, backward=True))
    b.action("Victory", anim.victory(STYLE))

    b.slot("AttackAlt", "AttackCombo")
    b.slot("Skill", "AttackCombo")
    b.slot("Block", "Block")

    # weapons, placed in the idle pose
    b.pose("Idle")
    rh = b.socket("RightHandSocket")
    sword = W.sword("RoyalSword", length=0.5, width=0.085, gem="gem_blue", guard=0.19)
    b.hold(sword, "RightHandSocket", rh.translation, (0.32, -0.55, -0.77), (0.1, -0.8, 0.5), "Weapon_WarriorSword")
    la = b.arm.pose.bones["LeftLowerArm"]
    elbow = b.arm.matrix_world @ la.head
    wrist = b.arm.matrix_world @ la.tail
    center = elbow.lerp(wrist, 0.55) + V((0.07, -0.05, 0.02))
    shield = W.heater_shield("RoyalShield", 0.34, 0.42, "blue", "gold", emblem=M.cross_pattee(0.11, 0.05, 0.05))
    b.hold_facing(shield, "LeftHandSocket", center, (0, 0.18, 1), (0.42, -1, 0), "Weapon_WarriorShield")
    return b
