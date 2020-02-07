using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Inasync.Hosting {

    public static class HostBuilderExtensions {

        public static Task InvokeAsync<THandler>(this IHostBuilder hostBuilder, CancellationToken cancellationToken = default) where THandler : IConsoleHandler {
            return hostBuilder
                .ConfigureServices(services => services.AddSingleton(typeof(THandler)))
                .InvokeAsync(provider => {
                    var handler = provider.GetRequiredService<THandler>();
                    return handler.InvokeAsync;
                }, cancellationToken);
        }

        public static Task InvokeAsync(this IHostBuilder hostBuilder, Func<CancellationToken, Task> handler, CancellationToken cancellationToken = default) {
            if (handler == null) { throw new ArgumentNullException(nameof(handler)); }

            return InvokeAsync(hostBuilder, _ => handler, cancellationToken);
        }

        public static async Task InvokeAsync(this IHostBuilder hostBuilder, Func<IServiceProvider, Func<CancellationToken, Task>> handlerFactory, CancellationToken cancellationToken = default) {
            if (hostBuilder == null) { throw new ArgumentNullException(nameof(hostBuilder)); }
            if (handlerFactory == null) { throw new ArgumentNullException(nameof(handlerFactory)); }

            var host = hostBuilder.Build();
            var provider = host.Services;
            var applicationLifetime = provider.GetRequiredService<IHostApplicationLifetime>();
            try {
                try {
                    await host.StartAsync(cancellationToken).ConfigureAwait(false);

                    var handler = handlerFactory(provider);
                    await applicationLifetime.InvokeAsync(handler, cancellationToken).ConfigureAwait(false);
                }
                catch (OperationCanceledException ex) when (applicationLifetime.ApplicationStopping.IsCancellationRequested) {
                    var logger = provider.GetRequiredService<ILoggerFactory>().CreateLogger(typeof(HostBuilderExtensions));
                    logger.LogInformation(ex.Message);
                }
                finally {
                    await host.StopAsync().ConfigureAwait(false);
                }
            }
            catch (Exception ex) {
                var logger = provider.GetRequiredService<ILoggerFactory>().CreateLogger(typeof(HostBuilderExtensions));
                logger.LogError(ex, "");

                var options = provider.GetRequiredService<IOptions<ConsoleHandlerOptions>>().Value;
                if (options.ThrowException) { throw; }
            }
            finally {
                // レガシー環境での deadlock 対応: https://github.com/dotnet/corefx/issues/26043 https://github.com/dotnet/runtime/issues/26165
                await Task.Yield();
                if (host is IAsyncDisposable asyncDisposable) {
                    await asyncDisposable.DisposeAsync().ConfigureAwait(false);
                }
                else {
                    host.Dispose();
                }
            }
        }
    }
}
