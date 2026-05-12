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
	// esta seccion sirve para agrupar la parte del sistema y dejar esa responsabilidad en un solo archivo - MisTicketsView
	public sealed partial class MisTicketsView : Page
	{
		private List<Venta> ticketsUsuario = new();

		// esta seccion sirve para agrupar la parte del sistema y dejar esa responsabilidad en un solo archivo - MisTicketsView
		public MisTicketsView()
		{
			InitializeComponent();
			InputRestrictionsHelper.AplicarTextoLibreSinEnter(txtBuscarTicket);
			cmbOrdenTickets.SelectedIndex = 0;
			CargarTickets();
		}

		// esta seccion sirve para cargar informacion de la parte del sistema y preparar lo que se muestra en pantalla - CargarTickets
		private void CargarTickets()
		{
			if (!SessionService.PuedeComprar || SessionService.UsuarioActivo == null)
			{
				txtEstado.Text = "Solo empleados pueden consultar tickets emitidos";
				txtEstado.Visibility = Visibility.Visible;
				lvTickets.IsEnabled = false;
				return;
			}

			ticketsUsuario = TicketService.ObtenerTicketsPorEmpleado(SessionService.UsuarioActivo.Id);
			AplicarFiltroTickets();
		}

		// esta seccion sirve para ordenar y ajustar datos de la parte del sistema para trabajar con valores limpios - AplicarFiltroTickets
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
				: $"{ticketsFiltrados.Count} de {ticketsUsuario.Count} ticket(s), ligados a ventas cobradas";
			txtConteoTickets.Text = $"{ticketsFiltrados.Count} tickets";
			txtSinTickets.Visibility = ticketsFiltrados.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
		}

		// esta seccion sirve para leer informacion de la parte del sistema y regresarla lista para usarse - ObtenerOrdenTickets
		private string ObtenerOrdenTickets()
		{
			return (cmbOrdenTickets.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Fecha reciente";
		}

		// esta seccion sirve para responder a la accion del usuario en la parte del sistema y mover el flujo al siguiente paso - FiltroTickets_Changed
		private void FiltroTickets_Changed(object sender, object e)
		{
			AplicarFiltroTickets();
		}

		// esta seccion sirve para responder a la accion del usuario en la parte del sistema y mover el flujo al siguiente paso - btnLimpiarTickets_Click
		private void btnLimpiarTickets_Click(object sender, RoutedEventArgs e)
		{
			txtBuscarTicket.Text = string.Empty;
			cmbOrdenTickets.SelectedIndex = 0;
			AplicarFiltroTickets();
		}

		// esta seccion sirve para responder a la accion del usuario en la parte del sistema y mover el flujo al siguiente paso - btnDescargarPdf_Click
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

		// esta seccion sirve para responder a la accion del usuario en la parte del sistema y mover el flujo al siguiente paso - btnPrevisualizar_Click
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
