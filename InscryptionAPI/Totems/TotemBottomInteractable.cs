using DiskCardGame;

namespace InscryptionAPI.Totems;

public class TotemBottomInteractable : AbilityIconInteractable
{

    public override void OnAlternateSelectStarted()
    {
        ItemData itemData = GetComponent<CompositeTotemPiece>().Data;
        TotemBottomData bottomData = (TotemBottomData)itemData;
            //bottomData.effect
    }
}
