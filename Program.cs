using System;
using System.Text;

// Базовый класс для состояний игры
abstract class GameState
{
    protected static int selection = 0;

    public abstract void Display();
    public abstract void HandleInput(ConsoleKey key);
    public virtual void Reset() { }
}

// Главное меню
class MainMenuState : GameState
{
    private static int mainMenuSelection = 0;
    private bool hasSavedGames = false;

    public override void Display()
    {
        Console.Clear();
        Console.WriteLine("=== ГЛАВНОЕ МЕНЮ ===\n");

        // 1. Продолжить игру
        Console.Write(mainMenuSelection == 0 ? "→ " : "  ");
        hasSavedGames = CheckSavedGames();
        if (hasSavedGames)
        {
            Console.WriteLine("1. Продолжить игру");
        }
        else
        {
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.WriteLine("1. Продолжить игру (нет сохранений)");
            Console.ResetColor();
        }

        // 2. Новая игра
        Console.Write(mainMenuSelection == 1 ? "→ " : "  ");
        Console.WriteLine("2. Новая игра");

        // 3. Статистика
        Console.Write(mainMenuSelection == 2 ? "→ " : "  ");
        Console.WriteLine("3. Статистика");

        // 4. Закрыть игру
        Console.Write(mainMenuSelection == 3 ? "→ " : "  ");
        Console.WriteLine("4. Закрыть игру");

        Console.WriteLine("\nИспользуйте стрелки ↑↓ для выбора, Enter для подтверждения, Esc для выхода");
    }

    public override void HandleInput(ConsoleKey key)
    {
        switch (key)
        {
            case ConsoleKey.UpArrow:
                mainMenuSelection = (mainMenuSelection - 1 + 4) % 4;
                break;
            case ConsoleKey.DownArrow:
                mainMenuSelection = (mainMenuSelection + 1) % 4;
                break;
            case ConsoleKey.Enter:
                switch (mainMenuSelection)
                {
                    case 0: // Продолжить игру
                        if (hasSavedGames)
                        {
                            GameController.LoadSavedGamesMenu();
                            GameController.ChangeState(new SavedGamesState());
                        }
                        break;
                    case 1: // Новая игра
                        GameController.ChangeState(new NewGameMenuState());
                        break;
                    case 2: // Статистика
                        GameController.ChangeState(new StatisticsState());
                        break;
                    case 3: // Закрыть игру
                        Environment.Exit(0);
                        break;
                }
                break;
            case ConsoleKey.Escape:
                Environment.Exit(0);
                break;
        }
    }

    private bool CheckSavedGames()
    {
        return false; // Заглушка
    }
}

// Меню новой игры
class NewGameMenuState : GameState
{
    private static int newGameMenuSelection = 0;
    private static string whitePlayerName = "Игрок 1";
    private static string blackPlayerName = "Игрок 2";
    private static bool whiteStarts = true;
    private static int boardHeight = 8;
    private static int boardWidth = 8;

    public override void Display()
    {
        Console.Clear();
        Console.WriteLine("=== НАСТРОЙКИ НОВОЙ ИГРЫ ===\n");

        // Белые игрок
        Console.Write(newGameMenuSelection == 0 ? "→ " : "  ");
        Console.WriteLine($"Белые: {whitePlayerName}");

        // Черные игрок
        Console.Write(newGameMenuSelection == 1 ? "→ " : "  ");
        Console.WriteLine($"Чёрные: {blackPlayerName}");

        // Первый ход
        Console.Write(newGameMenuSelection == 2 ? "→ " : "  ");
        string firstMoveText = whiteStarts ? "белые" : "чёрные";
        Console.WriteLine($"Первый ход: {firstMoveText}");

        // Высота доски
        Console.Write(newGameMenuSelection == 3 ? "→ " : "  ");
        Console.WriteLine($"Высота доски: {boardHeight}");

        // Ширина доски
        Console.Write(newGameMenuSelection == 4 ? "→ " : "  ");
        Console.WriteLine($"Ширина доски: {boardWidth}");

        // Кнопка начать игру
        Console.WriteLine();
        bool canStart = !string.IsNullOrWhiteSpace(whitePlayerName) &&
                       !string.IsNullOrWhiteSpace(blackPlayerName);

        if (canStart)
        {
            Console.Write(newGameMenuSelection == 5 ? "→ " : "  ");
            Console.WriteLine("НАЧАТЬ ИГРУ");
        }
        else
        {
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.WriteLine("  НАЧАТЬ ИГРУ (укажите имена игроков)");
            Console.ResetColor();
        }

        Console.WriteLine("\nИспользуйте стрелки ↑↓ для выбора, ←→ для изменения, Enter для редактирования, Esc для отмены");
    }

