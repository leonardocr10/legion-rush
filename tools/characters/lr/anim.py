"""Clip authoring for the humanoid rig.

Poses are dicts of bone -> (x, y, z) degrees in the rest-aligned armature frame (see rig.py):
  UpperArm X-  raise forward | LowerArm X- bend elbow | UpperLeg X- thigh forward | LowerLeg X+ bend knee
  Spine/Chest X+ lean forward | Z+ twist toward the character's left | Foot X- toes up
Left arm: Y- abducts outward. Right arm: Y+ abducts outward.
"""
import math

from . import rig


def merge(*poses):
    out = {}
    for p in poses:
        for k, v in p.items():
            if k in out and not k.startswith("_"):
                a = out[k]
                out[k] = (a[0] + v[0], a[1] + v[1], a[2] + v[2])
            elif (k == "_hips" or k.startswith("_loc:")) and k in out:
                a = out[k]
                out[k] = (a[0] + v[0], a[1] + v[1], a[2] + v[2])
            else:
                out[k] = v
    return out


def lerp_pose(a, b, t):
    keys = set(a) | set(b)
    out = {}
    for k in keys:
        va = a.get(k, (0, 0, 0))
        vb = b.get(k, (0, 0, 0))
        out[k] = tuple(x + (y - x) * t for x, y in zip(va, vb))
    return out


# ---------------------------------------------------------------- stances per weapon style

ARMS_RELAXED = {
    "LeftUpperArm": (-4, 6, 0), "RightUpperArm": (-4, -6, 0),
    "LeftLowerArm": (-14, 0, 0), "RightLowerArm": (-14, 0, 0),
}


def stance(style):
    """Base idle pose for a weapon style."""
    base = {"Spine": (3, 0, 0), "Chest": (2, 0, 0), "Neck": (-2, 0, 0), "Head": (-1, 0, 0),
            "LeftUpperLeg": (-4, 4, 0), "RightUpperLeg": (-4, -4, 0),
            "LeftLowerLeg": (8, 0, 0), "RightLowerLeg": (8, 0, 0),
            "LeftFoot": (-4, 0, 0), "RightFoot": (-4, 0, 0), "_hips": (0, 0, -0.008)}
    if style == "sword_shield":
        return merge(base, {
            "LeftUpperArm": (-28, -2, 18), "LeftLowerArm": (-62, 0, 0), "LeftHand": (0, 0, 0),
            "RightUpperArm": (-12, 14, 0), "RightLowerArm": (-40, 0, -10), "RightHand": (52, 0, 10)})
    if style == "spear_shield":
        return merge(base, {
            "LeftUpperArm": (-26, -2, 16), "LeftLowerArm": (-60, 0, 0),
            "RightUpperArm": (-10, 12, 0), "RightLowerArm": (-62, 0, 0), "RightHand": (0, 0, 0)})
    if style == "bow":
        return merge(base, {
            "LeftUpperArm": (-18, -6, 0), "LeftLowerArm": (-30, 0, 0), "LeftHand": (40, 0, 0),
            "RightUpperArm": (-6, 8, 0), "RightLowerArm": (-30, 0, 0)})
    if style == "staff":
        return merge(base, {
            "RightUpperArm": (-14, 12, 0), "RightLowerArm": (-62, 0, 0), "RightHand": (0, 0, 0),
            "LeftUpperArm": (-20, -10, 0), "LeftLowerArm": (-55, 0, -10)})
    if style == "sword":
        return merge(base, {
            "LeftUpperArm": (-6, 12, 0), "LeftLowerArm": (-30, 0, 0),
            "RightUpperArm": (-12, 14, 0), "RightLowerArm": (-40, 0, -10), "RightHand": (52, 0, 10)})
    if style == "two_hand":
        return merge(base, {
            "RightUpperArm": (-18, 14, 0), "RightLowerArm": (-58, 0, 0), "RightHand": (10, 0, 0),
            "LeftUpperArm": (-26, -4, 18), "LeftLowerArm": (-48, 0, 0)})
    return merge(base, ARMS_RELAXED)


