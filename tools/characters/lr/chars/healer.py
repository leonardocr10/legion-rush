"""Curandeira Real (Ally_Healer): white hood with gold tiara, auburn hair, white/blue robe with fleur-de-lis,
sun staff with blue crystal, holy book and potion pouch."""
import math
from mathutils import Vector

from .. import anim, armor as A, humanoid as H, mesh as M, rig, weapons as W
from ..character import Build
from .archer import FEMALE_TORSO

V = Vector
NAME = "Healer"
STYLE = "staff"


def build():
    P = H.Proportions(scale=0.97, width=0.9, female=True, head=1.2, hands=0.85)
    arm = rig.build_armature(NAME + "_Rig", H.skeleton(P, cape=True))
    b = Build(NAME, P, arm)
    b.height = 1.4

    b.add(H.torso(P, "white", sections=FEMALE_TORSO))
    b.add(A.breastplate(P, "blue", "gold", 0.6, 0.78, grow=0.006, chest=0.86,
                        emblem=M.cross_pattee(0.03, 0.016, 0.014)))
    b.add(A.belt(P, 0.565, "leather", "gold", height=0.04, buckle_size=0.85))
    pouch = M.box("Pouch", (0.07, 0.04, 0.06), (0.13 * P.w, -0.09, 0.52), "leather", "Hips", 0.008)
    b.add(pouch)
    for i, x in enumerate((-0.1, -0.14)):
        b.add(M.ellipsoid(f"Potion{i}", (x * P.w, -0.1, 0.51), (0.016, 0.016, 0.026), "glow_blue", "Hips", 8, 6))
        b.add(M.tube(f"PotionNeck{i}", [(x * P.w, -0.1, 0.535), (x * P.w, -0.1, 0.555)], 0.007, 6, "cream", "Hips"))
    b.add(A.skirt(P, 0.56, 0.08, "white", "gold", 1.6, top_a=0.15, top_b=0.115,
                  bind="Hips,LeftUpperLeg,RightUpperLeg,LeftLowerLeg,RightLowerLeg"))
    b.add(A.tabard(P, 0.555, 0.12, 0.075, "blue", "gold", True, emblem=M.fleur_de_lis(0.1),
                   bind="Hips,LeftUpperLeg,RightUpperLeg"))
    for side in (1, -1):
        b.add(H.boot(P, side, "boot", "gold", height=0.12, size=0.9, cuff=False))
        b.add(H.arm(P, side, "white", upper=0.05, fore=0.064))
        b.add(A.pauldron(P, side, "blue", "gold", 0.7, 1, tilt=20))
        b.add(H.cuff_ring(P, side, "wrist", "gold", r=0.07, h=0.03, flare=1.12))
        b.add(H.fist(P, side, "skin"))
    b.add(H.head(P, "skin"))
    b.add(H.hair_long(P, "hair_auburn", length=0.26, bangs=False, volume=1.08))
    b.add(H.hair_cap(P, "hair_auburn", grow=0.01, front=0.085))
    b.add(A.hood(P, "white", "gold", "blue", grow=0.035, point=0.02, back_drop=0.2, tip_back=0.1))
    z = P.head_c.z + 0.135 * P.hs
    tiara = M.extrude_poly("Tiara", M.fleur_de_lis(0.07), 0.012, "gold", "Head", "XZ", 0.002, 1)
    M.xform(tiara, loc=(0, P.surface_y(0, z - 0.03) - 0.05, z))
    b.add(tiara)
    b.add(A.cape(P, "blue", "gold", "blue_dark", length=0.8, width=0.2, flare=1.5, folds=3,
                 emblem=M.fleur_de_lis(0.16)))
    b.faces(iris="iris_blue", brow_pal="hair_auburn", lashes=True, eye_scale=1.35, brow_w=0.6,
            base_brow={"brow_in": 0.006, "brow_out": 0.004, "mouth_open": 0.01, "mouth_w": 0.03, "curve": 0.01})

    b.action("Idle", anim.idle(STYLE))
    b.action("Walk", anim.locomotion(STYLE, run=False))
    b.action("Run", anim.locomotion(STYLE, run=True))
    b.action("Heal", anim.heal(STYLE), impact=0.2)
    b.action("AreaHeal", anim.area_heal(STYLE), impact=0.6)
    b.action("Cast", anim.staff_cast(STYLE), impact=0.45)
    b.action("Shield", anim.shield_spell(STYLE))
    b.action("Revive", anim.revive(STYLE))
    b.action("Hit", anim.hit(STYLE))
    b.action("Death", anim.death(STYLE, backward=True))
    b.action("Victory", anim.victory(STYLE))
    b.slot("Attack", "Heal")
    b.slot("AttackAlt", "Cast")
    b.slot("Skill", "AreaHeal")
    b.slot("Block", "Shield")

    b.pose("Idle")
    rh = b.socket("RightHandSocket")
    staff = W.staff("SunStaff", length=1.2, shaft="gold", metal="gold", crystal="glow_blue", crown="ring",
                    butt=0.3, crystal_size=1.1)
    b.hold(staff, "RightHandSocket", rh.translation, (0.0, -0.06, 1), (1, 0, 0), "Weapon_SunStaff")
    tip = staff.matrix_world @ V((0, 0, staff["tip"]))
    b.socket_at("VFXSocket", "RightHand", tip)
    lh = b.socket("LeftHand").translation
    b.socket_at("ProjectileSpawn", "LeftHand", lh + V((0, -0.06, -0.02)))
    book = W.book("HolyBook", 0.13, 0.16, 0.045, "blue", "gold", emblem=M.fleur_de_lis(0.07))
    M.xform(book, rot=(0, 0, -90))
    M.xform(book, loc=(-0.17 * P.w, -0.02, 0.5))
    b.prop(book, "Hips", "Prop_HolyBook")
    return b
