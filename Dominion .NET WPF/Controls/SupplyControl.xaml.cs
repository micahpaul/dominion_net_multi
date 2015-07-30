using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

using GrayscaleEffect;

namespace Dominion.NET_WPF
{
	/// <summary>
	/// Interaction logic for SupplyControl.xaml
	/// </summary>
	public partial class SupplyControl : UserControl, IDisposable
	{
		public static readonly RoutedEvent SupplyClickEvent = EventManager.RegisterRoutedEvent(
			"SupplyClick", 
			RoutingStrategy.Bubble, 
			typeof(RoutedEventHandler), 
			typeof(SupplyControl));

		public event RoutedEventHandler SupplyClick
		{
			add { AddHandler(SupplyClickEvent, value); }
			remove { RemoveHandler(SupplyClickEvent, value); }
		}

		private DominionBase.Piles.Supply _Supply = null;
		private SupplyVisibility _Clickability = SupplyVisibility.Plain;
		private Boolean _SupplyGone = false;

		public SupplyControl()
		{
			InitializeComponent();
			Caching.ImageRepository repo = Caching.ImageRepository.Acquire();
			imBuyOverlay.Source = repo.GetBitmapImage("gainable", String.Empty);
			imSelectOverlay.Source = repo.GetBitmapImage("selectable", String.Empty);
			imDisableOverlay.Source = repo.GetBitmapImage("dither_20", String.Empty);
			Caching.ImageRepository.Release();

			if (wMain.Settings != null)
			{
				wMain.Settings.SettingsChanged += new NET_WPF.Settings.SettingsChangedEventHandler(Settings_SettingsChanged);
				Settings_SettingsChanged(wMain.Settings, null);
			}
		}

		public void Dispose()
		{
			if (wMain.Settings != null)
			{
				wMain.Settings.SettingsChanged -= new NET_WPF.Settings.SettingsChangedEventHandler(Settings_SettingsChanged);
			}
		}

		void Settings_SettingsChanged(object sender, SettingsChangedEventArgs e)
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

		/// <summary>
		/// Stuff.
		/// </summary>
		[CategoryAttribute("Custom Settings"), DescriptionAttribute(@"Stuff.")]
		public DominionBase.Piles.Supply Supply
		{
			get
			{
				return _Supply;
			}
			set
			{
				if (_Supply != null)
				{
					_Supply.PileChanged -= new DominionBase.Piles.Pile.PileChangedEventHandler(_Supply_PileChanged);
					_Supply.TokensChanged -= new DominionBase.Piles.Supply.TokensChangedEventHandler(_Supply_TokensChanged);
				}

				_Supply = value;
				ttcCard.ICard = value;

				if (_Supply != null)
				{
					_Supply.PileChanged += new DominionBase.Piles.Pile.PileChangedEventHandler(_Supply_PileChanged);
					_Supply.TokensChanged += new DominionBase.Piles.Supply.TokensChangedEventHandler(_Supply_TokensChanged);
					_Supply_TokensChanged(value, new DominionBase.Piles.TokensChangedEventArgs(null));

					_Supply_PileChanged(_Supply, null);
				}
			}
		}

