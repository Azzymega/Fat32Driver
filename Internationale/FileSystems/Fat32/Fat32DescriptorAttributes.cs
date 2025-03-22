using System;

namespace Internationale.FileSystems.Fat32
{
    [Flags]
    public enum Fat32DescriptorAttributes : byte
    {
        None,
        ReadOnly = 0x01,
        Hidden = 0x02,
        System = 0x04,
        VolumeId = 0x08,
        Directory = 0x10,
        Archive = 0x20,
        Unicode = 0x0F
    }
}