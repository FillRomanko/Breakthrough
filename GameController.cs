using System.Text;

namespace Breakthrough;

/// Статический класс для отрисовки без мерцания с использованием двойной буферизации.
/// Все операции записи сначала происходят в фоновом буфере (`backBuffer`).
/// Метод Present() сравнивает фоновый буфер с текущим (`frontBuffer`) и обновляет на экране только изменившиеся ячейки.
internal static class Renderer
{
    private struct ConsoleCell : IEquatable<ConsoleCell>
    {
        public char Character;
        public ConsoleColor ForegroundColor;

        // Для сравнения структур
        public bool Equals(ConsoleCell other)
        {
            return Character == other.Character && ForegroundColor == other.ForegroundColor;
        }

        public override bool Equals(object? obj)
        {
            return obj is ConsoleCell other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Character, ForegroundColor);
        }

        public static bool operator == (ConsoleCell left, ConsoleCell right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(ConsoleCell left, ConsoleCell right) => !left.Equals(right);
    }

    private static int _width;
    private static int _height;
    private static ConsoleCell[,] _frontBuffer =  new ConsoleCell[0, 0]; // Что сейчас на экране
    private static ConsoleCell[,] _backBuffer = new ConsoleCell[0, 0];  // Что мы хотим нарисовать
    
    public static void Initialize()
    {
        Console.OutputEncoding = Encoding.UTF8;
        Console.CursorVisible = false;
        _width = Console.WindowWidth;
        _height = Console.WindowHeight;

        _frontBuffer = new ConsoleCell[_width, _height];
        _backBuffer = new ConsoleCell[_width, _height];

        Clear(true); // Очищаем оба буфера
    }
    
    /// <summary>
    /// Очищает фоновый буфер для подготовки нового кадра.
    /// </summary>
    public static void Clear(bool clearBoth = false)
    {
        ConsoleCell empty = new() { Character = ' ', ForegroundColor = ConsoleColor.Gray };
        for (int y = 0; y < _height; y++)
        for (int x = 0; x < _width; x++)
        {
            _backBuffer[x, y] = empty;
            if (clearBoth) _frontBuffer[x, y] = empty;
        }
    }

    /// <summary>
    /// Записывает текст в фоновый буфер.
    /// </summary>
    public static void Write(int x, int y, string text, ConsoleColor color = ConsoleColor.Gray)
    {
        if (y >= _height) return;
        for (int i = 0; i < text.Length; i++)
        {
            int realX = x + i;
            if (realX >= _width) break;
            _backBuffer[realX, y] = new ConsoleCell { Character = text[i], ForegroundColor = color };
        }
    }

    /// <summary>
    /// Отображает фоновый буфер на экране, обновляя только изменившиеся ячейки.
    /// </summary>
    public static void Present()
    {
        for (int y = 0; y < _height; y++)
        {
            for (int x = 0; x < _width; x++)
            {
                if (_frontBuffer[x, y] != _backBuffer[x, y])
                {
                    _frontBuffer[x, y] = _backBuffer[x, y];
                    Console.SetCursorPosition(x, y);
                    Console.ForegroundColor = _backBuffer[x, y].ForegroundColor;
                    Console.Write(_backBuffer[x, y].Character);
                }
            }
        }
        Console.SetCursorPosition(0,0); // Возвращаем курсор, чтобы избежать артефактов при изменении размера окна
    }

    public static void SetCursorVisibility(bool visible) => Console.CursorVisible = visible;
    
    public static void PositionCursor(int x, int y) => Console.SetCursorPosition(x, y);
}


// Базовый класс для состояний игры
abstract class GameState
{
    // Метод CleanupArtifacts больше не нужен, так как двойная буферизация решает эту проблему
    public abstract void Display();
    public abstract void HandleInput(ConsoleKey key);
    public virtual void Reset() { }
}

// Главное меню
class MainMenuState : GameState
{
    private static int _mainMenuSelection;
    private bool _hasSavedGames;

