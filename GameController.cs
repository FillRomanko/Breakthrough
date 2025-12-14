using System.Text;

namespace Breakthrough;

/// Статический класс для отрисовки без мерцания с использованием двойной буферизации.
/// Все операции записи сначала происходят в фоновом буфере (`backBuffer`).
/// Метод Present() сравнивает фоновый буфер с текущим (`frontBuffer`) и обновляет на экране только изменившиеся ячейки.
internal static class Renderer
{
    // Структура для хранения данных об одной ячейке консоли (символ и цвет)
    private struct ConsoleCell : IEquatable<ConsoleCell>
    {
        public char Character; // Символ в ячейке
        public ConsoleColor ForegroundColor; // Цвет символа

        // Для сравнения структур
        public bool Equals(ConsoleCell other) => Character == other.Character && ForegroundColor == other.ForegroundColor;
        
        // Сравнение двух ячеек на равенство
        public override bool Equals(object? obj) => obj is ConsoleCell other && Equals(other);

        // Получение кода с учётом цвета символа
        public override int GetHashCode() => HashCode.Combine(Character, ForegroundColor);
        
        // Оператор сравнения на равенство двух ячеек (теперь можно использовать ==)
        public static bool operator == (ConsoleCell left, ConsoleCell right) => left.Equals(right);

        // Оператор сравнения на неравенство двух ячеек (теперь можно использовать ==)
        public static bool operator !=(ConsoleCell left, ConsoleCell right) => !left.Equals(right);
    }

    private static int _width; // ширина консоли
    private static int _height; // высота консоли
    private static ConsoleCell[,] _frontBuffer =  new ConsoleCell[0, 0]; // Текущее содержимое экрана
    private static ConsoleCell[,] _backBuffer = new ConsoleCell[0, 0];  // Буфер для подготовки следующего изменения
    
    // Инициализация класса с настройкой консоли в буфере
    public static void Initialize()
    {
        Console.OutputEncoding = Encoding.UTF8; // Поддержка UTF-8
        
        Console.SetWindowSize(80, 45); // Задаём размеры окна
        
        _width = Console.WindowWidth; // Получаем ширину окна
        _height = Console.WindowHeight; // Получаем высоту окна

        _frontBuffer = new ConsoleCell[_width, _height]; // Инициализируем старый буфер
        _backBuffer = new ConsoleCell[_width, _height]; // Инициализируем новый буфер
        
        SetCursorVisibility(false); // Скрываем курсор
        Clear(true); // Очищаем оба буфера
    }
    
    /// Очищает фоновый буфер для подготовки нового кадра.
    public static void Clear(bool clearBoth = false)
    {
        ConsoleCell empty = new() { Character = ' ', ForegroundColor = ConsoleColor.Gray }; // Пустая ячейка
        for (int y = 0; y < _height; y++)
        for (int x = 0; x < _width; x++)
        {
            _backBuffer[x, y] = empty; // Очищаем клетку в новом буфере
            if (clearBoth) _frontBuffer[x, y] = empty; // Очищаем клетку в старом буфере при необходимости
        }
    }

    /// Записывает текст в фоновый буфер по координатам
    public static void Write(int x, int y, string text, ConsoleColor color = ConsoleColor.Gray)
    {
        if (y >= _height) return; // Проверка на выход за высоту
        for (int i = 0; i < text.Length; i++)
        {
            int realX = x + i; // Вычисление реальной позиции по X
            if (realX >= _width) break; // Прерываем, что бы не вышел за границу
            _backBuffer[realX, y] = new ConsoleCell { Character = text[i], ForegroundColor = color }; // Записываем символ
        }
    }
    
    /// Отображает фоновый буфер на экране, обновляя только изменившиеся ячейки.
    public static void Present()
    {
        for (int y = 0; y < _height; y++)
        {
            for (int x = 0; x < _width; x++)
            {
                if (_frontBuffer[x, y] != _backBuffer[x, y]) // Если старое и новое состояние кадров не совпадают
                {
                    _frontBuffer[x, y] = _backBuffer[x, y]; // Обновляем передний
                    PositionCursor(x, y); // Устанавливаем нужную позицию курсора
                    Console.ForegroundColor = _backBuffer[x, y].ForegroundColor; // Устанавливаем цвет символа
                    Console.Write(_backBuffer[x, y].Character); // Пишем сам символ
                }
            }
        }
        Console.SetCursorPosition(0,0); // Возвращаем курсор в начало, чтобы избежать артефактов при изменении размера окна
    }

