using System;

namespace Microsoft.AspNetCore
{
    internal class ConflictItem
    {
        public ConflictItem(string fileName, string packageId, Version assemblyVersion, Version fileVersion)
        {
            FileName = fileName;
            PackageId = packageId;
            AssemblyVersion = assemblyVersion;
            FileVersion = fileVersion;
        }

        public string FileName { get; set; }
        public string PackageId { get; set; }
        public Version AssemblyVersion { get; set; }
        public Version FileVersion { get; set; }
    }
}
