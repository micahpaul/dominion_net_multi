using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;

using DominionBase.Cards;
using DominionBase.Piles;

namespace DominionBase.Players.AI
{
	public class Standard : Basic
	{
		public new static String AIName { get { return "Standard"; } }
		public new static String AIDescription { get { return "Baseline performance AI that makes as good of decisions as it can, but has no strong focus for buying."; } }

		public static Boolean IsDownloading = false;

		// These are good starting points for creating a "learning" AI
		private double potionLikelihood = 1f;
		private double talismanLikelihood = 1f;

		private Dictionary<Guid, CardCollection> KnownPlayerHands = new Dictionary<Guid, CardCollection>();

		public Standard(Game game, String name) : base(game, name) { }
		public Standard(Game game, String name, Player realThis) : base(game, name, realThis) { }

		internal override void Setup(Game game, Player realThis)
		{
			base.Setup(game, realThis);

			potionLikelihood += Utilities.Gaussian.NextGaussian(this._Game.RNG) / 10;
			talismanLikelihood += Utilities.Gaussian.NextGaussian(this._Game.RNG) * 3 / 4;

			Console.WriteLine(String.Format("Talisman likelihood: {0}", talismanLikelihood));

			foreach (Player player in game.Players)
			{
				// Skip myself -- we already know 100% info & don't care anyway
				if (player == this.RealThis)
					continue;

				KnownPlayerHands[player.UniqueId] = new CardCollection();
				player.TurnEnded += new TurnEndedEventHandler(otherPlayer_TurnEnded);
				player.CardsDiscarded += new CardsDiscardedEventHandler(otherPlayer_CardsDiscarded);
				player.Revealed.PileChanged += new Pile.PileChangedEventHandler(otherPlayer_CardsRevealed_PileChanged);
				player.Hand.PileChanged += new Pile.PileChangedEventHandler(otherPlayer_CardsHand_PileChanged);
			}
		}

		internal override void EndgameTriggered()
		{
			base.EndgameTriggered();

#if DEBUG
			if (this.RealThis._Game.State == GameState.Ended)
			{
				// Check to see if Talisman was used in this game and then record how well we did to a file
				Supply talismanSupply = this.RealThis._Game.Table.FindSupplyPileByCardType(Cards.Prosperity.TypeClass.Talisman, true);
				if (talismanSupply != null)
				{
					int winning_points = this.RealThis._Game.Winners.First().VictoryPoints;
					double point_percentage = this.RealThis.VictoryPoints / (double)winning_points;
					double weight_factor = 1 - (winning_points - this.RealThis.VictoryPoints) / (double)winning_points;
					while (true)
					{
						try
						{
							File.AppendAllText(System.IO.Path.Combine(Utilities.Application.ApplicationPath, "talisman.log"), String.Format("Won?: {0}, Score: {1}/{2}, Percentage: {3:p}, Weight Factor: {4}, Talisman Likelihood: {5}{6}",
								this.RealThis._Game.Winners.Contains(this.RealThis),
								this.RealThis.VictoryPoints,
								winning_points,
								point_percentage,
								weight_factor,
								this.talismanLikelihood,
								System.Environment.NewLine));

							break;
						}
						catch
						{
							Thread.Sleep((new Random()).Next(250, 750));
						}
					}
				}
			}
#endif
		}

		internal override void TearDown()
		{
			base.TearDown();

			foreach (Player player in this.RealThis._Game.Players)
			{
				if (player == this.RealThis)
					continue;
				player.TurnEnded -= new TurnEndedEventHandler(otherPlayer_TurnEnded);
				player.CardsDiscarded -= new CardsDiscardedEventHandler(otherPlayer_CardsDiscarded);
				player.Revealed.PileChanged -= new Pile.PileChangedEventHandler(otherPlayer_CardsRevealed_PileChanged);
				player.Hand.PileChanged -= new Pile.PileChangedEventHandler(otherPlayer_CardsHand_PileChanged);
			}
		}

		void otherPlayer_TurnEnded(object sender, TurnEndedEventArgs e)
		{
			KnownPlayerHands[e.Player.UniqueId] = new CardCollection();
		}

		void otherPlayer_CardsRevealed_PileChanged(object sender, PileChangedEventArgs e)
		{
			KnownPlayerHands[e.Player.PlayerUniqueId].AddRange(e.AddedCards);
		}

		void otherPlayer_CardsHand_PileChanged(object sender, PileChangedEventArgs e)
		{
			if (e.OperationPerformed == PileChangedEventArgs.Operation.Added)
			{
				// We can add red-backed cards (Stash) to the known cards when added to a player's hand
				KnownPlayerHands[e.Player.PlayerUniqueId].AddRange(e.AddedCards.Where(c => c.CardBack == CardBack.Red));
			}
			else if (e.OperationPerformed == PileChangedEventArgs.Operation.Removed)
			{
				foreach (Card card in e.RemovedCards.Where(c => c.CardBack == CardBack.Red))
				{
					if (KnownPlayerHands[e.Player.PlayerUniqueId].Contains(card))
						KnownPlayerHands[e.Player.PlayerUniqueId].Remove(card);
				}
			}
		}

		void otherPlayer_CardsDiscarded(object sender, CardsDiscardEventArgs e)
		{
			if (!KnownPlayerHands.ContainsKey(((Player)sender).UniqueId))
				return;
			// We shouldn't cheat -- only the last card should be visible
			Card lastCard = e.Cards.LastOrDefault();
			if (lastCard == null)
				return;
			// Remove the card if it's found in our list of cards that we know about
			Card foundCard = KnownPlayerHands[((Player)sender).UniqueId].FirstOrDefault(c => c.Name == lastCard.Name);
			if (foundCard != null)
				KnownPlayerHands[((Player)sender).UniqueId].Remove(foundCard);
		}

		protected override void PlayTreasure()
		{
			if ((this.RealThis.Phase == PhaseEnum.ActionTreasure || this.RealThis.Phase == PhaseEnum.BuyTreasure) &&
				 this.RealThis.PlayerMode == Players.PlayerMode.Normal)
			{
				Thread.Sleep(_SleepTime);
				CardCollection nextTreasures = this.FindBestCardsToPlay(this.RealThis.Hand[Category.Treasure]);
				if (nextTreasures.Count > 0)
				{
					this.RealThis.PlayCards(nextTreasures);
				}
				else if (this.RealThis.TokenPiles.ContainsKey(Cards.Guilds.TypeClass.CoinToken) &&
					this.RealThis.TokenPiles[Cards.Guilds.TypeClass.CoinToken].Count > 0 && 
					this.RealThis.Phase == PhaseEnum.BuyTreasure)
				{
					int coinTokens = this.RealThis.TokenPiles[Cards.Guilds.TypeClass.CoinToken].Count;
					int currentCurrency = this.RealThis.Currency.Coin.Value;
					Boolean colonyGainable = this._Game.Table.Supplies.ContainsKey(Cards.Prosperity.TypeClass.Colony) &&
						this._Game.Table.Supplies[Cards.Prosperity.TypeClass.Colony].CanBuy(this.RealThis, new Currency(this._Game.Table.Supplies[Cards.Prosperity.TypeClass.Colony].CurrentCost));
					Boolean provinceGainable = this._Game.Table.Province.CanBuy(this.RealThis, new Currency(this._Game.Table.Province.CurrentCost));

					int spendCoinTokens = 0;

					// Make slightly better decisions about spending coins vs. not
					// If we can gain a Colony by spending 1 coin, do so!
					if (currentCurrency == 10 && colonyGainable)
						spendCoinTokens = 1;
					// Less excitingly, if we can gain a Province by spending 1 coin, do so!
					else if (currentCurrency == 7 && provinceGainable)
						spendCoinTokens = 1;
					// If it's getting later in the game and we have enough Coin tokens to get a Colony, DO IT!
					else if (this.GameProgress < 0.75 && colonyGainable && currentCurrency + coinTokens >= this._Game.Table.Supplies[Cards.Prosperity.TypeClass.Colony].CurrentCost.Coin.Value)
						spendCoinTokens = this._Game.Table.Supplies[Cards.Prosperity.TypeClass.Colony].CurrentCost.Coin.Value - currentCurrency;
					// If it's getting later in the game and we have enough Coin tokens to get a Province, DO IT!
					else if (this.GameProgress < 0.50 && provinceGainable && currentCurrency + coinTokens >= this._Game.Table.Province.CurrentCost.Coin.Value)
						spendCoinTokens = this._Game.Table.Province.CurrentCost.Coin.Value - currentCurrency;

					if (spendCoinTokens == 0)
					{
						double previous_best_score = -1.0;
						List<Supply> buyableSupplies = new List<Supply>();
						for (int coinTokenCount = 0; coinTokenCount <= coinTokens; coinTokenCount++)
						{
							buyableSupplies.Clear();
							foreach (Supply supply in this.RealThis._Game.Table.Supplies.Values)
							{
								if (supply.CanBuy(this.RealThis, this.RealThis.Currency + new Currencies.Coin(coinTokenCount)) && this.ShouldBuy(supply))
									buyableSupplies.Add(supply);
							}

							Dictionary<double, List<Supply>> scores = this.ValuateCardsToBuy(buyableSupplies);

							double bestScore = scores.Keys.OrderByDescending(k => k).FirstOrDefault();
							if (bestScore > 0 && (previous_best_score < 0 || bestScore > (coinTokenCount - spendCoinTokens + 1) + previous_best_score))
							{
								spendCoinTokens = coinTokenCount;
								previous_best_score = bestScore;
							}
						}
					}


					if (spendCoinTokens > 0)
						this.RealThis.PlayTokens(this._Game, Cards.Guilds.TypeClass.CoinToken, spendCoinTokens);
					// Otherwise, we'll just save them for now.  This isn't great, but it's better than a kick to the head.

					if (this.RealThis.Phase != PhaseEnum.Buy)
						this.RealThis.GoToBuyPhase();
				}
				else
				{
					this.RealThis.GoToBuyPhase();
				}
			}
		}

		protected override Card FindBestCardToPlay(IEnumerable<Card> cards)
		{
			// Sort the cards by cost (potion = 2.5 * coin)
			// Also, use a cost of 7 for Prize cards (since they have no cost normally)
			cards = cards.Where(card => this.ShouldPlay(card)).OrderByDescending(
				card => (card.Category & Category.Prize) == Category.Prize ? 7 : 
					card.CardType == Cards.DarkAges.TypeClass.Madman ? 4 :
					card.CardType == Cards.DarkAges.TypeClass.Mercenary ? 4 :
					(card.BaseCost.Coin.Value + 2.5 * card.BaseCost.Potion.Value));

			// Always play King's Court if there is one (?)
			Card kc = cards.FirstOrDefault(card => card.CardType == Cards.Prosperity.TypeClass.KingsCourt);
			if (kc != null)
			{
				// Not quite -- Don't play KC if there are certain cards where it's detrimental, or at least not helpful, to play multiple times
				// Also, to not be hurtful in certain situations, disallow KC'ing certain cards like Island
				if (!cards.All(c =>
					c.CardType == Cards.Base.TypeClass.Chapel ||
					c.CardType == Cards.Base.TypeClass.Library ||
					c.CardType == Cards.Base.TypeClass.Remodel ||
					c.CardType == Cards.Intrigue.TypeClass.SecretChamber ||
					c.CardType == Cards.Intrigue.TypeClass.Upgrade ||
					c.CardType == Cards.Seaside.TypeClass.Island ||
					c.CardType == Cards.Seaside.TypeClass.Lookout ||
					c.CardType == Cards.Seaside.TypeClass.Outpost ||
					c.CardType == Cards.Seaside.TypeClass.Salvager ||
					c.CardType == Cards.Seaside.TypeClass.Tactician ||
					c.CardType == Cards.Seaside.TypeClass.TreasureMap ||
					c.CardType == Cards.Prosperity.TypeClass.CountingHouse ||
					c.CardType == Cards.Prosperity.TypeClass.Forge ||
					c.CardType == Cards.Prosperity.TypeClass.TradeRoute ||
					c.CardType == Cards.Prosperity.TypeClass.Watchtower ||
					c.CardType == Cards.Cornucopia.TypeClass.Remake ||
					c.CardType == Cards.Hinterlands.TypeClass.Develop ||
					c.CardType == Cards.DarkAges.TypeClass.JunkDealer ||
					c.CardType == Cards.DarkAges.TypeClass.Procession ||
					c.CardType == Cards.DarkAges.TypeClass.Rats ||
					c.CardType == Cards.DarkAges.TypeClass.Rebuild ||
					c.CardType == Cards.Guilds.TypeClass.MerchantGuild ||
					c.CardType == Cards.Guilds.TypeClass.Stonemason))
					return kc;
			}

			// Always play Throne Room if there is one (?)
			Card tr = cards.FirstOrDefault(card => card.CardType == Cards.Base.TypeClass.ThroneRoom);
			if (tr != null)
			{
				// Not quite -- Don't play TR if there are certain cards where it's detrimental, or at least not helpful, to play multiple times
				// Also, to not be hurtful in certain situations, disallow TR'ing certain cards like Island
				if (!cards.All(c =>
					c.CardType == Cards.Base.TypeClass.Chapel ||
					c.CardType == Cards.Base.TypeClass.Library ||
					c.CardType == Cards.Base.TypeClass.Remodel ||
					c.CardType == Cards.Intrigue.TypeClass.SecretChamber ||
					c.CardType == Cards.Intrigue.TypeClass.Upgrade ||
					c.CardType == Cards.Seaside.TypeClass.Island ||
					c.CardType == Cards.Seaside.TypeClass.Lookout ||
					c.CardType == Cards.Seaside.TypeClass.Outpost ||
					c.CardType == Cards.Seaside.TypeClass.Salvager ||
					c.CardType == Cards.Seaside.TypeClass.Tactician ||
					c.CardType == Cards.Seaside.TypeClass.TreasureMap ||
					c.CardType == Cards.Prosperity.TypeClass.CountingHouse ||
					c.CardType == Cards.Prosperity.TypeClass.Forge ||
					c.CardType == Cards.Prosperity.TypeClass.TradeRoute ||
					c.CardType == Cards.Prosperity.TypeClass.Watchtower ||
					c.CardType == Cards.Cornucopia.TypeClass.Remake ||
					c.CardType == Cards.Hinterlands.TypeClass.Develop ||
					c.CardType == Cards.DarkAges.TypeClass.JunkDealer ||
					c.CardType == Cards.DarkAges.TypeClass.Procession ||
					c.CardType == Cards.DarkAges.TypeClass.Rats ||
					c.CardType == Cards.DarkAges.TypeClass.Rebuild ||
					c.CardType == Cards.Guilds.TypeClass.MerchantGuild ||
					c.CardType == Cards.Guilds.TypeClass.Stonemason))
					return tr;
			}

			// Play Menagerie first if we've got a hand with only unique cards
			if (cards.Count(c => c.CardType == Cards.Cornucopia.TypeClass.Menagerie) == 1)
			{
				IEnumerable<Type> typesMenagerie = this.RealThis.Hand.Select(c => c.CardType);
				if (typesMenagerie.Count() == typesMenagerie.Distinct().Count())
					return cards.First(c => c.CardType == Cards.Cornucopia.TypeClass.Menagerie);
			}

			// Keep Shanty Town available to play only if any the following criteria are satisfied
			if (cards.Count(c => c.CardType == Cards.Intrigue.TypeClass.ShantyTown) > 0)
			{
				if (cards.Count(c => c.CardType != Cards.Intrigue.TypeClass.ShantyTown) == 0 || // No other Action cards in hand
					cards.Count(c => c.CardType == Cards.Intrigue.TypeClass.ShantyTown) > 1 || // At least 1 other Shanty Town in hand
					(this.RealThis.Actions == 1 && cards.Count(c => this.ShouldPlay(c) && c.Benefit.Actions == 0) >= 2) || // 1 Action left & 2 or more Terminal Actions
					(this.RealThis.Actions == 1 && cards.Count(c => this.ShouldPlay(c) && c.Benefit.Actions == 0 && c.Benefit.Cards > 0) >= 1) || // 1 Action left & 1 or more card-drawing Actions
					this.RealThis.Hand[Cards.Cornucopia.TypeClass.HornOfPlenty].Count > 0) // Horn of Plenty in hand
				{
					// Keep it.  Criteria has been satisfied
				}
				else
					cards = cards.Where(c => c.CardType != Cards.Intrigue.TypeClass.ShantyTown);
			}

			Card plusActions = null;
			if (this.RealThis.CurrentTurn.CardsPlayed.Count(c => (c.Category & Category.Action) == Category.Action) >= 2)
				plusActions = cards.FirstOrDefault(c => c.CardType == Cards.Intrigue.TypeClass.Conspirator);
			if (plusActions == null)
				plusActions = cards.FirstOrDefault(card => card.Benefit.Actions > 0);
			if (plusActions != null)
				return plusActions;

			Turn previousTurn = null;
			if (this.RealThis._Game.TurnsTaken.Count > 1)
				previousTurn = this.RealThis._Game.TurnsTaken[this.RealThis._Game.TurnsTaken.Count - 2];

			// Play Smugglers if the player to our right gained a card costing at least 5 (that we can gain as well)
			// Only do this about 40% of the time.  It's pretty lame, man!
			if (cards.Any(card => card.CardType == Cards.Seaside.TypeClass.Smugglers) &&
				previousTurn != null && previousTurn.CardsGained.Any(card => _Game.ComputeCost(card).Potion == 0 && card.BaseCost.Coin >= 5 &&
				this.RealThis._Game.Table.Supplies.ContainsKey(card) && this.RealThis._Game.Table.Supplies[card].CanGain()) &&
				this._Game.RNG.Next(0, 5) < 2)
				return cards.First(card => card.CardType == Cards.Seaside.TypeClass.Smugglers);

			if (this.RealThis.Hand[Category.Curse].Count > 0)
			{
				// Play an Ambassador card if there is one and we have at least 1 Curse in our hand
				Card trasher = cards.FirstOrDefault(
					card => card.CardType == Cards.Base.TypeClass.Chapel ||
						card.CardType == Cards.Base.TypeClass.Remodel ||
						card.CardType == Cards.Seaside.TypeClass.Ambassador ||
						card.CardType == Cards.Seaside.TypeClass.Salvager ||
						card.CardType == Cards.Alchemy.TypeClass.Apprentice ||
						card.CardType == Cards.Prosperity.TypeClass.Expand ||
						card.CardType == Cards.Prosperity.TypeClass.Forge ||
						card.CardType == Cards.Prosperity.TypeClass.TradeRoute ||
						(card.CardType == Cards.Cornucopia.TypeClass.Remake && this.RealThis.Hand[Category.Curse].Count > 1) ||
						card.CardType == Cards.Hinterlands.TypeClass.Develop ||
						card.CardType == Cards.Hinterlands.TypeClass.JackOfAllTrades ||
						card.CardType == Cards.Hinterlands.TypeClass.Trader ||
						card.CardType == Cards.DarkAges.TypeClass.JunkDealer ||
						card.CardType == Cards.DarkAges.TypeClass.Rats ||
						card.CardType == Cards.Guilds.TypeClass.Butcher ||
						card.CardType == Cards.Guilds.TypeClass.Stonemason);
				if (trasher != null)
					return trasher;
			}

			// Don't play Trader if:
			// A) the current game progress is greater than 0.75 (still early) and either
			//		1) there are no Coppers in hand or 
			//		2)the number of playable Treasure cards better than Copper is still small
			// B) The lowest-cost non-Victory card we have in hand is >= 3
			if (cards.Count(c => c.CardType == Cards.Hinterlands.TypeClass.Trader) > 0)
			{
				if (this.GameProgress > 0.75 && (this.RealThis.Hand[Cards.Universal.TypeClass.Copper].Count == 0 ||
					this.RealThis.CountAll(this.RealThis, c => (c.Category & Category.Treasure) == Category.Treasure && c.CardType != Cards.Universal.TypeClass.Copper, true, false) < 3))
					cards = cards.Where(c => c.CardType != Cards.Hinterlands.TypeClass.Trader);
				else if (this.RealThis.Hand.Count(c => (c.Category & Category.Victory) != Category.Victory && _Game.ComputeCost(c).Coin.Value < 3) == 0)
					cards = cards.Where(c => c.CardType != Cards.Hinterlands.TypeClass.Trader);
			}

			// Don't play Courtyard if there are fewer than 2 cards to draw
			if (cards.Count(c => c.CardType == Cards.Intrigue.TypeClass.Courtyard) > 0 && this.RealThis.CountAll(this.RealThis, c => true, true, true) < 2)
				cards = cards.Where(c => c.CardType != Cards.Intrigue.TypeClass.Courtyard);

			if (cards.Count() > 0)
				// Just play the most expensive one
				return cards.ElementAt(0);

			return null;
		}

		protected override CardCollection FindBestCardsToPlay(IEnumerable<Card> cards)
		{
			// Play all Contrabands first if we can
			Card contraband = cards.FirstOrDefault(c => c.CardType == Cards.Prosperity.TypeClass.Contraband);
			if (contraband != null)
				return new CardCollection() { contraband };

			// Play all Counterfeits next
			Card counterfeit = cards.FirstOrDefault(c => c.CardType == Cards.DarkAges.TypeClass.Counterfeit);
			if (counterfeit != null)
				return new CardCollection() { counterfeit };

			// Play all normal treasures next
			IEnumerable<Card> normalTreasures = cards.Where(c =>
				(c.Category & Category.Treasure) == Category.Treasure &&
				c.CardType != Cards.Prosperity.TypeClass.Bank &&
				c.CardType != Cards.Prosperity.TypeClass.Loan &&
				c.CardType != Cards.Prosperity.TypeClass.Venture &&
				c.CardType != Cards.Cornucopia.TypeClass.HornOfPlenty &&
				c.CardType != Cards.Hinterlands.TypeClass.IllGottenGains);
			if (normalTreasures.Count() > 0)
				return new CardCollection(normalTreasures);

			// Only play Loan & Venture after cards like Philosopher's Stone that work better with more cards
			// There are some very specific situations where playing Horn Of Plenty before Philospher's Stone
			// or Venture is the right way to play things, but that's so incredibly rare.
			IEnumerable<Card> loanVenture = cards.Where(c =>
				c.CardType == Cards.Prosperity.TypeClass.Loan || c.CardType == Cards.Prosperity.TypeClass.Venture);
			if (loanVenture.Count() > 0)
				return new CardCollection(loanVenture);

			// Play Ill-Gotten Gains later so we can figure out if we need that extra Copper
			Card illGottenGains = cards.FirstOrDefault(c => c.CardType == Cards.Hinterlands.TypeClass.IllGottenGains);
			if (illGottenGains != null)
				return new CardCollection() { illGottenGains };

			// Always play Bank & Horn of Plenty last
			IEnumerable<Card> bankHornOfPlenty = cards.Where(c =>
				c.CardType == Cards.Prosperity.TypeClass.Bank || c.CardType == Cards.Cornucopia.TypeClass.HornOfPlenty);
			foreach (Card card in bankHornOfPlenty)
			{
				if (card.CardType == Cards.Cornucopia.TypeClass.HornOfPlenty)
				{
					List<Type> cardTypes = new List<Type>() { Cards.Cornucopia.TypeClass.HornOfPlenty };
					foreach (Card c in this.RealThis.InPlay)
					{
						Type t = c.CardType;
						if (!cardTypes.Contains(t))
							cardTypes.Add(t);
					}
					foreach (Card c in this.RealThis.SetAside)
					{
						Type t = c.CardType;
						if (!cardTypes.Contains(t))
							cardTypes.Add(t);
					}
					// Don't even bother playing Horn of Plenty if there's nothing for me to gain except Copper & Estate
					if (this.RealThis._Game.Table.Supplies.Values.FirstOrDefault(
						s => s.CardType != Cards.Universal.TypeClass.Copper &&
							s.CardType != Cards.Universal.TypeClass.Estate &&
							s.CanGain() &&
							this.ShouldBuy(s) &&
							s.CurrentCost <= new Cost(cardTypes.Count)) == null)
						continue;
				}

				return new CardCollection() { card };
			}

			// Don't play anything if we've fallen to here.
			return new CardCollection();
		}

		protected override bool ShouldBuy(Type type)
		{
			if (type == Cards.Universal.TypeClass.Curse)
				return false;
			else if (type == Cards.Base.TypeClass.Moneylender)
				return false;
			else if (type == Cards.Base.TypeClass.Chapel)
				return false;
			else if (type == Cards.Base.TypeClass.Remodel)
				return false;
			else if (type == Cards.Intrigue.TypeClass.Upgrade)
				return false;
			else if (type == Cards.Intrigue.TypeClass.TradingPost)
				return false;
			else if (type == Cards.Intrigue.TypeClass.Masquerade)
				return false;
			else if (type == Cards.Intrigue.TypeClass.Coppersmith)
				return false;
			else if (type == Cards.Seaside.TypeClass.Lookout)
				return false;
			else if (type == Cards.Seaside.TypeClass.Ambassador)
				return false;
			else if (type == Cards.Seaside.TypeClass.Navigator)
				return false;
			else if (type == Cards.Seaside.TypeClass.Salvager)
				return false;
			else if (type == Cards.Seaside.TypeClass.TreasureMap)
				return false;
			//else if (type == Cards.Alchemy.TypeClass.Potion)
			//    return false;
			//else if (type == Cards.Alchemy.TypeClass.Possession)
			//    return false;
			else if (type == Cards.Alchemy.TypeClass.Apprentice)
				return false;
			else if (type == Cards.Alchemy.TypeClass.Transmute)
				return false;
			else if (type == Cards.Prosperity.TypeClass.Expand)
				return false;
			else if (type == Cards.Prosperity.TypeClass.Forge)
				return false;
			else if (type == Cards.Prosperity.TypeClass.TradeRoute)
				return false;
			else if (type == Cards.Cornucopia.TypeClass.Remake)
				return false;
			else if (type == Cards.Hinterlands.TypeClass.Develop)
				return false;
			else if (type == Cards.DarkAges.TypeClass.BandOfMisfits)
				return false;
			else if (type == Cards.DarkAges.TypeClass.Forager)
				return false;
			else if (type == Cards.DarkAges.TypeClass.Procession)
				return false;
			else if (type == Cards.DarkAges.TypeClass.Rats)
				return false;
			else if (type == Cards.DarkAges.TypeClass.Rebuild)
				return false;
			else if (type == Cards.DarkAges.TypeClass.RuinsSupply)
				return false;
			else if (type == Cards.Guilds.TypeClass.Butcher)
				return false;
			else if (type == Cards.Guilds.TypeClass.Stonemason)
				return false;
			return true;
		}

		protected override Supply FindBestCardToBuy(List<Supply> buyableSupplies)
		{
			Dictionary<double, List<Supply>> scores = this.ValuateCardsToBuy(buyableSupplies);

			double bestScore = scores.Keys.OrderByDescending(k => k).FirstOrDefault();
			if (bestScore >= 0d)
				return scores[bestScore][this._Game.RNG.Next(scores[bestScore].Count)];
			return null;
		}

