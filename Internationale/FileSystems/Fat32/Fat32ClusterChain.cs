namespace Internationale.FileSystems.Fat32
{
    public class Fat32ClusterChain
    {
        private readonly int[] _clusterIndexes;

        public int[] ClusterIndexes
        {
            get
            {
                return _clusterIndexes;
            }
        }
        
        public Fat32ClusterChain(int[] clusterIndexes)
        {
            _clusterIndexes = clusterIndexes;
        }
    }
}