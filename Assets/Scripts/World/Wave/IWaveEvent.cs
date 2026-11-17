using System;

public interface IWaveEvent
{
    event Action<bool> Finished;
    void ConfigureSpawn(float capacity, bool isWaveEvent);
    void Close();
}
