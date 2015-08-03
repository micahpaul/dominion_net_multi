using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;

using ICSharpCode.SharpZipLib.Zip;

using DominionBase.Utilities;

namespace Dominion.NET_WPF
{
	delegate void player_ChooseDelegate(DominionBase.Players.Player player, DominionBase.Choice choice);

	/// <summary>
	/// Interaction logic for wMain.xaml
	/// </summary>
	public partial class wMain : Window
	{
		private static Settings _Settings = null;
		public static Settings Settings { get { return _Settings; } private set { _Settings = value; } }

		public DominionBase.Game game = null;
		private DominionBase.Players.Player _Player = null;
		private VersionInfo latestVersionInfo = null;

		private Dictionary<Type, DominionBase.Piles.Pile.PileChangedEventHandler> _MatEventHandlers = new Dictionary<Type, DominionBase.Piles.Pile.PileChangedEventHandler>();
		private Thread gameThread = null;

		public AutoResetEvent WaitEvent = new AutoResetEvent(false);

		private Label _TradeRouteLabel = null;

		private int _CurrentPlayDepth = 0;

		private Boolean _StartingNewGame = false;

		private Statistics _Statistics = null;

		public wMain()
		{
			InitializeComponent();
			if (!System.IO.Directory.Exists(DominionBase.Utilities.Application.ApplicationPath))
				System.IO.Directory.CreateDirectory(DominionBase.Utilities.Application.ApplicationPath);

			bTurnDone.IsEnabled = false;
			bPlayTreasures.Text = "Play basic _Treasures";
			bPlayTreasures.IsEnabled = false;
			bPlayCoinTokens.IsEnabled = false;
			bBuyPhase.IsEnabled = false;
			bUndo.IsEnabled = false;

			cardTrash.PileName = "Trash";

			_Statistics = Statistics.Load();
		}

		private void Window_Initialized(object sender, EventArgs e)
		{
			wMain.Settings = Settings.Load();
			this.DataContext = wMain.Settings;

#if DEBUG
#else
			miCheckForUpdates.IsEnabled = false;
			BackgroundWorker bwUpdate = new BackgroundWorker();
			bwUpdate.DoWork += new DoWorkEventHandler(bwUpdate_DoWork);
			bwUpdate.RunWorkerCompleted += new RunWorkerCompletedEventHandler(bwUpdate_RunWorkerCompleted);
			bwUpdate.RunWorkerAsync();
#endif

			glMain.LogFile = System.IO.Path.Combine(DominionBase.Utilities.Application.ApplicationPath, "game.log");
			if (_Settings.WindowSize.Width > 0)
				this.Width = _Settings.WindowSize.Width;
			if (_Settings.WindowSize.Height > 0)
				this.Height = _Settings.WindowSize.Height;
			this.WindowState = _Settings.WindowState;
		}

		void bwUpdate_DoWork(object sender, DoWorkEventArgs e)
		{
			try { CheckForUpdates(false); }
			catch { }
		}

		void bwUpdate_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
		{
			miCheckForUpdates.IsEnabled = true;
		}

		private void EnqueueGameMessageAndWait(DominionBase.GameMessage message)
		{
			Boolean lockWasTaken = false;
			var temp = game.MessageRequestQueue;
			try { Monitor.Enter(temp, ref lockWasTaken); { game.MessageRequestQueue.Enqueue(message); } }
			finally { if (lockWasTaken) Monitor.Exit(temp); }
			game.WaitEvent.Set();

			// So... it turns out that this will completely hang the application if allowed to actually wait for the MessageResponseQueue to empty out.
			// It's not great that this isn't working the way it's *supposed to* work, but that's an investigation for a different day.  For now, it works
			// as it works and it's unclear what the ramifications of that are.
			//while (game.MessageResponseQueue.Count == 0)
				//Thread.Sleep(100);
		}

		private void ReleaseEvents()
		{
			if (game == null)
				return;

			game.GameEndedEvent -= new DominionBase.Game.GameEndedEventHandler(game_GameEndedEvent);
			game.GameMessage -= new DominionBase.Game.GameMessageEventHandler(game_GameMessage);

			if (game.Table != null)
			{
				if (game.Table.TokenPiles != null)
					game.Table.TokenPiles.TokenCollectionsChanged -= new DominionBase.TokenCollections.TokenCollectionsChangedEventHandler(TokenPiles_TokenCollectionsChanged);
				if (game.Table.Trash != null)
					game.Table.Trash.PileChanged -= new DominionBase.Piles.Pile.PileChangedEventHandler(Trash_PileChanged);

				if (game.Table.SpecialPiles.ContainsKey(DominionBase.Cards.Promotional.TypeClass.BlackMarketSupply))
					game.Table.SpecialPiles[DominionBase.Cards.Promotional.TypeClass.BlackMarketSupply].PileChanged -= _MatEventHandlers[DominionBase.Cards.Promotional.TypeClass.BlackMarketSupply];
				if (game.Table.SpecialPiles.ContainsKey(DominionBase.Cards.Cornucopia.TypeClass.PrizeSupply))
					game.Table.SpecialPiles[DominionBase.Cards.Cornucopia.TypeClass.PrizeSupply].PileChanged -= _MatEventHandlers[DominionBase.Cards.Cornucopia.TypeClass.PrizeSupply];
				if (game.Table.SpecialPiles.ContainsKey(DominionBase.Cards.DarkAges.TypeClass.Madman))
					game.Table.SpecialPiles[DominionBase.Cards.DarkAges.TypeClass.Madman].PileChanged -= _MatEventHandlers[DominionBase.Cards.DarkAges.TypeClass.Madman];
				if (game.Table.SpecialPiles.ContainsKey(DominionBase.Cards.DarkAges.TypeClass.Mercenary))
					game.Table.SpecialPiles[DominionBase.Cards.DarkAges.TypeClass.Mercenary].PileChanged -= _MatEventHandlers[DominionBase.Cards.DarkAges.TypeClass.Mercenary];
				if (game.Table.SpecialPiles.ContainsKey(DominionBase.Cards.DarkAges.TypeClass.Spoils))
					game.Table.SpecialPiles[DominionBase.Cards.DarkAges.TypeClass.Spoils].PileChanged -= _MatEventHandlers[DominionBase.Cards.DarkAges.TypeClass.Spoils];
			}

			if (game.Players != null)
			{
				foreach (DominionBase.Players.Player player in game.Players)
				{
					player.Choose = null;

					player.Revealed.PileChanged -= new DominionBase.Piles.Pile.PileChangedEventHandler(Revealed_PileChanged);
					player.BenefitReceiving -= new DominionBase.Players.Player.BenefitReceivingEventHandler(player_BenefitReceiving);
					player.CardPlaying -= new DominionBase.Players.Player.CardPlayingEventHandler(player_CardPlaying);
					player.CardPlayed -= new DominionBase.Players.Player.CardPlayedEventHandler(player_CardPlayed);
					player.CardUndoPlaying -= new DominionBase.Players.Player.CardUndoPlayingEventHandler(player_CardUndoPlaying);
					player.CardUndoPlayed -= new DominionBase.Players.Player.CardUndoPlayedEventHandler(player_CardUndoPlayed);
					player.CardBuying -= new DominionBase.Players.Player.CardBuyingEventHandler(player_CardBuying);
					player.CardBought -= new DominionBase.Players.Player.CardBoughtEventHandler(player_CardBought);
					player.CardBuyFinished -= new DominionBase.Players.Player.CardBuyFinishedEventHandler(player_CardBuyFinished);
					player.CardGaining -= new DominionBase.Players.Player.CardGainingEventHandler(player_CardGaining);
					player.CardGainedInto -= new DominionBase.Players.Player.CardGainedIntoEventHandler(player_CardGainedInto);
					player.CardGainFinished -= new DominionBase.Players.Player.CardGainFinishedEventHandler(player_CardGainFinished);
					player.TokenPlaying -= new DominionBase.Players.Player.TokenPlayingEventHandler(player_TokenPlaying);
					player.TokenPlayed -= new DominionBase.Players.Player.TokenPlayedEventHandler(player_TokenPlayed);
					player.Trashing -= new DominionBase.Players.Player.TrashingEventHandler(player_Trashing);
					player.TrashedFinished -= new DominionBase.Players.Player.TrashedFinishedEventHandler(player_Trashed);
					player.PhaseChanged -= new DominionBase.Players.Player.PhaseChangedEventHandler(player_PhaseChangedEvent);
					player.PlayerModeChanged -= new DominionBase.Players.Player.PlayerModeChangedEventHandler(player_PlayerModeChangedEvent);
					player.CardsDrawn -= new DominionBase.Players.Player.CardsDrawnEventHandler(player_CardsDrawn);
					player.TurnStarting -= new DominionBase.Players.Player.TurnStartingEventHandler(player_TurnStarting);
					player.Shuffling -= new DominionBase.Players.Player.ShufflingEventHandler(player_Shuffle);
					player.CardsAddedToDeck -= new DominionBase.Players.Player.CardsAddedToDeckEventHandler(player_CardsAddedToDeck);
					player.CardsAddedToHand -= new DominionBase.Players.Player.CardsAddedToHandEventHandler(player_CardsAddedToHand);
					player.CardsDiscarded -= new DominionBase.Players.Player.CardsDiscardedEventHandler(player_CardsDiscarded);
					player.PlayerMats.CardMatsChanged -= new DominionBase.Piles.CardMats.CardMatsChangedEventHandler(PlayerMats_DecksChanged);
					player.TokenPiles.TokenCollectionsChanged -= new DominionBase.TokenCollections.TokenCollectionsChangedEventHandler(PlayerTokenPiles_TokenCollectionsChanged);
					player.BenefitsChanged -= new DominionBase.Players.Player.BenefitsChangedEventHandler(player_BenefitsChanged);

					if (player == _Player)
					{
						player.CardReceived -= new DominionBase.Players.Player.CardReceivedEventHandler(player_CardReceived);
					}
				}
			}
		}

		private void ReleaseGame()
		{
			_Player = null;
			uccChooser.IsReady = false;

			cardTrash.Pile = null;
			foreach (TabItem ti in tcAreas.Items)
			{
				if (ti.Content is Controls.ucPlayerDisplay)
				{
					(ti.Content as Controls.ucPlayerDisplay).TearDown();
					(((ti.Header as DockPanel).ToolTip as ToolTip).Content as Controls.ucPlayerOverview).TearDown();
				}
			}

			foreach (StackPanel sp in stackPanelSupplyPiles.Children.OfType<StackPanel>())
			{
				foreach (SupplyControl sc in sp.Children.OfType<SupplyControl>())
				{
					sc.Supply = null;
				}
			}

			if (game == null)
				return;

			ReleaseEvents();

			game.Clear();
			game = null;

			GC.Collect();
			GC.Collect(1);
			GC.Collect(2);
			GC.Collect(3);
			GC.WaitForPendingFinalizers();
			GC.Collect();
		}

		private void StartGame(DominionBase.GameSettings settings)
		{
			_StartingNewGame = false;

			ReleaseGame();

			wMain.Settings = Settings.Load();

			// Clean out the Image Repository before starting a new game -- 
			// so we don't allocate too much memory for cards we're not even using
			Caching.ImageRepository.Reset();
			dpGameInfo.Visibility = System.Windows.Visibility.Visible;
			glMain.TearDown();
			glMain.Clear();

			LayoutSupplyPiles();
			while (tcAreas.Items.Count > 2)
				tcAreas.Items.RemoveAt(tcAreas.Items.Count - 1);
			dpMatsandPiles.Children.Clear();
			dpGameStuff.Children.Clear();
			_TradeRouteLabel = null;

			// Try to force garbage collection to save some memory
			GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced);

			try
			{
				game = new DominionBase.Game(
					_Settings.NumberOfHumanPlayers,
					_Settings.PlayerSettings.Take(_Settings.NumberOfPlayers).Select(ps => ps.Name), 
					_Settings.PlayerSettings.Take(_Settings.NumberOfPlayers).Select(ps => ps.AIClassType),
					settings);

				game.SelectCards();

				if (game.Settings.Preset != null || Settings.AutomaticallyAcceptKingdomCards)
					this.AcceptGame();
				else
				{
					wCardSelection selector = new wCardSelection();
					selector.Owner = this;
					if (selector.ShowDialog() == true)
					{
						Settings.AutomaticallyAcceptKingdomCards = (selector.cbAutoAccept.IsChecked == true);
						Settings.Save();
						this.AcceptGame();
					}
					else
						game = null;
				}
			}
			catch (DominionBase.Cards.ConstraintException ce)
			{
				wMessageBox.Show(ce.Message, "Constraint exception!", MessageBoxButton.OK, MessageBoxImage.Exclamation);
				return;
			}
			catch (DominionBase.GameCreationException gce)
			{
				wMessageBox.Show(gce.Message, "Game creation exception!", MessageBoxButton.OK, MessageBoxImage.Exclamation);
				return;
			}
		}

