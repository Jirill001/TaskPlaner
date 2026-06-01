using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using TaskPlaner.Models;
using TaskPlaner.Services;
using TaskPlaner.Views;
using System.ComponentModel;

namespace TaskPlaner.ViewModels
{
    public class MainViewModel : INotifyPropertyChanged
    {
        private readonly ProjectService _projectService;
        private ICollectionView _tasksView;
        private TaskItem _selectedTask;
        private Status _filterStatus;
        private ICommand _addTaskCommand;
        private ICommand _editTaskCommand;
        private ICommand _deleteTaskCommand;
        private ICommand _saveCommand;
        private ICommand _loadCommand;
        private ICommand _autoScheduleCommand;


        public List<KeyValuePair<string, Status>> FilterStatusesList { get; }
        public ObservableCollection<TaskItem> Tasks => _projectService.Tasks;
        public MainViewModel(ProjectService projectService)
        {
            _projectService = projectService;
            _filterStatus = Status.Planned | Status.InProgress | Status.Completed;

            _tasksView = CollectionViewSource.GetDefaultView(_projectService.Tasks);
            _tasksView.Filter = TaskFilter;
            _tasksView.SortDescriptions.Add(new SortDescription("StartDate", ListSortDirection.Ascending));
            _tasksView.SortDescriptions.Add(new SortDescription("Priority", ListSortDirection.Descending));
            _tasksView.SortDescriptions.Add(new SortDescription("CreatedAt", ListSortDirection.Ascending));
            if (_tasksView is ICollectionViewLiveShaping liveView)
            {
                liveView.LiveSortingProperties.Add("StartDate");
                liveView.LiveSortingProperties.Add("Priority");
                liveView.IsLiveSorting = true;
            }

            FilterStatusesList = new List<KeyValuePair<string, Status>>()
            {
                new KeyValuePair<string, Status>("Все", Status.Planned | Status.InProgress | Status.Completed),
                new KeyValuePair<string, Status>("Планируется", Status.Planned),
                new KeyValuePair<string, Status>("В работе", Status.InProgress),
                new KeyValuePair<string, Status>("Завершена", Status.Completed)
            };
        }

        public ICollectionView TasksView
        {
            get => _tasksView;
        }

        public TaskItem SelectedTask
        {
            get => _selectedTask;
            set
            {
                _selectedTask = value;
                OnPropertyChanged();
            }
        }

        public Status FilterStatus
        {
            get => _filterStatus;
            set
            {
                _filterStatus = value;
                OnPropertyChanged();
                _tasksView.Refresh();
            }
        }

        public ICommand AddTaskCommand
        {
            get
            {
                if (_addTaskCommand == null)
                {
                    _addTaskCommand = new RelayCommand(AddTask);
                }
                return _addTaskCommand;
            }
        }

        public ICommand EditTaskCommand
        {
            get
            {
                if (_editTaskCommand == null)
                { 
                    _editTaskCommand = new RelayCommand(EditTask, CanEditDeleteTask);
                }
                return _editTaskCommand;
            }
        }

        public ICommand DeleteTaskCommand
        {
            get
            {
                if (_deleteTaskCommand == null)
                {
                    _deleteTaskCommand = new RelayCommand(DeleteTask, CanEditDeleteTask);
                }
                return _deleteTaskCommand;
            }
        }

        public ICommand SaveCommand
        {
            get
            {
                if (_saveCommand == null)
                {
                    _saveCommand = new RelayCommand(SaveProject);
                }
                return _saveCommand;
            }
        }

        public ICommand LoadCommand
        {
            get
            {
                if (_loadCommand == null)
                {
                    _loadCommand = new RelayCommand(LoadProject);
                }
                return _loadCommand;
            }
        }

        public ICommand AutoScheduleCommand
        {
            get
            {
                if (_autoScheduleCommand == null)
                {
                    _autoScheduleCommand = new RelayCommand(AutoSchedule);
                }
                return _autoScheduleCommand;
            }
        }

        private bool TaskFilter(object item)
        {
            if (item is TaskItem task)
            {
                return (_filterStatus & task.Status) != 0;
            }
            return false;
        }

        private void AddTask(object parameter)
        {
            var dialog = new TaskEditDialog();
            dialog.Owner = Application.Current.MainWindow;
            if (dialog.ShowDialog() == true)
            {
                var newTask = dialog.EditedTask;
                _projectService.AddTask(newTask);
                SelectedTask = newTask;
            }
        }

        private void EditTask(object parameter)
        {
            if (SelectedTask == null) return;

            var dialog = new TaskEditDialog(SelectedTask);
            dialog.Owner = Application.Current.MainWindow;
            if (dialog.ShowDialog() == true)
            {
                _projectService.UpdateTask(SelectedTask);
            }
        }

        private bool CanEditDeleteTask(object parameter)
        {
            return SelectedTask != null;
        }

        private void DeleteTask(object parameter)
        {
            if (SelectedTask == null)
            {
                return;
            }
            _projectService.DeleteTask(SelectedTask.Id);
            SelectedTask = null;
        }

        private void SaveProject(object parameter)
        {
            SaveFileDialog dlg = new SaveFileDialog
            {
                Filter = "JSON files (*.json)|*.json|All files (*.*)|*.*",
                DefaultExt = ".json"
            };
            if (dlg.ShowDialog() == true)
            {
                _projectService.SaveToJson(dlg.FileName);
            }
        }

        private void LoadProject(object parameter)
        {
            OpenFileDialog dlg = new OpenFileDialog
            {
                Filter = "JSON files (*.json)|*.json|All files (*.*)|*.*"
            };
            if (dlg.ShowDialog() == true)
            {
                _projectService.LoadFromJson(dlg.FileName);
                _tasksView.Refresh();
            }
        }

        public event Action TasksRescheduled;

        private void AutoSchedule(object parameter)
        {
            _projectService.AutoSchedule();
            TasksRescheduled?.Invoke();
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class RelayCommand : ICommand
    {
        private readonly Action<object> _execute;
        private readonly Predicate<object> _canExecute;

        public RelayCommand(Action<object> execute, Predicate<object> canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        public bool CanExecute(object parameter)
        {
            return _canExecute == null || _canExecute(parameter);
        }

        public void Execute(object parameter)
        {
            _execute(parameter);
        }

        public event EventHandler CanExecuteChanged
        {
            add => CommandManager.RequerySuggested += value;
            remove => CommandManager.RequerySuggested -= value;
        }
    }
}
