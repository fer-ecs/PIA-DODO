using Microsoft.UI.Xaml;
using Vinoteca.Views;
using Vinoteca.Services;

namespace Vinoteca
{
	public sealed partial class MainWindow : Window
	{
		public MainWindow()
		{
			this.InitializeComponent();

			// Nos aseguramos de que el JSON exista al abrir la app
			DataService.InicializarArchivos();

			// Navegamos a la pantalla de login al iniciar
			RootFrame.Navigate(typeof(LoginView));
		}
	}
}