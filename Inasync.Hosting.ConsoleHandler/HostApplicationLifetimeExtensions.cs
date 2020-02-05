using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;

namespace Inasync.Hosting {

    internal static class HostApplicationLifetimeExtensions {

        public static async Task InvokeAsync(this IHostApplicationLifetime applicationLifetime, Func<CancellationToken, Task> handler, CancellationToken cancellationToken = default) {
            Debug.Assert(applicationLifetime != null);
            Debug.Assert(handler != null);

            using (var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, applicationLifetime.ApplicationStopping)) {
                var stoppingToken = linkedCts.Token;

                await handler(stoppingToken).ConfigureAwait(false);
            }
        }
    }
}