    public override void HandleInput(ConsoleKey key)
    {
        switch (key)
        {
            case ConsoleKey.UpArrow:
                newGameMenuSelection = (newGameMenuSelection - 1 + 6) % 6;
                break;
            case ConsoleKey.DownArrow:
                newGameMenuSelection = (newGameMenuSelection + 1) % 6;
                break;
            case ConsoleKey.Enter:
                if (newGameMenuSelection == 5) // Начать игру
                {
                    if (!string.IsNullOrWhiteSpace(whitePlayerName) &&
                        !string.IsNullOrWhiteSpace(blackPlayerName))
                    {
                        GameController.StartNewGame(whitePlayerName, blackPlayerName,
                                                  whiteStarts, boardHeight, boardWidth);
                        GameController.ChangeState(new SelectPawnState());
                    }
                }
                else if (newGameMenuSelection == 0) // Редактировать имя белых
                {
                    Console.Write("\nВведите имя белых игроков: ");
                    whitePlayerName = Console.ReadLine();
                    if (string.IsNullOrWhiteSpace(whitePlayerName))
                        whitePlayerName = "Игрок 1";
                }
                else if (newGameMenuSelection == 1) // Редактировать имя черных
                {
                    Console.Write("\nВведите имя чёрных игроков: ");
                    blackPlayerName = Console.ReadLine();
                    if (string.IsNullOrWhiteSpace(blackPlayerName))
                        blackPlayerName = "Игрок 2";
                }
                break;
            case ConsoleKey.RightArrow:
                HandleNewGameChange(true);
                break;
            case ConsoleKey.LeftArrow:
                HandleNewGameChange(false);
                break;
            case ConsoleKey.Escape:
                GameController.ChangeState(new MainMenuState());
                break;
        }
    }

    private void HandleNewGameChange(bool increase)
    {
        switch (newGameMenuSelection)
        {
            case 2: // Первый ход
                whiteStarts = !whiteStarts;
                break;
            case 3: // Высота доски
                if (increase && boardHeight < 16) boardHeight++;
                else if (!increase && boardHeight > 6) boardHeight--;
                break;
            case 4: // Ширина доски
                if (increase && boardWidth < 16) boardWidth++;
                else if (!increase && boardWidth > 4) boardWidth--;
                break;
        }
    }

    public override void Reset()
    {
        whitePlayerName = "Игрок 1";
        blackPlayerName = "Игрок 2";
        whiteStarts = true;
        boardHeight = 8;
        boardWidth = 8;
        newGameMenuSelection = 0;
    }
}

// Сохраненные игры
class SavedGamesState : GameState
{
    private string[,] savedGames;

