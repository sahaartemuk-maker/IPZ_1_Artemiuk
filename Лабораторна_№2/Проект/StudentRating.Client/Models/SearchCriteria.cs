using System;

namespace StudentRating.Client.Models
{
    /// <summary>Критерії пошуку кращих студентів (прив'язані до форми пошуку).</summary>
    public class SearchCriteria : ObservableObject
    {
        private int? _course;
        private string _group;
        private string _subject;
        private double _minScore = 60;
        private int _topCount = 5;

        public int? Course { get { return _course; } set { _course = value; OnPropertyChanged(); } }
        public string Group { get { return _group; } set { _group = value; OnPropertyChanged(); } }
        public string Subject { get { return _subject; } set { _subject = value; OnPropertyChanged(); } }

        public double MinScore
        {
            get { return _minScore; }
            set
            {
                if (value < 0 || value > 100)
                    throw new ArgumentOutOfRangeException(nameof(MinScore), "Мінімальний бал: від 0 до 100");
                _minScore = value; OnPropertyChanged();
            }
        }

        public int TopCount
        {
            get { return _topCount; }
            set
            {
                if (value < 1 || value > 100)
                    throw new ArgumentOutOfRangeException(nameof(TopCount), "Кількість: від 1 до 100");
                _topCount = value; OnPropertyChanged();
            }
        }
    }
}
