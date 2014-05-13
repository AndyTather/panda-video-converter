namespace PandaVideoMixer
{
    public class Track
    {
        public int ID { get; set; }

        public string Format { get; set; }

        public string Language { get; set; }

        public string Type { get; set; }

        public string CodecID { get; set; }

        public float FPS { get; set; }

        public bool DefaultTrack { get; set; }

        public string WorkingFile { get; set; }

        public int BitRate { get; set; } // in Kbps

        public bool RequiresRecode { get; set; }

        public string FmtProfile { get; set; }

        public string Title { get; set; }

        public bool Preferred { get; set; }
    }
}