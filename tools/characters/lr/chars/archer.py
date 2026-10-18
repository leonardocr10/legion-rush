"""Arqueira Real (Ally_RoyalArcher): blue hood and short cape, golden bow with blue crystal, quiver on the back."""
import math
from mathutils import Vector

from .. import anim, armor as A, humanoid as H, mesh as M, rig, weapons as W
from ..character import Build

V = Vector
NAME = "RoyalArcher"
STYLE = "bow"

FEMALE_TORSO = [(0.43, 0.17, 0.12), (0.5, 0.172, 0.122), (0.58, 0.138, 0.108), (0.66, 0.158, 0.124),
                (0.72, 0.17, 0.132), (0.78, 0.158, 0.11), (0.82, 0.098, 0.078), (0.85, 0.05, 0.048),
                (0.89, 0.048, 0.046)]


ROYAL = dict(name=NAME, main="blue", dark="blue_dark", light="blue_light", trim="gold", gem="gem_blue",
             glow="glow_blue", hair="hair_brown", iris="iris_blue", boot_cuff="blue", pauldron="steel",
             string="cream", fletch="blue_light", weapon="Royal", prop="Royal", emblem=None)


def build(c=None):
    c = dict(ROYAL, **(c or {}))
    NAME = c["name"]
    P = H.Proportions(scale=0.97, width=0.9, female=True, head=1.2, hands=0.85)
    hand = V((P.hand_end.x, P.hand_end.y, P.hand_end.z))
    extra = [("ProjectileSpawn", (hand.x + 0.01, -0.03, hand.z + 0.02), (hand.x + 0.01, -0.12, hand.z + 0.02),
              "LeftHand", False)]
    arm = rig.build_armature(NAME + "_Rig", H.skeleton(P, cape=True, extra=extra))
    b = Build(NAME, P, arm)
    b.height = 1.38

    b.add(H.torso(P, c["main"], sections=FEMALE_TORSO))
    # gold trims of the bodice
    for z, a, bb in ((0.79, 0.16, 0.112), (0.6, 0.142, 0.11)):
        b.add(M.tube("Bodice" + str(z), M.ring_path((0, 0.004, z * P.s), a * P.w + 0.006, bb * P.s + 0.006, 20,
                                                     arc=(math.pi * 1.05, math.pi * 1.95)), 0.007, 6, c["trim"],
                     "Spine,Chest"))
    # crossed leather straps
    for sx in (1, -1):
        pts = [(-0.11 * sx * P.w, -0.08, 0.8 * P.s), (0.0, -0.135, 0.7 * P.s), (0.12 * sx * P.w, -0.12, 0.58 * P.s)]
        b.add(M.tube("Strap" + str(sx), pts, 0.011, 6, "leather", "Spine,Chest", radii=[(0.016, 0.006)] * 3))
    b.add(M.ellipsoid("StrapGem", (0, -0.142, 0.7 * P.s), (0.02, 0.01, 0.024), c["gem"], "Chest"))
    for side in (1, -1):
        b.add(H.leg(P, side, "skin", thigh=0.066, calf=0.052))
        b.add(H.boot(P, side, "boot", c["boot_cuff"], height=0.215, size=0.92))
        b.add(A.knee_guard(P, side, c["main"], c["trim"], 0.82))
        b.add(H.arm(P, side, "skin", upper=0.045, fore=0.041))
        b.add(A.pauldron(P, side, c["pauldron"], c["trim"], 0.74, 1, tilt=22))
        b.add(A.vambrace(P, side, "leather", c["trim"], length=0.1, r=0.048))
        b.add(H.fist(P, side, "leather_dark"))
    b.add(A.belt(P, 0.56, height=0.04, buckle_size=0.85))
    b.add(A.skirt(P, 0.555, 0.42, c["main"], c["trim"], 1.3, top_a=0.15, top_b=0.115))
    b.add(H.head(P, "skin"))
    b.add(H.hair_long(P, c["hair"], length=0.2, bangs=False))
    b.add(H.hair_cap(P, c["hair"], grow=0.01, front=0.085))
    b.add(A.hood(P, c["main"], c["trim"], c["dark"], grow=0.035, point=0.025, back_drop=0.2, tip_back=0.12))
    b.add(A.cape(P, c["main"], c["trim"], c["dark"], length=0.5, width=0.2, flare=1.45, folds=3, emblem=c["emblem"],
                 emblem_pal="black_cloth"))
    b.faces(iris=c["iris"], brow_pal=c["hair"], lashes=True, eye_scale=1.35, brow_w=0.6,
            base_brow={"brow_in": 0.002, "brow_out": 0.006, "mouth_open": 0.009, "mouth_w": 0.028, "curve": 0.006})

    b.action("Idle", anim.idle(STYLE))
    b.action("Walk", anim.locomotion(STYLE, run=False))
    b.action("Run", anim.locomotion(STYLE, run=True))
    b.action("DrawBow", anim.bow_draw())
    b.action("Aim", anim.bow_aim())
    b.action("Shoot", anim.bow_shoot(), impact=0.48)
    b.action("Reload", anim.bow_reload())
    b.action("Dodge", anim.dodge(1))
    b.action("Hit", anim.hit(STYLE))
    b.action("Death", anim.death(STYLE, backward=True))
    b.action("Victory", anim.victory(STYLE, raise_arm="Left"))
    b.slot("Attack", "Shoot")
    b.slot("Skill", "Reload")
    b.slot("Block", "Dodge")

    # bow: placed vertical in the aiming pose (grip in the left fist)
    b.pose("Aim", 20)
    lh = b.socket("LeftHandSocket")
    bow = W.bow(c["weapon"] + "Bow", h=0.66, depth=0.11, wood="gold", gem=c["glow"], string=c["string"])
    b.hold(bow, "LeftHandSocket", lh.translation, (0, 0, 1), (1, 0, 0), "Weapon_" + c["weapon"] + "Bow")
    # quiver on the back, tilted toward the right shoulder
    b.pose("Idle")
    q = W.quiver(c["prop"] + "Quiver", h=0.3, r=0.052, body=c["main"] if c["main"] != "red" else "leather", trim=c["trim"],
                 fletch=c["fletch"], arrows=5)
    back = b.socket("BackSocket").translation
    M.xform(q, rot=(0, -28, 0))
    M.xform(q, loc=(back.x + 0.05, back.y + 0.035, back.z - 0.22))
    b.prop(q, "Chest", "Prop_" + c["prop"] + "Quiver")
    b.loose_prop(W.arrow(c["weapon"] + "Arrow", fletch=c["fletch"], glow=c["glow"]), "Weapon_" + c["weapon"] + "Arrow")
    return b
