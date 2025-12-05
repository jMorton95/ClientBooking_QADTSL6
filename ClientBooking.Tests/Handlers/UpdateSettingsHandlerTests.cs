using ClientBooking.Data.Entities;
using ClientBooking.Features.UpdateSettings;
using ClientBooking.Shared.Enums;
using ClientBooking.Tests.Setup;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;

namespace ClientBooking.Tests.Handlers;

public class UpdateSettingsHandlerTests : UnitTestContext
{
    // GET HANDLER
    [Fact]
    public async Task GetHandler_ReturnsMostRecentSettings()
    {
        // Arrange
        await using var db = CreateInMemoryContext();
        var older = new Settings { DefaultUserRole = RoleName.User };
        await db.Settings.AddAsync(older);
        await db.SaveChangesAsync();
        
        var newer = new Settings {  DefaultUserRole = RoleName.Admin };
        await db.Settings.AddAsync(newer);
        await db.SaveChangesAsync();

        // Act
        var result = await UpdateSettingsHandler.GetHandler(db);

        // Assert
        var rc = Assert.IsType<RazorComponentResult<UpdateSettingsComponent>>(result);
        var request = rc.Parameters["UpdateSettingsRequest"] as UpdateSettingsRequest;
        Assert.NotNull(request);
        Assert.Equal(1, older.Version);
        Assert.Equal(2, newer.Version);
        Assert.Equal(2, request!.Version);
        Assert.Equal(RoleName.Admin, request.DefaultUserRole);
    }

    // POST HANDLER
    [Fact]
    public async Task PostHandler_ValidRequest_ReturnsSuccess()
    {
        // Arrange
        await using var db = CreateInMemoryContext();
        var loggerMock = new Mock<ILogger<UpdateSettingsHandler>>();
        var validatorMock = new Mock<IValidator<UpdateSettingsRequest>>();
        validatorMock.Setup(v => v.ValidateAsync(It.IsAny<UpdateSettingsRequest>(), CancellationToken.None))
            .ReturnsAsync(new ValidationResult());

        var request = new UpdateSettingsRequest
        {
            DefaultWorkingHoursStart = new TimeOnly(8, 0),
            DefaultWorkingHoursEnd = new TimeOnly(16, 0),
            DefaultBreakTimeStart = new TimeOnly(12, 0),
            DefaultBreakTimeEnd = new TimeOnly(13, 0),
            DefaultUserRole = RoleName.User,
            Version = 1
        };

        // Act
        var result = await UpdateSettingsHandler.PostHandler(request, validatorMock.Object, db, loggerMock.Object);

        // Assert
        var rc = Assert.IsType<RazorComponentResult<UpdateSettingsComponent>>(result);
        Assert.True(rc.Parameters.ContainsKey("ShowSuccessMessage"));
        var savedRequest = rc.Parameters["UpdateSettingsRequest"] as UpdateSettingsRequest;
        Assert.NotNull(savedRequest);
        Assert.Equal(request.DefaultUserRole, savedRequest!.DefaultUserRole);

        // Ensure DB contains the new record
        var saved = await db.Settings.FirstOrDefaultAsync();
        Assert.NotNull(saved);
        Assert.Equal(RoleName.User, saved!.DefaultUserRole);
    }

    [Fact]
    public async Task PostHandler_InvalidRequest_ReturnsValidationErrors()
    {
        // Arrange
        await using var db = CreateInMemoryContext();
        var loggerMock = new Mock<ILogger<UpdateSettingsHandler>>();
        var validatorMock = new Mock<IValidator<UpdateSettingsRequest>>();
        validatorMock.Setup(v => v.ValidateAsync(It.IsAny<UpdateSettingsRequest>(), CancellationToken.None))
            .ReturnsAsync(new ValidationResult(new[] { new ValidationFailure("Field", "Error") }));

        var request = new UpdateSettingsRequest
        {
            DefaultWorkingHoursStart = new TimeOnly(8, 0),
            DefaultWorkingHoursEnd = new TimeOnly(16, 0),
            DefaultBreakTimeStart = new TimeOnly(12, 0),
            DefaultBreakTimeEnd = new TimeOnly(13, 0),
            DefaultUserRole = RoleName.User,
            Version = 1
        };

        // Act
        var result = await UpdateSettingsHandler.PostHandler(request, validatorMock.Object, db, loggerMock.Object);

        // Assert
        var rc = Assert.IsType<RazorComponentResult<UpdateSettingsComponent>>(result);
        Assert.True(rc.Parameters.ContainsKey("ValidationErrors"));
        var errors = rc.Parameters["ValidationErrors"] as Dictionary<string, string[]>;
        Assert.NotNull(errors);
        Assert.True(errors!.Count > 0);
    }

    [Fact]
    public async Task PostHandler_SaveChangesThrows_ReturnsErrorMessage()
    {
        // Arrange
        var faultyDb = CreateFaultyDataContext();
        var loggerMock = new Mock<ILogger<UpdateSettingsHandler>>();
        var validatorMock = new Mock<IValidator<UpdateSettingsRequest>>();
        validatorMock.Setup(v => v.ValidateAsync(It.IsAny<UpdateSettingsRequest>(), CancellationToken.None))
            .ReturnsAsync(new ValidationResult());

        var request = new UpdateSettingsRequest
        {
            DefaultWorkingHoursStart = new TimeOnly(8, 0),
            DefaultWorkingHoursEnd = new TimeOnly(16, 0),
            DefaultBreakTimeStart = new TimeOnly(12, 0),
            DefaultBreakTimeEnd = new TimeOnly(13, 0),
            DefaultUserRole = RoleName.User,
            Version = 1
        };

        // Act
        var result = await UpdateSettingsHandler.PostHandler(request, validatorMock.Object, faultyDb, loggerMock.Object);

        // Assert
        var rc = Assert.IsType<RazorComponentResult<UpdateSettingsComponent>>(result);
        Assert.True(rc.Parameters.ContainsKey("ErrorMessage"));
        var msg = rc.Parameters["ErrorMessage"] as string;
        Assert.NotNull(msg);
        Assert.Contains("DB failure", msg);
    }
}