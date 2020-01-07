using Microsoft.Win32;
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Media.Imaging;
using System.Security;
using System.Diagnostics;



// Pomysł: Użyć OpenGL żeby renderować postęp na żywo
namespace BalansKolorow
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        // Uchwyt do kontrolki z widokiem na obraz
        System.Windows.Controls.Image imageView;
        // Uchwyt do kontrolki z wyborem ilości wątków
        System.Windows.Controls.Slider sliderThreads;
        // Uchwyt do kontrolki ze ścieżka do bitmapy
        System.Windows.Controls.TextBox bitmapPathTextBox;

        // Ścieżka do pliku
        string bitmapFilename;
        // Wskaźnik do tablicy bajtów bitmapy
        byte[] bitmapByteArray;
        // szerokość bitmapy
        int width;
        // wysokość bitmapy
        int height;
        // stoper
        Stopwatch stopwatch;
    

        [DllImport("ja_cpp.dll", EntryPoint = "AdditionBitmapColorBalancer")]
        public extern static unsafe void CPPAdditionBalance(byte* bitmap, int size, byte* argb);

        [DllImport("ja_cpp.dll", EntryPoint = "MultiplicationColorBalancer")]
        public extern static unsafe void CPPMultiplicationBalance(byte* bitmap, int size, float* argb);

        [DllImport("ja_asm.dll", CallingConvention = CallingConvention.StdCall, EntryPoint = "FileASM")]
        public extern static IntPtr FileASM();

        // 
        public MainWindow()
        {
            InitializeComponent();
            imageView = (System.Windows.Controls.Image)this.FindName("ImageView");
            sliderThreads = (System.Windows.Controls.Slider)this.FindName("SliderAmountOfThreads");
            bitmapPathTextBox = (System.Windows.Controls.TextBox)this.FindName("BitmapUriTextBox");
        }
        // Obsługa przycisku "Wybierz bitmapę"
        private void LoadBitmap(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "Bitmap|*.bmp";
            openFileDialog.FilterIndex = 1;
            if(openFileDialog.ShowDialog() == true)
            {
                // Odbierz nazwę pliku
                bitmapFilename = openFileDialog.FileName;
                // Ustaw ścieżkę pliku w polu tekstowym
                bitmapPathTextBox.Text = bitmapFilename;
                // Wczytaj bitmapę z pliku i przekonwertuj na BGRA32
                var bitmap = ConvertBitmapToBgra32(new Bitmap(bitmapFilename));
                // Wyświetl bitmapę w kontrolce
                imageView.Source = Convert(bitmap);
                // Przekonwertuj bitmapę do tablicy bajtów
                bitmapByteArray = ImageToBytes(bitmap);
            }
        }

        private void SaveBitmap(Bitmap bitmap)
        {
            bitmap.Save("save.bmp");
        }

        // Zamienia format bitmapy na BGRA(4 bajtowy)
        public Bitmap ConvertBitmapToBgra32(Bitmap bitmap)
        {
            // Sprawdz czy obraz nie jest już w dobrym formacie
            if (bitmap.PixelFormat == System.Drawing.Imaging.PixelFormat.Format32bppArgb)
            {
                return bitmap; // Jest Ok, nic nie zmieniaj
            }
            else
            {
                // Utwórz bitmapę wyjściową w poprawnym formacie
                Bitmap reformattedBitmap = new Bitmap(bitmap.Width, bitmap.Height,
                    System.Drawing.Imaging.PixelFormat.Format32bppArgb);

                // Przekonwetuj na grafikę
                using (System.Drawing.Graphics graphics = System.Drawing.Graphics.FromImage(reformattedBitmap))
                {
                    // Zapisz na nowo obraz
                    graphics.DrawImage(bitmap, new Rectangle(0, 0, reformattedBitmap.Width, reformattedBitmap.Height));
                }

                // Zwróć przeformatowany obraz
                return reformattedBitmap;
            }
        }
        // Zczytuje bitmapę i zapisuje jako tablicę bajtów
        // Zwraca wskaźnik na tą tablicę
        public byte[] ImageToBytes(Bitmap bitmap)
        {
            byte[] imageBytes;
            // Pobierz szerokość bitmapy
            width = bitmap.Width;
            // Pobierz wysokość bitmapy
            height = bitmap.Height;
            // Utwórz strukturę mówiącą jak wyskalować bitmapę
            // W tym przypadku nie skaluj
            var rect = new Rectangle(0, 0, bitmap.Width, bitmap.Height);
            // "Przytrzymaj" bitmapę w pamięci w trybie R/W oraz w formacie BGRA (little endian)
            BitmapData bitmapInArgb = bitmap.LockBits(rect,ImageLockMode.ReadWrite,PixelFormat.Format32bppArgb);
            // Wielkość tablicy w pixelach * rozmiar pixela
            int sizeOfTheArray = width * height * 4;
            // Alokacja pamięci
            imageBytes = new byte[sizeOfTheArray];
            // Skopiuj bitmapę do tablicy bajtów
            Marshal.Copy(bitmapInArgb.Scan0, imageBytes, 0 , sizeOfTheArray);
            // Uwolnij bitmapę
            bitmap.UnlockBits(bitmapInArgb);

            return imageBytes;
        }

        public Bitmap BytesToBitmap(byte[] bytes)
        {
            Bitmap bitmap = new Bitmap(width, height, PixelFormat.Format32bppArgb);
            var rect = new Rectangle(0, 0, bitmap.Width, bitmap.Height);
            BitmapData bitmapInArgb = bitmap.LockBits(rect, ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb);
            Marshal.Copy(bitmapByteArray, 0, bitmapInArgb.Scan0, bitmapByteArray.Length);
            bitmap.UnlockBits(bitmapInArgb);

            return bitmap;
        }

        // Zamień bitmapę na źródło obrazu dla kontrolki
        public BitmapImage Convert(Bitmap src)
        {
            // Utwórz strumień pamięci
            MemoryStream ms = new MemoryStream();
            // Zapisz bitmapę do strumienia pamięci
            src.Save(ms, System.Drawing.Imaging.ImageFormat.Bmp);
            // Utwórz nowe źródło obrazu dla konrolki
            BitmapImage image = new BitmapImage();
            // Rozpocznij inicjalizację obrazu
            image.BeginInit();
            // Każ strumieniowi zaczynać od początku bitmapy
            ms.Seek(0, SeekOrigin.Begin);
            // Przypisz strumień do bitmapy
            image.StreamSource = ms;
            // Zakończ inicjalizację obrazu
            image.EndInit();
            // Zwróć źródło
            return image;
        }

        // TODO
        private void goVisual() { Console.WriteLine("TODO"); }


        private void DebugInfo()
        {
            Console.WriteLine("Byte array has size of {0}", bitmapByteArray.Length);
            Console.WriteLine($"width: {width}, bitmapHeight: {height}");
            for (int i = 0; i < (bitmapByteArray.Length < 64 ? bitmapByteArray.Length : 64); i += 4)
            {
                Console.WriteLine($"Bytes {bitmapByteArray[i]}, {bitmapByteArray[i + 1]}, {bitmapByteArray[i + 2]}, {bitmapByteArray[i+3]}" );
            }
        }

        private unsafe void RunCPP(object sender, RoutedEventArgs e)
        {
            CPPMultiplication();
        }

        private unsafe void CPPAddition()
        {
            stopwatch = new Stopwatch();
            byte[] argb = new byte[4];
            argb[0] = 0;  // Alpha
            argb[1] = 50; // Red
            argb[2] = 0;  // Green
            argb[3] = 0;  // Blue
            fixed (byte* bitmapPointer = bitmapByteArray, argbPointer = argb)
            {
                stopwatch.Reset();
                stopwatch.Start();
                // TODO dodać wielowątkowość
                CPPAdditionBalance(bitmapPointer, bitmapByteArray.Length, argbPointer);
                stopwatch.Stop();
            }
            Console.WriteLine("Operation took: {0} ms", stopwatch.ElapsedMilliseconds);
            var bitmap = BytesToBitmap(bitmapByteArray);
            SaveBitmap(bitmap);
        }

        private unsafe void CPPMultiplication()
        {
            stopwatch = new Stopwatch();
            // TODO pobrać kolory
            float[] argb = new float[4];
            argb[0] = 1f;  // Alpha
            argb[1] = 1f; // Red
            argb[2] = 1f;  // Green
            argb[3] = 3f;  // Blue
            fixed (byte* bitmapPointer = bitmapByteArray)
            {
                fixed (float* argbPointer = argb)
                {
                    stopwatch.Reset();
                    stopwatch.Start();
                    // TODO dodać wielowątkowość
                    CPPMultiplicationBalance(bitmapPointer, bitmapByteArray.Length, argbPointer);
                    stopwatch.Stop();
                }
            }
            Console.WriteLine("Operation took: {0} ms", stopwatch.ElapsedMilliseconds);
            var bitmap = BytesToBitmap(bitmapByteArray);
            SaveBitmap(bitmap);
        }

    }
}

/**
 * public void TestMethod()
{
    var incoming = new byte[100];
    fixed (byte* inBuf = incoming)
    {
        byte* outBuf = bufferOperations(inBuf, incoming.Length);
        // Assume, that the same buffer is returned, only with data changed.
        // Or by any other means, get the real lenght of output buffer (e.g. from library docs, etc).
        for (int i = 0; i < incoming.Length ; i++)
           incoming[i] = outBuf[i];
    }
}
*/