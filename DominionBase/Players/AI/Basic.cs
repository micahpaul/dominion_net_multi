using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;

using DominionBase.Cards;
using DominionBase.Piles;

namespace DominionBase.Players.AI
{
	public class Basic : Player, IComputerAI
	{
		public static String AIName { get { return "Basic"; } }
		public static String AIDescription { get { return "Very basic AI that makes random choices for every decision encountered."; } }

		public AutoResetEvent InternalWaitEvent = new AutoResetEvent(false);
		protected List<Type> _CardsGained = new List<Type>();
		protected Card _LastReactedCard = null;
		protected int _SleepTime = 750;

		protected Player RealThis = null;

		public virtual String AIType
		{
			get { return (String)this.GetType().GetProperty("AIName", BindingFlags.Static | BindingFlags.Public | BindingFlags.GetProperty | BindingFlags.FlattenHierarchy).GetValue(null, null); }
		}

		public AIState State { get; private set; }
		internal Boolean ShouldStop { get; private set; }

		public Basic(Game game, String name)
			: base(game, name)
		{
			this.State = AIState.NotStarted;
			this.ShouldStop = false;
			//Setup(game, this);
		}

		public Basic(Game game, String name, Player realThis)
			: base(game, name)
		{
			this.State = AIState.NotStarted;
			this.ShouldStop = false;
			//Setup(game, realThis);
		}

		internal override void Setup(Game game)
		{
			base.Setup(game);
			this.Setup(game, this);
		}

		internal virtual void Setup(Game game, Player realThis)
		{
			this.RealThis = realThis;

			_PlayerType = PlayerType.Computer;

			realThis.Choose = player_Choose;

			realThis.TurnStarting += new TurnStartingEventHandler(AI_TurnStarting);
			realThis.CardGained += new CardGainedEventHandler(Basic_CardGained);
		}

		public override void StartAsync()
		{
			try
			{
				System.Diagnostics.Trace.WriteLine(String.Format("AI_Worker({0}): Starting worker thread", this));
				this.PlayerModeChanged += new PlayerModeChangedEventHandler(player_PlayerModeChangedEvent);
				if (this.RealThis.UniqueId != this.UniqueId)
					this.RealThis.PlayerModeChanged += new PlayerModeChangedEventHandler(player_PlayerModeChangedEvent);

				this.ShouldStop = false;
				this.State = AIState.Running;

				while (!this.ShouldStop && this.RealThis.Phase != PhaseEnum.Endgame)
				{
					System.Threading.Thread.Sleep(500);
				}
				this.EndgameTriggered();
				System.Diagnostics.Trace.WriteLine(String.Format("AI_Worker({0}): Shutting down", this));
			}
			catch (Exception ex)
			{
				Utilities.Logging.LogError(ex);
				throw;
			}
		}

		internal virtual void EndgameTriggered()
		{
		}

		void player_PlayerModeChangedEvent(object sender, DominionBase.Players.PlayerModeChangedEventArgs e)
		{
			if ((e.CurrentPlayer != this && e.CurrentPlayer != this.RealThis) || e.NewPlayerMode != Players.PlayerMode.Normal)
				return;

			//System.Diagnostics.Trace.WriteLine(String.Format("AI_Worker: {0}: {1} {2} ({3}->{4})", sender, e.CurrentPlayer, e.CurrentPlayer.Phase, e.OldPlayerMode, e.NewPlayerMode));
			if (e.CurrentPlayer.Phase == PhaseEnum.Action)
			{
				// Ugly hack, but it mostly works -- just a slight delay between the end of the PhaseChangedEvent and the AutoPlay
				BackgroundWorker autoplayInvoker = new BackgroundWorker();
				autoplayInvoker.DoWork += delegate
				{
					Thread.Sleep(TimeSpan.FromMilliseconds(50));
					PlayActionCard();
				};
				autoplayInvoker.RunWorkerAsync();
			}

			if (e.CurrentPlayer.Phase == PhaseEnum.ActionTreasure || e.CurrentPlayer.Phase == PhaseEnum.BuyTreasure)
			{
				// Ugly hack, but it mostly works -- just a slight delay between the end of the PhaseChangedEvent and the AutoPlay
				BackgroundWorker autoplayInvoker = new BackgroundWorker();
				autoplayInvoker.DoWork += delegate
				{
					Thread.Sleep(TimeSpan.FromMilliseconds(50));
					PlayTreasure();
				};
				autoplayInvoker.RunWorkerAsync();
			}

			if (e.CurrentPlayer.Phase == PhaseEnum.Buy)
			{
				if (this.Buys > 0)
				{
					// Ugly hack, but it mostly works -- just a slight delay between the end of the PhaseChangedEvent and the AutoPlay
					BackgroundWorker autoplayInvoker = new BackgroundWorker();
					autoplayInvoker.DoWork += delegate
					{
						Thread.Sleep(TimeSpan.FromMilliseconds(50));
						BuyCard();
					};
					autoplayInvoker.RunWorkerAsync();
				}
				else
				{
					GameEndTurnMessage getm = new GameEndTurnMessage(null, this);
					getm.Message = String.Format("{0} ending turn", this);
					Boolean lockWasTaken = false;
					var temp = this._Game.MessageRequestQueue;
					try { Monitor.Enter(temp, ref lockWasTaken); { this._Game.MessageRequestQueue.Enqueue(getm); } }
					finally { if (lockWasTaken) Monitor.Exit(temp); }
					this._Game.WaitEvent.Set();

					while (this._Game.MessageResponseQueue.Count == 0)
						Thread.Sleep(100);
				}
			}
		}

		protected virtual void PlayActionCard()
		{
			if (this.RealThis.Phase == PhaseEnum.Action && this.RealThis.PlayerMode == Players.PlayerMode.Normal)
			{
				Thread.Sleep(_SleepTime);
				Card cardToPlay = this.FindBestCardToPlay(this.RealThis.Hand[Category.Action]);
				// Need to check twice -- things could've changed since _SleepTime
				if (this.RealThis.Phase == PhaseEnum.Action && this.RealThis.PlayerMode == Players.PlayerMode.Normal)
				{
					if (cardToPlay != null)
					{
						this.RealThis.PlayCard(cardToPlay);
					}
					else
					{
						this.RealThis.GoToTreasurePhase();
					}
				}
			}
		}

		protected virtual void PlayTreasure()
		{
			if ((this.RealThis.Phase == PhaseEnum.ActionTreasure || this.RealThis.Phase == PhaseEnum.BuyTreasure) &&
				 this.RealThis.PlayerMode == Players.PlayerMode.Normal)
			{
				Thread.Sleep(_SleepTime);
				CardCollection nextTreasures = this.FindBestCardsToPlay(this.RealThis.Hand[Category.Treasure]);

				// Need to check twice -- things could've changed since _SleepTime
				if ((this.RealThis.Phase == PhaseEnum.ActionTreasure || this.RealThis.Phase == PhaseEnum.BuyTreasure) &&
				 this.RealThis.PlayerMode == Players.PlayerMode.Normal)
				{
					if (nextTreasures.Count > 0)
					{
						this.RealThis.PlayCards(nextTreasures);
					}
					else if (this.RealThis.TokenPiles.ContainsKey(Cards.Guilds.TypeClass.CoinToken) &&
						this.RealThis.TokenPiles[Cards.Guilds.TypeClass.CoinToken].Count > 0 &&
						this.RealThis.Phase == PhaseEnum.BuyTreasure)
					{
						// This is really bad, but have the Basic AI play all the Coin tokens it has every time
						this.RealThis.PlayTokens(this._Game, Cards.Guilds.TypeClass.CoinToken, this.RealThis.TokenPiles[Cards.Guilds.TypeClass.CoinToken].Count);
						if (this.RealThis.Phase != PhaseEnum.Buy)
							this.RealThis.GoToBuyPhase();
					}
					else
					{
						this.RealThis.GoToBuyPhase();
					}
				}
			}
		}

		protected virtual void BuyCard()
		{
			List<Supply> buyableSupplies = new List<Supply>();
			Thread.Sleep(_SleepTime * 4 / 5);
			if (this.RealThis.Phase == PhaseEnum.Buy && this.RealThis.PlayerMode == Players.PlayerMode.Normal)
			{
				buyableSupplies.Clear();
				foreach (Supply supply in this.RealThis._Game.Table.Supplies.Values)
				{
					if (supply.CanBuy(this.RealThis) && this.ShouldBuy(supply))
						buyableSupplies.Add(supply);
				}
				//buyableSupplies.Sort(this.SortSupplyCostDescending());
				if (this.RealThis.Buys > 0 && buyableSupplies.Count > 0)
				{
					Supply supplyToBuy = this.FindBestCardToBuy(buyableSupplies);

					if (supplyToBuy == null)
					{
						GameEndTurnMessage getm = new GameEndTurnMessage(null, this);
						getm.Message = String.Format("{0} ending turn", this);
						Boolean lockWasTaken = false;
						var temp = this._Game.MessageRequestQueue;
						try { Monitor.Enter(temp, ref lockWasTaken); { this._Game.MessageRequestQueue.Enqueue(getm); } }
						finally { if (lockWasTaken) Monitor.Exit(temp); }
						this._Game.WaitEvent.Set();

						while (this._Game.MessageResponseQueue.Count == 0)
							Thread.Sleep(100);

						return;
					}

					this.RealThis.Buy(supplyToBuy);
				}
				else
				{
					GameEndTurnMessage getm = new GameEndTurnMessage(null, this);
					getm.Message = String.Format("{0} ending turn", this);
					Boolean lockWasTaken = false;
					var temp = this._Game.MessageRequestQueue;
					try { Monitor.Enter(temp, ref lockWasTaken); { this._Game.MessageRequestQueue.Enqueue(getm); } }
					finally { if (lockWasTaken) Monitor.Exit(temp); }
					this._Game.WaitEvent.Set();

					while (this._Game.MessageResponseQueue.Count == 0)
						Thread.Sleep(100);

					return;
				}
				Thread.Sleep(_SleepTime * 4 / 5);
			}
		}

