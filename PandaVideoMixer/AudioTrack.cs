namespace PandaVideoMixer
{
    public class AudioTrack : Track
    {
        public int AID { get; set; }
        public int Channels { get; set; }
        public int Delay { get; set; }
        public float SampleRate { get; set; }

        public AudioTrack()
        {
            Type = "Audio";
            RequiresRecode = false;
            Delay = 0; // ms
            SampleRate = 0.0f;
        }

        public string UIDescription
        {
            get { return string.Format("{0}: {1} - {2}", ID, CodecID, Language); }
        }
    }
}