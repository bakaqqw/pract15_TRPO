using System.ComponentModel;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using ElectronicsShop.classes;
using ElectronicsShop.Data;
using ElectronicsShop.Models;
using Microsoft.EntityFrameworkCore;

namespace ElectronicsShop.Views
{
    public partial class ProductEditorWindow : Window
    {
        public Product product = new Product();
        private bool isEdit;
        private bool isReadOnly;
        private bool isLoading = true;
        private bool isChanged;
        private bool isSaved;

        public ProductEditorWindow(Product? selectedProduct = null, bool readOnly = false)
        {
            InitializeComponent();
            isReadOnly = readOnly;

            if (selectedProduct != null)
            {
                isEdit = true;
                product.Id = selectedProduct.Id;
                product.Name = selectedProduct.Name;
                product.Description = selectedProduct.Description;
                product.Price = selectedProduct.Price;
                product.Stock = selectedProduct.Stock;
                product.Rating = selectedProduct.Rating;
                product.CreatedAt = selectedProduct.CreatedAt;
                product.CategoryId = selectedProduct.CategoryId;
                product.BrandId = selectedProduct.BrandId;

                foreach (Tag tag in selectedProduct.Tags)
                {
                    product.Tags.Add(tag);
                }

                Title = "Редактирование товара";
            }
            else
            {
                product.Name = "";
                product.Description = "";
                product.CreatedAt = DateOnly.FromDateTime(DateTime.Today);
                Title = "Добавление товара";
            }

            if (readOnly)
            {
                Title = "Просмотр товара";
                SaveButton.Visibility = Visibility.Collapsed;
                BackButton.Content = "Закрыть";
            }

            Heading.Text = Title;
            DataContext = product;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                using (ShopDbContext db = Database.GetContext())
                {
                    CategoryBox.ItemsSource = db.Categories.AsNoTracking().OrderBy(c => c.Name).ToList();
                    BrandBox.ItemsSource = db.Brands.AsNoTracking().OrderBy(b => b.Name).ToList();
                    TagsList.ItemsSource = db.Tags.AsNoTracking().OrderBy(t => t.Name).ToList();
                }

                CategoryBox.SelectedValue = product.CategoryId;
                BrandBox.SelectedValue = product.BrandId;
                CreatedPicker.SelectedDate = product.CreatedAt.ToDateTime(TimeOnly.MinValue);

                if (isEdit)
                {
                    PriceBox.Text = product.Price.ToString("F2");
                    StockBox.Text = product.Stock.ToString();
                    RatingBox.Text = product.Rating.ToString("F1");
                }

                foreach (Tag tag in TagsList.Items)
                {
                    if (product.Tags.Any(t => t.Id == tag.Id))
                    {
                        TagsList.SelectedItems.Add(tag);
                    }
                }

                FormGrid.IsEnabled = true;
                SaveButton.IsEnabled = true;
                StatusText.Text = "";

                if (isReadOnly)
                {
                    NameBox.IsReadOnly = true;
                    DescriptionBox.IsReadOnly = true;
                    PriceBox.IsReadOnly = true;
                    StockBox.IsReadOnly = true;
                    RatingBox.IsReadOnly = true;
                    CategoryBox.IsEnabled = false;
                    BrandBox.IsEnabled = false;
                    CreatedPicker.IsEnabled = false;
                    TagsList.IsEnabled = false;
                }

                isLoading = false;
                isChanged = false;
            }
            catch (Exception ex)
            {
                StatusText.Text = "Не удалось загрузить справочники";
                MessageBox.Show(this, DatabaseError.GetMessage(ex), "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private bool IsValid()
        {
            if (string.IsNullOrWhiteSpace(product.Name))
            {
                MessageBox.Show(this, "Введите название товара", "Ошибка");
                return false;
            }

            if (string.IsNullOrWhiteSpace(product.Description))
            {
                MessageBox.Show(this, "Введите описание товара", "Ошибка");
                return false;
            }

            decimal price;
            if (!InputRules.TryDecimal(PriceBox.Text, 2, out price) || price < 0 || price > InputRules.MaxPrice)
            {
                MessageBox.Show(this, "Введите цену от 0 до 9 999 999 999,99, до 2 знаков после запятой", "Ошибка");
                return false;
            }

            int stock;
            if (!int.TryParse(StockBox.Text.Trim(), NumberStyles.None, CultureInfo.InvariantCulture, out stock))
            {
                MessageBox.Show(this, "Остаток должен быть целым числом от 0 до 2 147 483 647", "Ошибка");
                return false;
            }

            decimal rating;
            if (!InputRules.TryDecimal(RatingBox.Text, 1, out rating) || rating < 0 || rating > 5)
            {
                MessageBox.Show(this, "Введите рейтинг от 0 до 5, до 1 знака после запятой", "Ошибка");
                return false;
            }

            DateTime date;
            if (CreatedPicker.SelectedDate == null || !DateTime.TryParse(CreatedPicker.Text, out date))
            {
                MessageBox.Show(this, "Выберите правильную дату", "Ошибка");
                return false;
            }

            Category? category = CategoryBox.SelectedItem as Category;
            Brand? brand = BrandBox.SelectedItem as Brand;
            if (category == null || brand == null)
            {
                MessageBox.Show(this, "Выберите категорию и бренд", "Ошибка");
                return false;
            }

            product.Price = price;
            product.Stock = stock;
            product.Rating = rating;
            product.CreatedAt = DateOnly.FromDateTime(date);
            product.CategoryId = category.Id;
            product.BrandId = brand.Id;
            product.Tags.Clear();
            foreach (Tag tag in TagsList.SelectedItems)
            {
                product.Tags.Add(tag);
            }

            return true;
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            if (isReadOnly || isLoading || !IsValid())
            {
                return;
            }

            if (isEdit && MessageBox.Show(this, "Сохранить изменения товара? Предыдущие значения будут заменены.",
                "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.No) != MessageBoxResult.Yes)
            {
                return;
            }

            try
            {
                InputRules.CheckProduct(product);

                using (ShopDbContext db = Database.GetContext())
                {
                    if (!db.Categories.Any(c => c.Id == product.CategoryId)
                        || !db.Brands.Any(b => b.Id == product.BrandId))
                    {
                        throw new Exception("Категория или бренд уже удалены. Откройте форму заново");
                    }

                    List<int> tagIds = new List<int>();
                    foreach (Tag tag in product.Tags)
                    {
                        tagIds.Add(tag.Id);
                    }
                    List<Tag> tags = db.Tags.Where(t => tagIds.Contains(t.Id)).ToList();
                    if (tags.Count != tagIds.Count)
                    {
                        throw new Exception("Один из тегов уже удалён. Откройте форму заново");
                    }

                    Product? savedProduct;
                    if (isEdit)
                    {
                        savedProduct = db.Products.Include(p => p.Tags).FirstOrDefault(p => p.Id == product.Id);
                        if (savedProduct == null)
                        {
                            throw new Exception("Товар уже удалён. Обновите список");
                        }
                    }
                    else
                    {
                        savedProduct = new Product();
                        db.Products.Add(savedProduct);
                    }

                    savedProduct.Name = product.Name.Trim();
                    savedProduct.Description = product.Description.Trim();
                    savedProduct.Price = product.Price;
                    savedProduct.Stock = product.Stock;
                    savedProduct.Rating = product.Rating;
                    savedProduct.CreatedAt = product.CreatedAt;
                    savedProduct.CategoryId = product.CategoryId;
                    savedProduct.BrandId = product.BrandId;
                    savedProduct.Tags.Clear();
                    foreach (Tag tag in tags)
                    {
                        savedProduct.Tags.Add(tag);
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

        private void Field_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!isLoading)
            {
                isChanged = true;
            }
        }

        private void Field_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!isLoading)
            {
                isChanged = true;
            }
        }

        private void GoBack_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void Window_Closing(object? sender, CancelEventArgs e)
        {
            if (!isReadOnly && !isSaved && isChanged)
            {
                MessageBoxResult result = MessageBox.Show(this, "Закрыть форму без сохранения изменений?", "Подтверждение",
                    MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.No);
                if (result != MessageBoxResult.Yes)
                {
                    e.Cancel = true;
                }
            }
        }
    }
}
