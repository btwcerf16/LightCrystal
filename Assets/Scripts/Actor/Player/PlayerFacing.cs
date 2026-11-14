using UnityEngine;
public sealed class PlayerFacing : MonoBehaviour
{
    [SerializeField] private Transform _handTransform;
    [SerializeField] private Transform _bodyVisual;

    private void LateUpdate()
    {
        float facingAngle = _handTransform.localPosition.x >= 0f ? 0f : 180f;
        _bodyVisual.localRotation = Quaternion.Euler(0f, facingAngle, 0f);
    }

}