    private string MenuItem(int index, string text) => $"{(_mainMenuSelection == index ? "→ " : "  ")}{text}";
    public override void Display()
    {
        int y = 0;
        Renderer.Write(0, y++, "=== ГЛАВНОЕ МЕНЮ ===");
        y++;

        _hasSavedGames = CheckSavedGames();
        ConsoleColor continueColor = _hasSavedGames ? ConsoleColor.Gray : ConsoleColor.DarkGray;
        string continueText = "1. Продолжить игру" + (_hasSavedGames ? "" : " (нет сохранений)");
        
        Renderer.Write(0, y++, (MenuItem(0,"")) + continueText, continueColor);
        Renderer.Write(0, y++, (MenuItem(1 ,"2. Новая игра")));
        Renderer.Write(0, y++, (MenuItem(2 ,"3. Статистика")));
        Renderer.Write(0, y++, (MenuItem(3 , "4. Закрыть игру")));
        y++;
        Renderer.Write(0, y, "Используйте стрелки ↑↓ для выбора, Enter для подтверждения, Esc для выхода");
    }

    public override void HandleInput(ConsoleKey key)
    {
        switch (key)
        {
            case ConsoleKey.UpArrow:
                _mainMenuSelection = (_mainMenuSelection - 1 + 4) % 4;
                break;
            case ConsoleKey.DownArrow:
                _mainMenuSelection = (_mainMenuSelection + 1) % 4;
                break;
            case ConsoleKey.Enter:
                switch (_mainMenuSelection)
                {
                    case 0: if (_hasSavedGames) GameController.ChangeState(new SavedGamesState()); break;
                    case 1: GameController.ChangeState(new NewGameMenuState()); break;
                    case 2: GameController.ChangeState(new StatisticsState()); break;
                    case 3: Environment.Exit(0); break;
                }
                break;
            case ConsoleKey.Escape:
                Environment.Exit(0);
                break;
        }
    }

    private bool CheckSavedGames() => (SaveManager.GetAllSaves().Length) > 0;
}

// Меню новой игры
class NewGameMenuState : GameState
{
    private static int _newGameMenuSelection;
    private static string _whitePlayerName = "Игрок 1";
    private static string _blackPlayerName = "Игрок 2";
    private static int _firstMoveMode;
    private static int _boardHeight = 8;
    private static int _boardWidth = 8;

    public override void Display()
    {
        int y = 0;
        Renderer.Write(0, y++, "=== НАСТРОЙКИ НОВОЙ ИГРЫ ===");
        y++;

        Renderer.Write(0, y++, (_newGameMenuSelection == 0 ? "→ " : "  ") + $"Белые: {_whitePlayerName}");
        Renderer.Write(0, y++, (_newGameMenuSelection == 1 ? "→ " : "  ") + $"Чёрные: {_blackPlayerName}");

        string firstMoveText = _firstMoveMode switch { 0 => "белые", 1 => "чёрные", _ => "случайно" };
        Renderer.Write(0, y++, (_newGameMenuSelection == 2 ? "→ " : "  ") + $"Первый ход: {firstMoveText}");
        
        Renderer.Write(0, y++, (_newGameMenuSelection == 3 ? "→ " : "  ") + $"Высота доски: {_boardHeight}");
        Renderer.Write(0, y++, (_newGameMenuSelection == 4 ? "→ " : "  ") + $"Ширина доски: {_boardWidth}");
        y++;

        bool canStart = !string.IsNullOrWhiteSpace(_whitePlayerName) && !string.IsNullOrWhiteSpace(_blackPlayerName);
        ConsoleColor startColor = canStart ? ConsoleColor.White : ConsoleColor.DarkGray;
        string startText = "НАЧАТЬ ИГРУ" + (canStart ? "" : " (укажите имена игроков)");
        Renderer.Write(0, y++, (_newGameMenuSelection == 5 ? "→ " : "  ") + startText, startColor);
        
        y++;
        Renderer.Write(0, y, "Используйте стрелки ↑↓ для выбора, ←→ для изменения, Enter для редактирования, Esc для отмены");
    }

