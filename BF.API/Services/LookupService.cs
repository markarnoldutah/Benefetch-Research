using System.Collections.Generic;
using System.Threading.Tasks;
using Ec.Api.Contracts;

namespace Ec.Api.Services
{
    public class LookupService : ILookupService
    {
        private readonly ILookupRepository _repo;

        public LookupService(ILookupRepository repo)
        {
            _repo = repo;
        }

        public Task<List<LookupItemDto>> GetVisitTypesAsync()
            => _repo.GetVisitTypesAsync();

        public Task<List<LookupItemDto>> GetCobReasonsAsync()
            => _repo.GetCobReasonsAsync();

        public Task<List<VisitTypeServiceTypesDto>> GetVisitTypeServiceTypesAsync()
            => _repo.GetVisitTypeServiceTypesAsync();
    }
}
