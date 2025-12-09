namespace Breakthrough;

internal enum Winner // Перечисление описывающее, кто победил в игре
{
    None,   // Никто не победил, игра продолжается
    White,  // Победили белые
    Black   // Победили черные
}

internal static class Core // Статический класс с ядром игровой логики, можно поменять на другой и будет совершенно другая игра
{
    // Свойство для определения чья очередь (на основе данных SaveManager)
    internal static bool IsWhiteTurn(SaveManager saveManager) => (saveManager.MoveCount + saveManager.FirstMove) % 2 == 0; 

    // Проверка доступных ходов для конкретной пешки
    internal static int[,] GetAvailableMovesForPawn(SaveManager saveManager, int row, int col)
    {
        int[,] matrix = saveManager.Matrix ?? throw new InvalidOperationException("Матрица не инициализирована"); // Копируем матрицу, если она не пустой объект, иначе кидаем исключение
        int height = matrix.GetLength(0), width = matrix.GetLength(1); // Получаем ширину и высоту соответственно
        int pawnType = matrix[row, col]; // Получаем элемент, который стоит на переданных координатах
        bool isWhite = pawnType == Objects.WhitePawn; // Сравниваем его с белой пешкой
        
        if (isWhite != IsWhiteTurn(saveManager)) // Проверяем, соответствует ли пешка текущему ходу
            return new int[0, 2]; // Возвращаем пустой массив
        
        int direction = isWhite ? -1 : 1; // Определяем направление движения (белые вверх [-1], черные вниз [+1])
        int[] colOffsets = [-1, 0, 1]; // Возможные смещения по столбцу: влево (-1), прямо (0), вправо (+1)
        
        var moves = colOffsets // LINQ для вычисления всех допустимых ходов
            .Select(offset => (row: row + direction, col: col + offset, colOffset: offset)) // Проходимся по всем позициям как по числовым значениям
            .Where(pos => IsWithinBounds(pos.row, pos.col, height, width)) // Фильтруем только те ходы, которые могут попасть на доску
            .Where(pos => IsValidMove(matrix, pos.row, pos.col, pos.colOffset, isWhite)) // Фильтруем ходы по правилам игры
            .Select(pos => (pos.row, pos.col)) // Переводим исключительно в координаты
            .ToArray();

        return ConvertToMatrix(moves); // Переводим в матрицу [N, 2] с координатами
    }

    // Проверка, что координаты хода находятся в пределах доски
    private static bool IsWithinBounds(int row, int col, int height, int width) => row >= 0 && row < height && col >= 0 && col < width;
    
    // Проверка корректности хода по правилам игры
    private static bool IsValidMove(int[,] matrix, int newRow, int newCol, int colOffset, bool isWhite)
    {
        int targetCell = matrix[newRow, newCol]; // Смотрим, что стоит по желаемым координатам
    
        // Прямо можно только на пустую клетку
        if (colOffset == 0)
            return targetCell == Objects.Space;
    
        // По диагонали - на вражескую пешку или пустую клетку
        return targetCell == Objects.Space || (isWhite && targetCell == Objects.BlackPawn) || (!isWhite && targetCell == Objects.WhitePawn);
    }

    // Вспомогательный метод для конвертации кортежа в матрицу
    private static int[,] ConvertToMatrix((int row, int col)[] moves)
    {
        int[,] result = new int[moves.Length, 2]; // Инициализируем пустой массив соответствующий количеству ходов
        for (int i = 0; i < moves.Length; i++) // Каждой паре в двумерном массиве (строке в матрице) 
        {
            result[i, 0] = moves[i].row; // строка
            result[i, 1] = moves[i].col; // столбец
        }
        return result;
    }

