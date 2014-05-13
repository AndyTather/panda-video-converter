using System;
using System.Text.RegularExpressions;

// String extraction functions abstracted out.

namespace PandaVideoMixer
{
    internal sealed class StringExtract
    {
        public static int GetTrackNumber(string s)
        {
            string t = "|  + Track number: ";
            if (s.Contains(t))
            {
                s = s.Substring(s.IndexOf(t) + t.Length);
                if (s.IndexOf(Environment.NewLine) > 0)
                {
                    return int.Parse(s.Substring(0, s.IndexOf(Environment.NewLine)));
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
                if (s.IndexOf(Environment.NewLine) > 0)
                {
                    s = s.Substring(0, s.IndexOf(Environment.NewLine));
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
            string t = "Language";
            if (s.Contains(t))
            {
                s = s.Substring(s.IndexOf(t) + t.Length);
                if (s.IndexOf(Environment.NewLine) > 0)
                {
                    return s.Substring(0, s.IndexOf(Environment.NewLine));
                }
                else
                {
                    return s.Substring(0, 3);
                }
            }
            else
            {
                return "";
            }
        }

        public static string GetFormat(string s)
        {
            string t = "Format  ";
            if (s.Contains(t))
            {
                s = s.Substring(s.IndexOf(t));
                s = s.Substring(s.IndexOf(":") + 2);
                if (s.IndexOf(Environment.NewLine) > 0)
                {
                    return s.Substring(0, s.IndexOf(Environment.NewLine));
                }
                else
                {
                    return s.Substring(0, 3);
                }
            }
            else
            {
                return "";
            }
        }

        public static string GetVideoFPS(string mkvInfoOutput)
        {
            string t = "Frame rate  ";
            if (mkvInfoOutput.Contains(t))
            {
                mkvInfoOutput = mkvInfoOutput.Substring(mkvInfoOutput.IndexOf(t));
                string s = mkvInfoOutput.Substring(mkvInfoOutput.IndexOf(":") + 2);
                s = s.Substring(0, s.IndexOf(Environment.NewLine));

                var reg = new Regex(@"[^0-9.]");
                s = reg.Replace(s, "");
                return s;
            }
            return "0";
        }

        public static int GetVideoWidth(string mkvInfoOutput)
        {
            String t = "Width";

            if (mkvInfoOutput.Contains(t))
            {
                String s;
                s = mkvInfoOutput.Substring(mkvInfoOutput.IndexOf(t) + t.Length);
                s = s.Substring(0, s.IndexOf(Environment.NewLine));

                var reg = new Regex(@"[^0-9]");
                s = reg.Replace(s, "");
                return int.Parse(s);
            }
            return 0;
        }

        public static int GetVideoHeight(string mkvInfoOutput)
        {
            String s;
            String t = "Height";
            if (mkvInfoOutput.Contains(t))
            {
                s = mkvInfoOutput.Substring(mkvInfoOutput.IndexOf(t) + t.Length);
                s = s.Substring(0, s.IndexOf(Environment.NewLine));
                var reg = new Regex(@"[^0-9]");
                s = reg.Replace(s, "");
                return int.Parse(s);
            }
            return 0;
        }

        public static int GetVideoOriginalHeight(string mkvInfoOutput)
        {
            String s;
            String t = "Original height";
            if (mkvInfoOutput.Contains(t))
            {
                s = mkvInfoOutput.Substring(mkvInfoOutput.IndexOf(t) + t.Length);
                s = s.Substring(0, s.IndexOf(Environment.NewLine));
                var reg = new Regex(@"[^0-9]");
                s = reg.Replace(s, "");
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
                int i = s.IndexOf(Environment.NewLine);
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
                int i = s.IndexOf(Environment.NewLine);
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
            return -1;
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

        public static int GetMaximumBitRate(string mediaInfo)
        {
            string t = "Maximum bit rate  ";
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

        public static float GetSampleRate(string mediaInfo)
        {
            string t = "Sampling rate  ";
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

                var stringSeparators = new string[] {"Hz", "KHz", "MHz"};
                string[] columns = s.Split(stringSeparators, StringSplitOptions.RemoveEmptyEntries);

                decimal result;
                string v = columns[0];
                if (decimal.TryParse(v, out result))
                {
                    if (s.Contains("MHz"))
                        return (float) (result*1024);
                    else if (s.Contains("KHz"))
                        return (float) result;
                    else
                        return (float) (result*1024);
                }
            }
            return 0.0f;
        }

        public static int GetID(string type, string mediaInfo)
        {
            string t = type + "\r\nID ";
            if (mediaInfo.Contains(t))
            {
                string s = mediaInfo.Substring(mediaInfo.IndexOf(t) + t.Length);
                s = s.Substring(s.IndexOf(": ") + 2);
                int i = s.IndexOf(Environment.NewLine);
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
            return -1;
        }

        public static string GetCodecID(string s)
        {
            string t = "Codec ID: ";
            if (s.Contains(t))
            {
                s = s.Substring(s.IndexOf(t) + t.Length);
                if ((s.IndexOf("\r\r\n")) > 0)
                    s = s.Substring(0, s.IndexOf("\r\r\n"));
                else
                    s = s.Substring(0, 5);
                return s; // ExtractCodec(s);
            }
            else
                return "";
        }


        public static string GetFormatProfile(string mediaInfo)
        {
            string t = "Format profile ";
            if (mediaInfo.Contains(t))
            {
                string s = mediaInfo.Substring(mediaInfo.IndexOf(t) + t.Length);
                s = s.Substring(s.IndexOf(": ") + 2);
                if ((s.IndexOf("\r\n")) > 0)
                    s = s.Substring(0, s.IndexOf("\r\n"));
                else
                    s = s.Substring(0, 5);
                return s;
            }
            else
                return "";
        }

        // 
        public static int GetVideoDelay(string mediaInfo)
        {
            string t = "Delay relative to video ";
            if (mediaInfo.Contains(t))
            {
                string s = mediaInfo.Substring(mediaInfo.IndexOf(t) + t.Length);
                s = s.Substring(s.IndexOf(": ") + 2);
                if ((s.IndexOf("\r\n")) > 0)
                    s = s.Substring(0, s.IndexOf("\r\n"));
                else
                    s = s.Substring(0, 5);

                var stringSeparators = new string[] {"ms"};
                string[] columns = s.Split(stringSeparators, StringSplitOptions.RemoveEmptyEntries);
                string d = columns[0];

                return int.Parse(d);
            }
            else
                return 0;
        }


        public static int GetChannels(string segment)
        {
            String s;
            String t = "Channel(s)";
            if (segment.Contains(t))
            {
                s = segment.Substring(segment.IndexOf(t) + t.Length);
                s = s.Substring(0, s.IndexOf(Environment.NewLine));
                var reg = new Regex(@"[^0-9]");
                s = reg.Replace(s, "");
                return int.Parse(s);
            }
            return 0;
        }

        public static string GetEncodingSettings(string mediaInfo)
        {
            string t = "Encoding settings ";
            if (mediaInfo.Contains(t))
            {
                string s = mediaInfo.Substring(mediaInfo.IndexOf(t) + t.Length);
                s = s.Substring(s.IndexOf(": ") + 2);
                if ((s.IndexOf("\r\n")) > 0)
                    s = s.Substring(0, s.IndexOf("\r\n"));
                else
                    s = s.Substring(0, 5);
                return s;
            }
            else
                return "";
        }

        public static string GetMovieName(string s)
        {
            string t = "Movie name  ";
            if (s.Contains(t))
            {
                s = s.Substring(s.IndexOf(t));
                s = s.Substring(s.IndexOf(":") + 2);
                if (s.IndexOf(Environment.NewLine) > 0)
                {
                    return s.Substring(0, s.IndexOf(Environment.NewLine));
                }
                else
                {
                    return s.Substring(0, 3);
                }
            }
            else
            {
                return "";
            }
        }

        public static string GetTitle(string s)
        {
            string t = "Title  ";
            if (s.Contains(t))
            {
                s = s.Substring(s.IndexOf(t));
                s = s.Substring(s.IndexOf(":") + 2);
                if (s.IndexOf(Environment.NewLine) > 0)
                {
                    return s.Substring(0, s.IndexOf(Environment.NewLine));
                }
                else
                {
                    return s.Substring(0, 3);
                }
            }
            else
            {
                return "";
            }
        }

        public static double GetDuration(string s)
        {
            string t = "Duration";
            if (s.Contains(t))
            {
                s = s.Substring(s.IndexOf(t));
                s = s.Substring(s.IndexOf(":") + 2);
                if (s.IndexOf(",") > 0)
                {
                    string tm = s.Substring(0, s.IndexOf(","));
                    TimeSpan result;
                    if (TimeSpan.TryParse(tm, out result))
                    {
                        return result.TotalSeconds;
                    }
                }
            }
            return 0;
        }

        public static double GetTime(string s)
        {
            string t = "time";
            if (s.Contains(t))
            {
                s = s.Substring(s.IndexOf(t));
                s = s.Substring(s.IndexOf("=") + 1);
                if (s.IndexOf(" ") > 0)
                {
                    string tm = s.Substring(0, s.IndexOf(" "));
                    TimeSpan result;
                    if (TimeSpan.TryParse(tm, out result))
                    {
                        return result.TotalSeconds;
                    }
                }
            }
            return 0;
        }
    }
}