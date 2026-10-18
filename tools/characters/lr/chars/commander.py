"""Comandante Real (Ally_RoyalCommander): crowned leader, lion pauldrons, fur collar, fleur-de-lis cape."""
from mathutils import Vector

from .. import anim, armor as A, humanoid as H, mesh as M, rig, weapons as W
from ..character import Build

V = Vector
NAME = "RoyalCommander"
STYLE = "sword"


def build():
    P = H.Proportions(scale=1.22, width=1.18, head=1.05, hands=1.1)
    vfx = ("VFXSocket", (0, -0.12, P.chest_z + 0.05), (0, -0.2, P.chest_z + 0.05), "Chest", False)
    arm = rig.build_armature(NAME + "_Rig", H.skeleton(P, cape=True, extra=[vfx]))
    b = Build(NAME, P, arm)
    b.height = 1.82

    b.add(H.torso(P, "blue", chest=1.06))
    b.add(A.breastplate(P, "blue", "gold", 0.6, 0.805, grow=0.012, chest=1.06,
                        emblem=M.diamond(0.075, 0.1)))
    gem = M.extrude_poly("ChestGem", M.diamond(0.04, 0.056), 0.02, "gem_blue", "Chest", "XZ", 0.004, 1)
    M.xform(gem, loc=(0, -0.17 * P.s - 0.03, 0.715 * P.s))
    b.add(gem)
    b.add(A.fur_collar(P, "fur", 0.05))
    for side in (1, -1):
        b.add(H.leg(P, side, "blue_dark", thigh=0.08, calf=0.066))
        b.add(H.boot(P, side, "boot", "gold", height=0.17, size=1.08))
        b.add(A.knee_guard(P, side, "gold", "gold_dark", 1.05))
        b.add(H.arm(P, side, "blue"))
        b.add(A.lion_pauldron(P, side, "gold", "gold_dark", "gold"))
        b.add(A.vambrace(P, side, "blue", "gold", length=0.12, r=0.062))
        b.add(H.fist(P, side, "iron_dark"))
        b.add(H.cuff_ring(P, side, "wrist", "gold", r=0.064, h=0.04))
    b.add(A.belt(P, 0.57, "leather", "gold", height=0.055, buckle_size=1.25))
    b.add(A.skirt(P, 0.56, 0.38, "blue", "gold", 1.22, split_front=True))
    b.add(A.tabard(P, 0.555, 0.3, 0.075, "blue", "gold", True, emblem=M.fleur_de_lis(0.11)))
    b.add(A.cape(P, "blue", "gold", "blue_dark", length=0.98, width=0.26, flare=1.6, folds=3,
                 emblem=M.fleur_de_lis(0.2)))
    b.add(H.head(P, "skin_tan", chin=1.4))
    b.add(H.hair_cap(P, "hair_dark", grow=0.008, front=0.07))
    b.add(A.crown(P, "gold", "gem_blue", 5, 0.075, z_off=0.07))
    b.faces(iris="iris_brown", mustache="thick", beard="full", hair_pal="hair_brown", eye_scale=1.1,
            base_brow={"brow_in": -0.016, "brow_out": 0.006})

    b.action("Idle", anim.idle(STYLE))
    b.action("Walk", anim.locomotion(STYLE, run=False))
    b.action("Run", anim.locomotion(STYLE, run=True))
    b.action("Attack", anim.sword_attack(STYLE), impact=0.27)
    b.action("Command", anim.command(STYLE))
    b.action("Rally", anim.rally(STYLE))
    b.action("Buff", anim.buff(STYLE))
    b.action("Hit", anim.hit(STYLE))
    b.action("Death", anim.death(STYLE, backward=True))
    b.action("Victory", anim.victory(STYLE))
    b.slot("AttackAlt", "Command")
    b.slot("Skill", "Rally")

    b.pose("Idle")
    rh = b.socket("RightHandSocket")
    sword = W.sword("CommanderSword", length=0.6, width=0.1, gem="gem_blue", guard=0.24, s=1.05)
    b.hold(sword, "RightHandSocket", rh.translation, (0.32, -0.55, -0.77), (0.1, -0.8, 0.5), "Weapon_CommanderSword")
    return b
