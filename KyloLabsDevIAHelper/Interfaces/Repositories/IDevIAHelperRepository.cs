using KyloLabs.DevIAHelper.Console.Models.Response;
using KyloLabs.DevIAHelper.Core.Models;

namespace KyloLabs.DevIAHelper.Console.Interfaces.Repositories
{
    public interface IDevIAHelperRepository
    {
        Task<ResponseIA> InputAsync(string input);
    }
}
