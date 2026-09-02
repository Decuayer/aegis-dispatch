using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.SignalR.Client;
using SocarDispatch.Domain.Enums;
using SocarDispatch.IntegrationTests.Fixtures;

namespace SocarDispatch.IntegrationTests.Common;

public static class SignalRTestHelper
{
    public static HubConnection CreateHubConnection(
        this CustomWebApplicationFactory factory,
        string hubPath,
        RoleType role,
        Guid? userId = null,
        string email = "signalr_user@socar.az")
    {
        var targetUserId = userId ?? Guid.NewGuid();
        var token = AuthTestHelper.GenerateToken(targetUserId, email, "SignalR Tester", role);
        var hubUrl = new Uri(factory.Server.BaseAddress, hubPath.TrimStart('/'));

        return new HubConnectionBuilder()
            .WithUrl(hubUrl, options =>
            {
                options.HttpMessageHandlerFactory = _ => factory.Server.CreateHandler();
                options.AccessTokenProvider = () => Task.FromResult<string?>(token);
                options.Transports = HttpTransportType.LongPolling;
            })
            .Build();
    }
}
