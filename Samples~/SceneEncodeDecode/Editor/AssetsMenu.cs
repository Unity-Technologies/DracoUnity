// SPDX-FileCopyrightText: 2023 Unity Technologies and the Draco for Unity authors
// SPDX-License-Identifier: Apache-2.0

using System.IO;
using System.Threading.Tasks;
using Draco.Encode;
using UnityEditor;
using UnityEngine;

namespace Draco.Sample.SceneEncodeDecode.Editor
{
    static class AssetsMenu
    {
        [MenuItem("Assets/Encode to Draco (.drc)", true)]
        static bool EncodeSelectedMeshMenuValidate()
        {
            var meshes = Selection.GetFiltered<Mesh>(SelectionMode.Deep);
            return meshes.Length > 0;
        }

        [MenuItem("Assets/Encode to Draco (.drc)")]
        static async void EncodeSelectedMeshMenu()
        {
            var meshes = Selection.GetFiltered<Mesh>(SelectionMode.Deep);
            if (meshes.Length < 1) return;
            var mesh = meshes[0];
            if (mesh == null) return;

            var meshName = mesh.name;
            if (string.IsNullOrEmpty(meshName))
            {
                meshName = "Mesh";
            }
            var destination = EditorUtility.SaveFilePanel(
                "Save Draco file",
                null,
                $"{meshName}.drc",
                "drc"
                );
            if (string.IsNullOrEmpty(destination)) return;

            await EncodeMesh(mesh, destination);
        }

        static async Task EncodeMesh(Mesh mesh, string destination)
        {
#if !UNITY_EDITOR
            if (!mesh.isReadable)
            {
                Debug.LogError($"Mesh {mesh.name} is not readable!");
                return;
            }
#endif
            var encodeResults = await DracoEncoder.EncodeMesh(mesh);
            if (encodeResults.Length > 1)
            {
                var extDotPos = destination.LastIndexOf('.');
                var basePath = destination.Substring(0, extDotPos);
                var ext = destination.Substring(extDotPos);
                for (var submesh = 0; submesh < encodeResults.Length; submesh++)
                {
                    File.WriteAllBytes(
                        $"{basePath}-submesh-{submesh}{ext}",
                        encodeResults[submesh].data.ToArray()
                        );
                    encodeResults[submesh].Dispose();
                }
            }
            else
            {
                File.WriteAllBytes(destination, encodeResults[0].data.ToArray());
                encodeResults[0].Dispose();
            }
        }
    }
}
