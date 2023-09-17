using DiskCardGame;
using UnityEngine;

namespace InscryptionAPI.Totems;

public class TotemTopEffect
{
    public CustomTotemTopData.TotemConditionType conditionType;

    public TotemTopEffect()
    {
        // Required so 'Activator.CreateInstance(totem.Effect)' works
    }
    
    public virtual List<CustomTotemTopData> GetAllOptions(int seed)
    {
        List<CustomTotemTopData> data = new List<CustomTotemTopData>();
        CustomTotemTopData totemBottomData = ScriptableObject.CreateInstance<CustomTotemTopData>();
        totemBottomData.ConditionID = conditionType;
        totemBottomData.prerequisites = new TotemTopData.TriggerCardPrerequisites();
        data.Add(totemBottomData);
        return data;
    }
    
    public virtual void ModifyCardWithTotem(TotemTopData totemData, CardModificationInfo cardModificationInfo)
    {
        
    }
}
