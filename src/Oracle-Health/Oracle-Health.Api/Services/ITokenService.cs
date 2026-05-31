using Oracle_Health.Models;

namespace Oracle_Health.Api.Services;

public interface ITokenService
{
    string CreateToken(User user, string role);
}
