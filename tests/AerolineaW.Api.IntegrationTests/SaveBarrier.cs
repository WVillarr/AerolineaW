using Microsoft.EntityFrameworkCore.Diagnostics;

namespace AerolineaW.Api.IntegrationTests;

/// <summary>Holds the first two saves after their reads; retries are deliberately not held.</summary>
internal sealed class SaveBarrier : SaveChangesInterceptor
{
    private readonly TaskCompletionSource _ready = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private int _arrivals;

    public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
    {
        if (Interlocked.Increment(ref _arrivals) == 2)
        {
            _ready.TrySetResult();
        }

        await _ready.Task.WaitAsync(TimeSpan.FromSeconds(30), cancellationToken);
        return result;
    }
}
