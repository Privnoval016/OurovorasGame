// Shim required by Unity's .NET Standard 2.1 / C# 9+ init-only properties.
// Without this, the compiler cannot find IsExternalInit in the BCL.
namespace System.Runtime.CompilerServices
{
    internal sealed class IsExternalInit { }
}

