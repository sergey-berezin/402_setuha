using System;
using SixLabors.ImageSharp; // Из одноимённого пакета NuGet
using SixLabors.ImageSharp.PixelFormats;
using System.Threading;
using System.Linq;
using SixLabors.ImageSharp.Processing;
using Microsoft.ML.OnnxRuntime.Tensors;
using Microsoft.ML.OnnxRuntime;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using static System.Net.Mime.MediaTypeNames;
using static System.Collections.Specialized.BitVector32;
using System.Threading.Tasks;


namespace EmotionFerPlusLib
{
    public class EmotionFerPlusAsync: IDisposable   
    {
        private InferenceSession _session;
        private bool _session_dispose = false;

//============================Конструктор=======================================
        public EmotionFerPlusAsync()
        {
            using var modelStream = typeof(EmotionFerPlusAsync).Assembly
                .GetManifestResourceStream("EmotionFerPlusLib.emotion-ferplus-7.onnx");
            using var memoryStream = new MemoryStream();
            modelStream.CopyTo(memoryStream);
            this._session = new InferenceSession(memoryStream.ToArray());
        }


        //===============================Функция распознавания эмоций====================
        public async Task<List<(string, float)>> Recognition_func
                       (DenseTensor<float> input, CancellationToken token)
        {
            //Проверка на удаление сессии(чистка памяти)
            if (_session_dispose)
            {
                throw new ObjectDisposedException("session is disposed");
            }
            //Проверка на внешнее прерывание
            token.ThrowIfCancellationRequested();
            return await Task<List<(string, float)>>.Factory.StartNew(() =>
            {
                //Проверка на внешнее прерывание
                token.ThrowIfCancellationRequested();
                IDisposableReadOnlyCollection<DisposableNamedOnnxValue> results;
                //Предобработка тензора
                var inputs = new List<NamedOnnxValue>
                {
                    NamedOnnxValue.CreateFromTensor("Input3", input)
                };
                //Запуск сессии
                lock (_session)
                {
                    results = _session.Run(inputs);
                }
                //Формирование результата
                var emotions = Softmax(results.First(v =>
                            v.Name == "Plus692_Output_0").AsEnumerable<float>().ToArray());

                List<Tuple<string, float>> result = new();
                string[] keys = { "neutral", "happiness", "surprise", "sadness",
                        "anger", "disgust", "fear", "contempt" };
                return keys.Zip(emotions).Select(e => (e.First, e.Second)).ToList();
            }, token, TaskCreationOptions.LongRunning, TaskScheduler.Default);
        }



        public void Dispose()
        {
            _session.Dispose();
            _session_dispose = true;
        }


//=======================Обычный несинхронный метод==============================
        public static float[] Softmax(float[] z)
        {
            var exps = z.Select(x => Math.Exp(x)).ToArray();
            var sum = exps.Sum();
            return exps.Select(x => (float)(x / sum)).ToArray();
        }
    }
}