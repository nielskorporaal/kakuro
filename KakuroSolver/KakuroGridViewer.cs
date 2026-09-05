namespace KakuroSolver;

public static class KakuroGridViewer
{
    public static void Print(KakuroGrid grid, TextWriter writer)
    {
        ArgumentNullException.ThrowIfNull(grid);
        ArgumentNullException.ThrowIfNull(writer);

        var tokens = grid.Cells
            .Select(row => row.Select(FormatCell).ToArray())
            .ToArray();
        var columnWidths = Enumerable.Range(0, grid.M)
            .Select(column => tokens.Max(row => row[column].Length))
            .ToArray();
        var border = "+" + string.Join("+", columnWidths.Select(width => new string('-', width + 2))) + "+";

        writer.WriteLine(border);
        foreach (var row in tokens)
        {
            writer.WriteLine("| " + string.Join(" | ", row.Select((token, column) => token.PadRight(columnWidths[column]))) + " |");
            writer.WriteLine(border);
        }
    }

    public static void PrintCandidates(
        KakuroGrid grid,
        IReadOnlyDictionary<(int Row, int Column), IReadOnlySet<int>> candidates,
        TextWriter writer)
    {
        ArgumentNullException.ThrowIfNull(grid);
        ArgumentNullException.ThrowIfNull(candidates);
        ArgumentNullException.ThrowIfNull(writer);

        var tokens = grid.Cells
            .Select((row, rowIndex) => row.Select((cell, columnIndex) =>
                cell is PuzzleCell ? FormatCandidates(candidates[(rowIndex, columnIndex)]) : FormatCell(cell)).ToArray())
            .ToArray();
        var columnWidths = Enumerable.Range(0, grid.M)
            .Select(column => tokens.Max(row => row[column].Length))
            .ToArray();
        var border = "+" + string.Join("+", columnWidths.Select(width => new string('-', width + 2))) + "+";

        writer.WriteLine(border);
        foreach (var row in tokens)
        {
            writer.WriteLine("| " + string.Join(" | ", row.Select((token, column) => token.PadRight(columnWidths[column]))) + " |");
            writer.WriteLine(border);
        }
    }

    private static string FormatCell(Cell cell) => cell switch
    {
        EmptyCell => "#",
        PuzzleCell puzzleCell => puzzleCell.Value?.ToString() ?? ".",
        ClueCell clueCell => $"{clueCell.VerticalClue?.ToString() ?? string.Empty}\\{clueCell.HorizontalClue?.ToString() ?? string.Empty}",
        _ => throw new InvalidOperationException($"Unknown cell type: {cell.GetType().Name}")
    };

    private static string FormatCandidates(IReadOnlySet<int> values)
    {
        var digits = string.Concat(values.OrderBy(value => value));
        return values.Count == 1 ? digits : $"{{{digits}}}";
    }
}