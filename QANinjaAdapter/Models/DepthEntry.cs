namespace QANinjaAdapter.Models
{
    /// <summary>
    /// Represents a single entry (bid or ask) in the market depth.
    /// This class was originally an inner class in Connector.cs.
    /// </summary>
    public class DepthEntry
    {
        public long Quantity { get; set; }
        public double Price { get; set; }
        public int Orders { get; set; } // Number of orders at this price level
    }
}
