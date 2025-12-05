using ClientBooking.Tests.Integration.Shared;

namespace ClientBooking.Tests.Integration.Features.Home;

[Collection("Authenticated Test Collection")]
public class HomePageAuthenticatedTests(AuthenticatedIntegrationTestContext context)
{
    private readonly IntegrationTestContext _context = context;

    [Fact]
    public async Task Get_HomePage_ReturnsSuccessAndContentType()
    {
        var response = await _context.HttpClient.GetAsync("/");
        
        Assert.True(response.IsSuccessStatusCode);
        Assert.Contains("text/html", response.Content.Headers.ContentType?.MediaType, StringComparison.OrdinalIgnoreCase);
    }
    
    // [Fact]
    // public async Task Get_HomePageComponent_ReturnsSuccessAndContentType()
    // {
    //     var response = await _context.HttpClient.GetAsync("/get");
    //     Assert.True(response.IsSuccessStatusCode);
    //     Assert.Contains("text/html", response.Content.Headers.ContentType?.MediaType, StringComparison.OrdinalIgnoreCase);
    // }
    
    // [Fact]
    // public async Task Get_ValidUser_RendersHomePageComponent()
    // {
    //     var response = await _context.HttpClient.GetAsync("/get");
    //     var html = await response.Content.ReadAsStringAsync();
    //
    //     Assert.Contains("Dashboard", html);
    //     Assert.Contains("Weekly Bookings", html);
    //     Assert.Contains("Today's Schedule", html);
    //     Assert.Contains("Your Upcoming Bookings", html);
    // }
}