def _legs(phase, stride, knee, lift=1.0):
    """Leg pose at gait phase (0..1, left foot contact at 0)."""
    a = math.sin(phase * math.tau)
    out = {}
    for side, ph in (("Left", 0.0), ("Right", 0.5)):
        p = (phase + ph) % 1.0
        s = math.cos(p * math.tau)  # +1 = leg forward (contact)
        swing = max(0.0, math.sin(p * math.tau))  # >0 while the leg swings forward through the air
        out[side + "UpperLeg"] = (-stride * s - 6 * swing * lift, 0, 0)
        out[side + "LowerLeg"] = (6 + knee * swing * lift + 4 * max(0.0, -s), 0, 0)
        out[side + "Foot"] = (-8 * s + 10 * swing * lift, 0, 0)
    return out


def locomotion(style, run=False, duration=None, weight=1.0):
    """Walk/Run cycle. Weapon arms move less than free arms."""
    base = stance(style)
    dur = duration or (0.62 if run else 1.0)
    stride = (38 if run else 24) * weight
    knee = 70 if run else 38
    bob = 0.03 if run else 0.014
    lean = 12 if run else 4
    keys = []
    n = 8
    for i in range(n + 1):
        ph = i / n
        pose = dict(base)
        pose = merge(pose, _legs(ph, stride, knee))
        swing = math.cos(ph * math.tau)
        twist = 5 * swing
        pose = merge(pose, {"Spine": (lean, 0, twist * 0.5), "Chest": (lean * 0.3, 0, twist * 0.5),
                            "Head": (-lean * 0.6, 0, -twist)})
        arm_amp = (32 if run else 18)
        free_l = style in ("bow", "none", "staff")
        free_r = style in ("none",)
        la = arm_amp * (1.0 if free_l else 0.35)
        ra = arm_amp * (1.0 if free_r else 0.4)
        pose = merge(pose, {"LeftUpperArm": (la * swing, 0, 0), "RightUpperArm": (-ra * swing, 0, 0)})
        if run:
            pose = merge(pose, {"LeftLowerArm": (-20, 0, 0), "RightLowerArm": (-20, 0, 0)})
        h = -bob * abs(math.cos(ph * math.tau)) + bob * 0.4
        pose["_hips"] = (0, 0, h - 0.01)
        pose["_hipsrot"] = (0, 0, -twist * 0.6)
        keys.append((dur * ph, pose))
    return keys, True


def idle(style, duration=2.4):
    base = stance(style)
    keys = []
    n = 4
    for i in range(n + 1):
        t = i / n
        br = math.sin(t * math.tau)
        pose = merge(base, {"Spine": (1.2 * br, 0, 0), "Chest": (1.5 * br, 0, 0), "Head": (-1.5 * br, 0, 1.5 * br),
                            "LeftUpperArm": (0, -1.5 * br, 0), "RightUpperArm": (0, 1.5 * br, 0),
                            "_hips": (0, 0, 0.004 * br)})
        keys.append((duration * t, pose))
    return keys, True


def hit(style):
    base = stance(style)
    recoil = merge(base, {"Spine": (-12, 0, 6), "Chest": (-8, 0, 0), "Head": (-14, 0, -8),
                          "LeftUpperArm": (10, -12, 0), "RightUpperArm": (10, 12, 0), "_hips": (0, 0.03, -0.01)})
    return [(0, base), (0.1, recoil), (0.4, base)], False


