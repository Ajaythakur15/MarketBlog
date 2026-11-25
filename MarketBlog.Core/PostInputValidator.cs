namespace MarketBlog;

public static class PostInputValidator
{
    public static Dictionary<string, string> Validate(string? title, string? content, string? author, string? category, string? excerpt)
    {
        var errors = new Dictionary<string, string>();
        CheckRequired(title, "title", "Title", 120, errors);
        CheckRequired(content, "content", "Story", 50000, errors);
        CheckRequired(author, "author", "Author name", 60, errors);
        CheckOptional(category, "category", "Topic", 40, errors);
        CheckOptional(excerpt, "excerpt", "Short intro", 220, errors);
        return errors;
    }

    private static void CheckRequired(string? value, string key, string label, int maxLength, Dictionary<string, string> errors)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            errors[key] = $"{label} is required.";
        }
        else if (value.Trim().Length > maxLength)
        {
            errors[key] = $"{label} must be {maxLength} characters or fewer.";
        }
    }

    private static void CheckOptional(string? value, string key, string label, int maxLength, Dictionary<string, string> errors)
    {
        if (!string.IsNullOrWhiteSpace(value) && value.Trim().Length > maxLength)
            errors[key] = $"{label} must be {maxLength} characters or fewer.";
    }
}
