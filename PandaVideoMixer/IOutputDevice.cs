using System;
using System.Windows.Media.Imaging;

namespace PandaVideoMixer
{
    public interface IOutputDevice
    {
        OutputDevice Type { get; }

        string Name { get; }

        int VideoMaxWidth { get; }
        int VideoMaxHeight { get; }

        int VideoMaxBitRate { get; }

        int MaxAudioChannels { get; }

        BitmapImage GetImage { get; }

        bool Ringtone { get; }
        bool AudioOnly { get; }
        bool HEVC { get; }
        bool TV3D { get; }
        bool MKV { get; }
    }


    public class DevicePS3 : IOutputDevice
    {
        private readonly BitmapImage _bi = new BitmapImage();

        public DevicePS3()
        {
            // BitmapImage.UriSource must be in a BeginInit/EndInit block.
            _bi.BeginInit();
            _bi.UriSource = new Uri(@"/PandaVideoMixer;component/Images/PS3Video.bmp", UriKind.RelativeOrAbsolute);
            _bi.EndInit();
        }

        #region IOutputDevice Members

        public OutputDevice Type
        {
            get { return OutputDevice.PS3; }
        }

        public string Name
        {
            get { return "PS3"; }
        }

        public int VideoMaxWidth
        {
            get { return 1920; }
        }

        public int VideoMaxHeight
        {
            get { return 1080; }
        }

        public int MaxAudioChannels
        {
            get { return 8; }
        }

        public BitmapImage GetImage
        {
            get { return _bi; }
        }

        public bool Ringtone
        {
            get { return false; }
        }

        public bool AudioOnly
        {
            get { return false; }
        }

        public bool HEVC { get { return false; } }

        public int VideoMaxBitRate { get { return -1; } }

        public bool TV3D { get { return false; } }
        public bool MKV { get { return false; } }
        #endregion
    }

    public class DeviceiPhone3GS : IOutputDevice
    {
        private readonly BitmapImage _bi = new BitmapImage();

        public DeviceiPhone3GS()
        {
            // BitmapImage.UriSource must be in a BeginInit/EndInit block.
            _bi.BeginInit();
            _bi.UriSource = new Uri(@"/PandaVideoMixer;component/Images/iPhone.bmp", UriKind.RelativeOrAbsolute);
            _bi.EndInit();
        }

        #region IOutputDevice Members

        public OutputDevice Type
        {
            get { return OutputDevice.iPhone3GS; }
        }

        public string Name
        {
            get { return "iPhone 3GS"; }
        }

        public int VideoMaxWidth
        {
            get { return 640; }
        }

        public int VideoMaxHeight
        {
            get { return 480; }
        }

        public int MaxAudioChannels
        {
            get { return 2; }
        }

        public BitmapImage GetImage
        {
            get { return _bi; }
        }

        public bool Ringtone
        {
            get { return true; }
        }

        public bool AudioOnly
        {
            get { return false; }
        }

        public bool HEVC { get { return false; } }

        public int VideoMaxBitRate { get { return -1; } }
        public bool TV3D { get { return false; } }
        public bool MKV { get { return false; } }

        #endregion
    }

    public class DeviceiPad : IOutputDevice
    {
        private readonly BitmapImage _bi = new BitmapImage();

        public DeviceiPad()
        {
            _bi.BeginInit();
            _bi.UriSource = new Uri(@"/PandaVideoMixer;component/Images/ipad.bmp", UriKind.RelativeOrAbsolute);
            _bi.EndInit();
        }

        #region IOutputDevice Members

        public OutputDevice Type
        {
            get { return OutputDevice.iPad; }
        }

        public string Name
        {
            get { return "iPad"; }
        }

        public int VideoMaxWidth
        {
            get { return 1280; }
        }

        public int VideoMaxHeight
        {
            get { return 720; }
        }

        public int MaxAudioChannels
        {
            get { return 2; }
        }

