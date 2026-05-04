using Microsoft.UI.Xaml.Data;
using System;

namespace Vinoteca.Helpers
{
	public class StringFormatConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, string language)
		{
			if (value == null || parameter == null)
				return value?.ToString() ?? string.Empty;

			try
			{
				return string.Format(parameter.ToString(), value);
			}
			catch
			{
				return value.ToString();
			}
		}

		public object ConvertBack(object value, Type targetType, object parameter, string language)
		{
			throw new NotImplementedException();
		}
	}
}