		public override void Cleanup()
		{
			if (this.RealThis != this)
				this.RealThis.Cleanup();
			else
				base.Cleanup();
		}

		internal override void Clear()
		{
			base.Clear();

			this._CardsGained.Clear();
			this._LastReactedCard = null;
		}

		internal override void TearDown()
		{
			base.TearDown();

			this.RealThis.TurnStarting -= new TurnStartingEventHandler(AI_TurnStarting);
			this.RealThis.CardGained -= new CardGainedEventHandler(Basic_CardGained);
		}

		private void AI_TurnStarting(object sender, TurnStartingEventArgs e)
		{
			_LastReactedCard = null;
		}

		private void Basic_CardGained(object sender, CardGainEventArgs e)
		{
			if (!_CardsGained.Contains(e.Card.CardType))
				_CardsGained.Add(e.Card.CardType);
		}

		protected static IComparer<Card> SortCardCostAscending()
		{
			return (IComparer<Card>)new SortCardPriceAscendingHelper();
		}

		protected class SortCardPriceAscendingHelper : IComparer<Card>
		{
			int IComparer<Card>.Compare(Card a, Card b)
			{
				if (a.BaseCost == b.BaseCost)
					return a.Name.CompareTo(b.Name);
				return a.BaseCost.CompareTo(b.BaseCost);
			}
		}

		protected static IComparer<Supply> SortSupplyCostDescending()
		{
			return (IComparer<Supply>)new SortSupplyPriceAscendingHelper();
		}

		protected class SortSupplyPriceAscendingHelper : IComparer<Supply>
		{
			int IComparer<Supply>.Compare(Supply a, Supply b)
			{
				if (a.BaseCost == b.BaseCost)
					return a.Name.CompareTo(b.Name);
				return -a.BaseCost.CompareTo(b.BaseCost);
			}
		}

		protected virtual Card FindBestCardToPlay(IEnumerable<Card> cards)
		{
			return cards.ElementAt(this._Game.RNG.Next(cards.Count()));
		}

		protected virtual CardCollection FindBestCardsToPlay(IEnumerable<Card> cards)
		{
			CardCollection cardsToPlay = new CardCollection(cards);
			Utilities.Shuffler.Shuffle(cardsToPlay);
			return new CardCollection(cardsToPlay.TakeWhile(c => c.Location == Location.General || 
				c.CardType.GetMethod("Play", new Type[] { typeof(Player) }).DeclaringType == typeof(Card)));
		}

		protected virtual Supply FindBestCardToBuy(List<Supply> buyableSupplies)
		{
			buyableSupplies.Sort(Basic.SortSupplyCostDescending());

			if (buyableSupplies.Count > 0)
			{
				Cost cost = buyableSupplies[0].BaseCost;
				List<Supply> mostExpensive = buyableSupplies.FindAll(s => s.BaseCost == cost);
				return mostExpensive[this._Game.RNG.Next(mostExpensive.Count)];
			}

			return null;
		}

		/// <summary>
		/// Returns a float representing the current progress of the game -- a smaller number means that the game will probably end sooner
		/// </summary>
		public virtual float GameProgress
		{
			get { return this.GameProgressNew; }
		}

		/// <summary>
		/// Returns a float representing the current progress of the game -- a smaller number means that the game will probably end sooner
		/// </summary>
		public virtual float GameProgressOld
		{
			get
			{
				SupplyCollection supplies = this.RealThis._Game.Table.Supplies;
				List<Supply> smallest = supplies.Values.OrderBy(s => s.Count).Take(4).ToList();
				//List<Supply> smallest = new List<Supply>();
				//// Get the 4 smallest piles
				//foreach (Supply supply in supplies.Values)
				//{
				//    if (smallest.Count < 4)
				//    {
				//        smallest.Add(supply);
				//        continue;
				//    }
				//    for (int i = 0; i < smallest.Count; i++)
				//    {
				//        if (supply.Count < smallest[i].Count)
				//        {
				//            smallest[i] = supply;
				//            break;
				//        }
				//    }
				//}
				float progress = smallest.Sum(supply => supply.Count) / (10 * 4f);
				float provinceProgress = ((float)supplies[Cards.Universal.TypeClass.Province].Count) / supplies[Cards.Universal.TypeClass.Province].StartingStackSize;
				provinceProgress = (float)(Math.Sin(2 * Math.PI * provinceProgress) / (2 * Math.PI) + provinceProgress);
				float colonyProgress = 1;
				if (supplies.ContainsKey(Cards.Prosperity.TypeClass.Colony))
					colonyProgress = ((float)supplies[Cards.Prosperity.TypeClass.Colony].Count) / supplies[Cards.Prosperity.TypeClass.Colony].StartingStackSize;
				colonyProgress = (float)(Math.Sin(2 * Math.PI * colonyProgress) / (2 * Math.PI) + colonyProgress);

				return Math.Min(Math.Min(provinceProgress, colonyProgress), progress);
				//if (provinceProgress < progress)
				//    progress = provinceProgress;
				//if (colonyProgress < progress)
				//    progress = colonyProgress;
				//return progress;
			}
		}

		/// <summary>
		/// Returns a float representing the current progress of the game -- a smaller number means that the game will probably end sooner
		/// </summary>
		public virtual float GameProgressNew
		{
			get
			{
				SupplyCollection supplies = this.RealThis._Game.Table.Supplies;

				List<Supply> smallest = supplies.Values.OrderBy(s => s.Count).Take(4).ToList();
				//List<Supply> smallest = new List<Supply>();
				//// Get the 4 smallest piles
				//foreach (Supply supply in supplies.Values)
				//{
				//    if (smallest.Count < 4)
				//    {
				//        smallest.Add(supply);
				//        continue;
				//    }
				//    for (int i = 0; i < smallest.Count; i++)
				//    {
				//        if (supply.Count < smallest[i].Count)
				//        {
				//            smallest[i] = supply;
				//            break;
				//        }
				//    }
				//}

				float provinceProgress = ((float)supplies[Cards.Universal.TypeClass.Province].Count) / supplies[Cards.Universal.TypeClass.Province].StartingStackSize;
				float provinceThreshold = (this.RealThis._Game.Players.Count * 2.25f / supplies[Cards.Universal.TypeClass.Province].StartingStackSize);
				float colonyProgress = 1;
				float colonyThreshold = 0.5f;
				if (this.RealThis._Game.Table.Supplies.ContainsKey(Cards.Prosperity.TypeClass.Colony))
				{
					colonyProgress = ((float)supplies[Cards.Prosperity.TypeClass.Colony].Count) / supplies[Cards.Prosperity.TypeClass.Colony].StartingStackSize;
					colonyThreshold = (this.RealThis._Game.Players.Count * 2.25f / supplies[Cards.Prosperity.TypeClass.Colony].StartingStackSize);
				}

				// Scaled late game (0.2 -> 0)
				if ((provinceProgress <= provinceThreshold || colonyProgress <= colonyThreshold) || smallest[0].Count == 0 || smallest.Take(3).Sum(s => s.Count) < 9)
					return Math.Min(Math.Min(4f / 15 * provinceProgress, 4f / 15 * colonyProgress),
						2f / 3 * Math.Min(30, smallest.Take(3).Sum(s => s.Count)) / 30f);

				// Scaled mid game (0.6 -> 0.2)
				if ((provinceProgress <= 0.9167 || colonyProgress <= 0.9167) || smallest.Take(3).Sum(s => s.Count) < 16)
					return Math.Min(Math.Min(12f / 5 * provinceProgress - 8f / 5, 12f / 5 * colonyProgress - 8f / 5), 
						12f / 7 * Math.Min(30, smallest.Take(3).Sum(s => s.Count)) / 30f - 11f / 35);

				// Scaled early game (1 -> 0.6)
				return 6f/7 * Math.Min(30, smallest.Take(3).Sum(s => s.Count)) / 30f + 1f/7;
			}
		}

		protected virtual Boolean ShouldBuy(Type type)
		{
			if (type == Cards.Universal.TypeClass.Curse)
				return false;
			return true;
		}

		protected Boolean ShouldBuy(Supply supply)
		{
			return this.ShouldBuy(supply.SupplyCardType);
		}

		protected virtual Boolean ShouldPlay(Card card)
		{
			if (!ShouldBuy(card.CardType))
				return false;

			return true;
		}

		/// <summary>
		/// Finds the best cards (based on whatever criteria is defined in the subclassed AI) of the given list
		/// </summary>
		/// <param name="cards">Cards to choose from</param>
		/// <param name="count">Number of cards to choose</param>
		/// <returns>IEnumerable list of cards that it thinks are the best</returns>
		protected virtual IEnumerable<Card> FindBestCards(IEnumerable<Card> cards, int count)
		{
			// Return the list at random
			CardCollection cardList = new CardCollection(cards);
			Utilities.Shuffler.Shuffle(cardList);
			return cardList.Take(count);
		}