    public override void HandleInput(ConsoleKey key)
    {
        switch (key)
        {
            case ConsoleKey.UpArrow: _newGameMenuSelection = (_newGameMenuSelection - 1 + 6) % 6; break;
            case ConsoleKey.DownArrow: _newGameMenuSelection = (_newGameMenuSelection + 1) % 6; break;
            case ConsoleKey.Enter: 
                if (_newGameMenuSelection is 0 or 1) HandleNameInput(); 
                else if (_newGameMenuSelection == 5)
                {
                    if (!string.IsNullOrWhiteSpace(_whitePlayerName) && !string.IsNullOrWhiteSpace(_blackPlayerName))
                    {
                        GameController.StartNewGame(_whitePlayerName, _blackPlayerName, _firstMoveMode, _boardHeight, _boardWidth);
                        GameController.ChangeState(new SelectPawnState());
                    }
                }
                break;
            case ConsoleKey.RightArrow: HandleNewGameChange(true); break;
            case ConsoleKey.LeftArrow: HandleNewGameChange(false); break;
            case ConsoleKey.Escape: GameController.ChangeState(new MainMenuState()); break;
        }
    }
    
    private void HandleNameInput()
    {
        // 1. Отобразить текущий экран
        Renderer.Present();
        
        // 2. Показать курсор и переместить его в нужную позицию
        int inputLine = _newGameMenuSelection == 0 ? 10 : 11;
        Renderer.PositionCursor(0, inputLine);
        Renderer.SetCursorVisibility(true);

        // 3. Получить ввод от пользователя
        Console.Write(_newGameMenuSelection == 0 ? "\nВведите имя игрока выступающего за белых: " : "\nВведите имя игрока выступающего за чёрных: ");
        string? input = Console.ReadLine();
        if (!string.IsNullOrWhiteSpace(input))
        {
            if (_newGameMenuSelection == 0) _whitePlayerName = input;
            else _blackPlayerName = input;
        }

        // 4. Скрыть курсор и перерисовать экран в следующем цикле
        Renderer.SetCursorVisibility(false);
    }

    private void HandleNewGameChange(bool increase)
    {
        switch (_newGameMenuSelection)
        {
            case 2: _firstMoveMode = increase ? (_firstMoveMode + 1) % 3 : (_firstMoveMode + 2) % 3; break;
            case 3: _boardHeight = Math.Clamp(_boardHeight + (increase ? 1 : -1), 6, 16); break;
            case 4: _boardWidth = Math.Clamp(_boardWidth + (increase ? 1 : -1), 4, 16); break;
        }
    }
    public override void Reset() => _newGameMenuSelection = 0;
}

// Сохраненные игры
class SavedGamesState : GameState
{
    private SaveManager[]? _savedGames;
    private int _selectedIndex;

    public override void Reset()
    {
        _savedGames = SaveManager.GetAllSaves();
        _selectedIndex = 0;
    }

    public override void Display()
    {
        int y = 0;
        Renderer.Write(0, y++, "=== ВЫБОР СОХРАНЕННОЙ ИГРЫ ===");
        y++;

        if (_savedGames is null || _savedGames.Length == 0)
        {
            Renderer.Write(0, y++, "Нет сохраненных игр");
            y++;
            Renderer.Write(0, y, "Нажмите Enter для возврата...");
            return;
        }

        for (int i = 0; i < _savedGames.Length; i++)
        {
            var sm = _savedGames[i];
            string p1 = sm.Players?[0] ?? "?";
            string p2 = sm.Players?[1] ?? "?";
            string line = $"{i + 1}. {sm.UniqueCode} - {p1} vs {p2} (Ход: {sm.MoveCount})";
            ConsoleColor color = i == _selectedIndex ? ConsoleColor.Green : ConsoleColor.Gray;
            Renderer.Write(0, y++, (i == _selectedIndex ? "> " : "  ") + line, color);
        }

        ConsoleColor backColor = _selectedIndex == _savedGames.Length ? ConsoleColor.Green : ConsoleColor.Gray;
        Renderer.Write(0, y++, (_selectedIndex == _savedGames.Length ? "> " : "  ") + "Назад", backColor);
        
        y++;
        Renderer.Write(0, y, "Используйте стрелки для выбора, Enter для загрузки.");
    }
    
