using System.Collections.ObjectModel;
using System.Linq;

namespace StudentRating.Client.Models
{
    public class Student : ObservableObject
    {
        private int _id;
        private string _lastName;
        private string _firstName;
        private string _group;
        private int _course;
        private int _place;

        public Student()
        {
            Grades = new ObservableCollection<Grade>();
            Grades.CollectionChanged += (s, e) => OnPropertyChanged(nameof(AverageScore));
        }

        public int Id { get { return _id; } set { _id = value; OnPropertyChanged(); } }

        public string LastName
        {
            get { return _lastName; }
            set { _lastName = value; OnPropertyChanged(); OnPropertyChanged(nameof(FullName)); }
        }

        public string FirstName
        {
            get { return _firstName; }
            set { _firstName = value; OnPropertyChanged(); OnPropertyChanged(nameof(FullName)); }
        }

        public string Group { get { return _group; } set { _group = value; OnPropertyChanged(); } }

        public int Course { get { return _course; } set { _course = value; OnPropertyChanged(); } }

        /// <summary>Місце в рейтингу.</summary>
        public int Place { get { return _place; } set { _place = value; OnPropertyChanged(); } }

        public ObservableCollection<Grade> Grades { get; private set; }

        public string FullName { get { return LastName + " " + FirstName; } }

        public double AverageScore
        {
            get { return Grades.Count == 0 ? 0 : System.Math.Round(Grades.Average(g => g.Score), 2); }
        }

        public void RefreshAverage() { OnPropertyChanged(nameof(AverageScore)); }
    }
}