def death(style, backward=True, duration=1.1):
    base = stance(style)
    s = -1 if backward else 1
    stagger = merge(base, {"Spine": (-14 * (1 if backward else -1), 0, 10), "Head": (-20, 0, 10),
                           "LeftUpperArm": (-30, -30, 0), "RightUpperArm": (-30, 30, 0),
                           "LeftLowerLeg": (30, 0, 0), "RightLowerLeg": (10, 0, 0), "_hips": (0, 0, -0.05)})
    fall = merge(base, {"Spine": (-6 * (1 if backward else -1), 0, 0), "Head": (-10 * (1 if backward else -1), 0, 15),
                        "LeftUpperArm": (-50 if backward else -160, -55, 0), "RightUpperArm": (-40 if backward else -150, 50, 0),
                        "LeftLowerArm": (-20, 0, 0), "RightLowerArm": (-30, 0, 0),
                        "LeftUpperLeg": (-10, 8, 0), "RightUpperLeg": (-24, -6, 0),
                        "LeftLowerLeg": (20, 0, 0), "RightLowerLeg": (40, 0, 0),
                        "_hipsrot": (s * 82, 0, 8), "_hips": (0, 0.0, -0.36)})
    settle = merge(fall, {"Head": (-14 * (1 if backward else -1), 0, 22), "_hips": (0, 0.0, -0.37)})
    return [(0, base), (0.16 * duration, stagger), (0.55 * duration, fall), (0.7 * duration, settle),
            (duration, settle)], False


def victory(style, raise_arm="Right"):
    base = stance(style)
    up = merge(base, {raise_arm + "UpperArm": (-132, 16 if raise_arm == "Right" else -16, 0),
                      raise_arm + "LowerArm": (-20, 0, 0), "Spine": (-6, 0, 0), "Chest": (-6, 0, 0),
                      "Head": (-14, 0, 0), "_hips": (0, 0, 0.0)})
    hop = merge(up, {"_hips": (0, 0, 0.05), "LeftLowerLeg": (20, 0, 0), "RightLowerLeg": (20, 0, 0),
                     raise_arm + "UpperArm": (-140, 10 if raise_arm == "Right" else -10, 0)})
    return [(0, up), (0.4, hop), (0.8, up), (1.2, hop), (1.6, up)], True


# ---------------------------------------------------------------- sword & shield

def sword_attack(style="sword_shield"):
    base = stance(style)
    wind = merge(base, {"RightUpperArm": (-150, 40, 0), "RightLowerArm": (-70, 0, 0), "RightHand": (-30, 0, 0),
                        "Spine": (-4, 0, 22), "Chest": (-4, 0, 14), "Head": (0, 0, -18),
                        "LeftUpperArm": (-40, -6, 22), "_hips": (0, 0.02, -0.02)})
    strike = merge(base, {"RightUpperArm": (-60, -16, -30), "RightLowerArm": (-12, 0, 0), "RightHand": (30, 0, 0),
                          "Spine": (14, 0, -22), "Chest": (8, 0, -14), "Head": (-6, 0, 18),
                          "LeftUpperLeg": (-24, 4, 0), "LeftLowerLeg": (24, 0, 0), "_hips": (0, -0.05, -0.04)})
    follow = merge(strike, {"RightUpperArm": (-30, -24, -40), "RightHand": (50, 0, 0)})
    return [(0, base), (0.16, wind), (0.27, strike), (0.38, follow), (0.7, base)], False


def sword_combo(style="sword_shield"):
    base = stance(style)
    k1, _ = sword_attack(style)
    side_wind = merge(base, {"RightUpperArm": (-80, 70, 30), "RightLowerArm": (-60, 0, 0), "Spine": (0, 0, 30),
                             "Chest": (0, 0, 15), "RightHand": (0, 0, -40)})
    side_hit = merge(base, {"RightUpperArm": (-80, -40, -40), "RightLowerArm": (-10, 0, 0), "Spine": (6, 0, -34),
                            "Chest": (4, 0, -18), "RightHand": (0, 0, 40), "_hips": (0, -0.05, -0.03)})
    keys = [(t, p) for t, p in k1[:-1]]
    keys += [(0.5, side_wind), (0.62, side_hit), (1.0, base)]
    return keys, False