		protected virtual IEnumerable<Card> FindBestCardsToDiscard(IEnumerable<Card> cards, int count)
		{
			// Return the list at random
			CardCollection cardList = new CardCollection(cards);
			Utilities.Shuffler.Shuffle(cardList);
			return cardList.Take(count);
		}

		protected virtual IEnumerable<Card> FindBestCardsToTrash(IEnumerable<Card> cards, int count)
		{
			// Return the list at random
			CardCollection cardList = new CardCollection(cards);
			Utilities.Shuffler.Shuffle(cardList);
			return cardList.Take(count);
		}

		protected virtual Supply FindBestCardForCost(IEnumerable<Supply> buyableSupplies, Currency currency, Boolean buying)
		{
			if (buyableSupplies.Count() == 0)
				return null;

			return buyableSupplies.ElementAt(this._Game.RNG.Next(buyableSupplies.Count()));
		}

		protected virtual Supply FindWorstCardForCost(IEnumerable<Supply> buyableSupplies, Currency currency)
		{
			if (buyableSupplies.Count() == 0)
				return null;

			return buyableSupplies.ElementAt(this._Game.RNG.Next(buyableSupplies.Count()));
		}

		public void player_Choose(Player player, Choice choice)
		{
			ChoiceResult result = null;

			Thread.Sleep(_SleepTime / 3);

			try
			{
				switch (choice.ChoiceType)
				{
					#region Options
					case ChoiceType.Options:
						result = this.ChooseOptions(choice);
						break;
					#endregion
					#region Cards
					case ChoiceType.Cards:
						result = this.ChooseCards(choice);
						break;
					#endregion
					#region Supply
					case ChoiceType.Supplies:
						result = this.ChooseSupply(choice);
						break;
					case ChoiceType.SuppliesAndCards:
						result = this.ChooseSuppliesCards(choice);
						break;
					#endregion

					default:
						throw new Exception("Don't know how to handle this choice type!");
				}

				if (choice.CardTriggers.Count > 0)
					_LastReactedCard = choice.CardTriggers[0];

				//return result;
				PlayerChoiceMessage pcm = new PlayerChoiceMessage(InternalWaitEvent, this.RealThis, result);
				pcm.Message = String.Format("{0} chooses", this.RealThis);
				lock (player.MessageRequestQueue)
					player.MessageRequestQueue.Enqueue(pcm);
				player.WaitEvent.Set();

				InternalWaitEvent.WaitOne();
				lock (player.MessageResponseQueue)
					player.MessageResponseQueue.Dequeue();
			}
			catch (Exception ex)
			{
				Utilities.Logging.LogError(ex);
				throw;
			}
		}

