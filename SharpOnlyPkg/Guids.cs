// Guids.cs
// MUST match guids.h
using System;

namespace BoydYang.SharpBuildPkg
{
    static class GuidList
    {
        public const string guidSharpBuildPkgPkgString = "73ef989a-8690-4b93-8e3e-b68f31029ac5";
        public const string guidSharpBuildPkgCmdSetString = "6fd8943a-def6-4616-861e-56ef35a8c38c";

        public static readonly Guid guidSharpBuildPkgCmdSet = new Guid(guidSharpBuildPkgCmdSetString);
    };
}