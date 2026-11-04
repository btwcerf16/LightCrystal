using UnityEngine;

public class MagicMissleBookWeapon : Weapon
{

    [SerializeField, Min(0f)] private float _sanityCost = 15f;
    [SerializeField, Min(1f)] private int _magicMisslesCount = 3;


    public override void BeginPrimaryUse()
    {
        throw new System.NotImplementedException();
    }

    public override bool BeginSecondaryUse()
    {
        throw new System.NotImplementedException();
    }

    public override void CancelUse()
    {
        throw new System.NotImplementedException();
    }

    public override void EndPrimaryUse()
    {
        throw new System.NotImplementedException();
    }

    public override void EndSecondaryUse()
    {
        throw new System.NotImplementedException();
    }

    public override void Use()
    {
        throw new System.NotImplementedException();
    }
}