		private ChoiceResult ChooseOptions(Choice choice)
		{
			Type cardSourceType = choice.CardSource == null ? null : choice.CardSource.CardType;
			IEnumerable<Type> cardTriggerTypes = choice.CardTriggers.Select(ct => ct.CardType);

			// We're looking at a couple of different things -- mainly events
			if (cardSourceType == null)
			{
				#region TurnStarted Event
				if (choice.EventArgs is TurnStartedEventArgs)
				{
					TurnStartedEventArgs tsea = choice.EventArgs as TurnStartedEventArgs;
					return Decide_TurnStarted(choice, tsea, tsea.Actions.Keys);
				}
				#endregion
				#region CardBuy Event
				if (choice.EventArgs is CardBuyEventArgs)
				{
					CardBuyEventArgs cbea = choice.EventArgs as CardBuyEventArgs;
					return Decide_CardBuy(choice, cbea, cbea.Actions.Keys);
				}
				#endregion
				#region CardGain Event
				if (choice.EventArgs is CardGainEventArgs)
				{
					CardGainEventArgs cgea = choice.EventArgs as CardGainEventArgs;
					return Decide_CardGain(choice, cgea, cgea.Actions.Keys);
				}
				#endregion
				#region CardDiscard Event
				else if (choice.EventArgs is CardsDiscardEventArgs)
				{
					CardsDiscardEventArgs cdea = choice.EventArgs as CardsDiscardEventArgs;
					return Decide_CardsDiscard(choice, cdea, cdea.Actions.Keys);

				}
				#endregion
				#region CleaningUp Event
				else if (choice.EventArgs is CleaningUpEventArgs)
				{
					CleaningUpEventArgs cuea = choice.EventArgs as CleaningUpEventArgs;
					return Decide_CleaningUp(choice, cuea, cuea.Actions.Keys);
				}
				#endregion
				#region Trash Event
				if (choice.EventArgs is TrashEventArgs)
				{
					TrashEventArgs tea = choice.EventArgs as TrashEventArgs;
					return Decide_Trash(choice, tea, tea.Actions.Keys);
				}
				#endregion
			}

			// This needs to come first -- if this is a Bane Token card
			else if (cardTriggerTypes.Count() > 0 && cardTriggerTypes.ElementAt(0) == Cards.Cornucopia.TypeClass.YoungWitch &&
				cardSourceType != Cards.Cornucopia.TypeClass.YoungWitch &&
				choice.EventArgs is TokenActionEventArgs)
			{
				return Decide_RevealBane(choice);
			}
			#region Base cards
			else if (cardSourceType == Cards.Base.TypeClass.Chancellor)
			{
				return Decide_Chancellor(choice);
			}
			else if (cardSourceType == Cards.Base.TypeClass.Library)
			{
				return Decide_Library(choice);
			}
			else if (cardSourceType == Cards.Base.TypeClass.Spy)
			{
				return Decide_Spy(choice);
			}
			#endregion
			#region Intrigue cards
			else if (cardSourceType == Cards.Intrigue.TypeClass.Baron)
			{
				return Decide_Baron(choice);
			}
			else if (cardSourceType == Cards.Intrigue.TypeClass.MiningVillage)
			{
				return Decide_MiningVillage(choice);
			}
			else if (cardSourceType == Cards.Intrigue.TypeClass.Minion)
			{
				return Decide_Minion(choice);
			}
			else if (cardSourceType == Cards.Intrigue.TypeClass.Nobles)
			{
				return Decide_Nobles(choice);
			}
			else if (cardSourceType == Cards.Intrigue.TypeClass.Pawn)
			{
				return Decide_Pawn(choice);
			}
			else if (cardSourceType == Cards.Intrigue.TypeClass.Steward)
			{
				return Decide_Steward(choice);
			}
			else if (cardSourceType == Cards.Intrigue.TypeClass.Torturer)
			{
				return Decide_Torturer(choice);
			}
			#endregion
			#region Seaside cards
			else if (cardSourceType == Cards.Seaside.TypeClass.Ambassador)
			{
				return Decide_Ambassador(choice);
			}
			else if (cardSourceType == Cards.Seaside.TypeClass.Explorer)
			{
				return Decide_Explorer(choice);
			}
			else if (cardSourceType == Cards.Seaside.TypeClass.NativeVillage)
			{
				return Decide_NativeVillage(choice);
			}
			else if (cardSourceType == Cards.Seaside.TypeClass.Navigator)
			{
				return Decide_Navigator(choice);
			}
			else if (cardSourceType == Cards.Seaside.TypeClass.PearlDiver)
			{
				return Decide_PearlDiver(choice);
			}
			else if (cardSourceType == Cards.Seaside.TypeClass.PirateShip)
			{
				return Decide_PirateShip(choice);
			}
			#endregion
			#region Alchemy cards
			else if (cardSourceType == Cards.Alchemy.TypeClass.ScryingPool)
			{
				return Decide_ScryingPool(choice);
			}
			#endregion
			#region Prosperity cards
			else if (cardSourceType == Cards.Prosperity.TypeClass.CountingHouse)
			{
				return Decide_CountingHouse(choice);
			}
			else if (cardSourceType == Cards.Prosperity.TypeClass.Loan)
			{
				return Decide_Loan(choice);
			}
			else if (cardSourceType == Cards.Prosperity.TypeClass.Mountebank)
			{
				return Decide_Mountebank(choice);
			}
			else if (cardSourceType == Cards.Prosperity.TypeClass.Vault)
			{
				return Decide_Vault(choice);
			}
			else if (cardSourceType == Cards.Prosperity.TypeClass.Watchtower)
			{
				return Decide_Watchtower(choice);
			}
			#endregion
			#region Cornucopia cards
			else if (cardSourceType == Cards.Cornucopia.TypeClass.Jester)
			{
				return Decide_Jester(choice);
			}
			else if (cardSourceType == Cards.Cornucopia.TypeClass.Tournament)
			{
				return Decide_Tournament(choice);
			}
			else if (cardSourceType == Cards.Cornucopia.TypeClass.TrustySteed)
			{
				return Decide_TrustySteed(choice);
			}
			#endregion
			#region Hinterlands cards
			else if (cardSourceType == Cards.Hinterlands.TypeClass.Duchess)
			{
				return Decide_Duchess(choice);
			}
			else if (cardSourceType == Cards.Hinterlands.TypeClass.IllGottenGains)
			{
				return Decide_IllGottenGains(choice);
			}
			else if (cardSourceType == Cards.Hinterlands.TypeClass.JackOfAllTrades)
			{
				return Decide_JackOfAllTrades(choice);
			}
			else if (cardSourceType == Cards.Hinterlands.TypeClass.Oracle)
			{
				return Decide_Oracle(choice);
			}
			else if (cardSourceType == Cards.Hinterlands.TypeClass.SpiceMerchant)
			{
				return Decide_SpiceMerchant(choice);
			}
			#endregion
			#region Dark Ages cards
			else if (cardSourceType == Cards.DarkAges.TypeClass.Catacombs)
			{
				return Decide_Catacombs(choice);
			}
			else if (cardSourceType == Cards.DarkAges.TypeClass.Count)
			{
				return Decide_Count(choice);
			}
			else if (cardSourceType == Cards.DarkAges.TypeClass.Cultist)
			{
				return Decide_Cultist(choice);
			}
			else if (cardSourceType == Cards.DarkAges.TypeClass.Graverobber)
			{
				return Decide_Graverobber(choice);
			}
			else if (cardSourceType == Cards.DarkAges.TypeClass.HuntingGrounds)
			{
				return Decide_HuntingGrounds(choice);
			}
			else if (cardSourceType == Cards.DarkAges.TypeClass.Ironmonger)
			{
				return Decide_Ironmonger(choice);
			}
			else if (cardSourceType == Cards.DarkAges.TypeClass.Mercenary)
			{
				return Decide_Mercenary(choice);
			}
			else if (cardSourceType == Cards.DarkAges.TypeClass.Scavenger)
			{
				return Decide_Scavenger(choice);
			}
			else if (cardSourceType == Cards.DarkAges.TypeClass.Squire)
			{
				return Decide_Squire(choice);
			}
			else if (cardSourceType == Cards.DarkAges.TypeClass.Survivors)
			{
				return Decide_Survivors(choice);
			}
			else if (cardSourceType == Cards.DarkAges.TypeClass.Urchin)
			{
				return Decide_Urchin(choice);
			}
			#endregion
			#region Guilds cards
			else if (cardSourceType == Cards.Guilds.TypeClass.Butcher)
			{
				return Decide_Butcher(choice);
			}
			else if (cardSourceType == Cards.Guilds.TypeClass.Doctor)
			{
				return Decide_Doctor(choice);
			}
			else if (cardSourceType == Cards.Guilds.TypeClass.Herald)
			{
				return Decide_Herald(choice);
			}
			else if (cardSourceType == Cards.Guilds.TypeClass.Masterpiece)
			{
				return Decide_Masterpiece(choice);
			}
			else if (cardSourceType == Cards.Guilds.TypeClass.Stonemason)
			{
				return Decide_Stonemason(choice);
			}
			#endregion
			#region Promotional cards
			else if (cardSourceType == Cards.Promotional.TypeClass.Governor)
			{
				return Decide_Governor(choice);
			}
			else if (cardSourceType == Cards.Promotional.TypeClass.Prince)
			{
				return Decide_Prince(choice);
			}
			#endregion

			throw new NotImplementedException(String.Format("Oops, this decision hasn't been encountered yet! {0}: \"{1}\"", choice.CardSource, choice.Text));
		}
		private ChoiceResult ChooseCards(Choice choice)
		{
			Type cardSourceType = choice.CardSource == null ? null : choice.CardSource.CardType;
			IEnumerable<Type> cardTriggerTypes = choice.CardTriggers.Select(ct => ct.CardType);

			List<Card> choiceCards = new List<Card>(choice.Cards);

			// We're looking at a couple of different things -- initially just a Reaction
			if (cardSourceType == null)
			{
				#region Attacked Event
				if (choice.EventArgs is AttackedEventArgs)
				{
					AttackedEventArgs aea = choice.EventArgs as AttackedEventArgs;
					return Decide_Attacked(choice, aea, aea.Revealable.Keys);
				}
				#endregion
				else if (choice.Text == "Choose order of cards to put back on your deck")
				{
					return Decide_CardsReorder(choice);
				}
			}
			#region Base cards
			else if (cardSourceType == Cards.Base.TypeClass.Bureaucrat)
			{
				return Decide_Bureaucrat(choice);
			}
			else if (cardSourceType == Cards.Base.TypeClass.Cellar)
			{
				return Decide_Cellar(choice);
			}
			else if (cardSourceType == Cards.Base.TypeClass.Chapel)
			{
				return Decide_Chapel(choice);
			}
			else if (cardSourceType == Cards.Base.TypeClass.Militia)
			{
				return Decide_Militia(choice);
			}
			else if (cardSourceType == Cards.Base.TypeClass.Mine)
			{
				return Decide_Mine(choice);
			}
			else if (cardSourceType == Cards.Base.TypeClass.Remodel)
			{
				return Decide_Remodel(choice);
			}
			else if (cardSourceType == Cards.Base.TypeClass.Thief)
			{
				return Decide_Thief(choice);
			}
			else if (cardSourceType == Cards.Base.TypeClass.ThroneRoom)
			{
				return Decide_ThroneRoom(choice);
			}
			#endregion
			#region Intrigue cards
			else if (cardSourceType == Cards.Intrigue.TypeClass.Courtyard)
			{
				return Decide_Courtyard(choice);
			}
			else if (cardSourceType == Cards.Intrigue.TypeClass.Masquerade)
			{
				return Decide_Masquerade(choice);
			}
			else if (cardSourceType == Cards.Intrigue.TypeClass.Scout)
			{
				return Decide_Scout(choice);
			}
			else if (cardSourceType == Cards.Intrigue.TypeClass.SecretChamber)
			{
				return Decide_SecretChamber(choice);
			}
			else if (cardSourceType == Cards.Intrigue.TypeClass.Steward)
			{
				return Decide_Steward(choice);
			}
			else if (cardSourceType == Cards.Intrigue.TypeClass.Torturer)
			{
				return Decide_Torturer(choice);
			}
			else if (cardSourceType == Cards.Intrigue.TypeClass.TradingPost)
			{
				return Decide_TradingPost(choice);
			}
			else if (cardSourceType == Cards.Intrigue.TypeClass.Upgrade)
			{
				return Decide_Upgrade(choice);
			}
			#endregion
			#region Seaside cards
			else if (cardSourceType == Cards.Seaside.TypeClass.Ambassador)
			{
				return Decide_Ambassador(choice);
			}
			else if (cardSourceType == Cards.Seaside.TypeClass.GhostShip)
			{
				return Decide_GhostShip(choice);
			}
			else if (cardSourceType == Cards.Seaside.TypeClass.Haven)
			{
				return Decide_Haven(choice);
			}
			else if (cardSourceType == Cards.Seaside.TypeClass.Island)
			{
				return Decide_Island(choice);
			}
			else if (cardSourceType == Cards.Seaside.TypeClass.Lookout)
			{
				return Decide_Lookout(choice);
			}
			else if (cardSourceType == Cards.Seaside.TypeClass.Navigator)
			{
				return Decide_Navigator(choice);
			}
			else if (cardSourceType == Cards.Seaside.TypeClass.PirateShip)
			{
				return Decide_PirateShip(choice);
			}
			else if (cardSourceType == Cards.Seaside.TypeClass.Salvager)
			{
				return Decide_Salvager(choice);
			}
			else if (cardSourceType == Cards.Seaside.TypeClass.Smugglers)
			{
				return Decide_Smugglers(choice);
			}
			else if (cardSourceType == Cards.Seaside.TypeClass.Warehouse)
			{
				return Decide_Warehouse(choice);
			}
			#endregion
			#region Alchemy cards
			else if (cardSourceType == Cards.Alchemy.TypeClass.Apothecary)
			{
				return Decide_Apothecary(choice);
			}
			else if (cardSourceType == Cards.Alchemy.TypeClass.Apprentice)
			{
				return Decide_Apprentice(choice);
			}
			else if (cardSourceType == Cards.Alchemy.TypeClass.Golem)
			{
				return Decide_Golem(choice);
			}
			else if (cardSourceType == Cards.Alchemy.TypeClass.Herbalist)
			{
				return Decide_Herbalist(choice);
			}
			else if (cardSourceType == Cards.Alchemy.TypeClass.Transmute)
			{
				return Decide_Transmute(choice);
			}
			#endregion
			#region Prosperity cards
			else if (cardSourceType == Cards.Prosperity.TypeClass.Bishop)
			{
				return Decide_Bishop(choice);
			}
			else if (cardSourceType == Cards.Prosperity.TypeClass.Expand)
			{
				return Decide_Expand(choice);
			}
			else if (cardSourceType == Cards.Prosperity.TypeClass.Forge)
			{
				return Decide_Forge(choice);
			}
			else if (cardSourceType == Cards.Prosperity.TypeClass.Goons)
			{
				return Decide_Goons(choice);
			}
			else if (cardSourceType == Cards.Prosperity.TypeClass.KingsCourt)
			{
				return Decide_KingsCourt(choice);
			}
			else if (cardSourceType == Cards.Prosperity.TypeClass.Mint)
			{
				return Decide_Mint(choice);
			}
			else if (cardSourceType == Cards.Prosperity.TypeClass.Rabble)
			{
				return Decide_Rabble(choice);
			}
			else if (cardSourceType == Cards.Prosperity.TypeClass.TradeRoute)
			{
				return Decide_TradeRoute(choice);
			}
			else if (cardSourceType == Cards.Prosperity.TypeClass.Vault)
			{
				return Decide_Vault(choice);
			}
			#endregion
			#region Cornucopia cards
			else if (cardSourceType == Cards.Cornucopia.TypeClass.Followers)
			{
				return Decide_Followers(choice);
			}
			else if (cardSourceType == Cards.Cornucopia.TypeClass.Hamlet)
			{
				return Decide_Hamlet(choice);
			}
			else if (cardSourceType == Cards.Cornucopia.TypeClass.HorseTraders)
			{
				return Decide_HorseTraders(choice);
			}
			else if (cardSourceType == Cards.Cornucopia.TypeClass.Remake)
			{
				return Decide_Remake(choice);
			}
			else if (cardSourceType == Cards.Cornucopia.TypeClass.Tournament)
			{
				return Decide_Tournament(choice);
			}
			else if (cardSourceType == Cards.Cornucopia.TypeClass.YoungWitch)
			{
				return Decide_YoungWitch(choice);
			}
			#endregion
			#region Hinterlands cards
			else if (cardSourceType == Cards.Hinterlands.TypeClass.Cartographer)
			{
				return Decide_Cartographer(choice);
			}
			else if (cardSourceType == Cards.Hinterlands.TypeClass.Develop)
			{
				return Decide_Develop(choice);
			}
			else if (cardSourceType == Cards.Hinterlands.TypeClass.Embassy)
			{
				return Decide_Embassy(choice);
			}
			else if (cardSourceType == Cards.Hinterlands.TypeClass.Farmland)
			{
				return Decide_Farmland(choice);
			}
			else if (cardSourceType == Cards.Hinterlands.TypeClass.Inn)
			{
				return Decide_Inn(choice);
			}
			else if (cardSourceType == Cards.Hinterlands.TypeClass.JackOfAllTrades)
			{
				return Decide_JackOfAllTrades(choice);
			}
			else if (cardSourceType == Cards.Hinterlands.TypeClass.Mandarin)
			{
				return Decide_Mandarin(choice);
			}
			else if (cardSourceType == Cards.Hinterlands.TypeClass.Margrave)
			{
				return Decide_Margrave(choice);
			}
			else if (cardSourceType == Cards.Hinterlands.TypeClass.NobleBrigand)
			{
				return Decide_NobleBrigand(choice);
			}
			else if (cardSourceType == Cards.Hinterlands.TypeClass.Oasis)
			{
				return Decide_Oasis(choice);
			}
			else if (cardSourceType == Cards.Hinterlands.TypeClass.Oracle)
			{
				return Decide_Oracle(choice);
			}
			else if (cardSourceType == Cards.Hinterlands.TypeClass.Scheme)
			{
				return Decide_Scheme(choice);
			}
			else if (cardSourceType == Cards.Hinterlands.TypeClass.SpiceMerchant)
			{
				return Decide_SpiceMerchant(choice);
			}
			else if (cardSourceType == Cards.Hinterlands.TypeClass.Stables)
			{
				return Decide_Stables(choice);
			}
			else if (cardSourceType == Cards.Hinterlands.TypeClass.Trader)
			{
				return Decide_Trader(choice);
			}
			#endregion
			#region Dark Ages cards
			else if (cardSourceType == Cards.DarkAges.TypeClass.Altar)
			{
				return Decide_Altar(choice);
			}
			else if (cardSourceType == Cards.DarkAges.TypeClass.Count)
			{
				return Decide_Count(choice);
			}
			else if (cardSourceType == Cards.DarkAges.TypeClass.Counterfeit)
			{
				return Decide_Counterfeit(choice);
			}
			else if (cardSourceType == Cards.DarkAges.TypeClass.DameAnna)
			{
				return Decide_DameAnna(choice);
			}
			else if (cardSourceType == Cards.DarkAges.TypeClass.DameJosephine)
			{
				return Decide_DameJosephine(choice);
			}
			else if (cardSourceType == Cards.DarkAges.TypeClass.DameMolly)
			{
				return Decide_DameMolly(choice);
			}
			else if (cardSourceType == Cards.DarkAges.TypeClass.DameNatalie)
			{
				return Decide_DameNatalie(choice);
			}
			else if (cardSourceType == Cards.DarkAges.TypeClass.DameSylvia)
			{
				return Decide_DameSylvia(choice);
			}
			else if (cardSourceType == Cards.DarkAges.TypeClass.DeathCart)
			{
				return Decide_DeathCart(choice);
			}
			else if (cardSourceType == Cards.DarkAges.TypeClass.Forager)
			{
				return Decide_Forager(choice);
			}
			else if (cardSourceType == Cards.DarkAges.TypeClass.Graverobber)
			{
				return Decide_Graverobber(choice);
			}
			else if (cardSourceType == Cards.DarkAges.TypeClass.Hermit)
			{
				return Decide_Hermit(choice);
			}
			else if (cardSourceType == Cards.DarkAges.TypeClass.JunkDealer)
			{
				return Decide_JunkDealer(choice);
			}
			else if (cardSourceType == Cards.DarkAges.TypeClass.Mercenary)
			{
				return Decide_Mercenary(choice);
			}
			else if (cardSourceType == Cards.DarkAges.TypeClass.Pillage)
			{
				return Decide_Pillage(choice);
			}
			else if (cardSourceType == Cards.DarkAges.TypeClass.Procession)
			{
				return Decide_Procession(choice);
			}
			else if (cardSourceType == Cards.DarkAges.TypeClass.Rats)
			{
				return Decide_Rats(choice);
			}
			else if (cardSourceType == Cards.DarkAges.TypeClass.Rogue)
			{
				return Decide_Rogue(choice);
			}
			else if (cardSourceType == Cards.DarkAges.TypeClass.Scavenger)
			{
				return Decide_Scavenger(choice);
			}
			else if (cardSourceType == Cards.DarkAges.TypeClass.SirBailey)
			{
				return Decide_SirBailey(choice);
			}
			else if (cardSourceType == Cards.DarkAges.TypeClass.SirDestry)
			{
				return Decide_SirDestry(choice);
			}
			else if (cardSourceType == Cards.DarkAges.TypeClass.SirMartin)
			{
				return Decide_SirMartin(choice);
			}
			else if (cardSourceType == Cards.DarkAges.TypeClass.SirMichael)
			{
				return Decide_SirMichael(choice);
			}
			else if (cardSourceType == Cards.DarkAges.TypeClass.SirVander)
			{
				return Decide_SirVander(choice);
			}
			else if (cardSourceType == Cards.DarkAges.TypeClass.Storeroom)
			{
				return Decide_Storeroom(choice);
			}
			else if (cardSourceType == Cards.DarkAges.TypeClass.Survivors)
			{
				return Decide_Survivors(choice);
			}
			else if (cardSourceType == Cards.DarkAges.TypeClass.Urchin)
			{
				return Decide_Urchin(choice);
			}
			else if (cardSourceType == Cards.DarkAges.TypeClass.WanderingMinstrel)
			{
				return Decide_WanderingMinstrel(choice);
			}
			#endregion
			#region Guilds cards
			else if (cardSourceType == Cards.Guilds.TypeClass.Advisor)
			{
				return Decide_Advisor(choice);
			}
			else if (cardSourceType == Cards.Guilds.TypeClass.Butcher)
			{
				return Decide_Butcher(choice);
			}
			else if (cardSourceType == Cards.Guilds.TypeClass.Doctor)
			{
				return Decide_Doctor(choice);
			}
			else if (cardSourceType == Cards.Guilds.TypeClass.Herald)
			{
				return Decide_Herald(choice);
			}
			else if (cardSourceType == Cards.Guilds.TypeClass.Plaza)
			{
				return Decide_Plaza(choice);
			}
			else if (cardSourceType == Cards.Guilds.TypeClass.Stonemason)
			{
				return Decide_Stonemason(choice);
			}
			else if (cardSourceType == Cards.Guilds.TypeClass.Taxman)
			{
				return Decide_Taxman(choice);
			}
			#endregion
			#region Promotional cards
			else if (cardSourceType == Cards.Promotional.TypeClass.Envoy)
			{
				return Decide_Envoy(choice);
			}
			else if (cardSourceType == Cards.Promotional.TypeClass.Governor)
			{
				return Decide_Governor(choice);
			}
			else if (cardSourceType == Cards.Promotional.TypeClass.Prince)
			{
				return Decide_Prince(choice);
			}
			else if (cardSourceType == Cards.Promotional.TypeClass.Stash)
			{
				return Decide_Stash(choice);
			}
			#endregion

			throw new NotImplementedException(String.Format("Oops, this decision hasn't been encountered yet! {0}, {1}", choice.CardSource, choice.Text));
		}
		private ChoiceResult ChooseSupply(Choice choice)
		{
			Type cardSourceType = choice.CardSource == null ? null : choice.CardSource.CardType;
			IEnumerable<Type> cardTriggerTypes = choice.CardTriggers.Select(ct => ct.CardType);

			#region Base cards
			if (cardSourceType == Cards.Base.TypeClass.Feast)
			{
				return Decide_Feast(choice);
			}
			else if (cardSourceType == Cards.Base.TypeClass.Mine)
			{
				return Decide_Mine(choice);
			}
			else if (cardSourceType == Cards.Base.TypeClass.Remodel)
			{
				return Decide_Remodel(choice);
			}
			else if (cardSourceType == Cards.Base.TypeClass.Workshop)
			{
				return Decide_Workshop(choice);
			}
			#endregion
			#region Intrigue cards
			else if (cardSourceType == Cards.Intrigue.TypeClass.Ironworks)
			{
				return Decide_Ironworks(choice);
			}
			else if (cardSourceType == Cards.Intrigue.TypeClass.Saboteur)
			{
				return Decide_Saboteur(choice);
			}
			else if (cardSourceType == Cards.Intrigue.TypeClass.Swindler)
			{
				return Decide_Swindler(choice);
			}
			else if (cardSourceType == Cards.Intrigue.TypeClass.Upgrade)
			{
				return Decide_Upgrade(choice);
			}
			#endregion
			#region Seaside cards
			else if (cardSourceType == Cards.Seaside.TypeClass.Embargo)
			{
				return Decide_Embargo(choice);
			}
			#endregion
			#region Alchemy cards
			else if (cardSourceType == Cards.Alchemy.TypeClass.University)
			{
				return Decide_University(choice);
			}
			#endregion
			#region Prosperity cards
			else if (cardSourceType == Cards.Prosperity.TypeClass.Contraband)
			{
				return Decide_Contraband(choice);
			}
			else if (cardSourceType == Cards.Prosperity.TypeClass.Expand)
			{
				return Decide_Expand(choice);
			}
			else if (cardSourceType == Cards.Prosperity.TypeClass.Forge)
			{
				return Decide_Forge(choice);
			}
			#endregion
			#region Cornucopia cards
			else if (cardSourceType == Cards.Cornucopia.TypeClass.HornOfPlenty)
			{
				return Decide_HornOfPlenty(choice);
			}
			else if (cardSourceType == Cards.Cornucopia.TypeClass.Remake)
			{
				return Decide_Remake(choice);
			}
			#endregion
			#region Hinterlands cards
			else if (cardSourceType == Cards.Hinterlands.TypeClass.BorderVillage)
			{
				return Decide_BorderVillage(choice);
			}
			else if (cardSourceType == Cards.Hinterlands.TypeClass.Develop)
			{
				return Decide_Develop(choice);
			}
			else if (cardSourceType == Cards.Hinterlands.TypeClass.Farmland)
			{
				return Decide_Farmland(choice);
			}
			else if (cardSourceType == Cards.Hinterlands.TypeClass.Haggler)
			{
				return Decide_Haggler(choice);
			}
			#endregion
			#region Dark Ages cards
			else if (cardSourceType == Cards.DarkAges.TypeClass.Altar)
			{
				return Decide_Altar(choice);
			}
			else if (cardSourceType == Cards.DarkAges.TypeClass.Armory)
			{
				return Decide_Armory(choice);
			}
			else if (cardSourceType == Cards.DarkAges.TypeClass.Catacombs)
			{
				return Decide_Catacombs(choice);
			}
			else if (cardSourceType == Cards.DarkAges.TypeClass.DameNatalie)
			{
				return Decide_DameNatalie(choice);
			}
			else if (cardSourceType == Cards.DarkAges.TypeClass.Graverobber)
			{
				return Decide_Graverobber(choice);
			}
			else if (cardSourceType == Cards.DarkAges.TypeClass.Hermit)
			{
				return Decide_Hermit(choice);
			}
			else if (cardSourceType == Cards.DarkAges.TypeClass.Procession)
			{
				return Decide_Procession(choice);
			}
			else if (cardSourceType == Cards.DarkAges.TypeClass.Rebuild)
			{
				return Decide_Rebuild(choice);
			}
			else if (cardSourceType == Cards.DarkAges.TypeClass.Rogue)
			{
				return Decide_Rogue(choice);
			}
			else if (cardSourceType == Cards.DarkAges.TypeClass.Squire)
			{
				return Decide_Squire(choice);
			}
			#endregion
			#region Guilds cards
			else if (cardSourceType == Cards.Guilds.TypeClass.Butcher)
			{
				return Decide_Butcher(choice);
			}
			else if (cardSourceType == Cards.Guilds.TypeClass.Stonemason)
			{
				return Decide_Stonemason(choice);
			}
			else if (cardSourceType == Cards.Guilds.TypeClass.Taxman)
			{
				return Decide_Stonemason(choice);
			}
			#endregion
			#region Promotional cards
			else if (cardSourceType == Cards.Promotional.TypeClass.Governor)
			{
				return Decide_Governor(choice);
			}
			#endregion

			throw new NotImplementedException(String.Format("Oops, this decision hasn't been encountered yet! {0}, {1}", choice.CardSource, choice.Text));
		}
		private ChoiceResult ChooseSuppliesCards(Choice choice)
		{
			Type cardSourceType = choice.CardSource == null ? null : choice.CardSource.CardType;
			IEnumerable<Type> cardTriggerTypes = choice.CardTriggers.Select(ct => ct.CardType);

			#region Intrigue cards
			if (cardSourceType == Cards.Intrigue.TypeClass.WishingWell)
			{
				return Decide_WishingWell(choice);
			}
			#endregion
			#region Dark Ages cards
			else if (cardSourceType == Cards.DarkAges.TypeClass.Mystic)
			{
				return Decide_Mystic(choice);
			}
			else if (cardSourceType == Cards.DarkAges.TypeClass.Rebuild)
			{
				return Decide_Rebuild(choice);
			}
			#endregion
			#region Guilds cards
			else if (cardSourceType == Cards.Guilds.TypeClass.Doctor)
			{
				return Decide_Doctor(choice);
			}
			else if (cardSourceType == Cards.Guilds.TypeClass.Journeyman)
			{
				return Decide_Journeyman(choice);
			}
			#endregion

			throw new NotImplementedException(String.Format("Oops, this decision hasn't been encountered yet! {0}, {1}", choice.CardSource, choice.Text));
		}

