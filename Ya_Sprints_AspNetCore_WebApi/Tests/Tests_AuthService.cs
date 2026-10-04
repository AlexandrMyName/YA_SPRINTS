using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions; 
using Moq;  
using SprintASP_NetCore_API.Application.Mapping;
using SprintASP_NetCore_API.Application.UseCases.DataServices;
using SprintASP_NetCore_API.Application.UseCases.DataServices.Contracts;
using SprintASP_NetCore_API.Domain.Entities; 
using SprintASP_NetCore_API.Infrastructure.Repositories; 
using SprintsASP_NetCore_API.Application.Abstractions;
using SprintsASP_NetCore_API.Application.Abstractions.Options;
using SprintsASP_NetCore_API.Application.Abstractions.Security;
using SprintsASP_NetCore_API.Application.Dtos.EntitiesDtos.Users;
using SprintsASP_NetCore_API.Application.UseCases.DataServices.Contracts;
using SprintsASP_NetCore_API.Domain.Exceptions;
using SprintsASP_NetCore_API.Infrastructure.Concurrency;
using SprintsASP_NetCore_API.Infrastructure.DataAccess.DbContexts;
using SprintsASP_NetCore_API.Infrastructure.Security;
using Xunit;  

namespace Tests;

public class Tests_AuthService : IDisposable
{
    private readonly ServiceProvider _serviceProvider;
    private readonly string _dbName;

    public Tests_AuthService()
    {
        _dbName = Guid.NewGuid().ToString();
        var services = new ServiceCollection();

        // InMemory DB — уникальная для каждого теста
        services.AddDbContext<AppDbContext>(options =>
            options.UseInMemoryDatabase(_dbName));

        // Репозитории
        services.AddScoped(typeof(IRepository<>), typeof(EfCoreRepository<>));

        // Синхронизация
        services.AddSingleton<IInterceptLockings, InterceptLockings>();

        // Security
        services.AddSingleton<IPasswordHasher, PasswordHasher>();
        services.AddSingleton<IJwtTokenGenerator, JwtTokenGenerator>();

        // JwtOptions — HS256, минимальный секрет в 32 символа
        services.Configure<JwtOptions>(opts =>
        {
            opts.Mode = "HS256";
            opts.HsKey = "TEST_SECRET_KEY_32_CHARS_MIN_LENGTH_XYZ";
            opts.Issuer = "TestIssuer";
            opts.Audience = "TestAudience";
            opts.AccessTokenMinutes = 15;
            opts.RefreshTokenDays = 7;
        });

        // Сервисы
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IUserService, UserService>();

        // Логгеры — моки
        services.AddSingleton<ILogger<AuthService>>(sp => Mock.Of<ILogger<AuthService>>());
        services.AddSingleton<ILogger<UserService>>(sp => Mock.Of<ILogger<UserService>>());
        services.AddSingleton<ILogger<BaseDataService< IUserInfoDto, User>>>(
            sp => Mock.Of<ILogger<BaseDataService< IUserInfoDto, User>>>());

        // AutoMapper с боевыми профилями
        var mapperConfig = new MapperConfiguration(cfg =>
        {
            cfg.AddProfile<MappingEntityProfile>();
            cfg.AddProfile<MappingDtoProfile>();
        }, NullLoggerFactory.Instance);
        services.AddSingleton<IMapper>(mapperConfig.CreateMapper());

        _serviceProvider = services.BuildServiceProvider();
    }

    public void Dispose() => _serviceProvider?.Dispose();
 
 
    private static RegisterDto NewRegisterDto(string login, string password = "pwd123456")
        => new() { Login = login, Password = password };

    private static LoginDto NewLoginDto(string login, string password = "pwd123456")
        => new() { Login = login, Password = password };

    private async Task<User?> GetUserByLoginAsync(string login)
    {
        using var scope = _serviceProvider.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<IRepository<User>>();
        var found = await repo.FindAsync(u => u.Login == login);
        return found.FirstOrDefault();
    }

    #region Register

