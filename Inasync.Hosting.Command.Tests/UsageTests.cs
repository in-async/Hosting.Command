using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Inasync.Hosting.Tests {

    [TestClass]
    public class UsageTests {

        [TestMethod]
        public Task Usage1(string[] args) {
            return Host.CreateDefaultBuilder(args).InvokeAsync<InternalCommand>();
        }

        [TestMethod]
        public Task Usage2(string[] args) {
            return Host.CreateDefaultBuilder(args).InvokeAsync(provider => {
                var logger = provider.GetRequiredService<ILogger<UsageTests>>();

                return async cancellationToken => {
                    logger.LogInformation("Pre Usage2");

                    await Command.InvokeAsync<InternalCommand>(provider, cancellationToken).ConfigureAwait(false);
                    await Task.Delay(1000, cancellationToken).ConfigureAwait(false);

                    logger.LogInformation("Post Usage2");
                };
            });
        }

        [TestMethod]
        public Task Usage3(string[] args) {
            return Host.CreateDefaultBuilder(args).InvokeAsync(provider => {
                var logger = provider.GetRequiredService<ILogger<UsageTests>>();

                return cancellationToken => {
                    logger.LogInformation("Usage3");

                    throw new ApplicationException();
                };
            });
        }

        private sealed class InternalCommand : ICommand {
            private readonly ILogger<InternalCommand> _logger;

            public InternalCommand(ILogger<InternalCommand> logger) {
                _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            }

            public async Task InvokeAsync(CancellationToken cancellationToken) {
                _logger.LogInformation("Pre InvokeAsync");
                await Task.Delay(1000, cancellationToken).ConfigureAwait(false);
                _logger.LogInformation("Post InvokeAsync");
            }
        }
    }
}
