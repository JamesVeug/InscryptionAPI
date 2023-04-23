using System.Reflection;
using System.Reflection.Emit;
using DiskCardGame;
using HarmonyLib;
using InscryptionAPI.Card;
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

    internal static void FillWithRandomTotemBottoms(List<ItemData> list, ref int seed)
    {
        InscryptionAPIPlugin.Logger.LogInfo($"Starting FillWithRandomTotemBottoms " + list.Count + " " + seed);
        foreach (TotemBottomEffect effect in TotemManager.allBottomEffects)
        {
            list.AddRange(effect.GetAllOptions(seed++));
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
        InscryptionAPIPlugin.Logger.LogInfo(Environment.StackTrace);
        TotemItemData data = new TotemItemData();
        data.top = ((CustomTotemDefinition)totemDefinition).MakeTop();
        data.bottom = ((CustomTotemDefinition)totemDefinition).MakeBottom();
        
        InscryptionAPIPlugin.Logger.LogInfo($"[TotemItemSlot_CreateItem] Bottom " + data.bottom);
        __instance.CreateItem(data, skipDropAnimation);
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

[HarmonyDebug]
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
        
        CardModificationInfo info = new CardModificationInfo();
        info.fromTotem = true;

        TotemBottomEffect effect = TotemManager.allBottomEffects.Find((a)=>a.EffectID == __instance.totem.TotemItemData.bottom.effect);
        if (effect != null)
        {
            effect.ModifyCardWithTotem(__instance.totem.TotemItemData.bottom, info);
        }
        else
        {
            InscryptionAPIPlugin.Logger.LogError($"Could not find totem data for totem bottom {__instance.totem.TotemItemData.bottom.name} {__instance.totem.TotemItemData.bottom.PrefabId}");
            info.abilities = new List<Ability>()
            {
                Ability.DrawRabbits
            };
        }

        card.AddTemporaryMod(info);
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
                EffectID = totemBottomData.effect
            };
            
            if (totemBottomData.effect == TotemEffect.CardGainAbility)
            {
                CustomBottom.Ability = RunState.Run.totemBottoms[RunState.Run.totemBottoms.Count - 1];
            }
            
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
    
    private static Type BuildPhaseClass = Type.GetType("DiskCardGame.BuildTotemSequencer+<BuildPhase>d__14" + AssemblyNamae);
    private static Type GenerateTotemChoicesClass = Type.GetType("DiskCardGame.BuildTotemSequencer+<>c" + AssemblyNamae);

    public static IEnumerable<MethodBase> TargetMethods()
    {
        yield return AccessTools.Method(typeof(BuildTotemSequencer), nameof(BuildTotemSequencer.AutoAssembleTotem)); // RunState.Run.totemBottoms.Count
        
        yield return AccessTools.Method(BuildPhaseClass, "MoveNext"); // RunState.Run.totemBottoms.Count
        
        yield return AccessTools.Method(GenerateTotemChoicesClass, "<GenerateTotemChoices>b__26_0"); // Contains & Count

        // RunState.Initialise = totemBottoms = new List<Ability>();
    }
    
    [HarmonyDebug]
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
        /*InscryptionAPIPlugin.Logger.LogInfo($"[ReplaceTotemBottomsWithAPI] RunStateRun {RunStateRun}");
        InscryptionAPIPlugin.Logger.LogInfo($"[ReplaceTotemBottomsWithAPI] TotemBottoms {TotemBottoms}");
        InscryptionAPIPlugin.Logger.LogInfo($"[ReplaceTotemBottomsWithAPI] Count {Count}");
        InscryptionAPIPlugin.Logger.LogInfo($"[ReplaceTotemBottomsWithAPI] Contains {Contains}");
        InscryptionAPIPlugin.Logger.LogInfo($"[ReplaceTotemBottomsWithAPI] ===");*/
        
        MethodInfo RunStateCustomTotems = typeof(TotemManager).GetProperty(nameof(TotemManager.RunStateCustomTotems)).GetMethod;
        MethodInfo FillerMethod = AccessTools.Method(typeof(CustomTotemsSaveData), nameof(CustomTotemsSaveData.DoesNothing));
        MethodInfo CustomTotemCountMethod = AccessTools.Method(typeof(CustomTotemsSaveData), nameof(CustomTotemsSaveData.TotemsBottomCount));
        MethodInfo CustomTotemContainsMethod = AccessTools.Method(typeof(CustomTotemsSaveData), nameof(CustomTotemsSaveData.ContainsCardGainAbility));
        /*InscryptionAPIPlugin.Logger.LogInfo($"[ReplaceTotemBottomsWithAPI] RunStateCustomTotems {RunStateCustomTotems}");
        InscryptionAPIPlugin.Logger.LogInfo($"[ReplaceTotemBottomsWithAPI] CustomTotemBottoms {CustomTotemBottoms}");
        InscryptionAPIPlugin.Logger.LogInfo($"[ReplaceTotemBottomsWithAPI] FillerMethod {FillerMethod}");
        InscryptionAPIPlugin.Logger.LogInfo($"[ReplaceTotemBottomsWithAPI] CustomTotemBottomCount {CustomTotemBottomCount}");
        InscryptionAPIPlugin.Logger.LogInfo($"[ReplaceTotemBottomsWithAPI] CustomTotemContainsMethod {CustomTotemContainsMethod}");
        InscryptionAPIPlugin.Logger.LogInfo($"[ReplaceTotemBottomsWithAPI] ===");*/

        int overrides = 0;
        int totemBottomCalls = 0;
        
        List<CodeInstruction> codes = new List<CodeInstruction>(instructions);
        for (int i = 0; i < codes.Count; i++)
        {
            object currentOperand = codes[i].operand;
            object nextOperand = i+1 < codes.Count ? codes[i+1].operand : null;
            object nextNextOperand = i+2 < codes.Count ? codes[i+2].operand : null;
            object nextNextNextOperand = i+3 < codes.Count ? codes[i+3].operand : null;
            /*InscryptionAPIPlugin.Logger.LogInfo($"[ReplaceTotemBottomsWithAPI] currentOperand {currentOperand}");
            InscryptionAPIPlugin.Logger.LogInfo($"[ReplaceTotemBottomsWithAPI] nextOperand {nextOperand}");
            InscryptionAPIPlugin.Logger.LogInfo($"[ReplaceTotemBottomsWithAPI] nextNextOperand {nextNextOperand}");
            InscryptionAPIPlugin.Logger.LogInfo($"[ReplaceTotemBottomsWithAPI] nextNextNextOperand {nextNextNextOperand}");*/

            // RunState.Run.totemBottoms.Count
            if (nextNextNextOperand == TotemBottoms)
            {
                totemBottomCalls++;
            }
            if (currentOperand == RunStateRun && nextOperand == TotemBottoms && nextNextOperand == Count)
            {
                InscryptionAPIPlugin.Logger.LogInfo($"[ReplaceTotemBottomsWithAPI] Found totemBottoms.Count at index " + i);
                codes[i].operand = RunStateCustomTotems;
                codes[i+1] = new CodeInstruction(OpCodes.Call, FillerMethod); // Can't remove this method call so replace it with something that technically does nothing
                codes[i+2] = new CodeInstruction(OpCodes.Call, CustomTotemCountMethod);
                overrides++;
            }
            else if (currentOperand == RunStateRun && nextOperand == TotemBottoms && nextNextNextOperand == Contains)
            {
                InscryptionAPIPlugin.Logger.LogInfo($"[ReplaceTotemBottomsWithAPI] Found totemBottoms.Contains at index " + i);
                // C: RunState.Run
                // N: Run.totemBottoms
                // NN: Ability.Brittle
                // NNN: totemBottoms.Contains
                codes[i].operand = RunStateCustomTotems;
                codes[i+1] = new CodeInstruction(OpCodes.Call, FillerMethod); // Can't remove this method call so replace it with something that technically does nothing
                codes[i+3] = new CodeInstruction(OpCodes.Call, CustomTotemContainsMethod);
                overrides++;
            }
        }
        
        InscryptionAPIPlugin.Logger.LogError($"[ReplaceTotemBottomsWithAPI] {original.DeclaringType}.{original.GetType()} overrode {overrides}/{totemBottomCalls} functions");
        for (int i = 0; i < codes.Count; i++)
        {
            CodeInstruction code = codes[i];
            InscryptionAPIPlugin.Logger.LogInfo(i + ": " + code.ToString());
        }
        InscryptionAPIPlugin.Logger.LogError($"[ReplaceTotemBottomsWithAPI] {original.DeclaringType}.{original.GetType()} overrode {overrides}/{totemBottomCalls} functions");

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
                InscryptionAPIPlugin.Logger.LogInfo($"[BuildTotemSequencer_FillInventorySlots] Found AlternateTopsAndBottoms");
                codes.Insert(i++, new CodeInstruction(OpCodes.Ldloc_3));
                codes.Insert(i++, new CodeInstruction(OpCodes.Call, totemBottomFiller));
                break;
            }
        }
        
        InscryptionAPIPlugin.Logger.LogInfo($"DONEZ");
        foreach (CodeInstruction code in codes)
        {
            InscryptionAPIPlugin.Logger.LogInfo(code.ToString());
        }

        return codes;
    }

    internal static void AddTotemsFromSave(List<ItemData> list)
    {
        InscryptionAPIPlugin.Logger.LogInfo($"[BuildTotemSequencer_FillInventorySlots] Found FillWithRandomTotemBottoms");
        list.Clear();
        foreach (CustomTotemBottom bottom in TotemManager.RunStateCustomTotems.TotemBottoms)
        {
            InscryptionAPIPlugin.Logger.LogInfo($"[BuildTotemSequencer_FillInventorySlots] totem bottom " + bottom.EffectID + " " + bottom.EffectID + " " + bottom);
            
            
            TotemBottomData totemBottomData = ScriptableObject.CreateInstance<TotemBottomData>();
            totemBottomData.effectParams = new TotemBottomData.EffectParameters();
            
            TotemManager.CustomTotemBottom totemBottom = TotemManager.totemBottoms.Find((a)=>a.EffectID == bottom.EffectID);
            if (totemBottom == null)
            {
                totemBottomData.effect = TotemEffect.CardGainAbility; // vanilla only has this 1 ability
                totemBottomData.effectParams.ability = bottom.Ability;
            }
            else
            {
                totemBottomData.effect = totemBottom.EffectID;
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
    private static Type BuildPhaseClass = Type.GetType("DiskCardGame.BuildTotemSequencer+<BuildPhase>d__14, Assembly-CSharp, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null");
    
    public static IEnumerable<MethodBase> TargetMethods()
    {
        yield return AccessTools.Method(BuildPhaseClass, "MoveNext");
    }

    internal static bool Prefix(object __instance)
    {
        FieldInfo TotemData = BuildPhaseClass.GetField("<>1__state", BindingFlags.Instance | BindingFlags.NonPublic);
        InscryptionAPIPlugin.Logger.LogInfo($"[BuildTotemSequencer_BuildPhase] Prefix " + TotemData.GetValue(__instance));
        return true;
    }

    internal static void Postfix(object __instance)
    {
        FieldInfo TotemData = BuildPhaseClass.GetField("<>1__state", BindingFlags.Instance | BindingFlags.NonPublic);
        InscryptionAPIPlugin.Logger.LogInfo($"[BuildTotemSequencer_BuildPhase] Postfix " + TotemData.GetValue(__instance));
    }
    
    internal static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        // === We want to turn this

        // RunState.Run.totems.Add(totemDefinition);

        // === Into this

        // RunState.Run.totems.Add(CreateCustomTotemDefinition(totemDefinition));

        // ===
        MethodInfo SetInventorySlotsSelectable = AccessTools.Method(typeof(BuildTotemSequencer), nameof(BuildTotemSequencer.SetInventorySlotsSelectable));
        MethodInfo AutoAssembleTotem = AccessTools.Method(typeof(BuildTotemSequencer), nameof(BuildTotemSequencer.AutoAssembleTotem));
        MethodInfo LogA = AccessTools.Method(typeof(BuildTotemSequencer_BuildPhase), nameof(BuildTotemSequencer_BuildPhase.LogA));
        MethodInfo LogB = AccessTools.Method(typeof(BuildTotemSequencer_BuildPhase), nameof(BuildTotemSequencer_BuildPhase.LogB));
        MethodInfo LogState = AccessTools.Method(typeof(BuildTotemSequencer_BuildPhase), nameof(BuildTotemSequencer_BuildPhase.LogState));
        MethodInfo LogReturn = AccessTools.Method(typeof(BuildTotemSequencer_BuildPhase), nameof(BuildTotemSequencer_BuildPhase.LogReturn));
        
        
        MethodInfo OverrideTotemDefinition = AccessTools.Method(typeof(BuildTotemSequencer_BuildPhase), nameof(BuildTotemSequencer_BuildPhase.CreateCustomTotemDefinition));
        //ConstructorInfo totemDefinitionCTR = typeof(TotemDefinition).GetConstructor(new Type[]{});
        FieldInfo TotemData = BuildPhaseClass.GetField("<completedTotem>5__2", BindingFlags.Instance | BindingFlags.NonPublic);
        FieldInfo StateField = BuildPhaseClass.GetField("<>1__state", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
        InscryptionAPIPlugin.Logger.LogInfo($"[BuildTotemSequencer_BuildPhase] TotemData " + TotemData);
        MethodInfo AddMethod = typeof(List<TotemDefinition>).GetMethod(nameof(List<TotemDefinition>.Add));


        bool foundAutoAssemble = false;
        List<CodeInstruction> codes = new List<CodeInstruction>(instructions);
        for (int i = 0; i < codes.Count; i++)
        {
            if (codes[i].opcode == OpCodes.Stfld && codes[i].operand == StateField)
            {
                codes.Insert(i++, new CodeInstruction(OpCodes.Call, LogState));
            }
            
            if (codes[i].operand == AddMethod)
            {
                InscryptionAPIPlugin.Logger.LogInfo($"[BuildTotemSequencer_BuildPhase] Found List<TotemDefinition>.Add");
                codes.Insert(i++, new CodeInstruction(OpCodes.Ldarg_0, TotemData));
                codes.Insert(i++, new CodeInstruction(OpCodes.Ldfld, TotemData));
                codes.Insert(i++, new CodeInstruction(OpCodes.Call, OverrideTotemDefinition));
            }
            else if (codes[i].operand == SetInventorySlotsSelectable)
            {
                InscryptionAPIPlugin.Logger.LogInfo($"[BuildTotemSequencer_BuildPhase] Found SetInventorySlotsSelectable");
                codes.Insert(++i, new CodeInstruction(OpCodes.Call, LogA));
            }
            else if (codes[i].opcode == OpCodes.Ret)
            {
                InscryptionAPIPlugin.Logger.LogInfo($"[BuildTotemSequencer_BuildPhase] Found Return");
                codes.Insert(i++, new CodeInstruction(OpCodes.Call, LogReturn));
            }
            else if (!foundAutoAssemble && codes[i].operand == AutoAssembleTotem)
            {
                InscryptionAPIPlugin.Logger.LogInfo($"[BuildTotemSequencer_BuildPhase] Found AutoAssembleTotem");
                codes.Insert(++i, new CodeInstruction(OpCodes.Call, LogA));
                foundAutoAssemble = true;
            }
        }
        
        /*InscryptionAPIPlugin.Logger.LogInfo($"DONEZ");
        foreach (CodeInstruction code in codes)
        {
            InscryptionAPIPlugin.Logger.LogInfo(code.ToString());
        }
        InscryptionAPIPlugin.Logger.LogInfo($"DONEZ");*/

        return codes;
    }

    internal static TotemDefinition CreateCustomTotemDefinition(TotemDefinition vanillaDefinition, TotemItemData bottomData)
    {
        CustomTotemDefinition definition = new CustomTotemDefinition()
        {
            BottomEffectID = bottomData.bottom.effect,
            ability = bottomData.bottom.effectParams.ability,
            tribe = bottomData.top.prerequisites.tribe,
        };
        
        InscryptionAPIPlugin.Logger.LogInfo($"[CreateCustomTotemDefinition] " + definition.BottomEffectID + " " + definition.ability);
        return definition;
    }

    internal static void LogA()
    {
        InscryptionAPIPlugin.Logger.LogInfo($"[SetInventorySlotsSelectable]");
    }
    
    internal static bool LogB(bool a)
    {
        InscryptionAPIPlugin.Logger.LogInfo($"[AutoAssemble] " + a);
        return a;
    }
    
    internal static int LogState(int state)
    {
        InscryptionAPIPlugin.Logger.LogInfo($"[BuildPhase] State: " + state);
        return state;
    }
    
    internal static bool LogReturn(bool a)
    {
        InscryptionAPIPlugin.Logger.LogInfo($"[BuildPhase] LogReturn " + a);
        return a;
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
            InscryptionAPIPlugin.Logger.LogInfo($"[ItemSlot_CreateItem] {data}");
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
            InscryptionAPIPlugin.Logger.LogInfo($"Making Vanilla Totem Bottom " + bottomData.prefabId);
            return true;
        }

        InscryptionAPIPlugin.Logger.LogInfo($"Making Custom Totem Bottom " + totemBottom.RulebookName);
        GameObject gameObject = UnityObject.Instantiate(totemBottom.Prefab, __instance.transform);

        if (!gameObject.activeSelf)
        {
            gameObject.SetActive(true);
        }

        gameObject.transform.localPosition = Vector3.zero;

        if (!gameObject.TryGetComponent(out TotemTriggerReceiver triggerReceiver))
        {
            InscryptionAPIPlugin.Logger.LogError($"Warning. Item {bottomData.name} with prefab id does not have a TotemTriggerReceiver component!");
            triggerReceiver = (TotemTriggerReceiver)gameObject.AddComponent(totemBottom.TriggerReceiver);
        }
        if (!gameObject.TryGetComponent(out CompositeTotemPiece compositeTotemPiece))
        {
            InscryptionAPIPlugin.Logger.LogError($"Warning. Item {bottomData.name} with prefab id does not have a CompositeTotemPiece component!");
            compositeTotemPiece = (CompositeTotemPiece)gameObject.AddComponent(totemBottom.CompositeType);
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

/// <summary>
/// /////////////////////////////// TEMPORARY
/// </summary>

[HarmonyPatch]
internal static class BuildTotemSequencer_AutoAssembleTotem
{
    public static IEnumerable<MethodBase> TargetMethods()
    {
        yield return AccessTools.Method(typeof(BuildTotemSequencer), nameof(BuildTotemSequencer.AutoAssembleTotem)); // RunState.Run.totemBottoms.Count
    }
    
    [HarmonyDebug]
    internal static void Postfix(BuildTotemSequencer __instance, ref bool __result)
    {
        InscryptionAPIPlugin.Logger.LogInfo($"[BuildTotemSequencer_AutoAssembleTotem] '{__instance.selectedTop}' '{__instance.selectedBottom}' '{__instance.completeTotemSlot.Item}' = {__result}");
    }
}


[HarmonyPatch]
internal static class BuildTotemSequencer_while_selectedTop_not_null_and_selectedBottom_not_null
{
    private static Type CompiledClass = Type.GetType("DiskCardGame.BuildTotemSequencer+<>c__DisplayClass14_0, Assembly-CSharp, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null");
    
    public static IEnumerable<MethodBase> TargetMethods()
    {
        yield return AccessTools.Method(CompiledClass, "<BuildPhase>b__2"); // RunState.Run.totemBottoms.Count
    }
    
    internal static void Postfix(BuildTotemSequencer __instance, ref bool __result)
    {
        InscryptionAPIPlugin.Logger.LogInfo($"[BuildTotemSequencer] WaitUntil(() => selectedTop != null && selectedBottom != null = '{__result}'");
    }
}


[HarmonyPatch]
internal static class BuildTotemSequencer_AssembleTotem
{
    private static Type CompiledClass = Type.GetType("DiskCardGame.BuildTotemSequencer+<BuildPhase>d__14, Assembly-CSharp, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null");
    public static IEnumerable<MethodBase> TargetMethods()
    {
        yield return AccessTools.Method(typeof(BuildTotemSequencer), "AssembleTotem"); // RunState.Run.totemBottoms.Count
    }
    
    internal static void Postfix(BuildTotemSequencer __instance, SelectableItemSlot topSlot, SelectableItemSlot bottomSlot)
    {
        InscryptionAPIPlugin.Logger.LogInfo($"[BuildTotemSequencer_AssembleTotem] topSlot: {topSlot.Item} botSlot: {topSlot.Item}");
    }
}



[HarmonyPatch]
internal static class BuildTotemSequencer_BuildPhase_PrefixPrefix
{
    public static IEnumerable<MethodBase> TargetMethods()
    {
        yield return AccessTools.Method(typeof(BuildTotemSequencer), nameof(BuildTotemSequencer.BuildPhase));
    }
    
    internal static bool Prefix()
    {
        InscryptionAPIPlugin.Logger.LogInfo($"[BuildTotemSequencer_BuildPhase_PrefixPrefix] Creating " + Environment.StackTrace);
        return true;
    }
}
