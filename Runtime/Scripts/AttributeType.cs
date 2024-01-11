// SPDX-FileCopyrightText: 2023 Unity Technologies and the Draco for Unity authors
// SPDX-License-Identifier: Apache-2.0

using System;

namespace Draco
{
    /// <summary>
    /// GeometryAttribute::Type as defined in
    /// <see href="https://github.com/google/draco/blob/9f856abaafb4b39f1f013763ff061522e0261c6f/src/draco/attributes/geometry_attribute.h#L46">geometry_attribute.h</see>
    /// </summary>
    public enum AttributeType
    {
        ///<summary>GeometryAttribute::Type::INVALID</summary>
        Invalid = -1,
        ///<summary>GeometryAttribute::Type::POSITION</summary>
        Position = 0,
        ///<summary>GeometryAttribute::Type::NORMAL</summary>
        Normal,
        ///<summary>GeometryAttribute::Type::COLOR</summary>
        Color,
        ///<summary>GeometryAttribute::Type::TEX_COORD</summary>
        TextureCoordinate,
        /// <summary>
        /// GeometryAttribute::Type::GENERIC.
        /// A special id used to mark attributes that are not assigned to any known predefined use case.
        /// Such attributes are often used for a shader specific data.
        /// </summary>
        Generic,

        /// <summary>
        /// GeometryAttribute::Type::TANGENT.
        /// Don't pass this value on to the native library, as it's not supported by Draco bit-stream version 2,2.
        /// </summary>
        /// <seealso href="https://github.com/google/draco/blob/9f856abaafb4b39f1f013763ff061522e0261c6f/src/draco/attributes/geometry_attribute.h#L60"/>
        Tangent,
        /// <summary>
        /// GeometryAttribute::Type::MATERIAL.
        /// Don't pass this value on to the native library, as it's not supported by Draco bit-stream version 2,2.
        /// </summary>
        /// <seealso href="https://github.com/google/draco/blob/9f856abaafb4b39f1f013763ff061522e0261c6f/src/draco/attributes/geometry_attribute.h#L60"/>
        Material,
        /// <summary>
        /// GeometryAttribute::Type::JOINTS.
        /// Don't pass this value on to the native library, as it's not supported by Draco bit-stream version 2,2.
        /// </summary>
        /// <seealso href="https://github.com/google/draco/blob/9f856abaafb4b39f1f013763ff061522e0261c6f/src/draco/attributes/geometry_attribute.h#L60"/>
        Joints,
        /// <summary>
        /// GeometryAttribute::Type::WEIGHTS.
        /// Don't pass this value on to the native library, as it's not supported by Draco bit-stream version 2,2.
        /// </summary>
        /// <seealso href="https://github.com/google/draco/blob/9f856abaafb4b39f1f013763ff061522e0261c6f/src/draco/attributes/geometry_attribute.h#L60"/>
        Weights,
    }
}
