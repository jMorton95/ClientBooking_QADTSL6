using ClientBooking.Authentication;
using ClientBooking.Data;
using ClientBooking.Data.Entities;
using ClientBooking.Features.Login;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;

namespace ClientBooking.Tests.Setup;

public class UnitTestContext
{
    private readonly Mock<ILogger<LoginHandler>> _loggerMock = new();
    private readonly Mock<ISessionStateManager> sessionMock = new();
    private readonly string _dbName = Guid.CreateVersion7().ToString();

    protected DataContext CreateInMemoryContext()
    {
        var options = new DbContextOptionsBuilder<DataContext>()
            .UseInMemoryDatabase(_dbName)
            .Options;

        return new DataContext(options, sessionMock.Object);
    }

    protected DataContext CreateFaultyDataContext()
    {
        return new FaultyDataContext(_dbName, sessionMock.Object);
    }

    protected IPasswordHelper CreatePasswordHelper()
    {
        var passwordHasher = new PasswordHasher();
        return new PasswordHelper(passwordHasher);
    }

    public class FaultyDataContext : DataContext
    {
        public FaultyDataContext(string dbName, ISessionStateManager sessionManager)
            : base(
                new DbContextOptionsBuilder<DataContext>()
                    .UseInMemoryDatabase(dbName)
                    .Options,
                sessionManager) { }

        public override DbSet<Client> Clients => throw new Exception("DB failure");
        
        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            throw new Exception("DB failure");
        }
    }
    
    protected async Task<DataContext> SeedUser(DataContext db)
    {
        db.Users.Add(new User
        {
            FirstName = "John",
            LastName = "Doe",
            Email = "test@test.com",
            HashedPassword = ""
        });
        await db.SaveChangesAsync();

        return db;
    }
}