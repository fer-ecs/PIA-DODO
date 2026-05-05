using Microsoft.UI.Xaml;
using Vinoteca.Services;
using Vinoteca.Views;

namespace Vinoteca
{
	public sealed partial class MainWindow : Window
	{
		public MainWindow()
		{
			InitializeComponent();

			DataService.InicializarArchivos();
			RootFrame.Navigate(typeof(LoginView));

			_appWindow = this.AppWindow;
			_appWindow.Closing += AppWindowClosing;
		}
		
		private async void AppWindowClosing(AppWindow sender, AppWindowClosingEventArgs args)
		{
			if (_isDialogOpen) 
            {
                args.Cancel = true;
                return;
            }

			args.Cancel = true;
			_isDialogOpen = true;

			var dialog = new ContentDialog
			{
				Title = "Salir",
				Content = "¿Deseas cerrar la aplicación?",
				PrimaryButtonText = "Sí",
				CloseButtonText = "Cancelar",
				XamlRoot = this.Content.XamlRoot
			};

			var result = await dialog.ShowAsync();
			_isDialogOpen = false;

			if (result == ContentDialogResult.Primary)
			{
				_appWindow.Closing -= AppWindowClosing;
				this.Close();
			}
		}

	}
}
