using DiskCardGame;
using GBC;
using HarmonyLib;
using InscryptionAPI.Card;
using InscryptionAPI.CardCosts;
//using InscryptionAPI.CardCosts;
using InscryptionAPI.Helpers;
using UnityEngine;

namespace InscryptionCommunityPatch.Card;

//[HarmonyPatch]
/// <summary>
/// Modifies how card costs are rendered in Act to add support for mixed card costs, custom costs, and energy and Mox costs.
/// </summary>
public static class Part1CardCostRender
{
    public static event Action<CardInfo, List<Texture2D>> UpdateCardCost;

    public static List<Texture2D> CostTextures(CardInfo cardInfo, PlayableCard playableCard, int bloodCost, int bonesCost, int energyCost, List<GemType> gemsCost)
    {
        List<Texture2D> costTextures = new();

        if (gemsCost.Count > 0)
        {
            // Get all the Mox textures, with placeholders if less than 3 Mox are present
            List<Texture2D> gemCost = new();

            if (gemsCost.Contains(GemType.Green))
                gemCost.Add(CardCostRender.GetTextureByName("mox_cost_g"));

            if (gemsCost.Contains(GemType.Blue))
                gemCost.Add(CardCostRender.GetTextureByName("mox_cost_b"));

            if (gemsCost.Contains(GemType.Orange))
                gemCost.Add(CardCostRender.GetTextureByName("mox_cost_o"));

            while (gemCost.Count < 3)
                gemCost.Insert(0, null);

            costTextures.Add(TextureHelper.CombineTextures(gemCost, TextureHelper.GetImageAsTexture("mox_cost_empty.png", typeof(Part1CardCostRender).Assembly), xStep: 21));
        }

        // there's a 6+ texture but since Energy can't go above 6 normally I have excluded it from consideration
        if (energyCost > 0)
            costTextures.Add(CardCostRender.GetTextureByName($"energy_cost_{Mathf.Min(6, energyCost)}"));

        if (bonesCost > 0)
            costTextures.Add(CardCostRender.GetTextureByName($"bone_cost_{Mathf.Min(14, bonesCost)}"));

        if (bloodCost > 0)
            costTextures.Add(CardCostRender.GetTextureByName($"blood_cost_{Mathf.Min(14, bloodCost)}"));

        // get a list of the custom costs we need textures for
        // check for PlayableCard to account for possible dynamic costs (no API support but who knows what modders do)
        List<CardCostManager.FullCardCost> customCosts;
        if (playableCard != null)
            customCosts = playableCard.GetCustomCardCosts().Select(x => CardCostManager.AllCustomCosts.Find(c => c.CostName == x.CostName)).ToList();
        else
            customCosts = cardInfo.GetCustomCosts();

        foreach (CardCostManager.FullCardCost fullCost in customCosts)
        {
            string key = fullCost.CostName + cardInfo.GetCustomCost(fullCost.CostName);
            if (CardCostRender.AssembledTextures.ContainsKey(key))
            {
                if (CardCostRender.AssembledTextures[key] != null)
                    costTextures.Add(CardCostRender.AssembledTextures[key]);
                else
                    CardCostRender.AssembledTextures.Remove(key);
            }
            else
            {
                Texture2D costTex = fullCost.GetCostTexture?.Invoke(cardInfo.GetCustomCost(fullCost.CostName), cardInfo, playableCard);
                if (costTex != null)
                {
                    costTextures.Add(costTex);
                    CardCostRender.AssembledTextures.Add(key, costTex);
                }
            }
        }

        // Call the event and allow others to modify the list of textures
        UpdateCardCost?.Invoke(cardInfo, costTextures);
        return costTextures;
    }

    /*#region old
    public const int MOX_OFFSET = 21;

    public static Texture2D CombineMoxTextures(List<Texture2D> costs) => null;

    public static Texture2D CombineCostTextures(List<Texture2D> costs)
    {
        return null;
        while (costs.Count < 4)
            costs.Add(null);

        Texture2D baseTexture = TextureHelper.GetImageAsTexture("empty_cost.png", typeof(Part1CardCostRender).Assembly);
        return TextureHelper.CombineTextures(costs, baseTexture, yStep: COST_OFFSET);
    }

    [HarmonyPrefix, HarmonyPatch(typeof(CardDisplayer), nameof(CardDisplayer.SetCostSprite))]
    private static bool Part1And2CardCostDisplayerPatch(CardDisplayer __instance)
    {
        // Make sure we are in Leshy's Cabin
        if (__instance is CardDisplayer3D && SceneLoader.ActiveSceneName.StartsWith("Part1"))
            return false;
        
        else if (__instance is PixelCardDisplayer && PatchPlugin.act2CostRender.Value)
            return false;

        return true;
    }
    #endregion*/
}