    public override void HandleInput(ConsoleKey key)
    {
        if (_savedGames is null || _savedGames.Length == 0)
        {
            if (key is ConsoleKey.Enter or ConsoleKey.Escape) GameController.ChangeState(new MainMenuState());
            return;
        }

        int totalOptions = _savedGames.Length + 1;
        switch (key)
        {
            case ConsoleKey.UpArrow: _selectedIndex = (_selectedIndex - 1 + totalOptions) % totalOptions; break;
            case ConsoleKey.DownArrow: _selectedIndex = (_selectedIndex + 1) % totalOptions; break;
            case ConsoleKey.Enter:
                if (_selectedIndex == _savedGames.Length) GameController.ChangeState(new MainMenuState());
                else
                {
                    GameController.LoadGame(_savedGames[_selectedIndex]);
                    GameController.ChangeState(new SelectPawnState());
                }
                break;
            case ConsoleKey.Escape: GameController.ChangeState(new MainMenuState()); break;
        }
    }
}

// Статистика
class StatisticsState : GameState
{
    private string[]? _topScores;

    public override void Display()
    {
        int y = 0;
        Renderer.Write(0, y++, "=== СТАТИСТИКА ===");
        y++;

        _topScores ??= SaveManager.GetTopScores();
        if (_topScores is null || _topScores.Length == 0)
        {
            Renderer.Write(0, y, "Статистика недоступна");
        }
        else
        {
            for (int i = 0; i < _topScores.Length; i += 2)
            {
                Renderer.Write(0, y++, $"{_topScores[i]}: {_topScores[i + 1]}");
            }
        }
        y++;
        Renderer.Write(0, y, "Нажмите Esc для возврата в меню");
    }

    public override void HandleInput(ConsoleKey key)
    {
        if (key == ConsoleKey.Escape) GameController.ChangeState(new MainMenuState());
    }
}

// Базовый класс для игровых состояний (геймплей)
abstract class GameplayState : GameState
{
    protected static int[,] AvailableMoves = new int[0, 2];
    protected static int SelectedPawnRow = -1, SelectedPawnCol = -1;
    protected static int CurrentIdx;
    
    protected int NextInRowRight(int[,] coords, int idx) => NextInRow(coords, idx, +1, true);
    protected int NextInRowLeft(int[,] coords, int idx) => NextInRow(coords, idx, -1, true);
    protected int NextRowDown(int[,] coords, int idx) => NextRow(coords, idx, +1, true);
    protected int NextRowUp(int[,] coords, int idx) => NextRow(coords, idx, -1, true);
    private static int NextInRow(int[,] coords, int idx, int direction, bool wrapToExtreme)
    {
        int n   = coords.GetLength(0);
        int row = coords[idx, 0];
        int col = coords[idx, 1];

        int extremeIdx = -1, bestIdx = -1;

        bool IsBetterExtreme(int c, int cur) =>
            extremeIdx == -1 ||
            (direction > 0 && c < cur) ||
            (direction < 0 && c > cur);

        bool IsCandidate(int c) => direction > 0 ? c > col : c < col;

        bool IsBetterBest(int c, int cur) => bestIdx == -1 || (direction > 0 && c < cur) || (direction < 0 && c > cur);

        for (int i = 0; i < n; i++)
        {
            if (coords[i, 0] != row) continue;
            int c = coords[i, 1];

            if (IsBetterExtreme(c, coords[extremeIdx == -1 ? i : extremeIdx, 1])) extremeIdx = i;

            if (IsCandidate(c) && IsBetterBest(c, coords[bestIdx == -1 ? i : bestIdx, 1])) bestIdx = i;
        }

        return bestIdx != -1 ? bestIdx : (wrapToExtreme ? extremeIdx : -1);
    }
    private static int NextRow(int[,] coords, int idx, int direction, bool wrapToExtreme)
    {
        int n      = coords.GetLength(0);
        int curRow = coords[idx, 0];

        int bestRow = direction > 0 ? int.MaxValue : int.MinValue;
        int bestIdx = -1;

        bool Towards(int row) => direction > 0 ? row > curRow && row < bestRow : row < curRow && row > bestRow;

        bool Extreme(int row) => direction > 0 ? row < bestRow : row > bestRow;

        for (int i = 0; i < n; i++)
        {
            int row = coords[i, 0];
            if (Towards(row)) {bestRow = row; bestIdx = i;}
        }

        if (bestIdx != -1 || !wrapToExtreme) return bestIdx;

        bestRow = direction > 0 ? int.MaxValue : int.MinValue;
        bestIdx = -1;

        for (int i = 0; i < n; i++)
        {
            int row = coords[i, 0];
            if (Extreme(row)) {bestRow = row; bestIdx = i;}
        }
        return bestIdx;
    }
}

