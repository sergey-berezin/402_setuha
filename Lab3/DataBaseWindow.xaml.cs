using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Windows;

namespace WPFDataBase
{
    /// <summary>
    /// Логика взаимодействия для DataBaseWindow.xaml
    /// </summary>
    public partial class DataBaseWindow : Window
    {
        public ObservableCollection<Image> ImagesCollection { get; set; }

        public DataBaseWindow()
        {
            ImagesCollection = new();

            using (var db = new ImageContext())
            {
                foreach (var image in db.Images)
                {
                    ImagesCollection.Add(image);
                }
            }

            InitializeComponent();
            DataContext = this;
        }


        //Атомарное удаление
        private SemaphoreSlim smp = new SemaphoreSlim(1, 1);
        private async void Delete_Image_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var image = ImagesCollection[Images_Collection_ListBox.SelectedIndex];

                if (image == null)
                {
                    return;
                }

                await smp.WaitAsync();
                using (var db = new ImageContext())
                {
                    var deletedImage = db.Images.Where(x => Equals(x.ImageId, image.ImageId))
                            .FirstOrDefault();

                    if (deletedImage == null)
                    {
                        return;
                    }


                    db.Images.Remove(deletedImage);
                    db.SaveChanges();
                    ImagesCollection.Remove(image);
                }
                smp.Release();
            }
            catch (Exception e1)
            {
                MessageBox.Show(e1.Message);
            }
        }

    }
}
