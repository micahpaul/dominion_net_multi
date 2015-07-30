using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime;
using System.Runtime.Serialization;
using System.Text;
using System.Xml;
using System.Xml.Serialization;

namespace Dominion.NET_WPF
{
	[DataContract]
	public class Statistics
	{
		private Dictionary<int, NPlayersStatistics> _GlobalStatistics = new Dictionary<int, NPlayersStatistics>();

		public Statistics()
		{

		}

		[DataMember]
		public Dictionary<int, NPlayersStatistics> GlobalStatistics { get { return _GlobalStatistics; } internal set { _GlobalStatistics = value; } }

		public void Add(DominionBase.Game game)
		{
			Add(game, null);
		}

		public void Add(DominionBase.Game game, DominionBase.Players.Player player)
		{
			// Make sure it exists
			if (!_GlobalStatistics.ContainsKey(game.Players.Count))
				_GlobalStatistics[game.Players.Count] = new NPlayersStatistics(game.Players.Count);

			// Add the game to the correct Statistics section
			_GlobalStatistics[game.Players.Count].Add(game, player);
		}

		private static String Filename
		{
			get
			{
				return System.IO.Path.Combine(DominionBase.Utilities.Application.ApplicationPath, "stats.xml");
			}
		}

		public void Save()
		{
			try
			{
				// TODO : This is not to be used for this release -- still some pondering to do.
				return;

				DataContractSerializer serializer = new DataContractSerializer(typeof(Statistics), new Type[] { });
				using (StreamWriter swStatistics = new StreamWriter(Statistics.Filename))
				{
					using (XmlTextWriter writer = new XmlTextWriter(swStatistics))
					{
						writer.Formatting = Formatting.Indented;
						serializer.WriteObject(writer, this);
						writer.Flush();
					}
				}

				//XmlSerializer xsStatistics = new XmlSerializer(typeof(Statistics), new Type[] { });
				//StreamWriter swStatistics = new StreamWriter(Statistics.Filename);
				//xsStatistics.Serialize(swStatistics, this);
				//swStatistics.Close();
			}
			catch (IOException) { }
		}

		public static Statistics Load()
		{
			Statistics statistics = null;
			try
			{
				// TODO : This is not to be used for this release -- still some pondering to do.
				return new Statistics();

				DataContractSerializer serializer = new DataContractSerializer(typeof(Statistics), new Type[] { });
				using (FileStream fs = new FileStream(Statistics.Filename, FileMode.Open))
				{
					using (XmlDictionaryReader reader = XmlDictionaryReader.CreateTextReader(fs, new XmlDictionaryReaderQuotas()))
					{
						while (reader.Read())
						{
							switch (reader.NodeType)
							{
								case XmlNodeType.Element:
									if (serializer.IsStartObject(reader))
									{
										statistics = (Statistics)serializer.ReadObject(reader);
									}
									break;
							}
						}

					}
				}
				//using (StreamWriter swStatistics = new StreamWriter(Statistics.Filename))
				//{
				//    using (XmlTextWriter writer = new XmlTextWriter(swStatistics))
				//    {
				//        writer.Formatting = Formatting.Indented;
				//        serializer.WriteObject(writer, this);
				//        writer.Flush();
				//    }
				//}


				//XmlSerializer mySerializer = new XmlSerializer(typeof(Statistics), new Type[] { });
				//FileStream myFileStream = new FileStream(Statistics.Filename, FileMode.Open);

				//statistics = (Statistics)mySerializer.Deserialize(myFileStream);
			}
			catch
			{
				statistics = new Statistics();
			}

			return statistics;
		}

	}

	[DataContract]
	public class NPlayersStatistics
	{
		private int _NumberOfPlayers = 1;
		private int _GamesPlayed = 0;
		private int _GamesWon = 0;
		private int _WinStreak = 0;
		private int _LossStreak = 0;
		private int _LongestWinStreak = 0;
		private int _LongestLossStreak = 0;
		private int _LowestScore = int.MaxValue;
		private int _HighestScore = int.MinValue;
		private long _TotalScore = 0;
		private Dictionary<String, CardStatistics> _WinningCardCounts = new Dictionary<String, CardStatistics>();
		private Dictionary<String, CardStatistics> _WinningHumanCardCounts = new Dictionary<String, CardStatistics>();

		public NPlayersStatistics()
		{
		}
		public NPlayersStatistics(int numberOfPlayers)
		{
			_NumberOfPlayers = numberOfPlayers;
		}

		[DataMember]
		public int NumberOfPlayers { get { return _NumberOfPlayers; } internal set { _NumberOfPlayers = value; } }
		[DataMember]
		public int GamesPlayed { get { return _GamesPlayed; } internal set { _GamesPlayed = value; } }
		[DataMember]
		public int GamesWon { get { return _GamesWon; } internal set { _GamesWon = value; } }
		[DataMember]
		public int WinStreak { get { return _WinStreak; } internal set { _WinStreak = value; } }
		[DataMember]
		public int LossStreak { get { return _LossStreak; } internal set { _LossStreak = value; } }
		[DataMember]
		public int LongestWinStreak { get { return _LongestWinStreak; } internal set { _LongestWinStreak = value; } }
		[DataMember]
		public int LongestLossStreak { get { return _LongestLossStreak; } internal set { _LongestLossStreak = value; } }
		[DataMember]
		public int LowestScore { get { return _LowestScore; } internal set { _LowestScore = value; } }
		[DataMember]
		public int HighestScore { get { return _HighestScore; } internal set { _HighestScore = value; } }
		[DataMember]
		private long TotalScore { get { return _TotalScore; } set { _TotalScore = value; } }
		public double AverageScore
		{
			get
			{
				if (_GamesPlayed == 0)
					return 0d;
				return (double)_TotalScore / _GamesPlayed;
			}
		}

