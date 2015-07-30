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
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

using DominionBase.Utilities;

namespace Dominion.NET_WPF.Controls
{
	/// <summary>
	/// Interaction logic for CardStackControl.xaml
	/// </summary>
	public partial class CardStackControl : UserControl, IDisposable
	{
		public static readonly RoutedEvent CardStackControlClickEvent = EventManager.RegisterRoutedEvent(
			"CardStackControlClick",
			RoutingStrategy.Bubble,
			typeof(RoutedEventHandler),
			typeof(CardStackControl));

		public event RoutedEventHandler CardStackControlClick
		{
			add { AddHandler(CardStackControlClickEvent, value); }
			remove { RemoveHandler(CardStackControlClickEvent, value); }
		}

		private IEnumerable<DominionBase.ICard> _CardCollection = new List<DominionBase.ICard>();
		private DominionBase.TokenCollection _Tokens = new DominionBase.TokenCollection();
		//private DominionBase.Cards.CardCollection _CardCollection = new DominionBase.Cards.CardCollection();
		private int _GlowSize = 0;
		private DominionBase.Cards.Card _ClickedCard = null;
		private DominionBase.Players.PhaseEnum _PlayerPhase = DominionBase.Players.PhaseEnum.Action;
		private DominionBase.Players.PlayerMode _PlayerMode = DominionBase.Players.PlayerMode.Waiting;
		private CardSize _CardSize = CardSize.Medium;

		public Boolean IsCardsVisible { get; set; }
		public DominionBase.Cards.Card ClickedCard
		{
			get { return _ClickedCard; }
		}
		public DominionBase.Players.PhaseEnum Phase
		{
			set
			{
				_PlayerPhase = value;

				UpdateGlowEffectAll();
			}
		}
		public DominionBase.Players.PlayerMode PlayerMode
		{
			set
			{
				_PlayerMode = value;

				UpdateGlowEffectAll();
			}
		}
		public Boolean IsClickable
		{
			get
			{
				return bImages.IsEnabled;
			}
			set
			{
				if (value)
				{
					bImages.IsEnabled = bName.IsEnabled = true;
					bImages.Visibility = bName.Visibility = System.Windows.Visibility.Visible;
					Panel.SetZIndex(bImages, 1);
					Panel.SetZIndex(bName, 1);
				}
				else
				{
					bImages.IsEnabled = bName.IsEnabled = false;
					bImages.Visibility = bName.Visibility = System.Windows.Visibility.Collapsed;
					Panel.SetZIndex(bImages, -1);
					Panel.SetZIndex(bName, -1);
				}
			}
		}
		public CardSize CardSize 
		{
			get { return _CardSize; }
			set
			{
				_CardSize = value;
				switch (_CardSize)
				{
					case NET_WPF.CardSize.Text:
					case NET_WPF.CardSize.SmallText:
						gImages.Visibility = System.Windows.Visibility.Collapsed;
						_GlowSize = 0;
						break;
					case NET_WPF.CardSize.Small: 
						gImages.Visibility = System.Windows.Visibility.Visible;
						_GlowSize = 10; 
						break;
					case NET_WPF.CardSize.Medium:
						gImages.Visibility = System.Windows.Visibility.Visible;
						_GlowSize = 20;
						break;
				}

				dpMain.Margin = new Thickness(_GlowSize / 4, 0, _GlowSize / 4, 0);
				gImages.Margin = new Thickness(_GlowSize / 2);

				UpdateCardDisplay();
			}
		}

		public void CountVPs(IEnumerable<DominionBase.Cards.Card> cards)
		{
			if (!(_CardCollection.ElementAt(0) is DominionBase.Cards.Card))
				return;

			dpVPCount.Visibility = System.Windows.Visibility.Visible;
			if (_CardCollection.Count() > 0)
			{
				int count = ((DominionBase.Cards.Card)_CardCollection.ElementAt(0)).GetVictoryPoints(cards);
				lVPCount.Content = count;
				lVP.Content = StringUtility.Plural("VP", count, false);
			}
			else
				lVPCount.Content = String.Empty;
		}
		public void HideVPs()
		{
			lVPCount.Content = String.Empty;
			dpVPCount.Visibility = System.Windows.Visibility.Collapsed;
		}

