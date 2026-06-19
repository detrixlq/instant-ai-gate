
using InstantAIGate.Domain.Entities;

namespace InstantAIGate.Application.Interfaces.Catalog
{
    public interface IModelRegistry
    {
        Task<IReadOnlyList<ModelManifest>> GetAllModelsAsync();
        Task<ModelManifest?> GetModelAsync(string repoId);

    }
}
