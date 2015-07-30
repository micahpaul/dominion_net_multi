using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
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
	/// SmallColorPicker user control
	/// This code is open source published with the Code Project Open License (CPOL).
	/// </summary>
	public partial class ucSmallColorPicker : UserControl
	{
		#region Dependency properties
		public Color SelectedColor
		{
			get { return (Color)GetValue(SelectedColorProperty); }
			set { SetValue(SelectedColorProperty, value); }
		}

		public static readonly DependencyProperty SelectedColorProperty =
			DependencyProperty.Register("SelectedColor", typeof(Color), typeof(ucSmallColorPicker),
			new FrameworkPropertyMetadata(OnSelectedColorChanged));

		public IEnumerable<Color> AvailableColors
		{
			get { return Picker.Items.OfType<Color>(); }
		}

		private bool ListContains(Color newColor)
		{
			foreach (Color c in Picker.Items.OfType<Color>())
				if (c == newColor)
					return true;
			return false;
		}

		private static void OnSelectedColorChanged(DependencyObject obj, DependencyPropertyChangedEventArgs args)
		{
			ucSmallColorPicker cp = obj as ucSmallColorPicker;
			Debug.Assert(cp != null);

			Color newColor = (Color)args.NewValue;
			Color oldColor = (Color)args.OldValue;

			if (newColor == oldColor)
				return;

			// When the SelectedColor changes, set the selected value of the combo box
			if (cp.Picker.SelectedValue == null || (Color)cp.Picker.SelectedValue != newColor)
			{
				// Add the color if not found
				if (!cp.ListContains(newColor))
				{
					cp.AddColor(newColor);
				}
			}

			// Also update the brush
			cp.SelectedBrush = new SolidColorBrush(newColor);

			cp.OnColorChanged(oldColor, newColor);

		}

		public Brush SelectedBrush
		{
			get { return (Brush)GetValue(SelectedBrushProperty); }
			set { SetValue(SelectedBrushProperty, value); }
		}

		// Using a DependencyProperty as the backing store for SelectedBrush.  This enables animation, styling, binding, etc...
		public static readonly DependencyProperty SelectedBrushProperty =
			DependencyProperty.Register("SelectedBrush", typeof(Brush), typeof(ucSmallColorPicker),
			new FrameworkPropertyMetadata(OnSelectedBrushChanged));

		private static void OnSelectedBrushChanged(DependencyObject obj, DependencyPropertyChangedEventArgs args)
		{
			ucSmallColorPicker cp = (ucSmallColorPicker)obj;
			SolidColorBrush newBrush = (SolidColorBrush)args.NewValue;

			if (cp.SelectedColor != newBrush.Color)
				cp.SelectedColor = newBrush.Color;
		}
		#endregion

		#region Events
		public static readonly RoutedEvent ColorChangedEvent =
			EventManager.RegisterRoutedEvent("ColorChanged", RoutingStrategy.Bubble,
				typeof(RoutedPropertyChangedEventHandler<Color>), typeof(ucSmallColorPicker));

		public event RoutedPropertyChangedEventHandler<Color> ColorChanged
		{
			add { AddHandler(ColorChangedEvent, value); }
			remove { RemoveHandler(ColorChangedEvent, value); }
		}

		protected virtual void OnColorChanged(Color oldValue, Color newValue)
		{
			RoutedPropertyChangedEventArgs<Color> args = new RoutedPropertyChangedEventArgs<Color>(oldValue, newValue);
			args.RoutedEvent = ucSmallColorPicker.ColorChangedEvent;
			RaiseEvent(args);
		}
		#endregion

		static Brush _CheckerBrush = CreateCheckerBrush();
		public static Brush CheckerBrush { get { return _CheckerBrush; } }

		public ucSmallColorPicker()
		{
			InitializeComponent();
		}

		public void Clear()
		{
			Picker.Items.Clear();
		}
		/// <summary>
		/// Add a color to the ColorPicker list
		/// </summary>
		/// <param name="c"></param>
		public void AddColor(Color c)
		{
			Picker.Items.Add(c);
		}

		public static Brush CreateCheckerBrush()
		{
			// from http://msdn.microsoft.com/en-us/library/aa970904.aspx

			DrawingBrush checkerBrush = new DrawingBrush();

			GeometryDrawing backgroundSquare =
				new GeometryDrawing(
					Brushes.White,
					null,
					new RectangleGeometry(new Rect(0, 0, 8, 8)));

			GeometryGroup aGeometryGroup = new GeometryGroup();
			aGeometryGroup.Children.Add(new RectangleGeometry(new Rect(0, 0, 4, 4)));
			aGeometryGroup.Children.Add(new RectangleGeometry(new Rect(4, 4, 4, 4)));

			GeometryDrawing checkers = new GeometryDrawing(Brushes.Black, null, aGeometryGroup);

			DrawingGroup checkersDrawingGroup = new DrawingGroup();
			checkersDrawingGroup.Children.Add(backgroundSquare);
			checkersDrawingGroup.Children.Add(checkers);

			checkerBrush.Drawing = checkersDrawingGroup;
			checkerBrush.Viewport = new Rect(0, 0, 0.5, 0.5);
			checkerBrush.TileMode = TileMode.Tile;

			return checkerBrush;
		}

	}

	[ValueConversion(typeof(Color), typeof(Brush))]
	public class ColorToBrushConverter : IValueConverter
	{
		#region IValueConverter Members

		public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
		{
			if (targetType != typeof(Brush)) return null;
			if (!(value is Color)) return null;
			SolidColorBrush scb = new SolidColorBrush((Color)value);
			return scb;
		}

		public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
		{
			throw new NotImplementedException();
		}

		#endregion
	}
}
