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

using DominionBase;
using DominionBase.Piles;
using DominionBase.Players;

namespace Dominion.NET_WPF.Controls
{
	/// <summary>
	/// Interaction logic for ucPlayerDisplay.xaml
	/// </summary>
	public partial class ucPlayerDisplay : UserControl
	{
		private Player.PhaseChangedEventHandler _PhaseChangedEventHandler = null;
		private Player.PlayerModeChangedEventHandler _PlayerModeChangedEventHandler = null;
		private Player.TurnStartedEventHandler _TurnStartedEventHandler = null;
		private Player.TurnEndedEventHandler _TurnEndedEventHandler = null;

		private Pile.PileChangedEventHandler _DeckEventHandler = null;
		private Dictionary<Type, Deck.PileChangedEventHandler> _MatEventHandlers = new Dictionary<Type, Pile.PileChangedEventHandler>();
		private CardMats.CardMatsChangedEventHandler _MatChangedEventHandler = null;
		private TokenCollections.TokenCollectionsChangedEventHandler _TokenCollectionsChangedEventHandler = null;
		private Player _Player = null;
		private Boolean _IsActive = false;
		private Color _ColorFocus = Colors.Transparent;

		public Color ColorFocus
		{
			get { return _ColorFocus; }
			set
			{
				ColorHls hlsValue = HLSColor.RgbToHls(value);

				Color cPlayer = HLSColor.HlsToRgb(hlsValue.H, Math.Min(1d, hlsValue.L * 1.125), hlsValue.S * 0.95, hlsValue.A);
				GradientStopCollection gsc = new GradientStopCollection();
				gsc.Add(new GradientStop(value, 0));
				gsc.Add(new GradientStop(cPlayer, 0.1));
				gsc.Add(new GradientStop(cPlayer, 0.9));
				gsc.Add(new GradientStop(value, 1));
				gsc.Freeze();
				this.Background = new LinearGradientBrush(gsc, 0);

				//this.Background = new SolidColorBrush(HLSColor.HlsToRgb(hlsValue.H, Math.Min(1d, hlsValue.L * 1.125), hlsValue.S * 0.95, hlsValue.A));
				dpStuff.Background = new SolidColorBrush(HLSColor.HlsToRgb((hlsValue.H + 10) % 360, hlsValue.L * 0.8, hlsValue.S, hlsValue.A));
				lStageText.Background = lStage.Background = new SolidColorBrush(HLSColor.HlsToRgb(hlsValue.H, hlsValue.L * 0.95, hlsValue.S, hlsValue.A));
			}
		}

		public Player Player
		{
			get { return _Player; }
			set
			{
				if (_Player != null)
				{
					if (_PhaseChangedEventHandler != null)
						_Player.PhaseChanged -= _PhaseChangedEventHandler;
					if (_PlayerModeChangedEventHandler != null)
						_Player.PlayerModeChanged -= _PlayerModeChangedEventHandler;
					if (_TurnStartedEventHandler != null)
						_Player.TurnStarted -= _TurnStartedEventHandler;
					if (_TurnEndedEventHandler != null)
						_Player.TurnEnded -= _TurnEndedEventHandler;
					if (_MatChangedEventHandler != null)
						_Player.PlayerMats.CardMatsChanged -= _MatChangedEventHandler;
					if (_TokenCollectionsChangedEventHandler != null)
						_Player.TokenPiles.TokenCollectionsChanged -= _TokenCollectionsChangedEventHandler;
				}
				
				_Player = value;

				if (_Player != null)
				{
					_PhaseChangedEventHandler = new Player.PhaseChangedEventHandler(_Player_PhaseChangedEvent);
					_Player.PhaseChanged += _PhaseChangedEventHandler;

					_PlayerModeChangedEventHandler = new Player.PlayerModeChangedEventHandler(_Player_PlayerModeChangedEvent);
					_Player.PlayerModeChanged += _PlayerModeChangedEventHandler;

					_TurnStartedEventHandler = new DominionBase.Players.Player.TurnStartedEventHandler(_Player_TurnStarted);
					_Player.TurnStarted += _TurnStartedEventHandler;

					_TurnEndedEventHandler = new DominionBase.Players.Player.TurnEndedEventHandler(_Player_TurnEnded);
					_Player.TurnEnded += _TurnEndedEventHandler;

					_MatChangedEventHandler = new CardMats.CardMatsChangedEventHandler(PlayerMats_DecksChanged);
					_Player.PlayerMats.CardMatsChanged += _MatChangedEventHandler;

					_TokenCollectionsChangedEventHandler = new TokenCollections.TokenCollectionsChangedEventHandler(TokenPiles_TokenCollectionsChanged);
					_Player.TokenPiles.TokenCollectionsChanged += _TokenCollectionsChangedEventHandler;

					// Fire off each event to get things rolling
					_Player_PhaseChangedEvent(null, new PhaseChangedEventArgs(_Player, _Player.Phase));
					_Player_PlayerModeChangedEvent(null, new PlayerModeChangedEventArgs(_Player, _Player.PlayerMode));
					foreach (CardMat cardMat in _Player.PlayerMats.Values)
						PlayerMats_DecksChanged(null, new CardMatsChangedEventArgs(cardMat, CardMatsChangedEventArgs.Operation.Reset));
					foreach (TokenCollection tokenPile in _Player.TokenPiles.Values)
						TokenPiles_TokenCollectionsChanged(null, new TokenCollectionsChangedEventArgs(tokenPile, TokenCollectionsChangedEventArgs.Operation.Reset));
				}

				// Force reset of IsActive
				this.IsActive = this.IsActive;
			}
		}

