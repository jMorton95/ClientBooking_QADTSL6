using ClientBooking.Data;
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
        Handler([FromForm] CreateClientRequest createClientRequest, IValidator<CreateClientRequest> validator, DataContext dataContext)
    {
        try
        {
            var validationResult = await  validator.ValidateAsync(createClientRequest);
            
            if (!validationResult.IsValid)
            {
                return new RazorComponentResult<CreateClientPage>(new
                {
                    createClientRequest,
                    ValidationErrors = validationResult.ToDictionary()
                });
            }
            
            var doesClientEmailAlreadyExist = await dataContext.Clients.AnyAsync(c => c.Email == createClientRequest.Email);

            if (doesClientEmailAlreadyExist)
            {
                return new RazorComponentResult<CreateClientPage>(new
                {
                    createClientRequest,
                    ErrorMessage = "Client with this email address already exists.",
                });
            }

            var newClientEntity = createClientRequest.MapCreateClientRequestToEntity();
            
            await dataContext.Clients.AddAsync(newClientEntity);
            await dataContext.SaveChangesAsync();
            
            return new HtmxRedirectResult("/clients");
        }
        catch (Exception e)
        {
            return new RazorComponentResult<CreateClientPage>(new
            {
                createClientRequest,
                ErrorMessage = e.Message,
            });
        }
    }
}