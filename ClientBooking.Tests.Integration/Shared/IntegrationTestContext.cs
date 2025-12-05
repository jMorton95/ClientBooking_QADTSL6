using System.Net;
using System.Text.RegularExpressions;
using ClientBooking.Authentication;
using ClientBooking.Data;
using ClientBooking.Data.Entities;
using ClientBooking.Data.JoiningTables;
using ClientBooking.Shared.Enums;
using ClientBooking.Tests.Integration.Setup;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Http;


namespace ClientBooking.Tests.Integration.Shared;

[CollectionDefinition("Authenticated Test Collection")]
public class AuthenticatedTestCollection 
    : ICollectionFixture<AuthenticatedIntegrationTestContext> { }

[CollectionDefinition("Anonymous Test Collection")]
public class AnonymousTestCollection 
    : ICollectionFixture<AnonymousIntegrationTestContext> { }

public class AuthenticatedIntegrationTestContext : IntegrationTestContext { }

public class AnonymousIntegrationTestContext : IntegrationTestContext { }

public abstract class IntegrationTestContext : IAsyncLifetime
{
    private readonly IServiceScope _scope;
    public DataContext Db { get; }
    private readonly HttpClientHandler _handler;
    public HttpClient HttpClient { get; }

    public readonly string TestUserEmail = $"{Guid.CreateVersion7().ToString()}@test.co.uk";
    public readonly string TestUserPassword = Guid.CreateVersion7().ToString();

    public IntegrationTestContext()
    {
        var factory = new IntegrationTestApplicationFactory();
        
         HttpClient = factory.CreateClient(new WebApplicationFactoryClientOptions
         {
             AllowAutoRedirect = false,
             HandleCookies = true
         });
        
        _scope = factory.Services.CreateScope();
        Db = _scope.ServiceProvider.GetRequiredService<DataContext>();
    }

    public virtual async Task InitializeAsync()
    {
        // Seed DB and get test user
        await SeedUserDataAsync();
        
       
        var getResponse = await HttpClient.GetAsync("/login");
        getResponse.EnsureSuccessStatusCode();
        
        var headers = getResponse.Headers.ToList();
        
        var setCookie = headers
            .First(h => h.Key == "Set-Cookie")
            .Value
            .First();

        var tokenCookie = setCookie.Split(';')[0];  
// ".AspNetCore.Antiforgery.28B_JVk6X_Y=...."

        var token = tokenCookie.Split('=')[1];
        
        // Step 2: POST /login with form + token
        // var formData = new Dictionary<string, string>
        // {
        //     ["__RequestVerificationToken"] = token,
        //     ["LoginRequest.Email"] = TestUserEmail,
        //     ["LoginRequest.Password"] = TestUserPassword,
        //     ["LoginRequest.RememberMe"] = "false"
        // };
        
        var formData = new Dictionary<string, string>
        {
            { "__RequestVerificationToken", token }, // can be dummy
            { "LoginRequest.Email", TestUserEmail },
            { "LoginRequest.Password", TestUserPassword },
            { "LoginRequest.RememberMe", "false" },
           
        };
        //var response = await HttpClient.PostAsync("logout", null);
        var response = await HttpClient.PostAsync("login", new FormUrlEncodedContent(formData));

        var body = await response.Content.ReadAsStringAsync();

        // Check if login was successful
        if (!response.IsSuccessStatusCode)
        {
            throw new Exception($"Login failed: {response.StatusCode}. Body: {body}");
        }
    }
    
    public async Task DisposeAsync()
    {
        _scope.Dispose();
        HttpClient.Dispose();
        await Db.DisposeAsync();
    }

    private async Task<User> SeedUserDataAsync()
    {
        var passwordHasher = _scope.ServiceProvider.GetRequiredService<IPasswordHelper>();

        var roles = new List<Role>
        {
            new() { Name = RoleName.User },
            new() { Name = RoleName.Admin }
        };
        await Db.Roles.AddRangeAsync(roles);

        var user = new User
        {
            FirstName = Guid.CreateVersion7().ToString()[..5],
            LastName = Guid.CreateVersion7().ToString()[..5],
            Email = TestUserEmail,
            HashedPassword = passwordHasher.HashPassword(TestUserPassword),
            UserRoles = roles.Select(r => new UserRole { Role = r }).ToList()
        };
        await Db.Users.AddAsync(user);

        var settings = new Settings
        {
            DefaultWorkingHoursStart = new TimeSpan(9, 0, 0),
            DefaultWorkingHoursEnd = new TimeSpan(17, 0, 0),
            DefaultBreakTimeStart = new TimeSpan(12, 0, 0),
            DefaultBreakTimeEnd = new TimeSpan(13, 0, 0),
            DefaultUserRole = RoleName.User,
            Version = 1,
            RowVersion = 1,
            SavedAt = DateTime.UtcNow
        };
        await Db.Settings.AddAsync(settings);

        await Db.SaveChangesAsync();

        return user;
    }
}



// var antiforgery = _scope.ServiceProvider.GetRequiredService<IAntiforgery>();
//
// // Prepare a fake HttpContext (only used to get the tokens)
// var httpContext = new DefaultHttpContext();
//
// // Get antiforgery cookie + request token
// var tokens = antiforgery.GetAndStoreTokens(httpContext);