		protected virtual Dictionary<double, List<Supply>> ValuateCardsToBuy(List<Supply> buyableSupplies)
		{
			Dictionary<double, List<Supply>> scores = new Dictionary<double, List<Supply>>();
			foreach (Supply supply in buyableSupplies)
			{
				// We need to compute score based on the original/base cost of the card
				double score = supply.BaseCost.Coin.Value + 2.5f * supply.BaseCost.Potion.Value;

				// Scale based on the original -vs- current cost -- cheaper cards should be more valuable to us!
				score += Math.Log((score + 1) / (supply.CurrentCost.Coin.Value + 2.5f * supply.CurrentCost.Potion.Value + 1));

				if (!ShouldBuy(supply))
					score = -1d;

				// Scale back the score accordingly if it's near the end of the game and the card is not a Victory card
				if (this.GameProgress < 0.25 && (supply.Category & Category.Victory) != Category.Victory)
					score *= 0.15d;

				// Scale up Province & Colony scores in late game
				if (this.GameProgress < 0.3 && (supply.CardType == Cards.Universal.TypeClass.Province ||
					supply.CardType == Cards.Prosperity.TypeClass.Colony))
					score *= 1.2d;

				// Never buy non-Province/Colony/Farmland/Tunnel Victory-only cards early
				if (this.GameProgress > 0.71 && supply.Category == Category.Victory &&
					supply.CardType != Cards.Universal.TypeClass.Province &&
					supply.CardType != Cards.Prosperity.TypeClass.Colony &&
					supply.CardType != Cards.Hinterlands.TypeClass.Farmland &&
					supply.CardType != Cards.Hinterlands.TypeClass.Tunnel)
					score = -1d;

				if ((this.GameProgress > 0.25 && supply.CardType == Cards.Universal.TypeClass.Estate) ||
					(this.GameProgress > 0.25 && supply.CardType == Cards.Alchemy.TypeClass.Vineyard) ||
					(this.GameProgress > 0.35 && supply.CardType == Cards.Base.TypeClass.Gardens) ||
					(this.GameProgress > 0.35 && supply.CardType == Cards.Hinterlands.TypeClass.SilkRoad) ||
					(this.GameProgress > 0.35 && supply.CardType == Cards.DarkAges.TypeClass.Feodum) ||
					(this.GameProgress > 0.4 && supply.CardType == Cards.Universal.TypeClass.Duchy) ||
					(this.GameProgress > 0.4 && supply.CardType == Cards.Intrigue.TypeClass.Duke) ||
					(this.GameProgress > 0.4 && supply.CardType == Cards.Cornucopia.TypeClass.Fairgrounds))
					score *= 0.15d;

				if ((this.GameProgress < 0.15 && supply.CardType == Cards.Universal.TypeClass.Estate) ||
					(this.GameProgress < 0.15 && supply.CardType == Cards.Alchemy.TypeClass.Vineyard) ||
					(this.GameProgress < 0.20 && supply.CardType == Cards.Base.TypeClass.Gardens) ||
					(this.GameProgress < 0.20 && supply.CardType == Cards.Hinterlands.TypeClass.SilkRoad) ||
					(this.GameProgress < 0.20 && supply.CardType == Cards.DarkAges.TypeClass.Feodum) ||
					(this.GameProgress < 0.25 && supply.CardType == Cards.Universal.TypeClass.Duchy) ||
					(this.GameProgress < 0.25 && supply.CardType == Cards.Intrigue.TypeClass.Duke) ||
					(this.GameProgress < 0.25 && supply.CardType == Cards.Cornucopia.TypeClass.Fairgrounds))
					score *= 1.1d;

				// Duke/Duchy decision
				if (supply.CardType == Cards.Intrigue.TypeClass.Duke ||
					supply.CardType == Cards.Universal.TypeClass.Duchy)
				{
					int duchies = this.RealThis.CountAll(this.RealThis, c => c.CardType == Cards.Universal.TypeClass.Duchy, false, false);
					int dukes = this.RealThis.CountAll(this.RealThis, c => c.CardType == Cards.Intrigue.TypeClass.Duke, false, false);

					// If gaining a Duke is not as useful as gaining a Duchy, don't get the Duke
					if (supply.CardType == Cards.Intrigue.TypeClass.Duke && duchies - dukes < 4)
						score *= 0.95d;
					// If gaining a Duchy is not as useful as gaining a Duke, don't get the Duchy
					if (supply.CardType == Cards.Universal.TypeClass.Duchy && duchies - dukes >= 4)
						score *= 0.95d;
				}

				// Scale Silk Road score based on how many Victory cards we have
				if (supply.CardType == Cards.Hinterlands.TypeClass.SilkRoad)
				{
					int totalVictoryCount = this.RealThis.CountAll(this.RealThis, c => (c.Category & Category.Victory) == Category.Victory, true, false);
					score *= Math.Pow(1.045, 2 * (totalVictoryCount - 6));
				}
				
				// Scale all Victory cards based on how many Silk Roads we have
				if ((supply.Category & Category.Victory) == Category.Victory)
				{
					int totalSilkRoadCount = this.RealThis.CountAll(this.RealThis, c => c.CardType == Cards.Hinterlands.TypeClass.SilkRoad, true, false);
					score *= Math.Pow(1.045, Math.Max(0, totalSilkRoadCount - 6));
				}

				// Scale Feodum score based on how many Silvers we have
				if (supply.CardType == Cards.DarkAges.TypeClass.Feodum)
				{
					int totalSilverCount = this.RealThis.CountAll(this.RealThis, c => c.CardType == Cards.Universal.TypeClass.Silver, true, false);
					score *= Math.Pow(1.045, 2 * (totalSilverCount - 6));
				}

				if (supply.CardType == Cards.Universal.TypeClass.Copper)
				{
					// Never buy Copper cards unless we have a Goons in play
					if (this.RealThis.InPlay[Cards.Prosperity.TypeClass.Goons].Count == 0)
						score = -1d;
					//else if (this.CurrentTurn.CardsBought.Count(c => c.CardType == Cards.Universal.TypeClass.Copper) > 1)
					//    score = -1d;
				}

				if (supply.CardType == Cards.Universal.TypeClass.Silver)
				{
					//int totalSilverCount = this.RealThis.CountAll(this, c => c.CardType == Cards.Universal.TypeClass.Silver, true, false);
					int totalFeodumCount = this.RealThis.CountAll(this.RealThis, c => c.CardType == Cards.DarkAges.TypeClass.Feodum, false, false);
					score *= Math.Pow(1.04, 2 * totalFeodumCount);
					if (totalFeodumCount > 0 && this.GameProgress < 0.25)
						score /= Math.Max(this.GameProgress, 0.08);
				}

				// Early on, and when we don't have many Silver, give a slight bias to Silvers
				//if (supply.CardType == Cards.Universal.TypeClass.Silver)
				//{
				//    score *= Math.Pow(1.04, 2 * Math.Pow(this.GameProgress, 2));

				//    int silverCount = this.RealThis.CountAll(this, c => c.CardType == Cards.Universal.TypeClass.Silver, true, false);
				//    int copperCount = this.RealThis.CountAll(this, c => c.CardType == Cards.Universal.TypeClass.Copper, true, false);
				//    int treasureCount = this.RealThis.CountAll(this, c => (c.Category & Category.Treasure) == Category.Treasure, true, false);
				//    int totalCount = this.RealThis.CountAll();

				//    //if (
				//    //score *= 
				//}

				if (supply.CardType == Cards.Base.TypeClass.Moat)
				{
					IEnumerable<Supply> attackSupplies = this.RealThis._Game.Table.Supplies.Values.Where(
						s => (s.Category & Category.Attack) == Category.Attack);

					int attackCardsLeft = attackSupplies.Sum(s => s.Count);
					int attackCardsInTrash = this.RealThis._Game.Table.Trash.Count(c => attackSupplies.Any(s => s.CardType == c.CardType));
					int attackCardsInDecks = attackSupplies.Sum(s => s.StartingStackSize) - attackCardsLeft - attackCardsInTrash;
					int attackCardsInMyDeck = CountAll(this.RealThis, c => (c.Category & Category.Attack) == Category.Attack, true, false);

					score *= Math.Pow(1.05, attackCardsInDecks - attackCardsInMyDeck + 0.25 * attackCardsLeft);
				}

				if (supply.CardType == Cards.Seaside.TypeClass.Lighthouse)
				{
					IEnumerable<Supply> attackSupplies = this.RealThis._Game.Table.Supplies.Values.Where(
						s => (s.Category & Category.Attack) == Category.Attack);

					int attackCardsLeft = attackSupplies.Sum(s => s.Count);
					int attackCardsInTrash = this.RealThis._Game.Table.Trash.Count(c => attackSupplies.Any(s => s.CardType == c.CardType));
					int attackCardsInDecks = attackSupplies.Sum(s => s.StartingStackSize) - attackCardsLeft - attackCardsInTrash;
					int attackCardsInMyDeck = CountAll(this.RealThis, c => (c.Category & Category.Attack) == Category.Attack, true, false);

					score *= Math.Pow(1.05, attackCardsInDecks - attackCardsInMyDeck + 0.25 * attackCardsLeft);
				}

				// Embargo tokens -- these make cards worth less than they would normally be (but only if there are Curse cards left)
				if (supply.Tokens.Count(token => token.GetType() == Cards.Seaside.TypeClass.EmbargoToken) > 0 && this.RealThis._Game.Table.Curse.Count > 0)
				{
					score *= Math.Pow(0.8, Math.Min(supply.Tokens.Count(token => token.GetType() == Cards.Seaside.TypeClass.EmbargoToken), this.RealThis._Game.Table.Curse.Count));

					// If Estates are Embargoed (and there are Curses left), make Estates not very nice to buy
					if (supply.CardType == Cards.Universal.TypeClass.Estate)
						score = 0.001;
				}

				// Peddler -- not really worth 8; scale it back if it's over 5
				if (supply.CardType == Cards.Prosperity.TypeClass.Peddler)
				{
					if (supply.CurrentCost.Coin > 5)
						score *= 0.6f;
				}

				if (supply.CardType == Cards.Prosperity.TypeClass.Mint)
				{
					int totalTreasureCards = this.RealThis.CountAll(this.RealThis, c => (c.Category & Category.Treasure) == Category.Treasure, true, false);
					int totalCopperCards = this.RealThis.CountAll(this.RealThis, c => c.CardType == Cards.Universal.TypeClass.Copper, true, false);
					IEnumerable<Card> cardsToTrash = this.RealThis.SetAside[Category.Treasure].Union(this.RealThis.InPlay[Category.Treasure]);
					double cardWorth = 0d;
					foreach (Card card in cardsToTrash)
					{
						// Since Diadem has no "Cost", we have to create an artificial worth for it
						if (card.CardType == Cards.Cornucopia.TypeClass.Diadem)
							cardWorth += 7d;
						// Copper has a slightly negative worth
						else if (card.CardType == Cards.Universal.TypeClass.Copper)
							cardWorth += -0.25;
						// Ill-Gotten Gains's worth is significantly less after it's been gained
						else if (card.CardType == Cards.Hinterlands.TypeClass.IllGottenGains)
							cardWorth -= 1.72;
						else
							cardWorth += card.BaseCost.Coin.Value + 2.5 * card.BaseCost.Potion.Value;
					}

					score *= Math.Pow(0.975, cardWorth);

					if (totalTreasureCards - totalCopperCards < cardsToTrash.Count())
						score *= 0.85;
				}

				// Too many Talismans leads to crappy decks -- we should limit ourselves to only a couple
				// even fewer if there aren't many <= 4-cost non-Victory, non-Talisman cards available
				if (supply.CardType == Cards.Prosperity.TypeClass.Talisman)
				{
					score *= talismanLikelihood;

					score *= Math.Pow(0.95, this.RealThis.CountAll(this.RealThis, c => c.CardType == Cards.Prosperity.TypeClass.Talisman, true, false));

					Console.WriteLine(String.Format("Talisman score: Initial score: {0}", score));
					double pileScore = 0.0;
					StringBuilder talismanSupplies = new StringBuilder();
					foreach (Supply talismanableSupply in this.RealThis._Game.Table.Supplies.Values.Where(s =>
							(s.Category & Category.Victory) != Category.Victory && s.CardType != Cards.Prosperity.TypeClass.Talisman && s.CardType != Cards.Universal.TypeClass.Copper && s.Count >= 2 && 
							this.ShouldBuy(s) && s.BaseCost <= new Currencies.Coin(4)))
					{
						pileScore += 0.1 * Math.Min(10, talismanableSupply.Count);
						talismanSupplies.AppendFormat("Supply: {0} {1}, ", talismanableSupply.Name, 0.1 * Math.Min(10, talismanableSupply.Count));
					}
					score *= 0.75 * Math.Pow(1.25, Math.Log(pileScore));
					Console.WriteLine(String.Format("Talisman scores: {0} --- Final score: {1}", talismanSupplies, score));
				}

				// Scale Potion likelihood based on how many we already have -- don't want a glut of them!
				// Also scale Potion likelihood based on the number of Potion-costing cards in the Supply -- More = bettar
				if (supply.CardType == Cards.Alchemy.TypeClass.Potion)
				{
					score *= potionLikelihood;

					score *= Math.Pow(0.8, this.RealThis.CountAll(this.RealThis, c => c.CardType == Cards.Alchemy.TypeClass.Potion, true, false));

					score *= Math.Pow(1.1, this.RealThis._Game.Table.Supplies.Values.Count(s => s.BaseCost.Potion > 0));
				}

				// Don't overemphasize Throne Room or King's Court if we don't have many Action cards compared to our deck size
				if (supply.CardType == Cards.Base.TypeClass.ThroneRoom ||
					supply.CardType == Cards.Prosperity.TypeClass.KingsCourt)
				{
					int nonTR_KC_actions = this.RealThis.CountAll(this.RealThis, c =>
						(c.Category & Category.Action) == Category.Action &&
						c.CardType != Cards.Base.TypeClass.ThroneRoom &&
						c.CardType != Cards.Prosperity.TypeClass.KingsCourt, true, false);
					// Fudge factor
					nonTR_KC_actions = (int)(nonTR_KC_actions * (1.0 + Utilities.Gaussian.NextGaussian(this._Game.RNG) / 8d));

					int totalCards = this.RealThis.CountAll();
					// Fudge factor
					totalCards = (int)(totalCards * (1.0 + Utilities.Gaussian.NextGaussian(this._Game.RNG) / 8d));

					double ratio = (double)nonTR_KC_actions / totalCards;

					score *= Math.Sqrt(ratio / 0.18d);
				}

				// Don't overemphasize Golem if we don't have many non-Golem Action cards
				if (supply.CardType == Cards.Alchemy.TypeClass.Golem)
				{
					int nonGolem_actions = this.RealThis.CountAll(this.RealThis, c =>
						(c.Category & Category.Action) == Category.Action &&
						c.CardType != Cards.Alchemy.TypeClass.Golem, true, false);
					// Fudge factor
					nonGolem_actions = (int)(nonGolem_actions * (1.0 + Utilities.Gaussian.NextGaussian(this._Game.RNG) / 8d));

					int totalCards = this.RealThis.CountAll();
					// Fudge factor
					totalCards = (int)(totalCards * (1.0 + Utilities.Gaussian.NextGaussian(this._Game.RNG) / 8d));

					double ratio = (double)nonGolem_actions / totalCards;

					score *= Math.Sqrt(ratio / 0.18d);
				}

				if (supply.CardType == Cards.Seaside.TypeClass.Embargo)
				{
					IEnumerable<Supply> curseGivingSupplies = this.RealThis._Game.Table.Supplies.Values.Where(
						s => s.CardType == Cards.Base.TypeClass.Witch ||
							s.CardType == Cards.Intrigue.TypeClass.Swindler ||
							s.CardType == Cards.Intrigue.TypeClass.Torturer ||
							s.CardType == Cards.Seaside.TypeClass.Ambassador ||
							s.CardType == Cards.Seaside.TypeClass.SeaHag ||
							s.CardType == Cards.Alchemy.TypeClass.Familiar ||
							s.CardType == Cards.Prosperity.TypeClass.Mountebank ||
							s.CardType == Cards.Cornucopia.TypeClass.Jester ||
							s.CardType == Cards.Cornucopia.TypeClass.YoungWitch ||
							s.CardType == Cards.Guilds.TypeClass.Soothsayer);

					int curseGivingCardsLeft = curseGivingSupplies.Sum(s => s.Count);
					int curseGivingCardsInTrash = this.RealThis._Game.Table.Trash.Count(c => curseGivingSupplies.Any(s => s.CardType == c.CardType));
					int curseGivingCardsInDecks = curseGivingSupplies.Sum(s => s.StartingStackSize) - curseGivingCardsLeft - curseGivingCardsInTrash;
					int curseGivingCardsInMyDeck = CountAll(this.RealThis,
						c => c.CardType == Cards.Base.TypeClass.Witch ||
							c.CardType == Cards.Intrigue.TypeClass.Swindler ||
							c.CardType == Cards.Intrigue.TypeClass.Torturer ||
							c.CardType == Cards.Seaside.TypeClass.Ambassador ||
							c.CardType == Cards.Seaside.TypeClass.SeaHag ||
							c.CardType == Cards.Alchemy.TypeClass.Familiar ||
							c.CardType == Cards.Prosperity.TypeClass.Mountebank ||
							c.CardType == Cards.Cornucopia.TypeClass.Jester ||
							c.CardType == Cards.Cornucopia.TypeClass.YoungWitch ||
							c.CardType == Cards.Cornucopia.TypeClass.Followers ||
							c.CardType == Cards.Guilds.TypeClass.Soothsayer,
							true, false);

					// Followers is either in Supply pile or player's piles
					if (this.RealThis._Game.Table.Supplies.ContainsKey(Cards.Cornucopia.TypeClass.PrizeSupply))
					{
						int followersInSupply = this.RealThis._Game.Table.Supplies[Cards.Cornucopia.TypeClass.PrizeSupply].Count(c => c.CardType == Cards.Cornucopia.TypeClass.Followers);
						int followersInTrash = this.RealThis._Game.Table.Trash.Count(c => c.CardType == Cards.Cornucopia.TypeClass.Followers);
						curseGivingCardsLeft += followersInSupply;
						if (followersInSupply == 0 && followersInTrash == 0)
							curseGivingCardsInDecks += 1;
					}

					// TODO : This still needs some work -- I need to come up with a way that scales the "worth" of Embargo cards 
					// based on the number of Curses left and the number of Curse-giving cards in play as well as remaining
					//double alphaTerm = 1.0 / (0.5 * Math.Log((double)(curseGivingCardsInDecks - curseGivingCardsInMyDeck + 1)) + 1.0);
					double gammaTerm = 0.5f;
					if (curseGivingCardsInMyDeck < 30)
						gammaTerm = Math.Max(1.0 - (Math.Pow(1.0 / (30.0 - curseGivingCardsInMyDeck), 0.25) - Math.Pow(1 / 30.0, 0.25)), gammaTerm);
					//double deltaTerm = Math.Log((double)(this.RealThis._Game.Table.Supplies[Cards.Universal.TypeClass.Curse].Count + 1)) / 2.5;
					//score *= alphaTerm * gammaTerm * deltaTerm;
					//score *= gammaTerm * deltaTerm;
					score *= 1.1 * gammaTerm;
				}

				// Witch gets less & less valuable with fewer & fewer curses, down to about 1.9 when there are none
				// We should actually make it slightly more likely to get Witch when there are lots of curses
				if (supply.CardType == Cards.Base.TypeClass.Witch)
				{
					score *= Math.Pow(0.8, (9.8d - (1d + Utilities.Gaussian.NextGaussian(this._Game.RNG) / 12d) * Math.Sqrt(10) * Math.Sqrt(((double)this.RealThis._Game.Table.Curse.Count) / (this.RealThis._Game.Players.Count - 1))) * 4.3335 / 10);
				}

				// Seahag gets less & less valuable with fewer & fewer curses, down to absolutely useless when there are none
				// We should actually make it slightly more likely to get SeaHag when there are lots of curses
				if (supply.CardType == Cards.Seaside.TypeClass.SeaHag)
				{
					score *= Math.Pow(0.8, 9.8d - (1d + Utilities.Gaussian.NextGaussian(this._Game.RNG) / 12d) * Math.Sqrt(10) * Math.Sqrt(((double)this.RealThis._Game.Table.Curse.Count) / (this.RealThis._Game.Players.Count - 1)));
				}

				// Familiar gets less & less valuable with fewer & fewer curses, down to about 1.6 when there are none
				// We should actually make it slightly more likely to get Familiar when there are lots of curses
				if (supply.CardType == Cards.Alchemy.TypeClass.Familiar)
				{
					score *= Math.Pow(0.8, (9.8d - (1d + Utilities.Gaussian.NextGaussian(this._Game.RNG) / 12d) * Math.Sqrt(10) * Math.Sqrt(((double)this.RealThis._Game.Table.Curse.Count) / (this.RealThis._Game.Players.Count - 1))) * 5.5335 / 10);
				}

				// Limit the number of Contrabands we'll buy to a fairly small amount (1 per every 20 cards or so)
				if (supply.CardType == Cards.Prosperity.TypeClass.Contraband)
				{
					int contrabandsICanPlay = this.RealThis.CountAll(this.RealThis, c => c.CardType == Cards.Prosperity.TypeClass.Contraband, true, false);
					int totalDeckSize = this.RealThis.CountAll();
					double percentageOfContrabands = ((double)contrabandsICanPlay) / totalDeckSize;
					if (percentageOfContrabands > 0.05)
						score *= Math.Pow(0.2, Math.Pow(percentageOfContrabands, 2));
				}

				// Limit the number of Outposts we'll buy to a fairly small amount (1 per every 15 cards or so)
				if (supply.CardType == Cards.Seaside.TypeClass.Outpost)
				{
					int outpostsICanPlay = this.RealThis.CountAll(this.RealThis, c => c.CardType == Cards.Seaside.TypeClass.Outpost, true, false);
					int totalDeckSize = this.RealThis.CountAll();
					double percentageOfOutposts = ((double)outpostsICanPlay) / totalDeckSize;
					if (percentageOfOutposts > 0.6667)
						score *= Math.Pow(0.2, Math.Pow(percentageOfOutposts, 2));
				}

				// Need to be careful when buying this card, since it can muck up our deck (e.g. making us trash a Province or Colony)
				if (supply.CardType == Cards.Hinterlands.TypeClass.Farmland)
				{
					if (this.RealThis.Hand.Count == 0)
						score *= 0.95;
					else
					{
						// We always like trashing Curses if we can, especially when we buy a different Victory card in the process
						if (this.RealThis.Hand[Category.Curse].Count > 0)
							score *= 1.1;

						// This complicated-looking little nested LINQ comparison checks to see if our hand consists only
						// of cards that don't have a Supply pile that costs exactly 2 coins more that is gainable
						// e.g. if we only have Provinces, Colonies, or 5-cost cards in our hand (and no 7-cost Supplies exist), we don't really want to trash them.
						else if (this.RealThis.Hand.All(c => !_Game.Table.Supplies.Any(kvp => kvp.Value.CanGain() && kvp.Value.CurrentCost == _Game.ComputeCost(c) + new Currencies.Coin(2))))
						{
							score *= 0.02;
						}

						// This LINQ query checks to see if there are any Supply piles that have Victory cards that are
						// strictly better than any Victory cards we have in our hand (e.g. Province vs. Farmland)
						else if (this.RealThis.Hand.Any(c => (c.Category & Category.Victory) == Category.Victory && _Game.Table.Supplies.Any(kvp =>
							(kvp.Value.Category & Category.Victory) == Category.Victory &&
							kvp.Value.VictoryPoints > c.VictoryPoints &&
							kvp.Value.CanGain() &&
							kvp.Value.CurrentCost == _Game.ComputeCost(c) + new Currencies.Coin(2))))
						{
							score *= 1.1;
						}

						// I dunno after that... will require testing.  In general, be cautious
						else
						{
							score *= 0.75;
						}
					}
				}

				if (supply.CardType == Cards.Hinterlands.TypeClass.NobleBrigand)
				{
					// Never buy Noble Brigand in the first 2 turns, and then be wary of it based on how many Silver & Gold everyone has
					if (this.RealThis._Game.TurnsTaken.Count(t => t.Player == this.RealThis) < 3)
						score = -1.0;
					double nbTotalSilverGold = this.RealThis._Game.Players.Where(p => p != this.RealThis).Sum(p => p.CountAll(this.RealThis, c => c.CardType == Cards.Universal.TypeClass.Silver || c.CardType == Cards.Universal.TypeClass.Gold, true, false)) / (this.RealThis._Game.Players.Count - 1);
					double nbTotalCards = this.RealThis._Game.Players.Where(p => p != this.RealThis).Sum(p => p.CountAll()) / (this.RealThis._Game.Players.Count - 1);
					if (nbTotalSilverGold < 2 || nbTotalSilverGold / nbTotalCards < 0.1)
						score *= 0.5;
				}

				if (supply.CardType == Cards.Guilds.TypeClass.Doctor)
				{
					if (this.RealThis.Currency.Coin.Value == supply.CurrentCost.Coin.Value)
						score *= 0.75;
					Console.WriteLine("Doctor new score: {0}", score);
				}

				if (supply.CardType == Cards.Guilds.TypeClass.Herald)
				{
					if (this.RealThis.Currency.Coin.Value == supply.CurrentCost.Coin.Value)
						score *= 0.75;
					Console.WriteLine("Herald new score: {0}", score);
				}

				if (supply.CardType == Cards.Guilds.TypeClass.Masterpiece)
				{
					// We'd have to gain at least 2 Silvers for this to be worth-while
					if (this.RealThis.Currency.Coin.Value <= (supply.CurrentCost.Coin + new Currencies.Coin(1)).Value)
						score *= 0.75;
					else
					{
						// Victory cards that make gaining lots of Silvers good
						int totalGardensCount = this.RealThis.CountAll(this.RealThis, c => c.CardType == Cards.Base.TypeClass.Gardens, true, false);
						int availableGardens = 0;
						if (this.RealThis._Game.Table.Supplies.ContainsKey(Cards.Base.TypeClass.Gardens))
							availableGardens = this.RealThis._Game.Table[Cards.Base.TypeClass.Gardens].Count;
						int totalFeodumCount = this.RealThis.CountAll(this.RealThis, c => c.CardType == Cards.DarkAges.TypeClass.Feodum, true, false);
						int availableFeodums = 0;
						if (this.RealThis._Game.Table.Supplies.ContainsKey(Cards.DarkAges.TypeClass.Feodum))
							availableFeodums = this.RealThis._Game.Table[Cards.DarkAges.TypeClass.Feodum].Count;
						// ALL THE FACTORS
						score *= Math.Pow(1.04 + 0.005 * totalGardensCount + 0.025 * totalFeodumCount + 0.0025 * availableGardens + 0.005 * availableFeodums, this.RealThis.Currency.Coin.Value - supply.CurrentCost.Coin.Value);
					}
					Console.WriteLine("Masterpiece new score: {0}", score);
				}

				if (supply.CardType == Cards.Guilds.TypeClass.Stonemason)
				{
					if (this.RealThis.Currency.Coin.Value == supply.CurrentCost.Coin.Value)
						score *= 0.5;
					Console.WriteLine("Stonemason new score: {0}", score);
				}

				if (!scores.ContainsKey(score))
					scores[score] = new List<Supply>();
				scores[score].Add(supply);
			}
			return scores;
		}

		protected override Supply FindBestCardForCost(IEnumerable<Supply> buyableSupplies, Currency currency, bool buying)
		{
			List<Supply> bestSupplies = new List<Supply>();
			double bestCost = -1.0;
			foreach (Supply supply in buyableSupplies)
			{
				if (!ShouldBuy(supply))
					continue;

				// Only return ones we CAN gain
				if (currency != (Currency)null && currency < supply.CurrentCost)
					continue;

				// Overpay cards are, by default, worse than their initial cost
				double supplyCost = (supply.BaseCost.Coin.Value + 2.5 * supply.BaseCost.Potion.Value) * (supply.BaseCost.CanOverpay ? 0.75 : 1.0);

				if (bestCost < 0.0 || bestCost <= supplyCost)
				{
					// Don't buy if it's not a Victory card near the end of the game
					if (this.GameProgress < 0.25 && (supply.Category & Category.Victory) != Category.Victory)
						continue;

					// Never buy Duchies or Estates early
					if ((this.GameProgress > 0.25 && supply.CardType == Cards.Universal.TypeClass.Estate) ||
						(this.GameProgress > 0.25 && supply.CardType == Cards.Alchemy.TypeClass.Vineyard) ||
						(this.GameProgress > 0.35 && supply.CardType == Cards.Base.TypeClass.Gardens) ||
						(this.GameProgress > 0.35 && supply.CardType == Cards.Hinterlands.TypeClass.SilkRoad) ||
						(this.GameProgress > 0.35 && supply.CardType == Cards.DarkAges.TypeClass.Feodum) ||
						(this.GameProgress > 0.4 && supply.CardType == Cards.Universal.TypeClass.Duchy) ||
						(this.GameProgress > 0.4 && supply.CardType == Cards.Intrigue.TypeClass.Duke) ||
						(this.GameProgress > 0.4 && supply.CardType == Cards.Cornucopia.TypeClass.Fairgrounds))
						continue;

					// Duke/Duchy decision
					if (supply.SupplyCardType == Cards.Intrigue.TypeClass.Duke ||
						supply.SupplyCardType == Cards.Universal.TypeClass.Duchy)
					{
						int duchies = this.RealThis.CountAll(this.RealThis, c => c.CardType == Cards.Universal.TypeClass.Duchy, false, false);
						int dukes = this.RealThis.CountAll(this.RealThis, c => c.CardType == Cards.Intrigue.TypeClass.Duke, false, false);

						// If gaining a Duke is not as useful as gaining a Duchy, don't get the Duke
						if (supply.SupplyCardType == Cards.Intrigue.TypeClass.Duke && duchies - dukes < 4)
							continue;
						// If gaining a Duchy is not as useful as gaining a Duke, don't get the Duchy
						if (supply.SupplyCardType == Cards.Universal.TypeClass.Duchy && duchies - dukes >= 4)
							continue;
					}

					// Reset best cost to new one
					if (bestCost < 0.0 || bestCost < supplyCost)
					{
						bestCost = supplyCost;
						bestSupplies.Clear();
					}
					bestSupplies.Add(supply);
				}
			}

			if (bestSupplies.Count == 0)
			{
				foreach (Supply supply in buyableSupplies)
				{
					if (supply.SupplyCardType == Cards.Universal.TypeClass.Curse)
						continue;

					// Overpay cards are, by default, worse than their initial cost
					double supplyCost = (supply.BaseCost.Coin.Value + 2.5 * supply.BaseCost.Potion.Value) * (supply.BaseCost.CanOverpay ? 0.75 : 1.0);

					if (bestCost < 0.0 || bestCost <= supply.BaseCost.Coin.Value)
					{
						// Reset best cost to new one
						if (bestCost < 0.0 || bestCost < supplyCost)
						{
							bestCost = supplyCost;
							bestSupplies.Clear();
						}
						bestSupplies.Add(supply);
					}

				}
			}

			if (bestSupplies.Count == 0)
			{
				if (buyableSupplies.Count() > 0)
					return buyableSupplies.ElementAt(this._Game.RNG.Next(buyableSupplies.Count()));
				return null;
			}

			return bestSupplies[this._Game.RNG.Next(bestSupplies.Count)];
		}

		protected override Supply FindWorstCardForCost(IEnumerable<Supply> buyableSupplies, Currency currency)
		{
			List<Supply> worstSupplies = new List<Supply>();
			foreach (Supply supply in buyableSupplies)
			{
				// Only return ones we CAN gain
				if (currency != (Currency)null && !supply.CurrentCost.Equals(currency))
					continue;

				if (ShouldBuy(supply))
					continue;

				worstSupplies.Add(supply);
			}

			if (worstSupplies.Count == 0)
				return buyableSupplies.ElementAt(this._Game.RNG.Next(buyableSupplies.Count()));

			Supply worstSupply = worstSupplies.Find(s => s.CardType == Cards.Universal.TypeClass.Curse);
			if (worstSupply != null)
				return worstSupply;
			worstSupply = worstSupplies.Find(s => (s.Category & Category.Ruins) == Category.Ruins);
			if (worstSupply != null)
				return worstSupply;

			return worstSupplies[this._Game.RNG.Next(worstSupplies.Count)];
		}

