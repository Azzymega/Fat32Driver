
namespace Internationale.FileSystems.Fat32
{
    public abstract class Fat32Element
    {
        public abstract string Name { get; }
        public abstract bool IsDirectory { get; }
    }
}