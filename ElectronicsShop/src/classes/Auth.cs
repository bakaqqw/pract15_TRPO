using System.Windows;

namespace ElectronicsShop.classes
{
    public class Auth
    {
        public static bool IsManager { get; private set; }

        public static bool Login(string pin)
        {
            IsManager = pin == "1234";
            return IsManager;
        }

        public static void EnterVisitor()
        {
            IsManager = false;
        }
    }
}
