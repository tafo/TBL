using System.Text.RegularExpressions;

var lines = File.ReadAllLines(@"C:\Program Files (x86)\Championship Manager 01-02\benchresult.txt");
var tabbedLines = (from line in lines where line.Contains("PAS Giannina") select line.Replace(" ", "\t")).ToList();
var formattedLines = tabbedLines.Select(line => Regex.Split(line, @"\t+").Where(x => x != "C" && x != "R")).Select(parts => string.Join(",", parts).TrimEnd(',')).ToList();

// Variables to store aggregated values
int totalMatches = 0, homeWins = 0, homeDraws = 0, homeLosses = 0;
int homeGoalsFor = 0, homeGoalsAgainst = 0, awayWins = 0, awayDraws = 0, awayLosses = 0;
int awayGoalsFor = 0, awayGoalsAgainst = 0, totalPoints = 0;

foreach (var line in formattedLines)
{
    var parts = line.Split(',');
    // Parsing numeric values and summing them up
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

// Create the total row
string totalRow = $"Total,PAS,Giannina,{totalMatches},{homeWins},{homeDraws},{homeLosses},{homeGoalsFor},{homeGoalsAgainst}," +
                  $"{awayWins},{awayDraws},{awayLosses},{awayGoalsFor},{awayGoalsAgainst},{totalPoints}";

// Append the total row to the data
formattedLines.Add(totalRow);
formattedLines.Insert(0, "Pos,Team, ,Pld,Won,Drn,Lst,For,Ag,Won,Drn,Lst,For,Ag,Pts");

const string path = @"C:\Program Files (x86)\Championship Manager 01-02\BenchResultFormatted.csv";
File.WriteAllLines(path, formattedLines);
Console.WriteLine("Operation is completed.");
Console.ReadLine();