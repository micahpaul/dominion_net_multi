using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading;
using System.Xml;
using System.Xml.Serialization;

using DominionBase.Piles;
using DominionBase.Players;

namespace DominionBase
{
	public abstract class GameMessage
	{
		public virtual Boolean CheckEndGame { get { return false; } }
		public String Message { get; set; }
		public WaitCallback WaitCallback { get; set; }

		public GameMessage() { }
		public GameMessage(String message) : this(null, message) { }
		public GameMessage(WaitCallback waitCallback) { WaitCallback = waitCallback; }
		public GameMessage(WaitCallback waitCallback, String message) : this(waitCallback) { Message = message; }

		public virtual void ActBefore(Game game) { }
		public virtual Thread ActAfter(Game game) { return null; }
	}
	public class GameResponseMessage : GameMessage { }
	public class GameBuyMessage : GameMessage
	{
		public Player Player { get; set; }
		public Supply Supply { get; set; }
		public GameBuyMessage() { }
		public GameBuyMessage(Player player, Supply supply)
			: this(null, player, supply)
		{
		}
		public GameBuyMessage(WaitCallback waitCallback, Player player, Supply supply)
			: base(waitCallback)
		{
			Player = player;
			Supply = supply;
		}
		public override void ActBefore(Game game)
		{
			Player.Buy(this.Supply);
		}
	}
	public class GamePlayMessage : GameMessage
	{
		public Player Player { get; set; }
		public Cards.Card Card { get; set; }
		public GamePlayMessage() { }
		public GamePlayMessage(Player player, Cards.Card card)
			: this(null, player, card)
		{
		}
		public GamePlayMessage(WaitCallback waitCallback, Player player, Cards.Card card)
			: base(waitCallback)
		{
			Player = player;
			Card = card;
		}
		public override void ActBefore(Game game)
		{
			Player.PlayCard(Card);
		}
	}
	public class GameUndoPlayMessage : GameMessage
	{
		public Player Player { get; set; }
		public Cards.Card Card { get; set; }
		public PhaseEnum Phase { get; set; }
		public GameUndoPlayMessage() { }
		public GameUndoPlayMessage(Player player, Cards.Card card)
			: this(null, player, card)
		{
		}
		public GameUndoPlayMessage(Player player, PhaseEnum phase)
			: this(null, player, phase)
		{
			this.Player = player;
			this.Phase = phase;
		}
		public GameUndoPlayMessage(WaitCallback waitCallback, Player player, Cards.Card card)
			: base(waitCallback)
		{
			this.Player = player;
			this.Card = card;
		}
		public GameUndoPlayMessage(WaitCallback waitCallback, Player player, PhaseEnum phase)
			: base(waitCallback)
		{
			this.Player = player;
			this.Phase = phase;
		}
		public override void ActBefore(Game game)
		{
			if (this.Card != null)
				this.Player.UndoPlayCard(this.Card);
			//if (this.Phase != PhaseEnum.Waiting)
			this.Player.UndoPhaseChange(this.Phase);
		}
	}
	public class GameEndTurnMessage : GameMessage
	{
		public override Boolean CheckEndGame { get { return true; } }
		public Player Player { get; set; }
		public GameEndTurnMessage() { }
		public GameEndTurnMessage(Player player)
			: this(null, player)
		{
		}
		public GameEndTurnMessage(WaitCallback waitCallback, Player player)
			: base(waitCallback)
		{
			Player = player;
		}
		public override void ActBefore(Game game)
		{
			//if (Player.Phase != PhaseEnum.Waiting)
			Player.Cleanup();
			//game.SetNextPlayer();
		}
		public override Thread ActAfter(Game game)
		{
			return game.SetNextPlayer();
		}
	}
	public class GamePlayTreasuresMessage : GameMessage
	{
		public Player Player { get; set; }
		public GamePlayTreasuresMessage() { }
		public GamePlayTreasuresMessage(Player player)
			: this(null, player)
		{
		}
		public GamePlayTreasuresMessage(WaitCallback waitCallback, Player player)
			: base(waitCallback)
		{
			Player = player;
		}
		public override void ActBefore(Game game)
		{
			Player.PlayTreasures(game);
		}
	}
	public class GameGoToBuyPhaseMessage : GameMessage
	{
		public Player Player { get; set; }
		public GameGoToBuyPhaseMessage() { }
		public GameGoToBuyPhaseMessage(Player player)
			: this(null, player)
		{
		}
		public GameGoToBuyPhaseMessage(WaitCallback waitCallback, Player player)
			: base(waitCallback)
		{
			Player = player;
		}
		public override void ActBefore(Game game)
		{
			Player.GoToBuyPhase();
		}
	}
	public class GamePlayTokensMessage : GameMessage
	{
		public Player Player { get; set; }
		public Type Token { get; set; }
		public int Count { get; set; }
		public GamePlayTokensMessage() { }
		public GamePlayTokensMessage(Player player, Type token, int count)
			: this(null, player, token, count)
		{
		}
		public GamePlayTokensMessage(WaitCallback waitCallback, Player player, Type token, int count)
			: base(waitCallback)
		{
			this.Player = player;
			this.Token = token;
			this.Count = count;
		}
		public override void ActBefore(Game game)
		{
			Player.PlayTokens(game, this.Token, this.Count);
		}
	}
	public class GameEndMessage : GameMessage
	{
		public Player Player { get; set; }
		public GameEndMessage() { }
		public GameEndMessage(Player player)
			: this(null, player)
		{
		}
		public GameEndMessage(WaitCallback waitCallback, Player player)
			: base(waitCallback)
		{
			Player = player;
		}
		public override void ActBefore(Game game)
		{
			game.Abort();
		}
	}

