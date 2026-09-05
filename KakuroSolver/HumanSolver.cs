namespace KakuroSolver;

public sealed record HumanDeduction(
    string Method,
    int RunTarget,
    string Direction,
    (int Row, int Column) Cell,
    IReadOnlySet<int> Before,
    IReadOnlySet<int> After,
    string Explanation);

public sealed class HumanSolver
{
    private readonly KakuroGrid grid;
    private readonly Dictionary<(int Row, int Column), HashSet<int>> candidates = new();

    public HumanSolver(KakuroGrid grid)
    {
        this.grid = grid ?? throw new ArgumentNullException(nameof(grid));

        for (var row = 0; row < grid.N; row++)
        {
            for (var column = 0; column < grid.M; column++)
            {
                if (grid.Cells[row][column] is PuzzleCell puzzleCell)
                {
                    candidates[(row, column)] = puzzleCell.Value is int value
                        ? new HashSet<int> { value }
                        : [1, 2, 3, 4, 5, 6, 7, 8, 9];
                }
            }
        }
    }

    public IReadOnlyDictionary<(int Row, int Column), IReadOnlySet<int>> Candidates =>
        candidates.ToDictionary(pair => pair.Key, pair => (IReadOnlySet<int>)pair.Value);

    public bool TryNext(out HumanDeduction? deduction)
    {
        foreach (var run in grid.KakuroRuns)
        {
            var assignments = GetPossibleAssignments(run);
            if (assignments.Count == 0)
            {
                throw new InvalidOperationException($"Run targeting {run.Target} has no possible assignments.");
            }

            for (var position = 0; position < run.Cells.Count; position++)
            {
                var supportedDigits = assignments
                    .Select(assignment => assignment[position])
                    .ToHashSet();
                var cell = candidates[run.Cells[position]];
                var before = cell.ToHashSet();
                cell.IntersectWith(supportedDigits);

                if (cell.Count == 0)
                {
                    throw new InvalidOperationException(
                        $"Cell at row {run.Cells[position].Row + 1}, column {run.Cells[position].Column + 1} has no candidates.");
                }

                if (!cell.SetEquals(before))
                {
                    var direction = run.Cells.Count > 1 && run.Cells[0].Row == run.Cells[1].Row
                        ? "horizontal"
                        : "vertical";
                    deduction = new HumanDeduction(
                        "Run combinations",
                        run.Target,
                        direction,
                        run.Cells[position],
                        before,
                        cell.ToHashSet(),
                        ExplainRunReduction(run, direction, before, cell, assignments.Count));
                    return true;
                }
            }
        }

        deduction = null;
        return false;
    }

    public int Propagate()
    {
        var deductions = 0;
        while (TryNext(out _))
        {
            deductions++;
        }

        return deductions;
    }

    private static string ExplainRunReduction(
        KakuroRun run,
        string direction,
        IReadOnlySet<int> before,
        IReadOnlySet<int> after,
        int possibleAssignments)
    {
        var removed = before.Except(after).OrderBy(digit => digit).ToArray();
        var removedText = $"{{{string.Concat(removed)}}}";
        var slotText = run.Cells.Count == 1 ? "1-cell" : $"{run.Cells.Count}-cell";

        return $"Removed {removedText} because no distinct {slotText} combination " +
            $"totalling {run.Target} supports those digits in this slot. " +
            $"The {direction} run has {possibleAssignments} remaining combination" +
            (possibleAssignments == 1 ? "." : "s.");
    }

    private List<int[]> GetPossibleAssignments(KakuroRun run)
    {
        var assignments = new List<int[]>();
        var assignment = new int[run.Cells.Count];
        BuildAssignments(run, 0, 0, 0, assignment, assignments);
        return assignments;
    }

    private void BuildAssignments(
        KakuroRun run,
        int position,
        int sum,
        int usedDigits,
        int[] assignment,
        List<int[]> assignments)
    {
        if (position == run.Cells.Count)
        {
            if (sum == run.Target)
            {
                assignments.Add((int[])assignment.Clone());
            }

            return;
        }

        foreach (var digit in candidates[run.Cells[position]])
        {
            var digitBit = 1 << digit;
            if ((usedDigits & digitBit) != 0 || sum + digit > run.Target)
            {
                continue;
            }

            assignment[position] = digit;
            BuildAssignments(run, position + 1, sum + digit, usedDigits | digitBit, assignment, assignments);
        }
    }
}