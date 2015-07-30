using System;
using System.Collections.Generic;
using System.Linq;
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

namespace Dominion.NET_WPF.Controls
{
	/// <summary>
	/// Interaction logic for ucButton.xaml
	/// </summary>
	public partial class ucButton : UserControl
	{
		public static readonly DependencyProperty TextProperty =
			DependencyProperty.Register("Text", typeof(String), typeof(ucButton),
			new FrameworkPropertyMetadata(String.Empty,
				FrameworkPropertyMetadataOptions.AffectsRender | FrameworkPropertyMetadataOptions.AffectsMeasure,
				new PropertyChangedCallback(ucButton.OnTextChanged)));

		private static void OnTextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			ucButton ctrl = d as ucButton;
			ctrl.Text = (String)e.NewValue;
		}

		public static readonly DependencyProperty IsDefaultProperty =
			DependencyProperty.Register("IsDefault", typeof(Boolean), typeof(ucButton),
			new FrameworkPropertyMetadata(false,
				FrameworkPropertyMetadataOptions.None,
				new PropertyChangedCallback(ucButton.OnIsDefaultChanged)));

		private static void OnIsDefaultChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			ucButton ctrl = d as ucButton;
			ctrl.IsDefault = (Boolean)e.NewValue;
		}

		public static readonly DependencyProperty IsCancelProperty =
			DependencyProperty.Register("IsCancel", typeof(Boolean), typeof(ucButton),
			new FrameworkPropertyMetadata(false,
				FrameworkPropertyMetadataOptions.None,
				new PropertyChangedCallback(ucButton.OnIsCancelChanged)));

		private static void OnIsCancelChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			ucButton ctrl = d as ucButton;
			ctrl.IsCancel = (Boolean)e.NewValue;
		}

		public static readonly RoutedEvent ClickEvent = EventManager.RegisterRoutedEvent(
			"Click",
			RoutingStrategy.Bubble,
			typeof(RoutedEventHandler),
			typeof(ucButton));

		public event RoutedEventHandler Click
		{
			add { AddHandler(ClickEvent, value); }
			remove { RemoveHandler(ClickEvent, value); }
		}

		public ucButton()
		{
			InitializeComponent();
		}

		public Thickness TextPadding
		{
			get { return bButton.Padding; }
			set { bButton.Padding = value; }
		}

		public Boolean IsDefault
		{
			get { return bButton.IsDefault; }
			set { bButton.IsDefault = value; }
		}

		public Boolean IsCancel
		{
			get { return bButton.IsCancel; }
			set { bButton.IsCancel = value; }
		}

		public String Text
		{
			get { return (String)GetValue(TextProperty); }
			set
			{
				SetValue(TextProperty, value);

				if (value.Contains('_'))
				{
					tbText.Inlines.Clear();

					String newValue = String.Format("{0}<sk>{1}</sk>{2}",
						value.Substring(0, value.IndexOf('_')),
						value.Substring(value.IndexOf('_') + 1, 1),
						value.Substring(value.IndexOf('_') + 2));

					TextBlock tbTemp = (TextBlock)Utilities.RenderText(newValue, NET_WPF.RenderSize.Small, false)[0];
					while (tbTemp.Inlines.Count > 0)
						tbText.Inlines.Add(tbTemp.Inlines.ElementAt(0));

					atHotkey.Text = value.Substring(value.IndexOf('_'), 2);
				}
				else
				{
					tbText.Text = value;
					atHotkey.Text = String.Empty;
				}
			}
		}

		private void b_Click(object sender, RoutedEventArgs e)
		{
			(sender as Button).IsEnabled = false;

			RaiseEvent(new RoutedEventArgs(ClickEvent));

			(sender as Button).IsEnabled = true;
		}

		private void UserControl_IsEnabledChanged(object sender, DependencyPropertyChangedEventArgs e)
		{
			bButton.IsEnabled = (Boolean)e.NewValue;
			if ((Boolean)e.NewValue)
			{
				//bButton.Background = (VisualBrush)FindResource("ActiveBrush");
				atHotkey.Foreground = Brushes.Crimson;
			}
			else
			{
				//bButton.Background = (VisualBrush)FindResource("DisabledBackgroundBrush");
				atHotkey.Foreground = Brushes.Gray;
			}
		}
	}
}
