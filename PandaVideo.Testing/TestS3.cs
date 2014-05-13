using System;
using System.IO;
using NUnit.Framework;
using PandaVideoMixer;

namespace PandaVideo.Testing.Samsung
{
    [TestFixture]
    public class TestS3 : TestBase
    {
        private IOutputDevice _selectedDevice = new DeviceSamsungS3();

        [TestFixtureSetUp]
        public void Init()
        {
            _outputPath = Path.Combine(_samplesPath, @"Output\SamsungS3");
            Directory.CreateDirectory(_outputPath);
        }


        /// <summary>
        ///   No video recode required
        ///   first sound stream AC3 in German
        ///   second is DTS which would need selecting
        /// </summary>
        [Test]
        [Category("NotAuto")]
        public void TestConvertSample1()
        {
            var convertFile = new PandaVideoConv();
            // set the output prop
            convertFile.OutputFolder = _outputPath;
            convertFile.WorkingFolder = _outputPath;
            convertFile.SelectedDevice = _selectedDevice;

            convertFile.AnalyseFile(Path.Combine(_samplesPath, SAMPLE_FILE1));

            // Test what we found
            Assert.IsTrue(convertFile.ConvertFile());
        }

        /// <summary>
        ///   The sound is already ac3
        /// </summary>
        [Test]
        [Category("NotAuto")]
        public void TestConvertSample2()
        {
            var convertFile = new PandaVideoConv();

            // set the output prop
            convertFile.OutputFolder = _outputPath;
            convertFile.WorkingFolder = _outputPath;
            convertFile.SelectedDevice = _selectedDevice;

            convertFile.AnalyseFile(Path.Combine(_samplesPath, SAMPLE_FILE2));

            // Test what we found
            Assert.IsTrue(convertFile.ConvertFile());
        }

        /// <summary>
        ///   This need video recode for PS3 - too many reference frames
        ///   The sound is already ac3
        /// </summary>
        [Test]
        [Category("NotAuto")]
        public void TestConvertSample3()
        {
            var convertFile = new PandaVideoConv();

            // set the output prop
            convertFile.OutputFolder = _outputPath;
            convertFile.WorkingFolder = _outputPath;
            convertFile.SelectedDevice = _selectedDevice;

            convertFile.AnalyseFile(Path.Combine(_samplesPath, SAMPLE_FILE3));

            // Test what we found
            Assert.IsTrue(convertFile.ConvertFile());
        }

        /// <summary>
        ///   This a YouTube video
        ///   The sound is already aac
        /// </summary>
        [Test]
        [Category("NotAuto")]
        public void TestConvertSample4()
        {
            var convertFile = new PandaVideoConv();

            // set the output prop
            convertFile.OutputFolder = _outputPath;
            convertFile.WorkingFolder = _outputPath;
            convertFile.SelectedDevice = _selectedDevice;

            Assert.IsTrue(
                convertFile.AnalyseFile(Path.Combine(_samplesPath,
                                                     SAMPLE_FILE4)));

            // Test what we found
            Assert.IsTrue(convertFile.ConvertFile());
        }

        /// <summary>
        ///   This a Windows 7 wmv video - VC1
        ///   The sound is WMA v2  - 2 channels
        /// </summary>
        [Test]
        [Category("NotAuto")]
        public void TestConvertSample5()
        {
            var convertFile = new PandaVideoConv();

            // set the output prop
            convertFile.OutputFolder = _outputPath;
            convertFile.WorkingFolder = _outputPath;
            convertFile.SelectedDevice = _selectedDevice;

            convertFile.AnalyseFile(Path.Combine(_samplesPath, SAMPLE_FILE5));

            // Test what we found
            Assert.IsTrue(convertFile.ConvertFile());
        }

        /// <summary>
        ///   This an .mp3 to make a ringtone out of
        /// </summary>
        [Test]
        [Category("NotAuto")]
        public void TestConvertSample6()
        {
            var convertFile = new PandaVideoConv();

            // set the output prop
            convertFile.OutputFolder = _outputPath;
            convertFile.WorkingFolder = _outputPath;
            convertFile.SelectedDevice = _selectedDevice;

            Assert.IsTrue(
                convertFile.AnalyseFile(Path.Combine(_samplesPath,
                                                     SAMPLE_FILE6)),
                "Failed Analysis");

            // Test what we found
            Assert.IsTrue(convertFile.ConvertFile(), "Failed Conversion");
        }

        [Test]
        [Category("NotAuto")]
        public void TestConvertSample7()
        {
            var convertFile = new PandaVideoConv();

            // set the output prop
            convertFile.OutputFolder = _outputPath;
            convertFile.WorkingFolder = _outputPath;
            convertFile.SelectedDevice = _selectedDevice;

            convertFile.AnalyseFile(Path.Combine(_samplesPath, SAMPLE_FILE7));

            // Test what we found
            Assert.IsTrue(convertFile.ConvertFile());
        }


        [Test]
        [Category("NotAuto")]
        public void TestConvertSample10()
        {
            var convertFile = new PandaVideoConv();

            // set the output prop
            convertFile.OutputFolder = _outputPath;
            convertFile.WorkingFolder = _outputPath;
            convertFile.SelectedDevice = _selectedDevice;

            Assert.IsTrue(
                convertFile.AnalyseFile(Path.Combine(_samplesPath,
                                                     SAMPLE_FILE8)),
                "Should have been okay");

            // Test what we found
            Assert.IsTrue(convertFile.ConvertFile());
        }

        /// <summary>
        ///   This a Windows 7 wmv video - VC1
        ///   The sound is WMA pro  - 6 channels
        /// </summary>
        [Test]
        [Category("NotAuto")]
        public void TestConvertSample60()
        {
            var convertFile = new PandaVideoConv();

            // set the output prop
            convertFile.OutputFolder = _outputPath;
            convertFile.WorkingFolder = _outputPath;
            convertFile.SelectedDevice = _selectedDevice;

            Assert.IsTrue(convertFile.AnalyseFile(Path.Combine(_samplesPath, SAMPLE_FILE9)));

            // Test what we found
            Assert.IsTrue(convertFile.ConvertFile());
        }


        /// <summary>
        ///   This a HEVC file
        /// </summary>
        [Test]
        [Category("NotAuto")]
        public void TestConvertSampleHEVC()
        {
            var convertFile = new PandaVideoConv();

            // set the output prop
            convertFile.OutputFolder = _outputPath;
            convertFile.WorkingFolder = _outputPath;
            convertFile.SelectedDevice = _selectedDevice;

            Assert.IsTrue(
                convertFile.AnalyseFile(Path.Combine(_samplesPath,
                                                     SAMPLE_FILE10)));

            // Test what we found
            Assert.IsTrue(convertFile.ConvertFile());
        }
    }
}