    // Установка видимости курсора
    public static void SetCursorVisibility(bool visible) => Console.CursorVisible = visible;
    
    // Установка позиции курсора
    public static void PositionCursor(int x, int y) => Console.SetCursorPosition(x, y);
}


// Базовый абстрактный класс для состояний игры
abstract class GameState
{
    public abstract void Display(); // Метод для отрисовки состояния
    public abstract void HandleInput(ConsoleKey key); // Метод для обработки ввода пользователя
    public virtual void Reset() { } // Метод для сброса состояния при переходе к нему
}

// Главное меню
class MainMenuState : GameState
{
    private static int _mainMenuSelection; // Индекс выбранного пункта в меню
    private bool _hasSavedGames; // Флаг наличия сохранённых игр

    private string MenuItem(int index, string text) => $"{(_mainMenuSelection == index ? "→ " : "  ")}{text}"; // Формирование пункта меню с индикатором выбора
    
    // Отрисовка главного меню
    public override void Display()
    {
        int y = 0; // Хранит на какой строке находится текст
        Renderer.Write(0, y++, "=== ГЛАВНОЕ МЕНЮ ===");
        y++; // Добавляем пустую строку

        _hasSavedGames = CheckSavedGames(); // Проверяем наличие сохранений
        ConsoleColor continueColor = _hasSavedGames ? ConsoleColor.Gray : ConsoleColor.DarkGray; // Меняем цвет кнопки в зависимости от наличия сохранений
        string continueText = "1. Продолжить игру" + (_hasSavedGames ? "" : " (нет сохранений)"); // Меняем текст в зависимости от наличия сохранений
        
        Renderer.Write(0, y++, (MenuItem(0,"")) + continueText, continueColor); // Выводим кнопку продолжить игру
        Renderer.Write(0, y++, (MenuItem(1 ,"2. Новая игра"))); // Выводим кнопку новая игра
        Renderer.Write(0, y++, (MenuItem(2 ,"3. Статистика"))); // То же самое, невероятно
        Renderer.Write(0, y++, (MenuItem(3 , "4. Закрыть игру"))); // То же самое, неудивительно
        y++; // Добавляем пустую строку
        Renderer.Write(0, y, "Используйте стрелки ↑↓ для выбора, Enter для подтверждения, Esc для выхода"); // Дружественное указание как рулить
    }

    // Обработка ввода в главном меню
    public override void HandleInput(ConsoleKey key)
    {
        switch (key) 
        {
            case ConsoleKey.UpArrow: _mainMenuSelection = (_mainMenuSelection - 1 + 4) % 4; break; // Циклически перемещаем выбор вверх
            case ConsoleKey.DownArrow: _mainMenuSelection = (_mainMenuSelection + 1) % 4; break; // Точно также, но вниз
            case ConsoleKey.Enter: // Если вводим Enter
                switch (_mainMenuSelection)
                {
                    case 0: if (_hasSavedGames) GameController.ChangeState(new SavedGamesState()); break; // Если выбор на 1 и есть сохранения, переходим в сохранения
                    case 1: GameController.ChangeState(new NewGameMenuState()); break; // Если 2, то запускаем игру
                    case 2: GameController.ChangeState(new StatisticsState()); break; // Смотрим статистику игр
                    case 3: Environment.Exit(0); break; // Завершаем программу
                }
                break;
            case ConsoleKey.Escape: Environment.Exit(0); break; // Закрываем программу, если нажата Esc
        }
    }
    
    // Метод, который проверяет наличие сохранений
    private bool CheckSavedGames() => (SaveManager.GetAllSaves().Length) > 0;
}

// Меню новой игры
class NewGameMenuState : GameState
{
    private static int _newGameMenuSelection; // Индекс выбранного пункта
    private static string _whitePlayerName = "Игрок 1"; // Имя игрока за белых, задано по умолчанию
    private static string _blackPlayerName = "Игрок 2"; // Имя игрока за чёрных, задано по умолчанию
    private static int _firstMoveMode; // Режим первого хода (0 - белые, 1 - чёрные, 2 - случайно)
    private static int _boardHeight = 8; // Высота доски, задана по умолчанию
    private static int _boardWidth = 8; // Ширина доски, задана по умолчанию

