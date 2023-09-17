using System.Collections;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;
using DiskCardGame;
using HarmonyLib;
using InscryptionAPI.Card;
using InscryptionAPI.Helpers.Extensions;
using InscryptionAPI.Saves;
using UnityEngine;

namespace InscryptionAPI.Totems;

[HarmonyPatch(typeof(BuildTotemSequencer), "GenerateTotemChoices", new System.Type[] { typeof(BuildTotemNodeData), typeof(int) })]
internal static class ItemsUtil_AllConsumables
{
    internal static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        if (InscryptionAPIPlugin.configCustomTotemTopTypes.Value == TotemManager.TotemTopState.Vanilla)
        {
            return instructions;
        }

        // === We want to turn this

        // List<Tribe> list = new()
        // {
        //     Tribe.Bird,
        //     Tribe.Canine,
        //     Tribe.Hooved,
        //     Tribe.Insect,
        //     Tribe.Reptile
        // };
        
        // ...
        
        // List<ItemData> list4 = new List<ItemData>();
        // while (list4.Count < num2)
        // {
        //     TotemBottomData totemBottomData = new TotemBottomData();
        //     totemBottomData.effect = TotemEffect.CardGainAbility;
        //     totemBottomData.effectParams = new TotemBottomData.EffectParameters();
        //     if (list2.Count == 0)
        //     {
        //         totemBottomData.effectParams.ability = Ability.DrawRabbits;
        //     }
        //     else
        //     {
        //         int powerLevelRoll = SeededRandom.Range(0, 7, randomSeed++);
        //         List<Ability> list5 = list2.FindAll((Ability x) => AbilitiesUtil.GetInfo(x).powerLevel <= powerLevelRoll);
        //         if (list5.Count > 0)
        //         {
        //             totemBottomData.effectParams.ability = list5[SeededRandom.Range(0, list5.Count, randomSeed++)];
        //             list2.Remove(totemBottomData.effectParams.ability);
        //             list4.Add(totemBottomData);
        //         }
        //     }
        // }

        // === Into this

        // List<Tribe> list = new()
        // {
        //     Tribe.Bird,
        //     Tribe.Canine,
        //     Tribe.Hooved,
        //     Tribe.Insect,
        //     Tribe.Reptile
        // };
        // ItemsUtil_AllConsumables.AddCustomTribesToList(list);
        
        // ...
        
        // FillWithRandomTotemBottoms(...)

        // ===
        MethodInfo alternateTopsandbottoms = AccessTools.Method(typeof(TotemsUtil), nameof(TotemsUtil.AlternateTopsAndBottoms));
        MethodInfo totemBottomFiller = AccessTools.Method(typeof(ItemsUtil_AllConsumables), nameof(FillWithRandomTotemBottoms));
        

        bool initializedTotemTops = false;

        List<CodeInstruction> codes = new List<CodeInstruction>(instructions);
        for (int i = 0; i < codes.Count; i++)
        {
            OpCode opCode = codes[i].opcode;
            if (!initializedTotemTops && opCode == OpCodes.Newobj)
            {
                // Make a new list
                for (int j = i + 1; i < codes.Count; j++)
                {
                    if (codes[j].opcode == OpCodes.Stloc_0)
                    {
                        MethodInfo customMethod = AccessTools.Method(typeof(ItemsUtil_AllConsumables), nameof(AddCustomTribesToList), new Type[] { typeof(List<Tribe>) });

                        // Stored the list
                        codes.Insert(j + 1, new CodeInstruction(OpCodes.Ldloc_0));
                        codes.Insert(j + 2, new CodeInstruction(OpCodes.Call, customMethod));
                        initializedTotemTops = true;
                        break;
                    }
                }
            }
            else if (codes[i].operand == alternateTopsandbottoms)
            {
                InscryptionAPIPlugin.Logger.LogInfo($"Found TotemBottomConstructor");

                int index = i - 2; // AlternateTopsAndBottoms has 2 arguments so so this before
                codes.Insert(index++, new CodeInstruction(OpCodes.Ldloc_S, 5));
                codes.Insert(index++, new CodeInstruction(OpCodes.Ldarga, 2));
                codes.Insert(index++, new CodeInstruction(OpCodes.Call, totemBottomFiller));
                break;
            }
        }
        /*InscryptionAPIPlugin.Logger.LogInfo($"DONEZ");
        foreach (CodeInstruction code in codes)
        {
            InscryptionAPIPlugin.Logger.LogInfo(code.ToString());
        }*/

