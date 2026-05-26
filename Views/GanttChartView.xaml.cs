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
        private const double HandleWidth = 6.0;

        private List<Rectangle> _taskRectangles = new List<Rectangle>();
        private List<Rectangle> _leftHandles = new List<Rectangle>();
        private List<Rectangle> _rightHandles = new List<Rectangle>();

        private bool _isDragging = false;
        private int _draggedTaskIndex = -1;
        private DragType _dragType = DragType.None;
        private Point _startMousePoint;
        private DateTime _originalStartDate;
        private DateTime _originalEndDate;
        private DateTime _minDate;
        private Rectangle _draggedRect;
        private Rectangle _draggedLeftHandle;
        private Rectangle _draggedRightHandle;

        private enum DragType { None, Move, ResizeLeft, ResizeRight }

        public GanttChartView()
        {
            InitializeComponent();
            GanttCanvas.MouseLeftButtonDown += GanttCanvas_MouseLeftButtonDown;
            GanttCanvas.MouseMove += GanttCanvas_MouseMove;
            GanttCanvas.MouseLeftButtonUp += GanttCanvas_MouseLeftButtonUp;
            GanttCanvas.MouseLeave += GanttCanvas_MouseLeave;
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
            _taskRectangles.Clear();
            _leftHandles.Clear();
            _rightHandles.Clear();

            if (Tasks == null || Tasks.Count == 0)
                return;

            _minDate = Tasks.Min(t => t.StartDate).AddDays(-2);
            DateTime maxDate = Tasks.Max(t => t.EndDate).AddDays(2);
            double totalDays = (maxDate - _minDate).TotalDays;
            double canvasWidth = totalDays * PixelsPerDay + 100;
            double canvasHeight = Tasks.Count * TaskRowHeight + TimelineHeaderHeight + 20;

            GanttCanvas.Width = canvasWidth;
            GanttCanvas.Height = canvasHeight;

            for (DateTime date = _minDate.Date; date <= maxDate.Date; date = date.AddDays(1))
            {
                double x = (date - _minDate).TotalDays * PixelsPerDay;
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

                TextBlock dateLabel = new TextBlock
                {
                    Text = date.ToString("dd.MM"),
                    FontSize = 10,
                    Foreground = Brushes.DarkGray,
                    IsHitTestVisible = false
                };
                Canvas.SetLeft(dateLabel, x - 15);
                Canvas.SetTop(dateLabel, 10);
                GanttCanvas.Children.Add(dateLabel);
            }

            // Горизонтальные линии строк
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

            // Блоки задач
            for (int i = 0; i < Tasks.Count; i++)
            {
                TaskItem task = Tasks[i];
                double startX = (task.StartDate - _minDate).TotalDays * PixelsPerDay;
                double endX = (task.EndDate - _minDate).TotalDays * PixelsPerDay;
                double width = endX - startX;
                if (width < 4) width = 4;

                double y = TimelineHeaderHeight + i * TaskRowHeight + 5;
                double height = TaskRowHeight - 10;

                // Основной прямоугольник задачи
                Rectangle rect = new Rectangle
                {
                    Width = width,
                    Height = height,
                    Fill = GetBrushByPriority(task.Priority),
                    Stroke = Brushes.DarkGray,
                    StrokeThickness = 1,
                    RadiusX = 3,
                    RadiusY = 3,
                    ToolTip = $"{task.Title}\n{task.StartDate:dd.MM.yyyy} - {task.EndDate:dd.MM.yyyy}\nПрогресс: {task.Progress}%",
                    IsHitTestVisible = true
                };
                Canvas.SetLeft(rect, startX);
                Canvas.SetTop(rect, y);
                GanttCanvas.Children.Add(rect);
                _taskRectangles.Add(rect);

                // Полоса прогресса
                double progressWidth = width * task.Progress / 100.0;
                if (progressWidth > 0)
                {
                    Rectangle progressRect = new Rectangle
                    {
                        Width = progressWidth,
                        Height = height,
                        Fill = new SolidColorBrush(Color.FromArgb(100, 0, 128, 0)),
                        RadiusX = 3,
                        RadiusY = 3,
                        IsHitTestVisible = false
                    };
                    Canvas.SetLeft(progressRect, startX);
                    Canvas.SetTop(progressRect, y);
                    GanttCanvas.Children.Add(progressRect);
                }

                // Текст названия задачи
                TextBlock taskLabel = new TextBlock
                {
                    Text = task.Title,
                    FontSize = 11,
                    Foreground = Brushes.Black,
                    Width = width - 4,
                    TextTrimming = TextTrimming.CharacterEllipsis,
                    IsHitTestVisible = false
                };
                Canvas.SetLeft(taskLabel, startX + 2);
                Canvas.SetTop(taskLabel, y + 2);
                GanttCanvas.Children.Add(taskLabel);

                // Левая ручка растягивания
                Rectangle leftHandle = new Rectangle
                {
                    Width = HandleWidth,
                    Height = height,
                    Fill = Brushes.Transparent,
                    Stroke = Brushes.Gray,
                    StrokeThickness = 0.5,
                    Cursor = Cursors.SizeWE,
                    IsHitTestVisible = true
                };
                Canvas.SetLeft(leftHandle, startX - HandleWidth / 2);
                Canvas.SetTop(leftHandle, y);
                GanttCanvas.Children.Add(leftHandle);
                _leftHandles.Add(leftHandle);

                // Правая ручка
                Rectangle rightHandle = new Rectangle
                {
                    Width = HandleWidth,
                    Height = height,
                    Fill = Brushes.Transparent,
                    Stroke = Brushes.Gray,
                    StrokeThickness = 0.5,
                    Cursor = Cursors.SizeWE,
                    IsHitTestVisible = true
                };
                Canvas.SetLeft(rightHandle, endX - HandleWidth / 2);
                Canvas.SetTop(rightHandle, y);
                GanttCanvas.Children.Add(rightHandle);
                _rightHandles.Add(rightHandle);
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

        private void GanttCanvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (Tasks == null) return;

            Point mousePos = e.GetPosition(GanttCanvas);
            var hitElement = GanttCanvas.InputHitTest(mousePos) as Rectangle;
            if (hitElement == null) return;

            // Определяем, по чему кликнули
            int index = _taskRectangles.IndexOf(hitElement);
            if (index >= 0)
            {
                StartDrag(index, DragType.Move, mousePos);
                return;
            }
            index = _leftHandles.IndexOf(hitElement);
            if (index >= 0)
            {
                StartDrag(index, DragType.ResizeLeft, mousePos);
                return;
            }
            index = _rightHandles.IndexOf(hitElement);
            if (index >= 0)
            {
                StartDrag(index, DragType.ResizeRight, mousePos);
                return;
            }
        }

        private void StartDrag(int index, DragType type, Point mousePos)
        {
            _isDragging = true;
            _draggedTaskIndex = index;
            _dragType = type;
            _startMousePoint = mousePos;
            _originalStartDate = Tasks[index].StartDate;
            _originalEndDate = Tasks[index].EndDate;

            _draggedRect = _taskRectangles[index];
            _draggedLeftHandle = _leftHandles[index];
            _draggedRightHandle = _rightHandles[index];

            GanttCanvas.CaptureMouse();
        }

        private void GanttCanvas_MouseMove(object sender, MouseEventArgs e)
        {
            if (!_isDragging || _draggedTaskIndex < 0) return;

            Point currentPos = e.GetPosition(GanttCanvas);
            double deltaX = currentPos.X - _startMousePoint.X;
            double deltaDays = deltaX / PixelsPerDay;
            DateTime newStart = _originalStartDate;
            DateTime newEnd = _originalEndDate;

            switch (_dragType)
            {
                case DragType.Move:
                    newStart = _originalStartDate.AddDays(deltaDays);
                    newEnd = _originalEndDate.AddDays(deltaDays);
                    break;
                case DragType.ResizeLeft:
                    newStart = _originalStartDate.AddDays(deltaDays);
                    if (newStart > _originalEndDate) newStart = _originalEndDate.AddDays(-1);
                    break;
                case DragType.ResizeRight:
                    newEnd = _originalEndDate.AddDays(deltaDays);
                    if (newEnd < _originalStartDate) newEnd = _originalStartDate.AddDays(1);
                    break;
            }

            // Обновляем визуально позиции и размеры временно
            double newStartX = (newStart - _minDate).TotalDays * PixelsPerDay;
            double newEndX = (newEnd - _minDate).TotalDays * PixelsPerDay;
            double newWidth = newEndX - newStartX;
            if (newWidth < 4) newWidth = 4;

            Canvas.SetLeft(_draggedRect, newStartX);
            _draggedRect.Width = newWidth;
            Canvas.SetLeft(_draggedLeftHandle, newStartX - HandleWidth / 2);
            Canvas.SetLeft(_draggedRightHandle, newEndX - HandleWidth / 2);
        }

        private void GanttCanvas_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (!_isDragging) return;
            _isDragging = false;
            GanttCanvas.ReleaseMouseCapture();

            if (_draggedTaskIndex < 0) return;

            // Применяем изменения к модели
            Point currentPos = e.GetPosition(GanttCanvas);
            double deltaDays = (currentPos.X - _startMousePoint.X) / PixelsPerDay;
            TaskItem task = Tasks[_draggedTaskIndex];

            switch (_dragType)
            {
                case DragType.Move:
                    task.StartDate = _originalStartDate.AddDays(deltaDays);
                    task.EndDate = _originalEndDate.AddDays(deltaDays);
                    break;
                case DragType.ResizeLeft:
                    DateTime newStart = _originalStartDate.AddDays(deltaDays);
                    if (newStart > task.EndDate) newStart = task.EndDate.AddDays(-1);
                    task.StartDate = newStart;
                    break;
                case DragType.ResizeRight:
                    DateTime newEnd = _originalEndDate.AddDays(deltaDays);
                    if (newEnd < task.StartDate) newEnd = task.StartDate.AddDays(1);
                    task.EndDate = newEnd;
                    break;
            }

            // Полная перерисовка диаграммы
            DrawGantt();
        }

        private void GanttCanvas_MouseLeave(object sender, MouseEventArgs e)
        {
            if (_isDragging)
            {
                _isDragging = false;
                GanttCanvas.ReleaseMouseCapture();
                DrawGantt(); // откат к исходным позициям
            }
        }
    }
}