def shield_block(style="sword_shield", hold=0.35):
    base = stance(style)
    block = merge(base, {"LeftUpperArm": (-70, -6, 30), "LeftLowerArm": (-80, 0, 0), "Spine": (10, 0, 12),
                         "Chest": (4, 0, 8), "Head": (6, 0, -10), "RightUpperArm": (6, 16, 0),
                         "LeftUpperLeg": (-28, 6, 0), "LeftLowerLeg": (34, 0, 0), "RightUpperLeg": (14, -6, 0),
                         "RightLowerLeg": (26, 0, 0), "_hips": (0, 0.02, -0.05)})
    return [(0, base), (0.12, block), (0.12 + hold, block), (0.3 + hold, base)], False


# ---------------------------------------------------------------- bow

def bow_aim_pose(style="bow", draw=1.0):
    """Side-on archer stance; draw=0 string relaxed, 1 fully drawn to the cheek."""
    base = stance(style)
    return merge(base, {
        "Spine": (2, 0, -30), "Chest": (0, 0, -22), "Neck": (0, 0, 24), "Head": (-4, 0, 26),
        "LeftUpperArm": (-84, -14, 0), "LeftLowerArm": (-6, 0, 0), "LeftHand": (0, 0, 0),
        "RightUpperArm": (-78 + 6 * draw, 18 + 24 * draw, -10 * draw), "RightLowerArm": (-60 - 70 * draw, 0, 0),
        "RightHand": (0, 0, 0),
        "LeftUpperLeg": (-12, 10, -10), "RightUpperLeg": (8, -10, 10), "LeftLowerLeg": (12, 0, 0),
        "RightLowerLeg": (10, 0, 0), "_hips": (0, 0, -0.02), "_hipsrot": (0, 0, -16)})


def bow_draw():
    base = stance("bow")
    return [(0, base), (0.25, bow_aim_pose("bow", 0.2)), (0.6, bow_aim_pose("bow", 1.0))], False


def bow_aim():
    a = bow_aim_pose("bow", 1.0)
    b = merge(a, {"Spine": (1, 0, 0), "RightLowerArm": (-3, 0, 0)})
    return [(0, a), (0.6, b), (1.2, a)], True


def bow_shoot():
    """Full attack: draw, hold, release (impact = release frame), recover."""
    base = stance("bow")
    drawn = bow_aim_pose("bow", 1.0)
    release = merge(bow_aim_pose("bow", 0.0), {"RightUpperArm": (-10, 40, 0), "RightLowerArm": (40, 0, 0),
                                              "LeftUpperArm": (6, 0, 0), "Spine": (-3, 0, 0)})
    return [(0, base), (0.16, bow_aim_pose("bow", 0.4)), (0.34, drawn), (0.42, drawn), (0.48, release),
            (0.62, merge(release, {"RightUpperArm": (6, 0, 0)})), (0.85, base)], False


def bow_reload():
    base = stance("bow")
    reach = merge(base, {"RightUpperArm": (-150, 30, -20), "RightLowerArm": (-60, 0, 0), "Head": (0, 0, -20),
                         "Spine": (-4, 0, 10)})
    nock = merge(base, {"RightUpperArm": (-60, 10, 0), "RightLowerArm": (-70, 0, 0), "LeftUpperArm": (-50, -10, 10),
                        "LeftLowerArm": (-40, 0, 0)})
    return [(0, base), (0.25, reach), (0.55, nock), (0.8, base)], False


def dodge(side=1):
    base = stance("bow")
    s = side
    roll = merge(base, {"Spine": (20, 6 * s, 10 * s), "LeftUpperLeg": (-40, 12 * s, 0), "RightUpperLeg": (10, 12 * s, 0),
                        "LeftLowerLeg": (50, 0, 0), "RightLowerLeg": (40, 0, 0), "_hips": (0.25 * s, 0, -0.12),
                        "_hipsrot": (0, -14 * s, 0)})
    return [(0, base), (0.2, roll), (0.45, merge(roll, {"_hips": (0.32 * s, 0, -0.06)})), (0.7, base)], False


