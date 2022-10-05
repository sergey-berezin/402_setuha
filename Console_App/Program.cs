using System;
using SixLabors.ImageSharp; // Из одноимённого пакета NuGet
using SixLabors.ImageSharp.PixelFormats;
using System.Linq;
using SixLabors.ImageSharp.Processing;
using Microsoft.ML.OnnxRuntime.Tensors;
using Microsoft.ML.OnnxRuntime;
using System.Collections.Generic;
using System.IO;
using Lib1;



//Загрузка картинки
using Image<Rgb24> image = Image.Load<Rgb24>("face1.png");
image.Mutate(ctx => {
    ctx.Resize(new Size(64, 64));
    // ctx.Grayscale();
});

//Перевод картинки в тензор
DenseTensor<float> image_tensor = GrayscaleImageToTensor(image);


using EmotionFerPlusAsync emotionFerPlus = new();
var r = await emotionFerPlus.Recognition_func(image_tensor, CancellationToken.None);

foreach (var item in r)
{
    Console.WriteLine(item.Item1 + " " + item.Item2.ToString());
}




DenseTensor<float> GrayscaleImageToTensor(Image<Rgb24> img)
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
