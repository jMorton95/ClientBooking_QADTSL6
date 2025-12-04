using ClientBooking.Tests.Integration.Setup;

namespace ClientBooking.Tests.Integration;

[Collection("Integration Test Collection")]
public class HomePageTests(IntegrationTestContext context)
{
    [Fact]
    public async Task GetHomePage()
    {
        var response = await context.HttpClient.GetAsync("/");
        
        var content = await response.Content.ReadAsStringAsync();
        Assert.True(response.IsSuccessStatusCode);
    }
}