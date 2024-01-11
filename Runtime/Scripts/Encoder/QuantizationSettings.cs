// SPDX-FileCopyrightText: 2023 Unity Technologies and the Draco for Unity authors
// SPDX-License-Identifier: Apache-2.0

using UnityEngine;

namespace Draco.Encode
{
    /// <summary>
    /// Quantization settings.
    /// </summary>
    public readonly struct QuantizationSettings
    {
        // ReSharper disable MemberCanBePrivate.Global
        /// <summary>Minimum quantization bits.</summary>
        public const int minQuantization = 1;
        /// <summary>Maximum quantization bits.</summary>
        public const int maxQuantization = 30;
        // ReSharper restore MemberCanBePrivate.Global

        const int k_DefaultPositionQuantization = 14;
        const int k_DefaultNormalQuantization = 10;
        const int k_DefaultTexCoordQuantization = 12;
        const int k_DefaultColorQuantization = 8;
        const int k_DefaultGenericQuantization = 12;

        /// <summary>
        /// Default quantization settings, used whenever no settings are provided.
        /// </summary>
        public static readonly QuantizationSettings Default = new QuantizationSettings(
            positionQuantization: k_DefaultPositionQuantization,
            normalQuantization: k_DefaultNormalQuantization,
            texCoordQuantization: k_DefaultTexCoordQuantization,
            colorQuantization: k_DefaultColorQuantization
            );

        /// <summary>
        /// Vertex position quantization.
        /// </summary>
        public readonly int positionQuantization;

        /// <summary>
        /// Normal quantization.
        /// </summary>
        public readonly int normalQuantization;

        /// <summary>
        /// Texture coordinate quantization.
        /// </summary>
        public readonly int texCoordQuantization;

        /// <summary>
        /// Color quantization.
        /// </summary>
        public readonly int colorQuantization;

        /// <summary>
        /// Default quantization for generic attributes. Unused at the moment.
        /// </summary>
        public static int genericQuantization => k_DefaultGenericQuantization;

        /// <summary>
        /// Constructs quantization settings.
        /// Defaults are applied for normal, texture coordinate and color quantization.
        /// </summary>
        /// <param name="positionQuantization">Initializes <see cref="QuantizationSettings.positionQuantization"/></param>
        public QuantizationSettings(int positionQuantization)
        {
            this.positionQuantization = Mathf.Clamp(positionQuantization, minQuantization, maxQuantization);
            normalQuantization = k_DefaultNormalQuantization;
            texCoordQuantization = k_DefaultTexCoordQuantization;
            colorQuantization = k_DefaultColorQuantization;
        }

        /// <summary>
        /// Constructs quantization settings.
        /// </summary>
        /// <param name="positionQuantization">Initializes <see cref="QuantizationSettings.positionQuantization"/></param>
        /// <param name="normalQuantization">Initializes <see cref="QuantizationSettings.normalQuantization"/></param>
        /// <param name="texCoordQuantization">Initializes <see cref="QuantizationSettings.texCoordQuantization"/></param>
        /// <param name="colorQuantization">Initializes <see cref="QuantizationSettings.colorQuantization"/></param>
        public QuantizationSettings(
            int positionQuantization,
            int normalQuantization,
            int texCoordQuantization,
            int colorQuantization
        )
        {
            this.positionQuantization = Mathf.Clamp(positionQuantization, minQuantization, maxQuantization);
            this.normalQuantization = Mathf.Clamp(normalQuantization, minQuantization, maxQuantization);
            this.texCoordQuantization = Mathf.Clamp(texCoordQuantization, minQuantization, maxQuantization);
            this.colorQuantization = Mathf.Clamp(colorQuantization, minQuantization, maxQuantization);
        }

        /// <summary>
        /// True if all quantization parameters have valid values within <see cref="minQuantization">minimum</see>
        /// and <see cref="maxQuantization">maximum</see>.
        /// </summary>
        public bool IsValid =>
            positionQuantization >= minQuantization && positionQuantization <= maxQuantization
            && normalQuantization >= minQuantization && normalQuantization <= maxQuantization
            && texCoordQuantization >= minQuantization && texCoordQuantization <= maxQuantization
            && colorQuantization >= minQuantization && colorQuantization <= maxQuantization;

        /// <inheritdoc cref="object.ToString"/>
        public override string ToString()
        {
            return $"QuantizationSettings(pos:{positionQuantization},normal:{normalQuantization},uv:{texCoordQuantization},color:{colorQuantization})";
        }

        /// <summary>
        /// Constructs quantization settings.
        /// The position quantization value is based on the mesh's bounds, its scale in the world and the desired
        /// precision in world units. The rest will be default values.
        /// </summary>
        /// <param name="meshBounds">Size of the mesh</param>
        /// <param name="worldScale">World scale of the object</param>
        /// <param name="precision">Desired minimum precision in world units</param>
        /// <returns>Quantization settings</returns>
        public static QuantizationSettings FromWorldSize(
            Bounds meshBounds,
            Vector3 worldScale,
            float precision
        )
        {
            return new QuantizationSettings(
                positionQuantization: GetIdealQuantization(worldScale, meshBounds, precision),
                normalQuantization: k_DefaultNormalQuantization,
                texCoordQuantization: k_DefaultTexCoordQuantization,
                colorQuantization: k_DefaultColorQuantization
            );
        }

        /// <summary>
        /// Constructs quantization settings.
        /// The position quantization value is based on the mesh's bounds, its scale in the world and the desired
        /// precision in world units.
        /// </summary>
        /// <param name="meshBounds">Size of the mesh</param>
        /// <param name="worldScale">World scale of the object</param>
        /// <param name="precision">Desired minimum precision in world units</param>
        /// <param name="normalQuantization">Initializes <see cref="QuantizationSettings.normalQuantization"/></param>
        /// <param name="texCoordQuantization">Initializes <see cref="QuantizationSettings.texCoordQuantization"/></param>
        /// <param name="colorQuantization">Initializes <see cref="QuantizationSettings.colorQuantization"/></param>
        /// <returns>QuantizationSettings settings</returns>
        public static QuantizationSettings FromWorldSize(
            Bounds meshBounds,
            Vector3 worldScale,
            float precision,
            int normalQuantization,
            int texCoordQuantization,
            int colorQuantization
            )
        {
            return new QuantizationSettings(
                positionQuantization: GetIdealQuantization(worldScale, meshBounds, precision),
                normalQuantization: normalQuantization,
                texCoordQuantization: texCoordQuantization,
                colorQuantization: colorQuantization
                );
        }

        /// <summary>
        /// Calculates the ideal position quantization value based on an object's world scale, bounds and the desired
        /// precision in world units.
        /// </summary>
        /// <param name="worldScale">World scale of the object</param>
        /// <param name="precision">Desired minimum precision in world units</param>
        /// <param name="bounds">Size of the mesh</param>
        /// <returns>Ideal quantization in bits</returns>
        static int GetIdealQuantization(Vector3 worldScale, Bounds bounds, float precision)
        {
            var scale = new Vector3(Mathf.Abs(worldScale.x), Mathf.Abs(worldScale.y), Mathf.Abs(worldScale.z));
            var maxSize = Mathf.Max(
                bounds.extents.x * scale.x,
                bounds.extents.y * scale.y,
                bounds.extents.z * scale.z
            ) * 2;
            return GetIdealQuantization(maxSize, precision);
        }

        /// <summary>
        /// Calculates the ideal quantization value based on the largest dimension and desired precision
        /// </summary>
        /// <param name="largestDimension">Length of the largest dimension (width/depth/height)</param>
        /// <param name="precision">Desired minimum precision in world units</param>
        /// <returns>Ideal quantization in bits</returns>
        static int GetIdealQuantization(float largestDimension, float precision)
        {
            var value = Mathf.RoundToInt(largestDimension / precision);
            var mostSignificantBit = -1;
            while (value > 0)
            {
                mostSignificantBit++;
                value >>= 1;
            }
            return Mathf.Clamp(mostSignificantBit, minQuantization, maxQuantization);
        }
    }
}
