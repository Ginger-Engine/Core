using System.Diagnostics;
using Engine.Core.Stages;

namespace Engine.Core;

public class Loop
{
    private readonly StageRunner _runner;
    private readonly Stopwatch _stopwatch = new();
    private long _previousFrameTicks = 0;

    public Loop(StageRunner runner)
    {
        _runner = runner;
        _stopwatch.Start();
        _runner.Start();
    }

    public void Update()
    {
        var currentFrameTicks = _stopwatch.ElapsedTicks;
        var deltaTicks = currentFrameTicks - _previousFrameTicks;
        _previousFrameTicks = currentFrameTicks;
                
        float dt = (float)deltaTicks / Stopwatch.Frequency;
        _runner.Update(dt);
    }

    public bool IsRunning()
    {
        return true;
    }
}