	public class GameEndedEventArgs : EventArgs
	{
		public GameEndedEventArgs()
		{
		}
	}

	public class GameMessageEventArgs : EventArgs
	{
		public Player Player;
		public Player AffectedPlayer = null;
		public Cards.Card SourceCard;
		public ICard Card1 = null;
		public ICard Card2 = null;
		public int Count = 1;
		public Currency Currency = null;
		public GameMessageEventArgs(Player player, Cards.Card sourceCard)
		{
			this.Player = player;
			this.SourceCard = sourceCard;
		}
		public GameMessageEventArgs(Player player, Cards.Card sourceCard, int count)
			: this(player, sourceCard)
		{
			this.Count = count;
		}
		public GameMessageEventArgs(Player player, Cards.Card sourceCard, ICard card)
			: this(player, sourceCard)
		{
			this.Card1 = card;
		}
		public GameMessageEventArgs(Player player, Cards.Card sourceCard, ICard card1, ICard card2)
			: this(player, sourceCard)
		{
			this.Card1 = card1;
			this.Card2 = card2;
		}
		public GameMessageEventArgs(Player player, Cards.Card sourceCard, ICard card, int count)
			: this(player, sourceCard, card)
		{
			this.Count = count;
		}
		public GameMessageEventArgs(Player player, Player playerAffected, Cards.Card sourceCard, ICard card)
			: this(player, sourceCard, card)
		{
			this.AffectedPlayer = playerAffected;
		}
		public GameMessageEventArgs(Player player, Cards.Card sourceCard, Currency currency)
			: this(player, sourceCard)
		{
			this.Currency = currency;
		}
	}

	public class CostComputeEventArgs : EventArgs
	{
		public ICard Card;
		public Cards.Cost Cost;
		public CostComputeEventArgs(ICard card, Cards.Cost cost)
		{
			if (card == null || cost == (Cards.Cost)null)
				return;

			this.Card = card;
			this.Cost = cost.Clone();
		}
	}

	public class GameCreationException : Exception
	{
		public GameCreationException() { }
		public GameCreationException(string message) : base(message) { }
		public GameCreationException(string message, Exception innerException) : base(message, innerException) { }
		internal GameCreationException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
	}

	public enum GameState
	{
		Unknown,
		Setup,
		CardSelection,
		CardSetup,
		NotStarted,
		Running,
		Ended,
		Aborted
	}

	public class Game : IDisposable
	{
		private Guid _Id = Guid.NewGuid();
		private GameState _GameState = GameState.Unknown;
		private DateTime _StartTime = DateTime.Now;

		private Random _RNG = new Random();

		public delegate void GameMessageEventHandler(object sender, GameMessageEventArgs e);
		public event GameMessageEventHandler GameMessage = null;
		public delegate void GameEndedEventHandler(object sender, GameEndedEventArgs e);
		public event GameEndedEventHandler GameEndedEvent = null;

		public delegate void CostComputeEventHandler(object sender, CostComputeEventArgs e);
		public event CostComputeEventHandler CostCompute = null;

