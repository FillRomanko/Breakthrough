using System.Globalization;
using System.Text.Json;

namespace Breakthrough
{
    // Управляет логикой одного сохранения игры
    public class SaveManager
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
        // Флаг, что партия завершилась победой
        private bool _isWin;

        // Путь к текущему файлу сохранения
        private string? _saveFilePath;
        // Объект для работы с файлами сохранений
        private readonly SaveFileManager _fileManager;
        // Генератор уникальных кодов
        private readonly UniqueCodeGenerator _codeGenerator;

        // Публичные свойства для чтения из внешнего кода
        public string UniqueCode => _uniqueCode;
        public int MoveCount => _moveCount;
        public string[]? Players => _players;
        public int FirstMove => _firstMove;
        public int[,]? Matrix => _matrix;
        public bool IsWin => _isWin;

        // Конструктор создает менеджеры для файлов и кодов и генерирует первый код
        public SaveManager()
        {
            _fileManager = new SaveFileManager();
            _codeGenerator = new UniqueCodeGenerator();
            _uniqueCode = _codeGenerator.Generate();
        }

        // Инициализация новой партии и моментальное сохранение стартового состояния
        public void Start(int[,] matrix, string[] players, int firstMove)
        {
            _matrix = matrix;
            _moveCount = 0;
            _players = players;
            _firstMove = firstMove;
            _isWin = false;
            PerformSave();
        }

        // Сохранение нового состояния матрицы (очередной ход)
        public void SaveMatrix(int[,] matrix)
        {
            _matrix = matrix;
            _moveCount += 1;      // Увеличиваем счетчик ходов
            PerformSave();
        }

        // Отметить партию как выигранную и сохранить её снова
        public void Win()
        {
            _isWin = true;
            PerformSave();
            // сохраняю здесь, что бы top-scores всегда имел top-scores, хотя на деле это нужно только для доп баллов по сохранению
            UpdateTopScores(); 
        }
        
        // Статический метод для получения всех сохранений из папки
        public static SaveManager[] GetAllSaves()
        {
            var fileManager = new SaveFileManager();
            return fileManager.LoadAll();
        }
        
        // Статический метод получения top-scores из файла
        public static string[] GetTopScores()
        {
            // Перед чтением всегда обновляем файл, это обеспечивает его существование и актуальность
            UpdateTopScores();
            
            // Возвращаем содержимое файла
            return File.ReadAllLines(Paths.TopScoresPath);
        }
        
        
        // Общий метод сохранения: создает новый файл и удаляет старый
        private void PerformSave()
        {
            // Запоминаем старый файл, чтобы удалить его после успешного сохранения
            string? oldFile = _saveFilePath;

            // Важно: код обновляется здесь, поэтому имя файла всегда новое
            _uniqueCode = _codeGenerator.Generate();
            _saveFilePath = _fileManager.BuildFilePath(_uniqueCode);

            // Сохраняем текущее состояние в новый файл
            _fileManager.Save(this, _saveFilePath);

            // Удаляем старый файл (если он был)
            _fileManager.DeleteOldFile(oldFile);
        }

