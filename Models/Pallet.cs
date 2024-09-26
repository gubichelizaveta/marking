using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Marking_TZ.Models
{
    public class Pallet
    {
        public int Id { get; set; }
        public string Code { get; set; }

        public ICollection<Box> Boxes { get; set; } = new List<Box>();
    }
}
