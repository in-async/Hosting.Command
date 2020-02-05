using System.Threading;
using System.Threading.Tasks;

namespace Inasync.Hosting {

    public interface IConsoleHandler {

        Task InvokeAsync(CancellationToken cancellationToken);
    }
}
