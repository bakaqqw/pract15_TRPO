using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using ElectronicsShop.classes;
using ElectronicsShop.Data;
using ElectronicsShop.Models;
using Microsoft.EntityFrameworkCore;

namespace ElectronicsShop.Views
{
    public partial class MainWindow : Window
    {
        public ObservableCollection<Product> Products { get; set; } = new ObservableCollection<Product>();
        public Product? SelectedProduct { get; set; }
        public ObservableCollection<Category> Categories { get; set; } = new ObservableCollection<Category>();
        public ObservableCollection<Brand> Brands { get; set; } = new ObservableCollection<Brand>();
        private ICollectionView productsView;
        private bool isLoading = true;
        private bool validPrice = true;
        private string search = "";
        private int categoryId;
        private int brandId;
        private decimal? minPrice;
        private decimal? maxPrice;

        public MainWindow()
        {
            productsView = CollectionViewSource.GetDefaultView(Products);
            productsView.Filter = FilterProducts;

            InitializeComponent();
            DataContext = this;

            if (Auth.IsManager)
            {
                RoleText.Text = "Менеджер";
            }
            else
            {
                RoleText.Text = "Посетитель";
                ManagerPanel.Visibility = Visibility.Collapsed;
                EditButton.Visibility = Visibility.Collapsed;
                DeleteButton.Visibility = Visibility.Collapsed;
            }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            LoadProducts();
        }

        private void LoadProducts()
        {
            int oldCategory = categoryId;
            int oldBrand = brandId;
            int oldProduct = 0;
            if (SelectedProduct != null)
            {
                oldProduct = SelectedProduct.Id;
            }

            isLoading = true;
            try
            {
                using (ShopDbContext db = Database.GetContext())
                {
                    List<Product> products = db.Products.AsNoTracking()
                        .Include(p => p.Category).Include(p => p.Brand).Include(p => p.Tags)
                        .OrderBy(p => p.Id).ToList();
                    List<Category> categories = db.Categories.AsNoTracking().OrderBy(c => c.Name).ToList();
                    List<Brand> brands = db.Brands.AsNoTracking().OrderBy(b => b.Name).ToList();

                    Products.Clear();
                    foreach (Product product in products)
                    {
                        Products.Add(product);
                    }

                    Categories.Clear();
                    Categories.Add(new Category { Id = 0, Name = "Все категории" });
                    foreach (Category category in categories)
                    {
                        Categories.Add(category);
                    }

                    Brands.Clear();
                    Brands.Add(new Brand { Id = 0, Name = "Все бренды" });
                    foreach (Brand brand in brands)
                    {
                        Brands.Add(brand);
                    }
                }

                if (!Categories.Any(c => c.Id == oldCategory))
                {
                    oldCategory = 0;
                }
                if (!Brands.Any(b => b.Id == oldBrand))
                {
                    oldBrand = 0;
                }

                CategoryBox.SelectedValue = oldCategory;
                BrandBox.SelectedValue = oldBrand;
                StatusText.Text = "Данные обновлены";
            }
            catch (Exception ex)
            {
                StatusText.Text = "Не удалось загрузить данные. Проверьте подключение и нажмите «Обновить»";
                MessageBox.Show(this, DatabaseError.GetMessage(ex), "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                isLoading = false;
            }

            RefreshFilters();
            ApplySort();
            ProductsList.SelectedItem = Products.FirstOrDefault(p => p.Id == oldProduct);
        }

        private bool FilterProducts(object obj)
        {
            Product? product = obj as Product;
            if (product == null || !validPrice)
            {
                return false;
            }

            return InputRules.Matches(product, search, categoryId, brandId, minPrice, maxPrice);
        }

        private void RefreshFilters()
        {
            if (isLoading)
            {
                return;
            }

            search = SearchBox.Text;
            categoryId = 0;
            brandId = 0;

            Category? category = CategoryBox.SelectedItem as Category;
            Brand? brand = BrandBox.SelectedItem as Brand;
            if (category != null)
            {
                categoryId = category.Id;
            }
            if (brand != null)
            {
                brandId = brand.Id;
            }

            string error;
            validPrice = InputRules.TryPriceRange(PriceFromBox.Text, PriceToBox.Text,
                out minPrice, out maxPrice, out error);
            FilterError.Text = error;
            productsView.Refresh();
            InfoProducts.Content = "Всего товаров: " + Products.Count;
            InfoShown.Content = "Показано: " + ProductsList.Items.Count;

            if (ProductsList.Items.Count == 0)
            {
                EmptyText.Visibility = Visibility.Visible;
            }
            else
            {
                EmptyText.Visibility = Visibility.Collapsed;
            }
        }

        private void Search_TextChanged(object sender, TextChangedEventArgs e)
        {
            RefreshFilters();
        }

        private void Filter_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            RefreshFilters();
        }

