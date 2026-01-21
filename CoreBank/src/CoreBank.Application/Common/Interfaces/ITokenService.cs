using CoreBank.Domain.Entities;

namespace CoreBank.Application.Common.Interfaces;

public interface ITokenService
{
    string GenerateAccessToken(User user);
    string GenerateRefreshToken();
    bool ValidateToken(string token);
}
