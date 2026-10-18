"""Arqueiro inimigo (Enemy_Archer): red hooded archer, golden bow with red string, red-fletched arrows."""
from .. import mesh as M
from . import archer


def build():
    return archer.build(dict(name="EnemyArcher", main="red", dark="red_dark", light="red_light", trim="gold",
                             gem="gem_red", glow="glow_red", hair="hair_dark", iris="iris_brown", boot_cuff="red_dark",
                             pauldron="iron", string="red", fletch="red", weapon="Enemy", prop="Enemy",
                             emblem=M.beast_emblem(0.18)))
