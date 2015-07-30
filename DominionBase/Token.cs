using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;

using DominionBase.Cards;
using DominionBase.Players;

namespace DominionBase
{
	public class TokenCollectionsChangedEventArgs : EventArgs
	{
		public enum Operation
		{
			Reset,
			Added,
			Removed
		}
		private TokenCollection _AddedTokens = new TokenCollection();
		private TokenCollection _RemovedTokens = new TokenCollection();
		private Operation _OperationPerformed;
		public TokenCollection TokenCollection;
		public Player Player = null;
		public TokenCollection AddedTokens { get { return _AddedTokens; } }
		public TokenCollection RemovedTokens { get { return _RemovedTokens; } }
		public Operation OperationPerformed { get { return _OperationPerformed; } private set { _OperationPerformed = value; } }
		public int Count { get { return this.TokenCollection.Count; } }
		public TokenCollectionsChangedEventArgs(TokenCollection tokenCollection, Operation operation)
		{
			this.TokenCollection = tokenCollection;
			this.OperationPerformed = operation;
		}
		public TokenCollectionsChangedEventArgs(TokenCollection tokenCollection, Operation operation, Token tokenChanged)
			: this(tokenCollection, null, operation, tokenChanged)
		{
		}
		public TokenCollectionsChangedEventArgs(TokenCollection tokenCollection, Operation operation, IEnumerable<Token> tokensChanged)
			: this(tokenCollection, null, operation, tokensChanged)
		{
		}
		public TokenCollectionsChangedEventArgs(TokenCollection tokenCollection, Player player, Operation operation)
			: this(tokenCollection, operation)
		{
			this.Player = player;
		}
		public TokenCollectionsChangedEventArgs(TokenCollection tokenCollection, Player player, Operation operation, Token tokenChanged)
			: this(tokenCollection, player, operation)
		{
			switch (operation)
			{
				case TokenCollectionsChangedEventArgs.Operation.Added:
					_AddedTokens.Add(tokenChanged);
					break;
				case TokenCollectionsChangedEventArgs.Operation.Removed:
					_RemovedTokens.Add(tokenChanged);
					break;
			}
		}
		public TokenCollectionsChangedEventArgs(TokenCollection tokenCollection, Player player, Operation operation, IEnumerable<Token> tokensChanged)
			: this(tokenCollection, player, operation)
		{
			switch (operation)
			{
				case TokenCollectionsChangedEventArgs.Operation.Added:
					_AddedTokens.AddRange(tokensChanged);
					break;
				case TokenCollectionsChangedEventArgs.Operation.Removed:
					_RemovedTokens.AddRange(tokensChanged);
					break;
			}
		}
	}

	public class TokenActionEventArgs : EventArgs
	{
		public Player Actor;
		public Player Actee;
		public Boolean Cancelled = false;
		public Card ActingCard;
		public List<Type> HandledBy = new List<Type>();
		public TokenActionEventArgs(Player actor, Player actee, Card actingCard)
		{
			Actor = actor;
			Actee = actee;
			ActingCard = actingCard;
		}
	}

	public abstract class Token
	{
		public delegate void TokenActionEventHandler(object sender, TokenActionEventArgs e);

		private String _DisplayString = String.Empty;
		private String _LongDisplayString = String.Empty;
		public Token(String displayString, String longDisplayString) { _DisplayString = displayString; _LongDisplayString = longDisplayString; }
		public String DisplayString { get { return _DisplayString; } }
		public String LongDisplayString { get { return _LongDisplayString; } }
		public virtual String Name { get { return this.GetType().Name; } }
		public virtual String Title { get { return this.Name; } }
		public virtual Boolean Buying(Table table, Player player) { return false; }
		public virtual Boolean Gaining() { return false; }
		public virtual Boolean ActDefined { get { return false; } }
		public virtual Boolean IsTemporary { get { return false; } }
		public virtual Boolean IsPlayable { get { return false; } }

		/// <summary>
		/// Used internally by the base Card class -- Don't use this.
		/// </summary>
		/// <param name="card"></param>
		/// <param name="e"></param>
		internal virtual void Act(Card card, TokenActionEventArgs e)
		{
		}

		/// <summary>
		/// Used internally by the base Card class -- Don't use this.
		/// </summary>
		internal virtual void Play(Player player, int count)
		{
		}

		/// <summary>
		/// Called when the Token should tear down any control -- used when it's not needed any more
		/// </summary>
		internal virtual void TearDown()
		{
		}

		public static Token CreateInstance(Type type)
		{
			return (Token)type.GetConstructor(Type.EmptyTypes).Invoke(null);
		}

		internal XmlNode GenerateXml(XmlDocument doc, String nodeName)
		{
			XmlElement xe = doc.CreateElement(nodeName);
			xe.InnerText = this.GetType().ToString();
			return xe;
		}

		internal static Token Load(XmlNode xnToken)
		{
			Type tokenType = Type.GetType(xnToken.InnerText);
			return Token.CreateInstance(tokenType);
		}
	}

	public class TokenCollection : List<Token>
	{
		public TokenCollection() : base() { }
		internal TokenCollection(IEnumerable<Token> collection) : base(collection) { }
		internal void TearDown()
		{
			foreach (Token token in this)
				token.TearDown();
		}

