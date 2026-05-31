using System.Diagnostics;
using System.Text;
using System.Text.Json;

Console.Write("Dataset filename (press Enter for words.json): ");
string? promptInput = Console.ReadLine();
string fileName = string.IsNullOrWhiteSpace(promptInput) ? "words.json" : promptInput.Trim();
string wordsPath = Path.IsPathRooted(fileName)
    ? fileName
    : Path.Combine(AppContext.BaseDirectory, fileName);

if (!File.Exists(wordsPath))
{
    Console.Error.WriteLine($"Dataset not found at {wordsPath}");
    return 1;
}

var sw = Stopwatch.StartNew();
List<string> words;
using (var stream = File.OpenRead(wordsPath))
{
    words = JsonSerializer.Deserialize<List<string>>(stream) ?? new List<string>();
}
words.Sort(StringComparer.Ordinal);
sw.Stop();

if (Console.IsInputRedirected)
{
    Console.WriteLine($"Loaded {words.Count:N0} words from \"{wordsPath}\" in {sw.ElapsedMilliseconds} ms.");
    RunLineLoop(words);
}
else
{
    Console.WriteLine($"Loaded {words.Count:N0} words from \"{wordsPath}\" in {sw.ElapsedMilliseconds} ms.");
    Console.WriteLine("Press any key to start searching...");
    Console.ReadKey(intercept: true);
    RunLiveSearch(words);
}

return 0;

