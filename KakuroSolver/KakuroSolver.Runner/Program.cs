using KakuroSolver.Core;
using KakuroSolver.Runner;

var puzzlePath = Path.Combine(AppContext.BaseDirectory, "Input", "kakuro-1.txt");
var puzzle = Parser.ParseFile(puzzlePath);

Console.WriteLine($"Parsed {puzzle.N}x{puzzle.M} grid with {puzzle.KakuroRuns.Count} runs.\n");

var solver = new Solver(puzzle);
GridViewer.PrintCandidates(puzzle, solver.Candidates, Console.Out);

Console.WriteLine("\nAuto-solving; applying one human deduction every 3 seconds.");

while (solver.TryNext(out var deduction))
{
	Console.WriteLine(
		$"Method: {deduction!.Method}; {deduction.Direction} run {deduction.RunTarget}; " +
		$"cell ({deduction.Cell.Row + 1}, {deduction.Cell.Column + 1}): " +
		$"{FormatCandidates(deduction.Before)} -> {FormatCandidates(deduction.After)}");
	GridViewer.PrintCandidates(puzzle, solver.Candidates, Console.Out);
	await Task.Delay(TimeSpan.FromSeconds(3));
}

Console.WriteLine("\nNo further human deductions are available.");

static string FormatCandidates(IReadOnlySet<int> values) =>
	values.Count == 1 ? string.Concat(values) : $"{{{string.Concat(values.OrderBy(value => value))}}}";