using ClientBooking.Data.Entities;
using ClientBooking.Features.Clients;

namespace ClientBooking.Shared.Mapping;

//Mapper for Client entities
//Used to convert ClientRequest to Client and vice versa
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
    
    extension(Client client)
    {
        public ClientRequest MapClientToRequestModel()
        {
            return new ClientRequest
            {
                Name = client.Name,
                Email = client.Email,
                Description = client.Description
            };
        }

        public void UpdateClientFromClientRequest(ClientRequest clientRequest)
        {
            client.Name = clientRequest.Name;
            client.Email = clientRequest.Email;
            client.Description = clientRequest.Description;
        }
    }
}