using Domain.Entities;

namespace Application.Interfaces;

public interface IJwtTokenService
{
    string GenerateToken(ApplicationUser user);
}