using System.Collections.Generic;
using System.Linq;
using Vinoteca.Models;

namespace Vinoteca.Services
{
	// esta seccion sirve para agrupar la consulta de tickets y dejar esa responsabilidad en un solo archivo - TicketService
	public static class TicketService
	{
		// esta seccion sirve para leer informacion de la consulta de tickets y regresarla lista para usarse - ObtenerTicketsEmitidos
		public static List<Venta> ObtenerTicketsEmitidos()
		{
			return DataService.ObtenerVentas()
				.OrderByDescending(v => v.Fecha)
				.ToList();
		}

		// esta seccion sirve para leer informacion de la consulta de tickets y regresarla lista para usarse - ObtenerTicketsPorEmpleado
		public static List<Venta> ObtenerTicketsPorEmpleado(string empleadoId)
		{
			return DataService.ObtenerVentasPorUsuario(empleadoId)
				.OrderByDescending(v => v.Fecha)
				.ToList();
		}

		// esta seccion sirve para manejar la consulta de tickets y concentrar aqui esta parte del flujo - ContarTicketsEmitidos
		public static int ContarTicketsEmitidos(IEnumerable<Venta> ventas)
		{
			return ventas.Count(VentaTieneTicket);
		}

		// esta seccion sirve para manejar la consulta de tickets y concentrar aqui esta parte del flujo - VentaTieneTicket
		public static bool VentaTieneTicket(Venta venta)
		{
			return !string.IsNullOrWhiteSpace(venta.Id) && venta.Productos.Count > 0;
		}
	}
}