        public BitmapImage GetImage
        {
            get { return _bi; }
        }

        public bool Ringtone
        {
            get { return false; }
        }

        public bool AudioOnly
        {
            get { return false; }
        }

        public bool HEVC { get { return false; } }

        public int VideoMaxBitRate { get { return -1; } }
        public bool TV3D { get { return false; } }
        public bool MKV { get { return false; } }

        #endregion
    }


    public class DeviceXBox360 : IOutputDevice
    {
        private readonly BitmapImage _bi = new BitmapImage();

        public DeviceXBox360()
        {
            _bi.BeginInit();
            _bi.UriSource = new Uri(@"/PandaVideoMixer;component/Images/Xbox360.bmp", UriKind.RelativeOrAbsolute);

            _bi.EndInit();
        }

        #region IOutputDevice Members

        public OutputDevice Type
        {
            get { return OutputDevice.XBox360; }
        }

        public string Name
        {
            get { return "XBox 360"; }
        }

        public int VideoMaxWidth
        {
            get { return 1920; }
        }

        public int VideoMaxHeight
        {
            get { return 1080; }
        }

        // for .h264 only aac 2
        public int MaxAudioChannels
        {
            get { return 2; }
        }

        public BitmapImage GetImage
        {
            get { return _bi; }
        }

        public bool Ringtone
        {
            get { return false; }
        }

        public bool AudioOnly
        {
            get { return false; }
        }

        public bool HEVC { get { return false; } }

        public int VideoMaxBitRate { get { return -1; } }
        public bool TV3D { get { return false; } }
        public bool MKV { get { return false; } }

        #endregion
    }

    public class DeviceGeneric : IOutputDevice
    {
        private readonly BitmapImage _bi = new BitmapImage();

        public DeviceGeneric()
        {
            // BitmapImage.UriSource must be in a BeginInit/EndInit block.
            _bi.BeginInit();
            _bi.UriSource = new Uri(@"/PandaVideoMixer;component/Images/Generic.bmp", UriKind.RelativeOrAbsolute);
            _bi.EndInit();
        }

        #region IOutputDevice Members

        public OutputDevice Type
        {
            get { return OutputDevice.Generic; }
        }

        public string Name
        {
            get { return "Generic Media Player"; }
        }

        public int VideoMaxWidth
        {
            get { return 1920; }
        }

        public int VideoMaxHeight
        {
            get { return 1080; }
        }

        public int MaxAudioChannels
        {
            get { return 8; }
        }

        public BitmapImage GetImage
        {
            get { return _bi; }
        }

        public bool Ringtone
        {
            get { return false; }
        }

        public bool AudioOnly
        {
            get { return false; }
        }

        public bool HEVC { get { return false; } }
        public int VideoMaxBitRate { get { return -1; } }
        public bool TV3D { get { return false; } }
        public bool MKV { get { return false; } }

        #endregion
    }

    public class DeviceAVCHD : IOutputDevice
    {
        private readonly BitmapImage _bi = new BitmapImage();

        public DeviceAVCHD()
        {
            // BitmapImage.UriSource must be in a BeginInit/EndInit block.
            _bi.BeginInit();
            _bi.UriSource = new Uri(@"/PandaVideoMixer;component/Images/AVCHD.bmp", UriKind.RelativeOrAbsolute);
            _bi.EndInit();
        }

        #region IOutputDevice Members

        public OutputDevice Type
        {
            get { return OutputDevice.AVCHD; }
        }

        public string Name
        {
            get { return "AVCHD Disk Image"; }
        }

        public int VideoMaxWidth
        {
            get { return 1920; }
        }

        public int VideoMaxHeight
        {
            get { return 1080; }
        }

        public int MaxAudioChannels
        {
            get { return 8; }
        }

        public BitmapImage GetImage
        {
            get { return _bi; }
        }

        public bool Ringtone
        {
            get { return false; }
        }

