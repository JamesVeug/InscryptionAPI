using DiskCardGame;
using InscryptionAPI.Helpers;
using InscryptionAPI.Rulebook;
using UnityEngine;

namespace InscryptionAPI.Totems;

public class TotemRulebookData : CustomRulebookSectionData
{
    public override int numPages => TotemManager.totemBottoms.Count;
    
    private TotemManager.CustomTotemBottom GetTotemBottomInfo(string pageID)
    {
        return TotemManager.totemBottoms.Find((a)=>a.GUID + "_" + a.EffectID == pageID);
    }
    
    public override string GetPageID(int index) => TotemManager.totemBottoms[index].GUID + "_" + TotemManager.totemBottoms[index].EffectID;
    public override string GetRulebookName(string pageID) => GetTotemBottomInfo(pageID).RulebookName;
    public override string GetRulebookDescription(string pageID) => GetTotemBottomInfo(pageID).RulebookDescription;
    public override Sprite GetRulebookSprite(string pageID) => GetTotemBottomInfo(pageID).Icon.ConvertTexture();
}
