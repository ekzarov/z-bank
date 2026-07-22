namespace BankOfZ.IntegrationTests;

[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class DatabaseTestCollection : ICollectionFixture<BankOfZTestsFixture>
{
    public const string Name = "Database";
}
