using System.Globalization;

namespace KakuroSolver;

public static class KakuroParser
{
    public static KakuroGrid Parse(string text)
    {
        ArgumentNullException.ThrowIfNull(text);

        var rows = new List<Cell[]>();
        var lines = text.Split('\n');

        for (var lineIndex = 0; lineIndex < lines.Length; lineIndex++)
        {
            var line = lines[lineIndex].Trim();
            if (line.Length == 0)
            {
                continue;
            }

            var tokens = line.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
            var cells = new Cell[tokens.Length];

            for (var column = 0; column < tokens.Length; column++)
            {
                cells[column] = ParseCell(tokens[column], lineIndex + 1, column + 1);
            }

            rows.Add(cells);
        }

        if (rows.Count == 0)
        {
            throw new FormatException("The Kakuro input does not contain any rows.");
        }

        var columnCount = rows[0].Length;
        if (columnCount == 0 || rows.Any(row => row.Length != columnCount))
        {
            throw new FormatException("The Kakuro input must be a non-empty rectangular grid.");
        }

        var cellsGrid = rows.ToArray();
        var runs = BuildRuns(cellsGrid);
        return new KakuroGrid(cellsGrid, runs);
    }

    public static KakuroGrid ParseFile(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        return Parse(File.ReadAllText(path));
    }

    private static Cell ParseCell(string token, int line, int column)
    {
        if (token == "#")
        {
            return new EmptyCell();
        }

        if (token == ".")
        {
            return new PuzzleCell();
        }

        var separator = token.IndexOf('\\');
        if (separator < 0 || separator != token.LastIndexOf('\\'))
        {
            throw InvalidToken(token, line, column);
        }

        var verticalText = token[..separator];
        var horizontalText = token[(separator + 1)..];
        if (verticalText.Length == 0 && horizontalText.Length == 0)
        {
            throw InvalidToken(token, line, column);
        }

        int? vertical = ParseClueValue(verticalText, token, line, column);
        int? horizontal = ParseClueValue(horizontalText, token, line, column);
        return new ClueCell(horizontal, vertical);
    }

    private static int? ParseClueValue(string text, string token, int line, int column)
    {
        if (text.Length == 0)
        {
            return null;
        }

        if (!int.TryParse(text, NumberStyles.None, CultureInfo.InvariantCulture, out var value) || value <= 0)
        {
            throw InvalidToken(token, line, column);
        }

        return value;
    }

    private static List<KakuroRun> BuildRuns(Cell[][] grid)
    {
        var runs = new List<KakuroRun>();

        for (var row = 0; row < grid.Length; row++)
        {
            for (var column = 0; column < grid[row].Length; column++)
            {
                if (grid[row][column] is not ClueCell clue)
                {
                    continue;
                }

                if (clue.HorizontalClue is int horizontalTarget)
                {
                    runs.Add(new KakuroRun(
                        horizontalTarget,
                        CollectPuzzleCells(grid, row, column, 0, 1, row, column)));
                }

                if (clue.VerticalClue is int verticalTarget)
                {
                    runs.Add(new KakuroRun(
                        verticalTarget,
                        CollectPuzzleCells(grid, row, column, 1, 0, row, column)));
                }
            }
        }

        return runs;
    }

    private static IReadOnlyList<(int Row, int Column)> CollectPuzzleCells(
        Cell[][] grid,
        int startRow,
        int startColumn,
        int rowStep,
        int columnStep,
        int clueRow,
        int clueColumn)
    {
        var cells = new List<(int Row, int Column)>();
        var row = startRow + rowStep;
        var column = startColumn + columnStep;

        while (row >= 0 && row < grid.Length && column >= 0 && column < grid[row].Length)
        {
            if (grid[row][column] is not PuzzleCell)
            {
                break;
            }

            cells.Add((row, column));
            row += rowStep;
            column += columnStep;
        }

        if (cells.Count == 0)
        {
            throw new FormatException(
                $"Clue at row {clueRow + 1}, column {clueColumn + 1} does not point to any puzzle cells.");
        }

        return cells;
    }

    private static FormatException InvalidToken(string token, int line, int column) =>
        new($"Invalid Kakuro token '{token}' at line {line}, column {column}.");
}