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
        InscryptionAPIPlugin.Logger.LogInfo("[TribePage.FillPage] " + headerText + " " + otherArgs);
        InscryptionAPIPlugin.Logger.LogInfo("[TribePage.FillPage] " + headerText + " " + otherArgs.Length);
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

        iconRenderer.transform.localPosition = new Vector3(0, 0.8f, 0);
        iconRenderer.transform.localScale = new Vector3(0.75f, 0.75f, 0.75f);
        iconRenderer.sprite = tribeInfo.icon;
        nameTextMesh.text = Localization.Translate(tribeInfo.rulebookName);
        
        string text = RuleBookPage.ParseCardDefinition(tribeInfo.rulebookDescription);
        string englishText = string.Format(Localization.Translate("Flavour: {0}"), text);
        descriptionTextMesh.text = Localization.Translate(englishText);
    }
}
