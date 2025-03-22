using System;
using System.Collections;

namespace Internationale.FileSystems.Fat32
{
    public struct Fat32DateTime
    {
        private byte _millis;
        private short _date;
        private short _time;

        public byte Millis
        {
            get { return _millis; }
        }

        public short Date
        {
            get { return _date; }
        }

        public short Time
        {
            get { return _time; }
        }

        public DateTime DateTime
        {
            get
            {
                BitArray values = new BitArray(new int[] { _time });

                int hour = 0;
                int minutes = 0;
                int seconds = 0;

                short offset = 0;
                for (short i = 0; i < 5; i++)
                {
                    hour |= (short)(Convert.ToByte(values.Get(offset++)) << i);
                }

                for (short i = 0; i < 6; i++)
                {
                    minutes |= (short)(Convert.ToByte(values.Get(offset++)) << i);
                }

                for (short i = 0; i < 5; i++)
                {
                    seconds |= (short)(Convert.ToByte(values.Get(offset++)) << i);
                }

                int year = 0;
                int month = 0;
                int day = 0;

                values = new BitArray(new int[] { _date });

                offset = 0;
                for (short i = 0; i < 7; i++)
                {
                    year |= (short)(Convert.ToByte(values.Get(offset++)) << i);
                }

                for (short i = 0; i < 4; i++)
                {
                    month |= (short)(Convert.ToByte(values.Get(offset++)) << i);
                }

                for (short i = 0; i < 5; i++)
                {
                    day |= (short)(Convert.ToByte(values.Get(offset++)) << i);
                }

                year += 1920;
                
                return new DateTime(year, month, day, hour, minutes, seconds, _millis);
            }
        }

        public Fat32DateTime(byte millis, short date, short time)
        {
            _millis = millis;
            _date = date;
            _time = time;
        }

        public Fat32DateTime(DateTime time)
        {
            _millis = (byte)time.Millisecond;
            _date = 0;
            _time = 0;

            BitArray hour = new BitArray(new int[] { time.Hour });
            BitArray minutes = new BitArray(new int[] { time.Minute });
            BitArray seconds = new BitArray(new int[] { time.Second });

            int offset = 0;
            for (short i = 0; i < 5; i++)
            {
                _time |= (short)(Convert.ToByte(hour.Get(i)) << offset++);
            }

            for (short i = 0; i < 6; i++)
            {
                _time |= (short)(Convert.ToByte(minutes.Get(i)) << offset++);
            }

            for (short i = 0; i < 5; i++)
            {
                _time |= (short)(Convert.ToByte(seconds.Get(i)) << offset++);
            }

            BitArray year = new BitArray(new int[] { time.Year });
            BitArray month = new BitArray(new int[] { time.Month });
            BitArray day = new BitArray(new int[] { time.Day });

            offset = 0;
            for (short i = 0; i < 7; i++)
            {
                _date |= (short)(Convert.ToByte(year.Get(i)) << offset++);
            }

            for (short i = 0; i < 4; i++)
            {
                _date |= (short)(Convert.ToByte(month.Get(i)) << offset++);
            }

            for (short i = 0; i < 5; i++)
            {
                _date |= (short)(Convert.ToByte(day.Get(i)) << offset++);
            }
        }
    }
}