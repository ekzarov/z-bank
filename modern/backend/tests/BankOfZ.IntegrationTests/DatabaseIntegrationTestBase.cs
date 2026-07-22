namespace BankOfZ.IntegrationTests;

public abstract class DatabaseIntegrationTestBase(BankOfZTestsFixture fixture) : IAsyncLifetime
{
    protected BankOfZTestsFixture Fixture { get; } = fixture;

    public Task InitializeAsync() => Fixture.ResetDatabaseAsync();

    public Task DisposeAsync() => Task.CompletedTask;
}
