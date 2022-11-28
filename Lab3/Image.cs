using Microsoft.EntityFrameworkCore;
using System.Windows.Input;
using System.ComponentModel.DataAnnotations;
using System.Security.Cryptography;
using System.Linq;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace WPFDataBase
{
    //1-я сущность
    public class Image
    {
        [Key]
        public int ImageId { get; set; }                // primary key
        public string Image_Name { get; set; }     // имя картинки
        public string Image_Hash {get; set;}       // ее хэш-код


        public ICollection<Emotion> Emotions { get; set; } = new List<Emotion>();// список <эмоция + значение>
        public ImageContent Content { get; set; }          // содержимое картинки
        
        
        public string Emotions_in_one_string { get; set; }

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
        public int ImageId { get; set; } //внешний ключ, а также первичный для этой сущности
        public byte[] Image_Data { get; set; } // содержимое картинки в байтах
        public Image im { get; set; }
    }


    //3-я сущность - эмоция и ее значение
    public class Emotion
    {
        public int EmotionId { get; set; } // первичный ключ
        public string emotion_name { get; set; }
        public float emotion_val { get; set; }

        public int ImageId { get; set; } //внешний ключ
        public Image im { get; set;}
        public override string ToString()
        {
            return emotion_name + ": " + String.Format("{0:0.0000000}", emotion_val) + "\n";
        }
    }




    //Связь с БД
    public class ImageContext : DbContext
    {
        public DbSet<Image> Images { get; set; } //объекты бд
        public DbSet<ImageContent> Images_Content { get; set; }
        public DbSet<Emotion> Image_emotions { get; set; }


        public ImageContext() => Database.EnsureCreated();//создание БД

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlite("Data Source=images_emotions_4.db");//имя бд
        }
    }
}
