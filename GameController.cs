using System.Text;

namespace Breakthrough;

// Базовый класс для состояний игры
abstract class GameState
{
    // Храним высоту последнего отрисованного кадра
    protected static int previousFrameHeight = 0;
    
    // Метод для очистки остатков экрана
    protected void CleanupArtifacts()
    {
        int currentTop = Console.CursorTop; // Где мы закончили рисовать сейчас

        // Если текущий кадр короче предыдущего, стираем лишние строки снизу
        if (currentTop < previousFrameHeight)
        {
            // Строка пробелов во всю ширину окна
            string blankLine = new string(' ', Console.WindowWidth);
            
            for (int i = currentTop; i < previousFrameHeight; i++)
            {
                Console.SetCursorPosition(0, i);
                Console.Write(blankLine);
            }
        }
        
        // Запоминаем текущую высоту для следующего кадра
        previousFrameHeight = currentTop;
    }
    
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

        Console.Write(mainMenuSelection == 0 ? "→ " : "  ");
        hasSavedGames = CheckSavedGames();
        
        if (hasSavedGames)
            Console.WriteLine("1. Продолжить игру");
        else
        {
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.WriteLine("1. Продолжить игру (нет сохранений)");
            Console.ResetColor();
        }

        Console.Write(mainMenuSelection == 1 ? "→ " : "  ");
        Console.WriteLine("2. Новая игра");

        Console.Write(mainMenuSelection == 2 ? "→ " : "  ");
        Console.WriteLine("3. Статистика");

        Console.Write(mainMenuSelection == 3 ? "→ " : "  ");
        Console.WriteLine("4. Закрыть игру");

        Console.WriteLine("\nИспользуйте стрелки ↑↓ для выбора, Enter для подтверждения, Esc для выхода");
        CleanupArtifacts();
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
                        if (hasSavedGames) GameController.ChangeState(new SavedGamesState());
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
        var saves = SaveManager.GetAllSaves();
        return saves.Length > 0;
    }
}

// Меню новой игры
class NewGameMenuState : GameState
{
    private static int newGameMenuSelection = 0;
    private static string whitePlayerName = "Игрок 1";
    private static string blackPlayerName = "Игрок 2";
    private static int firstMoveMode = 0;
    private static int boardHeight = 8;
    private static int boardWidth = 8;

    public override void Display()
    {
        Console.Clear();
        Console.WriteLine("=== НАСТРОЙКИ НОВОЙ ИГРЫ ===\n");

        Console.Write(newGameMenuSelection == 0 ? "→ " : "  ");
        Console.WriteLine($"Белые: {whitePlayerName}");

        Console.Write(newGameMenuSelection == 1 ? "→ " : "  ");
        Console.WriteLine($"Чёрные: {blackPlayerName}");

        Console.Write(newGameMenuSelection == 2 ? "→ " : "  ");
        string firstMoveText = firstMoveMode switch
        {
            0 => "белые",
            1 => "чёрные",
            _ => "случайно",
        };
        Console.WriteLine($"Первый ход: {firstMoveText}");

        Console.Write(newGameMenuSelection == 3 ? "→ " : "  ");
        Console.WriteLine($"Высота доски: {boardHeight}");

        Console.Write(newGameMenuSelection == 4 ? "→ " : "  ");
        Console.WriteLine($"Ширина доски: {boardWidth}");

        Console.WriteLine("");
        bool canStart = !string.IsNullOrWhiteSpace(whitePlayerName) && !string.IsNullOrWhiteSpace(blackPlayerName);

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
        CleanupArtifacts();
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
                        int firstMove;
                        if (firstMoveMode > 1)
                        {
                            var rnd = new Random();
                            firstMove = rnd.Next(0, 2); // 0 или 1
                        }
                        else
                        {
                            firstMove = firstMoveMode; // 0 или 1
                        }

                        GameController.StartNewGame(
                            whitePlayerName,
                            blackPlayerName,
                            firstMove,
                            boardHeight,
                            boardWidth);

                        GameController.ChangeState(new SelectPawnState());
                    }
                }
                else if (newGameMenuSelection == 0) // Имя белых
                {
                    Console.Write("\nВведите имя белых игроков: ");
                    string? input = Console.ReadLine();
                    if (!string.IsNullOrWhiteSpace(input)) whitePlayerName = input;
                }
                else if (newGameMenuSelection == 1) // Имя черных
                {
                    Console.Write("\nВведите имя чёрных игроков: ");
                    string? input = Console.ReadLine();
                    if (!string.IsNullOrWhiteSpace(input)) blackPlayerName = input;
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
                if (increase)
                    firstMoveMode = (firstMoveMode + 1) % 3;
                else
                    firstMoveMode = (firstMoveMode + 2) % 3; // эквивалентно -1 по модулю 3
                break;
            case 3: // Высота
                if (increase && boardHeight < 16) boardHeight++;
                else if (!increase && boardHeight > 6) boardHeight--;
                break;
            case 4: // Ширина
                if (increase && boardWidth < 16) boardWidth++;
                else if (!increase && boardWidth > 4) boardWidth--;
                break;
        }
    }

    public override void Reset()
    {
        newGameMenuSelection = 0;
    }
}

