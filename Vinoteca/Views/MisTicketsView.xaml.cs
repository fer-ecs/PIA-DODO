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
			if (SessionService.UsuarioActivo == null)
			{
				txtEstado.Text = "No hay una sesion activa para consultar tickets";
				txtEstado.Visibility = Visibility.Visible;
				return;
			}

			var tickets = DataService.ObtenerVentasPorUsuario(SessionService.UsuarioActivo.Id);
			lvTickets.ItemsSource = tickets;

			txtResumenTickets.Text = tickets.Count == 0
				? "Todavia no tienes compras registradas"
				: $"Tienes {tickets.Count} ticket(s) registrados";
			txtSinTickets.Visibility = tickets.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
		}

		private void btnDescargarPdf_Click(object sender, RoutedEventArgs e)
		{
			if (sender is not Button button || button.Tag is not Venta venta)
			{
				return;
			}

			string ruta = TicketPdfService.ExportarVentaPdf(venta);
			txtEstado.Text = $"Ticket exportado en: {ruta}";
			txtEstado.Foreground = new SolidColorBrush(Microsoft.UI.Colors.Green);
			txtEstado.Visibility = Visibility.Visible;
		}
	}
}
