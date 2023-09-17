using DiskCardGame;
using UnityEngine;

namespace InscryptionAPI.Totems;

public class CustomTotemDefinition : TotemDefinition
{
    public CustomTotemTopData.TotemConditionType ConditionID = CustomTotemTopData.TotemConditionType.Tribe;
    public TotemTopData.TriggerCardPrerequisites TopPrerequisites;
    
    public TotemEffect BottomEffectID = TotemEffect.CardGainAbility;
    public TotemBottomData.EffectParameters BottomEffectParameters;

    public new TotemTopData MakeTop()
    {
        // return new TotemTopData(tribe);
        
        InscryptionAPIPlugin.Logger.LogInfo($"[CustomTotemDefinition] MakeTop " + ConditionID);
        CustomTotemTopData instance = ScriptableObject.CreateInstance<CustomTotemTopData>();
        instance.ConditionID = ConditionID;
        if (ConditionID == CustomTotemTopData.TotemConditionType.Tribe)
        {
            instance.prerequisites = new TotemTopData.TriggerCardPrerequisites();
            instance.prerequisites.tribe = tribe;
        }
        else
        {
            instance.prerequisites = TopPrerequisites;
        }

        return instance;
    }
    
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
            instance.effectParams = BottomEffectParameters;
        }

        InscryptionAPIPlugin.Logger.LogInfo($"[CustomTotemDefinition] MakeBottom " + instance.effect);
        return instance;
    }
}
