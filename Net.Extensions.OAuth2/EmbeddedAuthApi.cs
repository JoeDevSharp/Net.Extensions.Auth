using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;

public class OAuth2EmbeddedServer
{
    private IWebHost? _webHost;
    private TaskCompletionSource<string>? _codeTcs;

    public Task<string> StartAndWaitForCodeAsync(int port = 60000, CancellationToken cancellationToken = default)
    {
        _codeTcs = new TaskCompletionSource<string>();

        _webHost = new WebHostBuilder()
            .UseKestrel()
            .UseUrls($"http://localhost:{port}")
            .Configure(app =>
            {
                app.Run(async context =>
                {
                    if (context.Request.Path == "/callback")
                    {
                        var code = context.Request.Query["code"];
                        if (!string.IsNullOrEmpty(code))
                        {
                            _codeTcs.TrySetResult(code);

                            await context.Response.WriteAsync("Authorization code received. You can close this window.");
                        }
                        else
                        {
                            await context.Response.WriteAsync("No code received in the callback.");
                        }
                    }
                    else
                    {
                        context.Response.StatusCode = 404;
                        await context.Response.WriteAsync("Not found");
                    }
                });
            })
            .Build();

        _webHost.Start();

        cancellationToken.Register(() =>
        {
            _codeTcs.TrySetCanceled();
            Stop();
        });

        return _codeTcs.Task;
    }

    public void Stop()
    {
        _webHost?.StopAsync().GetAwaiter().GetResult();
        _webHost?.Dispose();
        _webHost = null;
    }
}
