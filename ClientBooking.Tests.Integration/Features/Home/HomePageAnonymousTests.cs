using System.Net;
using ClientBooking.Tests.Integration.Shared;

namespace ClientBooking.Tests.Integration.Features.Home;

[Collection("Anonymous Test Collection")]
public class HomePageAnonymousTests(AnonymousIntegrationTestContext context)
{
    [Fact]
    public async Task Get_NoSession_RedirectsToLogin()
    {
        var response = await context.HttpClient.GetAsync("/get");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        Assert.True(response.Headers.Contains("HX-Redirect"));
        Assert.Equal("/login", response.Headers.GetValues("HX-Redirect").First());
    }
}