using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PaniniTracker.Core.Models
{
    public class DuplicateStickerRow
    {
        public string Code { get; set; } = "";
        public int Count { get; set; }
        public int? Page { get; set; }
    }
}
