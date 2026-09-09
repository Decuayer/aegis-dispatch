using SocarDispatch.Domain.Entities;

namespace SocarDispatch.Application.Common.Interfaces;

public interface IJwtTokenGenerator
{
    (string Token, DateTime ExpiresAt) GenerateToken(User user);
}
