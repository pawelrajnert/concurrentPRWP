using System;
using System.Windows;
using TP.ConcurrentProgramming.Presentation.ViewModel;

namespace TP.ConcurrentProgramming.PresentationView
{
    public partial class StartWindow : Window
    {
        public StartWindow()
        {
            InitializeComponent();
        }

        private void OnOkButtonClick(object sender, RoutedEventArgs e)
        {
            if (int.TryParse(BallCountTextBox.Text, out int ballCount) && ballCount > 0)
            {
                // Przejście do MainWindow
                MainWindow mainWindow = new MainWindow(ballCount);
                mainWindow.Show();
                this.Close();
            }
            else
            {
                MessageBox.Show("Proszę podać poprawną liczbę kulek.", "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}