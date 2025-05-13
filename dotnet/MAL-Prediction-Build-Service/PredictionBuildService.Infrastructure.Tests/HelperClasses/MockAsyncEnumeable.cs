namespace PredictionBuildService.Infrastructure.Tests.HelperClasses;

public class MockAsyncEnumerable<T> : IAsyncEnumerable<T>
{
    private readonly IEnumerable<T> _items;

    public MockAsyncEnumerable(IEnumerable<T> items)
    {
        _items = items;
    }

    public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default)
    {
        return new MockAsyncEnumerator<T>(_items.GetEnumerator());
    }
}