    public override void Display()
    {
        Console.Clear();
        Console.WriteLine("=== ВЫБОР СОХРАНЕННОЙ ИГРЫ ===\n");

        savedGames = GameController.GetSavedGames();

        if (savedGames == null || savedGames.GetLength(0) == 0)
        {
            Console.WriteLine("Нет сохраненных игр");
            Console.WriteLine("\nНажмите любую клавишу для возврата...");
            Console.ReadKey();
            GameController.ChangeState(new MainMenuState());
            return;
        }

        for (int i = 0; i < savedGames.GetLength(0); i++)
        {
            Console.WriteLine($"{i + 1}. {savedGames[i, 0]} - {savedGames[i, 1]} vs {savedGames[i, 2]}");
        }

        Console.WriteLine("\nВведите номер игры для загрузки или 0 для отмены:");
        string input = Console.ReadLine();
        if (int.TryParse(input, out int choice) && choice > 0 && choice <= savedGames.GetLength(0))
        {
            GameController.LoadGame(choice - 1);
            GameController.ChangeState(new SelectPawnState());
        }
        else if (choice == 0)
        {
            GameController.ChangeState(new MainMenuState());
        }
    }

    public override void HandleInput(ConsoleKey key) { }
}

// Статистика
class StatisticsState : GameState
{
    private string[,] statisticsData;

    public override void Display()
    {
        Console.Clear();
        Console.WriteLine("=== СТАТИСТИКА ===\n");

        statisticsData = GameController.GetStatisticsData();

        if (statisticsData == null || statisticsData.GetLength(0) == 0)
        {
            Console.WriteLine("Статистика пуста");
        }
        else
        {
            Console.WriteLine("{0,-15} {1,-10} {2,-10} {3,-12} {4,-10}",
                "Игрок", "Побед", "Поражений", "Всего игр", "% побед");
            Console.WriteLine(new string('-', 60));

            for (int i = 0; i < statisticsData.GetLength(0); i++)
            {
                Console.WriteLine("{0,-15} {1,-10} {2,-10} {3,-12} {4,-10}%",
                    statisticsData[i, 0],
                    statisticsData[i, 1],
                    statisticsData[i, 2],
                    statisticsData[i, 3],
                    statisticsData[i, 4]);
            }
        }

        Console.WriteLine("\nНажмите Esc для возврата в меню");
    }

    public override void HandleInput(ConsoleKey key)
    {
        if (key == ConsoleKey.Escape)
        {
            GameController.ChangeState(new MainMenuState());
        }
    }
}

// Базовый класс для игровых состояний
abstract class GameplayState : GameState
{
    protected static int[,] availableMoves = new int[0, 2];
    protected static int selectedPawnRow = -1, selectedPawnCol = -1;
    protected static int currentIdx = 0;

    // Методы для работы с координатами - исправлены направления
    protected int NextInRowRight(int[,] coords, int idx)
    {
        int n = coords.GetLength(0);
        int row = coords[idx, 0], col = coords[idx, 1];
        int firstIdx = -1;

        // Ищем самую левую пешку в ряду
        for (int i = 0; i < n; i++)
        {
            if (coords[i, 0] == row)
            {
                if (firstIdx == -1 || coords[i, 1] < coords[firstIdx, 1])
                    firstIdx = i;
            }
        }

        // Ищем пешку правее текущей
        int nextIdx = -1;
        for (int i = 0; i < n; i++)
        {
            if (coords[i, 0] == row && coords[i, 1] > col)
            {
                if (nextIdx == -1 || coords[i, 1] < coords[nextIdx, 1])
                    nextIdx = i;
            }
        }

        return nextIdx != -1 ? nextIdx : firstIdx;
    }

    protected int NextInRowLeft(int[,] coords, int idx)
    {
        int n = coords.GetLength(0);
        int row = coords[idx, 0], col = coords[idx, 1];
        int lastIdx = -1;

        // Ищем самую правую пешку в ряду
        for (int i = 0; i < n; i++)
        {
            if (coords[i, 0] == row)
            {
                if (lastIdx == -1 || coords[i, 1] > coords[lastIdx, 1])
                    lastIdx = i;
            }
        }

        // Ищем пешку левее текущей
        int prevIdx = -1;
        for (int i = 0; i < n; i++)
        {
            if (coords[i, 0] == row && coords[i, 1] < col)
            {
                if (prevIdx == -1 || coords[i, 1] > coords[prevIdx, 1])
                    prevIdx = i;
            }
        }

        return prevIdx != -1 ? prevIdx : lastIdx;
    }

