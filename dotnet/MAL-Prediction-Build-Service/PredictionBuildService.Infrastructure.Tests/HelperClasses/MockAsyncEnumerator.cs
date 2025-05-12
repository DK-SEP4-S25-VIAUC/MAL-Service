namespace PredictionBuildService.Infrastructure.Tests.HelperClasses;

public class MockAsyncEnumerator<T> : IAsyncEnumerator<T> {
    private readonly IEnumerator<T> _enumerator;

    public MockAsyncEnumerator(IEnumerator<T> enumerator) {
        _enumerator = enumerator;
    }

    public T Current => _enumerator.Current;

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    public ValueTask<bool> MoveNextAsync() =>
        ValueTask.FromResult(_enumerator.MoveNext());
}