		private void AcceptGame()
		{
			game.AcceptCards();

			game.GameEndedEvent += new DominionBase.Game.GameEndedEventHandler(game_GameEndedEvent);

			wMain.Settings.PlayerSettings.ForEach(ps => glMain.AddPlayerColor(ps.Name, ps.UIColor));
			glMain.NewSection(String.Format("Game started with {0} players", game.Players.Count));

			game.Table.TokenPiles.TokenCollectionsChanged += new DominionBase.TokenCollections.TokenCollectionsChangedEventHandler(TokenPiles_TokenCollectionsChanged);
			game.Table.Trash.PileChanged += new DominionBase.Piles.Pile.PileChangedEventHandler(Trash_PileChanged);
			Trash_PileChanged(game.Table.Trash, new DominionBase.Piles.PileChangedEventArgs(DominionBase.Piles.PileChangedEventArgs.Operation.Reset));
			game.GameMessage += new DominionBase.Game.GameMessageEventHandler(game_GameMessage);

			foreach (DominionBase.Players.Player player in game.Players.FindAll(p => p.PlayerType == DominionBase.Players.PlayerType.Human))
				player.Choose = player_Choose;

			if (game.Players.Any(player => player.PlayerType == DominionBase.Players.PlayerType.Human))
				_Player = game.Players.OfType<DominionBase.Players.Human>().First();
			else
				_Player = null;

			String message = "Using the following cards";
			if (game.Settings.Preset != null)
				message = String.Format("{0} from the preset \"{1}\"", message, game.Settings.Preset.Name);
			glMain.Log(String.Format("{0}:", message));
			IEnumerable<DominionBase.Piles.Supply> kingdomSupplies = game.Table.Supplies.Where(kvp => kvp.Value.Randomizer.Location == DominionBase.Cards.Location.Kingdom).Select(kvp => kvp.Value).OrderBy(s => s.Name);
			glMain.Log("   ", kingdomSupplies.Take(5));
			glMain.Log("   ", kingdomSupplies.Skip(5));

			int prosperityPiles = game.Table.Supplies.Count(kvp => kvp.Value.Location == DominionBase.Cards.Location.Kingdom && kvp.Value.Source == DominionBase.Cards.Source.Prosperity);
			int darkAgesPiles = game.Table.Supplies.Count(kvp => kvp.Value.Location == DominionBase.Cards.Location.Kingdom && kvp.Value.Source == DominionBase.Cards.Source.DarkAges);
			int kingdomPiles = game.Table.Supplies.Count(kvp => kvp.Value.Location == DominionBase.Cards.Location.Kingdom && kvp.Value.Tokens.Count(t => t.GetType() == DominionBase.Cards.Cornucopia.TypeClass.BaneToken) == 0);
			glMain.Log(String.Format("Prosperity Kingdom card ratio is {0}/{1} = {2:P0}", 
				prosperityPiles,
				kingdomPiles,
				((float)prosperityPiles) / kingdomPiles
				));
			glMain.Log(String.Format("   Colony / Platinum {0}selected",
				game.Settings.ColonyPlatinumUsage == DominionBase.ColonyPlatinumUsage.Used ? "" : "<u>not</u> "
				));
			glMain.Log(String.Format("Dark Ages Kingdom card ratio is {0}/{1} = {2:P0}",
				darkAgesPiles,
				kingdomPiles,
				((float)darkAgesPiles) / kingdomPiles
				));
			glMain.Log(String.Format("   Shelters {0}selected",
				game.Settings.ShelterUsage == DominionBase.ShelterUsage.Used ? "" : "<u>not</u> "
				));

			glMain.Log("Turn order is: ", String.Join(", ", game.Players.Select(p => p == _Player ? String.Format("{0} (You)", p.Name) : p.Name)));


			Type[] specialTypes = new Type[] { 
				DominionBase.Cards.Promotional.TypeClass.BlackMarketSupply, 
				DominionBase.Cards.Cornucopia.TypeClass.PrizeSupply, 
				DominionBase.Cards.DarkAges.TypeClass.Madman,
				DominionBase.Cards.DarkAges.TypeClass.Mercenary,
				DominionBase.Cards.DarkAges.TypeClass.Spoils };

			foreach (Type specialType in specialTypes)
			{
				if (game.Table.SpecialPiles.ContainsKey(specialType))
				{
					_MatEventHandlers[specialType] = new DominionBase.Piles.Pile.PileChangedEventHandler(GamePile_PileChanged);
					game.Table.SpecialPiles[specialType].PileChanged += _MatEventHandlers[specialType];
					GamePile_PileChanged(game.Table.SpecialPiles[specialType], new DominionBase.Piles.PileChangedEventArgs(DominionBase.Piles.PileChangedEventArgs.Operation.Reset));
				}
			}

			if (game.Table.Supplies.ContainsKey(DominionBase.Cards.Prosperity.TypeClass.TradeRoute))
			{
				if (dpGameStuff.Children.Count > 0)
				{
					Border bDiv = new Border();
					bDiv.BorderThickness = new Thickness(2);
					bDiv.BorderBrush = Brushes.Black;
					Panel.SetZIndex(bDiv, 1);
					DockPanel.SetDock(bDiv, Dock.Left);
					dpGameStuff.Children.Add(bDiv);
				}

				Label lTradeRoute = new Label();
				lTradeRoute.Content = "Trade Route Tokens:";
				lTradeRoute.FontSize = 16d;
				lTradeRoute.FontWeight = FontWeights.Bold;
				lTradeRoute.HorizontalContentAlignment = System.Windows.HorizontalAlignment.Right;
				lTradeRoute.Background = Caching.BrushRepository.GetBackgroundBrush(DominionBase.Cards.Category.Treasure);
				DockPanel.SetDock(lTradeRoute, Dock.Left);
				dpGameStuff.Children.Add(lTradeRoute);

				_TradeRouteLabel = new Label();
				_TradeRouteLabel.Content = "0";
				_TradeRouteLabel.FontWeight = FontWeights.Bold;
				_TradeRouteLabel.VerticalAlignment = System.Windows.VerticalAlignment.Stretch;
				_TradeRouteLabel.VerticalContentAlignment = System.Windows.VerticalAlignment.Center;
				_TradeRouteLabel.Background = Caching.BrushRepository.GetBackgroundBrush(DominionBase.Cards.Category.Treasure);
				_TradeRouteLabel.Padding = new Thickness(0, 0, 5, 0);
				_TradeRouteLabel.BorderThickness = new Thickness(0, 0, 1, 0);
				DockPanel.SetDock(_TradeRouteLabel, Dock.Left);
				dpGameStuff.Children.Add(_TradeRouteLabel);
			}

			if (dpGameStuff.Children.Count > 0)
				bStuffDivider.Visibility = System.Windows.Visibility.Visible;
			else
				bStuffDivider.Visibility = System.Windows.Visibility.Collapsed;

			foreach (DominionBase.Players.Player player in game.Players)
			{
				TabItem tiPlayer = new TabItem();

				DockPanel dpHeader = new DockPanel();
				Image iHeader = new Image();
				iHeader.Stretch = Stretch.None;
				iHeader.Margin = new Thickness(0, 0, 5, 0);
				DockPanel.SetDock(iHeader, Dock.Left);
				switch (player.PlayerType)
				{
					case DominionBase.Players.PlayerType.Human:
						iHeader.Source = (BitmapImage)this.Resources["imHuman"];
						break;

					case DominionBase.Players.PlayerType.Computer:
						iHeader.Source = (BitmapImage)this.Resources["imComputer"];
						break;
				}
				dpHeader.Children.Add(iHeader);
				TextBlock tbHeader = new TextBlock();
				tbHeader.Text = player.Name;
				dpHeader.Children.Add(tbHeader);
				tiPlayer.Header = dpHeader;

				tcAreas.Items.Add(tiPlayer);
				Controls.ucPlayerDisplay ucpdPlayer = new Controls.ucPlayerDisplay();
				tiPlayer.Content = ucpdPlayer;
				ucpdPlayer.IsUIPlayer = (player.PlayerType == DominionBase.Players.PlayerType.Human);
				ucpdPlayer.Player = player;

				PlayerSettings playerSettings = _Settings.PlayerSettings.FirstOrDefault(ps => ps.Name == player.Name);
				if (playerSettings != null)
				{
					ColorHls hlsValue = HLSColor.RgbToHls(playerSettings.UIColor);
					Color cPlayer = HLSColor.HlsToRgb(hlsValue.H, Math.Min(1d, hlsValue.L * 1.125), hlsValue.S * 0.95, hlsValue.A);
					GradientStopCollection gsc = new GradientStopCollection();
					gsc.Add(new GradientStop(cPlayer, 0));
					gsc.Add(new GradientStop(playerSettings.UIColor, 0.25));
					gsc.Add(new GradientStop(playerSettings.UIColor, 0.75));
					gsc.Add(new GradientStop(cPlayer, 1));
					gsc.Freeze();
					tiPlayer.Background = new LinearGradientBrush(gsc, 0);
					//tiPlayer.Background = new SolidColorBrush(playerSettings.UIColor);
					ucpdPlayer.ColorFocus = playerSettings.UIColor;
				}

				ToolTip tt = new System.Windows.Controls.ToolTip();
				Controls.ucPlayerOverview ucpo = new Controls.ucPlayerOverview();
				ucpo.Player = player;
				tt.Content = ucpo;
				ToolTipService.SetToolTip(dpHeader, tt);
				if (Settings.ToolTipShowDuration == ToolTipShowDuration.Off)
					ToolTipService.SetIsEnabled(dpHeader, false);
				else
				{
					ToolTipService.SetIsEnabled(dpHeader, true);
					ToolTipService.SetShowDuration(dpHeader, (int)Settings.ToolTipShowDuration);
				}
				dpHeader.MouseDown += new MouseButtonEventHandler(tiPlayer_MouseDown);
				dpHeader.MouseUp += new MouseButtonEventHandler(tiPlayer_MouseUp);

				player.Revealed.PileChanged += new DominionBase.Piles.Pile.PileChangedEventHandler(Revealed_PileChanged);
				player.BenefitReceiving += new DominionBase.Players.Player.BenefitReceivingEventHandler(player_BenefitReceiving);
				//player.DiscardPile.PileChanged += new DominionBase.Pile.PileChangedEventHandler(DiscardPile_PileChanged);
				player.CardPlaying += new DominionBase.Players.Player.CardPlayingEventHandler(player_CardPlaying);
				player.CardPlayed += new DominionBase.Players.Player.CardPlayedEventHandler(player_CardPlayed);
				player.CardUndoPlaying += new DominionBase.Players.Player.CardUndoPlayingEventHandler(player_CardUndoPlaying);
				player.CardUndoPlayed += new DominionBase.Players.Player.CardUndoPlayedEventHandler(player_CardUndoPlayed);
				player.CardBuying += new DominionBase.Players.Player.CardBuyingEventHandler(player_CardBuying);
				player.CardBought += new DominionBase.Players.Player.CardBoughtEventHandler(player_CardBought);
				player.CardBuyFinished += new DominionBase.Players.Player.CardBuyFinishedEventHandler(player_CardBuyFinished);
				player.CardGaining += new DominionBase.Players.Player.CardGainingEventHandler(player_CardGaining);
				player.CardGainedInto += new DominionBase.Players.Player.CardGainedIntoEventHandler(player_CardGainedInto);
				player.CardGainFinished += new DominionBase.Players.Player.CardGainFinishedEventHandler(player_CardGainFinished);
				player.TokenPlaying += new DominionBase.Players.Player.TokenPlayingEventHandler(player_TokenPlaying);
				player.TokenPlayed += new DominionBase.Players.Player.TokenPlayedEventHandler(player_TokenPlayed);
				player.Trashing += new DominionBase.Players.Player.TrashingEventHandler(player_Trashing);
				player.TrashedFinished += new DominionBase.Players.Player.TrashedFinishedEventHandler(player_Trashed);
				player.PhaseChanged += new DominionBase.Players.Player.PhaseChangedEventHandler(player_PhaseChangedEvent);
				player.PlayerModeChanged += new DominionBase.Players.Player.PlayerModeChangedEventHandler(player_PlayerModeChangedEvent);
				player.CardsDrawn += new DominionBase.Players.Player.CardsDrawnEventHandler(player_CardsDrawn);
				player.TurnStarting += new DominionBase.Players.Player.TurnStartingEventHandler(player_TurnStarting);
				player.Shuffling += new DominionBase.Players.Player.ShufflingEventHandler(player_Shuffle);
				player.CardsAddedToDeck += new DominionBase.Players.Player.CardsAddedToDeckEventHandler(player_CardsAddedToDeck);
				player.CardsAddedToHand += new DominionBase.Players.Player.CardsAddedToHandEventHandler(player_CardsAddedToHand);
				player.CardsDiscarded += new DominionBase.Players.Player.CardsDiscardedEventHandler(player_CardsDiscarded);
				player.PlayerMats.CardMatsChanged += new DominionBase.Piles.CardMats.CardMatsChangedEventHandler(PlayerMats_DecksChanged);
				player.TokenPiles.TokenCollectionsChanged += new DominionBase.TokenCollections.TokenCollectionsChangedEventHandler(PlayerTokenPiles_TokenCollectionsChanged);
				player.BenefitsChanged += new DominionBase.Players.Player.BenefitsChangedEventHandler(player_BenefitsChanged);

				if (player == _Player)
				{
					tcAreas.SelectedItem = tiPlayer;
					player.CardReceived += new DominionBase.Players.Player.CardReceivedEventHandler(player_CardReceived);
				}
			}

			game.FinalizeSetup();

			LayoutSupplyPiles();

			miNewGame.IsEnabled = false;
			miLoadGame.IsEnabled = false;
			miEndGame.IsEnabled = true;
			miSaveGame.IsEnabled = false;

			gameThread = new Thread(game.StartAsync);
			gameThread.Start();

			UpdateDisplay();
		}

		private void LayoutSupplyPiles()
		{
			stackPanelSupplyPiles.Children.Clear();

			Controls.ucGameLog gameLog = null;
			if (tiGameLog.Visibility == System.Windows.Visibility.Visible)
			{
				gameLog = tiGameLog.Content as Controls.ucGameLog;
				tiGameLog.Content = null;
				tiGameLog.Visibility = System.Windows.Visibility.Collapsed;
			}
			else if (dpGameInfo.Children.OfType<Controls.ucGameLog>().FirstOrDefault() != null)
			{
				gameLog = dpGameInfo.Children.OfType<Controls.ucGameLog>().FirstOrDefault();
				dpGameInfo.Children.Remove(gameLog);
			}
			switch (_Settings.GameLogLocation)
			{
				case GameLogLocation.InCommonArea:
					dpGameInfo.Children.Add(gameLog);
					break;

				case GameLogLocation.InGameTabArea:
					tiGameLog.Visibility = System.Windows.Visibility.Visible;
					tiGameLog.Content = gameLog;
					break;
			}

			if (game == null || game.Table == null)
				return;

			int pilesPerColumn = 0;
			switch (_Settings.LayoutStyle)
			{
				case LayoutStyle.Supply2Columns:
					pilesPerColumn = Math.Max(
						game.Table.Supplies.Count(skv => skv.Value.Location == DominionBase.Cards.Location.Kingdom),
						game.Table.Supplies.Count(skv => skv.Value.Location == DominionBase.Cards.Location.General));
					break;

				case LayoutStyle.Supply4Columns:
					pilesPerColumn = Math.Max(
						(game.Table.Supplies.Count(skv => skv.Value.Location == DominionBase.Cards.Location.Kingdom) + 1) / 2,
						(game.Table.Supplies.Count(skv => skv.Value.Location == DominionBase.Cards.Location.General) + 1) / 2);
					break;
			}

			StackPanel spFirstAction = new StackPanel();
			spFirstAction.FlowDirection = System.Windows.FlowDirection.LeftToRight;
			spFirstAction.HorizontalAlignment = System.Windows.HorizontalAlignment.Stretch;
			spFirstAction.Margin = new Thickness(0, 0, 4, 0);
			stackPanelSupplyPiles.Children.Add(spFirstAction);

			Border borderSupply = new Border();
			borderSupply.BorderThickness = new Thickness(1);
			borderSupply.BorderBrush = Brushes.DarkSlateBlue;
			stackPanelSupplyPiles.Children.Add(borderSupply);

			StackPanel spFirstGeneral = new StackPanel();
			spFirstGeneral.FlowDirection = System.Windows.FlowDirection.LeftToRight;
			spFirstGeneral.HorizontalAlignment = System.Windows.HorizontalAlignment.Stretch;
			spFirstGeneral.Margin = new Thickness(0, 0, 4, 0);
			stackPanelSupplyPiles.Children.Add(spFirstGeneral);

			StackPanel spCurrentAction = spFirstAction;
			StackPanel spCurrentGeneral = spFirstGeneral;
			foreach (Type supplyType in game.Table.SupplyKeysOrdered)
			{
				StackPanel sp = null;
				switch (game.Table.Supplies[supplyType].Location)
				{
					case DominionBase.Cards.Location.General:
						sp = spCurrentGeneral;
						if (_Settings.LayoutStyle == LayoutStyle.Supply4Columns &&
							sp.Children.Count > 0 &&
							sp.Children.OfType<SupplyControl>().Last().Supply.Category == DominionBase.Cards.Category.Curse)
						{
							sp = new StackPanel();
							sp.FlowDirection = System.Windows.FlowDirection.LeftToRight;
							sp.HorizontalAlignment = System.Windows.HorizontalAlignment.Stretch;
							sp.Margin = new Thickness(0, 0, 4, 0);

							borderSupply = new Border();
							borderSupply.BorderThickness = new Thickness(1);
							borderSupply.BorderBrush = Brushes.DarkSlateBlue;

							stackPanelSupplyPiles.Children.Add(borderSupply);
							stackPanelSupplyPiles.Children.Add(sp);
							spCurrentGeneral = sp;
						}

						break;
					case DominionBase.Cards.Location.Kingdom:
						sp = spCurrentAction;
						if (sp.Children.OfType<SupplyControl>().Count() >= pilesPerColumn)
						{
							sp = new StackPanel();
							sp.FlowDirection = System.Windows.FlowDirection.LeftToRight;
							sp.HorizontalAlignment = System.Windows.HorizontalAlignment.Stretch;
							sp.Margin = new Thickness(0, 0, 4, 0);

							borderSupply = new Border();
							borderSupply.BorderThickness = new Thickness(1);
							borderSupply.BorderBrush = Brushes.DarkSlateBlue;

							stackPanelSupplyPiles.Children.Insert(stackPanelSupplyPiles.Children.IndexOf(spFirstGeneral), sp);
							stackPanelSupplyPiles.Children.Insert(stackPanelSupplyPiles.Children.IndexOf(spFirstGeneral), borderSupply);
							spCurrentAction = sp;
						}

						break;
					default:
						continue;
				}

				SupplyControl newSC = new SupplyControl();
				sp.Children.Add(newSC);
				int previousSCIndex = sp.Children.Count - 1;
				while (previousSCIndex >= 0 && !(sp.Children[previousSCIndex--] is SupplyControl)) ;
				newSC.HorizontalAlignment = HorizontalAlignment.Stretch;
				newSC.Width = sp.Width;
				newSC.Supply = game.Table.Supplies[supplyType];
				if (previousSCIndex >= 0)
				{
					if ((game.Table.Supplies[supplyType].Location == DominionBase.Cards.Location.General && ((SupplyControl)sp.Children[previousSCIndex]).Supply.Category != newSC.Supply.Category) ||
						(game.Table.Supplies[supplyType].Location == DominionBase.Cards.Location.Kingdom && ((SupplyControl)sp.Children[previousSCIndex]).Supply.Randomizer.BaseCost != newSC.Supply.Randomizer.BaseCost))
					{
						borderSupply = new Border();
						borderSupply.Margin = new Thickness(15, 0, 15, 0);
						borderSupply.BorderThickness = new Thickness(1);
						borderSupply.BorderBrush = Brushes.LightSkyBlue;
						sp.Children.Insert(sp.Children.Count - 1, borderSupply);
					}
				}
			}

			CheckBuyable(game.ActivePlayer);
			stackPanelSupplyPiles.InvalidateVisual();

			rdGrid0.Height = new GridLength(stackPanelSupplyPiles.ActualHeight + 5);
			rdGrid0.Height = GridLength.Auto;
		}

		void tiPlayer_MouseDown(object sender, MouseButtonEventArgs e)
		{
			if (Settings != null && Settings.ShowToolTipOnRightClick && e.ChangedButton == MouseButton.Right && e.ButtonState == MouseButtonState.Pressed)
			{
				(sender as UIElement).CaptureMouse();
				FrameworkElement element = sender as FrameworkElement;
				ToolTip tt = element.ToolTip as ToolTip;
				Controls.ucPlayerOverview ucpo = (tt.Content as Controls.ucPlayerOverview);
				ucpo.Turn = game.TurnsTaken.LastOrDefault(t => t.Player == ((element.Parent as ContentControl).Content as Controls.ucPlayerDisplay).Player && t != game.CurrentTurn);
				tt.IsOpen = true;
			}
		}

		void tiPlayer_MouseUp(object sender, MouseButtonEventArgs e)
		{
			if (Settings != null && Settings.ShowToolTipOnRightClick && e.ChangedButton == MouseButton.Right && e.ButtonState == MouseButtonState.Released)
			{
				(sender as UIElement).ReleaseMouseCapture();
				ToolTip tt = (sender as FrameworkElement).ToolTip as ToolTip;
				tt.IsOpen = false;
			}
		}