    // Отрисовка меню
    public override void Display()
    {
        int y = 0; // Пишем сверху вниз
        Renderer.Write(0, y++, "=== НАСТРОЙКИ НОВОЙ ИГРЫ ===");
        y++; // Пропускаем одну строку

        // Все поля ниже - вывод текущих настроек
        Renderer.Write(0, y++, (_newGameMenuSelection == 0 ? "→ " : "  ") + $"Белые: {_whitePlayerName}");
        Renderer.Write(0, y++, (_newGameMenuSelection == 1 ? "→ " : "  ") + $"Чёрные: {_blackPlayerName}");

        string firstMoveText = _firstMoveMode switch { 0 => "белые", 1 => "чёрные", _ => "случайно" }; // Строка, которая показывает выбранный режим первого хода
        Renderer.Write(0, y++, (_newGameMenuSelection == 2 ? "→ " : "  ") + $"Первый ход: {firstMoveText}");
        
        Renderer.Write(0, y++, (_newGameMenuSelection == 3 ? "→ " : "  ") + $"Высота доски: {_boardHeight}");
        Renderer.Write(0, y++, (_newGameMenuSelection == 4 ? "→ " : "  ") + $"Ширина доски: {_boardWidth}");
        y++;

        bool canStart = !string.IsNullOrWhiteSpace(_whitePlayerName) && !string.IsNullOrWhiteSpace(_blackPlayerName) && _whitePlayerName != _blackPlayerName; // Определяем, можно ли начать игру по тому, правильно ли ввели пользователи свои имена
        ConsoleColor startColor = canStart ? ConsoleColor.White : ConsoleColor.DarkGray; // Если игру нельзя начать, меняем цвет "начать игру"
        string startText = "НАЧАТЬ ИГРУ" + (canStart ? "" : " (укажите разные существующие имена игроков)"); // Меняем кнопку в зависимости от возможности начать игру
        Renderer.Write(0, y++, (_newGameMenuSelection == 5 ? "→ " : "  ") + startText, startColor); // Выводим кнопку
        
        y++;
        Renderer.Write(0, y, "Используйте стрелки ↑↓ для выбора, ←→ для изменения, Enter для редактирования, Esc для отмены"); // Учим пользователя рулить
    }

    // Обработка ввода в меню новой игры
    public override void HandleInput(ConsoleKey key)
    {
        switch (key)
        {
            case ConsoleKey.UpArrow: _newGameMenuSelection = (_newGameMenuSelection - 1 + 6) % 6; break; // Циклически меняем выбор вверх
            case ConsoleKey.DownArrow: _newGameMenuSelection = (_newGameMenuSelection + 1) % 6; break; // Циклически меняем выбор вниз
            case ConsoleKey.Enter: // Если нажали Enter
                if (_newGameMenuSelection is 0 or 1) HandleNameInput(); // Если один или два, то смена имени игрока
                else if (_newGameMenuSelection == 5)  // Если 5 (там где начать игру)
                {
                    if (!string.IsNullOrWhiteSpace(_whitePlayerName) && !string.IsNullOrWhiteSpace(_blackPlayerName) && _whitePlayerName != _blackPlayerName) //Проверяем правильные имена, если верно, то
                    {
                        GameController.StartNewGame(_whitePlayerName, _blackPlayerName, _firstMoveMode, _boardHeight, _boardWidth); // Начинаем игру с характеристиками
                        GameController.ChangeState(new SelectPawnState()); // Меняемся на выбор хода
                    }
                }
                break;
            case ConsoleKey.RightArrow: HandleNewGameChange(true); break; // Меняем положение вправо
            case ConsoleKey.LeftArrow: HandleNewGameChange(false); break; // Меняем положение влево
            case ConsoleKey.Escape: GameController.ChangeState(new MainMenuState()); break; // Выходим в главное меню, если нажата Esc
        }
    }
    
