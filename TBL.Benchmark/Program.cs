using System.Text.RegularExpressions;
using Microsoft.Extensions.Configuration;

var config = new ConfigurationBuilder()
    .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .Build();

var sourceFolder = config["SourceFolder"]!;
var consolidationFile = config["ConsolidationFile"]!;
var statsFile = config["StatsFile"]!;

if (!Directory.Exists(sourceFolder))
{
    Console.WriteLine("The specified directory does not exist! Please update the appsettings.json file!");
    return;
}

var subDirectories = Directory.GetDirectories(sourceFolder);
File.Delete(consolidationFile);
File.Delete(statsFile);

foreach (var subDir in subDirectories)
{
    var filePath = Path.Combine(subDir, "benchresult.txt");
    var finalPath = Path.Combine(subDir, "BenchResultFormatted.csv");
    File.Delete(finalPath);
    if (File.Exists(filePath))
    {
        var formattedLines = ProcessFile(filePath, subDir);
        WriteFormattedLines(finalPath, formattedLines);
        AppendToConsolidationFile(consolidationFile, formattedLines, subDir == subDirectories[0]);
        Console.WriteLine($"Benchmark operation is completed for {Path.GetFileName(subDir)}.");
    }
}

var tacticPositionCounts = ParseAndCountPositions(consolidationFile);
WritePositionCountsToCsv(statsFile, tacticPositionCounts);

Console.ReadLine();

List<string> ProcessFile(string filePath, string subDir)
{
    var lines = File.ReadAllLines(filePath);
    var tabbedLines = lines.Where(line => line.Contains("PAS Giannina"))
                           .Select(line => line.Replace(" ", "\t"))
                           .ToList();

    var formattedLines = tabbedLines.Select(line => Regex.Split(line, @"\t+")
                                                         .Where(x => x != "C" && x != "R"))
                                    .Select(parts => string.Join(",", parts))
                                    .ToList();

    var lastDirectory = Path.GetFileName(subDir.TrimEnd(Path.DirectorySeparatorChar));
    double seasonCount = formattedLines.Count;

    var (totalMatches, homeWins, homeDraws, homeLosses, homeGoalsFor, homeGoalsAgainst, awayWins, awayDraws, awayLosses, awayGoalsFor, awayGoalsAgainst, totalPoints) =
        AggregateValues(formattedLines);

    for (var index = 0; index < formattedLines.Count; index++)
    {
        var line = formattedLines[index];
        var parts = line.Split(',');

        formattedLines[index] = $"{line},{lastDirectory},{parts[7] + parts[12]}-{parts[8] + parts[13]},{parts[14]},{parts[3]},{parts[4] + parts[9]}_{parts[5] + parts[10]}_{parts[6] + parts[11]}";
    }

    var totalRow = $"Total,PAS,Giannina,{totalMatches},{homeWins},{homeDraws},{homeLosses},{homeGoalsFor},{homeGoalsAgainst},{awayWins},{awayDraws},{awayLosses},{awayGoalsFor},{awayGoalsAgainst},{totalPoints}, ,{lastDirectory},{(homeGoalsFor + awayGoalsFor) / seasonCount}-{(homeGoalsAgainst + awayGoalsAgainst) / seasonCount},{totalPoints / seasonCount},{totalMatches / seasonCount},{(homeWins + awayWins) / seasonCount}_{(homeDraws + awayDraws) / seasonCount}_{(homeLosses + awayLosses) / seasonCount}";

    formattedLines.Insert(0, "Pos,Team, ,Pld,Won,Drn,Lst,For,Ag,Won,Drn,Lst,For,Ag,Pts, ,tactic_name, scored-conceded, points, games, wins_draws_losses");
    formattedLines.Add(totalRow);

    return formattedLines;
}

void WriteFormattedLines(string finalPath, List<string> formattedLines)
{
    File.WriteAllLines(finalPath, formattedLines);
}

void AppendToConsolidationFile(string streamedFile, List<string> formattedLines, bool includeHeader)
{
    using var writer = new StreamWriter(streamedFile, true);
    foreach (var line in includeHeader ? formattedLines : formattedLines.Skip(1))
    {
        writer.WriteLine(line);
    }
    writer.Flush();
}

(int totalMatches, int homeWins, int homeDraws, int homeLosses, int homeGoalsFor, int homeGoalsAgainst, int awayWins, int awayDraws, int awayLosses, int awayGoalsFor, int awayGoalsAgainst, int totalPoints)
AggregateValues(List<string> formattedLines)
{
    int totalMatches = 0, homeWins = 0, homeDraws = 0, homeLosses = 0;
    int homeGoalsFor = 0, homeGoalsAgainst = 0, awayWins = 0, awayDraws = 0, awayLosses = 0;
    int awayGoalsFor = 0, awayGoalsAgainst = 0, totalPoints = 0;

    foreach (var line in formattedLines)
    {
        var parts = line.Split(',');

        totalMatches += int.Parse(parts[3]);
        homeWins += int.Parse(parts[4]);
        homeDraws += int.Parse(parts[5]);
        homeLosses += int.Parse(parts[6]);
        homeGoalsFor += int.Parse(parts[7]);
        homeGoalsAgainst += int.Parse(parts[8]);
        awayWins += int.Parse(parts[9]);
        awayDraws += int.Parse(parts[10]);
        awayLosses += int.Parse(parts[11]);
        awayGoalsFor += int.Parse(parts[12]);
        awayGoalsAgainst += int.Parse(parts[13]);
        totalPoints += int.Parse(parts[14]);
    }

    return (totalMatches, homeWins, homeDraws, homeLosses, homeGoalsFor, homeGoalsAgainst, awayWins, awayDraws, awayLosses, awayGoalsFor, awayGoalsAgainst, totalPoints);
}

static Dictionary<string, Dictionary<int, int>> ParseAndCountPositions(string filePath)
{
    var tacticPositionCounts = new Dictionary<string, Dictionary<int, int>>();

    var lines = File.ReadAllLines(filePath);
    foreach (var line in lines.Skip(1)) // Skip header
    {
        var columns = line.Split(',');
        if (columns.Length <= 17) continue; // Ensure there are enough columns

        var positionString = columns[0].Trim();
        var tacticName = columns[16].Trim();

        if (int.TryParse(positionString.TrimEnd('s', 't', 'n', 'd', 'r', 'h'), out var position))
        {
            if (!tacticPositionCounts.ContainsKey(tacticName))
            {
                tacticPositionCounts[tacticName] = new Dictionary<int, int>();
            }

            if (!tacticPositionCounts[tacticName].TryAdd(position, 1))
            {
                tacticPositionCounts[tacticName][position]++;
            }
        }
    }

    return tacticPositionCounts;
}

static void WritePositionCountsToCsv(string filePath, Dictionary<string, Dictionary<int, int>> tacticPositionCounts)
{
    // Write to CSV
    using var writer = new StreamWriter(filePath);
    writer.WriteLine("Tactic Name," + string.Join(",", Enumerable.Range(1, 14)));

    foreach (var tactic in tacticPositionCounts)
    {
        var counts = Enumerable.Range(1, 14).Select(pos => tactic.Value.GetValueOrDefault(pos, 0));
        writer.WriteLine($"{tactic.Key},{string.Join(",", counts)}");
    }
}