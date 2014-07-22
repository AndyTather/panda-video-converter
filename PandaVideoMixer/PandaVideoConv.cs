//-------------------------------------------------------------------------------------------------
// <copyright file="PandaVideoConv.cs" company="Pandasoft">
//    Copyright (c) Pandasoft - Andy Tather.  All rights reserved.
//    
//    The use and distribution terms for this software are covered by the
//    Common Public License 1.0 (http://opensource.org/licenses/cpl.php)
//    By using this software in any fashion, you are agreeing to be bound by
//    the terms of this license.
//    
//    You must not remove this notice, or any other, from this software.
// </copyright>
// 
// <summary>
// The conversion engine.
// </summary>
//-------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace PandaVideoMixer
{
    // A delegate type for hooking up change notifications.
    public delegate void ChangedEventHandler(object sender, ProgressEventArgs e);

    public class PandaVideoConv
    {
        public static IList<IOutputDevice> OutputDeviceList = new List<IOutputDevice>
            {
                new DeviceAVCHD(),
                new DeviceBluray(),
                new DeviceGeneric(),
                new DeviceHTML5(),
                new DeviceiPad(),
                new DeviceiPhone3GS(),
                new DevicePS3(),
                new DeviceSamsungS3(),
                new DeviceSamsungS4(),
                new DeviceSamsungS5(),
                new DeviceSamsungUHDTV(),
                /*    new DeviceSonos(), */
                new DeviceWDLiveTV(),
                new DeviceXBox360(),
                new DeviceRaw()
            };

        public static IList<AudioLanguage> LanguageList = new List<AudioLanguage>
            {
                new AudioLanguage("Danish", "dan"),
                new AudioLanguage("Dutch", "dut"),
                new AudioLanguage("English", "eng"),
                new AudioLanguage("Finnish", "fin"),
                new AudioLanguage("French", "fre"),
                new AudioLanguage("German", "ger"),
                new AudioLanguage("Hungarian", "hun"),
                new AudioLanguage("Italian", "ita"),
                new AudioLanguage("Japanese", "jpn"),
                new AudioLanguage("Norwegian", "nor"),
                new AudioLanguage("Polish", "pol"),
                new AudioLanguage("Portuguese", "por"),
                new AudioLanguage("Russian", "rus"),
                new AudioLanguage("Spanish", "spa"),
                new AudioLanguage("Swedish", "swe"),
                new AudioLanguage("Turkish", "tur")
            };

        private readonly Regex _rx = new Regex(@"^\d+(\.\d)");

        private string _sourceFile;
        private string _currentFileName;

        private VideoTrack _defaultVideoTrack;
        private VideoTrack _selectedVideoTrack;

        private AudioTrack _defaultAudioTrack;
        private AudioTrack _selectedAudioTrack;

        private SubTrack _defaultSubTrack;
        private SubTrack _selectedSubTrack;

        public SubTrack SelectedSubTrack
        {
            get { return _selectedSubTrack; }
            set { _selectedSubTrack = value; }
        }

        public AudioTrack SelectedAudioTrack
        {
            get { return _selectedAudioTrack; }
            set { _selectedAudioTrack = value; }
        }

        public VideoTrack SelectedVideoTrack
        {
            get { return _selectedVideoTrack; }
            set { _selectedVideoTrack = value; }
        }

        public string PrefferedLanguage { get; set; }

        public IOutputDevice SelectedDevice { get; set; }

        public bool DiscMode { get; set; }
        public bool RingTone { get; set; }

        public bool EncodeSubtitles { get; set; }
        public bool ForceVideoRecode { get; set; }

        private string appPath;

        public String SourceVideoFile
        {
            get { return _sourceFile; }
        }

        public String OutputFolder { get; set; }
        public String WorkingFolder { get; set; }
        public String OutputFileName { get; set; }
        public String Title { get; set; }
        public bool ConvertTrueHD { get; set; }

        private List<RefClass> refFrameList = new List<RefClass>();

        private List<Track> trackList = new List<Track>();

        private Process _process;

        private StringBuilder _outputLog = new StringBuilder();

        // An event that clients can use to be notified whenever the
        // elements of the list change.
        public event ChangedEventHandler StatusChanged;


        public PandaVideoConv()
        {
            appPath = AppDomain.CurrentDomain.BaseDirectory;
            PrefferedLanguage = "eng";
            DiscMode = false;
            RingTone = false;
            EncodeSubtitles = false;
            ForceVideoRecode = false;

            ConvertTrueHD = true;
        }

        ~PandaVideoConv()
        {
            if (_process != null &&
                !_process.HasExited)
                _process.Kill();
        }

        public bool AnalyseFile(string filename)
        {
            bool result = true;
            var mediaInfo = new StringBuilder();

            FillRefList();

            ProcessStatusChange(0, "Starting Analysis of " + Path.GetFileName(filename));

            trackList.Clear();
            _sourceFile = filename;

            _currentFileName = Path.GetFileNameWithoutExtension(filename);
            string ext = Path.GetExtension(filename);

            // Quick for unit tests
            if (DiscMode)
            {
                if (!Directory.Exists(filename))
                    result = false;
            }
            else
            {
                if (!File.Exists(filename))
                    result = false;
            }

            // Test ext for a .mkv
            // Then convert the MKV file container to something suitable
            if (String.Compare(ext, ".mkv", true) == 0)
            {
                FillRefList();

                string mkvinfo = GetMKVTrackInfo(_sourceFile);
                ParseMKVTrackInfo(mkvinfo);
                mediaInfo.Append(mkvinfo);

                ProcessStatusChange(50, null);

                string mediainfo = GetMediaInfo(_sourceFile);
                ParseMediaInfoMKV(mediainfo);
                mediaInfo.Append(mediainfo);

                List<AudioTrack> alist = GetAudioTracks();
                foreach (var at in alist)
                {
                    if (at.Language.Contains(PrefferedLanguage) &&
                        (at.CodecID == "A_DTS" || at.CodecID == "A_DTS_MA" || at.CodecID == "A_TRUEHD" ||
                         at.CodecID == "A_AC3"))
                    {
                        SelectedAudioTrack = at;
                        break;
                    }
                }
            }
            else
            {
                string mediainfo = GetMediaInfo(_sourceFile);
                ParseMediaInfo(mediainfo);
                mediaInfo.Append(mediainfo);
            }

            // Do some processing on data we retrieved
            List<VideoTrack> vlist = GetVideoTracks();
            foreach (var vt in vlist)
            {
                int maxRefFrames = 0;
                bool refRecode = CheckReference(vt.Width, vt.Height, vt.ActualRefFrames, out maxRefFrames);
                if (vt.CodecID != "V_MPEG4/ISO/AVC" || refRecode)
                    vt.RequiresRecode = true;
                else
                    vt.RequiresRecode = false;
                vt.MaxRefFrames = maxRefFrames;


                if (_selectedVideoTrack == null)
                    _defaultVideoTrack = _selectedVideoTrack = vt;


                switch (SelectedDevice.Type)
                {
                    case OutputDevice.PS3:
                        if (String.Compare(ext, ".flv", true) == 0)
                        {
                            vt.RequiresRecode = true;
                        }
                        else if (String.Compare(ext, ".mkv", true) == 0)
                        {
                            // test
                            if (vt.CodecID == @"V_MS/VFW/FOURCC" ||
                                vt.CodecID == @"V_MS/VFW/WVC1")
                            {
                                vt.RequiresRecode = true;
                                vt.CodecID = @"V_MS/VFW/WVC1";
                            }
                        }
                        // if we need subtitles for PS3 then hardcoded reencode is required
                        if (EncodeSubtitles)
                            vt.RequiresRecode = true;

                        break;

                    case OutputDevice.iPhone3GS:
                    case OutputDevice.iPad:
                        vt.RequiresRecode = true;
                        break;
                }
            }

            // Sort out Audio
            List<AudioTrack> audioList = GetAudioTracks();
            foreach (var at in audioList)
            {
                switch (SelectedDevice.Type)
                {
                    case OutputDevice.PS3:
                        if (at.CodecID == "A_DTS" || at.CodecID == "A_DTS_MA" || at.CodecID == "A_FLAC" ||
                            at.CodecID == "A_TRUEHD")
                            at.RequiresRecode = true;
                        else
                            at.RequiresRecode = false;
                        break;

                    case OutputDevice.SamsungS4:
                    case OutputDevice.SamsungS3:
                    case OutputDevice.iPhone3GS:
                    case OutputDevice.iPad:
                        if (at.CodecID != "A_AAC")
                            at.RequiresRecode = true;
                        break;

                    case OutputDevice.XBox360:
                        if (at.CodecID != "A_AAC")
                            at.RequiresRecode = true;
                        break;

                    case OutputDevice.Sonos:
                        if (at.SampleRate == 96.0f)
                            at.RequiresRecode = true;
                        break;
                }

                if (SelectedAudioTrack == null)
                    SelectedAudioTrack = at;

                if (at.Language.Contains(PrefferedLanguage))
                {
                    at.Preferred = true;
                }
            }


            // Sort out Subtitles

            var subtitleList = GetSubtitleTracks();
            foreach (var sub in subtitleList)
            {
                if (sub.Language.Contains(PrefferedLanguage))
                {
                    sub.Preferred = true;
                }
            }


            var outputReport = new OutputReport();
            outputReport.outputReport = mediaInfo.ToString();
            if (result)
                ProcessStatusChange(100, "Analysis of " + Path.GetFileName(filename) + " complete.", outputReport);
            else
                ProcessStatusChange(100, "Analysis of " + Path.GetFileName(filename) + " failed.", outputReport);

            return true;
        }


        public bool ConvertFile()
        {
            var sw = new Stopwatch();
            sw.Start();

            var outputReport = new OutputReport();

            // Do a quick check on working folder
            if (String.IsNullOrEmpty(WorkingFolder))
                WorkingFolder = OutputFolder;


            ProcessStatusChange(0, "Starting conversion of " + Path.GetFileName(_sourceFile));

            VideoTrack vt = GetVideoTrack();

            if (_selectedAudioTrack == null)
                _selectedAudioTrack = GetAudioTrack();

            if (ForceVideoRecode == true)
                vt.RequiresRecode = true;

            bool result = false;
            string ext = Path.GetExtension(_sourceFile);

            switch (SelectedDevice.Type)
            {
                case OutputDevice.PS3:

                    if (String.Compare(ext, ".mkv", true) == 0)
                    {
                        if (vt.RequiresRecode)
                        {
                            MKVExtractTracks(_sourceFile, null, _selectedAudioTrack, SelectedSubTrack); // sound only 

                            // Video needs extra processing
                            if (EncodeSubtitles)
                                MEncoder_PS3_preprocess_video(_sourceFile, vt, SelectedSubTrack);
                            else
                                MEncoder_PS3_preprocess_video(_sourceFile, vt, null);
                        }
                        else
                            MKVExtractTracks(_sourceFile, vt, _selectedAudioTrack, SelectedSubTrack);
                        // extract both video and audio/subs


                        // Audio - AC3 5.1 now supported in PS3 firmware 2.42+
                        if (_selectedAudioTrack.RequiresRecode)
                        {
                            // Convert DTS-MA to DTS core using TsMuxer
                            if (_selectedAudioTrack.CodecID == "A_DTS_MA")
                            {
                                result = TSMuxer_DTSMA_DTS(_selectedAudioTrack);
                                if (!result)
                                    break;
                            }

                            // convert DTS into AC3 5.1
                            result = PS3_DTSAudio_preprocess(_selectedAudioTrack);
                            if (!result)
                                break;
                        }


                        // Now mux video and audio streams into m2ts
                        result = TSMuxer_PS3_preprocess(_currentFileName, vt, _selectedAudioTrack, SelectedSubTrack);
                        if (result)
                            OutputFileName = UpdateFileExtension(OutputFileName, "mpg");
                    }
                    else if (String.Compare(ext, ".mpg", true) == 0 || String.Compare(ext, ".m2ts", true) == 0)
                    {
                        // Experimental
                        if (vt.RequiresRecode)
                            result = MEncoder_PS3_preprocess_video(_sourceFile, vt, null);
                        else
                            result = MEncoder_Copy_video(_sourceFile, vt);
                        if (!result)
                            break;

                        result = FFMpeg_Copy_Audio(_sourceFile, _selectedAudioTrack);
                        if (!result)
                            break;


                        if (_selectedAudioTrack.RequiresRecode)
                        {
                            result = PS3_DTSAudio_preprocess(_selectedAudioTrack);
                            if (!result)
                                break;
                        }

                        // Now mux video and audio streams into m2ts
                        result = TSMuxer_PS3_preprocess(_currentFileName, vt, _selectedAudioTrack, SelectedSubTrack);
                        if (result)
                            OutputFileName = UpdateFileExtension(OutputFileName, "mpg");
                    }
                    else if (String.Compare(ext, ".mov", true) == 0)
                    {
                        result = FFMpeg_Mov_preprocess(_sourceFile, vt);
                    }
                    else
                    {
                        result = MEncoder_PS3Gen_preprocess(_sourceFile, vt);
                    }
                    break;

                case OutputDevice.SamsungS3:
                case OutputDevice.SamsungS4:
                    // no video - means audio convert
                    if (vt != null)
                    {
                        result = FFmpeg_x26x_process(_sourceFile, vt, false);
                    }
                    else
                        result = iPhone_audio_preprocess(_sourceFile, _selectedAudioTrack);

                    break;
                case OutputDevice.SamsungS5:
                    // no video - means audio convert
                    if (vt != null)
                    {
                        result = FFmpeg_x26x_process(_sourceFile, vt, true);
                    }
                    else
                        result = iPhone_audio_preprocess(_sourceFile, _selectedAudioTrack);

                    break;

                case OutputDevice.SamsungUHDTV:
                    // no video - means audio convert
                    if (vt != null)
                    {
                        result = FFmpeg_x26x_process(_sourceFile, vt, false);
                    }
                    break;

                case OutputDevice.iPhone3GS:
                case OutputDevice.iPad:
                    // no video - means audio convert
                    if (vt != null)
                    {
                        // Test ext for a .mkv
                        // Then convert the MKV file container to something suitable
                        if (String.Compare(ext, ".mkv", true) == 0)
                            result = MEncoder_MKV2iPhone_preprocess(_sourceFile, vt, _selectedAudioTrack);
                        else
                            result = MEncoder_iPhone_preprocess(_sourceFile, vt, _selectedAudioTrack, false);
                    }
                    else
                        result = iPhone_audio_preprocess(_sourceFile, _selectedAudioTrack);

                    break;

                case OutputDevice.HTML5:
                    result = HTML5_preprocess(_sourceFile, vt, _selectedAudioTrack);
                    break;

                case OutputDevice.XBox360:
                    if (String.Compare(ext, ".mkv", true) == 0)
                    {
                        result = MEncoder_MKV2Xbox_preprocess(_sourceFile, SelectedVideoTrack, SelectedAudioTrack);
                    }
                    else
                    {
                        result = MEncoder_PS3Gen_preprocess(_sourceFile, vt);
                    }
                    break;

                case OutputDevice.RawFiles:
                    if (String.Compare(ext, ".mkv", true) == 0)
                    {
                        MKVExtractTracks(_sourceFile, vt, _selectedAudioTrack, SelectedSubTrack);
                        // extract both video and audio/subs
                        File.Move(vt.WorkingFile, Path.Combine(OutputFolder, Path.GetFileName(vt.WorkingFile)));
                        File.Move(_selectedAudioTrack.WorkingFile,
                                  Path.Combine(OutputFolder, Path.GetFileName(_selectedAudioTrack.WorkingFile)));
                        result = true;
                    }
                    break;


                default:
                case OutputDevice.Generic:
                    if (String.Compare(ext, ".mkv", true) == 0)
                    {
                        MKVExtractTracks(_sourceFile, vt, _selectedAudioTrack, SelectedSubTrack);
                        // extract both video and audio/subs

                        // Convert DTS-MA to DTS core using TsMuxer
                        if (_selectedAudioTrack.CodecID == "A_DTS_MA")
                        {
                            result = TSMuxer_DTSMA_DTS(_selectedAudioTrack);
                            if (!result)
                                break;
                        }

                        // Now mux video and audio streams into m2ts
                        result = TSMuxer_PS3_preprocess(_currentFileName, vt, _selectedAudioTrack, SelectedSubTrack);
                        if (result)
                            OutputFileName = UpdateFileExtension(OutputFileName, "mpg");
                    }
                    break;

                case OutputDevice.AVCHD:
                    if (String.Compare(ext, ".mkv", true) == 0)
                    {
                        MKVExtractTracks(_sourceFile, vt, _selectedAudioTrack, SelectedSubTrack);
                        // extract both video and audio/subs

                        // Convert DTS-MA to DTS core using TsMuxer
                        if (_selectedAudioTrack.CodecID == "A_DTS_MA")
                        {
                            result = TSMuxer_DTSMA_DTS(_selectedAudioTrack);
                            if (!result)
                                break;
                        }

                        // Now mux video and audio streams into m2ts
                        result = TSMuxer_Disc(_currentFileName, vt, _selectedAudioTrack, SelectedSubTrack);
                    }
                    break;

                case OutputDevice.Bluray:
                    if (String.Compare(ext, ".mkv", true) == 0)
                    {
                        MKVExtractTracks(_sourceFile, vt, _selectedAudioTrack, SelectedSubTrack);
                        // extract both video and audio/subs

                        // Convert DTS-MA to DTS core using TsMuxer
                        if (_selectedAudioTrack.CodecID == "A_DTS_MA")
                        {
                            // Fool TS Muxer into using DTS MA stream, otherwise it fails
                            _selectedAudioTrack.CodecID = "A_DTS";
                        }

                        // Now mux video and audio streams into m2ts
                        result = TSMuxer_Disc(_currentFileName, vt, _selectedAudioTrack, SelectedSubTrack);
                    }
                    break;

                case OutputDevice.WDLiveTV:
                    if (String.Compare(ext, ".mkv", true) == 0)
                    {
                        MKVExtractTracks(_sourceFile, vt, _selectedAudioTrack, SelectedSubTrack);
                        // extract both video and audio/subs

                        if (ConvertTrueHD)
                        {
                        }

                        // merge back just the tracks we want
                        result = MKVMergeTracks(_currentFileName, vt, _selectedAudioTrack, SelectedSubTrack);
                    }
                    else if ((String.Compare(ext, ".flv", true) == 0)
                             && (String.Compare(vt.Format, "VP6", true) == 0))
                    {
                        result = FFMpeg_VP6_process(_sourceFile, vt);
                    }
                    else if ((String.Compare(ext, ".wmv", true) == 0)
                             && (String.Compare(vt.Format, "WMV3", true) != 0))
                    {
                        // convert WMV-1 and WMV-2 to H264
                        result = FFMpeg_WMV12_process(_sourceFile, vt);
                    }
                    else
                    {
                        // just copy source to dest
                        String filename = Path.GetFileName(_sourceFile);
                        String dest = Path.Combine(OutputFolder, filename);
                        File.Copy(_sourceFile, dest, true);
                        result = true;
                    }
                    break;

                case OutputDevice.Sonos:
                    if (String.Compare(ext, ".flac", true) == 0)
                    {
                        result = Sonos_HiRestoMedRes(_sourceFile, _selectedAudioTrack);
                    }
                    else
                    {
                        // just copy source to dest
                        String filename = Path.GetFileName(_sourceFile);
                        String dest = Path.Combine(OutputFolder, filename);
                        File.Copy(_sourceFile, dest, true);
                        result = true;
                    }
                    break;
            }

            if (vt != null)
            {
                outputReport.outputReport = _outputLog.ToString();
            }

            outputReport.outputFileName = OutputFileName;
            // Stop the clock
            sw.Stop();

            // inform user of status
            if (result)
                ProcessStatusChange(100,
                                    "Conversion of " + Path.GetFileName(_sourceFile) + " complete. Time Taken: " +
                                    sw.Elapsed.ToString(), outputReport);
            else
                ProcessStatusChange(100, "Failed conversion of " + Path.GetFileName(_sourceFile) + ".", outputReport);

            Debug.WriteLine("Time Taken: " + sw.Elapsed.ToString());
            return result;
        }


        public bool ConvertDisc()
        {
            var outputReport = new OutputReport();

            // Do a quick check on working folder
            if (String.IsNullOrEmpty(WorkingFolder))
                WorkingFolder = OutputFolder;

            String sourceFolder = _sourceFile;
            ProcessStatusChange(0, "Starting conversion of " + sourceFolder);

            VideoTrack vt = GetVideoTrack();

            if (_selectedAudioTrack == null)
                _selectedAudioTrack = GetAudioTrack();


            bool result = false;

            switch (SelectedDevice.Type)
            {
                case OutputDevice.PS3:
                    result = MEncoder_PS3Gen_preprocess_dvd(sourceFolder, vt);
                    break;

                case OutputDevice.iPhone3GS:
                case OutputDevice.iPad:
                    result = MEncoder_iPhone_preprocess_dvd(sourceFolder, vt);
                    break;
                case OutputDevice.XBox360:
                    result = MEncoder_PS3Gen_preprocess_dvd(sourceFolder, vt);
                    break;
            }

            if (vt != null)
            {
                outputReport.outputReport = _outputLog.ToString();
            }
            outputReport.outputFileName = OutputFileName;


            if (result)
                ProcessStatusChange(100, "Conversion of " + Path.GetFileName(_sourceFile) + " complete.", outputReport);
            else
                ProcessStatusChange(100, "Conversion of " + Path.GetFileName(_sourceFile) + " failed.", outputReport);

            return result;
        }

        /// <summary>
        ///   Parse MKV file
        /// </summary>
        /// <param name="mkvfile"> </param>
        /// <returns> </returns>
        private string GetMKVTrackInfo(string mkvfile)
        {
            ProcessStatusChange(0, "Getting MKV TrackInfo...", null);

            string toolPath = Path.Combine(appPath, @"MKVToolnix\mkvinfo.exe");

            var process = new Process();

            process.StartInfo.FileName = toolPath;
            process.StartInfo.Arguments = " \"" + mkvfile + "\"";
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.CreateNoWindow = true;
            process.StartInfo.RedirectStandardOutput = true;

            process.Start();

            string output = process.StandardOutput.ReadToEnd();
            process.WaitForExit();
            Debug.WriteLine(output);
            return output;
        }

        private string GetMediaInfo(string mediafile)
        {
            ProcessStatusChange(0, "Getting Media Info...", null);
            string toolPath = Path.Combine(appPath, @"MediaInfo\mediainfo.exe");

            var process = new Process();

            process.StartInfo.FileName = toolPath;
            process.StartInfo.Arguments = " \"" + mediafile + "\"";
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.CreateNoWindow = true;
            process.StartInfo.RedirectStandardOutput = true;

            process.Start();

            string output = process.StandardOutput.ReadToEnd();
            process.WaitForExit();
            Debug.WriteLine(output);
            return output;
        }

        private void ParseMKVTrackInfo(string mkvfileinfo)
        {
            // Take the output text and determine which track is the video and 
            // which track is the audio.

            //            string detectString = "| + A track";

            string trackInfo = mkvfileinfo;
            string totalTrackInfo = mkvfileinfo;

            var stringSeparators = new string[] { "| + A track" };
            string[] segments = mkvfileinfo.Split(stringSeparators, StringSplitOptions.None);

            foreach (var segment in segments)
            {
                if (segment.Contains("+ EBML head"))
                    continue; // This is the header - next segment

                string tempTrackInfo = segment;
                string trackType = StringExtractMKV.GetTrackType(tempTrackInfo);
                int trackNumber = StringExtractMKV.GetTrackNumber(tempTrackInfo);

                if (trackType == "video")
                {
                    var vt = new VideoTrack();

                    vt.ID = trackNumber;
                    vt.Type = "Video";
                    vt.CodecID = GetCodecID(tempTrackInfo);
                    vt.Format = vt.CodecID;
                    vt.DefaultTrack = StringExtractMKV.GetTrackIsDefault(tempTrackInfo);
                    vt.Language = StringExtractMKV.GetLanguage(tempTrackInfo);
                    vt.FPS = float.Parse(StringExtractMKV.GetVideoFPS(tempTrackInfo));
                    vt.Width = StringExtractMKV.GetVideoWidth(tempTrackInfo);
                    vt.Height = StringExtractMKV.GetVideoHeight(tempTrackInfo);

                    trackList.Add(vt);

                    if (vt.DefaultTrack)
                        _defaultVideoTrack = _selectedVideoTrack = vt;
                }
                else if (trackType == "audio")
                {
                    var at = new AudioTrack();
                    at.ID = trackNumber;
                    at.Type = "Audio";
                    at.CodecID = GetCodecID(tempTrackInfo);
                    at.Language = StringExtractMKV.GetLanguage(tempTrackInfo);
                    at.DefaultTrack = StringExtractMKV.GetTrackIsDefault(tempTrackInfo);
                    trackList.Add(at);

                    if (at.DefaultTrack)
                        _defaultAudioTrack = _selectedAudioTrack = at;
                }
                else if (trackType == "subtitles")
                {
                    var st = new SubTrack();
                    st.ID = trackNumber;
                    st.Type = "Subtitles";
                    st.Language = StringExtractMKV.GetLanguage(tempTrackInfo);
                    st.CodecID = GetCodecID(tempTrackInfo);
                    trackList.Add(st);

                    if (_defaultSubTrack == null)
                        _defaultSubTrack = _selectedSubTrack = st;
                }
            }
        }

        private void ParseMediaInfoMKV(string mediainfo)
        {
            // Take the output text and determine which track is the video and 
            // which track is the audio.

            string trackInfo = mediainfo;

            var stringSeparators = new string[] { "\r\n\r\n" };
            string[] segments = trackInfo.Split(stringSeparators, StringSplitOptions.RemoveEmptyEntries);

            foreach (var segment in segments)
            {
                if (segment.Contains("General\r"))
                {
                    Title = StringExtract.GetMovieName(segment);


                    continue; // This is the header - next segment
                }


                if (segment.Contains("Video\r"))
                {
                    //"Display aspect ratio" 
                    VideoTrack vt = GetVideoTrack();
                    try
                    {
                        vt.BitRate = StringExtract.GetBitRate(segment);
                        if (vt.BitRate == 0)
                            vt.BitRate = StringExtract.GetNominalBitRate(segment);
                        vt.ActualRefFrames = StringExtract.GetReferenceFrames(segment);
                        vt.EncodingSettings = StringExtract.GetEncodingSettings(segment);
                        vt.Title = StringExtract.GetTitle(segment);

                        if (!String.IsNullOrEmpty(vt.EncodingSettings))
                        {
                            String[] list = vt.EncodingSettings.Split(new char[] { '/' });
                            foreach (var paramPair in list)
                            {
                                String[] param = paramPair.Split(new char[] { '=' });
                                if (param[0].Trim().ToLower() == "b_pyramid")
                                    vt.BPyramid = Convert.ToInt32(param[1]);
                            }
                        }
                    }
                    finally
                    {
                    }
                }
                else if (segment.Contains("Audio\r") || segment.Contains("Audio #"))
                {
                    // get Audio Track number
                    int audio = StringExtractMKV.GetMIAudioTrack(segment);

                    List<AudioTrack> audioTracks = GetAudioTracks();
                    AudioTrack audioTrack = audioTracks[audio - 1];
                    audioTrack.AID = audio - 1;
                    audioTrack.Channels = StringExtract.GetChannels(segment);
                    audioTrack.BitRate = StringExtract.GetBitRate(segment);
                    if (audioTrack.BitRate == 0)
                        audioTrack.BitRate = StringExtract.GetNominalBitRate(segment);
                    if (audioTrack.BitRate == 0)
                        audioTrack.BitRate = StringExtract.GetMaximumBitRate(segment);
                    audioTrack.FmtProfile = StringExtract.GetFormatProfile(segment);
                    if (audioTrack.FmtProfile == "MA" || audioTrack.FmtProfile == "MA / Core")
                        audioTrack.CodecID = "A_DTS_MA";

                    audioTrack.Delay = StringExtract.GetVideoDelay(segment);

                    audioTrack.Title = StringExtract.GetTitle(segment);
                }
            }
        }

        private void ParseMediaInfo(string mediainfo)
        {
            // Take the output text and determine which track is the video and 
            // which track is the audio.
            int trackNumber = 1;
            string trackInfo = mediainfo;

            var stringSeparators = new string[] { "\r\n\r\n" };
            string[] segments = trackInfo.Split(stringSeparators, StringSplitOptions.RemoveEmptyEntries);

            foreach (var segment in segments)
            {
                if (segment.Contains("General"))
                {
                    // check its not a DVD Menu

                    continue; // This is the header - next segment
                }

                if (segment.Contains("Video\r"))
                {
                    //"Display aspect ratio" 
                    var vt = new VideoTrack();

                    if ((vt.ID = StringExtract.GetID("Video", segment)) == -1)
                        vt.ID = trackNumber++;
                    else
                        trackNumber = vt.ID;

                    vt.Type = "Video";

                    // Set first video track to default
                    if (_defaultVideoTrack == null)
                    {
                        _defaultVideoTrack = _selectedVideoTrack = vt;
                        vt.DefaultTrack = true;
                    }


                    vt.Format = StringExtract.GetFormat(segment);
                    vt.FPS = float.Parse(StringExtract.GetVideoFPS(segment));
                    vt.Width = StringExtract.GetVideoWidth(segment);
                    vt.Height = StringExtract.GetVideoHeight(segment);
                    // Seems Canon 60D has 1088 lines
                    vt.OriginalHeight = StringExtract.GetVideoOriginalHeight(segment);
                    if (vt.OriginalHeight == 0)
                        vt.OriginalHeight = vt.Height;

                    if (vt.CodecID == null)
                    {
                        switch (vt.Format)
                        {
                            case "AVC":
                                vt.CodecID = "V_MPEG4/ISO/AVC";
                                break;
                        }
                    }

                    try
                    {
                        vt.BitRate = StringExtract.GetBitRate(segment);
                        vt.ActualRefFrames = StringExtract.GetReferenceFrames(segment);
                    }
                    finally
                    {
                    }
                    trackList.Add(vt);

                    //vt.VID = StringExtract.GetID(segment);
                }
                else if (segment.Contains("Audio") || segment.Contains("Audio #"))
                {
                    // get Audio Track number
                    var at = new AudioTrack();
                    if ((at.ID = StringExtract.GetMIAudioTrack(segment)) == -1)
                        at.ID = trackNumber++;
                    else
                        trackNumber = at.ID;

                    at.Type = "Audio";
                    at.Format = StringExtract.GetFormat(segment);
                    if (at.CodecID == null)
                    {
                        switch (at.Format)
                        {
                            case "DTS":
                                at.CodecID = "A_DTS";
                                break;
                            case "AAC":
                                at.CodecID = "A_AAC";
                                break;
                            case "AC-3":
                                at.CodecID = "A_AC3";
                                break;
                            case "WMA":
                                at.CodecID = "A_WMA";
                                break;
                            case "PCM":
                                at.CodecID = "A_PCM";
                                break;
                            case "FLAC":
                                at.CodecID = "A_FLAC";
                                break;
                            default:
                                at.CodecID = at.Format;
                                break;
                        }
                    }
                    at.Language = StringExtract.GetLanguage(segment);
                    at.BitRate = StringExtract.GetBitRate(segment);
                    at.SampleRate = StringExtract.GetSampleRate(segment);

                    at.Channels = StringExtract.GetChannels(segment);

                    at.AID = 0;
                    trackList.Add(at);

                    if (_defaultAudioTrack == null)
                    {
                        _defaultAudioTrack = _selectedAudioTrack = at;
                        at.DefaultTrack = true;
                    }
                }
            }
        }


        private string GetCodecID(string s)
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

        private string ExtractCodecExt(string s)
        {
            if (s.Contains("A_AC3"))
                return ".ac3";
            else if (s.Contains("A_AAC"))
                return ".aac";
            else if (s.Contains("A_DTS_MA"))
                return ".dtshd";
            else if (s.Contains("A_DTS"))
                return ".dts";
            else if (s.Contains("A_TRUEHD"))
                return ".thd";
            else if (s.Contains("A_MP3"))
                return ".mp3";
            else if (s.Contains("A_VORBIS"))
                return ".ogg";
            else if (s.Contains("A_WMA"))
                return ".wma";
            else if (s.Contains("A_PCM"))
                return ".pcm";
            else if (s.Contains("A_FLAC"))
                return ".flac";
            else if (s.Contains("V_MS/VFW/FOURCC"))
                return ".avi";
            else if (s.Contains("V_MS/VFW/WVC1"))
                return ".vc1";
            else if (s.Contains("V_MPEG4/ISO/AVC"))
                return ".h264";
            else if (s.Contains("V_MPEGH/ISO/HEVC"))
                return ".h265";
            else if (s.Contains("V_MPEG2"))
                return ".mpg";
            else if (s.Contains("V_"))
                return ".avi"; // default video
            else if (s.Contains("S_TEXT/UTF8"))
                return ".srt";
            else if (s.Contains("S_TEXT/ASS"))
                return ".ssa";
            else if (s.Contains("S_HDMV/PGS"))
                return ".pgs";
            else
            {
                Debug.WriteLine("Error_UnknownCodec:" + s);
                return "";
            }
        }

        /// <summary>
        ///   This should be the choosen one
        /// </summary>
        /// <returns> </returns>
        public VideoTrack GetVideoTrack()
        {
            foreach (var track in trackList)
            {
                if (track.Type == "Video")
                {
                    return (track as VideoTrack);
                }
            }
            return null;
        }

        public List<VideoTrack> GetVideoTracks()
        {
            var videoList = new List<VideoTrack>();
            foreach (var track in trackList)
            {
                if (track.Type == "Video")
                {
                    videoList.Add(track as VideoTrack);
                }
            }
            return videoList;
        }


        public AudioTrack GetAudioTrack()
        {
            foreach (var track in trackList)
            {
                if (track.Type == "Audio")
                {
                    return (track as AudioTrack);
                }
            }
            return null;
        }

        public List<AudioTrack> GetAudioTracks()
        {
            var audioList = new List<AudioTrack>();
            foreach (var track in trackList)
            {
                if (track.Type == "Audio")
                {
                    audioList.Add(track as AudioTrack);
                }
            }
            return audioList;
        }

        public List<SubTrack> GetSubtitleTracks()
        {
            var subList = new List<SubTrack>();
            foreach (var track in trackList)
            {
                if (track.Type == "Subtitles")
                {
                    subList.Add(track as SubTrack);
                }
            }
            return subList;
        }


        private bool MKVExtractTracks(string mkvfile, VideoTrack vt, AudioTrack at, SubTrack st)
        {
            string videoArg = String.Empty;
            string audioArg = String.Empty;
            string subArg = String.Empty;

            ProcessStatusChange(0, "Extracting tracks from Mkv file");

            string toolPath = Path.Combine(appPath, @"MKVToolnix\mkvextract.exe");

            if (vt != null)
            {
                string videofile = Path.Combine(WorkingFolder, "video");
                videoArg = String.Format("{0}:\"{2}{0}{1}\"", vt.ID - 1, ExtractCodecExt(vt.CodecID), videofile);
                vt.WorkingFile = String.Format("{0}{1}{2}", videofile, vt.ID - 1, ExtractCodecExt(vt.CodecID));
            }

            if (at != null)
            {
                string audiofile = Path.Combine(WorkingFolder, "audio");
                audioArg = String.Format("{0}:\"{2}{0}{1}\"", at.ID - 1, ExtractCodecExt(at.CodecID), audiofile);
                at.WorkingFile = String.Format("{0}{1}{2}", audiofile, at.ID - 1, ExtractCodecExt(at.CodecID));
            }

            if (st != null)
            {
                string subfile = Path.Combine(WorkingFolder, "subtitle");
                subArg = String.Format("{0}:\"{2}{0}{1}\"", st.ID - 1, ExtractCodecExt(st.CodecID), subfile);
                st.WorkingFile = String.Format("{0}{1}{2}", subfile, st.ID - 1, ExtractCodecExt(st.CodecID));
            }

            // use no-ogg for FLAC support
            string metaData = " tracks \"" + mkvfile + "\" " + videoArg + " " + audioArg + "  " + subArg;
            _process = new Process();

            _process.StartInfo.FileName = toolPath;
            _process.StartInfo.Arguments = metaData;
            _process.StartInfo.UseShellExecute = false;
            _process.StartInfo.CreateNoWindow = true;
            _process.StartInfo.RedirectStandardOutput = true;
            Debug.WriteLine(_process.StartInfo.Arguments);

            _process.Start();

            CaptureToolsOutputStdOut();

            _process.WaitForExit();
            if (_process.ExitCode == 0)
            {
                return true;
            }
            else
                return false;
        }


        private bool MEncoder_Copy_video(string sourceFile, VideoTrack vt)
        {
            string videofile = Path.Combine(WorkingFolder, "video");
            string videoArg = String.Format("{0}{1}", videofile, ExtractCodecExt(vt.CodecID));

            vt.WorkingFile = videoArg;
            var md = new StringBuilder();
            md.AppendFormat(" -o \"{0}\" ", vt.WorkingFile); // Output details
            md.Append(" -nosound "); // Output No Sound
            md.Append(" -of rawvideo ");
            md.Append(" -ovc copy ");
            md.AppendFormat(" \"{0}\" ", sourceFile);

            string outputLog;
            bool result = MEncoder(md.ToString(), out outputLog);
            _outputLog.Append(outputLog);
            return result;
        }

        private bool MEncoder_Copy_audio(string sourceFile, AudioTrack at)
        {
            string audiofile = Path.Combine(WorkingFolder, "audio");
            string audioArg = String.Format("{0}{1}", audiofile, ExtractCodecExt(at.CodecID));

            at.WorkingFile = audioArg;
            var md = new StringBuilder();
            md.AppendFormat(" -o \"{0}\" ", at.WorkingFile); // Output details
            md.Append(" -novideo "); // Output No Video
            md.Append(" -of rawaudio ");
            md.Append(" -oac copy ");
            md.AppendFormat(" \"{0}\" ", sourceFile);

            string outputLog;
            bool result = MEncoder(md.ToString(), out outputLog);
            _outputLog.Append(outputLog);
            return result;
        }

        private bool MEncoder_PS3_preprocess_video(string sourceFile, VideoTrack vt, SubTrack st)
        {
            int bitRate = 0;
            vt.CodecID = "V_MPEG4/ISO/AVC";

            string videofile = Path.Combine(WorkingFolder, "video");
            string videoArg = String.Format("{0}{1}", videofile, ExtractCodecExt(vt.CodecID));

            vt.WorkingFile = videoArg;

            if (vt.BitRate > 0)
                bitRate = vt.BitRate;
            else
                bitRate = 10000; // 8000

            var md = new StringBuilder();
            md.AppendFormat(" -o \"{0}\" ", vt.WorkingFile); // Output details
            md.Append(" -nosound "); // Output No Sound

            md.AppendFormat(" -ovc x264 -x264encopts bitrate={0}:frameref={1}:bframes=3:b_pyramid:threads=auto", bitRate,
                            vt.MaxRefFrames); // bframes 5
            md.Append(" -lavdopts threads=2 -noskip -mc 0 ");
            md.Append(" -ofps 24000/1001 -fps 24000/1001 "); // Output Frame rate                
            md.AppendFormat(" \"{0}\" ", sourceFile);

            // If we are passed a subtitle track then get mencoder to render it.
            if (st != null)
            {
                md.AppendFormat(
                    " -sub \"{0}\" -subfont \"C:\\Windows\\Fonts\\Arial.ttf\" -subfont-text-scale 3 -subfont-blur 1 -subfont-outline 1 -subpos 99",
                    st.WorkingFile);
            }


            string outputLog;
            bool result = MEncoder(md.ToString(), out outputLog);
            _outputLog.Append(outputLog);
            return result;
        }

        private bool MEncoder2_PS3_preprocess(string sourceFile, VideoTrack vt)
        {
            int bitRate = 0;

            vt.CodecID = "V_MPEG4/ISO/AVC";

            string videofile = Path.Combine(WorkingFolder, "video");
            string videoArg = String.Format("{0}{1}", videofile, ExtractCodecExt(vt.CodecID));

            vt.WorkingFile = videoArg;

            if (vt.BitRate > 0 && vt.BitRate < 10000)
                bitRate = vt.BitRate;
            else
                bitRate = 8000;

            var md = new StringBuilder();
            md.AppendFormat(" -o \"{0}\" ", vt.WorkingFile); // Output details
            md.Append(" -vf harddup ");
            md.Append(" -nosound "); // Output No Sound
            md.AppendFormat(
                " -ovc x264 -x264encopts bitrate={0}:frameref={1}:bframes=4:b_pyramid=none:threads=auto -lavdopts threads=2 -noskip -mc 0 ",
                bitRate, 4 /*vt.MaxRefFrames*/);
            // :bframes=6:b_adapt:b_pyramid=none:weight_b:partitions=all:8x8dct:me=umh:subq=7:trellis=2:
            md.Append(" -ofps 24000/1001 -fps 24000/1001 "); // Output Frame rate                
            md.AppendFormat(" \"{0}\" ", sourceFile);

            string outputLog;
            bool result = MEncoder2(md.ToString(), out outputLog);
            _outputLog.Append(outputLog);
            return result;
        }

        private bool MEncoder_PS3Gen_preprocess(string sourceFile, VideoTrack vt)
        {
            int bitRate = 640;

            string videofile = Path.Combine(OutputFolder, Path.GetFileNameWithoutExtension(sourceFile));
            OutputFileName = String.Format("{0}.mp4", videofile);

            // Work out max bitrate
            if (vt != null)
            {
                if (vt.BitRate < 640)
                    bitRate = 640;
                else
                    bitRate = vt.BitRate;
            }

            // Build Parameters block
            var md = new StringBuilder();
            md.AppendFormat(" -o \"{0}\" ", OutputFileName); // Output details
            md.Append(" -vf harddup ");
            md.Append(" -of lavf -lavfopts format=mp4 ");
            md.Append(" -oac faac -faacopts mpeg=4:object=2:raw:br=160 ");
            md.AppendFormat(
                " -ovc x264 -sws 9 -x264encopts nocabac:level_idc=41:bframes=0:global_header:threads=auto:subq=5:frameref=4:partitions=all:trellis=1:chroma_me:me=umh:bitrate={0} ",
                bitRate);
            md.AppendFormat(" -ofps {0} ", vt.FPS);
            md.AppendFormat(" \"{0}\" ", sourceFile);


            string outputLog;
            bool result = MEncoder(md.ToString(), out outputLog);
            _outputLog.Append(outputLog);
            return result;
        }

        private bool MEncoder_PS3Gen_preprocess_dvd(string sourcePath, VideoTrack vt)
        {
            int bitRate = 0;

            string titlename = sourcePath;
            if (titlename.LastIndexOf("\\") != -1)
                titlename = titlename.Substring(titlename.LastIndexOf("\\") + 1);

            if (String.IsNullOrEmpty(titlename))
            {
                titlename = "unknown";
            }

            string videofile = Path.Combine(OutputFolder, titlename);
            OutputFileName = String.Format("{0}.mp4", videofile);

            string mencoderSource = String.Format("dvd://{0} -dvd-device \"{1}\" ", 1, sourcePath);

            // Work out max bitrate
            if (vt.BitRate < 640)
                bitRate = 6000;
            else
                bitRate = vt.BitRate;


            var md = new StringBuilder();
            md.AppendFormat(" -o \"{0}\" ", OutputFileName); // Output details
            md.Append(" -of lavf -lavfopts format=mp4 "); // Output container format
            md.Append(" -oac faac -faacopts mpeg=4:object=2:raw:br=320 -channels 2 -srate 48000 "); // Output sound 
            //md.Append(" -oac lavc -lavcopts acodec=ac3 -channels 6 ");  // Output sound 
            md.AppendFormat(
                " -ovc x264 -sws 9 -x264encopts nocabac:level_idc=30:bframes=0:global_header:threads=auto:subq=5:frameref=6:partitions=all:trellis=1:chroma_me:me=umh:bitrate={0} -mc 0 -noskip ",
                bitRate);
            md.Append(mencoderSource);

            string outputLog;
            bool result = MEncoder(md.ToString(), out outputLog);
            _outputLog.Append(outputLog);
            return result;
        }

        private bool MEncoder_iPhone_preprocess(string sourceFile, VideoTrack vt, AudioTrack at, bool omitFPS)
        {
            int bitRate = 0;

            string videofile = Path.Combine(OutputFolder, Path.GetFileNameWithoutExtension(sourceFile));
            OutputFileName = String.Format("{0}.mp4", videofile);

            // Work out max bitrate
            if (vt.BitRate > 2000)
                bitRate = 2000;
            else if (vt.BitRate < 640)
                bitRate = 640;
            else
                bitRate = vt.BitRate;

            var md = new StringBuilder();
            md.AppendFormat(" -o \"{0}\" ", OutputFileName); // Output details
            if (vt.Width > SelectedDevice.VideoMaxWidth)
                md.AppendFormat(" -vf scale={0}:-10,harddup ", SelectedDevice.VideoMaxWidth);
            else
                md.Append(" -vf harddup ");
            md.Append(" -of lavf -lavfopts format=mp4 "); // Output container format
            md.Append(" -oac faac -faacopts br=128:mpeg=4:raw:object=2 -channels 2 -srate 48000 "); // Output sound 
            md.AppendFormat(
                " -ovc x264 -sws 9 -x264encopts nocabac:level_idc=30:bframes=0:global_header:threads=auto:subq=5:frameref=6:partitions=all:trellis=1:chroma_me:me=umh:bitrate={0} -mc 0 -noskip ",
                bitRate);
            md.AppendFormat(" -ofps {0} ", vt.FPS);
            md.AppendFormat(" \"{0}\" ", sourceFile);


            string outputLog;
            bool result = MEncoder(md.ToString(), out outputLog);
            _outputLog.Append(outputLog);
            return result;
        }

        private bool MEncoder_iPhone_preprocess_dvd(string sourcePath, VideoTrack vt)
        {
            int bitRate = 0;

            string titlename = sourcePath;
            if (titlename.LastIndexOf("\\") != -1)
                titlename = titlename.Substring(titlename.LastIndexOf("\\") + 1);

            string videofile = Path.Combine(OutputFolder, titlename);
            OutputFileName = String.Format("{0}.mp4", videofile);

            string mencoderSource = String.Format("dvd://{0} -dvd-device \"{1}\" ", 1, sourcePath);

            // Work out max bitrate
            //if (vt.BitRate > 2000)
            //    bitRate = 2000;
            //else if (vt.BitRate < 500)
            bitRate = 640;
            //else
            //    bitRate = vt.BitRate;

            // debug -endpos 0:2:0
            string metaData =
                String.Format(
                    " -o \"{0}\" -vf scale=640:-10,harddup  -of lavf -lavfopts format=mp4  -oac faac -faacopts mpeg=4:object=2:raw:br=160  -ovc x264 -sws 9 -x264encopts nocabac:level_idc=30:bframes=0:global_header:threads=auto:subq=5:frameref=6:partitions=all:trellis=1:chroma_me:me=umh:bitrate={1} -ofps {2} {3} ",
                    OutputFileName, bitRate, vt.FPS, mencoderSource);

            string outputLog;
            bool result = MEncoder(metaData, out outputLog);
            _outputLog.Append(outputLog);
            return result;
        }

        private bool MEncoder_MKV2iPhone_preprocess(string sourceFile, VideoTrack vt, AudioTrack at)
        {
            int bitRate = 0;

            string videofile = Path.Combine(OutputFolder, Path.GetFileNameWithoutExtension(sourceFile));
            OutputFileName = String.Format("{0}.mp4", videofile);

            // Work out max bitrate
            if (vt.BitRate > 2000)
                bitRate = 2000;
            else if (vt.BitRate < 640)
                bitRate = 640;
            else
                bitRate = vt.BitRate;

            var md = new StringBuilder();
            md.AppendFormat(" -o \"{0}\" ", OutputFileName); // Output details
            md.AppendFormat(" -aid {0} ", at.AID);
            if (vt.Width > SelectedDevice.VideoMaxWidth)
                md.AppendFormat(" -vf scale={0}:-10,harddup ", SelectedDevice.VideoMaxWidth);
            else
                md.Append(" -vf harddup ");
            md.Append(" -of lavf -lavfopts format=mp4 "); // Output container format
            md.Append(" -oac faac -faacopts mpeg=4:object=2:raw:br=160 -channels 2 -srate 48000 "); // Output sound 
            md.AppendFormat(
                " -ovc x264 -sws 9 -x264encopts nocabac:level_idc=30:bframes=0:global_header:threads=auto:subq=5:frameref=6:partitions=all:trellis=1:chroma_me:me=umh:bitrate={0} -mc 0 -noskip ",
                bitRate);
            md.AppendFormat(" \"{0}\" ", sourceFile);

            string outputLog;
            bool result = MEncoder(md.ToString(), out outputLog);
            _outputLog.Append(outputLog);
            return result;
        }

        private bool MEncoder_MKV2Xbox_preprocess(string sourceFile, VideoTrack vt, AudioTrack at)
        {
            string videofile = Path.Combine(OutputFolder, Path.GetFileNameWithoutExtension(sourceFile));
            OutputFileName = String.Format("{0}.mp4", videofile);
            int bitRate = 0;
            if (vt.BitRate > 0)
                bitRate = vt.BitRate;
            else
                bitRate = 8000;

            var md = new StringBuilder();
            md.AppendFormat(" -o \"{0}\" ", OutputFileName); // Output details
            md.AppendFormat(" -aid {0} ", at.AID);
            md.Append(" -vf harddup ");
            md.Append(" -of lavf -lavfopts format=mp4 "); // Output container format
            md.Append(" -oac faac -faacopts mpeg=4:object=2:raw:br=320 -channels 2 -srate 48000 "); // Output sound 
            if (vt.RequiresRecode)
            {
                md.AppendFormat(
                    " -ovc x264 -sws 9 -x264encopts nocabac:level_idc=41:bframes=0:global_header:threads=auto:subq=5:frameref={0}:partitions=all:trellis=1:chroma_me:me=umh",
                    vt.MaxRefFrames);
                md.AppendFormat(":bitrate={0} ", bitRate);
                md.Append(" -mc 0 -noskip ");
            }
            else
            {
                md.Append(" -ovc copy ");
            }
            md.AppendFormat(" \"{0}\" ", sourceFile);

            string outputLog;
            bool result = MEncoder(md.ToString(), out outputLog);
            _outputLog.Append(outputLog);
            return result;
        }


        /// <summary>
        ///   Call eac3to
        /// </summary>
        /// <param name="at"> </param>
        /// <returns> </returns>
        private bool PS3_DTSAudio_preprocess(AudioTrack at)
        {
            const int BitRate = 640;
            //            string audiofile = Path.Combine(WorkingFolder, "audio");
            //            string audioArg = String.Format("{0}{1}", audiofile, ExtractCodecExt(at.CodecID));
            string outputAudiofile = Path.Combine(WorkingFolder, "audio.ac3");

            var sb = new StringBuilder();
            sb.AppendFormat(" \"{0}\" ", at.WorkingFile); // input 
            sb.AppendFormat(" \"{0}\" ", outputAudiofile); // output
            sb.AppendFormat(" -{0} ", BitRate); // bitrate
            sb.Append(" -resampleTo48000 "); // force to 48kHz 
            if (at.Channels > 6)
                sb.Append(" -down6 "); // force to 6 channels max
            //sb.Append(" -down6 -mixlfe -phaseShift ");          // downmix in case we want stereo
            sb.Append("  -progressnumbers "); // allow tool to give us % progress

            ProcessStatusChange(0, "Recode audio track...", null);
            bool result = Eac3to(sb.ToString());
            if (result)
                File.Delete(at.WorkingFile);

            // set converted details
            at.WorkingFile = outputAudiofile;
            at.BitRate = BitRate;
            at.CodecID = "A_AC3";

            // remove eac3to log file
            File.Delete(Path.Combine(WorkingFolder, "audio - Log.txt"));

            return result;
        }


        private bool FFMpeg_Copy_Audio(string sourceFile, AudioTrack at)
        {
            string audiofile = Path.Combine(WorkingFolder, "audio");
            string audioArg = String.Format("{0}{1}", audiofile, ExtractCodecExt(at.CodecID));
            at.WorkingFile = audioArg;

            string metaData = String.Format(" -i \"{0}\" -vn -acodec copy -y \"{1}\" ", sourceFile, at.WorkingFile);

            ProcessStatusChange(0, "Demux audio track...", null);
            string outputLog;
            bool result = FFMpeg(metaData, out outputLog);
            _outputLog.Append(outputLog);
            return result;
        }

        private bool FFMpeg_Mov_preprocess(string sourceFile, VideoTrack vt)
        {
            string videofile = Path.GetFileNameWithoutExtension(sourceFile);
            string videofilePath = Path.Combine(OutputFolder, videofile);
            string outputfile = String.Format("{0}.mpg", videofilePath);

            string preset = Path.Combine(appPath, @"ffmpeg\presets");
            var sb = new StringBuilder();
            sb.AppendFormat(" -i \"{0}\" ", sourceFile);
            sb.Append(" -f mp4 "); // output container
            sb.Append(" -vcodec libx264 -vprofile baseline  ");
            if (vt.OriginalHeight > 1080)
                sb.Append(" -s 1920x1080 ");

            sb.Append(" -acodec libvo_aacenc -ac 2 -ab 320000 "); // codec aac use 2 channels 320kbits max
            //sb.Append(" -acodec copy "); 
            sb.AppendFormat(" -y  \"{0}\" ", outputfile); // -y overwrite 

            ProcessStatusChange(0, "Recode video track...", null);
            string outputLog;
            bool result = FFMpeg(sb.ToString(), out outputLog);
            if (result)
            {
                OutputFileName = outputfile;
            }
            _outputLog.Append(outputLog);

            return result;
        }

        private bool iPhone_audio_preprocess(string sourceFile, AudioTrack at)
        {
            string videofile = Path.Combine(OutputFolder, Path.GetFileNameWithoutExtension(sourceFile));
            string outputAudiofile = String.Format("{0}.m4a", videofile);

            var sb = new StringBuilder();
            sb.AppendFormat(" -i \"{0}\" ", sourceFile);
            if (RingTone)
                sb.Append(" -t 30  "); // duration
            sb.Append(" -f mp4 "); // output container
            sb.Append(" -acodec libvo_aacenc -ac 2 -ab 320000 "); // codec aac use 2 channels 320kbits max
            sb.AppendFormat(" -y  \"{0}\" ", outputAudiofile); // -y overwrite 

            ProcessStatusChange(0, "Recode audio track...", null);
            string outputLog;
            bool result = FFMpeg(sb.ToString(), out outputLog);
            if (result)
            {
                // if we are a ringtone
                if (RingTone)
                    OutputFileName = UpdateFileExtension(outputAudiofile, ".m4r");
                else
                    OutputFileName = outputAudiofile;
            }
            _outputLog.Append(outputLog);
            return result;
        }

        private bool HTML5_preprocess(string sourceFile, VideoTrack vt, AudioTrack selectedAudioTrack)
        {
            string videofile = Path.GetFileNameWithoutExtension(sourceFile);
            string videofilePath = Path.Combine(OutputFolder, videofile);
            string outputfile = String.Format("{0}.mp4", videofilePath);

            string preset = Path.Combine(appPath, @"ffmpeg\presets");
            var sb = new StringBuilder();
            sb.AppendFormat(" -i \"{0}\" ", sourceFile);
            sb.Append(" -f mp4 "); // output container
            sb.Append(" -vcodec libx264 -vprofile high  ");
            if (vt.Height % 2 == 1)
            {
                sb.AppendFormat(" -s {0}x{1} ", vt.Width, vt.Height + 1);
            }
            sb.Append(" -threads 0 ");

            sb.Append(" -acodec libvo_aacenc -ac 2 -ab 320k "); // codec aac use 2 channels 320kbits max

            string ffmpegTemp = Path.Combine(WorkingFolder, Guid.NewGuid().ToString());
            sb.AppendFormat(" -passlogfile \"{0}\" ", ffmpegTemp);

            sb.AppendFormat(" -y  \"{0}\" ", outputfile); // -y overwrite 

            ProcessStatusChange(0, "Recode video track...", null);
            string outputLog;
            bool result = FFMpeg(sb.ToString(), out outputLog);
            if (result)
            {
                OutputFileName = outputfile;
            }
            _outputLog.Append(outputLog);

            var sb2 = new StringBuilder();
            sb2.AppendFormat(" -i \"{0}\" ", sourceFile);
            sb2.Append(" -threads 0 ");
            //if (vt.BitRate != -1)
            //    sb.AppendFormat(" -b:v {0}k ", vt.BitRate); 
            sb2.Append(" -vcodec libvpx ");
            sb2.Append(" -fpre \"" + preset + "\\libvpx-720p.ffpreset\" ");
            if (vt.BitRate != -1)
                sb2.AppendFormat(" -b:a {0}k ", 480 /*selectedAudioTrack.BitRate*/);

            string ffmpegTemp2 = Path.Combine(WorkingFolder, Guid.NewGuid().ToString());
            sb2.AppendFormat(" -passlogfile \"{0}\" ", ffmpegTemp2);

            sb2.AppendFormat(" -y  \"{0}\" ", String.Format("{0}.webm", videofilePath)); // -y overwrite 
            ProcessStatusChange(0, "Recode video track...", null);
            result = FFMpeg(sb2.ToString(), out outputLog);
            _outputLog.Append(outputLog);

            var sb3 = new StringBuilder();
            sb3.AppendFormat(" -i \"{0}\" ", sourceFile);
            sb3.Append(" -threads 0 ");
            if (vt.BitRate != -1)
                sb3.AppendFormat(" -b:v {0}k ", vt.BitRate);
            string ffmpegTemp3 = Path.Combine(WorkingFolder, Guid.NewGuid().ToString());
            sb3.AppendFormat(" -passlogfile \"{0}\" ", ffmpegTemp3);

            sb3.AppendFormat(" -y  \"{0}\" ", String.Format("{0}.ogv", videofilePath)); // -y overwrite 
            ProcessStatusChange(0, "Recode video track...", null);
            result = FFMpeg(sb3.ToString(), out outputLog);
            _outputLog.Append(outputLog);

            using (TextWriter tw = new StreamWriter(String.Format("{0}.html", videofilePath)))
            {
                tw.WriteLine("<!doctype html>");
                tw.WriteLine("<html>");
                tw.WriteLine("	<header> ");
                tw.WriteLine("	 <title>Video Demo</title>");
                tw.WriteLine("	</header> ");
                tw.WriteLine("	<body> ");
                tw.WriteLine("	 <video controls=\"true\" width=\"640\" height=\"400\" autoplay=\"autoplay\">  ");
                tw.WriteLine("      <source src=\"" + videofile + ".mp4\" type=\"video/mp4\"/>");
                tw.WriteLine("      <source src=\"" + videofile + ".webm\" type=\"video/webm\"/>");
                tw.WriteLine("      <source src=\"" + videofile + ".ogv\" type=\"video/ogg\"/>");
                tw.WriteLine("          Your browser does not support the video tag.");
                tw.WriteLine("	 </video>  ");
                tw.WriteLine("	</body> ");
                tw.WriteLine("</html> ");
            }

            return result;
        }


        private bool TSMuxer_PS3_preprocess(string outputFile, VideoTrack vt, AudioTrack at, SubTrack st)
        {
            string outPath = Path.Combine(OutputFolder, outputFile);
            string tempFile = outPath + ".m2ts";

            // Generate meta file that contains all the parameters for TSMuxeR 
            string tempMetafile = Path.Combine(WorkingFolder, outputFile);
            string metaFileName = Path.ChangeExtension(tempMetafile, "meta");

            string metaData = "MUXOPT --no-pcr-on-video-pid --new-audio-pes --vbr  --vbv-len=500 ";
            metaData += Environment.NewLine;
            metaData += String.Format("{0}, \"{1}\", level=4.1, insertSEI, contSPS " + Environment.NewLine, vt.CodecID,
                                      vt.WorkingFile);
            metaData += String.Format("{0}, \"{1}\", lang=eng " + Environment.NewLine, at.CodecID, at.WorkingFile);

            // create the command file
            using (var sw = new StreamWriter(metaFileName))
            {
                sw.WriteLine(metaData);
                sw.Close();
            }
            string args = " \"" + metaFileName + "\" " + " \"" + tempFile + "\" ";

            string outputLog;
            ProcessStatusChange(0, "Muxing tracks...", null);
            bool result = TSMuxer(args, out outputLog);
            OutputFileName = tempFile;

            // Remove our temporary files
            if (vt != null)
                File.Delete(vt.WorkingFile);
            if (at != null)
                File.Delete(at.WorkingFile);
            if (st != null)
                File.Delete(st.WorkingFile);
            File.Delete(metaFileName);

            _outputLog.Append(outputLog);

            return result;
        }


        private bool FFMpeg_VP6_process(string sourceFile, VideoTrack vt)
        {
            string videofile = Path.GetFileNameWithoutExtension(sourceFile);
            string videofilePath = Path.Combine(OutputFolder, videofile);
            string outputfile = String.Format("{0}.flv", videofilePath);

            string preset = Path.Combine(appPath, @"ffmpeg\presets");
            var sb = new StringBuilder();
            sb.AppendFormat(" -i \"{0}\" ", sourceFile);
            sb.Append(" -pass 1 ");
            sb.Append(" -vcodec libx264 -vprofile high ");
            sb.AppendFormat(" -r {0} ", vt.FPS);
            if (vt.BitRate != -1)
                sb.AppendFormat(" -b:v {0}k ", vt.BitRate);

            // if height isn't division by 2 make it so
            // otherwise FFMpeg reports  'height not divisible by 2....'
            if (vt.Height % 2 == 1)
            {
                sb.AppendFormat(" -s {0}x{1} ", vt.Width, vt.Height + 1);
            }

            sb.Append(" -threads 0 ");

            sb.Append(" -acodec copy ");

            string ffmpegTemp = Path.Combine(WorkingFolder, Guid.NewGuid().ToString());
            sb.AppendFormat(" -passlogfile \"{0}\" ", ffmpegTemp);

            sb.AppendFormat(" -y  \"{0}\" ", outputfile); // -y overwrite 

            ProcessStatusChange(0, "Recode video track...(pass 1)", null);
            string outputLog;
            bool result = FFMpeg(sb.ToString(), out outputLog);
            if (result)
            {
                ProcessStatusChange(0, "Recode video track...(pass 2)", null);
                result = FFMpeg(sb.ToString().Replace("-pass 1", "-pass 2"), out outputLog);
                OutputFileName = outputfile;
            }
            _outputLog.Append(outputLog);
            if (File.Exists(ffmpegTemp + "-0.log"))
                File.Delete(ffmpegTemp + "-0.log");
            if (File.Exists(ffmpegTemp + "-0.log.mbtree"))
                File.Delete(ffmpegTemp + "-0.log.mbtree");

            return result;
        }

        private bool FFMpeg_WMV12_process(string sourceFile, VideoTrack vt)
        {
            string videofile = Path.GetFileNameWithoutExtension(sourceFile);
            string videofilePath = Path.Combine(OutputFolder, videofile);
            string outputfile = String.Format("{0}.mkv", videofilePath);

            string preset = Path.Combine(appPath, @"ffmpeg\presets");
            var sb = new StringBuilder();
            sb.AppendFormat(" -i \"{0}\" ", sourceFile);
            sb.Append(" -pass 1 ");
            sb.Append(" -vcodec libx264 -vprofile high ");
            sb.AppendFormat(" -r {0} ", vt.FPS);
            if (vt.BitRate != -1)
                sb.AppendFormat(" -b:v {0}k ", vt.BitRate);

            // if height isn't division by 2 make it so
            // otherwise FFMpeg reports  'height not divisible by 2....'
            if (vt.Height % 2 == 1)
            {
                sb.AppendFormat(" -s {0}x{1} ", vt.Width, vt.Height + 1);
            }

            sb.Append(" -threads 0 ");

            sb.Append(" -acodec ac3 ");

            string ffmpegTemp = Path.Combine(WorkingFolder, Guid.NewGuid().ToString());
            sb.AppendFormat(" -passlogfile \"{0}\" ", ffmpegTemp);

            sb.AppendFormat(" -y  \"{0}\" ", outputfile); // -y overwrite 

            ProcessStatusChange(0, "Recode video track...(pass 1)", null);
            string outputLog;
            bool result = FFMpeg(sb.ToString(), out outputLog);
            if (result)
            {
                ProcessStatusChange(0, "Recode video track...(pass 2)", null);
                result = FFMpeg(sb.ToString().Replace("-pass 1", "-pass 2"), out outputLog);
                OutputFileName = outputfile;
            }
            _outputLog.Append(outputLog);
            if (File.Exists(ffmpegTemp + "-0.log"))
                File.Delete(ffmpegTemp + "-0.log");
            if (File.Exists(ffmpegTemp + "-0.log.mbtree"))
                File.Delete(ffmpegTemp + "-0.log.mbtree");

            return result;
        }

        private bool FFmpeg_x26x_process(string sourceFile, VideoTrack vt, bool useHevc)
        {
            string videofile = Path.GetFileNameWithoutExtension(sourceFile);
            string videofilePath = Path.Combine(OutputFolder, videofile);
            string outputfile = String.Format("{0}.mp4", videofilePath);

            string preset = Path.Combine(appPath, @"ffmpeg\presets");
            var sb = new StringBuilder();
            sb.AppendFormat(" -i \"{0}\" ", sourceFile);
            sb.Append(" -pass 1 ");
            if (useHevc)
                sb.Append(" -vcodec libx265");
            else
            {
                sb.Append(" -vcodec libx264");
                sb.Append(" -vprofile high "); //-vlevel 5.1 -pix_fmt yuv420p");
            }
            sb.AppendFormat(" -r {0} ", vt.FPS);

            int bitRate = 2000;
            
            // Work out max bitrate
            if (vt.BitRate < 640)
                bitRate = 640;
            else if (vt.BitRate > SelectedDevice.VideoMaxBitRate)
                bitRate = SelectedDevice.VideoMaxBitRate == -1 ? vt.BitRate : SelectedDevice.VideoMaxBitRate;
            //else
            //    bitRate = vt.BitRate;
            sb.AppendFormat(" -b:v {0}k ", bitRate);

            // if height isn't division by 2 make it so
            // otherwise FFMpeg reports  'height not divisible by 2....'
            int height = vt.Height;
            int width = vt.Width;
            if (vt.Height % 2 == 1)
            {
                height = vt.Height + 1;
            }
            if (height > SelectedDevice.VideoMaxHeight)
                height = SelectedDevice.VideoMaxHeight;

//            if (width > SelectedDevice.VideoMaxWidth)
//                width = SelectedDevice.VideoMaxWidth;


            if (width != vt.Width || height != vt.Height)
                sb.AppendFormat(" -vf scale=-1:{1} ", width, height);

            sb.Append(" -threads 0 ");

            sb.Append(" -acodec libvo_aacenc -ac 2 -ab 320000 ");

            string ffmpegTemp = Path.Combine(WorkingFolder, Guid.NewGuid().ToString());
            sb.AppendFormat(" -passlogfile \"{0}\" ", ffmpegTemp);

            sb.AppendFormat(" -y  \"{0}\" ", outputfile); // -y overwrite 

            ProcessStatusChange(0, "Recode video track...(pass 1)", null);
            string outputLog;
            bool result = FFMpeg(sb.ToString(), out outputLog);
            if (result)
            {
                ProcessStatusChange(0, "Recode video track...(pass 2)", null);
                result = FFMpeg(sb.ToString().Replace("-pass 1", "-pass 2"), out outputLog);
                OutputFileName = outputfile;
            }
            _outputLog.Append(outputLog);
            if (File.Exists(ffmpegTemp + "-0.log"))
                File.Delete(ffmpegTemp + "-0.log");
            if (File.Exists(ffmpegTemp + "-0.log.mbtree"))
                File.Delete(ffmpegTemp + "-0.log.mbtree");

            return result;
        }

        private bool Sonos_HiRestoMedRes(String _sourceFile, AudioTrack at)
        {
            string audiofile = Path.GetFileNameWithoutExtension(_sourceFile);
            string audiofilePath = Path.Combine(OutputFolder, audiofile);
            string outputAudiofile = String.Format("{0}.flac", audiofilePath);

            var sb = new StringBuilder();
            sb.AppendFormat(" \"{0}\" ", _sourceFile); // input 
            sb.AppendFormat(" \"{0}\" ", outputAudiofile); // output
            sb.Append(" -resampleTo48000 "); // force to 48kHz 
            sb.Append("-down16 ");
            sb.Append("-normalize ");

            sb.Append("  -progressnumbers "); // allow tool to give us % progress

            ProcessStatusChange(0, "Recode audio track...", null);
            bool result = Eac3to(sb.ToString());
            // set converted details
            at.WorkingFile = outputAudiofile;
            at.CodecID = "A_FLAC";

            // remove eac3to log file
            File.Delete(String.Format("{0} - Log.txt", audiofilePath));

            return result;
        }


        private bool MKVMergeTracks(string outputFile, VideoTrack vt, AudioTrack at, SubTrack st)
        {
            string outPath = Path.Combine(OutputFolder, outputFile);
            string tempFile = outPath + ".mkv";

            ProcessStatusChange(0, "Merging tracks from Mkv file");

            var args = new StringBuilder();
            if (!String.IsNullOrEmpty(Title))
                args.AppendFormat(" --title \"{0}\" ", Title);
            args.Append(" -o \"" + tempFile + "\"");


            if (!String.IsNullOrEmpty(vt.Title))
                args.AppendFormat(" --track-name 0:\"{0}\" ", vt.Title);
            if (!String.IsNullOrEmpty(vt.Language))
                args.AppendFormat(" --language 0:{0} ", vt.Language);
            args.AppendFormat(" --default-duration 0:{0}fps --compression -1:none  \"" + vt.WorkingFile + "\"", vt.FPS);

            if (!String.IsNullOrEmpty(at.Title))
                args.AppendFormat(" --track-name 0:\"{0}\" ", at.Title);
            if (!String.IsNullOrEmpty(at.Language))
                args.AppendFormat(" --language 0:{0} ", at.Language);
            if (at.Delay > 0)
                args.AppendFormat(" -y 0:{0}", at.Delay);
            args.Append(" --compression -1:none  \"" + at.WorkingFile + "\" ");

            // only add language preferred subtitles
            if (st != null && st.Preferred)
            {
                if (!String.IsNullOrEmpty(st.Language))
                    args.AppendFormat(" --language 0:{0} ", st.Language);
                args.Append("   \"" + st.WorkingFile + "\" ");
            }

            string outputLog;
            bool result = MKVMerge(args.ToString(), out outputLog);
            OutputFileName = tempFile;

            // Remove our temporary files
            if (vt != null)
                File.Delete(vt.WorkingFile);
            if (at != null)
                File.Delete(at.WorkingFile);
            if (st != null)
                File.Delete(st.WorkingFile);

            return result;
        }


        private bool TSMuxer_Disc(string outputFile, VideoTrack vt, AudioTrack at, SubTrack st)
        {
            string outPath = Path.Combine(OutputFolder, outputFile);

            // Generate meta file that contains all the parameters for TSMuxeR 
            string tempMetafile = Path.Combine(WorkingFolder, outputFile);
            string metaFileName = Path.ChangeExtension(tempMetafile, "meta");

            string metaData = "MUXOPT --no-pcr-on-video-pid --new-audio-pes --vbr  --vbv-len=500 ";
            if (SelectedDevice.Type == OutputDevice.AVCHD)
                metaData += " --avchd ";
            else if (SelectedDevice.Type == OutputDevice.Bluray)
                metaData += " --blu-ray ";
            metaData += Environment.NewLine;
            metaData += String.Format("{0}, \"{1}\", level=4.1, insertSEI, contSPS " + Environment.NewLine, vt.CodecID,
                                      vt.WorkingFile);
            metaData += String.Format("{0}, \"{1}\", lang=eng " + Environment.NewLine, at.CodecID, at.WorkingFile);

            // create the command file
            using (var sw = new StreamWriter(metaFileName))
            {
                sw.WriteLine(metaData);
                sw.Close();
            }
            string args = " \"" + metaFileName + "\" " + " \"" + outPath + "\" ";

            string outputLog;
            ProcessStatusChange(0, "Muxing tracks...", null);
            bool result = TSMuxer(args, out outputLog);
            //this.OutputFileName = tempFile;

            // Remove our temporary files
            if (vt != null)
                File.Delete(vt.WorkingFile);
            if (at != null)
                File.Delete(at.WorkingFile);
            if (st != null)
                File.Delete(st.WorkingFile);
            File.Delete(metaFileName);

            _outputLog.Append(outputLog);

            return result;
        }

        private bool TSMuxer_DTSMA_DTS(AudioTrack at)
        {
            string tempFile = Path.ChangeExtension(at.WorkingFile, "temp");

            // Generate meta file that contains all the parameters for TSMuxeR 
            string tempMetafile = Path.Combine(WorkingFolder, at.WorkingFile);
            string metaFileName = Path.ChangeExtension(tempMetafile, "meta");

            string metaData = "MUXOPT --no-pcr-on-video-pid --new-audio-pes --vbr  --vbv-len=500 " + Environment.NewLine;
            metaData += String.Format("A_DTS, \"{0}\", down-to-dts " + Environment.NewLine, at.WorkingFile);

            // create the command file
            using (var sw = new StreamWriter(metaFileName))
            {
                sw.WriteLine(metaData);
                sw.Close();
            }
            string args = " \"" + metaFileName + "\" " + " \"" + tempFile + "\" ";

            string outputLog;
            ProcessStatusChange(0, "De-muxing DTS-MA to DTS track...", null);
            bool result = TSMuxer(args, out outputLog);

            // Remove our temporary files
            if (at != null)
                File.Delete(at.WorkingFile);
            File.Delete(metaFileName);

            // Strange querk
            String newFileName = Path.ChangeExtension(at.WorkingFile, "dts");

            String source = Path.Combine(tempFile, Path.GetFileName(newFileName));
            String dest = Path.Combine(WorkingFolder, newFileName);
            File.Move(source, dest);
            Directory.Delete(tempFile, true);


            at.WorkingFile = dest;
            at.CodecID = "A_DTS";

            _outputLog.Append(outputLog);

            return result;
        }

        /// <summary>
        ///   Tools section
        /// </summary>
        private bool MEncoder(string args, out string outputLog)
        {
            ProcessStatusChange(0, "Recode video track...", null);

            string toolPath = Path.Combine(appPath, @"MEncoder\Mencoder.exe");

            _process = new Process();
            _process.StartInfo.FileName = toolPath;
            _process.StartInfo.Arguments = args;
            _process.StartInfo.UseShellExecute = false;
            _process.StartInfo.CreateNoWindow = true;
            _process.StartInfo.RedirectStandardOutput = true;
            _process.StartInfo.WorkingDirectory = appPath;
            Debug.WriteLine(_process.StartInfo.Arguments);
            _process.Start();

            outputLog = CaptureToolsOutputStdOut();
            _process.WaitForExit();
            Debug.WriteLine(outputLog);

            if (_process.ExitCode == 0)
            {
                return true;
            }
            else
                return false;
        }

        private bool MEncoder2(string args, out string outputLog)
        {
            ProcessStatusChange(0, "Recode video track...", null);

            string toolPath = Path.Combine(appPath, @"MEncoder2\Mencoder.exe");

            _process = new Process();
            _process.StartInfo.FileName = toolPath;
            _process.StartInfo.Arguments = args;
            _process.StartInfo.UseShellExecute = false;
            _process.StartInfo.CreateNoWindow = true;
            _process.StartInfo.RedirectStandardOutput = true;
            //process.StartInfo.RedirectStandardError = true;
            _process.StartInfo.WorkingDirectory = appPath;
            Debug.WriteLine(_process.StartInfo.Arguments);
            _process.Start();

            outputLog = CaptureToolsOutputStdOut();
            _process.WaitForExit();
            Debug.WriteLine(outputLog);

            if (_process.ExitCode == 0)
            {
                return true;
            }
            else
                return false;
        }

        private bool FFMpeg(string args, out string outputLog)
        {
            string toolPath = Path.Combine(appPath, @"ffmpeg\ffmpeg" + (Environment.Is64BitOperatingSystem ? "_x64" : "") + ".exe");

            _process = new Process();
            _process.StartInfo.FileName = toolPath;
            _process.StartInfo.Arguments = args;
            _process.StartInfo.UseShellExecute = false;
            _process.StartInfo.CreateNoWindow = true;
            _process.StartInfo.RedirectStandardOutput = true;
            _process.StartInfo.RedirectStandardError = true;
            _process.StartInfo.WorkingDirectory = appPath;
            Debug.WriteLine(toolPath);
            Debug.WriteLine(_process.StartInfo.Arguments);
            _process.Start();

            outputLog = CaptureToolsOutputStdErr();
            _process.WaitForExit();
            Debug.WriteLine(outputLog);

            if (_process.ExitCode == 0)
            {
                return true;
            }
            else
                return false;
        }

        private bool Eac3to(string args)
        {
            string toolPath = Path.Combine(appPath, @"eac3to\eac3to.exe");

            _process = new Process();
            _process.StartInfo.FileName = toolPath;
            _process.StartInfo.Arguments = args;
            _process.StartInfo.UseShellExecute = false;
            _process.StartInfo.CreateNoWindow = true;
            _process.StartInfo.RedirectStandardOutput = true;
            _process.StartInfo.WorkingDirectory = appPath;
            Debug.WriteLine(toolPath + " " + _process.StartInfo.Arguments);
            _process.Start();

            string output = CaptureToolsOutputStdOut();
            _process.WaitForExit();
            Debug.WriteLine(output);

            if (_process.ExitCode == 0)
            {
                return true;
            }
            else
                return false;
        }

        private bool TSMuxer(string args, out string outputLog)
        {
            string toolPath = Path.Combine(appPath, @"ts\tsMuxeR.exe");

            _process = new Process();

            _process.StartInfo.FileName = toolPath;
            _process.StartInfo.Arguments = args;
            _process.StartInfo.UseShellExecute = false;
            _process.StartInfo.CreateNoWindow = true;
            _process.StartInfo.RedirectStandardOutput = true;
            _process.StartInfo.WorkingDirectory = appPath;
            Debug.WriteLine(_process.StartInfo.Arguments);
            _process.Start();

            outputLog = CaptureToolsOutputStdOut();

            _process.WaitForExit();
            Debug.WriteLine(outputLog);

            int result = _process.ExitCode;
            return result == 0;
        }

        private bool MKVMerge(string args, out string outputLog)
        {
            string toolPath = Path.Combine(appPath, @"MKVToolnix\mkvmerge.exe");

            _process = new Process();

            _process.StartInfo.FileName = toolPath;
            _process.StartInfo.Arguments = args;
            _process.StartInfo.UseShellExecute = false;
            _process.StartInfo.CreateNoWindow = true;
            _process.StartInfo.RedirectStandardOutput = true;
            _process.StartInfo.WorkingDirectory = appPath;
            Debug.WriteLine(_process.StartInfo.Arguments);
            _process.Start();

            outputLog = CaptureToolsOutputStdOut();

            _process.WaitForExit();
            Debug.WriteLine(outputLog);

            return _process.ExitCode == 0;
        }


        /// <summary>
        ///   Change the file name passed in to use the provided extension
        ///   i.e. m2ts to mpg
        /// </summary>
        /// <param name="file"> </param>
        /// <param name="ext"> </param>
        private string UpdateFileExtension(string file, string ext)
        {
            var newFileName = Path.ChangeExtension(file, ext);
            if (File.Exists(newFileName))
                File.Delete(newFileName);

            File.Move(file, newFileName);
            return newFileName;
        }


        public string CaptureToolsOutputStdOut()
        {
            return CaptureToolsOutput(_process.StandardOutput);
        }

        public string CaptureToolsOutputStdErr()
        {
            return CaptureToolsOutput(_process.StandardError);
        }

        public string CaptureToolsOutput(StreamReader streamReader)
        {
            var sb = new StringBuilder();
            string prev = String.Empty;
            string line = String.Empty;
            double duration = 0.0;

            ProcessStatusChange(0, null);
            try
            {
                line = streamReader.ReadLine();
                while (line != null)
                {
                    if (line.Length > 0)
                    {
                        if (prev != line)
                        {
                            float result;
                            //if (line.Contains("progress: "))
                            if (line.Contains("progress: ") || line.Contains("Progress: ") || line.Contains("process: "))
                            //Using mkvextract or mkvmerge, both of these utils have this seting
                            {
                                line = line.Substring(line.IndexOf(": ") + 2);
                                line = line.Substring(0, line.IndexOf("%"));
                                if (float.TryParse(line, out result))
                                    ProcessStatusChange(result, null);
                            }
                            else if (line.Contains("% complete"))
                            {
                                line = line.Substring(0, line.IndexOf("%"));
                                if (float.TryParse(line, out result))
                                    ProcessStatusChange(result, null);
                            }
                            else if (line.Contains("%) "))
                            {
                                line = line.Substring(line.IndexOf("(") + 1);
                                line = line.Substring(0, line.IndexOf("%"));

                                if (float.TryParse(line, out result))
                                    ProcessStatusChange(result, null);
                            }
                            // handle FFMpeg duration and current time stderr output to work out percentage complete
                            else if (line.Contains("Duration:"))
                            {
                                duration = StringExtract.GetDuration(line);
                            }
                            else if (line.Contains("time="))
                            {
                                if (duration != 0.0)
                                {
                                    double current = StringExtract.GetTime(line);
                                    result = (float)((current / duration) * 100);
                                    ProcessStatusChange(result, null);
                                }
                            }

                            // TsMuxer
                            if (_rx.IsMatch(line))
                            {
                                if (float.TryParse(line, out result))
                                    ProcessStatusChange(result, null);
                            }

                            prev = line;
                            sb.AppendLine(line);
                        }
                    }

                    line = streamReader.ReadLine();
                }
            }
            catch (Exception e)
            {
                Debug.WriteLine(e.Source + ":" + e.Message);
                Debug.WriteLine(line);
            }
            ProcessStatusChange(100, null);

            return sb.ToString();
        }


        // Reference Frame List for h.264 streams
        private void FillRefList()
        {
            //refFrameList.Add(new RefClass(1920, 1200, 3));
            refFrameList.Add(new RefClass(1920, 1080, 4));
            refFrameList.Add(new RefClass(1920, 864, 5));
            refFrameList.Add(new RefClass(1920, 720, 6));

            refFrameList.Add(new RefClass(1280, 720, 9));
            refFrameList.Add(new RefClass(1280, 640, 10));
            refFrameList.Add(new RefClass(1280, 588, 11));
            refFrameList.Add(new RefClass(1280, 540, 12));
            refFrameList.Add(new RefClass(1280, 498, 13));
            refFrameList.Add(new RefClass(1280, 462, 14));
            refFrameList.Add(new RefClass(1280, 432, 15));
            refFrameList.Add(new RefClass(1280, 405, 16));
        }

        private bool CheckReference(int width, int height, int refFrames, out int maxRefFrames)
        {
            if (width == 1920)
                maxRefFrames = refFrameList[0].RefFrames;
            else
                maxRefFrames = refFrameList[3].RefFrames;

            foreach (var refItem in refFrameList)
            {
                if (width == refItem.Width)
                {
                    if (height >= refItem.Height)
                    {
                        if (refFrames > maxRefFrames)
                            return true;
                    }
                    else
                        maxRefFrames = refItem.RefFrames;
                }
            }
            return false;
        }


        public static IOutputDevice GetOutputDeviceByName(string name)
        {
            foreach (var dev in OutputDeviceList)
            {
                if (dev.Name.CompareTo(name) == 0)
                    return dev;
            }
            return null;
        }

        public static AudioLanguage GetAudioLanguageByDisplayName(string name)
        {
            foreach (var audio in LanguageList)
            {
                if (audio.DisplayName.CompareTo(name) == 0)
                    return audio;
            }
            return null;
        }

        public void ProcessStatusChange(float value, string message)
        {
            bool indeterminate = false;
            if (StatusChanged != null)
            {
                if (value == 0.0)
                    indeterminate = true;
                StatusChanged(this, new ProgressEventArgs(value, message, null, indeterminate));
            }
        }

        public void ProcessStatusChange(float value, string message, OutputReport data)
        {
            bool indeterminate = false;
            if (StatusChanged != null)
            {
                if (value == 0.0)
                    indeterminate = true;
                StatusChanged(this, new ProgressEventArgs(value, message, data, indeterminate));
            }
        }
    }

    // ProgressEventArgs for Status Change
    public class ProgressEventArgs : EventArgs
    {
        public ProgressEventArgs(float percent, string message, OutputReport data, bool indeterminate)
        {
            Percent = percent;
            Message = message;
            Indeterminate = indeterminate;
            Data = data;
        }

        public float Percent;
        public string Message;
        public OutputReport Data;
        public bool Indeterminate;
    }

    //end of class ProgressEventArgs


    public class OutputReport
    {
        public String outputReport;
        public String outputFileName;
    }
}