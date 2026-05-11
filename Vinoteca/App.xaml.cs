using Microsoft.UI.Xaml;
using System;
using System.Diagnostics;
using System.IO;
using WinRT;
using Vinoteca.Services;

namespace Vinoteca
{
	public partial class App : Application
	{
		private Window? _window;
		private static FormCacheService? _formCacheService;

		public static FormCacheService FormCacheService => _formCacheService ??= new FormCacheService();
		public static Window? VentanaPrincipal { get; private set; }

		public App()
		{
			InitializeComponent();
			UnhandledException += App_UnhandledException;
		}

		private void App_UnhandledException(object sender, Microsoft.UI.Xaml.UnhandledExceptionEventArgs e)
		{
			// Deja rastro del error para poder revisarlo despues
			string detalle = e.Exception?.ToString() ?? e.Message;
			Debug.WriteLine($"[UNHANDLED] {detalle}");
			RegistrarError("UNHANDLED", detalle);
		}

		public static void RegistrarError(string origen, string detalle)
		{
			try
			{
				string appFolder = Path.Combine(
					Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
					"Vinoteca");
				string logFolder = Path.Combine(appFolder, "logs");
				Directory.CreateDirectory(logFolder);
				string logPath = Path.Combine(logFolder, "crash.log");
				string linea = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [{origen}] {detalle}{Environment.NewLine}";
				File.AppendAllText(logPath, linea);
			}
			catch
			{
				// Si falla el log no conviene romper tambien el cierre
			}
		}

		protected override void OnLaunched(LaunchActivatedEventArgs args)
		{
			_window = new MainWindow();
			VentanaPrincipal = _window;
			_window.Activate();
		}
	}
}
