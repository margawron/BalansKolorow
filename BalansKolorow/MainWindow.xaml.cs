using Microsoft.Win32;
using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;
using System.Text.RegularExpressions;
using System.Windows.Input;


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
        // Uchwyt do kontrolki ze ścieżka do bitmapy
        System.Windows.Controls.TextBox bitmapPathTextBox;

        bool wasBitmapLoaded = false;

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

        bool shouldPrevWindowBeOn;
        Ref<bool> keepOpen;



        [DllImport("ja_cpp.dll", EntryPoint = "AdditionBitmapColorBalancer")]
        public extern static unsafe void CPPAdditionLibraryMethod(byte* bitmap, int size, byte* bgra, uint sleepForMs);

        [DllImport("ja_cpp.dll", EntryPoint = "SubtractionBitmapColorBalancer")]
        public extern static unsafe void CPPSubtractionLibraryMethod(byte* bitmap, int size, byte* bgra, uint sleepForMs);

        [DllImport("ja_cpp.dll", EntryPoint = "MultiplicationColorBalancer")]
        public extern static unsafe void CPPMultiplicationLibraryMethod(byte* bitmap, int size, float* bgra, uint sleepForMs);

        [DllImport("ja_asm.dll", CallingConvention = CallingConvention.Cdecl, EntryPoint = "AdditionBitmapColorBalancer")]
        public extern static unsafe void ASMAdditionLibraryMethod(byte* bitmap, int size, byte* bgra);

        [DllImport("ja_asm.dll", CallingConvention = CallingConvention.Cdecl, EntryPoint = "SubtractionBitmapColorBalancer")]
        public extern static unsafe void ASMSubtractionLibraryMethod(byte* bitmap, int size, byte* bgra);

        [DllImport("ja_asm.dll", CallingConvention = CallingConvention.Cdecl, EntryPoint = "MultiplicationColorBalancer")]
        public extern static unsafe void ASMMultiplicationLibraryMethod(byte* bitmap, int size, float* bgra);

        public MainWindow()
        {
            InitializeComponent();
            flatRadioButton.IsChecked = true;
            imageView = (System.Windows.Controls.Image)FindName("ImageView");
            bitmapPathTextBox = (System.Windows.Controls.TextBox)FindName("BitmapUriTextBox");
            updateValueTextBoxes();
            SetNumberOfThreads();
            shouldPrevWindowBeOn = launchLivePreviewCheckbox.IsChecked.Value;

            keepOpen = new Ref<bool>(() => shouldPrevWindowBeOn, v =>
            {
                // Values cant be modified across threads so we need to invoke UI
                // thread dispatcher to change the values
                Dispatcher.Invoke(() =>
                {
                    shouldPrevWindowBeOn = v;
                    launchLivePreviewCheckbox.IsChecked = v;
                });

            });
        }

        // Obsługa przycisku "Wybierz bitmapę"
        private void LoadBitmap(object sender, RoutedEventArgs e)
        {
            // When bitmap is reloaded and preview window was open, close and refresh it
            // Close it
            bool cachedKeepOpen = false;
            if (keepOpen.Value)
            {
                cachedKeepOpen = true;
                keepOpen.Value = false;
            }
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "Bitmap|*.bmp";
            openFileDialog.FilterIndex = 1;
            if (openFileDialog.ShowDialog() == true)
            {
                wasBitmapLoaded = true;
                // Odbierz nazwę pliku
                bitmapFilename = openFileDialog.FileName;
                // Ustaw ścieżkę pliku w polu tekstowym
                bitmapPathTextBox.Text = bitmapFilename;
                // Wczytaj bitmapę z pliku i przekonwertuj na BGRA32
                var bitmap = ConvertBitmapToBgra32(new Bitmap(bitmapFilename));
                // Wyświetl bitmapę w kontrolce
                imageView.Source = ConvertToBitmapImage(bitmap);
                // Przekonwertuj bitmapę do tablicy bajtów
                bitmapByteArray = ImageToBytes(bitmap);
            }
            if (cachedKeepOpen)
            {
                Task.Factory.StartNew(() =>
                {
                    System.Threading.Thread.Sleep(500);
                    keepOpen.Value = true;
                    Dispatcher.Invoke(() =>   // Launch on UI thread
                    {
                        tryToLaunchPreviewWindow();
                    });
                });
            }

        }

        private void SaveOrRefreshBitmap(byte[] bitmapBytes)
        {
            if (wasBitmapLoaded)
            {
                Bitmap bitmap = null;
                if (saveAfterFinishCheckbox.IsChecked.Value)
                {
                    bitmap = BytesToBitmap(bitmapBytes);
                    bitmap.Save("save.bmp");
                }
                if (refreshAfterFinishCheckbox.IsChecked.Value)
                {
                    if (bitmap == null)
                    {
                        bitmap = BytesToBitmap(bitmapBytes);
                    }
                    imageView.Source = ConvertToBitmapImage(bitmap);
                }
            }
        }

        // Zamienia format bitmapy na BGRA(4 bajtowy)
        public Bitmap ConvertBitmapToBgra32(Bitmap bitmap)
        {
            // Sprawdz czy obraz nie jest już w dobrym formacie
            if (bitmap.PixelFormat == PixelFormat.Format32bppArgb)
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
            BitmapData bitmapInArgb = bitmap.LockBits(rect, ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb);
            // Wielkość tablicy w pixelach * rozmiar pixela
            int sizeOfTheArray = width * height * 4;
            // Alokacja pamięci
            imageBytes = new byte[sizeOfTheArray];
            // Skopiuj bitmapę do tablicy bajtów
            Marshal.Copy(bitmapInArgb.Scan0, imageBytes, 0, sizeOfTheArray);
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
        public BitmapImage ConvertToBitmapImage(Bitmap src)
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

        private float expFunction(double val)
        {
            return (float)Math.Pow((val + 255), 3) / 16000000;
        }


        // Obsługa zmiany wartości slidera
        private void onSliderToolTipChange(object sender, RoutedEventArgs e)
        {
            updateValueTextBoxes();
        }

        private void ValidateTimeFractionValue(object sender, TextCompositionEventArgs e)
        {
            Regex regex = new Regex("[^0-9]+");
            e.Handled = regex.IsMatch(e.Text) && e.Text.Length < 6;
        }

        private void updateValueTextBoxes()
        {
            if (flatRadioButton.IsChecked.Value)
            {
                redText.Text = redComponent.Value.ToString();
                redText.UpdateLayout();
                greenText.Text = greenComponent.Value.ToString();
                greenText.UpdateLayout();
                blueText.Text = blueComponent.Value.ToString();
                blueText.UpdateLayout();
            }
            else
            {
                var redVal = expFunction(redComponent.Value).ToString();
                redText.Text = redVal.Length < 4 ? redVal : redVal.Remove(4);
                redText.UpdateLayout();
                var greenVal = expFunction(greenComponent.Value).ToString();
                greenText.Text = greenVal.Length < 4 ? greenVal : greenVal.Remove(4);
                greenText.UpdateLayout();
                var blueVal = expFunction(blueComponent.Value).ToString();
                blueText.Text = blueVal.Length < 4 ? blueVal : blueVal.Remove(4);
                blueText.UpdateLayout();

            }
        }

        private void SetNumberOfThreads()
        {
            int coreCount = 0;
            foreach (var item in new System.Management.ManagementObjectSearcher("Select * from Win32_Processor").Get())
            {
                coreCount += int.Parse(item["NumberOfCores"].ToString());
            }
            addToLog($"Number Of Cores: {coreCount}\n");
            SliderAmountOfThreads.Value = coreCount;
        }

        private int GetNumberOfThreads()
        {
            return (int)Math.Ceiling(SliderAmountOfThreads.Value);
        }

        private byte[] GetByteColorValues(bool getOnlyPositives)
        {
            byte[] bgra = new byte[4];
            if (getOnlyPositives)
            {
                bgra[0] = blueComponent.Value >= 0 ? (byte)blueComponent.Value : (byte)0;
                bgra[1] = greenComponent.Value >= 0 ? (byte)greenComponent.Value : (byte)0;
                bgra[2] = redComponent.Value >= 0 ? (byte)redComponent.Value : (byte)0;
                bgra[3] = 0;
            }
            else
            {

                bgra[0] = blueComponent.Value <= 0 ? (byte)Math.Abs(blueComponent.Value) : (byte)0;
                bgra[1] = greenComponent.Value <= 0 ? (byte)Math.Abs(greenComponent.Value) : (byte)0;
                bgra[2] = redComponent.Value <= 0 ? (byte)Math.Abs(redComponent.Value) : (byte)0;
                bgra[3] = 0;
            }
            return bgra;
        }

        private float[] GetFloatColorValues()
        {
            float[] bgra = new float[4];
            bgra[0] = expFunction(blueComponent.Value);
            bgra[1] = expFunction(greenComponent.Value);
            bgra[2] = expFunction(redComponent.Value);
            bgra[3] = 1f;
            return bgra;
        }

        private uint GetTimeFractionValue()
        {
            if (sleepyCpp.IsChecked.Value)
                return Convert.ToUInt32(timeFractionTextbox.Text);
            else
                return 0;
        }

        private void addToLog(string message)
        {
            logTextBlock.Text = message + logTextBlock.Text;
        }

        // Obsługa naciśnięć na checkboxy z zapisem i odświeżaniem
        private void onCheckboxClicked(object sender, RoutedEventArgs e)
        {
            SaveOrRefreshBitmap(bitmapByteArray);

        }
        // Obługa naciśnieć na checkboxy z podglądem
        private unsafe void checkBoxClicked(object sender, RoutedEventArgs e)
        {
            tryToLaunchPreviewWindow();
        }

        private void tryToLaunchPreviewWindow()
        {
            if (launchLivePreviewCheckbox.IsChecked.Value && wasBitmapLoaded)
            {
                keepOpen.Value = true;

                var result = Task.Factory.StartNew(() =>
                {
                    using (var window = new LiveBitmapRenderer(width, height, keepOpen, bitmapByteArray))
                    {
                        window.Run(60.0);
                    }
                });
            }
            else if (!wasBitmapLoaded)
            {
                keepOpen.Value = false;
                var msgBox = MessageBox.Show("Nie wybrano bitmapy do załadowania", "Wybierz bitmapę", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            else
            {
                keepOpen.Value = false;
            }
        }

        // Obsługa przycisku Użyj C++
        private void RunCPP(object sender, RoutedEventArgs e)
        {
            if (wasBitmapLoaded)
            {
                if (flatRadioButton.IsChecked.Value)
                    CPPFlat();
                else
                    CPPRelative();
            }
            else
            {
                var msgBox = MessageBox.Show("Nie wybrano bitmapy do załadowania", "Wybierz bitmapę", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }
        // Obsługa przycisku Użyj Asemblera
        private void RunASM(object sender, RoutedEventArgs e)
        {
            if (wasBitmapLoaded)
            {
                if (flatRadioButton.IsChecked.Value)
                    ASMFlat();
                else
                    ASMRelative();
            }
            else
            {
                var msgBox = MessageBox.Show("Nie wybrano bitmapy do załadowania", "Wybierz bitmapę", MessageBoxButton.OK, MessageBoxImage.Warning);

            }
        }

        private unsafe void CPPFlat()
        {
            stopwatch = new Stopwatch();
            byte[] bgraForAdding = GetByteColorValues(true);
            byte[] bgraForSubtracting = GetByteColorValues(false);
            int numOfThreads = GetNumberOfThreads();
            var length = bitmapByteArray.Length;
            var pixelsInBitmap = length / 4;
            var pixelsForSingleThread = pixelsInBitmap / numOfThreads;
            var perThreadOffset = pixelsForSingleThread * 4;
            var remainderOfPixelsForLastThread = pixelsInBitmap % numOfThreads;
            var remainderToFinishOffset = remainderOfPixelsForLastThread * 4;
            var sleepMsForEachPixel = GetTimeFractionValue();
            addToLog($"Launching flat image conversion using cpp with {numOfThreads} threads\n");
            ParallelOptions parallelOptions = new ParallelOptions();
            parallelOptions.MaxDegreeOfParallelism = numOfThreads;
            stopwatch.Reset();
            stopwatch.Start();
            var zmienna = Parallel.For(0, numOfThreads, parallelOptions, i =>
            {
                if (i == numOfThreads - 1)
                {
                    fixed (byte* bgraAdd = bgraForAdding, bgraSub = bgraForSubtracting)
                    {
                        fixed (byte* bitmapPointer = &bitmapByteArray[i * perThreadOffset])
                        {
                            if (bgraAdd[0] != 0 || bgraAdd[1] != 0 || bgraAdd[2] != 0)
                            {
                                CPPAdditionLibraryMethod(bitmapPointer, perThreadOffset + remainderToFinishOffset, bgraAdd, sleepMsForEachPixel);
                            }
                            if (bgraSub[0] != 0 || bgraSub[1] != 0 || bgraSub[2] != 0)
                            {
                                CPPSubtractionLibraryMethod(bitmapPointer, perThreadOffset + remainderToFinishOffset, bgraSub, sleepMsForEachPixel);
                            }
                        }
                    }
                }
                else
                {
                    fixed (byte* bgraAdd = bgraForAdding, bgraSub = bgraForSubtracting)
                    {
                        fixed (byte* bitmapPointer = &bitmapByteArray[i * perThreadOffset])
                        {
                            if (bgraAdd[0] != 0 || bgraAdd[1] != 0 || bgraAdd[2] != 0)
                            {
                                CPPAdditionLibraryMethod(bitmapPointer, perThreadOffset + remainderToFinishOffset, bgraAdd, sleepMsForEachPixel);
                            }
                            if (bgraSub[0] != 0 || bgraSub[1] != 0 || bgraSub[2] != 0)
                            {
                                CPPSubtractionLibraryMethod(bitmapPointer, perThreadOffset + remainderToFinishOffset, bgraSub, sleepMsForEachPixel);
                            }
                        }
                    }
                }
            });
            stopwatch.Stop();
            addToLog($"Operation using cpp with {numOfThreads} threads took: {stopwatch.ElapsedMilliseconds} ms\n");
            SaveOrRefreshBitmap(bitmapByteArray);
        }

        private unsafe void CPPRelative()
        {
            stopwatch = new Stopwatch();
            float[] bgra = GetFloatColorValues();
            int numOfThreads = GetNumberOfThreads();
            var length = bitmapByteArray.Length;
            var pixelsInBitmap = length / 4;
            var pixelsForSingleThread = pixelsInBitmap / numOfThreads;
            var perThreadOffset = pixelsForSingleThread * 4;
            var remainderOfPixelsForLastThread = pixelsInBitmap % numOfThreads;
            var remainderToFinishOffset = remainderOfPixelsForLastThread * 4;
            var sleepMsForEachPixel = GetTimeFractionValue();
            addToLog($"Launching relative image conversion using cpp with {numOfThreads} threads\n");
            ParallelOptions parallelOptions = new ParallelOptions();
            parallelOptions.MaxDegreeOfParallelism = numOfThreads;
            stopwatch.Reset();
            stopwatch.Start();
            Parallel.For(0, numOfThreads, parallelOptions, i =>
            {
                if (i == numOfThreads - 1)
                {
                    fixed (float* bgraPtr = bgra)
                    {
                        fixed (byte* bitmapPointer = &bitmapByteArray[i * perThreadOffset])
                        {
                            CPPMultiplicationLibraryMethod(bitmapPointer, perThreadOffset + remainderToFinishOffset, bgraPtr, sleepMsForEachPixel);

                        }
                    }
                }
                else
                {
                    fixed (float* bgraPtr = bgra)
                    {
                        fixed (byte* bitmapPointer = &bitmapByteArray[i * perThreadOffset])
                        {
                            CPPMultiplicationLibraryMethod(bitmapPointer, perThreadOffset, bgraPtr, sleepMsForEachPixel);

                        }
                    }
                }
            });
            stopwatch.Stop();
            addToLog($"Operation took: {stopwatch.ElapsedMilliseconds} ms\n");
            SaveOrRefreshBitmap(bitmapByteArray);
        }

        private unsafe void ASMFlat()
        {
            stopwatch = new Stopwatch();
            byte[] bgraForAdding = GetByteColorValues(true);
            byte[] bgraForSubtracting = GetByteColorValues(false);
            int numOfThreads = GetNumberOfThreads();
            var length = bitmapByteArray.Length;
            var pixelsInBitmap = length / 4;
            var pixelsForSingleThread = pixelsInBitmap / numOfThreads;
            var perThreadOffset = pixelsForSingleThread * 4;
            var remainderOfPixelsForLastThread = pixelsInBitmap % numOfThreads;
            var remainderToFinishOffset = remainderOfPixelsForLastThread * 4;
            addToLog($"Launching flat image conversion using asm with {numOfThreads} threads\n");
            ParallelOptions parallelOptions = new ParallelOptions();
            parallelOptions.MaxDegreeOfParallelism = numOfThreads;
            stopwatch.Reset();
            stopwatch.Start();
            Parallel.For(0, numOfThreads, parallelOptions, i =>
            {

                if (i == numOfThreads - 1)
                {
                    fixed (byte* bgraAdd = bgraForAdding, bgraSub = bgraForSubtracting)
                    {
                        fixed (byte* bitmapPointer = &bitmapByteArray[i * perThreadOffset])
                        {
                            if (bgraAdd[0] != 0 || bgraAdd[1] != 0 || bgraAdd[2] != 0)
                            {
                                ASMAdditionLibraryMethod(bitmapPointer, perThreadOffset + remainderToFinishOffset, bgraAdd);
                            }
                            if (bgraSub[0] != 0 || bgraSub[1] != 0 || bgraSub[2] != 0)
                            {
                                ASMSubtractionLibraryMethod(bitmapPointer, perThreadOffset + remainderToFinishOffset, bgraSub);
                            }
                        }
                    }
                }
                else
                {
                    fixed (byte* bgraAdd = bgraForAdding, bgraSub = bgraForSubtracting)
                    {
                        fixed (byte* bitmapPointer = &bitmapByteArray[i * perThreadOffset])
                        {
                            if (bgraAdd[0] != 0 || bgraAdd[1] != 0 || bgraAdd[2] != 0)
                            {
                                ASMAdditionLibraryMethod(bitmapPointer, perThreadOffset + remainderToFinishOffset, bgraAdd);
                            }
                            if (bgraSub[0] != 0 || bgraSub[1] != 0 || bgraSub[2] != 0)
                            {
                                ASMSubtractionLibraryMethod(bitmapPointer, perThreadOffset + remainderToFinishOffset, bgraSub);
                            }
                        }
                    }
                }
            });
            stopwatch.Stop();
            addToLog($"Operation using asm with {numOfThreads} threads took: {stopwatch.ElapsedMilliseconds} ms\n");
            SaveOrRefreshBitmap(bitmapByteArray);
        }
        private unsafe void ASMRelative()
        {
            stopwatch = new Stopwatch();
            float[] bgra = GetFloatColorValues();
            int numOfThreads = GetNumberOfThreads();
            var length = bitmapByteArray.Length;
            var pixelsInBitmap = length / 4;
            var pixelsForSingleThread = pixelsInBitmap / numOfThreads;
            var perThreadOffset = pixelsForSingleThread * 4;
            var remainderOfPixelsForLastThread = pixelsInBitmap % numOfThreads;
            var remainderToFinishOffset = remainderOfPixelsForLastThread * 4;
            addToLog($"Launching relative image conversion using asm with {numOfThreads} threads\n");
            ParallelOptions parallelOptions = new ParallelOptions();
            parallelOptions.MaxDegreeOfParallelism = numOfThreads;
            stopwatch.Reset();
            stopwatch.Start();
            Parallel.For(0, numOfThreads, parallelOptions, i =>
            {
                if (i == numOfThreads - 1)
                {
                    fixed (float* bgraPtr = bgra)
                    {
                        fixed (byte* bitmapPointer = &bitmapByteArray[i * perThreadOffset])
                        {
                            ASMMultiplicationLibraryMethod(bitmapPointer, perThreadOffset + remainderToFinishOffset, bgraPtr);

                        }
                    }
                }
                else
                {
                    fixed (float* bgraPtr = bgra)
                    {
                        fixed (byte* bitmapPointer = &bitmapByteArray[i * perThreadOffset])
                        {
                            ASMMultiplicationLibraryMethod(bitmapPointer, perThreadOffset, bgraPtr);

                        }
                    }
                }
            });
            stopwatch.Stop();
            addToLog($"Operation took: {stopwatch.ElapsedMilliseconds} ms\n");
            SaveOrRefreshBitmap(bitmapByteArray);
        }


    }
}
