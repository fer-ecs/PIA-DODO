using System.Linq;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Vinoteca.Models;
using Vinoteca.Services;

namespace Vinoteca.Views
{
	public sealed partial class MisTicketsView : Page
	{
		public MisTicketsView()
		{
			InitializeComponent();
			CargarTickets();
		}

		private void CargarTickets()
		{
			if (!SessionService.PuedeComprar || SessionService.UsuarioActivo == null)
			{
				txtEstado.Text = "Solo clientes pueden consultar tickets de compra";
				txtEstado.Visibility = Visibility.Visible;
				lvTickets.IsEnabled = false;
				return;
			}

			var tickets = DataService.ObtenerVentasPorUsuario(SessionService.UsuarioActivo.Id);
			lvTickets.ItemsSource = tickets;

			txtResumenTickets.Text = tickets.Count == 0
				? "Todavia no tienes compras registradas"
				: $"Tienes {tickets.Count} ticket(s) registrados";
			txtSinTickets.Visibility = tickets.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
		}

		private async void btnDescargarPdf_Click(object sender, RoutedEventArgs e)
		{
			if (!SessionService.PuedeComprar)
			{
				txtEstado.Text = "Solo clientes pueden descargar sus tickets";
				txtEstado.Visibility = Visibility.Visible;
				return;
			}

			if (sender is not Button button || button.Tag is not Venta venta)
			{
				return;
			}

			if (App.VentanaPrincipal == null)
			{
				txtEstado.Text = "No se pudo abrir el explorador de archivos";
				txtEstado.Foreground = new SolidColorBrush(Microsoft.UI.Colors.Red);
				txtEstado.Visibility = Visibility.Visible;
				return;
			}

			string? ruta = await TicketPdfService.ExportarVentaPdfAsync(venta, App.VentanaPrincipal);
			if (string.IsNullOrWhiteSpace(ruta))
			{
				txtEstado.Text = "Guardado cancelado";
				txtEstado.Foreground = new SolidColorBrush(Microsoft.UI.Colors.Red);
				txtEstado.Visibility = Visibility.Visible;
				return;
			}

			txtEstado.Text = $"Ticket guardado en: {ruta}";
			txtEstado.Foreground = new SolidColorBrush(Microsoft.UI.Colors.Green);
			txtEstado.Visibility = Visibility.Visible;
		}

		private async void btnPrevisualizar_Click(object sender, RoutedEventArgs e)
		{
			if (sender is not Button button || button.Tag is not Venta venta)
			{
				return;
			}

			await TicketPreviewService.MostrarAsync(venta, XamlRoot);
		}
	}
}
