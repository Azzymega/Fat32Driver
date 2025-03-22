using System;
using System.Collections;
using System.IO;
using System.Text;
using Internationale.FileSystems.Fat32.Writer;

namespace Internationale.FileSystems.Fat32
{
    public class Fat32Writer : IFileSystemWriter
    {
        private const int FatFileEntrySize = 32;
        private const int FatBootBlockSize = 512;
        private const int FatLongFileNameContains = 13;
        
        private readonly BinaryReader _reader;
        private readonly Fat32Reader _fat32Reader;

        public Fat32Writer(Stream stream)
        {
            _fat32Reader = new Fat32Reader(stream);
            _reader = new BinaryReader(stream);
        }

        private Fat32Block[] CreateFileDescriptor(string name, Fat32DescriptorAttributes attributes, DateTime dateTime)
        {
            ArrayList descriptors = new ArrayList();
            StringBuilder builder = new StringBuilder();
            
            int lasted = FatLongFileNameContains-((name.Length+1) % FatLongFileNameContains);
            builder.Append(name);
            builder.Append('\0');
            
            for (int i = 0; i < lasted; i++)
            {
                builder.Append(char.MaxValue);
            }
            name = builder.ToString();
            
            int counter = name.Length / FatLongFileNameContains;
            int offset = 0;
            
            Random random = new Random();
            byte[] unused = new byte[11];
            random.NextBytes(unused);
            
            for (int i = 0; i < counter; i++)
            {
                bool last = i + 1 >= counter;
                Fat32Block fat32Descriptor = new Fat32Block(name,offset,i+1,last,unused);
                descriptors.Add(fat32Descriptor);
                offset += FatLongFileNameContains;
            }
            
            descriptors.Add(new Fat32Block(dateTime, attributes,unused));

            Fat32Block[] blocks = new Fat32Block[descriptors.Count];
            for (int i = 0; i < descriptors.Count; i++)
            {
                blocks[i] = (Fat32Block)descriptors[i];
            }

            return blocks;
        }

        private void WriteFileDescriptor(Fat32Descriptor folder, Fat32Block block)
        {
            Boolean written = false;
            Fat32ExtendedBootRecord record = _fat32Reader.GetExtendedBootRecord();
            Fat32BootRecord boot = _fat32Reader.GetBootRecord();
            Fat32ClusterChain clusterChain = _fat32Reader.GetClusterChain(folder.Cluster);

            foreach (int clusterIndex in clusterChain.ClusterIndexes)
            {
                int fatSize = record.SectorsPerFat;
                int firstDataSector = boot.ReservedSectorCount + (boot.FatCount * fatSize);
                int firstSectorOfCluster = ((clusterIndex - 2) * boot.SectorPerCluster) + firstDataSector;
                int offset = firstSectorOfCluster * boot.BytesPerSector;
                _reader.BaseStream.Seek(offset, SeekOrigin.Begin);
                
                while (true)
                {
                    byte header = _reader.ReadByte();
                    _reader.BaseStream.Position -= 1;
                    if (header == 0)
                    {
                        if (_reader.BaseStream.Position-offset > boot.SectorPerCluster*boot.BytesPerSector)
                        {
                            break;
                        }
                        else
                        {
                            written = true;
                            BinaryWriter writer = new BinaryWriter(_reader.BaseStream);
                            writer.Write(block.ToArray());
                            break;
                        }
                    }
                    else
                    {
                        _reader.BaseStream.Position += 32;
                    }
                }

            }

            if (!written)
            {
                throw new WriteFailedException();
            }
        }

        private bool FindCluster(uint length)
        {
            Fat32ExtendedBootRecord record = _fat32Reader.GetExtendedBootRecord();
            Fat32BootRecord boot = _fat32Reader.GetBootRecord();

            uint sectorCount = (uint)(length / boot.BytesPerSector);
            uint clusterCount = (uint)(sectorCount / boot.SectorPerCluster);
            clusterCount++;
            
            int targetFat = 2 * 4;
            int fatSector = boot.ReservedSectorCount + (targetFat / boot.BytesPerSector);
            int clsOffset = targetFat % boot.BytesPerSector;
            _reader.BaseStream.Seek((fatSector * boot.BytesPerSector) + clsOffset, SeekOrigin.Begin);
            int clusterIndex = 2;

            int clusterStart = 0;
            int freeClusters = 0;
            
            while (true)
            {
                Fat32FatClusterAttributes clusterStatus = (Fat32FatClusterAttributes)_reader.ReadUInt32();
                
                if (clusterStatus == Fat32FatClusterAttributes.Free)
                {
                    freeClusters++;
                    if (freeClusters == clusterCount)
                    {
                        return true;
                        break;
                    }
                }
                else if (clusterStatus >= Fat32FatClusterAttributes.End)
                {
                    clusterStart = clusterIndex;
                    freeClusters++;
                }
                else
                {
                    freeClusters = 0;
                    clusterStart = clusterIndex;
                }
                clusterIndex++;
            }

            return false;
        }