// Сохраненные игры
class SavedGamesState : GameState
{
    private SaveManager[]? savedGames;
    private int selectedIndex = 0;

    // Загружаем сохранения один раз при входе в это состояние
    public override void Reset()
    {
        savedGames = SaveManager.GetAllSaves();
        selectedIndex = 0;
    }

    public override void Display()
    {
        Console.Clear();
        Console.WriteLine("=== ВЫБОР СОХРАНЕННОЙ ИГРЫ ===\n");

        if (savedGames == null || savedGames.Length == 0)
        {
            Console.WriteLine("Нет сохраненных игр");
            Console.WriteLine("\nНажмите Enter для возврата...");
            return;
        }

        // Отображаем список с курсором
        for (int i = 0; i < savedGames.Length; i++)
        {
            var sm = savedGames[i];
            string p1 = sm.Players != null && sm.Players.Length > 0 ? sm.Players[0] : "?";
            string p2 = sm.Players != null && sm.Players.Length > 1 ? sm.Players[1] : "?";
            
            // Подсветка выбранного пункта
            if (i == selectedIndex)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.Write("> ");
            }
            else
            {
                Console.Write("  ");
            }
            
            Console.WriteLine($"{i + 1}. {sm.UniqueCode} - {p1} vs {p2} (Ход: {sm.MoveCount})");
            Console.ResetColor();
        }

        // Пункт "Назад"
        if (selectedIndex == savedGames.Length)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("> Назад");
        }
        else
        {
            Console.WriteLine("  Назад");
        }
        Console.ResetColor();

        Console.WriteLine("\nИспользуйте стрелки для выбора, Enter для загрузки.");
        CleanupArtifacts();
    }

    public override void HandleInput(ConsoleKey key)
    {
        // Если сохранений нет, любой Enter/Esc возвращает в меню
        if (savedGames == null || savedGames.Length == 0)
        {
            if (key == ConsoleKey.Enter || key == ConsoleKey.Escape)
            {
                GameController.ChangeState(new MainMenuState());
            }
            return;
        }

        int totalOptions = savedGames.Length + 1; // Сохранения + кнопка Назад

        switch (key)
        {
            case ConsoleKey.UpArrow:
                selectedIndex = (selectedIndex - 1 + totalOptions) % totalOptions;
                break;
            case ConsoleKey.DownArrow:
                selectedIndex = (selectedIndex + 1) % totalOptions;
                break;
            case ConsoleKey.Enter:
                if (selectedIndex == savedGames.Length) // Выбрано "Назад"
                {
                    GameController.ChangeState(new MainMenuState());
                }
                else // Выбрано сохранение
                {
                    GameController.LoadGame(savedGames[selectedIndex]);
                    GameController.ChangeState(new SelectPawnState());
                }
                break;
            case ConsoleKey.Escape:
                GameController.ChangeState(new MainMenuState());
                break;
        }
    }
}

// Статистика
class StatisticsState : GameState
{
    private string[]? topScores;

    public override void Display()
    {
        Console.Clear();
        Console.WriteLine("=== СТАТИСТИКА ===\n");

        topScores = SaveManager.GetTopScores();
        if (topScores == null || topScores.Length == 0)
        {
            Console.WriteLine("Статистика недоступна");
        }
        else
        {
            for (int i = 0; i < topScores.Length; i += 2)
            {
                Console.WriteLine($"{topScores[i]}: {topScores[i+1]}");
            }
        }
        Console.WriteLine("\nНажмите Esc для возврата в меню");
        CleanupArtifacts();
    }