		protected override bool ShouldPlay(Card card)
		{
			if (!ShouldBuy(card.CardType))
				return false;

			int previousTurnIndex = this.RealThis._Game.TurnsTaken.Count - 2;
			Turn previousTurn = null;
			if (previousTurnIndex >= 0)
				previousTurn = this.RealThis._Game.TurnsTaken[previousTurnIndex];

			if (card.CardType == Cards.Base.TypeClass.Library)
			{
				// Don't play this if we're already at 7 cards (after playing it, of course)
				if (this.RealThis.Hand.Count > 7)
					return false;
			}
			else if (card.CardType == Cards.Base.TypeClass.Mine)
			{
				// Check to see if there are any Treasure cards in the Supply that are better than at least one of the Treasure cards in my hand
				foreach (Card treasureCard in this.RealThis.Hand[Category.Treasure])
				{
					Cost treasureCardCost = _Game.ComputeCost(treasureCard);
					if (this.RealThis._Game.Table.Supplies.Values.Any(supply => supply.Count > 0 && (supply.Category & Category.Treasure) == Category.Treasure && supply.CurrentCost.Coin > treasureCardCost.Coin && supply.CurrentCost.Potion >= treasureCardCost.Potion))
						return true;
				}
				return false;
			}
			else if (card.CardType == Cards.Base.TypeClass.Moneylender)
			{
				// Don't play if no Copper cards in hand
				if (this.RealThis.Hand[Cards.Universal.TypeClass.Copper].Count == 0)
					return false;
			}
			else if (card.CardType == Cards.Base.TypeClass.ThroneRoom)
			{
				// Only play if there's at least 1 card in hand that we *can* play
				if (this.RealThis.Hand[Category.Action].Any(c => ShouldBuy(c.CardType) && c.CardType != Cards.Base.TypeClass.ThroneRoom && c.CardType != Cards.Prosperity.TypeClass.KingsCourt))
					return true;
				return false;
			}
			else if (card.CardType == Cards.Seaside.TypeClass.Island)
			{
				// Only play Island if we have another Victory-only card in hand
				if (this.RealThis.Hand.Count(c => c.Category == Category.Victory) < 1)
					return false;
			}
			else if (card.CardType == Cards.Seaside.TypeClass.Outpost)
			{
				// Don't play if we're already in our 2nd turn
				if (previousTurn != null && previousTurn.Player == this)
					return false;
			}
			else if (card.CardType == Cards.Seaside.TypeClass.Tactician)
			{
				// Never play Tactician if there's one in play and we don't have anything to gain from playing another
				if (this.RealThis.SetAside[Cards.Seaside.TypeClass.Tactician].Count > 0 &&
					(this.RealThis.Hand.Count == 1 ||
					(this.RealThis.Currency.Coin + this.RealThis.Hand[Category.Treasure].Sum(c => c.Benefit.Currency.Coin.Value)) > 4 ||
					this.RealThis.Hand[Category.Action].Count() > 2))
					return false;
			}
			else if (card.CardType == Cards.Seaside.TypeClass.Smugglers)
			{
				Player playerToRight = this.RealThis._Game.GetPlayerFromIndex(this, -1);
				Turn mostRecentTurn = this.RealThis._Game.TurnsTaken.LastOrDefault(turn => turn.Player == playerToRight);
				if (mostRecentTurn == null)
					return false;

				// Only play Smugglers if the player to our right gained a card costing at least 2 (base price)
				if (mostRecentTurn.CardsGained.Any(c => c.BaseCost.Coin >= 2 && c.BaseCost.Potion == 0))
					return true;

				return false;
			}
			// Yes, the AI should be smart enough to know exactly how many Copper cards are in its own discard pile
			else if (card.CardType == Cards.Prosperity.TypeClass.CountingHouse)
			{
				if (this.RealThis.DiscardPile.LookThrough(c => c.CardType == Cards.Universal.TypeClass.Copper).Count == 0)
					return false;
			}
			else if (card.CardType == Cards.Prosperity.TypeClass.KingsCourt)
			{
				// Only play if there's at least 1 card in hand that we *can* play
				if (this.RealThis.Hand[Category.Action].Any(c => ShouldBuy(c.CardType) && c.CardType != Cards.Base.TypeClass.ThroneRoom && c.CardType != Cards.Prosperity.TypeClass.KingsCourt))
					return true;
				return false;
			}
			else if (card.CardType == Cards.Prosperity.TypeClass.Mint)
			{
				foreach (Card treasureCard in this.RealThis.Hand[Category.Treasure])
				{
					// We don't care about copying Copper cards
					if (treasureCard.CardType == Cards.Universal.TypeClass.Copper)
						continue;

					if (this.RealThis._Game.Table.Supplies.ContainsKey(treasureCard) && this.RealThis._Game.Table.Supplies[treasureCard].Count > 0)
						return true;
				}
				return false;
			}
			else if (card.CardType == Cards.Prosperity.TypeClass.Watchtower)
			{
				// Don't play this if we're already at 6 cards (after playing it, of course)
				if (this.RealThis.Hand.Count > 6)
					return false;
			}

			return true;
		}

		protected override IEnumerable<Card> FindBestCardsToDiscard(IEnumerable<Card> cards, int count)
		{
			// choose the worse card in hand in this order
			// 1) Tunnel
			// 2) positive victory points
			// 3) Curse
			// 4) Ruins
			// 5) cheapest card left

			return cards.OrderByDescending(c => this.ComputeDiscardValue(c)).Take(count);
			//IEnumerable<Card> cardsToDiscard = cards;
			//CardCollection cardsLeftOver = new CardCollection();
			//cardsToDiscard = cards.Where(c => c.CardType == Cards.Hinterlands.TypeClass.Tunnel);
			//if (cardsToDiscard.Count() >= count)
			//    return cardsToDiscard.Take(count);
			//cardsToDiscard = cardsToDiscard.Concat(cards.Where(c => c.Category == Category.Victory));
			//if (cardsToDiscard.Count() >= count)
			//    return cardsToDiscard.Take(count);
			//cardsToDiscard = cardsToDiscard.Concat(cards.Where(c => c.Category == Category.Curse));
			//if (cardsToDiscard.Count() >= count)
			//    return cardsToDiscard.Take(count);
			//cardsToDiscard = cardsToDiscard.Concat(cards.Where(c => (c.Category & Category.Ruins) == Category.Ruins));
			//if (cardsToDiscard.Count() >= count)
			//    return cardsToDiscard.Take(count);
			//cardsToDiscard = cardsToDiscard.Concat(FindBestCardsToTrash(cards.Except(cardsToDiscard), count - cardsToDiscard.Count()));
			//return cardsToDiscard.Take(count);
		}

		/// <summary>
		/// Computes the value of discarding this card
		/// </summary>
		/// <param name="card"></param>
		/// <returns>Value of discarding this card.  Higher value means it has higher priority to discard</returns>
		private double ComputeDiscardValue(Card card)
		{
			// Currently, these values are arbitrary.  This is another good candidate for evolving algorithms.
			if (card.CardType == Cards.Hinterlands.TypeClass.Tunnel)
				return 100.0;
			else if ((card.Category & Category.Curse) == Category.Curse)
				return 85.0;
			else if ((card.Category & Category.Victory) == Category.Victory && (card.Category & Category.Treasure) != Category.Treasure && (card.Category & Category.Action) != Category.Action)
				return 80.0;
			else if ((card.Category & Category.Ruins) == Category.Ruins)
				return 50.0;

			// Generic worth of these cards is based on their cost
			// I need to figure out how to scale this properly
			return (45.0 - 5 * ComputeValueInDeck(card));
		}

		/// <summary>
		/// Computes the value of this card for its worth to be in the player's deck.  This is usually just the cost, but not always.
		/// </summary>
		/// <param name="card"></param>
		/// <returns>In-deck value of this card.  Higher value means it has higher value for keeping around (sort-of the inverse of Trash value)</returns>
		private double ComputeValueInDeck(Card card)
		{
			// Curse's value is considered -1.0 (Yes, really)
			if (card.CardType == Cards.Universal.TypeClass.Curse)
				return -1.0;

			// Prize's cost of 0 makes this a slightly hard thing to judge, but 7 is a good starting point
			else if ((card.Category & Category.Prize) == Category.Prize)
				return 7.0;
			// I don't think Peddler is *worth* 8 coins -- its main power is with other cards seeing its cost of 8 coins
			else if (card.CardType == Cards.Prosperity.TypeClass.Peddler)
				return 4.0;
			// The main utility of this card is its gain ability.  Otherwise, it's only very slightly better than a normal Village
			else if (card.CardType == Cards.Hinterlands.TypeClass.BorderVillage)
				return 3.1;
			// Once gained, Cache is (almost) just as good as a Gold
			else if (card.CardType == Cards.Hinterlands.TypeClass.Cache)
				return 5.9;
			// Once gained, Embassy's value goes up slightly
			else if (card.CardType == Cards.Hinterlands.TypeClass.Embassy)
				return 5.3;
			// Farmland is tricky -- its main benefit is being trashed by other Farmlands or things like Remodel into Provinces
			else if (card.CardType == Cards.Hinterlands.TypeClass.Farmland)
				return 4.5;
			// I consider this card less than a Silver after it's been gained
			else if (card.CardType == Cards.Hinterlands.TypeClass.IllGottenGains)
				return 2.5;
			// Inn's value drops slightly after being gained, because it's a weakish Village
			else if (card.CardType == Cards.Hinterlands.TypeClass.Inn)
				return 3.5;
			// Nomad Camp basically turns into a Woodcutter after being gained
			else if (card.CardType == Cards.Hinterlands.TypeClass.NomadCamp)
				return 3.1;
			// Madman's pretty sweet -- basically like a delayed, when-you-want-it Tactician
			else if (card.CardType == Cards.DarkAges.TypeClass.Madman)
				return 5.5;
			// I'm not entirely sure about the overall worth of Mercenary.  I think for the Standard AI, it's low because it's a trasher
			else if (card.CardType == Cards.DarkAges.TypeClass.Mercenary)
				return 3.0;
			// A one-shot Gold is probably worth about 5 -- just a guess though
			else if (card.CardType == Cards.DarkAges.TypeClass.Spoils)
				return 5.0;
			// Doctor's value goes down slightly after being bought
			else if (card.CardType == Cards.Guilds.TypeClass.Doctor)
				return 2.1;
			// Herald's value goes down slightly after being bought -- it's slightly better than a Village
			else if (card.CardType == Cards.Guilds.TypeClass.Herald)
				return 3.5;
			// Masterpiece turns into slightly better than a Copper after being bought
			else if (card.CardType == Cards.Guilds.TypeClass.Masterpiece)
				return 1.0;
			// Stonemason's value goes down slightly after being bought
			else if (card.CardType == Cards.Guilds.TypeClass.Stonemason)
				return 1.5;

			// With no Curses left, some cards' values drops considerably
			if (this.RealThis._Game.Table.Curse.Count <= 0)
			{
				// Witch turns into something weaker than Smithy with no Curses
				if (card.CardType == Cards.Base.TypeClass.Witch)
					return 3.5;
				// Sea Hag turns completely useless with no Curses
				if (card.CardType == Cards.Seaside.TypeClass.SeaHag)
					return 0.0;
				// Familiar turns all but useless with no Curses
				if (card.CardType == Cards.Alchemy.TypeClass.Familiar)
					return 2.0;
				// Young Witch becomes very near worthless with no Curses
				if (card.CardType == Cards.Cornucopia.TypeClass.YoungWitch)
					return 2.0;
			}

			// This is the default Play value of each card.  If nothing else is defined above, this is what is used
			return card.BaseCost.Coin.Value + 2.5 * card.BaseCost.Potion.Value;
		}

		protected override IEnumerable<Card> FindBestCardsToTrash(IEnumerable<Card> cards, int count)
		{
			return FindBestCardsToTrash(cards, count, false);
		}

		protected IEnumerable<Card> FindBestCardsToTrash(IEnumerable<Card> cards, int count, Boolean onlyReturnTrashables)
		{
			// choose the worse card in hand in this order
			// 1) curse
			// 2) any ruins
			// 3) Sea Hag if there are no curses left
			// 4) Loan if we have fewer than 3 Coppers left
			// 5) Copper if we've got a lot of better Treasure
			// 6) Fortress
			// (If onlyReturnTrashables is false):
			// 7) lowest value from ComputeValueInDeck

			CardCollection cardsToTrash = new CardCollection();
			cardsToTrash.AddRange(cards.Where(c => c.Category == Category.Curse).Take(count));
			if (cardsToTrash.Count >= count)
				return cardsToTrash;

			cardsToTrash.AddRange(cards.Where(c => (c.Category & Category.Ruins) == Category.Ruins).Take(count - cardsToTrash.Count));
			if (cardsToTrash.Count >= count)
				return cardsToTrash;

			if (this.RealThis._Game.Table.Curse.Count <= 1)
			{
				cardsToTrash.AddRange(cards.Where(c => c.CardType == Cards.Seaside.TypeClass.SeaHag).Take(count - cardsToTrash.Count));
				if (cardsToTrash.Count >= count)
					return cardsToTrash;
			}

			cardsToTrash.AddRange(cards.Where(c => c.CardType == Cards.DarkAges.TypeClass.OvergrownEstate).Take(count - cardsToTrash.Count));
			if (cardsToTrash.Count >= count)
				return cardsToTrash;

			cardsToTrash.AddRange(cards.Where(c => c.CardType == Cards.DarkAges.TypeClass.Hovel).Take(count - cardsToTrash.Count));
			if (cardsToTrash.Count >= count)
				return cardsToTrash;

			if (this.RealThis.CountAll(this.RealThis, c => (c.Category & Category.Treasure) == Category.Treasure && (c.Benefit.Currency.Coin > 1 || c.CardType == Cards.Prosperity.TypeClass.Bank)) >=
				this.RealThis.CountAll(this.RealThis, c => c.CardType == Cards.Universal.TypeClass.Copper))
			{
				cardsToTrash.AddRange(cards.Where(c => c.CardType == Cards.Universal.TypeClass.Copper).Take(count - cardsToTrash.Count));
				if (cardsToTrash.Count >= count)
					return cardsToTrash;
			}

			if (this.RealThis.CountAll(this.RealThis, c => c.CardType == Cards.Universal.TypeClass.Copper) < 3)
			{
				cardsToTrash.AddRange(cards.Where(c => c.CardType == Cards.Prosperity.TypeClass.Loan).Take(count - cardsToTrash.Count));
				if (cardsToTrash.Count >= count)
					return cardsToTrash;
			}

			cardsToTrash.AddRange(cards.Where(c => c.CardType == Cards.DarkAges.TypeClass.Fortress).Take(count - cardsToTrash.Count));
			if (cardsToTrash.Count >= count)
				return cardsToTrash;

			if (!onlyReturnTrashables)
				cardsToTrash.AddRange(cards.OrderBy(c => ComputeValueInDeck(c)).ThenBy(c => c.Name).Where(c => !cardsToTrash.Contains(c)).Take(count - cardsToTrash.Count));

			return cardsToTrash;
		}

		protected override IEnumerable<Card> FindBestCards(IEnumerable<Card> cards, int count)
		{
			// Choose the most expensive cards

			CardCollection cardsToReturn = new CardCollection();

			cardsToReturn.AddRange(cards.OrderByDescending(c => c.BaseCost).ThenBy(c => c.Name).Take(count));

			return cardsToReturn;
		}

		protected virtual Boolean IsCardOKForMeToDiscard(Card card)
		{
			if ((card.Category & Category.Curse) == Category.Curse)
				return true;
			if ((card.Category & Category.Ruins) == Category.Ruins)
				return true;
			if ((card.Category & Category.Victory) == Category.Victory && 
				(card.Category & Category.Action) != Category.Action && 
				(card.Category & Category.Treasure) != Category.Treasure)
				return true;
			if (card.CardType == Cards.Universal.TypeClass.Copper)
				return true;
			if (card.CardType == Cards.Hinterlands.TypeClass.Tunnel)
				return true;
			if (card.CardType == Cards.DarkAges.TypeClass.Hovel)
				return true;

			return false;
		}

