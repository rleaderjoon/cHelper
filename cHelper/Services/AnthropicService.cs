using Anthropic;
using Anthropic.Models.Messages;
using Anthropic.Models.Models;

namespace cHelper.Services;

public class AnthropicService
{
    private AnthropicClient? _client;
    private readonly UsageTrackingService _usageTracker;

    public bool IsConfigured => _client != null;

    public AnthropicService(UsageTrackingService usageTracker)
    {
        _usageTracker = usageTracker;
    }

    public void Configure(string apiKey)
    {
        _client = new AnthropicClient { ApiKey = apiKey };
    }

    public void Deconfigure()
    {
        _client = null;
    }

    public async Task<bool> ValidateKeyAsync(string apiKey)
    {
        try
        {
            var testClient = new AnthropicClient { ApiKey = apiKey };
            var page = await testClient.Models.List(new ModelListParams());
            return page.Items.Count >= 0;
        }
        catch { return false; }
    }

    public async Task<List<string>> GetModelsAsync()
    {
        if (_client == null) return [];
        try
        {
            var page = await _client.Models.List(new ModelListParams());
            return page.Items.Select(m => m.ID).OrderBy(id => id).ToList();
        }
        catch { return []; }
    }

    public async Task<string> SendMessageAsync(string userMessage, string model)
    {
        if (_client == null) throw new InvalidOperationException("API key not configured.");

        var response = await _client.Messages.Create(new MessageCreateParams
        {
            Model = model,
            MaxTokens = 1024,
            Messages = [new MessageParam { Role = "user", Content = userMessage }]
        });

        _usageTracker.Record(
            model,
            response.Usage.InputTokens,
            response.Usage.OutputTokens,
            response.Usage.CacheCreationInputTokens ?? 0,
            response.Usage.CacheReadInputTokens ?? 0,
            "app"
        );

        string text = "";
        foreach (var block in response.Content)
        {
            if (block.TryPickText(out var textBlock))
            {
                text = textBlock.Text;
                break;
            }
        }
        return text;
    }
}
