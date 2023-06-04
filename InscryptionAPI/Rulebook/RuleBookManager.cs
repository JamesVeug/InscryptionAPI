using System.Reflection;
using System.Reflection.Emit;
using DiskCardGame;
using HarmonyLib;
using InscryptionAPI.Guid;
using InscryptionAPI.Helpers;
using UnityEngine;

namespace InscryptionAPI.Rulebook;

[HarmonyPatch]
public static class RuleBookManager
{
    public class CustomRulebookSection
    {
        public string PluginGUID;
        public string SectionName;
        public PageRangeInfo PageRangeInfo;
        public Type PageType;
        public CustomRulebookSectionData CustomPageType;
    }
    
    public static readonly PageRangeType TribeRulebookPage;
    
    internal static readonly List<CustomRulebookSection> newPages = new();
    private static readonly GenericRulebookPage fallbackPagePrefab = null;

    static RuleBookManager()
    {
        GameObject fallbackPage = Resources.Load<GameObject>("prefabs/rulebook/pagecontent/ItemPageContent");
        ItemPage itemPage = fallbackPage.GetComponent<ItemPage>();
        fallbackPagePrefab = fallbackPage.AddComponent<GenericRulebookPage>();
        fallbackPagePrefab.iconRenderer = itemPage.iconRenderer;
        fallbackPagePrefab.nameTextMesh = itemPage.nameTextMesh;
        fallbackPagePrefab.descriptionTextMesh = itemPage.descriptionTextMesh;
        UnityObject.Destroy(itemPage);
        GameObject.DontDestroyOnLoad(fallbackPage);
        fallbackPage.SetActive(false);
        
        
        TribeRulebookPage = New<TribeRulebookData>(InscryptionAPIPlugin.ModGUID, "Tribes").PageRangeInfo.type;
    }

    public static CustomRulebookSection New<T, Y>(string guid, string sectionName, GameObject pagePrefab=null) 
        where T : CustomRulebookSectionData
        where Y : RuleBookPage
    {
        PageRangeType pageRangeType = GuidManager.GetEnumValue<PageRangeType>(guid, sectionName);
        GameObject prefab = pagePrefab != null ? pagePrefab : fallbackPagePrefab.gameObject;
        
        CustomRulebookSection section = new CustomRulebookSection
        {
            PluginGUID = guid,
            SectionName = sectionName,
            PageRangeInfo = new PageRangeInfo()
            {
                rangePrefab = prefab,
                type = pageRangeType,
            },
            PageType = typeof(Y),
            CustomPageType = Activator.CreateInstance<T>()
        };

        newPages.Add(section);
        return section;
    }

    public static CustomRulebookSection New<T>(string guid, string sectionName) where T : CustomRulebookSectionData
    {
        return New<T, GenericRulebookPage>(guid, sectionName);
    }
    
    public static void OpenRulebookToCustomPage(PageRangeType rangeType, string pageID, bool offsetView=false)
    {
        RuleBookController ruleBookController = RuleBookController.Instance;
        ruleBookController.SetShown(shown: true, offsetView: offsetView);

        string page = rangeType + "_" + pageID;
        List<RuleBookPageInfo> pageData = ruleBookController.PageData;
        int pageIndex = pageData.IndexOf(pageData.Find((RuleBookPageInfo x) => !string.IsNullOrEmpty(x.pageId) && x.pageId == page));
        ruleBookController.StopAllCoroutines();
        ruleBookController.StartCoroutine(ruleBookController.flipper.FlipToPage(pageIndex, 0.2f));
    }
    
#region patches
    
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
                    if (component is GenericRulebookPage)
                    {
                        component.FillPage(pageInfo.headerText, new object[]
                        {
                            newPage.PageRangeInfo.type, pageInfo.pageId // Populated in CustomRulebookSectionData.CreatePage
                        });
                    }
                    else
                    {
                        component.FillPage(pageInfo.headerText, new object[]
                        {
                            pageInfo.pageId // Populated in CustomRulebookSectionData.CreatePage
                        });
                    }
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
                InscryptionAPIPlugin.Logger.LogInfo(page.SectionName);
                totalCategories++;
                List<RuleBookPageInfo> pages = instanceBookInfo.ConstructPages(page.PageRangeInfo, page.CustomPageType.numPages, 0, (int index) =>
                {
                    return page.CustomPageType.ShouldBeAdded(index, metaCategory);
                }, (a, b, index) =>
                {
                    a.pageId = page.PageRangeInfo.type + "_" + page.CustomPageType.GetPageID(index);
                }, "APPENDIX XII, SUBSECTION " + GeneralHelpers.ConvertToRomanNumeral(totalCategories) + " - " + page.SectionName + " {0}");
                __result.AddRange(pages);
            }
        }
    }
    
#endregion
}
