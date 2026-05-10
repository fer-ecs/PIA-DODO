using System;
using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Vinoteca.Services;
using Vinoteca.Views;
using WinRT.Interop;

namespace Vinoteca
{
	public sealed partial class MainWindow : Window
	{
		private readonly AppWindow _appWindow;
		private bool _isDialogOpen;

		public MainWindow()
		{
			InitializeComponent();

			// Fondo nativo de Windows
			SystemBackdrop = new MicaBackdrop();

			// Barra superior integrada
			ExtendsContentIntoTitleBar = true;
			SetTitleBar(AppTitleBar);

			IntPtr hWnd = WindowNative.GetWindowHandle(this);
			WindowId wndId = Win32Interop.GetWindowIdFromWindow(hWnd);
			_appWindow = AppWindow.GetFromWindowId(wndId);

			_appWindow.SetIcon("Assets/StoreLogo.png");
			AbrirVentanaCompleta();

			DataService.InicializarArchivos();
			RootFrame.Navigate(typeof(LoginView));

			_appWindow.Closing += AppWindowClosing;
		}

		private void AbrirVentanaCompleta()
		{
			if (_appWindow.Presenter is OverlappedPresenter presenter)
			{
				presenter.Maximize();
			}
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
