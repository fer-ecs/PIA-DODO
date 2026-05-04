using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Vinoteca.Services;

namespace Vinoteca.Views
{
	public sealed partial class UsuarioMenuView : Page
	{
		public UsuarioMenuView()
		{
			this.InitializeComponent();

			if (SessionService.UsuarioActivo != null)
			{
				txtNombreUsuarioActivo.Text = SessionService.UsuarioActivo.Nombre;
				txtCorreoUsuarioActivo.Text = SessionService.UsuarioActivo.Correo;
			}

			UsuarioContentFrame.Navigate(typeof(TiendaView));
		}

		private void btnNavTienda_Click(object sender, RoutedEventArgs e)
		{
			UsuarioContentFrame.Navigate(typeof(TiendaView));
		}

		private void btnNavCarrito_Click(object sender, RoutedEventArgs e)
		{
			UsuarioContentFrame.Navigate(typeof(CarritoView));
		}

		private void btnCerrarSesionUsuario_Click(object sender, RoutedEventArgs e)
		{
			SessionService.CerrarSesion();
			Frame.Navigate(typeof(LoginView));
		}
	}
}