    // Обработка ввода имени игрока с консоли
    private void HandleNameInput()
    {
        // 1. Отобразить текущий экран
        Renderer.Present();
        int inputLine = _newGameMenuSelection == 0 ? 10 : 11; // Определяем строку для ввода
        Renderer.PositionCursor(0, inputLine); // Устанавливаем курсор
        Renderer.SetCursorVisibility(true); // Делаем курсор видимым
        Console.Write(_newGameMenuSelection == 0 ? "\nВведите имя игрока выступающего за белых: " : "\nВведите имя игрока выступающего за чёрных: ");
        string? input = Console.ReadLine(); // Получаем ввод пользователя
        if (!string.IsNullOrWhiteSpace(input)) // Если пользователь ввёл не пробелы и не пустую строку
        {
            if (_newGameMenuSelection == 0) _whitePlayerName = input; // Сохраняем новое имя белы
            else _blackPlayerName = input; // Иначе чёрных
        }

        // Очистка артефактов
        Renderer.SetCursorVisibility(false); // Скрываем курсор
        string cleaner = new string(' ', Console.WindowWidth);// Создаем строку пробелов во всю ширину окна

        // Очищаем строки, где был ввод. 
        int lineToClear = inputLine + 1;
        Console.SetCursorPosition(0, lineToClear);
        Console.Write(cleaner); // Очищаем строку, где был ввод
        
        Console.SetCursorPosition(0, lineToClear + 1);
        Console.Write(cleaner); // Очищаем следующую строку, так как Enter плодит новую строку
        Renderer.PositionCursor(0, 0);// Возвращаем курсор в начало, чтобы не сломать логику Renderer
    }

    // Измени е параметров первой игры
    private void HandleNewGameChange(bool increase)
    {
        switch (_newGameMenuSelection)
        {
            case 2: _firstMoveMode = increase ? (_firstMoveMode + 1) % 3 : (_firstMoveMode + 2) % 3; break; // Даём выбрать только между 3 вариантами
            case 3: _boardHeight = Math.Clamp(_boardHeight + (increase ? 1 : -1), 6, 16); break; // Даём выбрать только между 6 и 16
            case 4: _boardWidth = Math.Clamp(_boardWidth + (increase ? 1 : -1), 4, 16); break; // Даём выбрать только между 4 и 16
        }
    }
    public override void Reset() => _newGameMenuSelection = 0; // Возвращаем в начальное состояние 
}

// Список сохранённых игр
class SavedGamesState : GameState
{
    private SaveManager[]? _savedGames; // Массив сохранённых игр
    private int _selectedIndex; // Индекс выбранного сохранения

    // Сброс состояния и перезагрузка всех сохранений
    public override void Reset()
    {
        _savedGames = SaveManager.GetAllSaves(); // Получаем все сохранения
        _selectedIndex = 0; // Ставим автоматически выбор на последнее
    }

    // Отрисовка списка сохранённых игр
    public override void Display()
    {
        int y = 0; // Номер строки, в котором вводим текст
        Renderer.Write(0, y++, "=== ВЫБОР СОХРАНЕННОЙ ИГРЫ ===");
        y++;

        if (_savedGames is null || _savedGames.Length == 0) // Если нет сохранённых игр (на всякий случай)
        {
            Renderer.Write(0, y++, "Нет сохраненных игр");
            y++;
            Renderer.Write(0, y, "Нажмите Enter для возврата...");
            return;
        }

        for (int i = 0; i < _savedGames.Length; i++) // Выводим список сохранений
        {
            var sm = _savedGames[i];
            string p1 = sm.Players?[0] ?? "???"; // Если не обнаруживаем имя игрока, то его зовут вопрос
            string p2 = sm.Players?[1] ?? "???";
            string line = $"{i + 1}. {sm.UniqueCode} - {p1} vs {p2} (Ход: {sm.MoveCount})"; // Состояние, в котором сохранилась игра
            ConsoleColor color = i == _selectedIndex ? ConsoleColor.Green : ConsoleColor.Gray; // Текущая строка выделена зелёным
            Renderer.Write(0, y++, (i == _selectedIndex ? "> " : "  ") + line, color); // Выводим строку с её цветом
        }

        ConsoleColor backColor = _selectedIndex == _savedGames.Length ? ConsoleColor.Green : ConsoleColor.Gray;
        Renderer.Write(0, y++, (_selectedIndex == _savedGames.Length ? "> " : "  ") + "Назад", backColor);
        
        y++;
        Renderer.Write(0, y, "Используйте стрелки для выбора, Enter для загрузки.");
    }
    
