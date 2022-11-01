using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;

namespace WpfAppEmotions
{
    public class Collection: INotifyPropertyChanged
    {
        private int _maxValue = 100;
        public int maxValue
        {
            get => _maxValue;
            set
            {
                _maxValue = value;
                OnPropertyChanged("MaxValue");
            }
        }

        private double _progress = 0;
        public double progress
        {
            get => _progress;
            set
            {
                _progress = value;
                OnPropertyChanged("Progress");
            }
        }

        public ObservableCollection<Data> MyList { get; set; } = new ObservableCollection<Data>();

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        //Сортировка списка
        public void quicksort(int first, int last, string mode)
        {
            int f = first, l = last;
            Data mid = new Data
            {
                image_name = this.MyList[(f + l) / 2].image_name
                        .Substring(0, this.MyList[(f + l) / 2].image_name.Length),
                emotions = this.MyList[(f + l) / 2].emotions
                        .GetRange(0, this.MyList[(f + l) / 2].emotions.Count())
            };
            do
            {
                while (Get_mode_value(this.MyList[f], mode) > Get_mode_value(mid, mode)) f++;
                while (Get_mode_value(this.MyList[l], mode) < Get_mode_value(mid, mode)) l--;
                if (f <= l) //перестановка элементов
                {
                    Data tmp = new Data
                    {
                        image_name = this.MyList[f].image_name
                                .Substring(0, this.MyList[f].image_name.Length),
                        emotions = this.MyList[f].emotions
                                .GetRange(0, this.MyList[f].emotions.Count())
                    };
                    this.MyList[f].image_name = this.MyList[l].image_name
                            .Substring(0, this.MyList[l].image_name.Length);
                    this.MyList[f].emotions = this.MyList[l].emotions
                            .GetRange(0, this.MyList[l].emotions.Count());

                    this.MyList[l].image_name = tmp.image_name
                            .Substring(0, tmp.image_name.Length);
                    this.MyList[l].emotions = tmp.emotions
                            .GetRange(0, tmp.emotions.Count());
                    f++;
                    l--;
                }
            } while (f < l);
            if (first < l) quicksort(first, l, mode);
            if (f < last) quicksort(f, last, mode);
        }

        public float Get_mode_value(Data obj, string mode)
        {
            Tuple<string, float> res = obj.emotions.Find(x => x.Item1.Equals(mode));
            return res.Item2;
        }

    }
}
