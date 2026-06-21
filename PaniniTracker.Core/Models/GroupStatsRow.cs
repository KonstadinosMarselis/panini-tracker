using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PaniniTracker.Core.Models
{
    public class GroupStatsRow
    {
        public string Group { get; set; } = "";
        public int Owned { get; set; }
        public int Total { get; set; }
        public int Missing { get; set; }
        public double CompletionPercentage { get; set; }
    }
}
