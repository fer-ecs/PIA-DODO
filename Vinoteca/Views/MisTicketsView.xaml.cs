using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Vinoteca.Helpers;
using Vinoteca.Models;
using Vinoteca.Services;

namespace Vinoteca.Views
{
	public sealed partial class MisTicketsView : Page
	{
		private List<Venta> ticketsUsuario = new();

		public MisTicketsView()
		{
			InitializeComponent();
			InputRestrictionsHelper.AplicarTextoLibreSinEnter(txtBuscarTicket);
			cmbOrdenTickets.SelectedIndex = 0;
			CargarTickets();
		}

		private void CargarTickets()
		{
			if (!SessionService.PuedeComprar || SessionService.UsuarioActivo == null)
			{
				txtEstado.Text = "Solo empleados pueden consultar tickets emitidos";
				txtEstado.Visibility = Visibility.Visible;
				lvTickets.IsEnabled = false;
				return;
			}

			ticketsUsuario = DataService.ObtenerVentasPorUsuario(SessionService.UsuarioActivo.Id);
			AplicarFiltroTickets();
		}

		private void AplicarFiltroTickets()
		{
			string busqueda = txtBuscarTicket.Text?.Trim().ToLowerInvariant() ?? string.Empty;
			IEnumerable<Venta> consulta = ticketsUsuario;

			if (!string.IsNullOrWhiteSpace(busqueda))
			{
				consulta = consulta.Where(v =>
					(v.Id?.ToLowerInvariant().Contains(busqueda) ?? false) ||
					(v.MetodoPago?.ToLowerInvariant().Contains(busqueda) ?? false) ||
					v.Total.ToString("0.##", CultureInfo.InvariantCulture).Contains(busqueda) ||
					v.Productos.Any(item =>
						(item.Producto.Nombre?.ToLowerInvariant().Contains(busqueda) ?? false) ||
						(item.Producto.Marca?.ToLowerInvariant().Contains(busqueda) ?? false) ||
						(item.Producto.Categoria?.ToLowerInvariant().Contains(busqueda) ?? false)));
			}

			consulta = ObtenerOrdenTickets() switch
			{
				"Fecha antigua" => consulta.OrderBy(v => v.Fecha),
				"Total mayor" => consulta.OrderByDescending(v => v.Total),
				"Total menor" => consulta.OrderBy(v => v.Total),
				"ID" => consulta.OrderByDescending(v => v.Id),
				_ => consulta.OrderByDescending(v => v.Fecha)
			};

			var ticketsFiltrados = consulta.ToList();
			lvTickets.ItemsSource = ticketsFiltrados;

			txtResumenTickets.Text = ticketsUsuario.Count == 0
				? "Todavia no tienes tickets emitidos"
				: $"{ticketsFiltrados.Count} de {ticketsUsuario.Count} ticket(s)";
			txtConteoTickets.Text = $"{ticketsFiltrados.Count} tickets";
			txtSinTickets.Visibility = ticketsFiltrados.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
		}

		private string ObtenerOrdenTickets()
		{
			return (cmbOrdenTickets.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Fecha reciente";
		}

		private void FiltroTickets_Changed(object sender, object e)
		{
			AplicarFiltroTickets();
		}

		private void btnLimpiarTickets_Click(object sender, RoutedEventArgs e)
		{
			txtBuscarTicket.Text = string.Empty;
			cmbOrdenTickets.SelectedIndex = 0;
			AplicarFiltroTickets();
		}

		private async void btnDescargarPdf_Click(object sender, RoutedEventArgs e)
		{
			if (!SessionService.PuedeComprar)
			{
				txtEstado.Text = "Solo empleados pueden descargar tickets";
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
