using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using TaskPlaner.Models;
using TaskPlaner.Services;
using TaskPlaner.ViewModels;
using TaskPlaner.Views;
using System.IO;
using Microsoft.Win32;
using PdfSharpCore.Drawing;
using PdfSharpCore.Pdf;


namespace TaskPlaner
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            var projectService = new ProjectService();
            DataContext = new MainViewModel(projectService);

            var vm = DataContext as MainViewModel;
            if (vm != null)
            {
                vm.TasksRescheduled += () => ganttChartView.DrawGantt();
            }
        }
            private void ExportPdf_Click(object sender, RoutedEventArgs e)
        {
            var ganttView = ganttChartView as FrameworkElement;
            if (ganttView == null) return;

            var bitmap = new RenderTargetBitmap(
                (int)ganttView.ActualWidth,
                (int)ganttView.ActualHeight,
                96, 96,
                PixelFormats.Pbgra32);
            bitmap.Render(ganttView);

            var encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(bitmap));
            using (var ms = new MemoryStream())
            {
                encoder.Save(ms);
                ms.Position = 0;

                var dlg = new SaveFileDialog
                {
                    Filter = "PDF files (*.pdf)|*.pdf",
                    DefaultExt = ".pdf"
                };
                if (dlg.ShowDialog() == true)
                {
                    var document = new PdfDocument();
                    var page = document.AddPage();
                    page.Width = (int)ganttView.ActualWidth;
                    page.Height = (int)ganttView.ActualHeight;

                    var gfx = XGraphics.FromPdfPage(page);
                    var image = XImage.FromStream(() => ms);
                    gfx.DrawImage(image, 0, 0, page.Width, page.Height);

                    document.Save(dlg.FileName);
                }
            }
        }
    }
    
}