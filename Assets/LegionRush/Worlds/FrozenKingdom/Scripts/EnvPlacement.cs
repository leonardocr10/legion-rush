using UnityEngine;

namespace LegionRush.Environments
{
    // Uma peça posicionada no mapa do Reino Congelado: qual prefab, onde, giro, escala, grupo da cena e a
    // seção da folha de referência que justifica a posição. Gerada pelo FrozenKingdomTrack (mesma lógica do
    // cenário do jogo) e usada pelo construtor da cena FrozenKingdom_01 e pela tabela de coordenadas.
    public struct EnvPlacement
    {
        public string Prefab;
        public Vector3 Position;
        public float Yaw;
        public Vector3 Scale;
        public string Group;
        public string Section;

        public EnvPlacement(string prefab, Vector3 position, float yaw, Vector3 scale, string group, string section)
        {
            Prefab = prefab;
            Position = position;
            Yaw = yaw;
            Scale = scale;
            Group = group;
            Section = section;
        }
    }
}