// Выбор пешки
class SelectPawnState : GameplayState
{
    private int[,] _selectableCoords = new int[0, 2];

    public override void Reset()
    {
        CurrentIdx = 0;
        var greenPawns = new List<int[]>();
        int totalPawns = GameController.Coords.GetLength(0);

        for (int i = 0; i < totalPawns; i++)
        {
            int r = GameController.Coords[i, 0], c = GameController.Coords[i, 1];
            int pawn = GameController.Field[r, c];
            bool isMyPawn = (GameController.IsWhiteTurn && pawn == Objects.WhitePawn) ||
                            (!GameController.IsWhiteTurn && pawn == Objects.BlackPawn);

            if (isMyPawn && Core.GetAvailableMovesForPawn(GameController.SaveManager, r, c).GetLength(0) > 0)
            {
                greenPawns.Add([r, c]);
            }
        }

        _selectableCoords = new int[greenPawns.Count, 2];
        for (int i = 0; i < greenPawns.Count; i++)
        {
            _selectableCoords[i, 0] = greenPawns[i][0];
            _selectableCoords[i, 1] = greenPawns[i][1];
        }
    }

    public override void Display()
    {
        int y = 0;
        string currentPlayer = GameController.IsWhiteTurn
            ? $"Сейчас ходит: {GameController.WhitePlayerName} (белые)"
            : $"Сейчас ходит: {GameController.BlackPlayerName} (чёрные)";
        
        // Шапка в едином стиле
        Renderer.Write(0, y++, "=== ИГРА ПРОРЫВ ===");
        Renderer.Write(0, y++, $"Белые: {GameController.WhitePlayerName} | Чёрные: {GameController.BlackPlayerName}");
        Renderer.Write(0, y++, currentPlayer);
        y++;
        Renderer.Write(0, y++, "=== ВЫБОР ПЕШКИ ===");
        Renderer.Write(0, y++, "Выберите пешку, которой хотите походить");
        Renderer.Write(0, y++, "Используйте стрелки (WASD) для выбора, Enter для подтверждения");
        Renderer.Write(0, y++, "Нажмите Esc для выхода в меню паузы");
        
        GameController.DrawField(y, _selectableCoords, CurrentIdx, true);
    }

