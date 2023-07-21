// SPDX-FileCopyrightText: 2023 Unity Technologies and the Draco for Unity authors
// SPDX-License-Identifier: Apache-2.0

using System.IO;
using UnityEngine;
#if UNITY_2020_2_OR_NEWER
using UnityEditor.AssetImporters;
#else
using UnityEditor.Experimental.AssetImporters;
#endif

namespace Draco.Editor {

    [ScriptedImporter(1, "drc")]
    class DracoImporter : ScriptedImporter {

        public override async void OnImportAsset(AssetImportContext ctx) {
#if NET_UNITY_4_8 // Unity 2021 or newer
            var dracoData = await File.ReadAllBytesAsync(ctx.assetPath);
#else
            var dracoData = File.ReadAllBytes(ctx.assetPath);
#endif
            var draco = new DracoMeshLoader();
            var mesh = await draco.ConvertDracoMeshToUnitySync(dracoData);
            if (mesh == null) {
                Debug.LogError("Import draco file failed");
                return;
            }
            mesh.RecalculateBounds();
            ctx.AddObjectToAsset("mesh", mesh);
            ctx.SetMainObject(mesh);
        }
    }
}