		public void TearDown()
		{
			this.IsActive = false;
			this.Player = null;
		}

		void TokenPiles_TokenCollectionsChanged(object sender, TokenCollectionsChangedEventArgs e)
		{
			if (this.Dispatcher.CheckAccess())
			{
				Token token = null;
				if (e.TokenCollection.Count > 0)
					token = e.TokenCollection[0];
				else if (e.AddedTokens.Count > 0)
					token = e.AddedTokens[0];
				else if (e.RemovedTokens.Count > 0)
					token = e.RemovedTokens[0];
				else
					return;

				if (token.GetType() == DominionBase.Cards.Seaside.TypeClass.PirateShipToken)
				{
					Label lPirateShipTokenValue = dpStuff.Children.OfType<Label>().SingleOrDefault(l => l.Name == "lPirateShipTokenValue");
					if (lPirateShipTokenValue == null)
					{
						if (dpStuff.Children.Count > 0)
						{
							Border bDiv = new Border();
							bDiv.BorderThickness = new Thickness(1);
							bDiv.BorderBrush = Brushes.Black;
							Panel.SetZIndex(bDiv, 1);
							DockPanel.SetDock(bDiv, Dock.Left);
							dpStuff.Children.Add(bDiv);
						}

						Label lPirateShipToken = new Label();
						lPirateShipToken.Content = "Pirate Ship Tokens:";
						lPirateShipToken.FontSize = 16d;
						lPirateShipToken.FontWeight = FontWeights.Bold;
						lPirateShipToken.HorizontalContentAlignment = System.Windows.HorizontalAlignment.Right;
						lPirateShipToken.Background = Caching.BrushRepository.GetBackgroundBrush(DominionBase.Cards.Category.Treasure);
						DockPanel.SetDock(lPirateShipToken, Dock.Left);
						dpStuff.Children.Add(lPirateShipToken);

						lPirateShipTokenValue = new Label();
						lPirateShipTokenValue.Name = "lPirateShipTokenValue";
						lPirateShipTokenValue.FontWeight = FontWeights.Bold;
						lPirateShipTokenValue.VerticalAlignment = System.Windows.VerticalAlignment.Stretch;
						lPirateShipTokenValue.VerticalContentAlignment = System.Windows.VerticalAlignment.Center;
						lPirateShipTokenValue.Background = Caching.BrushRepository.GetBackgroundBrush(DominionBase.Cards.Category.Treasure);
						lPirateShipTokenValue.Padding = new Thickness(0, 0, 5, 0);
						lPirateShipTokenValue.BorderThickness = new Thickness(0, 0, 1, 0);
						DockPanel.SetDock(lPirateShipTokenValue, Dock.Left);
						dpStuff.Children.Add(lPirateShipTokenValue);
					}
					lPirateShipTokenValue.Content = e.Count;
				}

				if (token.GetType() == DominionBase.Cards.Guilds.TypeClass.CoinToken)
				{
					Label lCoinTokenValue = dpStuff.Children.OfType<Label>().SingleOrDefault(l => l.Name == "lCoinTokenValue");
					if (lCoinTokenValue == null)
					{
						if (dpStuff.Children.Count > 0)
						{
							Border bDiv = new Border();
							bDiv.BorderThickness = new Thickness(1);
							bDiv.BorderBrush = Brushes.Black;
							Panel.SetZIndex(bDiv, 1);
							DockPanel.SetDock(bDiv, Dock.Left);
							dpStuff.Children.Add(bDiv);
						}

						Label lCoinToken = new Label();
						lCoinToken.Content = "Coin Tokens:";
						lCoinToken.FontSize = 16d;
						lCoinToken.FontWeight = FontWeights.Bold;
						lCoinToken.HorizontalContentAlignment = System.Windows.HorizontalAlignment.Right;
						lCoinToken.Background = Caching.BrushRepository.GetBackgroundBrush(DominionBase.Cards.Category.Treasure);
						DockPanel.SetDock(lCoinToken, Dock.Left);
						dpStuff.Children.Add(lCoinToken);

						lCoinTokenValue = new Label();
						lCoinTokenValue.Name = "lCoinTokenValue";
						lCoinTokenValue.FontWeight = FontWeights.Bold;
						lCoinTokenValue.VerticalAlignment = System.Windows.VerticalAlignment.Stretch;
						lCoinTokenValue.VerticalContentAlignment = System.Windows.VerticalAlignment.Center;
						lCoinTokenValue.Background = Caching.BrushRepository.GetBackgroundBrush(DominionBase.Cards.Category.Treasure);
						lCoinTokenValue.Padding = new Thickness(0, 0, 5, 0);
						lCoinTokenValue.BorderThickness = new Thickness(0, 0, 1, 0);
						DockPanel.SetDock(lCoinTokenValue, Dock.Left);
						dpStuff.Children.Add(lCoinTokenValue);
					}
					lCoinTokenValue.Content = e.Count;
				}

				else if (token.GetType() == DominionBase.Cards.Prosperity.TypeClass.VictoryToken)
				{
					Label lVictoryTokenValue = dpStuff.Children.OfType<Label>().SingleOrDefault(l => l.Name == "lVictoryTokenValue");
					if (lVictoryTokenValue == null)
					{
						if (dpStuff.Children.Count > 0)
						{
							Border bDiv = new Border();
							bDiv.BorderThickness = new Thickness(1);
							bDiv.BorderBrush = Brushes.Black;
							Panel.SetZIndex(bDiv, 1);
							DockPanel.SetDock(bDiv, Dock.Left);
							dpStuff.Children.Add(bDiv);
						}

						Label lVictoryToken = new Label();
						lVictoryToken.Content = "Victory Tokens:";
						lVictoryToken.FontSize = 16d;
						lVictoryToken.FontWeight = FontWeights.Bold;
						lVictoryToken.HorizontalContentAlignment = System.Windows.HorizontalAlignment.Right;
						lVictoryToken.Background = Caching.BrushRepository.GetBackgroundBrush(DominionBase.Cards.Category.Victory);
						DockPanel.SetDock(lVictoryToken, Dock.Left);
						dpStuff.Children.Add(lVictoryToken);

						lVictoryTokenValue = new Label();
						lVictoryTokenValue.Name = "lVictoryTokenValue";
						lVictoryTokenValue.FontWeight = FontWeights.Bold;
						lVictoryTokenValue.VerticalAlignment = System.Windows.VerticalAlignment.Stretch;
						lVictoryTokenValue.VerticalContentAlignment = System.Windows.VerticalAlignment.Center;
						lVictoryTokenValue.Background = Caching.BrushRepository.GetBackgroundBrush(DominionBase.Cards.Category.Victory);
						lVictoryTokenValue.Padding = new Thickness(0, 0, 5, 0);
						lVictoryTokenValue.BorderThickness = new Thickness(0, 0, 1, 0);
						DockPanel.SetDock(lVictoryTokenValue, Dock.Left);
						dpStuff.Children.Add(lVictoryTokenValue);
					}
					lVictoryTokenValue.Content = Utilities.RenderText(String.Format("{0}{1}", e.Count, token.DisplayString), NET_WPF.RenderSize.Tiny, false)[0];
				}
			}
			else
			{
				this.Dispatcher.BeginInvoke(new EventHandler<TokenCollectionsChangedEventArgs>(TokenPiles_TokenCollectionsChanged), System.Windows.Threading.DispatcherPriority.Normal, sender, e);
			}
		}