    // Обработка ввода в списке сохранений
    public override void HandleInput(ConsoleKey key)
    {
        if (_savedGames is null || _savedGames.Length == 0) // Если нет сохранений
        {
            if (key is ConsoleKey.Enter or ConsoleKey.Escape) GameController.ChangeState(new MainMenuState()); // Выходим в главное меню при нажатии Enter или Ecs
            return;
        }

        int totalOptions = _savedGames.Length + 1; // Количество опций для выбора + назад
        switch (key)
        {
            case ConsoleKey.UpArrow: _selectedIndex = (_selectedIndex - 1 + totalOptions) % totalOptions; break;
            case ConsoleKey.DownArrow: _selectedIndex = (_selectedIndex + 1) % totalOptions; break;
            case ConsoleKey.Enter:
                if (_selectedIndex == _savedGames.Length) GameController.ChangeState(new MainMenuState()); // Если строка и количество сохранений совпадают, то есть выбрана кнопка назад, то выходим
                else
                {
                    GameController.LoadGame(_savedGames[_selectedIndex]); // Загружаем игру
                    GameController.ChangeState(new SelectPawnState()); // Переходим к игре
                }
                break;
            case ConsoleKey.Escape: GameController.ChangeState(new MainMenuState()); break;
        }
    }
}

// Состояние отображения статистики
class StatisticsState : GameState
{
    private string[]? _topScores; // Массив строк статистики

    public override void Display()
    {
        int y = 0;
        Renderer.Write(0, y++, "=== СТАТИСТИКА ===");
        y++; // Пустая строка
        
        _topScores ??= SaveManager.GetTopScores(); // Загружаем статистику
        if (_topScores is null || _topScores.Length == 0) // Если статистика пуста, то пишем, что она недоступна
        {
            Renderer.Write(0, y, "Статистика недоступна"); 
        }
        else
        {
            for (int i = 0; i < _topScores.Length; i += 2)
            {
                Renderer.Write(0, y++, $"{_topScores[i]}: {_topScores[i + 1]}"); // Построчно выводим статистику
            }
        }
        y++;
        Renderer.Write(0, y, "Нажмите Esc для возврата в меню");
    }

    // Обработка ввода в статистике
    public override void HandleInput(ConsoleKey key)
    {
        if (key == ConsoleKey.Escape) GameController.ChangeState(new MainMenuState()); // Возврат в главное меню после нажатия Esc
    }
}

// Базовый класс для игровых состояний (геймплей ура)
abstract class GameplayState : GameState
{
    protected static int[,] AvailableMoves = new int[0, 2]; // Массив доступных ходов
    protected static int SelectedPawnRow = -1, SelectedPawnCol = -1; // Координаты выбранной пешки
    protected static int CurrentIdx; // Текущий индекс выб
    
    // Методы навигации по координатам
    protected int NextInRowRight(int[,] coords, int idx) => NextInRow(coords, idx, +1, true);
    protected int NextInRowLeft(int[,] coords, int idx) => NextInRow(coords, idx, -1, true);
    protected int NextRowDown(int[,] coords, int idx) => NextRow(coords, idx, +1, true);
    protected int NextRowUp(int[,] coords, int idx) => NextRow(coords, idx, -1, true);
    
    // Для столбца
    private static int NextInRow(int[,] coords, int idx, int direction, bool wrapToExtreme)
    {
        int n   = coords.GetLength(0);
        int row = coords[idx, 0];
        int col = coords[idx, 1];

        int extremeIdx = -1, bestIdx = -1; // Индексы крайней позиции

        // Проверка, является ли позиция самой крайней
        bool IsBetterExtreme(int c, int cur) =>
            extremeIdx == -1 ||
            (direction > 0 && c < cur) ||
            (direction < 0 && c > cur);
        
        bool IsCandidate(int c) => direction > 0 ? c > col : c < col; // Проверка, является ли позиция кандидатом для выбора

        bool IsBetterBest(int c, int cur) => bestIdx == -1 || (direction > 0 && c < cur) || (direction < 0 && c > cur);

        for (int i = 0; i < n; i++) // Проходим по всем координатам
        {
            if (coords[i, 0] != row) continue; // Пропускаем элементы других строк
            int c = coords[i, 1];

            if (IsBetterExtreme(c, coords[extremeIdx == -1 ? i : extremeIdx, 1])) extremeIdx = i;

            if (IsCandidate(c) && IsBetterBest(c, coords[bestIdx == -1 ? i : bestIdx, 1])) bestIdx = i;
        }

        return bestIdx != -1 ? bestIdx : (wrapToExtreme ? extremeIdx : -1);
    }
    