		private Table _Table = null;
		private PlayerCollection _Players = new PlayerCollection();
		private Player _ActivePlayer = null;

		private TurnList _TurnsTaken = new TurnList();

		private int _EndgameSupplies = 3;

		private Queue<GameMessage> _MessageRequestQueue = new Queue<GameMessage>();
		private Queue<GameMessage> _MessageResponseQueue = new Queue<GameMessage>();
		private Boolean _ShouldStop = false;

		private GameSettings _Settings = new GameSettings();
		private List<Cards.Card> _CardsAvailable = new List<Cards.Card>();

		public DateTime StartTime { get { return _StartTime; } private set { _StartTime = value; } }
		public Random RNG { get { return _RNG; } private set { _RNG = value; } }

		public Game() { }

		public Game(int numHumanPlayers, IEnumerable<String> playerNames, IEnumerable<Type> aiTypes, GameSettings settings)
		{
			this.State = GameState.Setup;
			this.Settings = settings;

			_Players = new PlayerCollection();
			// Add human players
			for (int i = 0; i < numHumanPlayers; i++)
				_Players.Add(new Players.Human(this, playerNames.ElementAt(i)));
			// Add AI players
			for (int i = numHumanPlayers; i < playerNames.Count(); i++)
				try
				{
					_Players.Add((Player)aiTypes.ElementAt(i).GetConstructor(new Type[] { typeof(Game), typeof(String) }).Invoke(new Object[] { this, playerNames.ElementAt(i) }));
				}
				catch (TargetInvocationException tie)
				{
					if (tie.InnerException != null)
						throw tie.InnerException;
				}

			_Players.Setup(this);

			Utilities.Shuffler.Shuffle<Player>(_Players);

			if (playerNames.Count() > 4)
				_EndgameSupplies = 4;

			this.State = GameState.CardSelection;
		}

		public void SelectCards()
		{
			if (this.State != GameState.CardSelection)
				throw new GameCreationException("This method can only be called during CardSelection!");

			if (_Table != null)
				_Table.TearDown();

			this.CardsAvailable = Cards.CardCollection.GetAllCards(c => IsCardAllowed(c));
			Cards.CardCollection _CardsChosen = new Cards.CardCollection();

			if (this.Settings.Preset != null)
			{
				_CardsChosen = this.Settings.Preset.Cards;
			}
			else if (this.Settings.Constraints != null)
			{
				_CardsChosen.AddRange(this.Settings.Constraints.SelectCards(this.CardsAvailable, 10));
			}

			this.CardsAvailable.RemoveAll(card => _CardsChosen.Contains(card));
			_Table = new Table(this, this.Players.Count);
			_CardsChosen.ForEach(c => _Table.AddKingdomSupply(_Players, c.CardType));

			// should now have a list of cards that can be drawn from for things like Bane supplies

			_Table.SetupSupplies(this);
		}

		public void AcceptCards()
		{
			if (this.State != GameState.CardSelection)
				throw new GameCreationException("This method can only be called during CardSelection!");

			_Players.InitializeDecks();
			_Players.FinalizeDecks();

			if (this.Settings.IdenticalStartingHands)
				foreach (Player player in _Players.Skip(1))
					player.SetupDeckAs(_Players.ElementAt(0));

			this.State = GameState.CardSetup;
		}

		public void FinalizeSetup()
		{
			if (this.State != GameState.CardSetup)
				throw new GameCreationException("This method can only be called during CardSetup!");

			_Table.FinalizeSupplies(this);

			this.State = GameState.NotStarted;
		}

		public void Clear()
		{
			if (this.Players != null)
				this.Players.TearDown();
			if (this.Table != null)
				this.Table.TearDown();
			foreach (Cards.Card card in this.CardsAvailable)
				card.TearDown();

			Cards.CardCollection cardsAvailable = new Cards.CardCollection(this.CardsAvailable);
			List<Player> players = new List<Player>();
			if (this.Players != null)
				players.AddRange(this.Players);
			if (this.Table != null)
				this.Table.Clear();
			if (this.Players != null)
				this.Players.Clear();
			this.TurnsTaken.Clear();
			this.MessageRequestQueue.Clear();
			this.MessageResponseQueue.Clear();
			this.CardsAvailable.Clear();

#if DEBUG
			foreach (Cards.Card card in cardsAvailable)
				card.TestFireAllEvents();

			foreach (Player p in players)
				p.TestFireAllEvents();

			TestFireAllEvents();
#endif
		}

