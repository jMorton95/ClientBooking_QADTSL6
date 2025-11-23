using ClientBooking.Data;
using ClientBooking.Features.Clients.Shared;
using ClientBooking.Shared.Mapping;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;

namespace ClientBooking.Features.Clients.Update;

public class UpdateClientHandler : IRequestHandler
{
    public static void Map(IEndpointRouteBuilder app)
    {
        app.MapGet("/client/edit/get/{id:int}", GetHandler).RequireAuthorization();
        app.MapPost("/client/edit/{id:int}", PostHandler).RequireAuthorization();
    }
    
    private static async Task<RazorComponentResult<UpdateClientComponent>>
        GetHandler([FromRoute] int id, DataContext dataContext)
    {
        try
        {
            var client = await dataContext.Clients
                .FirstOrDefaultAsync(c => c.Id == id);

            if (client == null)
            {
                return new RazorComponentResult<UpdateClientComponent>(new { ClientNotFound = true });
            }

            var clientRequest = client.MapClientToRequestModel();

            return new RazorComponentResult<UpdateClientComponent>(new 
            { 
                id, ClientRequest = clientRequest 
            });
        }
        catch (Exception ex)
        {
            return new RazorComponentResult<UpdateClientComponent>(new
            {
                ErrorMessage = $"An error occurred while loading the client: {ex.Message}"
            });
        }
    }
    
    private static async Task<RazorComponentResult<UpdateClientComponent>>
        PostHandler([FromRoute] int id, [FromForm] ClientRequest clientRequest, IValidator<ClientRequest> validator, DataContext dataContext)
    {
        try
        {
            var validationResult = await validator.ValidateAsync(clientRequest);
            
            if (!validationResult.IsValid)
            {
                return new RazorComponentResult<UpdateClientComponent>(new
                {
                    id, ClientRequest = clientRequest,
                    ValidationErrors = validationResult.ToDictionary()
                });
            }

            var client = await dataContext.Clients
                .FirstOrDefaultAsync(c => c.Id == id);

            if (client == null)
            {
                return new RazorComponentResult<UpdateClientComponent>(new { id, ClientNotFound = true });
            }

            var emailExists = await dataContext.Clients
                .AnyAsync(c => c.Email == clientRequest.Email && c.Id != id);

            if (emailExists)
            {
                return new RazorComponentResult<UpdateClientComponent>(new
                {
                    id,
                    ClientRequest = clientRequest,
                    ErrorMessage = "Another client with this email address already exists.",
                });
            }
            
            client.UpdateClientFromClientRequest(clientRequest);

            await dataContext.SaveChangesAsync();
            
            return new RazorComponentResult<UpdateClientComponent>(new
            {
                id,
                ClientRequest = clientRequest,
                ShowSuccessMessage = true
            });
        }
        catch (Exception e)
        {
            return new RazorComponentResult<UpdateClientComponent>(new
            {
                id,
                clientRequest,
                ErrorMessage = $"Error occurred updating client. {e.Message}",
            });
        }
    }
}