# ---------------------------------------------------------------- commander

def command(style="sword"):
    """Points the sword forward: 'advance!'"""
    base = stance(style)
    point = merge(base, {"RightUpperArm": (-88, -6, -8), "RightLowerArm": (-6, 0, 0), "RightHand": (-50, 0, 0),
                         "Spine": (6, 0, -12), "Chest": (4, 0, -8), "Head": (-8, 0, 10),
                         "LeftUpperArm": (-10, -30, 0), "LeftLowerArm": (-60, 0, 0),
                         "LeftUpperLeg": (-26, 6, 0), "LeftLowerLeg": (24, 0, 0), "_hips": (0, -0.04, -0.03)})
    return [(0, base), (0.22, point), (0.8, point), (1.1, base)], False


def rally(style="sword"):
    """Sword raised high, shouting."""
    base = stance(style)
    up = merge(base, {"RightUpperArm": (-140, 20, 0), "RightLowerArm": (-10, 0, 0), "RightHand": (-60, 0, 0),
                      "LeftUpperArm": (-20, -36, 0), "LeftLowerArm": (-70, 0, 0),
                      "Spine": (-8, 0, 0), "Chest": (-8, 0, 0), "Head": (-16, 0, 0), "_hips": (0, 0, -0.02)})
    pump = merge(up, {"RightUpperArm": (-10, 0, 0), "Head": (-6, 0, 0), "_hips": (0, 0, 0.02)})
    return [(0, base), (0.25, up), (0.5, pump), (0.75, up), (1.0, pump), (1.3, base)], False


def buff(style="sword"):
    """Both arms open, power-up stance (aura VFX on top)."""
    base = stance(style)
    open_ = merge(base, {"RightUpperArm": (-40, 60, 0), "RightLowerArm": (-30, 0, 0),
                         "LeftUpperArm": (-40, -60, 0), "LeftLowerArm": (-30, 0, 0),
                         "Spine": (-10, 0, 0), "Chest": (-8, 0, 0), "Head": (-12, 0, 0),
                         "LeftUpperLeg": (0, 10, 0), "RightUpperLeg": (0, -10, 0), "_hips": (0, 0, -0.04)})
    return [(0, base), (0.3, open_), (0.9, open_), (1.2, base)], False


# ---------------------------------------------------------------- staff casters (mage, healer)

def staff_cast(style="staff"):
    """Raise the staff and channel (impact at the top)."""
    base = stance(style)
    up = merge(base, {"RightUpperArm": (-110, 10, 0), "RightLowerArm": (-30, 0, 0), "RightHand": (0, 0, 0),
                      "LeftUpperArm": (-70, -20, 0), "LeftLowerArm": (-30, 0, 0), "Spine": (-6, 0, 6),
                      "Head": (-10, 0, 0), "_hips": (0, 0, -0.01)})
    hold = merge(up, {"LeftUpperArm": (-8, 0, 0), "RightUpperArm": (-6, 0, 0)})
    return [(0, base), (0.25, up), (0.45, hold), (0.75, hold), (1.0, base)], False


def staff_attack(style="staff"):
    """Thrust the staff forward, bolt released at the end of the thrust."""
    base = stance(style)
    wind = merge(base, {"RightUpperArm": (-30, 20, 20), "RightLowerArm": (-90, 0, 0), "Spine": (-6, 0, 18),
                        "LeftUpperArm": (-50, -10, 0), "LeftLowerArm": (-40, 0, 0), "_hips": (0, 0.03, 0)})
    thrust = merge(base, {"RightUpperArm": (-80, -10, -10), "RightLowerArm": (-10, 0, 0), "RightHand": (-30, 0, 0),
                          "Spine": (12, 0, -14), "LeftUpperArm": (-20, -30, 0), "LeftUpperLeg": (-26, 6, 0),
                          "LeftLowerLeg": (22, 0, 0), "_hips": (0, -0.05, -0.03)})
    return [(0, base), (0.18, wind), (0.34, thrust), (0.5, thrust), (0.8, base)], False