        public bool AudioOnly
        {
            get { return false; }
        }

        public bool HEVC { get { return false; } }
        public int VideoMaxBitRate { get { return -1; } }
        public bool TV3D { get { return false; } }
        public bool MKV { get { return false; } }

        #endregion
    }

    public class DeviceBluray : IOutputDevice
    {
        private readonly BitmapImage _bi = new BitmapImage();

        public DeviceBluray()
        {
            // BitmapImage.UriSource must be in a BeginInit/EndInit block.
            _bi.BeginInit();
            _bi.UriSource = new Uri(@"/PandaVideoMixer;component/Images/Bluray.bmp", UriKind.RelativeOrAbsolute);
            _bi.EndInit();
        }

        #region IOutputDevice Members

        public OutputDevice Type
        {
            get { return OutputDevice.Bluray; }
        }

        public string Name
        {
            get { return "Blu-ray  Disk Image"; }
        }

        public int VideoMaxWidth
        {
            get { return 1920; }
        }

        public int VideoMaxHeight
        {
            get { return 1080; }
        }

        public int MaxAudioChannels
        {
            get { return 8; }
        }

        public BitmapImage GetImage
        {
            get { return _bi; }
        }

        public bool Ringtone
        {
            get { return false; }
        }

        public bool AudioOnly
        {
            get { return false; }
        }

        public bool HEVC { get { return false; } }
        public int VideoMaxBitRate { get { return -1; } }
        public bool TV3D { get { return false; } }
        public bool MKV { get { return false; } }

        #endregion
    }


    public class DeviceHTML5 : IOutputDevice
    {
        private readonly BitmapImage _bi = new BitmapImage();

        public DeviceHTML5()
        {
            _bi.BeginInit();
            _bi.UriSource = new Uri(@"/PandaVideoMixer;component/Images/HTML5.bmp", UriKind.RelativeOrAbsolute);
            _bi.EndInit();
        }

        #region IOutputDevice Members

        public OutputDevice Type
        {
            get { return OutputDevice.HTML5; }
        }

        public string Name
        {
            get { return "HTML 5"; }
        }

        public int VideoMaxWidth
        {
            get { return 1920; }
        }

        public int VideoMaxHeight
        {
            get { return 1200; }
        }

        public int MaxAudioChannels
        {
            get { return 2; }
        }

        public BitmapImage GetImage
        {
            get { return _bi; }
        }

        public bool Ringtone
        {
            get { return false; }
        }

        public bool AudioOnly
        {
            get { return false; }
        }

        public bool HEVC { get { return false; } }
        public int VideoMaxBitRate { get { return -1; } }
        public bool TV3D { get { return false; } }
        public bool MKV { get { return false; } }

        #endregion
    }

    public class DeviceWDLiveTV : IOutputDevice
    {
        private readonly BitmapImage _bi = new BitmapImage();

        public DeviceWDLiveTV()
        {
            // BitmapImage.UriSource must be in a BeginInit/EndInit block.
            _bi.BeginInit();
            _bi.UriSource = new Uri(@"/PandaVideoMixer;component/Images/WDLiveTV.bmp", UriKind.RelativeOrAbsolute);
            _bi.EndInit();
        }

        #region IOutputDevice Members

        public OutputDevice Type
        {
            get { return OutputDevice.WDLiveTV; }
        }

        public string Name
        {
            get { return "WD Live TV Media Player"; }
        }

        public int VideoMaxWidth
        {
            get { return 1920; }
        }

        public int VideoMaxHeight
        {
            get { return 1080; }
        }

        public int MaxAudioChannels
        {
            get { return 8; }
        }

        public BitmapImage GetImage
        {
            get { return _bi; }
        }

        public bool Ringtone
        {
            get { return false; }
        }

        public bool AudioOnly
        {
            get { return false; }
        }

