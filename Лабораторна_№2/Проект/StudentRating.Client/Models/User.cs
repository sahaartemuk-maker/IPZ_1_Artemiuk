namespace StudentRating.Client.Models
{
    public enum UserRole { Student, Teacher, Admin }

    public class User : ObservableObject
    {
        private string _login;
        private string _fullName;
        private UserRole _role;

        public string Login
        {
            get { return _login; }
            set { _login = value; OnPropertyChanged(); }
        }

        public string FullName
        {
            get { return _fullName; }
            set { _fullName = value; OnPropertyChanged(); }
        }

        public UserRole Role
        {
            get { return _role; }
            set { _role = value; OnPropertyChanged(); OnPropertyChanged(nameof(IsTeacher)); OnPropertyChanged(nameof(RoleName)); }
        }

        public bool IsTeacher { get { return Role == UserRole.Teacher || Role == UserRole.Admin; } }

        public string RoleName
        {
            get
            {
                switch (Role)
                {
                    case UserRole.Teacher: return "Викладач";
                    case UserRole.Admin: return "Адміністратор";
                    default: return "Студент";
                }
            }
        }
    }
}
