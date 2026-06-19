

using InstantAIGate.Domain.Entities;

namespace InstantAIGate.Application.Interfaces.Storage
{
    public interface IModelStorageChecker
    {
        bool IsModelDownloaded(ModelManifest model);
        string? GetModelPath(ModelManifest model);
    }

}
