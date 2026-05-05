using Microsoft.UI.Xaml.Controls;
using Vinoteca.Services;

namespace Vinoteca.Views
{
	public sealed partial class UsuarioMenuView : Page
	{
		public UsuarioMenuView()
		{
			InitializeComponent();

			if (SessionService.UsuarioActivo == null || SessionService.EsAdminActivo)
			{
				Frame?.Navigate(typeof(LoginView));
				return;
			}

			txtNombreUsuarioActivo.Text = SessionService.UsuarioActivo.Nombre;
			txtCorreoUsuarioActivo.Text = SessionService.UsuarioActivo.Correo;

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

		private void btnNavTienda_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
		{
			UsuarioContentFrame.Navigate(typeof(TiendaView));
		}

		private void btnNavCarrito_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
		{
			UsuarioContentFrame.Navigate(typeof(CarritoView));
		}

		private void btnCerrarSesionUsuario_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
		{
			CarritoService.CarritoActualizado -= ActualizarContadorCarrito;
			SessionService.CerrarSesion();
			Frame.Navigate(typeof(LoginView));
		}
	}
}
