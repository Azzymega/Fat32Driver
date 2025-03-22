

using System.IO;
using System.Text;
using global::System;

namespace Internationale.FileSystems.Fat32
{
    public class Fat32ExtendedBootRecord : Fat32Element
    {
        private int _sectorsPerFat;
        private short _flags;
        private byte _majorVersion;
        private byte _minorVersion;
        private int _rootDirectoryCluster;
        private short _fsInfoSector;
        private short _backupBootSector;
        private byte[] _reserved;           // 12
        private byte _driveNumber;
        private byte _reservedFlags;
        private byte _signature;
        private int _volumeId;
        private byte[] _volumeLabelString;  // 11
        private byte[] _systemIdentifier;   // 8
        private byte[] _bootCode;           // 420
        private short _bootableSignature;

        public Fat32ExtendedBootRecord(BinaryReader reader)
        {
            _sectorsPerFat = reader.ReadInt32();
            _flags = reader.ReadInt16();
            _majorVersion = reader.ReadByte();
            _minorVersion = reader.ReadByte();
            _rootDirectoryCluster = reader.ReadInt32();
            _fsInfoSector = reader.ReadInt16();
            _backupBootSector = reader.ReadInt16();
            _reserved = reader.ReadBytes(12);
            _driveNumber = reader.ReadByte();
            _reservedFlags = reader.ReadByte();
            _signature = reader.ReadByte();
            _volumeId = reader.ReadInt32();
            _volumeLabelString = reader.ReadBytes(11);
            _systemIdentifier = reader.ReadBytes(8);
            _bootCode = reader.ReadBytes(420);
            _bootableSignature = reader.ReadInt16();
        }

        public String SystemIdentifier
        {
            get
            {
                StringBuilder builder = new StringBuilder();
                foreach (byte t in _systemIdentifier)
                {
                    builder.Append((char)t);
                }
                return builder.ToString();
            }
        }
        
        public String VolumeLabel
        {
            get
            {
                StringBuilder builder = new StringBuilder();
                foreach (byte t in _volumeLabelString)
                {
                    builder.Append((char)t);
                }
                return builder.ToString();
            }
        }

        public int RootDirectoryCluster
        {
            get { return _rootDirectoryCluster; }
        }

        public byte MinorVersion
        {
            get { return _minorVersion; }
        }

        public byte MajorVersion
        {
            get { return _majorVersion; }
        }

        public short Flags
        {
            get { return _flags; }
        }

        public int SectorsPerFat
        {
            get { return _sectorsPerFat; }
        }

        public override string Name
        {
            get { return null; }
        }

        public override bool IsDirectory
        {
            get { return false; }
        }
    }
}