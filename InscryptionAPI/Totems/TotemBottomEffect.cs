using DiskCardGame;
using UnityEngine;

namespace InscryptionAPI.Totems;

public class TotemBottomEffect
{
    public TotemEffect EffectID;

    public TotemBottomEffect()
    {
        // Required so 'Activator.CreateInstance(totem.Effect)' works
    }
    
    public virtual List<TotemBottomData> GetAllOptions(int seed)
    {
        List<TotemBottomData> data = new List<TotemBottomData>();
        TotemBottomData totemBottomData = ScriptableObject.CreateInstance<TotemBottomData>();
        totemBottomData.effect = EffectID;
        totemBottomData.effectParams = new TotemBottomData.EffectParameters();
        data.Add(totemBottomData);
        return data;
    }
    
    public virtual void ModifyCardWithTotem(TotemBottomData totemData, CardModificationInfo cardModificationInfo)
    {
        
    }
}
