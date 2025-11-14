namespace ClientBooking.Features;

public interface IRequestHandler
{
    static abstract void Map(IEndpointRouteBuilder app);
}