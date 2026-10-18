using UnityEngine;

namespace LegionRush
{
    // Catálogo dos sprites da tela inicial (Assets/LegionRush/UI/MainMenu/{Icons,Buttons,Panels,...}).
    // Gerado pelo menu "Legion Rush/UI/Tela Inicial/Atualizar arte" (e automaticamente ao importar a arte),
    // para os sprites ficarem fora de Resources e entrarem no Sprite Atlas.
    [CreateAssetMenu(fileName = "MainMenuArt", menuName = "Legion Rush/UI/Main Menu Art Catalog")]
    public class MainMenuArtCatalog : ScriptableObject
    {
        public Sprite[] sprites = new Sprite[0];
    }
}
