using System;
using System.Data;
using System.Diagnostics;
using System.Windows;

namespace ProEquipmentRental
{
    public partial class ShopDetails : Window
    {
        private string link;
        public ShopDetails(DataRowView row)
        {
            InitializeComponent();

            ShopName.Text = row["name"].ToString();
            ShopDescription.Text = row["description"].ToString();
            ShopAddress.Text = "📍 " + row["address"].ToString();
            ShopRating.Text = "⭐ Рейтинг: " + row["rating"].ToString();
            link = row["link"].ToString();
        }

        private void OpenLink_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Process.Start(new ProcessStartInfo(link) { UseShellExecute = true });
            }
            catch
            {
                MessageBox.Show("Не удалось открыть ссылку.", "Ошибка");
            }
        }
    }
}