    public override void HandleInput(ConsoleKey key)
    {
        if (_selectableCoords.GetLength(0) == 0) return;

        switch (key)
        {
            case ConsoleKey.RightArrow or ConsoleKey.D: CurrentIdx = NextInRowRight(_selectableCoords, CurrentIdx); break;
            case ConsoleKey.LeftArrow or ConsoleKey.A: CurrentIdx = NextInRowLeft(_selectableCoords, CurrentIdx); break;
            case ConsoleKey.UpArrow or ConsoleKey.W: CurrentIdx = NextRowUp(_selectableCoords, CurrentIdx); break;
            case ConsoleKey.DownArrow or ConsoleKey.S: CurrentIdx = NextRowDown(_selectableCoords, CurrentIdx); break;
            case ConsoleKey.Enter:
                SelectedPawnRow = _selectableCoords[CurrentIdx, 0];
                SelectedPawnCol = _selectableCoords[CurrentIdx, 1];
                AvailableMoves = Core.GetAvailableMovesForPawn(GameController.SaveManager, SelectedPawnRow, SelectedPawnCol);
                CurrentIdx = 0;
                GameController.ChangeState(new SelectMoveState());
                break;
            case ConsoleKey.Escape:
                GameController.ChangeState(new PauseMenuState());
                break;
        }
    }
}


// Выбор хода
class SelectMoveState : GameplayState
{
    public override void Display()
    {
        int y = 0;
        string currentPlayer = GameController.IsWhiteTurn
            ? $"Сейчас ходит: {GameController.WhitePlayerName} (белые)"
            : $"Сейчас ходит: {GameController.BlackPlayerName} (чёрные)";
        
        Renderer.Write(0, y++, "=== ИГРА ПРОРЫВ ===");
        Renderer.Write(0, y++, $"Белые: {GameController.WhitePlayerName} | Чёрные: {GameController.BlackPlayerName}");
        Renderer.Write(0, y++, currentPlayer);
        y++;
        Renderer.Write(0, y++, "=== ВЫБОР ХОДА ===");
        Renderer.Write(0, y++, "Выберите куда походить пешкой");
        Renderer.Write(0, y++, "Используйте стрелки для выбора хода, Enter для подтверждения");
        Renderer.Write(0, y++, "Нажмите Esc для возврата к выбору пешки");
        
        GameController.DrawField(y, AvailableMoves, CurrentIdx, false, SelectedPawnRow, SelectedPawnCol);
    }

    public override void HandleInput(ConsoleKey key)
    {
        if (AvailableMoves.GetLength(0) == 0) return;
        
        switch (key)
        {
            case ConsoleKey.RightArrow or ConsoleKey.D: CurrentIdx = (CurrentIdx + 1) % AvailableMoves.GetLength(0); break;
            case ConsoleKey.LeftArrow or ConsoleKey.A: CurrentIdx = (CurrentIdx - 1 + AvailableMoves.GetLength(0)) % AvailableMoves.GetLength(0); break;
            case ConsoleKey.UpArrow or ConsoleKey.W: CurrentIdx = FindVerticalMove(CurrentIdx, true); break;
            case ConsoleKey.DownArrow or ConsoleKey.S: CurrentIdx = FindVerticalMove(CurrentIdx, false); break;
            case ConsoleKey.Enter:
                GameController.SendMove(SelectedPawnRow, SelectedPawnCol, AvailableMoves[CurrentIdx, 0], AvailableMoves[CurrentIdx, 1]);
                CurrentIdx = 0;
                if (GameController.CurrentStateType is not MainMenuState)
                    GameController.ChangeState(new SelectPawnState());
                break;
            case ConsoleKey.Escape:
                CurrentIdx = 0;
                GameController.ChangeState(new SelectPawnState());
                break;
        }
    }

    private int FindVerticalMove(int startIdx, bool moveUp)
    {
        // ... (логика без изменений)
        if (AvailableMoves.GetLength(0) == 0) return startIdx;
        int currentCol = AvailableMoves[startIdx, 1];
        int bestIdx = -1;
        int bestRowDiff = int.MaxValue;

        for (int i = 0; i < AvailableMoves.GetLength(0); i++)
        {
            int row = AvailableMoves[i, 0];
            int col = AvailableMoves[i, 1];
            if (col == currentCol)
            {
                int rowDiff = row - AvailableMoves[startIdx, 0];
                if (moveUp && rowDiff < 0 && Math.Abs(rowDiff) < Math.Abs(bestRowDiff)) { bestRowDiff = rowDiff; bestIdx = i; }
                else if (!moveUp && rowDiff > 0 && rowDiff < Math.Abs(bestRowDiff)) { bestRowDiff = rowDiff; bestIdx = i; }
            }
        }
        return bestIdx != -1 ? bestIdx : startIdx;
    }
}

