using System;

namespace PhotoLabel.Services
{
    public interface ITimerService
    {
        void Pause(TimeSpan value);
    }
}