    public override void HandleInput(ConsoleKey key)
    {
        if (key == ConsoleKey.Escape) GameController.ChangeState(new MainMenuState());
    }
}

// Базовый класс для игровых состояний (геймплей)
abstract class GameplayState : GameState
{
    protected static int[,] availableMoves = new int[0, 2];
    protected static int selectedPawnRow = -1, selectedPawnCol = -1;
    protected static int currentIdx = 0;

    // Навигация по пешкам (UI логика, зависит от координат)
    protected int NextInRowRight(int[,] coords, int idx)
    {
        int n = coords.GetLength(0);
        int row = coords[idx, 0], col = coords[idx, 1];
        int firstIdx = -1, nextIdx = -1;

        for (int i = 0; i < n; i++)
        {
            if (coords[i, 0] == row)
            {
                if (firstIdx == -1 || coords[i, 1] < coords[firstIdx, 1]) firstIdx = i;
                if (coords[i, 1] > col)
                {
                    if (nextIdx == -1 || coords[i, 1] < coords[nextIdx, 1]) nextIdx = i;
                }
            }
        }
        return nextIdx != -1 ? nextIdx : firstIdx;
    }

    protected int NextInRowLeft(int[,] coords, int idx)
    {
        int n = coords.GetLength(0);
        int row = coords[idx, 0], col = coords[idx, 1];
        int lastIdx = -1, prevIdx = -1;

        for (int i = 0; i < n; i++)
        {
            if (coords[i, 0] == row)
            {
                if (lastIdx == -1 || coords[i, 1] > coords[lastIdx, 1]) lastIdx = i;
                if (coords[i, 1] < col)
                {
                    if (prevIdx == -1 || coords[i, 1] > coords[prevIdx, 1]) prevIdx = i;
                }
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
            if (row > curRow && row < minRow) { minRow = row; targetIdx = i; }
        }
        if (targetIdx != -1) return targetIdx;
        
        // Wrap around to top
        minRow = int.MaxValue;
        for (int i = 0; i < n; i++)
        {
            if (coords[i, 0] < minRow) { minRow = coords[i, 0]; targetIdx = i; }
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
            if (row < curRow && row > maxRow) { maxRow = row; targetIdx = i; }
        }
        if (targetIdx != -1) return targetIdx;

        // Wrap around to bottom
        maxRow = int.MinValue;
        for (int i = 0; i < n; i++)
        {
            if (coords[i, 0] > maxRow) { maxRow = coords[i, 0]; targetIdx = i; }
        }
        return targetIdx;
    }
}

// Выбор пешки
class SelectPawnState : GameplayState
{
    // Поле для хранения координат только доступных для выбора ("зелёных") пешек
    private int[,] selectableCoords;

    public override void Reset()
    {
        // Сбрасываем индекс курсора
        currentIdx = 0;

        // Используем List для динамического сбора подходящих пешек
        // Примечание: убедитесь, что в файле есть 'using System.Collections.Generic;' 
        // или используйте System.Collections.Generic.List
        System.Collections.Generic.List<int[]> greenPawns = new System.Collections.Generic.List<int[]>();
        
        int totalPawns = GameController.Coords.GetLength(0);

        for (int i = 0; i < totalPawns; i++)
        {
            int r = GameController.Coords[i, 0];
            int c = GameController.Coords[i, 1];
            int pawn = GameController.Field[r, c];

            // 1. Проверяем, принадлежит ли пешка текущему игроку
            bool isMyPawn = (GameController.IsWhiteTurn && pawn == Objects.WhitePawn) ||
                            (!GameController.IsWhiteTurn && pawn == Objects.BlackPawn);

            if (isMyPawn)
            {
                // 2. Проверяем, есть ли у пешки доступные ходы
                var moves = Core.GetAvailableMovesForPawn(GameController.saveManager, r, c);
                
                // Если ходы есть, значит пешка "зелёная" — добавляем её в список выбора
                if (moves.GetLength(0) > 0)
                {
                    greenPawns.Add(new int[] { r, c });
                }
            }
        }

        // Преобразуем список обратно в массив int[,] для совместимости с методами навигации
        selectableCoords = new int[greenPawns.Count, 2];
        for (int i = 0; i < greenPawns.Count; i++)
        {
            selectableCoords[i, 0] = greenPawns[i][0];
            selectableCoords[i, 1] = greenPawns[i][1];
        }
    }

