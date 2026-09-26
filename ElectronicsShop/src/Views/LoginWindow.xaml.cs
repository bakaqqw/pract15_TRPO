using System.Windows;
using ElectronicsShop.classes;

namespace ElectronicsShop.Views
{
    public partial class LoginWindow : Window
    {
        public LoginWindow()
        {
            InitializeComponent();
        }

        private void Login_Click(object sender, RoutedEventArgs e)
        {
            if (!Auth.Login(PinBox.Password))
            {
                ErrorText.Text = "Неверный ПИН-код";
                PinBox.Clear();
                PinBox.Focus();
                return;
            }

            OpenCatalog();
        }

        private void Visitor_Click(object sender, RoutedEventArgs e)
        {
            Auth.EnterVisitor();
            OpenCatalog();
        }

        private void OpenCatalog()
        {
            try
            {
                MainWindow window = new MainWindow();
                Application.Current.MainWindow = window;
                window.Show();
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, DatabaseError.GetMessage(ex), "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
