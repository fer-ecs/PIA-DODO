using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Vinoteca.Services;
using Vinoteca.Views;

namespace Vinoteca
{
	public sealed partial class MainWindow : Window
	{
		private readonly AppWindow _appWindow;
		private bool _isDialogOpen;

		public MainWindow()
		{
			InitializeComponent();

			DataService.InicializarArchivos();
			RootFrame.Navigate(typeof(LoginView));

			_appWindow = this.AppWindow;
			_appWindow.Closing += AppWindowClosing;
		}

		private async void AppWindowClosing(AppWindow sender, dynamic args)
		{
			if (_isDialogOpen)
			{
				args.Cancel = true;
				return;
			}

			args.Cancel = true;
			_isDialogOpen = true;

			bool confirmarCierre = await CambiosPendientesService.ConfirmarSalidaAsync(
				this.Content.XamlRoot,
				this.Content as DependencyObject,
				"cerrar el sistema");

			_isDialogOpen = false;

			if (!confirmarCierre)
			{
				return;
			}

			_appWindow.Closing -= AppWindowClosing;
			this.Close();
		}
	}
}
