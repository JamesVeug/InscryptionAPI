using DiskCardGame;
using InscryptionAPI.Guid;
using UnityEngine;

namespace InscryptionAPI.Totems;

public static partial class TotemManager
{
    public static CustomTotemBottom NewBottomPiece<T, Y>(string guid, string rulebookName, string rulebookDescription, GameObject prefab)
        where T : TotemTriggerReceiver
        where Y : TotemBottomEffect
    {
        if (prefab == null)
        {
            InscryptionAPIPlugin.Logger.LogError($"Cannot load NewBottomPiece for {guid}.{rulebookName}. Prefab is null!");
            return null;
        }

        TotemEffect id = GuidManager.GetEnumValue<TotemEffect>(guid, rulebookName);
        return Add(new CustomTotemBottom()
        {
            EffectID = id,
            RulebookName = rulebookName,
            RulebookDescription = rulebookDescription,
            GUID = guid,
            Prefab = prefab,
            TriggerReceiver = typeof(T),
            Effect = typeof(Y),
        });
    }
    
    public static CustomTotemBottom NewBottomPiece<T>(string guid, string rulebookName, string rulebookDescription, Texture icon) where T : TotemTriggerReceiver
    {
        if (icon == null)
        {
            InscryptionAPIPlugin.Logger.LogError($"Cannot load NewBottomPiece for {guid}.{rulebookName}. Texture is null!");
            return null;
        }

        TotemEffect id = GuidManager.GetEnumValue<TotemEffect>(guid, rulebookName);
        return Add(new CustomTotemBottom()
        {
            EffectID = id,
            RulebookName = rulebookName,
            RulebookDescription = rulebookDescription,
            GUID = guid,
            Icon = icon,
            Prefab = DefaultTotemBottom,
            TriggerReceiver = typeof(T)
        });
    }

    private static CustomTotemBottom Add(CustomTotemBottom totem)
    {
        totemBottoms.Add(totem);
        
        TotemBottomEffect totemBottomEffect = (TotemBottomEffect)Activator.CreateInstance(totem.Effect);
        totemBottomEffect.EffectID = totem.EffectID;
        allBottomEffects.Add(totemBottomEffect);
        return totem;
    }
}
