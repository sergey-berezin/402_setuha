using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Microsoft.EntityFrameworkCore;

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

     

        private void Delete_Image_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var image = ImagesCollection[Images_Collection_ListBox.SelectedIndex];
                using (var db = new ImageContext())
                {
                    var deletedImage = db.Images.Where(x => x.Id == image.Id)
                        .Include(x => x.Content).First();
                    if (deletedImage == null)
                    {
                        return;
                    }
                    db.Images_Content.Remove(deletedImage.Content);
                    db.Images.Remove(deletedImage);
                    db.SaveChanges();
                    ImagesCollection.Remove(image);
                }
            }
            catch (Exception e1)
            {
                MessageBox.Show(e1.Message);
            }
        }

    }
}
