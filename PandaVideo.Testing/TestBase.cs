using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PandaVideo.Testing
{
    public class TestBase
    {
        protected string _samplesPath;
        protected string _outputPath;

        public const string SAMPLE_FILE1 = "SAMPLE_FILE1.mkv";
        public const string SAMPLE_FILE2 = "SAMPLE_FILE2.mkv";
        public const string SAMPLE_FILE3 = "SAMPLE_FILE3.mkv";
        public const string SAMPLE_FILE4 = "SAMPLE_FILE4.flv";
        public const string SAMPLE_FILE5 = "SAMPLE_FILE5.wmv";
        public const string SAMPLE_FILE6 = "SAMPLE_FILE6.mp3";
        public const string SAMPLE_FILE7 = "SAMPLE_FILE7.m2ts";
        public const string SAMPLE_FILE8 = "SAMPLE_FILE8.mkv";
        public const string SAMPLE_FILE9 = "SAMPLE_FILE9.wmv";
        public const string SAMPLE_FILE10 = "SAMPLE_FILE10.mkv";
        public const string SAMPLE_FILE11 = "SAMPLE_FILE11.mkv";
        public const string SAMPLE_FILE12 = "SAMPLE_FILE12.mkv";
        public const string SAMPLE_FILE13 = "SAMPLE_FILE13";
        public const string SAMPLE_FILE14 = "SAMPLE_FILE14.mkv";
        public const string SAMPLE_FILE15 = "SAMPLE_FILE15.mkv";

        public TestBase()
        {
            _samplesPath = Environment.CurrentDirectory + @"\..\..\PandaVideoSamples";
        }
    }
}