        return codes;
    }

    internal static List<ItemData> GenerateTotemChoices(BuildTotemNodeData nodeData, int randomSeed)
    {
        List<ItemData> tops = GenerateTotemTopChoices(ref randomSeed);
        List<ItemData> bottoms = GenerateTotemBottomChoices(ref randomSeed);
        
        return TotemsUtil.AlternateTopsAndBottoms(tops, bottoms);
    }
    
    private static List<ItemData> GenerateTotemTopChoices(ref int randomSeed)
    {
        List<Tribe> list = new List<Tribe>
        {
            Tribe.Bird,
            Tribe.Canine,
            Tribe.Hooved,
            Tribe.Insect,
            Tribe.Reptile
        };
        if (StoryEventsData.EventCompleted(StoryEvent.SquirrelHeadDiscovered))
        {
            list.Add(Tribe.Squirrel);
        }
        foreach (Tribe totemTop in RunState.Run.totemTops)
        {
            list.Remove(totemTop);
        }
        
        List<ItemData> tops = new List<ItemData>();
        for (int i = 0; i < list.Count; i++)
        {
            TotemTopData totemTopData = new TotemTopData();
            totemTopData.prerequisites = new TotemTopData.TriggerCardPrerequisites();
            totemTopData.prerequisites.tribe = list[i];
            list.Remove(totemTopData.prerequisites.tribe);
            tops.Add(totemTopData);
        }
        tops.SeededShuffle(randomSeed++);

        return tops;
    }
    
    private static List<ItemData> GenerateTotemBottomChoices(ref int randomSeed)
    {
        List<Ability> allAbilities = new List<Ability>(ProgressionData.Data.learnedAbilities);
        allAbilities.RemoveAll((Ability x) => RunState.Run.totemBottoms.Contains(x) || !AbilitiesUtil.GetInfo(x).metaCategories.Contains(AbilityMetaCategory.Part1Modular));
        
        List<ItemData> bottoms = new List<ItemData>();
        while(allAbilities.Count > 0)
        {
            TotemBottomData totemBottomData = ScriptableObject.CreateInstance<TotemBottomData>();
            totemBottomData.effect = TotemEffect.CardGainAbility;
            totemBottomData.effectParams = new TotemBottomData.EffectParameters();
            if (allAbilities.Count > 0)
            {
                int powerLevelRoll = SeededRandom.Range(0, 7, randomSeed++);
                List<Ability> list5 = allAbilities.FindAll((Ability x) => AbilitiesUtil.GetInfo(x).powerLevel <= powerLevelRoll);
                if (list5.Count > 0)
                {
                    totemBottomData.effectParams.ability = list5[SeededRandom.Range(0, list5.Count, randomSeed++)];
                    allAbilities.Remove(totemBottomData.effectParams.ability);
                }
            }
            else
            {
                allAbilities.RemoveAt(0);
            }
            
            bottoms.Add(totemBottomData);
        }

        return bottoms;
    }

    internal static void FillWithRandomTotemBottoms(List<ItemData> list, ref int seed)
    {
        InscryptionAPIPlugin.Logger.LogInfo($"Starting FillWithRandomTotemBottoms " + list.Count + " " + seed);
        int startAmount = list.Count;
        
        foreach (TotemBottomEffect effect in TotemManager.allBottomEffects)
        {
            List<TotemBottomData> collection = effect.GetAllOptions(seed);
            InscryptionAPIPlugin.Logger.LogInfo($"Filling " + effect.EffectID + " " + collection.Count);
            for (int i = 0; i < startAmount && i < collection.Count; i++)
            {
                int index = collection.GetSeededRandomIndex(seed++);
                TotemBottomData totemBottomData = collection[index];
                InscryptionAPIPlugin.Logger.LogInfo($"totemBottomData " + totemBottomData + " " + totemBottomData.effect + " " + totemBottomData.effectParams);
                list.Add(totemBottomData);
                collection.RemoveAt(index);
            }
            InscryptionAPIPlugin.Logger.LogInfo($"Complete " + list.Count);
        }
        
        if (list.Count > startAmount)
        {
            list.SeededShuffle(seed);
            list.RemoveRange(startAmount, list.Count - startAmount);
        }
        
        InscryptionAPIPlugin.Logger.LogInfo($"Done FillWithRandomTotemBottoms " + list.Count + " " + seed);
    }

    internal static void AddCustomTribesToList(List<Tribe> list)
    {
        // get a list of all cards with a tribe
        List<CardInfo> tribedCards = CardManager.AllCardsCopy.FindAll(x => x.tribes.Count > 0);

        // iterate across all custom tribes that are obtainable as tribe choices
        foreach (TribeManager.TribeInfo tribeInfo in TribeManager.NewTribes.Where(x => x.tribeChoice))
        {
            // Only add if we have at least 1 card of it
            if (tribedCards.Exists(ci => ci.IsOfTribe(tribeInfo.tribe)))
                list.Add(tribeInfo.tribe);
        }

        // remove tribes without any cards
        list.RemoveAll(x => !tribedCards.Exists(ci => ci.IsOfTribe(x)));
    }
}

[HarmonyPatch(typeof(ResourceBank), "Awake", new System.Type[] { })]
internal static class ResourceBank_Awake
{
    internal static void Postfix(ResourceBank __instance)
    {
        // The resource bank has been cleared. refill it
        if (ResourceBank.Get<GameObject>(TotemManager.CustomTotemTopResourcePath) == null)
        {
            TotemManager.Initialize();
        }
    }
}

