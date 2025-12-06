using System;
using System.Text;

class Program
{
    // Переменные для управления состоянием игры
    static int currentGameState = 0; // 0 - выбор пешки, 1 - выбор хода, 2 - меню паузы, 3 - меню выхода
    static int currentMenuSelection = 0; // для меню выхода
    static int[,] availableMoves = new int[0, 2]; // доступные ходы
    static int selectedPawnRow = -1, selectedPawnCol = -1; // выбранная пешка

    static void Main()
    {
        Console.OutputEncoding = Encoding.UTF8;
        Console.BackgroundColor = ConsoleColor.DarkGray;

        // Игровое поле, передаёт Александр
        int[,] field = new int[8, 8];

        for (int i = 0; i < 8; i++)
        {
            for (int j = 0; j < 8; j++)
            {
                if (i < 2)          // Верхние два ряда
                    field[i, j] = 1;
                else if (i > 5)     // Нижние два ряда
                    field[i, j] = 2;
                else                // Остальные
                    field[i, j] = 0;
            }
        }

        // Координаты пешек, тоже передаёт Александр
        int[,] coords = { { 1, 0 }, { 1, 1 }, { 1, 2 }, { 1, 3 }, { 1, 4 }, { 1, 5 }, { 1, 6 }, { 1, 7 } };

        // Индекс текущей выбранной ячейки среди пешек, которые можно выбрать
        int currentIdx = 0;
        ConsoleKey key;

        // Основной цикл программы
        do
        {
            // Очищаем консоль
            Console.Clear();

            // В зависимости от состояния игры отображаем разные экраны
            switch (currentGameState)
            {
                case 0: // Выбор пешки
                    DisplaySelectPawnScreen(field, coords, currentIdx);
                    break;
                case 1: // Выбор хода
                    DisplaySelectMoveScreen(field, availableMoves, currentIdx);
                    break;
                case 2: // Меню паузы
                    DisplayPauseMenu();
                    break;
                case 3: // Меню выхода
                    DisplayExitMenu();
                    break;
            }

            // Считываем нажатую клавишу. Аргумент true не даёт сделать ввод в консоль
            key = Console.ReadKey(true).Key;

            // Обрабатываем ввод в зависимости от состояния игры
            HandleInput(key, ref currentIdx, coords, field);

        } while (key != ConsoleKey.Escape || currentGameState != 0);
    }

    // Обработка ввода в зависимости от состояния игры
    static void HandleInput(ConsoleKey key, ref int currentIdx, int[,] coords, int[,] field)
    {
        switch (currentGameState)
        {
            case 0: // Выбор пешки
                HandleSelectPawnInput(key, ref currentIdx, coords);
                break;
            case 1: // Выбор хода
                HandleSelectMoveInput(key, ref currentIdx, availableMoves);
                break;
            case 2: // Меню паузы
                HandlePauseMenuInput(key);
                break;
            case 3: // Меню выхода
                HandleExitMenuInput(key);
                break;
        }
    }

    // Обработка ввода при выборе пешки
    static void HandleSelectPawnInput(ConsoleKey key, ref int currentIdx, int[,] coords)
    {
        switch (key)
        {
            case ConsoleKey.RightArrow:
            case ConsoleKey.D:
                currentIdx = NextInRowRight(coords, currentIdx);
                break;
            case ConsoleKey.LeftArrow:
            case ConsoleKey.A:
                currentIdx = NextInRowLeft(coords, currentIdx);
                break;
            case ConsoleKey.UpArrow:
            case ConsoleKey.W:
                currentIdx = NextRowUp(coords, currentIdx);
                break;
            case ConsoleKey.DownArrow:
            case ConsoleKey.S:
                currentIdx = NextRowDown(coords, currentIdx);
                break;
            case ConsoleKey.Enter:
                // Сохраняем выбранную пешку и запрашиваем доступные ходы
                selectedPawnRow = coords[currentIdx, 0];
                selectedPawnCol = coords[currentIdx, 1];
                RequestAvailableMoves(selectedPawnRow, selectedPawnCol);
                currentGameState = 1; // Переходим к выбору хода
                currentIdx = 0; // Сбрасываем индекс для выбора хода
                break;
            case ConsoleKey.Escape:
                currentGameState = 2; // Переходим в меню паузы
                break;
        }
    }

