using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using DominionBase.Cards;
using DominionBase.Piles;

namespace DominionBase.Players.AI
{
	/// <summary>
	/// The jatill AI is as close to the same as the AI that jatill did for his application
	/// </summary>
	public class jatill : Basic
	{
		public new static String AIName { get { return "jatill's (old)"; } }
		public new static String AIDescription { get { return "Makes decisions based on jatill's code from back in February 2011 (now very dated).  Will perform worse & worse as expansions are released."; } }

		public jatill(Game game, String name) : base(game, name) { }
		public jatill(Game game, String name, Player realThis) : base(game, name, realThis) { }

		protected override Card FindBestCardToPlay(IEnumerable<Card> cards)
		{
			if (this.RealThis.Phase == PhaseEnum.Action)
			{
				// Sort the cards by cost (potion = 2.5 * coin)
				// Also, use a cost of 7 for Prize cards (since they have no cost normally)
				cards = cards.Where(card => this.ShouldPlay(card)).OrderByDescending(
					card => (card.Category & Category.Prize) == Category.Prize ? 7 : (card.BaseCost.Coin.Value + 2.5 * card.BaseCost.Potion.Value));

				// Always play King's Court if there is one (?)
				Card kc = cards.FirstOrDefault(card => card.CardType == Cards.Prosperity.TypeClass.KingsCourt);
				if (kc != null)
					return kc;

				// Always play Throne Room if there is one (?)
				Card tr = cards.FirstOrDefault(card => card.CardType == Cards.Base.TypeClass.ThroneRoom);
				if (tr != null)
					return tr;

				Card plusActions = cards.FirstOrDefault(card => card.Benefit.Actions > 0 && card.CardType != Cards.Intrigue.TypeClass.ShantyTown);
				if (plusActions != null)
					return plusActions;

				Card shantyTown = cards.FirstOrDefault(card => card.CardType == Cards.Intrigue.TypeClass.ShantyTown);
				if (shantyTown != null)
					return shantyTown;

				Turn previousTurn = null;
				if (this.RealThis._Game.TurnsTaken.Count > 1)
					previousTurn = this.RealThis._Game.TurnsTaken[this.RealThis._Game.TurnsTaken.Count - 2];

				// Play Smugglers if the player to our right gained a card costing at least 5 (that we can gain as well)
				if (cards.Any(card => card.CardType == Cards.Seaside.TypeClass.Smugglers) &&
					previousTurn != null && previousTurn.CardsGained.Any(card => _Game.ComputeCost(card).Potion == 0 && card.BaseCost.Coin >= 5 &&
					this.RealThis._Game.Table.Supplies.ContainsKey(card) && this.RealThis._Game.Table.Supplies[card].CanGain()))
					return cards.First(card => card.CardType == Cards.Seaside.TypeClass.Smugglers);

				// Play an Ambassador card if there is one and we have at least 1 Curse in our hand
				Card ambassador = cards.FirstOrDefault(card => card.CardType == Cards.Seaside.TypeClass.Ambassador);
				if (ambassador != null)
					return ambassador;

				if (cards.Count() > 0)
					// Just play the most expensive one
					return cards.ElementAt(0);

				return null;
			}
			return base.FindBestCardToPlay(cards);
		}

		protected override CardCollection FindBestCardsToPlay(IEnumerable<Card> cards)
		{
			if (this.RealThis.Phase == PhaseEnum.ActionTreasure || this.RealThis.Phase == PhaseEnum.BuyTreasure)
			{
				IEnumerable<Card> t = cards.Where(c => c.CardType != Cards.Prosperity.TypeClass.Contraband || c.CardType != Cards.Prosperity.TypeClass.Bank);
				if (t.Count() > 0)
					return new CardCollection(t);

				t = cards.Where(c => c.CardType == Cards.Prosperity.TypeClass.Bank);
				if (t.Count() > 0)
					return new CardCollection(t);

				return new CardCollection();
			}
			return base.FindBestCardsToPlay(cards);
		}

