using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace Inasync {

    public static class Command {

        public static async Task InvokeAsync<TCommand>(IServiceProvider provider, CancellationToken cancellationToken) where TCommand : ICommand {
            if (provider == null) { throw new ArgumentNullException(nameof(provider)); }

            var command = ActivatorUtilities.GetServiceOrCreateInstance<TCommand>(provider);
            try {
                await command.InvokeAsync(cancellationToken).ConfigureAwait(false);
            }
            finally {
                switch (command) {
                    case IAsyncDisposable asyncDisposable:
                        await asyncDisposable.DisposeAsync().ConfigureAwait(false);
                        break;

                    case IDisposable disposable:
                        disposable.Dispose();
                        break;
                }
            }
        }
    }
}
