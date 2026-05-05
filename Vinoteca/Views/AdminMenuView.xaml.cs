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

			if (!SessionService.EsAdminActivo)
			{
				Frame?.Navigate(typeof(LoginView));
				return;
			}

			if (SessionService.UsuarioActivo != null)
			{
				txtNombreAdminActivo.Text = SessionService.UsuarioActivo.Nombre;
				txtCorreoAdminActivo.Text = SessionService.UsuarioActivo.Correo;
			}

			AdminContentFrame.Navigate(typeof(TiendaView));
		}

		private void btnNavTienda_Click(object sender, RoutedEventArgs e)
		{
			AdminContentFrame.Navigate(typeof(TiendaView));
		}

		private void btnNavInventario_Click(object sender, RoutedEventArgs e)
		{
			AdminContentFrame.Navigate(typeof(InventarioView));
		}

		private void btnNavUsuarios_Click(object sender, RoutedEventArgs e)
		{
			AdminContentFrame.Navigate(typeof(UsuariosView));
		}

		private void btnNavVentas_Click(object sender, RoutedEventArgs e)
		{
			AdminContentFrame.Navigate(typeof(VentasAdminView));
		}

		private void btnNavReportes_Click(object sender, RoutedEventArgs e)
		{
			AdminContentFrame.Navigate(typeof(ReportesView));
		}

		private void btnCerrarSesionAdmin_Click(object sender, RoutedEventArgs e)
		{
			SessionService.CerrarSesion();
			Frame?.Navigate(typeof(LoginView));
		}
	}
}
