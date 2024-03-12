// SPDX-FileCopyrightText: 2024 Unity Technologies and the Draco for Unity authors
// SPDX-License-Identifier: Apache-2.0

using System.IO;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Draco.Editor.Tests
{
    class UnityVersionTests
    {
        [Test]
        public void ConstructorMajor()
        {
            var u = new UnityVersion("42");
            Assert.AreEqual(42, u.Major);
            Assert.AreEqual(0, u.Minor);
            Assert.AreEqual(0, u.Patch);
            Assert.AreEqual('f', u.Type);
            Assert.AreEqual(1, u.Sequence);
        }

        [Test]
        public void ConstructorMinor()
        {
            var u = new UnityVersion("2019.1");
            Assert.AreEqual(2019, u.Major);
            Assert.AreEqual(1, u.Minor);
            Assert.AreEqual(0, u.Patch);
            Assert.AreEqual('f', u.Type);
            Assert.AreEqual(1, u.Sequence);
        }

        [Test]
        public void ConstructorPatch()
        {
            var u = new UnityVersion("2020.12.9");
            Assert.AreEqual(2020, u.Major);
            Assert.AreEqual(12, u.Minor);
            Assert.AreEqual(9, u.Patch);
            Assert.AreEqual('f', u.Type);
            Assert.AreEqual(1, u.Sequence);
        }

        [Test]
        public void ConstructorType()
        {
            var u = new UnityVersion("2021.42.42a");
            Assert.AreEqual(2021, u.Major);
            Assert.AreEqual(42, u.Minor);
            Assert.AreEqual(42, u.Patch);
            Assert.AreEqual('a', u.Type);
            Assert.AreEqual(1, u.Sequence);
        }

        [Test]
        public void ConstructorFull()
        {
            var u = new UnityVersion("6.6.6b3");
            Assert.AreEqual(6, u.Major);
            Assert.AreEqual(6, u.Minor);
            Assert.AreEqual(6, u.Patch);
            Assert.AreEqual('b', u.Type);
            Assert.AreEqual(3, u.Sequence);
        }

        [Test]
        public void IsWebAssemblyCompatible2020()
        {
            var wasm2020 = new GUID(BuildPreProcessor.wasm2020Guid);
            Assert.IsTrue(BuildPreProcessor.IsWebAssemblyCompatible(wasm2020, new UnityVersion("2019.1.0b3")));
            Assert.IsTrue(BuildPreProcessor.IsWebAssemblyCompatible(wasm2020, new UnityVersion("2021.1.50")));
            Assert.IsFalse(BuildPreProcessor.IsWebAssemblyCompatible(wasm2020, new UnityVersion("2021.2")));
        }

        [Test]
        public void IsWebAssemblyCompatible2021()
        {
            var wasm2021 = new GUID(BuildPreProcessor.wasm2021Guid);
            Assert.IsFalse(BuildPreProcessor.IsWebAssemblyCompatible(wasm2021, new UnityVersion("2021.1.99f99")));
            Assert.IsTrue(BuildPreProcessor.IsWebAssemblyCompatible(wasm2021, new UnityVersion("2021.2.0f1")));
            Assert.IsTrue(BuildPreProcessor.IsWebAssemblyCompatible(wasm2021, new UnityVersion("2022.1.99f99")));
            Assert.IsFalse(BuildPreProcessor.IsWebAssemblyCompatible(wasm2021, new UnityVersion("2022.2")));
        }

        [Test]
        public void IsWebAssemblyCompatible2022()
        {
            var wasm2022 = new GUID(BuildPreProcessor.wasm2022Guid);
            Assert.IsFalse(BuildPreProcessor.IsWebAssemblyCompatible(wasm2022, new UnityVersion("2022.1.99f99")));
            Assert.IsTrue(BuildPreProcessor.IsWebAssemblyCompatible(wasm2022, new UnityVersion("2022.2.0f1")));
            Assert.IsTrue(BuildPreProcessor.IsWebAssemblyCompatible(wasm2022, new UnityVersion("2023.2.0a16")));
            Assert.IsFalse(BuildPreProcessor.IsWebAssemblyCompatible(wasm2022, new UnityVersion("2023.2.0a17")));
        }

        [Test]
        public void IsWebAssemblyCompatible2023()
        {
            var wasm2023 = new GUID(BuildPreProcessor.wasm2023Guid);
            Assert.IsFalse(BuildPreProcessor.IsWebAssemblyCompatible(wasm2023, new UnityVersion("2023.2.0a16")));
            Assert.IsTrue(BuildPreProcessor.IsWebAssemblyCompatible(wasm2023, new UnityVersion("2023.2.0a17")));
            Assert.IsTrue(BuildPreProcessor.IsWebAssemblyCompatible(wasm2023, new UnityVersion("2023.3")));
            Assert.IsTrue(BuildPreProcessor.IsWebAssemblyCompatible(wasm2023, new UnityVersion("2024")));
            Assert.IsTrue(BuildPreProcessor.IsWebAssemblyCompatible(wasm2023, new UnityVersion("2025")));
        }

        [Test]
        public void IsWebAssemblyCompatibleInvalid()
        {
            var invalidWasm = new GUID("42424242424242424242424242424242");
            Assert.Throws<InvalidDataException>(() =>
                Assert.IsFalse(BuildPreProcessor.IsWebAssemblyCompatible(invalidWasm, new UnityVersion("2023.2.0a16")))
                );
        }
    }
}
