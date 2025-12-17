using System.Text.Json;

namespace Breakthrough;

// Управляет логикой одного сохранения игры
internal class SaveManager
{
    // Уникальный код сохранения (используется в имени файла)
    private string _uniqueCode;
    // Количество сделанных ходов
    private int _moveCount;
    // Имена игроков
    private string[]? _players;
    // Кто ходит первым (0 или 1)
    private int _firstMove;
    // Текущее состояние игрового поля
    private int[,]? _matrix;

    // Путь к текущему файлу сохранения
    private string? _saveFilePath;
    // Объект для работы с файлами сохранений
    private readonly SaveFileManager _fileManager;

    // Публичные свойства для чтения из внешнего кода
    public string UniqueCode => _uniqueCode;
    public int MoveCount => _moveCount;
    public string[]? Players => _players;
    public int FirstMove => _firstMove;
    public int[,]? Matrix => _matrix;
    
    // Не публичные свойства, для чтения внутри файла
    internal string? SaveFilePath => _saveFilePath;

    // Конструктор создает менеджеры для файлов и кодов, генерирует первый код
    public SaveManager()
    {
        _fileManager = new SaveFileManager();
        _uniqueCode = UniqueCodeGenerator.Generate();
    }
    
    // Внутренний конструктор для загрузки из файла
    internal SaveManager(string uniqueCode, int moveCount, string[] players, int firstMove, int[,] matrix, string? saveFilePath)
    {
        _fileManager = new SaveFileManager();
        _uniqueCode = uniqueCode;
        _moveCount = moveCount;
        _players = players;
        _firstMove = firstMove;
        _matrix = matrix;
        _saveFilePath = saveFilePath;
    }

    // Инициализация новой партии и моментальное сохранение стартового состояния
    public void Start(int[,] matrix, string[] players, int firstMove)
    {
        _matrix = matrix;
        _moveCount = 0;
        _players = players;
        _firstMove = firstMove;
        PerformSave();
    }

    // Сохранение нового состояния матрицы (очередной ход)
    public void SaveMatrix(int[,] matrix)
    {
        _matrix = matrix;
        _moveCount += 1;      // Увеличиваем счетчик ходов
        PerformSave();
    }

    // Отметить партию как выигранную и сохранить статистику
    public void Win()
    {
        // Определяем победителя
        if (_players is null || _players.Length < 2)
            throw new InvalidOperationException("Невозможно определить победителя: не заданы имена игроков");

        string winner = _players[(_firstMove + _moveCount + 1) % 2];
        
        // Обновляем статистику в отдельном файле
        GameStatistics.UpdateStatistics(winner, _moveCount);
        
        // Удаляем сохранение, так как игра завершена
        _fileManager.DeleteOldFile(_saveFilePath);
    }
    
    // Статический метод для получения всех сохранений из папки
    public static SaveManager[] GetAllSaves()
    {
        var fileManager = new SaveFileManager();
        return fileManager.LoadAll();
    }
    
    // Статический метод получения топ-счетов
    public static string[] GetTopScores()
    {
        var stats = GameStatistics.Load();
        
        // Возвращаем массив строк
        return
        [
            "Игрок с самым большим количеством побед", stats.GetBestPlayer(),
            "Самая длинная игра", stats.LongestGame.HasValue ? stats.LongestGame.Value.ToString() : "Не определена",
            "Самая короткая игра", stats.ShortestGame.HasValue ? stats.ShortestGame.Value.ToString() : "Не определена"
        ];
    }
    
    // Общий метод сохранения: создает новый файл и удаляет старый
    private void PerformSave()
    {
        // Запоминаем старый файл, чтобы удалить его после успешного сохранения
        string? oldFile = SaveFilePath;

        // Код обновляется здесь, поэтому имя файла всегда новое
        _uniqueCode = UniqueCodeGenerator.Generate();
        _saveFilePath = _fileManager.BuildFilePath(_uniqueCode);

        // Сохраняем текущее состояние в новый файл
        _fileManager.Save(this, _saveFilePath);

        // Удаляем старый файл (если он был)
        _fileManager.DeleteOldFile(oldFile);
    }
}

// Генератор уникальных кодов
internal static class UniqueCodeGenerator
{
    // Генерирует строку по текущему времени в формате ГГГГММДДЧЧММССМММ
    public static string Generate() => DateTime.UtcNow.ToString("yyyyMMddHHmmssfff");
}

// Класс с путями папок
internal static class Paths
{
    // Сохраняем базовую директорию
    public static string BaseDirectory => AppDomain.CurrentDomain.BaseDirectory;
    
    // Сохраняем директорию для сохранений
    public static string SavesFolder => Path.Combine(BaseDirectory, "Saves");
    
