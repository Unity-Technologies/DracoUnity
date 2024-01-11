// SPDX-FileCopyrightText: 2023 Unity Technologies and the Draco for Unity authors
// SPDX-License-Identifier: Apache-2.0

using System.IO;
using UnityEngine;
using UnityEditor.AssetImporters;

namespace Draco.Editor
{

    [ScriptedImporter(1, "drc")]
    class DracoImporter : ScriptedImporter
    {

        public override void OnImportAsset(AssetImportContext ctx)
        {

            var dracoData = File.ReadAllBytes(ctx.assetPath);
            var mesh = AsyncHelpers.RunSync(() =>
                DracoDecoder.DecodeMeshInternal(dracoData, DecodeSettings.Default, null, true));
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
