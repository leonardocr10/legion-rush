using UnityEngine;

namespace LegionRush
{
    // Pontos de encaixe do modelo final (ossos não deformantes exportados do Blender).
    // Preenchido pelo construtor de prefabs; em runtime só é lido (origem de projéteis e efeitos).
    [DisallowMultipleComponent]
    public class CharacterSockets : MonoBehaviour
    {
        public Transform rightHand;
        public Transform leftHand;
        public Transform head;
        public Transform back;
        public Transform projectileSpawn;
        public Transform vfx;

        // Origem do projétil: ProjectileSpawn quando existe; senão a mão que segura a arma.
        public bool TryGetProjectileOrigin(out Vector3 position)
        {
            Transform t = projectileSpawn != null ? projectileSpawn : rightHand;
            position = t != null ? t.position : default;
            return t != null;
        }

        public Transform VfxAnchor => vfx != null ? vfx : projectileSpawn != null ? projectileSpawn : head;
    }
}
