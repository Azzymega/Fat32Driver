using System.Collections;
using System.IO;
using System.Text;

namespace Internationale.FileSystems.Fat32
{
    public class Fat32Reader : IFileSystemReader
    {
        private const int FatFileEntrySize = 32;
        private const int FatBootBlockSize = 512;
        private readonly BinaryReader _reader;

        public Fat32Reader(Stream stream)
        {
            _reader = new BinaryReader(stream);
        }
        
        public byte[] ReadFile(string fileName)
        {
            Fat32BootRecord boot = GetBootRecord();
            Fat32ExtendedBootRecord record = GetExtendedBootRecord();
            Fat32Descriptor fat32Descriptor = GetDescriptor(fileName);
            Fat32ClusterChain clusterChain = GetClusterChain(fat32Descriptor.Cluster);
            using (MemoryStream stream = new MemoryStream())
            {
                BinaryWriter writer = new BinaryWriter(stream);

                uint clusterSize = (uint)(boot.SectorPerCluster * boot.BytesPerSector);
                uint size = (uint)fat32Descriptor.Size;
                uint remainSize = size;
            
                foreach (int clusterIndex in clusterChain.ClusterIndexes)
                {
                    int fatSize = record.SectorsPerFat;
                    int firstDataSector = boot.ReservedSectorCount + (boot.FatCount * fatSize);
                    int firstSectorOfCluster = ((clusterIndex - 2) * boot.SectorPerCluster) + firstDataSector;
                    int offset = firstSectorOfCluster * boot.BytesPerSector;
                    _reader.BaseStream.Seek(offset, SeekOrigin.Begin);
                    if (remainSize > clusterSize)
                    {
                        writer.Write(_reader.ReadBytes((int)clusterSize));
                        remainSize -= clusterSize;
                    }
                    else
                    {
                        writer.Write(_reader.ReadBytes((int)remainSize));
                    }
                }
            
                return stream.ToArray();
            }
        }

        public Fat32ClusterChain GetClusterChain(int firstCluster)
        {
            Fat32BootRecord boot = GetBootRecord();

            ArrayList list = new ArrayList();
            list.Add(firstCluster);

            int targetFat = firstCluster * 4;
            int fatSector = boot.ReservedSectorCount + (targetFat / boot.BytesPerSector);
            int clsOffset = targetFat % boot.BytesPerSector;
            _reader.BaseStream.Seek((fatSector * boot.BytesPerSector) + clsOffset, SeekOrigin.Begin);

            while (true)
            {
                int chainValue = _reader.ReadInt32();
                if (chainValue >= 0x0FFFFFF8)
                {
                    break;
                }
                else
                {
                    list.Add(chainValue);
                }
            }

            int[] target = new int[list.Count];

            for (int i = 0; i < list.Count; i++)
            {
                target[i] = (int)list[i];
            }

            return new Fat32ClusterChain(target);
        }

        public Fat32Descriptor[] GetDirectories(int cluster)
        {
            ArrayList list = new ArrayList();

            Fat32ExtendedBootRecord record = GetExtendedBootRecord();
            Fat32BootRecord boot = GetBootRecord();
            Fat32ClusterChain clusterChain = GetClusterChain(cluster);

            foreach (int chainClusterIndex in clusterChain.ClusterIndexes)
            {
                int fatSize = record.SectorsPerFat;
                int firstDataSector = boot.ReservedSectorCount + (boot.FatCount * fatSize);
                int firstSectorOfCluster = ((chainClusterIndex - 2) * boot.SectorPerCluster) + firstDataSector;
                int offset = firstSectorOfCluster * boot.BytesPerSector;

                _reader.BaseStream.Seek(offset, SeekOrigin.Begin);
                byte[] buffer = _reader.ReadBytes(boot.SectorPerCluster * boot.BytesPerSector);

                MemoryStream stream = new MemoryStream(buffer);
                BinaryReader reader = new BinaryReader(stream);
                
                StringBuilder builder = new StringBuilder();

                while (true)
                {
                    byte header = reader.ReadByte();
                    reader.BaseStream.Position -= 1;
                    if (header == 0)
                    {
                        break;
                    }
                    else if (header == 0xE5)
                    {
                        reader.BaseStream.Position += FatFileEntrySize;
                    }
                    else
                    {
                        Fat32Descriptor fat32Descriptor = new Fat32Descriptor(reader);
                        if (fat32Descriptor.IsLongFileName)
                        {
                            builder.Append(fat32Descriptor.Name);
                        }
                        else
                        {
                            if (builder.Length > 0)
                            {
                                fat32Descriptor.SetLongName(builder.ToString());
                                builder.Clear();
                            }

                            list.Add(fat32Descriptor);
                        }
                    }
                }
            }

            Fat32Descriptor[] fat32Descriptors = new Fat32Descriptor[list.Count];
            for (int i = 0; i < list.Count; i++)
            {
                fat32Descriptors[i] = (Fat32Descriptor)list[i];
            }

            return fat32Descriptors;
        }

        public Fat32Descriptor[] GetRoots()
        {
            Fat32ExtendedBootRecord record = GetExtendedBootRecord();
            return GetDirectories(record.RootDirectoryCluster);
        }

        public Fat32Descriptor GetDescriptor(string name)
        {
            ArrayList strings = new ArrayList();
            StringBuilder builder = new StringBuilder();
            
            for (int i = 0; i < name.Length; i++)
            {
                if (name[i] == '/')
                {
                    if (builder.Length == 0)
                    {
                        throw new BadDirectoryException();
                    }
                    strings.Add(builder.ToString());
                    builder.Clear();
                }
                else
                {
                    builder.Append(name[i]);
                }
            }

            if (builder.Length != 0)
            {
                strings.Add(builder.ToString());
                builder.Clear();
            }

            Fat32Descriptor[] fat32Descriptors = GetRoots();
            Fat32Descriptor target = null;
            
            for (int i = 0; i < strings.Count; i++)
            {
                string value = (string)strings[i];
                foreach (Fat32Descriptor fat32Descriptor in fat32Descriptors)
                {
                    if (fat32Descriptor.Name == value)
                    {
                        target = fat32Descriptor;
                        if (fat32Descriptor.IsDirectory)
                        {
                            fat32Descriptors = GetDirectories(target.Cluster);                            
                        }
                    }
                }
            }

            if (target == null)
            {
                throw new FileNotFoundException();
            }
            
            return target;
        }

        public Fat32BootRecord GetBootRecord()
        {
            _reader.BaseStream.Seek(0, SeekOrigin.Begin);
            byte[] beginning = _reader.ReadBytes(FatBootBlockSize);
            using (MemoryStream memoryStream = new MemoryStream(beginning))
            {
                Fat32BootRecord record;
                using (BinaryReader reader = new BinaryReader(memoryStream))
                {
                    record = new Fat32BootRecord(reader);
                }

                return record;
            }
        }

        public Fat32ExtendedBootRecord GetExtendedBootRecord()
        {
            _reader.BaseStream.Seek(36, SeekOrigin.Begin);
            byte[] beginning = _reader.ReadBytes(FatBootBlockSize);
            using (MemoryStream memoryStream = new MemoryStream(beginning))
            {
                Fat32ExtendedBootRecord record;
                using (BinaryReader reader = new BinaryReader(new MemoryStream(beginning)))
                {
                    record = new Fat32ExtendedBootRecord(reader);
                }

                return record;                
            }
        }
    }
}