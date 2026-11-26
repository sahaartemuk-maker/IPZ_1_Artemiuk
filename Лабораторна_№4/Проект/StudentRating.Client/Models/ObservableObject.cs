using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace StudentRating.Client.Models
{
    /// <summary>Базовий клас для моделей з підтримкою Data Binding.</summary>
    public abstract class ObservableObject : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
