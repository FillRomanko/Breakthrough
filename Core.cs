using System;
using System.Collections.Generic;

namespace Breakthrough;

public static class Core
{
    // Свойство для определения чья очередь (на основе данных SaveManager)
    public static bool IsWhiteTurn(SaveManager saveManager)
    {
        return (saveManager.MoveCount + saveManager.FirstMove) % 2 == 0;
    }

    // Проверка доступных ходов для конкретной пешки
    public static int[,] GetAvailableMovesForPawn(SaveManager saveManager, int row, int col)
    {
        int[,] matrix = saveManager.Matrix ?? throw new InvalidOperationException("Матрица не инициализирована");
        int height = matrix.GetLength(0);
        int width = matrix.GetLength(1);
        
        int pawnType = matrix[row, col];
        bool isWhite = pawnType == Objects.WhitePawn;
        
        // Проверяем, соответствует ли пешка текущему ходу
        if (isWhite != IsWhiteTurn(saveManager))
            return new int[0, 2];

        // Определяем направление движения (белые вверх [-1], черные вниз [+1])
        int direction = isWhite ? -1 : 1;
        
        List<(int, int)> moves = new List<(int, int)>();
        int[] colOffsets = { -1, 0, 1 };

        foreach (int colOffset in colOffsets)
        {
            int newRow = row + direction;
            int newCol = col + colOffset;

            // Проверка границ
            if (newRow >= 0 && newRow < height && newCol >= 0 && newCol < width)
            {
                int targetCell = matrix[newRow, newCol];

                // Прямо - только на пустую клетку
                if (colOffset == 0 && targetCell == Objects.Space)
                {
                    moves.Add((newRow, newCol));
                }
                // По диагонали - только на вражескую пешку
                else if (colOffset != 0)
                {
                    // Атака: если клетка занята врагом (пешки разных цветов)
                    if ((isWhite && targetCell == Objects.BlackPawn) ||
                        (!isWhite && targetCell == Objects.WhitePawn))
                    {
                         moves.Add((newRow, newCol));
                    }
                    // В некоторых вариациях правил Прорыва можно бить и на пустую клетку по диагонали?
                    // В классическом - нет, но в вашем коде было условие targetCell == Objects.Space.
                    // Оставляем логику вашего кода (движение по диагонали возможно и на пустую клетку):
                    else if (targetCell == Objects.Space)
                    {
                        moves.Add((newRow, newCol));
                    }
                }
            }
        }

        // Конвертируем в массив
        int[,] result = new int[moves.Count, 2];
        for (int i = 0; i < moves.Count; i++)
        {
            result[i, 0] = moves[i].Item1;
            result[i, 1] = moves[i].Item2;
        }

        return result;
    }

    // Выполнение хода с проверкой победы
    public static GameResult MakeMove(SaveManager saveManager, int fromRow, int fromCol, int toRow, int toCol)
    {
        int[,] currentMatrix = saveManager.Matrix ?? throw new InvalidOperationException("Матрица не загружена");
        
        // 1. Создаем копию матрицы и применяем ход
        int[,] newMatrix = (int[,])currentMatrix.Clone();
        
        int pawn = newMatrix[fromRow, fromCol];
        newMatrix[fromRow, fromCol] = Objects.Space;
        newMatrix[toRow, toCol] = pawn;

        // 2. Сохраняем новую матрицу (это увеличит счетчик ходов в SaveManager)
        saveManager.SaveMatrix(newMatrix);

        // 3. Проверяем условия победы или окончания игры
        try
        {
            CheckWinCondition(newMatrix, toRow, pawn);
            CheckPawnPresence(newMatrix);
            
            return new GameResult { IsGameOver = false };
        }
        catch (Exception ex)
        {
            // Если игра окончена (победа или ошибка), фиксируем победу
            saveManager.Win();
            return new GameResult { IsGameOver = true, Message = ex.Message };
        }
    }

    // Вспомогательный метод для проверки достижения края доски
    private static void CheckWinCondition(int[,] matrix, int newRow, int pawnType)
    {
        int height = matrix.GetLength(0);
        
        if (pawnType == Objects.WhitePawn && newRow == 0)
            throw new Exception("Белые победили! Пешка достигла края доски.");
            
        if (pawnType == Objects.BlackPawn && newRow == height - 1)
            throw new Exception("Чёрные победили! Пешка достигла края доски.");
    }

    // Вспомогательный метод для проверки наличия фигур
    private static void CheckPawnPresence(int[,] matrix)
    {
        int whiteCount = 0;
        int blackCount = 0;
        int height = matrix.GetLength(0);
        int width = matrix.GetLength(1);

        for (int i = 0; i < height; i++)
        {
            for (int j = 0; j < width; j++)
            {
                if (matrix[i, j] == Objects.WhitePawn) whiteCount++;
                else if (matrix[i, j] == Objects.BlackPawn) blackCount++;
            }
        }

        if (blackCount == 0) throw new Exception("Белые победили! У чёрных не осталось пешек.");
        if (whiteCount == 0) throw new Exception("Чёрные победили! У белых не осталось пешек.");
    }

    // Создание начальной матрицы
    public static int[,] CreateMatrix(int height, int width)
    {
        if (height <= 5 || width <= 3 || width >= 17 || height >= 17)
            throw new ArgumentException("Недопустимые размеры поля");

        int[,] matrix = new int[height, width];

        for (int i = 0; i < height; i++)
        {
            for (int j = 0; j < width; j++)
            {
                if (i == 0 || i == 1)
                    matrix[i, j] = Objects.BlackPawn;
                else if (i == height - 1 || i == height - 2)
                    matrix[i, j] = Objects.WhitePawn;
                else
                    matrix[i, j] = Objects.Space;
            }
        }

        return matrix;
    }

    // Получение списка координат всех пешек (для навигации в UI)
    public static int[,] GetPawnCoordinates(int[,] matrix)
    {
        if (matrix == null) return new int[0, 2];
        
        var list = new List<int[]>();
        int rows = matrix.GetLength(0);
        int cols = matrix.GetLength(1);

        for (int i = 0; i < rows; i++)
        {
            for (int j = 0; j < cols; j++)
            {
                if (matrix[i, j] != Objects.Space)
                {
                    list.Add(new int[] { i, j });
                }
            }
        }

        // Перевод в int[,]
        int[,] result = new int[list.Count, 2];
        for (int k = 0; k < list.Count; k++)
        {
            result[k, 0] = list[k][0];
            result[k, 1] = list[k][1];
        }
        return result;
    }

    public struct GameResult
    {
        public bool IsGameOver;
        public string Message;
    }
}
