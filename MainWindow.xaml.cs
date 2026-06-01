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
            var ganttView = ganttChartView as GanttChartView;
            if (ganttView == null) return;

            var canvas = ganttView.DiagramCanvas;
            if (canvas == null) return;

            // Принудительная компоновка Canvas, чтобы гарантировать его содержимое
            canvas.Measure(new Size(canvas.Width, canvas.Height));
            canvas.Arrange(new Rect(0, 0, canvas.Width, canvas.Height));
            canvas.UpdateLayout();

            // Рендер всего холста (включая невидимую часть)
            RenderTargetBitmap bitmap = new RenderTargetBitmap(
                (int)canvas.ActualWidth,
                (int)canvas.ActualHeight,
                96, 96,
                PixelFormats.Pbgra32);
            bitmap.Render(canvas);

            var encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(bitmap));
            using (var ms = new MemoryStream())
            {
                encoder.Save(ms);
                ms.Position = 0;

                SaveFileDialog dlg = new SaveFileDialog
                {
                    Filter = "PDF files (*.pdf)|*.pdf",
                    DefaultExt = ".pdf"
                };
                if (dlg.ShowDialog() == true)
                {
                    var document = new PdfDocument();
                    var page = document.AddPage();
                    page.Width = canvas.ActualWidth;
                    page.Height = canvas.ActualHeight;

                    var gfx = XGraphics.FromPdfPage(page);
                    var image = XImage.FromStream(() => ms);
                    gfx.DrawImage(image, 0, 0, page.Width, page.Height);

                    document.Save(dlg.FileName);
                }
            }
        }
    }
}