// SPDX-FileCopyrightText: 2023 Unity Technologies and the Draco for Unity authors
// SPDX-License-Identifier: Apache-2.0

using System;
using UnityEngine;

namespace Draco.Encode
{
    /// <summary>
    /// Sets the desired encoding and decoding speed for the given options.
    /// Note that both speed options affect the encoder choice of used methods and algorithms. For example, a
    /// requirement for fast decoding may prevent the encoder from using the best compression methods even if the
    /// encoding speed is set to 0. In general, the faster of the two options limits the choice of features that can be
    /// used by the encoder. Additionally, setting <see cref="decodingSpeed"/> to be faster than the
    /// <see cref="encodingSpeed"/> may allow the encoder to choose the optimal method out of the available features for
    /// the given <see cref="decodingSpeed"/>.
    /// </summary>
    public readonly struct SpeedSettings
    {
        /// <summary>
        /// Default speed settings, used whenever no settings are provided.
        /// </summary>
        public static readonly SpeedSettings Default = new SpeedSettings(
            encodingSpeed: 0,
            decodingSpeed: 4
        );

        /// <summary>
        /// Encoding speed
        /// (0 = slowest speed, but the best compression; 10 = fastest, but the worst compression)
        /// </summary>
        public readonly int encodingSpeed;

        /// <summary>
        /// Decoding speed
        /// (0 = slowest speed, but the best compression; 10 = fastest, but the worst compression)
        /// </summary>
        public readonly int decodingSpeed;

        /// <inheritdoc cref="SpeedSettings"/>
        /// <param name="encodingSpeed">Encoding speed
        /// (0 = slowest speed, but the best compression; 10 = fastest, but the worst compression)</param>
        /// <param name="decodingSpeed">Decoding speed
        ///  (0 = slowest speed, but the best compression; 10 = fastest, but the worst compression)</param>
        public SpeedSettings(
            int encodingSpeed,
            int decodingSpeed
        )
        {
            this.encodingSpeed = Mathf.Clamp(encodingSpeed, 0, 10);
            this.decodingSpeed = Mathf.Clamp(decodingSpeed, 0, 10);
        }
    }
}
