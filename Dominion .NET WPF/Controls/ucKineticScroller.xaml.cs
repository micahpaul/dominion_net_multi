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
	/// Interaction logic for ucKineticScroller.xaml
	/// </summary>
	public partial class ucKineticScroller : UserControl
	{
		public ucKineticScroller()
		{
			InitializeComponent();
		}

		private void svScroller_ScrollChanged(object sender, ScrollChangedEventArgs e)
		{
			ScrollViewer sv = sender as ScrollViewer;
			bItemsHorizontal.Width = sv.ViewportWidth * sv.ViewportWidth / sv.ExtentWidth;
			bItemsVertical.Height = sv.ViewportHeight * sv.ViewportHeight / sv.ExtentHeight;

			bItemsHorizontal.Margin = new Thickness(sv.ViewportWidth * sv.HorizontalOffset / sv.ExtentWidth, 0, 0, 0);
			bItemsVertical.Margin = new Thickness(0, sv.ViewportHeight * sv.VerticalOffset / sv.ExtentHeight, 0, 0);
		}
	}
}
