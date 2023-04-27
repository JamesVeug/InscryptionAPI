using System.Reflection;
using System.Reflection.Emit;
using DiskCardGame;
using HarmonyLib;

namespace InscryptionAPI.Rulebook;

[HarmonyPatch]
public static class RuleBookManager
{
    public abstract class CustomRulebookPage
    {
        public abstract int numPages { get; }
        
        public abstract bool ShouldBeAdded(int index, AbilityMetaCategory metaCategory);
        public abstract void CreatePage(RuleBookPageInfo page, PageRangeInfo pageRange, int index);
    }
    
    public class CustomRulebookSection
    {
        public string PluginGUID;
        public string RulebookName;
        public PageRangeInfo PageRangeInfo;
        public Type PageType;
        public CustomRulebookPage CustomPageType;
    }
    
    internal static readonly List<CustomRulebookSection> newPages = new();

    static RuleBookManager()
    {
        New<TribeRulebookPage>(InscryptionAPIPlugin.ModGUID, "Tribes");
    }

    public static CustomRulebookSection New<T>(string PluginGUID, string rulebookName, PageRangeInfo pageRangeInfo,) where T : CustomRulebookPage
    {
        CustomRulebookSection section = new CustomRulebookSection
        {
            PluginGUID = PluginGUID,
            RulebookName = rulebookName,
            PageRangeInfo = ,
            PageType = null,
            CustomPageType = null
        };
        
    }
    
    [HarmonyPatch(typeof(PageContentLoader), nameof(PageContentLoader.LoadPage), new Type[]{typeof(RuleBookPageInfo)})]
    internal class PageContentLoader_LoadPage
    {
        internal static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            // === We want to turn this
            // ...
            // else if (component is ItemPage)
		    // {
            //  component.FillPage(pageInfo.headerText, pageInfo.pageId);
		    // }
		    // else
		    // {
            //  component.FillPage(pageInfo.headerText);
		    // }
            
            // === Into this
            
            // ...
            // else if (component is ItemPage)
            // {
            //  component.FillPage(pageInfo.headerText, pageInfo.pageId);
            // }
            // else if(ProcessedCustomPage(component))
            // {
            //  component.FillPage(pageInfo.headerText, pageInfo.pageId);
            // }
            // else
            // {
            //  component.FillPage(pageInfo.headerText);
            // }
            
            // ===
            List<CodeInstruction> codes = new List<CodeInstruction>(instructions);

            MethodInfo ProcessedCustomPageInfo =  SymbolExtensions.GetMethodInfo(() => ProcessedCustomPage(null, null));

            for (int i = 0; i < codes.Count - 1; i++)
            {
                CodeInstruction codeInstruction = codes[i];
                if (codeInstruction.opcode == OpCodes.Brfalse)
                {
                    for (int j = i+1; j < codes.Count; j++)
                    {
                        CodeInstruction innerCodeInstruction = codes[j];
                        if (innerCodeInstruction.opcode == OpCodes.Ret)
                        {
                            codes.InsertRange(j + 2, new List<CodeInstruction>()
                            {
                                new CodeInstruction(OpCodes.Ldarg_1), // pageInfo
                                new CodeInstruction(OpCodes.Callvirt, ProcessedCustomPageInfo), // Process custom page
                                new CodeInstruction(OpCodes.Brfalse, codeInstruction.operand), // Continue if normal if ProcessedCustomPageInfo returned false 
                                new CodeInstruction(OpCodes.Ret), // Stop method if ProcessedCustomPageInfo returned true
                                new CodeInstruction(OpCodes.Ldloc_0), // component
                            });
                            break;
                        }
                    }
                    break;
                }
            }
            
            return codes;
        }

        private static bool ProcessedCustomPage(RuleBookPage component, RuleBookPageInfo pageInfo)
        {
            foreach (CustomRulebookSection newPage in newPages)
            {
                if (newPage.PageType == component.GetType())
                {
                    component.FillPage(pageInfo.headerText, new object[]
                    {
                        pageInfo.pageId
                    });
                    return true;
                }
            }
            return false;
        }
    }
    
    [HarmonyPatch(typeof(RuleBookInfo), nameof(RuleBookInfo.ConstructPageData))]
    internal class RuleBookInfo_ConstructPageData
    {
        public static void Postfix(RuleBookInfo __instance, ref List<RuleBookPageInfo> __result, AbilityMetaCategory metaCategory)
        {
            int totalCategories = 9; // vanilla starts at 9 although there's only 5 categories lolwat
            RuleBookInfo instanceBookInfo = RuleBookController.Instance.bookInfo;
            foreach (CustomRulebookSection page in newPages)
            {
                totalCategories++;
                List<RuleBookPageInfo> pages = instanceBookInfo.ConstructPages(page.PageRangeInfo, page.numPages, 0, (int index) =>
                {
                    return page.ShouldBeAdded(index, metaCategory);
                }, page.CreatePage, "APPENDIX XII, SUBSECTION " + ConvertToRomanNumeral(totalCategories) + " - " + page.rulebookName + " {0}");
                __result.AddRange(pages);
            }
        }

        public static string ConvertToRomanNumeral(int number)
        {
            if (number < 1 || number > 3999)
            {
                throw new ArgumentOutOfRangeException("number", "Number must be between 1 and 3999.");
            }
    
            // Define arrays to hold the Roman numeral symbols and their corresponding values.
            string[] symbols = { "M", "CM", "D", "CD", "C", "XC", "L", "XL", "X", "IX", "V", "IV", "I" };
            int[] values = { 1000, 900, 500, 400, 100, 90, 50, 40, 10, 9, 5, 4, 1 };
    
            // Initialize an empty string to hold the Roman numeral.
            string result = "";
    
            // Loop through the arrays, subtracting the values until the number is reduced to zero.
            for (int i = 0; i < values.Length; i++)
            {
                while (number >= values[i])
                {
                    result += symbols[i];
                    number -= values[i];
                }
            }
    
            return result;
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
