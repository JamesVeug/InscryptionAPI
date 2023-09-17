using DiskCardGame;
using InscryptionAPI.Helpers;
using System.Collections.ObjectModel;
using InscryptionAPI.Guid;
using InscryptionAPI.Saves;
using UnityEngine;

namespace InscryptionAPI.Totems;

public static partial class TotemManager
{
    internal enum TotemTopState
    {
        Vanilla,
        CustomTribes,
        AllTribes
    }

    internal const string CustomTotemTopID = "TotemPieces/TotemTop_Custom";
    internal const string CustomTotemBottomID = "TotemPieces/TotemBottom_CardGainAbility";
    internal const string CustomTotemTopResourcePath = "Prefabs/Items/" + CustomTotemTopID;
    internal const string CustomTotemBottomResourcePath = "Prefabs/Items/" + CustomTotemBottomID;

    internal static CustomTotemTop defaultTotemTop = null;
    internal static List<TotemTopEffect> allTopEffects = new();
    internal readonly static List<CustomTotemTop> totemTops = new();
    
    internal static GameObject defaultTotemBottom = null;
    internal static List<TotemBottomEffect> allBottomEffects = new();
    internal readonly static List<CustomTotemBottom> totemBottoms = new();

    /// <summary>
    /// A collection of all new totem tops added using the API.
    /// </summary>
    public readonly static ReadOnlyCollection<CustomTotemTop> NewTotemTops = new(totemTops);

    /// <summary>
    /// Totem top that is used for custom tribes if no custom model is provided
    /// </summary>
    public static CustomTotemTop DefaultTotemTop => defaultTotemTop;
    
    /// <summary>
    /// Vanilla model
    /// </summary>
    public static GameObject DefaultTotemBottom
    {
        get
        {
            if (defaultTotemBottom == null)
                defaultTotemBottom = GetDefaultTotemBottom();
            return defaultTotemBottom;
        }
    }
    private static GameObject GetDefaultTotemBottom()
    {
        byte[] resourceBytes = TextureHelper.GetResourceBytes("customtotembottom", typeof(InscryptionAPIPlugin).Assembly);
        if (AssetBundleHelper.TryGet(resourceBytes, "CustomTotemBottom", out GameObject go))
        {
            return go;
        }
        
        InscryptionAPIPlugin.Logger.LogError($"Unable to load CustomTotemBottom model");
        return Resources.Load<GameObject>(CustomTotemBottomResourcePath);
    }

    [Obsolete("Obsolete. Use SetDefaultTotemTop<T> instead to ensure the totem top is set up correctly.")]
    public static void SetDefaultTotemTop(GameObject gameObject)
    {
        if (defaultTotemTop == null)
            InitializeDefaultTotemTop();

        defaultTotemTop.Prefab = gameObject;
        GameObject.DontDestroyOnLoad(gameObject);
    }

    public static void SetDefaultTotemTop<T>(GameObject gameObject) where T : CompositeTotemPiece
    {
        if (defaultTotemTop == null)
            InitializeDefaultTotemTop();

        // Attach missing components
        SetupTotemTopPrefab(gameObject, typeof(T));

        defaultTotemTop.Prefab = gameObject;
    }

    private static void InitializeDefaultTotemTop()
    {
        byte[] resourceBytes = TextureHelper.GetResourceBytes("customtotemtop", typeof(InscryptionAPIPlugin).Assembly);
        if (AssetBundleHelper.TryGet(resourceBytes, "CustomTotemTop", out GameObject go))
        {
            defaultTotemTop = NewTopPiece<CustomIconTotemTopPiece>("DefaultTotemTop",
                InscryptionAPIPlugin.ModGUID,
                Tribe.None,
                go
            );
            GameObject.DontDestroyOnLoad(go);
        }
    }

    internal static void Initialize()
    {
        // Don't change any totems!
        if (InscryptionAPIPlugin.configCustomTotemTopTypes.Value == TotemTopState.Vanilla)
            return;

        if (defaultTotemTop == null)
        {
            InitializeDefaultTotemTop();
        }

        // Add all totem tops to the game
        foreach (CustomTotemTop totem in totemTops)
        {
            string path = "Prefabs/Items/TotemPieces/TotemTop_" + totem.Tribe;
            if (totem == defaultTotemTop)
                path = CustomTotemTopResourcePath;

            GameObject prefab = totem.Prefab;
            if (prefab == null)
            {
                InscryptionAPIPlugin.Logger.LogError($"Cannot load NewTopPiece for {totem.GUID}.{totem.Name}. Prefab is null!");
                continue;
            }

            // Attach missing components
            SetupTotemTopPrefab(prefab, totem.Type);

            // Add to resources so it can be part of the pool
            ResourceBank.instance.resources.Add(new ResourceBank.Resource()
            {
                path = path,
                asset = prefab
            });
        }

        // Add all totem bottoms to the game
        foreach (CustomTotemBottom totem in totemBottoms)
        {
            InscryptionAPIPlugin.Logger.LogError($"Adding {totem.RulebookName} {totem.EffectID}");
            GameObject prefab = totem.Prefab;
            if (prefab == null)
            {
                InscryptionAPIPlugin.Logger.LogError($"Cannot load NewBottomPiece for {totem.GUID}.{totem.RulebookName}. Prefab is null!");
                continue;
            }
            
            if (prefab == DefaultTotemBottom)
            {
                prefab = UnityObject.Instantiate(prefab);
                totem.Prefab = prefab;
            }

            // Attach missing components
            SetupTotemBottomPrefab(prefab, totem.CompositeType);

            InscryptionAPIPlugin.Logger.LogError($"Adding to bank");
            
            // Add to resources so it can be part of the pool
            ResourceBank.instance.resources.Add(new ResourceBank.Resource()
            {
                path = "Prefabs/Items/TotemPieces/TotemBottom_" + totem.EffectID,
                asset = prefab
            });
        }
    }
    
