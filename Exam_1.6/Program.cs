using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

class FunctionMetrics
{
    public string Name { get; init; }
    public int Complexity { get; set; }
}

class CyclomaticCalculator
{
    private static readonly Regex Signature = new(@"^\s*(?:public|private|protected|internal|static|\s)*[\w<>\[\]]+\s+(\w+)\s*\([^)]*\)\s*$", RegexOptions.Compiled);
    private static readonly Regex If = new(@"\bif\s*\(", RegexOptions.Compiled);
    private static readonly Regex ElseIf = new(@"\belse\s+if\s*\(", RegexOptions.Compiled);
    private static readonly Regex Loop = new(@"\b(for|while|do)\s*\(", RegexOptions.Compiled);
    private static readonly Regex Case = new(@"\bcase\b", RegexOptions.Compiled);
    private static readonly Regex Ternary = new(@"\?", RegexOptions.Compiled);
    private static readonly Regex Logical = new(@"(&&|\|\|)", RegexOptions.Compiled);

    public IEnumerable<FunctionMetrics> Analyze(string[] lines)
    {
        var results = new List<FunctionMetrics>();
        FunctionMetrics current = null;
        bool waitingForBody = false;
        int depth = 0;

        foreach (var raw in lines)
        {
            var line = StripLineComment(raw);
            if (!waitingForBody && current == null)
            {
                var m = Signature.Match(line);
                if (m.Success)
                {
                    current = new FunctionMetrics { Name = m.Groups[1].Value, Complexity = 1 };
                    waitingForBody = true;
                }
            }

            if (waitingForBody)
            {
                var open = CountChar(line, '{');
                if (open > 0)
                {
                    depth = open - CountChar(line, '}');
                    waitingForBody = false;
                    CountDecisions(line, current);
                    if (depth == 0)
                    {
                        results.Add(current);
                        current = null;
                    }
                    continue;
                }
            }
            else if (current != null)
            {
                depth += CountChar(line, '{');
                depth -= CountChar(line, '}');
                CountDecisions(line, current);
                if (depth == 0)
                {
                    results.Add(current);
                    current = null;
                }
            }
        }
        return results;
    }

    private void CountDecisions(string line, FunctionMetrics m)
    {
        var elseIfCount = ElseIf.Matches(line).Count;
        m.Complexity += elseIfCount;
        m.Complexity += If.Matches(line).Count - elseIfCount;
        m.Complexity += Loop.Matches(line).Count;
        m.Complexity += Case.Matches(line).Count;
        m.Complexity += Ternary.Matches(line).Count;
        m.Complexity += Logical.Matches(line).Count;
    }

    private static string StripLineComment(string line)
    {
        var idx = line.IndexOf("//", StringComparison.Ordinal);
        return idx >= 0 ? line[..idx] : line;
    }

    private static int CountChar(string s, char c)
    {
        var cnt = 0;
        foreach (var ch in s)
            if (ch == c) cnt++;
        return cnt;
    }
}

class Program
{
    static void Main(string[] args)
    {
        if (args.Length == 0)
        {
            Console.WriteLine("usage: cyclomatic <file>");
            return;
        }

        var path = args[0];
        if (!File.Exists(path))
        {
            Console.WriteLine($"File not found: {path}");
            return;
        }

        var lines = File.ReadAllLines(path);
        var calc = new CyclomaticCalculator();
        foreach (var m in calc.Analyze(lines))
            Console.WriteLine($"{m.Name} ({m.Complexity})");
    }
}