def staff_area(style="staff"):
    """Raise high, slam the staff into the ground (area spell)."""
    base = stance(style)
    up = merge(base, {"RightUpperArm": (-150, 10, 0), "RightLowerArm": (-20, 0, 0), "LeftUpperArm": (-150, -10, 0),
                      "LeftLowerArm": (-30, 0, 0), "Spine": (-12, 0, 0), "Chest": (-6, 0, 0), "Head": (-14, 0, 0),
                      "_hips": (0, 0, 0.03)})
    slam = merge(base, {"RightUpperArm": (-40, 0, 0), "RightLowerArm": (-40, 0, 0), "RightHand": (30, 0, 0),
                        "LeftUpperArm": (-40, -10, 0), "LeftLowerArm": (-50, 0, 0), "Spine": (22, 0, 0),
                        "Chest": (8, 0, 0), "LeftUpperLeg": (-30, 4, 0), "RightUpperLeg": (-6, -4, 0),
                        "LeftLowerLeg": (40, 0, 0), "RightLowerLeg": (40, 0, 0), "_hips": (0, -0.03, -0.09)})
    return [(0, base), (0.3, up), (0.5, slam), (0.8, slam), (1.1, base)], False


def heal(style="staff"):
    """Staff forward, free palm toward the ally."""
    base = stance(style)
    give = merge(base, {"LeftUpperArm": (-80, -10, -10), "LeftLowerArm": (-10, 0, 0), "LeftHand": (-60, 0, 0),
                        "RightUpperArm": (-40, 10, 0), "Spine": (6, 0, -10), "Head": (4, 0, 8)})
    return [(0, base), (0.2, give), (0.55, give), (0.8, base)], False


def area_heal(style="staff"):
    base = stance(style)
    up = merge(base, {"RightUpperArm": (-160, 10, 0), "RightLowerArm": (-10, 0, 0), "LeftUpperArm": (-60, -60, 0),
                      "LeftLowerArm": (-20, 0, 0), "LeftHand": (-40, 0, 0), "Spine": (-10, 0, 0), "Chest": (-8, 0, 0),
                      "Head": (-18, 0, 0), "_hips": (0, 0, 0.01)})
    glow = merge(up, {"_hips": (0, 0, 0.03), "Head": (-6, 0, 0)})
    return [(0, base), (0.3, up), (0.6, glow), (0.9, up), (1.2, base)], False


def shield_spell(style="staff"):
    base = stance(style)
    push = merge(base, {"LeftUpperArm": (-90, -20, 10), "LeftLowerArm": (-20, 0, 0), "LeftHand": (-70, 0, 0),
                        "RightUpperArm": (-60, 20, 0), "RightLowerArm": (-50, 0, 0), "Spine": (8, 0, 0),
                        "LeftUpperLeg": (-20, 6, 0), "LeftLowerLeg": (20, 0, 0), "_hips": (0, -0.03, -0.03)})
    return [(0, base), (0.2, push), (0.7, push), (0.95, base)], False


def revive(style="staff"):
    base = stance(style)
    kneel = merge(base, {"LeftUpperLeg": (-80, 6, 0), "LeftLowerLeg": (95, 0, 0), "RightUpperLeg": (20, -6, 0),
                         "RightLowerLeg": (100, 0, 0), "RightFoot": (40, 0, 0), "Spine": (20, 0, 0),
                         "LeftUpperArm": (-60, -10, 0), "LeftLowerArm": (-30, 0, 0), "LeftHand": (-30, 0, 0),
                         "_hips": (0, 0.02, -0.18)})
    return [(0, base), (0.35, kneel), (1.0, kneel), (1.35, base)], False


