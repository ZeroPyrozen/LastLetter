# LastLetter

LastLetter is a fast .NET console app that searches a word list by prefix. It loads a JSON dictionary, sorts it, and lets you find matching words either through an interactive live search—typing filters results in real time, with arrow-key scrolling and sorting by length or alphabetically or through a simple line-by-line prompt loop when input is piped in. It uses a binary search over the sorted list for quick prefix lookups, making it handy for word games, puzzles, and dictionary exploration.

## How to Use

Run the app from the project folder:

```bash
dotnet run
```

When prompted, enter a dataset filename or press **Enter** to use the default `words.json`. The file must be a JSON array of strings (e.g. `["apple", "banana", "cherry"]`).

### Interactive live search

Press any key to start, then:

- **Type letters** to filter the list by prefix in real time.
- **Backspace** — delete the last character.
- **Up / Down** — scroll the results one line at a time.
- **Page Up / Page Down** — scroll a full page.
- **Home / End** — jump to the top or bottom.
- **`>`** — sort by longest first.
- **`<`** — sort by shortest first.
- **`?`** — sort alphabetically (A–Z).
- **Esc** — quit.

### Piped / line mode

When input is redirected, the app reads one prefix per line. Type a prefix and press **Enter** to list all matching words, or type `!x`, `!q`, or `!exit` to quit:

```bash
echo "app" | dotnet run
```

