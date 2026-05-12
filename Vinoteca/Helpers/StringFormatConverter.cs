using Microsoft.UI.Xaml.Data;
using System;

namespace Vinoteca.Helpers
{
	// esta seccion sirve para agrupar las validaciones y apoyos de entrada y dejar esa responsabilidad en un solo archivo - StringFormatConverter
	public class StringFormatConverter : IValueConverter
	{
		// esta seccion sirve para adaptar valores de las validaciones y apoyos de entrada para que XAML los pueda mostrar bien - Convert
		public object Convert(object value, Type targetType, object parameter, string language)
		{
			if (value == null || parameter == null)
				return value?.ToString() ?? string.Empty;

			try
			{
				var format = parameter.ToString();
				return string.IsNullOrWhiteSpace(format)
					? value.ToString() ?? string.Empty
					: string.Format(format, value);
			}
			catch
			{
				return value.ToString() ?? string.Empty;
			}
		}

		// esta seccion sirve para adaptar valores de las validaciones y apoyos de entrada para que XAML los pueda mostrar bien - ConvertBack
		public object ConvertBack(object value, Type targetType, object parameter, string language)
		{
			throw new NotImplementedException();
		}
	}
}
