using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using DominionBase.Cards;
using DominionBase.Piles;

namespace DominionBase.Players.AI
{
	public class RixAttack : Standard
	{
		public new static String AIName { get { return "Interaction"; } }
		public new static String AIDescription { get { return "Likes to buy Attack cards early and middle game."; } }

		public RixAttack(Game game, String name) : base(game, name) { }
		public RixAttack(Game game, String name, Player realThis) : base(game, name, realThis) { }

		public override float GameProgress
		{
			get
			{
				return base.GameProgressNew;
			}
		}

		protected override bool ShouldBuy(Type type)
		{
			// Never buy Potions
			if (type == Cards.Alchemy.TypeClass.Potion)
				return false;

			// Special Action cards without +Cards that are allowed
			if (type == Cards.Base.TypeClass.Adventurer || type == Cards.Intrigue.TypeClass.Nobles ||
				type == Cards.Intrigue.TypeClass.SecretChamber || type == Cards.Prosperity.TypeClass.CountingHouse)
				return true;

			// Allow all Treasure & Victory cards
			Card card = Card.CreateInstance(type);
			if ((card.Category & Category.Treasure) == Category.Treasure || (card.Category & Category.Victory) == Category.Victory)
				return true;

			// Also allow cards that provide +Cards
			if (card.Benefit.Cards > 0)
				return true;

			return base.ShouldBuy(type);
		}

		//internal int CountAll(Player fromPlayer, Predicate<Card> predicate, Boolean onlyObtainable, Boolean onlyCurrentlyDrawable)	
		float GetDeckTreasureBuySum()
		{
			Predicate<Card> predicate = (c => (c.Category & Category.Treasure) == Category.Treasure);
			Func<Card, int> selector = (c => c.Benefit.Currency.Coin.Value);
						
			return (float)this.RealThis.SumAll(this.RealThis, predicate, selector);
		}

