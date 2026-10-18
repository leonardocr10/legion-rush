"""Mago Arcano (Ally_ArcaneMage): old wizard, blue hooded robe with gold trim, white beard, crystal staff, tome.
Reference: projeto/Ficha de Personagens (no dedicated sheet was provided for this unit)."""
from mathutils import Vector

from .. import anim, armor as A, humanoid as H, mesh as M, rig, weapons as W
from ..character import Build

V = Vector
NAME = "ArcaneMage"
STYLE = "staff"


def build():
    P = H.Proportions(scale=1.0, width=1.0, head=1.12)
    arm = rig.build_armature(NAME + "_Rig", H.skeleton(P, cape=True))
    b = Build(NAME, P, arm)
    b.height = 1.45

    b.add(H.torso(P, "blue"))
    # front panel (cream) with gold borders
    b.add(A.tabard(P, 0.8, 0.3, 0.06, "cream", "gold", True, bind="Spine,Chest,Hips"))
    b.add(A.belt(P, 0.57, "leather", "gold", height=0.045))
    b.add(A.skirt(P, 0.56, 0.08, "blue", "gold", 1.55,
                  bind="Hips,LeftUpperLeg,RightUpperLeg,LeftLowerLeg,RightLowerLeg"))
    for side in (1, -1):
        b.add(H.boot(P, side, "leather_dark", "leather", height=0.1, size=0.95, cuff=False, toe_up=0.02))
        b.add(H.arm(P, side, "blue", upper=0.062, fore=0.074))
        b.add(H.cuff_ring(P, side, "wrist", "gold", r=0.082, h=0.03, flare=1.12))
        b.add(H.fist(P, side, "skin"))
    b.add(H.head(P, "skin"))
    b.add(A.hood(P, "blue", "gold", "blue_dark", grow=0.035, point=0.06, back_drop=0.22, tip_back=0.16))
    b.add(A.cape(P, "blue", "gold", "blue_dark", length=0.7, width=0.22, flare=1.35, folds=2))
    b.faces(iris="iris_blue", brow_pal="white", mustache="handlebar", beard="big", hair_pal="white",
            eye_scale=1.05, brow_w=1.3, base_brow={"brow_in": -0.01, "brow_out": -0.004})

    b.action("Idle", anim.idle(STYLE))
    b.action("Walk", anim.locomotion(STYLE, run=False))
    b.action("Run", anim.locomotion(STYLE, run=True))
    b.action("Cast", anim.staff_cast(STYLE))
    b.action("Attack", anim.staff_attack(STYLE), impact=0.34)
    b.action("AreaSpell", anim.staff_area(STYLE), impact=0.5)
    b.action("Hit", anim.hit(STYLE))
    b.action("Death", anim.death(STYLE, backward=False))
    b.action("Victory", anim.victory(STYLE))
    b.slot("AttackAlt", "AreaSpell")
    b.slot("Skill", "Cast")

    b.pose("Idle")
    rh = b.socket("RightHandSocket")
    staff = W.staff("ArcaneStaff", length=1.25, shaft="wood_dark", metal="gold", crystal="glow_blue", crown="claw",
                    butt=0.3, crystal_size=1.25)
    b.hold(staff, "RightHandSocket", rh.translation, (0.0, -0.08, 1), (1, 0, 0), "Weapon_ArcaneStaff")
    tip = staff.matrix_world @ V((0, 0, staff["tip"]))
    b.socket_at("ProjectileSpawn", "RightHand", tip)
    b.socket_at("VFXSocket", "RightHand", tip)
    tome = W.book("ArcaneTome", 0.13, 0.16, 0.045, "blue", "gold", emblem=M.star4(0.035, 0.012))
    M.xform(tome, rot=(0, 0, 90))
    M.xform(tome, loc=(0.17 * P.w, -0.03, 0.5))
    b.prop(tome, "Hips", "Prop_ArcaneTome")
    return b