        private void Sort_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!isLoading)
            {
                ApplySort();
            }
        }

        private void ApplySort()
        {
            ComboBoxItem? item = SortBox.SelectedItem as ComboBoxItem;
            if (item == null)
            {
                return;
            }

            productsView.SortDescriptions.Clear();
            switch (item.Tag.ToString())
            {
                case "NameAsc":
                    productsView.SortDescriptions.Add(new SortDescription("Name", ListSortDirection.Ascending));
                    break;
                case "NameDesc":
                    productsView.SortDescriptions.Add(new SortDescription("Name", ListSortDirection.Descending));
                    break;
                case "PriceAsc":
                    productsView.SortDescriptions.Add(new SortDescription("Price", ListSortDirection.Ascending));
                    break;
                case "PriceDesc":
                    productsView.SortDescriptions.Add(new SortDescription("Price", ListSortDirection.Descending));
                    break;
                case "StockAsc":
                    productsView.SortDescriptions.Add(new SortDescription("Stock", ListSortDirection.Ascending));
                    break;
                case "StockDesc":
                    productsView.SortDescriptions.Add(new SortDescription("Stock", ListSortDirection.Descending));
                    break;
            }
            productsView.SortDescriptions.Add(new SortDescription("Id", ListSortDirection.Ascending));
        }

        private void Reset_Click(object sender, RoutedEventArgs e)
        {
            isLoading = true;
            SearchBox.Clear();
            PriceFromBox.Clear();
            PriceToBox.Clear();
            CategoryBox.SelectedValue = 0;
            BrandBox.SelectedValue = 0;
            SortBox.SelectedIndex = 0;
            isLoading = false;
            RefreshFilters();
            ApplySort();
        }

        private void Create_Click(object sender, RoutedEventArgs e)
        {
            if (!Auth.IsManager)
            {
                return;
            }

            ProductEditorWindow window = new ProductEditorWindow();
            window.Owner = this;
            if (window.ShowDialog() == true)
            {
                LoadProducts();
            }
        }

        private void Change_Click(object sender, RoutedEventArgs e)
        {
            if (!Auth.IsManager)
            {
                return;
            }
            if (SelectedProduct == null)
            {
                MessageBox.Show(this, "Выберите товар", "Редактирование");
                return;
            }

            ProductEditorWindow window = new ProductEditorWindow(SelectedProduct);
            window.Owner = this;
            if (window.ShowDialog() == true)
            {
                LoadProducts();
            }
        }

        private void View_Click(object sender, RoutedEventArgs e)
        {
            ViewProduct();
        }

        private void ViewProduct()
        {
            if (SelectedProduct == null)
            {
                MessageBox.Show(this, "Выберите товар", "Просмотр");
                return;
            }

            ProductEditorWindow window = new ProductEditorWindow(SelectedProduct, true);
            window.Owner = this;
            window.ShowDialog();
        }

        private void Delete_Click(object sender, RoutedEventArgs e)
        {
            if (!Auth.IsManager)
            {
                return;
            }
            if (SelectedProduct == null)
            {
                MessageBox.Show(this, "Выберите товар", "Удаление");
                return;
            }

            if (MessageBox.Show(this, $"Удалить товар «{SelectedProduct.Name}»?\nОтменить удаление нельзя.",
                "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.No) != MessageBoxResult.Yes)
            {
                return;
            }

            try
            {
                using (ShopDbContext db = Database.GetContext())
                {
                    Product? product = db.Products.FirstOrDefault(p => p.Id == SelectedProduct.Id);
                    if (product == null)
                    {
                        throw new Exception("Товар уже удалён. Обновите список");
                    }

                    db.Products.Remove(product);
                    db.SaveChanges();
                }
                LoadProducts();
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, DatabaseError.GetMessage(ex), "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Categories_Click(object sender, RoutedEventArgs e)
        {
            OpenDictionary("categories");
        }

        private void Brands_Click(object sender, RoutedEventArgs e)
        {
            OpenDictionary("brands");
        }

        private void Tags_Click(object sender, RoutedEventArgs e)
        {
            OpenDictionary("tags");
        }

        private void OpenDictionary(string type)
        {
            if (!Auth.IsManager)
            {
                return;
            }

            DictionaryWindow window = new DictionaryWindow(type);
            window.Owner = this;
            window.ShowDialog();
            LoadProducts();
        }

        private void Refresh_Click(object sender, RoutedEventArgs e)
        {
            LoadProducts();
        }

        private void GoBack_Click(object sender, RoutedEventArgs e)
        {
            Auth.EnterVisitor();
            LoginWindow window = new LoginWindow();
            Application.Current.MainWindow = window;
            window.Show();
            Close();
        }
    }
}
