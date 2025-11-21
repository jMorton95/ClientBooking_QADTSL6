using ClientBooking.Data.Entities;
using ClientBooking.Features.Clients.Create;

namespace ClientBooking.Shared.Mapping;

public static class ClientMapping
{
    public static Client MapCreateClientRequestToEntity(this CreateClientRequest createClientRequest)
    {
        return new Client
        {
            Name = createClientRequest.Name,
            Email = createClientRequest.Email,
            Description = createClientRequest.Description,
        };
    }
}