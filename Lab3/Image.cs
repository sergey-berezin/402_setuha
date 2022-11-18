using Microsoft.EntityFrameworkCore;
using System.Windows.Input;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Security.Cryptography;
using System.Linq;
using System.Collections.ObjectModel;
using System.Windows.Documents;
using System;
using System.Collections.Generic;

namespace WPFDataBase
{
    //1-я сущность
    public class Image
    {
        [Key]
        public int Id { get; set; }                // primary key
        public string Image_Name { get; set; }     // имя картинки
        public string Image_Hash {get; set;}       // ее хэш-код


        public ObservableCollection<Emotion> Emotions { get; set; } // список <эмоция + значение>
        public ImageContent Content { get; set; }          // содержимое картинки

        public static string GetHashCode(byte[] seq)
        {
            SHA256 shaM = SHA256.Create();
            return string.Concat(shaM.ComputeHash(seq).Select(x => x.ToString("X2")));
        }

    }

    //2-я сущность - содержимое изображения
    public class ImageContent
    {
        [Key]
        [ForeignKey(nameof(Image))]
        public int Id { get; set; }
        public byte[] Image_Data { get; set; } // содержимое картинки в байтах
    }


    //3-я сущность - эмоция и ее значение
    public class Emotion
    {
        [Key]
        [ForeignKey(nameof(Image))]
        public int emotionID { get; set; }
        public string emotion_name { get; set; }
        public float emotion_val { get; set; }
        public override string ToString()
        {
            return emotion_name + ": " + String.Format("{0:0.000}", emotion_val) + "\n";
        }
    }




    //Связь с БД
    public class ImageContext : DbContext
    {
        public ImageContext() => Database.EnsureCreated();//создание БД

        public DbSet<Image> Images { get; set; } //объекты бд
        public DbSet<ImageContent> Images_Content { get; set; }
        public DbSet<Emotion> Image_emotions { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlite("Data Source=images_emotions_2.db");//имя бд
        }
    }
}
