using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Vinoteca.Services;

namespace Vinoteca.Views
{
	public sealed partial class AdminMenuView : Page
	{
		public AdminMenuView()
		{
			InitializeComponent();

			if (!SessionService.EsAdministradorActivo)
			{
				Frame?.Navigate(typeof(LoginView));
				return;
			}

			if (SessionService.UsuarioActivo != null)
			{
				txtNombreAdminActivo.Text = SessionService.UsuarioActivo.Nombre;
				txtCorreoAdminActivo.Text = SessionService.UsuarioActivo.Correo;
			}

			AdminContentFrame.Navigate(typeof(InventarioView));
		}

		private async void btnNavInventario_Click(object sender, RoutedEventArgs e)
		{
			await NavegarModuloAsync(typeof(InventarioView));
		}

		private async void btnNavUsuarios_Click(object sender, RoutedEventArgs e)
		{
			await NavegarModuloAsync(typeof(UsuariosView));
		}

		private async void btnNavReportes_Click(object sender, RoutedEventArgs e)
		{
			await NavegarModuloAsync(typeof(ReportesView));
		}

		private async void btnCerrarSesionAdmin_Click(object sender, RoutedEventArgs e)
		{
			bool confirmarSalida = await CambiosPendientesService.ConfirmarSalidaAsync(
				XamlRoot,
				this,
				"cerrar la sesion");
			if (!confirmarSalida)
			{
				return;
			}

			SessionService.CerrarSesion();
			Frame?.Navigate(typeof(LoginView));
		}

		private async System.Threading.Tasks.Task NavegarModuloAsync(System.Type destino)
		{
			if (AdminContentFrame.CurrentSourcePageType == destino)
			{
				return;
			}

			bool puedeNavegar = await CambiosPendientesService.ConfirmarAccionSiHayCambiosAsync(
				XamlRoot,
				AdminContentFrame,
				"cambiar de pestana",
				false);
			if (!puedeNavegar)
			{
				return;
			}

			AdminContentFrame.Navigate(destino);
		}
	}
}
