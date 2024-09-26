using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Marking_TZ.Models
{
    public class Box
    {
        public int Id { get; set; }
        public string Code { get; set; }
        public int PalletId { get; set; }
        public Pallet Pallet { get; set; }
        public ICollection<Bottle> Bottles { get; set; } = new List<Bottle>();
    }
}
