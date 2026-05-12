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
	// esta seccion sirve para agrupar la parte del sistema y dejar esa responsabilidad en un solo archivo - MainWindow
	public sealed partial class MainWindow : Window
	{
		private readonly AppWindow _appWindow;
		private bool _isDialogOpen;

		// esta seccion sirve para agrupar la parte del sistema y dejar esa responsabilidad en un solo archivo - MainWindow
		public MainWindow()
		{
			InitializeComponent();

			// Mantiene la ventana alineada con el estilo del sistema
			SystemBackdrop = new MicaBackdrop();

			// Usa la franja superior como barra real de la app
			ExtendsContentIntoTitleBar = true;
			SetTitleBar(AppTitleBar);

			IntPtr hWnd = WindowNative.GetWindowHandle(this);
			WindowId wndId = Win32Interop.GetWindowIdFromWindow(hWnd);
			_appWindow = AppWindow.GetFromWindowId(wndId);

			_appWindow.SetIcon("Assets/Vinoteca.ico");
			AbrirVentanaCompleta();

			DataService.InicializarArchivos();
			RootFrame.Navigate(typeof(LoginView));

			_appWindow.Closing += AppWindowClosing;
		}

		// esta seccion sirve para manejar la parte del sistema y concentrar aqui esta parte del flujo - AbrirVentanaCompleta
		private void AbrirVentanaCompleta()
		{
			if (_appWindow.Presenter is OverlappedPresenter presenter)
			{
				presenter.Maximize();
			}
		}

		// esta seccion sirve para responder a la accion del usuario en la parte del sistema y mover el flujo al siguiente paso - AppWindowClosing
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
