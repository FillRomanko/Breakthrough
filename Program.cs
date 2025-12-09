namespace Breakthrough;

public struct Objects
{
    public const int Space = 0;
    public const int WhitePawn = 1;
    public const int BlackPawn = 2;
    
    public const char Pawn = '\u2659';
}

class Program
{
    static void Main()
    {
        // Инициализируем игру
        GameController.Initialize();
    }
}