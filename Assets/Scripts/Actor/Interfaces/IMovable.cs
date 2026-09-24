using UnityEngine;

public interface IMovable
{
    void Move(Vector2 direction);

    void ApplyKnockback(Vector2 direction, float strength, float duration);

    void Stop();
}