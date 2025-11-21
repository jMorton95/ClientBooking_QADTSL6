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

    private static async Task<RazorComponentResult<ClientsComponent>> GetHandler(
        [FromServices] DataContext dataContext,
        [FromQuery] int page = 1,
        [FromQuery] string sortBy = "name",
        [FromQuery] string sortDirection = "asc",
        [FromQuery] string search = "")
    {
        //Construct initial query
        var query = dataContext.Clients
            .Include(c => c.Bookings)
            .AsQueryable();

        //Apply search filter and sort options
        if (!string.IsNullOrEmpty(search))
        {
            query = query.Where(c => 
                c.Name.Contains(search) || 
                c.Email.Contains(search) ||
                c.Description.Contains(search));
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

        //Apply pagination logic
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
}