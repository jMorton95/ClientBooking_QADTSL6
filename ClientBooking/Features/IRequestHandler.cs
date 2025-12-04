namespace ClientBooking.Features;

public interface IRequestHandler
{
    //Provide a way to map the endpoint to the handler
    static abstract void Map(IEndpointRouteBuilder app);
}