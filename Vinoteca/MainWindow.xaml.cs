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
		}
	}
}
