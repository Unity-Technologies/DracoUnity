// SPDX-FileCopyrightText: 2023 Unity Technologies and the Draco for Unity authors
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using NUnit.Framework.Interfaces;
using NUnit.Framework.Internal;
using NUnit.Framework.Internal.Builders;
using UnityEngine;

namespace Draco.Tests
{

    public class UseDracoTestFileCaseAttribute : UnityEngine.TestTools.UnityTestAttribute, ITestBuilder
    {

        string[] m_SampleSet;

        NUnitTestCaseBuilder m_Builder = new NUnitTestCaseBuilder();

        public UseDracoTestFileCaseAttribute(string[] sampleSetPath)
        {
            m_SampleSet = sampleSetPath;
        }

        IEnumerable<TestMethod> ITestBuilder.BuildFrom(IMethodInfo method, Test suite)
        {
            List<TestMethod> results = new List<TestMethod>();
            var nameCounts = new Dictionary<string, int>();

            if (m_SampleSet == null)
            {
                throw new Exception("SampleSet  not set");
            }

            try
            {
                foreach (var testCase in m_SampleSet)
                {
                    var data = new TestCaseData(new object[] { testCase });

                    var origName = Path.GetFileName(testCase);
                    string name;
                    if (nameCounts.TryGetValue(origName, out var count))
                    {
                        name = $"{method.Name}-{origName}-{count}";
                        nameCounts[origName] = count + 1;
                    }
                    else
                    {
                        name = $"{method.Name}-{origName}";
                        nameCounts[origName] = 1;
                    }

                    data.SetName(name);
                    data.ExpectedResult = new UnityEngine.Object();
                    data.HasExpectedResult = true;

                    var test = this.m_Builder.BuildTestMethod(method, suite, data);
                    if (test.parms != null)
                        test.parms.HasExpectedResult = false;

                    test.Name = name;

                    results.Add(test);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Failed to generate glTF testcases!");
                Debug.LogException(ex);
                throw;
            }

            Console.WriteLine("Generated {0} glTF test cases.", results.Count);
            return results;
        }
    }
}
