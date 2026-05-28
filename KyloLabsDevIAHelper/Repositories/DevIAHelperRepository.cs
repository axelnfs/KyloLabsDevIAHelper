using KyloLabs.DevIAHelper.Console.Interfaces.Repositories;
using KyloLabs.DevIAHelper.Console.Models.Response;
using KyloLabs.DevIAHelper.Core;
using KyloLabs.DevIAHelper.Core.Models;

namespace KyloLabs.DevIAHelper.Console.Repositories
{
    public class DevIAHelperRepository : IDevIAHelperRepository
    {
        private readonly DevIAHelperFunctions _devIAHelperFunctions;

        public DevIAHelperRepository(DevIAHelperFunctions devIAHelperFunctions)
        {
            _devIAHelperFunctions = devIAHelperFunctions;
        }

        public async Task<ResponseIA> InputAsync(string input)
        {
            ResponseIA result = await _devIAHelperFunctions.InputAsync(input);
            return result;
        }
    }
}
