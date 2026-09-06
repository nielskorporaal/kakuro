namespace KakuroSolver.Core;

// A kakuro grid consists of cells in a grid size N x M. Cells can either be a clue cell or a puzzle cell. 
// A clue cell can contain two values (one for horizontal clues, the other for vertical clues)
// A clue cell horizontal clue value indicates the total of the directly neighbouring cells next to the clue cell that are not a border or another clue cell
// A clue cell vertical clue value indicates the total of the directly neighbouring vertical cells below the clue cell that are PuzzleCells and not another clue cell.

public sealed record KakuroGrid(Cell[][] Cells, IReadOnlyList<KakuroRun> KakuroRuns)
{
    public int N => Cells.Length;
    public int M => Cells.Length == 0 ? 0: Cells[0].Length;
}

public abstract record Cell;

public sealed record ClueCell(int? HorizontalClue, int? VerticalClue) : Cell;

public sealed record PuzzleCell(int? Value = null) : Cell;

public sealed record EmptyCell : Cell;

public sealed record KakuroRun(int Target, IReadOnlyList<(int Row, int Column)> Cells);
