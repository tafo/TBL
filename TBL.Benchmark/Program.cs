using System.Text.RegularExpressions;
using Microsoft.Extensions.Configuration;

var config = new ConfigurationBuilder()
    .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .Build();

var sourceFolder = config["SourceFolder"]!;

if (!Directory.Exists(sourceFolder))
{
    Console.WriteLine("The specified directory does not exist! Please update the appsettings.json file!");
    return;
}

// Get all subdirectories in the root directory
var subDirectories = Directory.GetDirectories(sourceFolder);

var outputFile = config["ConsolidationFile"]!; // Path for the merged file

// Delete existing consolidation file
File.Delete(outputFile);

for (var fileIndex = 0; fileIndex < subDirectories.Length; fileIndex++)
{
    var subDir = subDirectories[fileIndex];
    var filePath = Path.Combine(subDir, "benchresult.txt"); // Construct the file path
    var finalPath = @$"{subDir}\\BenchResultFormatted.csv";
    var isCompleted = File.Exists(finalPath);
    if (File.Exists(filePath) && !isCompleted)
    {
        var lines = File.ReadAllLines(filePath);
        var tabbedLines =
            (from line in lines where line.Contains("PAS Giannina") select line.Replace(" ", "\t")).ToList();
        var formattedLines = tabbedLines.Select(line => Regex.Split(line, @"\t+").Where(x => x != "C" && x != "R"))
            .Select(parts => string.Join(",", parts)).ToList();

        // Variables to store aggregated values
        int totalMatches = 0, homeWins = 0, homeDraws = 0, homeLosses = 0;
        int homeGoalsFor = 0, homeGoalsAgainst = 0, awayWins = 0, awayDraws = 0, awayLosses = 0;
        int awayGoalsFor = 0, awayGoalsAgainst = 0, totalPoints = 0;

        var lastDirectory = Path.GetFileName(subDir.TrimEnd(Path.DirectorySeparatorChar));

        double seasonCount = formattedLines.Count;
        for (var index = 0; index < formattedLines.Count; index++)
        {
            var line = formattedLines[index];
            var parts = line.Split(',');
            // Parsing numeric values and summing them up

            int matches = int.Parse(parts[3]);
            int hWins = int.Parse(parts[4]);
            int hDraws = int.Parse(parts[5]);
            int hLosses = int.Parse(parts[6]);
            int hGoalsFor = int.Parse(parts[7]);
            int hGoalsAgainst = int.Parse(parts[8]);
            int aWins = int.Parse(parts[9]);
            int aDraws = int.Parse(parts[10]);
            int aLosses = int.Parse(parts[11]);
            int aGoalsFor = int.Parse(parts[12]);
            int aGoalsAgainst = int.Parse(parts[13]);
            int points = int.Parse(parts[14]);


            totalMatches += matches;
            homeWins += hWins;
            homeDraws += hDraws;
            homeLosses += hLosses;
            homeGoalsFor += hGoalsFor;
            homeGoalsAgainst += hGoalsAgainst;
            awayWins += aWins;
            awayDraws += aDraws;
            awayLosses += aLosses;
            awayGoalsFor += aGoalsFor;
            awayGoalsAgainst += aGoalsAgainst;
            totalPoints += points;

            formattedLines[index] = line + "," +
                                    $"{lastDirectory}, {hGoalsFor + aGoalsFor}-{hGoalsAgainst + aGoalsAgainst}," +
                                    $"{points},{matches},{hWins + aWins}_{hDraws + aDraws}_{hLosses + aLosses}";
        }


        // Create the total row
        var totalRow =
            $"Total,PAS,Giannina,{totalMatches},{homeWins},{homeDraws},{homeLosses},{homeGoalsFor},{homeGoalsAgainst}," +
            $"{awayWins},{awayDraws},{awayLosses},{awayGoalsFor},{awayGoalsAgainst},{totalPoints}, ,{lastDirectory}, {(homeGoalsFor + awayGoalsFor) / seasonCount}-{(homeGoalsAgainst + awayGoalsAgainst) / seasonCount}," +
            $"{totalPoints / seasonCount}, {totalMatches / seasonCount}, {(homeWins + awayWins) / seasonCount}_{(homeDraws + awayDraws) / seasonCount}_{(homeLosses + awayLosses) / seasonCount}";

        // Append the total row to the data
        formattedLines.Add(totalRow);
        const string headerRow =
            "Pos,Team, ,Pld,Won,Drn,Lst,For,Ag,Won,Drn,Lst,For,Ag,Pts, ,tactic_name, scored-conceded, points, games, wins_draws_losses";
        formattedLines.Insert(0, headerRow);
        File.WriteAllLines(finalPath, formattedLines);

        Console.WriteLine($"Benchmark operation is completed for {lastDirectory}.");
        
        // Determine if we need to include the header or not
        var includeHeader = fileIndex == 0;

        using var writer = new StreamWriter(outputFile, true);

        foreach (var line in includeHeader ? formattedLines : formattedLines.Skip(1))
        {
            writer.WriteLine(line);
        }

        // Flush once after all writes (optional, since Dispose() does this)
        writer.Flush();

        Console.WriteLine($"CSV files merged successfully for {lastDirectory}.!");
    }
}

Console.ReadLine();
