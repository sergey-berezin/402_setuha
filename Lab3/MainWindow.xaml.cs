using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Windows;
using EmotionFerPlusLib;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp;
using Microsoft.ML.OnnxRuntime.Tensors;
using SixLabors.ImageSharp.Processing;
using Microsoft.EntityFrameworkCore;
using System.Text;
using System.Collections;
using System.Collections.ObjectModel;

namespace WPFDataBase
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private List<Image<Rgb24>> images_list;//список изображений
        private List<string> images_path;//список путей к изображениям
        public Collection MyCollection { get; set; }// коллекция {имя + emotions} + pbar

        EmotionFerPlusAsync obj1;


        bool calculation_status = false;
        private CancellationToken token;
        private CancellationTokenSource token_source;




        public MainWindow()
        {
            images_list = new();
            images_path = new();
            MyCollection = new();
            obj1 = new EmotionFerPlusAsync();
            InitializeComponent();
            DataContext = this;
            token_source = new CancellationTokenSource();
            token = token_source.Token;
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

            pbar_Clear();
            token_source = new CancellationTokenSource();

            Open_Button.IsEnabled = false;
            Start_Button.IsEnabled = false;



            for (int i = 0; i < images_list.Count; i++)
            {
                try
                {
                    //Ищем картинку в уже сохраненных
                    Image New_im = null;
                    using (var db = new ImageContext())
                    {
                        string hash = Image.GetHashCode(System.IO.File.ReadAllBytes(images_path[i]));
                        var q = db.Images.Where(x => x.Image_Hash == hash)//если хэш не совпал, содержимое картинки не подругажется
                            .Include(x => x.Content)
                            .Where(x => Equals(x.Content.Image_Data, System.IO.File.ReadAllBytes(images_path[i])));
                        if (q.Any())
                        {
                            New_im = q.First();
                        }
                    }

                    //Если не нашли ее в БД, то делаем вычисления и сохраняем
                    if (New_im == null)
                    {
                        images_list[i].Mutate(ctx => {
                            ctx.Resize(new SixLabors.ImageSharp.Size(64, 64));
                        });
                        DenseTensor<float> image_tensor = GrayscaleImageToTensor(images_list[i]);
                        token = token_source.Token;
                        var r = await obj1.Recognition_func(image_tensor, token);//получение эмоций
                        var res = r.OrderByDescending(t => t.Item2).ToList();

                        Data data_tmp = new Data
                        {
                            image_name = images_path[i],
                            emotions = res.Select(t => new Tuple<string, float>(t.Item1, t.Item2)).ToList()
                        };
                        MyCollection.MyList.Add(data_tmp);
                        MyCollection.progress += 100.0 / images_list.Count();

                        //Сохранение в БД
                        using (var db = new ImageContext())
                        {
                            var newImageContent = new ImageContent{ 
                                Image_Data = System.IO.File.ReadAllBytes(images_path[i])
                            };

                            Image newImage = new Image
                            {
                                Image_Name = images_path[i],
                                Image_Hash = Image.GetHashCode(System.IO.File.ReadAllBytes(images_path[i])),
                                Content = newImageContent,
                                Emotions = new ObservableCollection<Emotion>()
                            };

                            foreach (var item in res)
                            {
                                Emotion new_emotion = new Emotion
                                {
                                    emotion_name = item.Item1,
                                    emotion_val = item.Item2
                                };
                                newImage.Emotions.Add(new_emotion);
                            }
                            db.Add(newImage);
                            db.SaveChanges();
                        }

                    }
                }

                catch (OperationCanceledException e1)
                {
                    Console.WriteLine($"{nameof(OperationCanceledException)} " +
                        $"thrown with message: {e1.Message}");
                }
            }

            calculation_status = true;
            Open_Button.IsEnabled = true;
            Start_Button.IsEnabled = true;
        }


        //Кнопка остановки вычислений
        private void Stop_Calc_Click(object sender, RoutedEventArgs e)
        {
            token_source.Cancel();
            MessageBox.Show("Вычисления прерваны.");
        }


        //Кнопка открывает хранилище с базой данных
        private void Open_Storage_Click(object sender, RoutedEventArgs e)
        {
            DataBaseWindow databasewindow = new();
            databasewindow.ShowDialog();
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
