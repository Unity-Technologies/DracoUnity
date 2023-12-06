// SPDX-FileCopyrightText: 2023 Unity Technologies and the Draco for Unity authors
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Threading.Tasks;
using UnityEngine;

namespace Draco.Sample.SceneEncodeDecode
{

    /// <summary>
    /// Decodes multiple Draco data and assigns it to <see cref="MeshFilter"/> targets.
    /// </summary>
    public class DracoSceneDecoder : MonoBehaviour
    {

        /// <summary>
        /// Decode instances (one per Draco mesh)
        /// </summary>
        public DecodeInstance[] instances;

        async void Start()
        {
            var startTime = Time.realtimeSinceStartup;
            var tasks = new Task[instances.Length];
            for (var i = 0; i < instances.Length; i++)
            {
                var instance = instances[i];
                tasks[i] = instance.Decode();
            }
            await Task.WhenAll(tasks);
            var time = Time.realtimeSinceStartup - startTime;
            Debug.Log($"Decoded {instances.Length} meshes in {time:0.000} seconds");
        }
    }
}
