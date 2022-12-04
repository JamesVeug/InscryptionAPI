using DiskCardGame;
using InscryptionAPI.Card;
using TMPro;
using UnityEngine;
using Object = System.Object;

namespace InscryptionAPI.Rulebook;

public class TribePage : RuleBookPage
{
    public SpriteRenderer iconRenderer;
    public TextMeshPro nameTextMesh;
    public TextMeshPro descriptionTextMesh;

    public override void FillPage(string headerText, params object[] otherArgs)
    {
        base.FillPage(headerText, otherArgs);

        TribeManager.TribeInfo tribeInfo = null;
        foreach (TribeManager.TribeInfo info in TribeManager.AllTribesInfos)
        {
            if (info.tribe.ToString() == otherArgs[0] as string)
            {
                tribeInfo = info;
                break;
            }
        }
        
        this.iconRenderer.sprite = tribeInfo.icon;
        this.nameTextMesh.text = Localization.Translate(tribeInfo.rulebookName);
        string text = RuleBookPage.ParseCardDefinition(tribeInfo.rulebookDescription);
        string englishText = string.Format(Localization.Translate("To the user: {0}"), text);
        this.descriptionTextMesh.text = Localization.Translate(englishText);
    }
}
