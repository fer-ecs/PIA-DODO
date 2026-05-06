using Microsoft.UI.Xaml.Controls;
using Vinoteca.Services;

namespace Vinoteca.Views
{
	public sealed partial class UsuarioMenuView : Page
	{
		public UsuarioMenuView()
		{
			InitializeComponent();

			if (SessionService.UsuarioActivo == null || !SessionService.EsClienteActivo)
			{
				Frame?.Navigate(typeof(LoginView));
				return;
			}

			txtNombreUsuarioActivo.Text = SessionService.UsuarioActivo.Nombre;
			txtCorreoUsuarioActivo.Text = SessionService.UsuarioActivo.Correo;
			txtRolActivo.Text = $"{SessionService.RolActivo} activo";

			CarritoService.CarritoActualizado += ActualizarContadorCarrito;
			ActualizarContadorCarrito();

			UsuarioContentFrame.Navigate(typeof(TiendaView));
			Unloaded += UsuarioMenuView_Unloaded;
		}

		private void UsuarioMenuView_Unloaded(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
		{
			CarritoService.CarritoActualizado -= ActualizarContadorCarrito;
			Unloaded -= UsuarioMenuView_Unloaded;
		}

		private void ActualizarContadorCarrito()
		{
			int total = CarritoService.ObtenerCantidadTotalArticulos();
			txtContadorCarritoMenu.Text = total.ToString();
			bdgCarritoMenu.Visibility = total > 0
				? Microsoft.UI.Xaml.Visibility.Visible
				: Microsoft.UI.Xaml.Visibility.Collapsed;
		}

		private async void btnNavTienda_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
		{
			await NavegarModuloAsync(typeof(TiendaView));
		}

		private async void btnNavCarrito_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
		{
			await NavegarModuloAsync(typeof(CarritoView));
		}

		private async void btnNavMisTickets_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
		{
			await NavegarModuloAsync(typeof(MisTicketsView));
		}

		private async void btnCerrarSesionUsuario_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
		{
			bool confirmarSalida = await CambiosPendientesService.ConfirmarSalidaAsync(
				XamlRoot,
				this,
				"cerrar la sesion");
			if (!confirmarSalida)
			{
				return;
			}

			CarritoService.CarritoActualizado -= ActualizarContadorCarrito;
			SessionService.CerrarSesion();
			Frame.Navigate(typeof(LoginView));
		}

		private async System.Threading.Tasks.Task NavegarModuloAsync(System.Type destino)
		{
			if (UsuarioContentFrame.CurrentSourcePageType == destino)
			{
				return;
			}

			bool puedeNavegar = await CambiosPendientesService.ConfirmarAccionSiHayCambiosAsync(
				XamlRoot,
				UsuarioContentFrame,
				"cambiar de pestana",
				false);
			if (!puedeNavegar)
			{
				return;
			}

			UsuarioContentFrame.Navigate(destino);
		}
	}
}
