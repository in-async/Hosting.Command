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
        [DataRow(new[] { "foo", "bar" })]
        public Task Usage1(string[] args) {
            // (Disposable な) ICommand を呼ぶシナリオ
            return Host.CreateDefaultBuilder(args).InvokeAsync<DisposableCommand>();
        }

        [TestMethod]
        [DataRow(new[] { "foo", "bar" })]
        public Task Usage2(string[] args) {
            // Disposable なユースケースを呼ぶシナリオ
            return Host.CreateDefaultBuilder(args)
                .ConfigureServices(services => services.AddSingleton<DisposableUseCase>())
                .InvokeAsync(provider => {
                    var command = provider.GetRequiredService<DisposableUseCase>();
                    return cancellationToken => command.ExecuteAsync(args, cancellationToken);
                });
        }

        [TestMethod]
        [DataRow(new[] { "foo", "bar" })]
        public Task Usage3(string[] args) {
            // デリゲートを呼ぶシナリオ
            return Host.CreateDefaultBuilder(args).InvokeAsync(provider => {
                var logger = provider.GetRequiredService<ILogger<UsageTests>>();

                return cancellationToken => {
                    logger.LogInformation("Usage3");
                    return Task.CompletedTask;
                };
            });
        }

        [TestMethod]
        [DataRow(new[] { "foo", "bar" })]
        public void Usage4(string[] args) {
            // 例外を抑制するシナリオ
            TestAA
                .Act(() => Host.CreateDefaultBuilder(args).InvokeAsync<ThrowExceptionCommand>())
                .Assert();

            // 例外を返すシナリオ
            TestAA
                .Act(() => Host
                    .CreateDefaultBuilder(args)
                    .ConfigureServices(services => services.Configure<CommandOptions>(x => x.ThrowException = true))
                    .InvokeAsync<ThrowExceptionCommand>()
                )
                .Assert<ApplicationException>();
        }

        [TestMethod]
        [DataRow(new[] { "foo", "bar" })]
        public void Usage5(string[] args) {
            // キャンセル例外を返すシナリオ
            var cts = new CancellationTokenSource();
            cts.Cancel();
            TestAA
                .Act(() => Host
                    .CreateDefaultBuilder(args)
                    .ConfigureServices(services => services.Configure<CommandOptions>(x => x.ThrowException = true))
                    .InvokeAsync<DisposableCommand>(cts.Token)
                )
                .Assert<OperationCanceledException>();
        }

        private sealed class DisposableCommand : ICommand, IDisposable {
            private readonly ILogger<DisposableCommand> _logger;

            public DisposableCommand(ILogger<DisposableCommand> logger) {
                _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            }

            public void Dispose() {
                _logger.LogInformation("Dispose");
            }

            public async Task InvokeAsync(CancellationToken cancellationToken) {
                using (_logger.BeginScope(nameof(InvokeAsync))) {
                    _logger.LogInformation("Start");
                    await Task.Delay(100, cancellationToken).ConfigureAwait(false);
                    _logger.LogInformation("End");
                }
            }
        }

        private sealed class DisposableUseCase : IDisposable {
            private readonly ILogger<DisposableUseCase> _logger;

            public DisposableUseCase(ILogger<DisposableUseCase> logger) {
                _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            }

            public void Dispose() {
                _logger.LogInformation("Dispose");
            }

            public async Task ExecuteAsync(string[] args, CancellationToken cancellationToken) {
                using (_logger.BeginScope(nameof(ExecuteAsync))) {
                    _logger.LogInformation("Start");
                    _logger.LogInformation("args: " + string.Join(" ", args));
                    await Task.Delay(100, cancellationToken).ConfigureAwait(false);
                    _logger.LogInformation("End");
                }
            }
        }

        private sealed class ThrowExceptionCommand : ICommand {
            private readonly ILogger<ThrowExceptionCommand> _logger;

            public ThrowExceptionCommand(ILogger<ThrowExceptionCommand> logger) {
                _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            }

            public Task InvokeAsync(CancellationToken cancellationToken) {
                using (_logger.BeginScope(nameof(InvokeAsync))) {
                    _logger.LogInformation("Start");
                    throw new ApplicationException();
                }
            }
        }
    }
}
