using DiskCardGame;
using UnityEngine;

namespace InscryptionAPI.Totems;

public class CustomTotemDefinition : TotemDefinition
{
    public TotemEffect BottomEffectID;

    public new TotemBottomData MakeBottom()
    {
        InscryptionAPIPlugin.Logger.LogInfo($"[CustomTotemDefinition] MakeBottom");
        TotemBottomData instance = ScriptableObject.CreateInstance<TotemBottomData>();
        instance.effect = BottomEffectID;
        instance.effectParams = new TotemBottomData.EffectParameters();
        instance.effectParams.ability = this.ability;
        
        InscryptionAPIPlugin.Logger.LogInfo($"[CustomTotemDefinition] MakeBottom " + instance.effect);
        return instance;
    }
}