[HarmonyPatch(typeof(Totem), "GetTopPiecePrefab", new Type[] { typeof(TotemTopData) })]
internal static class Totem_GetTopPiecePrefab
{
    internal static bool Prefix(Totem __instance, TotemTopData data, ref GameObject __result)
    {
        if (TribeManager.IsCustomTribe(data.prerequisites.tribe))
        {
            TotemManager.CustomTotemTop customTribeTotem = TotemManager.totemTops.Find((a) => a.Tribe == data.prerequisites.tribe);
            if (customTribeTotem != null)
            {
                // Get custom totem model
                __result = customTribeTotem.Prefab;
            }
            else
            {
                // No custom totem model - use default model
                __result = TotemManager.defaultTotemTop.Prefab;
            }
            return false;
        }
        else if (InscryptionAPIPlugin.configCustomTotemTopTypes.Value == TotemManager.TotemTopState.AllTribes)
        {
            __result = TotemManager.defaultTotemTop.Prefab;
            return false;
        }

        return true;
    }
}

[HarmonyPatch(typeof(Totem), "SetData", new Type[] { typeof(ItemData) })]
internal static class Totem_SetData
{
    internal static bool Prefix(Totem __instance, ItemData data)
    {
        InscryptionAPIPlugin.Logger.LogInfo($"[Totem_SetData] data {data}");
        __instance.Data = data; // Base.SetData(data);

        TotemBottomData totemBottomData = __instance.TotemItemData.bottom;

        Type bottomEffectScript = null;
        if (totemBottomData.effect == TotemEffect.CardGainAbility)
        {
            bottomEffectScript = typeof(CardGainAbility);
        }
        else
        {
            TotemManager.CustomTotemBottom effect = TotemManager.totemBottoms.Find((a) => a.EffectID == totemBottomData.effect);
            if (effect != null)
            {
                bottomEffectScript = effect.TriggerReceiver;
            }
            else
            {
                InscryptionAPIPlugin.Logger.LogError($"[Totem_SetData] No Custom Effect class found {totemBottomData.effect}");
            }
        }
        
        __instance.gameObject.AddComponent(bottomEffectScript);
        
        GameObject totemTop = UnityObject.Instantiate(__instance.GetTopPiecePrefab(__instance.TotemItemData.top), __instance.topPieceParent);
        totemTop.GetComponentInChildren<CompositeTotemPiece>().SetData(__instance.TotemItemData.top);
        
        GameObject bottomPiecePrefab = __instance.GetBottomPiecePrefab(totemBottomData);
        GameObject totemBottom = UnityObject.Instantiate(bottomPiecePrefab, __instance.bottomPieceParent);
        CompositeTotemPiece bottomCompositePiece = totemBottom.GetComponent<CompositeTotemPiece>();
        InscryptionAPIPlugin.Logger.LogInfo($"[Totem_SetData] BottomPiece {bottomCompositePiece}");
        bottomCompositePiece.SetData(totemBottomData);
        
        InscryptionAPIPlugin.Logger.LogInfo($"[Totem_SetData] Done {data}");
        return false;
    }
}

[HarmonyPatch(typeof(TotemItemSlot), "CreateItem", new Type[]{typeof(TotemDefinition), typeof(bool)})]
internal static class TotemItemSlot_CreateItem
{
    internal static bool Prefix(TotemItemSlot __instance, TotemDefinition totemDefinition, bool skipDropAnimation)
    {
        InscryptionAPIPlugin.Logger.LogInfo($"[TotemItemSlot_CreateItem] Prefix " + totemDefinition);
        TotemItemData data = new TotemItemData();
        data.top = ((CustomTotemDefinition)totemDefinition).MakeTop();
        data.bottom = ((CustomTotemDefinition)totemDefinition).MakeBottom();
        
        InscryptionAPIPlugin.Logger.LogInfo($"[TotemItemSlot_CreateItem] Bottom " + data.bottom);
        __instance.CreateItem(data, skipDropAnimation);
        
        /*Totem totemItem = (Totem)__instance.Item;
        TriggerReceiver triggerReceiver = totemItem.GetComponent<TriggerReceiver>();
        InscryptionAPIPlugin.Logger.LogInfo($"[TotemItemSlot_CreateItem] triggerReceiver " + triggerReceiver);
        if (triggerReceiver == null)
        {
            TotemManager.CustomTotemBottom bottom = TotemManager.totemBottoms.Find((a)=>a.EffectID == data.bottom.effect);
            totemItem.gameObject.AddComponent(bottom.TriggerReceiver);
            InscryptionAPIPlugin.Logger.LogInfo($"[TotemItemSlot_CreateItem] added triggerReceiver " + bottom.TriggerReceiver);
        }*/
        return false;
    }
    
    internal static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        if (InscryptionAPIPlugin.configCustomTotemTopTypes.Value == TotemManager.TotemTopState.Vanilla)
        {
            return instructions;
        }

        // === We want to turn this

        // totemDefinition

        // === Into this

        // (CustomTotemDefinition)totemDefinition

        // ===
        InscryptionAPIPlugin.Logger.LogInfo($"[TotemItemSlot_CreateItem]");
        
