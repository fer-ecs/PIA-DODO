using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Vinoteca.Models;
using Windows.Storage.Pickers;
using WinRT.Interop;

namespace Vinoteca.Services
{
	public static class TicketPdfService
	{
		public static async Task<string?> ExportarVentaPdfAsync(Venta venta, Window ventana)
		{
			var picker = new FileSavePicker
			{
				SuggestedStartLocation = PickerLocationId.DocumentsLibrary,
				DefaultFileExtension = ".pdf",
				SuggestedFileName = ObtenerNombreTicket(venta)
			};
			picker.FileTypeChoices.Add("Archivo PDF", new List<string> { ".pdf" });
			InitializeWithWindow.Initialize(picker, WindowNative.GetWindowHandle(ventana));

			var archivo = await picker.PickSaveFileAsync();
			if (archivo == null)
			{
				return null;
			}

			ExportarVentaPdf(venta, archivo.Path);
			return archivo.Path;
		}

		public static void ExportarVentaPdf(Venta venta, string ruta)
		{
			string contenido = ConstruirContenido(venta);
			byte[] contenidoBytes = Encoding.ASCII.GetBytes(contenido);

			var objetos = new List<string>
			{
				"<< /Type /Catalog /Pages 2 0 R >>",
				"<< /Type /Pages /Count 1 /Kids [3 0 R] >>",
				"<< /Type /Page /Parent 2 0 R /MediaBox [0 0 595 842] /Resources << /Font << /F1 4 0 R /F2 5 0 R >> >> /Contents 6 0 R >>",
				"<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>",
				"<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica-Bold >>",
				$"<< /Length {contenidoBytes.Length} >>\nstream\n{contenido}\nendstream"
			};

			EscribirPdf(ruta, objetos);
		}

		private static string ConstruirContenido(Venta venta)
		{
			var lineas = new List<string>
			{
				"BT",
				"/F2 22 Tf",
				"40 790 Td",
				$"({EscaparTextoPdf("Vinoteca")}) Tj",
				"ET",
				"BT",
				"/F2 11 Tf",
				"40 760 Td",
				$"({EscaparTextoPdf($"Ticket: {venta.Id}")}) Tj",
				"0 -18 Td",
				$"({EscaparTextoPdf($"Fecha: {venta.Fecha:g}")}) Tj",
				"0 -18 Td",
				$"({EscaparTextoPdf($"Cliente: {venta.NombreCliente}")}) Tj",
				"0 -18 Td",
				$"({EscaparTextoPdf($"Correo: {venta.CorreoCliente}")}) Tj",
				"0 -18 Td",
				$"({EscaparTextoPdf($"Rol: {venta.RolUsuario}")}) Tj",
				"0 -30 Td",
				$"({EscaparTextoPdf("Detalle de compra")}) Tj",
				"ET",
				"0.85 0.85 0.85 RG",
				"1 w",
				"40 646 m",
				"555 646 l",
				"S"
			};

			double y = 624;
			foreach (var item in venta.Productos)
			{
				string descripcion = $"{item.Producto.Nombre ?? "Producto"} - {item.Producto.Marca ?? string.Empty}".Trim().TrimEnd('-').Trim();
				string cantidad = $"Cant: {item.Cantidad}";
				string subtotal = item.Subtotal.ToString("C", CultureInfo.CurrentCulture);

				lineas.Add("BT");
				lineas.Add("/F1 10 Tf");
				lineas.Add($"40 {FormatearNumero(y)} Td");
				lineas.Add($"({EscaparTextoPdf(descripcion)}) Tj");
				lineas.Add("ET");

				lineas.Add("BT");
				lineas.Add("/F1 10 Tf");
				lineas.Add($"340 {FormatearNumero(y)} Td");
				lineas.Add($"({EscaparTextoPdf(cantidad)}) Tj");
				lineas.Add("ET");

				lineas.Add("BT");
				lineas.Add("/F2 10 Tf");
				lineas.Add($"460 {FormatearNumero(y)} Td");
				lineas.Add($"({EscaparTextoPdf(subtotal)}) Tj");
				lineas.Add("ET");

				y -= 18;
			}

			lineas.Add("0.85 0.85 0.85 RG");
			lineas.Add("1 w");
			lineas.Add($"40 {FormatearNumero(y - 8)} m");
			lineas.Add($"555 {FormatearNumero(y - 8)} l");
			lineas.Add("S");

			lineas.Add("BT");
			lineas.Add("/F2 18 Tf");
			lineas.Add($"350 {FormatearNumero(y - 34)} Td");
			lineas.Add($"({EscaparTextoPdf($"Total: {venta.Total.ToString("C", CultureInfo.CurrentCulture)}")}) Tj");
			lineas.Add("ET");

			return string.Join("\n", lineas);
		}

		private static void EscribirPdf(string ruta, List<string> objetos)
		{
			string? carpeta = Path.GetDirectoryName(ruta);
			if (!string.IsNullOrWhiteSpace(carpeta))
			{
				Directory.CreateDirectory(carpeta);
			}

			using var stream = new FileStream(ruta, FileMode.Create, FileAccess.Write, FileShare.None);
			using var writer = new StreamWriter(stream, new ASCIIEncoding());

			writer.Write("%PDF-1.4\n");
			writer.Flush();

			var offsets = new List<long> { 0 };
			for (int i = 0; i < objetos.Count; i++)
			{
				offsets.Add(stream.Position);
				writer.Write($"{i + 1} 0 obj\n{objetos[i]}\nendobj\n");
				writer.Flush();
			}

			long inicioXref = stream.Position;
			writer.Write($"xref\n0 {objetos.Count + 1}\n");
			writer.Write("0000000000 65535 f \n");
			for (int i = 1; i < offsets.Count; i++)
			{
				writer.Write($"{offsets[i].ToString("D10", CultureInfo.InvariantCulture)} 00000 n \n");
			}

			writer.Write($"trailer\n<< /Size {objetos.Count + 1} /Root 1 0 R >>\n");
			writer.Write($"startxref\n{inicioXref}\n%%EOF");
		}

		private static string EscaparTextoPdf(string? texto)
		{
			if (string.IsNullOrWhiteSpace(texto))
			{
				return string.Empty;
			}

			var limpio = new string(texto.Where(c => c >= 32 && c <= 126).ToArray());
			return limpio
				.Replace("\\", "\\\\")
				.Replace("(", "\\(")
				.Replace(")", "\\)");
		}

		private static string FormatearNumero(double valor)
		{
			return valor.ToString("0.##", CultureInfo.InvariantCulture);
		}

		private static string ObtenerNombreTicket(Venta venta)
		{
			string nombreSeguro = string.IsNullOrWhiteSpace(venta.Id) ? Guid.NewGuid().ToString("N") : venta.Id;
			return $"Ticket-{nombreSeguro}";
		}
	}
}
