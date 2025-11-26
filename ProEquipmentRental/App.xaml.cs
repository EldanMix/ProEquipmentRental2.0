using ProEquipmentRental;
using System.Threading.Tasks; // <-- Добавьте это
using System.Windows;

namespace ProEquipmentRental
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        // Используем async void для OnStartup, что является
        // допустимым исключением для обработчиков событий.
        protected override async void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // 1. Создать и показать загрузочный экран
            var splashScreen = new SplashScreenWindow();
            splashScreen.Show();

            // 2. Здесь вы можете выполнять "тяжелую" работу по загрузке:
            //    например, загрузку настроек, проверку обновлений и т.д.
            //    Мы просто имитируем это 3-секундной задержкой.
            await Task.Delay(3000); // Ждем 3 секунды

            // 3. Создать и показать главное окно
            var mainWindow = new MainWindow();
            mainWindow.Show();

            // 4. Закрыть загрузочный экран
            splashScreen.Close();
        }
    }
}