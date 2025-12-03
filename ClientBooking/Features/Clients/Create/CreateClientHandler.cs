using ClientBooking.Data;
using ClientBooking.Features.Clients.Shared;
using ClientBooking.Shared.Mapping;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;

namespace ClientBooking.Features.Clients.Create;

public class CreateClientHandler : IRequestHandler
{
    public static void Map(IEndpointRouteBuilder app)
    {
        app.MapPost("/client/create", Handler).RequireAuthorization();
    }

    private static async Task<Results<HtmxRedirectResult, RazorComponentResult<CreateClientPage>>>
        Handler([FromForm] ClientRequest clientRequest, IValidator<ClientRequest> validator, DataContext dataContext, ILogger<CreateClientHandler> logger)
    {
        try
        {
            var validationResult = await validator.ValidateAsync(clientRequest);
            
            if (!validationResult.IsValid)
            {
                return new RazorComponentResult<CreateClientPage>(new
                {
                    createClientRequest = clientRequest,
                    ValidationErrors = validationResult.ToDictionary()
                });
            }
            
            var doesClientEmailAlreadyExist = await dataContext.Clients.AnyAsync(c => c.Email == clientRequest.Email);

            if (doesClientEmailAlreadyExist)
            {
                logger.LogError("Client with email address {email} already exists.", clientRequest.Email);
                return new RazorComponentResult<CreateClientPage>(new
                {
                    createClientRequest = clientRequest,
                    ErrorMessage = "Client with this email address already exists.",
                });
            }

            var newClientEntity = clientRequest.MapCreateClientRequestToEntity();
            
            await dataContext.Clients.AddAsync(newClientEntity);
            await dataContext.SaveChangesAsync();
            
            return new HtmxRedirectResult("/clients");
        }
        catch (Exception e)
        {
            logger.LogError(e, "An unexpected error occurred while trying to create client.");
            return new RazorComponentResult<CreateClientPage>(new
            {
                createClientRequest = clientRequest,
                ErrorMessage = e.Message,
            });
        }
    }
}