		void game_GameMessage(object sender, DominionBase.GameMessageEventArgs e)
		{
			if (this.Dispatcher.CheckAccess())
			{
				Type cardType = e.SourceCard.CardType;
				if (cardType == DominionBase.Cards.Base.TypeClass.Chancellor ||
					cardType == DominionBase.Cards.Cornucopia.TypeClass.TrustySteed ||
					cardType == DominionBase.Cards.DarkAges.TypeClass.Scavenger)
				{
					glMain.Log(
						e.Player,
						e.Player.PlayerType == DominionBase.Players.PlayerType.Human ? (Object)"You" : (Object)e.Player,
						String.Format(" put{0} deck into discard pile", e.Player.PlayerType == DominionBase.Players.PlayerType.Human ? "" : "s")
						);
				}
				else if (cardType == DominionBase.Cards.Intrigue.TypeClass.Bridge ||
					cardType == DominionBase.Cards.Prosperity.TypeClass.Quarry ||
					cardType == DominionBase.Cards.Cornucopia.TypeClass.Princess ||
					cardType == DominionBase.Cards.Hinterlands.TypeClass.Highway)
				{
					String cardTypes = String.Empty;
					if (cardType == DominionBase.Cards.Prosperity.TypeClass.Quarry)
						cardTypes = "Action ";
					glMain.Log(e.Player, e.SourceCard, " reduces the cost of all ", cardTypes, "cards by ", new DominionBase.Currencies.Coin(e.Count));
				}
				else if (cardType == DominionBase.Cards.Intrigue.TypeClass.Masquerade)
				{
					String postText = String.Format(" to the left ({0})", e.AffectedPlayer);

					if (e.Player.PlayerType == DominionBase.Players.PlayerType.Human)
						glMain.Log(e.Player, "You pass ", e.Card1, postText);
					else
						glMain.Log(e.Player, e.Player, " passes a card", postText);
				}
				else if ((cardType == DominionBase.Cards.Intrigue.TypeClass.WishingWell || 
					cardType == DominionBase.Cards.DarkAges.TypeClass.Mystic ||
					cardType == DominionBase.Cards.DarkAges.TypeClass.Rebuild ||
					cardType == DominionBase.Cards.Guilds.TypeClass.Doctor ||
					cardType == DominionBase.Cards.Guilds.TypeClass.Journeyman) && e.Card1 != null)
				{
					glMain.Log(
						e.Player,
						e.Player.PlayerType == DominionBase.Players.PlayerType.Human ? (Object)"You" : (Object)e.Player,
						String.Format(" name{0} ", e.Player.PlayerType == DominionBase.Players.PlayerType.Human ? "" : "s"), 
						e.Card1);
				}
				else if (cardType == DominionBase.Cards.Seaside.TypeClass.Ambassador)
				{
					glMain.Log(
						e.Player,
						e.Player.PlayerType == DominionBase.Players.PlayerType.Human ? (Object)"You" : (Object)e.Player,
						String.Format(" return{0} {1} to the ",
							e.Player.PlayerType == DominionBase.Players.PlayerType.Human ? "" : "s",
							StringUtility.Plural("card", e.Count)),
						e.Card1,
						" supply pile");
				}
				else if (cardType == DominionBase.Cards.Seaside.TypeClass.Embargo)
				{
					glMain.Log(
						e.Player,
						e.Player == _Player ? (Object)"You" : (Object)e.Player,
						String.Format(" put{0} {1} token on ",
							e.Player == _Player ? "" : "s",
							e.SourceCard.Name),
						e.Card1);
				}
				else if (cardType == DominionBase.Cards.Seaside.TypeClass.Haven)
				{
					if (e.Player == _Player)
						glMain.Log(e.Player, "You set aside ", e.Card1);
					else
						glMain.Log(e.Player, e.Player, " sets aside a card");
				}
				else if (cardType == DominionBase.Cards.Promotional.TypeClass.Prince)
				{
					if (e.Player == _Player)
						glMain.Log(e.Player, "You set aside ", e.Card1, " on ", e.SourceCard);
					else
						glMain.Log(e.Player, e.Player, " sets aside ", e.Card1, " on ", e.SourceCard);
				}
				else if (cardType == DominionBase.Cards.Seaside.TypeClass.Lighthouse)
				{
					glMain.Log(
						e.Player,
						e.Player == _Player ? (Object)"Your" : (Object)e.Player,
						e.Player == _Player ? " " : "'s ",
						e.SourceCard.PhysicalCard,
						" provides immunity to the attack.");
				}
				else if (cardType == DominionBase.Cards.Prosperity.TypeClass.Contraband ||
					cardType == DominionBase.Cards.DarkAges.TypeClass.BandOfMisfits)
				{
					glMain.Log(
						e.Player,
						e.Player == _Player ? (Object)"You" : (Object)e.Player,
						String.Format(" name{0} ", e.Player == _Player ? "" : "s"), 
						e.Card1 != null ? (Object)e.Card1 : (Object)"nothing");
				}
				else if (cardType == DominionBase.Cards.Hinterlands.TypeClass.Trader)
				{
					glMain.Log(
						e.Player,
						e.Player == _Player ? (Object)"You" : (Object)e.Player,
						String.Format(" gain{0} ", e.Player == _Player ? "" : "s"), 
						e.Card2, 
						" instead of ", 
						e.Card1);
				}
				else if (cardType == DominionBase.Cards.DarkAges.TypeClass.Madman ||
					cardType == DominionBase.Cards.DarkAges.TypeClass.Spoils)
				{
					glMain.Log(
						e.Player,
						e.Player == _Player ? (Object)"You" : (Object)e.Player,
						" return ", e.Card1, " to the ", e.Card1, " pile");
				}
				else if ((cardType == DominionBase.Cards.Guilds.TypeClass.Doctor ||
					cardType == DominionBase.Cards.Guilds.TypeClass.Herald ||
					cardType == DominionBase.Cards.Guilds.TypeClass.Masterpiece ||
					cardType == DominionBase.Cards.Guilds.TypeClass.Stonemason) && e.Card1 == null)
				{
					glMain.Log(
						e.Player,
						e.Player == _Player ? (Object)"You" : (Object)e.Player,
						String.Format(" overpay{0} by ", e.Player == _Player ? "" : "s"),
						e.Currency);
				}
				else if (cardType == DominionBase.Cards.Guilds.TypeClass.Butcher)
				{
					glMain.Log(
						e.Player,
						e.Player == _Player ? (Object)"You" : (Object)e.Player,
						String.Format(" spends {0}",
							StringUtility.Plural("Coin token", e.Count)));
				}
			}
			else
			{
				this.Dispatcher.BeginInvoke(new EventHandler<DominionBase.GameMessageEventArgs>(game_GameMessage), System.Windows.Threading.DispatcherPriority.Normal, sender, e);
			}
		}

		internal String DepthPrefix()
		{
			StringBuilder sb = new StringBuilder();
			for (int c = 0; c < _CurrentPlayDepth; c++)
				sb.Append("... ");
			return sb.ToString();
		}

		void PlayerTokenPiles_TokenCollectionsChanged(object sender, DominionBase.TokenCollectionsChangedEventArgs e)
		{
			if (this.Dispatcher.CheckAccess())
			{
				if (e.OperationPerformed == DominionBase.TokenCollectionsChangedEventArgs.Operation.Added)
				{
					if (e.AddedTokens[0].GetType() == DominionBase.Cards.Seaside.TypeClass.PirateShipToken)
					{
						glMain.Log(
							e.Player,
							e.Player == _Player ? (Object)"You" : (Object)e.Player,
							String.Format(" gain{0} a Pirate Ship token", e.Player == _Player ? "" : "s")
							);
					}
					if (e.AddedTokens[0].GetType() == DominionBase.Cards.Guilds.TypeClass.CoinToken)
					{
						glMain.Log(
							e.Player,
							e.Player == _Player ? (Object)"You" : (Object)e.Player,
							String.Format(" gain{0} a Coin token", e.Player == _Player ? "" : "s")
							);
					}
				}
			}
			else
			{
				this.Dispatcher.BeginInvoke(new EventHandler<DominionBase.TokenCollectionsChangedEventArgs>(PlayerTokenPiles_TokenCollectionsChanged), System.Windows.Threading.DispatcherPriority.Normal, sender, e);
			}
		}

		void PlayerMats_DecksChanged(object sender, DominionBase.Piles.CardMatsChangedEventArgs e)
		{
			if (this.Dispatcher.CheckAccess())
			{
				if (e.CardMat.GetType() == DominionBase.Cards.Seaside.TypeClass.IslandMat)
				{
					if (e.OperationPerformed == DominionBase.Piles.CardMatsChangedEventArgs.Operation.Added)
					{
						glMain.Log(
							e.Player,
							e.Player == _Player ? (Object)"You" : (Object)e.Player,
							String.Format(" set{0} aside ", e.Player == _Player ? "" : "s"),
							e.AddedCards.Select(c => c.PhysicalCard),
							" on Island Mat");
					}
				}
				else if (e.CardMat.GetType() == DominionBase.Cards.Seaside.TypeClass.NativeVillageMat)
				{
					if (e.OperationPerformed == DominionBase.Piles.CardMatsChangedEventArgs.Operation.Added)
					{
						if (e.Player == _Player)
							glMain.Log(e.Player, "You put ", e.AddedCards.Select(c => c.PhysicalCard), " on Native Village Mat");
						else
							glMain.Log(
								e.Player,
								e.Player, 
								String.Format(" puts {0} on Native Village Mat", 
									DominionBase.Utilities.StringUtility.Plural("card", e.AddedCards.Count)
								));
					}
					else if (e.OperationPerformed == DominionBase.Piles.CardMatsChangedEventArgs.Operation.Removed && e.Player.Phase != DominionBase.Players.PhaseEnum.Endgame)
					{
						if (e.Player == _Player)
						{
							IEnumerable<DominionBase.Cards.Card> nvTakenCards = e.RemovedCards.Select(c => c.PhysicalCard);
							glMain.Log(e.Player, "You take ", nvTakenCards.Count() == 0 ? (object)"nothing" : (object)nvTakenCards, " from Native Village Mat");
						}
						else
							glMain.Log(
								e.Player,
								e.Player,
								String.Format(" takes {0} from Native Village Mat",
									DominionBase.Utilities.StringUtility.Plural("card", e.RemovedCards.Count)
								));
					}
				}
				else if (e.CardMat.GetType() == DominionBase.Cards.Promotional.TypeClass.PrinceSetAside)
				{
					if (e.OperationPerformed == DominionBase.Piles.CardMatsChangedEventArgs.Operation.Added)
					{
						glMain.Log(
							e.Player,
							e.Player == _Player ? (Object)"You" : (Object)e.Player,
							String.Format(" set{0} aside ", e.Player == _Player ? "" : "s"),
							e.AddedCards.Select(c => c.PhysicalCard));
					}
				}
			}
			else
			{
				this.Dispatcher.BeginInvoke(new EventHandler<DominionBase.Piles.CardMatsChangedEventArgs>(PlayerMats_DecksChanged), System.Windows.Threading.DispatcherPriority.Normal, sender, e);
			}
		}

		void player_CardsAddedToDeck(object sender, DominionBase.Players.CardsAddedToDeckEventArgs e)
		{
			if (this.Dispatcher.CheckAccess())
			{
				String locationMod = String.Empty;
				if (e.DeckPosition == DominionBase.Piles.DeckPosition.Bottom)
					locationMod = "the ";

				if (e.Cards.Count == 0)
					return;
				if (sender == _Player)
					glMain.Log(
						sender as DominionBase.Players.Player,
						"You put ",
						e.Cards.Select(c => c.PhysicalCard),
						String.Format(" on {0}{1} of your deck", locationMod, e.DeckPosition.ToString().ToLower())
						);
				else
					glMain.Log(
						sender as DominionBase.Players.Player,
						sender, 
						String.Format(" puts {0} on {2}{1} of their deck", 
							StringUtility.Plural("card", e.Cards.Count), 
							e.DeckPosition.ToString().ToLower(), 
							locationMod
						));
			}
			else
			{
				this.Dispatcher.BeginInvoke(new EventHandler<DominionBase.Players.CardsAddedToDeckEventArgs>(player_CardsAddedToDeck), System.Windows.Threading.DispatcherPriority.Normal, sender, e);
			}
		}

		void player_CardsAddedToHand(object sender, DominionBase.Players.CardsAddedToHandEventArgs e)
		{
			if (this.Dispatcher.CheckAccess())
			{
				if (e.Cards.Count == 0)
					return;
				if (sender == _Player)
					glMain.Log(
						sender as DominionBase.Players.Player,
						"You put ",
						e.Cards.Select(c => c.PhysicalCard),
						" into your hand");
				else
					glMain.Log(sender as DominionBase.Players.Player, sender, String.Format(" puts {0} into their hand", StringUtility.Plural("card", e.Cards.Count)));
			}
			else
			{
				this.Dispatcher.BeginInvoke(new EventHandler<DominionBase.Players.CardsAddedToHandEventArgs>(player_CardsAddedToHand), System.Windows.Threading.DispatcherPriority.Normal, sender, e);
			}
		}

		void player_CardsDiscarded(object sender, DominionBase.Players.CardsDiscardEventArgs e)
		{
			if (this.Dispatcher.CheckAccess())
			{
				if (e.Cards.Count == 0 || e.HandledBy.Contains(this))
					return;

				e.HandledBy.Add(this);

				String location = String.Empty;
				switch (e.FromLocation)
				{
					case DominionBase.Players.DeckLocation.InPlay:
					case DominionBase.Players.DeckLocation.SetAside:
					case DominionBase.Players.DeckLocation.InPlayAndSetAside:
						return;
					case DominionBase.Players.DeckLocation.Hand:
					case DominionBase.Players.DeckLocation.Deck:
						location = String.Format(" from {0} {1}", sender == _Player ? "your" : "their", e.FromLocation.ToString().ToLower());
						break;
				}

				Object name = (sender == _Player ? (Object)"You" : (Object)sender);
				String verb = String.Format(" discard{0} ", sender == _Player ? "" : "s");

				if (e.Cards.Count == 1)
					glMain.Log(sender as DominionBase.Players.Player, name, verb, e.Cards.Select(c => c.PhysicalCard), location);
				else
					glMain.Log(sender as DominionBase.Players.Player, name, verb, String.Format("{0}{1}", StringUtility.Plural("card", e.Cards.Count), location));
			}
			else
			{
				this.Dispatcher.BeginInvoke(new EventHandler<DominionBase.Players.CardsDiscardEventArgs>(player_CardsDiscarded), System.Windows.Threading.DispatcherPriority.Normal, sender, e);
			}
		}

		void player_Shuffle(object sender, DominionBase.Players.ShuffleEventArgs e)
		{
			if (this.Dispatcher.CheckAccess())
			{
				glMain.Log(
					e.Player,
					"(",
					e.Player == _Player ? (Object)"You" : (Object)e.Player,
					String.Format(" shuffle{0}...)", e.Player == _Player ? "" : "s")
					);
			}
			else
			{
				this.Dispatcher.BeginInvoke(new EventHandler<DominionBase.Players.ShuffleEventArgs>(player_Shuffle), System.Windows.Threading.DispatcherPriority.Normal, sender, e);
			}
		}

		void player_BenefitReceiving(object sender, DominionBase.Players.BenefitReceivingEventArgs e)
		{
			if (this.Dispatcher.CheckAccess())
			{
				StringBuilder sb = new StringBuilder();
				sb.AppendFormat(" get{0}", _Player != null && e.Player.PlayerUniqueId == _Player.UniqueId ? "" : "s");
				if (e.Benefit.Cards > 0)
					sb.AppendFormat(" +{0} card{1}", e.Benefit.Cards, e.Benefit.Cards == 1 ? "" : "s");
				if (e.Benefit.Actions > 0)
					sb.AppendFormat(" +{0}", StringUtility.Plural("action", e.Benefit.Actions));
				else if (e.Benefit.Actions < 0)
					sb.AppendFormat(" {0}", StringUtility.Plural("action", e.Benefit.Actions));
				if (e.Benefit.Buys > 0)
					sb.AppendFormat(" +{0}", StringUtility.Plural("buy", e.Benefit.Buys));
				else if (e.Benefit.Buys < 0)
					sb.AppendFormat(" {0}", StringUtility.Plural("buy", e.Benefit.Buys));
				if (e.Benefit.Currency > new DominionBase.Currency())
					sb.AppendFormat(" +{0}", Utilities.RenderText(e.Benefit.Currency.ToString()));
				else if (e.Benefit.Currency < new DominionBase.Currency())
					sb.AppendFormat(" {0}", Utilities.RenderText(e.Benefit.Currency.ToString()));
				if (e.Benefit.VictoryPoints > 0)
					sb.Append(Utilities.RenderText(String.Format(" +<vp>{0}</vp>", e.Benefit.VictoryPoints)));
				else if (e.Benefit.VictoryPoints < 0)
					sb.Append(Utilities.RenderText(String.Format(" -<vp>{0}</vp>", -e.Benefit.VictoryPoints)));
				sb.Append(e.Benefit.FlavorText);
				if (e.Phase == DominionBase.Players.PhaseEnum.Starting)
				{
					sb.Append(" from ");
					glMain.Log(
						e.Player,
						_Player != null && e.Player.PlayerUniqueId == _Player.UniqueId ? (Object)"You" : (Object)e.Player,
						sb.ToString(),
						((DominionBase.Cards.Card)sender).PhysicalCard);
				}
				else
					glMain.Log(
						e.Player,
						_Player != null && e.Player.PlayerUniqueId == _Player.UniqueId ? (Object)"You" : (Object)e.Player, 
						sb.ToString());
			}
			else
			{
				this.Dispatcher.BeginInvoke(new EventHandler<DominionBase.Players.BenefitReceivingEventArgs>(player_BenefitReceiving), System.Windows.Threading.DispatcherPriority.Normal, sender, e);
			}
		}

