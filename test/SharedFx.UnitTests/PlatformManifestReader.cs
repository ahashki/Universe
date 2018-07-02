// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;

namespace Microsoft.AspNetCore
{
    /// <summary>
    /// A simplified version of https://github.com/dotnet/sdk/blob/v2.1.300/src/Tasks/Common/ConflictResolution/PlatformManifestReader.cs
    /// </summary>
    internal static class PlatformManifestReader
    {
        private static readonly char[] _manifestLineSeparator = new[] { '|' };

        public static IEnumerable<ConflictItem> LoadConflictItems(string manifestPath)
        {
            using (var manfiestStream = File.Open(manifestPath, FileMode.Open, FileAccess.Read, FileShare.Read | FileShare.Delete))
            using (var manifestReader = new StreamReader(manfiestStream))
            {
                for (int lineNumber = 0; !manifestReader.EndOfStream; lineNumber++)
                {
                    var line = manifestReader.ReadLine().Trim();

                    if (line.Length == 0 || line[0] == '#')
                    {
                        continue;
                    }

                    var lineParts = line.Split(_manifestLineSeparator);

                    if (lineParts.Length != 4)
                    {
                        string errorMessage = string.Format("Error parsing PlatformManifest from '{0}' line {1}.  Lines must have the format {2}.",
                            manifestPath,
                            lineNumber,
                            "fileName|packageId|assemblyVersion|fileVersion");
                        throw new InvalidDataException(errorMessage);
                    }

                    var fileName = lineParts[0].Trim();
                    var packageId = lineParts[1].Trim();
                    var assemblyVersionString = lineParts[2].Trim();
                    var fileVersionString = lineParts[3].Trim();

                    Version assemblyVersion = null, fileVersion = null;

                    if (assemblyVersionString.Length != 0 && !Version.TryParse(assemblyVersionString, out assemblyVersion))
                    {
                        string errorMessage = string.Format("Error parsing PlatformManifest from '{0}' line {1}.  {2} '{3}' was invalid.",
                            manifestPath,
                            lineNumber,
                            "AssemblyVersion",
                            assemblyVersionString);
                        throw new InvalidDataException(errorMessage);
                    }

                    if (fileVersionString.Length != 0 && !Version.TryParse(fileVersionString, out fileVersion))
                    {
                        string errorMessage = string.Format("Error parsing PlatformManifest from '{0}' line {1}.  {2} '{3}' was invalid.",
                            manifestPath,
                            lineNumber,
                            "FileVersion",
                            fileVersionString);
                        throw new InvalidDataException(errorMessage);
                    }

                    yield return new ConflictItem(fileName, packageId, assemblyVersion, fileVersion);
                }
            }
        }
    }
}
