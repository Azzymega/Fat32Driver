using System;
using System.IO;
using System.Text;

namespace Internationale.FileSystems.Fat32
{
    public class Fat32Descriptor : Fat32Element
    {
        private readonly byte[] _maybe;                        
        private readonly Fat32DescriptorAttributes _attributes;
        private readonly byte _reserved;
        private readonly byte _millis;
        private readonly short _time;
        private readonly short _date;
        private readonly short _lastAccessedDate;
        private short _highCluster;
        private readonly short _lastModTime;
        private readonly short _lastModDate;
        private short _lowCluster;
        private readonly int _size;

        private string _longName;

        public bool IsLongFileName
        {
            get { return _attributes == Fat32DescriptorAttributes.Unicode; }
        }

        public DateTime Time
        {
            get
            {
                return new Fat32DateTime(_millis, _date, _time).DateTime;
            }
        }
        
        public int Cluster
        {
            get
            {
                byte[] low = BitConverter.GetBytes(_lowCluster);
                byte[] high = BitConverter.GetBytes(_highCluster);

                byte[] bytes = new byte[4];
                bytes[0] = low[0];
                bytes[1] = low[1];
                bytes[2] = high[0];
                bytes[3] = high[1];

                return BitConverter.ToInt32(bytes);
            }
            set
            {
                short firstHalf = (short) (value >> 16);
                short secondHalf = (short) (value & 0xffff);

                _lowCluster = secondHalf;
                _highCluster = firstHalf;
            }
        }

        public override string Name
        {
            get
            {
                if (IsLongFileName)
                {
                    StringBuilder builder = new StringBuilder();
                    for (int i = 1; i < 11; i++)
                    {
                        builder.Append((char)_maybe[i]);
                    }

                    builder.Append(BitConverter.ToChar(BitConverter.GetBytes(_time)));
                    builder.Append(BitConverter.ToChar(BitConverter.GetBytes(_date)));
                    builder.Append(BitConverter.ToChar(BitConverter.GetBytes(_lastAccessedDate)));
                    builder.Append(BitConverter.ToChar(BitConverter.GetBytes(_highCluster)));
                    builder.Append(BitConverter.ToChar(BitConverter.GetBytes(_lastModTime)));
                    builder.Append(BitConverter.ToChar(BitConverter.GetBytes(_lastModDate)));
                    builder.Append(Encoding.Unicode.GetChars(BitConverter.GetBytes(_size)));

                    string oldStr = builder.ToString();
                    builder.Clear();

                    for (int i = 0; i < oldStr.Length; i++)
                    {
                        if (oldStr[i] == Char.MaxValue)
                        {
                            break;
                        }
                        else if (oldStr[i] != '\0')
                        {
                            builder.Append(oldStr[i]);
                        }
                    }

                    return builder.ToString();
                }
                else if (_longName != null)
                {
                    return _longName;
                }
                else
                {
                    StringBuilder builder = new StringBuilder();
                    for (int i = 0; i < _maybe.Length; i++)
                    {
                        builder.Append((char)_maybe[i]);
                    }

                    return builder.ToString();
                }
            }
        }

        public override bool IsDirectory
        {
            get
            {
                if ((_attributes & Fat32DescriptorAttributes.Directory) == Fat32DescriptorAttributes.Directory)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
        }

        internal void SetLongName(string newName)
        {
            _longName = newName;
        }

        public int Size
        {
            get { return _size; }
        }

        public Fat32DescriptorAttributes Attributes
        {
            get { return _attributes; }
        }

        public Fat32Descriptor(BinaryReader reader)
        {
            _maybe = reader.ReadBytes(11);
            _attributes = (Fat32DescriptorAttributes)reader.ReadByte();
            _reserved = reader.ReadByte();
            _millis = reader.ReadByte();
            _time = reader.ReadInt16();
            _date = reader.ReadInt16();
            _lastAccessedDate = reader.ReadInt16();
            _highCluster = reader.ReadInt16();
            _lastModTime = reader.ReadInt16();
            _lastModDate = reader.ReadInt16();
            _lowCluster = reader.ReadInt16();
            _size = reader.ReadInt32();
        }

        public override string ToString()
        {
            return Name;
        }
    }
}