using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using EmotionFerPlusLib;
using System.Threading;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp;
using Microsoft.ML.OnnxRuntime.Tensors;
using SixLabors.ImageSharp.Processing;
using System.Collections.ObjectModel;

namespace WpfApp1
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private List<Image<Rgb24>> images_list;//список изображений
        private List<string> images_path;//список путей к изображениям
        public Collection MyCollection { get; set; }// коллекция {имя + emotions} + pbar
        public Collection MyCollection_sort { get; set; }// коллекция {имя + emotions} + pbar

        EmotionFerPlusAsync obj1;

        bool calculation_status = false;
        private CancellationToken token;
        private CancellationTokenSource token_source;




        public MainWindow()
        {
            images_list = new();
            images_path = new();
            MyCollection = new();
            MyCollection_sort = new();
            obj1 = new EmotionFerPlusAsync();
            InitializeComponent();
            DataContext = this;
            token_source = new CancellationTokenSource();
            token = token_source.Token;
        }


        //Выбор эмоции для сортировки
        private string _selected_mode = "neutral";//значение по умолчанию
        public string selected_mode
        {
            get
            {
                return _selected_mode;
            }
            set
            {
                _selected_mode = value;
            }
        }

        private int _selected_index = 0;
        public int  selected_index
        {
            get 
            { 
                return _selected_index; 
            }
            set
            {
                switch(value)
                {

                    case 0:
                        _selected_mode = "neutral";
                        _selected_index = 0;
                        break;
                    case 1:
                        _selected_mode = "happiness";
                        _selected_index = 1;
                        break;
                    case 2:
                        _selected_mode = "surprise";
                        _selected_index = 2;
                        break;
                    case 3:
                        _selected_mode = "sadness";
                        _selected_index = 3;
                        break;
                    case 4:
                        _selected_mode = "anger";
                        _selected_index = 4;
                        break;
                    case 5:
                        _selected_mode = "disgust";
                        _selected_index = 5;
                        break;
                    case 6:
                        _selected_mode = "fear";
                        _selected_index = 6;
                        break;
                    case 7:
                        _selected_mode = "contempt";
                        _selected_index = 7;
                        break;
                    default:
                        break;
                }
                //MessageBox.Show(_selected_mode);
            }
        }

        //Кнопка выбора каталога с изображениями
        private void Open_Images_Click(object sender, RoutedEventArgs e)
        {
            Data_Clear();//очищает все данные
            Microsoft.Win32.OpenFileDialog ofd = new Microsoft.Win32.OpenFileDialog();
            ofd.Multiselect = true;
            ofd.Filter = "Images (*.jpg, *.png)|*.jpg;*.png";
            var projectRootFolder = System.IO.Path.GetFullPath("../../../../../Images");
            ofd.InitialDirectory = projectRootFolder;
            var response = ofd.ShowDialog();
            if (response == true)
                if (response == true)
            {
                foreach (var path in ofd.FileNames)
                {
                    var face = SixLabors.ImageSharp.Image.Load<Rgb24>(path);
                    images_list.Add(face);
                    images_path.Add(path);
                }
            }
        }

        //Кнопка начала вычислений
        private async void Start_Calc_Click(object sender, RoutedEventArgs e)
        {
            if (images_list.Count == 0)
            {
                MessageBox.Show("Выбранный каталог пуст.");
                return;
            }

            if (calculation_status)
            {
                MessageBox.Show("Вычисления уже произведены. Пожалуйста, выберите другие изображения.");
                return;
            }


            pbar_Clear();
            token_source = new CancellationTokenSource();

            for (int i = 0; i < images_list.Count; i++)
            {
                try
                {
                    images_list[i].Mutate(ctx => {
                        ctx.Resize(new SixLabors.ImageSharp.Size(64, 64));
                        // ctx.Grayscale();
                    });
                    DenseTensor<float> image_tensor = GrayscaleImageToTensor(images_list[i]);
                    token = token_source.Token;
                    var r = await obj1.Recognition_func(image_tensor, token);//получение эмоций
                    var res = r.OrderByDescending(t => t.Item2).ToList();

                    Data data_tmp = new Data {
                        image_name = images_path[i],
                        emotions = res 
                    };
                    MyCollection.MyList.Add(data_tmp);
                    MyCollection.progress += 100.0 / images_list.Count();
                    MyCollection_sort.progress += 100.0 / images_list.Count();
                }

                catch (OperationCanceledException e1)
                {
                    Console.WriteLine($"{nameof(OperationCanceledException)} " +
                        $"thrown with message: {e1.Message}");
                }
            }

            //Сортировка изображений по убыванию по выбранной эмоции
            string mode = selected_mode;
            MyCollection.quicksort(0, MyCollection.MyList.Count() - 1, mode);
            foreach (var im in MyCollection.MyList)
            {
                MyCollection_sort.MyList.Add(new Data
                {
                    image_name = im.image_name,
                    emotions = im.emotions
                });
            }
            calculation_status = true;
        }


        //Кнопка остановки вычислений
        private void Stop_Calc_Click(object sender, RoutedEventArgs e)
        {
            token_source.Cancel();
            MessageBox.Show("Вычисления прерваны.");
        }


        //Очистка данных
        private void Data_Clear()
        {
            calculation_status = false;
            token_source = new CancellationTokenSource();
            token = token_source.Token;
            if (images_list.Count == 0) 
                return;
            pbar_Clear();
            images_path.Clear();
            images_list.Clear();
        }

        //Очистка pbar
        private void pbar_Clear()
        {
            MyCollection.MyList.Clear();
            MyCollection.progress = 0;
            MyCollection.maxValue = 100;
        }


        //преобразование изображения в тензор
        public DenseTensor<float> GrayscaleImageToTensor(Image<Rgb24> img)
        {
            var w = img.Width;
            var h = img.Height;
            var t = new DenseTensor<float>(new[] { 1, 1, h, w });

            img.ProcessPixelRows(pa =>
            {
                for (int y = 0; y < h; y++)
                {
                    Span<Rgb24> pixelSpan = pa.GetRowSpan(y);
                    for (int x = 0; x < w; x++)
                    {
                        t[0, 0, y, x] = pixelSpan[x].R; // B and G are the same
                    }
                }
            });

            return t;
        }

    }
}
