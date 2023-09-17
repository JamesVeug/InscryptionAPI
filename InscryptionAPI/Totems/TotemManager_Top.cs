using DiskCardGame;
using UnityEngine;

namespace InscryptionAPI.Totems;

public static partial class TotemManager
{
    [Obsolete("Deprecated. Use NewTopPiece<T> instead.")]
    public static CustomTotemTop NewTopPiece(string name, string guid, Tribe tribe, GameObject prefab = null)
    {
        if (prefab == null)
        {
            InscryptionAPIPlugin.Logger.LogError($"Cannot load NewTopPiece for {guid}.{name}. Prefab is null!");
            return null;
        }

        return Add(new CustomTotemTop()
        {
            Name = name,
            GUID = guid,
            Prefab = prefab,
            Tribe = tribe
        });
    }
    public static CustomTotemTop NewTopPiece<T>(string name, string guid, Tribe tribe, GameObject prefab) where T : CompositeTotemPiece
    {
        if (prefab == null)
        {
            InscryptionAPIPlugin.Logger.LogError($"Cannot load NewTopPiece for {guid}.{name}. Prefab is null!");
            return null;
        }

        return Add(new CustomTotemTop()
        {
            Name = name,
            GUID = guid,
            Type = typeof(T),
            Prefab = prefab,
            Tribe = tribe
        });
    }
    private static CustomTotemTop Add(CustomTotemTop totem)
    {
        totemTops.Add(totem);
        return totem;
    }
}