    protected int NextRowDown(int[,] coords, int idx)
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

    protected int NextRowUp(int[,] coords, int idx)
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
}

// Выбор пешки
class SelectPawnState : GameplayState
{
    public override void Display()
    {
        Console.Clear();
        Console.WriteLine($"=== ИГРА ПРОРЫВ ===");
        Console.WriteLine($"Белые: {GameController.WhitePlayerName} | Чёрные: {GameController.BlackPlayerName}");

        // Определяем, какой игрок сейчас должен ходить
        string currentPlayer;
        if (GameController.IsWhiteTurn)
        {
            currentPlayer = $"Сейчас ходит: {GameController.WhitePlayerName} (белые)";
        }
        else
        {
            currentPlayer = $"Сейчас ходит: {GameController.BlackPlayerName} (чёрные)";
        }
        Console.WriteLine(currentPlayer);

        Console.WriteLine();
        Console.WriteLine("=== ВЫБОР ПЕШКИ ===");
        Console.WriteLine("Используйте стрелки для выбора своей пешки, Enter для подтверждения");
        Console.WriteLine("Нажмите Esc для меню паузы");
        Console.WriteLine();
        DrawField();
    }

    public override void HandleInput(ConsoleKey key)
    {
        switch (key)
        {
            case ConsoleKey.RightArrow:
            case ConsoleKey.D:
                // Исправлено: правая стрелка идёт вправо (к большим индексам колонок)
                currentIdx = NextInRowRight(GameController.Coords, currentIdx);
                break;
            case ConsoleKey.LeftArrow:
            case ConsoleKey.A:
                // Исправлено: левая стрелка идёт влево (к меньшим индексам колонок)
                currentIdx = NextInRowLeft(GameController.Coords, currentIdx);
                break;
            case ConsoleKey.UpArrow:
            case ConsoleKey.W:
                // Вверх - к меньшим индексам строк
                currentIdx = NextRowUp(GameController.Coords, currentIdx);
                break;
            case ConsoleKey.DownArrow:
            case ConsoleKey.S:
                // Вниз - к большим индексам строк
                currentIdx = NextRowDown(GameController.Coords, currentIdx);
                break;
            case ConsoleKey.Enter:
                selectedPawnRow = GameController.Coords[currentIdx, 0];
                selectedPawnCol = GameController.Coords[currentIdx, 1];

                // Проверяем, что выбрана пешка текущего игрока
                int selectedPawn = GameController.Field[selectedPawnRow, selectedPawnCol];
                bool isCorrectPawn = (GameController.IsWhiteTurn && selectedPawn == 1) ||
                                   (!GameController.IsWhiteTurn && selectedPawn == 2);

                if (isCorrectPawn)
                {
                    RequestAvailableMoves(selectedPawnRow, selectedPawnCol);

                    // Проверяем, есть ли доступные ходы у выбранной пешки
                    if (availableMoves.GetLength(0) > 0)
                    {
                        currentIdx = 0;
                        GameController.ChangeState(new SelectMoveState());
                    }
                    else
                    {
                        Console.WriteLine("\nУ этой пешки нет доступных ходов!");
                        Console.WriteLine("Нажмите любую клавишу...");
                        Console.ReadKey();
                    }
                }
                else
                {
                    Console.WriteLine("\nВы не можете ходить чужими пешками!");
                    Console.WriteLine("Нажмите любую клавишу...");
                    Console.ReadKey();
                }
                break;
            case ConsoleKey.Escape:
                GameController.ChangeState(new PauseMenuState());
                break;
        }
    }

