namespace Oracle_Health.Api.Dtos;

public record LoginRequest(string Email, string Password);

public record LoginResponse(string AccessToken, DateTime ExpiresAtUtc, string Role, string FullName);
