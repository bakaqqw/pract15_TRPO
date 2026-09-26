using System.ComponentModel;
using System.Windows;
using ElectronicsShop.classes;
using ElectronicsShop.Data;
using ElectronicsShop.Models;

namespace ElectronicsShop.Views
{
    public partial class NameEditorWindow : Window
    {
        public DictionaryItem item = new DictionaryItem();
        private string type;
        private string originalName = "";
        private bool isEdit;
        private bool isSaved;

        public NameEditorWindow(string dictionaryType, DictionaryItem? selectedItem = null)
        {
            Auth.CheckManager();
            InitializeComponent();
            type = dictionaryType;
            if (selectedItem != null)
            {
                item.Id = selectedItem.Id;
                item.Name = selectedItem.Name;
                originalName = selectedItem.Name;
                isEdit = true;
            }

            string name;
            switch (type)
            {
                case "categories":
                    name = "категории";
                    break;
                case "brands":
                    name = "бренда";
                    break;
                case "tags":
                    name = "тега";
                    break;
                default:
                    throw new ArgumentException("Неизвестный справочник");
            }

            if (isEdit)
            {
                Title = "Редактирование " + name;
            }
            else
            {
                Title = "Добавление " + name;
            }

            Heading.Text = Title;
            DataContext = item;
        }

        private bool IsValid()
        {
            if (string.IsNullOrWhiteSpace(item.Name))
            {
                MessageBox.Show(this, "Введите название", "Ошибка");
                return false;
            }
            return true;
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            if (!IsValid())
            {
                return;
            }

            if (isEdit && MessageBox.Show(this, "Сохранить новое название? Оно изменится во всех связанных товарах.",
                "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.No) != MessageBoxResult.Yes)
            {
                return;
            }

            try
            {
                Auth.CheckManager();
                InputRules.CheckName(item.Name);
                string name = item.Name.Trim();

                using (ShopDbContext db = Database.GetContext())
                {
                    switch (type)
                    {
                        case "categories":
                            if (db.Categories.Any(c => c.Name.ToLower() == name.ToLower() && c.Id != item.Id))
                            {
                                throw new Exception("Такое название уже существует");
                            }
                            if (isEdit)
                            {
                                Category? category = db.Categories.FirstOrDefault(c => c.Id == item.Id);
                                if (category == null)
                                {
                                    throw new Exception("Категория уже удалена");
                                }
                                category.Name = name;
                            }
                            else
                            {
                                Category category = new Category();
                                category.Name = name;
                                db.Categories.Add(category);
                            }
                            break;
                        case "brands":
                            if (db.Brands.Any(b => b.Name.ToLower() == name.ToLower() && b.Id != item.Id))
                            {
                                throw new Exception("Такое название уже существует");
                            }
                            if (isEdit)
                            {
                                Brand? brand = db.Brands.FirstOrDefault(b => b.Id == item.Id);
                                if (brand == null)
                                {
                                    throw new Exception("Бренд уже удалён");
                                }
                                brand.Name = name;
                            }
                            else
                            {
                                Brand brand = new Brand();
                                brand.Name = name;
                                db.Brands.Add(brand);
                            }
                            break;
                        case "tags":
                            if (db.Tags.Any(t => t.Name.ToLower() == name.ToLower() && t.Id != item.Id))
                            {
                                throw new Exception("Такое название уже существует");
                            }
                            if (isEdit)
                            {
                                Tag? tag = db.Tags.FirstOrDefault(t => t.Id == item.Id);
                                if (tag == null)
                                {
                                    throw new Exception("Тег уже удалён");
                                }
                                tag.Name = name;
                            }
                            else
                            {
                                Tag tag = new Tag();
                                tag.Name = name;
                                db.Tags.Add(tag);
                            }
                            break;
                    }
                    db.SaveChanges();
                }

                isSaved = true;
                DialogResult = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, DatabaseError.GetMessage(ex), "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void GoBack_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void Window_Closing(object? sender, CancelEventArgs e)
        {
            if (!isSaved && NameBox.Text != originalName)
            {
                if (MessageBox.Show(this, "Закрыть форму без сохранения изменений?", "Подтверждение",
                    MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.No) != MessageBoxResult.Yes)
                {
                    e.Cancel = true;
                }
            }
        }
    }
}
