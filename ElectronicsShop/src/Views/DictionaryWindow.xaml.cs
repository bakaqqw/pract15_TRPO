using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using ElectronicsShop.classes;
using ElectronicsShop.Data;
using ElectronicsShop.Models;
using Microsoft.EntityFrameworkCore;

namespace ElectronicsShop.Views
{
    public partial class DictionaryWindow : Window
    {
        public ObservableCollection<DictionaryItem> Items { get; set; } = new ObservableCollection<DictionaryItem>();
        public DictionaryItem? SelectedItem { get; set; }
        private string type;

        public DictionaryWindow(string dictionaryType)
        {
            InitializeComponent();
            type = dictionaryType;
            DataContext = this;

            switch (type)
            {
                case "categories":
                    Title = "Категории товаров";
                    break;
                case "brands":
                    Title = "Бренды товаров";
                    break;
                case "tags":
                    Title = "Теги товаров";
                    break;
                default:
                    throw new ArgumentException("Неизвестный справочник");
            }
            Heading.Text = Title;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            LoadItems();
        }

        private void LoadItems()
        {
            try
            {
                List<DictionaryItem> list = new List<DictionaryItem>();
                using (ShopDbContext db = Database.GetContext())
                {
                    switch (type)
                    {
                        case "categories":
                            foreach (Category category in db.Categories.AsNoTracking().OrderBy(c => c.Name).ToList())
                            {
                                list.Add(new DictionaryItem { Id = category.Id, Name = category.Name });
                            }
                            break;
                        case "brands":
                            foreach (Brand brand in db.Brands.AsNoTracking().OrderBy(b => b.Name).ToList())
                            {
                                list.Add(new DictionaryItem { Id = brand.Id, Name = brand.Name });
                            }
                            break;
                        case "tags":
                            foreach (Tag tag in db.Tags.AsNoTracking().OrderBy(t => t.Name).ToList())
                            {
                                list.Add(new DictionaryItem { Id = tag.Id, Name = tag.Name });
                            }
                            break;
                    }
                }

                Items.Clear();
                foreach (DictionaryItem item in list)
                {
                    Items.Add(item);
                }
                InfoItems.Content = "Всего записей: " + Items.Count;
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, DatabaseError.GetMessage(ex), "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Create_Click(object sender, RoutedEventArgs e)
        {
            NameEditorWindow window = new NameEditorWindow(type);
            window.Owner = this;
            if (window.ShowDialog() == true)
            {
                LoadItems();
            }
        }

        private void Change_Click(object sender, RoutedEventArgs e)
        {
            EditItem();
        }

        private void Items_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            var row = ItemsControl.ContainerFromElement(ItemsList, e.OriginalSource as DependencyObject);
            if (row is ListViewItem)
            {
                EditItem();
            }
        }

        private void EditItem()
        {
            if (SelectedItem == null)
            {
                MessageBox.Show(this, "Выберите запись", "Редактирование");
                return;
            }

            NameEditorWindow window = new NameEditorWindow(type, SelectedItem);
            window.Owner = this;
            if (window.ShowDialog() == true)
            {
                LoadItems();
            }
        }

        private void Delete_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedItem == null)
            {
                MessageBox.Show(this, "Выберите запись", "Удаление");
                return;
            }

            string message = $"Удалить «{SelectedItem.Name}»?\nОтменить удаление нельзя.";
            if (type == "tags")
            {
                message += "\nТег будет снят со всех товаров.";
            }

            if (MessageBox.Show(this, message, "Подтверждение", MessageBoxButton.YesNo,
                MessageBoxImage.Question, MessageBoxResult.No) != MessageBoxResult.Yes)
            {
                return;
            }

            try
            {
                using (ShopDbContext db = Database.GetContext())
                {
                    switch (type)
                    {
                        case "categories":
                            if (db.Products.Any(p => p.CategoryId == SelectedItem.Id))
                            {
                                throw new Exception("Нельзя удалить категорию, пока есть связанные товары");
                            }
                            Category? category = db.Categories.FirstOrDefault(c => c.Id == SelectedItem.Id);
                            if (category == null)
                            {
                                throw new Exception("Категория уже удалена");
                            }
                            db.Categories.Remove(category);
                            break;
                        case "brands":
                            if (db.Products.Any(p => p.BrandId == SelectedItem.Id))
                            {
                                throw new Exception("Нельзя удалить бренд, пока есть связанные товары");
                            }
                            Brand? brand = db.Brands.FirstOrDefault(b => b.Id == SelectedItem.Id);
                            if (brand == null)
                            {
                                throw new Exception("Бренд уже удалён");
                            }
                            db.Brands.Remove(brand);
                            break;
                        case "tags":
                            Tag? tag = db.Tags.FirstOrDefault(t => t.Id == SelectedItem.Id);
                            if (tag == null)
                            {
                                throw new Exception("Тег уже удалён");
                            }
                            db.Tags.Remove(tag);
                            break;
                    }
                    db.SaveChanges();
                }
                LoadItems();
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, DatabaseError.GetMessage(ex), "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Refresh_Click(object sender, RoutedEventArgs e)
        {
            LoadItems();
        }

        private void GoBack_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
