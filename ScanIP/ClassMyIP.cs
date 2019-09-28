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
        string myName = "Name";
        public string MyName
        {
            get
            {
                return myName;
            }
            set
            {
                myName = value;
                OnPropertyChanged();
            }
        }
        string myIp4 = "127.1.1.1.";
        public string MyIp4
        {
            get
            {
                return myIp4;
            }
            set
            {
                myIp4 = value;
                OnPropertyChanged();
            }
        }
        string myIp6;
        public string MyIp6
        {
            get
            {
                return myIp6;
            }
            set
            {
                myIp6 = value;
                OnPropertyChanged();
            }
        }
        string mytipInt4 = "IP4";
        public string MytipInt4
        {
            get
            {
                return mytipInt4;
            }
            set
            {
                mytipInt4 = value;
                OnPropertyChanged();
            }
        }
        public bool Activ = false;
        
    }
}
