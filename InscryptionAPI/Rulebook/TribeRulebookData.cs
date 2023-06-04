using DiskCardGame;
using InscryptionAPI.Card;
using UnityEngine;

namespace InscryptionAPI.Rulebook;

public class TribeRulebookData : CustomRulebookSectionData
{
    public override int numPages => TribeManager.AllTribesInfos.Count;

    private TribeManager.TribeInfo GetTribeInfo(string pageID)
    {
        if (!int.TryParse(pageID, out int num))
        {
            num = (int)Enum.Parse(typeof(Tribe), pageID);
        }
        Tribe tribe = (Tribe)num;
        InscryptionAPIPlugin.Logger.LogError("tribe " + tribe);
        return TribeManager.GetTribeInfo(tribe);
    }

    public override string GetPageID(int index) => TribeManager.AllTribesInfos[index].tribe.ToString();
    public override string GetRulebookName(string pageID) => GetTribeInfo(pageID).rulebookName;
    public override string GetRulebookDescription(string pageID) => GetTribeInfo(pageID).rulebookDescription;
    public override Sprite GetRulebookSprite(string pageID) => GetTribeInfo(pageID).icon;
}