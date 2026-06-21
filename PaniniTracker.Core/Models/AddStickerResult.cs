using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PaniniTracker.Core.Models
{
    public class AddStickerResult
    {
        public int Added { get; set; }
        public int Duplicates { get; set; }
        public int Unknown { get; set; }
    }
}
