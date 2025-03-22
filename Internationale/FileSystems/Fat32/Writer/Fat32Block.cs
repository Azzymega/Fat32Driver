using System;
using System.Collections;
using System.IO;
using System.Text;

namespace Internationale.FileSystems.Fat32.Writer
{
    public struct Fat32Block
    {
        public byte[] Random;                        
        public Fat32DescriptorAttributes Attributes;
        public byte Reserved;
        public byte Millis;
        public ushort Time;
        public ushort Date;
        public ushort LastAccessedDate;
        public ushort HighCluster;
        public ushort LastModTime;
        public ushort LastModDate;
        public ushort LowCluster;
        public uint Size;

        public byte[] ToArray()
        {
            using (MemoryStream stream = new MemoryStream())
            {
                using (BinaryWriter writer = new BinaryWriter(stream))
                {
                    writer.Write(Random);
                    writer.Write((byte)Attributes);
                    writer.Write(Reserved);
                    writer.Write(Millis);
                    writer.Write(Time);
                    writer.Write(Date);
                    writer.Write(LastAccessedDate);
                    writer.Write(HighCluster);
                    writer.Write(LastModTime);
                    writer.Write(LastModDate);
                    writer.Write(LowCluster);
                    writer.Write(Size);
                }

                return stream.ToArray();
            }
        }
        
        public int Cluster
        {
            get
            {
                byte[] low = BitConverter.GetBytes(LowCluster);
                byte[] high = BitConverter.GetBytes(HighCluster);

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

                LowCluster = (ushort)secondHalf;
                HighCluster = (ushort)firstHalf;
            }
        }
        
        public Fat32Block(string name, int offset, int index, bool last, byte[] garbage)
        {
            int offsetIndex = offset;
            byte[] first = Encoding.Unicode.GetBytes(name);
            
            Random = new byte[11];

            for (int i = 0; i < Random.Length; i++)
            {
                Random[i] = byte.MaxValue;
            }

            Time = char.MaxValue;
            Date = char.MaxValue;
            LastAccessedDate = char.MaxValue;
            HighCluster = char.MaxValue;
            LastModTime = char.MaxValue;
            LastModDate = char.MaxValue;
            Size = uint.MaxValue;

            byte target = 0;
            if (last)
            {
                target = (byte)index;
                target |= 1 << 6;
            }
            else
            {
                target = (byte)index;
            }
            Random[0] = target;

            StringBuilder builder = new StringBuilder();
            for (int i = 0; i < 5; i++)
            {
                builder.Append(name[offsetIndex++]);
            }
            
            byte[] result = Encoding.Unicode.GetBytes(builder.ToString());
            for (int i = 0; i < result.Length; i++)
            {
                Random[i + 1] = result[i];
            }
            
            Attributes = Fat32DescriptorAttributes.Unicode;
            Reserved = 0;

            byte checksum = 0;
            for (int i = 1; i < 11; i++)
            {
                byte bit = (byte)((checksum & 0x1) << 7);
                checksum = (byte)(((checksum >> 1) & 0x7F) | bit);
                checksum = (byte)(checksum + garbage[i]);
            }
            
            Millis = checksum;

            Time = name[offsetIndex++];
            Date = name[offsetIndex++];
            LastAccessedDate = name[offsetIndex++];
            HighCluster = name[offsetIndex++];
            LastModTime = name[offsetIndex++];
            LastModDate = name[offsetIndex++];
            LowCluster = 0;

            Size = BitConverter.ToUInt32(first,offsetIndex*2);
        }

        public Fat32Block(DateTime time, Fat32DescriptorAttributes attributes, byte[] garbage)
        {
            Fat32DateTime fat32DateTime = new Fat32DateTime(time);
            
            Reserved = 0;
            Random = garbage;
            
            Attributes = attributes;
            Reserved = 0;
            Millis = (byte)time.Millisecond;
            Time = (ushort)fat32DateTime.Time;
            Date = (ushort)fat32DateTime.Date;
            LastAccessedDate = Date;
            HighCluster = 0;
            LastModTime = Time;
            LastModDate = Date;
            LowCluster = 0;
            Size = 0;
        }
    }
}