		void PlayerMats_DecksChanged(object sender, CardMatsChangedEventArgs e)
		{
			_Player_PileChanged(e.CardMat, new PileChangedEventArgs(PileChangedEventArgs.Operation.Reset));
		}

		private void _Player_TurnStarted(object sender, DominionBase.Players.TurnStartedEventArgs e)
		{
			if (cardHand.Dispatcher.CheckAccess())
			{
				if (this.IsUIPlayer)
					cardHand.IsClickable = true;
				else
					cardHand.IsClickable = false;
			}
			else
			{
				cardHand.Dispatcher.BeginInvoke(new EventHandler<DominionBase.Players.TurnStartedEventArgs>(_Player_TurnStarted), System.Windows.Threading.DispatcherPriority.Normal, sender, e);
			}
		}

		private void _Player_TurnEnded(object sender, TurnEndedEventArgs e)
		{
			if (cardHand.Dispatcher.CheckAccess())
			{
				cardHand.IsClickable = false;
			}
			else
			{
				cardHand.Dispatcher.BeginInvoke(new EventHandler<DominionBase.Players.TurnEndedEventArgs>(_Player_TurnEnded), System.Windows.Threading.DispatcherPriority.Normal, sender, e);
			}
		}

		private void _Player_PhaseChangedEvent(object sender, PhaseChangedEventArgs e)
		{
			if (this.Dispatcher.CheckAccess())
			{
				DisplayStage(e.CurrentPlayer.Phase, e.CurrentPlayer.PlayerMode);

				if (this.IsUIPlayer)
				{
					if (e.CurrentPlayer.Phase == DominionBase.Players.PhaseEnum.Starting ||
						e.CurrentPlayer.Phase == DominionBase.Players.PhaseEnum.Waiting ||
						e.CurrentPlayer.Phase == DominionBase.Players.PhaseEnum.Cleanup ||
						e.CurrentPlayer.Phase == DominionBase.Players.PhaseEnum.Endgame ||
						e.CurrentPlayer.PlayerMode == DominionBase.Players.PlayerMode.Waiting ||
						e.CurrentPlayer.PlayerMode == DominionBase.Players.PlayerMode.Choosing)
						cardHand.IsClickable = false;
					else
						cardHand.IsClickable = true;

					if (e.CurrentPlayer == _Player)
						cardHand.Phase = e.NewPhase;
				}
				else
					cardHand.IsClickable = false;

				if (e.NewPhase == PhaseEnum.Endgame)
				{
					lock (e.CurrentPlayer.Hand)
					{
						cardHand.SplitAt = e.CurrentPlayer.Hand.First(
							c => (c.Category & DominionBase.Cards.Category.Victory) != DominionBase.Cards.Category.Victory &&
								(c.Category & DominionBase.Cards.Category.Curse) != DominionBase.Cards.Category.Curse);
					}
					cardDiscard.Visibility = bDeckDiscardDivider.Visibility = cardDeck.Visibility = bDeckHandDivider.Visibility = System.Windows.Visibility.Collapsed;
					bHandInPlayivider.Visibility = dpInPlay.Visibility = bInPlayMatsDivider.Visibility = dpMatsandPiles.Visibility = System.Windows.Visibility.Collapsed;
					bVictoryPointsDivider.Visibility = lVictoryPointsTitle.Visibility = lVictoryPoints.Visibility = System.Windows.Visibility.Visible;
					svPlayerDisplay.HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled;
				}
			}
			else
			{
				this.Dispatcher.BeginInvoke(new EventHandler<PhaseChangedEventArgs>(_Player_PhaseChangedEvent), System.Windows.Threading.DispatcherPriority.Normal, sender, e);
			}
		}
		private void _Player_PlayerModeChangedEvent(object sender, PlayerModeChangedEventArgs e)
		{
			if (this.Dispatcher.CheckAccess())
			{
				DisplayStage(e.CurrentPlayer.Phase, e.CurrentPlayer.PlayerMode);

				if (this.IsUIPlayer)
				{
					if (e.CurrentPlayer.Phase == DominionBase.Players.PhaseEnum.Starting ||
						e.CurrentPlayer.Phase == DominionBase.Players.PhaseEnum.Waiting ||
						e.CurrentPlayer.Phase == DominionBase.Players.PhaseEnum.Cleanup ||
						e.CurrentPlayer.Phase == DominionBase.Players.PhaseEnum.Endgame ||
						e.CurrentPlayer.PlayerMode == DominionBase.Players.PlayerMode.Waiting ||
						e.CurrentPlayer.PlayerMode == DominionBase.Players.PlayerMode.Choosing)
						cardHand.IsClickable = false;
					else
						cardHand.IsClickable = true;

					if (e.CurrentPlayer == _Player)
						cardHand.PlayerMode = e.NewPlayerMode;
				}
				else
					cardHand.IsClickable = false;
			}
			else
			{
				this.Dispatcher.BeginInvoke(new EventHandler<PlayerModeChangedEventArgs>(_Player_PlayerModeChangedEvent), System.Windows.Threading.DispatcherPriority.Normal, sender, e);
			}
		}
		private void DisplayStage(PhaseEnum phase, PlayerMode playerMode)
		{
			StringBuilder sbStage = new StringBuilder();
			switch (phase)
			{
				case PhaseEnum.ActionTreasure:
					sbStage.Append("Action (Treasure)");
					break;
				case PhaseEnum.BuyTreasure:
					sbStage.Append("Buy (Treasure)");
					break;
				default:
					sbStage.Append(phase);
					break;
			}
			switch (playerMode)
			{
				case PlayerMode.Playing:
				case PlayerMode.Buying:
				case PlayerMode.Choosing:
					sbStage.AppendFormat(" - {0}", playerMode);
					break;
			}

			lStage.Content = sbStage.ToString();
		}
		public Boolean IsUIPlayer { get; set; }
		public Boolean IsActive
		{
			get { return _IsActive; }
			set
			{
				if (_IsActive != value)
				{
					if (_Player != null)
					{
						_Player.Hand.PileChanged -= _DeckEventHandler;
						_Player.Revealed.PileChanged -= _DeckEventHandler;
						_Player.InPlay.PileChanged -= _DeckEventHandler;
						_Player.SetAside.PileChanged -= _DeckEventHandler;
						_Player.DrawPile.PileChanged -= _DeckEventHandler;
						_Player.DiscardPile.PileChanged -= _DeckEventHandler;
						_Player.Private.PileChanged -= _DeckEventHandler;

						foreach (Type matType in _MatEventHandlers.Keys)
							_Player.PlayerMats[matType].PileChanged -= _MatEventHandlers[matType];
					}
					_DeckEventHandler = null;
					_MatEventHandlers.Clear();

					_IsActive = value;

					if (_IsActive && _Player != null)
					{
						PileChangedEventArgs pcea = new PileChangedEventArgs(_Player, PileChangedEventArgs.Operation.Added);

						_DeckEventHandler = new Deck.PileChangedEventHandler(_Player_PileChanged);
						_Player.Hand.PileChanged += _DeckEventHandler;
						_Player_PileChanged(_Player.Hand, pcea);

						_Player.Revealed.PileChanged += _DeckEventHandler;
						_Player_PileChanged(_Player.Revealed, pcea);

						_Player.InPlay.PileChanged += _DeckEventHandler;
						_Player.SetAside.PileChanged += _DeckEventHandler;
						_Player_PileChanged(_Player.InPlay, pcea);
						_Player_PileChanged(_Player.SetAside, pcea);

						_Player.DrawPile.PileChanged += _DeckEventHandler;
						_Player_PileChanged(_Player.DrawPile, pcea);

						_Player.DiscardPile.PileChanged += _DeckEventHandler;
						_Player_PileChanged(_Player.DiscardPile, pcea);

						_Player.Private.PileChanged += _DeckEventHandler;
						_Player_PileChanged(_Player.Private, pcea);

						if (_Player.PlayerMats.ContainsKey(DominionBase.Cards.Seaside.TypeClass.IslandMat))
						{
							_MatEventHandlers[DominionBase.Cards.Seaside.TypeClass.IslandMat] = new Pile.PileChangedEventHandler(_Player_PileChanged);
							_Player.PlayerMats[DominionBase.Cards.Seaside.TypeClass.IslandMat].PileChanged += _MatEventHandlers[DominionBase.Cards.Seaside.TypeClass.IslandMat];
							_Player_PileChanged(_Player.PlayerMats[DominionBase.Cards.Seaside.TypeClass.IslandMat], pcea);
						}

						if (_Player.PlayerMats.ContainsKey(DominionBase.Cards.Seaside.TypeClass.NativeVillageMat))
						{
							_MatEventHandlers[DominionBase.Cards.Seaside.TypeClass.NativeVillageMat] = new Pile.PileChangedEventHandler(_Player_PileChanged);
							_Player.PlayerMats[DominionBase.Cards.Seaside.TypeClass.NativeVillageMat].PileChanged += _MatEventHandlers[DominionBase.Cards.Seaside.TypeClass.NativeVillageMat];
							_Player_PileChanged(_Player.PlayerMats[DominionBase.Cards.Seaside.TypeClass.NativeVillageMat], pcea);
						}

						if (_Player.PlayerMats.ContainsKey(DominionBase.Cards.Promotional.TypeClass.PrinceSetAside))
						{
							_MatEventHandlers[DominionBase.Cards.Promotional.TypeClass.PrinceSetAside] = new Pile.PileChangedEventHandler(_Player_PileChanged);
							_Player.PlayerMats[DominionBase.Cards.Promotional.TypeClass.PrinceSetAside].PileChanged += _MatEventHandlers[DominionBase.Cards.Promotional.TypeClass.PrinceSetAside];
							_Player_PileChanged(_Player.PlayerMats[DominionBase.Cards.Promotional.TypeClass.PrinceSetAside], pcea);
						}
					}
				}
			}
		}

