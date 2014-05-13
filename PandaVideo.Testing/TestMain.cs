using System;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using PandaVideoMixer;

namespace PandaVideo.Testing
{
    [TestFixture]
    public class TestMain : TestBase
    {
        [Test]
        public void TestCreationInstance()
        {
            var instance = new PandaVideoConv();
            Assert.IsNotNull(instance, "Should have created instance but didn't");
        }

        [Test]
        [Category("NotAuto")]
        public void TestInitialAnalysis()
        {
            var samplesPath = Environment.CurrentDirectory + @"\..\..\PandaVideoSamples";
            var outputPath = Path.Combine(samplesPath, "Output");
            Directory.CreateDirectory(outputPath);

            var convertFile = new PandaVideoConv
                {OutputFolder = outputPath, WorkingFolder = outputPath, SelectedDevice = new DevicePS3()};
            Assert.IsTrue(convertFile.AnalyseFile(Path.Combine(samplesPath, SAMPLE_FILE1)), "Failed Analysis");

            // Test what we found
            List<VideoTrack> vTracks = convertFile.GetVideoTracks();
            Assert.IsTrue(vTracks.Count > 0);
            VideoTrack vTrack1 = vTracks[0];
            Assert.IsTrue(vTrack1.Width == 1920, "Width is wrong");
            Assert.IsTrue(vTrack1.Height == 800, "Height is wrong");
        }
    }
}