    [Fact]
    public async Task Register_WithNewLogin_CreatesUser_AndReturnsTokens()
    {
        using var scope = _serviceProvider.CreateScope();
        var auth = scope.ServiceProvider.GetRequiredService<IAuthService>();

        var result = await auth.RegisterAsync(NewRegisterDto("newuser"));

        Assert.True(result.IsSuccesfuly);
        Assert.NotNull(result.Data);
        Assert.False(string.IsNullOrEmpty(result.Data!.AccessToken));
        Assert.False(string.IsNullOrEmpty(result.Data.RefreshToken));
        Assert.Equal("Bearer", result.Data.TokenType);
        Assert.True(result.Data.ExpiresIn > 0);

        // Пользователь действительно создан
        var user = await GetUserByLoginAsync("newuser");
        Assert.NotNull(user);
    }

    [Fact]
    public async Task Register_AlwaysAssignsUserRole_NeverAdmin()
    {
        using var scope = _serviceProvider.CreateScope();
        var auth = scope.ServiceProvider.GetRequiredService<IAuthService>();

        var result = await auth.RegisterAsync(NewRegisterDto("rolecheck"));

        Assert.True(result.IsSuccesfuly);

        var user = await GetUserByLoginAsync("rolecheck");
        Assert.NotNull(user);
        Assert.Equal(UserRole.User, user!.Role);
    }

    [Fact]
    public async Task Register_StoresHashedPassword_NotPlaintext()
    {
        using var scope = _serviceProvider.CreateScope();
        var auth = scope.ServiceProvider.GetRequiredService<IAuthService>();

        const string password = "plaintext123";
        await auth.RegisterAsync(NewRegisterDto("hashuser", password));

        var user = await GetUserByLoginAsync("hashuser");
        Assert.NotNull(user);

        // Не открытый текст
        Assert.NotEqual(password, user!.PasswordHash);

        // SHA-256 в hex = 64 символа
        Assert.Equal(64, user.PasswordHash.Length);

        // Все символы — hex
        Assert.All(user.PasswordHash, c => Assert.True(Uri.IsHexDigit(c)));
    }

    [Fact]
    public async Task Register_HashedPassword_MatchesSha256OfInput()
    {
        using var scope = _serviceProvider.CreateScope();
        var auth = scope.ServiceProvider.GetRequiredService<IAuthService>();
        var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();

        const string password = "verify_me_123";
        await auth.RegisterAsync(NewRegisterDto("verify_hash", password));

        var user = await GetUserByLoginAsync("verify_hash");
        Assert.NotNull(user);

        // Хеш в БД совпадает с тем, что даёт IPasswordHasher
        Assert.Equal(hasher.Hash(password), user!.PasswordHash);
    }

    [Fact]
    public async Task Register_WithDuplicateLogin_ThrowsUserAlreadyExistsException()
    {
        using var scope = _serviceProvider.CreateScope();
        var auth = scope.ServiceProvider.GetRequiredService<IAuthService>();

        await auth.RegisterAsync(NewRegisterDto("duplicate"));

        await Assert.ThrowsAsync<UserAlreadyExistsException>(() =>
            auth.RegisterAsync(NewRegisterDto("duplicate")));
    }

    [Fact]
    public async Task Register_CreatesRefreshTokenRecordInDatabase()
    {
        using var scope = _serviceProvider.CreateScope();
        var auth = scope.ServiceProvider.GetRequiredService<IAuthService>();
        var refreshRepo = scope.ServiceProvider.GetRequiredService<IRepository<RefreshToken>>();

        await auth.RegisterAsync(NewRegisterDto("refresh_check"));

        var all = await refreshRepo.GetAllAsync();
        Assert.Single(all);
        Assert.True(all.First().IsActive);
    }

    #endregion

    #region Login

    [Fact]
    public async Task Login_WithValidCredentials_ReturnsTokens()
    {
        using var scope = _serviceProvider.CreateScope();
        var auth = scope.ServiceProvider.GetRequiredService<IAuthService>();

        await auth.RegisterAsync(NewRegisterDto("login_user", "correct_pwd_123"));

        var result = await auth.LoginAsync(NewLoginDto("login_user", "correct_pwd_123"));

        Assert.True(result.IsSuccesfuly);
        Assert.NotNull(result.Data);
        Assert.False(string.IsNullOrEmpty(result.Data!.AccessToken));
        Assert.False(string.IsNullOrEmpty(result.Data.RefreshToken));
    }

