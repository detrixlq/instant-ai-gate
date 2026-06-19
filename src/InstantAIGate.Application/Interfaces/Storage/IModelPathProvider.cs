using InstantAIGate.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace InstantAIGate.Application.Interfaces.Storage
{
    public interface IModelPathProvider
    {
        string GetModelDirectory(string repoId);
        string GetModelFilePath(string repoId, string fileName);
        Task<string> GetFullModelPathAsync(string repoId);
        Task<ModelManifest?> GetModelFromPathAsync(string fullPath);
    }
}
