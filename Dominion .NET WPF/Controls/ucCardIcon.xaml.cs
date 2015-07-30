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

using DominionBase.Utilities;

namespace Dominion.NET_WPF.Controls
{
	/// <summary>
	/// Interaction logic for ucCardIcon.xaml
	/// </summary>
	public partial class ucCardIcon : UserControl, IDisposable
	{
		public static readonly DependencyProperty SizeProperty =
			DependencyProperty.Register("Size", typeof(CardSize), typeof(ucCardIcon), new PropertyMetadata(CardSize.SmallText));
		public CardSize Size
		{
			get { return (CardSize)this.GetValue(SizeProperty); }
			set
			{
				this.SetValue(SizeProperty, value);

				switch (value)
				{ 
					case CardSize.SmallText:
						dpName.Height = 16;
						lName.Margin = new Thickness(0, 0, 2, 0);
						lName.Padding = new Thickness(0);
						this.Padding = new Thickness(0);
						break;
					case CardSize.Text:
						dpName.Height = Double.NaN;
						lName.Margin = new Thickness(0, 0, 5, 0);
						lName.Padding = new Thickness(0, 2, 0, 2);
						this.Padding = new Thickness(5);
						break;
				}
			}
		}

		private DominionBase.ICard _Card = null;
		private int _Count = 0;

		private Settings _containee;
		private WeakucCardIcon _weakContainer = null;
		//private byte[] memoryLeak = null;

		public int Count
		{
			get { return _Count; }
			set
			{
				_Count = value;
				if (_Count < 0)
					_Count = 0;

				if (_Count > 1)
				{
					lCount.Visibility = System.Windows.Visibility.Visible;
					lCount.Content = String.Format("{0}x", _Count);
				}
				else
				{
					lCount.Visibility = System.Windows.Visibility.Collapsed;
				}
			}
		}

		public ucCardIcon()
		{
			InitializeComponent();

			if (wMain.Settings != null)
			{
				this.Settings = wMain.Settings;
				//wMain.Settings.SettingsChanged += new NET_WPF.Settings.SettingsChangedEventHandler(Settings_SettingsChanged);
				Settings_SettingsChanged(wMain.Settings, null);
			}

			//memoryLeak = new byte[5000000];
		}

		#region IDisposable variables, properties, & methods
		// Track whether Dispose has been called.
		private bool disposed = false;

		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		protected virtual void Dispose(bool disposing)
		{
			// Check to see if Dispose has already been called.
			if (!this.disposed)
			{
				// If disposing equals true, dispose all managed
				// and unmanaged resources.
				if (disposing)
				{
					// Dispose managed resources.
				}

				// Call the appropriate methods to clean up
				// unmanaged resources here.
				// If disposing is false,
				// only the following code is executed.
				_Card = null;
				//memoryLeak = null;
				if (wMain.Settings != null)
				{
					wMain.Settings.SettingsChanged -= new NET_WPF.Settings.SettingsChangedEventHandler(Settings_SettingsChanged);
				}

				// Note disposing has been done.
				disposed = true;
			}
		}

		~ucCardIcon()
		{
			Dispose(false);
		}
		#endregion

		public Settings Settings
		{
			get { return _containee; }
			set
			{
				if (_weakContainer == null)
					_weakContainer = new WeakucCardIcon(this);

				// unsubscribe old
				if (_containee != null)
					_containee.SettingsChanged -= new Settings.SettingsChangedEventHandler(_weakContainer.Settings_SettingsChanged);

				_containee = value;
				// subscribe new
				if (_containee != null)
					_containee.SettingsChanged += new Settings.SettingsChangedEventHandler(_weakContainer.Settings_SettingsChanged);
			}
		}

		protected virtual void Settings_SettingsChanged(object sender, SettingsChangedEventArgs e)
		{
			Settings settings = sender as Settings;

			if (settings != null)
			{
				if (settings.ToolTipShowDuration == ToolTipShowDuration.Off)
					ToolTipService.SetIsEnabled(this, false);
				else
				{
					ToolTipService.SetIsEnabled(this, true);
					ToolTipService.SetShowDuration(this, (int)settings.ToolTipShowDuration);
				}
			}
		}

		private class WeakucCardIcon : WeakReference
		{
			public WeakucCardIcon(ucCardIcon target) : base(target) { }

			public void Settings_SettingsChanged(object sender, SettingsChangedEventArgs e)
			{
				ucCardIcon b = (ucCardIcon)this.Target;
				if (b != null)
					b.Settings_SettingsChanged(sender, e);
				else
				{
					Settings c = sender as Settings;
					if (c != null)
					{
						c.SettingsChanged -= new Settings.SettingsChangedEventHandler(this.Settings_SettingsChanged);
					}
				}
			}

