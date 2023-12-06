// SPDX-FileCopyrightText: 2023 Unity Technologies and the Draco for Unity authors
// SPDX-License-Identifier: Apache-2.0

using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Draco.Sample.SceneEncodeDecode.Editor
{
    static class ToolsMenu
    {
        [MenuItem("Tools/Draco/Encode Selected GameObject", true)]
        static bool EncodeSelectedGameObjectMenuValidate()
        {
            if (Selection.activeObject != null && Selection.activeObject is GameObject)
            {
                var meshFilters = SceneEncoder.GetAllMeshFilters(Selection.activeObject as GameObject);
                return meshFilters != null && meshFilters.Length > 0;
            }

            return false;
        }

        [MenuItem("Tools/Draco/Encode Selected GameObject")]
        static async void EncodeSelectedGameObjectMenu()
        {
            await SceneEncoder.CompressMeshFilters(SceneEncoder.GetAllMeshFilters((GameObject)Selection.activeObject));
        }

        [MenuItem("Tools/Draco/Encode Active Scene", true)]
        static bool EncodeActiveSceneMenuValidate()
        {
            var scene = SceneManager.GetActiveScene();
            var meshFilters = SceneEncoder.GetAllMeshFilters(scene);
            return meshFilters != null && meshFilters.Count > 0;
        }

        [MenuItem("Tools/Draco/Encode Active Scene")]
        static async void EncodeActiveSceneMenu()
        {
            await SceneEncoder.CompressScene(SceneManager.GetActiveScene());
        }

        [MenuItem("Tools/Draco/Setup Draco Decoder for Active Scene", true)]
        static bool SetupDracoDecoderForSceneMenuValidate()
        {
            return EncodeActiveSceneMenuValidate();
        }

        [MenuItem("Tools/Draco/Setup Draco Decoder for Active Scene", false, priority: 1100)]
        static async void SetupDracoDecoderForSceneMenu()
        {
            if (EditorUtility.DisplayDialog(
                    "Setup Draco Decoder",
                    "Please backup the active scene before proceeding! All meshes will be removed from the scene, encoded to Draco assets and a Draco Mesh Decoder GameObject will be setup that decodes and restores the meshes at runtime.",
                    "Proceed",
                    "Cancel"))
            {
                await SceneEncoder.CompressScene(SceneManager.GetActiveScene(), true);
            }
        }
    }
}
