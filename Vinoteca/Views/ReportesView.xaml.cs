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
	public sealed partial class ReportesView : Page
	{
		private readonly ObservableCollection<Venta> ventasMostradas = new();
		private List<Venta> todasLasVentas = new();
		private bool exportandoPdf;

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

		private void CargarDatos()
		{
			todasLasVentas = DataService.ObtenerVentas().ToList();
			AplicarFiltroReportes();
		}

		private void AplicarFiltroReportes()
		{
			string busqueda = txtBuscarReporte.Text?.Trim().ToLowerInvariant() ?? string.Empty;
			double? totalMin = ObtenerTotalFiltro(txtTotalMin.Text);
			double? totalMax = ObtenerTotalFiltro(txtTotalMax.Text);

			var ventas = todasLasVentas.Where(v =>
				(string.IsNullOrWhiteSpace(busqueda) ||
				(v.Id?.ToLowerInvariant().Contains(busqueda) ?? false) ||
				(v.NombreCliente?.ToLowerInvariant().Contains(busqueda) ?? false) ||
				(v.CorreoCliente?.ToLowerInvariant().Contains(busqueda) ?? false) ||
				(v.UsuarioId?.ToLowerInvariant().Contains(busqueda) ?? false) ||
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
				"Cliente A-Z" => ventas.OrderBy(v => v.NombreCliente),
				"ID" => ventas.OrderBy(v => v.Id),
				_ => ventas.OrderByDescending(v => v.Fecha)
			};

			ventasMostradas.Clear();
			foreach (var venta in ventas)
			{
				ventasMostradas.Add(venta);
			}

			double totalGlobal = ventasMostradas.Sum(v => v.Total);
			int totalVentas = ventasMostradas.Count;
			int totalProductos = ventasMostradas.Sum(v => v.Productos.Count);

			txtGananciasTotales.Text = $"Ganancias totales: {totalGlobal:C}";
			txtResumenVentas.Text = $"Ventas registradas: {totalVentas} | Lineas vendidas: {totalProductos}";
			txtResumenFiltrado.Text = $"{totalVentas} de {todasLasVentas.Count} ventas visibles";
			txtSinVentas.Visibility = totalVentas == 0 ? Visibility.Visible : Visibility.Collapsed;
		}

		private static double? ObtenerTotalFiltro(string? texto)
		{
			string normalizado = (texto ?? string.Empty).Trim().Replace(',', '.');
			return double.TryParse(normalizado, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out double valor) ? valor : null;
		}

		private string ObtenerOrdenReportes()
		{
			return (cmbOrdenReportes.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Fecha reciente";
		}

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

		private async void btnPrevisualizar_Click(object sender, RoutedEventArgs e)
		{
			if (sender is not Button button || button.Tag is not Venta venta)
			{
				return;
			}

			await TicketPreviewService.MostrarAsync(venta, XamlRoot);
		}

		private void FiltroReportes_Changed(object sender, object e)
		{
			AplicarFiltroReportes();
		}
	}
}
