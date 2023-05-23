using DiskCardGame;
using UnityEngine;

namespace InscryptionAPI.Totems;

public class CustomTotemDefinition : TotemDefinition
{
    public TotemEffect BottomEffectID;
    public TotemBottomData.EffectParameters EffectParameters;

    public new TotemBottomData MakeBottom()
    {
        InscryptionAPIPlugin.Logger.LogInfo($"[CustomTotemDefinition] MakeBottom");
        TotemBottomData instance = ScriptableObject.CreateInstance<TotemBottomData>();
        instance.effect = BottomEffectID;
        if (BottomEffectID == TotemEffect.CardGainAbility)
        {
            
            instance.effectParams = new TotemBottomData.EffectParameters()
            {
                ability = ability
            };
        }
        else
        {
            instance.effectParams = EffectParameters;
        }

        InscryptionAPIPlugin.Logger.LogInfo($"[CustomTotemDefinition] MakeBottom " + instance.effect);
        return instance;
    }
}