// Меню паузы
class PauseMenuState : GameState
{
    private static int _pauseMenuSelection;
    private bool _showSavedMessage;

    public override void Display()
    {
        int y = 0;
        Renderer.Write(0, y++, "=== МЕНЮ ПАУЗЫ ===");
        y++;
        string[] items = ["Продолжить игру", "Сохранить игру", "В главное меню", "Выйти из игры"];
        
        for (int i = 0; i < items.Length; i++)
        {
            Renderer.Write(0, y++, (i == _pauseMenuSelection ? "-> " : "   ") + items[i]);
        }
        y++;
        Renderer.Write(0, y++, "Используйте стрелки ↑↓ для выбора, Enter для подтверждения, Esc для отмены");
        
        if(_showSavedMessage)
        {
            y++;
            Renderer.Write(0, y, "Игра сохраняется автоматически после каждого хода.", ConsoleColor.Yellow);
        }
    }

    public override void HandleInput(ConsoleKey key)
    {
        _showSavedMessage = false; // Сбрасываем сообщение при любом действии
        switch (key)
        {
            case ConsoleKey.UpArrow: _pauseMenuSelection = (_pauseMenuSelection - 1 + 4) % 4; break;
            case ConsoleKey.DownArrow: _pauseMenuSelection = (_pauseMenuSelection + 1) % 4; break;
            case ConsoleKey.Enter:
                switch (_pauseMenuSelection)
                {
                    case 0: GameController.ChangeState(new SelectPawnState()); break;
                    case 1: _showSavedMessage = true; break; // Просто покажем сообщение в следующем кадре
                    case 2: GameController.ChangeState(new MainMenuState()); break;
                    case 3: Environment.Exit(0); break;
                }
                break;
            case ConsoleKey.Escape: GameController.ChangeState(new SelectPawnState()); break;
        }
    }
}


// Контроллер игры
static class GameController
{
    private static GameState _currentState = null!;
    public static SaveManager SaveManager = null!;

    public static int[,] Field { get; private set; } = new int[0,0];
    public static int[,] Coords { get; private set; } = new int[0, 0];
    public static bool IsWhiteTurn => Core.IsWhiteTurn(SaveManager);
    public static string WhitePlayerName { get; private set; } = string.Empty;
    public static string BlackPlayerName { get; private set; } =  string.Empty;
    public static GameState CurrentStateType => _currentState;

    public static void Initialize()
    {
        Renderer.Initialize();
        ChangeState(new MainMenuState());
        Run();
    }

    public static void ChangeState(GameState newState)
    {
        _currentState = newState;
        newState.Reset();
    }

    private static void Run()
    {
        while (true)
        {
            Renderer.Clear();
            _currentState.Display();
            Renderer.Present();
            
            ConsoleKey key = Console.ReadKey(true).Key;
            
            // Логика для завершения игры (например, из PauseMenu)
            if (key == ConsoleKey.F10) break; // Пример аварийного выхода

            _currentState.HandleInput(key);
        }
    }

    public static void LoadGame(SaveManager loadedManager)
    {
        SaveManager = loadedManager;
        UpdateLocalState();

        if (SaveManager.Players is not null && SaveManager.Players.Length >= 2)
        {
            WhitePlayerName = SaveManager.Players[0];
            BlackPlayerName = SaveManager.Players[1];
        }
    }

    public static void StartNewGame(string whiteName, string blackName, int firstMove, int height, int width)
    {
        WhitePlayerName = whiteName;
        BlackPlayerName = blackName;
        SaveManager = new SaveManager();
        int[,] matrix = Core.CreateMatrix(height, width);
        SaveManager.Start(matrix, [whiteName, blackName], firstMove);
        UpdateLocalState();
    }

