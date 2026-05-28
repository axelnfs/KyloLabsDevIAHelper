using KyloLabs.DevIAHelper.Console.Interfaces.Repositories;
using KyloLabs.DevIAHelper.Console.Interfaces.Services;
using KyloLabs.DevIAHelper.Console.Models.Response;
using KyloLabs.DevIAHelper.Console.Repositories;
using KyloLabs.DevIAHelper.Core;

namespace KyloLabs.DevIAHelper.Console.Services
{
    public class DevIAHelperService : IDevIAHelperService
    {
        private readonly IDevIAHelperRepository _devIAHelperRepository;  
        public DevIAHelperService(IDevIAHelperRepository devIAHelperRepository)  
        {
            _devIAHelperRepository = devIAHelperRepository;
        }

        public async Task<SimpleResponseModel> Input(string input)
        {
            var response = await _devIAHelperRepository.InputAsync(input);
            if (!response.IsSuccess)
            {
                return new SimpleResponseModel
                {
                    IsError = true,
                    Message = response.ErrorMessage,
                    Data = response
                };
            }
            return new SimpleResponseModel
            {
                IsError = false,
                Message = "All ok",
                Data = response
            };
        }
    }
}
