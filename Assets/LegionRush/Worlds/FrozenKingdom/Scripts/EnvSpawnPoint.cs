using UnityEngine;

namespace LegionRush.Environments
{
    public enum EnvSpawnKind { Player, Enemy, Boss, GateRow }

    // Marcador de spawn/formação na cena (só informativo e para o editor: o jogo continua criando exército,
    // inimigos, portais e chefão pelo LevelManager nas mesmas coordenadas).
    public class EnvSpawnPoint : MonoBehaviour
    {
        public EnvSpawnKind kind;
        [Tooltip("Inimigos da onda / opções da fileira de portais.")] public int count;
        public Vector2 area = new Vector2(8f, 6f);
        [TextArea] public string note;

        private void OnDrawGizmos()
        {
            Color c = kind switch
            {
                EnvSpawnKind.Player => new Color(0.2f, 0.5f, 1f, 0.35f),
                EnvSpawnKind.Boss => new Color(0.6f, 0f, 0.8f, 0.35f),
                EnvSpawnKind.GateRow => new Color(1f, 0.85f, 0.2f, 0.35f),
                _ => new Color(1f, 0.15f, 0.15f, 0.35f)
            };
            Gizmos.color = c;
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawCube(new Vector3(0f, 0.05f, 0f), new Vector3(area.x, 0.1f, area.y));
            c.a = 1f;
            Gizmos.color = c;
            Gizmos.DrawWireCube(new Vector3(0f, 0.05f, 0f), new Vector3(area.x, 0.1f, area.y));
            Gizmos.DrawLine(Vector3.zero, Vector3.forward * (area.y * 0.5f + 1f));
        }
    }
}
