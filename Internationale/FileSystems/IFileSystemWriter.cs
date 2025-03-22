namespace Internationale.FileSystems
{
    public interface IFileSystemWriter
    {
        void WriteFile(string fileName, byte[] values);
        void CreateDirectory(string directoryName);
    }
}