    private void RequestAvailableMoves(int row, int col)
    {
        if (GameController.Field[row, col] == 1) // Белые
        {
            availableMoves = new int[,] {
                { row + 1, col },
                { row + 1, col - 1 },
                { row + 1, col + 1 }
            };
        }
        else if (GameController.Field[row, col] == 2) // Черные
        {
            availableMoves = new int[,] {
                { row - 1, col },
                { row - 1, col - 1 },
                { row - 1, col + 1 }
            };
        }
        else
        {
            availableMoves = new int[0, 2];
        }

        // Фильтруем ходы, которые выходят за пределы доски или заняты своими пешками
        FilterAvailableMoves();
    }

    private void FilterAvailableMoves()
    {
        int rows = GameController.Field.GetLength(0);
        int cols = GameController.Field.GetLength(1);
        int currentPlayer = GameController.IsWhiteTurn ? 1 : 2;

        // Создаем временный список для валидных ходов
        var validMoves = new System.Collections.Generic.List<int[]>();

        for (int i = 0; i < availableMoves.GetLength(0); i++)
        {
            int toRow = availableMoves[i, 0];
            int toCol = availableMoves[i, 1];

            // Проверяем границы доски
            if (toRow >= 0 && toRow < rows && toCol >= 0 && toCol < cols)
            {
                // Проверяем, что клетка не занята своей пешкой
                if (GameController.Field[toRow, toCol] != currentPlayer)
                {
                    validMoves.Add(new int[] { toRow, toCol });
                }
            }
        }

        // Преобразуем список обратно в массив
        availableMoves = new int[validMoves.Count, 2];
        for (int i = 0; i < validMoves.Count; i++)
        {
            availableMoves[i, 0] = validMoves[i][0];
            availableMoves[i, 1] = validMoves[i][1];
        }
    }

    private void DrawField()
    {
        GameController.DrawField(GameController.Coords, currentIdx, true);
    }
}

// Выбор хода
class SelectMoveState : GameplayState
{
    public override void Display()
    {
        Console.Clear();
        Console.WriteLine($"=== ИГРА ПРОРЫВ ===");
        Console.WriteLine($"Белые: {GameController.WhitePlayerName} | Чёрные: {GameController.BlackPlayerName}");

        // Определяем, какой игрок сейчас должен ходить
        string currentPlayer;
        if (GameController.IsWhiteTurn)
        {
            currentPlayer = $"Сейчас ходит: {GameController.WhitePlayerName} (белые)";
        }
        else
        {
            currentPlayer = $"Сейчас ходит: {GameController.BlackPlayerName} (чёрные)";
        }
        Console.WriteLine(currentPlayer);

        Console.WriteLine();
        Console.WriteLine("=== ВЫБОР ХОДА ===");
        Console.WriteLine("Выберите куда походить пешкой");
        Console.WriteLine("Используйте стрелки для выбора хода, Enter для подтверждения");
        Console.WriteLine("Нажмите Esc для возврата к выбору пешки");
        Console.WriteLine();
        DrawField();
    }

    public override void HandleInput(ConsoleKey key)
    {
        switch (key)
        {
            case ConsoleKey.RightArrow:
            case ConsoleKey.D:
                if (availableMoves.GetLength(0) > 0)
                {
                    // Исправлено: правая стрелка увеличивает индекс колонки
                    currentIdx = (currentIdx + 1) % availableMoves.GetLength(0);
                }
                break;
            case ConsoleKey.LeftArrow:
            case ConsoleKey.A:
                if (availableMoves.GetLength(0) > 0)
                {
                    // Исправлено: левая стрелка уменьшает индекс колонки
                    currentIdx = (currentIdx - 1 + availableMoves.GetLength(0)) % availableMoves.GetLength(0);
                }
                break;
            case ConsoleKey.UpArrow:
            case ConsoleKey.W:
                if (availableMoves.GetLength(0) > 0)
                {
                    // Вверх - поиск хода вверх
                    currentIdx = FindVerticalMove(currentIdx, true);
                }
                break;
            case ConsoleKey.DownArrow:
            case ConsoleKey.S:
                if (availableMoves.GetLength(0) > 0)
                {
                    // Вниз - поиск хода вниз
                    currentIdx = FindVerticalMove(currentIdx, false);
                }
                break;
            case ConsoleKey.Enter:
                if (availableMoves.GetLength(0) > 0)
                {
                    GameController.SendMove(selectedPawnRow, selectedPawnCol,
                                          availableMoves[currentIdx, 0],
                                          availableMoves[currentIdx, 1]);
                    GameController.IsWhiteTurn = !GameController.IsWhiteTurn;
                    GameController.UpdateCoordsAfterMove(selectedPawnRow, selectedPawnCol,
                                                       availableMoves[currentIdx, 0],
                                                       availableMoves[currentIdx, 1]);
                    currentIdx = 0;

                    // Просто переходим к выбору пешки для следующего игрока
                    GameController.ChangeState(new SelectPawnState());
                }
                break;
            case ConsoleKey.Escape:
                currentIdx = 0;
                GameController.ChangeState(new SelectPawnState());
                break;
        }
    }