		void player_TurnStarting(object sender, DominionBase.Players.TurnStartingEventArgs e)
		{
			if (this.Dispatcher.CheckAccess())
			{
				if (e.Player.PlayerType == DominionBase.Players.PlayerType.Human)
				{
					dpStuff.IsEnabled = true;
					miSaveGame.IsEnabled = true;
                    _Player = e.Player;
				}
				else
				{
					dpStuff.IsEnabled = false;
					miSaveGame.IsEnabled = false;
				}

				// Just in case
				_CurrentPlayDepth = 0;
				if (game.Players[0] == e.Player && e.GrantedBy == null)
					glMain.NewTurn(game.TurnsTaken.TurnNumber(e.Player));
				glMain.NewTurn(e.Player, e.GrantedBy);
			}
			else
			{
				this.Dispatcher.BeginInvoke(new EventHandler<DominionBase.Players.TurnStartingEventArgs>(player_TurnStarting), System.Windows.Threading.DispatcherPriority.Normal, sender, e);
			}
		}

		void player_CardsDrawn(object sender, DominionBase.Players.CardsDrawnEventArgs e)
		{
			if (this.Dispatcher.CheckAccess())
			{
				if (e.Cards.Count > 0)
				{
					String from = String.Empty;
					if (e.FromDeckPosition == DominionBase.Piles.DeckPosition.Bottom)
						from = String.Format(" from the bottom of {0} deck", sender == _Player ? "your" : "their");
					if (sender == _Player)
						glMain.Log(
							sender as DominionBase.Players.Player,
							"You draw ",
							e.Cards.Select(c => c.PhysicalCard),
							from);
					else
						glMain.Log(
							sender as DominionBase.Players.Player, 
							sender, 
							String.Format(" draws {0}", StringUtility.Plural("card", e.Cards.Count)), 
							from);
				}
			}
			else
			{
				this.Dispatcher.BeginInvoke(new EventHandler<DominionBase.Players.CardsDrawnEventArgs>(player_CardsDrawn), System.Windows.Threading.DispatcherPriority.Normal, sender, e);
			}
		}

		void game_GameEndedEvent(object sender, DominionBase.GameEndedEventArgs e)
		{
			if (this.Dispatcher.CheckAccess())
			{
				//_Statistics.Add(game, _Player);
				//_Statistics.Save();

				miNewGame.IsEnabled = true;
				miLoadGame.IsEnabled = true;
				miEndGame.IsEnabled = false;
				miSaveGame.IsEnabled = false;
				miReplay.IsEnabled = true;

				if (_StartingNewGame)
				{
					glMain.TearDown();
					glMain.Clear();
				}

				glMain.NewSection("Game ended");
				foreach (DominionBase.Players.Player player in game.Players)
				{
					String playerType = String.Empty;
					switch (player.PlayerType)
					{
						case DominionBase.Players.PlayerType.Human:
							playerType = "Human";
							break;
						case DominionBase.Players.PlayerType.Computer:
							playerType = (String)player.GetType().GetProperty("AIType", BindingFlags.Instance | BindingFlags.Public | BindingFlags.GetProperty | BindingFlags.FlattenHierarchy).GetValue(player, null);
							break;
					}

					glMain.Log(String.Empty, player, String.Format(" ({0}): ", playerType), Colors.Crimson, player.VictoryPoints, Colors.Transparent, String.Format(" {0} in ", DominionBase.Utilities.StringUtility.Plural("point", player.VictoryPoints, false)), Colors.DodgerBlue, game.TurnsTaken.Count(t => t.Player == player && !t.ModifiedTurn), Colors.Transparent, " turns");
				}

				if (game.Winners.Count > 0)
					glMain.Log(DominionBase.Utilities.StringUtility.Plural("Winner", game.Winners.Count, false), ": ", game.Winners, " with ", game.Winners[0].VictoryPoints, DominionBase.Utilities.StringUtility.Plural(" point", game.Winners[0].VictoryPoints, false));

				miReplay.IsEnabled = (game.State == DominionBase.GameState.Ended || game.State == DominionBase.GameState.Aborted); 
				tbActions.Text = String.Empty;
				tbBuys.Text = String.Empty;
				tbCurrency.Text = String.Empty;
				bPlayTreasures.Text = "Play basic _Treasures";
				bPlayTreasures.IsEnabled = false;
				bPlayCoinTokens.IsEnabled = false;
				bBuyPhase.IsEnabled = false;
				bUndo.IsEnabled = false;
				bTurnDone.IsEnabled = false;

				ReleaseEvents();

				if (_StartingNewGame)
					Game_NewGame_Click(null, null);
			}
			else
			{
				this.Dispatcher.BeginInvoke(new EventHandler<DominionBase.GameEndedEventArgs>(game_GameEndedEvent), System.Windows.Threading.DispatcherPriority.Normal, sender, e);
			}
		}

		void player_BenefitsChanged(object sender, DominionBase.Players.BenefitsChangedEventArgs e)
		{
			if (this.Dispatcher.CheckAccess())
			{
				if (e.Player == game.ActivePlayer)
				{
					tbActions.Inlines.Clear();
					tbActions.Inlines.Add(((TextBlock)Utilities.RenderText(String.Format("{1}{0}{2}", e.Actions, e.Actions > 0 ? "<b>" : "", e.Actions > 0 ? "</b>" : ""), NET_WPF.RenderSize.Tiny, true)[0]).Inlines.ElementAt(0));
					tbBuys.Inlines.Clear();
					tbBuys.Inlines.Add(((TextBlock)Utilities.RenderText(String.Format("{1}{0}{2}", e.Buys, e.Buys > 0 ? "<b>" : "", e.Buys > 0 ? "</b>" : ""), NET_WPF.RenderSize.Tiny, true)[0]).Inlines.ElementAt(0));
					tbCurrency.Inlines.Clear();
					TextBlock tbTemp = (TextBlock)Utilities.RenderText(e.Player.Currency.ToString(), NET_WPF.RenderSize.Tiny, false)[0];
					while (tbTemp.Inlines.Count > 0)
					{
						if (tbTemp.Inlines.ElementAt(0) is InlineUIContainer && ((InlineUIContainer)tbTemp.Inlines.ElementAt(0)).Child is Canvas)
							((Canvas)((InlineUIContainer)tbTemp.Inlines.ElementAt(0)).Child).Margin = new Thickness(2, 0, 2, 0);
						tbCurrency.Inlines.Add(tbTemp.Inlines.ElementAt(0));
					}
				}
			}
			else
			{
				this.Dispatcher.BeginInvoke(new EventHandler<DominionBase.Players.BenefitsChangedEventArgs>(player_BenefitsChanged), System.Windows.Threading.DispatcherPriority.Normal, sender, e);
			}
		}

		void player_PhaseChangedEvent(object sender, DominionBase.Players.PhaseChangedEventArgs e)
		{
			if (this.Dispatcher.CheckAccess())
			{
				if (e.CurrentPlayer == game.ActivePlayer)
				{
					//System.Diagnostics.Trace.WriteLine(String.Format("{2} Phase changed: {0} to {1}", e.OldPhase, e.NewPhase, DateTime.Now.ToString("o")));

					if (e.NewPhase == DominionBase.Players.PhaseEnum.Starting ||
							e.NewPhase == DominionBase.Players.PhaseEnum.Buy ||
							e.CurrentPlayer.PlayerMode == DominionBase.Players.PlayerMode.Waiting)
						CheckBuyable(e.CurrentPlayer);
					else
						ClearBuyable();
				}

				if (e.CurrentPlayer != _Player)
					return;

				if (e.NewPhase == DominionBase.Players.PhaseEnum.Starting || e.NewPhase == DominionBase.Players.PhaseEnum.Endgame)
					miSettings.IsEnabled = miCurrentGame.IsEnabled = true;
				else if (e.CurrentPlayer.PlayerMode == DominionBase.Players.PlayerMode.Waiting)
					miSettings.IsEnabled = miCurrentGame.IsEnabled = false;

				UpdateDisplay();

				if (_Settings.AutoPlayTreasures && (e.NewPhase == DominionBase.Players.PhaseEnum.ActionTreasure || e.NewPhase == DominionBase.Players.PhaseEnum.BuyTreasure))
				{
					// Ugly hack, but it mostly works -- just a slight delay between the end of the PhaseChangedEvent and the AutoPlay
					BackgroundWorker autoplayInvoker = new BackgroundWorker();
					autoplayInvoker.DoWork += delegate
					{
						Thread.Sleep(TimeSpan.FromMilliseconds(50));
						AutoPlayTreasures();
					};
					autoplayInvoker.RunWorkerAsync();
				}
			}
			else
			{
				this.Dispatcher.BeginInvoke(new EventHandler<DominionBase.Players.PhaseChangedEventArgs>(player_PhaseChangedEvent), System.Windows.Threading.DispatcherPriority.Normal, sender, e);
			}
		}

		void player_PlayerModeChangedEvent(object sender, DominionBase.Players.PlayerModeChangedEventArgs e)
		{
			if (this.Dispatcher.CheckAccess())
			{
				if (e.CurrentPlayer == game.ActivePlayer)
				{
					//System.Diagnostics.Trace.WriteLine(String.Format("{2} Phase changed: {0} to {1}", e.OldPhase, e.NewPhase, DateTime.Now.ToString("o")));

					if (e.CurrentPlayer.Phase == DominionBase.Players.PhaseEnum.Starting ||
							e.CurrentPlayer.Phase == DominionBase.Players.PhaseEnum.Buy ||
							e.NewPlayerMode == DominionBase.Players.PlayerMode.Waiting)
						CheckBuyable(e.CurrentPlayer);
					else
						ClearBuyable();
				}

				if (e.CurrentPlayer != _Player)
					return;

				if (e.CurrentPlayer.Phase == DominionBase.Players.PhaseEnum.Starting || e.CurrentPlayer.Phase == DominionBase.Players.PhaseEnum.Endgame)
					miSettings.IsEnabled = miCurrentGame.IsEnabled = true;
				else if (e.CurrentPlayer.PlayerMode == DominionBase.Players.PlayerMode.Waiting)
					miSettings.IsEnabled = miCurrentGame.IsEnabled = false;

				UpdateDisplay();

				if (_Settings.AutoPlayTreasures && e.NewPlayerMode == DominionBase.Players.PlayerMode.Normal && 
					(e.CurrentPlayer.Phase == DominionBase.Players.PhaseEnum.ActionTreasure || e.CurrentPlayer.Phase == DominionBase.Players.PhaseEnum.BuyTreasure))
				{
					// Ugly hack, but it mostly works -- just a slight delay between the end of the PhaseChangedEvent and the AutoPlay
					BackgroundWorker autoplayInvoker = new BackgroundWorker();
					autoplayInvoker.DoWork += delegate
					{
						Thread.Sleep(TimeSpan.FromMilliseconds(50));
						AutoPlayTreasures();
					};
					autoplayInvoker.RunWorkerAsync();
				}
			}
			else
			{
				this.Dispatcher.BeginInvoke(new EventHandler<DominionBase.Players.PlayerModeChangedEventArgs>(player_PlayerModeChangedEvent), System.Windows.Threading.DispatcherPriority.Normal, sender, e);
			}
		}
		private void AutoPlayTreasures()
		{
			WaitCallback wcb = new WaitCallback(UpdateDisplayTarget);
			DominionBase.GamePlayMessage gpm = null;

			// Always play Contraband first
			DominionBase.Cards.CardCollection contrabandTreasures = _Player.Hand[DominionBase.Cards.Prosperity.TypeClass.Contraband];
			foreach (DominionBase.Cards.Card card in contrabandTreasures)
			{
				if (_Player.Phase != DominionBase.Players.PhaseEnum.ActionTreasure && _Player.Phase != DominionBase.Players.PhaseEnum.BuyTreasure)
					break;

				while (game.MessageResponseQueue.Count > 0)
					game.MessageResponseQueue.Dequeue();

				gpm = new DominionBase.GamePlayMessage(wcb, _Player, card);
				gpm.Message = String.Format("{0} playing {1}", _Player, card);
				EnqueueGameMessageAndWait(gpm);
			}

			// Play "normal" Treasure cards next
			DominionBase.Cards.CardCollection tNormal = _Player.Hand[c =>
				(c.Category & DominionBase.Cards.Category.Treasure) == DominionBase.Cards.Category.Treasure &&
				c.CardType != DominionBase.Cards.Prosperity.TypeClass.Bank &&
				c.CardType != DominionBase.Cards.Prosperity.TypeClass.Contraband &&
				c.CardType != DominionBase.Cards.Prosperity.TypeClass.Loan &&
				c.CardType != DominionBase.Cards.Prosperity.TypeClass.Venture &&
				c.CardType != DominionBase.Cards.Cornucopia.TypeClass.HornOfPlenty];
			if (tNormal.Count > 0)
				_Player.PlayCards(tNormal);

			// Only play Loan & Venture after cards like Philosopher's Stone that work better with more cards
			// There are some very specific situations where playing Horn Of Plenty before Philospher's Stone
			// or Venture is the right way to play things, but that's so incredibly rare.
			DominionBase.Cards.CardCollection tLoanVenture = _Player.Hand[DominionBase.Cards.Prosperity.TypeClass.Venture];
			if (_Settings.AutoPlayTreasures_IncludingLoan)
			{
				if (_Settings.AutoPlayTreasures_LoanFirst)
					tLoanVenture.InsertRange(0, _Player.Hand[DominionBase.Cards.Prosperity.TypeClass.Loan]);
				else
					tLoanVenture.AddRange(_Player.Hand[DominionBase.Cards.Prosperity.TypeClass.Loan]);
			}

			foreach (DominionBase.Cards.Card card in tLoanVenture)
			{
				if (_Player.Phase != DominionBase.Players.PhaseEnum.ActionTreasure && _Player.Phase != DominionBase.Players.PhaseEnum.BuyTreasure)
					break;

				gpm = new DominionBase.GamePlayMessage(wcb, _Player, card);
				gpm.Message = String.Format("{0} playing {1}", _Player, card);
				EnqueueGameMessageAndWait(gpm);
				return;
			}

			// Always play Bank & Horn of Plenty last
			DominionBase.Cards.CardCollection tBankHornofPlenty = _Player.Hand[DominionBase.Cards.Prosperity.TypeClass.Bank];
			if (_Settings.AutoPlayTreasures_IncludingHornOfPlenty)
			{
				// If Horn Of Plenty is to be played first, play ALL Horn Of Plenty cards first
				if (_Settings.AutoPlayTreasures_HornOfPlentyFirst)
					tBankHornofPlenty.InsertRange(0, _Player.Hand[DominionBase.Cards.Cornucopia.TypeClass.HornOfPlenty]);
				// Otherwise, play a SINGLE Bank card, then ALL Horn of Plenty cards, then all remaining Bank cards
				else
					tBankHornofPlenty.InsertRange(tBankHornofPlenty.Count == 0 ? 0 : 1, _Player.Hand[DominionBase.Cards.Cornucopia.TypeClass.HornOfPlenty]);
			}
			foreach (DominionBase.Cards.Card card in tBankHornofPlenty)
			{
				if (_Player.Phase != DominionBase.Players.PhaseEnum.ActionTreasure && _Player.Phase != DominionBase.Players.PhaseEnum.BuyTreasure)
					break;

				gpm = new DominionBase.GamePlayMessage(wcb, _Player, card);
				gpm.Message = String.Format("{0} playing {1}", _Player, card);
				EnqueueGameMessageAndWait(gpm);
			}
		}

		private void ClearBuyable()
		{
			foreach (StackPanel sp in stackPanelSupplyPiles.Children.OfType<StackPanel>())
			{
				foreach (SupplyControl sc in sp.Children.OfType<SupplyControl>())
				{
					sc.SupplyClick -= SupplyControl_SupplyClick;
					sc.Clickability = sc.Clickability;
				}
			}
		}

