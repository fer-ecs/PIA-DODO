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

			// 1. Material Mica para estética técnica en Windows 11
			SystemBackdrop = new MicaBackdrop();

			// 2. Extender contenido a la barra de título
			ExtendsContentIntoTitleBar = true;
			SetTitleBar(AppTitleBar);

			// 3. Obtener AppWindow para modificar icono de la barra de tareas
			IntPtr hWnd = WindowNative.GetWindowHandle(this);
			WindowId wndId = Win32Interop.GetWindowIdFromWindow(hWnd);
			_appWindow = AppWindow.GetFromWindowId(wndId);

			// Carga el icono a nivel de proceso de Windows
			_appWindow.SetIcon("Assets/StoreLogo.png");

			DataService.InicializarArchivos();
			RootFrame.Navigate(typeof(LoginView));

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