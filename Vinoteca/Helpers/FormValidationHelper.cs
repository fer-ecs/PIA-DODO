using System.Linq;
using System.Text.RegularExpressions;

namespace Vinoteca.Helpers
{
	public static class FormValidationHelper
	{
		private const string PatronCorreo = @"^[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Za-z]{2,}$";
		private const string PatronLetrasConEspacios = @"^[\p{L}]+(?: [\p{L}]+)*$";

		public static bool EsCorreoValido(string correo)
		{
			return !string.IsNullOrWhiteSpace(correo) &&
				correo == correo.Trim() &&
				correo.Length <= 80 &&
				!correo.Any(char.IsWhiteSpace) &&
				Regex.IsMatch(correo, PatronCorreo, RegexOptions.IgnoreCase);
		}

		public static bool EsTextoConLetrasYEspacios(string texto)
		{
			return !string.IsNullOrWhiteSpace(texto) &&
				texto == texto.Trim() &&
				!texto.Contains("  ") &&
				Regex.IsMatch(texto, PatronLetrasConEspacios);
		}
	}
}