    public override void Display()
    {
        Console.Clear();
        Console.WriteLine("--------------------------");
        Console.WriteLine($"Игрок 1: {GameController.WhitePlayerName}");
        Console.WriteLine($"Игрок 2: {GameController.BlackPlayerName}");
        string currentPlayer = GameController.IsWhiteTurn ? GameController.WhitePlayerName : GameController.BlackPlayerName;
        Console.WriteLine($"\nХодит: {currentPlayer}");
        Console.WriteLine("--------------------------");
        Console.WriteLine("Выберите пешку (WASD/Стрелки), Enter - подтвердить");
        Console.WriteLine("Esc - Пауза");

        DrawField();
        CleanupArtifacts();
    }

    public override void HandleInput(ConsoleKey key)
    {
        // Если "зелёных" пешек нет (тупик), управление блокируется или можно добавить логику завершения
        if (selectableCoords == null || selectableCoords.GetLength(0) == 0) return;

        switch (key)
        {
            // Навигация теперь использует selectableCoords вместо GameController.Coords
            case ConsoleKey.RightArrow:
            case ConsoleKey.D:
                currentIdx = NextInRowRight(selectableCoords, currentIdx);
                break;
            case ConsoleKey.LeftArrow:
            case ConsoleKey.A:
                currentIdx = NextInRowLeft(selectableCoords, currentIdx);
                break;
            case ConsoleKey.UpArrow:
            case ConsoleKey.W:
                currentIdx = NextRowUp(selectableCoords, currentIdx);
                break;
            case ConsoleKey.DownArrow:
            case ConsoleKey.S:
                currentIdx = NextRowDown(selectableCoords, currentIdx);
                break;

            case ConsoleKey.Enter:
                // Мы гарантированно выбираем валидную пешку, так как других в selectableCoords нет
                selectedPawnRow = selectableCoords[currentIdx, 0];
                selectedPawnCol = selectableCoords[currentIdx, 1];

                // Загружаем ходы для выбранной пешки (они точно есть)
                availableMoves = Core.GetAvailableMovesForPawn(GameController.saveManager, selectedPawnRow, selectedPawnCol);
                
                currentIdx = 0; // Сброс индекса для следующего состояния (выбора хода)
                GameController.ChangeState(new SelectMoveState());
                break;

            case ConsoleKey.Escape:
                GameController.ChangeState(new PauseMenuState());
                break;
        }
    }

    private void DrawField()
    {
        // Передаем в метод отрисовки только наши отфильтрованные координаты.
        // Курсор будет перескакивать только по "зелёным" пешкам.
        GameController.DrawField(selectableCoords, currentIdx, true);
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
        
        string currentPlayer = GameController.IsWhiteTurn 
            ? $"Сейчас ходит: {GameController.WhitePlayerName} (белые)" 
            : $"Сейчас ходит: {GameController.BlackPlayerName} (чёрные)";

        Console.WriteLine(currentPlayer);
        Console.WriteLine("\n=== ВЫБОР ХОДА ===");
        Console.WriteLine("Выберите куда походить пешкой");
        Console.WriteLine("Используйте стрелки для выбора хода, Enter для подтверждения");
        Console.WriteLine("Нажмите Esc для возврата к выбору пешки\n");
        DrawField();
        CleanupArtifacts();
    }

