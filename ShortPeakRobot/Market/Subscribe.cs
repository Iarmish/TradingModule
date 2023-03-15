using ShortPeakRobot.Market.Models;
using System.Collections.Generic;

namespace ShortPeakRobot.Market
{
    public class Subscribe
    {
        public List<int> SignedrobotIds { get; set; } = new List<int>();
        public Dictionary<int, List<int>> Signedrobots { get; set; } = new Dictionary<int, List<int>>();
        
    }
}