		protected override Supply FindBestCardToBuy(List<Supply> buyableSupplies)
		{

			float fGameProgress = GameProgress;
			float fAll = (float)this.RealThis.CountAll();
			float fActionCards = (float)this.RealThis.CountAll(this.RealThis, c => (c.Category & Category.Action) == Category.Action) / fAll;
			float fCurseCards = (float)this.RealThis.CountAll(this.RealThis, c => (c.Category & Category.Curse) == Category.Curse) / fAll;
			float fTreasureCards = (float)this.RealThis.CountAll(this.RealThis, c => (c.Category & Category.Treasure) == Category.Treasure) / fAll;
			float fVictoryCards = (float)this.RealThis.CountAll(this.RealThis, c => (c.Category & Category.Victory) == Category.Victory) / fAll;
		   
			float fBenifitCardCards = this.RealThis.CountAll(this.RealThis, c => c.Benefit.Cards > 0) / fAll;
			float fBenifitActionCards = this.RealThis.CountAll(this.RealThis, c => c.Benefit.Actions > 0) / fAll;

			float fTreasureIndex = GetDeckTreasureBuySum() / fAll; 
			
			//(float)this(this, c => (c.Category & Category.Treasure) == Category.Treasure) / fAll;
			

			float fStealTreasureCards = (float)this.RealThis.CountAll(this.RealThis, c => ((c.CardType == Cards.Base.TypeClass.Thief) || (c.CardType == Cards.Seaside.TypeClass.PirateShip) || (c.CardType == Cards.Hinterlands.TypeClass.NobleBrigand))) / fAll;

			bool bMoatInPlay = this.RealThis._Game.Table.Supplies.Any(kvp => ((kvp.Key == Cards.Base.TypeClass.Moat) || (kvp.Key == Cards.Seaside.TypeClass.Lighthouse)));
		  //  System.Diagnostics.Debug.WriteLine("Moat:"+bMoatInPlay.ToString());
			System.Diagnostics.Debug.WriteLine(String.Format("all:{0}, Game:{6:0.00} A{1:0.00}, T{2:0.00}:{7:0.00}, V{3:0.00}, BC{4:0.00}, BA{5:0.00}", fAll, fActionCards, fTreasureCards, fVictoryCards, fBenifitCardCards, fBenifitActionCards, fGameProgress, fTreasureIndex));
		
			Dictionary<double, List<Supply>> scores = new Dictionary<double, List<Supply>>();
			foreach (Supply supply in buyableSupplies)
			{

				// We need to compute score based on the original/base cost of the card
				double fBaseCost = supply.BaseCost.Coin.Value + 2.5f * supply.BaseCost.Potion.Value;
				double score = fBaseCost;

				// Scale based on the original -vs- current cost -- cheaper cards should be more valuable to us!
			 //   score += Math.Log((score + 1) / (supply.CurrentCost.Coin.Value + 2.5f * supply.CurrentCost.Potion.Value + 1));
				
				if (!ShouldBuy(supply))
					score = -1d;
				
				// buy attack cards early in the game.
				if ((((supply.Category & Category.Attack) == Category.Attack) || (supply.CardType == Cards.Seaside.TypeClass.Embargo)) && (fGameProgress > 0.2))
				{
					if (
						// don;t want list.
						 (supply.CardType == Cards.Seaside.TypeClass.Ambassador) || // only good with curses
						 (supply.CardType == Cards.Cornucopia.TypeClass.Jester) || // nice card, but difficult to play.
						 (supply.CardType == Cards.Prosperity.TypeClass.Mountebank) ||                         
						 (supply.CardType == Cards.Seaside.TypeClass.Cutpurse) ||
						 (supply.CardType == Cards.Cornucopia.TypeClass.FortuneTeller) 
						)
						score = -1d;

					if (fActionCards < 0.3) // 3 in 10 action cards is enough.
					{
						if (
							  //3
							 ((supply.CardType == Cards.Base.TypeClass.Bureaucrat) && (fGameProgress > 0.8)) || // 1 or 2 of these early game is enough.
							  //3P
							 (supply.CardType == Cards.Alchemy.TypeClass.Familiar) ||
							 //2 not an attack card but nice all the same.
							 (supply.CardType == Cards.Seaside.TypeClass.Embargo) ||
							 //5
							 (supply.CardType == Cards.Seaside.TypeClass.GhostShip) ||
							 //6
							 (supply.CardType == Cards.Prosperity.TypeClass.Goons) ||
							 //5
							 (supply.CardType == Cards.Hinterlands.TypeClass.Margrave) ||
							 //4
							 (supply.CardType == Cards.Base.TypeClass.Militia) ||
							 //5
							 (supply.CardType == Cards.Intrigue.TypeClass.Minion) || // can the choice be done?
							 //3
							 (supply.CardType == Cards.Hinterlands.TypeClass.Oracle) ||  // can the choice be done?
							 //4
							 (supply.CardType == Cards.Seaside.TypeClass.PirateShip) || // can the choice be done?
							 //5
							 (supply.CardType == Cards.Prosperity.TypeClass.Rabble) ||
							 //5
							 (supply.CardType == Cards.Intrigue.TypeClass.Saboteur) ||
							 //2P
							 (supply.CardType == Cards.Alchemy.TypeClass.ScryingPool) || // 1 of these is great.
							 //4
							 (supply.CardType == Cards.Seaside.TypeClass.SeaHag) ||
							 //5
							 (supply.CardType == Cards.Intrigue.TypeClass.Torturer) ||
							 //5
							 (supply.CardType == Cards.Base.TypeClass.Witch) ||
							 //4
							 (supply.CardType == Cards.Cornucopia.TypeClass.YoungWitch)
							)
						{
							double fActionMult = (double)((10.0f * fBaseCost * fGameProgress * fGameProgress) / (fActionCards + 0.5f));
							//global::System.Windows.Forms.MessageBox.Show(fActionMult.ToString());
							score *= fActionMult;
							if (score < 0) // there maybe some unrecommened cards from ShouldBuy() 
								score = -score;

						}

						// better in mid-game.
						if (
							//4
							 (supply.CardType == Cards.Hinterlands.TypeClass.NobleBrigand) ||  // can the choice be done?
							//4
							 (supply.CardType == Cards.Base.TypeClass.Thief)
							)
						{
							double fActionMult = (double)((8.0f * fBaseCost * fGameProgress) / (fActionCards + 0.5f));
							//global::System.Windows.Forms.MessageBox.Show(fActionMult.ToString());
							score *= fActionMult;
							if (score < 0) // there maybe some unrecommened cards from ShouldBuy() 
								score = -score;
						}

						// less attractive attack cards.
						
						//4 - spy already gets a bonus from +cards, it overwhelms other 4 priced attacks.
						//3 - swindler not too many as low cost
						if ((supply.CardType == Cards.Base.TypeClass.Spy) || (supply.CardType == Cards.Intrigue.TypeClass.Swindler))
						{
							double fActionMult = (double)((2.0f * fBaseCost * fGameProgress * fGameProgress) / (fActionCards + 0.5f));
							//global::System.Windows.Forms.MessageBox.Show(fActionMult.ToString());
							score *= fActionMult;
							if (score < 0) // there maybe some unrecommened cards from ShouldBuy() 
								score = -score;
						}                      
					}
				}

			  

				// Scale back the score accordingly if it's near the end of the game and the card is not a Victory card
				if (fGameProgress < 0.25 && (supply.Category & Category.Victory) != Category.Victory)
					score *= 0.1d;

				// Never buy non-Province/Colony/Victory-only cards early
				if (fGameProgress > 0.81 && supply.Category == Category.Victory &&
					supply.CardType != Cards.Universal.TypeClass.Province &&
					supply.CardType != Cards.Prosperity.TypeClass.Colony &&
					supply.CardType != Cards.Hinterlands.TypeClass.Farmland &&
					supply.CardType != Cards.Hinterlands.TypeClass.Tunnel)
					score = -1d;

				if ((fGameProgress > 0.15 && supply.CardType == Cards.Universal.TypeClass.Estate) ||
					(fGameProgress > 0.30 && supply.CardType == Cards.Alchemy.TypeClass.Vineyard) ||
					(fGameProgress > 0.40 && supply.CardType == Cards.Base.TypeClass.Gardens) ||
					(fGameProgress > 0.40 && supply.CardType == Cards.Hinterlands.TypeClass.SilkRoad) ||
					(fGameProgress > 0.45 && supply.CardType == Cards.Universal.TypeClass.Duchy) ||
					(fGameProgress > 0.45 && supply.CardType == Cards.Intrigue.TypeClass.Duke) ||
					(fGameProgress > 0.45 && supply.CardType == Cards.Cornucopia.TypeClass.Fairgrounds))
					score *= 0.1d;

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

				if (supply.CardType == Cards.Universal.TypeClass.Copper)
				{
					// Never buy Copper cards unless we have a Goons in play
					if (this.RealThis.InPlay[Cards.Prosperity.TypeClass.Goons].Count == 0)
						score = -1d;
					//else if (this.CurrentTurn.CardsBought.Count(c => c.CardType == Cards.Universal.TypeClass.Copper) > 1)
					//    score = -1d;
				}

				//can get the money using steal cards.
				if ((fStealTreasureCards > 0.05) && (!bMoatInPlay))
				{
					if ((supply.CardType == Cards.Universal.TypeClass.Silver) || (supply.CardType == Cards.Universal.TypeClass.Gold))
					{
						System.Diagnostics.Debug.WriteLine(String.Format("Lowering Gold & Silver due to steal, fStealTreasureCards:{0:0.00} : {1:0.00}", fStealTreasureCards, (fTreasureIndex + 1.0f)));
						score /= (fTreasureIndex + 1.0f);
					}
				}


				// Witch gets less & less valuable with fewer & fewer curses, down to about 1.9 when there are none
				// We should actually make it slightly more likely to get Witch when there are lots of curses
				if (supply.CardType == Cards.Base.TypeClass.Witch)
				{
					score *= Math.Pow(0.8, (9.8d - (1d + Utilities.Gaussian.NextGaussian(this._Game.RNG) / 12d) * Math.Sqrt(10) * Math.Sqrt(((double)this.RealThis._Game.Table.Curse.Count) / (this.RealThis._Game.Players.Count - 1))) * 4.3335 / 10);
				}

				// Horn of Plenty -- not very useful in most set-ups -- Worth is normally about 1/2 for this AI
				if (supply.CardType == Cards.Cornucopia.TypeClass.HornOfPlenty)
				{
					score *= 0.4f;
				}

				// Silver -- this can sometimes flood a deck -- make sure we don't have too many
				if (supply.CardType == Cards.Universal.TypeClass.Silver)
				{
					int numSilvers = this.RealThis.CountAll(this.RealThis, c => c.CardType == Cards.Universal.TypeClass.Silver, true, false);
					int numBetterThanSilver = this.RealThis.CountAll(this.RealThis,
						c => c.CardType == Cards.Universal.TypeClass.Gold ||
							c.CardType == Cards.Prosperity.TypeClass.Platinum ||
							c.CardType == Cards.Prosperity.TypeClass.Venture ||
							c.CardType == Cards.Prosperity.TypeClass.Hoard ||
							c.CardType == Cards.Prosperity.TypeClass.Bank ||
							c.CardType == Cards.Prosperity.TypeClass.RoyalSeal ||
							c.CardType == Cards.Cornucopia.TypeClass.Diadem,
						true, false);
					int numCards = this.RealThis.CountAll();
					if ((double)numBetterThanSilver / numCards > 0.05d && (double)numSilvers / numCards > 0.4d)
						score *= 0.1;
				}

				// Loan -- we don't want too many; at most, we should have 2
				if (supply.CardType == Cards.Prosperity.TypeClass.Loan)
				{
					int numLoans = this.RealThis.CountAll(this.RealThis, c => c.CardType == Cards.Prosperity.TypeClass.Loan, true, false);
					int numCountingHouses = this.RealThis.CountAll(this.RealThis, c => c.CardType == Cards.Prosperity.TypeClass.CountingHouse, true, false);
					int numCoppers = this.RealThis.CountAll(this.RealThis, c => c.CardType == Cards.Universal.TypeClass.Copper, true, false);
					if (numLoans >= 2 || numCountingHouses > 0)
						score *= 0.1;
					score *= Math.Pow(1.05, (numCoppers > 9 ? 9 : numCoppers) - 7);
				}

				// Limit the number of Contrabands we'll buy to a fairly small amount (1 per every 20 cards or so)
				if (supply.CardType == Cards.Prosperity.TypeClass.Contraband)
				{
					int contrabandsICanPlay = this.RealThis.CountAll(this.RealThis, c => c.CardType == Cards.Prosperity.TypeClass.Contraband, true, false);
					int totalDeckSize = this.RealThis.CountAll();
					double percentageOfContrabands = ((double)contrabandsICanPlay) / totalDeckSize;
					if (percentageOfContrabands > 0.5)
						score *= Math.Pow(0.2, Math.Pow(percentageOfContrabands, 2));
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

				// The more +Cards an Action card gives, the more worthwhile it is.
				// Almost all Action cards are worthless to this AI's strategy, so most things scale to near 0
				// An absolute baseline is +2 Cards.  Anything less than that isn't getting us anywhere, even if it has other bonuses
				if ((supply.Category & Category.Action) == Category.Action)
				{
					Card supplyCard = Cards.Card.CreateInstance(supply.CardType);
					if (supplyCard.Benefit.Cards < 1)
					{
						// Special case for Adventurer -- it's not all that bad with Big Money
						if (supply.CardType == Cards.Base.TypeClass.Adventurer)
							score *= 0.8d;
						// Special case for Nobles -- it's got +3 Cards as an option
						else if (supply.CardType == Cards.Intrigue.TypeClass.Nobles)
							score *= Math.Pow(1.075, supplyCard.Benefit.Cards);
						// Special case for Secret Chamber -- it's good when there's a bunch of crap in your deck (Curse, Action, Victory cards)
						else if (supply.CardType == Cards.Intrigue.TypeClass.SecretChamber)
						{
							int allNonTreasureCards = this.RealThis.CountAll(this.RealThis, c => (c.Category & Category.Treasure) != Category.Treasure, true, false);
							int allTreasureCards = this.RealThis.CountAll(this.RealThis, c => (c.Category & Category.Treasure) == Category.Treasure, true, false);
							if ((double)allNonTreasureCards / allTreasureCards > 0.5d)
								score *= 1.1d;
						}
						// We like Counting House with lots of Coppers!
						else if (supply.CardType == Cards.Prosperity.TypeClass.CountingHouse)
						{
							score *= Math.Pow(1.05, 7 - this.RealThis.CountAll(this.RealThis, c => c.CardType == Cards.Universal.TypeClass.Copper, true, false));
						}
						// Special case for Governor -- it's got +1(+3) Cards as an option
						else if (supply.CardType == Cards.Promotional.TypeClass.Governor)
							score *= Math.Pow(1.075, supplyCard.Benefit.Cards);
						else
							score *= 0.1d;
					}
					else if (supplyCard.Benefit.Cards < 2)
					{
						if (supplyCard.DurationBenefit.Cards > 0)
							score *= 0.5d * Math.Pow(1.075, supplyCard.DurationBenefit.Cards);
						// Special case for Oasis -- It's not *ACTUALLY* +1 Card
						else if (supply.CardType == Cards.Hinterlands.TypeClass.Oasis)
							score *= 0.2d;
						else
							score *= 0.3d;
					}
					else
					{
						// Special case for Courtyard -- it's really only +2 Cards
						if (supply.CardType == Cards.Intrigue.TypeClass.Courtyard)
							score *= Math.Pow(1.075, supplyCard.Benefit.Cards - 1);
						// Special case for Warehouse -- It's not *ACTUALLY* +3 Cards
						else if (supply.CardType == Cards.Seaside.TypeClass.Warehouse)
							score *= 0.25d;
						// Special case for Envoy -- it's really only +3 Cards
						else if (supply.CardType == Cards.Promotional.TypeClass.Envoy)
							score *= Math.Pow(1.075, supplyCard.Benefit.Cards - 1);
						// Special case for YoungWitch -- It's not *ACTUALLY* +2 Cards
						else if (supply.CardType == Cards.Cornucopia.TypeClass.YoungWitch)
							score *= 0.25d;
						// Special case for Inn -- It's not *ACTUALLY* +2 Cards
						else if (supply.CardType == Cards.Hinterlands.TypeClass.Inn)
							score *= 0.25d;
						// Special case for Embassy -- it's really only +2 Cards
						else if (supply.CardType == Cards.Hinterlands.TypeClass.Embassy)
							score *= Math.Pow(1.075, supplyCard.Benefit.Cards - 2.5); 
						else
							//fBenifitActionCards, fActionCards
							score *= Math.Pow(1.075 + fActionCards - fBenifitActionCards, supplyCard.Benefit.Cards);   // change slightly to make more actions when we need them.
							//score *= Math.Pow(1.075, supplyCard.Benefit.Cards); 
					}
					if (supplyCard.Benefit.Buys > 0)
						score *= 1.025;
					score *= 0.995;

					int allActionCards = this.RealThis.CountAll(this.RealThis, c => (c.Category & Category.Action) == Category.Action, true, false);
					int allCards = this.RealThis.CountAll();

					if ((double)allActionCards / allCards > 0.3d)
						score *= 0.4;

					// Final adjustment for Attack cards
					if ((supply.Category & Category.Attack) == Category.Attack)
						score *= 1.12d;
				}

				if (!scores.ContainsKey(score))
					scores[score] = new List<Supply>();
				scores[score].Add(supply);
			}

			double bestScore = scores.Keys.OrderByDescending(k => k).First();

			IOrderedEnumerable<double> OrderedScores = scores.Keys.OrderByDescending(k => k);

			foreach (double s in OrderedScores)
			{
				for (int i = 0 ; i < scores[s].Count; i++)
					System.Diagnostics.Debug.WriteLine(String.Format("score:{1:0.00}, card:{0}", scores[s][i].CardType.Name, s));                            
			}

			if (bestScore >= 0d)
				return scores[bestScore][this._Game.RNG.Next(scores[bestScore].Count)];
			return null;
		}
	}
}
