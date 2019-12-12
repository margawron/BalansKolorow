using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace BalansKolorow
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        [DllImport("ja_cpp.dll")]
        public extern static IntPtr FileCPP();

        [DllImport("ja_asm.dll",CallingConvention = CallingConvention.StdCall, EntryPoint = "FileASM")]
        public extern static IntPtr FileASM();

        public MainWindow()
        {
            InitializeComponent();
        }

        private void LaunchHelloWorld(object sender, RoutedEventArgs e)
        {
            var elo = (TextBox)this.FindName("textbox");
            elo.Text = "Hello World";
        }

        private void LauchASM(object sender, RoutedEventArgs e)
        {
            var elo = (TextBox)this.FindName("textbox");
            IntPtr lel = FileASM();
            elo.Text = Marshal.PtrToStringAnsi(lel);
        }

        private void LaunchCPP(object sender, RoutedEventArgs e)
        {
            var elo = (TextBox)this.FindName("textbox");
            elo.Text = Marshal.PtrToStringAnsi(FileCPP());
        }
    }
}