# ---------------------------------------------------------------- spears (lancer, pikeman)

SPEAR_LEVEL = 95  # wrist flex that brings a vertically held spear down to horizontal


def spear_thrust(style="spear_shield", two_hand=False, lunge=1.0):
    base = stance(style)
    lvl = SPEAR_LEVEL
    left = {"LeftUpperArm": (-60, -20, -30), "LeftLowerArm": (-60, 0, 0)} if two_hand else {}
    wind = merge(base, {"RightUpperArm": (10, 26, 0), "RightLowerArm": (-80, 0, 0), "RightHand": (lvl, 0, 0),
                        "Spine": (-4, 0, 20), "Chest": (0, 0, 10), "_hips": (0, 0.04, -0.02)}, left)
    hit = merge(base, {"RightUpperArm": (-62, -6, -10), "RightLowerArm": (-14, 0, 0), "RightHand": (lvl - 12, 0, 0),
                       "Spine": (14 * lunge, 0, -16), "Chest": (6, 0, -8),
                       "LeftUpperLeg": (-36 * lunge, 6, 0), "LeftLowerLeg": (30 * lunge, 0, 0),
                       "RightUpperLeg": (14 * lunge, -4, 0), "_hips": (0, -0.08 * lunge, -0.05 * lunge)}, left)
    return [(0, base), (0.2, wind), (0.32, hit), (0.46, hit), (0.75, base)], False


def spear_attack(style="spear_shield", two_hand=False):
    """Overhead stab, shorter than the thrust."""
    base = stance(style)
    lvl = SPEAR_LEVEL
    left = {"LeftUpperArm": (-90, -10, -20), "LeftLowerArm": (-50, 0, 0)} if two_hand else {}
    up = merge(base, {"RightUpperArm": (-150, 20, 0), "RightLowerArm": (-30, 0, 0), "RightHand": (lvl + 40, 0, 0),
                      "Spine": (-6, 0, 10)}, left)
    stab = merge(base, {"RightUpperArm": (-110, 0, 0), "RightLowerArm": (-10, 0, 0), "RightHand": (lvl + 70, 0, 0),
                        "Spine": (16, 0, -6), "_hips": (0, -0.04, -0.03)}, left)
    return [(0, base), (0.2, up), (0.32, stab), (0.45, stab), (0.7, base)], False


def spear_charge(style="spear_shield", two_hand=False):
    keys, _ = locomotion(style, run=True, duration=0.56, weight=1.15)
    lvl = SPEAR_LEVEL
    left = {"LeftUpperArm": (-50, -20, -30), "LeftLowerArm": (-60, 0, 0)} if two_hand else {}
    over = merge({"RightUpperArm": (-40, -10, 0), "RightLowerArm": (-40, 0, 0), "RightHand": (lvl, 0, 0),
                  "Spine": (10, 0, 0)}, left)
    out = []
    for t, p in keys:
        q = dict(p)
        for k, v in over.items():
            q[k] = v if not k.startswith("_") else v
        q["Spine"] = (p["Spine"][0] + 10, p["Spine"][1], p["Spine"][2])
        out.append((t, q))
    return out, True


def spear_defend(style="spear_shield"):
    base = stance(style)
    brace = merge(base, {"LeftUpperArm": (-70, -6, 30), "LeftLowerArm": (-80, 0, 0), "RightUpperArm": (-30, 10, 0),
                         "RightHand": (SPEAR_LEVEL * 0.5, 0, 0), "Spine": (12, 0, 10),
                         "LeftUpperLeg": (-30, 6, 0), "LeftLowerLeg": (36, 0, 0), "RightUpperLeg": (16, -6, 0),
                         "RightLowerLeg": (26, 0, 0), "_hips": (0, 0.02, -0.06)})
    return [(0, base), (0.15, brace), (0.6, brace), (0.85, base)], False
