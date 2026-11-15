using System;

public interface IWaveEvent
{
    event Action<bool> Finished;
    void Close();
}