    public override void HandleInput(ConsoleKey key)
    {
        switch (key)
        {
            case ConsoleKey.RightArrow: case ConsoleKey.D:
                if (availableMoves.GetLength(0) > 0)
                    currentIdx = (currentIdx + 1) % availableMoves.GetLength(0);
                break;
            case ConsoleKey.LeftArrow: case ConsoleKey.A:
                if (availableMoves.GetLength(0) > 0)
                    currentIdx = (currentIdx - 1 + availableMoves.GetLength(0)) % availableMoves.GetLength(0);
                break;
            case ConsoleKey.UpArrow: case ConsoleKey.W:
                if (availableMoves.GetLength(0) > 0)
                    currentIdx = FindVerticalMove(currentIdx, true);
                break;
            case ConsoleKey.DownArrow: case ConsoleKey.S:
                if (availableMoves.GetLength(0) > 0)
                    currentIdx = FindVerticalMove(currentIdx, false);
                break;
            case ConsoleKey.Enter:
                if (availableMoves.GetLength(0) > 0)
                {
                    GameController.SendMove(selectedPawnRow, selectedPawnCol,
                        availableMoves[currentIdx, 0], availableMoves[currentIdx, 1]);
                    
                    // После успешного хода состояние сбрасывается внутри SendMove, если игра не окончена
                    currentIdx = 0;
                    if (GameController.CurrentStateType is SelectMoveState) // Если состояние не сменилось (игра не кончилась)
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
        int currentCol = availableMoves[startIdx, 1];
        int bestIdx = -1;
        int bestRowDiff = int.MaxValue;

        for (int i = 0; i < availableMoves.GetLength(0); i++)
        {
            int row = availableMoves[i, 0];
            int col = availableMoves[i, 1];
            if (col == currentCol)
            {
                int rowDiff = row - availableMoves[startIdx, 0];
                if (moveUp && rowDiff < 0 && Math.Abs(rowDiff) < Math.Abs(bestRowDiff)) { bestRowDiff = rowDiff; bestIdx = i; }
                else if (!moveUp && rowDiff > 0 && Math.Abs(rowDiff) < Math.Abs(bestRowDiff)) { bestRowDiff = rowDiff; bestIdx = i; }
            }
        }
        return bestIdx != -1 ? bestIdx : startIdx;
    }

    private void DrawField()
    {
        GameController.DrawField(availableMoves, currentIdx, false, selectedPawnRow, selectedPawnCol);
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
        string[] items = { "Продолжить игру", "Сохранить игру", "В главное меню", "Выйти из игры" };
        
        for (int i = 0; i < items.Length; i++)
        {
            Console.WriteLine(i == pauseMenuSelection ? $"-> {items[i]}" : $"   {items[i]}");
        }
        Console.WriteLine("\nИспользуйте стрелки ↑↓ для выбора, Enter для подтверждения, Esc для отмены");
        CleanupArtifacts();
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
                    case 0: GameController.ChangeState(new SelectPawnState()); break;
                    case 1:
                        // В текущей реализации ход сохраняется автоматически после каждого действия (SendMove).
                        // Здесь можно просто вывести сообщение, т.к. состояние уже сохранено.
                        Console.WriteLine("\nИгра сохраняется автоматически после каждого хода. Нажмите любую клавишу...");
                        Console.ReadKey();
                        break;
                    case 2: GameController.ChangeState(new MainMenuState()); break;
                    case 3: Environment.Exit(0); break;
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
    public static SaveManager saveManager;

    // Свойства для доступа к данным игры
    public static int[,] Field { get; private set; }
    public static int[,] Coords { get; private set; }
    public static bool IsWhiteTurn => Core.IsWhiteTurn(saveManager);
    public static string WhitePlayerName { get; private set; }
    public static string BlackPlayerName { get; private set; }
    public static GameState CurrentStateType => currentState;

    public static void Initialize()
    {
        Console.OutputEncoding = Encoding.UTF8;
        //Скрывает мигающий курсор
        Console.CursorVisible = false;
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

    // Загрузка игры
    public static void LoadGame(SaveManager loadedManager)
    {
        saveManager = loadedManager;
        UpdateLocalState();

        if (saveManager.Players != null && saveManager.Players.Length >= 2)
        {
            WhitePlayerName = saveManager.Players[0];
            BlackPlayerName = saveManager.Players[1];
        }
    }

    // Новая игра
    public static void StartNewGame(string whiteName, string blackName, int firstMove, int height, int width)
    {
        WhitePlayerName = whiteName;
        BlackPlayerName = blackName;

        saveManager = new SaveManager();
        int[,] matrix = Core.CreateMatrix(height, width);

        saveManager.Start(matrix, new string[] { whiteName, blackName }, firstMove);
        UpdateLocalState();
    }

    // Выполнение хода
    public static void SendMove(int fromRow, int fromCol, int toRow, int toCol)
    {
        // 1. Делегируем логику Core
        var result = Core.MakeMove(saveManager, fromRow, fromCol, toRow, toCol);

        // 2. Обновляем локальные ссылки на поле (важно для отрисовки)
        UpdateLocalState();

        if (result.IsGameOver)
        {
            Console.Clear();
            // Рисуем финальное состояние доски (оно теперь корректно обновлено в SaveManager.Matrix)
            // Для красоты создадим заглушку координат (0,0) чтобы метод не упал, или передадим null если DrawField умеет
            DrawField(new int[0, 0], 0, false);

            Console.WriteLine($"\nИГРА ОКОНЧЕНА! {result.Message}");
            Console.WriteLine("Нажмите любую клавишу для выхода в меню...");
            Console.ReadKey();
            ChangeState(new MainMenuState());
        }
    }

    // Обновление локальных данных (кэшей) из SaveManager
    private static void UpdateLocalState()
    {
        if (saveManager?.Matrix != null)
        {
            Field = saveManager.Matrix;
            Coords = Core.GetPawnCoordinates(Field);
        }
    }

    // Отрисовка поля
    public static void DrawField(int[,] activeCoords, int activeIdx, bool isPawnSelection, int selectedRow = -1,
        int selectedCol = -1)
    {
        Console.BackgroundColor = ConsoleColor.Black;
        Console.WriteLine(
            $"Ход: {saveManager.MoveCount + 1} | Ходит: {(IsWhiteTurn ? WhitePlayerName : BlackPlayerName)}".PadRight(
                Console.WindowWidth));
        Console.WriteLine();
        int rows = Field.GetLength(0);
        int cols = Field.GetLength(1);

        // Отрисовка букв столбцов (a, b, c...)
        Console.Write("    "); // Отступ для номеров строк
        for (int c = 0; c < cols; c++)
        {
            Console.Write($" {(char)('a' + c)}  ");
        }

        Console.WriteLine();

        // Горизонтальный разделитель
        Action drawLine = () =>
        {
            Console.ForegroundColor = ConsoleColor.White;
            Console.Write("   +");
            for (int c = 0; c < cols; c++) Console.Write("---+");
            Console.WriteLine("+");
        };

        drawLine();

        for (int r = 0; r < rows; r++)
        {
            Console.ForegroundColor = ConsoleColor.White;
            // Отрисовка номера строки (1, 2, 3...) с выравниванием
            Console.Write($" {r + 1,-2}|");

            for (int c = 0; c < cols; c++)
            {
                // Проверяем, является ли текущая клетка курсором
                bool isCursor = false;
                if (activeCoords != null && activeCoords.GetLength(0) > 0)
                {
                    if (activeIdx < activeCoords.GetLength(0) &&
                        activeCoords[activeIdx, 0] == r &&
                        activeCoords[activeIdx, 1] == c)
                        isCursor = true;
                }

                int cell = Field[r, c];
                bool canMove = false;

                // Логика подсветки доступных для хода пешек (зеленая подсветка)
                // Проверяем только если это стадия выбора пешки (SelectPawnState)
                if (isPawnSelection && cell != Objects.Space)
                {
                    bool isMyPawn = (IsWhiteTurn && cell == Objects.WhitePawn) ||
                                    (!IsWhiteTurn && cell == Objects.BlackPawn);

                    if (isMyPawn)
                    {
                        // Обращаемся к Core для проверки правил
                        var moves = Core.GetAvailableMovesForPawn(saveManager, r, c);
                        if (moves.GetLength(0) > 0)
                        {
                            canMove = true;
                        }
                    }
                }

                string pawnSymbol = " " + Objects.Pawn + " ";

                // Установка цвета и символов
                if (isCursor)
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    pawnSymbol = cell == Objects.Space ? "[ ]" : "[" + Objects.Pawn + "]";
                }
                else if (canMove || (r == selectedRow && c == selectedCol))
                {
                    // Пешки, которыми можно походить ИЛИ выбранная пешка - зеленые
                    Console.ForegroundColor = ConsoleColor.Green;
                }
                else if (canMove)
                {
                    // Пешки, которыми можно походить - зеленые
                    Console.ForegroundColor = ConsoleColor.Green;
                }
                else if (cell == Objects.WhitePawn)
                {
                    Console.ForegroundColor = ConsoleColor.White;
                }
                else if (cell == Objects.BlackPawn)
                {
                    Console.ForegroundColor = ConsoleColor.DarkGray;
                }
                else
                {
                    pawnSymbol = "   "; // Пустая клетка без курсора
                }

                Console.Write(pawnSymbol);

                // Возвращаем белый цвет для разделителей
                Console.ForegroundColor = ConsoleColor.White;
                Console.Write("|");
            }

            Console.WriteLine();
            drawLine();
        }

        Console.ResetColor();
    }

}