    // Для строки
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

// Выбор пешки для хода
class SelectPawnState : GameplayState
{
    private int[,] _selectableCoords = new int[0, 2]; // Координаты пешек, которые могут ходить

    // Сброс состояния и определение пешек с доступными ходами
    public override void Reset()
    {
        CurrentIdx = 0;
        var greenPawns = new List<int[]>(); // Список пешек с доступными ходами
        int totalPawns = GameController.Coords.GetLength(0);

        for (int i = 0; i < totalPawns; i++)
        {
            int r = GameController.Coords[i, 0], c = GameController.Coords[i, 1];
            
            if (Core.GetAvailableMovesForPawn(GameController.SaveManager, r, c).GetLength(0) > 0)
            {
                greenPawns.Add([r, c]); // Добавляем пешку в список доступных
            }
        }

        _selectableCoords = new int[greenPawns.Count, 2]; // Преобразуем в двумерный массив
        for (int i = 0; i < greenPawns.Count; i++)
        {
            _selectableCoords[i, 0] = greenPawns[i][0];
            _selectableCoords[i, 1] = greenPawns[i][1];
        }
    }

    // Отрисовка состояние выбора пешки
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
        Renderer.Write(0, y++, "=== ВЫБОР ПЕШКИ ===");
        Renderer.Write(0, y++, "Выберите пешку, которой хотите походить");
        Renderer.Write(0, y++, "Используйте стрелки или (WASD) для выбора, Enter для подтверждения");
        Renderer.Write(0, y++, "Нажмите Esc для выхода в меню паузы");
        
        GameController.DrawField(y, _selectableCoords, CurrentIdx, true); // Выводим поле
    }

    // Обработка ввода при выборе пешки
    public override void HandleInput(ConsoleKey key)
    {
        if (_selectableCoords.GetLength(0) == 0) return; // Если нет доступных пешек, то ничего не делаем

        switch (key)
        {
            case ConsoleKey.RightArrow or ConsoleKey.D: CurrentIdx = NextInRowRight(_selectableCoords, CurrentIdx); break;
            case ConsoleKey.LeftArrow or ConsoleKey.A: CurrentIdx = NextInRowLeft(_selectableCoords, CurrentIdx); break;
            case ConsoleKey.UpArrow or ConsoleKey.W: CurrentIdx = NextRowUp(_selectableCoords, CurrentIdx); break;
            case ConsoleKey.DownArrow or ConsoleKey.S: CurrentIdx = NextRowDown(_selectableCoords, CurrentIdx); break;
            case ConsoleKey.Enter:
                SelectedPawnRow = _selectableCoords[CurrentIdx, 0]; // Сохраняем координаты выбранной пешки
                SelectedPawnCol = _selectableCoords[CurrentIdx, 1];
                AvailableMoves = Core.GetAvailableMovesForPawn(GameController.SaveManager, SelectedPawnRow, SelectedPawnCol); // Передаём в Core выбор игрока, что бы получить доступные ходы
                CurrentIdx = 0;
                GameController.ChangeState(new SelectMoveState()); // Переводим в выбор хода
                break;
            case ConsoleKey.Escape:
                GameController.ChangeState(new PauseMenuState()); // Выходим в меню паузы
                break;
        }
    }
}


// Состояние выбора хода для пешки
class SelectMoveState : GameplayState
{
    // Отрисовка выбора игрока
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
        
