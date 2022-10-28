using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace WpfApp1
{
    public class Data: INotifyPropertyChanged
    {
        private string _image_name;
        public string image_name
        {
            get => _image_name;
            set
            {
                _image_name = value;
                OnPropertyChanged("ImageName");
            }
        }

        private List<Tuple<string, float>> _emotions;
        public List<Tuple<string, float>> emotions { 
            get => _emotions; 
            set
            {
                _emotions = value;
                OnPropertyChanged("Emotions");
            }
        }


        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

    }
}
