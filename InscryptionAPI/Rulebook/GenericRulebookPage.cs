using DiskCardGame;
using InscryptionAPI.Card;
using TMPro;
using UnityEngine;

namespace InscryptionAPI.Rulebook;

public sealed class GenericRulebookPage : RuleBookPage
{
    public SpriteRenderer iconRenderer;
    public TextMeshPro nameTextMesh;
    public TextMeshPro descriptionTextMesh;

    public override void FillPage(string headerText, params object[] otherArgs)
    {
        base.FillPage(headerText, otherArgs);

        PageRangeType pageRangeInfo = (PageRangeType)otherArgs[0];
        string pageId = (string)otherArgs[1];

        RuleBookManager.CustomRulebookSection rulebookSection = RuleBookManager.newPages.Find((a) => a.PageRangeInfo.type == pageRangeInfo);
        InscryptionAPIPlugin.Logger.LogInfo(pageRangeInfo + " " + pageId);
        iconRenderer.transform.localPosition = new Vector3(0, 0.8f, 0);
        iconRenderer.transform.localScale = new Vector3(0.75f, 0.75f, 0.75f);
        iconRenderer.sprite = rulebookSection.CustomPageType.GetRulebookSprite(pageId);
        nameTextMesh.text = Localization.Translate(rulebookSection.CustomPageType.GetRulebookName(pageId));
        
        string text = ParseCardDefinition(rulebookSection.CustomPageType.GetRulebookDescription(pageId));
        string englishText = string.Format(Localization.Translate("Flavour: {0}"), text);
        descriptionTextMesh.text = Localization.Translate(englishText);
    }
}
