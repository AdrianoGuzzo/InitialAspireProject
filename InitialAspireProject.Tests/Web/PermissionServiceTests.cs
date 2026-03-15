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

public class PermissionServiceTests
{
    private static (IPermissionService Service, Mock<IHttpContextAccessor> Accessor) CreateService(HttpResponseMessage response)
    {
        var handler = new FakeHandler(response);
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://localhost") };

        var sessionMock = new Mock<ISession>();
        byte[]? tokenBytes = Encoding.UTF8.GetBytes("test-token");
        sessionMock.Setup(x => x.TryGetValue("AuthToken", out tokenBytes)).Returns(true);

        var contextMock = new Mock<HttpContext>();
        contextMock.Setup(x => x.Session).Returns(sessionMock.Object);

        var accessorMock = new Mock<IHttpContextAccessor>();
        accessorMock.Setup(x => x.HttpContext).Returns(contextMock.Object);

        var service = new PermissionService(httpClient, accessorMock.Object, NullLogger<PermissionService>.Instance);
        return (service, accessorMock);
    }

    [Fact]
    public async Task GetAllPermissionsAsync_Success_ReturnsPermissions()
    {
        var expected = PermissionConstants.All;
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent.Create(expected)
        };
        var (service, _) = CreateService(response);

        var result = await service.GetAllPermissionsAsync();

        Assert.Equal(expected.Length, result.Length);
    }

    [Fact]
    public async Task GetAllPermissionsAsync_Error_ReturnsEmpty()
    {
        var response = new HttpResponseMessage(HttpStatusCode.InternalServerError);
        var (service, _) = CreateService(response);

        var result = await service.GetAllPermissionsAsync();

        Assert.Empty(result);
    }

    [Fact]
    public async Task GetAllRolePermissionsAsync_Success_ReturnsList()
    {
        var data = new List<RolePermissionsDto>
        {
            new() { RoleName = "Admin", Permissions = [PermissionConstants.CanViewSettings] }
        };
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent.Create(data)
        };
        var (service, _) = CreateService(response);

        var result = await service.GetAllRolePermissionsAsync();

        Assert.Single(result);
        Assert.Equal("Admin", result[0].RoleName);
    }

    [Fact]
    public async Task AssignPermissionAsync_Success_ReturnsTrue()
    {
        var response = new HttpResponseMessage(HttpStatusCode.OK);
        var (service, _) = CreateService(response);

        var result = await service.AssignPermissionAsync("Admin", PermissionConstants.CanViewSettings);

        Assert.True(result);
    }

    [Fact]
    public async Task AssignPermissionAsync_Failure_ReturnsFalse()
    {
        var response = new HttpResponseMessage(HttpStatusCode.BadRequest);
        var (service, _) = CreateService(response);

        var result = await service.AssignPermissionAsync("Admin", "Invalid");

        Assert.False(result);
    }

    [Fact]
    public async Task RemovePermissionAsync_Success_ReturnsTrue()
    {
        var response = new HttpResponseMessage(HttpStatusCode.OK);
        var (service, _) = CreateService(response);

        var result = await service.RemovePermissionAsync("Admin", PermissionConstants.CanViewSettings);

        Assert.True(result);
    }

    [Fact]
    public async Task RemovePermissionAsync_Failure_ReturnsFalse()
    {
        var response = new HttpResponseMessage(HttpStatusCode.NotFound);
        var (service, _) = CreateService(response);

        var result = await service.RemovePermissionAsync("Admin", PermissionConstants.CanViewSettings);

        Assert.False(result);
    }

    private class FakeHandler(HttpResponseMessage response) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            => Task.FromResult(response);
    }
}
