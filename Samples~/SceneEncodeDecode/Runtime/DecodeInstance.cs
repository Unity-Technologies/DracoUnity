// SPDX-FileCopyrightText: 2023 Unity Technologies and the Draco for Unity authors
// SPDX-License-Identifier: Apache-2.0

using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace Draco.Sample.SceneEncodeDecode
{

    /// <summary>
    /// Lets you assigns Draco data (in form of a <see cref="TextAsset"/>) to one or more
    /// <see cref="MeshFilter"/> targets and decode them at runtime.
    /// </summary>
    /// <seealso cref="DracoSceneDecoder"/>
    public class DecodeInstance : ScriptableObject
    {

        [SerializeField]
        TextAsset dracoAsset;

        [SerializeField]
        Bounds bounds;

        [SerializeField]
        List<MeshFilter> targets;

        /// <summary>
        /// Decodes the Draco data and assigns it to all targets.
        /// </summary>
        /// <returns>A <see cref="Task"/></returns>
        public async Task Decode()
        {
            var mesh = await DracoDecoder.DecodeMesh(dracoAsset.bytes);
            mesh.bounds = bounds;
#if DEBUG
            mesh.name = dracoAsset.name;
#endif
            foreach (var meshFilter in targets)
            {
                meshFilter.mesh = mesh;
            }
        }

        /// <summary>
        /// Sets the Draco data asset and its bounds.
        /// </summary>
        /// <param name="newDracoAsset">Draco data.</param>
        /// <param name="newBounds">Bounds of the decoded Draco mesh.</param>
        public void SetAsset(TextAsset newDracoAsset, Bounds newBounds)
        {
            dracoAsset = newDracoAsset;
            bounds = newBounds;
        }

        /// <summary>
        /// Adds a <see cref="MeshFilter"/> target that the Draco mesh will be assigned to when <see cref="Decode"/> is
        /// invoked.
        /// </summary>
        /// <param name="meshFilter">New target to be added</param>
        public void AddTarget(MeshFilter meshFilter)
        {
            if (targets == null) targets = new List<MeshFilter>();
            targets.Add(meshFilter);
        }
    }
}
