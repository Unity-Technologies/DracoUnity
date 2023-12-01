// SPDX-FileCopyrightText: 2023 Unity Technologies and the Draco for Unity authors
// SPDX-License-Identifier: Apache-2.0

using System.IO;
using UnityEngine;
#if UNITY_2020_2_OR_NEWER
using UnityEditor.AssetImporters;
#else
using UnityEditor.Experimental.AssetImporters;
#endif

namespace Draco.Editor
{

    [ScriptedImporter(1, "drc")]
    class DracoImporter : ScriptedImporter
    {

        public override void OnImportAsset(AssetImportContext ctx)
        {

            var dracoData = File.ReadAllBytes(ctx.assetPath);
            var mesh = AsyncHelpers.RunSync<Mesh>(() => DracoDecoder.DecodeMeshInternal(dracoData, sync: true));
            if (mesh == null)
            {
                Debug.LogError("Import draco file failed");
                return;
            }
            ctx.AddObjectToAsset("mesh", mesh);
            ctx.SetMainObject(mesh);
        }
    }
}
