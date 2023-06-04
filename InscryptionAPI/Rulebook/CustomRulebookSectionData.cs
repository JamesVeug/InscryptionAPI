using DiskCardGame;
using UnityEngine;

namespace InscryptionAPI.Rulebook;

public abstract class CustomRulebookSectionData
{
    public abstract int numPages { get; }

    public virtual bool ShouldBeAdded(int index, AbilityMetaCategory metaCategory)
    {
        return true;
    }

    public abstract string GetPageID(int index);
    public abstract string GetRulebookName(string pageID);
    public abstract string GetRulebookDescription(string pageID);
    public abstract Sprite GetRulebookSprite(string pageID);
}
