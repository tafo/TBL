using System.Text.RegularExpressions;

var lines = File.ReadAllLines(@"C:\Program Files (x86)\Championship Manager 01-02\benchresult.txt");
var tabbedLines = (from line in lines where line.Contains("PAS Giannina") select line.Replace(" ", "\t")).ToList();
var formattedLines = tabbedLines.Select(line => Regex.Split(line, @"\t+")).Select(parts => string.Join(",", parts).TrimEnd(',')).ToList();
const string path = @"C:\Program Files (x86)\Championship Manager 01-02\BenchResultFormatted.xls";
File.WriteAllLines(path, formattedLines);
Console.WriteLine("Operation is completed.");
Console.ReadLine();