			~WeakucCardIcon()
			{
			}
		}

		public DominionBase.ICard Card
		{
			get { return _Card; }
			set
			{
				_Card = value;
				if (_Card != null)
				{
					this.Title = _Card.Name;

					DominionBase.Cards.Category category = this.Card.Category;
					if (this.Card is DominionBase.Cards.Card)
						category = ((DominionBase.Cards.Card)this.Card).PhysicalCategory;

					lName.Background = Caching.BrushRepository.GetBackgroundBrush(category);
					lName.Foreground = Caching.BrushRepository.GetForegroundBrush(category);

					if ((category & DominionBase.Cards.Category.Reaction) == DominionBase.Cards.Category.Reaction)
						tbName.Effect = Caching.DropShadowRepository.GetDSE(8, Colors.White, 1d);

					this.Count = 1;
				}
				else
				{
					this.Title = String.Empty;

					this.Count = 0;
				}

				ttcCard.ICard = this.Card;
			}
		}

		public String Title
		{
			get { return tbName.Text; }
			private set
			{
				tbName.Text = value;
				if (String.IsNullOrEmpty(value))
					lName.Visibility = System.Windows.Visibility.Hidden;
				else
					lName.Visibility = System.Windows.Visibility.Visible;
			}
		}

		private void UserControl_MouseDown(object sender, MouseButtonEventArgs e)
		{
			if (wMain.Settings == null || this.ToolTip == null || !(this.ToolTip is ToolTip) || (this.ToolTip as ToolTip).Content == null ||
				!((this.ToolTip as ToolTip).Content is ToolTipCard) || ((this.ToolTip as ToolTip).Content as ToolTipCard).ICard == null ||
				((this.ToolTip as ToolTip).Content as ToolTipCard).ICard.CardType == DominionBase.Cards.Universal.TypeClass.Dummy)
				return;

			if (wMain.Settings.ShowToolTipOnRightClick && e.ChangedButton == MouseButton.Right && e.ButtonState == MouseButtonState.Pressed)
			{
				this.CaptureMouse();
				(this.ToolTip as ToolTip).IsOpen = true;
				e.Handled = true;
			}
		}

		private void UserControl_MouseUp(object sender, MouseButtonEventArgs e)
		{
			if (wMain.Settings == null || this.ToolTip == null)
				return;

			if (wMain.Settings.ShowToolTipOnRightClick && e.ChangedButton == MouseButton.Right && e.ButtonState == MouseButtonState.Released)
			{
				this.ReleaseMouseCapture();
				(this.ToolTip as ToolTip).IsOpen = false;
				e.Handled = true;
			}
		}

		private void UserControl_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
		{
			if ((this.ToolTip as ToolTip).IsOpen)
				(this.ToolTip as ToolTip).IsOpen = false;
		}
	}

	public class CardIconUtilities
	{

		public static IEnumerable<UserControl> CreateCardIcons(IEnumerable<DominionBase.ICard> cards)
		{
			List<UserControl> list = new List<UserControl>();

			if (cards.Count() == 0)
				return list;

			ucCardIcon icon = null;
			int previousIndex = -1;
			for (int c = 0; c < cards.Count(); c++)
			{
				if (previousIndex < 0)
				{
					previousIndex = c;
					continue;
				}

				if (cards.ElementAt(previousIndex).CardType != cards.ElementAt(c).CardType)
				{
					icon = new ucCardIcon();
					icon.Card = cards.ElementAt(previousIndex);
					icon.Count = c - previousIndex;

					if (icon.Card is DominionBase.Piles.Supply && (icon.Card as DominionBase.Piles.Supply).Tokens.Any(t => t.Name == "BaneToken"))
						list.Add(new ucTokenIcon { Token = (icon.Card as DominionBase.Piles.Supply).Tokens.First(t => t.Name == "BaneToken"), Size = CardSize.SmallText });

					list.Add(icon);

					previousIndex = c;
				}
			}

			icon = new ucCardIcon();
			icon.Card = cards.Last();
			icon.Count = cards.Count() - previousIndex;

			if (icon.Card is DominionBase.Piles.Supply && (icon.Card as DominionBase.Piles.Supply).Tokens.Any(t => t.Name == "BaneToken"))
				list.Add(new ucTokenIcon { Token = (icon.Card as DominionBase.Piles.Supply).Tokens.First(t => t.Name == "BaneToken"), Size = CardSize.SmallText });

			list.Add(icon);

			return list;
		}

		public static ucCardIcon CreateCardIcon(DominionBase.ICard card)
		{
			ucCardIcon icon = new ucCardIcon();
			icon.Card = card;
			icon.Count = 1;

			return icon;
		}
	}

}
