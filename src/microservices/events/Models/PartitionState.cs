namespace events.Models
{
    public class PartitionState
    {
        public object Gate { get; } = new();
        public long NextToCommit { get; set; }
        public HashSet<long> Completed { get; } = new();
    }
}
