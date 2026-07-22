using System.Text.Json.Serialization;
using BankOfZ.Domain.Accounts;

namespace BankOfZ.Api.Contracts;

public sealed record AccountMetadataRequest(
    [property: JsonRequired] AccountType Type,
    decimal InterestRate,
    int OverdraftLimit,
    string Currency)
{
    public AccountMetadata ToDomain() => new(Type, InterestRate, OverdraftLimit, Currency);
}
