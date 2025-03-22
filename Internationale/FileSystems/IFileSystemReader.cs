namespace Internationale.FileSystems
{
    public interface IFileSystemReader
    { 
        byte[] ReadFile(string fileName);
    }
}