    // Применение хода и определение результата (победа или продолжение)
    internal static GameResult MakeMove(SaveManager saveManager, int fromRow, int fromCol, int toRow, int toCol)
    {
        int[,] currentMatrix = saveManager.Matrix ?? throw new InvalidOperationException("Матрица не загружена"); // Кидаем ошибку, если матрица не определена

        // 1. Применяем ход
        int[,] newMatrix = (int[,])currentMatrix.Clone(); // Клонируем матрицу
        int pawn = newMatrix[fromRow, fromCol]; // Копируем значение пешки, на которую применяется ход
        newMatrix[fromRow, fromCol] = Objects.Space;// Старое положение становится пустым
        newMatrix[toRow, toCol] = pawn; // Новое положение занимает пешка
        
        saveManager.SaveMatrix(newMatrix); // 2. Сохраняем (ход валиден, матрица обновлена)
        
        Winner winner = CheckReachEndCondition(newMatrix, toRow, pawn); // 3. Проверяем кто дошёл до края и выдаём значение через коллекцию Winner
        
        if (winner == Winner.None) winner = CheckWipeoutCondition(newMatrix); // Если никто не дошел до края, проверяем, не съели ли всех
        
        if (winner != Winner.None) // Если кто-то победил, то фиксируем результат
        {
            saveManager.Win(); // Обращаемся к SaveManager, что бы он закончил игру на файловом уровне
            string msg = $"{(winner == Winner.White ? "Белые" : "Чёрные")} победили!"; // Формируем сообщение с информацией кто победил
            return new GameResult { IsGameOver = true, Message = msg }; // Возвращаем сообщение
        }

        // Игра продолжается
        return new GameResult { IsGameOver = false };
    }


    // Метод проверяет, дошла ли пешка до края
    private static Winner CheckReachEndCondition(int[,] matrix, int movedRow, int pawnType)
    {
        int height = matrix.GetLength(0); // Получаем высоту доски
        if (pawnType == Objects.WhitePawn && movedRow == 0) // Если пешка белая и дошла до верхней точки - победа белых
            return Winner.White;

        if (pawnType == Objects.BlackPawn && movedRow == height - 1) // Если чёрная дошла до низа - чёрных
            return Winner.Black;

        return Winner.None; // В ином случае никто не победил, так как никто не дошёл до края
    }

    // Метод проверяет наличие фигур на доске (на случай полного уничтожения)
    private static Winner CheckWipeoutCondition(int[,] matrix)
    {
        int whiteCount = matrix.Cast<int>().Count(cell => cell == Objects.WhitePawn); // Получаем количество белых пешек
        int blackCount = matrix.Cast<int>().Count(cell => cell == Objects.BlackPawn); // Получаем количество чёрных пешек
        
        return blackCount == 0 ? Winner.White : whiteCount == 0 ? Winner.Black : Winner.None; // Если у чёрных не кончились пешки, проверяем наличие белых и возвращаем ответ
    }

    // Создание начальной матрицы
    public static int[,] CreateMatrix(int height, int width)
    {
        int[,] matrix = new int[height, width]; // Расчерчиваем матрицу

        for (int i = 0; i < height; i++) 
        {
            for (int j = 0; j < width; j++)
            {
                if (i <= 1) matrix[i, j] = Objects.BlackPawn; // Если верхние два ряда - чёрная пешка
                if (i >= height - 2) matrix[i, j] = Objects.WhitePawn; // Если нижние два ряда - белая
            }
        }

        return matrix;
    }

    // Получение списка координат всех пешек (для навигации в UI)
    internal static int[,] GetPawnCoordinates(int[,] matrix)
    {
        int rows = matrix.GetLength(0);
        int cols = matrix.GetLength(1);
    
        // Собираем координаты одним выражением
        var coords = Enumerable.Range(0, rows) // Проходимся по всем строкам
            .SelectMany(row => Enumerable.Range(0, cols) // Проходимся по всем столбцам
                .Where(col => matrix[row, col] != Objects.Space) // Смотрим, где есть пешки
                .Select(col => (row, col))) // Строим кортеж координат
            .ToArray(); // Преобразуем кортежи в массив
    
        // Преобразуем в двумерный массив для отправки в UI
        int[,] result = new int[coords.Length, 2]; // Создаём массив с высотой 2
        for (int i = 0; i < coords.Length; i++) (result[i, 0], result[i, 1]) = coords[i]; // Проходясь по каждой паре раскладываем её внутри массива
        return result;
    }

    // Структура результата применения хода
    internal struct GameResult
    {
        public bool IsGameOver; // Флаг, завершена ли игра
        public string Message; // Сообщение для пользователя (победа)
    }
}