        List<CodeInstruction> codes = new List<CodeInstruction>(instructions);
        for (int i = 0; i < codes.Count; i++)
        {
            if (codes[i].opcode == OpCodes.Ldarg_1)
            {
                codes.Insert(++i, new CodeInstruction(OpCodes.Castclass, typeof(CustomTotemDefinition)));
            }
        }
        
        InscryptionAPIPlugin.Logger.LogInfo($"[TotemItemSlot_CreateItem] DONEZ");
        foreach (CodeInstruction code in codes)
        {
            InscryptionAPIPlugin.Logger.LogInfo(code.ToString());
        }

        return codes;
    }
}

[HarmonyPatch(typeof(TotemTopData), "PrefabId", MethodType.Getter)]
internal static class TotemTopData_PrefabId
{
    internal static bool Prefix(TotemTopData __instance, ref string __result)
    {
        // Custom totem tops will always use the fallback UNLESS there is an override
        if (TribeManager.IsCustomTribe(__instance.prerequisites.tribe))
        {
            TotemManager.CustomTotemTop customTribeTotem = TotemManager.totemTops.Find((a) => a.Tribe == __instance.prerequisites.tribe);
            if (customTribeTotem == null)
            {
                __result = TotemManager.CustomTotemTopID;
                return false;
            }
        }
        else if (InscryptionAPIPlugin.configCustomTotemTopTypes.Value == TotemManager.TotemTopState.AllTribes)
        {
            // All non-custom tribes will use the fallback model 
            __result = TotemManager.CustomTotemTopID;
            return false;
        }

        return true;
    }
}

[HarmonyPatch(typeof(Part1Opponent), "TryModifyCardWithTotem", new Type[]{typeof(PlayableCard)})]
internal static class Part1Opponent_TryModifyCardWithTotem
{
    internal static bool Prefix(Part1Opponent __instance, PlayableCard card)
    {
        if (__instance.totem == null || !card.Info.IsOfTribe(__instance.totem.TotemItemData.top.prerequisites.tribe))
            return false;
        
        card.StatsLayer.SetEmissionColor(__instance.InteractablesGlowColor);
        if (card.TemporaryMods.Exists((CardModificationInfo x) => x.fromTotem))
            return false;
        
        CardModificationInfo mod = new CardModificationInfo();
        mod.fromTotem = true;

        if (__instance.totem.TotemItemData.bottom.effect == TotemEffect.CardGainAbility)
        {
            // vanilla effects
            mod.abilities = new List<Ability>()
            {
                __instance.totem.TotemItemData.bottom.effectParams.ability
            };
        }
        else
        {
            // custom effects
            TotemBottomEffect effect = TotemManager.allBottomEffects.Find((a) => a.EffectID == __instance.totem.TotemItemData.bottom.effect);
            if (effect != null)
            {
                effect.ModifyCardWithTotem(__instance.totem.TotemItemData.bottom, mod);
            }
            else
            {
                InscryptionAPIPlugin.Logger.LogError($"Could not find totem data for totem bottom {__instance.totem.TotemItemData.bottom.name} {__instance.totem.TotemItemData.bottom.PrefabId}");
                mod.abilities = new List<Ability>()
                {
                    Ability.DrawRabbits
                };
            }
        }

        card.AddTemporaryMod(mod);
        return false;
    }
}

[HarmonyPatch]
internal static class BuildTotemSequencer_NewPiecePhase
{
    private static Type NewPiecePhaseClass = Type.GetType("DiskCardGame.BuildTotemSequencer+<NewPiecePhase>d__13, Assembly-CSharp, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null");
    private static Type selectedSlotClass = Type.GetType("DiskCardGame.BuildTotemSequencer.<>c__DisplayClass13_0, Assembly-CSharp, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null");
    
    public static IEnumerable<MethodBase> TargetMethods()
    {
        yield return AccessTools.Method(NewPiecePhaseClass, "MoveNext");
    }
    
    internal static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        //InscryptionAPIPlugin.Logger.LogInfo($"[BuildTotemSequencer_NewPiecePhase] Starting");
        // === We want to turn this

        // base.DisableSlotsAndExitItems(selectedSlot);

        // === Into this

        // AdjustSavedTotemData(selectedSlot);
        // base.DisableSlotsAndExitItems(selectedSlot);

        // ===
        MethodInfo DisableSlotsAndExitItems = AccessTools.Method(typeof(SelectItemsSequencer), nameof(SelectItemsSequencer.DisableSlotsAndExitItems));
        MethodInfo AdjustSavedTotems = AccessTools.Method(typeof(BuildTotemSequencer_NewPiecePhase), nameof(BuildTotemSequencer_NewPiecePhase.AdjustSavedTotemData));
        //InscryptionAPIPlugin.Logger.LogInfo($"[BuildTotemSequencer_NewPiecePhase] DisableSlotsAndExitItems {DisableSlotsAndExitItems}");
        //InscryptionAPIPlugin.Logger.LogInfo($"[BuildTotemSequencer_NewPiecePhase] AdjustSavedTotems {AdjustSavedTotems}");
        