		private void CheckBuyable(DominionBase.Players.Player player)
		{
			Boolean buyablePhase = player != null &&
				(player.Phase == DominionBase.Players.PhaseEnum.Action ||
				player.Phase == DominionBase.Players.PhaseEnum.ActionTreasure ||
				player.Phase == DominionBase.Players.PhaseEnum.BuyTreasure ||
				player.Phase == DominionBase.Players.PhaseEnum.Buy);
			foreach (StackPanel sp in stackPanelSupplyPiles.Children.OfType<StackPanel>())
			{
				foreach (SupplyControl sc in sp.Children.OfType<SupplyControl>())
				{
					sc.SupplyClick -= SupplyControl_SupplyClick;

					if (_Player != player)
						sc.Clickability = SupplyVisibility.Plain;
					else if (buyablePhase && _Player == player && sc.Supply.CanBuy(player))
					{
						sc.Clickability = SupplyVisibility.Gainable;
						// Only attach the SupplyClick event if we're actually buying a card.
						// Otherwise, we'll double-trigger on certain events (like Border Village's Gain ability)
						if (player.PlayerMode == DominionBase.Players.PlayerMode.Normal)
							sc.SupplyClick += SupplyControl_SupplyClick;
					}
					else
						sc.Clickability = SupplyVisibility.NotClickable;
				}
			}
		}

		void player_Trashing(object sender, DominionBase.Players.TrashEventArgs e)
		{
			if (this.Dispatcher.CheckAccess())
			{
				e.TrashedCards.Sort();
				glMain.Log(
					sender as DominionBase.Players.Player,
					sender == _Player ? (Object)"You" : (Object)sender,
					String.Format(" trash{0} ", sender == _Player ? "" : "es"),
					e.TrashedCards.Select(c => c.PhysicalCard));
				glMain.Push();
				_CurrentPlayDepth++;
			}
			else
			{
				this.Dispatcher.BeginInvoke(new EventHandler<DominionBase.Players.TrashEventArgs>(player_Trashing), System.Windows.Threading.DispatcherPriority.Normal, sender, e);
			}
		}

		void player_Trashed(object sender, DominionBase.Players.TrashEventArgs e)
		{
			if (this.Dispatcher.CheckAccess())
			{
				glMain.Pop();
				_CurrentPlayDepth--;
			}
			else
			{
				this.Dispatcher.BeginInvoke(new EventHandler<DominionBase.Players.TrashEventArgs>(player_Trashed), System.Windows.Threading.DispatcherPriority.Normal, sender, e);
			}
		}

		void player_CardReceived(object sender, DominionBase.Players.CardReceivedEventArgs e)
		{
			if (this.Dispatcher.CheckAccess())
			{
				DominionBase.Players.Player player = sender as DominionBase.Players.Player;

				StringBuilder extra = new StringBuilder();

				switch (e.Location)
				{
					case DominionBase.Players.DeckLocation.Deck:
						String locationMod = String.Empty;
						DominionBase.Piles.DeckPosition dp = player.ResolveDeckPosition(e.Location, e.Position);
						if (dp == DominionBase.Piles.DeckPosition.Bottom)
							locationMod = "the ";
						extra.AppendFormat(", putting it on {1}{0} of your {2}", dp.ToString().ToLower(), locationMod, e.Location.ToString().ToLower());
						break;
					case DominionBase.Players.DeckLocation.Hand:
					case DominionBase.Players.DeckLocation.InPlay:
					case DominionBase.Players.DeckLocation.SetAside:
						extra.AppendFormat(", putting it into your {0}", e.Location.ToString().ToLower());
						break;
				}
				glMain.Log(
					player,
					player == _Player ? (Object)"You" : (Object)player,
					String.Format(" receive{0} ", player == _Player ? "" : "s"),
					e.Card.PhysicalCard,
					" from ",
					e.FromPlayer,
					extra);

				UpdateDisplay();
			}
			else
			{
				this.Dispatcher.BeginInvoke(new EventHandler<DominionBase.Players.CardReceivedEventArgs>(player_CardReceived), System.Windows.Threading.DispatcherPriority.Normal, sender, e);
			}
		}

		void player_CardGaining(object sender, DominionBase.Players.CardGainEventArgs e)
		{
			if (this.Dispatcher.CheckAccess())
			{
				if (!e.Bought)
				{
					glMain.Log(
						sender as DominionBase.Players.Player,
						sender == _Player ? (Object)"You" : (Object)sender,
						String.Format(" gain{0} ", sender == _Player ? "" : "s"), 
						e.Card.PhysicalCard);
				}
				glMain.Push();
				_CurrentPlayDepth++;
			}
			else
			{
				this.Dispatcher.BeginInvoke(new EventHandler<DominionBase.Players.CardGainEventArgs>(player_CardGaining), System.Windows.Threading.DispatcherPriority.Normal, sender, e);
			}
		}

		void player_CardGainedInto(object sender, DominionBase.Players.CardGainEventArgs e)
		{
			if (this.Dispatcher.CheckAccess())
			{
				// the CardBought event will have already handled this, so we can just skip printing any message
				if (e.Location == DominionBase.Players.DeckLocation.Discard)
					return;

				StringBuilder extra = new StringBuilder();
				String pronoun = "their";
				if (sender as DominionBase.Players.Player == _Player)
					pronoun = "your";

				switch (e.Location)
				{
					case DominionBase.Players.DeckLocation.Deck:
						String locationMod = String.Empty;
						if ((sender as DominionBase.Players.Player).ResolveDeckPosition(e.Location, e.Position) == DominionBase.Piles.DeckPosition.Bottom)
							locationMod = "the bottom of ";
						extra.AppendFormat(" on {0}{2} {1}", locationMod, e.Location.ToString().ToLower(), pronoun);
						break;
					case DominionBase.Players.DeckLocation.Hand:
					case DominionBase.Players.DeckLocation.InPlay:
					case DominionBase.Players.DeckLocation.SetAside:
						extra.AppendFormat(" into {1} {0}", e.Location.ToString().ToLower(), pronoun);
						break;
				}

				glMain.Log(
					sender as DominionBase.Players.Player,
					sender == _Player ? (Object)"You" : (Object)sender,
					String.Format(" put{0} ", sender == _Player ? "" : "s"), 
					e.Card.PhysicalCard, 
					extra.ToString());

				UpdateDisplay();

			}
			else
			{
				this.Dispatcher.BeginInvoke(new EventHandler<DominionBase.Players.CardGainEventArgs>(player_CardGainedInto), System.Windows.Threading.DispatcherPriority.Normal, sender, e);
			}
		}

		void player_CardGainFinished(object sender, DominionBase.Players.CardGainEventArgs e)
		{
			if (this.Dispatcher.CheckAccess())
			{
				glMain.Pop();
				_CurrentPlayDepth--;
			}
			else
			{
				this.Dispatcher.BeginInvoke(new EventHandler<DominionBase.Players.CardGainEventArgs>(player_CardGainFinished), System.Windows.Threading.DispatcherPriority.Normal, sender, e);
			}
		}

		void player_CardBuying(object sender, DominionBase.Players.CardBuyEventArgs e)
		{
			if (this.Dispatcher.CheckAccess())
			{
				glMain.Log(
					sender as DominionBase.Players.Player,
					sender == _Player ? (Object)"You" : (Object)sender,
					String.Format(" buy{0} ", sender == _Player ? "" : "s"), 
					e.Card.PhysicalCard);
				glMain.Push();
				_CurrentPlayDepth++;
			}
			else
			{
				this.Dispatcher.BeginInvoke(new EventHandler<DominionBase.Players.CardBuyEventArgs>(player_CardBuying), System.Windows.Threading.DispatcherPriority.Normal, sender, e);
			}
		}

		void player_CardBought(object sender, DominionBase.Players.CardBuyEventArgs e)
		{
			if (this.Dispatcher.CheckAccess())
			{
			}
			else
			{
				this.Dispatcher.BeginInvoke(new EventHandler<DominionBase.Players.CardBuyEventArgs>(player_CardBought), System.Windows.Threading.DispatcherPriority.Normal, sender, e);
			}
		}

		void player_CardBuyFinished(object sender, DominionBase.Players.CardBuyEventArgs e)
		{
			if (this.Dispatcher.CheckAccess())
			{
				glMain.Pop();
				_CurrentPlayDepth--;
				if (sender == _Player)
					CheckBuyable((DominionBase.Players.Player)sender);
			}
			else
			{
				this.Dispatcher.BeginInvoke(new EventHandler<DominionBase.Players.CardBuyEventArgs>(player_CardBuyFinished), System.Windows.Threading.DispatcherPriority.Normal, sender, e);
			}
		}

		void player_CardPlaying(object sender, DominionBase.Players.CardPlayingEventArgs e)
		{
			if (this.Dispatcher.CheckAccess())
			{
				glMain.Log(
					e.Player,
					e.Player == _Player ? (Object)"You" : (Object)e.Player,
					String.Format(" play{0} ", e.Player == _Player ? "" : "s"),
					e.Cards == null ? (Object)"nothing" : (Object)e.Cards.Select(c => c.PhysicalCard),
					String.Format(" {0}", e.Modifier));
				glMain.Push();
				_CurrentPlayDepth++;
			}
			else
			{
				this.Dispatcher.BeginInvoke(new EventHandler<DominionBase.Players.CardPlayingEventArgs>(player_CardPlaying), System.Windows.Threading.DispatcherPriority.Normal, sender, e);
			}
		}

		void player_CardPlayed(object sender, DominionBase.Players.CardPlayedEventArgs e)
		{
			if (this.Dispatcher.CheckAccess())
			{
				miSaveGame.IsEnabled = false;

				glMain.Pop();
				_CurrentPlayDepth--;
			}
			else
			{
				this.Dispatcher.BeginInvoke(new EventHandler<DominionBase.Players.CardPlayedEventArgs>(player_CardPlayed), System.Windows.Threading.DispatcherPriority.Normal, sender, e);
			}
		}

		void player_CardUndoPlaying(object sender, DominionBase.Players.CardUndoPlayingEventArgs e)
		{
			if (this.Dispatcher.CheckAccess())
			{
				glMain.Log(
					e.Player, 
					e.Player == _Player ? (Object)"You" : (Object)e.Player,
					String.Format(" undo{0} playing ", e.Player == _Player ? "" : "es"),
					e.Cards == null ? (Object)"nothing" : (Object)e.Cards.Select(c => c.PhysicalCard), 
					String.Format(" {0}", e.Modifier));
				glMain.Push();
				_CurrentPlayDepth++;
			}
			else
			{
				this.Dispatcher.BeginInvoke(new EventHandler<DominionBase.Players.CardUndoPlayingEventArgs>(player_CardUndoPlaying), System.Windows.Threading.DispatcherPriority.Normal, sender, e);
			}
		}

		void player_CardUndoPlayed(object sender, DominionBase.Players.CardUndoPlayedEventArgs e)
		{
			if (this.Dispatcher.CheckAccess())
			{
				glMain.Pop();
				_CurrentPlayDepth--;
			}
			else
			{
				this.Dispatcher.BeginInvoke(new EventHandler<DominionBase.Players.CardUndoPlayedEventArgs>(player_CardUndoPlayed), System.Windows.Threading.DispatcherPriority.Normal, sender, e);
			}
		}

		void player_TokenPlaying(object sender, DominionBase.Players.TokenPlayingEventArgs e)
		{
			if (this.Dispatcher.CheckAccess())
			{
				glMain.Log(
					e.Player,
					e.Player == _Player ? (Object)"You" : (Object)e.Player,
					String.Format(" play{0} ", e.Player == _Player ? "" : "s"),
					e.Tokens == null ? (Object)"nothing" : (Object)e.Tokens);
				glMain.Push();
				_CurrentPlayDepth++;
			}
			else
			{
				this.Dispatcher.BeginInvoke(new EventHandler<DominionBase.Players.TokenPlayingEventArgs>(player_TokenPlaying), System.Windows.Threading.DispatcherPriority.Normal, sender, e);
			}
		}

		void player_TokenPlayed(object sender, DominionBase.Players.TokenPlayedEventArgs e)
		{
			if (this.Dispatcher.CheckAccess())
			{
				miSaveGame.IsEnabled = false;

				glMain.Pop();
				_CurrentPlayDepth--;
			}
			else
			{
				this.Dispatcher.BeginInvoke(new EventHandler<DominionBase.Players.TokenPlayedEventArgs>(player_TokenPlayed), System.Windows.Threading.DispatcherPriority.Normal, sender, e);
			}
		}

		void Revealed_PileChanged(object sender, DominionBase.Piles.PileChangedEventArgs e)
		{
			if (e.OperationPerformed == DominionBase.Piles.PileChangedEventArgs.Operation.Added)
			{
				if (this.Dispatcher.CheckAccess())
				{
					glMain.Log(
						e.Player,
						_Player != null && e.Player.PlayerUniqueId == _Player.UniqueId ? (Object)"You" : (Object)e.Player,
						String.Format(" reveal{0}: ", _Player != null && e.Player.PlayerUniqueId == _Player.UniqueId ? "" : "s"),
						e.AddedCards.Select(c => c.PhysicalCard));
				}
				else
				{
					this.Dispatcher.BeginInvoke(new EventHandler<DominionBase.Piles.PileChangedEventArgs>(Revealed_PileChanged), System.Windows.Threading.DispatcherPriority.Normal, sender, e);
				}
			}
		}

		void DiscardPile_PileChanged(object sender, DominionBase.Piles.PileChangedEventArgs e)
		{
			if (e.OperationPerformed == DominionBase.Piles.PileChangedEventArgs.Operation.Added)
			{
				if (this.Dispatcher.CheckAccess())
				{
					glMain.Log(
						e.Player,
						_Player != null && e.Player.PlayerUniqueId == _Player.UniqueId ? (Object)"You" : (Object)e.Player,
						String.Format(" discard{0}: {1}", _Player != null && e.Player.PlayerUniqueId == _Player.UniqueId ? "" : "s", StringUtility.Plural("card", e.AddedCards.Count))
						);
				}
				else
				{
					this.Dispatcher.BeginInvoke(new EventHandler<DominionBase.Piles.PileChangedEventArgs>(DiscardPile_PileChanged), System.Windows.Threading.DispatcherPriority.Normal, sender, e);
				}
			}
		}

		void Trash_PileChanged(object sender, DominionBase.Piles.PileChangedEventArgs e)
		{
			if (cardTrash.Dispatcher.CheckAccess())
			{
				cardTrash.ExactCount = true;
				cardTrash.IsCardsVisible = true;
				cardTrash.Phase = DominionBase.Players.PhaseEnum.Action;
				cardTrash.PlayerMode = DominionBase.Players.PlayerMode.Waiting;
				cardTrash.CardSize = CardSize.Text;
				cardTrash.Pile = game.Table.Trash;
			}
			else
			{
				cardTrash.Dispatcher.BeginInvoke(new EventHandler<DominionBase.Piles.PileChangedEventArgs>(Trash_PileChanged), System.Windows.Threading.DispatcherPriority.Normal, sender, e);
			}
		}

		void TokenPiles_TokenCollectionsChanged(object sender, DominionBase.TokenCollectionsChangedEventArgs e)
		{
			if (_TradeRouteLabel == null)
				return;

			if (_TradeRouteLabel.Dispatcher.CheckAccess())
			{
				_TradeRouteLabel.Content = e.Count.ToString();
			}
			else
			{
				_TradeRouteLabel.Dispatcher.BeginInvoke(new EventHandler<DominionBase.TokenCollectionsChangedEventArgs>(TokenPiles_TokenCollectionsChanged), System.Windows.Threading.DispatcherPriority.Normal, sender, e);
			}
		}

		private void player_Choose(DominionBase.Players.Player player, DominionBase.Choice choice)
		{
			if (this.Dispatcher.CheckAccess())
			{
				UpdateDisplay();

				uccChooser.Player = player;
				uccChooser.Choice = choice;

				uccChooser.Visibility = System.Windows.Visibility.Visible;

				List<SupplyControl> supplyControls = new List<SupplyControl>();
				foreach (StackPanel sp in stackPanelSupplyPiles.Children.OfType<StackPanel>())
					supplyControls.AddRange(sp.Children.OfType<SupplyControl>());
				uccChooser.SupplyControls = supplyControls;

				uccChooser.Target = "PlayerChoiceMessage";
				uccChooser.IsReady = true;
			}
			else
			{
				this.Dispatcher.BeginInvoke(new player_ChooseDelegate(player_Choose), System.Windows.Threading.DispatcherPriority.Normal, player, choice);
			}
		}

		private void uccChooser_ChooserOKClick(object sender, RoutedEventArgs e)
		{
			uccChooser.Visibility = System.Windows.Visibility.Collapsed;

			Controls.ucChooser chooser = sender as Controls.ucChooser;

			switch (chooser.Target)
			{
				case "PlayerChoiceMessage":
					DominionBase.Players.PlayerChoiceMessage pcm = new DominionBase.Players.PlayerChoiceMessage(WaitEvent, chooser.Player, chooser.ChoiceResult);
					pcm.Message = String.Format("{0} chooses", chooser.Player);
					lock (chooser.Player.MessageRequestQueue)
						chooser.Player.MessageRequestQueue.Enqueue(pcm);
					chooser.Player.WaitEvent.Set();

					while (WaitEvent.WaitOne(250)) ;
					lock (chooser.Player.MessageResponseQueue)
						if (chooser.Player.MessageResponseQueue.Count > 0)
							chooser.Player.MessageResponseQueue.Dequeue();

					break;

				case "GamePlayTokensMessage":
					WaitCallback wcb = new WaitCallback(UpdateDisplayTarget);
					DominionBase.GamePlayTokensMessage gptm = new DominionBase.GamePlayTokensMessage(game.ActivePlayer, DominionBase.Cards.Guilds.TypeClass.CoinToken, int.Parse(chooser.ChoiceResult.Options[0]));
					gptm.Message = String.Format("{0} playing Tokens", game.ActivePlayer);
					EnqueueGameMessageAndWait(gptm);

					break;
			}

			UpdateDisplay();
			CheckBuyable(chooser.Player);
		}

