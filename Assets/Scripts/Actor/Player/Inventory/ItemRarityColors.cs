using UnityEngine;

public static class ItemRarityColors
{
    public static Color Get(EItemRarity rarity)
    {
        switch (rarity)
        {
            case EItemRarity.Rare:
                return new Color(0.15f, 0.4f, 0.9f);
            case EItemRarity.Epic:
                return new Color(0.6f, 0.25f, 0.85f);
            case EItemRarity.Mythical:
                return new Color(0.48f, 0.06f, 0.14f);
            case EItemRarity.Legendary:
                return new Color(0.95f, 0.75f, 0.12f);
            default:
                return new Color(0.55f, 0.55f, 0.55f);
        }
    }
}
