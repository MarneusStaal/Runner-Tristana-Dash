using System;

public static class RunnerEventSystem
{
    public static Action<float> OnPlayerJump;
    public static Action OnPlayerOutOfFuel;
    public static Action OnFlyingDamage;

    public static Action OnFlyDown;
    public static Action OnFlyUp;
    public static Action OnFlyEnd;

    public static Action OnStartRunning;
    public static Action OnStartWalking;
    
    public static Action OnJumpStarted;
    public static Action OnJumpEnded;

    public static Action<SpeedState> OnSpeedChange;
    public static Action<SpeedState> OnSpeedTargetChange;
}
