// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Xunit;

namespace Microsoft.AspNetCore
{
    [Trait("Category", "Metapackage")]
    public class PlatformManifestTests
    {
        [Fact]
        public void GeneratedManifestIncludesAllBinaries()
        {
            var manifestItems = PlatformManifestReader.LoadConflictItems(TestData.GetAppPlatformManifestFilePath()).ToDictionary(c => c.FileName, c => c, StringComparer.Ordinal);
            var sharedFxRoot = Path.Combine(TestData.GetDotNetRoot(), "shared", "Microsoft.AspNetCore.App", TestData.GetPackageVersion());
            var binaries = Directory.EnumerateFiles(sharedFxRoot)
                .Select(path => AssemblyInfo.Get(path));

            Assert.NotEmpty(binaries);

            Assert.All(binaries.Where(a => !a.IsManagedAssembly), unmanagedBinary =>
              {
                  var fileName = Path.GetFileName(unmanagedBinary.Path);
                  Assert.False(manifestItems.ContainsKey(fileName), $"Manifest should not contain an entry for unmanaged binary {fileName}");
              });

            Assert.All(binaries.Where(a => a.IsManagedAssembly), assembly =>
              {
                  var fileName = Path.GetFileName(assembly.Path);
                  Assert.True(manifestItems.TryGetValue(fileName, out var manifestItem), $"Manifest should contain an entry for assembly {fileName}");
                  Assert.NotNull(manifestItem.PackageId);
                  Assert.NotNull(manifestItem.AssemblyVersion);

                  if (_upgradeablePackage.Contains(manifestItem.PackageId))
                  {
                      Assert.Equal(assembly.AssemblyVersion, manifestItem.AssemblyVersion);
                      Assert.Equal(assembly.FileVersion, manifestItem.FileVersion);
                  }
                  else
                  {
                      Assert.True(assembly.AssemblyVersion < manifestItem.AssemblyVersion, $"The binary {fileName} assembly version '{assembly.AssemblyVersion}' should be less than the assembly version listed in the platform manifest, '{manifestItem.AssemblyVersion}'");
                      Assert.Equal(new Version(0, 0, 0, 0), manifestItem.FileVersion);
                  }
              });
        }

        private static readonly HashSet<string> _upgradeablePackage = new HashSet<string>
        {
            "System.Data.SqlClient",
            "Microsoft.AspNet.WebApi.Client",
            "Microsoft.DotNet.PlatformAbstractions",
            "Microsoft.IdentityModel.Logging",
            "Microsoft.IdentityModel.Protocols.OpenIdConnect",
            "Microsoft.IdentityModel.Protocols.WsFederation",
            "Microsoft.IdentityModel.Protocols",
            "Microsoft.IdentityModel.Tokens.Saml",
            "Microsoft.IdentityModel.Tokens",
            "Microsoft.IdentityModel.Xml",
            "Newtonsoft.Json.Bson",
            "Newtonsoft.Json",
            "Remotion.Linq",
            "System.IdentityModel.Tokens.Jwt",
            "System.Interactive.Async",
            "System.IO.Pipelines",
            "System.Net.WebSockets.WebSocketProtocol",
            "System.Runtime.CompilerServices.Unsafe",
            "System.Security.Cryptography.Pkcs",
            "System.Security.Cryptography.Xml",
            "System.Security.Permissions",
            "System.Text.Encoding.CodePages",
            "System.Text.Encodings.Web",
            "System.Threading.Channels",
        };
    }
}
