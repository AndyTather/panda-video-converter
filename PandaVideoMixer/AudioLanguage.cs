namespace PandaVideoMixer
{
    public class AudioLanguage
    {
        public string DisplayName { get; set; }
        public string Lang { get; set; }

        public AudioLanguage(string displayName, string lang)
        {
            DisplayName = displayName;
            Lang = lang;
        }
    }
}