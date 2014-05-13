using System;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using PandaVideoMixer;

namespace PandaVideo.Testing
{
    [TestFixture]
    public class TestWD : TestBase
    {
        private readonly IOutputDevice _device = new DeviceWDLiveTV();

        [TestFixtureSetUp]
        public void Init()
        {
            _outputPath = Path.Combine(_samplesPath, @"Output\WD");
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
            convertFile.SelectedDevice = _device;

            convertFile.AnalyseFile(Path.Combine(_samplesPath, SAMPLE_FILE1));

            // Test what we found
            Assert.IsTrue(convertFile.ConvertFile());
        }

        /// <summary>
        ///   This need video recode for PS3 - too many reference frames
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
            convertFile.SelectedDevice = _device;

            convertFile.AnalyseFile(Path.Combine(_samplesPath, SAMPLE_FILE2));

            // Test what we found
            Assert.IsTrue(convertFile.ConvertFile());
        }

        /// <summary>
        ///   No video recode required
        ///   The sound is dts
        /// </summary>
        [Test]
        [Category("NotAuto")]
        public void TestConvertSample3()
        {
            var convertFile = new PandaVideoConv();

            // set the output prop
            convertFile.OutputFolder = _outputPath;
            convertFile.WorkingFolder = _outputPath;
            convertFile.SelectedDevice = _device;

            convertFile.AnalyseFile(Path.Combine(_samplesPath, SAMPLE_FILE3));

            // Test what we found
            Assert.IsTrue(convertFile.ConvertFile());
        }

        /// <summary>
        ///   This need video recode for PS3 - too many reference frames
        ///   The sound has two eng tracks DTS and VOBIS
        /// </summary>
        [Test]
        [Category("NotAuto")]
        public void TestConvertSample4()
        {
            var convertFile = new PandaVideoConv();

            // set the output prop
            convertFile.OutputFolder = _outputPath;
            convertFile.WorkingFolder = _outputPath;
            convertFile.SelectedDevice = _device;

            convertFile.AnalyseFile(Path.Combine(_samplesPath, SAMPLE_FILE11));

            // Test what we found
            Assert.IsTrue(convertFile.ConvertFile());
        }

        /// <summary>
        ///   This a YouTube video
        ///   The sound is already aac
        /// </summary>
        [Test]
        [Category("NotAuto")]
        public void TestConvertSample5()
        {
            var convertFile = new PandaVideoConv();

            // set the output prop
            convertFile.OutputFolder = _outputPath;
            convertFile.WorkingFolder = _outputPath;
            convertFile.SelectedDevice = _device;

            Assert.IsTrue(
                convertFile.AnalyseFile(Path.Combine(_samplesPath,
                                                     SAMPLE_FILE4)));

            // Test what we found
            Assert.IsTrue(convertFile.ConvertFile());
        }


        [Test]
        [Category("NotAuto")]
        public void TestConvertSample7()
        {
            var convertFile = new PandaVideoConv();

            // set the output prop
            convertFile.OutputFolder = _outputPath;
            convertFile.WorkingFolder = _outputPath;
            convertFile.SelectedDevice = _device;

            convertFile.AnalyseFile(Path.Combine(_samplesPath, SAMPLE_FILE7));

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
            convertFile.SelectedDevice = _device;

            Assert.IsTrue(convertFile.AnalyseFile(Path.Combine(_samplesPath, SAMPLE_FILE9)));

            // Test what we found
            Assert.IsTrue(convertFile.ConvertFile());
        }

        /// <summary>
        ///   No video recode required
        ///   first sound stream FLAC
        ///   second is AC3 which would need selecting
        /// </summary>
        [Test]
        [Category("NotAuto")]
        public void TestConvertSample70()
        {
            var convertFile = new PandaVideoConv();

            // set the output prop
            convertFile.OutputFolder = _outputPath;
            convertFile.WorkingFolder = _outputPath;
            convertFile.SelectedDevice = _device;

            convertFile.AnalyseFile(Path.Combine(_samplesPath, SAMPLE_FILE12));

            List<AudioTrack> audList = convertFile.GetAudioTracks();
            convertFile.SelectedAudioTrack = audList[0];

            // Test what we found
            Assert.IsTrue(convertFile.ConvertFile());
        }

        /// <summary>
        ///   No video recode required
        ///   sound stream FLAC
        /// </summary>
        [Test]
        [Category("NotAuto")]
        public void TestConvertSample80()
        {
            var convertFile = new PandaVideoConv();

            // set the output prop
            convertFile.OutputFolder = _outputPath;
            convertFile.WorkingFolder = _outputPath;
            convertFile.SelectedDevice = _device;

            Assert.IsTrue(
                convertFile.AnalyseFile(Path.Combine(_samplesPath,
                                                     SAMPLE_FILE8)),
                "Should have been okay");

            // Test what we found
            Assert.IsTrue(convertFile.ConvertFile());
        }

        /// <summary>
        ///   No video recode required
        ///   sound stream FLAC
        /// </summary>
        [Test]
        [Category("NotAuto")]
        public void TestConvertSubtitlesSample()
        {
            var convertFile = new PandaVideoConv();

            // set the output prop
            convertFile.OutputFolder = _outputPath;
            convertFile.WorkingFolder = _outputPath;
            convertFile.SelectedDevice = _device;
            convertFile.EncodeSubtitles = true;

            Assert.IsTrue(
                convertFile.AnalyseFile(Path.Combine(_samplesPath,
                                                     SAMPLE_FILE14)),
                "Should have been okay");

            // Test what we found
            Assert.IsTrue(convertFile.ConvertFile());
        }

        /// <summary>
        ///   This need video recode for PS3 - too many reference frames
        ///   The sound is already ac3
        /// </summary>
        [Test]
        [Category("NotAuto")]
        public void TestConvertSampleDTS_MA()
        {
            var convertFile = new PandaVideoConv();

            // set the output prop
            convertFile.OutputFolder = _outputPath;
            convertFile.WorkingFolder = _outputPath;
            convertFile.SelectedDevice = _device;

            convertFile.AnalyseFile(Path.Combine(_samplesPath,
                                                 SAMPLE_FILE15));

            // Test what we found
            Assert.IsTrue(convertFile.ConvertFile());
        }

        /*
        [Test]
        [Category("NotAuto")]
        public void TestConvertVP6()
        {
            PandaVideoConv convertFile = new PandaVideoConv();

            // set the output prop
            convertFile.OutputFolder = outputPath;
            convertFile.WorkingFolder = outputPath;
            convertFile.SelectedDevice = device;

            convertFile.AnalyseFile(Path.Combine(samplesPath, "Test.flv"));

            // Test what we found
            Assert.IsTrue(convertFile.ConvertFile());

        }

        [Test]
        [Category("NotAuto")]
        public void TestConvertWMV2()
        {
            PandaVideoConv convertFile = new PandaVideoConv();

            // set the output prop
            convertFile.OutputFolder = outputPath;
            convertFile.WorkingFolder = outputPath;
            convertFile.SelectedDevice = device;

            convertFile.AnalyseFile(Path.Combine(samplesPath, "Test2.wmv"));

            // Test what we found
            Assert.IsTrue(convertFile.ConvertFile());

        }
        */
    }
}