		internal XmlNode GenerateXml(XmlDocument doc, String nodeName)
		{
			XmlElement xeTokens = doc.CreateElement(nodeName);
			foreach (Token token in this)
				xeTokens.AppendChild(token.GenerateXml(doc, "token"));

			return xeTokens;
		}

		internal static TokenCollection Load(XmlNode xnTokens)
		{
			TokenCollection tc = new TokenCollection();
			foreach (XmlNode xnToken in xnTokens.SelectNodes("token"))
				tc.Add(Token.Load(xnToken));

			return tc;
		}
	}

	public class TokenCollections : SerializableDictionary<Type, TokenCollection>
	{
		public delegate void TokenCollectionsChangedEventHandler(object sender, TokenCollectionsChangedEventArgs e);
		public event TokenCollectionsChangedEventHandler TokenCollectionsChanged;

		public void Add(Token token)
		{
			Type t = token.GetType();
			this[t].Add(token);

			if (TokenCollectionsChanged != null)
			{
				TokenCollectionsChangedEventArgs tccea = new TokenCollectionsChangedEventArgs(this[t], TokenCollectionsChangedEventArgs.Operation.Added, token);
				TokenCollectionsChanged(this, tccea);
			}
		}

		internal void Add(Token token, Player player)
		{
			Type t = token.GetType();
			this[t].Add(token);

			if (TokenCollectionsChanged != null)
			{
				TokenCollectionsChangedEventArgs tccea = new TokenCollectionsChangedEventArgs(this[t], player, TokenCollectionsChangedEventArgs.Operation.Added, token);
				TokenCollectionsChanged(this, tccea);
			}
		}

		public void Remove(Token token)
		{
			Type t = token.GetType();
			this[t].Remove(token);

			if (TokenCollectionsChanged != null)
			{
				TokenCollectionsChangedEventArgs tccea = new TokenCollectionsChangedEventArgs(this[t], TokenCollectionsChangedEventArgs.Operation.Removed, token);
				TokenCollectionsChanged(this, tccea);
			}
		}

		public void Remove(IEnumerable<Token> tokens)
		{
			Type t = tokens.First().GetType();
			List<Token> tokensList = new List<Token>(tokens);
			this[t].RemoveAll(token => tokensList.Contains(token));

			if (TokenCollectionsChanged != null)
			{
				TokenCollectionsChangedEventArgs tccea = new TokenCollectionsChangedEventArgs(this[t], TokenCollectionsChangedEventArgs.Operation.Removed, tokensList);
				TokenCollectionsChanged(this, tccea);
			}
		}

		internal void Remove(Token token, Player player)
		{
			Type t = token.GetType();
			this[t].Remove(token);

			if (TokenCollectionsChanged != null)
			{
				TokenCollectionsChangedEventArgs tccea = new TokenCollectionsChangedEventArgs(this[t], player, TokenCollectionsChangedEventArgs.Operation.Removed, token);
				TokenCollectionsChanged(this, tccea);
			}
		}

		internal void Remove(IEnumerable<Token> tokens, Player player)
		{
			Type t = tokens.First().GetType();
			this[t].RemoveAll(token => tokens.Contains(token));

			if (TokenCollectionsChanged != null)
			{
				TokenCollectionsChangedEventArgs tccea = new TokenCollectionsChangedEventArgs(this[t], player, TokenCollectionsChangedEventArgs.Operation.Removed, tokens);
				TokenCollectionsChanged(this, tccea);
			}
		}

		internal Boolean IsAnyPlayable
		{
			get
			{
				return this.Any(kvp => kvp.Value.Count > 0 && kvp.Value.Any(t => t.IsPlayable));
			}
		}

		internal void TearDown()
		{
			foreach (TokenCollection tokens in this.Values)
				tokens.TearDown();
		}

		internal XmlNode GenerateXml(XmlDocument doc, String nodeName)
		{
			XmlElement xeTokenPiles = doc.CreateElement(nodeName);

			foreach (KeyValuePair<Type, TokenCollection> kvpTokenCollection in this)
			{
				XmlElement xeTokenPile = doc.CreateElement("tokenpile");
				xeTokenPiles.AppendChild(xeTokenPile);

				XmlElement xe = doc.CreateElement("type");
				xe.InnerText = kvpTokenCollection.Key.ToString();
				xeTokenPile.AppendChild(xe);

				xeTokenPile.AppendChild(kvpTokenCollection.Value.GenerateXml(doc, "tokens"));
			}

			return xeTokenPiles;
		}

		internal void Load(XmlNode xnRoot)
		{
			foreach (XmlNode xnTokenPile in xnRoot.SelectNodes("tokenpile"))
			{
				XmlNode xnType = xnTokenPile.SelectSingleNode("type");

				if (xnType == null)
					continue;
				Type type = Type.GetType(xnType.InnerText);

				this[type] = TokenCollection.Load(xnTokenPile.SelectSingleNode("tokens"));
				if (TokenCollectionsChanged != null)
				{
					TokenCollectionsChangedEventArgs tccea = new TokenCollectionsChangedEventArgs(this[type], TokenCollectionsChangedEventArgs.Operation.Added, this[type]);
					TokenCollectionsChanged(this, tccea);
				}
			}
		}
	}
}
