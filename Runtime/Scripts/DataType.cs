// SPDX-FileCopyrightText: 2023 Unity Technologies and the Draco for Unity authors
// SPDX-License-Identifier: Apache-2.0

using System;

namespace Draco
{
    /// <summary>
    /// Attribute data type as defined in draco_types.h
    /// </summary>
    enum DataType
    {
        ///<summary>DT_INVALID</summary>
        Invalid = 0,
        ///<summary>DT_INT8</summary>
        Int8,
        ///<summary>DT_UINT8</summary>
        UInt8,
        ///<summary>DT_INT16</summary>
        Int16,
        ///<summary>DT_UINT16</summary>
        UInt16,
        ///<summary>DT_INT32</summary>
        Int32,
        ///<summary>DT_UINT32</summary>
        UInt32,
        ///<summary>DT_INT64</summary>
        Int64,
        ///<summary>DT_UINT64</summary>
        UInt64,
        ///<summary>DT_FLOAT32</summary>
        Float32,
        ///<summary>DT_FLOAT64</summary>
        Float64,
        ///<summary>DT_BOOL</summary>
        Bool
    }
}
