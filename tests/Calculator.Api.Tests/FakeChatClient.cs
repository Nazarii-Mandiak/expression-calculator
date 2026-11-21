using System.Runtime.CompilerServices;

using Microsoft.Extensions.AI;

namespace Calculator.Api.Tests;

/// <summary>Stands in for a real LLM: replies with fixed text (or throws / stalls) and records what it was asked.</summary>
internal sealed class FakeChatClient : IChatClient
{
    private readonly string _reply;
    private readonly Exception? _failure;
    private readonly TimeSpan _delay;

    private FakeChatClient(string reply, Exception? failure, TimeSpan delay) =>
        (_reply, _failure, _delay) = (reply, failure, delay);

    public static FakeChatClient Replying(string reply) => new(reply, null, TimeSpan.Zero);

    public static FakeChatClient Throwing(Exception failure) => new("", failure, TimeSpan.Zero);

    public static FakeChatClient Delaying(TimeSpan delay, string reply) => new(reply, null, delay);

    public bool WasCalled { get; private set; }

    public IReadOnlyList<ChatMessage> ReceivedMessages { get; private set; } = [];

    /// <summary>All text the model was sent, joined, for loose prompt assertions.</summary>
    public string ReceivedText => string.Join("\n", ReceivedMessages.Select(m => m.Text));

    public async Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> messages, ChatOptions? options = null, CancellationToken cancellationToken = default) =>
        new(new ChatMessage(ChatRole.Assistant, await ReplyAsync(messages, cancellationToken)));

    public async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> messages, ChatOptions? options = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        yield return new ChatResponseUpdate(ChatRole.Assistant, await ReplyAsync(messages, cancellationToken));
    }

    public object? GetService(Type serviceType, object? serviceKey = null) => null;

    public void Dispose()
    {
    }

    private async Task<string> ReplyAsync(IEnumerable<ChatMessage> messages, CancellationToken cancellationToken)
    {
        WasCalled = true;
        ReceivedMessages = [.. messages];
        if (_failure is not null)
        {
            throw _failure;
        }

        await Task.Delay(_delay, cancellationToken);
        return _reply;
    }
}
