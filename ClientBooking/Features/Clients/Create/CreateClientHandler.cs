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
        Handler([FromForm] ClientRequest clientRequest, IValidator<ClientRequest> validator, DataContext dataContext)
    {
        try
        {
            var validationResult = await  validator.ValidateAsync(clientRequest);
            
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
            return new RazorComponentResult<CreateClientPage>(new
            {
                createClientRequest = clientRequest,
                ErrorMessage = e.Message,
            });
        }
    }
}