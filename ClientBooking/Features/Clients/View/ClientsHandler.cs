using ClientBooking.Components.Generic;
using ClientBooking.Data;
using Microsoft.AspNetCore.Mvc;

namespace ClientBooking.Features.Clients.View;

public class GetClientsHandler : IRequestHandler
{
    public const int ClientsPagePageSize = 5;
    public static void Map(IEndpointRouteBuilder app)
    {
        app.MapGet("/clients/get", GetHandler).RequireAuthorization();
    }

    //Request handler that returns the clients page.
    //The clients are retrieved from the database and sorted based on the specified criteria.
    //The clients are also paginated.
    private static async Task<Results<RazorComponentResult<ClientsComponent>, RazorComponentResult<ErrorMessageComponent>>> GetHandler(
        [FromServices] DataContext dataContext,
        ILogger<GetClientsHandler> logger,
        [FromQuery] int page = 1,
        [FromQuery] string sortBy = "name",
        [FromQuery] string sortDirection = "asc",
        [FromQuery] string search = "")
    {
        try
        {
            var query = dataContext.Clients
                .Include(c => c.Bookings)
                .AsQueryable();

            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(c => 
                    c.Name.ToLower().Contains(search.ToLower()) || 
                    c.Email.ToLower().Contains(search.ToLower()) ||
                    c.Description.ToLower().Contains(search.ToLower()));
            }

            query = sortBy.ToLower() switch
            {
                "email" => sortDirection == "desc" 
                    ? query.OrderByDescending(c => c.Email)
                    : query.OrderBy(c => c.Email),
                "bookings" => sortDirection == "desc"
                    ? query.OrderByDescending(c => c.Bookings.Count)
                    : query.OrderBy(c => c.Bookings.Count),
                _ => sortDirection == "desc"
                    ? query.OrderByDescending(c => c.Name)
                    : query.OrderBy(c => c.Name)
            };

            var totalCount = await query.CountAsync();
            var totalPages = (int)Math.Ceiling(totalCount / (double)ClientsPagePageSize);
            
            var clients = await query
                .Skip((page - 1) * ClientsPagePageSize)
                .Take(ClientsPagePageSize)
                .ToListAsync();

            return new RazorComponentResult<ClientsComponent>(new
            {
                Clients = clients,
                CurrentPage = page,
                PageSize = ClientsPagePageSize,
                TotalCount = totalCount,
                TotalPages = totalPages,
                SortBy = sortBy,
                SortDirection = sortDirection,
                SearchTerm = search
            });
        
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error occurred loading clients.");
            return new RazorComponentResult<ErrorMessageComponent>(new
            {
                ErrorMessage = $"Error occurred updating client. {ex.Message}",
            });
        }
    }
}