using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using DiskCardGame;
using HarmonyLib;
using Sirenix.Serialization.Utilities;
using UnityEngine;

namespace InscryptionAPI.Card;

[HarmonyPatch]
public static class CardManager
{
    public static readonly ReadOnlyCollection<CardInfo> BaseGameCards = new(Resources.LoadAll<CardInfo>("Data/Cards"));
    private static readonly ObservableCollection<CardInfo> NewCards = new();
    
    public static event Func<List<CardInfo>, List<CardInfo>> ModifyCardList;

    static CardManager()
    {
        AllCards = BaseGameCards.ToList();
        NewCards.CollectionChanged += static (_, _) =>
        {
            var cards = BaseGameCards.Append(NewCards).ToList();
            AllCards = ModifyCardList?.Invoke(cards) ?? cards;
        };
    }

    public static List<CardInfo> AllCards { get; private set; }

    public static void Add(CardInfo newCard) => NewCards.Add(newCard);
    public static void Remove(CardInfo card) => NewCards.Remove(card);

    [HarmonyPrefix]
    [HarmonyPatch(typeof(ScriptableObjectLoader<UnityObject>), nameof(ScriptableObjectLoader<UnityObject>.LoadData))]
    [SuppressMessage("Member Access", "Publicizer001", Justification = "Need to set internal list of cards")]
    private static void CardLoadPrefix()
    {
        ScriptableObjectLoader<CardInfo>.allData = AllCards;
    }
}
