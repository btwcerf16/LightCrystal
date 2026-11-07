using UnityEngine;

[RequireComponent (typeof(CircleCollider2D))]
public class Trap : MonoBehaviour
{
    [SerializeField] protected LayerMask _targetsLayerMask;
    [SerializeField, Min(1)] protected int _operationsCount;

    [SerializeField] protected ActorStats _owner;


    protected void OnCaught(Collision collision)
    {
        
    }


    public void SetOwner(ActorStats owner)
    {
        _owner = owner;
    }
}
