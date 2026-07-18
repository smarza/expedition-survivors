using UnityEngine;

namespace ProjectExpedition
{
    [CreateAssetMenu(fileName = "ProductionContent", menuName = "Project Expedition/Production Content Database")]
    public sealed class ProductionContentDatabase : ScriptableObject
    {
        public CharacterContentRecord[] characters = new CharacterContentRecord[0];
        public MapContentRecord[] maps = new MapContentRecord[0];
        public ItemContentRecord[] items = new ItemContentRecord[0];
        public EnemyContentRecord[] enemies = new EnemyContentRecord[0];
    }
}
