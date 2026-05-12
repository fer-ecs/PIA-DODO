using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Vinoteca.Services;

namespace Vinoteca.Views
{
	// esta seccion sirve para agrupar la navegacion del menu y dejar esa responsabilidad en un solo archivo - AdminMenuView
	public sealed partial class AdminMenuView : Page
	{
		// esta seccion sirve para agrupar la navegacion del menu y dejar esa responsabilidad en un solo archivo - AdminMenuView
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
			ActualizarModuloActivo(typeof(InventarioView));
		}

		// esta seccion sirve para responder a la accion del usuario en la navegacion del menu y mover el flujo al siguiente paso - btnNavInventario_Click
		private async void btnNavInventario_Click(object sender, RoutedEventArgs e)
		{
			await NavegarModuloAsync(typeof(InventarioView));
		}

		// esta seccion sirve para responder a la accion del usuario en la navegacion del menu y mover el flujo al siguiente paso - btnNavUsuarios_Click
		private async void btnNavUsuarios_Click(object sender, RoutedEventArgs e)
		{
			await NavegarModuloAsync(typeof(UsuariosView));
		}

		// esta seccion sirve para responder a la accion del usuario en la navegacion del menu y mover el flujo al siguiente paso - btnNavReportes_Click
		private async void btnNavReportes_Click(object sender, RoutedEventArgs e)
		{
			await NavegarModuloAsync(typeof(ReportesView));
		}

		// esta seccion sirve para responder a la accion del usuario en la navegacion del menu y mover el flujo al siguiente paso - btnCerrarSesionAdmin_Click
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

		// esta seccion sirve para cambiar de pantalla dentro de la navegacion del menu y cuidar cambios pendientes - NavegarModuloAsync
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
			ActualizarModuloActivo(destino);
		}

		// esta seccion sirve para actualizar la navegacion del menu despues de un cambio y sincronizar la pantalla - ActualizarModuloActivo
		private void ActualizarModuloActivo(System.Type destino)
		{
			AplicarEstadoNav(btnNavInventario, destino == typeof(InventarioView));
			AplicarEstadoNav(btnNavUsuarios, destino == typeof(UsuariosView));
			AplicarEstadoNav(btnNavReportes, destino == typeof(ReportesView));
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
