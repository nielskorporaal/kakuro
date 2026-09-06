using KakuroSolver.Core;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Web;

namespace KakuroSolver.Web.Pages;

public partial class Home : IAsyncDisposable
{
    [Inject]
    private HttpClient Http { get; set; } = default!;

    private KakuroGrid? puzzle;
    private readonly List<Deduction> deductions = [];
    private readonly List<IReadOnlyDictionary<(int Row, int Column), IReadOnlySet<int>>> states = [];
    private int selectedStep;
    private bool loading = true;
    private string? error;
    private string? fileName = "kakuro-puzzle.txt";
    private readonly CancellationTokenSource cancellation = new();

    private bool IsAtStart => selectedStep == 0;

    private bool IsAtEnd => selectedStep >= deductions.Count;

    protected override async Task OnInitializedAsync()
    {
        try
        {
            var text = await Http.GetStringAsync("kakuro-puzzle.txt", cancellation.Token);
            LoadPuzzle(text, "kakuro-puzzle.txt");
        }
        catch (Exception exception)
        {
            error = exception.Message;
        }
        finally
        {
            loading = false;
        }
    }

    private async Task HandleFileSelected(InputFileChangeEventArgs args)
    {
        var file = args.File;
        if (file is null)
        {
            return;
        }

        try
        {
            loading = true;
            error = null;
            await using var stream = file.OpenReadStream(maxAllowedSize: 1024 * 1024, cancellation.Token);
            using var reader = new StreamReader(stream);
            var text = await reader.ReadToEndAsync(cancellation.Token);
            LoadPuzzle(text, file.Name);
        }
        catch (Exception exception)
        {
            error = exception.Message;
        }
        finally
        {
            loading = false;
        }
    }

    private void LoadPuzzle(string text, string name)
    {
        var parsedPuzzle = Parser.Parse(text);
        var solver = new Solver(parsedPuzzle);
        var recordedDeductions = new List<Deduction>();
        var recordedStates = new List<IReadOnlyDictionary<(int Row, int Column), IReadOnlySet<int>>>
        {
            CopyCandidates(solver.Candidates)
        };

        while (solver.TryNext(out var deduction))
        {
            recordedDeductions.Add(deduction!);
            recordedStates.Add(CopyCandidates(solver.Candidates));
        }

        puzzle = parsedPuzzle;
        deductions.Clear();
        deductions.AddRange(recordedDeductions);
        states.Clear();
        states.AddRange(recordedStates);
        selectedStep = 0;
        fileName = name;
    }

    private IReadOnlyDictionary<(int Row, int Column), IReadOnlySet<int>> CurrentCandidates => states[selectedStep];

    private void Next() => SelectStep(Math.Min(selectedStep + 1, deductions.Count));

    private void Previous() => SelectStep(Math.Max(selectedStep - 1, 0));

    private void HandleKeyDown(KeyboardEventArgs args)
    {
        if (args.Key == "ArrowLeft")
        {
            Previous();
        }
        else if (args.Key == "ArrowRight")
        {
            Next();
        }
    }

    private void Seek(ChangeEventArgs args)
    {
        if (int.TryParse(args.Value?.ToString(), out var step))
        {
            SelectStep(step);
        }
    }

    private void SelectStep(int step)
    {
        selectedStep = Math.Clamp(step, 0, deductions.Count);
    }

    private static string StepClass(bool active) => active ? "step-item active" : "step-item";

    private string CellText(Cell cell, int row, int column) => cell switch
    {
        EmptyCell => "#",
        ClueCell clue => $"{clue.VerticalClue?.ToString() ?? ""}\\{clue.HorizontalClue?.ToString() ?? ""}",
        PuzzleCell puzzleCell when puzzleCell.Value is int value => value.ToString(),
        PuzzleCell => FormatCandidates(CurrentCandidates[(row, column)]),
        _ => "?"
    };

    private string CellClass(Cell cell) => cell switch
    {
        EmptyCell => "blocked",
        ClueCell => "clue",
        PuzzleCell puzzleCell when puzzleCell.Value is int => "puzzle solved",
        _ => "puzzle"
    };

    private static string FormatCandidates(IReadOnlySet<int> values) =>
        values.Count == 1 ? string.Concat(values) : $"{{{string.Concat(values.OrderBy(value => value))}}}";

    private static IReadOnlyDictionary<(int Row, int Column), IReadOnlySet<int>> CopyCandidates(
        IReadOnlyDictionary<(int Row, int Column), IReadOnlySet<int>> source) =>
        source.ToDictionary(pair => pair.Key, pair => (IReadOnlySet<int>)pair.Value.ToHashSet());

    public ValueTask DisposeAsync()
    {
        cancellation.Cancel();
        cancellation.Dispose();
        return ValueTask.CompletedTask;
    }
}