		private void UpdateDisplayTarget(object target)
		{
			if (this.Dispatcher.CheckAccess())
			{
				UpdateDisplay();
			}
			else
			{
				WaitCallback wcb = new WaitCallback(UpdateDisplayTarget);
				this.Dispatcher.BeginInvoke(wcb, System.Windows.Threading.DispatcherPriority.Normal, target);
			}
		}

		private void UpdateDisplay()
		{
			miReplay.IsEnabled = (game.State == DominionBase.GameState.Ended || game.State == DominionBase.GameState.Aborted);
			UpdateDisplayPlayer(game.ActivePlayer);
		}

		private void UpdateDisplayPlayer(DominionBase.Players.Player player)
		{
			bPlayTreasures.IsEnabled = false;
			bPlayCoinTokens.IsEnabled = false;
			bBuyPhase.IsEnabled = false;

			bTurnDone.IsEnabled = false;
			bUndo.IsEnabled = false;
			bPlayTreasures.Text = "Play basic _Treasures";
			if (player == null)
			{
				return;
			}

			if (player.PlayerType == DominionBase.Players.PlayerType.Human)
			{
				DominionBase.Cards.CardCollection treasures;
				DominionBase.Currency totalCurrency;
				String currency = String.Empty;
				switch (player.Phase)
				{
					case DominionBase.Players.PhaseEnum.Action:
						if (player.PlayerMode == DominionBase.Players.PlayerMode.Normal)
						{
							treasures = player.Hand[c =>
								(c.Category & DominionBase.Cards.Category.Treasure) == DominionBase.Cards.Category.Treasure &&
								(c.Location == DominionBase.Cards.Location.General ||
									c.CardType.GetMethod("Play", new Type[] { typeof(DominionBase.Players.Player) }).DeclaringType == typeof(DominionBase.Cards.Card))];

							bPlayTreasures.IsEnabled = treasures.Count > 0;
							totalCurrency = new DominionBase.Currency();
							foreach (DominionBase.Currency c in treasures.Select(t => t.Benefit.Currency))
								totalCurrency += c;
							currency = totalCurrency.ToStringInline();

							bPlayCoinTokens.IsEnabled = player.TokenPiles.ContainsKey(DominionBase.Cards.Guilds.TypeClass.CoinToken) && 
								player.TokenPiles[DominionBase.Cards.Guilds.TypeClass.CoinToken].Count > 0 &&
								player.CurrentTurn.CardsBought.Count == 0;
							bBuyPhase.IsEnabled = true;
							bTurnDone.IsEnabled = true;
						}
						break;

					case DominionBase.Players.PhaseEnum.ActionTreasure:
					case DominionBase.Players.PhaseEnum.BuyTreasure:
						if (player.PlayerMode == DominionBase.Players.PlayerMode.Normal)
						{
							treasures = player.Hand[c =>
								(c.Category & DominionBase.Cards.Category.Treasure) == DominionBase.Cards.Category.Treasure &&
								(c.Location == DominionBase.Cards.Location.General ||
									c.CardType.GetMethod("Play", new Type[] { typeof(DominionBase.Players.Player) }).DeclaringType == typeof(DominionBase.Cards.Card))];

							bPlayTreasures.IsEnabled = treasures.Count > 0;
							totalCurrency = new DominionBase.Currency();
							foreach (DominionBase.Currency c in treasures.Select(t => t.Benefit.Currency))
								totalCurrency += c;
							currency = totalCurrency.ToStringInline();

							bPlayCoinTokens.IsEnabled = player.TokenPiles.ContainsKey(DominionBase.Cards.Guilds.TypeClass.CoinToken) && 
								player.TokenPiles[DominionBase.Cards.Guilds.TypeClass.CoinToken].Count > 0 &&
								player.CurrentTurn.CardsBought.Count == 0;
							bBuyPhase.IsEnabled = true;
							bTurnDone.IsEnabled = true;
						}
						break;

					case DominionBase.Players.PhaseEnum.Buy:
						if (player.PlayerMode == DominionBase.Players.PlayerMode.Normal)
						{
							bTurnDone.IsEnabled = true;
						}
						break;

					case DominionBase.Players.PhaseEnum.Cleanup:
					case DominionBase.Players.PhaseEnum.Endgame:
						break;
				}

				bUndo.IsEnabled = player.CurrentTurn.CardsPlayed.Count > 0 && player.CurrentTurn.CardsPlayed[player.CurrentTurn.CardsPlayed.Count - 1].CanUndo && player.CurrentTurn.CardsBought.Count == 0;

				if (bPlayTreasures.IsEnabled)
					bPlayTreasures.Text = String.Format("Play basic _Treasures for: {0}", currency);
			}
		}

		private void bPlayTreasures_Click(object sender, RoutedEventArgs e)
		{
			if (_Settings.PromptUnplayedActions &&
				game.ActivePlayer.Phase == DominionBase.Players.PhaseEnum.Action &&
				game.ActivePlayer.Hand[DominionBase.Cards.Category.Action].Count > 0 &&
				game.ActivePlayer.Actions > 0)
			{
				if (wMessageBox.Show("You have unplayed actions left.  Are you sure you want to do this?", "Please confirm", MessageBoxButton.YesNo) != MessageBoxResult.Yes)
					return;
			}

			(sender as Control).IsEnabled = false;
			WaitCallback wcb = new WaitCallback(UpdateDisplayTarget);
			DominionBase.GamePlayTreasuresMessage gptm = new DominionBase.GamePlayTreasuresMessage(wcb, game.ActivePlayer);
			gptm.Message = String.Format("{0} playing treasures", game.ActivePlayer);
			EnqueueGameMessageAndWait(gptm);
		}

		private void bPlayCoinTokens_Click(object sender, RoutedEventArgs e)
		{
			if (_Settings.PromptUnplayedActions &&
				game.ActivePlayer.Phase == DominionBase.Players.PhaseEnum.Action &&
				game.ActivePlayer.Hand[DominionBase.Cards.Category.Action].Count > 0 &&
				game.ActivePlayer.Actions > 0)
			{
				if (wMessageBox.Show("You have unplayed actions left.  Are you sure you want to do this?", "Please confirm", MessageBoxButton.YesNo) != MessageBoxResult.Yes)
					return;
			}

			UpdateDisplay();

			uccChooser.Player = game.ActivePlayer;

			List<String> options = new List<string>();
			for (int i = 0; i <= game.ActivePlayer.TokenPiles[DominionBase.Cards.Guilds.TypeClass.CoinToken].Count; i++)
				options.Add(i.ToString());
			DominionBase.Choice choice = new DominionBase.Choice("How many Coin tokens do you want to spend?", null, null, options, game.ActivePlayer);
			uccChooser.Choice = choice;

			uccChooser.Visibility = System.Windows.Visibility.Visible;

			uccChooser.Target = "GamePlayTokensMessage";
			uccChooser.IsReady = true;
		}

		private void bUndo_Click(object sender, RoutedEventArgs e)
		{
			(sender as Control).IsEnabled = false;
			WaitCallback wcb = new WaitCallback(UpdateDisplayTarget);
			DominionBase.GameUndoPlayMessage gupm = null;
			if (game.ActivePlayer.Phase == DominionBase.Players.PhaseEnum.Buy && game.ActivePlayer.CurrentTurn.CardsBought.Count == 0 && game.ActivePlayer.Hand.Count(c => (c.Category & DominionBase.Cards.Category.Treasure) == DominionBase.Cards.Category.Treasure) > 0)
				gupm = new DominionBase.GameUndoPlayMessage(wcb, game.ActivePlayer, DominionBase.Players.PhaseEnum.BuyTreasure);
			else
				gupm = new DominionBase.GameUndoPlayMessage(wcb, game.ActivePlayer, game.ActivePlayer.CurrentTurn.CardsPlayed[game.ActivePlayer.CurrentTurn.CardsPlayed.Count - 1]);
			gupm.Message = String.Format("{0} undoing", game.ActivePlayer);
			EnqueueGameMessageAndWait(gupm);
		}

		private void SupplyControl_SupplyClick(object sender, RoutedEventArgs e)
		{

			DominionBase.Piles.Supply supply = (e.Source as SupplyControl).Supply;

			WaitCallback wcb = new WaitCallback(Bought);
			DominionBase.GameBuyMessage gbm = new DominionBase.GameBuyMessage(wcb, game.ActivePlayer, supply);
			gbm.Message = String.Format("{0} buying {1}", game.ActivePlayer, supply);
			EnqueueGameMessageAndWait(gbm);
		}

		private void Bought(object target)
		{
			if (this.Dispatcher.CheckAccess())
			{
				if (game.ActivePlayer == null)
					return;

				if (game.ActivePlayer.Buys == 0)
					bTurnDone_Click(bTurnDone, null);

				// If the only cards we can buy are Copper, Curse, & Ruins and there are no Goons or Merchant Guild cards 
				// in play and the proper setting is enabled, it will automatically skip the remaining buys
				else if (_Settings.NeverBuyCopperOrCurseExceptWhenGoonsIsInPlay && 
					game.ActivePlayer.InPlay[DominionBase.Cards.Prosperity.TypeClass.Goons].Count == 0 &&
					game.ActivePlayer.InPlay[DominionBase.Cards.Guilds.TypeClass.MerchantGuild].Count == 0 &&
					game.Table.Supplies.Values.Count(supply => 
						supply.SupplyCardType != DominionBase.Cards.Universal.TypeClass.Copper && 
						supply.SupplyCardType != DominionBase.Cards.Universal.TypeClass.Curse &&
						supply.SupplyCardType != DominionBase.Cards.DarkAges.TypeClass.RuinsSupply && 
						supply.CanBuy(game.ActivePlayer)) == 0)
				{
					bTurnDone_Click(bTurnDone, null);
				}

				UpdateDisplay();
			}
			else
			{
				WaitCallback wcb = new WaitCallback(Bought);
				this.Dispatcher.BeginInvoke(wcb, System.Windows.Threading.DispatcherPriority.Normal, target);
			}
		}

		private void CardCollectionControl_CardCollectionControlClick(object sender, RoutedEventArgs e)
		{
			Controls.CardStackControl csc = e.OriginalSource as Controls.CardStackControl;
			if (game == null || game.ActivePlayer == null || csc == null)
				return;

			if ((game.ActivePlayer.Phase == DominionBase.Players.PhaseEnum.Action &&
				((csc.ClickedCard.Category & DominionBase.Cards.Category.Action) == DominionBase.Cards.Category.Action ||
				(csc.ClickedCard.Category & DominionBase.Cards.Category.Treasure) == DominionBase.Cards.Category.Treasure)) ||
				((game.ActivePlayer.Phase == DominionBase.Players.PhaseEnum.ActionTreasure || game.ActivePlayer.Phase == DominionBase.Players.PhaseEnum.BuyTreasure) &&
				(csc.ClickedCard.Category & DominionBase.Cards.Category.Treasure) == DominionBase.Cards.Category.Treasure))
			{
				if (_Settings.PromptUnplayedActions &&
					game.ActivePlayer.Phase == DominionBase.Players.PhaseEnum.Action &&
					game.ActivePlayer.Hand[DominionBase.Cards.Category.Action].Count > 0 &&
					game.ActivePlayer.Actions > 0 &&
					((csc.ClickedCard.Category & DominionBase.Cards.Category.Action) != DominionBase.Cards.Category.Action))
				{
					if (wMessageBox.Show("You have unplayed actions left.  Are you sure you want to do this?", "Please confirm", MessageBoxButton.YesNo) != MessageBoxResult.Yes)
						return;
				}

				WaitCallback wcb = new WaitCallback(UpdateDisplayTarget);
				DominionBase.GamePlayMessage gpm = new DominionBase.GamePlayMessage(wcb, game.ActivePlayer, csc.ClickedCard);
				gpm.Message = String.Format("{0} playing {1}", game.ActivePlayer, csc.ClickedCard);
				EnqueueGameMessageAndWait(gpm);
			}
		}

		private void bTurnDone_Click(object sender, RoutedEventArgs e)
		{
			(sender as Control).IsEnabled = false;
			if (_Settings.PromptUnspentBuysTreasure &&
				game.ActivePlayer.Buys > 0 &&
				(!_Settings.PromptUnspentBuysTreasure_OnlyNotCopperCurseRuins || game.Table.Supplies.Values.Count(supply => 
						supply.SupplyCardType != DominionBase.Cards.Universal.TypeClass.Copper && 
						supply.SupplyCardType != DominionBase.Cards.Universal.TypeClass.Curse &&
						supply.SupplyCardType != DominionBase.Cards.DarkAges.TypeClass.RuinsSupply && 
						supply.CanBuy(game.ActivePlayer)) > 0))
			{
				if (wMessageBox.Show("You have unused coins and buys left.  Are you sure you want to do this?", "Please confirm", MessageBoxButton.YesNo) != MessageBoxResult.Yes)
				{
					(sender as Control).IsEnabled = true;
					return;
				}
			}

            WaitCallback wcb = new WaitCallback(UpdateDisplayTarget);
			DominionBase.GameEndTurnMessage getm = new DominionBase.GameEndTurnMessage(wcb, game.ActivePlayer);
			getm.Message = String.Format("{0} ending turn", game.ActivePlayer);

            EnqueueGameMessageAndWait(getm);
         }

		private void Game_Exit_Click(object sender, RoutedEventArgs e)
		{
			this.Close();
		}

		private void Game_Settings_Click(object sender, RoutedEventArgs e)
		{
			wSettings settingsDialogBox = new wSettings(ref _Settings);
			settingsDialogBox.Owner = this;
			if (settingsDialogBox.ShowDialog() == true)
			{
				wMain.Settings.Save();
				this.DataContext = null;
				this.DataContext = wMain.Settings;

				LayoutSupplyPiles();
			}
		}

		private void CurrentGame_Save_Click(object sender, RoutedEventArgs e)
		{
			Microsoft.Win32.SaveFileDialog sfd = new Microsoft.Win32.SaveFileDialog();
			sfd.DefaultExt = ".save";
			sfd.Filter = "Saved game files (.save)|*.save";
			sfd.FileName = "dominion_net.save";
			Nullable<Boolean> dialogResult = sfd.ShowDialog(this);
			if (dialogResult == true)
			{
				this.game.Save(sfd.FileName);
			}
		}

		private void Game_Load_Click(object sender, RoutedEventArgs e)
		{
			Microsoft.Win32.OpenFileDialog ofd = new Microsoft.Win32.OpenFileDialog();
			ofd.DefaultExt = ".save";
			ofd.Filter = "Saved game files (.save)|*.save";
			Nullable<Boolean> dialogResult = ofd.ShowDialog(this);
			if (dialogResult != true)
				return;

			this.game = new DominionBase.Game();
			this.game.Load(ofd.FileName);

			// Clean out the Image Repository before starting a new game -- 
			// so we don't allocate too much memory for cards we're not even using
			Caching.ImageRepository.Reset();
			dpGameInfo.Visibility = System.Windows.Visibility.Visible;
			glMain.TearDown();
			glMain.Clear();

			LayoutSupplyPiles();
			while (tcAreas.Items.Count > 2)
				tcAreas.Items.RemoveAt(tcAreas.Items.Count - 1);
			dpMatsandPiles.Children.Clear();
			dpGameStuff.Children.Clear();
			_TradeRouteLabel = null;

			// Try to force garbage collection to save some memory
			GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced);

			// -------------------------------------------------------------------------------

			game.GameEndedEvent += new DominionBase.Game.GameEndedEventHandler(game_GameEndedEvent);

			wMain.Settings.PlayerSettings.ForEach(ps => glMain.AddPlayerColor(ps.Name, ps.UIColor));
			if (game.ActivePlayer != null)
			{
				glMain.NewSection(String.Format("Game loaded from {0}.", game.StartTime));
				glMain.Log("It's ", game.ActivePlayer, "'s turn");
			}

			game.Table.TokenPiles.TokenCollectionsChanged += new DominionBase.TokenCollections.TokenCollectionsChangedEventHandler(TokenPiles_TokenCollectionsChanged);
			game.Table.Trash.PileChanged += new DominionBase.Piles.Pile.PileChangedEventHandler(Trash_PileChanged);
			Trash_PileChanged(game.Table.Trash, new DominionBase.Piles.PileChangedEventArgs(DominionBase.Piles.PileChangedEventArgs.Operation.Reset));
			game.GameMessage += new DominionBase.Game.GameMessageEventHandler(game_GameMessage);

