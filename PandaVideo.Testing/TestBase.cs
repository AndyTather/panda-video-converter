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

        public const string SAMPLE_FILE1 = "compass.mkv";
        public const string SAMPLE_FILE2 = "casino.mkv";
        public const string SAMPLE_FILE3 = "Iron.Man.720p.mkv";
        public const string SAMPLE_FILE4 = "Everybody's Free to Wear Sunscreen! (Original + English Subtitles).flv";
        public const string SAMPLE_FILE5 = "Wildlife.wmv";
        public const string SAMPLE_FILE6 = "Kasabian - Underdog - West Ryder Pauper Lunatic Asylum.mp3";
        public const string SAMPLE_FILE7 = "hd_other_bbc_motion_gallery_cctv.m2ts";
        public const string SAMPLE_FILE8 = "SAMPLE-Casino.Royale.2006.BluRay.1080p.x264.FLAC-iLL.mkv";
        public const string SAMPLE_FILE9 = "Robotica_1080.wmv";
        public const string SAMPLE_FILE10 = "TearsOfSteelFull12min_1080p_24fps_27qp_1474kbps_GPSNR_42.29_HM11.mkv";
        public const string SAMPLE_FILE11 = "21.2008.720p.BluRay.DTS.x264-ESiR.sample.mkv";
        public const string SAMPLE_FILE12 = "gonzo_2000_okinawa_flac_7.1.mkv";
        public const string SAMPLE_FILE13 = "Hannah Montana The Movie";
        public const string SAMPLE_FILE14 = "howl.2010.limited.720p.bluray.x264-amiable.sample.mkv";
        public const string SAMPLE_FILE15 = "The Company Men 2010 1080p BluRay X264 Subtox (1)-001.mkv";
        public const string SAMPLE_FILE16 = "4k-uhdson.mov";
        public const string SAMPLE_FILE17 = "HONEY BEES 96fps IN 4K (ULTRA HD)(Original_H.264-AAC).mp4";

        public TestBase()
        {
            _samplesPath = Environment.CurrentDirectory + @"\..\..\PandaVideoSamples";
        }
    }
}
