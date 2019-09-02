using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace ScanIP
{
   public class ClassMyIP : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged = delegate { };
        public void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            // Raise the PropertyChanged event, passing the name of the property whose value has changed.
            this.PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }
        string myIp = "127.1.1.1.";
        public string MyIp
        {
            get
            {
                return myIp;
            }
            set
            {
                myIp = value;
                OnPropertyChanged();
            }
        }
        
        string mytipInt = "IP4";
        public string MytipInt
        {
            get
            {
                return mytipInt;
            }
            set
            {
                mytipInt = value;
                OnPropertyChanged();
            }
        }
        
    }
}
