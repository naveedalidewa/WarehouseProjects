namespace Api.Auth;

public record LoginRequest(string Email, string Password);

public record LoginResponse(string Token, string Email, string DisplayName, string Role);

public record LoginError(string Message);
