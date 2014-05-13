using System;
using PandaVideoMixer;

namespace PandaVideo
{
    public class LogItem
    {
        public DateTime TimeStamp { get; set; }
        public String Message { get; set; }
        public OutputReport Data { get; set; }

        public LogItem(String msg)
        {
            TimeStamp = DateTime.Now;
            Message = msg;
        }

        public LogItem(DateTime dt, String msg)
        {
            TimeStamp = dt;
            Message = msg;
        }

        public LogItem(DateTime dt, String msg, OutputReport data)
        {
            TimeStamp = dt;
            Message = msg;
            Data = data;
        }

        public string LocalTime
        {
            get { return TimeStamp.ToLongTimeString(); }
        }
    }
}