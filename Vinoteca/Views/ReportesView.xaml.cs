using Microsoft.UI.Xaml.Controls;
using System.Linq;
using Vinoteca.Services;

namespace Vinoteca.Views
{
	public sealed partial class ReportesView : Page
	{
		public ReportesView()
		{
			this.InitializeComponent();
			CargarDatos();
		}

		private void CargarDatos()
		{
			var ventas = DataService.ObtenerVentas().OrderByDescending(v => v.Fecha).ToList();
			lvHistorialVentas.ItemsSource = ventas;

			double totalGlobal = ventas.Sum(v => v.Total);
			txtGananciasTotales.Text = $"Ganancias Totales: {totalGlobal:C}";
		}
	}
}