    // Обработка ввода при выборе хода
    static void HandleSelectMoveInput(ConsoleKey key, ref int currentIdx, int[,] moves)
    {
        switch (key)
        {
            case ConsoleKey.RightArrow:
            case ConsoleKey.D:
                if (moves.GetLength(0) > 0)
                    currentIdx = NextInRowRight(moves, currentIdx);
                break;
            case ConsoleKey.LeftArrow:
            case ConsoleKey.A:
                if (moves.GetLength(0) > 0)
                    currentIdx = NextInRowLeft(moves, currentIdx);
                break;
            case ConsoleKey.UpArrow:
            case ConsoleKey.W:
                if (moves.GetLength(0) > 0)
                    currentIdx = NextRowUp(moves, currentIdx);
                break;
            case ConsoleKey.DownArrow:
            case ConsoleKey.S:
                if (moves.GetLength(0) > 0)
                    currentIdx = NextRowDown(moves, currentIdx);
                break;
            case ConsoleKey.Enter:
                if (moves.GetLength(0) > 0)
                {
                    // Отправляем выбранный ход Александру
                    SendMove(selectedPawnRow, selectedPawnCol, moves[currentIdx, 0], moves[currentIdx, 1]);
                }
                break;
            case ConsoleKey.Escape:
                currentGameState = 0; // Возвращаемся к выбору пешки
                break;
        }
    }

    // Обработка ввода в меню паузы
    static void HandlePauseMenuInput(ConsoleKey key)
    {
        switch (key)
        {
            case ConsoleKey.Enter:
                currentGameState = 0; // Продолжить игру - возвращаемся к выбору пешки
                break;
            case ConsoleKey.Escape:
                currentGameState = 3; // Переходим в меню выхода
                currentMenuSelection = 0; // Сбрасываем выбор на первый вариант
                break;
        }
    }

    // Обработка ввода в меню выхода
    static void HandleExitMenuInput(ConsoleKey key)
    {
        switch (key)
        {
            case ConsoleKey.UpArrow:
                currentMenuSelection = 0; // Перемещаем выбор вверх
                break;
            case ConsoleKey.DownArrow:
                currentMenuSelection = 1; // Перемещаем выбор вниз
                break;
            case ConsoleKey.Enter:
                if (currentMenuSelection == 0)
                {
                    // Покинуть игру
                    Environment.Exit(0);
                }
                else
                {
                    // Продолжить игру - возвращаемся в меню паузы
                    currentGameState = 2;
                }
                break;
            case ConsoleKey.Escape:
                // Возвращаемся в меню паузы
                currentGameState = 2;
                break;
        }
    }

    // Экран выбора пешки
    static void DisplaySelectPawnScreen(int[,] field, int[,] coords, int currentIdx)
    {
        Console.WriteLine("=== ВЫБОР ПЕШКИ ===");
        Console.WriteLine("Используйте стрелки для выбора пешки, Enter для подтверждения");
        Console.WriteLine("Нажмите Esc для выхода в меню паузы");
        Console.WriteLine();

        // Отрисовка поля с выделением доступных пешек
        DrawField(field, coords, currentIdx);
    }

    // Экран выбора хода
    static void DisplaySelectMoveScreen(int[,] field, int[,] moves, int currentIdx)
    {
        Console.WriteLine("=== ВЫБОР ХОДА ===");
        Console.WriteLine("Выберите куда походить пешкой");
        Console.WriteLine("Используйте стрелки для выбора хода, Enter для подтверждения");
        Console.WriteLine("Нажмите Esc для возврата к выбору пешки");
        Console.WriteLine();

        // Отрисовка поля с выделением доступных ходов
        DrawField(field, moves, currentIdx);
    }

    // Меню паузы
    static void DisplayPauseMenu()
    {
        Console.WriteLine("=== МЕНЮ ПАУЗЫ ===");
        Console.WriteLine("Продолжить игру - Enter");
        Console.WriteLine("Выйти из игры - Esc");
        Console.WriteLine();
        Console.WriteLine("Выберите действие:");
    }

    // Меню выхода
    static void DisplayExitMenu()
    {
        Console.WriteLine("=== ВЫХОД ИЗ ИГРЫ ===");

        // Отображаем варианты выбора
        if (currentMenuSelection == 0)
        {
            Console.WriteLine("-> Покинуть игру");
            Console.WriteLine("   Продолжить игру");
        }
        else
        {
            Console.WriteLine("   Покинуть игру");
            Console.WriteLine("-> Продолжить игру");
        }

        Console.WriteLine();
        Console.WriteLine("Используйте стрелки ↑↓ для выбора, Enter для подтверждения");
    }

    // Запрос доступных ходов у Александра (заглушка)
    static void RequestAvailableMoves(int row, int col)
    {
        // В реальной реализации здесь будет обращение к коду Александра
        // Сейчас используем заглушку - создаем тестовые доступные ходы

        Console.WriteLine($"Запрос к Александру: получить ходы для пешки [{row},{col}]...");

        // Заглушка - имитируем получение данных
        // Например, для пешки в позиции (1,0) доступны ходы вперед
        availableMoves = new int[,] { { 2, 0 }, { 3, 0 } };

        System.Threading.Thread.Sleep(300);
    }

    // Отправка хода Александру (заглушка)
    static void SendMove(int fromRow, int fromCol, int toRow, int toCol)
    {

        Console.WriteLine($"Отправка хода Александру: с [{fromRow},{fromCol}] на [{toRow},{toCol}]...");

        System.Threading.Thread.Sleep(300);

        Console.WriteLine("Ход отправлен! Игровое поле обновлено.");
    }

