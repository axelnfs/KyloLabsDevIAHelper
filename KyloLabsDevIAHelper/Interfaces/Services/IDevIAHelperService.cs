using KyloLabs.DevIAHelper.Console.Models.Response;

namespace KyloLabs.DevIAHelper.Console.Interfaces.Services
{
    public interface IDevIAHelperService
    {
        Task<SimpleResponseModel> Input(string input);
    }
}