        public bool HEVC { get { return false; } }
        public int VideoMaxBitRate { get { return -1; } }
        public bool TV3D { get { return false; } }
        public bool MKV { get { return false; } }

        #endregion
    }

    public class DeviceRaw : IOutputDevice
    {
        private readonly BitmapImage _bi = new BitmapImage();

        public DeviceRaw()
        {
            // BitmapImage.UriSource must be in a BeginInit/EndInit block.
            _bi.BeginInit();
            _bi.UriSource = new Uri(@"/PandaVideoMixer;component/Images/Generic.bmp", UriKind.RelativeOrAbsolute);
            _bi.EndInit();
        }

        #region IOutputDevice Members

        public OutputDevice Type
        {
            get { return OutputDevice.RawFiles; }
        }

        public string Name
        {
            get { return "Raw Files"; }
        }

        public int VideoMaxWidth
        {
            get { return 1920; }
        }

        public int VideoMaxHeight
        {
            get { return 1080; }
        }

        public int MaxAudioChannels
        {
            get { return 8; }
        }

        public BitmapImage GetImage
        {
            get { return _bi; }
        }

        public bool Ringtone
        {
            get { return false; }
        }

        public bool AudioOnly
        {
            get { return false; }
        }

        public bool HEVC { get { return false; } }
        public int VideoMaxBitRate { get { return -1; } }
        public bool TV3D { get { return false; } }
        public bool MKV { get { return false; } }

        #endregion
    }

    public class DeviceSamsungS3 : IOutputDevice
    {
        private readonly BitmapImage _bi = new BitmapImage();

        public DeviceSamsungS3()
        {
            // BitmapImage.UriSource must be in a BeginInit/EndInit block.
            _bi.BeginInit();
            _bi.UriSource = new Uri(@"/PandaVideoMixer;component/Images/SamsungS3.bmp", UriKind.RelativeOrAbsolute);
            _bi.EndInit();
        }

        #region IOutputDevice Members

        public OutputDevice Type
        {
            get { return OutputDevice.SamsungS3; }
        }

        public string Name
        {
            get { return "Samsung S3"; }
        }

        public int VideoMaxWidth
        {
            get { return 1280; }
        }

        public int VideoMaxHeight
        {
            get { return 720; }
        }

        public int MaxAudioChannels
        {
            get { return 2; }
        }

        public BitmapImage GetImage
        {
            get { return _bi; }
        }

        public bool Ringtone
        {
            get { return true; }
        }

        public bool AudioOnly
        {
            get { return false; }
        }

        public bool HEVC { get { return false; } }
        public int VideoMaxBitRate { get { return 2500; } }
        public bool TV3D { get { return false; } }
        public bool MKV { get { return false; } }

        #endregion
    }

    public class DeviceSamsungS4 : IOutputDevice
    {
        private readonly BitmapImage _bi = new BitmapImage();

        public DeviceSamsungS4()
        {
            // BitmapImage.UriSource must be in a BeginInit/EndInit block.
            _bi.BeginInit();
            _bi.UriSource = new Uri(@"/PandaVideoMixer;component/Images/SamsungS4.bmp", UriKind.RelativeOrAbsolute);
            _bi.EndInit();
        }

        #region IOutputDevice Members

        public OutputDevice Type
        {
            get { return OutputDevice.SamsungS4; }
        }

        public string Name
        {
            get { return "Samsung S4"; }
        }

        public int VideoMaxWidth
        {
            get { return 1920; }
        }

        public int VideoMaxHeight
        {
            get { return 1080; }
        }

        public int MaxAudioChannels
        {
            get { return 2; }
        }

        public BitmapImage GetImage
        {
            get { return _bi; }
        }

        public bool Ringtone
        {
            get { return true; }
        }

        public bool AudioOnly
        {
            get { return false; }
        }

        public bool HEVC { get { return false; } }
        public int VideoMaxBitRate { get { return 2500; } }
        public bool TV3D { get { return false; } }
        public bool MKV { get { return false; } }