static void RunLiveSearch(List<string> words)
{
    var query = new StringBuilder();
    List<string> matches = [];
    int scrollTop = 0;
    var sortMode = SortMode.Alphabetical;
    bool dirty = true;

    while (true)
    {
        if (dirty)
        {
            matches = query.Length == 0
                ? []
                : FindByPrefix(words, query.ToString());
            ApplySort(matches, sortMode);
            int maxTop = Math.Max(0, matches.Count - ViewportSize());
            if (scrollTop > maxTop) scrollTop = maxTop;
            Render(query.ToString(), matches, scrollTop, sortMode);
            dirty = false;
        }

        var key = Console.ReadKey(intercept: true);
        switch (key.Key)
        {
            case ConsoleKey.Escape:
                Console.Clear();
                return;

            case ConsoleKey.Backspace:
                if (query.Length > 0) { query.Length--; scrollTop = 0; dirty = true; }
                break;

            case ConsoleKey.UpArrow:
                if (scrollTop > 0) { scrollTop--; dirty = true; }
                break;
            case ConsoleKey.DownArrow:
            {
                int maxTop = Math.Max(0, matches.Count - ViewportSize());
                if (scrollTop < maxTop) { scrollTop++; dirty = true; }
                break;
            }
            case ConsoleKey.PageUp:
                if (scrollTop > 0) { scrollTop = Math.Max(0, scrollTop - ViewportSize()); dirty = true; }
                break;
            case ConsoleKey.PageDown:
            {
                int maxTop = Math.Max(0, matches.Count - ViewportSize());
                if (scrollTop < maxTop) { scrollTop = Math.Min(maxTop, scrollTop + ViewportSize()); dirty = true; }
                break;
            }
            case ConsoleKey.Home:
                if (scrollTop != 0) { scrollTop = 0; dirty = true; }
                break;
            case ConsoleKey.End:
            {
                int maxTop = Math.Max(0, matches.Count - ViewportSize());
                if (scrollTop != maxTop) { scrollTop = maxTop; dirty = true; }
                break;
            }

            default:
                char c = key.KeyChar;
                if (c == '>')
                {
                    if (sortMode != SortMode.Longest) { sortMode = SortMode.Longest; scrollTop = 0; dirty = true; }
                }
                else if (c == '<')
                {
                    if (sortMode != SortMode.Shortest) { sortMode = SortMode.Shortest; scrollTop = 0; dirty = true; }
                }
                else if (c == '?')
                {
                    if (sortMode != SortMode.Alphabetical) { sortMode = SortMode.Alphabetical; scrollTop = 0; dirty = true; }
                }
                else if ((c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z'))
                {
                    query.Append(char.ToLowerInvariant(c));
                    scrollTop = 0;
                    dirty = true;
                }
                break;
        }
    }
}

static int ViewportSize() => Math.Max(3, Console.WindowHeight - 5);

static void Render(string query, List<string> matches, int top, SortMode sortMode)
{
    int viewportHeight = ViewportSize();
    int width = Math.Clamp(Console.WindowWidth - 1, 10, 100);
    string sep = new('-', width);
    string promptPrefix = "Search: ";
    string sortLabel = sortMode switch
    {
        SortMode.Longest => "longest first",
        SortMode.Shortest => "shortest first",
        _ => "A-Z",
    };

    Console.Clear();
    Console.WriteLine(promptPrefix + query);
    Console.WriteLine(sep);

    if (query.Length == 0)
    {
        Console.WriteLine("Start typing to search the dictionary.");
    }
    else if (matches.Count == 0)
    {
        Console.WriteLine($"No matches for \"{query}\".");
    }
    else
    {
        int end = Math.Min(top + viewportHeight, matches.Count);
        Console.WriteLine($"{matches.Count:N0} match(es)  [{sortLabel}]  (showing {top + 1}-{end})");
        for (int i = top; i < end; i++)
            Console.WriteLine(matches[i]);
    }

    Console.WriteLine();
    Console.Write("type to search  Backspace: delete  up/down: scroll  >: longest  <: shortest  ?: A-Z  Esc: quit");

    Console.SetCursorPosition(promptPrefix.Length + query.Length, 0);
}

static void ApplySort(List<string> matches, SortMode mode)
{
    switch (mode)
    {
        case SortMode.Longest:
            matches.Sort((a, b) =>
            {
                int c = b.Length.CompareTo(a.Length);
                return c != 0 ? c : StringComparer.Ordinal.Compare(a, b);
            });
            break;
        case SortMode.Shortest:
            matches.Sort((a, b) =>
            {
                int c = a.Length.CompareTo(b.Length);
                return c != 0 ? c : StringComparer.Ordinal.Compare(a, b);
            });
            break;
        case SortMode.Alphabetical:
        default:
            break;
    }
}

static void RunLineLoop(List<string> words)
{
    Console.WriteLine("Type a prefix and press Enter. Type '!x', '!q', or '!exit' to quit.");
    Console.WriteLine();
    while (true)
    {
        Console.Write("> ");
        string? input = Console.ReadLine();
        if (input is null) break;

        string prefix = input.Trim().ToLowerInvariant();
        if (prefix.Length == 0) continue;
        if (prefix is "!x" or "!q" or "!exit") break;

        var matches = FindByPrefix(words, prefix);
        if (matches.Count == 0)
        {
            Console.WriteLine($"No words found starting with \"{prefix}\".");
        }
        else
        {
            Console.WriteLine($"{matches.Count:N0} word(s) starting with \"{prefix}\":");
            foreach (var w in matches) Console.WriteLine(w);
        }
        Console.WriteLine();
    }
}

static List<string> FindByPrefix(List<string> sorted, string prefix)
{
    int lo = LowerBound(sorted, prefix);
    var results = new List<string>();
    for (int i = lo; i < sorted.Count; i++)
    {
        if (!sorted[i].StartsWith(prefix, StringComparison.Ordinal)) break;
        results.Add(sorted[i]);
    }
    return results;
}

static int LowerBound(List<string> sorted, string value)
{
    int lo = 0, hi = sorted.Count;
    while (lo < hi)
    {
        int mid = lo + (hi - lo) / 2;
        if (StringComparer.Ordinal.Compare(sorted[mid], value) < 0) lo = mid + 1;
        else hi = mid;
    }
    return lo;
}

enum SortMode { Alphabetical, Longest, Shortest }
