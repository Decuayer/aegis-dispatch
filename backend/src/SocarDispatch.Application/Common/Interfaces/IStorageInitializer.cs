namespace SocarDispatch.Application.Common.Interfaces;

public interface IStorageInitializer
{
    Task InitializeStorageAsync(CancellationToken ct = default);
}
