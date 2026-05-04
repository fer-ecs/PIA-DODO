using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Vinoteca.Services;

namespace Vinoteca.Views
{
	public sealed partial class AdminMenuView : Page
	{
		public AdminMenuView()
		{
			this.InitializeComponent();

			// Cargar datos de la sesión activa
			if (SessionService.UsuarioActivo != null)
			{
				txtNombreAdminActivo.Text = SessionService.UsuarioActivo.Nombre;
				txtCorreoAdminActivo.Text = SessionService.UsuarioActivo.Correo;
			}

			// Cambié esto para que inicie por defecto en la Tienda y puedas probarla de inmediato
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
			if (this.Frame != null)
			{
				this.Frame.Navigate(typeof(LoginView));
			}
		}
	}
}