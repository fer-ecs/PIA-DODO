using System;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Vinoteca.Services;

namespace Vinoteca.Views
{
	public sealed partial class AnalisisSupervisorView : Page
	{
		private string reporteActual = string.Empty;

		public AnalisisSupervisorView()
		{
			InitializeComponent();

			if (!SessionService.PuedeVerAnalisisSupervisor)
			{
				txtEstado.Text = "Solo supervision puede consultar este modulo";
				txtEstado.Visibility = Visibility.Visible;
				return;
			}

			CargarAnalisis();
		}

		private void CargarAnalisis()
		{
			var ventas = DataService.ObtenerVentas();
			var lineas = ventas
				.SelectMany(v => v.Productos.Select(p => new { Venta = v, Item = p }))
				.ToList();

			double ingresos = ventas.Sum(v => v.Total);
			int totalProductos = lineas.Sum(l => l.Item.Cantidad);
			double ticketPromedio = ventas.Count == 0 ? 0 : ingresos / ventas.Count;

			var productoTop = lineas
				.GroupBy(l => l.Item.Producto.Nombre)
				.Select(g => new { Nombre = g.Key, Cantidad = g.Sum(x => x.Item.Cantidad) })
				.OrderByDescending(x => x.Cantidad)
				.FirstOrDefault();

			var empleadoTop = ventas
				.GroupBy(v => string.IsNullOrWhiteSpace(v.NombreEmpleado) ? "Empleado sin nombre" : v.NombreEmpleado)
				.Select(g => new { Nombre = g.Key, Ventas = g.Count(), Total = g.Sum(v => v.Total) })
				.OrderByDescending(x => x.Ventas)
				.ThenByDescending(x => x.Total)
				.FirstOrDefault();

			var categorias = lineas
				.GroupBy(l => string.IsNullOrWhiteSpace(l.Item.Producto.Categoria) ? "Sin categoria" : l.Item.Producto.Categoria)
				.Select(g => $"{g.Key}: {g.Sum(x => x.Item.Cantidad)} unidad(es) | {g.Sum(x => x.Item.Subtotal):C}")
				.OrderBy(x => x)
				.ToList();

			txtTotalVentas.Text = ventas.Count.ToString();
			txtIngresos.Text = ingresos.ToString("C");
			txtProductosVendidos.Text = totalProductos.ToString();
			txtTicketPromedio.Text = ticketPromedio.ToString("C");
			txtProductoTop.Text = productoTop == null ? "Sin datos" : $"{productoTop.Nombre} ({productoTop.Cantidad} unidad(es))";
			txtEmpleadoTop.Text = empleadoTop == null ? "Sin datos" : $"{empleadoTop.Nombre} ({empleadoTop.Ventas} venta(s), {empleadoTop.Total:C})";
			lvCategorias.ItemsSource = categorias.Count == 0 ? new[] { "Sin ventas registradas" } : categorias;

			reporteActual = ConstruirReporte(
				ventas.Count,
				ingresos,
				totalProductos,
				ticketPromedio,
				txtProductoTop.Text,
				txtEmpleadoTop.Text,
				categorias);
		}

		private static string ConstruirReporte(
			int totalVentas,
			double ingresos,
			int totalProductos,
			double ticketPromedio,
			string productoTop,
			string empleadoTop,
			System.Collections.Generic.IEnumerable<string> categorias)
		{
			var sb = new StringBuilder();
			sb.AppendLine("Reporte estadistico - Vinoteca");
			sb.AppendLine($"Generado: {DateTime.Now:yyyy-MM-dd HH:mm}");
			sb.AppendLine();
			sb.AppendLine($"Ventas registradas: {totalVentas}");
			sb.AppendLine($"Ingresos totales: {ingresos:C}");
			sb.AppendLine($"Productos vendidos: {totalProductos}");
			sb.AppendLine($"Ticket promedio: {ticketPromedio:C}");
			sb.AppendLine($"Producto mas vendido: {productoTop}");
			sb.AppendLine($"Empleado con mas ventas: {empleadoTop}");
			sb.AppendLine();
			sb.AppendLine("Ventas por categoria:");

			foreach (var categoria in categorias)
			{
				sb.AppendLine($"- {categoria}");
			}

			return sb.ToString();
		}

		private void btnDescargarReporte_Click(object sender, RoutedEventArgs e)
		{
			if (!SessionService.PuedeVerAnalisisSupervisor)
			{
				txtEstado.Text = "No tienes permiso para descargar este reporte";
				txtEstado.Visibility = Visibility.Visible;
				return;
			}

			string downloads = Path.Combine(
				Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
				"Downloads");
			Directory.CreateDirectory(downloads);

			string ruta = Path.Combine(downloads, $"reporte-estadistico-vinoteca-{DateTime.Now:yyyyMMdd-HHmmss}.txt");
			File.WriteAllText(ruta, reporteActual, Encoding.UTF8);

			txtEstado.Text = $"Reporte exportado en: {ruta}";
			txtEstado.Foreground = (SolidColorBrush)Application.Current.Resources["WineSuccessBrush"];
			txtEstado.Visibility = Visibility.Visible;
		}
	}
}
