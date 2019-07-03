using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MileageRecordApp
{
    class Record : IComparable<Record>, INotifyPropertyChanged
    {
        public DateTime date { get; set; } 

        public string day { get; set; }

        public uint startDistance { get; set; }

        public uint endDistance { get; set; }

        public uint totalDistance { get; set; }

        public string locationTravelled { get; set; }

        

        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged (string info)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(info));
        }

        public string remark { get; set; }

        public string Remark
        {
            get
            {
                return remark;
            }
            set
            {
                if (value != remark)
                {
                    remark = value;
                    NotifyPropertyChanged("Remark");
                }
            }
        }





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
