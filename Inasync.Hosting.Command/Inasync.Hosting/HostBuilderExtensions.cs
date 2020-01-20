using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Inasync.Hosting {

    public static class HostBuilderExtensions {

        public static Task InvokeAsync<TCommand>(this IHostBuilder hostBuilder, CancellationToken cancellationToken = default) where TCommand : ICommand {
            return InvokeAsync(hostBuilder, provider => ct => Command.InvokeAsync<TCommand>(provider, ct), cancellationToken);
        }

        public static Task InvokeAsync(this IHostBuilder hostBuilder, Func<CancellationToken, Task> command, CancellationToken cancellationToken = default) {
            if (command == null) { throw new ArgumentNullException(nameof(command)); }

            return InvokeAsync(hostBuilder, _ => command, cancellationToken);
        }

        public static async Task InvokeAsync(this IHostBuilder hostBuilder, Func<IServiceProvider, Func<CancellationToken, Task>> commandFactory, CancellationToken cancellationToken = default) {
            if (hostBuilder == null) { throw new ArgumentNullException(nameof(hostBuilder)); }
            if (commandFactory == null) { throw new ArgumentNullException(nameof(commandFactory)); }

            var host = hostBuilder.Build();
            var provider = host.Services;
            var applicationLifetime = provider.GetRequiredService<IHostApplicationLifetime>();
            try {
                try {
                    await host.StartAsync(cancellationToken).ConfigureAwait(false);

                    var command = commandFactory(provider);
                    await applicationLifetime.InvokeAsync(command, cancellationToken).ConfigureAwait(false);
                }
                finally {
                    await host.StopAsync().ConfigureAwait(false);
                }
            }
            catch (OperationCanceledException ex) when (applicationLifetime.ApplicationStopping.IsCancellationRequested) {
                var logger = provider.GetRequiredService<ILoggerFactory>().CreateLogger(typeof(HostBuilderExtensions));
                logger.LogInformation(ex.Message);
            }
            finally {
                // レガシー環境での deadlock 対応: https://github.com/dotnet/corefx/issues/26043
                await Task.Run(async () => {
                    if (host is IAsyncDisposable asyncDisposable) {
                        await asyncDisposable.DisposeAsync().ConfigureAwait(false);
                    }
                    else {
                        host.Dispose();
                    }
                }).ConfigureAwait(false);
            }
        }
    }
}