    [Fact]
    public async Task Login_WithWrongPassword_ThrowsInvalidCredentialsException()
    {
        using var scope = _serviceProvider.CreateScope();
        var auth = scope.ServiceProvider.GetRequiredService<IAuthService>();

        await auth.RegisterAsync(NewRegisterDto("wrong_pwd_user", "correct"));

        await Assert.ThrowsAsync<InvalidCredentialsException>(() =>
            auth.LoginAsync(NewLoginDto("wrong_pwd_user", "wrong")));
    }

    [Fact]
    public async Task Login_WithNonExistentLogin_ThrowsInvalidCredentialsException()
    {
        using var scope = _serviceProvider.CreateScope();
        var auth = scope.ServiceProvider.GetRequiredService<IAuthService>();

        await Assert.ThrowsAsync<InvalidCredentialsException>(() =>
            auth.LoginAsync(NewLoginDto("ghost_user", "any_pwd_123")));
    }

    [Fact]
    public async Task Login_IssuesNewRefreshToken_EachTime()
    {
        using var scope = _serviceProvider.CreateScope();
        var auth = scope.ServiceProvider.GetRequiredService<IAuthService>();
        var refreshRepo = scope.ServiceProvider.GetRequiredService<IRepository<RefreshToken>>();

        await auth.RegisterAsync(NewRegisterDto("multi_login", "pwd123456"));

        var login1 = await auth.LoginAsync(NewLoginDto("multi_login", "pwd123456"));
        var login2 = await auth.LoginAsync(NewLoginDto("multi_login", "pwd123456"));

        // Refresh-токены должны различаться
        Assert.NotEqual(login1.Data!.RefreshToken, login2.Data!.RefreshToken);

        // В БД теперь 3 refresh-записи: register + 2 login
        var all = await refreshRepo.GetAllAsync();
        Assert.Equal(3, all.Count());
    }

    #endregion

    #region Refresh

    [Fact]
    public async Task Refresh_WithValidToken_ReturnsNewPair()
    {
        using var scope = _serviceProvider.CreateScope();
        var auth = scope.ServiceProvider.GetRequiredService<IAuthService>();

        var registered = await auth.RegisterAsync(NewRegisterDto("refresh_user", "pwd123456"));
        var oldRefresh = registered.Data!.RefreshToken;

        var refreshed = await auth.RefreshAsync(oldRefresh);

        Assert.True(refreshed.IsSuccesfuly);
        Assert.NotNull(refreshed.Data);
        Assert.False(string.IsNullOrEmpty(refreshed.Data!.AccessToken));
        Assert.NotEqual(oldRefresh, refreshed.Data.RefreshToken);
    }

    [Fact]
    public async Task Refresh_RotatesOldToken_OldBecomesInvalid()
    {
        using var scope = _serviceProvider.CreateScope();
        var auth = scope.ServiceProvider.GetRequiredService<IAuthService>();

        var registered = await auth.RegisterAsync(NewRegisterDto("rotate_user", "pwd123456"));
        var oldRefresh = registered.Data!.RefreshToken;

        // Первый refresh — успешен, ротирует
        await auth.RefreshAsync(oldRefresh);

        // Повторное использование старого токена — ошибка
        await Assert.ThrowsAsync<InvalidRefreshTokenException>(() =>
            auth.RefreshAsync(oldRefresh));
    }

    [Fact]
    public async Task Refresh_WithEmptyToken_ThrowsInvalidRefreshTokenException()
    {
        using var scope = _serviceProvider.CreateScope();
        var auth = scope.ServiceProvider.GetRequiredService<IAuthService>();

        await Assert.ThrowsAsync<InvalidRefreshTokenException>(() =>
            auth.RefreshAsync(string.Empty));
    }

