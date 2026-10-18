"""Guerreiro Pesado (Enemy_HeavyWarrior): bulky dark-iron armour, horned helmet with red crest, red tabard with the
beast emblem, heavy sword and reinforced red shield."""
from mathutils import Vector

from .. import anim, armor as A, humanoid as H, mesh as M, rig, weapons as W
from ..character import Build

V = Vector
NAME = "HeavyWarrior"
STYLE = "sword_shield"


def build():
    P = H.Proportions(scale=1.1, width=1.25, head=1.05, hands=1.15)
    arm = rig.build_armature(NAME + "_Rig", H.skeleton(P))
    b = Build(NAME, P, arm)
    b.height = 1.62

    b.add(H.torso(P, "red", chest=1.1, belly=1.1))
    b.add(A.breastplate(P, "iron", "gold", 0.6, 0.81, grow=0.014, chest=1.1, emblem=M.diamond(0.06, 0.08)))
    for sx in (1, -1):
        pts = [(-0.15 * sx * P.w, -0.15, 0.82 * P.s), (0.0, -0.19, 0.7 * P.s), (0.16 * sx * P.w, -0.17, 0.58 * P.s)]
        b.add(M.tube("Strap" + str(sx), pts, 0.013, 6, "red", "Spine,Chest", radii=[(0.02, 0.008)] * 3))
    for side in (1, -1):
        b.add(H.leg(P, side, "iron_dark", thigh=0.085, calf=0.072))
        b.add(H.boot(P, side, "iron", "gold", height=0.18, size=1.12))
        b.add(A.knee_guard(P, side, "iron", "gold", 1.15))
        b.add(H.arm(P, side, "red", upper=0.068, fore=0.06))
        b.add(A.pauldron(P, side, "iron", "gold", 1.3, 2, spikes=3, spike_pal="steel"))
        b.add(A.vambrace(P, side, "iron", "gold", length=0.12, r=0.066))
        b.add(H.fist(P, side, "iron_dark"))
    b.add(A.belt(P, 0.57, "leather_dark", "gold", height=0.06, buckle_size=1.3))
    b.add(A.skirt(P, 0.56, 0.38, "iron_dark", "gold", 1.2, split_front=True))
    b.add(A.tabard(P, 0.555, 0.28, 0.085, "red", None, True, emblem=M.beast_emblem(0.13), emblem_pal="black_cloth"))
    b.add(H.head(P, "skin_tan", chin=1.3))
    b.add(A.helmet_shell(P, "iron", "gold", grow=0.03, brow=0.065, cheek=-0.08, back=-0.1, opening=0.56,
                         ridge=False))
    b.add(A.crest(P, "red_crest", height=0.08, width=0.03))
    b.add(A.horns(P, "cream", 1.1))
    b.faces(iris="iris_brown", mustache="thick", beard="full", hair_pal="hair_dark", eye_scale=1.0,
            base_brow={"brow_in": -0.02, "brow_out": 0.008})

    b.action("Idle", anim.idle(STYLE))
    b.action("Walk", anim.locomotion(STYLE, run=False, weight=0.8))
    b.action("Run", anim.locomotion(STYLE, run=True, duration=0.7, weight=0.85))
    b.action("Attack", anim.sword_attack(STYLE), impact=0.27)
    b.action("Defend", anim.shield_block(STYLE, hold=0.25))
    b.action("ShieldBlock", anim.shield_block(STYLE, hold=0.7))
    b.action("Hit", anim.hit(STYLE))
    b.action("Death", anim.death(STYLE, backward=False))
    b.action("Victory", anim.victory(STYLE))
    b.slot("Block", "ShieldBlock")
    b.slot("Skill", "Defend")

    b.pose("Idle")
    rh = b.socket("RightHandSocket")
    sword = W.sword("HeavySword", length=0.46, width=0.11, gem="gem_red", guard=0.2, grip_pal="red_dark", s=1.1)
    b.hold(sword, "RightHandSocket", rh.translation, (0.32, -0.55, -0.77), (0.1, -0.8, 0.5), "Weapon_HeavySword")
    la = b.arm.pose.bones["LeftLowerArm"]
    center = (b.arm.matrix_world @ la.head).lerp(b.arm.matrix_world @ la.tail, 0.55) + V((0.09, -0.07, 0.02))
    shield = W.heater_shield("HeavyShield", 0.42, 0.54, "red", "steel", emblem=M.star4(0.13, 0.04), rim_r=0.022)
    b.hold_facing(shield, "LeftHandSocket", center, (0, 0.18, 1), (0.42, -1, 0), "Weapon_HeavyShield")
    return b