		void _Supply_PileChanged(object sender, DominionBase.Piles.PileChangedEventArgs e)
		{
			if (this.Dispatcher.CheckAccess())
			{
				DominionBase.Piles.Supply supply = sender as DominionBase.Piles.Supply;

				if ((wMain.Settings.DisplaySupplyPileNames && supply.Location == DominionBase.Cards.Location.Kingdom) ||
					(wMain.Settings.DisplayBasicSupplyPileNames && supply.Location == DominionBase.Cards.Location.General))
				{
					lName.Visibility = System.Windows.Visibility.Visible;
					tbName.Text = supply.Randomizer.Name;

					lName.Background = Caching.BrushRepository.GetBackgroundBrush(supply.Randomizer.Category);
					lName.Foreground = Caching.BrushRepository.GetForegroundBrush(supply.Randomizer.Category);

					if ((supply.Randomizer.Category & DominionBase.Cards.Category.Reaction) == DominionBase.Cards.Category.Reaction)
						tbName.Effect = Caching.DropShadowRepository.GetDSE(8, Colors.White, 1d);
				}
				else
				{
					lName.Visibility = System.Windows.Visibility.Collapsed;
					tbName.Text = String.Empty;
				}

				Caching.ImageRepository repo = Caching.ImageRepository.Acquire();
				if (supply.TopCard != null)
				{
					imCardIcon.Source = repo.GetBitmapImage(supply.TopCard.Name.Replace(" ", "").Replace("'", ""), "small");
					ttcCard.ICard = supply.TopCard;
				}
				else
				{
					imCardIcon.Source = repo.GetBitmapImage(supply.Name.Replace(" ", "").Replace("'", ""), "small");
					ttcCard.ICard = supply.Randomizer;
				}
				Caching.ImageRepository.Release(); 
				
				this.InvalidateVisual();
			}
			else
			{
				this.Dispatcher.BeginInvoke(new EventHandler<DominionBase.Piles.PileChangedEventArgs>(_Supply_PileChanged), System.Windows.Threading.DispatcherPriority.Normal, sender, e);
			}
		}

		void _Supply_TokensChanged(object sender, DominionBase.Piles.TokensChangedEventArgs e)
		{
			if (this.Dispatcher.CheckAccess())
			{
				spExtraStuff.Children.Clear();
				var tokenGroups = (sender as DominionBase.Piles.Supply).Tokens.GroupBy(t => t.GetType());
				foreach (var tokenGroup in tokenGroups)
				{
					if (tokenGroup.Count() > 2)
					{
						spExtraStuff.Children.Add(new Controls.ucTokenIcon { Token = tokenGroup.ElementAt(0) });
						spExtraStuff.Children.Add(new TextBlock() { Margin = new Thickness(3, 0, 3, 0), Text = String.Format("{0}x", tokenGroup.Count()) });
					}
					else
					{
						foreach (DominionBase.Token token in tokenGroup) 
							spExtraStuff.Children.Add(new Controls.ucTokenIcon { Token = token });
					}
				}
			}
			else
			{
				this.Dispatcher.BeginInvoke(new EventHandler<DominionBase.Piles.TokensChangedEventArgs>(_Supply_TokensChanged), System.Windows.Threading.DispatcherPriority.Normal, sender, e);
			}
		}

		/// <summary>
		/// Stuff.
		/// </summary>
		[CategoryAttribute("Custom Settings"), DescriptionAttribute(@"Stuff.")]
		public SupplyVisibility Clickability
		{
			get
			{
				return _Clickability;
			}
			set
			{
				_Clickability = value;

				switch (_Clickability)
				{
					case SupplyVisibility.Plain:
						if (_SupplyGone)
						{
							imCardGone.Visibility = Visibility.Visible;
						}
						else
						{
							this.bBuy.IsEnabled = false;
							imBuyOverlay.Visibility = Visibility.Hidden;
							imDisableOverlay.Visibility = imSelectOverlay.Visibility = imCardGone.Visibility = Visibility.Hidden;
							imCardIcon.Effect = null;
							this.bBuy.Cursor = null;
						}
						break;

					case SupplyVisibility.NotClickable:
						this.bBuy.IsEnabled = false;
						imBuyOverlay.Visibility = imSelectOverlay.Visibility = Visibility.Hidden;
						imDisableOverlay.Visibility = Visibility.Visible;

						if (_SupplyGone)
							imCardGone.Visibility = Visibility.Visible;
						else
						{
							imCardGone.Visibility = Visibility.Hidden;
							imCardIcon.Effect = null;
						}

						this.bBuy.Cursor = null;
						break;

					case SupplyVisibility.Gainable:
						if (_SupplyGone)
						{
							this.bBuy.Cursor = null;
							imCardGone.Visibility = Visibility.Visible;
						}
						else
						{
							this.bBuy.IsEnabled = true;
							imBuyOverlay.Visibility = Visibility.Visible;
							imDisableOverlay.Visibility = imSelectOverlay.Visibility = Visibility.Hidden;

							imCardIcon.Effect = Caching.DropShadowRepository.GetDSE(10, Color.FromRgb(247, 214, 98), 1d);

							this.bBuy.Cursor = Cursors.Hand;
						}
						break;

					case SupplyVisibility.Selectable:
						this.bBuy.IsEnabled = true;
						imSelectOverlay.Visibility = Visibility.Visible;
						imDisableOverlay.Visibility = imBuyOverlay.Visibility = imCardGone.Visibility = Visibility.Hidden;

						imCardIcon.Effect = Caching.DropShadowRepository.GetDSE(10, Color.FromRgb(247, 214, 98), 1d);

						this.bBuy.Cursor = Cursors.Hand;
						break;
				}

				this.InvalidateVisual();
			}
		}

