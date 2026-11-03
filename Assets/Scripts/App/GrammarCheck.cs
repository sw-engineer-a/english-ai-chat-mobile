using System.Text.RegularExpressions;

public static class GrammarCheck
{
    static readonly string[] Markers =
    {
        "am", "is", "are", "was", "were", "do", "does", "did", "can", "could",
        "will", "would", "have", "has", "had", "what", "where", "who", "how",
        "why", "when", "please", "my", "your", "like", "want", "go", "see"
    };

    static readonly string[] Greetings =
    {
        "hi", "hello", "hey", "hiya", "yo",
        "good morning", "good afternoon", "good evening", "good night",
        "how are you", "how are you?", "how's it going", "how's it going?"
    };

    public static bool Accept(string text)
    {
        if (IsBlank(text)) return false;
        if (IsGreeting(text)) return true;

        var trimmed = text.Trim();
        var words = Regex.Split(trimmed, @"\s+");
        if (words.Length < 2) return false;
        if (!char.IsUpper(trimmed[0])) return false;
        var last = trimmed[trimmed.Length - 1];
        if (last != '.' && last != '?' && last != '!') return false;
        var lower = trimmed.ToLower();
        for (var i = 0; i < Markers.Length; i++)
        {
            if (Regex.IsMatch(lower, @"\b" + Markers[i] + @"\b")) return true;
        }
        return false;
    }

    public static bool IsGreeting(string text)
    {
        if (IsBlank(text)) return false;
        var lower = text.Trim().TrimEnd('.', '!', '?').ToLower();
        for (var i = 0; i < Greetings.Length; i++)
        {
            if (lower == Greetings[i]) return true;
        }
        return false;
    }

    /// <summary>Short tip when a line is rejected.</summary>
    public static string Coach(string text)
    {
        if (IsBlank(text)) return "Please type an English sentence.";
        var trimmed = text.Trim();
        var words = Regex.Split(trimmed, @"\s+");
        if (words.Length < 2)
            return "Please write a full sentence, like: How are you?";
        if (!char.IsUpper(trimmed[0]))
            return "Please start with a capital letter.";
        var last = trimmed[trimmed.Length - 1];
        if (last != '.' && last != '?' && last != '!')
            return "Please end with . ? or !";
        return "Please use words like what, how, is, are, like, or want.";
    }

    public static bool IsBlank(string text)
    {
        return text == null || text.Trim().Length == 0;
    }
}
