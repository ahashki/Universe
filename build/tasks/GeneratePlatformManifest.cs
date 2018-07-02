// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using System.IO;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using Microsoft.AspNetCore;

namespace RepoTasks
{
    public class GeneratePlatformManifest : Task
    {
        [Required]
        public ITaskItem[] Assemblies { get; set; }

        [Required]
        public ITaskItem[] Dependencies { get; set; }

        [Required]
        public string OutputPath { get; set; }

        public override bool Execute()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(OutputPath));

            var preventUpgrades = Dependencies.ToDictionary(d => d.ItemSpec, d => bool.Parse(d.GetMetadata("PreventUpgrade")), StringComparer.OrdinalIgnoreCase);

            using (var stream = File.OpenWrite(OutputPath))
            using (var writer = new StreamWriter(stream))
            {
                foreach (var assembly in Assemblies.OrderBy(a => a.ItemSpec))
                {
                    var fileName = Path.GetFileName(assembly.ItemSpec);
                    var packageId = assembly.GetMetadata("PackageName"); // this metadata should be set by RunResolvePublishAssemblies
                    var assemblyInfo = AssemblyInfo.Get(assembly.ItemSpec);

                    if (!assemblyInfo.IsManagedAssembly)
                    {
                        Log.LogMessage($"Skipping {assembly.ItemSpec} because it does not appear to be a managed assembly");
                        continue;
                    }

                    Version assemblyVersion;
                    Version fileVersion;
                    if (!preventUpgrades.TryGetValue(packageId, out var preventUpgrade) || preventUpgrade)
                    {
                        // We are intentionally setting the patch version to 999. We are abusing the behavior of PlatformManifest to trick
                        // version conflict resolution into thinking the shared framework always has the higher version. This prevents
                        // users from unintentionally upgrading assemblies out of the shared framework when using packages such as
                        // Microsoft.EntityFramework.SQLite which share a dependency with Microsoft.AspNetCore.App, but would have caused
                        // a higher version to be consumed.
                        assemblyVersion = new Version(
                            assemblyInfo.AssemblyVersion.Major,
                            assemblyInfo.AssemblyVersion.Minor,
                            999,
                            0);

                        // file version isn't actually used because assembly version should always be higher
                        fileVersion = new Version(0, 0, 0, 0);
                    }
                    else
                    {
                        assemblyVersion = assemblyInfo.AssemblyVersion;
                        fileVersion = assemblyInfo.FileVersion;
                    }

                    writer.WriteLine(string.Join("|",
                        fileName,
                        packageId,
                        assemblyVersion.ToString(),
                        fileVersion.ToString()));
                }
            }
            return true;
        }
    }
}
