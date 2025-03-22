using System.IO;

namespace Internationale.FileSystems.Fat32
{
    public class Fat32BootRecord : Fat32Element
    {
        private short _sectorPerCluster;
        private short _bytesPerSector;
        private short _reservedSectorCount;
        private short _fatCount;
        private ulong _sectorCount;
        private ulong _partitionStart;

        public override bool IsDirectory
        {
            get { return false; }
        }

        public Fat32BootRecord(BinaryReader reader)
        {
            reader.BaseStream.Position += 3;
            reader.BaseStream.Position += 8;

            _bytesPerSector = reader.ReadInt16();
            _sectorPerCluster = reader.ReadByte();
            _reservedSectorCount = reader.ReadInt16();
            _fatCount = reader.ReadByte();

            reader.BaseStream.Position += 2;

            short sectorCount = reader.ReadInt16();
            if (sectorCount != 0)
            {
                _sectorCount = (ulong)sectorCount;
            }

            reader.BaseStream.Position += 1;
            reader.BaseStream.Position += 2;
            reader.BaseStream.Position += 2;
            reader.BaseStream.Position += 2;

            _partitionStart = (ulong)reader.ReadInt32();
            _sectorCount = (ulong)reader.ReadInt32();
        }
        
        public ulong VolumeSize
        {
            get { return _sectorCount * (ulong)_bytesPerSector; }
        }

        public ulong SectorsCount
        {
            get { return _sectorCount; }
        }

        public short SectorPerCluster
        {
            get { return _sectorPerCluster; }
        }

        public short BytesPerSector
        {
            get { return _bytesPerSector; }
        }

        public short ReservedSectorCount
        {
            get { return _reservedSectorCount; }
        }

        public short FatCount
        {
            get { return _fatCount; }
        }

        public override string Name
        {
            get { return null; }
        }
    }
}