using ClientBooking.Data.Entities;
using ClientBooking.Features.Clients;
using ClientBooking.Features.Clients.Shared;

namespace ClientBooking.Shared.Mapping;

public static class ClientMapping
{
    public static Client MapCreateClientRequestToEntity(this ClientRequest clientRequest)
    {
        return new Client
        {
            Name = clientRequest.Name,
            Email = clientRequest.Email,
            Description = clientRequest.Description,
        };
    }
    
    public static ClientRequest MapClientToRequestModel(this Client client)
    {
        return new ClientRequest
        {
            Name = client.Name,
            Email = client.Email,
            Description = client.Description
        };
    }

    public static void UpdateClientFromClientRequest(this Client client)
    {
        client.Name = client.Name;
        client.Email = client.Email;
        client.Description = client.Description;
    }
}