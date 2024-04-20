// Copyright (c) 2024 Roger Brown.
// Licensed under the MIT License.

#if NETCOREAPP
#else
using Microsoft.VisualStudio.TestTools.UnitTesting;
#endif
using System;
using System.IO;
using System.Management.Automation;
using System.Management.Automation.Runspaces;
using System.Reflection;

namespace RhubarbGeekNz.Base64
{
    [TestClass]
    public class UnitTests
    {
        readonly InitialSessionState initialSessionState = InitialSessionState.CreateDefault();
        public UnitTests()
        {
            foreach (Type t in new Type[] {
                typeof(ConvertToBase64),
                typeof(ConvertFromBase64)
            })
            {
                CmdletAttribute ca = t.GetCustomAttribute<CmdletAttribute>();

                if (ca == null) throw new NullReferenceException();

                initialSessionState.Commands.Add(new SessionStateCmdletEntry($"{ca.VerbName}-{ca.NounName}", t, ca.HelpUri));
            }

            initialSessionState.Variables.Add(new SessionStateVariableEntry("ErrorActionPreference", ActionPreference.Stop, "Stop action"));
        }

        [TestMethod]
        public void TestConvertToBase64()
        {
            using (PowerShell powerShell = PowerShell.Create(initialSessionState))
            {
                powerShell.AddScript("New-Object Byte[] -ArgumentList @(,256) | ConvertTo-Base64");

                var outputPipeline = powerShell.Invoke();

                Assert.AreEqual(6, outputPipeline.Count);

                Assert.AreEqual(24, outputPipeline[outputPipeline.Count - 1].BaseObject.ToString().Length);
            }
        }

        [TestMethod]
        public void TestConvertToAndFromBase64()
        {
            using (PowerShell powerShell = PowerShell.Create(initialSessionState))
            {
                powerShell.AddScript("@(,(New-Object Byte[] -ArgumentList @(,10000))) | ConvertTo-Base64 | ConvertFrom-Base64");

                var outputPipeline = powerShell.Invoke();

                int total = 0;

                foreach (var obj in outputPipeline)
                {
                    byte[] buffer = (byte[])obj.BaseObject;

                    total += buffer.Length;
                }

                Assert.AreEqual(10000, total);
            }
        }

        [TestMethod]
        public void TestHelloWorld()
        {
            using (PowerShell powerShell = PowerShell.Create(initialSessionState))
            {
                powerShell.AddScript("[System.Text.Encoding]::ASCII.GetBytes('Hello World') | ConvertTo-Base64 | ConvertFrom-Base64");

                var outputPipeline = powerShell.Invoke();

                Assert.AreEqual(1, outputPipeline.Count);

                string result = System.Text.Encoding.ASCII.GetString((byte[])outputPipeline[0].BaseObject);

                Assert.AreEqual("Hello World", result);
            }
        }

        [TestMethod]
        public void TestRandomData()
        {
            using (PowerShell powerShell = PowerShell.Create(initialSessionState))
            {

                byte[] bytes = new byte[10000];

                new Random().NextBytes(bytes);

                powerShell.AddScript(
                        "Param([byte[]]$bytes)" + Environment.NewLine +
                        "@(,$bytes) | ConvertTo-Base64 | ConvertFrom-Base64").AddArgument(bytes);

                var outputPipeline = powerShell.Invoke();

                MemoryStream memoryStream = new MemoryStream();

                foreach (var obj in outputPipeline)
                {
                    byte[] buffer = (byte[])obj.BaseObject;

                    memoryStream.Write(buffer, 0, buffer.Length);
                }

                byte[] result = memoryStream.ToArray();

                Assert.AreEqual(bytes.Length, result.Length);

                for (int i = 0; i < bytes.Length; i++)
                {
                    Assert.AreEqual(bytes[i], result[i]);
                }
            }
        }

        [TestMethod]
        public void TestBadData()
        {
            bool caught = false;
            string exName = null;

            using (PowerShell powerShell = PowerShell.Create(initialSessionState))
            {
                try
                {
                    powerShell.AddScript("'!$%^&*()-_=+:;<>,.?/#~@][{}' | ConvertFrom-Base64");
                    powerShell.Invoke();
                }
                catch (ActionPreferenceStopException ex)
                {
                    exName = ex.ErrorRecord.Exception.GetType().Name;
                    caught = ex.ErrorRecord.Exception is FormatException;
                }
            }

            Assert.IsTrue(caught, exName);
        }

        [TestMethod]
        public void TestConvertToWithNull()
        {
            using (PowerShell powerShell = PowerShell.Create(initialSessionState))
            {
                powerShell.AddScript("$null | ConvertTo-Base64");
                var outputPipeline = powerShell.Invoke();
                Assert.AreEqual(0, outputPipeline.Count);
            }
        }

        [TestMethod]
        public void TestConvertFromWithNull()
        {
            using (PowerShell powerShell = PowerShell.Create(initialSessionState))
            {
                powerShell.AddScript("$null | ConvertFrom-Base64");
                var outputPipeline = powerShell.Invoke();
                Assert.AreEqual(0, outputPipeline.Count);
            }
        }

        [TestMethod]
        public void TestConvertToWithEmpty()
        {
            using (PowerShell powerShell = PowerShell.Create(initialSessionState))
            {
                powerShell.AddScript("New-Object -TypeName byte[] -ArgumentList 0 | ConvertTo-Base64");
                var outputPipeline = powerShell.Invoke();
                Assert.AreEqual(0, outputPipeline.Count);
            }
        }

        [TestMethod]
        public void TestConvertFromWithEmpty()
        {
            using (PowerShell powerShell = PowerShell.Create(initialSessionState))
            {
                powerShell.AddScript("'' | ConvertFrom-Base64");
                var outputPipeline = powerShell.Invoke();
                Assert.AreEqual(0, outputPipeline.Count);
            }
        }
    }
}
