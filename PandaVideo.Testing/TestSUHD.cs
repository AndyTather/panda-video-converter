using System;
using System.IO;
using NUnit.Framework;
using PandaVideoMixer;

namespace PandaVideo.Testing.Samsung
{
    [TestFixture]
    public class TestSUHD : TestBase
    {
        private IOutputDevice _selectedDevice = new DeviceSamsungUHDTV();

        [TestFixtureSetUp]
        public void Init()
        {
            _outputPath = Path.Combine(_samplesPath, @"Output\SamsungUHDTV");
            Directory.CreateDirectory(_outputPath);
        }




        /// <summary>
        ///   This a Mov file wrong colour space
        /// </summary>
        [Test]
        [Category("NotAuto")]
        public void TestConvertSampleUHDMov()
        {
            var convertFile = new PandaVideoConv();

            // set the output prop
            convertFile.OutputFolder = _outputPath;
            convertFile.WorkingFolder = _outputPath;
            convertFile.SelectedDevice = _selectedDevice;

            Assert.IsTrue(
                convertFile.AnalyseFile(Path.Combine(_samplesPath,
                                                     SAMPLE_FILE16)));

            // Test what we found
            Assert.IsTrue(convertFile.ConvertFile());
        }

        [Test]
        [Category("NotAuto")]
        public void TestConvertSampleUHDTooHigh()
        {
            var convertFile = new PandaVideoConv();

            // set the output prop
            convertFile.OutputFolder = _outputPath;
            convertFile.WorkingFolder = _outputPath;
            convertFile.SelectedDevice = _selectedDevice;

            Assert.IsTrue(
                convertFile.AnalyseFile(Path.Combine(_samplesPath,
                                                     SAMPLE_FILE17)));

            // Test what we found
            Assert.IsTrue(convertFile.ConvertFile());
        }

    
    }
}