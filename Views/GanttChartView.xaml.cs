using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using TaskPlaner.Models;
using TaskPlaner.Models;

namespace TaskPlaner.Views
{
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
        private TaskItem _draggedTask = null;
        private DragType _dragType = DragType.None;
        private Point _startMousePoint;
        private DateTime _originalStartDate;
        private DateTime _originalEndDate;
        private DateTime _minDate;
        private Rectangle _draggedRect;
        private Rectangle _draggedLeftHandle;
        private Rectangle _draggedRightHandle;
        public Canvas DiagramCanvas => GanttCanvas;

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
            {
                oldCollection.CollectionChanged -= Tasks_CollectionChanged;
            }
            if (newCollection != null)
            {
                newCollection.CollectionChanged += Tasks_CollectionChanged;
            }
            DrawGantt();
        }

        private void Tasks_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            DrawGantt();
        }

        public void DrawGantt()
        {
            GanttCanvas.Children.Clear();
            _taskRectangles.Clear();
            _leftHandles.Clear();
            _rightHandles.Clear();

            if (Tasks == null || Tasks.Count == 0) return;

            var sortedTasks = Tasks.OrderBy(t => t.StartDate).ToList();

            _minDate = sortedTasks.Min(t => t.StartDate).AddDays(-2);
            DateTime maxDate = sortedTasks.Max(t => t.EndDate).AddDays(2);
            double totalDays = (maxDate - _minDate).TotalDays;
            double canvasWidth = totalDays * PixelsPerDay + 100;
            double canvasHeight = sortedTasks.Count * TaskRowHeight + TimelineHeaderHeight + 20;

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

            for (int i = 0; i < sortedTasks.Count; i++)
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

            for (int i = 0; i < sortedTasks.Count; i++)
            {
                TaskItem task = sortedTasks[i];
                double startX = (task.StartDate - _minDate).TotalDays * PixelsPerDay;
                double endX = (task.EndDate - _minDate).TotalDays * PixelsPerDay;
                double width = endX - startX;
                if (width < 4) width = 4;

                double y = TimelineHeaderHeight + i * TaskRowHeight + 5;
                double height = TaskRowHeight - 10;

                Rectangle rect = new Rectangle
                {
                    Width = width,
                    Height = height,
                    Fill = GetTaskFill(task),
                    Stroke = Brushes.DarkGray,
                    StrokeThickness = 1,
                    RadiusX = 3,
                    RadiusY = 3,
                    ToolTip = $"{task.Title}\n{task.StartDate:dd.MM.yyyy} - {task.EndDate:dd.MM.yyyy}\nПрогресс: {task.Progress}%",
                    IsHitTestVisible = true,
                    Tag = task
                };
                Canvas.SetLeft(rect, startX);
                Canvas.SetTop(rect, y);
                GanttCanvas.Children.Add(rect);
                _taskRectangles.Add(rect);

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

                Rectangle leftHandle = new Rectangle
                {
                    Width = HandleWidth,
                    Height = height,
                    Fill = Brushes.Transparent,
                    Stroke = Brushes.Gray,
                    StrokeThickness = 0.5,
                    Cursor = Cursors.SizeWE,
                    IsHitTestVisible = true,
                    Tag = task
                };
                Canvas.SetLeft(leftHandle, startX - HandleWidth / 2);
                Canvas.SetTop(leftHandle, y);
                GanttCanvas.Children.Add(leftHandle);
                _leftHandles.Add(leftHandle);

                Rectangle rightHandle = new Rectangle
                {
                    Width = HandleWidth,
                    Height = height,
                    Fill = Brushes.Transparent,
                    Stroke = Brushes.Gray,
                    StrokeThickness = 0.5,
                    Cursor = Cursors.SizeWE,
                    IsHitTestVisible = true,
                    Tag = task
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
        private Brush GetTaskFill(TaskItem task)
        {
            if (task.CustomColor.HasValue)
                return new SolidColorBrush(task.CustomColor.Value);
            return GetBrushByPriority(task.Priority);
        }

        private void GanttCanvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (Tasks == null) return;

            Point mousePos = e.GetPosition(GanttCanvas);
            var hitElement = GanttCanvas.InputHitTest(mousePos) as Rectangle;
            if (hitElement == null) return;

            int index = _taskRectangles.IndexOf(hitElement);
            if (index >= 0)
            {
                var task = (TaskItem)_taskRectangles[index].Tag;
                StartDrag(task, index, DragType.Move, mousePos);
                return;
            }
            index = _leftHandles.IndexOf(hitElement);
            if (index >= 0)
            {
                var task = (TaskItem)_leftHandles[index].Tag;
                StartDrag(task, index, DragType.ResizeLeft, mousePos);
                return;
            }
            index = _rightHandles.IndexOf(hitElement);
            if (index >= 0)
            {
                var task = (TaskItem)_rightHandles[index].Tag;
                StartDrag(task, index, DragType.ResizeRight, mousePos);
                return;
            }
        }

        private void StartDrag(TaskItem task, int visualIndex, DragType type, Point mousePos)
        {
            _isDragging = true;
            _draggedTask = task;
            _dragType = type;
            _startMousePoint = mousePos;
            _originalStartDate = task.StartDate;
            _originalEndDate = task.EndDate;

            _draggedRect = _taskRectangles[visualIndex];
            _draggedLeftHandle = _leftHandles[visualIndex];
            _draggedRightHandle = _rightHandles[visualIndex];

            GanttCanvas.CaptureMouse();
        }

        private void GanttCanvas_MouseMove(object sender, MouseEventArgs e)
        {
            if (!_isDragging || _draggedTask == null) return;

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
                    if (newStart >= _originalEndDate) newStart = _originalEndDate.AddDays(-1);
                    break;
                case DragType.ResizeRight:
                    newEnd = _originalEndDate.AddDays(deltaDays);
                    if (newEnd <= _originalStartDate) newEnd = _originalStartDate.AddDays(1);
                    break;
            }

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
            if (!_isDragging || _draggedTask == null) return;
            _isDragging = false;
            GanttCanvas.ReleaseMouseCapture();

            Point currentPos = e.GetPosition(GanttCanvas);
            double deltaDays = (currentPos.X - _startMousePoint.X) / PixelsPerDay;

            switch (_dragType)
            {
                case DragType.Move:
                    _draggedTask.StartDate = _originalStartDate.AddDays(deltaDays);
                    _draggedTask.EndDate = _originalEndDate.AddDays(deltaDays);
                    break;
                case DragType.ResizeLeft:
                    DateTime newStart = _originalStartDate.AddDays(deltaDays);
                    if (newStart >= _draggedTask.EndDate) newStart = _draggedTask.EndDate.AddDays(-1);
                    _draggedTask.StartDate = newStart;
                    break;
                case DragType.ResizeRight:
                    DateTime newEnd = _originalEndDate.AddDays(deltaDays);
                    if (newEnd <= _draggedTask.StartDate) newEnd = _draggedTask.StartDate.AddDays(1);
                    _draggedTask.EndDate = newEnd;
                    break;
            }

            _draggedTask = null;
            DrawGantt();
        }

        private void GanttCanvas_MouseLeave(object sender, MouseEventArgs e)
        {
            if (_isDragging)
            {
                _isDragging = false;
                GanttCanvas.ReleaseMouseCapture();
                _draggedTask = null;
                DrawGantt();
            }
        }
        private bool _sortByDate = false;
        public void SortTasksByDate()
        {
            _sortByDate = true;
            DrawGantt();
            _sortByDate = false;
        }

        public System.Windows.Media.Imaging.BitmapSource CaptureCanvas()
        {
            if (GanttCanvas.ActualWidth == 0 || GanttCanvas.ActualHeight == 0)
                return null;

            RenderTargetBitmap renderBitmap = new RenderTargetBitmap(
                (int)GanttCanvas.ActualWidth,
                (int)GanttCanvas.ActualHeight,
                96, 96, PixelFormats.Pbgra32);
            renderBitmap.Render(GanttCanvas);
            return renderBitmap;
        }

    }
}