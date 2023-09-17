using DiskCardGame;

namespace InscryptionAPI.Totems;

[Serializable]
public class CustomTotemsSaveData
{
    public List<CustomTotemTop> TotemTops = new List<CustomTotemTop>();
    public List<CustomTotemBottom> TotemBottoms = new List<CustomTotemBottom>();

    public bool ContainsCardGainAbility(Ability ability)
    {
        return TotemBottoms.Find((a) =>
        {
            return a.EffectID == TotemEffect.CardGainAbility && a.EffectParameters.ability == ability;
        }) != null;
    }

    public bool ContainsTribeCondition(Tribe tribe)
    {
        return TotemTops.Find((a) =>
        {
            return a.ConditionID == CustomTotemTopData.TotemConditionType.Tribe && a.Prerequisites.tribe == tribe;
        }) != null;
    }
    
    /// <summary>
    /// Easy to track patches and their stack traces
    /// </summary>
    public int TotemsTopCount()
    {
        return TotemTops.Count;
    }
    
    /// <summary>
    /// Easy to track patches and their stack traces
    /// </summary>
    public int TotemsBottomCount()
    {
        return TotemBottoms.Count;
    }

    public CustomTotemsSaveData DoesNothing()
    {
        // Filler method so patches can keep 3 method calls to match RunState.Run.TotemBottoms.Contains
        return this;
    }
}

[Serializable]
public class CustomTotemTop
{
    public CustomTotemTopData.TotemConditionType ConditionID;
    public TotemTopData.TriggerCardPrerequisites Prerequisites; // Default data
}

[Serializable]
public class CustomTotemBottom
{
    public TotemEffect EffectID; // Type
    public TotemBottomData.EffectParameters EffectParameters; // Default data
}