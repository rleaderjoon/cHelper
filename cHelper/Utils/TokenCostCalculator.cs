using cHelper.Models;

namespace cHelper.Utils;

public static class TokenCostCalculator
{
    // Per 1M tokens pricing (input, output)
    private static readonly Dictionary<string, (decimal Input, decimal Output)> Pricing = new(StringComparer.OrdinalIgnoreCase)
    {
        ["claude-opus-4-6"]             = (15.00m, 75.00m),
        ["claude-opus-4-5"]             = (15.00m, 75.00m),
        ["claude-opus-4-20250514"]      = (15.00m, 75.00m),
        ["claude-sonnet-4-6"]           = (3.00m,  15.00m),
        ["claude-sonnet-4-5"]           = (3.00m,  15.00m),
        ["claude-sonnet-4-20250514"]    = (3.00m,  15.00m),
        ["claude-haiku-4-5"]            = (0.80m,  4.00m),
        ["claude-haiku-4-5-20251001"]   = (0.80m,  4.00m),
        ["claude-3-5-sonnet-20241022"]  = (3.00m,  15.00m),
        ["claude-3-5-haiku-20241022"]   = (0.80m,  4.00m),
        ["claude-3-haiku-20240307"]     = (0.25m,  1.25m),
        ["claude-3-opus-20240229"]      = (15.00m, 75.00m),
    };

    public static decimal Calculate(string model, long inputTokens, long outputTokens,
        long cacheCreationTokens = 0, long cacheReadTokens = 0)
    {
        string key = NormalizeModel(model);
        if (!Pricing.TryGetValue(key, out var price))
            price = (3.00m, 15.00m); // default to Sonnet pricing

        decimal inputCost = (inputTokens / 1_000_000m) * price.Input;
        decimal outputCost = (outputTokens / 1_000_000m) * price.Output;
        decimal cacheWriteCost = (cacheCreationTokens / 1_000_000m) * price.Input * 1.25m;
        decimal cacheReadCost = (cacheReadTokens / 1_000_000m) * price.Input * 0.10m;

        return inputCost + outputCost + cacheWriteCost + cacheReadCost;
    }

    public static string FormatCost(decimal cost)
    {
        if (cost < 0.01m) return $"${cost:F4}";
        if (cost < 1m) return $"${cost:F3}";
        return $"${cost:F2}";
    }

    public static string FormatTokens(long tokens)
    {
        if (tokens >= 1_000_000) return $"{tokens / 1_000_000.0:F1}M";
        if (tokens >= 1_000) return $"{tokens / 1_000.0:F1}K";
        return tokens.ToString();
    }

    private static string NormalizeModel(string model)
    {
        // Try exact match first
        if (Pricing.ContainsKey(model)) return model;
        // Try prefix match
        foreach (var key in Pricing.Keys)
            if (model.StartsWith(key, StringComparison.OrdinalIgnoreCase)) return key;
        return model;
    }
}
