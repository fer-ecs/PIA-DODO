using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Vinoteca.Services;

namespace Vinoteca.Views
{
	// esta seccion sirve para agrupar la navegacion del menu y dejar esa responsabilidad en un solo archivo - SupervisorMenuView
	public sealed partial class SupervisorMenuView : Page
	{
		// esta seccion sirve para agrupar la navegacion del menu y dejar esa responsabilidad en un solo archivo - SupervisorMenuView
		public SupervisorMenuView()
		{
			InitializeComponent();

			if (!SessionService.EsSupervisorActivo)
			{
				Frame?.Navigate(typeof(LoginView));
				return;
			}

			if (SessionService.UsuarioActivo != null)
			{
				txtNombreSupervisorActivo.Text = SessionService.UsuarioActivo.Nombre;
				txtCorreoSupervisorActivo.Text = SessionService.UsuarioActivo.Correo;
			}

			SupervisorContentFrame.Navigate(typeof(AnalisisSupervisorView));
			ActualizarModuloActivo(typeof(AnalisisSupervisorView));
		}

		// esta seccion sirve para responder a la accion del usuario en la navegacion del menu y mover el flujo al siguiente paso - btnNavAnalisis_Click
		private async void btnNavAnalisis_Click(object sender, RoutedEventArgs e)
		{
			await NavegarModuloAsync(typeof(AnalisisSupervisorView));
		}

		// esta seccion sirve para responder a la accion del usuario en la navegacion del menu y mover el flujo al siguiente paso - btnCerrarSesionSupervisor_Click
		private async void btnCerrarSesionSupervisor_Click(object sender, RoutedEventArgs e)
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
			if (SupervisorContentFrame.CurrentSourcePageType == destino)
			{
				return;
			}

			bool puedeNavegar = await CambiosPendientesService.ConfirmarAccionSiHayCambiosAsync(
				XamlRoot,
				SupervisorContentFrame,
				"cambiar de pestana",
				false);
			if (!puedeNavegar)
			{
				return;
			}

			SupervisorContentFrame.Navigate(destino);
			ActualizarModuloActivo(destino);
		}

		// esta seccion sirve para actualizar la navegacion del menu despues de un cambio y sincronizar la pantalla - ActualizarModuloActivo
		private void ActualizarModuloActivo(System.Type destino)
		{
			AplicarEstadoNav(btnNavAnalisis, destino == typeof(AnalisisSupervisorView));
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
