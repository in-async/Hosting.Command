using System.Threading;
using System.Threading.Tasks;

namespace Inasync.Hosting {

    public interface ICommand {

        Task InvokeAsync(CancellationToken cancellationToken);
    }
}
