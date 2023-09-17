using DiskCardGame;

namespace InscryptionAPI.Totems;

public class CustomTotemTopData : TotemTopData
{
    public enum TotemConditionType
    {
        Tribe,
    }

    public TotemConditionType ConditionID;
}
