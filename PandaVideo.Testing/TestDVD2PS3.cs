using System;
using System.IO;
using NUnit.Framework;
using PandaVideoMixer;

namespace PandaVideo.Testing
{
    [TestFixture]
    public class TestDVD2PS3 : TestBase
    {
        [TestFixtureSetUp]
        public void Init()
        {
            _outputPath = Path.Combine(_samplesPath, @"Output\PS3");
            Directory.CreateDirectory(_outputPath);
        }


        [Test]
        [Category("NotAuto")]
        public void TestConvertSample50()
        {
            var convertFile = new PandaVideoConv();

            // set the output prop
            convertFile.OutputFolder = _outputPath;
            convertFile.WorkingFolder = _outputPath;
            convertFile.SelectedDevice = new DevicePS3();

            Assert.IsTrue(convertFile.AnalyseFile(Path.Combine(_samplesPath, SAMPLE_FILE13))); // \VIDEO_TS

            // Test what we found
            Assert.IsTrue(convertFile.ConvertDisc());
        }
    }
}