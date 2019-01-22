using System;
using System.IO;
using System.Diagnostics;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using Handmada.ReLang.Compilation.Parsing;
using Handmada.ReLang.Compilation.Runtime;


namespace Handmada.ReLang.Tests {
    [TestClass]
    public class VirtualMachineTest {
        public TestContext Context { get; set; }


        [TestMethod]
        public void TestMethod1() {
            RunAllScenarios();
        }


        private void RunAllScenarios([CallerFilePath] string sourceFilePath = "") {
            var directoryPath = Path.GetDirectoryName(sourceFilePath);
            var scenariosPath = Path.Combine(directoryPath, "Scenarios");
            var filePaths = Directory.GetFiles(scenariosPath);
            
            foreach (var filePath in filePaths) {
                var extension = Path.GetExtension(filePath);
                if (extension == ".input") {
                    var outputPath = Path.ChangeExtension(filePath, ".output");
                    RunScenario(filePath, outputPath);
                    Debug.WriteLine($"Executed scenario: {Path.GetFileNameWithoutExtension(filePath)}");
                }
            }
        }


        private void RunScenario(string inputPath, string outputPath) {
            // Load program's text
            var lines = LoadFile(inputPath);

            // Load expected output
            var expected = string.Join("", LoadFile(outputPath));

            // Parse it
            var parser = new Parser(lines);
            var program = parser.ParseProgram();

            // Execute it and compare with results
            var writer = new StringWriter();
            var machine = new VirtualMachine(writer);
            var args = new string[] { "Sample", "Text", "Serious", "Arguments" };
            var exitCode = machine.Execute(program, args);
            Assert.AreEqual(0, exitCode);

            var actual = Regex.Replace(writer.ToString(), @"\r\n", "\n");
            Assert.AreEqual(expected, actual);
        }


        private List<string> LoadFile(string path) {
            var lines = new List<string>();
            using (var stream = new StreamReader(path)) {
                var line = "";
                while ((line = stream.ReadLine()) != null) {
                    lines.Add($"{line}\n");
                }
            }
            return lines;
        }
    }
}
