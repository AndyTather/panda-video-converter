using System;

namespace PandaVideoMixer
{
    internal sealed class StringExtractMKV
    {
        private const string mkvNewLine = "\r\n";

        public static int GetTrackNumber(string s)
        {
            string t = "|  + Track number: ";
            if (s.Contains(t))
            {
                s = s.Substring(s.IndexOf(t) + t.Length);
                if (s.IndexOf("(track ID for") > 0) // latest mkvtoolsnix has another track item listed inline
                {
                    return int.Parse(s.Substring(0, s.IndexOf("(track ID for")));
                }
                else
                {
                    return int.Parse(s);
                }
            }
            else
            {
                return 0;
            }
        }

        public static bool GetTrackIsDefault(string s)
        {
            string t = "|  + Default flag: ";
            if (s.Contains(t))
            {
                s = s.Substring(s.IndexOf(t) + t.Length);
                if (s.IndexOf(mkvNewLine) > 0)
                {
                    s = s.Substring(0, s.IndexOf(mkvNewLine));
                }
                else
                {
                    s = s.Substring(int.Parse(s), 1);
                }
                return s.Contains("1");
            }
            else
            {
                return false;
            }
        }

        public static string GetLanguage(string s)
        {
            string t = "|  + Language: ";
            if (s.Contains(t))
            {
                s = s.Substring(s.IndexOf(t) + t.Length);
                if (s.IndexOf(mkvNewLine) > 0)
                {
                    return s.Substring(0, s.IndexOf(mkvNewLine));
                }
                else
                {
                    return s.Substring(0, 3);
                }
            }
            else
            {
                return "eng";
            }
        }


        public static string GetVideoFPS(string s)
        {
            string t = "|  + Default duration: ";
            if (s.Contains(t))
            {
                s = s.Substring(s.IndexOf(t) + t.Length);
                s = s.Substring(s.IndexOf("(") + 1);
                if (s.IndexOf(" ") > 0)
                {
                    return s.Substring(0, s.IndexOf(" "));
                }
                else
                {
                    return s.Substring(0, 4);
                }
            }
            else
            {
                return "0";
            }
        }

        public static string GetTrackType(string s)
        {
            string t = "|  + Track type: ";
            if (s.Contains(t))
            {
                s = s.Substring(s.IndexOf(t) + t.Length);
                if (s.IndexOf(mkvNewLine) > 0)
                {
                    return s.Substring(0, s.IndexOf(mkvNewLine));
                }
                else
                {
                    return s.Substring(0, 5);
                }
            }
            else
            {
                return "0";
            }
        }

        public static int GetVideoWidth(string mkvInfoOutput)
        {
            String s;
            String t = "+ Pixel width: ";
            if (mkvInfoOutput.Contains(t))
            {
                s = mkvInfoOutput.Substring(mkvInfoOutput.IndexOf(t) + t.Length);
                s = s.Substring(0, s.IndexOf(mkvNewLine));
                return int.Parse(s);
            }
            return 0;
        }

        public static int GetVideoHeight(string mkvInfoOutput)
        {
            String s;
            String t = "+ Pixel height: ";
            if (mkvInfoOutput.Contains(t))
            {
                s = mkvInfoOutput.Substring(mkvInfoOutput.IndexOf(t) + t.Length);
                s = s.Substring(0, s.IndexOf(mkvNewLine));
                return int.Parse(s);
            }
            return 0;
        }

        // Media Info

        public static int GetReferenceFrames(string mediaInfo)
        {
            string t = "Format settings, ReFrames";
            if (mediaInfo.Contains(t))
            {
                string s = mediaInfo.Substring(mediaInfo.IndexOf(t) + t.Length);
                s = s.Substring(s.IndexOf(": ") + 2);
                int i = s.IndexOf(mkvNewLine);
                if (i == -1)
                {
                    i = 2; // pick default value in case vbNewLine isn't in string
                }
                s = s.Substring(0, i); // this will either be a 2 digit number or 1 number followed by a letter

                var stringSeparators = new string[] {"frames"};
                string[] columns = s.Split(stringSeparators, StringSplitOptions.RemoveEmptyEntries);

                int result;
                if (int.TryParse(columns[0], out result))
                {
                    return int.Parse(columns[0]);
                }
                else
                {
                    return int.Parse(s.Substring(0, 1));
                }
            }
            return 0;
        }

        public static int GetMIAudioTrack(string mediaInfo)
        {
            string t = "Audio #";
            if (mediaInfo.Contains(t))
            {
                string s = mediaInfo.Substring(mediaInfo.IndexOf(t) + t.Length);
                int i = s.IndexOf(mkvNewLine);
                if (i == -1)
                {
                    i = 2; // pick default value in case vbNewLine isn't in string
                }
                s = s.Substring(0, i); // this will either be a 2 digit number or 1 number followed by a letter

                int result;

                if (int.TryParse(s, out result))
                {
                    return int.Parse(s);
                }
            }
            return 1;
        }

        public static int GetBitRate(string mediaInfo)
        {
            string t = "Bit rate  ";
            if (mediaInfo.Contains(t))
            {
                string s = mediaInfo.Substring(mediaInfo.IndexOf(t) + t.Length);
                s = s.Substring(s.IndexOf(": ") + 2);
                int i = s.IndexOf(Environment.NewLine);
                if (i == -1)
                {
                    i = 2;
                }
                s = s.Substring(0, i).Replace(" ", "");

                var stringSeparators = new string[] {"bps", "Kbps", "Mbps"};
                string[] columns = s.Split(stringSeparators, StringSplitOptions.RemoveEmptyEntries);

                decimal result;
                string v = columns[0];
                if (decimal.TryParse(v, out result))
                {
                    if (s.Contains("Mbps"))
                        return (int) (result*1024);
                    else if (s.Contains("Kbps"))
                        return (int) result;
                    else
                        return (int) (result*1024);
                }
            }
            return 0;
        }

        public static int GetNominalBitRate(string mediaInfo)
        {
            string t = "Nominal bit rate  ";
            if (mediaInfo.Contains(t))
            {
                string s = mediaInfo.Substring(mediaInfo.IndexOf(t) + t.Length);
                s = s.Substring(s.IndexOf(": ") + 2);
                int i = s.IndexOf(Environment.NewLine);
                if (i == -1)
                {
                    i = 2;
                }
                s = s.Substring(0, i).Replace(" ", "");

                var stringSeparators = new string[] {"bps", "Kbps", "Mbps"};
                string[] columns = s.Split(stringSeparators, StringSplitOptions.RemoveEmptyEntries);

                decimal result;
                string v = columns[0];
                if (decimal.TryParse(v, out result))
                {
                    if (s.Contains("Mbps"))
                        return (int) (result*1024);
                    else if (s.Contains("Kbps"))
                        return (int) result;
                    else
                        return (int) (result*1024);
                }
            }
            return 0;
        }

        public static int GetID(string mediaInfo)
        {
            string t = "Video\r\nID ";
            if (mediaInfo.Contains(t))
            {
                string s = mediaInfo.Substring(mediaInfo.IndexOf(t) + t.Length);
                s = s.Substring(s.IndexOf(": ") + 2);
                int i = s.IndexOf(mkvNewLine);
                if (i == -1)
                {
                    i = 2;
                }
                s = s.Substring(0, i);

                var stringSeparators = new string[] {" "};
                string[] columns = s.Split(stringSeparators, StringSplitOptions.RemoveEmptyEntries);


                int result;
                s = columns[0];
                if (int.TryParse(s, out result))
                {
                    return int.Parse(s);
                }
            }
            return 0;
        }
    }
}