        GameController.DrawField(y, AvailableMoves, CurrentIdx, false, SelectedPawnRow, SelectedPawnCol); // Отрисовка поля с ходами
    }

    // Отработка ввода игрока
    public override void HandleInput(ConsoleKey key)
    {
        if (AvailableMoves.GetLength(0) == 0) return; // Если нет доступных ходов, ничего не делаем
        
        switch (key)
        {
            case ConsoleKey.RightArrow or ConsoleKey.D: CurrentIdx = (CurrentIdx + 1) % AvailableMoves.GetLength(0); break; // Циклический ход вправо
            case ConsoleKey.LeftArrow or ConsoleKey.A: CurrentIdx = (CurrentIdx - 1 + AvailableMoves.GetLength(0)) % AvailableMoves.GetLength(0); break; // Циклический ход влево
            case ConsoleKey.Enter:
                GameController.SendMove(SelectedPawnRow, SelectedPawnCol, AvailableMoves[CurrentIdx, 0], AvailableMoves[CurrentIdx, 1]); // Отправляем ход
                CurrentIdx = 0;
                if (GameController.CurrentStateType is not MainMenuState)
                    GameController.ChangeState(new SelectPawnState());
                break;
            case ConsoleKey.Escape:
                CurrentIdx = 0;
                GameController.ChangeState(new SelectPawnState()); // Возвращаемся к выбору пешки
                break;
        }
    }
}

// Меню паузы
class PauseMenuState : GameState
{
    private static int _pauseMenuSelection; //Индекс выбранного пункта
    private bool _showSavedMessage; // Флаг отображения сообщения о сохранении

    // Отрисовка меню паузы
    public override void Display()
    {
        int y = 0;
        Renderer.Write(0, y++, "=== МЕНЮ ПАУЗЫ ===");
        y++; // Пропускаем строку
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
            Renderer.Write(0, y, "Игра сохраняется автоматически после каждого хода.", ConsoleColor.Yellow); // Если нужно показать сообщение об автоматическом сохранении
        }
    }

    // Обработка ввода в меню паузы
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
                    case 0: GameController.ChangeState(new SelectPawnState()); break; // Возвращаемся в игру
                    case 1: _showSavedMessage = true; break; // Меняем на показ сообщений об автоматическом сохранении
                    case 2: GameController.ChangeState(new MainMenuState()); break; // Возвращаемся в главное меню
                    case 3: Environment.Exit(0); break; // Завершаем программу
                }
                break;
            case ConsoleKey.Escape: GameController.ChangeState(new SelectPawnState()); break; // Возвращаем в игру
        }
    }
}


// Главный контроллер игры, управляющий состояниями и игровой логикой
static class GameController
{
    private static GameState _currentState = null!; // Текущее состояние игры
    public static SaveManager SaveManager = null!; // Менеджер сохранений

    private static int[,] Field { get; set; } = new int[0,0]; // Игровое поле
    public static int[,] Coords { get; private set; } = new int[0, 0]; // Координаты всех пешек на поле
    public static bool IsWhiteTurn => Core.IsWhiteTurn(SaveManager); // Свойство определяющее чей сейчас ход
    public static string WhitePlayerName { get; private set; } = string.Empty; // Имя игрока за белых
    public static string BlackPlayerName { get; private set; } =  string.Empty; // Имя игрока за чёрных
    public static GameState CurrentStateType => _currentState; // Свойство для доступа к текущему состоянию

    // Инициализация игры
    public static void Initialize()
    {
        Renderer.Initialize();
        ChangeState(new MainMenuState()); // Установка начального состояния
        Run(); // Запуск игрового цикла
    }

    // Смена состояния игры
    public static void ChangeState(GameState newState)
    {
        _currentState = newState; // Устанавливаем новое состояние
        newState.Reset(); // Сбрасываем состояние к начальному виду
    }

    // Основной игровой цикл
    private static void Run()
    {
        while (true)
        {
            Renderer.Clear(); // Очищаем буфер 
            _currentState.Display(); // Показываем текущее состояние
            Renderer.Present(); // Отображаем буфер на экране
            
            ConsoleKey key = Console.ReadKey(true).Key; // Считываем нажатую клавишу
            
            if (key == ConsoleKey.F10) break; // Аварийный выход (опционально)
            
            _currentState.HandleInput(key); // Обрабатываем ввод в текущем состоянии
        }
    }