        List<CodeInstruction> codes = new List<CodeInstruction>(instructions);
        for (int i = 0; i < codes.Count; i++)
        {
            if (codes[i].operand == DisableSlotsAndExitItems)
            {
                InscryptionAPIPlugin.Logger.LogInfo($"[BuildTotemSequencer_NewPiecePhase] Found DisableSlotsAndExitItems");
                codes.Insert(i++, new CodeInstruction(OpCodes.Call, AdjustSavedTotems));
                break;
            }
        }
        
        /*InscryptionAPIPlugin.Logger.LogInfo($"DONEZ");
        foreach (CodeInstruction code in codes)
        {
            InscryptionAPIPlugin.Logger.LogInfo(code.ToString());
        }*/

        return codes;
    }

    internal static SelectableItemSlot AdjustSavedTotemData(SelectableItemSlot slot)
    {
        InscryptionAPIPlugin.Logger.LogInfo($"[BuildTotemSequencer_NewPiecePhase] slot " + slot);
        if (slot.Item.Data is TotemBottomData totemBottomData)
        {
            InscryptionAPIPlugin.Logger.LogInfo($"[BuildTotemSequencer_NewPiecePhase] Saving {totemBottomData.effect} to save data");
            
            // Add to Modded save
            CustomTotemBottom CustomBottom = new CustomTotemBottom()
            {
                EffectID = totemBottomData.effect,
                EffectParameters = totemBottomData.effectParams
            };
            
            // if (totemBottomData.effect == TotemEffect.CardGainAbility)
            // {
            //     CustomBottom.EffectParameters = new TotemBottomData.EffectParameters()
            //     {
            //         ability = RunState.Run.totemBottoms[RunState.Run.totemBottoms.Count - 1]
            //     };
            // }
            // else
            // {
            //     CustomBottom.EffectParameters = new TotemBottomData.EffectParameters()
            //     {
            //         ability = RunState.Run.totemBottoms[RunState.Run.totemBottoms.Count - 1]
            //     };
            // }
            
            // Remove item (Vanilla code already added it)
            RunState.Run.totemBottoms.RemoveAt(RunState.Run.totemBottoms.Count - 1);
            
            // Add to API
            CustomTotemsSaveData data = TotemManager.RunStateCustomTotems;
            data.TotemBottoms.Add(CustomBottom);
            TotemManager.RunStateCustomTotems = data; // Save
        }

        InscryptionAPIPlugin.Logger.LogInfo($"[BuildTotemSequencer_NewPiecePhase] Done");
        return slot;
    }
}

[HarmonyPatch]
internal static class ReplaceTotemBottomsWithAPI
{
    private static string AssemblyNamae = ", Assembly-CSharp, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null";
    
    private static Type GenerateTotemChoicesClass = Type.GetType("DiskCardGame.BuildTotemSequencer+<>c" + AssemblyNamae);

    public static IEnumerable<MethodBase> TargetMethods()
    {
        yield return AccessTools.Method(typeof(BuildTotemSequencer), nameof(BuildTotemSequencer.AutoAssembleTotem)); // RunState.Run.totemBottoms.Count
        yield return AccessTools.Method(GenerateTotemChoicesClass, "<GenerateTotemChoices>b__26_0"); // Contains & Count
    }
    
    internal static IEnumerable<CodeInstruction> Transpiler(MethodBase original, IEnumerable<CodeInstruction> instructions)
    {
        // === We want to turn this

        // RunState.Run.totemBottoms.Count
        // RunState.Run.totemBottoms.Contains

        // === Into this

        // TotemManager.RunStateCustomTotemBottoms.Count(RunState.Run.totemBottoms.Count)
        // TotemManager.RunStateCustomTotemBottoms.ContainsCardGainAbility

        // ===
        MethodInfo RunStateRun = typeof(RunState).GetProperty(nameof(RunState.Run)).GetMethod;
        FieldInfo TotemBottoms = typeof(RunState).GetField(nameof(RunState.totemBottoms));
        MethodInfo Count = typeof(List<Ability>).GetProperty(nameof(List<Ability>.Count)).GetMethod;
        MethodInfo Contains = AccessTools.Method(typeof(List<Ability>), nameof(List<Ability>.Contains));
        
        MethodInfo RunStateCustomTotems = typeof(TotemManager).GetProperty(nameof(TotemManager.RunStateCustomTotems)).GetMethod;
        MethodInfo FillerMethod = AccessTools.Method(typeof(CustomTotemsSaveData), nameof(CustomTotemsSaveData.DoesNothing));
        MethodInfo CustomTotemCountMethod = AccessTools.Method(typeof(CustomTotemsSaveData), nameof(CustomTotemsSaveData.TotemsBottomCount));
        MethodInfo CustomTotemContainsMethod = AccessTools.Method(typeof(CustomTotemsSaveData), nameof(CustomTotemsSaveData.ContainsCardGainAbility));

        List<CodeInstruction> codes = new List<CodeInstruction>(instructions);
        for (int i = 0; i < codes.Count; i++)
        {
            object currentOperand = codes[i].operand;
            object nextOperand = i+1 < codes.Count ? codes[i+1].operand : null;
            object nextNextOperand = i+2 < codes.Count ? codes[i+2].operand : null;
            object nextNextNextOperand = i+3 < codes.Count ? codes[i+3].operand : null;

            // RunState.Run.totemBottoms.Count
            if (currentOperand == RunStateRun && nextOperand == TotemBottoms && nextNextOperand == Count)
            {
                codes[i].operand = RunStateCustomTotems;
                codes[i+1] = new CodeInstruction(OpCodes.Call, FillerMethod); // Can't remove this method call so replace it with something that technically does nothing
                codes[i+2] = new CodeInstruction(OpCodes.Call, CustomTotemCountMethod);
            }
            else if (currentOperand == RunStateRun && nextOperand == TotemBottoms && nextNextNextOperand == Contains)
            {
                // C: RunState.Run
                // N: Run.totemBottoms
                // NN: Ability.Brittle
                // NNN: totemBottoms.Contains
                codes[i].operand = RunStateCustomTotems;
                codes[i+1] = new CodeInstruction(OpCodes.Call, FillerMethod); // Can't remove this method call so replace it with something that technically does nothing
                codes[i+3] = new CodeInstruction(OpCodes.Call, CustomTotemContainsMethod);
            }
        }
        
        return codes;
    }
}

