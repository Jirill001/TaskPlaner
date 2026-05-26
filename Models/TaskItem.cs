using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace TaskPlaner.Models
{
    public class TaskItem : INotifyPropertyChanged
    {
        private string _id;
        private string _title;
        private string _description;
        private DateTime _startDate;
        private DateTime _endDate;
        private Priority _priority;
        private Status _status;
        private int _progress;
        private List<string> _predecessorIds;

        public string Id
        {
            get => _id;
            set { _id = value; OnPropertyChanged(); }
        }
        public string Title
        {
            get => _title;
            set { _title = value; OnPropertyChanged(); }
        }
        public string Description
        {
            get => _description;
            set { _description = value; OnPropertyChanged(); }
        }
        public DateTime StartDate
        {
            get => _startDate;
            set { _startDate = value; OnPropertyChanged(); }
        }
        public DateTime EndDate
        {
            get => _endDate;
            set { _endDate = value; OnPropertyChanged(); }
        }
        public Priority Priority
        {
            get => _priority;
            set { _priority = value; OnPropertyChanged(); }
        }
        public Status Status
        {
            get => _status;
            set { _status = value; OnPropertyChanged(); }
        }
        public int Progress
        {
            get => _progress;
            set { _progress = value; OnPropertyChanged(); }
        }
        public List<string> PredecessorIds
        {
            get => _predecessorIds;
            set { _predecessorIds = value; OnPropertyChanged(); }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public enum Priority
    {
        Low,
        Medium,
        High
    }

    public enum Status
    {
        Planned = 1,
        InProgress = 2,
        Completed = 4
    }
}