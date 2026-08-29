using UnityEngine;

namespace DungeonsCrows.World
{
    public enum PlaceholderCategory
    {
        Environment,
        Character,
        Creature,
        Crow,
        Prop,
        Vfx,
        Audio
    }

    /// <summary>Marks development-only content that must be replaced before production validation passes.</summary>
    public sealed class PlaceholderAssetMarker : MonoBehaviour
    {
        public PlaceholderCategory category;
        [TextArea] public string replacementIntent;
    }
}
