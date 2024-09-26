using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Marking_TZ.Models
{
    public class Bottle
    {
        public int Id { get; set; }
        public string Code { get; set; }
        public int BoxId { get; set; }
        public Box Box { get; set; }
    }
}