        private void AllocateClustersForFile(ref Fat32Block file, uint length)
        {
            Fat32ExtendedBootRecord record = _fat32Reader.GetExtendedBootRecord();
            Fat32BootRecord boot = _fat32Reader.GetBootRecord();

            file.Size = length;

            uint sectorCount = (uint)(length / boot.BytesPerSector);
            uint clusterCount = (uint)(sectorCount / boot.SectorPerCluster);
            clusterCount++;
            
            int targetFat = 2 * 4;
            int fatSector = boot.ReservedSectorCount + (targetFat / boot.BytesPerSector);
            int clsOffset = targetFat % boot.BytesPerSector;
            _reader.BaseStream.Seek((fatSector * boot.BytesPerSector) + clsOffset, SeekOrigin.Begin);
            int clusterIndex = 2;

            int clusterStart = 0;
            int freeClusters = 0;
            
            while (true)
            {
                Fat32FatClusterAttributes clusterStatus = (Fat32FatClusterAttributes)_reader.ReadUInt32();
                
                if (clusterStatus == Fat32FatClusterAttributes.Free)
                {
                    freeClusters++;
                    if (freeClusters == clusterCount)
                    {
                        file.Cluster = clusterStart+1;
                        for (int i = 0; i < freeClusters; i++)
                        {
                            targetFat = 2 * (clusterStart+1);
                            fatSector = boot.ReservedSectorCount + (targetFat / boot.BytesPerSector);
                            clsOffset = targetFat % boot.BytesPerSector;
                            _reader.BaseStream.Seek((fatSector * boot.BytesPerSector) + clsOffset, SeekOrigin.Begin);
                            BinaryWriter writer = new BinaryWriter(_reader.BaseStream);
                            
                            for (int j = 1; j < freeClusters; j++)
                            {
                                writer.Write(++clusterStart);
                            }
                        }
                        break;
                    }
                }
                else if (clusterStatus >= Fat32FatClusterAttributes.End)
                {
                    clusterStart = clusterIndex;
                    freeClusters++;
                }
                else
                {
                    freeClusters = 0;
                    clusterStart = clusterIndex;
                }
                clusterIndex++;
            }
            
        }
        
        public void WriteFile(string fileName, byte[] values)
        {
            Fat32BootRecord boot = _fat32Reader.GetBootRecord();
            Fat32ExtendedBootRecord record = _fat32Reader.GetExtendedBootRecord();
            
            StringBuilder builder = new StringBuilder();
            ArrayList list = new ArrayList();

            foreach (char c in fileName)
            {
                if (c == '/')
                {
                    list.Add(builder.ToString());
                    builder.Clear();
                }
                else
                {
                    builder.Append(c);
                }
            }

            string fileNameCreation = builder.ToString();
            builder.Clear();
            for (int i = 0; i < list.Count; i++)
            {
                builder.Append(list[i]);
                if (i+1 < list.Count)
                {
                    builder.Append('/');

                }
                else
                {
                    
                }
            }

            string path = builder.ToString();

            Fat32Descriptor folderDescriptor = null;
            if (path.Length == 0)
            {
                folderDescriptor = _fat32Reader.GetRoots()[0];
                folderDescriptor.Cluster = record.RootDirectoryCluster;
            }
            else
            {
                folderDescriptor = _fat32Reader.GetDescriptor(fileName);
            }
            
            
            Fat32Block[] blocks = CreateFileDescriptor(fileNameCreation, Fat32DescriptorAttributes.Archive, DateTime.Now);
            Fat32Block realDesc = blocks[blocks.Length - 1];

            if (!FindCluster((uint)values.Length))
            {
                throw new OutOfStorageException();
            }
            
            AllocateClustersForFile(ref realDesc,(uint)values.Length);
            blocks[blocks.Length - 1] = realDesc;

            for (int i = 0; i < blocks.Length; i++)
            {
                WriteFileDescriptor(folderDescriptor,blocks[i]);
            }
            
            uint clusterSize = (uint)(boot.SectorPerCluster * boot.BytesPerSector);
            Fat32ClusterChain clusterChain = _fat32Reader.GetClusterChain(realDesc.Cluster);
            BinaryWriter writer = new BinaryWriter(_reader.BaseStream);
            MemoryStream memoryStream = new MemoryStream(values);
            BinaryReader reader = new BinaryReader(memoryStream);
            
            foreach (int clusterIndex in clusterChain.ClusterIndexes)
            {
                int fatSize = record.SectorsPerFat;
                int firstDataSector = boot.ReservedSectorCount + (boot.FatCount * fatSize);
                int firstSectorOfCluster = ((clusterIndex - 2) * boot.SectorPerCluster) + firstDataSector;
                int offset = firstSectorOfCluster * boot.BytesPerSector;
                writer.BaseStream.Seek(offset, SeekOrigin.Begin);
                
                if (memoryStream.Length-memoryStream.Position < clusterSize)
                {
                    writer.Write(reader.ReadBytes((int)(memoryStream.Length-memoryStream.Position)));
                    break;
                }
                else
                {
                    writer.Write(reader.ReadBytes((int)clusterSize));
                    continue;
                }
            }
        }

        public void CreateDirectory(string directoryName)
        {
            throw new System.NotImplementedException();
        }
    }
}