// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Text;

namespace Microsoft.AspNetCore
{
    internal class AssemblyInfo
    {
        private Version _fileVersion;

        public string Path { get; set; }
        public bool IsManagedAssembly { get; set; }
        public Version AssemblyVersion { get; set; }
        public Version FileVersion
        {
            get
            {
                if (_fileVersion == null)
                {
                    var (_, fileVersionRaw) = AssemblyAttributes.FirstOrDefault(f => f.Key == "AssemblyFileVersionAttribute");
                    Version.TryParse(fileVersionRaw, out _fileVersion);
                }

                return _fileVersion;
            }
        }

        public string Name { get; set; }
        public ICollection<KeyValuePair<string, string>> AssemblyAttributes { get; set; } = new List<KeyValuePair<string, string>>();

        public static AssemblyInfo Get(string filePath)
        {
            var metadata = new AssemblyInfo
            {
                Path = filePath
            };

            using (var stream = File.OpenRead(filePath))
            using (var peReader = new PEReader(stream))
            {
                MetadataReader metadataReader;
                try
                {
                    metadataReader = peReader.GetMetadataReader();
                }
                catch
                {
                    return metadata;
                }

                var assemblyDefinition = metadataReader.GetAssemblyDefinition();
                metadata.Name = metadataReader.GetString(assemblyDefinition.Name);
                metadata.AssemblyVersion = assemblyDefinition.Version;
                metadata.IsManagedAssembly = true;
                ReadAssemblyAttributes(metadata, metadataReader, assemblyDefinition);
            }

            return metadata;
        }


        private static void ReadAssemblyAttributes(AssemblyInfo metadata, MetadataReader metadataReader, AssemblyDefinition assemblyDefinition)
        {
            // AssemblyVersion is not actually a custom attribute
            if (assemblyDefinition.Version != new Version(0, 0, 0, 0))
            {
                metadata.AssemblyAttributes.Add(KeyValuePair.Create("AssemblyVersionAttribute", assemblyDefinition.Version.ToString()));
            }

            foreach (var handle in assemblyDefinition.GetCustomAttributes())
            {
                var attribute = metadataReader.GetCustomAttribute(handle);
                var constructor = metadataReader.GetMemberReference((MemberReferenceHandle)attribute.Constructor);
                var type = metadataReader.GetTypeReference((TypeReferenceHandle)constructor.Parent);
                var name = metadataReader.GetString(type.Name);

                var signature = metadataReader.GetBlobReader(constructor.Signature);
                var value = metadataReader.GetBlobReader(attribute.Value);
                var header = signature.ReadSignatureHeader();

                const ushort prolog = 1; // two-byte "prolog" defined by ECMA-335 (II.23.3) to be at the beginning of attribute value blobs
                if (value.ReadUInt16() != prolog || header.Kind != SignatureKind.Method || header.IsGeneric)
                {
                    throw new BadImageFormatException();
                }

                var paramCount = signature.ReadCompressedInteger();
                if (paramCount <= 0 || // must have at least 1 parameter
                    signature.ReadSignatureTypeCode() != SignatureTypeCode.Void) // return type must be void
                {
                    continue;
                }

                var sb = new StringBuilder();
                while (paramCount > 0 && sb != null)
                {
                    switch (signature.ReadSignatureTypeCode())
                    {
                        case SignatureTypeCode.String:
                            sb.Append(value.ReadSerializedString());
                            break;
                        default:
                            sb = null;
                            break;
                    }

                    paramCount--;
                    if (paramCount != 0)
                    {
                        sb?.Append(':');
                    }
                }

                if (sb != null)
                {
                    metadata.AssemblyAttributes.Add(KeyValuePair.Create(name, sb.ToString()));
                }
            }
        }
    }
}