    // Путь к файлу со статистикой
    public static string StatisticsPath => Path.Combine(BaseDirectory, "top-scores.json");
}

// Класс для работы с файлами сохранений (создание, загрузка, удаление)
internal class SaveFileManager
{
    // Формирует полный путь к файлу по уникальному коду
    public string BuildFilePath(string uniqueCode) => Path.Combine(Paths.SavesFolder, $"{uniqueCode}.json");

    // Сохраняет состояние SaveManager в файл по указанному пути
    public void Save(SaveManager manager, string path)
    {
        // Гарантируем, что папка существует
        Directory.CreateDirectory(Path.GetDirectoryName(path) ?? Paths.SavesFolder);

        // Конвертируем SaveManager в DTO для кодирования
        var converter = new SaveDataConverter();
        var saveData = converter.ToSaveData(manager);

        // Кодируем DTO в JSON и пишем в файл
        var serializer = new JsonFileSerializer();
        serializer.Serialize(saveData, path);
    }   

    // Удаляет старый файл, если путь рабочий и файл существует
    public void DeleteOldFile(string? oldFilePath)
    {
        if (!string.IsNullOrEmpty(oldFilePath) && File.Exists(oldFilePath))
            File.Delete(oldFilePath);
    }

    // Загружает все сохранения из папки и сортирует их по уникальному коду (по времени последнего сохранения)
    public SaveManager[] LoadAll()
    {
        // Создаем папку, если ее нет, чтобы избежать ошибок при GetFiles
        Directory.CreateDirectory(Paths.SavesFolder);

        // Берем все JSON-файлы сохранений
        var files = Directory.GetFiles(Paths.SavesFolder, "*.json");
        var converter = new SaveDataConverter();
        var serializer = new JsonFileSerializer();
        
        List<SaveManager> result = new List<SaveManager>();
        
        foreach (var file in files)
        {
            // Пытаемся загрузить каждый файл, игнорируя битые
            var manager = TryLoadFile(file, serializer, converter);
            if (manager is not null)
                result.Add(manager);
        }
        
        // Возвращаем отсортированный массив
        return SortByUniqueCode(result);
    }

    // Пытается загрузить один файл; при ошибке возвращает null
    private SaveManager? TryLoadFile(string filePath, JsonFileSerializer serializer, SaveDataConverter converter)    
    {
        try
        {
            var saveData = serializer.Deserialize(filePath);
            return saveData is not null ? converter.ToSaveManager(saveData) : null;
        }
        catch (Exception ex)
        {
            // Switch-expression для классификации ошибки
            var errorType = ex switch
            {
                JsonException => "JSON_ERROR",               // Проблема с форматом JSON
                IOException => "FILE_ERROR",                  // Проблема чтения файла
                UnauthorizedAccessException => "ACCESS_ERROR",// Нет прав
                InvalidDataException => "SAVE_DATA_ERROR",    // В файле не корректные данные
                _ => "UNKNOWN_ERROR"                          // Любая другая ошибка
            };
            
            // Добавляем строку с ошибкой в лог
            File.AppendAllText(
                Path.Combine(Paths.BaseDirectory, "error.log"),
                $"{DateTime.Now:yyyy-MM-dd HH:mm:ss:fff}\t{filePath}\t{errorType}\t{ex.Message}"
                + Environment.NewLine);
            
            return null;
        }
    }

    // Сортирует сохранения по уникальному коду по убыванию (новые выше)
    private SaveManager[] SortByUniqueCode(List<SaveManager> managers)
    {
        return managers.OrderByDescending(sm => sm.UniqueCode).ToArray();
    }
}

// Кодировщик JSON в/из файла
internal class JsonFileSerializer
{
    // Кодирует DTO SaveData в JSON и записывает в файл
    public void Serialize(SaveData data, string path)
    {
        string json = JsonSerializer.Serialize(data);
        File.WriteAllText(path, json);
    }

    // Читает JSON из файла и декодирует в SaveData
    public SaveData? Deserialize(string path)
    {
        string json = File.ReadAllText(path);
        return JsonSerializer.Deserialize<SaveData>(json);
    }
}

// Конвертер между SaveManager (домашняя модель) и SaveData (DTO для кодирования)
internal class SaveDataConverter
{
    // Отдельный конвертер для матриц
    private readonly MatrixConverter _matrixConverter = new();

    // Перекладывает данные из SaveManager в SaveData
    public SaveData ToSaveData(SaveManager manager)
    {
        return new SaveData
        {
            UniqueCode = manager.UniqueCode,
            MoveCount = manager.MoveCount,
            Players = manager.Players,
            FirstMove = manager.FirstMove,
            // Многомерный массив нельзя напрямую кодировать, поэтому конвертация в ступенчатый
            Matrix = _matrixConverter.ToJagged(manager.Matrix),
            SaveFilePath = manager.SaveFilePath
        };
    }

