"""Mago Sombrio (Enemy_DarkMage): dark purple hooded robe with red trim, spiked shoulders, black beard,
arcane staff with a red flame."""
from mathutils import Vector

from .. import anim, armor as A, humanoid as H, mesh as M, rig, weapons as W
from ..character import Build

V = Vector
NAME = "DarkMage"
STYLE = "staff"


def build():
    P = H.Proportions(scale=1.0, width=1.0, head=1.1)
    arm = rig.build_armature(NAME + "_Rig", H.skeleton(P, cape=True))
    b = Build(NAME, P, arm)
    b.height = 1.5

    b.add(H.torso(P, "purple_dark"))
    b.add(A.tabard(P, 0.8, 0.3, 0.055, "red", "gold", True, emblem=M.diamond(0.05, 0.07), emblem_pal="red_dark",
                   bind="Spine,Chest,Hips"))
    b.add(A.belt(P, 0.57, "leather_dark", "gold", height=0.05))
    b.add(A.skirt(P, 0.56, 0.08, "purple_dark", "red", 1.55,
                  bind="Hips,LeftUpperLeg,RightUpperLeg,LeftLowerLeg,RightLowerLeg"))
    for side in (1, -1):
        b.add(H.boot(P, side, "black_cloth", "iron_dark", height=0.1, size=0.95, cuff=False, toe_up=0.03))
        b.add(H.arm(P, side, "purple_dark", upper=0.062, fore=0.074))
        b.add(H.cuff_ring(P, side, "wrist", "red", r=0.082, h=0.03, flare=1.12))
        b.add(A.pauldron(P, side, "iron_dark", "gold", 0.9, 1, spikes=2, spike_pal="gold"))
        b.add(H.fist(P, side, "leather_dark"))
    b.add(H.head(P, "skin_tan"))
    b.add(A.hood(P, "purple_dark", "red", "black_cloth", grow=0.035, point=0.1, back_drop=0.22, tip_back=0.05))
    b.add(A.cape(P, "purple_dark", "red", "black_cloth", length=0.75, width=0.22, flare=1.4, folds=3))
    b.faces(iris="iris_red", mustache="thick", beard="full", hair_pal="hair_dark", eye_scale=1.0,
            base_brow={"brow_in": -0.022, "brow_out": 0.01, "curve": -0.004})

    b.action("Idle", anim.idle(STYLE))
    b.action("Walk", anim.locomotion(STYLE, run=False))
    b.action("Run", anim.locomotion(STYLE, run=True))
    b.action("Cast", anim.staff_cast(STYLE))
    b.action("Attack", anim.staff_attack(STYLE), impact=0.34)
    b.action("AreaAttack", anim.staff_area(STYLE), impact=0.5)
    b.action("Hit", anim.hit(STYLE))
    b.action("Death", anim.death(STYLE, backward=False))
    b.action("Victory", anim.victory(STYLE))
    b.slot("AttackAlt", "AreaAttack")
    b.slot("Skill", "Cast")

    b.pose("Idle")
    rh = b.socket("RightHandSocket")
    staff = W.staff("DarkStaff", length=1.25, shaft="black_cloth", metal="gold", crystal="glow_red", crown="claw",
                    butt=0.3, crystal_size=1.25)
    b.hold(staff, "RightHandSocket", rh.translation, (0.0, -0.08, 1), (1, 0, 0), "Weapon_DarkStaff")
    tip = staff.matrix_world @ V((0, 0, staff["tip"]))
    b.socket_at("ProjectileSpawn", "RightHand", tip)
    lh = b.socket("LeftHand").translation
    b.socket_at("VFXSocket", "LeftHand", lh + V((0, -0.06, 0.0)))
    return b
