using api_app.DTOs.Users;
using api_app.Models;
using api_app.Repositories.Interfaces;
using api_app.Services.Interfaces;
using FirebaseAdmin;
using FirebaseAdmin.Auth;
using Google.Apis.Auth.OAuth2;

namespace api_app.Services;

public class FirebaseAuthService : IFirebaseAuthService
{
    private const string ProviderName = "firebase";

    private readonly IConfiguration _configuration;
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly Lazy<FirebaseAuth> _firebaseAuth;

    public FirebaseAuthService(
        IConfiguration configuration,
        IUserRepository userRepository,
        IPasswordHasher passwordHasher)
    {
        _configuration = configuration;
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _firebaseAuth = new Lazy<FirebaseAuth>(CreateFirebaseAuth, LazyThreadSafetyMode.ExecutionAndPublication);
    }

    public async Task<UserResponseDto> LoginWithFirebaseAsync(string token, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            throw new ArgumentException("Token is required.");
        }

        FirebaseToken decodedToken;
        try
        {
            decodedToken = await _firebaseAuth.Value.VerifyIdTokenAsync(token, cancellationToken);
        }
        catch (FirebaseAuthException ex)
        {
            throw new UnauthorizedAccessException("Invalid Firebase token.", ex);
        }

        var providerUserId = decodedToken.Uid;
        var email = GetStringClaim(decodedToken, "email");
        if (string.IsNullOrWhiteSpace(email))
        {
            throw new ArgumentException("Firebase token does not contain email.");
        }

        var displayName = GetStringClaim(decodedToken, "name");
        var avatarUrl = GetStringClaim(decodedToken, "picture");

        var user = await _userRepository.GetByIdentityAsync(ProviderName, providerUserId, cancellationToken);
        if (user is null)
        {
            user = await _userRepository.GetByEmailAsync(email, cancellationToken);
        }

        if (user is null)
        {
            var username = BuildUsername(displayName, email);
            user = await _userRepository.CreateAsync(new User
            {
                Username = username,
                Email = email,
                Role = "Developer",
                PasswordHash = _passwordHasher.Hash(Guid.NewGuid().ToString("N"))
            }, cancellationToken);
        }

        await _userRepository.UpsertIdentityAsync(new UserIdentity
        {
            UserId = user.Id,
            Provider = ProviderName,
            ProviderUserId = providerUserId,
            Email = email,
            DisplayName = displayName,
            AvatarUrl = avatarUrl
        }, cancellationToken);

        return new UserResponseDto
        {
            Id = user.Id,
            Username = user.Username,
            Email = user.Email,
            Role = user.Role,
            UserCode = user.UserCode
        };
    }

    private FirebaseAuth CreateFirebaseAuth()
    {
        var credentialsPath = _configuration["Firebase:CredentialsPath"];
        if (string.IsNullOrWhiteSpace(credentialsPath))
        {
            credentialsPath = Environment.GetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS");
        }

        GoogleCredential credential;
        if (!string.IsNullOrWhiteSpace(credentialsPath))
        {
            if (!Path.IsPathRooted(credentialsPath))
            {
                credentialsPath = Path.Combine(AppContext.BaseDirectory, credentialsPath);
            }

            if (!File.Exists(credentialsPath))
            {
                throw new InvalidOperationException($"Firebase credentials file not found at '{credentialsPath}'.");
            }

            credential = GoogleCredential.FromFile(credentialsPath);
        }
        else
        {
            credential = GoogleCredential.GetApplicationDefault();
        }

        var options = new AppOptions
        {
            Credential = credential
        };

        var projectId = _configuration["Firebase:ProjectId"];
        if (!string.IsNullOrWhiteSpace(projectId))
        {
            options.ProjectId = projectId;
        }

        FirebaseApp? app = null;
        try
        {
            app = FirebaseApp.DefaultInstance;
        }
        catch (InvalidOperationException)
        {
            app = null;
        }

        app ??= FirebaseApp.Create(options);

        return FirebaseAuth.GetAuth(app);
    }

    private static string? GetStringClaim(FirebaseToken token, string key)
    {
        if (token.Claims is null)
        {
            return null;
        }

        return token.Claims.TryGetValue(key, out var value)
            ? Convert.ToString(value)
            : null;
    }

    private static string BuildUsername(string? displayName, string email)
    {
        if (!string.IsNullOrWhiteSpace(displayName))
        {
            return displayName.Trim();
        }

        var localPart = email.Split('@', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
        return string.IsNullOrWhiteSpace(localPart) ? email : localPart;
    }
}