    [Fact]
    public async Task Refresh_WithUnknownToken_ThrowsInvalidRefreshTokenException()
    {
        using var scope = _serviceProvider.CreateScope();
        var auth = scope.ServiceProvider.GetRequiredService<IAuthService>();

        await Assert.ThrowsAsync<InvalidRefreshTokenException>(() =>
            auth.RefreshAsync("some_unknown_token_value"));
    }

    #endregion

    #region Logout

    [Fact]
    public async Task Logout_WithValidRefreshToken_RevokesIt()
    {
        using var scope = _serviceProvider.CreateScope();
        var auth = scope.ServiceProvider.GetRequiredService<IAuthService>();
        var refreshRepo = scope.ServiceProvider.GetRequiredService<IRepository<RefreshToken>>();

        var registered = await auth.RegisterAsync(NewRegisterDto("logout_user", "pwd123456"));
        var refresh = registered.Data!.RefreshToken;

        var result = await auth.LogoutAsync(refresh);

        Assert.True(result.IsSuccesfuly);

        var tokens = await refreshRepo.GetAllAsync();
        Assert.All(tokens, t => Assert.False(t.IsActive));
    }

    [Fact]
    public async Task Logout_ThenRefresh_ThrowsInvalidRefreshTokenException()
    {
        using var scope = _serviceProvider.CreateScope();
        var auth = scope.ServiceProvider.GetRequiredService<IAuthService>();

        var registered = await auth.RegisterAsync(NewRegisterDto("logout_then_refresh", "pwd123456"));
        var refresh = registered.Data!.RefreshToken;

        await auth.LogoutAsync(refresh);

        await Assert.ThrowsAsync<InvalidRefreshTokenException>(() =>
            auth.RefreshAsync(refresh));
    }

    [Fact]
    public async Task Logout_WithNullOrEmpty_ReturnsSuccess_NoThrow()
    {
        using var scope = _serviceProvider.CreateScope();
        var auth = scope.ServiceProvider.GetRequiredService<IAuthService>();

        var r1 = await auth.LogoutAsync(null);
        var r2 = await auth.LogoutAsync("");
        var r3 = await auth.LogoutAsync("   ");

        Assert.True(r1.IsSuccesfuly);
        Assert.True(r2.IsSuccesfuly);
        Assert.True(r3.IsSuccesfuly);
    }

    [Fact]
    public async Task Logout_WithUnknownToken_ReturnsSuccess_NoThrow()
    {
        using var scope = _serviceProvider.CreateScope();
        var auth = scope.ServiceProvider.GetRequiredService<IAuthService>();

        var result = await auth.LogoutAsync("totally_unknown_token_12345");

        Assert.True(result.IsSuccesfuly);
    }

    #endregion

    #region JWT claims

    [Fact]
    public async Task Login_TokenContains_UserId_Role_Login_Claims()
    {
        using var scope = _serviceProvider.CreateScope();
        var auth = scope.ServiceProvider.GetRequiredService<IAuthService>();

        var registered = await auth.RegisterAsync(NewRegisterDto("claims_user", "pwd123456"));
        var token = registered.Data!.AccessToken;

        // Простейший разбор payload без валидации подписи
        var parts = token.Split('.');
        Assert.Equal(3, parts.Length);

        var payload = System.Text.Json.JsonDocument.Parse(
            System.Text.Encoding.UTF8.GetString(
                DecodeBase64Url(parts[1]))).RootElement;

        Assert.True(payload.TryGetProperty("userId", out var userId));
        Assert.False(string.IsNullOrEmpty(userId.GetString()));

        Assert.True(payload.TryGetProperty("unique_name", out var login));
        Assert.Equal("claims_user", login.GetString());

        Assert.True(payload.TryGetProperty(
            "http://schemas.microsoft.com/ws/2008/06/identity/claims/role",
            out var role));
        Assert.Equal("User", role.GetString());
    }

    private static byte[] DecodeBase64Url(string input)
    {
        var s = input.Replace('-', '+').Replace('_', '/');
        switch (s.Length % 4)
        {
            case 2: s += "=="; break;
            case 3: s += "="; break;
        }
        return Convert.FromBase64String(s);
    }

    #endregion
}