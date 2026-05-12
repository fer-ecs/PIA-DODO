using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Vinoteca.Services;

namespace Vinoteca.Views
{
	// esta seccion sirve para agrupar la navegacion del menu y dejar esa responsabilidad en un solo archivo - UsuarioMenuView
	public sealed partial class UsuarioMenuView : Page
	{
		// esta seccion sirve para agrupar la navegacion del menu y dejar esa responsabilidad en un solo archivo - UsuarioMenuView
		public UsuarioMenuView()
		{
			InitializeComponent();

			if (SessionService.UsuarioActivo == null || !SessionService.EsEmpleadoActivo)
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
			ActualizarModuloActivo(typeof(TiendaView));
			Unloaded += UsuarioMenuView_Unloaded;
		}

		// esta seccion sirve para responder a la accion del usuario en la navegacion del menu y mover el flujo al siguiente paso - UsuarioMenuView_Unloaded
		private void UsuarioMenuView_Unloaded(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
		{
			CarritoService.CarritoActualizado -= ActualizarContadorCarrito;
			Unloaded -= UsuarioMenuView_Unloaded;
		}

		// esta seccion sirve para actualizar la navegacion del menu despues de un cambio y sincronizar la pantalla - ActualizarContadorCarrito
		private void ActualizarContadorCarrito()
		{
			int total = CarritoService.ObtenerCantidadTotalArticulos();
			txtContadorCarritoMenu.Text = total.ToString();
			bdgCarritoMenu.Visibility = total > 0
				? Microsoft.UI.Xaml.Visibility.Visible
				: Microsoft.UI.Xaml.Visibility.Collapsed;
		}

		// esta seccion sirve para responder a la accion del usuario en la navegacion del menu y mover el flujo al siguiente paso - btnNavTienda_Click
		private async void btnNavTienda_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
		{
			await NavegarModuloAsync(typeof(TiendaView));
		}

		// esta seccion sirve para responder a la accion del usuario en la navegacion del menu y mover el flujo al siguiente paso - btnNavCarrito_Click
		private async void btnNavCarrito_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
		{
			await NavegarModuloAsync(typeof(CarritoView));
		}

		// esta seccion sirve para responder a la accion del usuario en la navegacion del menu y mover el flujo al siguiente paso - btnNavMisTickets_Click
		private async void btnNavMisTickets_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
		{
			await NavegarModuloAsync(typeof(MisTicketsView));
		}

		// esta seccion sirve para responder a la accion del usuario en la navegacion del menu y mover el flujo al siguiente paso - btnCerrarSesionUsuario_Click
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

		// esta seccion sirve para cambiar de pantalla dentro de la navegacion del menu y cuidar cambios pendientes - NavegarModuloAsync
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
			ActualizarModuloActivo(destino);
		}

		// esta seccion sirve para actualizar la navegacion del menu despues de un cambio y sincronizar la pantalla - ActualizarModuloActivo
		private void ActualizarModuloActivo(System.Type destino)
		{
			AplicarEstadoNav(btnNavTienda, destino == typeof(TiendaView));
			AplicarEstadoNav(btnNavCarrito, destino == typeof(CarritoView));
			AplicarEstadoNav(btnNavMisTickets, destino == typeof(MisTicketsView));
		}

		// esta seccion sirve para ordenar y ajustar datos de la navegacion del menu para trabajar con valores limpios - AplicarEstadoNav
		private static void AplicarEstadoNav(Button boton, bool activo)
		{
			if (activo)
			{
				boton.Background = (Brush)Application.Current.Resources["WineSidebarActiveBrush"];
				boton.BorderBrush = (Brush)Application.Current.Resources["WineHeroSoftBrush"];
				boton.BorderThickness = new Thickness(1);
				return;
			}

			boton.ClearValue(Button.BackgroundProperty);
			boton.ClearValue(Button.BorderBrushProperty);
			boton.ClearValue(Button.BorderThicknessProperty);
		}
	}
}
