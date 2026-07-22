using BankOfZ.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;

namespace BankOfZ.Api.Security;

public sealed class InvalidCredentialWorkFactor
{
    private readonly ApplicationUser _dummyUser = new() { UserName = "invalid-credential-probe" };
    private readonly PasswordHasher<ApplicationUser> _hasher = new();
    private readonly string _dummyHash;

    public InvalidCredentialWorkFactor()
    {
        _dummyHash = _hasher.HashPassword(_dummyUser, "not-a-real-user-password");
    }

    public void Verify(string suppliedPassword)
    {
        _ = _hasher.VerifyHashedPassword(_dummyUser, _dummyHash, suppliedPassword);
    }
}
