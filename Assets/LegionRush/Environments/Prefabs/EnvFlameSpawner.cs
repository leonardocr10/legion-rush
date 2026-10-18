using UnityEngine;

namespace LegionRush.Environments
{
    // Usado pelos prefabs do kit (braseiro): cria o fogo em partículas ao iniciar a cena.
    public class EnvFlameSpawner : MonoBehaviour
    {
        public Vector3[] LocalFlames = new Vector3[0];
        public Color TorchColor = new Color(1f, 0.55f, 0.2f);
        [Range(0f, 1f)] public float Particles = 1f;
        public bool Pools = true;

        private EnvFireFx fx;

        // Perfil gráfico (pode ser chamado antes ou depois do Start).
        public void SetQuality(float particles, bool pools)
        {
            Particles = particles;
            Pools = pools;
            if (fx != null) fx.ApplyQuality(particles, pools);
        }

        private void Start()
        {
            fx = new GameObject("Fire").AddComponent<EnvFireFx>();
            fx.transform.SetParent(transform, false);
            fx.Initialize(TorchColor);
            foreach (Vector3 local in LocalFlames)
            {
                Vector3 world = transform.TransformPoint(local);
                fx.Add(world, transform.position.y);
            }
            fx.ApplyQuality(Particles, Pools);
        }
    }
}
