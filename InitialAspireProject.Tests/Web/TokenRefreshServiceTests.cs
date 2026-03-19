using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using InitialAspireProject.Shared.Constants;
using InitialAspireProject.Shared.Models;
using InitialAspireProject.Web.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace InitialAspireProject.Tests.Web;

public class TokenRefreshServiceTests
{
    private static (TokenRefreshService Service, Mock<ISession> Session) CreateService(
        HttpResponseMessage response, string? storedRefreshToken = "stored-refresh-token")
    {
        var handler = new TestHttpMessageHandler(response);
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://localhost") };

        var sessionMock = new Mock<ISession>();
        sessionMock.Setup(x => x.Id).Returns(Guid.NewGuid().ToString());
        if (storedRefreshToken is not null)
        {
            byte[]? tokenBytes = Encoding.UTF8.GetBytes(storedRefreshToken);
            sessionMock.Setup(x => x.TryGetValue(SessionConstants.RefreshTokenKey, out tokenBytes)).Returns(true);
        }
        else
        {
            byte[]? nullBytes = null;
            sessionMock.Setup(x => x.TryGetValue(SessionConstants.RefreshTokenKey, out nullBytes)).Returns(false);
        }

        var httpContext = new DefaultHttpContext { Session = sessionMock.Object };
        var accessorMock = new Mock<IHttpContextAccessor>();
        accessorMock.Setup(x => x.HttpContext).Returns(httpContext);

        var service = new TokenRefreshService(httpClient, accessorMock.Object, NullLogger<TokenRefreshService>.Instance);
        return (service, sessionMock);
    }

    [Fact]
    public async Task TryRefreshAsync_Success_UpdatesSessionAndReturnsTrue()
    {
        var loginResponse = new LoginResponse { Token = "new-jwt", RefreshToken = "new-refresh" };
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent.Create(loginResponse)
        };

        var (service, sessionMock) = CreateService(response);

        var result = await service.TryRefreshAsync();

        Assert.True(result);
        sessionMock.Verify(s => s.Set(SessionConstants.TokenKey, It.IsAny<byte[]>()), Times.Once);
        sessionMock.Verify(s => s.Set(SessionConstants.RefreshTokenKey, It.IsAny<byte[]>()), Times.Once);
    }

    [Fact]
    public async Task TryRefreshAsync_Unauthorized_ReturnsFalse()
    {
        var response = new HttpResponseMessage(HttpStatusCode.Unauthorized);
        var (service, _) = CreateService(response);

        var result = await service.TryRefreshAsync();

        Assert.False(result);
    }

    [Fact]
    public async Task TryRefreshAsync_NoRefreshTokenInSession_ReturnsFalse()
    {
        var response = new HttpResponseMessage(HttpStatusCode.OK);
        var (service, _) = CreateService(response, storedRefreshToken: null);

        var result = await service.TryRefreshAsync();

        Assert.False(result);
    }

    [Fact]
    public async Task TryRefreshAsync_NoHttpContext_ReturnsFalse()
    {
        var handler = new TestHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.OK));
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://localhost") };
        var accessorMock = new Mock<IHttpContextAccessor>();
        accessorMock.Setup(x => x.HttpContext).Returns((HttpContext?)null);

        var service = new TokenRefreshService(httpClient, accessorMock.Object, NullLogger<TokenRefreshService>.Instance);

        var result = await service.TryRefreshAsync();

        Assert.False(result);
    }

    [Fact]
    public async Task TryRefreshAsync_NetworkError_ReturnsFalse()
    {
        var handler = new TestHttpMessageHandler(new HttpRequestException("network error"));
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://localhost") };

        var sessionMock = new Mock<ISession>();
        sessionMock.Setup(x => x.Id).Returns(Guid.NewGuid().ToString());
        byte[]? tokenBytes = Encoding.UTF8.GetBytes("some-refresh-token");
        sessionMock.Setup(x => x.TryGetValue(SessionConstants.RefreshTokenKey, out tokenBytes)).Returns(true);

        var httpContext = new DefaultHttpContext { Session = sessionMock.Object };
        var accessorMock = new Mock<IHttpContextAccessor>();
        accessorMock.Setup(x => x.HttpContext).Returns(httpContext);

        var service = new TokenRefreshService(httpClient, accessorMock.Object, NullLogger<TokenRefreshService>.Instance);

        var result = await service.TryRefreshAsync();

        Assert.False(result);
    }
}