		[DataMember]
		public Dictionary<String, CardStatistics> WinningCardCounts { get { return _WinningCardCounts; } set { _WinningCardCounts = value; } }
		[DataMember]
		public Dictionary<String, CardStatistics> WinningHumanCardCounts { get { return _WinningHumanCardCounts; } set { _WinningHumanCardCounts = value; } }

		public void Add(DominionBase.Game game, DominionBase.Players.Player player)
		{
			if (game.Players.Count != _NumberOfPlayers)
				throw new Exception("Incorrect number of players in game!");

			if (player != null)
			{
				if (!game.Players.Contains(player))
					throw new Exception("Player not found in game!");

				// One more game played!
				this.GamesPlayed++;

				// If the game actually finished properly, instead of being aborted
				if (game.State == DominionBase.GameState.Ended)
				{
					// Player won (or at least tied in winning)
					if (game.Winners.Contains(player))
						this.GamesWon++;

					if (player.VictoryPoints < this.LowestScore)
						this.LowestScore = player.VictoryPoints;
					if (player.VictoryPoints > this.HighestScore)
						this.HighestScore = player.VictoryPoints;

					_TotalScore += player.VictoryPoints;
				}
			}

			if (game.State == DominionBase.GameState.Ended)
			{
				foreach (DominionBase.Players.Player winner in game.Winners)
				{

					// Since properly-used cards like Feast, Mining Village, & Embargo never end up in a player's hand, 
					// we need to count how many of the cards were played during the game and add them to the player's
					// total as well as the total number of cards in the player's hand
					IEnumerable<DominionBase.Cards.Card> trashedCards = game.TurnsTaken.Where(t => t.Player == winner).
						SelectMany<DominionBase.Turn, DominionBase.Cards.Card>(
						t => 
							t.CardsPlayed.Where(c => 
								c.CardType == DominionBase.Cards.Base.TypeClass.Feast || 
								c.CardType == DominionBase.Cards.Seaside.TypeClass.Embargo ||
								(c.CardType == DominionBase.Cards.Intrigue.TypeClass.MiningVillage && t.CardsTrashed.Contains(c)) ||
								(c.CardType == DominionBase.Cards.Cornucopia.TypeClass.HornOfPlenty && t.CardsTrashed.Contains(c))
								)
							);

					int handCount = winner.Hand.Count + trashedCards.Count();

					foreach (DominionBase.Piles.Supply supply in game.Table.Supplies.Values)
					{
						Type cardType = supply.CardType;
						String cardTypeKey = cardType.AssemblyQualifiedName;

						int cardTypeCount = winner.Hand.Count(c => c.CardType == cardType) + trashedCards.Count(c => c.CardType == cardType);

						if (!_WinningCardCounts.ContainsKey(cardTypeKey))
							_WinningCardCounts[cardTypeKey] = new CardStatistics(cardType);
						_WinningCardCounts[cardTypeKey].Add(cardTypeCount, handCount);

						if (winner.PlayerType == DominionBase.Players.PlayerType.Human)
						{
							if (!_WinningHumanCardCounts.ContainsKey(cardTypeKey))
								_WinningHumanCardCounts[cardTypeKey] = new CardStatistics(cardType);
							_WinningHumanCardCounts[cardTypeKey].Add(cardTypeCount, handCount);
						}
					}
				}
			}
		}
	}
	[DataContract]
	public class CardStatistics
	{
		private Type _CardType = null;
		private int _Count = 0;
		private int _OutOfCount = 0;
		private int _GameCount = 0;

		public CardStatistics()
		{
		}

		public CardStatistics(Type cardType)
		{
			this.CardType = cardType;
		}

		public CardStatistics(Type cardType, int count, int outOfCount)
			: this(cardType)
		{
			this.Add(count, outOfCount);
		}

		[IgnoreDataMember]
		public Type CardType { get { return _CardType; } internal set { _CardType = value; } }
		[DataMember]
		public String AssemblyQualifiedName
		{
			get
			{
				if (this.CardType == null)
					return String.Empty;
				return this.CardType.AssemblyQualifiedName;
			}
			internal set
			{
				this.CardType = Type.GetType(value);
			}
		}
		[DataMember]
		public int Count { get { return _Count; } internal set { _Count = value; } }
		[DataMember]
		public int OutOfCount { get { return _OutOfCount; } internal set { _OutOfCount = value; } }
		[DataMember]
		public int GameCount { get { return _GameCount; } internal set { _GameCount = value; } }
		public double CardFrequency { get { return (double)this.Count / this.OutOfCount; } }

		public void Add(int count, int outOfCount)
		{
			_Count += count;
			_OutOfCount += outOfCount;
			_GameCount++;
		}

		public override string ToString()
		{
			return String.Format("Card: {0} :: {1}/{2} ({3:0.0%}) in {4} games", this.CardType.Name, this.Count, this.OutOfCount, this.CardFrequency, this.GameCount);
		}
	}
}
