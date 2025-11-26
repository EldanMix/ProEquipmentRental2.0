using System; // Базовые типы и исключения.
using System.Data; // Работа с DataTable (таблицы в памяти).
using System.Diagnostics; // Работа с процессами (для открытия ссылок).
using System.IO; // Работа с файловой системой (сохранение/загрузка избранного).
using System.Text.Json; // Сериализация/десериализация JSON (для избранного).
using System.Windows; // Основные классы WPF.
using System.Windows.Controls; // Элементы управления (Button, TextBlock и т.д.).
using System.Windows.Media; // Графика, включая трансформации.
using System.Windows.Media.Animation; // Анимации (для разворачивания/сворачивания и кнопки избранного).
using System.Windows.Navigation; // События навигации (для Hyperlink).
using System.Collections.Generic; // Коллекции (HashSet).
using System.Linq; // Методы расширения (LINQ).

namespace ProEquipmentRental
{
    // Главное окно приложения. 'partial' связывает код с XAML.
    public partial class MainWindow : Window
    {
        // --- Поля класса ---

        private DataTable _shopTable; // Кэш данных всех магазинов, загруженных из БД.
        private HashSet<int> _favoriteIds = new HashSet<int>(); // Набор ID магазинов, добавленных в избранное.
        private const string FavoritesFile = "favorites.json"; // Имя файла для сохранения избранного.
        private bool _showingFavorites = false; // Флаг: true, если сейчас отображаются только избранные.

        // --- Конструктор ---

        public MainWindow()
        {
            InitializeComponent(); // Инициализация UI из XAML.
            LoadFavorites(); // Загружаем список избранных ID из файла.
            LoadShops(); // Загружаем все магазины из базы данных.
        }

        // --- Методы загрузки и поиска ---

        // Загружает магазины из БД, привязывает к списку и обновляет UI.
        private void LoadShops(string search = "")
        {
            try
            {
                // Получаем данные. Предполагается, что Database.GetShops обращается к БД.
                _shopTable = Database.GetShops(search);
                if (_shopTable == null)
                    throw new Exception("Таблица не загружена.");

                // Привязываем таблицу к ItemsControl (ShopList) для отображения.
                ShopList.ItemsSource = _shopTable.DefaultView;

                // Обновляем визуальные состояния кнопок избранного (сердечки)
                // с небольшой задержкой, чтобы контейнеры элементов успели сгенерироваться.
                ShopList.Dispatcher.BeginInvoke(new Action(() => RefreshShopListVisuals()));
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка загрузки данных: " + ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Обработчик кнопки "Поиск".
        private void Search_Click(object sender, RoutedEventArgs e)
        {
            // Перезагружаем магазины с текстом из поля поиска.
            LoadShops(SearchBox.Text);
        }

        // --- Методы сортировки ---

        // Обработчик кнопки "Фильтр".
        private void FilterButton_Click(object sender, RoutedEventArgs e)
        {
            var fw = new FilterWindow(); // Создаем окно фильтрации.
            fw.Owner = this;

            // Открываем окно как модальный диалог.
            if (fw.ShowDialog() == true)
            {
                string sortOption = fw.SortOption;
                if (_shopTable != null)
                {
                    var view = _shopTable.DefaultView;
                    // Устанавливаем свойство Sort для DataView на основе выбранной опции.
                    switch (sortOption)
                    {
                        case "name_asc": view.Sort = "name ASC"; break;
                        case "name_desc": view.Sort = "name DESC"; break;
                        case "rating_desc": view.Sort = "rating DESC"; break;
                        case "rating_asc": view.Sort = "rating ASC"; break;
                        default: view.Sort = ""; break;
                    }
                    // Перепривязка обновленной View.
                    ShopList.ItemsSource = view;
                    // Обновляем сердечки после сортировки.
                    ShopList.Dispatcher.BeginInvoke(new Action(() => RefreshShopListVisuals()));
                }
            }
        }

        // --- Внешние ссылки ---

        // Обработчик нажатия на Hyperlink.
        private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            try
            {
                // Запускаем внешний процесс (браузер) для открытия URI.
                Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri) { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                MessageBox.Show("Не удалось открыть ссылку: " + ex.Message);
            }
            e.Handled = true; // Отменяем стандартную обработку WPF.
        }

        // --- Анимация разворачивания/сворачивания деталей ---

        // Обработчик нажатия кнопки "Показать/Скрыть детали".
        private void ToggleDetails_Click(object sender, RoutedEventArgs e)
        {
            if (!(sender is Button btn)) return;

            // Ищем родительский Border и элемент с деталями по имени ("DetailsBorder").
            var parentBorder = btn.Tag as Border ?? FindAncestor<Border>(btn);
            if (parentBorder == null) return;
            var details = FindDescendantByName(parentBorder, "DetailsBorder") as Border;
            if (details == null) return;

            var arrow = btn.Content as TextBlock; // Получаем стрелку.

            bool isVisible = details.Visibility == Visibility.Visible;

            if (isVisible)
            {
                // Анимация сворачивания (с текущей высоты до 0).
                double currentHeight = details.ActualHeight > 0 ? details.ActualHeight : details.DesiredSize.Height;
                var anim = new DoubleAnimation(currentHeight, 0, TimeSpan.FromMilliseconds(250))
                {
                    EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseInOut }
                };
                anim.Completed += (s, ev) =>
                {
                    // После завершения анимации скрываем и сбрасываем Height.
                    details.Visibility = Visibility.Collapsed;
                    details.BeginAnimation(HeightProperty, null);
                    details.Height = Double.NaN;
                    if (arrow != null) arrow.Text = "▼"; // Меняем стрелку на "вниз".
                };
                details.BeginAnimation(HeightProperty, anim);
            }
            else
            {
                // Анимация разворачивания (с 0 до нужной высоты).
                details.Visibility = Visibility.Visible;
                details.Height = 0;
                // Измеряем, чтобы узнать целевую высоту.
                details.Measure(new Size(parentBorder.ActualWidth, double.PositiveInfinity));
                double targetHeight = details.DesiredSize.Height;

                var anim = new DoubleAnimation(0, targetHeight, TimeSpan.FromMilliseconds(300))
                {
                    EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
                };
                anim.Completed += (s, ev) =>
                {
                    // После завершения анимации сбрасываем Height.
                    details.BeginAnimation(HeightProperty, null);
                    details.Height = Double.NaN;
                    if (arrow != null) arrow.Text = "▲"; // Меняем стрелку на "вверх".
                };
                details.BeginAnimation(HeightProperty, anim);
            }
        }

