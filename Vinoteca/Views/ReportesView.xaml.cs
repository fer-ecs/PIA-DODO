using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System.Linq;
using Vinoteca.Services;

namespace Vinoteca.Views
{
	public sealed partial class ReportesView : Page
	{
		public ReportesView()
		{
			InitializeComponent();

			if (!SessionService.EsAdminActivo)
			{
				txtEstado.Text = "Solo un administrador puede ver reportes";
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
	}
}
