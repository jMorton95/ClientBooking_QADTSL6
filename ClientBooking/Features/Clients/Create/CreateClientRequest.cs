namespace ClientBooking.Features.Clients.Create;

public class CreateClientRequest
{
    public string Name { get; set; } = "";
    public string Email { get; set; } = "";
    public string Description { get; set; } = "";
}