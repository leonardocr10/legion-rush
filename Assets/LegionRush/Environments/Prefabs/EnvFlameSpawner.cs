using UnityEngine;

namespace LegionRush.Environments
{
    // Usado pelos prefabs do kit (braseiro): cria o fogo em partículas ao iniciar a cena.
    public class EnvFlameSpawner : MonoBehaviour
    {
        public Vector3[] LocalFlames = new Vector3[0];
        public Color TorchColor = new Color(1f, 0.55f, 0.2f);
        [Range(0f, 1f)] public float Particles = 1f;

        private void Start()
        {
            var fx = new GameObject("Fire").AddComponent<EnvFireFx>();
            fx.transform.SetParent(transform, false);
            fx.Initialize(TorchColor);
            foreach (Vector3 local in LocalFlames)
            {
                Vector3 world = transform.TransformPoint(local);
                fx.Add(world, transform.position.y);
            }
            fx.ApplyQuality(Particles, true);
        }
    }
}
