// using ClientBooking.Authentication;
//
// namespace ClientBooking.Features.Home;
//
// public class HomeHandler : IRequestHandler
// {
//     public static void Map(IEndpointRouteBuilder app)
//     {
//         app.MapGet("/", HandleAsync);
//     }
//
//     private static Task<RazorComponentResult<HomePage>> HandleAsync()
//     {
//         return Task.FromResult(new RazorComponentResult<HomePage>());
//     }
// }