    private int FindVerticalMove(int startIdx, bool moveUp)
    {
        if (availableMoves.GetLength(0) == 0) return startIdx;

        int currentRow = availableMoves[startIdx, 0];
        int currentCol = availableMoves[startIdx, 1];
        int bestIdx = -1;
        int bestRowDiff = int.MaxValue;

        for (int i = 0; i < availableMoves.GetLength(0); i++)
        {
            int row = availableMoves[i, 0];
            int col = availableMoves[i, 1];

            if (col == currentCol) // Тот же столбец
            {
                int rowDiff = row - currentRow;

                if (moveUp && rowDiff < 0 && Math.Abs(rowDiff) < Math.Abs(bestRowDiff))
                {
                    bestRowDiff = rowDiff;
                    bestIdx = i;
                }
                else if (!moveUp && rowDiff > 0 && Math.Abs(rowDiff) < Math.Abs(bestRowDiff))
                {
                    bestRowDiff = rowDiff;
                    bestIdx = i;
                }
            }
        }

        return bestIdx != -1 ? bestIdx : startIdx;
    }

    private void DrawField()
    {
        GameController.DrawField(availableMoves, currentIdx, false);
    }
}

// Меню паузы
class PauseMenuState : GameState
{
    private static int pauseMenuSelection = 0;

    public override void Display()
    {
        Console.Clear();
        Console.WriteLine("=== МЕНЮ ПАУЗЫ ===");

        if (pauseMenuSelection == 0)
        {
            Console.WriteLine("-> Продолжить игру");
            Console.WriteLine("   Сохранить игру");
            Console.WriteLine("   В главное меню");
            Console.WriteLine("   Выйти из игры");
        }
        else if (pauseMenuSelection == 1)
        {
            Console.WriteLine("   Продолжить игру");
            Console.WriteLine("-> Сохранить игру");
            Console.WriteLine("   В главное меню");
            Console.WriteLine("   Выйти из игры");
        }
        else if (pauseMenuSelection == 2)
        {
            Console.WriteLine("   Продолжить игру");
            Console.WriteLine("   Сохранить игру");
            Console.WriteLine("-> В главное меню");
            Console.WriteLine("   Выйти из игры");
        }
        else if (pauseMenuSelection == 3)
        {
            Console.WriteLine("   Продолжить игру");
            Console.WriteLine("   Сохранить игру");
            Console.WriteLine("   В главное меню");
            Console.WriteLine("-> Выйти из игры");
        }

        Console.WriteLine("\nИспользуйте стрелки ↑↓ для выбора, Enter для подтверждения, Esc для отмены");
    }