		private void bBuy_Click(object sender, RoutedEventArgs e)
		{
			(sender as Button).IsEnabled = false;
			RaiseEvent(new RoutedEventArgs(SupplyClickEvent));
			(sender as Button).IsEnabled = true;
		}

		protected override void OnRender(DrawingContext drawingContext)
		{
			base.OnRender(drawingContext);
			if (_Supply != null)
			{
				if (_Supply.Count == 0)
				{
					if (_SupplyGone == false)
					{
						_SupplyGone = true;
						GrayscaleEffect.GrayscaleEffect gse = new GrayscaleEffect.GrayscaleEffect();
						imCardIcon.Effect = gse;

						Caching.ImageRepository repo = Caching.ImageRepository.Acquire();
						imCardGone.Source = repo.GetBitmapImage("gone", "small");
						Caching.ImageRepository.Release();

						imCardGone.Visibility = System.Windows.Visibility.Visible;
						tbName.TextDecorations = TextDecorations.Strikethrough;

						imBuyOverlay.Visibility = imDisableOverlay.Visibility = imSelectOverlay.Visibility = Visibility.Hidden;
					}
				}
				else
				{
					if (_SupplyGone == true)
					{
						_SupplyGone = false;
						this.Clickability = this.Clickability;
					}
					_SupplyGone = false;
					imCardIcon.Effect = null;
					imCardGone.Source = null;
					imCardGone.Visibility = System.Windows.Visibility.Hidden;
					tbName.TextDecorations = null;
				}

				DominionBase.Cards.Cost supplyCost = _Supply.CurrentCost;
				lCost.Content = String.Format("{0}¢{1}{2}{3}", supplyCost.Coin.Value, supplyCost.Potion.Value > 0 ? " ¤" : "", supplyCost.Special ? "*" : "", supplyCost.CanOverpay ? "+" : "");
				if (supplyCost < _Supply.BaseCost)
					lCost.Foreground = Brushes.LimeGreen;
				else if (supplyCost > _Supply.BaseCost)
					lCost.Foreground = Brushes.Red;
				else
					lCost.Foreground = Brushes.Black;
				lCount.Content = String.Format("({0})", _Supply.Count);
			}
		}

		private void UserControl_MouseDown(object sender, MouseButtonEventArgs e)
		{
			if (wMain.Settings != null && wMain.Settings.ShowToolTipOnRightClick && e.ChangedButton == MouseButton.Right && e.ButtonState == MouseButtonState.Pressed)
			{
				this.CaptureMouse();
				ttCard.IsOpen = true;
			}
		}

		private void UserControl_MouseUp(object sender, MouseButtonEventArgs e)
		{
			if (wMain.Settings != null && wMain.Settings.ShowToolTipOnRightClick && e.ChangedButton == MouseButton.Right && e.ButtonState == MouseButtonState.Released)
			{
				this.ReleaseMouseCapture();
				ttCard.IsOpen = false;
			}
		}

		private void UserControl_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
		{
			if (ttCard.IsOpen)
				ttCard.IsOpen = false;
		}

	}
}
