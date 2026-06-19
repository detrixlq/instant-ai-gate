using System;
using System.Collections.Generic;
using System.Text;

namespace InstantAIGate.Application.Interfaces.Inference
{
    /// <summary>
    /// Abstract marker for loaded model weights across any native backend.
    /// </summary>
    public interface IInferenceModel : IDisposable { }
}
