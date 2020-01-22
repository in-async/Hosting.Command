using System.Threading;
using System.Threading.Tasks;

namespace Inasync {

    public interface ICommand {

        Task InvokeAsync(CancellationToken cancellationToken);
    }
}