		protected override bool ShouldBuy(Type type)
		{
			if (type == Cards.Universal.TypeClass.Curse)
				return false;
			else if (type == Cards.Base.TypeClass.Adventurer)
				return false;
			else if (type == Cards.Base.TypeClass.Moneylender)
				return false;
			else if (type == Cards.Base.TypeClass.Woodcutter)
				return false;
			else if (type == Cards.Base.TypeClass.Chancellor)
				return false;
			else if (type == Cards.Base.TypeClass.Chapel)
				return false;
			else if (type == Cards.Base.TypeClass.Spy)
				return false;
			else if (type == Cards.Base.TypeClass.Remodel)
				return false;
			else if (type == Cards.Intrigue.TypeClass.Upgrade)
				return false;
			else if (type == Cards.Intrigue.TypeClass.Bridge)
				return false;
			else if (type == Cards.Intrigue.TypeClass.TradingPost)
				return false;
			else if (type == Cards.Intrigue.TypeClass.Masquerade)
				return false;
			else if (type == Cards.Intrigue.TypeClass.Conspirator)
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
			else if (type == Cards.Alchemy.TypeClass.Potion)
				return false;
			//else if (type == Cards.Alchemy.TypeClass.Possession)
			//    return false;
			else if (type == Cards.Alchemy.TypeClass.Apprentice)
				return false;
			else if (type == Cards.Prosperity.TypeClass.Contraband)
				return false;
			else if (type == Cards.Prosperity.TypeClass.Expand)
				return false;
			else if (type == Cards.Prosperity.TypeClass.Forge)
				return false;
			else if (type == Cards.Prosperity.TypeClass.GrandMarket)
				return false;
			else if (type == Cards.Prosperity.TypeClass.Loan)
				return false;
			else if (type == Cards.Prosperity.TypeClass.TradeRoute)
				return false;
			else if (type == Cards.Prosperity.TypeClass.WorkersVillage)
				return false;
			else if (type == Cards.Cornucopia.TypeClass.Remake)
				return false;
			return true;
		}

		protected override Supply FindBestCardToBuy(List<Supply> buyableSupplies)
		{
			return this.FindBestCardForCost(buyableSupplies, this.RealThis.Currency, true);
		}