[HarmonyPatch]
internal static class BuildTotemSequencer_FillInventorySlots
{
    private static Type classType = Type.GetType("DiskCardGame.BuildTotemSequencer+<FillInventorySlots>d__19, Assembly-CSharp, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null");
    
    public static IEnumerable<MethodBase> TargetMethods()
    {
        yield return AccessTools.Method(classType, "MoveNext");
    }
    
    internal static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        // === We want to turn this

        // List<ItemData> inventory = TotemsUtil.AlternateTopsAndBottoms(list, list2);

        // === Into this

        // FillWithRandomTotemBottoms(...)
        // List<ItemData> inventory = TotemsUtil.AlternateTopsAndBottoms(list, list2);

        // ===
        MethodInfo alternateTopsandbottoms = AccessTools.Method(typeof(TotemsUtil), nameof(TotemsUtil.AlternateTopsAndBottoms));
        MethodInfo totemBottomFiller = AccessTools.Method(typeof(BuildTotemSequencer_FillInventorySlots), nameof(AddTotemsFromSave));
        

        List<CodeInstruction> codes = new List<CodeInstruction>(instructions);
        for (int i = 0; i < codes.Count; i++)
        {
            if (codes[i].operand == alternateTopsandbottoms)
            {
                codes.Insert(i++, new CodeInstruction(OpCodes.Ldloc_3));
                codes.Insert(i++, new CodeInstruction(OpCodes.Call, totemBottomFiller));
                break;
            }
        }
        
        return codes;
    }

    internal static void AddTotemsFromSave(List<ItemData> list)
    {
        list.Clear();
        foreach (CustomTotemBottom bottom in TotemManager.RunStateCustomTotems.TotemBottoms)
        {
            TotemBottomData totemBottomData = ScriptableObject.CreateInstance<TotemBottomData>();
            totemBottomData.effectParams = new TotemBottomData.EffectParameters();
            
            TotemManager.CustomTotemBottom totemBottom = TotemManager.totemBottoms.Find((a)=>a.EffectID == bottom.EffectID);
            if (totemBottom == null)
            {
                totemBottomData.effect = TotemEffect.CardGainAbility; // vanilla only has this 1 ability
                totemBottomData.effectParams.ability = bottom.EffectParameters.ability;
            }
            else
            {
                totemBottomData.effect = totemBottom.EffectID;
                totemBottomData.effectParams = bottom.EffectParameters;
            }
            
            // Custom totem bottom
            list.Add(totemBottomData);
            if (list.Count > 3)
            {
                break;
            }
        }
    }
}

