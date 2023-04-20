using DiskCardGame;

namespace InscryptionAPI.Totems;

[Serializable]
public class CustomTotemsSaveData
{
    public List<CustomTotemBottom> TotemBottoms = new List<CustomTotemBottom>();

    public bool ContainsCardGainAbility(Ability ability)
    {
        return TotemBottoms.Find((a) => a.EffectID == TotemEffect.CardGainAbility && a.Ability == ability) != null;
    }

    public CustomTotemsSaveData DoesNothing()
    {
        // Filler method so patches can keep 3 method calls to match RunState.Run.TotemBottoms.Contains
        return this;
    }
}

[Serializable]
public class CustomTotemBottom
{
    public TotemEffect EffectID; // Type
    public Ability Ability; // Default data
}