    // Загрузка существующей игры
    public static void LoadGame(SaveManager loadedManager)
    {
        SaveManager = loadedManager; // Устанавливаем загруженный менеджер
        UpdateLocalState(); // Обновляем локальное состояние

        if (SaveManager.Players is not null && SaveManager.Players.Length >= 2) // Загружаем имена игроков
        {
            WhitePlayerName = SaveManager.Players[0];
            BlackPlayerName = SaveManager.Players[1];
        }
    }

    // Создание новой игры
    public static void StartNewGame(string whiteName, string blackName, int firstMove, int height, int width)
    {
        WhitePlayerName = whiteName;
        BlackPlayerName = blackName;
        SaveManager = new SaveManager(); // Создаём новый менеджер сохранений
        int[,] matrix = Core.CreateMatrix(height, width); // Создаём начальную матрицу
        SaveManager.Start(matrix, [whiteName, blackName], firstMove);  // Инициализируем игру в менеджере
        UpdateLocalState(); // Обновляем локальное состояние
    }

    // Отправка хода в игровую логику
    public static void SendMove(int fromRow, int fromCol, int toRow, int toCol) 
    {
        var result = Core.MakeMove(SaveManager, fromRow, fromCol, toRow, toCol); // Применяем ход через Core
        UpdateLocalState(); // Обновляем локальное состояние

        if (result.IsGameOver) // Если игра завершена
        {
            Renderer.Clear();
            DrawField(0, new int[0,0], 0, false); // Отрисовка финального поля
            Renderer.Write(0, Field.GetLength(0) * 2 + 5, $"\nИГРА ОКОНЧЕНА! {result.Message}", ConsoleColor.Yellow);
            Renderer.Write(0, Field.GetLength(0) * 2 + 6, "Нажмите любую клавишу для выхода в меню...", ConsoleColor.Yellow);
            Renderer.Present();
            Console.ReadKey(true); // Ожидаем нажатия клавиши
            ChangeState(new MainMenuState()); // Возврат в главное меню
        }
    }

    // Обновление локального состояния (поле и координаты пешек)
    private static void UpdateLocalState()
    {
        if (SaveManager.Matrix is not null)
        {
            Field = SaveManager.Matrix; // Обновляем поле
            Coords = Core.GetPawnCoordinates(Field); // Обновляем координаты пешек
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

        // Отрисовка столбцов с буквами
        StringBuilder header = new StringBuilder("    ");
        for (int c = 0; c < cols; c++) header.Append($" {(char)('a' + c)}  ");
        Renderer.Write(0, y++, header.ToString());

        // Отрисовка горизонтального разделителя
        StringBuilder line = new StringBuilder("   +");
        for (int c = 0; c < cols; c++) line.Append("---+");
        string lineStr = line.ToString();
        Renderer.Write(0, y++, lineStr);
        
        // Отрисовка строк поля
        for (int r = 0; r < rows; r++)
        {
            StringBuilder rowStr = new StringBuilder();
            rowStr.Append($" {r + 1,-2}|");

            for (int c = 0; c < cols; c++)
            {
                bool isCursor = activeCoords.GetLength(0) > 0 && activeIdx < activeCoords.GetLength(0) &&
                                activeCoords[activeIdx, 0] == r && activeCoords[activeIdx, 1] == c; // Проверка, является ли текущая клетка курсором
                
                int cell = Field[r, c];
                string pawnSymbol = "   "; // По умолчанию пустая клетка
                ConsoleColor color = ConsoleColor.Gray;

                if (cell == Objects.WhitePawn) // Белая пешка
                {
                    pawnSymbol = " " + Objects.Pawn + " ";
                    color = ConsoleColor.White;
                }
                else if (cell == Objects.BlackPawn) // Чёрная пешка
                {
                    pawnSymbol = " " + Objects.Pawn + " ";
                    color = ConsoleColor.DarkGray;
                }
                
                // Подсвечиваем пешки с доступными ходами или выбранную пешку
                bool canMove = isPawnSelection && Core.GetAvailableMovesForPawn(SaveManager, r, c).GetLength(0) > 0;
                
                if (canMove || (r == selectedRow && c == selectedCol)) color = ConsoleColor.Green;
                if (isCursor)
                {
                    pawnSymbol = cell == Objects.Space ? "[ ]" : "[" + Objects.Pawn + "]";
                    color = ConsoleColor.Green;
                }

                // Отрисовка символов пешки посимвольно с цветом
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