		#region Decisions
		#region Events
		protected virtual ChoiceResult Decide_TurnStarted(Choice choice, TurnStartedEventArgs tsea, IEnumerable<String> cardTriggerTypes)
		{
			// Pick one at random
			if (cardTriggerTypes.Count() == 0)
				return null;

			return new ChoiceResult(new List<string>() { tsea.Actions[cardTriggerTypes.ElementAt(this._Game.RNG.Next(cardTriggerTypes.Count()))].Text });
		}
		protected virtual ChoiceResult Decide_CardBuy(Choice choice, CardBuyEventArgs cbea, IEnumerable<Type> cardTriggerTypes)
		{
			// Pick one at random
			if (cardTriggerTypes.Count() == 0)
				return null;

			return new ChoiceResult(new List<string>() { cbea.Actions[cardTriggerTypes.ElementAt(this._Game.RNG.Next(cardTriggerTypes.Count()))].Text });
		}
		protected virtual ChoiceResult Decide_CardGain(Choice choice, CardGainEventArgs cgea, IEnumerable<Type> cardTriggerTypes)
		{
			// If it's optional to reveal a card, have that option be one of the possibilities
			int index = this._Game.RNG.Next((choice.Minimum == 0 ? 1 : 0) + cardTriggerTypes.Count());

			// If it's optional, make the Count be selecting that optional one
			if (index >= cardTriggerTypes.Count())
				return new ChoiceResult(new List<string>());

			// Otherwise, just return which one was selected
			return new ChoiceResult(new List<string>() { cgea.Actions[cardTriggerTypes.ElementAt(index)].Text });
		}
		protected virtual ChoiceResult Decide_CardsDiscard(Choice choice, CardsDiscardEventArgs cdea, IEnumerable<Tuple<Type, Type>> cardTriggerTypes)
		{
			// If it's optional to reveal a card, have that option be one of the possibilities
			int index = this._Game.RNG.Next((choice.Minimum == 0 ? 1 : 0) + cardTriggerTypes.Count());

			// If it's optional, make the Count be selecting that optional one
			if (index >= cardTriggerTypes.Count())
				return new ChoiceResult(new List<string>());

			// Otherwise, just return which one was selected
			return new ChoiceResult(new List<string>() { cdea.Actions[cardTriggerTypes.ElementAt(index)].Text });
		}
		protected virtual ChoiceResult Decide_CleaningUp(Choice choice, CleaningUpEventArgs cuea, IEnumerable<Type> cardTriggerTypes)
		{
			// If it's optional to reveal a card, have that option be one of the possibilities
			int index = this._Game.RNG.Next((choice.Minimum == 0 ? 1 : 0) + cardTriggerTypes.Count());

			// If it's optional, make the Count be selecting that optional one
			if (index >= cardTriggerTypes.Count())
				return new ChoiceResult(new List<string>());

			// Otherwise, just return which one was selected
			return new ChoiceResult(new List<string>() { cuea.Actions[cardTriggerTypes.ElementAt(index)].Text });
		}
		protected virtual ChoiceResult Decide_Trash(Choice choice, TrashEventArgs tea, IEnumerable<Type> cardTriggerTypes)
		{
			// Pick one at random
			if (cardTriggerTypes.Count() == 0)
				return null;

			return new ChoiceResult(new List<string>() { tea.Actions[cardTriggerTypes.ElementAt(this._Game.RNG.Next(cardTriggerTypes.Count()))].Text });
		}
		protected virtual ChoiceResult Decide_Attacked(Choice choice, AttackedEventArgs aea, IEnumerable<Type> cardsToReveal)
		{
			// If it's optional to reveal a card, have that option be one of the possibilities
			int index = this._Game.RNG.Next((choice.Minimum == 0 ? 1 : 0) + cardsToReveal.Count());

			// If it's optional, make the Count be selecting that optional one
			if (index >= cardsToReveal.Count())
				return new ChoiceResult(new CardCollection());

			// Otherwise, just return which one was selected
			return new ChoiceResult(new CardCollection() { aea.Revealable[cardsToReveal.ElementAt(index)].Card });
		}
		#endregion
		#region Cards
		#region All
		protected virtual ChoiceResult Decide_CardsReorder(Choice choice)
		{
			return Decide_Random(choice);
		}
		private ChoiceResult Decide_Random(Choice choice)
		{
			int numChoices;
			switch (choice.ChoiceType)
			{
				case ChoiceType.Options:
					numChoices = this._Game.RNG.Next(choice.Minimum, (choice.Options.Count < choice.Maximum ? choice.Options.Count : choice.Maximum) + 1);
					List<String> choices = new List<string>(choice.Options.Select(o => o.Text));
					Utilities.Shuffler.Shuffle(choices);
					return new ChoiceResult(new List<String>(choices.Take(numChoices)));

				case ChoiceType.Cards:
					CardCollection choiceCards = new CardCollection(choice.Cards.Where(c => c.CardType != Cards.Universal.TypeClass.Dummy));
					numChoices = this._Game.RNG.Next(choice.Minimum, (choiceCards.Count < choice.Maximum ? choiceCards.Count : choice.Maximum) + 1);
					Utilities.Shuffler.Shuffle(choiceCards);
					return new ChoiceResult(new CardCollection(choiceCards.Take(numChoices)));

				case ChoiceType.Supplies:
					return new ChoiceResult(choice.Supplies.ElementAt(this._Game.RNG.Next(choice.Supplies.Count)).Value);

				case ChoiceType.SuppliesAndCards:
					int index = this._Game.RNG.Next(choice.Supplies.Count + choice.Cards.Count());
					if (index >= choice.Supplies.Count)
						return new ChoiceResult(new CardCollection { choice.Cards.ElementAt(index - choice.Supplies.Count) });
					return new ChoiceResult(choice.Supplies.ElementAt(index).Value);
			}

			throw new NotImplementedException("Still need random choices defined!");
		}
		#endregion
		#region Special (Bane)
		protected virtual ChoiceResult Decide_RevealBane(Choice choice)
		{
			return Decide_Random(choice);
		}
		#endregion
		#region Base
		protected virtual ChoiceResult Decide_Bureaucrat(Choice choice)
		{
			return Decide_Random(choice);
		}
		protected virtual ChoiceResult Decide_Cellar(Choice choice)
		{
			return Decide_Random(choice);
		}
		protected virtual ChoiceResult Decide_Chancellor(Choice choice)
		{
			return Decide_Random(choice);
		}
		protected virtual ChoiceResult Decide_Chapel(Choice choice)
		{
			return Decide_Random(choice);
		}
		protected virtual ChoiceResult Decide_Feast(Choice choice)
		{
			return Decide_Random(choice);
		}
		protected virtual ChoiceResult Decide_Library(Choice choice)
		{
			return Decide_Random(choice);
		}
		protected virtual ChoiceResult Decide_Militia(Choice choice)
		{
			return Decide_Random(choice);
		}
		protected virtual ChoiceResult Decide_Mine(Choice choice)
		{
			return Decide_Random(choice);
		}
		protected virtual ChoiceResult Decide_Remodel(Choice choice)
		{
			return Decide_Random(choice);
		}
		protected virtual ChoiceResult Decide_Spy(Choice choice)
		{
			return Decide_Random(choice);
		}
		protected virtual ChoiceResult Decide_Thief(Choice choice)
		{
			return Decide_Random(choice);
		}
		protected virtual ChoiceResult Decide_ThroneRoom(Choice choice)
		{
			return Decide_Random(choice);
		}
		protected virtual ChoiceResult Decide_Workshop(Choice choice)
		{
			return Decide_Random(choice);
		}
		#endregion
		#region Intrigue
		protected virtual ChoiceResult Decide_Baron(Choice choice)
		{
			return Decide_Random(choice);
		}
		protected virtual ChoiceResult Decide_Courtyard(Choice choice)
		{
			return Decide_Random(choice);
		}
		protected virtual ChoiceResult Decide_Ironworks(Choice choice)
		{
			return Decide_Random(choice);
		}
		protected virtual ChoiceResult Decide_Masquerade(Choice choice)
		{
			return Decide_Random(choice);
		}
		protected virtual ChoiceResult Decide_MiningVillage(Choice choice)
		{
			return Decide_Random(choice);
		}
		protected virtual ChoiceResult Decide_Minion(Choice choice)
		{
			return Decide_Random(choice);
		}
		protected virtual ChoiceResult Decide_Nobles(Choice choice)
		{
			return Decide_Random(choice);
		}
		protected virtual ChoiceResult Decide_Pawn(Choice choice)
		{
			return Decide_Random(choice);
		}
		protected virtual ChoiceResult Decide_Saboteur(Choice choice)
		{
			return Decide_Random(choice);
		}
		protected virtual ChoiceResult Decide_Scout(Choice choice)
		{
			return Decide_Random(choice);
		}
		protected virtual ChoiceResult Decide_SecretChamber(Choice choice)
		{
			return Decide_Random(choice);
		}
		protected virtual ChoiceResult Decide_Steward(Choice choice)
		{
			return Decide_Random(choice);
		}
		protected virtual ChoiceResult Decide_Swindler(Choice choice)
		{
			return Decide_Random(choice);
		}
		protected virtual ChoiceResult Decide_Torturer(Choice choice)
		{
			return Decide_Random(choice);
		}
		protected virtual ChoiceResult Decide_TradingPost(Choice choice)
		{
			return Decide_Random(choice);
		}
		protected virtual ChoiceResult Decide_Upgrade(Choice choice)
		{
			return Decide_Random(choice);
		}
		protected virtual ChoiceResult Decide_WishingWell(Choice choice)
		{
			return Decide_Random(choice);
		}
		#endregion
		#region Seaside
		protected virtual ChoiceResult Decide_Ambassador(Choice choice)
		{
			return Decide_Random(choice);
		}
		protected virtual ChoiceResult Decide_Embargo(Choice choice)
		{
			return Decide_Random(choice);
		}
		protected virtual ChoiceResult Decide_Explorer(Choice choice)
		{
			return Decide_Random(choice);
		}
		protected virtual ChoiceResult Decide_GhostShip(Choice choice)
		{
			return Decide_Random(choice);
		}
		protected virtual ChoiceResult Decide_Haven(Choice choice)
		{
			return Decide_Random(choice);
		}
		protected virtual ChoiceResult Decide_Island(Choice choice)
		{
			return Decide_Random(choice);
		}
		protected virtual ChoiceResult Decide_Lookout(Choice choice)
		{
			return Decide_Random(choice);
		}
		protected virtual ChoiceResult Decide_NativeVillage(Choice choice)
		{
			return Decide_Random(choice);
		}
		protected virtual ChoiceResult Decide_Navigator(Choice choice)
		{
			return Decide_Random(choice);
		}
		protected virtual ChoiceResult Decide_PearlDiver(Choice choice)
		{
			return Decide_Random(choice);
		}
		protected virtual ChoiceResult Decide_PirateShip(Choice choice)
		{
			return Decide_Random(choice);
		}
		protected virtual ChoiceResult Decide_Salvager(Choice choice)
		{
			return Decide_Random(choice);
		}
		protected virtual ChoiceResult Decide_Smugglers(Choice choice)
		{
			return Decide_Random(choice);
		}
		protected virtual ChoiceResult Decide_Warehouse(Choice choice)
		{
			return Decide_Random(choice);
		}
		#endregion
		#region Alchemy
		protected virtual ChoiceResult Decide_Apothecary(Choice choice)
		{
			return Decide_Random(choice);
		}
		protected virtual ChoiceResult Decide_Apprentice(Choice choice)
		{
			return Decide_Random(choice);
		}
		protected virtual ChoiceResult Decide_Golem(Choice choice)
		{
			return Decide_Random(choice);
		}
		protected virtual ChoiceResult Decide_Herbalist(Choice choice)
		{
			return Decide_Random(choice);
		}
		protected virtual ChoiceResult Decide_ScryingPool(Choice choice)
		{
			return Decide_Random(choice);
		}
		protected virtual ChoiceResult Decide_Transmute(Choice choice)
		{
			return Decide_Random(choice);
		}
		protected virtual ChoiceResult Decide_University(Choice choice)
		{
			return Decide_Random(choice);
		}
		#endregion
		#region Prosperity
		protected virtual ChoiceResult Decide_Bishop(Choice choice)
		{
			return Decide_Random(choice);
		}
		protected virtual ChoiceResult Decide_Contraband(Choice choice)
		{
			return Decide_Random(choice);
		}
		protected virtual ChoiceResult Decide_CountingHouse(Choice choice)
		{
			return Decide_Random(choice);
		}
		protected virtual ChoiceResult Decide_Expand(Choice choice)
		{
			return Decide_Random(choice);
		}
		protected virtual ChoiceResult Decide_Forge(Choice choice)
		{
			return Decide_Random(choice);
		}
		protected virtual ChoiceResult Decide_Goons(Choice choice)
		{
			return Decide_Random(choice);
		}
		protected virtual ChoiceResult Decide_KingsCourt(Choice choice)
		{
			return Decide_Random(choice);
		}
		protected virtual ChoiceResult Decide_Loan(Choice choice)
		{
			return Decide_Random(choice);
		}
		protected virtual ChoiceResult Decide_Mint(Choice choice)
		{
			return Decide_Random(choice);
		}
		protected virtual ChoiceResult Decide_Mountebank(Choice choice)
		{
			return Decide_Random(choice);
		}
		protected virtual ChoiceResult Decide_Rabble(Choice choice)
		{
			return Decide_Random(choice);
		}
		protected virtual ChoiceResult Decide_TradeRoute(Choice choice)
		{
			return Decide_Random(choice);
		}
		protected virtual ChoiceResult Decide_Vault(Choice choice)
		{
			return Decide_Random(choice);
		}
		protected virtual ChoiceResult Decide_Watchtower(Choice choice)
		{
			return Decide_Random(choice);
		}
		#endregion
		#region Cornucopia
		protected virtual ChoiceResult Decide_Followers(Choice choice)
		{
			return Decide_Random(choice);
		}
		protected virtual ChoiceResult Decide_Hamlet(Choice choice)
		{
			return Decide_Random(choice);
		}
		protected virtual ChoiceResult Decide_HornOfPlenty(Choice choice)
		{
			return Decide_Random(choice);
		}
		protected virtual ChoiceResult Decide_HorseTraders(Choice choice)
		{
			return Decide_Random(choice);
		}
		protected virtual ChoiceResult Decide_Jester(Choice choice)
		{
			return Decide_Random(choice);
		}
		protected virtual ChoiceResult Decide_Remake(Choice choice)
		{
			return Decide_Random(choice);
		}
		protected virtual ChoiceResult Decide_Tournament(Choice choice)
		{
			return Decide_Random(choice);
		}
		protected virtual ChoiceResult Decide_TrustySteed(Choice choice)
		{
			return Decide_Random(choice);
		}
		protected virtual ChoiceResult Decide_YoungWitch(Choice choice)
		{
			return Decide_Random(choice);
		}
		#endregion
		#region Hinterlands
		protected virtual ChoiceResult Decide_BorderVillage(Choice choice)
		{
			return Decide_Random(choice);
		}
		protected virtual ChoiceResult Decide_Cartographer(Choice choice)
		{
			return Decide_Random(choice);
		}
		protected virtual ChoiceResult Decide_Develop(Choice choice)
		{
			return Decide_Random(choice);
		}
		protected virtual ChoiceResult Decide_Duchess(Choice choice)
		{
			return Decide_Random(choice);
		}
		protected virtual ChoiceResult Decide_Embassy(Choice choice)
		{
			return Decide_Random(choice);
		}
		protected virtual ChoiceResult Decide_Farmland(Choice choice)
		{
			return Decide_Random(choice);
		}
		protected virtual ChoiceResult Decide_Haggler(Choice choice)
		{
			return Decide_Random(choice);
		}
		protected virtual ChoiceResult Decide_IllGottenGains(Choice choice)
		{
			return Decide_Random(choice);
		}
		protected virtual ChoiceResult Decide_Inn(Choice choice)
		{
			return Decide_Random(choice);
		}
		protected virtual ChoiceResult Decide_JackOfAllTrades(Choice choice)
		{
			return Decide_Random(choice);
		}
		protected virtual ChoiceResult Decide_Mandarin(Choice choice)
		{
			return Decide_Random(choice);
		}
		protected virtual ChoiceResult Decide_Margrave(Choice choice)
		{
			return Decide_Random(choice);
		}
		protected virtual ChoiceResult Decide_NobleBrigand(Choice choice)
		{
			return Decide_Random(choice);
		}
		protected virtual ChoiceResult Decide_Oasis(Choice choice)
		{
			return Decide_Random(choice);
		}
		protected virtual ChoiceResult Decide_Oracle(Choice choice)
		{
			return Decide_Random(choice);
		}
		protected virtual ChoiceResult Decide_Scheme(Choice choice)
		{
			return Decide_Random(choice);
		}
		protected virtual ChoiceResult Decide_SpiceMerchant(Choice choice)
		{
			return Decide_Random(choice);
		}
		protected virtual ChoiceResult Decide_Stables(Choice choice)
		{
			return Decide_Random(choice);
		}
		protected virtual ChoiceResult Decide_Trader(Choice choice)
		{
			return Decide_Random(choice);
		}
		#endregion
		#region Dark Ages
		protected virtual ChoiceResult Decide_Altar(Choice choice)
		{
			return Decide_Random(choice);
		}
		protected virtual ChoiceResult Decide_Armory(Choice choice)
		{
			return Decide_Random(choice);
		}
		protected virtual ChoiceResult Decide_Catacombs(Choice choice)
		{
			return Decide_Random(choice);
		}
		protected virtual ChoiceResult Decide_Count(Choice choice)
		{
			return Decide_Random(choice);
		}
		protected virtual ChoiceResult Decide_Counterfeit(Choice choice)
		{
			return Decide_Random(choice);
		}
		protected virtual ChoiceResult Decide_Cultist(Choice choice)
		{
			return Decide_Random(choice);
		}
		protected virtual ChoiceResult Decide_DameAnna(Choice choice)
		{
			return Decide_Random(choice);
		}
		protected virtual ChoiceResult Decide_DameJosephine(Choice choice)
		{
			return Decide_Random(choice);
		}
		protected virtual ChoiceResult Decide_DameMolly(Choice choice)
		{
			return Decide_Random(choice);
		}
		protected virtual ChoiceResult Decide_DameNatalie(Choice choice)
		{
			return Decide_Random(choice);
		}
		protected virtual ChoiceResult Decide_DameSylvia(Choice choice)
		{
			return Decide_Random(choice);
		}
		protected virtual ChoiceResult Decide_DeathCart(Choice choice)
		{
			return Decide_Random(choice);
		}
		protected virtual ChoiceResult Decide_Forager(Choice choice)
		{
			return Decide_Random(choice);
		}
		protected virtual ChoiceResult Decide_Graverobber(Choice choice)
		{
			return Decide_Random(choice);
		}
		protected virtual ChoiceResult Decide_Hermit(Choice choice)
		{
			return Decide_Random(choice);
		}
		protected virtual ChoiceResult Decide_HuntingGrounds(Choice choice)
		{
			return Decide_Random(choice);
		}
		protected virtual ChoiceResult Decide_Ironmonger(Choice choice)
		{
			return Decide_Random(choice);
		}
		protected virtual ChoiceResult Decide_JunkDealer(Choice choice)
		{
			return Decide_Random(choice);
		}
		protected virtual ChoiceResult Decide_Mercenary(Choice choice)
		{
			return Decide_Random(choice);
		}
		protected virtual ChoiceResult Decide_Mystic(Choice choice)
		{
			return Decide_Random(choice);
		}
		protected virtual ChoiceResult Decide_Pillage(Choice choice)
		{
			return Decide_Random(choice);
		}
		protected virtual ChoiceResult Decide_Procession(Choice choice)
		{
			return Decide_Random(choice);
		}
		protected virtual ChoiceResult Decide_Rats(Choice choice)
		{
			return Decide_Random(choice);
		}
		protected virtual ChoiceResult Decide_Rebuild(Choice choice)
		{
			return Decide_Random(choice);
		}
		protected virtual ChoiceResult Decide_Rogue(Choice choice)
		{
			return Decide_Random(choice);
		}
		protected virtual ChoiceResult Decide_Scavenger(Choice choice)
		{
			return Decide_Random(choice);
		}
		protected virtual ChoiceResult Decide_SirBailey(Choice choice)
		{
			return Decide_Random(choice);
		}
		protected virtual ChoiceResult Decide_SirDestry(Choice choice)
		{
			return Decide_Random(choice);
		}
		protected virtual ChoiceResult Decide_SirMartin(Choice choice)
		{
			return Decide_Random(choice);
		}
		protected virtual ChoiceResult Decide_SirMichael(Choice choice)
		{
			return Decide_Random(choice);
		}
		protected virtual ChoiceResult Decide_SirVander(Choice choice)
		{
			return Decide_Random(choice);
		}
		protected virtual ChoiceResult Decide_Storeroom(Choice choice)
		{
			return Decide_Random(choice);
		}
		protected virtual ChoiceResult Decide_Squire(Choice choice)
		{
			return Decide_Random(choice);
		}
		protected virtual ChoiceResult Decide_Survivors(Choice choice)
		{
			return Decide_Random(choice);
		}
		protected virtual ChoiceResult Decide_Urchin(Choice choice)
		{
			return Decide_Random(choice);
		}
		protected virtual ChoiceResult Decide_WanderingMinstrel(Choice choice)
		{
			return Decide_Random(choice);
		}
		#endregion
		#region Guilds
		protected virtual ChoiceResult Decide_Advisor(Choice choice)
		{
			return Decide_Random(choice);
		}
		protected virtual ChoiceResult Decide_Butcher(Choice choice)
		{
			return Decide_Random(choice);
		}
		protected virtual ChoiceResult Decide_Doctor(Choice choice)
		{
			return Decide_Random(choice);
		}
		protected virtual ChoiceResult Decide_Herald(Choice choice)
		{
			return Decide_Random(choice);
		}
		protected virtual ChoiceResult Decide_Journeyman(Choice choice)
		{
			return Decide_Random(choice);
		}
		protected virtual ChoiceResult Decide_Masterpiece(Choice choice)
		{
			return Decide_Random(choice);
		}
		protected virtual ChoiceResult Decide_Plaza(Choice choice)
		{
			return Decide_Random(choice);
		}
		protected virtual ChoiceResult Decide_Stonemason(Choice choice)
		{
			return Decide_Random(choice);
		}
		protected virtual ChoiceResult Decide_Taxman(Choice choice)
		{
			return Decide_Random(choice);
		}
		#endregion
		#region Promotional
		protected virtual ChoiceResult Decide_Envoy(Choice choice)
		{
			return Decide_Random(choice);
		}
		protected virtual ChoiceResult Decide_Governor(Choice choice)
		{
			return Decide_Random(choice);
		}
		protected virtual ChoiceResult Decide_Prince(Choice choice)
		{
			return Decide_Random(choice);
		}
		protected virtual ChoiceResult Decide_Stash(Choice choice)
		{
			return Decide_Random(choice);
		}
		#endregion
		#endregion
		#endregion
	}
}