    public override void HandleInput(ConsoleKey key)
    {
        switch (key)
        {
            case ConsoleKey.UpArrow:
                pauseMenuSelection = (pauseMenuSelection - 1 + 4) % 4;
                break;
            case ConsoleKey.DownArrow:
                pauseMenuSelection = (pauseMenuSelection + 1) % 4;
                break;
            case ConsoleKey.Enter:
                switch (pauseMenuSelection)
                {
                    case 0: // Продолжить игру
                        GameController.ChangeState(new SelectPawnState());
                        break;
                    case 1: // Сохранить игру
                        GameController.SaveGame();
                        Console.WriteLine("\nИгра сохранена! Нажмите любую клавишу...");
                        Console.ReadKey();
                        break;
                    case 2: // В главное меню
                        GameController.ChangeState(new MainMenuState());
                        break;
                    case 3: // Выйти из игры
                        Environment.Exit(0);
                        break;
                }
                break;
            case ConsoleKey.Escape:
                GameController.ChangeState(new SelectPawnState());
                break;
        }
    }
}

// Контроллер игры
static class GameController
{
    private static GameState currentState;

    // Игровые переменные
    public static int[,] Field { get; private set; }
    public static int[,] Coords { get; private set; }
    public static bool IsWhiteTurn { get; set; }
    public static string WhitePlayerName { get; private set; }
    public static string BlackPlayerName { get; private set; }

    public static void Initialize()
    {
        Console.OutputEncoding = Encoding.UTF8;
        ChangeState(new MainMenuState());
        Run();
    }

    public static void ChangeState(GameState newState)
    {
        currentState = newState;
        newState.Reset();
    }

    private static void Run()
    {
        ConsoleKey key;
        do
        {
            currentState.Display();
            key = Console.ReadKey(true).Key;
            currentState.HandleInput(key);
        } while (true);
    }

    public static void StartNewGame(string whiteName, string blackName, bool whiteStarts, int height, int width)
    {
        WhitePlayerName = whiteName;
        BlackPlayerName = blackName;
        IsWhiteTurn = whiteStarts;

        // Инициализация поля
        Field = new int[height, width];

        for (int i = 0; i < height; i++)
        {
            for (int j = 0; j < width; j++)
            {
                if (i < 2)
                    Field[i, j] = 1; // Белые пешки
                else if (i > height - 3)
                    Field[i, j] = 2; // Черные пешки
                else
                    Field[i, j] = 0; // Пусто
            }
        }

        // Инициализация координат
        int pawnCount = width * 4;
        Coords = new int[pawnCount, 2];

        int idx = 0;
        for (int i = 0; i < 2; i++)
            for (int j = 0; j < width; j++)
            {
                Coords[idx, 0] = i;
                Coords[idx, 1] = j;
                idx++;
            }
        for (int i = height - 2; i < height; i++)
            for (int j = 0; j < width; j++)
            {
                Coords[idx, 0] = i;
                Coords[idx, 1] = j;
                idx++;
            }
    }

    public static void SendMove(int fromRow, int fromCol, int toRow, int toCol)
    {
        int player = Field[fromRow, fromCol];
        Field[fromRow, fromCol] = 0;
        Field[toRow, toCol] = player;

        System.Threading.Thread.Sleep(500);
    }

    public static void UpdateCoordsAfterMove(int fromRow, int fromCol, int toRow, int toCol)
    {
        for (int i = 0; i < Coords.GetLength(0); i++)
        {
            if (Coords[i, 0] == fromRow && Coords[i, 1] == fromCol)
            {
                Coords[i, 0] = toRow;
                Coords[i, 1] = toCol;
                break;
            }
        }
    }

