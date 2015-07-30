using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace Dominion.NET_WPF
{
	/// <summary>
	/// Interaction logic for wMessageBox.xaml
	/// </summary>
	public partial class wMessageBox : Window
	{
		[DllImport("gdi32.dll", SetLastError = true)]
		private static extern bool DeleteObject(IntPtr hObject);

		private MessageBoxResult _Result = MessageBoxResult.None;

		public wMessageBox()
		{
			InitializeComponent();
		}

		public String Text
		{
			get { return tbBody.Text; }
			private set { tbBody.Text = value; }
		}

		private MessageBoxButton Button
		{
			set
			{
				switch (value)
				{
					case MessageBoxButton.OK:
						this.bNo.Visibility = System.Windows.Visibility.Collapsed;
						this.bCancel.Visibility = System.Windows.Visibility.Collapsed;
						break;
					case MessageBoxButton.OKCancel:
						this.bNo.Visibility = System.Windows.Visibility.Collapsed;
						break;
					case MessageBoxButton.YesNo:
						this.bOk.Text = "_Yes";
						this.bCancel.Visibility = System.Windows.Visibility.Collapsed;
						break;
					case MessageBoxButton.YesNoCancel:
						this.bOk.Text = "_Yes";
						break;
				}
			}
		}

		private System.Drawing.Bitmap ImageIcon
		{
			set
			{
				if (value == null)
				{
					iIcon.Visibility = System.Windows.Visibility.Collapsed;
					iIcon.Source = null;
					return;
				}
				IntPtr ip = value.GetHbitmap();
				BitmapSource bs = null;
				try
				{
					bs = System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap(ip,
					   IntPtr.Zero, Int32Rect.Empty,
					   System.Windows.Media.Imaging.BitmapSizeOptions.FromEmptyOptions());
				}
				finally
				{
					DeleteObject(ip);
				}

				iIcon.Visibility = System.Windows.Visibility.Visible;
				iIcon.Source = bs;
			}
		}

		public MessageBoxResult Result
		{
			get { return _Result; }
			private set { _Result = value; }
		}

		public static MessageBoxResult Show(String body)
		{
			return _Show(null, body, String.Empty, MessageBoxButton.OK, MessageBoxImage.None, MessageBoxResult.None, MessageBoxOptions.None);
		}

		public static MessageBoxResult Show(String body, String caption)
		{
			return _Show(null, body, caption, MessageBoxButton.OK, MessageBoxImage.None, MessageBoxResult.None, MessageBoxOptions.None);
		}

		public static MessageBoxResult Show(String body, String caption, MessageBoxButton button)
		{
			return _Show(null, body, caption, button, MessageBoxImage.None, MessageBoxResult.None, MessageBoxOptions.None);
		}

		public static MessageBoxResult Show(String body, String caption, MessageBoxButton button, MessageBoxImage icon)
		{
			return _Show(null, body, caption, button, icon, MessageBoxResult.None, MessageBoxOptions.None);
		}

		public static MessageBoxResult Show(String body, String caption, MessageBoxButton button, MessageBoxImage icon, MessageBoxResult defaultResult)
		{
			return _Show(null, body, caption, button, icon, defaultResult, MessageBoxOptions.None);
		}

		public static MessageBoxResult Show(Window owner, String body)
		{
			return _Show(owner, body, String.Empty, MessageBoxButton.OK, MessageBoxImage.None, MessageBoxResult.None, MessageBoxOptions.None);
		}

		public static MessageBoxResult Show(Window owner, String body, String caption)
		{
			return _Show(owner, body, caption, MessageBoxButton.OK, MessageBoxImage.None, MessageBoxResult.None, MessageBoxOptions.None);
		}

		public static MessageBoxResult Show(Window owner, String body, String caption, MessageBoxButton button)
		{
			return _Show(owner, body, caption, button, MessageBoxImage.None, MessageBoxResult.None, MessageBoxOptions.None);
		}

		public static MessageBoxResult Show(Window owner, String body, String caption, MessageBoxButton button, MessageBoxImage icon)
		{
			return _Show(owner, body, caption, button, icon, MessageBoxResult.None, MessageBoxOptions.None);
		}

		public static MessageBoxResult Show(Window owner, String body, String caption, MessageBoxButton button, MessageBoxImage icon, MessageBoxResult defaultResult)
		{
			return _Show(owner, body, caption, button, icon, defaultResult, MessageBoxOptions.None);
		}

		private static MessageBoxResult _Show(Window owner, String body, String caption, MessageBoxButton button, MessageBoxImage icon, MessageBoxResult defaultResult, MessageBoxOptions options)
		{
			wMessageBox mb = new wMessageBox();

			mb.Text = body;
			mb.Owner = owner != null ? owner : Application.Current.Windows.OfType<Window>().SingleOrDefault(x => x.IsActive);
			mb.Title = caption;
			mb.Button = button;

			switch (icon)
			{
				case MessageBoxImage.Error: // & .Hand & .Stop
					mb.ImageIcon = System.Drawing.SystemIcons.Error.ToBitmap();
					break;
				case MessageBoxImage.Exclamation: // & .Warning
					mb.ImageIcon = System.Drawing.SystemIcons.Exclamation.ToBitmap();
					break;
				case MessageBoxImage.Information: // & .Asterisk
					mb.ImageIcon = System.Drawing.SystemIcons.Information.ToBitmap();
					break;
				case MessageBoxImage.None:
					mb.ImageIcon = null;
					break;
				case MessageBoxImage.Question:
					mb.ImageIcon = System.Drawing.SystemIcons.Question.ToBitmap();
					break;
			}

			if (mb.ShowDialog() == true)
				return mb.Result;
			return defaultResult;
		}

		private void bOk_Click(object sender, RoutedEventArgs e)
		{
			if (bOk.Text == "_Yes")
				this.Result = MessageBoxResult.Yes;
			else
				this.Result = MessageBoxResult.OK;
			this.DialogResult = true;
			this.Close();
		}

		private void bNo_Click(object sender, RoutedEventArgs e)
		{
			this.Result = MessageBoxResult.No;
			this.DialogResult = true;
			this.Close();
		}

		private void bCancel_Click(object sender, RoutedEventArgs e)
		{
			this.Result = MessageBoxResult.Cancel;
			this.DialogResult = true;
			this.Close();
		}
	}
}