    // Восстанавливает SaveManager из DTO
    public SaveManager ToSaveManager(SaveData data)
    {
        return new SaveManager(
            data.UniqueCode ?? throw new InvalidDataException("Save file is missing 'UniqueCode' field"),
            data.MoveCount,
            data.Players ?? throw new InvalidDataException("Save file is missing 'Players' field"),
            data.FirstMove,
            _matrixConverter.ToMultidimensional(data.Matrix) ?? throw new InvalidDataException("Save file is missing 'Matrix' field"),
            data.SaveFilePath ?? throw new InvalidDataException("Save file is missing 'SaveFilePath' field")
        );
    }
}

// Конвертер матриц между многомерным массивом и ступенчатым (jagged)
internal class MatrixConverter
{
    // Переводит int[,] в int[][]
    public int[][]? ToJagged(int[,]? matrix)
    {
        if (matrix is null) return null;
        
        int rows = matrix.GetLength(0);
        int cols = matrix.GetLength(1);
        int[][] jagged = new int[rows][];
        
        for (int i = 0; i < rows; i++)
        {
            jagged[i] = new int[cols];
            for (int j = 0; j < cols; j++)
                jagged[i][j] = matrix[i, j];   // Копируем каждый элемент
        }
        
        return jagged;
    }

    // Переводит int[][] обратно в int[,]
    public int[,]? ToMultidimensional(int[][]? jagged)
    {
        if (jagged is null) return null;
        
        int rows = jagged.Length;
        // Предполагается, что все строки одинаковой длины; берем длину первой
        int cols = jagged.Length > 0 ? jagged[0].Length : 0;
        int[,] matrix = new int[rows, cols];
        
        for (int i = 0; i < rows; i++)
        {
            for (int j = 0; j < cols; j++)
                matrix[i, j] = jagged[i][j];   // Копируем обратно
        }
        
        return matrix;
    }
}

// DTO для кодирования сохранений (структура данных, которая уходит в JSON)
internal class SaveData
{
    // Уникальный код сохранения (часть имени файла)
    public string? UniqueCode { get; init; }
    // Количество ходов
    public int MoveCount { get; init; }
    // Имена игроков
    public string[]? Players { get; init; }
    // Кто ходит первым
    public int FirstMove { get; init; }
    // Игровое поле в виде ступенчатого массива (удобно для JSON)
    public int[][]? Matrix { get; init; }
    // Путь до файла, что бы удалять старые сохранения
    public string? SaveFilePath { get; init; }
}

// Класс для работы со статистикой игр
internal class GameStatistics
{
    // Словарь с количеством побед каждого игрока
    public Dictionary<string, int> PlayerWins { get; init; } = new();
    
    // Самая короткая игра (количество ходов)
    public int? ShortestGame { get; set; }
    
    // Самая длинная игра (количество ходов)
    public int? LongestGame { get; set; }

    // Загрузка статистики из файла
    public static GameStatistics Load()
    {
        if (!File.Exists(Paths.StatisticsPath))
            return new GameStatistics();

        try
        {
            string json = File.ReadAllText(Paths.StatisticsPath);
            return JsonSerializer.Deserialize<GameStatistics>(json) ?? new GameStatistics();
        }
        catch
        {
            // Если файл поврежден, возвращаем новую статистику
            return new GameStatistics();
        }
    }

    // Сохранение статистики в файл
    public void Save()
    {
        var options = new JsonSerializerOptions { WriteIndented = true };
        string json = JsonSerializer.Serialize(this, options);
        File.WriteAllText(Paths.StatisticsPath, json);
    }

    // Обновление статистики после победы
    public static void UpdateStatistics(string winner, int moveCount)
    {
        var stats = Load();

        // Обновляем количество побед игрока
        if (!stats.PlayerWins.TryAdd(winner, 1))
            stats.PlayerWins[winner]++;

        // Обновляем самую короткую игру
        if (!stats.ShortestGame.HasValue || moveCount < stats.ShortestGame.Value)
            stats.ShortestGame = moveCount;

        // Обновляем самую длинную игру
        if (!stats.LongestGame.HasValue || moveCount > stats.LongestGame.Value)
            stats.LongestGame = moveCount;

        stats.Save();
    }

    // Получение игрока с наибольшим количеством побед
    public string GetBestPlayer()
    {
        if (PlayerWins.Count == 0)
            return "Не определён";

        var maxWins = PlayerWins.Max(p => p.Value);
        var bestPlayers = PlayerWins.Where(p => p.Value == maxWins).Select(p => p.Key).ToArray();

        return bestPlayers.Length == 1 ? bestPlayers[0] : "Не определён";
    }
}
