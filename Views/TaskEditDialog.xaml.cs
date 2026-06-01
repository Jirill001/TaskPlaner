using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using TaskPlaner.Models;

namespace TaskPlaner.Views
{
    public partial class TaskEditDialog : Window
    {
        private TaskItem _task;
        private bool _isNewTask;
        private List<KeyValuePair<string, Color?>> _colorList;

        public TaskItem EditedTask => _task;

        public TaskEditDialog()
        {
            InitializeComponent();
            _task = new TaskItem
            {
                Title = "",
                Description = "",
                StartDate = DateTime.Today,
                EndDate = DateTime.Today.AddDays(5),
                Priority = Priority.Medium,
                Status = Status.Planned,
                Progress = 0,
                PredecessorIds = new System.Collections.Generic.List<string>(),
                CreatedAt = DateTime.Now
            };
            _isNewTask = true;
            SetupControls();
            LoadFromTask();
        }

        public TaskEditDialog(TaskItem existingTask)
        {
            InitializeComponent();
            if (existingTask == null) throw new ArgumentNullException(nameof(existingTask));
            _task = existingTask;
            _isNewTask = false;
            SetupControls();
            LoadFromTask();
        }

        private void SetupControls()
        {
            PriorityComboBox.ItemsSource = Enum.GetValues(typeof(Priority));
            StatusComboBox.ItemsSource = Enum.GetValues(typeof(Status));
            _colorList = new List<KeyValuePair<string, Color?>>
            {
                new KeyValuePair<string, Color?>("По приоритету", null),
                new KeyValuePair<string, Color?>("Красный", Colors.Red),
                new KeyValuePair<string, Color?>("Оранжевый", Colors.Orange),
                new KeyValuePair<string, Color?>("Жёлтый", Colors.Yellow),
                new KeyValuePair<string, Color?>("Зелёный", Colors.LimeGreen),
                new KeyValuePair<string, Color?>("Синий", Colors.DodgerBlue),
                new KeyValuePair<string, Color?>("Фиолетовый", Colors.Purple),
                new KeyValuePair<string, Color?>("Розовый", Colors.Pink),
                new KeyValuePair<string, Color?>("Коричневый", Colors.Brown),
                new KeyValuePair<string, Color?>("Серый", Colors.Gray),
            };
            ColorComboBox.ItemsSource = _colorList;
            ColorComboBox.DisplayMemberPath = "Key";
            ColorComboBox.SelectedValuePath = "Value";
        }

        private void LoadFromTask()
        {
            TitleBox.Text = _task.Title;
            DescriptionBox.Text = _task.Description;
            StartDatePicker.SelectedDate = _task.StartDate;
            EndDatePicker.SelectedDate = _task.EndDate;
            PriorityComboBox.SelectedItem = _task.Priority;
            StatusComboBox.SelectedItem = _task.Status;
            ProgressSlider.Value = _task.Progress;
            var selectedColor = _colorList.FirstOrDefault(c => c.Value == _task.CustomColor);
            if (selectedColor.Key != null)
                ColorComboBox.SelectedItem = selectedColor;
            else
                ColorComboBox.SelectedIndex = 0;
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            if (StartDatePicker.SelectedDate == null || EndDatePicker.SelectedDate == null)
            {
                MessageBox.Show("Выберите даты начала и окончания.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (EndDatePicker.SelectedDate.Value < StartDatePicker.SelectedDate.Value)
            {
                MessageBox.Show("Дата окончания не может быть раньше даты начала.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            _task.Title = TitleBox.Text.Trim();
            _task.Description = DescriptionBox.Text.Trim();
            _task.StartDate = StartDatePicker.SelectedDate.Value;
            _task.EndDate = EndDatePicker.SelectedDate.Value;
            _task.Priority = (Priority)PriorityComboBox.SelectedItem;
            _task.Status = (Status)StatusComboBox.SelectedItem;
            _task.Progress = (int)ProgressSlider.Value;
            _task.CustomColor = (ColorComboBox.SelectedItem as KeyValuePair<string, Color?>?)?.Value;

            DialogResult = true;
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}