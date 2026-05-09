using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Vinoteca.Services;

namespace Vinoteca.Views
{
	public sealed partial class SupervisorMenuView : Page
	{
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
		}

		private async void btnNavAnalisis_Click(object sender, RoutedEventArgs e)
		{
			await NavegarModuloAsync(typeof(AnalisisSupervisorView));
		}

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
		}
	}
}
