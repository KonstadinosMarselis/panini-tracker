using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PaniniTracker.Core.Models
{
    public class SaveData
    {
        public List<string> All { get; set; } = new();

        public List<string> Owned { get; set; } = new();

        public List<string> Ignored { get; set; } = new();

        public Dictionary<string, int> Pages { get; set; } = new();

        public Dictionary<string, int> Duplicates { get; set; } = new();
    }
}
