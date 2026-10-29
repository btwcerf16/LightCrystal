public interface ILightSensitive
{
    void EnterLight();
    void ExitLight();
    void ReceiveLight(float amount);
}
