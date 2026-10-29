using System;
namespace OneMoreBuild
{
    public sealed class GameClock
    {
        double accumulated;
        // Modal windows and pause consume no time. Hitches never fast-forward the studio.
        public void Advance(float realSeconds, int speed, Func<bool> blocked, Action tick)
        {
            if (speed <= 0 || blocked() || float.IsNaN(realSeconds) || realSeconds <= 0) return;
            accumulated += Math.Min(realSeconds, .25f) * Math.Min(speed, 3);
            while (accumulated >= 1 && !blocked()) { tick(); accumulated -= 1; }
        }
    }
}
