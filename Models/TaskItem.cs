using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TaskPlaner.Models
{
    public class TaskItem : INotifyPropertyChanged
    {
        private string _id;
        private string _title;
        private string _description;
        private DateTime _startDate;
        private DateTime _endDate;
        private Priority priority;
        private Status status;
        private int progress;
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
            set { _id = value; OnPropertyChanged(); }
        }
        public enum Priority
        {
            Low,
            Medium,
            High
        }
        public enum Status
        {
            Planned,
            InProgress,
            Completed
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        public void OnPropertyChanged()
        {

        }
    }
}