		public ucPlayerDisplay()
		{
			InitializeComponent();

			cardHand.PileName = "Hand";
			cardRevealed.PileName = "Revealed";
			cardInPlay.PileName = "In play";
			cardSetAside.PileName = "Set Aside";
			cardDeck.PileName = "Deck";
			cardDiscard.PileName = "Discard Pile";
			cardPrivate.PileName = "Looking At";
		}

		private void CardCollectionControl_CardCollectionControlClick(object sender, RoutedEventArgs e)
		{
		}

		private void _Player_PileChanged(object sender, PileChangedEventArgs e)
		{
			if (this.Dispatcher.CheckAccess())
			{

				// This sometimes has an Enumeration exception -- can't figure out how to Lock it properly
				if (e.Player != null)
					try { lVictoryPoints.Content = e.Player.VictoryPoints.ToString(); }
					catch { lVictoryPoints.Content = "0"; }

				Deck deck = sender as Deck;
				Boolean visible = this.IsUIPlayer || (e.Player != null && e.Player.Phase == DominionBase.Players.PhaseEnum.Endgame);

				switch (deck.DeckLocation)
				{
					case DeckLocation.Hand:
						cardHand.ExactCount = true;
						cardHand.IsCardsVisible = visible;
						cardHand.Phase = e.Player.Phase;
						cardHand.PlayerMode = e.Player.PlayerMode;
						cardHand.CardSize = e.Player.Phase == PhaseEnum.Endgame ? CardSize.Text : CardSize.Medium;
						cardHand.IsVPsVisible = e.Player.Phase == PhaseEnum.Endgame ? true : false;
						cardHand.Pile = deck;
						if (visible)
							svPlayerDisplay.ScrollToTop();
						break;

					case DeckLocation.Revealed:
						cardRevealed.ExactCount = true;
						cardRevealed.IsCardsVisible = true;
						cardRevealed.Phase = PhaseEnum.Action;
						cardRevealed.PlayerMode = PlayerMode.Waiting;
						cardRevealed.CardSize = CardSize.Small;
						if (e.Player.Revealed.Count > 0)
							bInPlayRevealedDivider.Visibility = cardRevealed.Visibility = System.Windows.Visibility.Visible;
						else
							bInPlayRevealedDivider.Visibility = cardRevealed.Visibility = System.Windows.Visibility.Collapsed;
						cardRevealed.Pile = deck;
						break;

					case DeckLocation.InPlay:
						cardInPlay.ExactCount = true;
						cardInPlay.IsCardsVisible = true;
						cardInPlay.Phase = PhaseEnum.Action;
						cardInPlay.PlayerMode = PlayerMode.Waiting;
						cardInPlay.CardSize = CardSize.Small;
						cardInPlay.Pile = deck;
						if (e.Player.PlayerUniqueId != _Player.UniqueId && e.Player.Phase != DominionBase.Players.PhaseEnum.Endgame)
							svPlayerDisplay.ScrollToVerticalOffset(cardHand.ActualHeight + cardDiscard.ActualHeight);

						break;

					case DeckLocation.SetAside:
						cardSetAside.ExactCount = true;
						cardSetAside.IsCardsVisible = true;
						cardSetAside.Phase = PhaseEnum.Action;
						cardSetAside.PlayerMode = PlayerMode.Waiting;
						cardSetAside.CardSize = CardSize.Small;
						if (deck.Count > 0)
							bSetAsideDivider.Visibility = cardSetAside.Visibility = System.Windows.Visibility.Visible;
						else
							bSetAsideDivider.Visibility = cardSetAside.Visibility = System.Windows.Visibility.Collapsed;
						cardSetAside.Pile = deck;

						break;

					case DeckLocation.Deck:
						cardDeck.ExactCount = this.IsUIPlayer;
						cardDeck.IsCardsVisible = false;
						cardDeck.Phase = PhaseEnum.Action;
						cardDeck.PlayerMode = PlayerMode.Waiting;
						cardDeck.CardSize = CardSize.Small;
						cardDeck.Pile = deck;
						break;

					case DeckLocation.Discard:
						cardDiscard.ExactCount = false;
						cardDiscard.IsCardsVisible = true;
						cardDiscard.Phase = PhaseEnum.Action;
						cardDiscard.PlayerMode = PlayerMode.Waiting;
						cardDiscard.CardSize = CardSize.Small;
						if (e.Player.DiscardPile.Count > 0)
							bDeckDiscardDivider.Visibility = cardDiscard.Visibility = System.Windows.Visibility.Visible;
						else
							bDeckDiscardDivider.Visibility = cardDiscard.Visibility = System.Windows.Visibility.Collapsed;
						cardDiscard.Pile = deck;
						break;

					case DeckLocation.Private:
						cardPrivate.ExactCount = true;
						cardPrivate.IsCardsVisible = visible;
						cardPrivate.Phase = PhaseEnum.Action;
						cardPrivate.PlayerMode = PlayerMode.Waiting;
						cardPrivate.CardSize = CardSize.Small;
						if (e.Player.Private.Count > 0)
							bRevealedLookingAtDivider.Visibility = cardPrivate.Visibility = System.Windows.Visibility.Visible;
						else
							bRevealedLookingAtDivider.Visibility = cardPrivate.Visibility = System.Windows.Visibility.Collapsed;
						cardPrivate.Pile = deck;
						break;

					case DeckLocation.PlayerMat:

						if (sender.GetType() == DominionBase.Cards.Seaside.TypeClass.IslandMat)
						{
							CardCollectionControl cccIsland = dpMatsandPiles.Children.OfType<CardCollectionControl>().SingleOrDefault(ccc => ccc.Name == "cardIsland");
							if (cccIsland == null)
							{
								if (dpMatsandPiles.Children.Count > 0)
								{
									Border bDiv = new Border();
									bDiv.BorderThickness = new Thickness(2);
									bDiv.BorderBrush = Brushes.Black;
									DockPanel.SetDock(bDiv, Dock.Left);
									dpMatsandPiles.Children.Add(bDiv);
								}
								cccIsland = new CardCollectionControl();
								cccIsland.Name = "cardIsland";
								cccIsland.Padding = new Thickness(0);
								DockPanel.SetDock(cccIsland, Dock.Left);
								//cccIsland.Background = Brushes.AliceBlue;
								cccIsland.PileName = "Island Mat";
								cccIsland.CardSize = CardSize.Text;
								dpMatsandPiles.Children.Add(cccIsland);
							}
							cccIsland.ExactCount = true;
							cccIsland.IsCardsVisible = true;
							cccIsland.Phase = PhaseEnum.Action;
							cccIsland.PlayerMode = PlayerMode.Waiting;
							cccIsland.Pile = deck;
						}
						else if (sender.GetType() == DominionBase.Cards.Seaside.TypeClass.NativeVillageMat)
						{
							CardCollectionControl cccNativeVillage = dpMatsandPiles.Children.OfType<CardCollectionControl>().SingleOrDefault(ccc => ccc.Name == "cardNativeVillage");
							if (cccNativeVillage == null)
							{
								if (dpMatsandPiles.Children.Count > 0)
								{
									Border bDiv = new Border();
									bDiv.BorderThickness = new Thickness(2);
									bDiv.BorderBrush = Brushes.Black;
									DockPanel.SetDock(bDiv, Dock.Left);
									dpMatsandPiles.Children.Add(bDiv);
								}
								cccNativeVillage = new CardCollectionControl();
								cccNativeVillage.Name = "cardNativeVillage";
								cccNativeVillage.Padding = new Thickness(0);
								DockPanel.SetDock(cccNativeVillage, Dock.Left);
								//cccNativeVillage.Background = Brushes.AliceBlue;
								cccNativeVillage.PileName = "Native Village Mat";
								cccNativeVillage.CardSize = CardSize.Text;
								dpMatsandPiles.Children.Add(cccNativeVillage);
							}
							cccNativeVillage.ExactCount = true;
							cccNativeVillage.IsCardsVisible = visible;
							cccNativeVillage.Phase = PhaseEnum.Action;
							cccNativeVillage.PlayerMode = PlayerMode.Waiting;
							cccNativeVillage.Pile = deck;
						}
						else if (sender.GetType() == DominionBase.Cards.Promotional.TypeClass.PrinceSetAside)
						{
							CardCollectionControl cccPrinceSetAside = dpMatsandPiles.Children.OfType<CardCollectionControl>().SingleOrDefault(ccc => ccc.Name == "cardPrinceSetAside");
							if (cccPrinceSetAside == null)
							{
								if (dpMatsandPiles.Children.Count > 0)
								{
									Border bDiv = new Border();
									bDiv.BorderThickness = new Thickness(2);
									bDiv.BorderBrush = Brushes.Black;
									DockPanel.SetDock(bDiv, Dock.Left);
									dpMatsandPiles.Children.Add(bDiv);
								}
								cccPrinceSetAside = new CardCollectionControl();
								cccPrinceSetAside.Name = "cardPrinceSetAside";
								cccPrinceSetAside.Padding = new Thickness(0);
								DockPanel.SetDock(cccPrinceSetAside, Dock.Left);
								cccPrinceSetAside.PileName = "Set Aside";
								cccPrinceSetAside.CardSize = CardSize.Text;
								dpMatsandPiles.Children.Add(cccPrinceSetAside);
							}
							cccPrinceSetAside.ExactCount = true;
							cccPrinceSetAside.IsCardsVisible = true;
							cccPrinceSetAside.CardSize = CardSize.Small;
							cccPrinceSetAside.Phase = PhaseEnum.Action;
							cccPrinceSetAside.PlayerMode = PlayerMode.Waiting;
							cccPrinceSetAside.Pile = deck;
						}

						break;
				}
			}
			else
			{
				this.Dispatcher.BeginInvoke(new EventHandler<PileChangedEventArgs>(_Player_PileChanged), System.Windows.Threading.DispatcherPriority.Normal, sender, e);
			}
		}

