using DiskCardGame;
using HarmonyLib;
using InscryptionAPI.Saves;
using UnityEngine;

namespace InscryptionAPI.Card;

/// <summary>
/// TemporaryCardInfo is a class that provides a few helper methods for working with temporary cardInfos
/// These cards can be changed during runs, stored in your deck, obtained few nodes and more.
/// </summary>
[HarmonyPatch]
public static partial class CardManager
{
    /// <summary>
    /// Saves the cardInfo into the Run so it can be added to your deck and used in various other places.
    /// </summary>
    public static void SaveTemporaryCard(this CardInfo cardInfo)
    {
        if (!IsTemporaryCardInfo(cardInfo))
        {
            CreateTemporaryCardInfoMod(cardInfo, "");
        }
        
        string json = JsonUtility.ToJson(cardInfo);
        ModdedSaveManager.RunState.SetValue(InscryptionAPIPlugin.ModGUID, "TemporaryCard_" + cardInfo.name, json);
    }
    
    /// <summary>
    /// Gets the temporary cardInfo from the RunState if it exists otherwise returns null
    /// </summary>
    public static CardInfo GetTemporaryCard(this string cardInfoName)
    {
        string s = ModdedSaveManager.RunState.GetValue(InscryptionAPIPlugin.ModGUID, "TemporaryCard_" + cardInfoName);
        if (!string.IsNullOrEmpty(s))
        {
            CardInfo cardInfo = ScriptableObject.CreateInstance<CardInfo>();
            JsonUtility.FromJsonOverwrite(s, cardInfo);

            cardInfo.name = cardInfoName; // name doesn't translate over
            return cardInfo;
        }

        return null;
    }
    
    /// <summary>
    /// Checks if the CardInfo provided was created in this run and stored temporarily
    /// Checks by looking for a mod with a temporary_card singleton id 
    /// </summary>
    public static bool IsTemporaryCardInfo(this CardInfo cardInfo)
    {
        return cardInfo != null &&
            cardInfo.Mods != null &&
            cardInfo.Mods.Any(a => !string.IsNullOrEmpty(a.singletonId) && a.singletonId.StartsWith("temporary_card"));
    }

    /// <summary>
    /// Checks if the CardInfo was originally cloned from another card
    /// Returns true and the name of the original card that it was cloned from
    /// </summary>
    public static bool TryGetTemporaryCardTemplateName(this CardInfo cardInfo, out string templateCardName)
    {
        // Looks for a mod with a temporary_card singleton id
        // Extracts the name from the id and returns true if it exists
        templateCardName = GetTemporaryCardTemplateName(cardInfo);
        return !string.IsNullOrEmpty(templateCardName);
    }

    /// <summary>
    /// Gets the name of the original card that this card was cloned from if it was
    /// </summary>
    public static string GetTemporaryCardTemplateName(this CardInfo cardInfo)
    {
        // Looks for a mod with a temporary_card singleton id
        // Extracts the name from the id and returns true if it exists
        var mod = cardInfo.Mods.Find((a) => !string.IsNullOrEmpty(a.singletonId) && a.singletonId.StartsWith("temporary_card"));
        return mod != null ? mod.singletonId.Substring("temporary_card|".Length) : cardInfo.name;
    }
    