        // Статический метод, обновляющий файл top-scores 
        private static void UpdateTopScores()
        {
            // Получаем все сохранения и берём из них те, где игра закончена
            TopScores gameData = new TopScores(GetAllSaves().Where(s => s.IsWin));
            
            // Создаём массив строк, в который сразу кладём нужные значения
            string[] topScores =
            [
                "Игрок с самым большим количеством побед", gameData.BestPlayer, 
                "Самая длинная игра", gameData.LongestGame, 
                "Самая короткая игра", gameData.ShortestGame
            ];
            
            // Записываем получившийся массив в нужный файл
            File.WriteAllLines(Paths.TopScoresPath, topScores);
        }
    }

    // Генератор уникальных кодов
    internal class UniqueCodeGenerator
    {
        // Генерирует строку по текущему времени в формате ГГГГММДДЧЧММССМММ
        public string Generate() => DateTime.UtcNow.ToString("yyyyMMddHHmmssfff");
    }
    
    // Класс с путями папок
    internal static class Paths
    {
        // Сохраняем базовую директорию
        public static string BaseDirectory => AppDomain.CurrentDomain.BaseDirectory;
        
        // Сохраняем директорию для сохранений
        public static string SavesFolder => Path.Combine(BaseDirectory, "Saves");
        
        // Сохраняем директорию для top-scores
        public static string TopScoresPath => Path.Combine(BaseDirectory, "top-scores.txt");
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

        // Удаляет старый файл, если путь валидный и файл существует
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
                if (manager != null)
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
                EnsureFileNotModifiedExternally(filePath);
                return saveData != null ? converter.ToSaveManager(saveData) : null;
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
        
        // Защита от изменений
        private void EnsureFileNotModifiedExternally(string filePath)
        {
            // сохраняем имя файла - уникальный код без вскрытия файла
            var filename = Path.GetFileNameWithoutExtension(filePath);
            // Проверяем, что имя файла можно преобразовать в DateTime формат
            if (DateTime.TryParseExact(filename, "yyyyMMddHHmmssfff",
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.None, out var fromName))
            {
                // Сохраняем время последнего изменения файла
                var fromFs = File.GetLastWriteTimeUtc(filePath);
                // Считаем разницу между последним изменением файла и его кодом
                var diff = (fromFs - fromName).Duration();
                
                // Если разница больше 100 ms, то кидаем ошибку с комментарием, что время изменения и код не совпадают
                if (diff > TimeSpan.FromMilliseconds(100)) throw new InvalidDataException("FileWriteAndCodeDoNotMatch");
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
                IsWin = manager.IsWin,
                // Многомерный массив нельзя напрямую кодировать, поэтому конвертация в ступенчатый
                Matrix = _matrixConverter.ToJagged(manager.Matrix)
            };
        }

        // Восстанавливает SaveManager из DTO
        public SaveManager ToSaveManager(SaveData data)
        {
            // Создаем новый SaveManager. Его конструктор инициализирует свои поля,
            // затем ниже приватные поля переписываются через reflection.
            var manager = new SaveManager();

            // Reflection: выставляем приватные поля напрямую, обходя публичный API
            SetPrivateField(manager, "_uniqueCode", data.UniqueCode ?? throw new InvalidDataException("Save file is missing 'UniqueCode' field"));
            SetPrivateField(manager, "_moveCount", data.MoveCount);
            SetPrivateField(manager, "_players", data.Players ?? throw new InvalidDataException("Save file is missing 'Players' field"));
            SetPrivateField(manager, "_firstMove", data.FirstMove);
            SetPrivateField(manager, "_matrix", _matrixConverter.ToMultidimensional(data.Matrix) ?? throw new InvalidDataException("Save file is missing 'Matrix' field"));
            SetPrivateField(manager, "_isWin", data.IsWin);
            
            
            return manager;
        }

        // Универсальный метод для установки приватного поля через reflection
        private void SetPrivateField(object obj, string fieldName, object value)
        {
            var field = obj.GetType().GetField(
                fieldName,
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            // Если поле найдено, записываем значение
            field?.SetValue(obj, value);
        }
    }

    // Конвертер матриц между многомерным массивом и ступенчатым (jagged)
    internal class MatrixConverter
    {
        // Переводит int[,] в int[][]
        public int[][]? ToJagged(int[,]? matrix)
        {
            if (matrix == null) return null;
            
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
            if (jagged == null) return null;
            
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

    // DTO для кодирования (структура данных, которая уходит в JSON)
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
        // Флаг завершения партии победой
        public bool IsWin { get; init; }
    }

    // Класс находящий top-scores
    internal class TopScores
    {

        // Внутренний класс с конкретными значениями, что бы IDE не пытался найти очередной bool, а также работать только с нужными значениями
        private class OnlyStats(string winner, int moveCount)
        {
            // Имя победителя
            public readonly string Winner = winner;
            
            // Количество ходов
            public readonly int MoveCount = moveCount;
            
        }
        
        // Список законченных игр
        readonly IEnumerable<OnlyStats> _endedGames;

        // Длительность кратчайшей игры
        private readonly int _shortestGame;
        
        // Длительность длиннейшей игры
        private readonly int _longestGame;
        
        
        // Публичные свойства для чтения при перезаписи
        public string BestPlayer { get; }
        public string ShortestGame => _shortestGame < int.MaxValue ? _shortestGame.ToString() : "Не определена";
        public string LongestGame => int.MinValue < _longestGame ? _longestGame.ToString() : "Не определена";
        
        // Конструктор, что бы с помощью экземпляра сразу заполнить 
        public TopScores(IEnumerable<SaveManager> endedGames)
        {
            // создаём лист экземпляров OnlyScores не учитывая все null значения
            _endedGames = endedGames
                .Select(g =>
                {
                    // проверка того, что массив имён существует и записываем их в OnlyStats, в ином же случае создаём экземпляр с игнорируемыми значениями
                    if (g.Players == null) return new OnlyStats("", int.MaxValue);
                    var winner = g.Players[(g.FirstMove + g.MoveCount + 1) % 2];
                    return new OnlyStats(winner, g.MoveCount);
                })
                // Проверяем, что бы у победителя было имя (исключая даже те случаи, когда списка имён вообще не было) 
                .Where(s => !string.IsNullOrEmpty(s.Winner))
                .ToList();
            
            // Внутри конструктора определяем все возвращаемые значения
            BestPlayer = FindBestPlayer();
            _shortestGame = FindShortestGame();
            _longestGame = FindLongestGame();
        }

        // Находим лучшего игрока
        private string FindBestPlayer()
        {
            // Проверяем наличие законченных игр
            if (!_endedGames.Any())
                return "Не определён";
            
            // Составляем список игроков
            var players = _endedGames.Select(s => s.Winner).ToArray();

            // Создаём список с победами
            var wins = players
                .GroupBy(p => p)
                .Select(g => new { Player = g.Key, Wins = g.Count() })
                .ToList();
            
            // Получаем наибольшее количество упоминаний
            int max = wins.Max(g => g.Wins);
            
            // Выводим список тех, у кого максимальное количество упоминаний
            var bestPlayers = wins
                .Where(g => g.Wins == max)
                .Select(g => g.Player)
                .ToArray();
            
            // Если в массиве только 1 игрок, то сохраняем его как лучшего, иначе считаем, что легенда ещё не появилась
            return bestPlayers.Length == 1 ? bestPlayers[0] : "Не определён";
        }

        // Находим кратчайшую игру
        private int FindShortestGame()
        {
            // Проверяем наличие законченных игр
            if (!_endedGames.Any()) return int.MaxValue;
            
            // Возвращаем минимальное значение
            return _endedGames.Min(g => g.MoveCount);
        }

        // Находим длиннейшую игру
        private int FindLongestGame()
        {
            // Проверяем наличие законченных игр
            if (!_endedGames.Any()) return int.MinValue;

            // Возвращаем максимальное значение
            return _endedGames.Max(g => g.MoveCount);
        }
    }
}