		private void svPlayerDisplay_ScrollChanged(object sender, ScrollChangedEventArgs e)
		{
			ScrollViewer sv = sender as ScrollViewer;
			bHorizontal.Width = sv.ViewportWidth * sv.ViewportWidth / sv.ExtentWidth;
			bVertical.Height = sv.ViewportHeight * sv.ViewportHeight / sv.ExtentHeight;

			bHorizontal.Margin = new Thickness(sv.ViewportWidth * sv.HorizontalOffset / sv.ExtentWidth, 0, 0, 0);
			bVertical.Margin = new Thickness(0, sv.ViewportHeight * sv.VerticalOffset / sv.ExtentHeight, 0, 0);

			bOpacityLayerLeft.Visibility = bOpacityLayerRight.Visibility = System.Windows.Visibility.Visible;
			if (bHorizontal.Width >= sv.ViewportWidth)
				bOpacityLayerLeft.Visibility = bOpacityLayerRight.Visibility = System.Windows.Visibility.Collapsed;
			else if (bHorizontal.Margin.Left <= 0)
				bOpacityLayerLeft.Visibility = System.Windows.Visibility.Collapsed;
			else if (bHorizontal.Margin.Left + bHorizontal.Width >= sv.ViewportWidth)
				bOpacityLayerRight.Visibility = System.Windows.Visibility.Collapsed;

			bOpacityLayerTop.Visibility = bOpacityLayerBottom.Visibility = System.Windows.Visibility.Visible;
			if (bVertical.Height >= sv.ViewportHeight)
				bOpacityLayerTop.Visibility = bOpacityLayerBottom.Visibility = System.Windows.Visibility.Collapsed;
			else if (bVertical.Margin.Top <= 0)
				bOpacityLayerTop.Visibility = System.Windows.Visibility.Collapsed;
			else if (bVertical.Margin.Top + bVertical.Height >= sv.ViewportHeight)
				bOpacityLayerBottom.Visibility = System.Windows.Visibility.Collapsed;
		}