    public static void DrawField(int[,] coords, int idx, bool isPawnSelection)
    {
        const char Pawn = '♙';

        int rows = Field.GetLength(0), cols = Field.GetLength(1);
        int currentPlayer = IsWhiteTurn ? 1 : 2;

        Console.BackgroundColor = ConsoleColor.DarkGray;
        Console.Write("  ");
        for (int c = 0; c < cols; c++)
        {
            Console.BackgroundColor = ConsoleColor.DarkGray;
            Console.Write($"{c}   ");
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

        for (int r = 0; r < rows; r++)
        {
            Console.BackgroundColor = ConsoleColor.DarkGray;
            Console.Write($"{r}|");
            for (int c = 0; c < cols; c++)
            {
                bool isSelectable = false;
                bool isPawnHere = Field[r, c] != 0;

                if (isPawnSelection)
                {
                    // Только пешки текущего игрока подсвечиваются зеленым
                    for (int i = 0; i < coords.GetLength(0); i++)
                    {
                        if (coords[i, 0] == r && coords[i, 1] == c)
                        {
                            // Пешка должна принадлежать текущему игроку, чтобы быть подсвеченной
                            if (Field[r, c] == currentPlayer)
                            {
                                isSelectable = true;
                            }
                            break;
                        }
                    }
                }
                else
                {
                    // Для выбора ходов подсвечиваем все доступные ходы
                    for (int i = 0; i < coords.GetLength(0); i++)
                        if (coords[i, 0] == r && coords[i, 1] == c)
                            isSelectable = true;
                }

                bool isSelected = (coords.GetLength(0) > 0 && r == coords[idx, 0] && c == coords[idx, 1]);

                if (!isSelected && !isSelectable)
                {
                    // Обычное отображение пешек без подсветки
                    if (Field[r, c] == 1)
                    {
                        Console.ForegroundColor = ConsoleColor.White;
                        Console.Write($" {Pawn} ");
                        Console.ResetColor();
                        Console.BackgroundColor = ConsoleColor.DarkGray;
                        Console.Write("|");
                    }
                    else if (Field[r, c] == 2)
                    {
                        Console.ForegroundColor = ConsoleColor.Black;
                        Console.Write($" {Pawn} ");
                        Console.ResetColor();
                        Console.BackgroundColor = ConsoleColor.DarkGray;
                        Console.Write("|");
                    }
                    else if (Field[r, c] == 0)
                    {
                        Console.BackgroundColor = ConsoleColor.DarkGray;
                        Console.Write("   |");
                    }
                }
                else if (isSelected)
                {
                    // Выделенная клетка (текущий выбор)
                    if (Field[r, c] == 1)
                    {
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.Write($"[{Pawn}]");
                        Console.ResetColor();
                        Console.BackgroundColor = ConsoleColor.DarkGray;
                        Console.Write("|");
                    }
                    else if (Field[r, c] == 2)
                    {
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.Write($"[{Pawn}]");
                        Console.ResetColor();
                        Console.BackgroundColor = ConsoleColor.DarkGray;
                        Console.Write("|");
                    }
                    else
                    {
                        // Пустая клетка при выборе хода
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.Write("[ ]");
                        Console.ResetColor();
                        Console.BackgroundColor = ConsoleColor.DarkGray;
                        Console.Write("|");
                    }
                }
                else if (isSelectable)
                {
                    // Подсвеченные клетки (доступные для выбора)
                    if (Field[r, c] == 1)
                    {
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.Write($" {Pawn} ");
                        Console.ResetColor();
                        Console.BackgroundColor = ConsoleColor.DarkGray;
                        Console.Write("|");
                    }
                    else if (Field[r, c] == 2)
                    {
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.Write($" {Pawn} ");
                        Console.ResetColor();
                        Console.BackgroundColor = ConsoleColor.DarkGray;
                        Console.Write("|");
                    }
                    else
                    {
                        // Пустые клетки для ходов
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.Write(" • ");
                        Console.ResetColor();
                        Console.BackgroundColor = ConsoleColor.DarkGray;
                        Console.Write("|");
                    }
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

    // Заглушки для методов данных
    public static void LoadSavedGamesMenu() { }
    public static string[,] GetSavedGames() => null;
    public static string[,] GetStatisticsData() => new string[,] {
        { "Игрок 1", "5", "3", "8", "62.5" },
        { "Игрок 2", "3", "5", "8", "37.5" }
    };
    public static void LoadGame(int index) { }
    public static void SaveGame()
    {
        Console.WriteLine("Сохранение игры...");
        System.Threading.Thread.Sleep(500);
    }
}

// Главный класс программы
class Program
{
    static void Main()
    {
        GameController.Initialize();
    }
}
