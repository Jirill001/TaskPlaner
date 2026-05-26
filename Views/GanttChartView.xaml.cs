using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
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
using TaskPlaner.Models;

namespace TaskPlaner.Views
{
    /// <summary>
    /// Логика взаимодействия для GanttChartView.xaml
    /// </summary>
    public partial class GanttChartView : UserControl
    {
        public static readonly DependencyProperty TasksProperty =
            DependencyProperty.Register(nameof(Tasks), typeof(ObservableCollection<TaskItem>), typeof(GanttChartView),
                new PropertyMetadata(null, OnTasksChanged));

        public ObservableCollection<TaskItem> Tasks
        {
            get => (ObservableCollection<TaskItem>)GetValue(TasksProperty);
            set => SetValue(TasksProperty, value);
        }

        private const double PixelsPerDay = 30.0;
        private const double TaskRowHeight = 40.0;
        private const double TimelineHeaderHeight = 40.0;

        public GanttChartView()
        {
            InitializeComponent();
        }

        private static void OnTasksChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = (GanttChartView)d;
            control.OnTasksCollectionChanged(e.OldValue as ObservableCollection<TaskItem>, e.NewValue as ObservableCollection<TaskItem>);
        }

        private void OnTasksCollectionChanged(ObservableCollection<TaskItem> oldCollection, ObservableCollection<TaskItem> newCollection)
        {
            if (oldCollection != null)
                oldCollection.CollectionChanged -= Tasks_CollectionChanged;

            if (newCollection != null)
                newCollection.CollectionChanged += Tasks_CollectionChanged;

            DrawGantt();
        }

        private void Tasks_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            DrawGantt();
        }

        private void DrawGantt()
        {
            GanttCanvas.Children.Clear();
            if (Tasks == null || Tasks.Count == 0)
                return;

            // Определяем диапазон дат
            DateTime minDate = Tasks.Min(t => t.StartDate).AddDays(-2);
            DateTime maxDate = Tasks.Max(t => t.EndDate).AddDays(2);
            double totalDays = (maxDate - minDate).TotalDays;
            double canvasWidth = totalDays * PixelsPerDay + 100;
            double canvasHeight = Tasks.Count * TaskRowHeight + TimelineHeaderHeight + 20;

            GanttCanvas.Width = canvasWidth;
            GanttCanvas.Height = canvasHeight;

            // Рисуем вертикальные линии сетки и даты
            for (DateTime date = minDate.Date; date <= maxDate.Date; date = date.AddDays(1))
            {
                double x = (date - minDate).TotalDays * PixelsPerDay;
                // Линия
                Line line = new Line
                {
                    X1 = x,
                    Y1 = TimelineHeaderHeight,
                    X2 = x,
                    Y2 = canvasHeight,
                    Stroke = Brushes.LightGray,
                    StrokeThickness = 0.5
                };
                GanttCanvas.Children.Add(line);

                // Подпись даты
                TextBlock dateLabel = new TextBlock
                {
                    Text = date.ToString("dd.MM"),
                    FontSize = 10,
                    Foreground = Brushes.DarkGray
                };
                Canvas.SetLeft(dateLabel, x - 15);
                Canvas.SetTop(dateLabel, 10);
                GanttCanvas.Children.Add(dateLabel);
            }

            // Рисуем горизонтальные линии для строк задач
            for (int i = 0; i < Tasks.Count; i++)
            {
                double y = TimelineHeaderHeight + i * TaskRowHeight;
                Line rowLine = new Line
                {
                    X1 = 0,
                    Y1 = y,
                    X2 = canvasWidth,
                    Y2 = y,
                    Stroke = Brushes.LightGray,
                    StrokeThickness = 0.5
                };
                GanttCanvas.Children.Add(rowLine);
            }

            // Рисуем блоки задач
            for (int i = 0; i < Tasks.Count; i++)
            {
                TaskItem task = Tasks[i];
                double startX = (task.StartDate - minDate).TotalDays * PixelsPerDay;
                double endX = (task.EndDate - minDate).TotalDays * PixelsPerDay;
                double width = endX - startX;
                if (width < 4) width = 4; // минимальная ширина

                double y = TimelineHeaderHeight + i * TaskRowHeight + 5;

                Rectangle rect = new Rectangle
                {
                    Width = width,
                    Height = TaskRowHeight - 10,
                    Fill = GetBrushByPriority(task.Priority),
                    Stroke = Brushes.DarkGray,
                    StrokeThickness = 1,
                    RadiusX = 3,
                    RadiusY = 3,
                    ToolTip = $"{task.Title}\n{task.StartDate:dd.MM.yyyy} - {task.EndDate:dd.MM.yyyy}\nПрогресс: {task.Progress}%"
                };
                Canvas.SetLeft(rect, startX);
                Canvas.SetTop(rect, y);
                GanttCanvas.Children.Add(rect);

                // Отображение прогресса (вторая полоса внутри)
                double progressWidth = width * task.Progress / 100.0;
                if (progressWidth > 0)
                {
                    Rectangle progressRect = new Rectangle
                    {
                        Width = progressWidth,
                        Height = TaskRowHeight - 10,
                        Fill = new SolidColorBrush(Color.FromArgb(100, 0, 128, 0)),
                        RadiusX = 3,
                        RadiusY = 3
                    };
                    Canvas.SetLeft(progressRect, startX);
                    Canvas.SetTop(progressRect, y);
                    GanttCanvas.Children.Add(progressRect);
                }

                // Название задачи на блоке
                TextBlock taskLabel = new TextBlock
                {
                    Text = task.Title,
                    FontSize = 11,
                    Foreground = Brushes.Black,
                    Width = width - 4,
                    TextTrimming = TextTrimming.CharacterEllipsis
                };
                Canvas.SetLeft(taskLabel, startX + 2);
                Canvas.SetTop(taskLabel, y + 2);
                GanttCanvas.Children.Add(taskLabel);
            }
        }

        private Brush GetBrushByPriority(Priority priority)
        {
            return priority switch
            {
                Priority.High => new SolidColorBrush(Color.FromRgb(255, 180, 180)),
                Priority.Medium => new SolidColorBrush(Color.FromRgb(180, 200, 255)),
                Priority.Low => new SolidColorBrush(Color.FromRgb(200, 255, 200)),
                _ => Brushes.LightGray
            };
        }
    }
}
