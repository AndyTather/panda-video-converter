namespace PandaVideoMixer
{
    public class SubTrack : Track
    {
        public SubTrack()
        {
            Type = "subtitles";
        }

        public string UIDescription
        {
            get { return string.Format("{0}: {1} - {2}", ID, CodecID, Language); }
        }
    }
}