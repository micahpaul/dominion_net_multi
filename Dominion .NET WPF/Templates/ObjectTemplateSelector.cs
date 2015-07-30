using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;

namespace Dominion.NET_WPF
{
	public class ObjectTemplateSelector : DataTemplateSelector
	{
		public DataTemplate StringTemplate { get; set; }
		public DataTemplate BooleanTemplate { get; set; }
		public DataTemplate IntTemplate { get; set; }
		public DataTemplate ConstraintTemplate { get; set; }

		public override System.Windows.DataTemplate SelectTemplate(object item, System.Windows.DependencyObject container)
		{
			if (!(item is DominionBase.Cards.CardSetting))
				return StringTemplate;

			Type objectType = (item as DominionBase.Cards.CardSetting).Value.GetType();

			if (objectType == typeof(String))
				return StringTemplate;

			else if (objectType == typeof(Boolean))
				return BooleanTemplate;

			else if (objectType == typeof(int))
				return IntTemplate;

			else if (objectType == typeof(DominionBase.Cards.ConstraintCollection))
				return ConstraintTemplate;

			return StringTemplate;
		}
	}
}