			foreach (DominionBase.Players.Player player in game.Players.FindAll(p => p.PlayerType == DominionBase.Players.PlayerType.Human))
				player.Choose = player_Choose;

			if (game.Players.Any(player => player.PlayerType == DominionBase.Players.PlayerType.Human))
				_Player = game.Players.OfType<DominionBase.Players.Human>().First();
			else
				_Player = null;

			// -------------------------------------------------------------------------------

			Type[] specialTypes = new Type[] { 
				DominionBase.Cards.Promotional.TypeClass.BlackMarketSupply, 
				DominionBase.Cards.Cornucopia.TypeClass.PrizeSupply, 
				DominionBase.Cards.DarkAges.TypeClass.Madman,
				DominionBase.Cards.DarkAges.TypeClass.Mercenary,
				DominionBase.Cards.DarkAges.TypeClass.Spoils };

			foreach (Type specialType in specialTypes)
			{
				if (game.Table.SpecialPiles.ContainsKey(specialType))
				{
					_MatEventHandlers[specialType] = new DominionBase.Piles.Pile.PileChangedEventHandler(GamePile_PileChanged);
					game.Table.SpecialPiles[specialType].PileChanged += _MatEventHandlers[specialType];
					GamePile_PileChanged(game.Table.SpecialPiles[specialType], new DominionBase.Piles.PileChangedEventArgs(DominionBase.Piles.PileChangedEventArgs.Operation.Reset));
				}
			}

			if (game.Table.Supplies.ContainsKey(DominionBase.Cards.Prosperity.TypeClass.TradeRoute))
			{
				if (dpGameStuff.Children.Count > 0)
				{
					Border bDiv = new Border();
					bDiv.BorderThickness = new Thickness(2);
					bDiv.BorderBrush = Brushes.Black;
					Panel.SetZIndex(bDiv, 1);
					DockPanel.SetDock(bDiv, Dock.Left);
					dpGameStuff.Children.Add(bDiv);
				}

				Label lTradeRoute = new Label();
				lTradeRoute.Content = "Trade Route Tokens:";
				lTradeRoute.FontSize = 16d;
				lTradeRoute.FontWeight = FontWeights.Bold;
				lTradeRoute.HorizontalContentAlignment = System.Windows.HorizontalAlignment.Right;
				lTradeRoute.Background = Caching.BrushRepository.GetBackgroundBrush(DominionBase.Cards.Category.Treasure);
				DockPanel.SetDock(lTradeRoute, Dock.Left);
				dpGameStuff.Children.Add(lTradeRoute);

				_TradeRouteLabel = new Label();
				_TradeRouteLabel.Content = game.Table.TokenPiles[DominionBase.Cards.Prosperity.TypeClass.TradeRouteToken].Count;
				_TradeRouteLabel.FontWeight = FontWeights.Bold;
				_TradeRouteLabel.VerticalAlignment = System.Windows.VerticalAlignment.Stretch;
				_TradeRouteLabel.VerticalContentAlignment = System.Windows.VerticalAlignment.Center;
				_TradeRouteLabel.Background = Caching.BrushRepository.GetBackgroundBrush(DominionBase.Cards.Category.Treasure);
				_TradeRouteLabel.Padding = new Thickness(0, 0, 5, 0);
				_TradeRouteLabel.BorderThickness = new Thickness(0, 0, 1, 0);
				DockPanel.SetDock(_TradeRouteLabel, Dock.Left);
				dpGameStuff.Children.Add(_TradeRouteLabel);
			}

			if (dpGameStuff.Children.Count > 0)
				bStuffDivider.Visibility = System.Windows.Visibility.Visible;
			else
				bStuffDivider.Visibility = System.Windows.Visibility.Collapsed;

			foreach (DominionBase.Players.Player player in game.Players)
			{
				TabItem tiPlayer = new TabItem();

				DockPanel dpHeader = new DockPanel();
				Image iHeader = new Image();
				iHeader.Stretch = Stretch.None;
				iHeader.Margin = new Thickness(0, 0, 5, 0);
				DockPanel.SetDock(iHeader, Dock.Left);
				switch (player.PlayerType)
				{
					case DominionBase.Players.PlayerType.Human:
						iHeader.Source = (BitmapImage)this.Resources["imHuman"];
						break;

					case DominionBase.Players.PlayerType.Computer:
						iHeader.Source = (BitmapImage)this.Resources["imComputer"];
						break;
				}
				dpHeader.Children.Add(iHeader);
				TextBlock tbHeader = new TextBlock();
				tbHeader.Text = player.Name;
				dpHeader.Children.Add(tbHeader);
				tiPlayer.Header = dpHeader;

				tcAreas.Items.Add(tiPlayer);
				Controls.ucPlayerDisplay ucpdPlayer = new Controls.ucPlayerDisplay();
				tiPlayer.Content = ucpdPlayer;
				ucpdPlayer.IsUIPlayer = (player == _Player);
				ucpdPlayer.Player = player;

				PlayerSettings playerSettings = _Settings.PlayerSettings.FirstOrDefault(ps => ps.Name == player.Name);
				if (playerSettings != null)
				{
					ColorHls hlsValue = HLSColor.RgbToHls(playerSettings.UIColor);
					Color cPlayer = HLSColor.HlsToRgb(hlsValue.H, Math.Min(1d, hlsValue.L * 1.125), hlsValue.S * 0.95, hlsValue.A);
					GradientStopCollection gsc = new GradientStopCollection();
					gsc.Add(new GradientStop(cPlayer, 0));
					gsc.Add(new GradientStop(playerSettings.UIColor, 0.25));
					gsc.Add(new GradientStop(playerSettings.UIColor, 0.75));
					gsc.Add(new GradientStop(cPlayer, 1));
					gsc.Freeze();
					tiPlayer.Background = new LinearGradientBrush(gsc, 0);
					//tiPlayer.Background = new SolidColorBrush(playerSettings.UIColor);
					ucpdPlayer.ColorFocus = playerSettings.UIColor;
				}

				ToolTip tt = new System.Windows.Controls.ToolTip();
				Controls.ucPlayerOverview ucpo = new Controls.ucPlayerOverview();
				ucpo.Player = player;
				tt.Content = ucpo;
				ToolTipService.SetToolTip(dpHeader, tt);
				if (Settings.ToolTipShowDuration == ToolTipShowDuration.Off)
					ToolTipService.SetIsEnabled(dpHeader, false);
				else
				{
					ToolTipService.SetIsEnabled(dpHeader, true);
					ToolTipService.SetShowDuration(dpHeader, (int)Settings.ToolTipShowDuration);
				}
				dpHeader.MouseDown += new MouseButtonEventHandler(tiPlayer_MouseDown);
				dpHeader.MouseUp += new MouseButtonEventHandler(tiPlayer_MouseUp);

				player.Revealed.PileChanged += new DominionBase.Piles.Pile.PileChangedEventHandler(Revealed_PileChanged);
				player.BenefitReceiving += new DominionBase.Players.Player.BenefitReceivingEventHandler(player_BenefitReceiving);
				//player.DiscardPile.PileChanged += new DominionBase.Pile.PileChangedEventHandler(DiscardPile_PileChanged);
				player.CardPlaying += new DominionBase.Players.Player.CardPlayingEventHandler(player_CardPlaying);
				player.CardPlayed += new DominionBase.Players.Player.CardPlayedEventHandler(player_CardPlayed);
				player.CardUndoPlaying += new DominionBase.Players.Player.CardUndoPlayingEventHandler(player_CardUndoPlaying);
				player.CardUndoPlayed += new DominionBase.Players.Player.CardUndoPlayedEventHandler(player_CardUndoPlayed);
				player.CardBuying += new DominionBase.Players.Player.CardBuyingEventHandler(player_CardBuying);
				player.CardBought += new DominionBase.Players.Player.CardBoughtEventHandler(player_CardBought);
				player.CardBuyFinished += new DominionBase.Players.Player.CardBuyFinishedEventHandler(player_CardBuyFinished);
				player.CardGaining += new DominionBase.Players.Player.CardGainingEventHandler(player_CardGaining);
				player.CardGainedInto += new DominionBase.Players.Player.CardGainedIntoEventHandler(player_CardGainedInto);
				player.CardGainFinished += new DominionBase.Players.Player.CardGainFinishedEventHandler(player_CardGainFinished);
				player.TokenPlaying += new DominionBase.Players.Player.TokenPlayingEventHandler(player_TokenPlaying);
				player.TokenPlayed += new DominionBase.Players.Player.TokenPlayedEventHandler(player_TokenPlayed);
				player.Trashing += new DominionBase.Players.Player.TrashingEventHandler(player_Trashing);
				player.TrashedFinished += new DominionBase.Players.Player.TrashedFinishedEventHandler(player_Trashed);
				player.PhaseChanged += new DominionBase.Players.Player.PhaseChangedEventHandler(player_PhaseChangedEvent);
				player.PlayerModeChanged += new DominionBase.Players.Player.PlayerModeChangedEventHandler(player_PlayerModeChangedEvent);
				player.CardsDrawn += new DominionBase.Players.Player.CardsDrawnEventHandler(player_CardsDrawn);
				player.TurnStarting += new DominionBase.Players.Player.TurnStartingEventHandler(player_TurnStarting);
				player.Shuffling += new DominionBase.Players.Player.ShufflingEventHandler(player_Shuffle);
				player.CardsAddedToDeck += new DominionBase.Players.Player.CardsAddedToDeckEventHandler(player_CardsAddedToDeck);
				player.CardsAddedToHand += new DominionBase.Players.Player.CardsAddedToHandEventHandler(player_CardsAddedToHand);
				player.CardsDiscarded += new DominionBase.Players.Player.CardsDiscardedEventHandler(player_CardsDiscarded);
				player.PlayerMats.CardMatsChanged += new DominionBase.Piles.CardMats.CardMatsChangedEventHandler(PlayerMats_DecksChanged);
				player.TokenPiles.TokenCollectionsChanged += new DominionBase.TokenCollections.TokenCollectionsChangedEventHandler(PlayerTokenPiles_TokenCollectionsChanged);
				player.BenefitsChanged += new DominionBase.Players.Player.BenefitsChangedEventHandler(player_BenefitsChanged);

				if (player == _Player)
				{
					tcAreas.SelectedItem = tiPlayer;
					player.CardReceived += new DominionBase.Players.Player.CardReceivedEventHandler(player_CardReceived);
				}
			}

			miNewGame.IsEnabled = false;
			miLoadGame.IsEnabled = false;
			miEndGame.IsEnabled = true;
			miSaveGame.IsEnabled = false;

			gameThread = new Thread(game.StartAsync);
			gameThread.Start();