    /// <summary>
    /// Creates a new CardInfo that is a clone of the provided CardInfo
    /// Changes the name of the card to be unique.
    /// Still requires calling TemporaryCardInfo.SaveTemporaryCard() to save the card
    /// </summary>
    public static CardInfo CloneAsTemporaryCard(this CardInfo templateCardInfo)
    {
        int value = ModdedSaveManager.RunState.GetValueAsInt(InscryptionAPIPlugin.ModGUID, "TemporaryCardTotalTempCards") + 1;
        ModdedSaveManager.RunState.SetValue(InscryptionAPIPlugin.ModGUID, "TemporaryCardTotalTempCards", value);

        CardInfo clone = ScriptableObject.CreateInstance<CardInfo>();
        clone.name = $"{templateCardInfo.name}_temp_{value}";
        clone.abilities = new List<Ability>(templateCardInfo.abilities);
        clone.specialAbilities = new List<SpecialTriggeredAbility>(templateCardInfo.specialAbilities);
        clone.tribes = new List<Tribe>(templateCardInfo.tribes);
        clone.gemsCost = new List<GemType>(templateCardInfo.gemsCost);
        clone.appearanceBehaviour = new List<CardAppearanceBehaviour.Appearance>(templateCardInfo.appearanceBehaviour);
        clone.description = templateCardInfo.description;
        clone.baseAttack = templateCardInfo.baseAttack; 
        clone.baseHealth = templateCardInfo.baseHealth;
        clone.cost = templateCardInfo.cost;
        clone.bonesCost = templateCardInfo.bonesCost;
        clone.energyCost = templateCardInfo.energyCost;
        clone.boon = templateCardInfo.boon;
        clone.onePerDeck = templateCardInfo.onePerDeck;
        clone.temple = templateCardInfo.temple;
        clone.displayedName = templateCardInfo.displayedName;
        clone.displayedNameLocId = templateCardInfo.displayedNameLocId;
        clone.hideAttackAndHealth = templateCardInfo.hideAttackAndHealth;
        clone.specialStatIcon = templateCardInfo.specialStatIcon;
        clone.titleGraphic = templateCardInfo.titleGraphic;
        clone.portraitTex = templateCardInfo.portraitTex;
        clone.alternatePortrait = templateCardInfo.alternatePortrait;
        clone.holoPortraitPrefab = templateCardInfo.holoPortraitPrefab;
        clone.animatedPortrait = templateCardInfo.animatedPortrait;
        clone.pixelPortrait = templateCardInfo.pixelPortrait;
        clone.defaultEvolutionName = templateCardInfo.defaultEvolutionName;
        clone.flipPortraitForStrafe = templateCardInfo.flipPortraitForStrafe;
        clone.cardComplexity = templateCardInfo.cardComplexity;
        clone.decals = new List<Texture>(templateCardInfo.decals);
        clone.metaCategories = new List<CardMetaCategory>(templateCardInfo.metaCategories);
        clone.traits = new List<Trait>(templateCardInfo.traits);
        clone.ascensionAbilities = new List<Ability>(templateCardInfo.ascensionAbilities);
        clone.temporaryDecals = new List<Texture>(templateCardInfo.temporaryDecals);
        clone.get_decals = new List<Texture>(templateCardInfo.get_decals);
        foreach (KeyValuePair<string, string> pair in templateCardInfo.GetCardExtensionTable())
        {
            clone.SetExtendedProperty(pair.Key, pair.Value);
        }

        if (clone.evolveParams != null)
        {
            clone.evolveParams = new EvolveParams();
            clone.evolveParams.turnsToEvolve = templateCardInfo.evolveParams.turnsToEvolve;
            clone.evolveParams.evolution = templateCardInfo.evolveParams.evolution;
        }
        
        if (clone.tailParams != null)
        {
            clone.tailParams = new TailParams();
            clone.tailParams.tail = templateCardInfo.tailParams.tail;
            clone.tailParams.tailLostPortrait = templateCardInfo.tailParams.tailLostPortrait;
        }
        
        if (clone.iceCubeParams != null)
        {
            clone.iceCubeParams = new IceCubeParams();
            clone.iceCubeParams.creatureWithin = templateCardInfo.iceCubeParams.creatureWithin;
        }
        
        clone.Mods = new List<CardModificationInfo>(templateCardInfo.Mods.Select(a=>a.Clone()).Cast<CardModificationInfo>());
        if (!templateCardInfo.IsTemporaryCardInfo())
        {
            CreateTemporaryCardInfoMod(clone, templateCardInfo.name);
        }
        
        return clone;
    }
    
    private static void CreateTemporaryCardInfoMod(CardInfo clone, string templateName)
    {
        clone.Mods.Add(new CardModificationInfo()
        {
            singletonId = "temporary_card|" + templateName
        });
    }
}