    private static void SetupTotemTopPrefab(GameObject prefab, Type compositeType)
    {
        // Add require components in case the prefab doesn't have them
        if (prefab.GetComponent<CompositeTotemPiece>() == null)
        {
            prefab.AddComponent(compositeType);
        }

        if (prefab.GetComponent<Animator>() == null)
        {
            Animator addComponent = prefab.AddComponent<Animator>();
            addComponent.runtimeAnimatorController = Resources.Load<RuntimeAnimatorController>("animation/items/ItemAnim");
            addComponent.Rebind();
        }

        // Mark as dont destroy on load so it doesn't get removed between levels
        UnityObject.DontDestroyOnLoad(prefab);
    }

    
    
    public static CustomTotemsSaveData RunStateCustomTotems
    {
        get
        {
            string json = ModdedSaveManager.RunState.GetValue(InscryptionAPIPlugin.ModGUID, "CustomTotemBottoms");
            if (string.IsNullOrEmpty(json))
            {
                return new CustomTotemsSaveData();
            }

            CustomTotemsSaveData data = SaveManager.FromJSON<CustomTotemsSaveData>(json);
            return data;
        }
        set
        {
            string json = value == null ? null : SaveManager.ToJSON(value);
            ModdedSaveManager.RunState.SetValue(InscryptionAPIPlugin.ModGUID, "CustomTotemBottoms", json);
        }
    }
    
    private static void SetupTotemBottomPrefab(GameObject prefab, Type compositeType)
    {
        // Add require components in case the prefab doesn't have them
        if (!prefab.TryGetComponent(out CompositeTotemPiece t) || t.GetType() != compositeType)
        {
            InscryptionAPIPlugin.Logger.LogInfo($"CompositeTotemPiece count {prefab.GetComponents<CompositeTotemPiece>().Length}");
            if (t != null)
            {
                InscryptionAPIPlugin.Logger.LogError($"Removing CompositeTotemPiece from " + prefab);
                UnityObject.Destroy(t);
                InscryptionAPIPlugin.Logger.LogInfo($"CompositeTotemPiece count {prefab.GetComponents<CompositeTotemPiece>().Length}");
            }
            prefab.AddComponent(compositeType);
            InscryptionAPIPlugin.Logger.LogInfo($"SetupTotemBottomPrefab data {compositeType}");
        }
        
        InscryptionAPIPlugin.Logger.LogInfo($"CompositeTotemPiece count {prefab.GetComponents<CompositeTotemPiece>().Length}");

        if (prefab.GetComponent<Animator>() == null)
        {
            Animator addComponent = prefab.AddComponent<Animator>();
            addComponent.runtimeAnimatorController = Resources.Load<RuntimeAnimatorController>("animation/items/ItemAnim");
            addComponent.Rebind();
        }

        // Mark as dont destroy on load so it doesn't get removed between levels
        UnityObject.DontDestroyOnLoad(prefab);
    }

    public class CustomTotemTop
    {
        public string Name;
        public string GUID;
        public Type Type = typeof(CustomIconTotemTopPiece);
        public GameObject Prefab;
        public Tribe Tribe;
    }

    public class CustomTotemBottom
    {
        public string PrefabID => GUID + "_" + RulebookName;
        
        public string GUID;
        public string RulebookName;
        public string RulebookDescription;
        public TotemEffect EffectID;
        public GameObject Prefab;
        public Texture Icon;
        public Type CompositeType = typeof(CustomIconTotemBottomPiece);
        public Type Effect = typeof(TotemBottomEffect);
        public Type TriggerReceiver = null;

        public CustomTotemBottom SetTriggerReceiverType(Type type)
        {
            TriggerReceiver = type;
            return this;
        }

        public CustomTotemBottom SetCompositeTotemPieceType(Type type)
        {
            CompositeType = type;
            return this;
        }

        public CustomTotemBottom SetEffectType(Type type)
        {
            Effect = type;
            return this;
        }
    }
}