    public static void SendMove(int fromRow, int fromCol, int toRow, int toCol)
    {
        var result = Core.MakeMove(SaveManager, fromRow, fromCol, toRow, toCol);
        UpdateLocalState();

        if (result.IsGameOver)
        {
            Renderer.Clear();
            DrawField(0, new int[0,0], 0, false); // Отрисовка финального поля
            Renderer.Write(0, Field.GetLength(0) * 2 + 5, $"\nИГРА ОКОНЧЕНА! {result.Message}", ConsoleColor.Yellow);
            Renderer.Write(0, Field.GetLength(0) * 2 + 6, "Нажмите любую клавишу для выхода в меню...", ConsoleColor.Yellow);
            Renderer.Present();
            Console.ReadKey(true);
            ChangeState(new MainMenuState());
        }
    }

    private static void UpdateLocalState()
    {
        if (SaveManager.Matrix is not null)
        {
            Field = SaveManager.Matrix;
            Coords = Core.GetPawnCoordinates(Field);
        }
    }
    
    // Метод отрисовки поля перенесен в GameController и адаптирован под Renderer
    public static void DrawField(int startY, int[,] activeCoords, int activeIdx, bool isPawnSelection, int selectedRow = -1, int selectedCol = -1)
    {
        int y = startY;
        string turnInfo = $"Ход: {SaveManager.MoveCount + 1} | Ходит: {(IsWhiteTurn ? WhitePlayerName : BlackPlayerName)}";
        Renderer.Write(0, y++, turnInfo);
        y++;
        int rows = Field.GetLength(0);
        int cols = Field.GetLength(1);

        // Буквы столбцов
        StringBuilder header = new StringBuilder("    ");
        for (int c = 0; c < cols; c++) header.Append($" {(char)('a' + c)}  ");
        Renderer.Write(0, y++, header.ToString());

        // Разделитель
        StringBuilder line = new StringBuilder("   +");
        for (int c = 0; c < cols; c++) line.Append("---+");
        line.Append('+');
        string lineStr = line.ToString();

        Renderer.Write(0, y++, lineStr);

        for (int r = 0; r < rows; r++)
        {
            StringBuilder rowStr = new StringBuilder();
            rowStr.Append($" {r + 1,-2}|");

            for (int c = 0; c < cols; c++)
            {
                bool isCursor = activeCoords.GetLength(0) > 0 && activeIdx < activeCoords.GetLength(0) &&
                                activeCoords[activeIdx, 0] == r && activeCoords[activeIdx, 1] == c;
                
                int cell = Field[r, c];
                string pawnSymbol = "   ";
                ConsoleColor color = ConsoleColor.Gray;

                if (cell == Objects.WhitePawn)
                {
                    pawnSymbol = " " + Objects.Pawn + " ";
                    color = ConsoleColor.White;
                }
                else if (cell == Objects.BlackPawn)
                {
                    pawnSymbol = " " + Objects.Pawn + " ";
                    color = ConsoleColor.DarkGray;
                }

                bool canMove = isPawnSelection && Core.GetAvailableMovesForPawn(SaveManager, r, c).GetLength(0) > 0;
                
                if (canMove || (r == selectedRow && c == selectedCol)) color = ConsoleColor.Green;
                if (isCursor)
                {
                    pawnSymbol = cell == Objects.Space ? "[ ]" : "[" + Objects.Pawn + "]";
                    color = ConsoleColor.Green;
                }

                // Пишем в рендер посимвольно с цветом
                int currentX = rowStr.Length;
                for (int i = 0; i < pawnSymbol.Length; i++)
                {
                    Renderer.Write(currentX + i, y, pawnSymbol[i].ToString(), color);
                }
                rowStr.Append(pawnSymbol);
                
                Renderer.Write(rowStr.Length, y, "|", ConsoleColor.White);
                rowStr.Append("|");
            }
            // Генерируем номера строк и разделители
            Renderer.Write(0, y, $" {r + 1,-2}|");
            y++;
            Renderer.Write(0, y++, lineStr);
        }
    }
}