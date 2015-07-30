using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

using DominionBase.Players;

namespace Dominion.NET_WPF.Controls
{
	/// <summary>
	/// Interaction logic for ucPlayerSettings.xaml
	/// </summary>
	public partial class ucPlayerSettings : UserControl
	{
		private PlayerType _PlayerType = PlayerType.Computer;
		private IEnumerable _AITypes = null;
		private PlayerSettings _PlayerSettings = null;

		public IEnumerable AITypes
		{
			get { return _AITypes; }
			set { cbAISelection.ItemsSource = _AITypes = value; }
		}
		public PlayerSettings PlayerSettings
		{
			get { return _PlayerSettings; }
			set
			{
				_PlayerSettings = value;
				this.tbName.Text = value.Name;
				if (scpTint.AvailableColors.Contains(value.UIColor))
					scpTint.SelectedColor = value.UIColor;
				foreach (Type type in cbAISelection.Items.OfType<Type>())
				{
					if (type == value.AIClassType)
					{
						cbAISelection.SelectedItem = type;
						break;
					}
				}
			}
		}
		public int PlayerNumber
		{
			get
			{
				if (!(this.lNameNumber.Content is int))
					this.lNameNumber.Content = 0;
				return (int)this.lNameNumber.Content;
			}
			set 
			{ 
				this.lNameNumber.Content = value;
				tbName.TabIndex = (value + 2) * 10;
				scpTint.TabIndex = (value + 2) * 10 + 1;
				cbAISelection.TabIndex = (value + 2) * 10 + 2;
			}
		}
		public PlayerType PlayerType
		{
			get { return _PlayerType; }
			set
			{
				_PlayerType = value;
				switch (value)
				{
					case PlayerType.Human:
						iType.Source = (BitmapImage)this.Resources["imHuman"];
						iType.ToolTip = "Human player";
						lAISelect.Visibility = cbAISelection.Visibility = System.Windows.Visibility.Hidden;
						break;

					case PlayerType.Computer:
						iType.Source = (BitmapImage)this.Resources["imComputer"];
						iType.ToolTip = "Computer player";
						lAISelect.Visibility = cbAISelection.Visibility = System.Windows.Visibility.Visible;
						break;
				}
			}
		}

		public ucPlayerSettings()
		{
			InitializeComponent();

			scpTint.Clear();
			for (int h = 0; h < 15; h++)
				scpTint.AddColor(HLSColor.HlsToRgb(24 * h, 0.85, 1, 1));
			scpTint.AddColor(Colors.Transparent);
		}

		private void cbAISelection_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			this.PlayerSettings.AIClassType = (Type)((sender as ComboBox).SelectedItem);
		}

		private void tbName_TextChanged(object sender, TextChangedEventArgs e)
		{
			this.PlayerSettings.Name = (sender as TextBox).Text;
		}

		private void tbName_GotFocus(object sender, RoutedEventArgs e)
		{
			(sender as TextBox).SelectAll();
		}

		private void scpTint_ColorChanged(object sender, RoutedPropertyChangedEventArgs<Color> e)
		{
			this.PlayerSettings.UIColor = (sender as ucSmallColorPicker).SelectedColor;
		}
	}

	public class AITypeConverterName : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			if (value is Type && ((Type)value == typeof(DominionBase.Players.AI.Basic) || ((Type)value).IsSubclassOf(typeof(DominionBase.Players.AI.Basic))))
			{
				return (String)((Type)value).GetProperty("AIName", BindingFlags.Static | BindingFlags.Public | BindingFlags.GetProperty | BindingFlags.FlattenHierarchy).GetValue(null, null);
			}
			else if (value is Type && ((Type)value == typeof(DominionBase.Players.AI.RandomAI) || ((Type)value).IsSubclassOf(typeof(DominionBase.Players.AI.RandomAI))))
			{
				return (String)((Type)value).GetProperty("AIName", BindingFlags.Static | BindingFlags.Public | BindingFlags.GetProperty | BindingFlags.FlattenHierarchy).GetValue(null, null);
			}
			return value.ToString();
		}
		public object ConvertBack(object value, Type targetType, object parameter,
			CultureInfo culture)
		{
			return null;
		}
	}

	public class AITypeConverterDescription : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			if (value is Type && ((Type)value == typeof(DominionBase.Players.AI.Basic) || ((Type)value).IsSubclassOf(typeof(DominionBase.Players.AI.Basic))))
			{
				return (String)((Type)value).GetProperty("AIDescription", BindingFlags.Static | BindingFlags.Public | BindingFlags.GetProperty | BindingFlags.FlattenHierarchy).GetValue(null, null);
			}
			else if (value is Type && ((Type)value == typeof(DominionBase.Players.AI.RandomAI) || ((Type)value).IsSubclassOf(typeof(DominionBase.Players.AI.RandomAI))))
			{
				return (String)((Type)value).GetProperty("AIDescription", BindingFlags.Static | BindingFlags.Public | BindingFlags.GetProperty | BindingFlags.FlattenHierarchy).GetValue(null, null);
			}
			return value.ToString();
		}
		public object ConvertBack(object value, Type targetType, object parameter,
			CultureInfo culture)
		{
			return null;
		}
	}
}
