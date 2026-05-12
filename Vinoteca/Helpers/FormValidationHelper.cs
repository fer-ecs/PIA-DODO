using System.Linq;
using System.Text.RegularExpressions;

namespace Vinoteca.Helpers
{
	// esta seccion sirve para agrupar las validaciones y apoyos de entrada y dejar esa responsabilidad en un solo archivo - FormValidationHelper
	public static class FormValidationHelper
	{
		// esta seccion sirve para manejar las validaciones y apoyos de entrada y concentrar aqui esta parte del flujo - @"^[A-Za-z0-9]
		private const string PatronCorreo = @"^[A-Za-z0-9](?:[A-Za-z0-9._-]{0,38}[A-Za-z0-9])?@[A-Za-z0-9]+(?:\.[A-Za-z0-9]+)+$";
		// esta seccion sirve para manejar las validaciones y apoyos de entrada y concentrar aqui esta parte del flujo - @"^[\p{L}]+
		private const string PatronLetrasConEspacios = @"^[\p{L}]+(?: [\p{L}]+)*$";

		// esta seccion sirve para revisar reglas de las validaciones y apoyos de entrada y evitar que pase un dato incorrecto - EsCorreoValido
		public static bool EsCorreoValido(string correo)
		{
			return !string.IsNullOrWhiteSpace(correo) &&
				correo == correo.Trim() &&
				correo.Length <= 80 &&
				!correo.Any(char.IsWhiteSpace) &&
				correo.Contains('@') &&
				!correo[..correo.IndexOf('@')].Contains("..") &&
				Regex.IsMatch(correo, PatronCorreo, RegexOptions.IgnoreCase);
		}

		// esta seccion sirve para revisar reglas de las validaciones y apoyos de entrada y evitar que pase un dato incorrecto - EsTextoConLetrasYEspacios
		public static bool EsTextoConLetrasYEspacios(string texto)
		{
			return !string.IsNullOrWhiteSpace(texto) &&
				texto == texto.Trim() &&
				!texto.Contains("  ") &&
				Regex.IsMatch(texto, PatronLetrasConEspacios);
		}
	}
}
