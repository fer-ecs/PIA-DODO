using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using System.Linq;
using Vinoteca.Models;
using Vinoteca.Services;

namespace Vinoteca.Views
{
	public sealed partial class ReportesView : Page
	{
		public ReportesView()
		{
			InitializeComponent();

			if (!SessionService.PuedeVerReportes)
			{
				txtEstado.Text = "Solo administracion puede ver reportes administrativos";
				txtEstado.Visibility = Visibility.Visible;
				lvHistorialVentas.IsEnabled = false;
				return;
			}

			CargarDatos();
		}

		private void CargarDatos()
		{
			var ventas = DataService.ObtenerVentas().OrderByDescending(v => v.Fecha).ToList();
			lvHistorialVentas.ItemsSource = ventas;

			double totalGlobal = ventas.Sum(v => v.Total);
			int totalVentas = ventas.Count;
			int totalProductos = ventas.Sum(v => v.Productos.Count);

			txtGananciasTotales.Text = $"Ganancias totales: {totalGlobal:C}";
			txtResumenVentas.Text = $"Ventas registradas: {totalVentas} | Lineas vendidas: {totalProductos}";
			txtSinVentas.Visibility = ventas.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
		}

		private async void btnExportarPdf_Click(object sender, RoutedEventArgs e)
		{
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
	}
}
