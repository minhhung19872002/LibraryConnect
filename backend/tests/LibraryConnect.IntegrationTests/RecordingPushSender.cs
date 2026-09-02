using LibraryConnect.Application.Common.Interfaces;

namespace LibraryConnect.IntegrationTests;

/// <summary>
/// Bộ gửi thông báo đẩy giả cho kiểm thử: ghi lại từng lượt gửi, và coi mọi mã thiết bị bắt đầu bằng
/// <c>dead-</c> là đã chết (Firebase trả UNREGISTERED) để thử đường gỡ thiết bị.
/// </summary>
public sealed class RecordingPushSender : IPushSender
{
    public record Sent(IReadOnlyList<string> Tokens, string Title, string Body, IReadOnlyDictionary<string, string>? Data);

    private readonly List<Sent> _sent = new();

    public bool IsConfigured => true;

    public IReadOnlyList<Sent> All
    {
        get { lock (_sent) return _sent.ToList(); }
    }

    public Task<PushResult> SendAsync(
        IReadOnlyList<string> tokens,
        string title,
        string body,
        IReadOnlyDictionary<string, string>? data,
        CancellationToken ct = default)
    {
        lock (_sent)
        {
            _sent.Add(new Sent(tokens.ToList(), title, body, data));
        }

        var dead = tokens.Where(token => token.StartsWith("dead-", StringComparison.Ordinal)).ToList();
        return Task.FromResult(new PushResult(tokens.Count - dead.Count, dead));
    }
}