		private void Grid_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
		{
			Grid g = (Grid)sender;
			g.CaptureMouse();
			g.Cursor = Cursors.ScrollNS;
			svPlayerDisplay.ScrollToVerticalOffset(e.GetPosition(g).Y / g.ActualHeight * svPlayerDisplay.ExtentHeight);
		}

		private void Grid_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
		{
			Grid g = (Grid)sender;
			g.ReleaseMouseCapture();
			g.Cursor = Cursors.Arrow;
		}

		private void Grid_MouseMove(object sender, MouseEventArgs e)
		{
			Grid g = (Grid)sender;
			if (g.IsMouseCaptured)
				svPlayerDisplay.ScrollToVerticalOffset(e.GetPosition(g).Y / g.ActualHeight * svPlayerDisplay.ExtentHeight);
		}

		private void Grid_MouseLeftButtonDown_1(object sender, MouseButtonEventArgs e)
		{
			Grid g = (Grid)sender;
			g.CaptureMouse();
			g.Cursor = Cursors.ScrollWE;
			svPlayerDisplay.ScrollToHorizontalOffset(e.GetPosition(g).X / g.ActualWidth * svPlayerDisplay.ExtentWidth);
		}

		private void Grid_MouseMove_1(object sender, MouseEventArgs e)
		{
			Grid g = (Grid)sender;
			if (g.IsMouseCaptured)
				svPlayerDisplay.ScrollToHorizontalOffset(e.GetPosition(g).X / g.ActualWidth * svPlayerDisplay.ExtentWidth);
		}
	}
}
