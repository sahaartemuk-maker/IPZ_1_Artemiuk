using System;

namespace StudentRating.Client.Models
{
    /// <summary>Оцінка студента з предмету (0..100).</summary>
    public class Grade : ObservableObject
    {
        private string _subject;
        private int _score;

        public string Subject
        {
            get { return _subject; }
            set { _subject = value; OnPropertyChanged(); }
        }

        public int Score
        {
            get { return _score; }
            set
            {
                if (value < 0 || value > 100)
                    throw new ArgumentOutOfRangeException(nameof(Score), "Оцінка має бути в діапазоні 0..100");
                _score = value;
                OnPropertyChanged();
            }
        }
    }
}