		public void TestFireAllEvents()
		{
			if (GameMessage != null)
				GameMessage(this, new GameMessageEventArgs(null, null));
			if (GameEndedEvent != null)
				GameEndedEvent(this, new GameEndedEventArgs());
			if (CostCompute != null)
				CostCompute(this, new CostComputeEventArgs(null, null));
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
					this._ActivePlayer = null;
					this._CardsAvailable = null;
					this._MessageRequestQueue = null;
					this._MessageResponseQueue = null;
					this._Players = null;
					this._RNG = null;
					this._Table = null;
					this._TurnsTaken = null;
				}

				// Call the appropriate methods to clean up
				// unmanaged resources here.
				// If disposing is false,
				// only the following code is executed.

				// Note disposing has been done.
				disposed = true;
			}
		}

		~Game()
		{
			Dispose(false);
		}
		#endregion

		public void SendMessage(Player player, Cards.Card sourceCard)
		{
			if (GameMessage != null)
			{
				GameMessageEventArgs gmea = new GameMessageEventArgs(player, sourceCard);
				GameMessage(this, gmea);
			}
		}

		public void SendMessage(Player player, Cards.Card sourceCard, int count)
		{
			if (GameMessage != null)
			{
				GameMessageEventArgs gmea = new GameMessageEventArgs(player, sourceCard, count);
				GameMessage(this, gmea);
			}
		}

		public void SendMessage(Player player, Cards.Card sourceCard, ICard card)
		{
			if (GameMessage != null)
			{
				GameMessageEventArgs gmea = new GameMessageEventArgs(player, sourceCard, card);
				GameMessage(this, gmea);
			}
		}

		public void SendMessage(Player player, Cards.Card sourceCard, ICard card1, ICard card2)
		{
			if (GameMessage != null)
			{
				GameMessageEventArgs gmea = new GameMessageEventArgs(player, sourceCard, card1, card2);
				GameMessage(this, gmea);
			}
		}

		public void SendMessage(Player player, Cards.Card sourceCard, ICard card, int count)
		{
			if (GameMessage != null)
			{
				GameMessageEventArgs gmea = new GameMessageEventArgs(player, sourceCard, card, count);
				GameMessage(this, gmea);
			}
		}

		public void SendMessage(Player player, Player playerAffected, Cards.Card sourceCard, ICard card)
		{
			if (GameMessage != null)
			{
				GameMessageEventArgs gmea = new GameMessageEventArgs(player, playerAffected, sourceCard, card);
				GameMessage(this, gmea);
			}
		}

		public void SendMessage(Player player, Cards.Card sourceCard, Currency currency)
		{
			if (GameMessage != null)
			{
				GameMessageEventArgs gmea = new GameMessageEventArgs(player, sourceCard, currency);
				GameMessage(this, gmea);
			}
		}

		/// <summary>
		/// Returns True if the card is allowed to be in the game
		/// </summary>
		/// <param name="c"></param>
		/// <returns></returns>
		private bool IsCardAllowed(Cards.Card card)
		{
			if (card.Location == Cards.Location.Kingdom)
				return true;

			return false;
		}

		internal Cards.Card GetNewCardPile(Func<Cards.Card, Boolean> predicate)
		{
			IEnumerable<Cards.Card> matchingCards = this.CardsAvailable.Where(predicate);
			if (matchingCards.Count() == 0)
				throw new GameCreationException("Cannot satisfy specified constraints!  Please double-check and make sure it's possible to construct a Kingdom card setup with the constraints specified");
			int index;
			// For some reason, Random.Next() actually sometimes returns the end point as an option -- Why?  That seems really wrong.
			do
				index = _RNG.Next(matchingCards.Count());
			while (index == matchingCards.Count());
			Cards.Card toReturn = matchingCards.ElementAt(index);
			this.CardsAvailable.Remove(toReturn);
			return toReturn;
		}

		public GameState State { get { return _GameState; } private set { _GameState = value; } }
		internal List<Cards.Card> CardsAvailable { get { return _CardsAvailable; } private set { _CardsAvailable = value; } }
		public Table Table { get { return _Table; } }
		public PlayerCollection Players { get { return _Players; } }
		public Player ActivePlayer { get { return _ActivePlayer; } private set { _ActivePlayer = value; } }
		public Player GetActivePlayer() { return ActivePlayer; }

		public Queue<GameMessage> MessageRequestQueue
		{
			get { return _MessageRequestQueue; }
		}
		public Queue<GameMessage> MessageResponseQueue
		{
			get { return _MessageResponseQueue; }
		}

		public IEnumerator<Player> GetPlayersStartingWithActiveEnumerator()
		{
			IEnumerator<Player> p = this.Players.GetPlayersStartingWithEnumerator(this.ActivePlayer);
			return p;
		}

		public IEnumerator<Player> GetPlayersStartingWithEnumerator(Player player)
		{
			IEnumerator<Player> p = this.Players.GetPlayersStartingWithEnumerator(player);
			return p;
		}

		public TurnList TurnsTaken { get { return _TurnsTaken; } }
		public Turn CurrentTurn
		{
			get
			{
				if (_TurnsTaken == null || _TurnsTaken.Count == 0)
					return null;
				return _TurnsTaken[_TurnsTaken.Count - 1];
			}
		}

		public GameSettings Settings { get { return _Settings; } private set { _Settings = value; } }

		public void Reset()
		{
			_Table.Reset();
			foreach (Player player in _Players)
				player.Reset();
		}

		public Player GetPlayerFromIndex(Player currentPlayer, int index)
		{
			while (index < 0)
				index += _Players.Count;
			return _Players[(_Players.IndexOf(currentPlayer) + index) % _Players.Count];
		}

        private Boolean CurrentTurnHasNextPlayer()
        {
            return this.TurnsTaken.Count > 0 && this.CurrentTurn.IsTurnFinished && this.CurrentTurn.NextPlayer != null;
        }

        public Player FigureNextPlayer()
        {
            if (CurrentTurnHasNextPlayer())
            {
                return this.CurrentTurn.NextPlayer;
            }
            else if (this.ActivePlayer != null)
            {
                return this.Players.Next(this.ActivePlayer);
            }    
            else
            {
                return this.Players[0];
            }
        }

        public Thread SetNextPlayer()
		{
			Boolean modifiedTurn = false;
			Turn turn = null;

            this.ActivePlayer = FigureNextPlayer();

			if (CurrentTurnHasNextPlayer())
			{
				modifiedTurn = true;
			}
			else if (this.TurnsTaken.Count > 0 && !this.CurrentTurn.IsTurnFinished)
			{
				//// I'm not dead yet!
				turn = this.CurrentTurn;
				//this.ActivePlayer = this.CurrentTurn.Player;
				//Thread startThread = new Thread(() => this.ActivePlayer.Start(this.CurrentTurn));
				//return startThread;
			}
			
			Reset();

			if (turn == null)
			{
				turn = new Turn(this.ActivePlayer) { ModifiedTurn = modifiedTurn };
				if (this.CurrentTurn != null)
					turn.GrantedBy = this.CurrentTurn.NextGrantedBy;
				this.TurnsTaken.Add(turn);
			}

			Thread startThread = new Thread(() => this.ActivePlayer.Start(turn));
			return startThread;
		}

		private void End()
		{
			if (this.State != GameState.Aborted)
				this.State = GameState.Ended;

			Reset();
			_ActivePlayer = null;
			foreach (Player player in _Players)
			{
				lock(player)
					player.End();
			}

			if (GameEndedEvent != null)
			{
				GameEndedEventArgs geea = new GameEndedEventArgs();
				GameEndedEvent(this, geea);
			}
		}

		public AutoResetEvent WaitEvent = new AutoResetEvent(false);

		private List<Thread> AIThreads { get; set; }

		public void StartAsync()
		{
			try
			{
				this.AIThreads = new List<Thread>();
				foreach (DominionBase.Players.Player aiPlayer in this.Players.Where(p => p.PlayerType == PlayerType.Computer))
				{
					Thread t = new Thread(aiPlayer.StartAsync);
					this.AIThreads.Add(t);
					t.Start();
					int counter = 0;
					while (((DominionBase.Players.AI.IComputerAI)aiPlayer).State != DominionBase.Players.AI.AIState.Running)
					{
						counter++;
						if (counter > 20)
							throw new GameCreationException("AI player did not register soon enough!");
						Thread.Sleep(250);
					}
				}

				Thread startingThread = null;
				_ShouldStop = true;
				if (this.State != GameState.Ended)
				{
					this.State = GameState.Running;
					_ShouldStop = false;
					startingThread = SetNextPlayer();
				}

				while (!_ShouldStop)
				{
					if (startingThread != null)
						startingThread.Start();
					WaitEvent.WaitOne();
					startingThread = null;

					while (true)
					{
						GameMessage message = null;
						// Making this lock section as small as possible
						lock (this.MessageRequestQueue)
						{
							if (this.MessageRequestQueue.Count > 0)
								message = this.MessageRequestQueue.Dequeue();
							else
								break;
						}

						//System.Diagnostics.Trace.WriteLine(String.Format("Message: {0}", message.Message));
						GameMessage response = new GameResponseMessage();
						response.Message = "ACK";

						try
						{
							message.ActBefore(this);
						}
						catch (NullReferenceException nre)
						{
							if (!_ShouldStop)
								throw nre;
						}

						lock (_MessageResponseQueue)
						{
							_MessageResponseQueue.Enqueue(response);
						}
						if (message.WaitCallback != null)
							message.WaitCallback(null);

						Thread t = null;

						if (message.CheckEndGame && IsEndgameTriggered)
							_ShouldStop = true;
						else
							t = message.ActAfter(this);

						if (t != null)
							startingThread = t;
					}
				}

				End();
			}
			catch (Exception ex)
			{
				Utilities.Logging.LogError(ex);
				throw;
			}
		}

		public Boolean IsEndgameTriggered
		{
			get
			{
				return (_Table.Supplies.Values.Any(s => s.IsEndgameTriggered) ||
					_Table.Supplies.EmptySupplyPiles >= _EndgameSupplies);
			}
		}

		public PlayerCollection Winners
		{
			get
			{
				PlayerCollection playersWhoWon = new PlayerCollection();
				if (!this.IsEndgameTriggered)
					return playersWhoWon;
				foreach (Player player in this.Players.OrderByDescending(p => p.VictoryPoints).ThenBy(p => this.TurnsTaken.Count(t => t.IsTurnFinished && t.Player == p && !t.ModifiedTurn)))
				{
					if (playersWhoWon.Count == 0 || 
						(playersWhoWon[0].VictoryPoints == player.VictoryPoints &&
							this.TurnsTaken.Count(t => t.IsTurnFinished && t.Player == playersWhoWon[0] && !t.ModifiedTurn) == 
							this.TurnsTaken.Count(t => t.IsTurnFinished && t.Player == player && !t.ModifiedTurn)))
						playersWhoWon.Add(player);
				}
				return playersWhoWon;
			}
		}

		public void Abort()
		{
			this.State = GameState.Aborted;
			_ShouldStop = true;
		}

		public Cards.Cost ComputeCost(ICard card)
		{
			if (CostCompute != null)
			{
				CostComputeEventArgs ccea = null;
				lock (CostCompute)
				{
					ccea = new CostComputeEventArgs(card, card.BaseCost);
					// Somehow, this sometimes gets shut off between the initial null check and now.
					// So we need to double-check to make sure that firing off the event is valid
					if (CostCompute != null)
						CostCompute(this, ccea);
				}
				return ccea.Cost;
			}
			else
				return card.BaseCost;
		}

		private static List<Type> GetAllSerializingTypes(Cards.CardCollection cards)
		{
			List<Type> typeDict = new List<Type>() { typeof(Cards.Cost), typeof(Cards.Group), typeof(Cards.Category) };
			cards.ForEach(c => typeDict.AddRange(c.GetSerializingTypes()));
			return typeDict;
		}

		public void Save(String filename)
		{
			XmlDocument xdGame = new XmlDocument();
			XmlElement xeGame = xdGame.CreateElement("game");
			xdGame.AppendChild(xeGame);

			MemoryStream msRandom = new MemoryStream();
			BinaryFormatter bf = new BinaryFormatter();
			bf.Serialize(msRandom, this.RNG);
			msRandom.Close();
			XmlElement xe = xdGame.CreateElement("rng");
			xe.InnerText = Convert.ToBase64String(msRandom.ToArray());
			xeGame.AppendChild(xe);

			xe = xdGame.CreateElement("start_time");
			xe.InnerText = this.StartTime.ToString();
			xeGame.AppendChild(xe);

			xe = xdGame.CreateElement("state");
			xe.InnerText = this.State.ToString();
			xeGame.AppendChild(xe);

			xe = xdGame.CreateElement("activeplayer");
			if (this.ActivePlayer != null)
				xe.InnerText = this.ActivePlayer.UniqueId.ToString();
			xeGame.AppendChild(xe);

			XmlElement xeSettings = xdGame.CreateElement("settings");
			xeGame.AppendChild(xeSettings);

			Cards.CardCollection allCards = Cards.CardCollection.GetAllCards(c => true);

			XmlSerializer xsSettings = new XmlSerializer(typeof(GameSettings), GetAllSerializingTypes(allCards).ToArray());
			StringWriter swSettings = new StringWriter();
			xsSettings.Serialize(swSettings, this.Settings);
			swSettings.Close();
			
			XmlDocument xdSettings = new XmlDocument();
			xdSettings.LoadXml(swSettings.ToString());
			xeSettings.AppendChild(xdGame.ImportNode(xdSettings.DocumentElement, true));

			xeGame.AppendChild(this.Table.GenerateXml(xdGame, "table"));

			xeGame.AppendChild(this.Players.GenerateXml(xdGame));

			xeGame.AppendChild(this.TurnsTaken.GenerateXml(xdGame, "turns"));

			xdGame.Save("gamedump.xml");

			using (StringWriter sw = new StringWriter())
			using (XmlWriter xw = XmlWriter.Create(sw))
			{
				xdGame.WriteTo(xw);
				xw.Flush();
				System.IO.File.WriteAllBytes(filename, Utilities.StringUtility.Zip(Utilities.StringUtility.Encrypt(sw.GetStringBuilder().ToString())));
			}
		}

		public void Load(String filename)
		{
			if (this.State != GameState.Unknown)
				return;

			//try
			//{
				XmlDocument xdGame = new XmlDocument();

				xdGame.LoadXml(Utilities.StringUtility.Decrypt(Utilities.StringUtility.Unzip(System.IO.File.ReadAllBytes(filename))));
				//xdGame.Load("gamedump.xml");

				XmlNode xn = xdGame.SelectSingleNode("game/rng");
				if (xn != null)
				{
					MemoryStream msRandom = new MemoryStream(Convert.FromBase64String(xn.InnerText));
					BinaryFormatter bf = new BinaryFormatter();
					this.RNG = (Random)bf.Deserialize(msRandom);
					msRandom.Close();
				}

				xn = xdGame.SelectSingleNode("game/start_time");
				if (xn != null)
					this.StartTime = DateTime.Parse(xn.InnerText);

				this.State = GameState.Setup;

				xn = xdGame.SelectSingleNode("game/settings/GameSettings");
				if (xn != null)
				{
					Cards.CardCollection allCards = Cards.CardCollection.GetAllCards(c => true);
					XmlSerializer myDeserializer = new XmlSerializer(typeof(GameSettings), GetAllSerializingTypes(allCards).ToArray());
					using (StringReader sr = new StringReader(xn.OuterXml))
					{
						this.Settings = (GameSettings)myDeserializer.Deserialize(sr);
					}
				}

				_Table = new Table(this);
				XmlNodeList xnl = xdGame.SelectNodes("game/players/player");
				this._Players = new PlayerCollection();
				foreach (XmlNode xnPlayer in xnl)
				{
					Player player = Player.Load(this, xnPlayer);
					this._Players.Add(player);
					this.Table.AddPlayer(player);
				}

				xn = xdGame.SelectSingleNode("game/table");
				if (xn == null)
					return;
				this.Table.Load(xn);

				this._Players.Setup(this);

				xn = xdGame.SelectSingleNode("game/state");
				if (xn != null)
					this.State = (GameState)Enum.Parse(typeof(GameState), xn.InnerText, true);

				this.TurnsTaken.Load(this, xdGame.SelectSingleNode("game/turns"));

				xn = xdGame.SelectSingleNode("game/activeplayer");
				if (xn != null && !String.IsNullOrEmpty(xn.InnerText))
				{
					this.ActivePlayer = this.Players.FirstOrDefault(p => p.UniqueId == new Guid(xn.InnerText));
					this.ActivePlayer.SetCurrentTurn(this.TurnsTaken.Last());
				}
			//}
			//catch (Exception ex)
			//{
			//    throw ex;
			//}
		}
	}
}
