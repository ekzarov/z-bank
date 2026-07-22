using BankOfZ.Application.Common;
using BankOfZ.Application.Customers;
using BankOfZ.Domain.Customers;

namespace BankOfZ.UnitTests.Customers;

public sealed class CustomerServiceTests
{
    private static readonly DateTimeOffset Now = new(2026, 7, 22, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task Create_Averages_Five_Assessments_And_Sets_Review_Date()
    {
        var repository = new FakeRepository();
        var provider = new FakeCreditProvider([500, 600, 700, 800, 900]);
        var service = CreateService(repository, provider);

        var result = await service.CreateAsync(
            CustomerTests.Details(), SourceSystem.Modern, null, "operator", "correlation", default);

        Assert.Equal(700, result.CreditScore);
        Assert.Equal(new DateOnly(2026, 8, 12), result.CreditReviewDate);
        Assert.Single(repository.Customers);
        Assert.Equal(5, provider.CallCount);
    }

    [Fact]
    public async Task Create_Averages_Only_Successful_Assessments()
    {
        var repository = new FakeRepository();
        var service = CreateService(repository, new FakeCreditProvider([500, null, 700, null, 900]));

        var result = await service.CreateAsync(
            CustomerTests.Details(), SourceSystem.Cics, "legacy-42", "operator", "correlation", default);

        Assert.Equal(700, result.CreditScore);
        Assert.Equal(SourceSystem.Cics, result.SourceSystem);
        Assert.Equal("legacy-42", result.SourceIdentifier);
    }

    [Fact]
    public async Task Create_Rejects_Total_Provider_Failure_Before_Persistence()
    {
        var repository = new FakeRepository();
        var service = CreateService(repository, new FakeCreditProvider([null, null, null, null, null]));

        await Assert.ThrowsAsync<CreditAssessmentUnavailableException>(() => service.CreateAsync(
            CustomerTests.Details(), SourceSystem.Modern, null, "operator", "correlation", default));

        Assert.Empty(repository.Customers);
        Assert.False(repository.SaveCalled);
    }

    private static CustomerService CreateService(FakeRepository repository, FakeCreditProvider provider) => new(
        repository,
        provider,
        new FakeAccountStatusReader(),
        new FakeAuditWriter(),
        new FakeClock(),
        new CustomerOptions { CreditProviders = CustomerOptions.DefaultCreditProviders.ToArray() });

    private sealed class FakeClock : IClock
    {
        public DateTimeOffset UtcNow => Now;
    }

    private sealed class FakeCreditProvider(int?[] scores) : ICreditAssessmentProvider
    {
        private int index;
        public int CallCount => index;

        public Task<CreditAssessment?> AssessAsync(
            string provider,
            string customerId,
            CustomerDetails details,
            CancellationToken cancellationToken)
        {
            var score = scores[index++];
            return Task.FromResult(score is null ? null : new CreditAssessment(provider, score.Value));
        }
    }

    private sealed class FakeAccountStatusReader : ICustomerAccountStatusReader
    {
        public Task<CustomerAccountStatus> GetAsync(string customerId, CancellationToken cancellationToken) =>
            Task.FromResult(new CustomerAccountStatus(false, false));
    }

    private sealed class FakeAuditWriter : ICustomerAuditWriter
    {
        public void Add(CustomerAuditEntry entry)
        {
        }
    }

    private sealed class FakeRepository : ICustomerRepository
    {
        public List<Customer> Customers { get; } = [];
        public bool SaveCalled { get; private set; }
        public Task<Customer?> FindAsync(string id, bool tracking, CancellationToken cancellationToken) =>
            Task.FromResult(Customers.SingleOrDefault(customer => customer.Id == id));

        public Task<IReadOnlyList<Customer>> SearchAsync(string normalizedName, int page, int pageSize, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<Customer>>(Customers);

        public Task<string> AllocateIdAsync(CancellationToken cancellationToken) => Task.FromResult("1000000002");

        public void Add(Customer customer) => Customers.Add(customer);

        public void SetExpectedVersion(Customer customer, byte[] version)
        {
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken)
        {
            SaveCalled = true;
            return Task.CompletedTask;
        }
    }
}
