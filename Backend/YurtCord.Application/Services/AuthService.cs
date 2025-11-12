using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using YurtCord.Core.Common;
using YurtCord.Core.Entities;
using YurtCord.Infrastructure.Data;
using BCrypt.Net;

namespace YurtCord.Application.Services;

public interface IAuthService
{
    Task<AuthResult> RegisterAsync(RegisterRequest request);
    Task<AuthResult> LoginAsync(LoginRequest request);
    Task<bool> ValidateTokenAsync(string token);
    Task<User?> GetUserFromTokenAsync(string token);
}

public class AuthService : IAuthService
{
    private readonly YurtCordDbContext _context;
    private readonly IConfiguration _configuration;
    private readonly SnowflakeGenerator _snowflakeGenerator;

    public AuthService(YurtCordDbContext context, IConfiguration configuration, SnowflakeGenerator snowflakeGenerator)
    {
        _context = context;
        _configuration = configuration;
        _snowflakeGenerator = snowflakeGenerator;
    }

    public async Task<AuthResult> RegisterAsync(RegisterRequest request)
    {
        // Check if email or username already exists
        if (await _context.Users.AnyAsync(u => u.Email == request.Email))
            return AuthResult.Failed("Email already exists");

        // Generate discriminator
        var discriminator = await GenerateDiscriminatorAsync(request.Username);

        // Hash password
        var passwordHash = BCrypt.Net.BCrypt.HashPassword(request.Password, workFactor: 12);

        var user = new User
        {
            Id = _snowflakeGenerator.Generate(),
            Username = request.Username,
            Discriminator = discriminator,
            Email = request.Email,
            PasswordHash = passwordHash,
            Verified = false,
            MfaEnabled = false,
            Flags = UserFlags.None,
            PremiumType = PremiumType.None,
            CreatedAt = DateTime.UtcNow
        };

        _context.Users.Add(user);

        // Create default presence
        var presence = new UserPresence
        {
            UserId = user.Id,
            Status = PresenceStatus.Online,
            UpdatedAt = DateTime.UtcNow
        };
        _context.UserPresences.Add(presence);

        await _context.SaveChangesAsync();

        var token = GenerateJwtToken(user);
        return AuthResult.Success(token, user);
    }

    public async Task<AuthResult> LoginAsync(LoginRequest request)
    {
        var user = await _context.Users
            .Include(u => u.Presence)
            .FirstOrDefaultAsync(u => u.Email == request.Email);

        if (user == null)
            return AuthResult.Failed("Invalid credentials");

        if (!BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            return AuthResult.Failed("Invalid credentials");

        // Update last login
        user.LastLoginAt = DateTime.UtcNow;

        // Update presence
        if (user.Presence != null)
        {
            user.Presence.Status = PresenceStatus.Online;
            user.Presence.UpdatedAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();

        var token = GenerateJwtToken(user);
        return AuthResult.Success(token, user);
    }

    public async Task<bool> ValidateTokenAsync(string token)
    {
        try
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.UTF8.GetBytes(_configuration["Jwt:Secret"] ?? throw new InvalidOperationException("JWT Secret not configured"));

            tokenHandler.ValidateToken(token, new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ValidateIssuer = true,
                ValidIssuer = _configuration["Jwt:Issuer"],
                ValidateAudience = true,
                ValidAudience = _configuration["Jwt:Audience"],
                ClockSkew = TimeSpan.Zero
            }, out _);

            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task<User?> GetUserFromTokenAsync(string token)
    {
        try
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var jwtToken = tokenHandler.ReadJwtToken(token);
            var userIdClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == "sub");

            if (userIdClaim == null || !long.TryParse(userIdClaim.Value, out var userId))
                return null;

            return await _context.Users
                .Include(u => u.Presence)
                .FirstOrDefaultAsync(u => u.Id == new Snowflake(userId));
        }
        catch
        {
            return null;
        }
    }

    private string GenerateJwtToken(User user)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.UTF8.GetBytes(_configuration["Jwt:Secret"] ?? throw new InvalidOperationException("JWT Secret not configured"));

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new[]
            {
                new Claim("sub", user.Id.ToString()),
                new Claim("email", user.Email),
                new Claim("username", user.Tag),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            }),
            Expires = DateTime.UtcNow.AddDays(7),
            Issuer = _configuration["Jwt:Issuer"],
            Audience = _configuration["Jwt:Audience"],
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
        };

        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }

    private async Task<string> GenerateDiscriminatorAsync(string username)
    {
        var random = new Random();
        var existingDiscriminators = await _context.Users
            .Where(u => u.Username == username)
            .Select(u => u.Discriminator)
            .ToListAsync();

        for (int i = 0; i < 1000; i++)
        {
            var discriminator = random.Next(0, 10000).ToString("D4");
            if (!existingDiscriminators.Contains(discriminator))
                return discriminator;
        }

        throw new InvalidOperationException("Could not generate unique discriminator");
    }
}

public record RegisterRequest(string Username, string Email, string Password);
public record LoginRequest(string Email, string Password);

public class AuthResult
{
    public bool Succeeded { get; init; }
    public string? Token { get; init; }
    public User? User { get; init; }
    public string? Error { get; init; }

    public static AuthResult Success(string token, User user) => new()
    {
        Succeeded = true,
        Token = token,
        User = user
    };

    public static AuthResult Failed(string error) => new()
    {
        Succeeded = false,
        Error = error
    };
}