		protected override ChoiceResult Decide_Attacked(Choice choice, AttackedEventArgs aea, IEnumerable<Type> cardsToReveal)
		{
			// Always reveal my Moat if the attack hasn't been cancelled yet
			if (!aea.Cancelled && cardsToReveal.Contains(Cards.Base.TypeClass.Moat))
				return new ChoiceResult(new CardCollection() { aea.Revealable[Cards.Base.TypeClass.Moat].Card });

			// Always reveal my Secret Chamber if it hasn't been revealed yet
			if ((_LastReactedCard == null || _LastReactedCard != choice.CardTriggers[0]) &&
				cardsToReveal.Contains(Cards.Intrigue.TypeClass.SecretChamber) &&
				!aea.HandledBy.Contains(Cards.Intrigue.TypeClass.SecretChamber))
				return new ChoiceResult(new CardCollection() { aea.Revealable[Cards.Intrigue.TypeClass.SecretChamber].Card });

			// Always reveal my Horse Traders if I can
			if (cardsToReveal.Contains(Cards.Cornucopia.TypeClass.HorseTraders))
				return new ChoiceResult(new CardCollection() { aea.Revealable[Cards.Cornucopia.TypeClass.HorseTraders].Card });

			// Always reveal my Horse Traders if I can
			if (cardsToReveal.Contains(Cards.DarkAges.TypeClass.Beggar))
			{
				// Don't reveal Beggar for these Attacks -- Also, keep Beggar around if Young Witch is played and Beggar is the Bane card
				if (aea.AttackCard.CardType != Cards.Base.TypeClass.Thief && aea.AttackCard.CardType != Cards.Seaside.TypeClass.PirateShip &&
					aea.AttackCard.CardType != Cards.Hinterlands.TypeClass.NobleBrigand && 
					(aea.AttackCard.CardType != Cards.Cornucopia.TypeClass.YoungWitch || !this.RealThis._Game.Table.Supplies[Cards.DarkAges.TypeClass.Beggar].Tokens.Any(t => t.GetType() != Cards.Cornucopia.TypeClass.BaneToken)))
				{
					return new ChoiceResult(new CardCollection() { aea.Revealable[Cards.DarkAges.TypeClass.Beggar].Card });
				}
			}

			return new ChoiceResult(new CardCollection());
		}
		protected override ChoiceResult Decide_CardBuy(Choice choice, CardBuyEventArgs cbea, IEnumerable<Type> cardTriggerTypes)
		{
			Type cardType = cardTriggerTypes.FirstOrDefault(t =>
				t == Cards.Seaside.TypeClass.EmbargoToken || t == Cards.Prosperity.TypeClass.Hoard ||
				t == Cards.Prosperity.TypeClass.Mint || t == Cards.Prosperity.TypeClass.Talisman ||
				t == Cards.Hinterlands.TypeClass.Farmland || t == Cards.Hinterlands.TypeClass.Haggler ||
				t == Cards.Hinterlands.TypeClass.NobleBrigand || t == Cards.Guilds.TypeClass.Doctor ||
				t == Cards.Guilds.TypeClass.Herald || t == Cards.Guilds.TypeClass.Masterpiece ||
				t == Cards.Guilds.TypeClass.Stonemason);

			if (cardType != null)
				return new ChoiceResult(new List<String>() { cbea.Actions[cardType].Text });

			return base.Decide_CardBuy(choice, cbea, cardTriggerTypes);
		}
		protected override ChoiceResult Decide_CardGain(Choice choice, CardGainEventArgs cgea, IEnumerable<Type> cardTriggerTypes)
		{
			// Always reveal & trash this if we don't have 2 or more in hand
			if (choice.PlayerSource != this && cardTriggerTypes.Contains(Cards.Hinterlands.TypeClass.FoolsGold))
			{
				if (this.RealThis.Hand[Cards.Hinterlands.TypeClass.FoolsGold].Count < 2)
					return new ChoiceResult(new List<String>() { cgea.Actions[Cards.Hinterlands.TypeClass.FoolsGold].Text });
			}

			// Always reveal Trader when Gaining a Curse or Copper -- Silver (or even nothing) is better anyway
			// This should happen before Watchtower -- we'll assume that gaining a Silver is better
			// than trashing the Curse or Copper
			if (cardTriggerTypes.Contains(Cards.Hinterlands.TypeClass.Trader))
			{
				if (choice.CardTriggers[0].Category == Category.Curse || choice.CardTriggers[0].CardType == Cards.Universal.TypeClass.Copper)
					return new ChoiceResult(new List<String>() { cgea.Actions[Cards.Hinterlands.TypeClass.Trader].Text });
			}

			// Always put card on top of your deck
			if (cardTriggerTypes.Contains(Cards.Prosperity.TypeClass.RoyalSeal))
			{
				// Only put non-Curse & non-Victory-only cards on top of your deck
				if (choice.CardTriggers[0].Category != Category.Curse && 
					choice.CardTriggers[0].Category != Category.Victory && 
					choice.CardTriggers[0].CardType != Cards.Universal.TypeClass.Copper)
					return new ChoiceResult(new List<String>() { cgea.Actions[Cards.Prosperity.TypeClass.RoyalSeal].Text });
			}

			// Always reveal for Curse & Copper cards from a Watchtower (to trash)
			if (cardTriggerTypes.Contains(Cards.Prosperity.TypeClass.Watchtower))
			{
				if (choice.CardTriggers[0].Category != Category.Curse &&
					choice.CardTriggers[0].Category != Category.Victory &&
					choice.CardTriggers[0].CardType != Cards.Universal.TypeClass.Copper)
					return new ChoiceResult(new List<String>() { cgea.Actions[Cards.Prosperity.TypeClass.Watchtower].Text });
			}

			// Eh... it's an OK card, I guess... don't really want too many of them
			// Especially if we have lots of Action cards and very few +2 or more Action cards
			if (cardTriggerTypes.Contains(Cards.Hinterlands.TypeClass.Duchess))
			{
				double duchessMultipleActionCards = this.RealThis.CountAll(this.RealThis, c => (c.GroupMembership & Group.PlusMultipleActions) == Group.PlusMultipleActions, true, false);
				double duchessTotalActionCards = this.RealThis.CountAll(this.RealThis, c => (c.Category & Category.Action) == Category.Action, true, false);
				double duchessDuchesses = this.RealThis.CountAll(this.RealThis, c => c.CardType == Cards.Hinterlands.TypeClass.Duchess, true, false);
				double duchessTotalCards = this.RealThis.CountAll();

				if ((duchessTotalActionCards / duchessTotalCards) < 0.075 ||
					(duchessDuchesses / duchessTotalActionCards) < 0.20 ||
					(duchessMultipleActionCards / duchessTotalActionCards) > 0.33)
					return new ChoiceResult(new List<String>() { cgea.Actions[Cards.Hinterlands.TypeClass.Duchess].Text });
			}

			// Dunno what to do -- choose a random IsRequired one or nothing if there are none
			IEnumerable<CardGainAction> requiredActions = cgea.Actions.Select(kvp => kvp.Value).Where(a => a.IsRequired);
			if (requiredActions.Count() > 0)
			{
				int index = this._Game.RNG.Next(requiredActions.Count());
				return new ChoiceResult(new List<String>() { cgea.Actions[requiredActions.ElementAt(index).Card.CardType].Text });
			}

			return new ChoiceResult(new List<String>());
		}
		protected override ChoiceResult Decide_CardsDiscard(Choice choice, CardsDiscardEventArgs cdea, IEnumerable<Tuple<Type, Type>> cardTriggerTypes)
		{
			// Always put Treasury on my deck if I can
			if (cardTriggerTypes.Any(t => t.Item1 == Cards.Seaside.TypeClass.Treasury))
				return new ChoiceResult(new List<String>() { cdea.GetAction(Cards.Seaside.TypeClass.Treasury).Text });

			// Always put Alchemist on my deck if I can
			if (cardTriggerTypes.Any(t => t.Item1 == Cards.Alchemy.TypeClass.Alchemist))
				return new ChoiceResult(new List<String>() { cdea.GetAction(Cards.Alchemy.TypeClass.Alchemist).Text });

			// Only perform Herbalist if there's at least 1 non-Copper Treasure card in play
			if (cardTriggerTypes.Any(t => t.Item1 == Cards.Alchemy.TypeClass.Herbalist))
				return new ChoiceResult(new List<String>() { cdea.GetAction(Cards.Alchemy.TypeClass.Herbalist).Text });

			// Always reveal this when discarding
			if (cardTriggerTypes.Any(t => t.Item1 == Cards.Hinterlands.TypeClass.Tunnel))
				return new ChoiceResult(new List<String>() { cdea.GetAction(Cards.Hinterlands.TypeClass.Tunnel).Text });

			IEnumerable<CardsDiscardAction> schemeOptions = cdea.Actions.Where(kvp => kvp.Key.Item1 == Cards.Hinterlands.TypeClass.Scheme).Select(kvp => kvp.Value).OrderBy(cda => ((ICard)cda.Data).BaseCost.Coin.Value + 2.5f * ((ICard)cda.Data).BaseCost.Potion.Value);
			if (schemeOptions.Count() > 0)
				return new ChoiceResult(new List<String>() { schemeOptions.ElementAt(0).Text });

			// Dunno what to do -- choose a random IsRequired one or nothing if there are none
			IEnumerable<KeyValuePair<Tuple<Type, Type>, CardsDiscardAction>> requiredActions = cdea.Actions.Where(a => a.Value.IsRequired);
			if (requiredActions.Count() > 0)
			{
				int index = this._Game.RNG.Next(requiredActions.Count());
				return new ChoiceResult(new List<String>() { cdea.Actions[requiredActions.ElementAt(index).Key].Text });
			}

			return new ChoiceResult(new List<String>());
		}
		protected override ChoiceResult Decide_CleaningUp(Choice choice, CleaningUpEventArgs cuea, IEnumerable<Type> cardTriggerTypes)
		{
			// Always choose a card with Scheme if I can (I should always be able to, yes?)
			if (cardTriggerTypes.Contains(Cards.Hinterlands.TypeClass.Scheme))
				return new ChoiceResult(new List<String>() { cuea.Actions[Cards.Hinterlands.TypeClass.Scheme].Text });

			// Always put Walled Village on my deck if I can
			if (cardTriggerTypes.Contains(Cards.Promotional.TypeClass.WalledVillage))
				return new ChoiceResult(new List<String>() { cuea.Actions[Cards.Promotional.TypeClass.WalledVillage].Text });

			return new ChoiceResult(new List<String>());
		}
		protected override ChoiceResult Decide_Trash(Choice choice, TrashEventArgs tea, IEnumerable<Type> cardTriggerTypes)
		{
			// Resolve Fortress first -- there's no reason not to
			if (cardTriggerTypes.Contains(Cards.DarkAges.TypeClass.Fortress))
				return new ChoiceResult(new List<String>() { tea.Actions[Cards.DarkAges.TypeClass.Fortress].Text });

			// Always reveal Market Square when we can
			if (cardTriggerTypes.Contains(Cards.DarkAges.TypeClass.MarketSquare))
				return new ChoiceResult(new List<String>() { tea.Actions[Cards.DarkAges.TypeClass.MarketSquare].Text });

			// Resolve Sir Vander next -- not sure if any of these even matter
			if (cardTriggerTypes.Contains(Cards.DarkAges.TypeClass.SirVander))
				return new ChoiceResult(new List<String>() { tea.Actions[Cards.DarkAges.TypeClass.SirVander].Text });

			// Resolve Feodum next -- not sure if any of these even matter
			if (cardTriggerTypes.Contains(Cards.DarkAges.TypeClass.Feodum))
				return new ChoiceResult(new List<String>() { tea.Actions[Cards.DarkAges.TypeClass.Feodum].Text });

			// Resolve Squire next -- not sure if any of these even matter
			if (cardTriggerTypes.Contains(Cards.DarkAges.TypeClass.Squire))
				return new ChoiceResult(new List<String>() { tea.Actions[Cards.DarkAges.TypeClass.Squire].Text });

			// Resolve Catacombs next -- not sure if any of these even matter
			if (cardTriggerTypes.Contains(Cards.DarkAges.TypeClass.Catacombs))
				return new ChoiceResult(new List<String>() { tea.Actions[Cards.DarkAges.TypeClass.Catacombs].Text });

			// Resolve Rats next -- not sure if any of these even matter
			if (cardTriggerTypes.Contains(Cards.DarkAges.TypeClass.Rats))
				return new ChoiceResult(new List<String>() { tea.Actions[Cards.DarkAges.TypeClass.Rats].Text });

			// Resolve Overgrown Estate next -- not sure if any of these even matter
			if (cardTriggerTypes.Contains(Cards.DarkAges.TypeClass.OvergrownEstate))
				return new ChoiceResult(new List<String>() { tea.Actions[Cards.DarkAges.TypeClass.OvergrownEstate].Text });

			// Resolve Cultist next -- not sure if any of these even matter
			if (cardTriggerTypes.Contains(Cards.DarkAges.TypeClass.Cultist))
				return new ChoiceResult(new List<String>() { tea.Actions[Cards.DarkAges.TypeClass.Cultist].Text });

			// Resolve Hunting Grounds last -- not sure if any of these even matter
			if (cardTriggerTypes.Contains(Cards.DarkAges.TypeClass.HuntingGrounds))
				return new ChoiceResult(new List<String>() { tea.Actions[Cards.DarkAges.TypeClass.HuntingGrounds].Text });

			// Dunno what to do -- choose a random IsRequired one or nothing if there are none
			IEnumerable<TrashAction> requiredActions = tea.Actions.Select(kvp => kvp.Value).Where(a => a.IsRequired);
			if (requiredActions.Count() > 0)
			{
				int index = this._Game.RNG.Next(requiredActions.Count());
				return new ChoiceResult(new List<String>() { tea.Actions[requiredActions.ElementAt(index).Card.CardType].Text });
			}

			return new ChoiceResult(new List<String>());
		}
		protected override ChoiceResult Decide_CardsReorder(Choice choice)
		{
			CardCollection cards = new CardCollection(choice.Cards);
			// Order them in roughly random order
			Utilities.Shuffler.Shuffle(cards);
			return new ChoiceResult(cards);
		}
		protected override ChoiceResult Decide_RevealBane(Choice choice)
		{
			// Always reveal the Bane card if I can
			return new ChoiceResult(new List<String>() { choice.Options[0].Text });
		}
		protected override ChoiceResult Decide_Advisor(Choice choice)
		{
			// Find most-expensive non-Victory card to discard
			// Focus only on Treasure cards if there are no Actions remaining
			Card cardAdvisor = null;
			foreach (Card card in choice.Cards)
			{
				if (((card.Category & Category.Treasure) == Category.Treasure ||
					(_Game.TurnsTaken.Last().Player.Actions > 0 && (card.Category & Category.Action) == Category.Action))
					&&
					(cardAdvisor == null || this.ComputeValueInDeck(card) > this.ComputeValueInDeck(cardAdvisor)))
					cardAdvisor = card;
			}

			if (cardAdvisor != null)
				return new ChoiceResult(new CardCollection() { cardAdvisor });

			return base.Decide_Advisor(choice);
		}
		protected override ChoiceResult Decide_Altar(Choice choice)
		{
			switch (choice.ChoiceType)
			{
				case ChoiceType.Cards:
					return new ChoiceResult(new CardCollection(FindBestCardsToTrash(choice.Cards, 1)));

				case ChoiceType.Supplies:
					return new ChoiceResult(FindBestCardForCost(choice.Supplies.Values, null, false));

				default:
					return base.Decide_Altar(choice);
			}
		}
		protected override ChoiceResult Decide_Ambassador(Choice choice)
		{
			switch (choice.ChoiceType)
			{
				case ChoiceType.Options:
					// Always return as many copies as we can
					return new ChoiceResult(new List<String>() { choice.Options[choice.Options.Count - 1].Text });

				case ChoiceType.Cards:
					return new ChoiceResult(new CardCollection(this.FindBestCardsToTrash(choice.Cards, 1)));

				default:
					return base.Decide_Ambassador(choice);
			}
		}
		protected override ChoiceResult Decide_Apothecary(Choice choice)
		{
			CardCollection apothCards = new CardCollection(choice.Cards);
			// Order them in roughly random order
			Utilities.Shuffler.Shuffle(apothCards);
			return new ChoiceResult(apothCards);
		}
		protected override ChoiceResult Decide_Apprentice(Choice choice)
		{
			return new ChoiceResult(new CardCollection(this.FindBestCardsToTrash(choice.Cards, 1)));
		}
		protected override ChoiceResult Decide_Armory(Choice choice)
		{
			return new ChoiceResult(FindBestCardForCost(choice.Supplies.Values, null, false));
		}
		protected override ChoiceResult Decide_Baron(Choice choice)
		{
			// Always discard an Estate if I can
			return new ChoiceResult(new List<String>() { choice.Options[0].Text });
		}
		protected override ChoiceResult Decide_Bishop(Choice choice)
		{
			if (choice.Text.StartsWith("Trash a card."))
			{
				// All of this logic is based on the assumption that all costs are standard.
				// Obviously, Bridge, Princess, Highway, and even Quarry can muck this up

				// Trash Curses first
				Card bishopBestCard = choice.Cards.FirstOrDefault(c => (c.Category & Category.Curse) == Category.Curse);

				// Useless Seahags & Familiars go next
				if (bishopBestCard == null && _Game.Table.Curse.Count < _Game.Players.Count)
					bishopBestCard = choice.Cards.FirstOrDefault(c => c.CardType == Cards.Seaside.TypeClass.SeaHag || c.CardType == Cards.Alchemy.TypeClass.Familiar);

				// Rats are usually a great choice as well
				if (bishopBestCard == null)
					bishopBestCard = choice.Cards.FirstOrDefault(c => c.CardType == Cards.DarkAges.TypeClass.Rats);

				// Ruins go next
				if (bishopBestCard == null)
					bishopBestCard = choice.Cards.FirstOrDefault(c => (c.Category & Category.Ruins) == Category.Ruins);

				// Estates are usually a great choice as well
				if (bishopBestCard == null)
					bishopBestCard = choice.Cards.FirstOrDefault(c => c.CardType == Cards.Universal.TypeClass.Estate);

				// Overgrown Estates are a good choice as well
				if (bishopBestCard == null)
					bishopBestCard = choice.Cards.FirstOrDefault(c => c.CardType == Cards.DarkAges.TypeClass.OvergrownEstate);

				// Fortress is sweet -- it comes right back into my hand
				if (bishopBestCard == null)
					bishopBestCard = choice.Cards.FirstOrDefault(c => c.CardType == Cards.DarkAges.TypeClass.Fortress);

				// Hovels aren't horrible to trash
				if (bishopBestCard == null)
					bishopBestCard = choice.Cards.FirstOrDefault(c => c.CardType == Cards.DarkAges.TypeClass.Hovel);

				// Trasher cards (since we really won't play them anyway) are also good
				// As are cards that are pretty useless in certain situations
				// e.g. Coppersmith, Counting House, Loan, or Spice Merchant with very little Copper available,
				// Treasure Map with plenty of gold already, Potion with no Supply piles costing potions left
				if (bishopBestCard == null)
					bishopBestCard = choice.Cards.FirstOrDefault(c =>
						(c.CardType == Cards.Base.TypeClass.Bureaucrat && this.GameProgress < 0.4) ||
						(c.CardType == Cards.Base.TypeClass.Moneylender && this.RealThis.CountAll(this.RealThis, cC => cC.CardType == Cards.Universal.TypeClass.Copper, true, false) < 3) ||
						c.CardType == Cards.Base.TypeClass.Remodel ||
						(c.CardType == Cards.Intrigue.TypeClass.Coppersmith && this.RealThis.CountAll(this.RealThis, cC => cC.CardType == Cards.Universal.TypeClass.Copper, true, false) < 5) ||
						(c.CardType == Cards.Intrigue.TypeClass.Ironworks && this.GameProgress < 0.4) ||
						c.CardType == Cards.Intrigue.TypeClass.Masquerade ||
						c.CardType == Cards.Intrigue.TypeClass.TradingPost ||
						c.CardType == Cards.Intrigue.TypeClass.Upgrade ||
						c.CardType == Cards.Seaside.TypeClass.Ambassador ||
						c.CardType == Cards.Seaside.TypeClass.Lookout ||
						c.CardType == Cards.Seaside.TypeClass.Salvager ||
						(c.CardType == Cards.Seaside.TypeClass.TreasureMap && this.RealThis.CountAll(this.RealThis, cG => cG.CardType == Cards.Universal.TypeClass.Gold, true, false) > 2) ||
						(c.CardType == Cards.Alchemy.TypeClass.Potion && !_Game.Table.Supplies.Any(kvp => kvp.Value.BaseCost.Potion.Value > 0 && kvp.Value.CanGain())) ||
						(c.CardType == Cards.Prosperity.TypeClass.CountingHouse && this.RealThis.CountAll(this.RealThis, cC => cC.CardType == Cards.Universal.TypeClass.Copper, true, false) < 5) ||
						c.CardType == Cards.Prosperity.TypeClass.Expand ||
						c.CardType == Cards.Prosperity.TypeClass.Forge ||
						(c.CardType == Cards.Prosperity.TypeClass.Loan && this.RealThis.CountAll(this.RealThis, cC => cC.CardType == Cards.Universal.TypeClass.Copper, true, false) < 3) ||
						c.CardType == Cards.Cornucopia.TypeClass.Remake ||
						c.CardType == Cards.Hinterlands.TypeClass.Develop ||
						(c.CardType == Cards.Hinterlands.TypeClass.SpiceMerchant && this.RealThis.CountAll(this.RealThis, cC => cC.CardType == Cards.Universal.TypeClass.Copper || cC.CardType == Cards.Prosperity.TypeClass.Loan, true, false) < 4) ||
						(c.CardType == Cards.DarkAges.TypeClass.HuntingGrounds && this.GameProgress < 0.4) ||
						(c.CardType == Cards.DarkAges.TypeClass.SirVander && this.GameProgress < 0.3) ||
						(c.CardType == Cards.DarkAges.TypeClass.Armory && this.GameProgress < 0.4) ||
						c.CardType == Cards.Guilds.TypeClass.Stonemason
						);

				// Copper is a distant 10th
				if (bishopBestCard == null)
					bishopBestCard = choice.Cards.FirstOrDefault(c => c.CardType == Cards.Universal.TypeClass.Copper);

				// Masterpiece's main benefit is its on-buy ability, so might as well trash it now
				// Same goes for Ill-Gotten Gain's on-gain ability
				if (bishopBestCard == null)
					bishopBestCard = choice.Cards.FirstOrDefault(c => c.CardType == Cards.Guilds.TypeClass.Masterpiece || c.CardType == Cards.Hinterlands.TypeClass.IllGottenGains);

				// If a suitable one's STILL not been found, allow Peddler, well, because getting an extra 4 VPs off Peddler is *AMAZING*
				if (bishopBestCard == null)
					bishopBestCard = choice.Cards.FirstOrDefault(c => c.CardType == Cards.Prosperity.TypeClass.Peddler);

				// Otherwise, choose a non-Victory card to trash
				if (bishopBestCard == null)
				{
					IEnumerable<Card> bishCards = this.FindBestCardsToTrash(choice.Cards.Where(c => (c.Category & Category.Victory) != Category.Victory), 1);
					if (bishCards.Count() > 0)
						bishopBestCard = bishCards.ElementAt(0);
				}

				// Duchies or Dukes are usually an OK choice
				if (bishopBestCard == null)
					bishopBestCard = choice.Cards.FirstOrDefault(c =>
						c.CardType == Cards.Universal.TypeClass.Duchy || c.CardType == Cards.Intrigue.TypeClass.Duke
						);


				// OK, last chance... just PICK one!
				if (bishopBestCard == null)
				{
					IEnumerable<Card> bishCards = this.FindBestCardsToTrash(choice.Cards, 1);
					if (bishCards.Count() > 0)
						bishopBestCard = bishCards.ElementAt(0);
				}

				if (bishopBestCard != null)
					return new ChoiceResult(new CardCollection() { bishopBestCard });

				return new ChoiceResult(new CardCollection(this.FindBestCardsToDiscard(choice.Cards, 1)));
			}
			else // Optionally trash a card -- other players
			{
				// Always choose to trash a Curse from Bishop if I have one
				if (choice.Cards.Count(c => c.CardType == Cards.Universal.TypeClass.Curse) > 0)
					return new ChoiceResult(new CardCollection() { choice.Cards.First(c => c.CardType == Cards.Universal.TypeClass.Curse) });
				// Always choose to trash a Ruins from Bishop if I have one
				else if (choice.Cards.Count(c => (c.Category & Category.Ruins) == Category.Ruins) > 0)
					return new ChoiceResult(new CardCollection() { choice.Cards.First(c => (c.Category & Category.Ruins) == Category.Ruins) });
				// Also trash Overgrown Estates
				else if (choice.Cards.Count(c => c.CardType == Cards.DarkAges.TypeClass.OvergrownEstate) > 0)
					return new ChoiceResult(new CardCollection() { choice.Cards.First(c => c.CardType == Cards.DarkAges.TypeClass.OvergrownEstate) });
				// And Hovels, too... why not
				else if (choice.Cards.Count(c => c.CardType == Cards.DarkAges.TypeClass.Hovel) > 0)
					return new ChoiceResult(new CardCollection() { choice.Cards.First(c => c.CardType == Cards.DarkAges.TypeClass.Hovel) });
				else
					return new ChoiceResult(new CardCollection());
			}
		}
		protected override ChoiceResult Decide_BorderVillage(Choice choice)
		{
			return new ChoiceResult(FindBestCardForCost(choice.Supplies.Values, null, false));
		}
		protected override ChoiceResult Decide_Bureaucrat(Choice choice)
		{
			return new ChoiceResult(new CardCollection() { choice.Cards.ElementAt(this._Game.RNG.Next(choice.Cards.Count())) });
		}
		protected override ChoiceResult Decide_Butcher(Choice choice)
		{
			switch (choice.ChoiceType)
			{
				case ChoiceType.Options:
					int coinTokens = this.RealThis.TokenPiles[Cards.Guilds.TypeClass.CoinToken].Count;
					int spendCoinTokens = 0;
					double previous_best_score = -1.0;
					List<Supply> gainableSupplies = new List<Supply>();

					Cost trashedCardCost = this.RealThis._Game.ComputeCost(this.RealThis.CurrentTurn.CardsTrashed[this.RealThis.CurrentTurn.CardsTrashed.Count - 1]);

					for (int coinTokenCount = 0; coinTokenCount <= coinTokens; coinTokenCount++)
					{
						gainableSupplies.Clear();
						Dictionary<double, List<Supply>> scores = new Dictionary<double, List<Supply>>();
						foreach (Supply supply in this.RealThis._Game.Table.Supplies.Values)
						{
							if (supply.CanGain() && supply.CurrentCost <= trashedCardCost + new Currencies.Coin(coinTokenCount) && this.ShouldBuy(supply))
							{
								double score = this.ComputeValueInDeck(supply.TopCard);
								if (!scores.ContainsKey(score))
									scores[score] = new List<Supply>();
								scores[score].Add(supply);
							}
						}

						double bestScore = scores.Keys.OrderByDescending(k => k).FirstOrDefault();
						if (bestScore > 0 && (previous_best_score < 0 || bestScore >= (coinTokenCount - spendCoinTokens + 1) + previous_best_score))
						{
							spendCoinTokens = coinTokenCount;
							previous_best_score = bestScore;
						}
					}

					return new ChoiceResult(new List<String>() { choice.Options.First(o => o.Text == spendCoinTokens.ToString()).Text });

				case ChoiceType.Cards:
					return new ChoiceResult(new CardCollection(FindBestCardsToTrash(choice.Cards, 1)));

				case ChoiceType.Supplies:
					return new ChoiceResult(choice.Supplies.Values.OrderByDescending(s => this.ComputeValueInDeck(s.TopCard)).First());

				default:
					return base.Decide_Butcher(choice);
			}
		}
		protected override ChoiceResult Decide_Catacombs(Choice choice)
		{
			switch (choice.ChoiceType)
			{
				case ChoiceType.Options:
					// This is only OK -- it should scale the "discardability" of Action cards & Treasure cards according to how many Actions 
					// I have available to play.  0 Actions should ramp up Action cards and ramp down Treasure cards.  Multiple Actions should
					// do the inverse, though not quite as extremely
					double totalDeckDiscardability = this.RealThis.SumAll(this.RealThis, c => true, c => this.ComputeDiscardValue(c), true, true);
					int totalCards = this.RealThis.CountAll(this.RealThis, c => true, true, true);
					double cardsDiscardability = choice.CardTriggers.Sum(c => this.ComputeDiscardValue(c));

					// If it's better to keep these cards than discard them
					if (cardsDiscardability / choice.CardTriggers.Count >= totalDeckDiscardability / totalCards)
						return new ChoiceResult(new List<String>() { choice.Options[0].Text });
					else
						return new ChoiceResult(new List<String>() { choice.Options[1].Text });

				case ChoiceType.Supplies:
					return new ChoiceResult(FindBestCardForCost(choice.Supplies.Values, null, false));

				default:
					return base.Decide_Catacombs(choice);
			}
		}
		protected override ChoiceResult Decide_Cartographer(Choice choice)
		{
			if (choice.Text.StartsWith("Choose cards to discard"))
			{
				// Grab all cards that we don't really care about
				return new ChoiceResult(new CardCollection(choice.Cards.Where(c => IsCardOKForMeToDiscard(c))));
			}
			else
			{
				CardCollection cartCards = new CardCollection(choice.Cards);
				// Order them in roughly random order
				Utilities.Shuffler.Shuffle(cartCards);
				return new ChoiceResult(cartCards);
			}
		}
		protected override ChoiceResult Decide_Cellar(Choice choice)
		{
			CardCollection cellarCards = new CardCollection();
			// TODO -- the AI makes slightly bad decisions when it comes to Cellar -- Fix me!
			cellarCards.AddRange(choice.Cards.Where(c => c.CardType == Cards.Base.TypeClass.Cellar));
			cellarCards.AddRange(choice.Cards.Where(c => c.CardType == Cards.Universal.TypeClass.Curse));
			cellarCards.AddRange(choice.Cards.Where(c => (c.Category & Category.Ruins) == Category.Ruins));
			cellarCards.AddRange(choice.Cards.Where(c => c.CardType == Cards.Hinterlands.TypeClass.Tunnel));
			cellarCards.AddRange(choice.Cards.Where(c => c.CardType == Cards.DarkAges.TypeClass.Hovel));
			cellarCards.AddRange(choice.Cards.Where(c => c.CardType == Cards.DarkAges.TypeClass.OvergrownEstate));
			cellarCards.AddRange(choice.Cards.Where(c => c.CardType == Cards.DarkAges.TypeClass.Rats));
			cellarCards.AddRange(choice.Cards.Where(c => (c.Category == Category.Victory && c.CardType != Cards.Universal.TypeClass.Estate && c.CardType != Cards.Universal.TypeClass.Province)));
			if (choice.Cards.Count(c => c.CardType == Cards.Universal.TypeClass.Estate) > 0)
				cellarCards.AddRange(choice.Cards.Where(c => c.CardType == Cards.Universal.TypeClass.Estate).Take(choice.Cards.Count(c => c.CardType == Cards.Universal.TypeClass.Estate) - choice.Cards.Count(c => c.CardType == Cards.Intrigue.TypeClass.Baron)));
			if (choice.Cards.Count(c => c.CardType == Cards.Universal.TypeClass.Province) > 0)
				cellarCards.AddRange(choice.Cards.Where(c => c.CardType == Cards.Universal.TypeClass.Province).Take(choice.Cards.Count(c => c.CardType == Cards.Universal.TypeClass.Province) - choice.Cards.Count(c => c.CardType == Cards.Cornucopia.TypeClass.Tournament)));
			if (this.GameProgress < 0.63)
				cellarCards.AddRange(choice.Cards.Where(c => c.CardType == Cards.Universal.TypeClass.Copper || c.CardType == Cards.Guilds.TypeClass.Masterpiece));

			return new ChoiceResult(cellarCards);
		}
		protected override ChoiceResult Decide_Chancellor(Choice choice)
		{
			// Never put deck into discard pile later in the game -- we don't want those Victory cards back in our deck sooner
			if (this.GameProgress < 0.30)
				return new ChoiceResult(new List<String>() { choice.Options[1].Text });
			return new ChoiceResult(new List<String>() { choice.Options[0].Text });
		}
		protected override ChoiceResult Decide_Chapel(Choice choice)
		{
			CardCollection chapelToTrash = new CardCollection();

			// Always choose to trash all Curses
			chapelToTrash.AddRange(choice.Cards.Where(c => c.CardType == Cards.Universal.TypeClass.Curse).Take(4));
			// Always choose to trash all Ruins
			chapelToTrash.AddRange(choice.Cards.Where(c => (c.Category & Category.Ruins) == Category.Ruins).Take(4 - chapelToTrash.Count));

			return new ChoiceResult(chapelToTrash);
		}
		protected override ChoiceResult Decide_Count(Choice choice)
		{
			switch (choice.ChoiceType)
			{
				case ChoiceType.Options:
					String option = String.Empty;
					// First choice
					if (choice.Options[0].Text == "Discard 2 cards")
					{
						// This is it for now -- very complicated decision-making to decide if we have 2 cards that we're willing to discard
						// The assumption here is that we should want to play everything that's of value in our hand (which is very bad, in general)
						if (this.RealThis.Hand.Count(c =>
							c.CardType == Cards.Universal.TypeClass.Curse ||
							c.Category == Category.Victory ||
							(c.Category & Category.Ruins) == Category.Ruins ||
							((c.Category & Category.Action) == Category.Action && this.RealThis.Actions <= 0) ||
							(c.CardType == Cards.Seaside.TypeClass.SeaHag && this.RealThis._Game.Table.Curse.Count < this.RealThis._Game.Players.Count) ||
							(c.CardType == Cards.Prosperity.TypeClass.CountingHouse && this.RealThis.DiscardPile.Count(dc => dc.CardType == Cards.Universal.TypeClass.Copper) == 0) ||
							(c.CardType == Cards.Base.TypeClass.Moneylender && this.RealThis.Hand.Count(hc => hc.CardType == Cards.Universal.TypeClass.Copper) == 0) ||
							((c.CardType == Cards.Base.TypeClass.ThroneRoom || c.CardType == Cards.Prosperity.TypeClass.KingsCourt || c.CardType == Cards.DarkAges.TypeClass.Procession) && 
									this.RealThis.Hand.Count(hc => 
										(hc.Category & Category.Action) == Category.Action && 
										hc.CardType != Cards.Base.TypeClass.ThroneRoom && 
										hc.CardType != Cards.Prosperity.TypeClass.KingsCourt && 
										hc.CardType != Cards.DarkAges.TypeClass.Procession) == 0) ||
							(c.CardType == Cards.Seaside.TypeClass.TreasureMap && this.RealThis.Hand.Count(hc => hc.CardType == Cards.Seaside.TypeClass.TreasureMap) < 2)
							) > 2)
							option = choice.Options[0].Text;
						else
							option = choice.Options[1].Text;
					}
					// Second choice
					else
					{
						if (this.GameProgress <= 0.4 && this.RealThis._Game.Table.Duchy.CanGain())
							option = choice.Options[2].Text;
						else if (( // Trash our hand if we have more than 3 of only Curse/Copper/Ruins/Rats cards
							this.RealThis.Hand.Count == this.RealThis.Hand[c => c.CardType == Cards.Universal.TypeClass.Copper ||
														c.CardType == Cards.Universal.TypeClass.Curse ||
														(c.Category & Category.Ruins) == Category.Ruins ||
														c.CardType == Cards.DarkAges.TypeClass.Rats].Count)
							&&
							this.RealThis.Hand.Count > 3
							)
							option = choice.Options[1].Text;
						else
							option = choice.Options[0].Text;
					}

					if (option != String.Empty)
						return new ChoiceResult(new List<String>() { option });
					break;

				case ChoiceType.Cards:
					if (choice.Text == "Discard 2 cards.")
					{
						return new ChoiceResult(new CardCollection(this.FindBestCardsToDiscard(choice.Cards, 2)));
					}
					else if (choice.Text == "Choose a card to put back on your deck")
					{
						return new ChoiceResult(new CardCollection(this.FindBestCardsToDiscard(choice.Cards, 1)));
					}
					break;
				
			}
			return base.Decide_Count(choice);
		}
		protected override ChoiceResult Decide_Counterfeit(Choice choice)
		{
			// This is a first stab at a decision-making strategy for Counterfeit
			// It's not entirely efficient or good at what it does, but it's at least better than picking a random Treasure from time to time
			// There are some amazing power moves like a late-game Counterfeit'ed Platinum or Bank that can be pretty amazingly devastating
			// but more analysis needs to be done to find out the exact timing for stuff like that.  A decision tree to know how much buying
			// power we currently have and how much we need to do certain things would be really nice & help this choice out immensely
			Card counterfeitingCard = null;

			// Spoils are first, since they're super, super awesome w/ Counterfeit
			counterfeitingCard = choice.Cards.FirstOrDefault(c => c.CardType == Cards.DarkAges.TypeClass.Spoils);

			// If we've got a few Coppers left, let's do one of those
			// Masterpiece is basically in the same boat as Copper, so include that as well
			if (counterfeitingCard == null && this.RealThis.CountAll(this.RealThis, c => c.CardType == Cards.Universal.TypeClass.Copper || c.CardType == Cards.Guilds.TypeClass.Masterpiece, true, false) > 4)
				counterfeitingCard = choice.Cards.FirstOrDefault(card => card.CardType == Cards.Universal.TypeClass.Copper || card.CardType == Cards.Guilds.TypeClass.Masterpiece);

			// If we've got a Loan and not many Coppers left, we don't want to keep the Loan around anyway
			if (counterfeitingCard == null && this.RealThis.CountAll(this.RealThis, c => c.CardType == Cards.Universal.TypeClass.Copper, true, false) < 4)
				counterfeitingCard = choice.Cards.FirstOrDefault(card => card.CardType == Cards.Prosperity.TypeClass.Loan);

			// If the game progress is at least 1/2-way & we've got at least 3 buys, then let's use a Quarry
			if (counterfeitingCard == null && this.GameProgress < 0.50 && this.RealThis.Buys > 2)
				counterfeitingCard = choice.Cards.FirstOrDefault(card => card.CardType == Cards.Prosperity.TypeClass.Quarry);

			// If the game progress is at least 1/3-way & we've got at least 6 Silvers or at least 4 cards better than a Silver, let's use a Silver
			if (counterfeitingCard == null && this.GameProgress < 0.66 &&
				(this.RealThis.CountAll(this.RealThis, c => c.CardType == Cards.Universal.TypeClass.Silver, true, false) >= 6 ||
				this.RealThis.CountAll(this.RealThis, c => c.CardType == Cards.Universal.TypeClass.Gold || c.CardType == Cards.Intrigue.TypeClass.Harem || 
					c.CardType == Cards.Alchemy.TypeClass.PhilosophersStone || c.CardType == Cards.Prosperity.TypeClass.Bank || 
					c.CardType == Cards.Prosperity.TypeClass.Platinum || c.CardType == Cards.Prosperity.TypeClass.RoyalSeal || 
					c.CardType == Cards.Prosperity.TypeClass.Venture || c.CardType == Cards.Hinterlands.TypeClass.Cache, true, false) >= 6))
				counterfeitingCard = choice.Cards.FirstOrDefault(card => card.CardType == Cards.Universal.TypeClass.Silver);

			// If we've got an Ill-Gotten Gains, use that (it's main use is the Gain, anyway)
			if (counterfeitingCard == null)
				counterfeitingCard = choice.Cards.FirstOrDefault(card => card.CardType == Cards.Hinterlands.TypeClass.IllGottenGains);

			if (counterfeitingCard != null)
				return new ChoiceResult(new CardCollection() { counterfeitingCard });

			// Don't play anything
			return new ChoiceResult(new CardCollection());
		}
		protected override ChoiceResult Decide_CountingHouse(Choice choice)
		{
			// Always grab all of the coppers
			return new ChoiceResult(new List<String>() { choice.Options[choice.Options.Count - 1].Text });
		}
		protected override ChoiceResult Decide_Contraband(Choice choice)
		{
			// Let's guess how many coins the player has left.
			// We'll assume roughly 2 coins per card left in his hand (scale that slightly based on how early in the game it is)
			double multiplier = 2.0;
			if (this.GameProgress > 0.85)
				multiplier = 1.75;
			else if (this.GameProgress > 0.65)
				multiplier = 1.85;
			int remainingCoins = Math.Max(0, (int)(0.5 + multiplier * choice.PlayerSource.Hand.Count + 3 * Utilities.Gaussian.NextGaussian(this._Game.RNG)));
			Supply supply = null;
			while (supply == null)
			{
				supply = FindBestCardForCost(choice.Supplies.Values.Where(
					s => s.Count > 0 && !s.Tokens.Any(t => t.GetType() == Cards.Prosperity.TypeClass.ContrabandToken)),
					choice.PlayerSource.Currency + new Currencies.Coin(remainingCoins), false);
				remainingCoins++;
			}
			return new ChoiceResult(supply);
		}
		protected override ChoiceResult Decide_Courtyard(Choice choice)
		{
			return new ChoiceResult(new CardCollection(this.FindBestCardsToDiscard(choice.Cards, 1)));
		}
		protected override ChoiceResult Decide_Cultist(Choice choice)
		{
			// Always return "Yes"
			return new ChoiceResult(new List<String>() { choice.Options[0].Text }); // Yes
		}
		protected override ChoiceResult Decide_DameAnna(Choice choice)
		{
			if (choice.Text == "Choose up to 2 cards to trash")
			{
				CardCollection dameAnnaToTrash = new CardCollection();

				// Always choose to trash all Curses
				dameAnnaToTrash.AddRange(choice.Cards.Where(c => c.CardType == Cards.Universal.TypeClass.Curse).Take(2));
				// Always choose to trash all Ruins
				dameAnnaToTrash.AddRange(choice.Cards.Where(c => (c.Category & Category.Ruins) == Category.Ruins).Take(2 - dameAnnaToTrash.Count));

				return new ChoiceResult(dameAnnaToTrash);
			}
			else if (choice.Text == "Choose a card to trash")
				return new ChoiceResult(new CardCollection(FindBestCardsToTrash(choice.Cards, 1)));
			else
				return base.Decide_Rogue(choice);
		}
		protected override ChoiceResult Decide_DameJosephine(Choice choice)
		{
			return new ChoiceResult(new CardCollection(FindBestCardsToTrash(choice.Cards, 1)));
		}
		protected override ChoiceResult Decide_DameMolly(Choice choice)
		{
			return new ChoiceResult(new CardCollection(FindBestCardsToTrash(choice.Cards, 1)));
		}
		protected override ChoiceResult Decide_DameNatalie(Choice choice)
		{
			if (choice.Text.StartsWith("You may gain a card"))
			{
				Supply supply = FindBestCardForCost(choice.Supplies.Values, null, false);
				if (supply.CardType == Cards.Universal.TypeClass.Curse || supply.CardType == Cards.Universal.TypeClass.Copper || (supply.Category & Category.Ruins) == Category.Ruins)
					supply = null;
				return new ChoiceResult(supply);
			}
			else if (choice.Text == "Choose a card to trash")
				return new ChoiceResult(new CardCollection(FindBestCardsToTrash(choice.Cards, 1)));
			else
				return base.Decide_Rogue(choice);
		}
		protected override ChoiceResult Decide_DameSylvia(Choice choice)
		{
			return new ChoiceResult(new CardCollection(FindBestCardsToTrash(choice.Cards, 1)));
		}
		protected override ChoiceResult Decide_DeathCart(Choice choice)
		{
			// There are a few cards we'd rather keep around other than Death Cart
			Card bestTrashCard = this.FindBestCardsToTrash(choice.Cards, 1).First();
			if (bestTrashCard.CardType == Cards.Base.TypeClass.CouncilRoom || bestTrashCard.CardType == Cards.Base.TypeClass.Festival ||
				bestTrashCard.CardType == Cards.Base.TypeClass.Laboratory || 
				bestTrashCard.CardType == Cards.Intrigue.TypeClass.Minion || bestTrashCard.CardType == Cards.Intrigue.TypeClass.Nobles || 
				bestTrashCard.CardType == Cards.Intrigue.TypeClass.Torturer ||
				bestTrashCard.CardType == Cards.Seaside.TypeClass.Bazaar || bestTrashCard.CardType == Cards.Seaside.TypeClass.GhostShip ||
				bestTrashCard.CardType == Cards.Seaside.TypeClass.MerchantShip || bestTrashCard.CardType == Cards.Seaside.TypeClass.Treasury || 
				bestTrashCard.CardType == Cards.Seaside.TypeClass.Wharf || 
				bestTrashCard.CardType == Cards.Alchemy.TypeClass.Golem || 
				bestTrashCard.CardType == Cards.Prosperity.TypeClass.City || bestTrashCard.CardType == Cards.Prosperity.TypeClass.Goons || 
				bestTrashCard.CardType == Cards.Prosperity.TypeClass.GrandMarket || bestTrashCard.CardType == Cards.Prosperity.TypeClass.KingsCourt || 
				bestTrashCard.CardType == Cards.Prosperity.TypeClass.Mountebank || bestTrashCard.CardType == Cards.Prosperity.TypeClass.Peddler || 
				bestTrashCard.CardType == Cards.Prosperity.TypeClass.Rabble || bestTrashCard.CardType == Cards.Prosperity.TypeClass.Vault || 
				bestTrashCard.CardType == Cards.Hinterlands.TypeClass.Embassy || bestTrashCard.CardType == Cards.Hinterlands.TypeClass.Highway ||
				bestTrashCard.CardType == Cards.Hinterlands.TypeClass.Mandarin || bestTrashCard.CardType == Cards.Hinterlands.TypeClass.Margrave ||
				bestTrashCard.CardType == Cards.Hinterlands.TypeClass.Stables || 
				bestTrashCard.CardType == Cards.DarkAges.TypeClass.Altar || bestTrashCard.CardType == Cards.DarkAges.TypeClass.BanditCamp || 
				bestTrashCard.CardType == Cards.DarkAges.TypeClass.Mystic || bestTrashCard.CardType == Cards.DarkAges.TypeClass.Pillage ||
				bestTrashCard.CardType == Cards.Guilds.TypeClass.Baker || bestTrashCard.CardType == Cards.Guilds.TypeClass.Journeyman ||
				bestTrashCard.CardType == Cards.Guilds.TypeClass.MerchantGuild || bestTrashCard.CardType == Cards.Guilds.TypeClass.Soothsayer ||
				(bestTrashCard.Category & Category.Prize) == Category.Prize ||
				(bestTrashCard.Category & Category.Knight) == Category.Knight || 
				(bestTrashCard.CardType == Cards.Base.TypeClass.Witch && this.RealThis._Game.Table.Curse.Count > this.RealThis._Game.Players.Count - 1) ||
				(bestTrashCard.CardType == Cards.DarkAges.TypeClass.Cultist && this.RealThis._Game.Table[Cards.DarkAges.TypeClass.RuinsSupply].Count > this.RealThis._Game.Players.Count - 1) ||
				(bestTrashCard.CardType == Cards.DarkAges.TypeClass.HuntingGrounds && this.GameProgress > 0.5))
				return new ChoiceResult(new CardCollection());
			return new ChoiceResult(new CardCollection() { bestTrashCard });
		}
		protected override ChoiceResult Decide_Develop(Choice choice)
		{
			switch (choice.ChoiceType)
			{
				case ChoiceType.Cards:
					return new ChoiceResult(new CardCollection(FindBestCardsToTrash(choice.Cards, 1)));

				case ChoiceType.Supplies:
					return new ChoiceResult(FindBestCardForCost(choice.Supplies.Values, null, false));

				default:
					return base.Decide_Develop(choice);
			}
		}
		protected override ChoiceResult Decide_Doctor(Choice choice)
		{
			switch (choice.ChoiceType)
			{
				case ChoiceType.Options:
					if (choice.Text.StartsWith("Do you want to discard"))
					{
						// Choose to trash Copper roughly 1/3 of the time (a little odd, but it should work decently)
						// Let's change this to be a bit more aggressive
						// If it's a Curse, Ruins, Hovel, Overgrown Estate, or Rats, trash it
						// If it's a Copper, or if it's a Silver/Talisman/Quarry and we have at least 1 Platinum and at least 3 Ventures, or if it's a Loan and we have fewer than 3 Coppers
						if (choice.CardTriggers[0].CardType == Cards.Universal.TypeClass.Curse ||
							(choice.CardTriggers[0].Category & Category.Ruins) == Category.Ruins ||
							choice.CardTriggers[0].CardType == Cards.DarkAges.TypeClass.Hovel ||
							choice.CardTriggers[0].CardType == Cards.DarkAges.TypeClass.OvergrownEstate ||
							choice.CardTriggers[0].CardType == Cards.DarkAges.TypeClass.Rats ||
							choice.CardTriggers[0].CardType == Cards.Universal.TypeClass.Copper ||
							((choice.CardTriggers[0].CardType == Cards.Universal.TypeClass.Silver ||
								choice.CardTriggers[0].CardType == Cards.Prosperity.TypeClass.Talisman ||
								choice.CardTriggers[0].CardType == Cards.Prosperity.TypeClass.Quarry) &&
								this.RealThis.CountAll(this.RealThis, c => c.CardType == Cards.Prosperity.TypeClass.Platinum, true, false) > 0 &&
								this.RealThis.CountAll(this.RealThis, c => c.CardType == Cards.Prosperity.TypeClass.Venture, true, false) > 3) ||
							(choice.CardTriggers[0].CardType == Cards.Prosperity.TypeClass.Loan &&
								this.RealThis.CountAll(this.RealThis, c => c.CardType == Cards.Universal.TypeClass.Copper, true, false) < 3))
							return new ChoiceResult(new List<String>() { choice.Options[1].Text }); // Trash
						else if (
								((choice.CardTriggers[0].Category & Category.Victory) == Category.Victory &&
								(choice.CardTriggers[0].Category & Category.Action) != Category.Action &&
								(choice.CardTriggers[0].Category & Category.Treasure) != Category.Treasure) ||
							choice.CardTriggers[0].CardType == Cards.Universal.TypeClass.Copper
							)
							return new ChoiceResult(new List<String>() { choice.Options[0].Text }); // Discard
						else
							return new ChoiceResult(new List<String>() { choice.Options[2].Text }); // Put it back
					}
					else
					{
						for (int index = choice.Options.Count - 1; index >= 0; index--)
						{
							// Overpay by up to 4
							Currency overpayAmount = new DominionBase.Currency(choice.Options[index].Text);
							if (overpayAmount.Potion.Value > 0 || overpayAmount.Coin.Value > 4)
								continue;

							return new ChoiceResult(new List<String>() { choice.Options[index].Text });
						}
						return base.Decide_Doctor(choice);
					}

				case ChoiceType.Cards:
					CardCollection doctorCards = new CardCollection(choice.Cards);
					// Order them in roughly random order
					Utilities.Shuffler.Shuffle(doctorCards);
					return new ChoiceResult(doctorCards);

				case ChoiceType.SuppliesAndCards:
					// This should choose cards we don't want.  Obvious first targets are Curses, Ruins, Coppers, and perhaps Estates (early, early on)
					if (this.RealThis.CountAll(this.RealThis, c => c.CardType == Cards.Universal.TypeClass.Curse, false, true) > 0)
						return new ChoiceResult(choice.Supplies.FirstOrDefault(kvp => kvp.Value.CardType == Cards.Universal.TypeClass.Curse).Value);

					// Let's get rid of Rats as quickly as possible -- We, as the AI, *HATE* Rats
					if (this.RealThis.CountAll(this.RealThis, c => c.CardType == Cards.DarkAges.TypeClass.Rats, false, true) > 0)
						return new ChoiceResult(choice.Supplies.FirstOrDefault(kvp => kvp.Value.CardType == Cards.DarkAges.TypeClass.Rats).Value);

					// These priorities are based on nothing more than my intuition of how "good" each of the following Ruins cards are
					if (this.RealThis.CountAll(this.RealThis, c => c.CardType == Cards.DarkAges.TypeClass.RuinedVillage, false, true) > 0)
						return new ChoiceResult(new CardCollection() { choice.Cards.FirstOrDefault(c => c.CardType == Cards.DarkAges.TypeClass.RuinedVillage) });
					if (this.RealThis.CountAll(this.RealThis, c => c.CardType == Cards.DarkAges.TypeClass.RuinedMarket, false, true) > 0)
						return new ChoiceResult(new CardCollection() { choice.Cards.FirstOrDefault(c => c.CardType == Cards.DarkAges.TypeClass.RuinedMarket) });
					if (this.RealThis.CountAll(this.RealThis, c => c.CardType == Cards.DarkAges.TypeClass.Survivors, false, true) > 0)
						return new ChoiceResult(new CardCollection() { choice.Cards.FirstOrDefault(c => c.CardType == Cards.DarkAges.TypeClass.Survivors) });

					if (this.RealThis.CountAll(this.RealThis, c => c.CardType == Cards.DarkAges.TypeClass.Hovel, false, true) > 0)
						return new ChoiceResult(new CardCollection() { choice.Cards.FirstOrDefault(c => c.CardType == Cards.DarkAges.TypeClass.Hovel) });

					if (this.RealThis.CountAll(this.RealThis, c => c.CardType == Cards.DarkAges.TypeClass.AbandonedMine, false, true) > 0)
						return new ChoiceResult(new CardCollection() { choice.Cards.FirstOrDefault(c => c.CardType == Cards.DarkAges.TypeClass.AbandonedMine) });
					if (this.RealThis.CountAll(this.RealThis, c => c.CardType == Cards.DarkAges.TypeClass.RuinedLibrary, false, true) > 0)
						return new ChoiceResult(new CardCollection() { choice.Cards.FirstOrDefault(c => c.CardType == Cards.DarkAges.TypeClass.RuinedLibrary) });

					if (this.RealThis.CountAll(this.RealThis, c => c.CardType == Cards.DarkAges.TypeClass.OvergrownEstate, false, true) > 0)
						return new ChoiceResult(new CardCollection() { choice.Cards.FirstOrDefault(c => c.CardType == Cards.DarkAges.TypeClass.OvergrownEstate) });

					if (this.GameProgress > 0.65 && this.RealThis.CountAll(this.RealThis, c => c.CardType == Cards.Universal.TypeClass.Estate, false, true) > 0)
						return new ChoiceResult(choice.Supplies.FirstOrDefault(kvp => kvp.Value.CardType == Cards.Universal.TypeClass.Estate).Value);
					if (this.GameProgress < 0.65 && this.RealThis.CountAll(this.RealThis, c => c.CardType == Cards.Universal.TypeClass.Copper, false, true) > 0)
						return new ChoiceResult(choice.Supplies.FirstOrDefault(kvp => kvp.Value.CardType == Cards.Universal.TypeClass.Copper).Value);

					// We don't want to trash anything useful, so just choose Curse
					return new ChoiceResult(choice.Supplies.FirstOrDefault(kvp => kvp.Value.CardType == Cards.Universal.TypeClass.Curse).Value);

				default:
					return base.Decide_Doctor(choice);
			}
		}
		protected override ChoiceResult Decide_Duchess(Choice choice)
		{
			if (this.IsCardOKForMeToDiscard(choice.CardTriggers[0]))
				return new ChoiceResult(new List<String>() { choice.Options[0].Text });
			else
				return new ChoiceResult(new List<String>() { choice.Options[1].Text });
		}
		protected override ChoiceResult Decide_Embargo(Choice choice)
		{
			List<Supply> embargoAbleSupplies = new List<Supply>();
			foreach (Supply supply in choice.Supplies.Values.Where(s => s.SupplyCardType != Cards.Universal.TypeClass.Curse))
			{
				// Only allow at most 4 Embargo tokens on a Supply pile
				if (!ShouldBuy(supply) && supply.Tokens.Count(t => t.GetType() == Cards.Seaside.TypeClass.EmbargoToken) < 4)
					embargoAbleSupplies.Add(supply);
			}
			if (embargoAbleSupplies.Count == 0)
				embargoAbleSupplies.Add(choice.Supplies[Cards.Universal.TypeClass.Province]);
			return new ChoiceResult(embargoAbleSupplies[this._Game.RNG.Next(embargoAbleSupplies.Count)]);
		}
		protected override ChoiceResult Decide_Embassy(Choice choice)
		{
			return new ChoiceResult(new CardCollection(this.FindBestCardsToDiscard(choice.Cards, 3)));
		}
		protected override ChoiceResult Decide_Envoy(Choice choice)
		{
			// Find most-expensive non-Victory card to discard
			// Focus only on Treasure cards if there are no Actions remaining
			Card cardEnvoy = null;
			foreach (Card card in choice.Cards)
			{
				if (((card.Category & Category.Treasure) == Category.Treasure ||
					(_Game.TurnsTaken.Last().Player.Actions > 0 && (card.Category & Category.Action) == Category.Action))
					&&
					(cardEnvoy == null || this.ComputeValueInDeck(card) > this.ComputeValueInDeck(cardEnvoy)))
					cardEnvoy = card;
			}

			if (cardEnvoy != null)
				return new ChoiceResult(new CardCollection() { cardEnvoy });

			return base.Decide_Envoy(choice);
		}
		protected override ChoiceResult Decide_Expand(Choice choice)
		{
			switch (choice.ChoiceType)
			{
				case ChoiceType.Cards:
					return new ChoiceResult(new CardCollection(FindBestCardsToTrash(choice.Cards, 1)));

				case ChoiceType.Supplies:
					return new ChoiceResult(FindBestCardForCost(choice.Supplies.Values, null, false));

				default:
					return base.Decide_Expand(choice);
			}
		}
		protected override ChoiceResult Decide_Explorer(Choice choice)
		{
			// Always reveal a Province if we can
			return new ChoiceResult(new List<String>() { choice.Options[0].Text });
		}
		protected override ChoiceResult Decide_Farmland(Choice choice)
		{
			switch (choice.ChoiceType)
			{
				case ChoiceType.Cards:
					// Always trash Curses if we can
					Card farmlandBestCard = choice.Cards.FirstOrDefault(c => (c.Category & Category.Curse) == Category.Curse);
					// Trashing a 9-cost non-Victory card for a Colony later in the game seems like a good idea
					if (farmlandBestCard == null && this.GameProgress < 0.5 && _Game.Table.Supplies.ContainsKey(Cards.Prosperity.TypeClass.Colony) && _Game.Table.Supplies[Cards.Prosperity.TypeClass.Colony].CanGain())
						farmlandBestCard = choice.Cards.FirstOrDefault(c => (c.Category & Category.Victory) != Category.Victory && c.BaseCost == new Cost(9));
					// Trashing a 6-cost non-Victory card for a Province later in the game seems like a good idea
					if (farmlandBestCard == null && this.GameProgress < 0.5 && _Game.Table[Cards.Universal.TypeClass.Province].CanGain())
						farmlandBestCard = choice.Cards.FirstOrDefault(c => (c.Category & Category.Victory) != Category.Victory && c.BaseCost == new Cost(6));
					// Trashing a 9-cost Victory card for a Colony seems like a good idea
					if (farmlandBestCard == null && _Game.Table.Supplies.ContainsKey(Cards.Prosperity.TypeClass.Colony) && _Game.Table.Supplies[Cards.Prosperity.TypeClass.Colony].CanGain())
						farmlandBestCard = choice.Cards.FirstOrDefault(c => (c.Category & Category.Victory) == Category.Victory && c.BaseCost == new Cost(9));
					// Trashing a 6-cost Victory card for a Province seems like a good idea
					if (farmlandBestCard == null && _Game.Table[Cards.Universal.TypeClass.Province].CanGain())
						farmlandBestCard = choice.Cards.FirstOrDefault(c => (c.Category & Category.Victory) == Category.Victory && c.BaseCost == new Cost(6));
					// Trashing Masterpiece for a Duchy (or other 5-cost?) seems good
					if (farmlandBestCard == null)
						farmlandBestCard = choice.Cards.FirstOrDefault(c => c.CardType == Cards.Guilds.TypeClass.Masterpiece);
					// Trash Copper later in the game -- they just suck
					if (farmlandBestCard == null && this.GameProgress < 0.65)
						farmlandBestCard = choice.Cards.FirstOrDefault(c => c.CardType == Cards.Universal.TypeClass.Copper);

					if (farmlandBestCard != null)
						return new ChoiceResult(new CardCollection() { farmlandBestCard });

					return new ChoiceResult(new CardCollection(this.FindBestCardsToTrash(choice.Cards, 1)));

				case ChoiceType.Supplies:
					return new ChoiceResult(FindBestCardForCost(choice.Supplies.Values, null, false));

				default:
					return base.Decide_Farmland(choice);
			}
		}
		protected override ChoiceResult Decide_Feast(Choice choice)
		{
			return new ChoiceResult(FindBestCardForCost(choice.Supplies.Values, null, false));
		}
		protected override ChoiceResult Decide_Followers(Choice choice)
		{
			return new ChoiceResult(new CardCollection(this.FindBestCardsToDiscard(choice.Cards, choice.Cards.Count() - 3)));
		}
		protected override ChoiceResult Decide_Forager(Choice choice)
		{
			if (this.RealThis._Game.Table.Trash.Count(c => (c.Category & Category.Treasure) == Category.Treasure) == 0 && choice.Cards.Any(c => c.CardType == Cards.Universal.TypeClass.Copper))
				return new ChoiceResult(new CardCollection() { choice.Cards.First(c => c.CardType == Cards.Universal.TypeClass.Copper) });
			return new ChoiceResult(new CardCollection(this.FindBestCardsToTrash(choice.Cards, 1)));
		}
		protected override ChoiceResult Decide_Forge(Choice choice)
		{
			switch (choice.ChoiceType)
			{
				case ChoiceType.Cards:
					// Only trash Curses & Ruins
					CardCollection forgeToTrash = new CardCollection();

					// Always choose to trash all Curses
					forgeToTrash.AddRange(choice.Cards.Where(c => c.CardType == Cards.Universal.TypeClass.Curse));
					// Always choose to trash all Ruins
					forgeToTrash.AddRange(choice.Cards.Where(c => (c.Category & Category.Ruins) == Category.Ruins));

					return new ChoiceResult(forgeToTrash);

				case ChoiceType.Supplies:
					return new ChoiceResult(FindBestCardForCost(choice.Supplies.Values, null, false));

				default:
					return base.Decide_Forge(choice);
			}
		}
		protected override ChoiceResult Decide_GhostShip(Choice choice)
		{
			return new ChoiceResult(new CardCollection(this.FindBestCardsToDiscard(choice.Cards, choice.Cards.Count() - 3)));
		}
		protected override ChoiceResult Decide_Golem(Choice choice)
		{
			// Just choose one at random.
			return new ChoiceResult(new CardCollection() { choice.Cards.ElementAt(this._Game.RNG.Next(choice.Cards.Count())) });
		}
		protected override ChoiceResult Decide_Goons(Choice choice)
		{
			return new ChoiceResult(new CardCollection(this.FindBestCardsToDiscard(choice.Cards, choice.Cards.Count() - 3)));
		}
		protected override ChoiceResult Decide_Governor(Choice choice)
		{
			switch (choice.ChoiceType)
			{
				case ChoiceType.Options:
					if (this.RealThis.Hand[c => c.CardType == Cards.Universal.TypeClass.Curse || (c.Category & Category.Ruins) == Category.Ruins].Count > 0)
						return new ChoiceResult(new List<String>() { choice.Options[2].Text }); // Trash a card

					return new ChoiceResult(new List<String>() { choice.Options[0].Text }); // +1(+3) Cards

				case ChoiceType.Cards:
					// Only ever trash Curses or Ruins
					if (choice.Cards.Count(c => c.CardType == Cards.Universal.TypeClass.Curse || (c.Category & Category.Ruins) == Category.Ruins) > 0)
						return new ChoiceResult(new CardCollection(FindBestCardsToTrash(choice.Cards, 1)));
					else
						return new ChoiceResult(new CardCollection());

				case ChoiceType.Supplies:
					return new ChoiceResult(FindBestCardForCost(choice.Supplies.Values, null, false));

				default:
					return base.Decide_Governor(choice);
			}
		}
		protected override ChoiceResult Decide_Graverobber(Choice choice)
		{
			switch (choice.ChoiceType)
			{
				case ChoiceType.Options:
					List<Cost> availableCosts = new List<Cost>() { new Cost(3), new Cost(4), new Cost(5), new Cost(6) };

					// If it's later in the game and there are Victory cards in the Trash between 3 & 6, then gain a Victory card
					if (this.GameProgress < 0.4 && this.RealThis._Game.Table.Trash.Count(c => availableCosts.Any(cost => cost == this.RealThis._Game.ComputeCost(c)) && (c.Category & Category.Victory) == Category.Victory) > 0)
						return new ChoiceResult(new List<String>() { choice.Options[0].Text }); // Gain a card from the trash
					// Choose to trash a Ruins from Graverobber if I have one
					else if (this.RealThis.Hand.Count(c => (c.Category & Category.Ruins) == Category.Ruins) > 0)
						return new ChoiceResult(new List<String>() { choice.Options[1].Text }); // Trash an Action card
					else
						return new ChoiceResult(new List<String>() { choice.Options[0].Text }); // Choose a card to gain from the trash

				case ChoiceType.Cards:
					if (choice.Text == "Choose a card to gain from the trash")
					{
						// If it's later in the game and there are Victory cards in the Trash between 3 & 6, then gain the best Victory card
						if (this.GameProgress < 0.4 && choice.Cards.Count(c => (c.Category & Category.Victory) == Category.Victory) > 0)
							return new ChoiceResult(new CardCollection(this.FindBestCards(choice.Cards.Where(c => (c.Category & Category.Victory) == Category.Victory), 1)));
						else
							return new ChoiceResult(new CardCollection(this.FindBestCards(choice.Cards, 1)));
					}
					else // "Choose an Action card to trash"
					{
						return new ChoiceResult(new CardCollection(FindBestCardsToTrash(choice.Cards, 1)));
					}

				case ChoiceType.Supplies:
					return new ChoiceResult(FindBestCardForCost(choice.Supplies.Values, null, false));

				default:
					return base.Decide_Graverobber(choice);
			}
		}
		protected override ChoiceResult Decide_Haggler(Choice choice)
		{
			return new ChoiceResult(FindBestCardForCost(choice.Supplies.Values, null, false));
		}
		protected override ChoiceResult Decide_Hamlet(Choice choice)
		{
			if (choice.Text == "You may discard a card for +1 Action.")
			{
				// Tunnel is always a great bet
				Card hamletABestCard = choice.Cards.FirstOrDefault(c => c.CardType == Cards.Hinterlands.TypeClass.Tunnel);

				// No? How about others...
				if (hamletABestCard == null)
				{
					int actionTerminationsLeft = this.RealThis.Hand.Count(c => this.ShouldPlay(c) && (c.Category & Category.Action) == Category.Action && (c.GroupMembership & Group.PlusAction) != Group.PlusAction);

					int actionSplitsLeft = this.RealThis.Hand.Count(c => this.ShouldPlay(c) && (c.Category & Category.Action) == Category.Action && (c.GroupMembership & Group.PlusMultipleActions) == Group.PlusMultipleActions);
					int actionChainsLeft = this.RealThis.Hand.Count(c => this.ShouldPlay(c) && (c.Category & Category.Action) == Category.Action && (c.GroupMembership & Group.PlusAction) == Group.PlusAction) - actionSplitsLeft;
					// Adjust this number a bit -- TR/KC can make an ActionChain an ActionSplit (end result of an extra 1 or 2 Actions left)
					actionSplitsLeft += Math.Min(this.RealThis.Hand[Cards.Base.TypeClass.ThroneRoom].Count, actionChainsLeft);
					actionSplitsLeft += Math.Min(2 * this.RealThis.Hand[Cards.Prosperity.TypeClass.KingsCourt].Count, actionChainsLeft);
					// Only the first Crossroads counts for action splitting, *AND HOW* if it does!
					if (this.RealThis.Hand[Cards.Hinterlands.TypeClass.Crossroads].Count > 0)
					{
						actionSplitsLeft -= Math.Max(0, this.RealThis.Hand[Cards.Hinterlands.TypeClass.Crossroads].Count - 1);
						if (this.RealThis.CurrentTurn.CardsPlayed.Any(c => c.CardType == Cards.Hinterlands.TypeClass.Crossroads))
							actionSplitsLeft--;
						else
							actionSplitsLeft++;
					}

					int actionPlayDeficit = this.RealThis.Actions - actionTerminationsLeft;
					// If we've got a deficit of Actions left to play our Action cards and we've got a candidate card to discard, then let's go for it!
					if (actionPlayDeficit < 0)
					{
						hamletABestCard = choice.Cards.FirstOrDefault(c =>
							(c.Category & Category.Curse) == Category.Curse ||
							(c.Category & Category.Ruins) == Category.Ruins ||
							((c.Category & Category.Victory) == Category.Victory &&
							(c.Category & Category.Action) != Category.Action &&
							(c.Category & Category.Treasure) != Category.Treasure));

						// Open it up to Coppers now
						if (hamletABestCard == null)
							hamletABestCard = choice.Cards.FirstOrDefault(c => c.CardType == Cards.Universal.TypeClass.Copper);
					}
				}

				if (hamletABestCard != null)
					return new ChoiceResult(new CardCollection() { hamletABestCard });

				return new ChoiceResult(new CardCollection());
			}
			else
			{
				// Tunnel is always a great bet
				Card hamletBBestCard = choice.Cards.FirstOrDefault(c => c.CardType == Cards.Hinterlands.TypeClass.Tunnel);

				// Fairly simple analysis on the +1 Buy front
				if (this.RealThis.Buys < 2 && this.RealThis.Hand[Category.Treasure].Sum(c => c.Benefit.Currency.Coin.Value) + this.RealThis.Currency.Coin.Value > 6)
					hamletBBestCard = choice.Cards.FirstOrDefault(c =>
						(c.Category & Category.Curse) == Category.Curse ||
						(c.Category & Category.Ruins) == Category.Ruins ||
						((c.Category & Category.Victory) == Category.Victory &&
						(c.Category & Category.Action) != Category.Action &&
						(c.Category & Category.Treasure) != Category.Treasure));

				if (hamletBBestCard != null)
					return new ChoiceResult(new CardCollection() { hamletBBestCard });

				return new ChoiceResult(new CardCollection());
			}
		}
		protected override ChoiceResult Decide_Haven(Choice choice)
		{
			Card havenBestCard = null;
			if (this.RealThis.Currency.Coin > 4)
			{
				havenBestCard = choice.Cards.Where(c => (c.Category & Category.Action) == Category.Action).OrderByDescending(c => this.ComputeValueInDeck(c)).FirstOrDefault();
				if (havenBestCard == null)
				{
					// If there are none, pick a random non-Treasure card instead
					IEnumerable<Card> havenNonTreasures = choice.Cards.Where(c => (c.Category & Category.Treasure) != Category.Treasure);
					return new ChoiceResult(new CardCollection() { havenNonTreasures.ElementAt(this._Game.RNG.Next(havenNonTreasures.Count())) });
				}
			}
			else
			{
				// We don't have a lot of gold, so choose our biggest coin
				IEnumerable<Card> havenTreasures = choice.Cards.Where(c => (c.Category & Category.Treasure) == Category.Treasure);
				// If there are no Treasures, try to grab Action cards instead
				if (havenTreasures.Count() == 0)
					havenTreasures = choice.Cards.Where(c => (c.Category & Category.Action) == Category.Action);
				// Just pick any old card
				if (havenTreasures.Count() == 0)
					havenTreasures = choice.Cards;
				return new ChoiceResult(new CardCollection() { havenTreasures.ElementAt(this._Game.RNG.Next(havenTreasures.Count())) });
			}
			// Just pick a random card if we still haven't found a decent one
			return new ChoiceResult(new CardCollection() { choice.Cards.ElementAt(this._Game.RNG.Next(choice.Cards.Count())) });
		}
		protected override ChoiceResult Decide_Herald(Choice choice)
		{
			switch (choice.ChoiceType)
			{
				case ChoiceType.Options:
					for (int index = choice.Options.Count - 1; index >= 0; index--)
					{
						// Overpay by up to the amount of cards we have in our discard pile,
						// excluding Coppers, Curses, Ruins, Victory cards, & Shelters
						Currency overpayAmount = new DominionBase.Currency(choice.Options[index].Text);
						if (overpayAmount.Potion.Value > 0)
							continue;

						if (this.DiscardPile.LookThrough(c => 
							c.CardType != Cards.Universal.TypeClass.Copper &&
							c.CardType != Cards.Universal.TypeClass.Curse &&
							(c.Category & Category.Ruins) != Category.Ruins &&
							(c.Category & Category.Shelter) != Category.Shelter &&
							c.Category != Category.Victory &&
							c.CardType != Cards.Hinterlands.TypeClass.Tunnel).Count >= overpayAmount.Coin.Value)
							return new ChoiceResult(new List<String>() { choice.Options[index].Text });
					}
					return base.Decide_Herald(choice);

				case ChoiceType.Cards:
					// Find highest in-deck value cards to put on top
					// While ignoring Victory cards, Curses, & Shelters
					CardCollection heraldCards = new CardCollection(choice.Cards
						.Where(c => c.Category != Category.Victory &&
							c.CardType != Cards.Universal.TypeClass.Curse &&
							c.CardType != Cards.Hinterlands.TypeClass.Tunnel &&
							(c.Category & Category.Shelter) != Category.Shelter)
						.OrderByDescending(c => ComputeValueInDeck(c))
						.Take(choice.Minimum));

					// If this doesn't lead to enough cards, then include Shelters
					if (heraldCards.Count < choice.Minimum)
						heraldCards.AddRange(choice.Cards
							.Where(c => (c.Category & Category.Shelter) == Category.Shelter)
							.Take(choice.Minimum - heraldCards.Count));

					// If this *still* doesn't include enough cards, include Victory cards
					if (heraldCards.Count < choice.Minimum)
						heraldCards.AddRange(choice.Cards
							.Where(c => c.Category == Category.Victory ||
								c.CardType == Cards.Hinterlands.TypeClass.Tunnel)
							.OrderByDescending(c => ComputeValueInDeck(c))
							.Take(choice.Minimum - heraldCards.Count));

					// Uh... our deck is absolutely shit.  Just take the first however many cards
					if (heraldCards.Count < choice.Minimum)
						heraldCards = new CardCollection(choice.Cards
						.OrderByDescending(c => ComputeValueInDeck(c))
						.Take(choice.Minimum));

					return new ChoiceResult(heraldCards);

				default:
					return base.Decide_Herald(choice);
			}
		}
		protected override ChoiceResult Decide_Herbalist(Choice choice)
		{
			// Always choose the Treasure card that costs the most to put on top, except if it's Copper (F Copper)
			Card bestCard = choice.Cards.
				OrderByDescending(card => (card.Category & Category.Prize) == Category.Prize ? new Cost(7) : card.BaseCost).
				FirstOrDefault();
			if (bestCard != null && bestCard.CardType != Cards.Universal.TypeClass.Copper)
				return new ChoiceResult(new CardCollection() { bestCard });

			return Decide_Herbalist(choice);
		}
		protected override ChoiceResult Decide_Hermit(Choice choice)
		{
			switch (choice.ChoiceType)
			{
				case ChoiceType.Cards:
					// Always choose to trash a Curse from Hermit if possible
					if (choice.Cards.Count(c => c.CardType == Cards.Universal.TypeClass.Curse) > 0)
						return new ChoiceResult(new CardCollection() { choice.Cards.First(c => c.CardType == Cards.Universal.TypeClass.Curse) });
					// Always choose to trash a Ruins from Hermit if possible
					else if (choice.Cards.Count(c => (c.Category & Category.Ruins) == Category.Ruins) > 0)
						return new ChoiceResult(new CardCollection() { choice.Cards.First(c => (c.Category & Category.Ruins) == Category.Ruins) });
					else
						return new ChoiceResult(new CardCollection());

				case ChoiceType.Supplies:
					return new ChoiceResult(FindBestCardForCost(choice.Supplies.Values, null, false));

				default:
					return base.Decide_Hermit(choice);
			}
		}
		protected override ChoiceResult Decide_HornOfPlenty(Choice choice)
		{
			// If it's early on, never gain a Victory card (since that trashes the Horn of Plenty)
			// Also, only do it about 2/5 the time after that
			if (this.GameProgress > 0.35 || this._Game.RNG.Next(5) <= 1)
				return new ChoiceResult(FindBestCardForCost(choice.Supplies.Values.Where(supply => (supply.Category & Category.Victory) != Category.Victory), null, false));
			else
				return new ChoiceResult(FindBestCardForCost(choice.Supplies.Values, null, false));
		}
		protected override ChoiceResult Decide_HorseTraders(Choice choice)
		{
			return new ChoiceResult(new CardCollection(this.FindBestCardsToDiscard(choice.Cards, 2)));
		}
		protected override ChoiceResult Decide_HuntingGrounds(Choice choice)
		{
			String hgChoice = choice.Options[0].Text;

			// Only choose Estates if there are no Duchies or if we're at the end game, there are at least 3 Estates, and we've got at least 1 Gardens/Silk Road
			if (this.RealThis._Game.Table.Duchy.Count == 0)
				hgChoice = choice.Options[1].Text;
			if (this.RealThis._Game.IsEndgameTriggered && this.RealThis._Game.Table.Estate.Count >= 3 &&
				this.RealThis.CountAll(this.RealThis, c => c.CardType == Cards.Base.TypeClass.Gardens || c.CardType == Cards.Hinterlands.TypeClass.SilkRoad) > 0)
				hgChoice = choice.Options[1].Text;

			return new ChoiceResult(new List<String>() { hgChoice });
		}
		protected override ChoiceResult Decide_IllGottenGains(Choice choice)
		{
			// Always take the Copper -- more money = better, right?
			return new ChoiceResult(new List<String>() { choice.Options[0].Text });
		}
		protected override ChoiceResult Decide_Inn(Choice choice)
		{
			if (choice.Text.StartsWith("Discard 2 cards"))
			{
				return new ChoiceResult(new CardCollection(this.FindBestCardsToDiscard(choice.Cards, 2)));
			}
			else
			{
				// Always select ALL Action cards we want to play
				return new ChoiceResult(new CardCollection(choice.Cards.Where(c => this.ShouldPlay(c))));
			}
		}
		protected override ChoiceResult Decide_Ironmonger(Choice choice)
		{
			if (this.IsCardOKForMeToDiscard(choice.CardTriggers[0]))
				return new ChoiceResult(new List<String>() { choice.Options[0].Text }); // Discard
			else
				return new ChoiceResult(new List<String>() { choice.Options[1].Text }); // Put back
		}
		protected override ChoiceResult Decide_Ironworks(Choice choice)
		{
			return new ChoiceResult(FindBestCardForCost(choice.Supplies.Values, null, false));
		}
		protected override ChoiceResult Decide_Island(Choice choice)
		{
			Card islandBestCard = choice.Cards.Where(c => c.Category == Category.Victory || c.Category == Category.Curse).OrderByDescending(c => this.ComputeValueInDeck(c)).FirstOrDefault(c => true);

			if (islandBestCard != null)
				return new ChoiceResult(new CardCollection() { islandBestCard });

			return new ChoiceResult(new CardCollection(this.FindBestCardsToDiscard(choice.Cards, 1)));
		}
		protected override ChoiceResult Decide_JackOfAllTrades(Choice choice)
		{
			switch (choice.ChoiceType)
			{
				case ChoiceType.Options:
					if (choice.CardTriggers[0].Category == Category.Victory ||
						choice.CardTriggers[0].CardType == Cards.Universal.TypeClass.Copper)
						return new ChoiceResult(new List<String>() { choice.Options[0].Text }); // Discard
					else if (choice.CardTriggers[0].Category == Category.Curse)
					{
						// Put a Curse or Ruins back if we can draw it and trash it (and we don't have a Curse or Ruins already in hand)
						if (this.RealThis.Hand.Count < 5 && this.RealThis.Hand[Category.Curse].Count == 0 && this.RealThis.Hand[Category.Ruins].Count == 0)
							return new ChoiceResult(new List<String>() { choice.Options[1].Text }); // Put it back
						else
							return new ChoiceResult(new List<String>() { choice.Options[1].Text }); // Discard
					}
					else
						return new ChoiceResult(new List<String>() { choice.Options[1].Text }); // Put it back

				case ChoiceType.Cards:
					// Only ever trash Curses, Ruins, and SeaHags if there are no Curses left
					Card joatCurse = choice.Cards.FirstOrDefault(c =>
						c.CardType == Cards.Universal.TypeClass.Curse ||
						(c.Category & Category.Ruins) == Category.Ruins ||
						(c.CardType == Cards.Seaside.TypeClass.SeaHag && !_Game.Table[Cards.Universal.TypeClass.Curse].CanGain()));
					if (joatCurse != null)
						return new ChoiceResult(new CardCollection() { joatCurse });

					return new ChoiceResult(new CardCollection());

				default:
					return base.Decide_JackOfAllTrades(choice);
			}
		}
		protected override ChoiceResult Decide_Jester(Choice choice)
		{
			Type cardType = choice.CardTriggers[0].CardType;
			// HUGE list of cards & criteria for which player gets the card
			if (cardType == Cards.Universal.TypeClass.Curse ||
				cardType == Cards.Universal.TypeClass.Copper ||
				(choice.CardTriggers[0].Category & Category.Ruins) == Category.Ruins ||
				(cardType == Cards.Base.TypeClass.Chapel && this.RealThis.CountAll(this.RealThis, c => c.CardType == Cards.Base.TypeClass.Chapel, true, false) > 2) ||
				(cardType == Cards.Base.TypeClass.Mine && this.RealThis.CountAll(this.RealThis, c => c.CardType == Cards.Base.TypeClass.Mine, true, false) > 2) ||
				(cardType == Cards.Base.TypeClass.Moneylender && this.RealThis.CountAll(this.RealThis, c => c.CardType == Cards.Universal.TypeClass.Copper, true, false) < 4) ||
				cardType == Cards.Base.TypeClass.Remodel ||
				(cardType == Cards.Base.TypeClass.Workshop && _Game.Table.Supplies.Count(kvp => kvp.Value.BaseCost == new Cost(4)) < 2) ||
				(cardType == Cards.Intrigue.TypeClass.Coppersmith && this.RealThis.CountAll(this.RealThis, c => c.CardType == Cards.Universal.TypeClass.Copper, true, false) < 4) ||
				(cardType == Cards.Intrigue.TypeClass.Ironworks && _Game.Table.Supplies.Count(kvp => kvp.Value.BaseCost == new Cost(4)) < 3) ||
				cardType == Cards.Intrigue.TypeClass.Masquerade ||
				cardType == Cards.Intrigue.TypeClass.TradingPost ||
				cardType == Cards.Seaside.TypeClass.Ambassador ||
				cardType == Cards.Seaside.TypeClass.Lookout ||
				cardType == Cards.Seaside.TypeClass.Salvager ||
				(cardType == Cards.Seaside.TypeClass.SeaHag && !_Game.Table[Cards.Universal.TypeClass.Curse].CanGain()) ||
				cardType == Cards.Seaside.TypeClass.TreasureMap ||
				(cardType == Cards.Prosperity.TypeClass.Contraband && this.RealThis.CountAll(this.RealThis, c => c.CardType == Cards.Prosperity.TypeClass.Contraband, true, false) > 3) ||
				(cardType == Cards.Prosperity.TypeClass.CountingHouse && this.RealThis.CountAll(this.RealThis, c => c.CardType == Cards.Universal.TypeClass.Copper, true, false) < 4) ||
				cardType == Cards.Prosperity.TypeClass.Expand ||
				cardType == Cards.Prosperity.TypeClass.Forge ||
				(cardType == Cards.Prosperity.TypeClass.Mint && this.RealThis.CountAll(this.RealThis, c => c.CardType == Cards.Prosperity.TypeClass.Mint, true, false) > 2) ||
				(cardType == Cards.Prosperity.TypeClass.Talisman &&
					(_Game.Table.Supplies.Count(kvp => kvp.Value.BaseCost == new Cost(4)) < 3 ||
					this.RealThis.CountAll(this.RealThis, c => c.CardType == Cards.Prosperity.TypeClass.Talisman, true, false) > 2)) ||
				cardType == Cards.Prosperity.TypeClass.TradeRoute ||
				(cardType == Cards.Cornucopia.TypeClass.HornOfPlenty && this.RealThis.CountAll(this.RealThis, c => c.CardType == Cards.Cornucopia.TypeClass.HornOfPlenty, true, false) > 3) ||
				cardType == Cards.Cornucopia.TypeClass.Remake ||
				(cardType == Cards.Cornucopia.TypeClass.YoungWitch && !_Game.Table[Cards.Universal.TypeClass.Curse].CanGain()) ||
				cardType == Cards.Hinterlands.TypeClass.Develop ||
				cardType == Cards.DarkAges.TypeClass.Procession ||
				cardType == Cards.DarkAges.TypeClass.Rats ||
				cardType == Cards.DarkAges.TypeClass.Rebuild ||
				cardType == Cards.Guilds.TypeClass.Masterpiece ||
				cardType == Cards.Guilds.TypeClass.Stonemason)
				return new ChoiceResult(new List<String>() { choice.Options[1].Text });
			else
				return new ChoiceResult(new List<String>() { choice.Options[0].Text });
		}
		protected override ChoiceResult Decide_JunkDealer(Choice choice)
		{
			return new ChoiceResult(new CardCollection(FindBestCardsToTrash(choice.Cards, 1)));
		}
		protected override ChoiceResult Decide_KingsCourt(Choice choice)
		{
			Card bestCard = this.FindBestCardToPlay(choice.Cards.Where(c =>
				c.CardType != Cards.Base.TypeClass.Chapel &&
				c.CardType != Cards.Base.TypeClass.Library &&
				c.CardType != Cards.Base.TypeClass.Remodel &&
				c.CardType != Cards.Intrigue.TypeClass.SecretChamber &&
				c.CardType != Cards.Intrigue.TypeClass.Upgrade &&
				c.CardType != Cards.Seaside.TypeClass.Island &&
				c.CardType != Cards.Seaside.TypeClass.Lookout &&
				c.CardType != Cards.Seaside.TypeClass.Outpost &&
				c.CardType != Cards.Seaside.TypeClass.Salvager &&
				c.CardType != Cards.Seaside.TypeClass.Tactician &&
				c.CardType != Cards.Seaside.TypeClass.TreasureMap &&
				c.CardType != Cards.Prosperity.TypeClass.CountingHouse &&
				c.CardType != Cards.Prosperity.TypeClass.Forge &&
				c.CardType != Cards.Prosperity.TypeClass.TradeRoute &&
				c.CardType != Cards.Prosperity.TypeClass.Watchtower &&
				c.CardType != Cards.Cornucopia.TypeClass.Remake &&
				c.CardType != Cards.Hinterlands.TypeClass.Develop &&
				c.CardType != Cards.DarkAges.TypeClass.JunkDealer &&
				c.CardType != Cards.DarkAges.TypeClass.Procession &&
				c.CardType != Cards.DarkAges.TypeClass.Rats &&
				c.CardType != Cards.DarkAges.TypeClass.Rebuild &&
				c.CardType != Cards.Guilds.TypeClass.MerchantGuild &&
				c.CardType != Cards.Guilds.TypeClass.Stonemason));
			// OK, nothing good found.  Now let's allow not-so-useful cards to be played
			if (bestCard == null)
				bestCard = this.FindBestCardToPlay(choice.Cards.Where(c =>
				c.CardType != Cards.Base.TypeClass.Remodel &&
				c.CardType != Cards.Intrigue.TypeClass.Upgrade &&
				c.CardType != Cards.Seaside.TypeClass.Island &&
				c.CardType != Cards.Seaside.TypeClass.Lookout &&
				c.CardType != Cards.Seaside.TypeClass.Salvager &&
				c.CardType != Cards.Seaside.TypeClass.TreasureMap &&
				c.CardType != Cards.Prosperity.TypeClass.TradeRoute &&
				c.CardType != Cards.Cornucopia.TypeClass.Remake &&
				c.CardType != Cards.Hinterlands.TypeClass.Develop &&
				c.CardType != Cards.DarkAges.TypeClass.Rats &&
				c.CardType != Cards.DarkAges.TypeClass.Rebuild &&
				c.CardType != Cards.Guilds.TypeClass.Stonemason));
			if (bestCard != null)
				return new ChoiceResult(new CardCollection() { bestCard });

			// Don't play anything
			return new ChoiceResult(new CardCollection());
		}
		protected override ChoiceResult Decide_Library(Choice choice)
		{
			/// TODO -- This should be updated to check to see how many "terminal" Action cards we have vs. how many Actions we have to play them all
			// If there are no Actions remaining, always set aside Action cards we *want to* play
			// Otherwise, always keep Action cards
			if (this.RealThis.Actions > 0 || !this.ShouldPlay(choice.CardTriggers[0]))
				return new ChoiceResult(new List<String>() { choice.Options[0].Text });
			else
				return new ChoiceResult(new List<String>() { choice.Options[1].Text });
		}
		protected override ChoiceResult Decide_Loan(Choice choice)
		{
			// Choose to trash Copper roughly 1/3 of the time (a little odd, but it should work decently)
			// Let's change this to be a bit more aggressive
			// If it's a Copper, or if it's a Silver/Talisman/Quarry/Masterpiece and we have at least 1 Platinum and at least 3 Ventures, or if it's a Loan and we have fewer than 3 Coppers
			if (choice.CardTriggers[0].CardType == Cards.Universal.TypeClass.Copper ||
				((choice.CardTriggers[0].CardType == Cards.Universal.TypeClass.Silver ||
					choice.CardTriggers[0].CardType == Cards.Prosperity.TypeClass.Talisman ||
					choice.CardTriggers[0].CardType == Cards.Prosperity.TypeClass.Quarry ||
					choice.CardTriggers[0].CardType == Cards.Guilds.TypeClass.Masterpiece) &&
					this.RealThis.CountAll(this.RealThis, c => c.CardType == Cards.Prosperity.TypeClass.Platinum, true, false) > 0 &&
					this.RealThis.CountAll(this.RealThis, c => c.CardType == Cards.Prosperity.TypeClass.Venture, true, false) > 3) ||
				(choice.CardTriggers[0].CardType == Cards.Prosperity.TypeClass.Loan &&
					this.RealThis.CountAll(this.RealThis, c => c.CardType == Cards.Universal.TypeClass.Copper, true, false) < 3))
				return new ChoiceResult(new List<String>() { choice.Options[1].Text }); // Trash
			else
				return new ChoiceResult(new List<String>() { choice.Options[0].Text }); // Discard
		}
		protected override ChoiceResult Decide_Lookout(Choice choice)
		{
			if (choice.Text == "Choose a card to trash")
				return new ChoiceResult(new CardCollection(this.FindBestCardsToTrash(choice.Cards, 1)));
			else
				return new ChoiceResult(new CardCollection(this.FindBestCardsToDiscard(choice.Cards, 1)));
		}
		protected override ChoiceResult Decide_Mandarin(Choice choice)
		{
			// Not always the best decision, but for now, it's the easiest
			if (choice.Text.StartsWith("Choose a card to put back on your deck"))
			{
				return new ChoiceResult(new CardCollection(this.FindBestCardsToDiscard(choice.Cards, 1)));
			}
			else
			{
				CardCollection cards = new CardCollection(choice.Cards);
				// Order them in roughly random order
				Utilities.Shuffler.Shuffle(cards);
				return new ChoiceResult(cards);
			}
		}
		protected override ChoiceResult Decide_Margrave(Choice choice)
		{
			return new ChoiceResult(new CardCollection(this.FindBestCardsToDiscard(choice.Cards, choice.Cards.Count() - 3)));
		}
		protected override ChoiceResult Decide_Masquerade(Choice choice)
		{
			if (choice.Text == "Choose a card to pass to the left")
			{
				Card masqBestCard = choice.Cards.FirstOrDefault(c => (c.Category & Category.Curse) == Category.Curse);
				if (masqBestCard == null)
					masqBestCard = choice.Cards.FirstOrDefault(c => (c.Category & Category.Ruins) == Category.Ruins);
				if (masqBestCard == null)
					masqBestCard = choice.Cards.FirstOrDefault(c => c.CardType == Cards.Universal.TypeClass.Copper);
				if (masqBestCard == null)
					masqBestCard = choice.Cards.FirstOrDefault(c => 
						c.CardType == Cards.Base.TypeClass.Chapel || c.CardType == Cards.Base.TypeClass.Moneylender ||
						c.CardType == Cards.Base.TypeClass.Remodel || c.CardType == Cards.Intrigue.TypeClass.Masquerade ||
						c.CardType == Cards.Intrigue.TypeClass.TradingPost || c.CardType == Cards.Intrigue.TypeClass.Upgrade ||
						c.CardType == Cards.Seaside.TypeClass.Lookout || c.CardType == Cards.Seaside.TypeClass.Salvager ||
						c.CardType == Cards.Alchemy.TypeClass.Transmute || c.CardType == Cards.Prosperity.TypeClass.Expand ||
						c.CardType == Cards.Prosperity.TypeClass.Forge || c.CardType == Cards.Prosperity.TypeClass.TradeRoute ||
						c.CardType == Cards.Cornucopia.TypeClass.Remake || c.CardType == Cards.Hinterlands.TypeClass.Develop ||
						c.CardType == Cards.DarkAges.TypeClass.Hovel || c.CardType == Cards.DarkAges.TypeClass.OvergrownEstate ||
						c.CardType == Cards.DarkAges.TypeClass.Rats || c.CardType == Cards.DarkAges.TypeClass.Rebuild ||
						c.CardType == Cards.Guilds.TypeClass.Stonemason
						);
				if (masqBestCard == null)
					masqBestCard = choice.Cards.FirstOrDefault(c => c.CardType == Cards.Guilds.TypeClass.Masterpiece);
				// Last chance -- just take the cheapest one
				if (masqBestCard == null)
					masqBestCard = choice.Cards.OrderBy(c => c.BaseCost.Coin.Value + 2.5 * c.BaseCost.Potion.Value).FirstOrDefault();

				if (masqBestCard != null)
					return new ChoiceResult(new CardCollection() { masqBestCard });

				return new ChoiceResult(new CardCollection(this.FindBestCardsToDiscard(choice.Cards, 1)));
			}
			else
			{
				Card masqCurse = choice.Cards.FirstOrDefault(c => (c.Category & Category.Curse) == Category.Curse);
				if (masqCurse != null)
					return new ChoiceResult(new CardCollection() { masqCurse });

				return new ChoiceResult(new CardCollection());
			}
		}
		protected override ChoiceResult Decide_Masterpiece(Choice choice)
		{
			// Always overpay by as much as we can
			return new ChoiceResult(new List<String>() { choice.Options[choice.Options.Count - 1].Text });
		}
		protected override ChoiceResult Decide_Mercenary(Choice choice)
		{
			switch (choice.ChoiceType)
			{
				case ChoiceType.Options:
					String choiceMercenary = choice.Options[1].Text;
					IEnumerable<Card> trashableCards = this.FindBestCardsToTrash(this.RealThis.Hand, 2, true);
					if (trashableCards.Count() >= 2)
						choiceMercenary = choice.Options[0].Text;
					return new ChoiceResult(new List<String>() { choiceMercenary });

				case ChoiceType.Cards:
					if (choice.Text == "Choose 2 cards to trash")
						return new ChoiceResult(new CardCollection(this.FindBestCardsToTrash(this.RealThis.Hand, 2, false)));
					else if (choice.Text.StartsWith("Choose cards to discard."))
						return new ChoiceResult(new CardCollection(this.FindBestCardsToDiscard(choice.Cards, choice.Cards.Count() - 3)));
					else
						return base.Decide_Mercenary(choice);

				default:
					return base.Decide_Mercenary(choice);
			}
		}
		protected override ChoiceResult Decide_Militia(Choice choice)
		{
			return new ChoiceResult(new CardCollection(this.FindBestCardsToDiscard(choice.Cards, choice.Cards.Count() - 3)));
		}
		protected override ChoiceResult Decide_Mine(Choice choice)
		{
			switch (choice.ChoiceType)
			{
				case ChoiceType.Cards:
					Card mineCard = null;
					if (mineCard == null && _Game.Table.Supplies.ContainsKey(Cards.Prosperity.TypeClass.Platinum) && _Game.Table.Supplies[Cards.Prosperity.TypeClass.Platinum].CanGain())
						mineCard = choice.Cards.FirstOrDefault(c => c.CardType == Cards.Universal.TypeClass.Gold);
					if (mineCard == null)
						mineCard = choice.Cards.FirstOrDefault(c => c.CardType == Cards.Guilds.TypeClass.Masterpiece);
					if (mineCard == null)
						mineCard = choice.Cards.FirstOrDefault(c => c.CardType == Cards.Universal.TypeClass.Silver);
					if (mineCard == null)
						mineCard = choice.Cards.FirstOrDefault(c => c.CardType == Cards.Universal.TypeClass.Copper);
					if (mineCard == null) // Pick a random Treasure at this point
						mineCard = choice.Cards.ElementAt(this._Game.RNG.Next(choice.Cards.Count()));
					return new ChoiceResult(new CardCollection() { mineCard });

				case ChoiceType.Supplies:
					return new ChoiceResult(FindBestCardForCost(choice.Supplies.Values, null, false));

				default:
					return base.Decide_Mine(choice);
			}
		}
		protected override ChoiceResult Decide_MiningVillage(Choice choice)
		{
			// Trash if between 4 & 7 Coins available (?? Odd choice)
			if (this.RealThis.Currency.Coin > 3 && this.RealThis.Currency.Coin < 8)
				return new ChoiceResult(new List<String>() { choice.Options[0].Text }); // Yes
			else
				return new ChoiceResult(new List<String>() { choice.Options[1].Text }); // No
		}
		protected override ChoiceResult Decide_Minion(Choice choice)
		{
			// Gain coins if we have another Minion in hand
			// Gain coins if between 4 & 7 Coins available (?? Odd choice)
			if (this.RealThis.Hand[Cards.Intrigue.TypeClass.Minion].Count > 0 || (this.RealThis.Currency.Coin > 3 && this.RealThis.Currency.Coin < 8))
				return new ChoiceResult(new List<String>() { choice.Options[0].Text }); // +2 Coins
			else
				return new ChoiceResult(new List<String>() { choice.Options[1].Text }); // Discard Hand
		}
		protected override ChoiceResult Decide_Mint(Choice choice)
		{
			// Always choose the Treasure card that costs the most to duplicate
			Card bestCard = null;
			foreach (Card card in choice.Cards)
			{
				if (this.RealThis._Game.Table.Supplies.ContainsKey(card) && this.RealThis._Game.Table.Supplies[card].CanGain() &&
					(bestCard == null || bestCard.Benefit.Currency.Coin < card.Benefit.Currency.Coin || bestCard.Benefit.Currency.Coin == 0))
					bestCard = card;
			}
			if (bestCard != null)
				return new ChoiceResult(new CardCollection() { bestCard });

			return new ChoiceResult(new CardCollection());
		}
		protected override ChoiceResult Decide_Mountebank(Choice choice)
		{
			// Discard curse if I don't have a Trader in my hand -- 2 Silvers are better than no Curse card in hand
			if (this.RealThis.Hand[Cards.Hinterlands.TypeClass.Trader].Count > 0)
				return new ChoiceResult(new List<String>() { choice.Options[1].Text });
			// Otherwise, just discard the Curse
			return new ChoiceResult(new List<String>() { choice.Options[0].Text });
		}
		protected override ChoiceResult Decide_Mystic(Choice choice)
		{
			Dictionary<Type, int> _CardsCount = new Dictionary<Type, int>();
			foreach (Type cardType in _CardsGained)
				_CardsCount[cardType] = (int)Math.Pow(this.RealThis.CountAll(this.RealThis, c => c.CardType == cardType, false, true), 2);

			// Choose one at random, with a probability based on the cards left to be able to draw
			int indexChosen = this._Game.RNG.Next(_CardsCount.Sum(kvp => kvp.Value));

			Card mysticCard = null;
			foreach (Type cardType in _CardsCount.Keys)
			{
				if (_CardsCount[cardType] == 0)
					continue;
				if (indexChosen < _CardsCount[cardType])
				{
					Supply mysticSupply = choice.Supplies.FirstOrDefault(kvp => kvp.Value.CardType == cardType).Value;
					if (mysticSupply != null)
						return new ChoiceResult(mysticSupply);
					mysticCard = choice.Cards.FirstOrDefault(c => c.CardType == cardType);
					break;
				}
				indexChosen -= _CardsCount[cardType];
			}

			if (mysticCard != null)
				return new ChoiceResult(new CardCollection() { mysticCard });

			return new ChoiceResult(choice.Supplies.ElementAt(this._Game.RNG.Next(choice.Supplies.Count)).Value);
		}
		protected override ChoiceResult Decide_NativeVillage(Choice choice)
		{
			// Retrieve cards from the Native Village mat if there are at least 2 cards there (odd, again...)
			// Let's change that to if there are more than a uniformly-random number between 2 and 4
			// Unless, of course, there are multiple Native Village cards in hand -- then ALWAYS put cards on the Mat
			if (this.RealThis.PlayerMats[Cards.Seaside.TypeClass.NativeVillageMat].Count > this._Game.RNG.Next(2, 5) &&
				this.RealThis.Hand[Cards.Seaside.TypeClass.NativeVillage].Count == 0)
				return new ChoiceResult(new List<String>() { choice.Options[1].Text });
			else
				return new ChoiceResult(new List<String>() { choice.Options[0].Text });
		}
		protected override ChoiceResult Decide_Navigator(Choice choice)
		{
			// Untested Navigator logic
			// Basically, all this is doing is seeing if the average "in-deck value" of the revealed cards is more than the average 
			// "in-deck value" of the entire deck, with Victory cards having a value of 0.
			double totalDeckValue = this.RealThis.SumAll(this.RealThis, c => true, c => c.Category == Category.Victory ? 0 : this.ComputeValueInDeck(c), true, false);
			double averageDeckValue = totalDeckValue / this.RealThis.CountAll();
			double averageRevealedValue = this.Revealed.Sum(c => c.Category == Category.Victory ? 0 : this.ComputeValueInDeck(c)) / this.Revealed.Count;

			// A modest fudge factor may be needed here.  If the revealed cards are borderline, then we should maybe just keep them
			if (averageRevealedValue + 0.5 > averageDeckValue)
				return new ChoiceResult(new List<String>() { choice.Options[1].Text });

			return new ChoiceResult(new List<String>() { choice.Options[0].Text });
		}
		protected override ChoiceResult Decide_NobleBrigand(Choice choice)
		{
			return new ChoiceResult(new CardCollection(this.FindBestCards(choice.Cards, 1)));
		}
		protected override ChoiceResult Decide_Nobles(Choice choice)
		{
			// Choose +2 Actions only if there are fewer Actions than Action cards we want to play
			if (this.RealThis.Hand.Count(c => (c.Category & Category.Action) == Category.Action && this.ShouldPlay(c)) > this.RealThis.Actions)
				return new ChoiceResult(new List<String>() { choice.Options[1].Text }); // +2 Actions
			else
				return new ChoiceResult(new List<String>() { choice.Options[0].Text }); // +3 Cards
		}
		protected override ChoiceResult Decide_Oasis(Choice choice)
		{
			return new ChoiceResult(new CardCollection(this.FindBestCardsToDiscard(choice.Cards, 1)));
		}
		protected override ChoiceResult Decide_Oracle(Choice choice)
		{
			switch (choice.ChoiceType)
			{
				case ChoiceType.Options:
					if (choice.PlayerSource == this.RealThis)
					{
						double totalDeckDiscardability = this.RealThis.SumAll(this.RealThis, c => true, c => this.ComputeDiscardValue(c), true, true);
						int totalCards = this.RealThis.CountAll(this.RealThis, c => true, true, true);
						double cardsDiscardability = choice.CardTriggers.Sum(c => this.ComputeDiscardValue(c));

						// If it's better to keep these cards than discard them
						if (cardsDiscardability / choice.CardTriggers.Count >= totalDeckDiscardability / totalCards)
							return new ChoiceResult(new List<String>() { choice.Options[0].Text });
						else
							return new ChoiceResult(new List<String>() { choice.Options[1].Text });
					}
					else
					{
						double totalDeckDiscardability = choice.PlayerSource.SumAll(this.RealThis, c => true, c => this.ComputeDiscardValue(c), true, true);
						int totalCards = choice.PlayerSource.CountAll(this.RealThis, c => true, true, true);
						double cardsDiscardability = choice.CardTriggers.Sum(c => this.ComputeDiscardValue(c));

						// If it's better to discard these cards than keep them
						if (cardsDiscardability / choice.CardTriggers.Count >= totalDeckDiscardability / totalCards)
							return new ChoiceResult(new List<String>() { choice.Options[1].Text });
						else
							return new ChoiceResult(new List<String>() { choice.Options[0].Text });
					}

				case ChoiceType.Cards:
					CardCollection oracleCards = new CardCollection(choice.Cards);
					// Order them in roughly random order
					Utilities.Shuffler.Shuffle(oracleCards);
					return new ChoiceResult(oracleCards);

				default:
					return base.Decide_Oracle(choice);
			}
		}
		protected override ChoiceResult Decide_Pawn(Choice choice)
		{
			// Always choose +1 Coin.  Only choose +1 Action if there's at least 1 Action card in hand that we want to play and can play and need the extra Action for
			List<String> pawnChoices = new List<string>() { choice.Options[3].Text }; // +1 Coin
			if (this.RealThis.Hand.Count(c => (c.Category & Category.Action) == Category.Action && this.ShouldPlay(c)) > this.RealThis.Actions)
				pawnChoices.Add(choice.Options[1].Text); // +1 Action
			else
				pawnChoices.Add(choice.Options[0].Text); // +1 Card
			return new ChoiceResult(pawnChoices);
		}
		protected override ChoiceResult Decide_PearlDiver(Choice choice)
		{
			// only put on top if the card has no victory points associated with it (??? What about Harem, Island, etc.?)
			//if (choice.CardTrigger.VictoryPoints == 0)
			// Only put on top if the card is not a Victory (only Victory -- not dual-purpose) or Curse or Copper card
			if (choice.CardTriggers[0].Category == Category.Victory ||
				choice.CardTriggers[0].Category == Category.Curse ||
				(choice.CardTriggers[0].Category & Category.Ruins) == Category.Ruins ||
				choice.CardTriggers[0].CardType == Cards.Universal.TypeClass.Copper ||
				choice.CardTriggers[0].CardType == Cards.Hinterlands.TypeClass.Tunnel ||
				choice.CardTriggers[0].CardType == Cards.DarkAges.TypeClass.OvergrownEstate ||
				choice.CardTriggers[0].CardType == Cards.DarkAges.TypeClass.Hovel)
				return new ChoiceResult(new List<String>() { choice.Options[1].Text });
			else
				return new ChoiceResult(new List<String>() { choice.Options[0].Text });
		}
		protected override ChoiceResult Decide_Pillage(Choice choice)
		{
			// First priority is Platinum
			if (choice.Cards.Count(c => c.CardType == Cards.Prosperity.TypeClass.Platinum) > 0)
				return new ChoiceResult(new CardCollection() { choice.Cards.First(c => c.CardType == Cards.Prosperity.TypeClass.Platinum) });
			// Next priority is King's Court if the player has Action cards other than KC/TR
			else if (choice.Cards.Count(c => c.CardType == Cards.Prosperity.TypeClass.KingsCourt) > 0 && 
				choice.Cards.Count(c => (c.Category & Category.Action) == Category.Action && c.CardType != Cards.Prosperity.TypeClass.KingsCourt && c.CardType != Cards.Base.TypeClass.ThroneRoom) > 0)
				return new ChoiceResult(new CardCollection() { choice.Cards.First(c => c.CardType == Cards.Prosperity.TypeClass.KingsCourt) });
			// Next priority is 5-cost+ Attack cards
			else if (choice.Cards.Count(c => (c.Category & Category.Attack) == Category.Attack && c.BaseCost.Coin >= 5) > 0)
				return new ChoiceResult(new CardCollection(this.FindBestCards(choice.Cards.Where(c => (c.Category & Category.Attack) == Category.Attack && c.BaseCost.Coin >= 5), 1)));
			// Next priority is Gold
			else if (choice.Cards.Count(c => c.CardType == Cards.Universal.TypeClass.Gold) > 0)
				return new ChoiceResult(new CardCollection() { choice.Cards.First(c => c.CardType == Cards.Universal.TypeClass.Gold) });
			// Next priority is 5-cost+ Action/Treasure cards (other than Ill-Gotten Gains)
			else if (choice.Cards.Count(c => ((c.Category & Category.Action) == Category.Action || (c.Category & Category.Treasure) == Category.Treasure) && c.BaseCost.Coin >= 5 && c.CardType != Cards.Hinterlands.TypeClass.IllGottenGains) > 0)
				return new ChoiceResult(new CardCollection(this.FindBestCards(choice.Cards.Where(c => ((c.Category & Category.Action) == Category.Action || (c.Category & Category.Treasure) == Category.Treasure) && c.BaseCost.Coin >= 5 && c.CardType != Cards.Hinterlands.TypeClass.IllGottenGains), 1)));
			// Next priority is any remaining Attack cards
			else if (choice.Cards.Count(c => (c.Category & Category.Attack) == Category.Attack) > 0)
				return new ChoiceResult(new CardCollection(this.FindBestCards(choice.Cards.Where(c => (c.Category & Category.Attack) == Category.Attack), 1)));
			// Next priority is any remaining Action/Treasure cards
			else if (choice.Cards.Count(c => (c.Category & Category.Action) == Category.Action || (c.Category & Category.Treasure) == Category.Treasure) > 0)
				return new ChoiceResult(new CardCollection(this.FindBestCards(choice.Cards.Where(c => (c.Category & Category.Action) == Category.Action || (c.Category & Category.Treasure) == Category.Treasure), 1)));
			// Final fall-through
			return new ChoiceResult(new CardCollection(this.FindBestCards(choice.Cards, 1)));
		}
		protected override ChoiceResult Decide_PirateShip(Choice choice)
		{
			switch (choice.ChoiceType)
			{
				case ChoiceType.Options:
					IEnumerator<Player> ePlayers = this.RealThis._Game.GetPlayersStartingWithActiveEnumerator();
					ePlayers.MoveNext();
					int blockedCount = 0;
					while (ePlayers.MoveNext())
					{
						Player attackee = ePlayers.Current;
						if (attackee.SetAside[Cards.Seaside.TypeClass.Lighthouse].Count > 0 ||
							(this.KnownPlayerHands.ContainsKey(attackee.UniqueId) && this.KnownPlayerHands[attackee.UniqueId].Any(c => c.CardType == Cards.Base.TypeClass.Moat)))
							blockedCount++;
					}

					// Take the Pirate Ship tokens if all attacks have been blocked -- no point in attacking
					if (blockedCount == this.RealThis._Game.Players.Count - 1)
						return new ChoiceResult(new List<String>() { choice.Options[1].Text });
					// Steal coins until I have at least 3 Pirate Ship tokens on my mat
					else if (this.RealThis.TokenPiles[Cards.Seaside.TypeClass.PirateShipToken].Count > 3)
						return new ChoiceResult(new List<String>() { choice.Options[1].Text });
					else
						return new ChoiceResult(new List<String>() { choice.Options[0].Text });

				case ChoiceType.Cards:
					return new ChoiceResult(new CardCollection(this.FindBestCards(choice.Cards, 1)));

				default:
					return base.Decide_Remodel(choice);
			}
		}
		protected override ChoiceResult Decide_Plaza(Choice choice)
		{
			// Always discard Copper
			Card plazaBestCard = choice.Cards.FirstOrDefault(c => c.CardType == Cards.Universal.TypeClass.Copper);
			if (plazaBestCard == null && (this.RealThis.Hand[Cards.Alchemy.TypeClass.Potion].Count > 1 || _Game.Table.Supplies.Count(kvp => kvp.Value.BaseCost.Potion > 0 && kvp.Value.CanGain()) < 1))
				plazaBestCard = choice.Cards.FirstOrDefault(c => c.CardType == Cards.Alchemy.TypeClass.Potion);
			if (plazaBestCard == null && this.RealThis.Hand[Cards.Hinterlands.TypeClass.FoolsGold].Count == 1)
				plazaBestCard = choice.Cards.FirstOrDefault(c => c.CardType == Cards.Hinterlands.TypeClass.FoolsGold);
			if (plazaBestCard == null && this.RealThis.CountAll(this.RealThis, c => c.CardType == Cards.Universal.TypeClass.Copper, true, false) < 4)
				plazaBestCard = choice.Cards.FirstOrDefault(c => c.CardType == Cards.Prosperity.TypeClass.Loan);
			if (plazaBestCard == null)
				plazaBestCard = choice.Cards.FirstOrDefault(c => c.CardType == Cards.Guilds.TypeClass.Masterpiece);
			if (plazaBestCard != null)
				return new ChoiceResult(new CardCollection() { plazaBestCard });

			return new ChoiceResult(new CardCollection());
		}
		protected override ChoiceResult Decide_Prince(Choice choice)
		{
			Card cardToSetAside = null;
			double bestScore = 0f;
			Currency cardCap = new Currency(4);
			foreach (Card cardTest in this.RealThis.Hand[c => (c.Category & Category.Action) == Category.Action && this.RealThis._Game.ComputeCost(c) <= cardCap])
			{
				// Skip all Duration cards
				if ((cardTest.Category & Category.Duration) == Category.Duration)
					continue;

				// Skip the following cards
				if (cardTest.CardType == Cards.Base.TypeClass.Feast || cardTest.CardType == Cards.Base.TypeClass.Remodel ||
					cardTest.CardType == Cards.Intrigue.TypeClass.TradingPost || cardTest.CardType == Cards.Intrigue.TypeClass.Upgrade ||
					cardTest.CardType == Cards.Seaside.TypeClass.Embargo || cardTest.CardType == Cards.Seaside.TypeClass.Island || 
					cardTest.CardType == Cards.Seaside.TypeClass.Lookout || cardTest.CardType == Cards.Seaside.TypeClass.Salvager || 
					cardTest.CardType == Cards.Seaside.TypeClass.TreasureMap || cardTest.CardType == Cards.Alchemy.TypeClass.Apprentice ||
					cardTest.CardType == Cards.Prosperity.TypeClass.Expand || cardTest.CardType == Cards.Prosperity.TypeClass.Forge || 
					cardTest.CardType == Cards.Prosperity.TypeClass.TradeRoute || cardTest.CardType == Cards.Cornucopia.TypeClass.Remake || 
					cardTest.CardType == Cards.Hinterlands.TypeClass.Develop || cardTest.CardType == Cards.Hinterlands.TypeClass.Trader || 
					cardTest.CardType == Cards.DarkAges.TypeClass.DeathCart || cardTest.CardType == Cards.DarkAges.TypeClass.Forager || 
					cardTest.CardType == Cards.DarkAges.TypeClass.JunkDealer || cardTest.CardType == Cards.DarkAges.TypeClass.Madman || 
					cardTest.CardType == Cards.DarkAges.TypeClass.Pillage || cardTest.CardType == Cards.DarkAges.TypeClass.Procession || 
					cardTest.CardType == Cards.DarkAges.TypeClass.Rats || cardTest.CardType == Cards.DarkAges.TypeClass.Rebuild || 
					cardTest.CardType == Cards.Guilds.TypeClass.Butcher || cardTest.CardType == Cards.Guilds.TypeClass.Stonemason || 
					cardTest.CardType == Cards.Promotional.TypeClass.Prince)
					continue;

				// Score the card and then pick it if it's higher than the previous best card
				double baseScore = this.RealThis._Game.ComputeCost(cardTest).Coin.Value;
				if (cardTest.CardType == Cards.Base.TypeClass.Chapel)
					baseScore = 1;
				else if (cardTest.CardType == Cards.Prosperity.TypeClass.Bishop)
					baseScore = 3.5;
				else if (cardTest.CardType == Cards.Prosperity.TypeClass.Peddler)
					baseScore = 4.5;
				else if (cardTest.CardType == Cards.Cornucopia.TypeClass.BagOfGold)
					baseScore = 6;
				else if (cardTest.CardType == Cards.Cornucopia.TypeClass.Followers)
					baseScore = 6;
				else if (cardTest.CardType == Cards.Cornucopia.TypeClass.Princess)
					baseScore = 6;
				else if (cardTest.CardType == Cards.Cornucopia.TypeClass.TrustySteed)
					baseScore = 6;
				else if (cardTest.CardType == Cards.Hinterlands.TypeClass.SpiceMerchant)
					baseScore = 2;
				else if (cardTest.CardType == Cards.DarkAges.TypeClass.Beggar)
					baseScore = 1;
				else if (cardTest.CardType == Cards.Guilds.TypeClass.Doctor)
					baseScore = 1.2;

				if (baseScore > bestScore)
				{
					cardToSetAside = cardTest;
					bestScore = baseScore;
				}
			}

			switch (choice.ChoiceType)
			{
				case ChoiceType.Options:
					// "Do you want to set this card aside?"
					// Only do this if there's a useful card to set aside
					if (cardToSetAside != null)
						// Yes, set Prince aside
						return new ChoiceResult(new List<String>() { choice.Options[0].Text });
					else
						// No, don't set Prince aside
						return new ChoiceResult(new List<String>() { choice.Options[1].Text });

				case ChoiceType.Cards:
					if (cardToSetAside != null && choice.Cards.Contains(cardToSetAside))
						return new ChoiceResult(new CardCollection() { cardToSetAside });
					return new ChoiceResult(new CardCollection(this.FindBestCards(choice.Cards, 1)));

				default:
					return base.Decide_Prince(choice);
			}
		}
		protected override ChoiceResult Decide_Procession(Choice choice)
		{
			// Priority first is for Fortress, since it's soooo awesome to trash Fortress
			Card bestCard = choice.Cards.FirstOrDefault(c => c is Cards.DarkAges.Fortress);

			// Second priority is for Ruins, since they pretty much suck
			if (bestCard == null)
				bestCard = choice.Cards.FirstOrDefault(c => c is Cards.DarkAges.RuinedVillage);
			if (bestCard == null)
				bestCard = choice.Cards.FirstOrDefault(c => c is Cards.DarkAges.RuinedMarket);
			if (bestCard == null)
				bestCard = choice.Cards.FirstOrDefault(c => c is Cards.DarkAges.Survivors);
			if (bestCard == null)
				bestCard = choice.Cards.FirstOrDefault(c => c is Cards.DarkAges.RuinedLibrary);
			if (bestCard == null)
				bestCard = choice.Cards.FirstOrDefault(c => c is Cards.DarkAges.AbandonedMine);

			// We need to be a little careful with Procession
			if (bestCard == null)
				bestCard = this.FindBestCardToPlay(choice.Cards.Where(c =>
					c.CardType != Cards.Base.TypeClass.Chapel &&
					c.CardType != Cards.Base.TypeClass.Library &&
					c.CardType != Cards.Base.TypeClass.Remodel &&
					c.CardType != Cards.Intrigue.TypeClass.SecretChamber &&
					c.CardType != Cards.Intrigue.TypeClass.Upgrade &&
					c.CardType != Cards.Seaside.TypeClass.Island &&
					c.CardType != Cards.Seaside.TypeClass.Lookout &&
					c.CardType != Cards.Seaside.TypeClass.Outpost &&
					c.CardType != Cards.Seaside.TypeClass.Salvager &&
					c.CardType != Cards.Seaside.TypeClass.Tactician &&
					c.CardType != Cards.Seaside.TypeClass.TreasureMap &&
					c.CardType != Cards.Prosperity.TypeClass.CountingHouse &&
					c.CardType != Cards.Prosperity.TypeClass.Forge &&
					c.CardType != Cards.Prosperity.TypeClass.TradeRoute &&
					c.CardType != Cards.Prosperity.TypeClass.Watchtower &&
					c.CardType != Cards.Cornucopia.TypeClass.Remake &&
					c.CardType != Cards.Hinterlands.TypeClass.Develop &&
					c.CardType != Cards.DarkAges.TypeClass.JunkDealer &&
					c.CardType != Cards.DarkAges.TypeClass.Procession &&
					c.CardType != Cards.DarkAges.TypeClass.Rats &&
					c.CardType != Cards.DarkAges.TypeClass.Rebuild &&
					c.CardType != Cards.Guilds.TypeClass.MerchantGuild &&
					c.CardType != Cards.Guilds.TypeClass.Stonemason &&
					this.RealThis._Game.Table.Supplies.Select(kvp => kvp.Value).Any(s => 
						(s.Category & Category.Action) == Category.Action && s.CurrentCost == this.RealThis._Game.ComputeCost(c) + new Cards.Cost(1))));
					// ^^^ --- this is to make sure that we can actually gain a card from the card we're trashing

			// Let's trash some Ruins if we can't find anything fun
			if (bestCard == null)
				bestCard = this.FindBestCardToPlay(choice.Cards.Where(c => (c.Category & Category.Ruins) == Category.Ruins));

			// OK, nothing good found.  Now let's allow not-so-useful cards to be played
			if (bestCard == null)
				bestCard = this.FindBestCardToPlay(choice.Cards.Where(c =>
					c.CardType != Cards.Base.TypeClass.Remodel &&
					c.CardType != Cards.Intrigue.TypeClass.Upgrade &&
					c.CardType != Cards.Seaside.TypeClass.Island &&
					c.CardType != Cards.Seaside.TypeClass.Lookout &&
					c.CardType != Cards.Seaside.TypeClass.Salvager &&
					c.CardType != Cards.Seaside.TypeClass.TreasureMap &&
					c.CardType != Cards.Prosperity.TypeClass.TradeRoute &&
					c.CardType != Cards.Cornucopia.TypeClass.Remake &&
					c.CardType != Cards.Hinterlands.TypeClass.Develop &&
					c.CardType != Cards.DarkAges.TypeClass.Rats &&
					c.CardType != Cards.DarkAges.TypeClass.Rebuild &&
					c.CardType != Cards.Guilds.TypeClass.Stonemason &&
					this.RealThis._Game.Table.Supplies.Select(kvp => kvp.Value).Any(s => 
						(s.Category & Category.Action) == Category.Action && s.CurrentCost == this.RealThis._Game.ComputeCost(c) + new Cards.Cost(1))));
					// ^^^ --- this is to make sure that we can actually gain a card from the card we're trashing

			if (bestCard != null)
				return new ChoiceResult(new CardCollection() { bestCard });

			// Don't play anything
			return new ChoiceResult(new CardCollection());
		}
		protected override ChoiceResult Decide_Rabble(Choice choice)
		{
			CardCollection cards = new CardCollection(choice.Cards);
			// Order them in roughly random order
			Utilities.Shuffler.Shuffle(cards);
			return new ChoiceResult(cards);
		}
		protected override ChoiceResult Decide_Rats(Choice choice)
		{
			return new ChoiceResult(new CardCollection(FindBestCardsToTrash(choice.Cards, 1)));
		}
		protected override ChoiceResult Decide_Rebuild(Choice choice)
		{
			switch (choice.ChoiceType)
			{
				case ChoiceType.SuppliesAndCards:
					Boolean colonyExists = false;
					Boolean colonyAvailable = false;
					int colonyCount = 0;
					if (this.RealThis._Game.Table.Supplies.ContainsKey(Cards.Prosperity.TypeClass.Colony))
					{
						colonyExists = true;
						colonyAvailable = this.RealThis._Game.Table.Supplies[Cards.Prosperity.TypeClass.Colony].CanGain();
						colonyCount = this.RealThis.CountAll(this.RealThis, c => c.CardType == Cards.Prosperity.TypeClass.Colony, true, true);
					}
					Boolean provinceAvailable = this.RealThis._Game.Table.Province.CanGain();
					int provinceCount = this.RealThis.CountAll(this.RealThis, c => c.CardType == Cards.Universal.TypeClass.Province, true, true);
					Boolean duchyAvailable = this.RealThis._Game.Table.Province.CanGain();
					int duchyCount = this.RealThis.CountAll(this.RealThis, c => c.CardType == Cards.Universal.TypeClass.Duchy, true, true);
					Boolean estateAvailable = this.RealThis._Game.Table.Province.CanGain();
					int estateCount = this.RealThis.CountAll(this.RealThis, c => c.CardType == Cards.Universal.TypeClass.Estate, true, true);

					Supply victorySupply = null;
					if (colonyExists && (colonyCount > 0 || colonyAvailable))
						victorySupply = choice.Supplies.First(kvp => kvp.Value.CardType == Cards.Prosperity.TypeClass.Colony).Value;
					if (victorySupply == null && provinceCount > 0)
						victorySupply = choice.Supplies.First(kvp => kvp.Value.CardType == Cards.Universal.TypeClass.Province).Value;
					if (victorySupply == null)
					{
						victorySupply = choice.Supplies.Select(kvp => kvp.Value).Where(s => 
							(s.Category & Category.Victory) == Category.Victory && s.CardType != Cards.Hinterlands.TypeClass.Farmland)
							.OrderByDescending(s => s.BaseCost).First();
					}

					return new ChoiceResult(victorySupply);

				case ChoiceType.Supplies:
					return new ChoiceResult(FindBestCardForCost(choice.Supplies.Values, null, false));

				default:
					return base.Decide_Rebuild(choice);
			}
		}
		protected override ChoiceResult Decide_Remake(Choice choice)
		{
			switch (choice.ChoiceType)
			{
				case ChoiceType.Cards:
					return new ChoiceResult(new CardCollection(FindBestCardsToTrash(choice.Cards, 1)));

				case ChoiceType.Supplies:
					return new ChoiceResult(FindBestCardForCost(choice.Supplies.Values, null, false));

				default:
					return base.Decide_Remake(choice);
			}
		}
		protected override ChoiceResult Decide_Remodel(Choice choice)
		{
			switch (choice.ChoiceType)
			{
				case ChoiceType.Cards:
					return new ChoiceResult(new CardCollection(FindBestCardsToTrash(choice.Cards, 1)));

				case ChoiceType.Supplies:
					return new ChoiceResult(FindBestCardForCost(choice.Supplies.Values, null, false));

				default:
					return base.Decide_Remodel(choice);
			}
		}
		protected override ChoiceResult Decide_Rogue(Choice choice)
		{
			if (choice.Text == "Choose a card to gain from the trash")
				return new ChoiceResult(new CardCollection(FindBestCards(choice.Cards, 1)));
			else if (choice.Text == "Choose a card to trash")
				return new ChoiceResult(new CardCollection(FindBestCardsToTrash(choice.Cards, 1)));
			else
				return base.Decide_Rogue(choice);
		}
		protected override ChoiceResult Decide_Saboteur(Choice choice)
		{
			Supply bestSupply = FindBestCardForCost(choice.Supplies.Values, null, false);

			if (this.RealThis.Hand[Cards.Hinterlands.TypeClass.Trader].Count > 0 && this.RealThis._Game.Table.Silver.CanGain())
			{
				// This is the only instance where we'll "gain" whatever we can (even a Curse)
			}
			else
			{
				// Never, ever gain a Curse
				if (bestSupply.CardType == Cards.Universal.TypeClass.Curse)
					bestSupply = null;
				// Never, ever gain a Ruins
				else if (bestSupply.CardType == Cards.DarkAges.TypeClass.RuinsSupply)
					bestSupply = null;
				else if (bestSupply.CardType == Cards.Universal.TypeClass.Copper)
				{
					// Only ever gain a Copper in specific situations (Counting House, Coppersmith, Gardens, etc.)
					int copperUsingCards = this.RealThis.CountAll(this.RealThis, c => c.CardType == Cards.Base.TypeClass.Gardens ||
						c.CardType == Cards.Base.TypeClass.Moneylender || c.CardType == Cards.Intrigue.TypeClass.Coppersmith ||
						c.CardType == Cards.Alchemy.TypeClass.Apothecary || c.CardType == Cards.Prosperity.TypeClass.CountingHouse, true, false);

					int copperCards = this.RealThis.CountAll(this.RealThis, c => c.CardType == Cards.Universal.TypeClass.Copper, true, false);

					int treasureCards = this.RealThis.CountAll(this.RealThis, c => (c.Category & Category.Treasure) == Category.Treasure, true, false);

					int totalCards = this.RealThis.CountAll();

					if (((float)copperUsingCards / totalCards < 0.20 && (float)copperCards / totalCards > 0.40) ||
						(float)copperCards / treasureCards < 0.30)
						bestSupply = null;
				}
			}

			return new ChoiceResult(bestSupply);
		}
		protected override ChoiceResult Decide_Salvager(Choice choice)
		{
			return new ChoiceResult(new CardCollection(this.FindBestCardsToTrash(choice.Cards, 1)));
		}
		protected override ChoiceResult Decide_Scavenger(Choice choice)
		{
			switch (choice.ChoiceType)
			{
				case ChoiceType.Options:
					// I have no freaking clue... -- just choose at random
					return new ChoiceResult(new List<String>() { choice.Options[this._Game.RNG.Next(0, 1)].Text });

				case ChoiceType.Cards:
					// Take the best card (? I'unno... seems OK-ish)
					return new ChoiceResult(new CardCollection(this.FindBestCards(choice.Cards, 1)));

				default:
					return base.Decide_Scavenger(choice);
			}
		}
		protected override ChoiceResult Decide_Scheme(Choice choice)
		{
			// Always take the most expensive card
			return new ChoiceResult(new CardCollection(this.FindBestCards(choice.Cards, choice.Maximum)));
		}
		protected override ChoiceResult Decide_Scout(Choice choice)
		{
			CardCollection scoutCards = new CardCollection(choice.Cards);
			// Order them in roughly random order
			Utilities.Shuffler.Shuffle(scoutCards);
			return new ChoiceResult(scoutCards);
		}
		protected override ChoiceResult Decide_ScryingPool(Choice choice)
		{
			if (choice.PlayerSource == this.RealThis)
			{
				if (IsCardOKForMeToDiscard(choice.CardTriggers[0]) && 
					(choice.CardTriggers[0].Category & Category.Ruins) != Category.Ruins)
					return new ChoiceResult(new List<String>() { choice.Options[0].Text });
				else
					return new ChoiceResult(new List<String>() { choice.Options[1].Text });
			}
			else
			{
				if (!IsCardOKForMeToDiscard(choice.CardTriggers[0]))
					return new ChoiceResult(new List<String>() { choice.Options[0].Text });
				else
					return new ChoiceResult(new List<String>() { choice.Options[1].Text });
			}
		}
		protected override ChoiceResult Decide_SecretChamber(Choice choice)
		{
			if (choice.Text == "Choose order of cards to put back on your deck")
			{
				// Order all the cards
				IEnumerable<Card> cardsToReturn = this.FindBestCardsToDiscard(choice.Cards, choice.Cards.Count());

				// Try to save 1 Curse if we can
				if (choice.CardTriggers[0].CardType == Cards.Prosperity.TypeClass.Mountebank)
				{
					cardsToReturn = cardsToReturn.Take(3);
					if (cardsToReturn.ElementAt(0).CardType == Cards.Universal.TypeClass.Curse)
						return new ChoiceResult(new CardCollection(cardsToReturn.Skip(1)));
					if (cardsToReturn.ElementAt(1).CardType == Cards.Universal.TypeClass.Curse)
						return new ChoiceResult(new CardCollection() { cardsToReturn.ElementAt(0), cardsToReturn.ElementAt(2)});
				}
				
				// Try to not put Treasure cards onto our deck, even if that means putting Action cards there
				else if (choice.CardTriggers[0].CardType == Cards.Seaside.TypeClass.PirateShip)
				{
					CardCollection pirateShipCards = new CardCollection(cardsToReturn.Where(c => (c.Category & Category.Treasure) != Category.Treasure));
					if (pirateShipCards.Count < 2)
						pirateShipCards.AddRange(cardsToReturn.Where(c => (c.Category & Category.Treasure) != Category.Treasure).Take(2 - pirateShipCards.Count));
					return new ChoiceResult(pirateShipCards);
				}

				return new ChoiceResult(new CardCollection(cardsToReturn.Take(2)));
			}
			else
			{
				CardCollection scCards = new CardCollection();
				foreach (Card card in choice.Cards)
				{
					if (card.Category == Category.Curse ||
						((card.Category & Category.Victory) == Category.Victory && (card.Category & Category.Treasure) != Category.Treasure) ||
						(card.CardType == Cards.Universal.TypeClass.Copper && this.RealThis.InPlay[Cards.Intrigue.TypeClass.Coppersmith].Count == 0) ||
						(this.RealThis.Actions == 0 && (card.Category & Category.Treasure) != Category.Treasure))
						scCards.Add(card);
				}
				return new ChoiceResult(scCards);
			}
		}
		protected override ChoiceResult Decide_SirBailey(Choice choice)
		{
			return new ChoiceResult(new CardCollection(FindBestCardsToTrash(choice.Cards, 1)));
		}
		protected override ChoiceResult Decide_SirDestry(Choice choice)
		{
			return new ChoiceResult(new CardCollection(FindBestCardsToTrash(choice.Cards, 1)));
		}
		protected override ChoiceResult Decide_SirMartin(Choice choice)
		{
			return new ChoiceResult(new CardCollection(FindBestCardsToTrash(choice.Cards, 1)));
		}
		protected override ChoiceResult Decide_SirMichael(Choice choice)
		{
			if (choice.Text.StartsWith("Choose cards to discard."))
				return new ChoiceResult(new CardCollection(this.FindBestCardsToDiscard(choice.Cards, choice.Cards.Count() - 3)));
			else
				return new ChoiceResult(new CardCollection(FindBestCardsToTrash(choice.Cards, 1)));
		}
		protected override ChoiceResult Decide_SirVander(Choice choice)
		{
			return new ChoiceResult(new CardCollection(FindBestCardsToTrash(choice.Cards, 1)));
		}
		protected override ChoiceResult Decide_Smugglers(Choice choice)
		{
			return new ChoiceResult(new CardCollection(FindBestCards(choice.Cards, 1)));
		}
		protected override ChoiceResult Decide_SpiceMerchant(Choice choice)
		{
			switch (choice.ChoiceType)
			{
				case ChoiceType.Options:
					// To fall in line with Pawn, always take the Coins & Buy
					return new ChoiceResult(new List<String>() { choice.Options[1].Text });

				case ChoiceType.Cards:
					// Only ever trash Coppers
					Card smCopper = choice.Cards.FirstOrDefault(c => c.CardType == Cards.Universal.TypeClass.Copper);
					if (smCopper != null)
						return new ChoiceResult(new CardCollection() { smCopper });

					return new ChoiceResult(new CardCollection());

				default:
					return base.Decide_SpiceMerchant(choice);
			}
		}
		protected override ChoiceResult Decide_Spy(Choice choice)
		{
			if (choice.PlayerSource == this.RealThis)
			{
				if (this.IsCardOKForMeToDiscard(choice.CardTriggers[0]))
					return new ChoiceResult(new List<String>() { choice.Options[0].Text }); // Discard
				else
					return new ChoiceResult(new List<String>() { choice.Options[1].Text }); // Put back
			}
			else
			{
				if (!this.IsCardOKForMeToDiscard(choice.CardTriggers[0]))
					return new ChoiceResult(new List<String>() { choice.Options[0].Text }); // Discard
				else
					return new ChoiceResult(new List<String>() { choice.Options[1].Text }); // Put back
			}
		}
		protected override ChoiceResult Decide_Squire(Choice choice)
		{
			switch (choice.ChoiceType)
			{
				case ChoiceType.Options:
					// Count the number of Action cards in my Hand, Deck, & Discard pile.
					// If there is at least 2 in my hand then choose +2 Actions.  Otherwise, choose Gain a Silver
					List<String> squireChoices = new List<string>() { choice.Options[2].Text };
					if (this.RealThis.Hand[Category.Action].Count >= 2)
						squireChoices[0] = choice.Options[0].Text;
					return new ChoiceResult(squireChoices);

				case ChoiceType.Supplies:
					return new ChoiceResult(FindBestCardForCost(choice.Supplies.Values, null, false));

				default:
					return base.Decide_Squire(choice);
			}
		}
		protected override ChoiceResult Decide_Stables(Choice choice)
		{
			// Always discard Copper
			Card stablesBestCard = choice.Cards.FirstOrDefault(c => c.CardType == Cards.Universal.TypeClass.Copper);
			if (stablesBestCard == null && (this.RealThis.Hand[Cards.Alchemy.TypeClass.Potion].Count > 1 || _Game.Table.Supplies.Count(kvp => kvp.Value.BaseCost.Potion > 0 && kvp.Value.CanGain()) < 1))
				stablesBestCard = choice.Cards.FirstOrDefault(c => c.CardType == Cards.Alchemy.TypeClass.Potion);
			if (stablesBestCard == null && this.RealThis.CountAll(this.RealThis, c => c.CardType == Cards.Universal.TypeClass.Copper, true, false) < 4)
				stablesBestCard = choice.Cards.FirstOrDefault(c => c.CardType == Cards.Prosperity.TypeClass.Loan);
			if (stablesBestCard == null && this.RealThis.Hand[Cards.Hinterlands.TypeClass.FoolsGold].Count == 1)
				stablesBestCard = choice.Cards.FirstOrDefault(c => c.CardType == Cards.Hinterlands.TypeClass.FoolsGold);
			if (stablesBestCard == null)
				stablesBestCard = choice.Cards.FirstOrDefault(c => c.CardType == Cards.Guilds.TypeClass.Masterpiece);
			if (stablesBestCard != null)
				return new ChoiceResult(new CardCollection() { stablesBestCard });

			return new ChoiceResult(new CardCollection());
		}
		protected override ChoiceResult Decide_Stash(Choice choice)
		{
			// For now, always put Stash on top of the draw pile
			// This is very bad for instances involving Thief or Saboteur, but for now, it'll function
			CardCollection cards = new CardCollection();
			cards.AddRange(choice.Cards.Where(c => c.CardType == Cards.Promotional.TypeClass.Stash));
			cards.AddRange(choice.Cards.Where(c => c.CardType != Cards.Promotional.TypeClass.Stash));
			return new ChoiceResult(cards);
		}
		protected override ChoiceResult Decide_Steward(Choice choice)
		{
			switch (choice.ChoiceType)
			{
				case ChoiceType.Options:
					// Trash 2 cards if we have 2 Curse/Ruins cards
					if (this.RealThis.Hand[Category.Curse].Count + this.RealThis.Hand[Category.Ruins].Count >= 2)
						return new ChoiceResult(new List<String>() { choice.Options[2].Text });
					// Otherwise, take 2 Coins if we have at least 3 already
					else if (this.RealThis.Currency.Coin >= 3)
						return new ChoiceResult(new List<String>() { choice.Options[1].Text });
					// Otherwise, just draw 2 cards
					else
						return new ChoiceResult(new List<String>() { choice.Options[0].Text });

				case ChoiceType.Cards:  // Trashing cards
					if (choice.Cards.Count(c => c.CardType == Cards.Universal.TypeClass.Curse || (c.Category & Category.Ruins) == Category.Ruins) >= 2)
						return new ChoiceResult(new CardCollection(choice.Cards.Where(c => 
							c.CardType == Cards.Universal.TypeClass.Curse || (c.Category & Category.Ruins) == Category.Ruins
							).Take(2)));
					else
						return new ChoiceResult(new CardCollection(this.FindBestCardsToTrash(choice.Cards, 2)));

				default:
					return base.Decide_Steward(choice);
			}
		}
		protected override ChoiceResult Decide_Stonemason(Choice choice)
		{
			switch (choice.ChoiceType)
			{
				case ChoiceType.Options:
					for (int index = choice.Options.Count - 1; index >= 0; index--)
					{
						// Overpay by as much as we can properly gain Action cards
						Currency overpayAmount = new DominionBase.Currency(choice.Options[index].Text);
						if (this._Game.Table.Supplies.Values.Any(s => s.CanGain() && (s.Category & Category.Action) == Category.Action && s.CurrentCost == overpayAmount))
							return new ChoiceResult(new List<String>() { choice.Options[index].Text });
					}
					return base.Decide_Stonemason(choice);
				case ChoiceType.Cards:
					return new ChoiceResult(new CardCollection(FindBestCardsToTrash(choice.Cards, 1)));

				case ChoiceType.Supplies:
					if (choice.Text.StartsWith("Gain a card costing less than"))
						return new ChoiceResult(FindBestCardForCost(choice.Supplies.Values, null, false));
					else
						return new ChoiceResult(FindBestCardForCost(choice.Supplies.Values, null, false));

				default:
					return base.Decide_Stonemason(choice);
			}
		}
		protected override ChoiceResult Decide_Storeroom(Choice choice)
		{
			/// TODO -- this needs to be a bit smarter.  It shouldn't discard Action cards if we have 1+ Actions left
			/// It can also be improved to chain with Tactician (and so can Secret Chamber, for that matter)
			// Cards for cards (Cellar)
			if (choice.Text.Contains("+1 Card"))
			{
				if (this.RealThis.Actions == 0)
					// Discard all non-Treasure cards
					return new ChoiceResult(new CardCollection(choice.Cards.Where(c => (c.Category & Category.Treasure) != Category.Treasure)));
				else
					// Discard all non-Action/Treasure cards
					return new ChoiceResult(new CardCollection(choice.Cards.Where(c => (c.Category & Category.Action) != Category.Action && (c.Category & Category.Treasure) != Category.Treasure)));
			}
			// Cards for coins (Secret Chamber/Vault)
			else // "+<coin>1</coin>"
			{
				if (this.RealThis.Actions == 0)
					// Discard all non-Treasure cards
					return new ChoiceResult(new CardCollection(choice.Cards.Where(c => (c.Category & Category.Treasure) != Category.Treasure)));
				else
					// Discard all non-Action/Treasure cards
					return new ChoiceResult(new CardCollection(choice.Cards.Where(c => (c.Category & Category.Action) != Category.Action && (c.Category & Category.Treasure) != Category.Treasure)));
			}
		}
		protected override ChoiceResult Decide_Swindler(Choice choice)
		{
			return new ChoiceResult(FindWorstCardForCost(choice.Supplies.Values, null));
		}
		protected override ChoiceResult Decide_Taxman(Choice choice)
		{
			switch (choice.ChoiceType)
			{
				case ChoiceType.Cards:
					// For Taxman, we have a slightly different priority than with Mine
					// This could be a bit better (taking into account the other players' hands), but for now, it will suffice
					Card mineCard = null;
					if (mineCard == null)
						mineCard = choice.Cards.FirstOrDefault(c => c.CardType == Cards.Universal.TypeClass.Copper);
					if (mineCard == null)
						mineCard = choice.Cards.FirstOrDefault(c => c.CardType == Cards.Universal.TypeClass.Silver);
					if (mineCard == null && _Game.Table.Supplies.ContainsKey(Cards.Prosperity.TypeClass.Platinum) && _Game.Table.Supplies[Cards.Prosperity.TypeClass.Platinum].CanGain())
						mineCard = choice.Cards.FirstOrDefault(c => c.CardType == Cards.Universal.TypeClass.Gold);
					if (mineCard == null)
						mineCard = choice.Cards.FirstOrDefault(c => c.CardType == Cards.Guilds.TypeClass.Masterpiece);
					if (mineCard == null) // Pick a random Treasure at this point
						mineCard = choice.Cards.ElementAt(this._Game.RNG.Next(choice.Cards.Count()));
					return new ChoiceResult(new CardCollection() { mineCard });

				case ChoiceType.Supplies:
					return new ChoiceResult(FindBestCardForCost(choice.Supplies.Values, null, false));

				default:
					return base.Decide_Taxman(choice);
			}
		}
		protected override ChoiceResult Decide_Thief(Choice choice)
		{
			if (choice.Text.StartsWith("Choose a Treasure card of"))
			{
				return new ChoiceResult(new CardCollection(this.FindBestCards(choice.Cards, 1)));
			}
			// Always gain all Treasure cards
			else if (choice.Text.StartsWith("Choose which cards you'd like to gain"))
			{
				// Except Coppers
				CardCollection ccThief = new CardCollection(choice.Cards.Where(c => c.CardType != Cards.Universal.TypeClass.Copper));

				int coppers = this.RealThis.CountAll(this.RealThis, c => c.CardType == Cards.Universal.TypeClass.Copper, true, false);
				int allTreasures = this.RealThis.CountAll(this.RealThis, c => (c.Category & Category.Treasure) == Category.Treasure, true, false);
				double percentageCoppers = ((double)coppers) / allTreasures;

				// Don't gain Loan if we don't have many Coppers
				if (percentageCoppers < 0.1 || (coppers < 3 && percentageCoppers < 0.4))
					ccThief.RemoveAll(c => c.CardType == Cards.Prosperity.TypeClass.Loan);

				return new ChoiceResult(ccThief);
			}
			return base.Decide_Thief(choice);
		}
		protected override ChoiceResult Decide_ThroneRoom(Choice choice)
		{
			Card bestCard = this.FindBestCardToPlay(choice.Cards.Where(c => 
				c.CardType != Cards.Base.TypeClass.Chapel &&
				c.CardType != Cards.Base.TypeClass.Library &&
				c.CardType != Cards.Base.TypeClass.Remodel &&
				c.CardType != Cards.Intrigue.TypeClass.SecretChamber &&
				c.CardType != Cards.Intrigue.TypeClass.Upgrade &&
				c.CardType != Cards.Seaside.TypeClass.Island &&
				c.CardType != Cards.Seaside.TypeClass.Lookout &&
				c.CardType != Cards.Seaside.TypeClass.Outpost &&
				c.CardType != Cards.Seaside.TypeClass.Salvager &&
				c.CardType != Cards.Seaside.TypeClass.Tactician &&
				c.CardType != Cards.Seaside.TypeClass.TreasureMap &&
				c.CardType != Cards.Prosperity.TypeClass.CountingHouse &&
				c.CardType != Cards.Prosperity.TypeClass.Forge &&
				c.CardType != Cards.Prosperity.TypeClass.TradeRoute &&
				c.CardType != Cards.Prosperity.TypeClass.Watchtower &&
				c.CardType != Cards.Cornucopia.TypeClass.Remake &&
				c.CardType != Cards.Hinterlands.TypeClass.Develop &&
				c.CardType != Cards.DarkAges.TypeClass.JunkDealer &&
				c.CardType != Cards.DarkAges.TypeClass.Procession &&
				c.CardType != Cards.DarkAges.TypeClass.Rats &&
				c.CardType != Cards.DarkAges.TypeClass.Rebuild &&
				c.CardType != Cards.Guilds.TypeClass.MerchantGuild &&
				c.CardType != Cards.Guilds.TypeClass.Stonemason));
			// OK, nothing good found.  Now let's allow not-so-useful cards to be played
			if (bestCard == null)
				bestCard = this.FindBestCardToPlay(choice.Cards.Where(c =>
				c.CardType != Cards.Base.TypeClass.Remodel &&
				c.CardType != Cards.Intrigue.TypeClass.Upgrade &&
				c.CardType != Cards.Seaside.TypeClass.Island &&
				c.CardType != Cards.Seaside.TypeClass.Lookout &&
				c.CardType != Cards.Seaside.TypeClass.Salvager &&
				c.CardType != Cards.Seaside.TypeClass.TreasureMap &&
				c.CardType != Cards.Prosperity.TypeClass.TradeRoute &&
				c.CardType != Cards.Cornucopia.TypeClass.Remake &&
				c.CardType != Cards.Hinterlands.TypeClass.Develop &&
				c.CardType != Cards.DarkAges.TypeClass.Rats &&
				c.CardType != Cards.DarkAges.TypeClass.Rebuild &&
				c.CardType != Cards.Guilds.TypeClass.Stonemason));
			if (bestCard != null)
				return new ChoiceResult(new CardCollection() { bestCard });

			return new ChoiceResult(new CardCollection() { choice.Cards.ElementAt(this._Game.RNG.Next(choice.Cards.Count())) });
		}
		protected override ChoiceResult Decide_Tournament(Choice choice)
		{
			switch (choice.ChoiceType)
			{
				case ChoiceType.Options:
					// Always reveal a Province if I can
					return new ChoiceResult(new List<String>() { choice.Options[0].Text });

				case ChoiceType.Cards:
					// Prioritize card worth based on my own metric
					// 1. Duchy if Game Progress is 0.4 or less (closeish to the end)
					// 2. Trusty Steed
					// 3. Followers
					// 4. Princess
					// 5. Bag of Gold
					// 6. Diadem
					// 7. Duchy
					Card bestCard = null;
					if (this.GameProgress < 0.4 && this.RealThis._Game.Table[Cards.Universal.TypeClass.Duchy].Count > 0)
						bestCard = choice.Cards.FirstOrDefault(c => c.CardType == Cards.Universal.TypeClass.Duchy);
					if (bestCard == null)
						bestCard = choice.Cards.FirstOrDefault(c => c.CardType == Cards.Cornucopia.TypeClass.TrustySteed);
					if (bestCard == null)
						bestCard = choice.Cards.FirstOrDefault(c => c.CardType == Cards.Cornucopia.TypeClass.Followers);
					if (bestCard == null)
						bestCard = choice.Cards.FirstOrDefault(c => c.CardType == Cards.Cornucopia.TypeClass.Princess);
					if (bestCard == null)
						bestCard = choice.Cards.FirstOrDefault(c => c.CardType == Cards.Cornucopia.TypeClass.BagOfGold);
					if (bestCard == null)
						bestCard = choice.Cards.FirstOrDefault(c => c.CardType == Cards.Cornucopia.TypeClass.Diadem);
					if (bestCard == null)
						bestCard = choice.Cards.FirstOrDefault(c => c.CardType == Cards.Universal.TypeClass.Duchy);
					if (bestCard != null)
						return new ChoiceResult(new CardCollection() { bestCard });

					return new ChoiceResult(new CardCollection());

				default:
					return base.Decide_Tournament(choice);
			}
		}
		protected override ChoiceResult Decide_Torturer(Choice choice)
		{
			switch (choice.ChoiceType)
			{
				case ChoiceType.Options:
					int torturerCrapCards = this.RealThis.Hand.Count(card => 
						card.CardType == Cards.Universal.TypeClass.Copper || 
						card.Category == Category.Victory || 
						card.Category == Category.Curse ||
						(card.Category & Category.Ruins) == Category.Ruins ||
						card.CardType == Cards.Guilds.TypeClass.Masterpiece);
					// Choose to take a Curse if there aren't any left
					// or if we have a Watchtower or Trader in hand
					if (this.RealThis._Game.Table.Supplies[Cards.Universal.TypeClass.Curse].Count == 0 ||
						this.RealThis.Hand[Cards.Prosperity.TypeClass.Watchtower].Count > 0 ||
						this.RealThis.Hand[Cards.Hinterlands.TypeClass.Trader].Count > 0)
						return new ChoiceResult(new List<String>() { choice.Options[1].Text });
					// Choose to discard 2 cards if we have at least 2 Copper, Victory, Curse, and/or Ruins cards, or if that's all our hand is
					else if (torturerCrapCards >= 2 || this.RealThis.Hand.Count == torturerCrapCards)
						return new ChoiceResult(new List<String>() { choice.Options[0].Text });
					// Choose to take on a Curse
					else
						return new ChoiceResult(new List<String>() { choice.Options[1].Text });

				case ChoiceType.Cards:
					return new ChoiceResult(new CardCollection(this.FindBestCardsToDiscard(choice.Cards, 2)));

				default:
					return base.Decide_Torturer(choice);

			}
		}
		protected override ChoiceResult Decide_Trader(Choice choice)
		{
			// Always trash Curses if we can
			Card traderBestCard = choice.Cards.FirstOrDefault(c => (c.Category & Category.Curse) == Category.Curse);
			// Always trash Ruins if we can
			if (traderBestCard == null)
				traderBestCard = choice.Cards.FirstOrDefault(c => (c.Category & Category.Ruins) == Category.Ruins);
			// Trash Copper later in the game -- they just suck
			if (traderBestCard == null && this.GameProgress < 0.75)
				traderBestCard = choice.Cards.FirstOrDefault(c => c.CardType == Cards.Universal.TypeClass.Copper);
			if (traderBestCard == null)
				traderBestCard = this.FindBestCardsToTrash(choice.Cards.Where(c => (c.Category & Category.Victory) != Category.Victory), 1).FirstOrDefault();
			if (traderBestCard == null)
				traderBestCard = this.FindBestCardsToTrash(choice.Cards, 1).ElementAt(0);

			if (traderBestCard != null)
				return new ChoiceResult(new CardCollection() { traderBestCard });

			return new ChoiceResult(new CardCollection(this.FindBestCardsToTrash(choice.Cards, 1)));
		}
		protected override ChoiceResult Decide_TradeRoute(Choice choice)
		{
			return new ChoiceResult(new CardCollection(this.FindBestCardsToTrash(choice.Cards, 1)));
		}
		protected override ChoiceResult Decide_TradingPost(Choice choice)
		{
			return new ChoiceResult(new CardCollection(this.FindBestCardsToTrash(choice.Cards, 2)));
		}
		protected override ChoiceResult Decide_Transmute(Choice choice)
		{
			return new ChoiceResult(new CardCollection(this.FindBestCardsToTrash(choice.Cards, 1)));
		}
		protected override ChoiceResult Decide_TrustySteed(Choice choice)
		{
			// Count the number of Action cards in my Hand, Deck, & Discard pile.
			// If there is at least 2 in my hand or there is a decent probability that I'll get 2 with drawing,
			// then choose +2 Cards & +2 Actions.  Otherwise, choose +2 Cards & +2 Coin.
			List<String> trustySteedChoices = new List<string>() { choice.Options[0].Text };
			if (this.RealThis.Hand[Category.Action].Count >= 2)
				trustySteedChoices.Add(choice.Options[1].Text);
			else
			{
				int actionCardsAvailable = this.RealThis.CountAll(this.RealThis, c => (c.Category & Category.Action) == Category.Action, true, true);
				int totalCards = this.RealThis.DrawPile.Count + this.RealThis.DiscardPile.Count;
				double chanceOfGettingActions;
				if (this.RealThis.Hand[Category.Action].Count == 1)
					chanceOfGettingActions = 1.0 - ((double)(totalCards - actionCardsAvailable) / totalCards) * ((double)(totalCards - actionCardsAvailable) / (totalCards - 1));
				else
					chanceOfGettingActions = ((double)(actionCardsAvailable) / totalCards) * ((double)(actionCardsAvailable - 1) / (totalCards - 1));

				// We'll be slightly optimistic here -- this may need some tweaking)
				if (chanceOfGettingActions > 0.40)
					trustySteedChoices.Add(choice.Options[1].Text);
				else
					trustySteedChoices.Add(choice.Options[2].Text);
			}
			return new ChoiceResult(trustySteedChoices);
		}
		protected override ChoiceResult Decide_University(Choice choice)
		{
			return new ChoiceResult(FindBestCardForCost(choice.Supplies.Values, null, false));
		}
		protected override ChoiceResult Decide_Upgrade(Choice choice)
		{
			switch (choice.ChoiceType)
			{
				case ChoiceType.Cards:
					return new ChoiceResult(new CardCollection(FindBestCardsToTrash(choice.Cards, 1)));

				case ChoiceType.Supplies:
					return new ChoiceResult(FindBestCardForCost(choice.Supplies.Values, null, false));

				default:
					return base.Decide_Upgrade(choice);
			}
		}
		protected override ChoiceResult Decide_Urchin(Choice choice)
		{
			switch (choice.ChoiceType)
			{
				case ChoiceType.Options:
					// Choose to trash Urchin roughly 1/3 the time
					if (this._Game.RNG.Next(3) == 0)
						return new ChoiceResult(new List<String>() { choice.Options[0].Text });
					else
						return new ChoiceResult(new List<String>() { choice.Options[1].Text });

				case ChoiceType.Cards:
					return new ChoiceResult(new CardCollection(FindBestCardsToDiscard(choice.Cards, choice.Cards.Count() - 4)));

				default:
					return base.Decide_Urchin(choice);
			}
		}
		protected override ChoiceResult Decide_Vault(Choice choice)
		{
			switch (choice.ChoiceType)
			{
				case ChoiceType.Options:
					// If there are at least 2 non-Action & non-Treasure cards, discard 2 of them
					IEnumerable<Card> vaultDiscardableCards = this.RealThis.Hand[c =>
						(c.Category & Category.Treasure) != Category.Treasure &&
						(c.Category & Category.Action) != Category.Action];

					if (vaultDiscardableCards.Count() >= 2)
						return new ChoiceResult(new List<String>() { choice.Options[0].Text });
					else
						return new ChoiceResult(new List<String>() { choice.Options[1].Text });

				case ChoiceType.Cards:
					if (choice.Text.StartsWith("Discard any number of cards"))
					{
						/// TODO -- this needs to be a bit smarter.  It shouldn't discard Action cards if we have 1+ Actions left
						/// It can also be improved to chain with Tactician (and so can Secret Chamber, for that matter)
						// Discard all non-Treasure cards
						return new ChoiceResult(new CardCollection(choice.Cards.Where(c => (c.Category & Category.Treasure) != Category.Treasure)));
					}
					else // "Choose 2 cards to discard"
					{
						CardCollection vDiscards = new CardCollection();
						vDiscards.AddRange(choice.Cards.Where(c => (c.Category & Category.Curse) == Category.Curse));
						if (vDiscards.Count < 2)
							vDiscards.AddRange(choice.Cards.Where(card => (card.Category & Category.Ruins) == Category.Ruins));
						if (vDiscards.Count < 2)
							vDiscards.AddRange(choice.Cards.Where(card => card.Category == Category.Victory));
						if (vDiscards.Count > 2)
							vDiscards.RemoveRange(2, vDiscards.Count - 2);
						else
							vDiscards.AddRange(this.FindBestCardsToDiscard(choice.Cards, 2 - vDiscards.Count));
						return new ChoiceResult(vDiscards);
					}

				default:
					return base.Decide_Vault(choice);
			}
		}
		protected override ChoiceResult Decide_WanderingMinstrel(Choice choice)
		{
			// Just sort everything best-to-worst
			CardCollection cards = new CardCollection(choice.Cards);
			cards.Sort(new DominionBase.Cards.Sorting.ByCost(DominionBase.Cards.Sorting.SortDirection.Descending));
			return new ChoiceResult(cards);
		}
		protected override ChoiceResult Decide_Warehouse(Choice choice)
		{
			return new ChoiceResult(new CardCollection(this.FindBestCardsToDiscard(choice.Cards, 3)));
		}
		protected override ChoiceResult Decide_Watchtower(Choice choice)
		{
			// Always trash Curse & Copper cards from a Watchtower
			if (choice.CardTriggers[0].CardType == Cards.Universal.TypeClass.Curse ||
				choice.CardTriggers[0].CardType == Cards.Universal.TypeClass.Copper)
				return new ChoiceResult(new List<String>() { choice.Options[0].Text });
			// Almost always trash Ruins (only if we have Death Cart do we want to keep them)
			else if ((choice.CardTriggers[0].Category & Category.Ruins) == Category.Ruins &&
				this.RealThis.CountAll(this.RealThis, c => c.CardType == Cards.DarkAges.TypeClass.DeathCart, true, false) == 0)
				return new ChoiceResult(new List<String>() { choice.Options[0].Text });
			else
				return new ChoiceResult(new List<String>() { choice.Options[1].Text });
		}
		protected override ChoiceResult Decide_WishingWell(Choice choice)
		{
			Dictionary<Type, int> _CardsCount = new Dictionary<Type, int>();
			foreach (Type cardType in _CardsGained)
				_CardsCount[cardType] = (int)Math.Pow(this.RealThis.CountAll(this.RealThis, c => c.CardType == cardType, false, true), 2);

			// Choose one at random, with a probability based on the cards left to be able to draw
			int indexChosen = this._Game.RNG.Next(_CardsCount.Sum(kvp => kvp.Value));

			Card wishingWellCard = null;
			foreach (Type cardType in _CardsCount.Keys)
			{
				if (_CardsCount[cardType] == 0)
					continue;
				if (indexChosen < _CardsCount[cardType])
				{
					Supply wishingWellSupply = choice.Supplies.FirstOrDefault(kvp => kvp.Value.CardType == cardType).Value;
					if (wishingWellSupply != null)
						return new ChoiceResult(wishingWellSupply);
					wishingWellCard = choice.Cards.FirstOrDefault(c => c.CardType == cardType);
					break;
				}
				indexChosen -= _CardsCount[cardType];
			}

			if (wishingWellCard != null)
				return new ChoiceResult(new CardCollection() { wishingWellCard });

			return base.Decide_WishingWell(choice);
		}
		protected override ChoiceResult Decide_Workshop(Choice choice)
		{
			return new ChoiceResult(FindBestCardForCost(choice.Supplies.Values, null, false));
		}
		protected override ChoiceResult Decide_YoungWitch(Choice choice)
		{
			return new ChoiceResult(new CardCollection(this.FindBestCardsToDiscard(choice.Cards, 2)));
		}
	}
}
