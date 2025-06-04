using System.Diagnostics;
using Engine.Core.Scenes;

namespace Engine.Core;

public class Loop
{
    private static Stopwatch _stopwatch = new();
    private static long _previousFrameTicks = 0;

    public void Update(Scene scene)
    {
        long currentFrameTicks = _stopwatch.ElapsedTicks;
        long deltaTicks = currentFrameTicks - _previousFrameTicks;
        _previousFrameTicks = currentFrameTicks;
                
        float dt = (float)deltaTicks / Stopwatch.Frequency;
        scene.Update(dt);
    }

    public bool IsRunnig()
    {
        return true;
    }
}