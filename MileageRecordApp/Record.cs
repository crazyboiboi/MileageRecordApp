using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MileageRecordApp
{
    class Record : IComparable<Record>
    {
        public DateTime date { get; set; } 

        public string day { get; set; }

        public uint startDistance { get; set; }

        public uint endDistance { get; set; }

        public uint totalDistance { get; set; }

        public string locationTravelled { get; set; }

        public string remark { get; set; }

        public override string ToString()
        {
            return "Record dated on " + date + ". Total distance travelled: " +
                totalDistance;
        }


        public int CompareTo(Record other)
        {
            return date.CompareTo(other.date);
        }

    }
}