[HarmonyPatch]
internal static class BuildTotemSequencer_BuildPhase
{
    /// <summary>
    /// This patch was made as a hack so we can insert custom totem bottoms into the API
    /// This was tried with a transpiler to replace functions but it resulted in the game skipping the selection process and isntead
    /// it zoomed into the select piece position, played dialogue, took the mask off and exited the node.  
    /// </summary>
    /// <returns></returns>
    [HarmonyPostfix, HarmonyPatch(typeof(BuildTotemSequencer), nameof(BuildTotemSequencer.BuildPhase))]
    public static IEnumerator Postfix(IEnumerator enumerator, BuildTotemSequencer __instance)
    {
        __instance.selectedTop = null;
        __instance.selectedBottom = null;
		Singleton<ViewManager>.Instance.Controller.LockState = ViewLockState.Locked;
		if (RunState.Run.totemTops.Count + TotemManager.RunStateCustomTotems.TotemsBottomCount() > 2 && RunState.Run.totemTops.Count > 0 && TotemManager.RunStateCustomTotems.TotemsBottomCount() > 0)
		{
            __instance.SetInventorySlotsSelectable(selectable: true);
			Singleton<ViewManager>.Instance.SwitchToView(View.TotemInventory);
			yield return new WaitForSeconds(0.05f);
			foreach (SelectableItemSlot inventorySlot in __instance.inventorySlots)
			{
				inventorySlot.CursorSelectStarted = (Action<MainInputInteractable>)Delegate.Combine(inventorySlot.CursorSelectStarted, (Action<MainInputInteractable>)delegate(MainInputInteractable i)
				{
                    __instance.OnSlotSelectedForBuild(i as SelectableItemSlot);
				});
				inventorySlot.CursorEntered = (Action<MainInputInteractable>)Delegate.Combine(inventorySlot.CursorEntered, (Action<MainInputInteractable>)delegate(MainInputInteractable i)
				{
					Singleton<OpponentAnimationController>.Instance.SetLookTarget(i.transform, Vector3.up * 2f);
				});
			}
		}
		else
		{
			if (!__instance.AutoAssembleTotem())
			{
				yield return LeshyAnimationController.Instance.TakeOffMask();
				if (!ProgressionData.LearnedMechanic(MechanicsConcept.BuildingTotems))
				{
					yield return new WaitForSeconds(0.25f);
					yield return Singleton<TextDisplayer>.Instance.PlayDialogueEvent("TutorialWoodcarverIncomplete", TextDisplayer.MessageAdvanceMode.Input);
					yield return new WaitForSeconds(0.25f);
				}
				else
				{
					yield return new WaitForSeconds(0.25f);
					yield return Singleton<TextDisplayer>.Instance.PlayDialogueEvent("WoodcarverOutro", TextDisplayer.MessageAdvanceMode.Input);
					yield return new WaitForSeconds(0.75f);
				}
				yield break;
			}
			__instance.selectedTop = __instance.inventorySlots.Find((SelectableItemSlot x) => x.Item != null && x.Item.Data is TotemTopData);
			__instance.selectedBottom = __instance.inventorySlots.Find((SelectableItemSlot x) => x.Item != null && x.Item.Data is TotemBottomData);
			yield return new WaitForSeconds(0.2f);
            __instance.selectedTop.Item.PlayExitAnimation();
			yield return new WaitForSeconds(0.1f);
            __instance.selectedBottom.Item.PlayExitAnimation();
			yield return new WaitForSeconds(0.1f);
		}
		bool totemConfirmed = false;
		SelectableItemSlot selectableItemSlot = __instance.completeTotemSlot;
		selectableItemSlot.CursorSelectStarted = (Action<MainInputInteractable>)Delegate.Combine(selectableItemSlot.CursorSelectStarted, (Action<MainInputInteractable>)delegate
		{
			totemConfirmed = __instance.completeTotemSlot.Item != null;
		});
		while (!totemConfirmed)
		{
			if (!__instance.AutoAssembleTotem())
			{
				Singleton<ViewManager>.Instance.Controller.SwitchToControlMode(ViewController.ControlMode.TotemBuilding);
				Singleton<ViewManager>.Instance.Controller.LockState = ViewLockState.Unlocked;
			}
			yield return new WaitUntil(() => __instance.selectedTop != null && __instance.selectedBottom != null);
			Singleton<RuleBookController>.Instance.SetShown(shown: false);
			Singleton<ViewManager>.Instance.Controller.SwitchToControlMode(ViewController.ControlMode.TotemPieceSelection);
			Singleton<ViewManager>.Instance.SwitchToView(View.Default);
			Singleton<OpponentAnimationController>.Instance.ClearLookTarget();
			yield return __instance.AssembleTotem(__instance.selectedTop, __instance.selectedBottom);
			if (!__instance.AutoAssembleTotem())
			{
                __instance.returnToInventoryInteractable.SetEnabled(enabled: true);
			}
			yield return new WaitUntil(() => __instance.selectedTop == null || __instance.selectedBottom == null || totemConfirmed);
            __instance.returnToInventoryInteractable.SetEnabled(enabled: false);
			Singleton<RuleBookController>.Instance.SetShown(shown: false);
		}
		Singleton<ViewManager>.Instance.Controller.LockState = ViewLockState.Locked;
		foreach (SelectableItemSlot inventorySlot2 in __instance.inventorySlots)
		{
			inventorySlot2.ClearDelegates();
		}
		TotemItemData completedTotem = __instance.completeTotemSlot.Item.Data as TotemItemData;
		if (!ProgressionData.LearnedMechanic(MechanicsConcept.BuildingTotems))
		{
			string[] variableStrings = new string[2]
			{
				Localization.Translate(AbilitiesUtil.GetInfo(completedTotem.bottom.effectParams.ability).rulebookName),
				Localization.Translate(completedTotem.top.prerequisites.tribe.ToString())
			};
			yield return Singleton<TextDisplayer>.Instance.PlayDialogueEvent("TutorialWoodcarverComplete", TextDisplayer.MessageAdvanceMode.Input, TextDisplayer.EventIntersectMode.Wait, variableStrings);
			ProgressionData.SetMechanicLearned(MechanicsConcept.BuildingTotems);
		}
		else
		{
			yield return Singleton<TextDisplayer>.Instance.PlayDialogueEvent("WoodcarverOutro", TextDisplayer.MessageAdvanceMode.Input);
		}
		TotemDefinition totemDefinition = CreateCustomTotemDefinition(completedTotem);
		RunState.Run.totems.Clear();
		RunState.Run.totems.Add(totemDefinition);
		__instance.completeTotemSlot.SetEnabled(enabled: false);
		__instance.completeTotemSlot.ClearDelegates();
		__instance.completeTotemSlot.Item.PlayExitAnimation();
		UnityEngine.Object.Destroy(__instance.completeTotemSlot.Item.gameObject, 0.25f);
		yield return new WaitForSeconds(0.3f);
		Singleton<ItemsManager>.Instance.UpdateItems();
		Singleton<OpponentAnimationController>.Instance.ClearLookTarget();
		yield return LeshyAnimationController.Instance.TakeOffMask();
		yield return new WaitForSeconds(1f);
	}
    
