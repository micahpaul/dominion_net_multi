using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
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
	/// Interaction logic for ucToggleButton.xaml
	/// </summary>
	public partial class ucToggleButton : UserControl
	{
		public static readonly DependencyProperty IsCheckedProperty =
			DependencyProperty.Register("IsChecked", typeof(bool?), typeof(ucToggleButton),
			new FrameworkPropertyMetadata(false,
				FrameworkPropertyMetadataOptions.AffectsRender | FrameworkPropertyMetadataOptions.AffectsMeasure,
				new PropertyChangedCallback(ucToggleButton.IsCheckedChanged)));

		private static void IsCheckedChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			ucToggleButton ctrl = d as ucToggleButton;
			ctrl.IsChecked = (bool?)e.NewValue;
		}

		public static readonly DependencyProperty TextProperty =
			DependencyProperty.Register("Text", typeof(String), typeof(ucToggleButton),
			new FrameworkPropertyMetadata(String.Empty,
				FrameworkPropertyMetadataOptions.AffectsRender | FrameworkPropertyMetadataOptions.AffectsMeasure,
				new PropertyChangedCallback(ucToggleButton.OnTextChanged)));

		private static void OnTextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			ucToggleButton ctrl = d as ucToggleButton;
			ctrl.Text = (String)e.NewValue;
		}

		public static readonly DependencyProperty PreTextProperty =
			DependencyProperty.Register("PreText", typeof(String), typeof(ucToggleButton),
			new FrameworkPropertyMetadata(String.Empty,
				FrameworkPropertyMetadataOptions.AffectsRender | FrameworkPropertyMetadataOptions.AffectsMeasure,
				new PropertyChangedCallback(ucToggleButton.OnPreTextChanged)));

		private static void OnPreTextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			ucToggleButton ctrl = d as ucToggleButton;
			ctrl.PreText = (String)e.NewValue;
		}

		public static readonly RoutedEvent CheckedEvent = EventManager.RegisterRoutedEvent(
			"Checked",
			RoutingStrategy.Bubble,
			typeof(RoutedEventHandler),
			typeof(ucToggleButton));

		public event RoutedEventHandler Checked
		{
			add { AddHandler(CheckedEvent, value); }
			remove { RemoveHandler(CheckedEvent, value); }
		}

		public ucToggleButton()
		{
			InitializeComponent();
		}

		public ICommand Command {
			get { return bButton.Command; }
			set { bButton.Command = value; }
		}

		public bool? IsChecked
		{
			get {
				return (bool?)GetValue(IsCheckedProperty);
				//return bName.IsChecked; 
			}
			set {
				SetValue(IsCheckedProperty, value);
				bButton.IsChecked = value;
			}
		}

		public String Text
		{
			get { return (String)GetValue(TextProperty); }
			set
			{
				SetValue(TextProperty, value);

				if (value.Contains('_'))
				{
					tbDisplay.Inlines.Clear();
					TextBlock tbPreTemp = (TextBlock)Utilities.RenderText(value.Substring(0, value.IndexOf('_')), NET_WPF.RenderSize.Small, false)[0];
					while (tbPreTemp.Inlines.Count > 0)
						tbDisplay.Inlines.Add(tbPreTemp.Inlines.ElementAt(0));

					tbDisplay.Inlines.Add(new Run(value.Substring(value.IndexOf('_') + 1, 1)) { FontWeight = FontWeights.Bold, Foreground = Brushes.Crimson, TextDecorations = TextDecorations.Underline });

					TextBlock tbPostTemp = (TextBlock)Utilities.RenderText(value.Substring(value.IndexOf('_') + 2), NET_WPF.RenderSize.Small, false)[0];
					while (tbPostTemp.Inlines.Count > 0)
						tbDisplay.Inlines.Add(tbPostTemp.Inlines.ElementAt(0));
				}
				else
				{
					tbDisplay.Text = value;
				}
			}
		}

		public String PreText
		{
			get { return (String)GetValue(PreTextProperty); }
			set
			{
				SetValue(PreTextProperty, value);
				tbPre.Text = value;
			}
		}

		private void b_Click(object sender, RoutedEventArgs e)
		{
			//this.IsChecked = !this.IsChecked;
			RaiseEvent(new RoutedEventArgs(CheckedEvent));
		}

		private void UserControl_IsEnabledChanged(object sender, DependencyPropertyChangedEventArgs e)
		{
			if ((Boolean)e.NewValue)
			{
				bButton.Background = (VisualBrush)FindResource("ActiveBrush");
			}
			else
			{
				bButton.Background = (VisualBrush)FindResource("DiagonalHashBrush");
			}
		}
	}
}
