using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using Vinoteca.Models;
using Vinoteca.Helpers;
using Vinoteca.Services;

namespace Vinoteca.Views
{
	// esta seccion sirve para agrupar los reportes y dejar esa responsabilidad en un solo archivo - ReportesView
	public sealed partial class ReportesView : Page
	{
		private readonly ObservableCollection<Venta> ventasMostradas = new();
		private List<Venta> todasLasVentas = new();
		private bool exportandoPdf;

		// esta seccion sirve para agrupar los reportes y dejar esa responsabilidad en un solo archivo - ReportesView
		public ReportesView()
		{
			InitializeComponent();
			InputRestrictionsHelper.AplicarTextoLibreSinEnter(txtBuscarReporte);
			InputRestrictionsHelper.AplicarSoloDecimal(txtTotalMin, txtTotalMax);
			lvHistorialVentas.ItemsSource = ventasMostradas;
			cmbOrdenReportes.SelectedIndex = 0;

			if (!SessionService.PuedeVerReportes)
			{
				txtEstado.Text = "Solo administracion puede ver reportes administrativos";
				txtEstado.Visibility = Visibility.Visible;
				lvHistorialVentas.IsEnabled = false;
				txtBuscarReporte.IsEnabled = false;
				txtTotalMin.IsEnabled = false;
				txtTotalMax.IsEnabled = false;
				cmbOrdenReportes.IsEnabled = false;
				return;
			}

			CargarDatos();
		}

		// esta seccion sirve para cargar informacion de los reportes y preparar lo que se muestra en pantalla - CargarDatos
		private void CargarDatos()
		{
			todasLasVentas = TicketService.ObtenerTicketsEmitidos();
			AplicarFiltroReportes();
		}

		// esta seccion sirve para ordenar y ajustar datos de los reportes para trabajar con valores limpios - AplicarFiltroReportes
		private void AplicarFiltroReportes()
		{
			string busqueda = txtBuscarReporte.Text?.Trim().ToLowerInvariant() ?? string.Empty;
			double? totalMin = ObtenerTotalFiltro(txtTotalMin.Text);
			double? totalMax = ObtenerTotalFiltro(txtTotalMax.Text);

			var ventas = todasLasVentas.Where(v =>
				(string.IsNullOrWhiteSpace(busqueda) ||
				(v.Id?.ToLowerInvariant().Contains(busqueda) ?? false) ||
				(v.NombreEmpleado?.ToLowerInvariant().Contains(busqueda) ?? false) ||
				(v.CorreoEmpleado?.ToLowerInvariant().Contains(busqueda) ?? false) ||
				(v.NombreCliente?.ToLowerInvariant().Contains(busqueda) ?? false) ||
				(v.CorreoCliente?.ToLowerInvariant().Contains(busqueda) ?? false) ||
				(v.EmpleadoId?.ToLowerInvariant().Contains(busqueda) ?? false) ||
				(v.UsuarioId?.ToLowerInvariant().Contains(busqueda) ?? false) ||
				(v.MetodoPago?.ToLowerInvariant().Contains(busqueda) ?? false) ||
				v.Productos.Any(p =>
					(p.Producto.Nombre?.ToLowerInvariant().Contains(busqueda) ?? false) ||
					(p.Producto.Marca?.ToLowerInvariant().Contains(busqueda) ?? false))) &&
				(!totalMin.HasValue || v.Total >= totalMin.Value) &&
				(!totalMax.HasValue || v.Total <= totalMax.Value));

			ventas = ObtenerOrdenReportes() switch
			{
				"Fecha antigua" => ventas.OrderBy(v => v.Fecha),
				"Total mayor" => ventas.OrderByDescending(v => v.Total),
				"Total menor" => ventas.OrderBy(v => v.Total),
				"Empleado A-Z" => ventas.OrderBy(v => string.IsNullOrWhiteSpace(v.NombreEmpleado) ? v.NombreCliente : v.NombreEmpleado),
				"ID" => ventas.OrderBy(v => ObtenerIdNumerico(v.Id)),
				_ => ventas.OrderByDescending(v => v.Fecha)
			};

			ventasMostradas.Clear();
			foreach (var venta in ventas)
			{
				ventasMostradas.Add(venta);
			}

			double totalGlobal = ventasMostradas.Sum(v => v.Total);
			int totalVentas = ventasMostradas.Count;
			int totalTickets = TicketService.ContarTicketsEmitidos(ventasMostradas);
			int totalProductos = ventasMostradas.Sum(v => v.Productos.Count);

			txtGananciasTotales.Text = $"Ganancias totales: {totalGlobal:C}";
			txtResumenVentas.Text = $"Ventas registradas: {totalVentas} | Tickets emitidos: {totalTickets} | Lineas vendidas: {totalProductos}";
			txtResumenFiltrado.Text = $"{totalVentas} de {todasLasVentas.Count} ventas/tickets visibles";
			txtSinVentas.Visibility = totalVentas == 0 ? Visibility.Visible : Visibility.Collapsed;
		}

		// esta seccion sirve para leer informacion de los reportes y regresarla lista para usarse - ObtenerTotalFiltro
		private static double? ObtenerTotalFiltro(string? texto)
		{
			string normalizado = (texto ?? string.Empty).Trim().Replace(',', '.');
			return double.TryParse(normalizado, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out double valor) ? valor : null;
		}

		// esta seccion sirve para leer informacion de los reportes y regresarla lista para usarse - ObtenerIdNumerico
		private static int ObtenerIdNumerico(string? id)
		{
			return int.TryParse(id, out int valor) ? valor : int.MaxValue;
		}

		// esta seccion sirve para leer informacion de los reportes y regresarla lista para usarse - ObtenerOrdenReportes
		private string ObtenerOrdenReportes()
		{
			return (cmbOrdenReportes.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Fecha reciente";
		}

		// esta seccion sirve para responder a la accion del usuario en los reportes y mover el flujo al siguiente paso - btnExportarPdf_Click
		private async void btnExportarPdf_Click(object sender, RoutedEventArgs e)
		{
			if (exportandoPdf)
			{
				return;
			}

			if (sender is not Button button || button.Tag is not Venta venta)
			{
				return;
			}

			exportandoPdf = true;
			button.IsEnabled = false;

			try
			{
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
			finally
			{
				exportandoPdf = false;
				button.IsEnabled = true;
			}
		}

		// esta seccion sirve para responder a la accion del usuario en los reportes y mover el flujo al siguiente paso - btnPrevisualizar_Click
		private async void btnPrevisualizar_Click(object sender, RoutedEventArgs e)
		{
			if (sender is not Button button || button.Tag is not Venta venta)
			{
				return;
			}

			await TicketPreviewService.MostrarAsync(venta, XamlRoot);
		}

		// esta seccion sirve para responder a la accion del usuario en los reportes y mover el flujo al siguiente paso - FiltroReportes_Changed
		private void FiltroReportes_Changed(object sender, object e)
		{
			AplicarFiltroReportes();
		}
	}
}
