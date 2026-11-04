using Unity.Netcode;
using UnityEngine;

[CreateAssetMenu(menuName = "Characters/Character")]
public sealed class CharacterConfig : ScriptableObject
{
    public string DisplayName;
    [TextArea] public string Description;
    public Sprite Preview;
    public Sprite BodySprite;
    public ActorStatsConfig Stats;
    public SpellConfig[] Spells;
    public NetworkObject PlayerPrefab;
}
