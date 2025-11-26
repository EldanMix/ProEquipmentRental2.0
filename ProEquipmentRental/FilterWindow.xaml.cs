// Подключаем WPF для работы с окнами и UI.
using System.Windows;

namespace ProEquipmentRental
{
    // Класс окна фильтрации. 'partial' для связки с XAML-файлом.
    public partial class FilterWindow : Window
    {
        // Свойство для хранения выбранного кода сортировки (только для чтения извне).
        public string SortOption { get; private set; } = "";

        // Конструктор.
        public FilterWindow()
        {
            // Загружает и инициализирует элементы UI из XAML.
            InitializeComponent();
        }

        // Обработчик нажатия кнопки "Применить".
        private void Apply_Click(object sender, RoutedEventArgs e)
        {
            // Проверяем, какой RadioButton отмечен, и устанавливаем соответствующий код сортировки.
            if (SortByNameAZ.IsChecked == true)
                SortOption = "name_asc";         // Имя, A-Z
            else if (SortByNameZA.IsChecked == true)
                SortOption = "name_desc";        // Имя, Z-A
            else if (SortByRatingHigh.IsChecked == true)
                SortOption = "rating_desc";      // Рейтинг, High-Low
            else if (SortByRatingLow.IsChecked == true)
                SortOption = "rating_asc";       // Рейтинг, Low-High
            else
                SortOption = "";                 // Сброс

            // Указываем, что окно закрыто успешно (применить фильтр).
            DialogResult = true;
            Close();
        }

        // Обработчик нажатия кнопки "Отмена".
        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            // Указываем, что окно закрыто без применения фильтра.
            DialogResult = false;
            Close();
        }
    }
}