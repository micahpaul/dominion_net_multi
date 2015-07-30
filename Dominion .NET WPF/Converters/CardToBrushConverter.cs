using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Data;

namespace Dominion.NET_WPF.Converters
{
	public sealed class CardToBackgroundBrushConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
		{
			if (value is DominionBase.Cards.Card)
			{
				return Caching.BrushRepository.GetBackgroundBrush(((DominionBase.Cards.Card)value).Category);
			}
			return System.Windows.Media.Brushes.Transparent;
		}

		public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
		{
			throw new NotImplementedException();
		}
	}

	public sealed class CardToForegroundBrushConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
		{
			if (value is DominionBase.Cards.Card)
			{
				return Caching.BrushRepository.GetForegroundBrush(((DominionBase.Cards.Card)value).Category);
			}
			return System.Windows.Media.Brushes.Transparent;
		}

		public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
		{
			throw new NotImplementedException();
		}
	}
}
