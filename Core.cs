
using System.ComponentModel.Design;
using System.Text;

class Program
{
    public static void Main()
    {
        Console.OutputEncoding = Encoding.UTF8;
        Console.WriteLine("Введите высоту доски и ширину доски через Enter:");
        PrintBoard(CreateMatrix(boardSizeY, boardSizeX));
        Console.WriteLine(GetPlayerChoice(WhitePieces(CreateMatrix(boardSizeY, boardSizeX))));
    }

    //=========Создание Игрового Поля==========

    public static int boardSizeY = int.Parse(Console.ReadLine()); //получаем значение высоты доски
    public static int boardSizeX = int.Parse(Console.ReadLine()); //получаем значение ширины доски
    public static int freespot = 0; //значение пустых полей
    public static int white = 1; //значение игрока, играющего за белых
    public static int black = 2; //значение игрока, играющего за черных

    //Создаю матрицу 
    public static int[,] CreateMatrix (int boardSizeY, int boardSizeX)
    {
        int[,] newMatrix = new int[boardSizeY, boardSizeX];
        for(int i = 0; i < boardSizeY; i++)
        {
            for(int j = 0; j < boardSizeX; j++)
            {
                if (i == 0 || i == 1) 
                {
                    newMatrix[i, j] = black;
                }
                else if (i == boardSizeY - 1 || i == boardSizeY - 2)
                {
                    newMatrix[i, j] = white;
                }
                else
                {
                    newMatrix[i, j] = freespot;
                }
            }
        }
        return newMatrix;
    }
    //метод для вывода доски в консоль
    public static void PrintBoard(int[,] boardMatrix)
    {
        for(int i = 0; i < boardMatrix.GetLength(0); i++)
        {
            Console.Write("\t");
            for (int j = 0; j < boardMatrix.GetLength(1); j++)
            {
                if (boardMatrix[i, j] == freespot)
                {
                    Console.Write(boardMatrix[i, j] + " ");
                }
                if (boardMatrix[i, j] == white)
                {
                    Console.Write("■" + " ");
                }
                if (boardMatrix[i, j] == black)
                {
                    Console.Write("□" + " ");
                }
            }
            Console.WriteLine();
        }
    }
    //==========Создание отдельных массивов фигур============//
    public static int[] WhitePieces(int[,] matrix)
    {
        int pieceCounter = 0;
        for (int i = 0;i < matrix.GetLength(0); i++)
        {
            for (int j = 0; j < matrix.GetLength(1);j++)
            {
                if(matrix[i, j] == white)
                {
                    pieceCounter++;
                }
            }
        }
        int[] whiteArr = new int[pieceCounter];
        for (int i = 0; i < whiteArr.Length; i++)
        {
            whiteArr[i]++;
        }
        return whiteArr;
    }
    public static int[] BlackPieces(int[,] matrix)
    {
        int pieceCounter = 0;
        for (int i = 0; i < matrix.GetLength(0); i++)
        {
            for (int j = 0; j < matrix.GetLength(1); j++)
            {
                if (matrix[i, j] == black)
                {
                    pieceCounter++;
                }
            }
        }
        int[] blackArr = new int[pieceCounter];
        for(int i = 0; i<blackArr.Length; i++)
        {
            blackArr[i]++;
        }
        return blackArr;
    }
    //==========Создание Игрового процесса=======================//

    //Выбор фигуры игроком
    public static int GetPlayerChoice(int[] arr)
    {
        int choosing = 1; //количество наших попыток сделать выбор
        int chosen = 0; //наш выбор
        bool choosingCondition = true;
        do
            {
            ConsoleKeyInfo choosingMove = Console.ReadKey();
            if (choosingMove.Key == ConsoleKey.UpArrow)
                {
                    for (int i = 0; i < choosing; i++)
                    {
                        Console.WriteLine(i);
                        ConsoleKeyInfo approveMove = Console.ReadKey();
                        if (approveMove.Key == ConsoleKey.Enter)
                        {
                                chosen = choosing-1;
                                choosingCondition = false;
                        }
                        else if(approveMove.Key == ConsoleKey.UpArrow)
                        {
                        if (choosing >= arr.Length)
                        {
                            choosing = 0;
                        }
                        choosing++;
                        continue;
                        
                        }
                    Console.WriteLine($"Вы выбрали свою {chosen} фигуру!");
                }

            }
            }
            while (choosingCondition);
        return chosen;
    }
    public static int order = 0; //порядок хода
    public static int positionX = 3;
    public static int positionY = 3;
    public static int[] ChangePosition (int positionX, int positionY)
    {
        int[]move = {positionX, positionY};
        return move;
    }
    public static void DisplayMove(int order, int[] move, int[,] boardMatrix)
    {
        Console.SetCursorPosition(0, 0);
        Console.Write(new string (' ', Console.WindowWidth));
        Console.SetCursorPosition(move[0], move[1]);
        for(int i = 0; i< boardMatrix.GetLength(1); i++)
        {
            for(int j = 0;  j< boardMatrix.GetLength(2); j++)
            {
                if (order == 0)
                {

                }
            }
        }
    }
}