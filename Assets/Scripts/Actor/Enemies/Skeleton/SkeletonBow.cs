using UnityEngine;

public sealed class SkeletonBow : MonoBehaviour
{
    [SerializeField] private Transform _upperTip;
    [SerializeField] private Transform _lowerTip;
    [SerializeField] private Transform _drawPoint;
    [SerializeField] private LineRenderer _string;

    private void LateUpdate()
    {
        _string.SetPosition(0, transform.InverseTransformPoint(_upperTip.position));
        _string.SetPosition(1, transform.InverseTransformPoint(_drawPoint.position));
        _string.SetPosition(2, transform.InverseTransformPoint(_lowerTip.position));
    }
}
