using DiskCardGame;
using InscryptionAPI.Card;
using InscryptionAPI.Helpers.Extensions;
using UnityEngine;

namespace InscryptionAPI.Totems;

public class CustomIconTotemBottomPiece : CompositeTotemPiece
{
    protected virtual string IconGameObjectName => "IconRenderer";

    public override void SetData(ItemData data)
    {
        InscryptionAPIPlugin.Logger.LogError($"[CustomIconTotemBottomPiece] SetData " + data);
        
        // Assign gameobject the icon will appear on
        AssignEmittingIcon();
        if (emissiveRenderer != null)
        {
            if (data is TotemBottomData bottomData)
            {
                TotemManager.CustomTotemBottom customData = TotemManager.totemBottoms.Find((a) => a.EffectID == bottomData.effect);
                emissiveRenderer.material.mainTexture = customData.Icon;
            }
        }
        
        // Set icon
        base.SetData(data);
    }

    public void AssignEmittingIcon()
    {
        if (emissiveRenderer != null)
        {
            return;
        }
        
        GameObject icon = this.gameObject.FindChild(IconGameObjectName);
        if (icon != null)
        {
            emissiveRenderer = icon.GetComponent<Renderer>();
            if (emissiveRenderer == null)
            {
                InscryptionAPIPlugin.Logger.LogError($"Could not find Renderer on GameObject with name {IconGameObjectName} to assign totem icon!");
            }
        }
        else
        {
            InscryptionAPIPlugin.Logger.LogError($"Could not find GameObject with name {IconGameObjectName} to assign totem icon!");
        }
    }
}