			if (game.ActivePlayer != null)
			{
				if (game.Players[0] != game.ActivePlayer)
					glMain.NewTurn(game.TurnsTaken.TurnNumber(game.ActivePlayer));
				player_BenefitsChanged(game.ActivePlayer, new DominionBase.Players.BenefitsChangedEventArgs(game.ActivePlayer)
				{
					Actions = game.ActivePlayer.Actions,
					Buys = game.ActivePlayer.Buys,
					Currency = game.ActivePlayer.Currency,
					Player = game.ActivePlayer
				});
			}
			if (_Player.Phase == DominionBase.Players.PhaseEnum.Starting ||
					_Player.Phase == DominionBase.Players.PhaseEnum.Buy ||
					_Player.PlayerMode == DominionBase.Players.PlayerMode.Waiting)
				CheckBuyable(_Player);
			else
				ClearBuyable();
			UpdateDisplay();
		}

		private void Game_NewGame_Click(object sender, RoutedEventArgs e)
		{
			if (game != null && game.State == DominionBase.GameState.Running)
			{
				_StartingNewGame = true;
				if (!IsEndGameOK())
					return;
			}
			else
			{
				DominionBase.GameSettings settings = new DominionBase.GameSettings();
				settings.IdenticalStartingHands = _Settings.IdenticalStartingHands;
				settings.Constraints.AddRange(_Settings.Constraints);
				if (_Settings.UsePreset)
				{
					settings.Constraints.Clear();
					try { settings.Preset = _Settings.Presets.SingleOrDefault(p => p.Name == _Settings.PresetName); }
					catch { }

					if (settings.Preset == null)
					{
						wMessageBox.Show(String.Format("Cannot find preset named \"{0}\"!{1}Please check your presets.txt file.", _Settings.PresetName, System.Environment.NewLine), "Cannot find preset", MessageBoxButton.OK, MessageBoxImage.Exclamation);
						return;
					}
				}
				settings.CardSettings.AddRange(_Settings.CardSettings);
				settings.RandomAI_Unique = _Settings.RandomAI_Unique;
				settings.RandomAI_AllowedAIs.Clear();
				settings.RandomAI_AllowedAIs.AddRange(_Settings.RandomAI_AllowedAIs);
				if (_Settings.ForceColonyPlatinum)
					settings.ColonyPlatinumUsage = DominionBase.ColonyPlatinumUsage.Always;
				if (_Settings.ForceShelters)
					settings.ShelterUsage = DominionBase.ShelterUsage.Always;

				StartGame(settings);

				this.DataContext = wMain.Settings;
			}
		}

		void GamePile_PileChanged(object sender, DominionBase.Piles.PileChangedEventArgs e)
		{
			if (this.Dispatcher.CheckAccess())
			{
				DominionBase.Piles.Supply supply = sender as DominionBase.Piles.Supply;

				String wpName = String.Format("wp{0}", supply.SupplyCardType.Name);
				String cccName = String.Format("card{0}", supply.SupplyCardType.Name);
				String cccPileName = supply.SupplyCardType.Name;
				if (supply.SupplyCardType == DominionBase.Cards.Promotional.TypeClass.BlackMarketSupply)
				{
					cccPileName = "Black Market cards";
				}
				else if (supply.SupplyCardType == DominionBase.Cards.Cornucopia.TypeClass.PrizeSupply)
				{
					cccPileName = "Prizes";
				}

				WrapPanel wpSpecialPile = dpMatsandPiles.Children.OfType<WrapPanel>().SingleOrDefault(wp => wp.Name == wpName);
				CardCollectionControl cccSpecialPile = null;
				if (wpSpecialPile == null)
				{
					if (dpMatsandPiles.Children.Count > 0)
					{
						Border bDiv = new Border();
						bDiv.BorderThickness = new Thickness(2);
						bDiv.BorderBrush = Brushes.Black;
						DockPanel.SetDock(bDiv, Dock.Top);
						dpMatsandPiles.Children.Add(bDiv);
					}
					wpSpecialPile = new WrapPanel();
					wpSpecialPile.Name = wpName;
					wpSpecialPile.Orientation = Orientation.Horizontal;
					DockPanel.SetDock(wpSpecialPile, Dock.Top);

					cccSpecialPile = new CardCollectionControl();
					cccSpecialPile.Name = cccName;
					cccSpecialPile.Padding = new Thickness(0);
					cccSpecialPile.PileName = cccPileName;
					cccSpecialPile.CardSize = CardSize.Text;

					cccSpecialPile.ExactCount = true;
					cccSpecialPile.IsCardsVisible = true;
					cccSpecialPile.IsDisplaySorted = true;
					cccSpecialPile.Phase = DominionBase.Players.PhaseEnum.Action;
					cccSpecialPile.PlayerMode = DominionBase.Players.PlayerMode.Waiting;

					wpSpecialPile.Children.Add(cccSpecialPile);

					dpMatsandPiles.Children.Add(wpSpecialPile);
				}
				else
					cccSpecialPile = wpSpecialPile.Children.OfType<CardCollectionControl>().FirstOrDefault();

				cccSpecialPile.Pile = supply;
			}
			else
			{
				this.Dispatcher.BeginInvoke(new EventHandler<DominionBase.Piles.PileChangedEventArgs>(GamePile_PileChanged), System.Windows.Threading.DispatcherPriority.Normal, sender, e);
			}
		}

		private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
		{
			if (game != null)
			{
				if (!IsEndGameOK())
				{
					e.Cancel = true;
					return;
				}

				if (uccChooser.Visibility == System.Windows.Visibility.Visible)
					uccChooser.Player.WaitEvent.Set();
			}
			if (gameThread != null)
				gameThread.Join();

			_Settings.WindowSize = new Size(this.Width, this.Height);
			_Settings.WindowState = this.WindowState;
			_Settings.Save();
		}

		private void bBuyPhase_Click(object sender, RoutedEventArgs e)
		{
			if (_Settings.PromptUnplayedActions &&
				game.ActivePlayer.Phase == DominionBase.Players.PhaseEnum.Action &&
				game.ActivePlayer.Hand[DominionBase.Cards.Category.Action].Count > 0 &&
				game.ActivePlayer.Actions > 0)
			{
				if (wMessageBox.Show("You have unplayed actions left.  Are you sure you want to do this?", "Please confirm", MessageBoxButton.YesNo) != MessageBoxResult.Yes)
					return;
			}

			(sender as Control).IsEnabled = false;
			WaitCallback wcb = new WaitCallback(UpdateDisplayTarget); 
			DominionBase.GameGoToBuyPhaseMessage ggtbpm = new DominionBase.GameGoToBuyPhaseMessage(wcb, game.ActivePlayer);
			ggtbpm.Message = String.Format("{0} going to Buy phase", game.ActivePlayer);
			EnqueueGameMessageAndWait(ggtbpm);
		}

		private void tcAreas_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			foreach (TabItem removedTab in e.RemovedItems.OfType<TabItem>())
			{
				if ((removedTab.Content as Controls.ucPlayerDisplay) == null)
					continue;
				(removedTab.Content as Controls.ucPlayerDisplay).IsActive = false;
			}
			foreach (TabItem addedTab in e.AddedItems.OfType<TabItem>())
			{
				if ((addedTab.Content as Controls.ucPlayerDisplay) == null)
					continue;
				(addedTab.Content as Controls.ucPlayerDisplay).IsActive = true;
				addedTab.InvalidateVisual();
			}
		}

		private void OpenUrl(String target)
		{
			try
			{
				System.Diagnostics.Process.Start(target);
			}
			catch (System.ComponentModel.Win32Exception noBrowser)
			{
				if (noBrowser.ErrorCode == -2147467259)
					wMessageBox.Show(noBrowser.Message);
			}
			catch (System.Exception other)
			{
				wMessageBox.Show(other.Message);
			}
		}

		private void Help_OfficialSite_Click(object sender, RoutedEventArgs e)
		{
			OpenUrl("http://www.riograndegames.com/games.html?id=278");
		}

		private void Help_DeveloperSite_Click(object sender, RoutedEventArgs e)
		{
			OpenUrl("http://dominion.technowall.net/");
		}

		private void Help_CheckForUpdates_Click(object sender, RoutedEventArgs e)
		{
			try { CheckForUpdates(true); }
			catch { }
		}

		private void Help_CardViewer_Click(object sender, RoutedEventArgs e)
		{
			wCardViewer wcv = new wCardViewer();
			wcv.Show();
		}

		private void svGame_ScrollChanged(object sender, ScrollChangedEventArgs e)
		{
			ScrollViewer sv = sender as ScrollViewer;
			if (sv.ExtentHeight == 0 || sv.ExtentWidth == 0)
				return;

			bGameHorizontal.Width = sv.ViewportWidth * sv.ViewportWidth / sv.ExtentWidth;
			bGameVertical.Height = sv.ViewportHeight * sv.ViewportHeight / sv.ExtentHeight;

			bGameHorizontal.Margin = new Thickness(sv.ViewportWidth * sv.HorizontalOffset / sv.ExtentWidth, 0, 0, 0);
			bGameVertical.Margin = new Thickness(0, sv.ViewportHeight * sv.VerticalOffset / sv.ExtentHeight, 0, 0);

			bOpacityLayerLeft.Visibility = bOpacityLayerRight.Visibility = System.Windows.Visibility.Visible;
			if (bGameHorizontal.Width >= sv.ViewportWidth)
				bOpacityLayerLeft.Visibility = bOpacityLayerRight.Visibility = System.Windows.Visibility.Collapsed;
			else if (bGameHorizontal.Margin.Left <= 0)
				bOpacityLayerLeft.Visibility = System.Windows.Visibility.Collapsed;
			else if (bGameHorizontal.Margin.Left + bGameHorizontal.Width >= sv.ViewportWidth)
				bOpacityLayerRight.Visibility = System.Windows.Visibility.Collapsed;

			bOpacityLayerTop.Visibility = bOpacityLayerBottom.Visibility = System.Windows.Visibility.Visible;
			if (bGameVertical.Height >= sv.ViewportHeight)
				bOpacityLayerTop.Visibility = bOpacityLayerBottom.Visibility = System.Windows.Visibility.Collapsed;
			else if (bGameVertical.Margin.Top <= 0)
				bOpacityLayerTop.Visibility = System.Windows.Visibility.Collapsed;
			else if (bGameVertical.Margin.Top + bGameVertical.Height >= sv.ViewportHeight)
				bOpacityLayerBottom.Visibility = System.Windows.Visibility.Collapsed;
		}

		private void Grid_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
		{
			Grid g = (Grid)sender;
			g.CaptureMouse();
			g.Cursor = Cursors.ScrollNS;
			svGame.ScrollToVerticalOffset(e.GetPosition(g).Y / g.ActualHeight * svGame.ExtentHeight);
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
				svGame.ScrollToVerticalOffset(e.GetPosition(g).Y / g.ActualHeight * svGame.ExtentHeight);
		}

		private void Grid_MouseLeftButtonDown_1(object sender, MouseButtonEventArgs e)
		{
			Grid g = (Grid)sender;
			g.CaptureMouse();
			g.Cursor = Cursors.ScrollWE;
			svGame.ScrollToHorizontalOffset(e.GetPosition(g).X / g.ActualWidth * svGame.ExtentWidth);
		}

		private void Grid_MouseMove_1(object sender, MouseEventArgs e)
		{
			Grid g = (Grid)sender;
			if (g.IsMouseCaptured)
				svGame.ScrollToHorizontalOffset(e.GetPosition(g).X / g.ActualWidth * svGame.ExtentWidth);
		}

		private void CheckForUpdates(Boolean forceCheck)
		{
			Assembly a = Assembly.GetExecutingAssembly();

			// If this isn't null, we're trying to update to a version
			if (!forceCheck && System.Windows.Application.Current.Properties["Update"] != null)
			{
				System.Net.WebClient wClient = new System.Net.WebClient();
				wClient.DownloadFileCompleted += new System.ComponentModel.AsyncCompletedEventHandler(wClient_DownloadFileCompleted);
				wClient.DownloadProgressChanged += new System.Net.DownloadProgressChangedEventHandler(wClient_DownloadProgressChanged);
				wClient.DownloadFileAsync(new Uri(System.Windows.Application.Current.Properties["Update"].ToString()), System.IO.Path.Combine(System.IO.Path.GetDirectoryName(a.Location), "update.zip"));
			}
			else
			{
				if (System.Windows.Application.Current.Properties["Updated"] != null && (Boolean)System.Windows.Application.Current.Properties["Updated"])
				{
					wMessageBox.Show(String.Format("Successfully updated to latest version {0}!", a.GetName().Version), "Update complete!", MessageBoxButton.OK, MessageBoxImage.Information);
					_Settings.UpdateAvailable = false;
					_Settings.Save();
				}

				String tempPath = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(a.Location), "update");
				// Blow away any existing files
				if (System.IO.Directory.Exists(tempPath))
					System.IO.Directory.Delete(tempPath, true);

				iUpdate.Dispatcher.BeginInvoke((Action)(() => iUpdate.Visibility = miDownload.Visibility = System.Windows.Visibility.Collapsed));

				// Only check for an update if we haven't checked in the last 24 hours
				// Or if we're forcing a check
				if (forceCheck || DateTime.Now - TimeSpan.FromHours(24) > _Settings.LastUpdateCheck)
				{
					this.latestVersionInfo = VersionChecker.GetLatestVersion();
					_Settings.LastUpdateCheck = DateTime.Now;

					Version currentVersion = a.GetName().Version;
					if (latestVersionInfo.IsVersionValid && latestVersionInfo.IsNewerThan(currentVersion))
						_Settings.UpdateAvailable = true;

					if (forceCheck)
						wMessageBox.Show(String.Format("Your version: {0}{2}Latest version: {1}", currentVersion, latestVersionInfo.Version, System.Environment.NewLine), "Version info", MessageBoxButton.OK, MessageBoxImage.Information);

					_Settings.Save();
				}

				if (_Settings.UpdateAvailable)
				{
					iUpdate.Dispatcher.BeginInvoke((Action)(() => iUpdate.Visibility = miDownload.Visibility = System.Windows.Visibility.Visible));
					iUpdate.Dispatcher.BeginInvoke((Action)(() => iUpdate.ToolTip = miDownload.ToolTip = "Update available"));
				}
			}
		}

		void wClient_DownloadProgressChanged(object sender, System.Net.DownloadProgressChangedEventArgs e)
		{
			if (this.Dispatcher.CheckAccess())
			{
				cpStatus.Content = String.Format("Downloading {0}...", System.Windows.Application.Current.Properties["Update"]);
				pbStatus.Visibility = System.Windows.Visibility.Visible;
				pbStatus.Minimum = 0;
				pbStatus.Value = 0;
				pbStatus.Maximum = e.TotalBytesToReceive;
				pbStatus.Value = e.BytesReceived;
				pbStatus.ToolTip = String.Format("{0:0.00}KB / {1:0.00}KB ({2}%)", e.BytesReceived / 1024.0, e.TotalBytesToReceive / 1024.0, e.ProgressPercentage);
			}
			else
			{
				this.Dispatcher.BeginInvoke(new EventHandler<System.Net.DownloadProgressChangedEventArgs>(wClient_DownloadProgressChanged), System.Windows.Threading.DispatcherPriority.Normal, sender, e);
			}
		}

		void wClient_DownloadFileCompleted(object sender, System.ComponentModel.AsyncCompletedEventArgs e)
		{
			if (this.Dispatcher.CheckAccess())
			{
				cpStatus.Content = String.Empty; // "Hi!";
				pbStatus.Visibility = System.Windows.Visibility.Collapsed;

				Assembly a = Assembly.GetExecutingAssembly();
				System.IO.DirectoryInfo di = new System.IO.DirectoryInfo(System.IO.Path.GetDirectoryName(a.Location));
				String zipFile = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(a.Location), "update.zip");
				try
				{
					using (System.IO.FileStream fileStreamIn = System.IO.File.OpenRead(zipFile))
					{
						using (ZipInputStream zipInStream = new ZipInputStream(fileStreamIn))
						{
							ZipEntry zEntry;
							while ((zEntry = zipInStream.GetNextEntry()) != null)
							{
								byte[] buffer = new byte[4096];
								String fullZipToPath = System.IO.Path.Combine(di.Parent.FullName, zEntry.Name);
								String directoryName = System.IO.Path.GetDirectoryName(zEntry.Name);
								if (!String.IsNullOrWhiteSpace(directoryName) && !System.IO.Directory.Exists(directoryName))
									System.IO.Directory.CreateDirectory(directoryName);

								if (String.IsNullOrWhiteSpace(System.IO.Path.GetFileName(fullZipToPath)))
									continue;

								using (System.IO.FileStream streamWriter = System.IO.File.Create(fullZipToPath))
								{
									ICSharpCode.SharpZipLib.Core.StreamUtils.Copy(zipInStream, streamWriter, buffer);
								}
							}
						}
					}
				}
				catch (Exception ex) { wMessageBox.Show(ex.Message); wMessageBox.Show(ex.StackTrace); throw; }

				String tempFile = System.IO.Path.Combine(di.Parent.FullName, System.IO.Path.GetFileName(a.Location));
				System.Diagnostics.Process.Start(tempFile, "-U");
				this.Close();
			}
			else
			{
				this.Dispatcher.BeginInvoke(new EventHandler<System.ComponentModel.AsyncCompletedEventArgs>(wClient_DownloadFileCompleted), System.Windows.Threading.DispatcherPriority.Normal, sender, e);
			}
		}

		private void Help_DownloadLatest_Click(object sender, RoutedEventArgs e)
		{
			if (!_Settings.UpdateAvailable)
				return;

			Assembly a = Assembly.GetExecutingAssembly();
			String tempPath = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(a.Location), "update");

			// Blow away any existing files
			if (System.IO.Directory.Exists(tempPath))
				System.IO.Directory.Delete(tempPath, true);
			System.IO.Directory.CreateDirectory(tempPath);

			// Copy all DLLs to the temp Update directory -- these are the only ones that can't be overwritten
			foreach (String file in System.IO.Directory.GetFiles(System.IO.Path.GetDirectoryName(a.Location), "*", System.IO.SearchOption.TopDirectoryOnly))
			{
				if (file.ToLower().EndsWith(".dll") || file == a.Location)
					System.IO.File.Copy(file, System.IO.Path.Combine(tempPath, System.IO.Path.GetFileName(file)));
			}

			String tempFile = System.IO.Path.Combine(tempPath, System.IO.Path.GetFileName(a.Location));
			if (this.latestVersionInfo == null)
				this.latestVersionInfo = VersionChecker.GetLatestVersion();
			System.Diagnostics.Process.Start(tempFile, String.Format("-u \"{0}\"", this.latestVersionInfo.FileUrl));
			this.Close();
		}

		private void Game_EndGame_Click(object sender, RoutedEventArgs e)
		{
			IsEndGameOK();
		}

		private Boolean IsEndGameOK()
		{
			if (game != null && game.State == DominionBase.GameState.Running)
			{
				if (game.State == DominionBase.GameState.Running && game.Players.Contains(_Player))
				{
					MessageBoxResult mbr = wMessageBox.Show("Do you want to abort the current game?  This will count as a loss in your statistics.", "Please confirm", MessageBoxButton.YesNo, MessageBoxImage.None, MessageBoxResult.No);
					if (mbr != MessageBoxResult.Yes)
						return false;
				}

				if (uccChooser.Visibility == System.Windows.Visibility.Visible)
				{
					game.Abort();
					uccChooser.Player.WaitEvent.Set();
					uccChooser.Player = null;
					uccChooser.Choice = null;
					uccChooser.IsReady = false;
					uccChooser.Visibility = System.Windows.Visibility.Collapsed;
				}

				DominionBase.GameEndMessage gem = new DominionBase.GameEndMessage(_Player);
				gem.Message = String.Format("{0} ending game", _Player);
				EnqueueGameMessageAndWait(gem);
			}

			bTurnDone.IsEnabled = false;
			bPlayTreasures.Text = "Play basic _Treasures";
			bPlayTreasures.IsEnabled = false;
			bPlayCoinTokens.IsEnabled = false;
			bBuyPhase.IsEnabled = false;
			bUndo.IsEnabled = false;
			
			return true;
		}

		private void Game_Replay_Click(object sender, RoutedEventArgs e)
		{
			if (game == null || game.State == DominionBase.GameState.Running || game.State == DominionBase.GameState.NotStarted)
			{
				miReplay.IsEnabled = false;
				return;
			}

			DominionBase.GameSettings settings = new DominionBase.GameSettings();
			settings.IdenticalStartingHands = game.Settings.IdenticalStartingHands;
			settings.ColonyPlatinumUsage = DominionBase.ColonyPlatinumUsage.Never;
			settings.ShelterUsage = DominionBase.ShelterUsage.Never;
			Dictionary<String, DominionBase.Cards.Card> cardDictMap = new Dictionary<string,DominionBase.Cards.Card>();

			settings.Preset = new DominionBase.Cards.Preset("Replay");
			foreach (DominionBase.Piles.Supply supply in game.Table.Supplies.Values)
			{
				if (supply.Location == DominionBase.Cards.Location.Kingdom)
				{
					settings.Preset.Cards.Add(DominionBase.Cards.Card.CreateInstance(supply.CardType));
				}

				//if (supply.CardType == DominionBase.Cards.Prosperity.TypeClass.Colony || supply.CardType == DominionBase.Cards.Prosperity.TypeClass.Platinum)
				//    settings.ColonyPlatinumUsage = DominionBase.ColonyPlatinumUsage.Always;
			}
			if (game.Settings.ColonyPlatinumUsage == DominionBase.ColonyPlatinumUsage.Used)
				settings.ColonyPlatinumUsage = DominionBase.ColonyPlatinumUsage.Always;
			if (game.Settings.ShelterUsage == DominionBase.ShelterUsage.Used)
				settings.ShelterUsage = DominionBase.ShelterUsage.Always;

			List<DominionBase.Cards.Card> copyOfPresetCards = new List<DominionBase.Cards.Card>(settings.Preset.Cards);
			foreach (DominionBase.Cards.Card presetCard in copyOfPresetCards)
				presetCard.CheckSetup(settings.Preset, game.Table);
			settings.RandomAI_Unique = game.Settings.RandomAI_Unique;
			settings.RandomAI_AllowedAIs.Clear();
			settings.RandomAI_AllowedAIs.AddRange(game.Settings.RandomAI_AllowedAIs);

			StartGame(settings);

		}

		private void GridSplitter_MouseDoubleClick(object sender, MouseButtonEventArgs e)
		{
			GridLengthConverter glc = new GridLengthConverter();
			gMainDisplay.RowDefinitions[0].Height = GridLength.Auto;
			gMainDisplay.RowDefinitions[0].Height = (GridLength)glc.ConvertFrom(gMainDisplay.RowDefinitions[0].MinHeight + 4);
			Debug.WriteLine(String.Format("Margin: {0}", ((GridSplitter)sender).Margin));
			
			Debug.WriteLine(String.Format("ActualHeight: {0}", stackPanelSupplyPiles.ActualHeight));
		}

		private void CurrentGame_ViewGameLog_Click(object sender, RoutedEventArgs e)
		{
			if (System.IO.File.Exists(glMain.LogFile))
				System.Diagnostics.Process.Start(glMain.LogFile);
		}

		private void MenuItem_SubmenuOpened(object sender, RoutedEventArgs e)
		{
			miViewGameLog.IsEnabled = System.IO.File.Exists(glMain.LogFile);
		}

		private void miLayoutChanged(object sender, RoutedEventArgs e)
		{
			wMain.Settings.Save();
			LayoutSupplyPiles();
		}
	}
}