		public CardStackControl()
		{
			InitializeComponent();

			this.IsClickable = false;
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

		public IEnumerable<DominionBase.ICard> CardCollection
		{
			get { return _CardCollection; }
			set
			{
				_CardCollection = value;

				UpdateCardDisplay();
			}
		}

		public DominionBase.TokenCollection Tokens
		{
			get { return _Tokens; }
			set
			{
				_Tokens = value;

				UpdateCardDisplay();
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

		public DominionBase.Piles.Visibility PileVisibility { get; set; }
		public Boolean ExactCount { get; set; }

		private void UpdateCardDisplay()
		{
			spExtraStuff.Visibility = System.Windows.Visibility.Visible;
			spExtraStuff.Children.Clear();
			this.Tokens.ForEach(t => spExtraStuff.Children.Add(new Controls.ucTokenIcon { Token = t, Size = CardSize.Text }));
			if (spExtraStuff.Children.Count == 0)
				spExtraStuff.Visibility = System.Windows.Visibility.Collapsed;

			Size stackOffset = new Size();
			switch (this.CardSize)
			{
				case NET_WPF.CardSize.Text:
					break;
				case NET_WPF.CardSize.SmallText:
					dpName.Height = 16;
					lCount.Margin = new Thickness(0);
					lName.Padding = new Thickness(0);
					lName.Margin = new Thickness(0, 0, 2, 0);
					this.Padding = new Thickness(0);
					break;
				case NET_WPF.CardSize.Small:
					stackOffset = new Size(12, 6);
					break;
				case NET_WPF.CardSize.Medium:
					stackOffset = new Size(24, 10);
					break;
			}
			lCount.Visibility = System.Windows.Visibility.Collapsed;
			if (_CardCollection.Count() > 4 || (gImages.Visibility == System.Windows.Visibility.Collapsed && _CardCollection.Count() > 1))
			{
				lCount.Visibility = System.Windows.Visibility.Visible;
				if (IsCardsVisible && PileVisibility == DominionBase.Piles.Visibility.All)
					lCount.Content = String.Format("{0}x", _CardCollection.Count());
				stackOffset.Width = 0.75 * stackOffset.Width;
			}
			//else if (this.CardSize == NET_WPF.CardSize.SmallText)
			//{
			//    lCount.Visibility = System.Windows.Visibility.Collapsed;
			//}
			if (_CardCollection.Count() > 6 && stackOffset.Height > 0)
			{
				float verticalScale = 56f / (_CardCollection.Count() - 1);
				stackOffset = new Size((int)(0.625 * stackOffset.Width * verticalScale / stackOffset.Height), (int)verticalScale);
			}

			if (this.PileVisibility != DominionBase.Piles.Visibility.All)
				stackOffset.Width = 0;

			for (int index = 0; index < _CardCollection.Count(); index++)
			{
				DominionBase.ICard card = _CardCollection.ElementAt(index);
				if (card == null)
					continue;

				if (IsCardsVisible && PileVisibility == DominionBase.Piles.Visibility.All || PileVisibility == DominionBase.Piles.Visibility.Top)
				{
					DominionBase.Cards.Category category = card.Category;
					if (card is DominionBase.Cards.Card)
						category = ((DominionBase.Cards.Card)card).PhysicalCategory;

					lName.Background = Caching.BrushRepository.GetBackgroundBrush(category);
					lName.Foreground = Caching.BrushRepository.GetForegroundBrush(category);

					if ((category & DominionBase.Cards.Category.Reaction) == DominionBase.Cards.Category.Reaction)
						tbName.Effect = Caching.DropShadowRepository.GetDSE(8, Colors.White, 1d);
				}

				if (gImages.Visibility == System.Windows.Visibility.Visible)
				{
					Image newImage = new Image();
					Caching.ImageRepository repo = Caching.ImageRepository.Acquire();
					switch (this.CardSize)
					{
						case NET_WPF.CardSize.Small:
							if (IsCardsVisible &&
								(PileVisibility == DominionBase.Piles.Visibility.All || PileVisibility == DominionBase.Piles.Visibility.Top) &&
								card.CardType != DominionBase.Cards.Universal.TypeClass.Dummy)
								newImage.Source = repo.GetBitmapImage(card.Name.Replace(" ", "").Replace("'", ""), "small");
							else
							{
								switch (card.CardBack)
								{
									case DominionBase.Cards.CardBack.Standard:
										newImage.Source = repo.GetBitmapImage("back", "small");
										break;
									case DominionBase.Cards.CardBack.Red:
										newImage.Source = repo.GetBitmapImage("back_red", "small");
										break;
								}
							}
							break;
						case NET_WPF.CardSize.Medium:
							if (IsCardsVisible &&
								(PileVisibility == DominionBase.Piles.Visibility.All || PileVisibility == DominionBase.Piles.Visibility.Top) &&
								card.CardType != DominionBase.Cards.Universal.TypeClass.Dummy)
								newImage.Source = repo.GetBitmapImage(card.Name.Replace(" ", "").Replace("'", ""), "medium");
							else
							{
								switch (card.CardBack)
								{
									case DominionBase.Cards.CardBack.Standard:
										newImage.Source = repo.GetBitmapImage("back", "medium");
										break;
									case DominionBase.Cards.CardBack.Red:
										newImage.Source = repo.GetBitmapImage("back_red", "medium");
										break;
								}
							}
							break;
					}
					Caching.ImageRepository.Release();

					if (newImage.Source != null)
					{
						newImage.Width = newImage.Source.Width;
						newImage.Height = newImage.Source.Height;
					}

					newImage.HorizontalAlignment = HorizontalAlignment.Left;
					newImage.VerticalAlignment = VerticalAlignment.Top;

					newImage.Margin = new Thickness(index * stackOffset.Width, index * stackOffset.Height, 0, 0);
					newImage.Tag = card;

					gImages.Children.Insert(gImages.Children.Count - 1, newImage);
				}
			}

			this.Title = String.Empty;
			if (_CardCollection.Count() > 0 && IsCardsVisible)
			{
				if (PileVisibility == DominionBase.Piles.Visibility.All || PileVisibility == DominionBase.Piles.Visibility.Top)
				{
					DominionBase.ICard card = _CardCollection.Last();
					if (card != null)
					{
						this.Title = card.Name;
						ttcCard.ICard = card;
					}
				}
			}

			UpdateGlowEffectAll();

			if (!IsCardsVisible || PileVisibility == DominionBase.Piles.Visibility.None)
			{
				int count = _CardCollection.Count();
				if (this.ExactCount || count <= 1)
					this.Title = StringUtility.Plural("Card", count);
			}
		}

		private void UpdateGlowEffectAll()
		{
			if (IsCardsVisible && PileVisibility == DominionBase.Piles.Visibility.All)
			{
				Boolean anyGlowAdded = false;
				foreach (Image image in gImages.Children.OfType<Image>())
				{
					DominionBase.Cards.Card card = image.Tag as DominionBase.Cards.Card;
					Boolean addGlow = (_PlayerPhase == DominionBase.Players.PhaseEnum.Action && _PlayerMode == DominionBase.Players.PlayerMode.Normal &&
						((card.Category & DominionBase.Cards.Category.Action) == DominionBase.Cards.Category.Action ||
						(card.Category & DominionBase.Cards.Category.Treasure) == DominionBase.Cards.Category.Treasure)) ||
						((_PlayerPhase == DominionBase.Players.PhaseEnum.ActionTreasure || _PlayerPhase == DominionBase.Players.PhaseEnum.BuyTreasure) &&
						_PlayerMode == DominionBase.Players.PlayerMode.Normal &&
						(card.Category & DominionBase.Cards.Category.Treasure) == DominionBase.Cards.Category.Treasure);

					UpdateGlowEffect(image, addGlow, _GlowSize, Colors.Black, Colors.MediumBlue);
					anyGlowAdded |= addGlow;
				}

				UpdateGlowEffect(lName, anyGlowAdded, _GlowSize, Colors.Black, Colors.MediumBlue);
			}
		}

		private void UpdateGlowEffect(UIElement element, Boolean addGlow, int glowSize, Color color1, Color color2)
		{
			if (addGlow)
			{
				DropShadowEffect dse = Caching.DropShadowRepository.GetDSE(glowSize, color1, 0.85, false);

				ColorAnimation ca = new ColorAnimation()
				{
					From = color1,
					To = color2,
					Duration = TimeSpan.FromSeconds(1),
					AutoReverse = true,
					RepeatBehavior = RepeatBehavior.Forever,
					AccelerationRatio = 0.4,
				};
				DoubleAnimation da = new DoubleAnimation()
				{
					From = glowSize * 2 / 3,
					To = glowSize * 1.5,
					Duration = TimeSpan.FromSeconds(1),
					AutoReverse = true,
					RepeatBehavior = RepeatBehavior.Forever,
					AccelerationRatio = 0.4,
				};
				dse.BeginAnimation(DropShadowEffect.ColorProperty, ca);
				dse.BeginAnimation(DropShadowEffect.BlurRadiusProperty, da);

				element.Effect = dse;

				this.IsClickable = true;
				bImages.Cursor = bName.Cursor = Cursors.Hand;
			}
			else
			{
				element.Effect = null;
				this.IsClickable = false;
				bImages.Cursor = bName.Cursor = null;
			}
		}

		private void b_Click(object sender, RoutedEventArgs e)
		{
			(sender as Button).IsEnabled = false;
			_ClickedCard = (gImages.Children[gImages.Children.Count - 2] as Image).Tag as DominionBase.Cards.Card;

			RaiseEvent(new RoutedEventArgs(CardStackControlClickEvent));
			_ClickedCard = null;
			(sender as Button).IsEnabled = true;
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
}