    static int NextInRowRight(int[,] coords, int idx)
    {
        int n = coords.GetLength(0);
        int row = coords[idx, 0], col = coords[idx, 1];
        int firstIdx = -1;
        for (int i = 0; i < n; i++)
        {
            if (coords[i, 0] == row)
            {
                if (firstIdx == -1) firstIdx = i;
                if (coords[i, 1] > col)
                    return i;
            }
        }
        return firstIdx;
    }

    static int NextInRowLeft(int[,] coords, int idx)
    {
        int n = coords.GetLength(0);
        int row = coords[idx, 0], col = coords[idx, 1];
        int lastIdx = -1;
        for (int i = n - 1; i >= 0; i--)
        {
            if (coords[i, 0] == row)
            {
                if (lastIdx == -1) lastIdx = i;
                if (coords[i, 1] < col)
                    return i;
            }
        }
        return lastIdx;
    }

    static int NextRowDown(int[,] coords, int idx)
    {
        int n = coords.GetLength(0);
        int curRow = coords[idx, 0];
        int minRow = int.MaxValue, targetIdx = -1;

        for (int i = 0; i < n; i++)
        {
            int row = coords[i, 0];
            if (row > curRow && row < minRow)
            {
                minRow = row;
                targetIdx = i;
            }
        }

        if (targetIdx != -1)
            return targetIdx;

        minRow = int.MaxValue;
        for (int i = 0; i < n; i++)
            if (coords[i, 0] < minRow)
            {
                minRow = coords[i, 0];
                targetIdx = i;
            }
        return targetIdx;
    }

    static int NextRowUp(int[,] coords, int idx)
    {
        int n = coords.GetLength(0);
        int curRow = coords[idx, 0];
        int maxRow = int.MinValue, targetIdx = -1;

        for (int i = 0; i < n; i++)
        {
            int row = coords[i, 0];
            if (row < curRow && row > maxRow)
            {
                maxRow = row;
                targetIdx = i;
            }
        }

        if (targetIdx != -1)
            return targetIdx;

        maxRow = int.MinValue;
        for (int i = 0; i < n; i++)
            if (coords[i, 0] > maxRow)
            {
                maxRow = coords[i, 0];
                targetIdx = i;
            }
        return targetIdx;
    }

    public static class Objects
    {
        public const int Space = 0;
        public const int WhitePawn = 1;
        public const int BlackPawn = 2;
        public const char Pawn = '\u2659';
    }

    static void DrawField(int[,] field, int[,] coords, int idx)
    {
        int rows = field.GetLength(0), cols = field.GetLength(1);

        Console.BackgroundColor = ConsoleColor.DarkGray;
        Console.Write("+");
        for (int c = 0; c < cols; c++)
        {
            Console.BackgroundColor = ConsoleColor.DarkGray;
            Console.Write("---+");
        }

        Console.WriteLine();

        for (int r = 0; r < rows; r++)
        {
            Console.BackgroundColor = ConsoleColor.DarkGray;
            Console.Write("|");
            for (int c = 0; c < cols; c++)
            {
                bool isSelectable = false;
                for (int i = 0; i < coords.GetLength(0); i++)
                    if (coords[i, 0] == r && coords[i, 1] == c)
                        isSelectable = true;

                bool isSelected = (coords.GetLength(0) > 0 && r == coords[idx, 0] && c == coords[idx, 1]);

                if (!isSelected && !isSelectable)
                {
                    if (field[r, c] == 1)
                    {
                        Console.ForegroundColor = ConsoleColor.White;
                        Console.Write($" {Objects.Pawn} ");
                        Console.ResetColor();
                        Console.BackgroundColor = ConsoleColor.DarkGray;
                        Console.Write("|");
                    }
                    else if (field[r, c] == 2)
                    {
                        Console.ForegroundColor = ConsoleColor.Black;
                        Console.Write($" {Objects.Pawn} ");
                        Console.ResetColor();
                        Console.BackgroundColor = ConsoleColor.DarkGray;
                        Console.Write("|");
                    }
                    else if (field[r, c] == 0)
                    {
                        Console.BackgroundColor = ConsoleColor.DarkGray;
                        Console.Write("   |");
                    }
                }
                else if (isSelected)
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.Write($"[{Objects.Pawn}]");
                    Console.ResetColor();
                    Console.BackgroundColor = ConsoleColor.DarkGray;
                    Console.Write("|");
                }
                else if (isSelectable)
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.Write($" {Objects.Pawn} ");
                    Console.ResetColor();
                    Console.BackgroundColor = ConsoleColor.DarkGray;
                    Console.Write("|");
                }
            }

            Console.WriteLine();
            Console.BackgroundColor = ConsoleColor.DarkGray;
            Console.Write("+");
            for (int c = 0; c < cols; c++)
            {
                Console.BackgroundColor = ConsoleColor.DarkGray;
                Console.Write("---+");
            }
            Console.WriteLine();
        }
    }
}