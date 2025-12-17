namespace Breakthrough;

internal struct Objects
{
    public const int Space = 0;
    public const int WhitePawn = 1;
    public const int BlackPawn = 2;
    public const char Pawn = '\u2659';
}

static class Program
{
    public static readonly Random Random = new();
    static void Main()
    {
        // Инициализируем игру
        GameController.Initialize();
    }
}