        #endregion
    }

    public class DeviceSonos : IOutputDevice
    {
        private readonly BitmapImage _bi = new BitmapImage();

        public DeviceSonos()
        {
            // BitmapImage.UriSource must be in a BeginInit/EndInit block.
            _bi.BeginInit();
            _bi.UriSource = new Uri(@"/PandaVideoMixer;component/Images/Sonos.bmp", UriKind.RelativeOrAbsolute);
            _bi.EndInit();
        }

        #region IOutputDevice Members

        public OutputDevice Type
        {
            get { return OutputDevice.Sonos; }
        }

        public string Name
        {
            get { return "Sonos"; }
        }

        public int VideoMaxWidth
        {
            get { return -1; }
        }

        public int VideoMaxHeight
        {
            get { return -1; }
        }

        public int MaxAudioChannels
        {
            get { return 2; }
        }

        public BitmapImage GetImage
        {
            get { return _bi; }
        }

        public bool Ringtone
        {
            get { return false; }
        }

        public bool AudioOnly
        {
            get { return true; }
        }

        public bool HEVC { get { return false; } }
        public int VideoMaxBitRate { get { return -1; } }
        public bool TV3D { get { return false; } }
        public bool MKV { get { return false; } }

        #endregion
    }

    public class DeviceSamsungS5 : IOutputDevice
    {
        private readonly BitmapImage _bi = new BitmapImage();

        public DeviceSamsungS5()
        {
            // BitmapImage.UriSource must be in a BeginInit/EndInit block.
            _bi.BeginInit();
            _bi.UriSource = new Uri(@"/PandaVideoMixer;component/Images/SamsungS5.bmp", UriKind.RelativeOrAbsolute);
            _bi.EndInit();
        }

        #region IOutputDevice Members

        public OutputDevice Type
        {
            get { return OutputDevice.SamsungS5; }
        }

        public string Name
        {
            get { return "Samsung S5"; }
        }

        public int VideoMaxWidth
        {
            get { return 1920; }
        }

        public int VideoMaxHeight
        {
            get { return 1080; }
        }

        public int MaxAudioChannels
        {
            get { return 2; }
        }

        public BitmapImage GetImage
        {
            get { return _bi; }
        }

        public bool Ringtone
        {
            get { return true; }
        }

        public bool AudioOnly
        {
            get { return false; }
        }

        public bool HEVC { get { return true; } }
        public int VideoMaxBitRate { get { return -1; } }
        public bool TV3D { get { return false; } }
        public bool MKV { get { return false; } }

        #endregion
    }

    public class DeviceSamsungUHDTV : IOutputDevice
    {
        private readonly BitmapImage _bi = new BitmapImage();

        public DeviceSamsungUHDTV()
        {
            // BitmapImage.UriSource must be in a BeginInit/EndInit block.
            _bi.BeginInit();
            _bi.UriSource = new Uri(@"/PandaVideoMixer;component/Images/SamsungUHDTV.bmp", UriKind.RelativeOrAbsolute);
            _bi.EndInit();
        }

        #region IOutputDevice Members

        public OutputDevice Type
        {
            get { return OutputDevice.SamsungUHDTV; }
        }

        public string Name
        {
            get { return "Samsung UHD TV"; }
        }

        public int VideoMaxWidth
        {
            get { return 4096; }
        }

        public int VideoMaxHeight
        {
            get { return 2160; }
        }

        public int MaxAudioChannels
        {
            get { return 6; }
        }

        public BitmapImage GetImage
        {
            get { return _bi; }
        }

        public bool Ringtone
        {
            get { return false; }
        }

        public bool AudioOnly
        {
            get { return false; }
        }

        public bool HEVC { get { return true; } }
        public int VideoMaxBitRate { get { return 50000; } }
        public bool TV3D { get { return true; } }
        public bool MKV { get { return true; } }

        #endregion
    }


}