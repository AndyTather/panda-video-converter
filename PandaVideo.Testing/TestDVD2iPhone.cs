using System;
using System.IO;
using NUnit.Framework;
using PandaVideoMixer;

namespace PandaVideo.Testing
{
    [TestFixture]
    public class TestDVD2iPhone : TestBase
    {
        [TestFixtureSetUp]
        public void Init()
        {
            _outputPath = Path.Combine(_samplesPath, @"Output\iPhone");
            Directory.CreateDirectory(_outputPath);
        }


        [Test]
        [Category("NotAuto")]
        public void TestConvertSample50()
        {
            var convertFile = new PandaVideoConv
                {OutputFolder = _outputPath, WorkingFolder = _outputPath, SelectedDevice = new DeviceiPhone3GS()};
            Assert.IsTrue(convertFile.AnalyseFile(Path.Combine(_samplesPath, SAMPLE_FILE13))); // \VIDEO_TS

            // Test what we found
            Assert.IsTrue(convertFile.ConvertDisc());
        }
    }
}