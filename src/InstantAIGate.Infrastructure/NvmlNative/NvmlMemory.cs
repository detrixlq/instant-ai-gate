using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace InstantAIGate.Infrastructure.NvmlNative
{
    [StructLayout(LayoutKind.Sequential)]
    public struct NvmlMemory
    {
        public ulong Total;
        public ulong Free;
        public ulong Used;
    }
}
