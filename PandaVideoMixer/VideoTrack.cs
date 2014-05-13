namespace PandaVideoMixer
{
    public class VideoTrack : Track
    {
        public VideoTrack()
        {
            Type = "Video";
            RequiresRecode = false;
        }

        public int VID { get; set; }
        public string PixelAspectRatio { get; set; }

        public int Width { get; set; }

        public int Height { get; set; }

        public int OriginalHeight { get; set; }

        public string Profile { get; set; }

        public int ActualRefFrames { get; set; }

        public int MaxRefFrames { get; set; }

        public string EncodingSettings { get; set; }

        public int BPyramid { get; set; }

        public string UIDescription
        {
            get { return string.Format("{0}: {1}", ID, Format); }
        }
    }
}