		protected override Supply FindBestCardForCost(IEnumerable<Supply> buyableSupplies, Currency currency, Boolean buying)
		{
			List<Supply> bestSupplies = new List<Supply>();
			Cost bestCost = null;

			// Buy a potion if we don't have any and we can (??)
			if (this.RealThis.Currency != (Currency)null && (this.RealThis.Currency.Coin == 4 && this.RealThis.CountAll(this.RealThis, c => c.CardType == Cards.Alchemy.TypeClass.Potion, true, false) == 0))
			{
				Supply potion = buyableSupplies.SingleOrDefault(supply => supply.CardType == Cards.Alchemy.TypeClass.Potion);
				if (potion != null)
					return potion;
			}

			foreach (Supply supply in buyableSupplies)
			{
				if (!ShouldBuy(supply))
					continue;

				// Only return ones we CAN gain
				if (currency != (Currency)null && currency < supply.CurrentCost)
					continue;

				// If the card chosen is under embargo, then possibly skip it
				if (buying && this._Game.RNG.Next(supply.Tokens.Count(t => t.GetType() == Cards.Seaside.TypeClass.EmbargoToken)) != 0)
					continue;

				// If we chose Peddler, but it costs more than 5, then possibly skip it
				if (supply.CardType == Cards.Prosperity.TypeClass.Peddler && supply.CurrentCost.Coin > 5 && this._Game.RNG.Next(10) != 0)
					continue;

				if (bestCost == (Cost)null ||
					(bestCost.Coin.Value + 2.5 * bestCost.Potion.Value) <= (supply.BaseCost.Coin.Value + 2.5 * supply.BaseCost.Potion.Value))
				{
					// Don't buy if it's not a Victory card near the end of the game
					if (this.GameProgress < 0.25 && (supply.Category & Category.Victory) != Category.Victory)
						continue;

					// Never buy Duchies or Estates early
					if ((this.GameProgress > 0.25 && supply.SupplyCardType == Cards.Universal.TypeClass.Estate) ||
						(this.GameProgress > 0.4 && supply.SupplyCardType == Cards.Universal.TypeClass.Duchy))
						continue;

					// Reset best cost to new one
					if (bestCost == (Cost)null || (bestCost.Coin.Value + 2.5 * bestCost.Potion.Value) < (supply.BaseCost.Coin.Value + 2.5 * supply.BaseCost.Potion.Value))
					{
						bestCost = supply.BaseCost;
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

					if (bestCost == (Cost)null || bestCost.Coin.Value <= supply.BaseCost.Coin.Value)
					{
						// Reset best cost to new one
						if (bestCost == (Cost)null || bestCost.Coin.Value < supply.BaseCost.Coin.Value)
						{
							bestCost = supply.BaseCost;
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

			return worstSupplies[this._Game.RNG.Next(worstSupplies.Count)];
		}

		protected override IEnumerable<Card> FindBestCards(IEnumerable<Card> cards, int count)
		{
			// Chose the most expensive cards
			CardCollection cardsToReturn = new CardCollection();

			cardsToReturn.AddRange(cards.OrderByDescending(c => c.BaseCost).ThenBy(c => c.Name).Take(count));

			return cardsToReturn;
		}

		protected override IEnumerable<Card> FindBestCardsToDiscard(IEnumerable<Card> cards, int count)
		{
			// choose the worse card in hand in this order
			// 1) positive victory points
			// 2) curse
			// 3) cheapest card left

			CardCollection cardsToDiscard = new CardCollection();
			CardCollection cardsLeftOver = new CardCollection();
			foreach (Card card in cards)
			{
				if (card.Category == Category.Victory)
					cardsToDiscard.Add(card);
				else
					cardsLeftOver.Add(card);
				if (cardsToDiscard.Count >= count)
					break;
			}
			if (cardsToDiscard.Count >= count)
				return cardsToDiscard;
			cardsToDiscard.AddRange(FindBestCardsToTrash(cardsLeftOver, count - cardsToDiscard.Count));
			return cardsToDiscard;
		}

		protected override IEnumerable<Card> FindBestCardsToTrash(IEnumerable<Card> cards, int count)
		{
			// choose the worse card in hand in this order
			// 1) curse
			// 2) cheapest card left

			CardCollection cardsToTrash = new CardCollection();
			cardsToTrash.AddRange(cards.Where(c => c.Category == Category.Curse).Take(count));
			if (cardsToTrash.Count >= count)
				return cardsToTrash;

			cardsToTrash.AddRange(cards.OrderBy(c => c.BaseCost).ThenBy(c => c.Name).Where(c => !cardsToTrash.Contains(c)).Take(count - cardsToTrash.Count));

			return cardsToTrash;
		}

		protected override bool ShouldPlay(Card card)
		{
			if (!ShouldBuy(card.CardType))
				return false;

			int previousTurnIndex = this.RealThis._Game.TurnsTaken.Count - 2;
			Turn previousTurn = null;
			if (previousTurnIndex >= 0)
				previousTurn = this.RealThis._Game.TurnsTaken[previousTurnIndex];

			if (card.CardType == Cards.Base.TypeClass.Mine)
			{
				// Mine can only be played if you have a copper or silver in hand,
				// and the next highest coin is available
				if ((this.RealThis.Hand[Cards.Universal.TypeClass.Copper].Count > 0 && this.RealThis._Game.Table.Supplies[Cards.Universal.TypeClass.Silver].Count > 0) ||
					(this.RealThis.Hand[Cards.Universal.TypeClass.Silver].Count > 0 && this.RealThis._Game.Table.Supplies[Cards.Universal.TypeClass.Gold].Count > 0))
					return true;
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
				// Only play Island if we have Estate, Duchy, Province, or Curse in hand
				if (this.RealThis.Hand.Count(
					c => c.CardType == Cards.Universal.TypeClass.Estate ||
						c.CardType == Cards.Universal.TypeClass.Duchy ||
						c.CardType == Cards.Universal.TypeClass.Province ||
						c.CardType == Cards.Universal.TypeClass.Curse) <= 1)
					return false;
			}
			else if (card.CardType == Cards.Seaside.TypeClass.Outpost)
			{
				// Don't play if we're already in our 2nd turn
				if (previousTurn != null && previousTurn.Player == this.RealThis)
					return false;
			}
			else if (card.CardType == Cards.Seaside.TypeClass.Smugglers)
			{
				Player playerToRight = this.RealThis._Game.GetPlayerFromIndex(this.RealThis, -1);
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
				if (this.RealThis.Hand[Category.Treasure].Count == 0)
					return false;
			}

			return true;
		}

		protected override ChoiceResult Decide_Attacked(Choice choice, AttackedEventArgs aea, IEnumerable<Type> cardsToReveal)
		{
			if (cardsToReveal.Contains(Cards.Base.TypeClass.Moat) && !aea.Cancelled)
				return new ChoiceResult(new CardCollection() { aea.Revealable[Cards.Base.TypeClass.Moat].Card });

			// Always reveal Horse Traders (any reason *not* to?)
			if (cardsToReveal.Contains(Cards.Cornucopia.TypeClass.HorseTraders))
				return new ChoiceResult(new CardCollection() { aea.Revealable[Cards.Cornucopia.TypeClass.HorseTraders].Card });

			if (cardsToReveal.Contains(Cards.Intrigue.TypeClass.SecretChamber) && !aea.HandledBy.Contains(Cards.Intrigue.TypeClass.SecretChamber) && 
				(_LastReactedCard == null || _LastReactedCard != choice.CardTriggers[0]))
				return new ChoiceResult(new CardCollection() { aea.Revealable[Cards.Intrigue.TypeClass.SecretChamber].Card });

			return new ChoiceResult(new CardCollection());
		}
		protected override ChoiceResult Decide_CardGain(Choice choice, CardGainEventArgs cgea, IEnumerable<Type> cardTriggerTypes)
		{
			// Always put card on top of your deck
			if (cardTriggerTypes.Contains(Cards.Prosperity.TypeClass.RoyalSeal))
				return new ChoiceResult(new List<String>() { cgea.Actions[Cards.Prosperity.TypeClass.RoyalSeal].Text });

			// Always reveal for Curse & Copper cards from a Watchtower (to trash)
			if (cardTriggerTypes.Contains(Cards.Prosperity.TypeClass.Watchtower))
			{
				if (choice.CardTriggers[0].Category == Category.Curse || choice.CardTriggers[0].CardType == Cards.Universal.TypeClass.Copper)
					return new ChoiceResult(new List<String>() { cgea.Actions[Cards.Prosperity.TypeClass.Watchtower].Text });
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
			{
				if (this.RealThis.InPlay[Category.Treasure].Any(c => c.CardType != Cards.Universal.TypeClass.Copper))
					return new ChoiceResult(new List<String>() { cdea.GetAction(Cards.Alchemy.TypeClass.Herbalist).Text });
			}

			// Always reveal this when discarding
			if (cardTriggerTypes.Any(t => t.Item1 == Cards.Hinterlands.TypeClass.Tunnel))
				return new ChoiceResult(new List<String>() { cdea.GetAction(Cards.Hinterlands.TypeClass.Tunnel).Text });

			return new ChoiceResult(new List<String>());
		}
		protected override ChoiceResult Decide_CleaningUp(Choice choice, CleaningUpEventArgs cuea, IEnumerable<Type> cardTriggerTypes)
		{
			// Always put Walled Village on my deck if I can
			if (cardTriggerTypes.Contains(Cards.Promotional.TypeClass.WalledVillage))
				return new ChoiceResult(new List<String>() { cuea.Actions[Cards.Promotional.TypeClass.WalledVillage].Text });

			return new ChoiceResult(new List<String>());
		}

		protected override ChoiceResult Decide_RevealBane(Choice choice)
		{
			// Always reveal the Bane card if I can
			return new ChoiceResult(new List<String>() { choice.Options[0].Text });
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
		protected override ChoiceResult Decide_Apprentice(Choice choice)
		{
			return new ChoiceResult(new CardCollection(this.FindBestCardsToTrash(choice.Cards, 1)));
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
				// Always choose to trash a Curse from Bishop if I have one
				if (choice.Cards.Count(c => c.CardType == Cards.Universal.TypeClass.Curse) > 0)
					return new ChoiceResult(new CardCollection() { choice.Cards.First(c => c.CardType == Cards.Universal.TypeClass.Curse) });
				else
					return new ChoiceResult(new CardCollection(this.FindBestCardsToTrash(choice.Cards, 1)));
			}
			else // Optionally trash a card -- other players
			{
				// Always choose to trash a Curse from Bishop if I have one
				if (choice.Cards.Count(c => c.CardType == Cards.Universal.TypeClass.Curse) > 0)
					return new ChoiceResult(new CardCollection() { choice.Cards.First(c => c.CardType == Cards.Universal.TypeClass.Curse) });
				else
					return new ChoiceResult(new CardCollection());
			}
		}
		protected override ChoiceResult Decide_BorderVillage(Choice choice)
		{
			return new ChoiceResult(FindBestCardForCost(choice.Supplies.Values, null, false));
		}
		protected override ChoiceResult Decide_Cellar(Choice choice)
		{
			// Discard all non-Treasure cards
			return new ChoiceResult(new CardCollection(choice.Cards.Where(c => (c.Category & Category.Treasure) != Category.Treasure)));
		}
		protected override ChoiceResult Decide_Chancellor(Choice choice)
		{
			// Always discard deck -- ??? That seems a bit odd...
			return new ChoiceResult(new List<String>() { choice.Options[0].Text });
		}
		protected override ChoiceResult Decide_Chapel(Choice choice)
		{
			// Always choose to trash all Curses
			if (choice.Cards.Count(c => c.CardType == Cards.Universal.TypeClass.Curse) > 0)
				return new ChoiceResult(new CardCollection(choice.Cards.Where(c => c.CardType == Cards.Universal.TypeClass.Curse).Take(4)));
			else
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
			// We'll assume roughly 2 coins per card left in his hand
			int remainingCoins = Math.Max(0, 2 * choice.PlayerSource.Hand.Count + (int)(3 * Utilities.Gaussian.NextGaussian(this._Game.RNG)));
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
		protected override ChoiceResult Decide_Embargo(Choice choice)
		{
			List<Supply> embargoAbleSupplies = new List<Supply>();
			foreach (Supply supply in choice.Supplies.Values.Where(s => s.SupplyCardType != Cards.Universal.TypeClass.Curse))
			{
				if (!ShouldBuy(supply))
					embargoAbleSupplies.Add(supply);
			}
			if (embargoAbleSupplies.Count == 0)
				embargoAbleSupplies.Add(choice.Supplies[Cards.Universal.TypeClass.Province]);
			return new ChoiceResult(embargoAbleSupplies[this._Game.RNG.Next(embargoAbleSupplies.Count)]);
		}
		protected override ChoiceResult Decide_Envoy(Choice choice)
		{
			// Find most-expensive non-Victory card to discard
			IEnumerable<Card> cardsEnvoy = this.FindBestCards(choice.Cards.Where(c => c.Category != Category.Curse && c.Category != Category.Victory), 1);
			if (cardsEnvoy != null && cardsEnvoy.Count() > 0)
				return new ChoiceResult(new CardCollection(cardsEnvoy));
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
					return new ChoiceResult(new CardCollection(FindBestCardsToTrash(choice.Cards, 1)));

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
		protected override ChoiceResult Decide_Forge(Choice choice)
		{
			switch (choice.ChoiceType)
			{
				case ChoiceType.Cards:
					// Only trash Curses
					if (choice.Cards.Count(c => c.CardType == Cards.Universal.TypeClass.Curse) > 0)
						return new ChoiceResult(new CardCollection() { choice.Cards.First(c => c.CardType == Cards.Universal.TypeClass.Curse) });
					else
						return new ChoiceResult(new CardCollection());

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
		protected override ChoiceResult Decide_Goons(Choice choice)
		{
			return new ChoiceResult(new CardCollection(this.FindBestCardsToDiscard(choice.Cards, choice.Cards.Count() - 3)));
		}
		protected override ChoiceResult Decide_Haggler(Choice choice)
		{
			return new ChoiceResult(FindBestCardForCost(choice.Supplies.Values, null, false));
		}
		protected override ChoiceResult Decide_Haven(Choice choice)
		{
			Card havenBestCard = null;
			if (this.RealThis.Currency.Coin > 4)
			{
				havenBestCard = choice.Cards.Where(c => (c.Category & Category.Action) == Category.Action).OrderByDescending(c => c.BaseCost.Coin.Value + 2.5 * c.BaseCost.Potion.Value).FirstOrDefault();
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
				if (havenTreasures.Count() > 0)
					return new ChoiceResult(new CardCollection() { havenTreasures.ElementAt(this._Game.RNG.Next(havenTreasures.Count())) });
			}

			// Just pick a random card if we still haven't found a decent one
			return new ChoiceResult(new CardCollection() { choice.Cards.ElementAt(this._Game.RNG.Next(choice.Cards.Count())) });
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
		protected override ChoiceResult Decide_HornOfPlenty(Choice choice)
		{
			// If it's early on, never gain a Victory card (since that trashes the Horn of Plenty)
			// Also, only do it about 1/2 the time after that
			if (this.GameProgress > 0.35 || this._Game.RNG.Next(2) == 0)
				return new ChoiceResult(FindBestCardForCost(choice.Supplies.Values.Where(supply => (supply.Category & Category.Victory) != Category.Victory), null, false));
			else
				return new ChoiceResult(FindBestCardForCost(choice.Supplies.Values, null, false));
		}
		protected override ChoiceResult Decide_Ironworks(Choice choice)
		{
			return new ChoiceResult(FindBestCardForCost(choice.Supplies.Values, null, false));
		}
		protected override ChoiceResult Decide_Island(Choice choice)
		{
			if (choice.Cards.Count(c => c.CardType == Cards.Universal.TypeClass.Estate) > 0)
				return new ChoiceResult(new CardCollection() { choice.Cards.First(c => c.CardType == Cards.Universal.TypeClass.Estate) });
			else if (choice.Cards.Count(c => c.CardType == Cards.Universal.TypeClass.Duchy) > 0)
				return new ChoiceResult(new CardCollection() { choice.Cards.First(c => c.CardType == Cards.Universal.TypeClass.Duchy) });
			else if (choice.Cards.Count(c => c.CardType == Cards.Universal.TypeClass.Province) > 0)
				return new ChoiceResult(new CardCollection() { choice.Cards.First(c => c.CardType == Cards.Universal.TypeClass.Province) });
			else if (choice.Cards.Count(c => c.CardType == Cards.Universal.TypeClass.Curse) > 0)
				return new ChoiceResult(new CardCollection() { choice.Cards.First(c => c.CardType == Cards.Universal.TypeClass.Curse) });
			return base.Decide_Island(choice);
		}
		protected override ChoiceResult Decide_KingsCourt(Choice choice)
		{
			Card bestCard = this.FindBestCardToPlay(choice.Cards);
			if (bestCard != null)
				return new ChoiceResult(new CardCollection() { this.FindBestCardToPlay(choice.Cards) });
			return new ChoiceResult(new CardCollection());
		}
		protected override ChoiceResult Decide_Library(Choice choice)
		{
			// Always set aside Action cards
			return new ChoiceResult(new List<String>() { choice.Options[0].Text });
		}
		protected override ChoiceResult Decide_Loan(Choice choice)
		{
			// Choose to trash Copper roughly 1/3 of the time (a little odd, but it should work decently)
			if (choice.CardTriggers[0].CardType == Cards.Universal.TypeClass.Copper && this._Game.RNG.Next(3) == 0)
				return new ChoiceResult(new List<String>() { choice.Options[1].Text });
			else
				return new ChoiceResult(new List<String>() { choice.Options[0].Text });
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
					Card mineCard = choice.Cards.FirstOrDefault(c => c.CardType == Cards.Universal.TypeClass.Silver);
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
			// Gain coins if between 4 & 7 Coins available (?? Odd choice)
			if (this.RealThis.Currency.Coin > 3 && this.RealThis.Currency.Coin < 8)
				return new ChoiceResult(new List<String>() { choice.Options[0].Text }); // Yes
			else
				return new ChoiceResult(new List<String>() { choice.Options[1].Text }); // No
		}
		protected override ChoiceResult Decide_Mint(Choice choice)
		{
			// Always choose the Treasure card that costs the most to duplicate
			Card bestCard = null;
			foreach (Card card in choice.Cards)
			{
				if (this.RealThis._Game.Table.Supplies[card].CanGain() &&
					(bestCard == null || bestCard.Benefit.Currency.Coin < card.Benefit.Currency.Coin || bestCard.Benefit.Currency.Coin == 0))
					bestCard = card;
			}
			if (bestCard != null)
				return new ChoiceResult(new CardCollection() { bestCard });

			return new ChoiceResult(new CardCollection());
		}
		protected override ChoiceResult Decide_Mountebank(Choice choice)
		{
			// Always discard curse if I can (better to not if I can trash it, but I never buy trashing cards anyway)
			return new ChoiceResult(new List<String>() { choice.Options[0].Text });
		}
		protected override ChoiceResult Decide_NativeVillage(Choice choice)
		{
			// Retrieve cards from the Native Village mat if there are at least 2 cards there
			if (this.RealThis.PlayerMats[Cards.Seaside.TypeClass.NativeVillageMat].Count >= 2)
				return new ChoiceResult(new List<String>() { choice.Options[1].Text });
			else
				return new ChoiceResult(new List<String>() { choice.Options[0].Text });
		}
		protected override ChoiceResult Decide_Nobles(Choice choice)
		{
			// Choose +2 Actions only if there are at least 2 Action cards in hand already
			if (this.RealThis.Hand[Category.Action].Count >= 2)
				return new ChoiceResult(new List<String>() { choice.Options[1].Text }); // +2 Actions
			else
				return new ChoiceResult(new List<String>() { choice.Options[0].Text }); // +3 Cards
		}
		protected override ChoiceResult Decide_Pawn(Choice choice)
		{
			// Always choose +1 Coin.  Only choose +1 Action if there's at least 1 Action card in hand
			List<String> pawnChoices = new List<string>() { choice.Options[3].Text }; // +1 Coin
			if (this.RealThis.Hand[Category.Action].Count >= 1)
				pawnChoices.Add(choice.Options[1].Text); // +1 Action
			else
				pawnChoices.Add(choice.Options[0].Text); // +1 Card
			return new ChoiceResult(pawnChoices);
		}
		protected override ChoiceResult Decide_PearlDiver(Choice choice)
		{
			// only put on top if the card has no victory points associated with it
			if ((choice.CardTriggers[0].Category & Category.Victory) == Category.Victory ||
				choice.CardTriggers[0].Category == Category.Curse)
				return new ChoiceResult(new List<String>() { choice.Options[1].Text });
			else
				return new ChoiceResult(new List<String>() { choice.Options[0].Text });
		}
		protected override ChoiceResult Decide_PirateShip(Choice choice)
		{
			switch (choice.ChoiceType)
			{
				case ChoiceType.Options:
					// Steal coins until I have at least 3 Pirate Ship tokens on my mat
					if (this.RealThis.TokenPiles[Cards.Seaside.TypeClass.PirateShipToken].Count > 3)
						return new ChoiceResult(new List<String>() { choice.Options[1].Text });
					else
						return new ChoiceResult(new List<String>() { choice.Options[0].Text });

				case ChoiceType.Cards:
					return new ChoiceResult(new CardCollection(this.FindBestCards(choice.Cards, 1)));

				default:
					return base.Decide_Remodel(choice);
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
		protected override ChoiceResult Decide_Saboteur(Choice choice)
		{
			return new ChoiceResult(FindBestCardForCost(choice.Supplies.Values, null, false));
		}
		protected override ChoiceResult Decide_ScryingPool(Choice choice)
		{
			if (choice.PlayerSource == this.RealThis)
			{
				if (choice.CardTriggers[0].Category == Category.Victory ||
					choice.CardTriggers[0].Category == Category.Curse ||
					choice.CardTriggers[0].CardType == Cards.Universal.TypeClass.Copper)
					return new ChoiceResult(new List<String>() { choice.Options[0].Text });
				else
					return new ChoiceResult(new List<String>() { choice.Options[1].Text });
			}
			else
			{
				if (choice.CardTriggers[0].Category == Category.Victory ||
					choice.CardTriggers[0].Category == Category.Curse ||
					choice.CardTriggers[0].CardType == Cards.Universal.TypeClass.Copper)
					return new ChoiceResult(new List<String>() { choice.Options[1].Text });
				else
					return new ChoiceResult(new List<String>() { choice.Options[0].Text });
			}
		}
		protected override ChoiceResult Decide_SecretChamber(Choice choice)
		{
			if (choice.Text == "Choose order of cards to put back on your deck")
				return new ChoiceResult(new CardCollection(this.FindBestCardsToDiscard(choice.Cards, 2)));
			else
				// Discard all non-Treasure cards
				return new ChoiceResult(new CardCollection(choice.Cards.Where(c => (c.Category & Category.Treasure) != Category.Treasure)));
		}
		protected override ChoiceResult Decide_Smugglers(Choice choice)
		{
			return new ChoiceResult(FindBestCardForCost(choice.Supplies.Values, null, false));
		}
		protected override ChoiceResult Decide_Spy(Choice choice)
		{
			if (choice.PlayerSource == this.RealThis)
			{
				if (choice.CardTriggers[0].Category == Category.Victory ||
					choice.CardTriggers[0].Category == Category.Curse ||
					choice.CardTriggers[0].CardType == Cards.Universal.TypeClass.Copper ||
					choice.CardTriggers[0].CardType == Cards.Hinterlands.TypeClass.Tunnel)
					return new ChoiceResult(new List<String>() { choice.Options[0].Text }); // Discard
				else
					return new ChoiceResult(new List<String>() { choice.Options[1].Text }); // Put back
			}
			else
			{
				if (choice.CardTriggers[0].Category == Category.Victory ||
					choice.CardTriggers[0].Category == Category.Curse ||
					choice.CardTriggers[0].CardType == Cards.Universal.TypeClass.Copper)
					return new ChoiceResult(new List<String>() { choice.Options[1].Text }); // Put back
				else
					return new ChoiceResult(new List<String>() { choice.Options[0].Text }); // Discard
			}
		}
		protected override ChoiceResult Decide_Steward(Choice choice)
		{
			switch (choice.ChoiceType)
			{
				case ChoiceType.Options:
					// Take 2 Coins if we have at least 3 already
					if (this.RealThis.Currency.Coin >= 3)
						return new ChoiceResult(new List<String>() { choice.Options[1].Text }); // +2 Coins
					// Otherwise, just draw 2 cards
					else
						return new ChoiceResult(new List<String>() { choice.Options[0].Text }); // +2 Cards

				case ChoiceType.Cards:  // Trashing cards
					if (choice.Cards.Count(c => c.CardType == Cards.Universal.TypeClass.Curse) >= 2)
						return new ChoiceResult(new CardCollection(choice.Cards.Where(c => c.CardType == Cards.Universal.TypeClass.Curse).Take(2)));
					else
						return new ChoiceResult(new CardCollection(this.FindBestCardsToTrash(choice.Cards, 2)));

				default:
					return base.Decide_Steward(choice);
			}
		}
		protected override ChoiceResult Decide_Swindler(Choice choice)
		{
			return new ChoiceResult(FindWorstCardForCost(choice.Supplies.Values, null));
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
				return new ChoiceResult(new CardCollection(choice.Cards));
			}
			return base.Decide_Thief(choice);
		}
		protected override ChoiceResult Decide_ThroneRoom(Choice choice)
		{
			Card bestCard = this.FindBestCardToPlay(choice.Cards);
			if (bestCard != null)
				return new ChoiceResult(new CardCollection() { this.FindBestCardToPlay(choice.Cards) });

			return base.Decide_ThroneRoom(choice);
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
					// 2. Random of everything else
					Card bestCard = null;
					if (this.GameProgress < 0.4 && this.RealThis._Game.Table[Cards.Universal.TypeClass.Duchy].Count > 0)
						bestCard = choice.Cards.FirstOrDefault(c => c.CardType == Cards.Universal.TypeClass.Duchy);
					if (bestCard == null)
					{
						IEnumerable<Card> tournamentCCards = choice.Cards.Where(c => c.Source == Source.Cornucopia);
						bestCard = tournamentCCards.ElementAt(this._Game.RNG.Next(tournamentCCards.Count()));
					}
					if (bestCard == null)
						bestCard = choice.Cards.FirstOrDefault(c => c.CardType == Cards.Universal.TypeClass.Duchy);
					if (bestCard != null)
						return new ChoiceResult(new CardCollection() { bestCard });
					else
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
					int torturerCrapCards = this.RealThis.Hand.Count(card => card.CardType == Cards.Universal.TypeClass.Copper || card.Category == Category.Victory || card.Category == Category.Curse);
					// Choose to take a Curse if there aren't any left
					if (this.RealThis._Game.Table.Supplies[Cards.Universal.TypeClass.Curse].Count == 0)
						return new ChoiceResult(new List<String>() { choice.Options[1].Text });
					// Choose to discard 2 cards if we have at least 2 Copper, Victory, and/or Curse cards, or if that's all our hand is
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
			// Always choose +2 Coin.  Only choose +2 Actions if there's at least 1 Action card in hand
			List<String> trustySteedChoices = new List<string>() { choice.Options[2].Text };
			if (this.RealThis.Hand[Category.Action].Count >= 1)
				trustySteedChoices.Add(choice.Options[1].Text);
			else
				trustySteedChoices.Add(choice.Options[0].Text);
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
						// Discard all non-Treasure cards
						return new ChoiceResult(new CardCollection(choice.Cards.Where(c => (c.Category & Category.Treasure) != Category.Treasure)));
					}
					else // "Choose 2 cards to discard"
					{
						// Discard 2 non-Action & non-Treasure cards at random
						List<Card> vaultMatchingCards = choice.Cards.Where(c =>
							(c.Category & Category.Treasure) != Category.Treasure &&
							(c.Category & Category.Action) != Category.Action).ToList();

						Utilities.Shuffler.Shuffle(vaultMatchingCards);
						return new ChoiceResult(new CardCollection(vaultMatchingCards.Take(2)));
					}

				default:
					return base.Decide_Vault(choice);
			}
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
			else
				return new ChoiceResult(new List<String>() { choice.Options[1].Text });
		}
		protected override ChoiceResult Decide_WishingWell(Choice choice)
		{
			Card wwCard = null;
			Supply wwSupply = null;
			while (wwCard == null)
			{
				Type randomCard = _CardsGained[this._Game.RNG.Next(_CardsGained.Count)];
				wwSupply = choice.Supplies.FirstOrDefault(kvp => kvp.Value.CardType == randomCard).Value;
				if (wwSupply != null)
					return new ChoiceResult(wwSupply);
				wwCard = choice.Cards.FirstOrDefault(c => c.CardType == randomCard);
				if (wwCard != null)
					return new ChoiceResult(new CardCollection() { wwCard });
			}
			return new ChoiceResult(new CardCollection() { wwCard });
		}
		protected override ChoiceResult Decide_Workshop(Choice choice)
		{
			return new ChoiceResult(FindBestCardForCost(choice.Supplies.Values, null, false));
		}
	}
}