    internal static TotemDefinition CreateCustomTotemDefinition(TotemItemData bottomData)
    {
        CustomTotemDefinition definition = null;
        if (bottomData.bottom.effect == TotemEffect.CardGainAbility)
        {
            definition = new CustomTotemDefinition()
            {
                BottomEffectID = bottomData.bottom.effect,
                ability = bottomData.bottom.effectParams.ability,
                tribe = bottomData.top.prerequisites.tribe,
            };
        }
        else
        {
            definition = new CustomTotemDefinition()
            {
                BottomEffectID = bottomData.bottom.effect,
                BottomEffectParameters = bottomData.bottom.effectParams,
                tribe = bottomData.top.prerequisites.tribe,
            };
        }
        
        InscryptionAPIPlugin.Logger.LogInfo($"[CreateCustomTotemDefinition] " + definition.BottomEffectID + " " + definition.ability);
        return definition;
    }
}

[HarmonyPatch(typeof(ItemSlot), "CreateItem", new Type[] { typeof(ItemData), typeof(bool) })]
internal class ItemSlot_CreateItem
{
    public static bool Prefix(ItemSlot __instance, ItemData data, bool skipDropAnimation)
    {
        if (data == null)
        {
            InscryptionAPIPlugin.Logger.LogError($"Failed create item. ItemData is null!");
            return false;
        }
        if (data is not TotemBottomData bottomData)
        {
            // We only care about TotemBottomData
            InscryptionAPIPlugin.Logger.LogInfo($"[ItemSlot_CreateItem] {data} {data.GetType()}");
            return true;
        }
        if (__instance == null)
        {
            InscryptionAPIPlugin.Logger.LogError($"Failed create totem item. Item slot not specified for item using prefab id '{data.PrefabId}' ItemData is null!");
            return false;
        }
        
        InscryptionAPIPlugin.Logger.LogInfo($"Making Totem Bottom " + bottomData.prefabId);
        if (__instance.Item != null)
        {
            UnityObject.Destroy(__instance.Item.gameObject);
        }
        
        
        TotemManager.CustomTotemBottom totemBottom = TotemManager.totemBottoms.Find((a) => a.EffectID == bottomData.effect);
        if (totemBottom == null)
        {
            // no custom data
            InscryptionAPIPlugin.Logger.LogInfo($"Making Vanilla Totem Bottom " + bottomData.effect);
            return true;
        }

        InscryptionAPIPlugin.Logger.LogInfo($"Making Custom Totem Bottom " + totemBottom.RulebookName);
        GameObject gameObject = UnityObject.Instantiate(totemBottom.Prefab, __instance.transform);

        if (!gameObject.activeSelf)
        {
            gameObject.SetActive(true);
        }

        gameObject.transform.localPosition = Vector3.zero;

        /*if (!gameObject.TryGetComponent(out TotemTriggerReceiver triggerReceiver))
        {
            gameObject.AddComponent(totemBottom.TriggerReceiver);
        }*/
        if (!gameObject.TryGetComponent(out CompositeTotemPiece compositeTotemPiece))
        {
            gameObject.AddComponent(totemBottom.CompositeType);
        }
        InscryptionAPIPlugin.Logger.LogInfo($"CompositeTotemPiece " + compositeTotemPiece);
        
        __instance.Item = compositeTotemPiece;
        __instance.Item.SetData(data);
        if (skipDropAnimation)
        {
            __instance.Item.PlayEnterAnimation(true);
        }

        InscryptionAPIPlugin.Logger.LogInfo($"[ItemSlot_CreateItem] Done");
        return false;
    }
}

[HarmonyPatch(typeof(CompositeTotemPiece), nameof(CompositeTotemPiece.Start), new Type[]{})]
internal class CompositeTotemPiece_Start
{
    public static bool Prefix(CompositeTotemPiece __instance)
    {
        if (__instance is CustomIconTotemBottomPiece customIconTotemBottomPiece)
        {
            customIconTotemBottomPiece.AssignEmittingIcon();
        }
        
        // Change icon emission if we have a renderer for it
        return __instance.emissiveRenderer != null;
    }
}
