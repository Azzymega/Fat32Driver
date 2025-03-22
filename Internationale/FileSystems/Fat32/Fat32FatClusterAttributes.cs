namespace Internationale.FileSystems.Fat32
{
    public enum Fat32FatClusterAttributes : uint
    {
        Free = 0,
        Allocated = 0x00000002,
        Reserved = 0xFFFFFFF6,
        Bad = 0xFFFFFFF7,
        End = 0xFFFFFFFF
    }
}