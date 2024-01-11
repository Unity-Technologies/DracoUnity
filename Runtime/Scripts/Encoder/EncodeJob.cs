// SPDX-FileCopyrightText: 2023 Unity Technologies and the Draco for Unity authors
// SPDX-License-Identifier: Apache-2.0

using System;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;

namespace Draco.Encode
{
    struct EncodeJob : IJob
    {
        [NativeDisableUnsafePtrRestriction]
        public IntPtr dracoEncoder;

        public void Execute()
        {
            DracoEncoder.dracoEncoderEncode(dracoEncoder, false);
        }
    }
}
