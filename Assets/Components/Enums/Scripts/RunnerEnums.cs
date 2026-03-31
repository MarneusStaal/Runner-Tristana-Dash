// Represent the player moving states
public enum SpeedState
{
    Stop,
    Walk,
    Run,
    Fly
}

// The commands the game is able to handle
public enum PlayerCommand
{
    Idle,
    MoveLeft,
    MoveRight,
    MoveUp,
    MoveDown,
    Jump
}

// The different types of objects types you can collect in the game
public enum CollectableType
{
    Fuel,
    RedBottle,
    GreenBottle,
    BlueBottle,
    Candle
}