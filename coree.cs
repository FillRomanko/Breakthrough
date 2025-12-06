
using System.Security.Cryptography;

class Program
{
    public static void Main()
    {
        Core Game = new Core(8, 8);
        int[,] matrix = Game.CreateMatrix();
        int[,] cords = Game.GetPiecesThatCanMove(matrix, 1, 1);
        int[] pawnCords = { 6, 3 };
        int[] pawnMoveCords = { 5, 2 };
        Game.GameCurrrentState(matrix, 1, 1, pawnCords, pawnMoveCords);
    }
}
public class Objects
{
    public const int Space = 0;
    public const int WhitePawn = 1;
    public const int BlackPawn = 2;
    public const char Pawn = '\u2659';
}
public class Core
{
    private int _height;
    private int _width;
    public Core(int height, int width)
    {
        if(height > 5 && width > 3 && width < 17 && height < 17)
        {
            _height = height;
            _width = width;
        }
    }
    public int[,] CreateMatrix()
    {
        int[,] matrix = new int[_height, _width];
        for (int i = 0; i < _height; i++)
        {
            for (int j = 0; j < _width; j++)
            {
                if (i == 0 || i == 1)
                {
                    matrix[i, j] = Objects.BlackPawn;
                }
                else if (i == _height - 1 || i == _height - 2)
                {
                    matrix[i, j] = Objects.WhitePawn;
                }
                else
                {
                    matrix[i, j] = Objects.Space;
                }
            }
        }
        return matrix;
    }
    public int[,] GetPiecesThatCanMove(int[,] matrix, int moveNumber, int firstMove)
    {
        bool isWhiteTurn = ((moveNumber+firstMove) % 2 == 0); // Четный ход - белые, нечетный - черные

        // Подсчет количества фигур, которые могут двигаться
        int count = 0;
        for (int i = 0; i < _height; i++)
        {
            for (int j = 0; j < _width; j++)
            {
                if (isWhiteTurn && matrix[i, j] == Objects.WhitePawn)
                {
                    // Проверка для белых пешек (движение вверх)
                    if (i > 0) // Проверка, что не на верхней границе
                    {
                        if (j > 0 && j < _width - 1)
                        {
                            if (matrix[i - 1, j] == Objects.Space &&
                                matrix[i - 1, j - 1] == Objects.Space &&
                                matrix[i - 1, j + 1] == Objects.Space)
                            {
                                count++;
                            }
                        }
                        else if (j == 0) // Левая граница
                        {
                            if (matrix[i - 1, j] == Objects.Space &&
                                matrix[i - 1, j + 1] == Objects.Space)
                            {
                                count++;
                            }
                        }
                        else if (j == _width - 1) // Правая граница
                        {
                            if (matrix[i - 1, j] == Objects.Space &&
                                matrix[i - 1, j - 1] == Objects.Space)
                            {
                                count++;
                            }
                        }
                    }
                }
                else if (!isWhiteTurn && matrix[i, j] == Objects.BlackPawn)
                {
                    // Проверка для черных пешек (движение вниз)
                    if (i < _height - 1) // Проверка, что не на нижней границе
                    {
                        if (j > 0 && j < _width - 1)
                        {
                            if (matrix[i + 1, j] == Objects.Space &&
                                matrix[i + 1, j - 1] == Objects.Space &&
                                matrix[i + 1, j + 1] == Objects.Space)
                            {
                                count++;
                            }
                        }
                        else if (j == 0) // Левая граница
                        {
                            if (matrix[i + 1, j] == Objects.Space &&
                                matrix[i + 1, j + 1] == Objects.Space)
                            {
                                count++;
                            }
                        }
                        else if (j == _width - 1) // Правая граница
                        {
                            if (matrix[i + 1, j] == Objects.Space &&
                                matrix[i + 1, j - 1] == Objects.Space)
                            {
                                count++;
                            }
                        }
                    }
                }
            }
        }

        // Создание массива результатов
        int[,] piecesThatCanMove = new int[count, 2];
        int index = 0;

        // Заполнение массива координатами фигур, которые могут двигаться
        for (int i = 0; i < _height; i++)
        {
            for (int j = 0; j < _width; j++)
            {
                bool canMove = false;

                if (isWhiteTurn && matrix[i, j] == Objects.WhitePawn)
                {
                    if (i > 0)
                    {
                        if (j > 0 && j < _width - 1)
                        {
                            if (matrix[i - 1, j] == Objects.Space &&
                                matrix[i - 1, j - 1] == Objects.Space &&
                                matrix[i - 1, j + 1] == Objects.Space)
                            {
                                canMove = true;
                            }
                        }
                        else if (j == 0)
                        {
                            if (matrix[i - 1, j] == Objects.Space &&
                                matrix[i - 1, j + 1] == Objects.Space)
                            {
                                canMove = true;
                            }
                        }
                        else if (j == _width - 1)
                        {
                            if (matrix[i - 1, j] == Objects.Space &&
                                matrix[i - 1, j - 1] == Objects.Space)
                            {
                                canMove = true;
                            }
                        }
                    }
                }
                else if (!isWhiteTurn && matrix[i, j] == Objects.BlackPawn)
                {
                    if (i < _height - 1)
                    {
                        if (j > 0 && j < _width - 1)
                        {
                            if (matrix[i + 1, j] == Objects.Space &&
                                matrix[i + 1, j - 1] == Objects.Space &&
                                matrix[i + 1, j + 1] == Objects.Space)
                            {
                                canMove = true;
                            }
                        }
                        else if (j == 0)
                        {
                            if (matrix[i + 1, j] == Objects.Space &&
                                matrix[i + 1, j + 1] == Objects.Space)
                            {
                                canMove = true;
                            }
                        }
                        else if (j == _width - 1)
                        {
                            if (matrix[i + 1, j] == Objects.Space &&
                                matrix[i + 1, j - 1] == Objects.Space)
                            {
                                canMove = true;
                            }
                        }
                    }
                }

                if (canMove && index < count)
                {
                    piecesThatCanMove[index, 0] = i;
                    piecesThatCanMove[index, 1] = j;
                    index++;
                }
            }
        }

        return piecesThatCanMove;
    }
    public void GameCurrrentState(int[,] matrix, int moveNumber, int firstMove, int[] pawnCords, int[] pawnMoveCords)
    {
        // Проверка наличия пешек
        int whitePawnCounter = 0;
        int blackPawnCounter = 0;

        for (int i = 0; i < matrix.GetLength(0); i++)
        {
            for (int j = 0; j < matrix.GetLength(1); j++)
            {
                if (matrix[i, j] == Objects.WhitePawn)
                {
                    whitePawnCounter++;
                }
                if (matrix[i, j] == Objects.BlackPawn)
                {
                    blackPawnCounter++;
                }
            }
        }

        // Проверка условия окончания игры из-за отсутствия пешек
        if (whitePawnCounter == 0 || blackPawnCounter == 0)
        {
            GameOver();
            throw new Exception("Игра окончена: у одной из сторон не осталось пешек");
        }

        // Определение чей сейчас ход
        bool isWhiteTurn = ((moveNumber + firstMove) % 2 == 0);

        // Проверка хода в конец доски
        if (isWhiteTurn && pawnMoveCords[0] == 0)
        {
            GameOver();
            throw new Exception("Игра окончена: белая пешка достигла конца доски");
        }

        if (!isWhiteTurn && pawnMoveCords[0] == (matrix.GetLength(0) - 1))
        {
            GameOver();
            throw new Exception("Игра окончена: черная пешка достигла конца доски");
        }
        // Перемещение пешки
        matrix[pawnCords[0], pawnCords[1]] = Objects.Space;

        if (isWhiteTurn)
        {
            matrix[pawnMoveCords[0], pawnMoveCords[1]] = Objects.WhitePawn;
        }
        else
        {
            matrix[pawnMoveCords[0], pawnMoveCords[1]] = Objects.BlackPawn;
        }
    }
    public void GameOver()
    {
        Console.WriteLine("GAME OVER");
    }
}


