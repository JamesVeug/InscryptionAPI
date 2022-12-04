using System.Reflection;
using System.Reflection.Emit;
using DiskCardGame;
using HarmonyLib;

namespace InscryptionAPI.Rulebook;

[HarmonyPatch]
public static class RuleBookManager
{
    [HarmonyPatch(typeof(PageContentLoader), nameof(PageContentLoader.LoadPage), new Type[]{typeof(RuleBookPageInfo)})]
    internal class PageContentLoader_LoadPage
    {
        internal static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            // === We want to turn this
            
            /*
            if (component is ItemPage)
            {
                component.FillPage(pageInfo.headerText, new object[]
                {
                    pageInfo.pageId
                });
                return;
            }
            component.FillPage(pageInfo.headerText, Array.Empty<object>());
            */
            
            // === Into this
            
            /*
            if (component is ItemPage)
            {
                component.FillPage(pageInfo.headerText, new object[]
                {
                    pageInfo.pageId
                });
                return;
            }
            if(ProcessedCustomPage(component)){
                return;
            }
            component.FillPage(pageInfo.headerText, Array.Empty<object>());
            */
            
            // ===
            List<CodeInstruction> codes = new List<CodeInstruction>(instructions);
            foreach (CodeInstruction code in codes)
            {
                InscryptionAPIPlugin.Logger.LogInfo(code.opcode + " " + code.operand);
                /*foreach (Label codeLabel in code.labels)
                {
                    InscryptionAPIPlugin.Logger.LogInfo("\t" + codeLabel);
                }*/
            }

            MethodInfo ProcessedCustomPageInfo =  SymbolExtensions.GetMethodInfo(() => ProcessedCustomPage(null));

            for (int i = 0; i < codes.Count - 1; i++)
            {
                CodeInstruction codeInstruction = codes[i];
                if (codeInstruction.opcode == OpCodes.Brfalse_S)
                {
                    for (int j = i+1; j < codes.Count; j++)
                    {
                        CodeInstruction innerCodeInstruction = codes[j];
                        if (innerCodeInstruction.opcode == OpCodes.Ret)
                        {
                            codes.InsertRange(j + 1, new List<CodeInstruction>()
                            {
                                new CodeInstruction(OpCodes.Ldloc_0), // Component
                                new CodeInstruction(OpCodes.Callvirt, ProcessedCustomPageInfo), // Process custom page
                                new CodeInstruction(OpCodes.Brfalse_S, codeInstruction.operand), // Continue if normal if ProcessedCustomPageInfo returned false 
                                new CodeInstruction(OpCodes.Ret), // Stop method if ProcessedCustomPageInfo returned true
                            });
                            InscryptionAPIPlugin.Logger.LogInfo("Injected custom page");
                            break;
                        }
                    }
                    break;
                }
            }
            
            return codes;
        }

        private static bool ProcessedCustomPage(RuleBookPage component)
        {
            if (component is StatIconPage)
            {
                component.FillPage(pageInfo.headerText, new object[]
                {
                    pageInfo.pageId
                });
                return true;
            }
            return false;
        }
    }
    
    public static void OpenRulebookToTribePage(this RuleBookController instance, string tribeName, PlayableCard card, bool immediate = false)
    {
        instance.SetShown(true, instance.OffsetViewForCard(card));
        int pageIndex = instance.PageData.IndexOf(instance.PageData.Find((RuleBookPageInfo x) => x.pageId == tribeName));
        instance.StopAllCoroutines();
        instance.StartCoroutine(instance.flipper.FlipToPage(pageIndex, immediate ? 0f : 0.2f));
    }
}
