using UnityEngine;
public sealed class PlayerFacing : MonoBehaviour
{
    [SerializeField] private Transform _handTransform;
    [SerializeField] private Transform _bodyVisual;

    private void LateUpdate()
    {
        bool isFacingRight = _handTransform.localPosition.x >= 0f;

        if (isFacingRight)
            _bodyVisual.localRotation = Quaternion.Euler(0f, 0f, _bodyVisual.localEulerAngles.z);
        else
            _bodyVisual.localRotation = Quaternion.Euler(0f, 180f, _bodyVisual.localEulerAngles.z);
    }

}