        // --- Избранное (Favorites) ---
        // Загружает ID избранных магазинов из JSON-файла.
        private void LoadFavorites()
        {
            try
            {
                if (File.Exists(FavoritesFile))
                {
                    string json = File.ReadAllText(FavoritesFile);
                    // Десериализуем JSON в HashSet<int>.
                    _favoriteIds = JsonSerializer.Deserialize<HashSet<int>>(json) ?? new HashSet<int>();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка загрузки избранных: " + ex.Message);
                _favoriteIds = new HashSet<int>(); // Сбрасываем при ошибке.
            }
        }

        // Сохраняет ID избранных магазинов в JSON-файл.
        private void SaveFavorites()
        {
            try
            {
                // Сериализуем HashSet<int> в JSON с форматированием.
                string json = JsonSerializer.Serialize(_favoriteIds, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(FavoritesFile, json);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка сохранения избранных: " + ex.Message);
            }
        }

        // Обработчик нажатия кнопки "сердечко" в карточке магазина.
        private void AddToFavorites_Click(object sender, RoutedEventArgs e)
        {
            if (!(sender is Button btn)) return;

            int shopId = -1;
            // Сложный код для извлечения ID магазина из Tag или DataContext кнопки.
            try
            {
                if (btn.Tag is int tid) shopId = tid;
                else if (btn.Tag is string tstr && int.TryParse(tstr, out int parsed)) shopId = parsed;
                // Если DataContext — это DataRowView (из DataTable).
                else if (btn.DataContext is System.Data.DataRowView drv && drv.Row.Table.Columns.Contains("id"))
                    shopId = Convert.ToInt32(drv["id"]);
                // Попытка извлечь 'id' через рефлексию (для универсальности).
                else
                {
                    var dc = btn.DataContext;
                    if (dc != null)
                    {
                        var prop = dc.GetType().GetProperty("id") ?? dc.GetType().GetProperty("Id") ?? dc.GetType().GetProperty("ID");
                        if (prop != null)
                        {
                            var val = prop.GetValue(dc);
                            if (val != null && int.TryParse(val.ToString(), out int pid)) shopId = pid;
                        }
                    }
                }
            }
            catch { shopId = -1; } // Игнорируем ошибки при извлечении ID.

            if (shopId <= 0) return;

            bool isFavorite = _favoriteIds.Contains(shopId);

            if (isFavorite)
            {
                // Удаление из избранного.
                _favoriteIds.Remove(shopId);
                btn.Content = "❤️"; // Меняем на пустое сердечко.

                // Если показываем только избранное, удаляем элемент из текущего списка.
                if (_showingFavorites && ShopList.ItemsSource is DataView dv)
                {
                    var rowToRemove = dv.Table.AsEnumerable()
                        .FirstOrDefault(r => r.Field<int>("id") == shopId);
                    if (rowToRemove != null)
                    {
                        dv.Table.Rows.Remove(rowToRemove);
                        // Если список избранного опустел, показываем сообщение.
                        if (dv.Table.Rows.Count == 0) MessageText.Visibility = Visibility.Visible;
                    }
                }
            }
            else
            {
                // Добавление в избранное.
                _favoriteIds.Add(shopId);
                btn.Content = "💔"; // Меняем на заполненное сердечко.
                AnimateFavoriteButton(btn); // Запускаем анимацию.
            }

            SaveFavorites(); // Сохраняем изменения в файл.
                             // Обновляем визуальное состояние всех кнопок.
            ShopList.Dispatcher.BeginInvoke(new Action(() => RefreshShopListVisuals()));
        }

        // Обработчик верхней кнопки "Избранное" (переключение режимов).
        private void Favorites_Click(object sender, RoutedEventArgs e)
        {
            if (_shopTable == null)
            {
                LoadShops(); // Загружаем все, если кэш пуст.
                return;
            }

            if (_showingFavorites)
            {
                // Режим "Все магазины": Показываем полный список.
                LoadShops();
                _showingFavorites = false;
                MessageText.Visibility = Visibility.Collapsed;
                if (sender is Button b) b.Content = "Избранное"; // Меняем текст кнопки.
            }
            else
            {
                // Режим "Только избранное":
                var favTable = _shopTable.Clone(); // Копируем схему таблицы.
                // LINQ-запрос для выбора только избранных строк.
                foreach (DataRow row in _shopTable.Rows)
                {
                    if (row.Table.Columns.Contains("id") &&
                        int.TryParse(row["id"].ToString(), out int id) &&
                        _favoriteIds.Contains(id))
                    {
                        favTable.ImportRow(row); // Копируем строку в новую таблицу.
                    }
                }

                ShopList.ItemsSource = favTable.DefaultView; // Привязываем таблицу избранного.
                _showingFavorites = true;

                if (sender is Button b) b.Content = "Избранное"; // Меняем текст кнопки.

                // Показываем/скрываем сообщение, если список избранного пуст.
                MessageText.Visibility = (favTable.Rows.Count == 0) ? Visibility.Visible : Visibility.Collapsed;

                RefreshShopListVisuals(); // Обновляем сердечки.
            }
        }

        // Обновляет текст кнопок "Добавить в избранное" на всех видимых элементах списка.
        private void RefreshShopListVisuals()
        {
            try
            {
                // Итерация по видимым контейнерам элементов.
                for (int i = 0; i < ShopList.Items.Count; i++)
                {
                    var container = ShopList.ItemContainerGenerator.ContainerFromIndex(i) as FrameworkElement;
                    if (container == null) continue;

                    // Ищем кнопку избранного по имени "FavButton".
                    var favBtn = FindDescendantByName(container, "FavButton") as Button;
                    if (favBtn == null) continue;

                    // Попытка извлечь ID.
                    int shopId = -1;
                    if (favBtn.Tag is int tid) shopId = tid;
                    else if (favBtn.Tag is string tstr && int.TryParse(tstr, out int parsed)) shopId = parsed;
                    else if (favBtn.DataContext is System.Data.DataRowView drv && drv.Row.Table.Columns.Contains("id"))
                        shopId = Convert.ToInt32(drv["id"]);

                    if (shopId <= 0) continue;

                    // Устанавливаем иконку сердечка в зависимости от наличия ID в _favoriteIds.
                    favBtn.Content = _favoriteIds.Contains(shopId) ? "💔" : "❤️";
                }
            }
            catch
            {
                // Игнорируем ошибки визуализации, чтобы не прерывать работу.
            }
        }

        // Анимация масштабирования кнопки при добавлении в избранное.
        private void AnimateFavoriteButton(Button btn)
        {
            var scale = new ScaleTransform(1.0, 1.0);
            btn.RenderTransformOrigin = new Point(0.5, 0.5); // Центр трансформации.
            btn.RenderTransform = scale;
            // Анимация масштабирования: 1.0 -> 1.2 -> 1.0 за 140мс.
            var anim = new DoubleAnimation(1.0, 1.2, TimeSpan.FromMilliseconds(140))
            {
                AutoReverse = true,
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseInOut }
            };

            scale.BeginAnimation(ScaleTransform.ScaleXProperty, anim);
            scale.BeginAnimation(ScaleTransform.ScaleYProperty, anim);
        }

        // --- Утилиты для визуального дерева ---

        // Общий метод для поиска родительского элемента по типу.
        private static T FindAncestor<T>(DependencyObject current) where T : DependencyObject
        {
            while (current != null)
            {
                if (current is T t) return t;
                current = VisualTreeHelper.GetParent(current);
            }
            return null;
        }

        // Общий метод для поиска дочернего элемента по имени.
        private static DependencyObject FindDescendantByName(DependencyObject parent, string name)
        {
            if (parent == null) return null;
            int childrenCount = VisualTreeHelper.GetChildrenCount(parent);
            for (int i = 0; i < childrenCount; i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                // Проверяем имя элемента.
                if (child is FrameworkElement fe && fe.Name == name)
                    return fe;

                // Рекурсивный вызов для дочерних элементов.
                var result = FindDescendantByName(child, name);
                if (result != null) return result;
            }
            return null;
        }
    }
}