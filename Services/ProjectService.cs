using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using TaskPlaner.Models;
using System.IO;

namespace TaskPlaner.Services
{
    public class ProjectService
    {
        private ObservableCollection<TaskItem> _tasks;
        private string _currentFilePath;

        public ProjectService()
        {
            _tasks = new ObservableCollection<TaskItem>();
            _currentFilePath = string.Empty;
        }

        public ObservableCollection<TaskItem> Tasks
        {
            get => _tasks;
            set => _tasks = value;
        }

        public void AddTask(TaskItem task)
        {
            if (task == null)
            {
                return;
            }

            if (string.IsNullOrEmpty(task.Id))
            {
                task.Id = Guid.NewGuid().ToString();
            }

            _tasks.Add(task);
        }

        public void UpdateTask(TaskItem updatedTask)
        {
            if (updatedTask == null || string.IsNullOrEmpty(updatedTask.Id))
            {
                return;
            }

            var existing = _tasks.FirstOrDefault(t => t.Id == updatedTask.Id);
            if (existing == null)
            {
                return;
            }

            var index = _tasks.IndexOf(existing);
            _tasks.RemoveAt(index);
            _tasks.Insert(index, updatedTask);
        }

        public void DeleteTask(string taskId)
        {
            if (string.IsNullOrEmpty(taskId))
            {
                return;
            }

            var task = _tasks.FirstOrDefault(t => t.Id == taskId);
            if (task != null)
            {
                _tasks.Remove(task);
            }
        }

        public TaskItem GetTaskById(string taskId)
        {
            return _tasks.FirstOrDefault(t => t.Id == taskId);
        }

        public void SaveToJson(string filePath)
        {
            var options = new JsonSerializerOptions { WriteIndented = true };
            string json = JsonSerializer.Serialize(_tasks, options);
            File.WriteAllText(filePath, json);
            _currentFilePath = filePath;
        }

        public void LoadFromJson(string filePath)
        {
            if (!File.Exists(filePath))
            {
                return;
            }

            string json = File.ReadAllText(filePath);
            var loadedTasks = JsonSerializer.Deserialize<ObservableCollection<TaskItem>>(json);
            if (loadedTasks != null)
            {
                _tasks.Clear();
                foreach (var task in loadedTasks)
                {
                    _tasks.Add(task);
                }
            }
            _currentFilePath = filePath;
        }

        public bool HasUnsavedChanges()
        {
            if (string.IsNullOrEmpty(_currentFilePath) || !File.Exists(_currentFilePath))
            {
                return true;
            }

            string existingJson = File.ReadAllText(_currentFilePath);
            string currentJson = JsonSerializer.Serialize(_tasks);
            return !string.Equals(existingJson, currentJson);
        }
    }
}
