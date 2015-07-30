using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Windows;
using System.Windows.Media;
using System.Xml.Serialization;

using DominionBase.Cards;

namespace Dominion.NET_WPF
{
	public class SettingsChangedEventArgs : EventArgs
	{
		public SettingsChangedEventArgs()
		{
		}
	}

	public enum ToolTipShowDuration
	{
		Off = 0,
		Short = 2000,
		Normal = 5000,
		Long = 10000,
		SuperLong = 20000
	}

	public enum LayoutStyle
	{
		[DisplayString(ResourceKey = "LayoutStyle_Supply2Columns")]
		Supply2Columns,
		[DisplayString(ResourceKey = "LayoutStyle_Supply4Columns")]
		Supply4Columns
	}

	public enum GameLogLocation
	{
		[DisplayString(ResourceKey = "GameLogLocation_InCommonArea")]
		InCommonArea,
		[DisplayString(ResourceKey = "GameLogLocation_InGameTabArea")]
		InGameTabArea
	}

	[AttributeUsage(AttributeTargets.Field)]
	public sealed class DisplayStringAttribute : Attribute
	{
		private readonly string value;
		public string Value
		{
			get { return value; }
		}

		public string ResourceKey { get; set; }

		public DisplayStringAttribute(string v)
		{
			this.value = v;
		}

		public DisplayStringAttribute()
		{
		}
	}

	[Serializable]
	public class PlayerSettings
	{
		private String _Name = String.Empty;
		private String _AIClass = String.Empty;
		private Type _AIClassType = null;
		private Color _UIColor = Colors.Transparent;

		public PlayerSettings() { }

		public String Name { get { return _Name; } set { _Name = value; } }
		public String AIClass 
		{ 
			get { return _AIClass; } 
			set 
			{ 
				_AIClass = value;
				_AIClassType = DominionBase.Players.PlayerCollection.GetAllAIs().FirstOrDefault(t => t.FullName == value);
			} 
		}
		[XmlIgnore]
		public Type AIClassType { get { return _AIClassType; } set { _AIClassType = value; _AIClass = value.FullName; } }
		public Color UIColor { get { return _UIColor; } set { _UIColor = new Color() { R = value.R, G = value.G, B = value.B, A = value.A }; } }
	}

	[Serializable]
	public class PlayerSettingsCollection : List<PlayerSettings>
	{
	}

	[Serializable]
	public class Settings
	{
		public delegate void SettingsChangedEventHandler(object sender, SettingsChangedEventArgs e);
		public virtual event SettingsChangedEventHandler SettingsChanged = null;

		private int _NumberOfPlayers = 2;
		private int _NumberOfHumanPlayers = 1;

		private Boolean _IdenticalStartingHands = false;

		private Boolean _PromptUnplayedActions = true;
		private Boolean _PromptUnspentBuysTreasure = true;
		private Boolean _PromptUnspentBuysTreasure_OnlyNotCopperCurseRuins = true;
		private Boolean _NeverBuyCopperOrCurseExceptWhenGoonsIsInPlay = false;
		private Boolean _DisplaySupplyPileNames = true;
		private Boolean _DisplayBasicSupplyPileNames = true;
		private Boolean _ShowToolTipOnRightClick = false;
		private Boolean _Chooser_AutomaticallyClickWhenSatisfied = true;
		private Boolean _Chooser_AutomaticallyRevealMoat = false;
		private Boolean _Chooser_AutomaticallyRevealProvince = false;
		private Boolean _Chooser_AutomaticallyMoveStashToTop = false;

		private PlayerSettingsCollection _PlayerSettings = new PlayerSettingsCollection();
		private CardsSettingsCollection _CardSettings = new CardsSettingsCollection();
		private ConstraintCollection _Constraints = new ConstraintCollection();
		private ToolTipShowDuration _ToolTipShowDuration = ToolTipShowDuration.Normal;
		private LayoutStyle _LayoutStyle = LayoutStyle.Supply2Columns;
		private GameLogLocation _GameLogLocation = GameLogLocation.InCommonArea;
		private Boolean _AutoCollapseOldTurns = false;
		private Boolean _UseCustomImages = false;
		private String _CustomImagesPathSmall = "small";
		private String _CustomImagesPathMedium = "medium";
		private Boolean _UseCustomToolTips = false;
		private String _CustomToolTipsPath = String.Empty;

		private Size _WindowSize = new Size();
		private WindowState _WindowState = WindowState.Normal;
		private DateTime _LastUpdateCheck = DateTime.MinValue;
		private Boolean _UpdateAvailable = false;

		private PresetCollection _Presets = null;
		private Boolean _UsePreset = false;
		private Boolean _Settings_ShowPresetCards = true;
		private String _PresetName = String.Empty;

		private Boolean _AutoPlayTreasures = false;
		private Boolean _AutoPlayTreasures_IncludingLoan = true;
		private Boolean _AutoPlayTreasures_LoanFirst = true;
		private Boolean _AutoPlayTreasures_IncludingHornOfPlenty = true;
		private Boolean _AutoPlayTreasures_HornOfPlentyFirst = true;

		private Boolean _RandomAI_Unique = false;
		private List<String> _RandomAI_AllowedAIs = new List<String>();
		private Boolean _AutomaticallyAcceptKingdomCards = false;
		private Boolean _ForceColonyPlatinum = false;
		private Boolean _ForceShelters = false;

		public Settings()
		{
		}

		public Settings(Settings settings)
		{
			this.CopyFrom(settings);
		}

		public int NumberOfPlayers
		{
			get { return _NumberOfPlayers; }
			set
			{
				_NumberOfPlayers = value; 
				if (_NumberOfPlayers < 1) 
					_NumberOfPlayers = 1;
				if (_NumberOfPlayers > 6)
					_NumberOfPlayers = 6;
				if (NumberOfHumanPlayers > _NumberOfPlayers)
					NumberOfHumanPlayers = _NumberOfPlayers; 
			}
		}

		public int NumberOfHumanPlayers
		{
			get { return _NumberOfHumanPlayers; }
			set
			{
				_NumberOfHumanPlayers = value;
				if (_NumberOfHumanPlayers < 0)
					_NumberOfHumanPlayers = 0;
				if (_NumberOfHumanPlayers > NumberOfPlayers)
					_NumberOfHumanPlayers = NumberOfPlayers;
			}
		}


		public Boolean IdenticalStartingHands { get { return _IdenticalStartingHands; } set { _IdenticalStartingHands = value; } }

		public Boolean PromptUnplayedActions { get { return _PromptUnplayedActions; } set { _PromptUnplayedActions = value; } }
		public Boolean PromptUnspentBuysTreasure { get { return _PromptUnspentBuysTreasure; } set { _PromptUnspentBuysTreasure = value; } }
		public Boolean PromptUnspentBuysTreasure_OnlyNotCopperCurseRuins { get { return _PromptUnspentBuysTreasure_OnlyNotCopperCurseRuins; } set { _PromptUnspentBuysTreasure_OnlyNotCopperCurseRuins = value; } }

		public Boolean NeverBuyCopperOrCurseExceptWhenGoonsIsInPlay { get { return _NeverBuyCopperOrCurseExceptWhenGoonsIsInPlay; } set { _NeverBuyCopperOrCurseExceptWhenGoonsIsInPlay = value; } }

		public Boolean Chooser_AutomaticallyClickWhenSatisfied { get { return _Chooser_AutomaticallyClickWhenSatisfied; } set { _Chooser_AutomaticallyClickWhenSatisfied = value; } }
		public Boolean Chooser_AutomaticallyRevealMoat { get { return _Chooser_AutomaticallyRevealMoat; } set { _Chooser_AutomaticallyRevealMoat = value; } }
		public Boolean Chooser_AutomaticallyRevealProvince { get { return _Chooser_AutomaticallyRevealProvince; } set { _Chooser_AutomaticallyRevealProvince = value; } }
		public Boolean Chooser_AutomaticallyMoveStashToTop { get { return _Chooser_AutomaticallyMoveStashToTop; } set { _Chooser_AutomaticallyMoveStashToTop = value; } }

		[XmlArray]
		public PlayerSettingsCollection PlayerSettings { get { return _PlayerSettings; } set { _PlayerSettings = value; } }

		[XmlArray]
		public CardsSettingsCollection CardSettings { get { return _CardSettings; } set { _CardSettings = value; } }

		[XmlArray]
		public ConstraintCollection Constraints { get { return _Constraints; } set { _Constraints = value; } }

		public ToolTipShowDuration ToolTipShowDuration { get { return _ToolTipShowDuration; } set { _ToolTipShowDuration = value; } }
		public Boolean ShowToolTipOnRightClick { get { return _ShowToolTipOnRightClick; } set { _ShowToolTipOnRightClick = value; } }
		public Boolean DisplaySupplyPileNames { get { return _DisplaySupplyPileNames; } set { _DisplaySupplyPileNames = value; } }
		public Boolean DisplayBasicSupplyPileNames { get { return _DisplayBasicSupplyPileNames; } set { _DisplayBasicSupplyPileNames = value; } }
		public LayoutStyle LayoutStyle { get { return _LayoutStyle; } set { _LayoutStyle = value; } }
		public GameLogLocation GameLogLocation { get { return _GameLogLocation; } set { _GameLogLocation = value; } }
		public Boolean AutoCollapseOldTurns { get { return _AutoCollapseOldTurns; } set { _AutoCollapseOldTurns = value; } }

		public Boolean UseCustomImages { get { return _UseCustomImages; } set { _UseCustomImages = value; } }
		public String CustomImagesPathSmall { get { return _CustomImagesPathSmall; } set { _CustomImagesPathSmall = value; } }
		public String CustomImagesPathMedium { get { return _CustomImagesPathMedium; } set { _CustomImagesPathMedium = value; } }
		public Boolean UseCustomToolTips { get { return _UseCustomToolTips; } set { _UseCustomToolTips = value; } }
		public String CustomToolTipsPath { get { return _CustomToolTipsPath; } set { _CustomToolTipsPath = value; } }

		public Size WindowSize { get { return _WindowSize; } set { _WindowSize = value; } }
		public WindowState WindowState { get { return _WindowState; } set { _WindowState = value; } }

		public DateTime LastUpdateCheck { get { return _LastUpdateCheck; } set { _LastUpdateCheck = value; } }
		public Boolean UpdateAvailable { get { return _UpdateAvailable; } set { _UpdateAvailable = value; } }

		#region Presets
		[XmlIgnore]
		public PresetCollection Presets
		{
			get
			{
				if (_Presets == null)
					_Presets = PresetCollection.Parse();
				return _Presets;
			}
			private set { _Presets = value; }
		}

		public Boolean UsePreset { get { return _UsePreset; } set { _UsePreset = value; } }
		public Boolean Settings_ShowPresetCards { get { return _Settings_ShowPresetCards; } set { _Settings_ShowPresetCards = value; } } 
		public String PresetName { get { return _PresetName; } set { _PresetName = value; } }
		#endregion 
		#region Auto-Play Treasures 
		public Boolean AutoPlayTreasures { get { return _AutoPlayTreasures; } set { _AutoPlayTreasures = value; } } 
		public Boolean AutoPlayTreasures_IncludingLoan { get { return _AutoPlayTreasures_IncludingLoan; } set { _AutoPlayTreasures_IncludingLoan = value; } } 
		public Boolean AutoPlayTreasures_LoanFirst { get { return _AutoPlayTreasures_LoanFirst; } set { _AutoPlayTreasures_LoanFirst = value; } }
		public Boolean AutoPlayTreasures_IncludingHornOfPlenty { get { return _AutoPlayTreasures_IncludingHornOfPlenty; } set { _AutoPlayTreasures_IncludingHornOfPlenty = value; } }
		public Boolean AutoPlayTreasures_HornOfPlentyFirst { get { return _AutoPlayTreasures_HornOfPlentyFirst; } set { _AutoPlayTreasures_HornOfPlentyFirst = value; } }
		#endregion 
		public Boolean RandomAI_Unique { get { return _RandomAI_Unique; } set { _RandomAI_Unique = value; } }
		[XmlArray]
		public List<String> RandomAI_AllowedAIs { get { return _RandomAI_AllowedAIs; } set { _RandomAI_AllowedAIs = value; } }
		public Boolean AutomaticallyAcceptKingdomCards { get { return _AutomaticallyAcceptKingdomCards; } set { _AutomaticallyAcceptKingdomCards = value; } }
		public Boolean ForceColonyPlatinum { get { return _ForceColonyPlatinum; } set { _ForceColonyPlatinum = value; } }
		public Boolean ForceShelters { get { return _ForceShelters; } set { _ForceShelters = value; } }

		private static String Filename
		{
			get
			{
				return System.IO.Path.Combine(DominionBase.Utilities.Application.ApplicationPath, "settings.xml");
			}
		}

		private static String OldFilename
		{
			get
			{
				return System.IO.Path.Combine(System.IO.Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName), "settings.xml");
			}
		}

		private static List<Type> GetAllSerializingTypes(CardCollection cards)
		{
			List<Type> typeDict = new List<Type>() { typeof(Cost), typeof(Group) };
			cards.ForEach(c => typeDict.AddRange(c.GetSerializingTypes()));
			return typeDict;
		}

		public void Save()
		{
			if (SettingsChanged != null)
			{
				SettingsChanged(this, new SettingsChangedEventArgs());
			}
			try
			{
				CardCollection allCards = CardCollection.GetAllCards(c => true);

				XmlSerializer xsSettings = new XmlSerializer(typeof(Settings), GetAllSerializingTypes(allCards).ToArray());
				StreamWriter swSettings = new StreamWriter(Settings.Filename);
				xsSettings.Serialize(swSettings, this);
				swSettings.Close();
			}
			catch (IOException) { }
		}

		public static Settings Load()
		{
			CardCollection allCards = CardCollection.GetAllCards(c => true);

			Settings settings = null;
			try
			{
				XmlSerializer mySerializer = new XmlSerializer(typeof(Settings), GetAllSerializingTypes(allCards).ToArray());
				// This should only need to be here temporarily -- probably 3-5 releases -- until all the other versions get transitioned to the new saving area
				String filename = Settings.Filename;
				if (!System.IO.File.Exists(Settings.Filename))
					filename = Settings.OldFilename;
				using (FileStream myFileStream = new FileStream(filename, FileMode.Open))
				{
					settings = (Settings)mySerializer.Deserialize(myFileStream);
				}
			}
			catch
			{
				settings = new Settings();
			}

			while (settings.PlayerSettings.Count < 6)
				settings.PlayerSettings.Add(new PlayerSettings() { 
					Name = String.Format("Player {0}", settings.PlayerSettings.Count + 1), 
					AIClassType = typeof(DominionBase.Players.AI.Standard),
					UIColor = HLSColor.HlsToRgb(24 * (settings.PlayerSettings.Count * 2), 0.85, 1, 1)
				});


			// Go through each card to make sure that the card's default settings are defined
			foreach (Card card in allCards)
			{
				CardSettingCollection csc = card.GenerateSettings();
				if (csc.Count == 0) // This card has no custom settings, so we can skip it
					continue;

				if (!settings.CardSettings.ContainsKey(card.Name))
					settings.CardSettings[card.Name] = new CardsSettings(card.Name);

				CardsSettings cardSettings = settings.CardSettings[card.Name];

				// Go through each setting defined for the card & make sure it exists
				foreach (CardSetting cSetting in csc)
				{
					if (!cardSettings.CardSettingCollection.ContainsKey(cSetting.GetType()))
						cardSettings.CardSettingCollection[cSetting.GetType()] = cSetting;
				}

				card.FinalizeSettings(cardSettings.CardSettingCollection);
			}

			return settings;
		}

		internal void CopyFrom(Settings settings)
		{
			this.NumberOfPlayers = settings.NumberOfPlayers;
			this.NumberOfHumanPlayers = settings.NumberOfHumanPlayers;
			this.IdenticalStartingHands = settings.IdenticalStartingHands;
			this.PromptUnplayedActions = settings.PromptUnplayedActions;
			this.PromptUnspentBuysTreasure = settings.PromptUnspentBuysTreasure;
			this.PromptUnspentBuysTreasure_OnlyNotCopperCurseRuins = settings.PromptUnspentBuysTreasure_OnlyNotCopperCurseRuins;
			this.NeverBuyCopperOrCurseExceptWhenGoonsIsInPlay = settings.NeverBuyCopperOrCurseExceptWhenGoonsIsInPlay;
			this.DisplaySupplyPileNames = settings.DisplaySupplyPileNames;
			this.DisplayBasicSupplyPileNames = settings.DisplayBasicSupplyPileNames;
			this.Chooser_AutomaticallyClickWhenSatisfied = settings.Chooser_AutomaticallyClickWhenSatisfied;
			this.ShowToolTipOnRightClick = settings.ShowToolTipOnRightClick;
			this.Chooser_AutomaticallyRevealMoat = settings.Chooser_AutomaticallyRevealMoat;
			this.Chooser_AutomaticallyRevealProvince = settings.Chooser_AutomaticallyRevealProvince;
			this.Chooser_AutomaticallyMoveStashToTop = settings.Chooser_AutomaticallyMoveStashToTop;
			this.PlayerSettings = settings.PlayerSettings;
			this.CardSettings = settings.CardSettings.DeepClone();
			this.Constraints = new ConstraintCollection();
			foreach (Constraint constraint in settings.Constraints)
				this.Constraints.Add(new Constraint(constraint.ConstraintType, constraint.ConstraintValue, constraint.Minimum, constraint.Maximum));
			this.ToolTipShowDuration = settings.ToolTipShowDuration;
			this.LayoutStyle = settings.LayoutStyle;
			this.GameLogLocation = settings.GameLogLocation;
			this.AutoCollapseOldTurns = settings.AutoCollapseOldTurns;
			this.UseCustomImages = settings.UseCustomImages;
			this.CustomImagesPathSmall = settings.CustomImagesPathSmall;
			this.CustomImagesPathMedium = settings.CustomImagesPathMedium;
			this.UseCustomToolTips = settings.UseCustomToolTips;
			this.CustomToolTipsPath = settings.CustomToolTipsPath;

			this.WindowSize = settings.WindowSize;
			this.WindowState = settings.WindowState;
			this.LastUpdateCheck = settings.LastUpdateCheck;
			this.UpdateAvailable = settings.UpdateAvailable;

			this.Presets = settings.Presets;
			this.UsePreset = settings.UsePreset;
			this.Settings_ShowPresetCards = settings.Settings_ShowPresetCards;
			this.PresetName = settings.PresetName;

			this.AutoPlayTreasures = settings.AutoPlayTreasures;
			this.AutoPlayTreasures_IncludingLoan = settings.AutoPlayTreasures_IncludingLoan;
			this.AutoPlayTreasures_LoanFirst = settings.AutoPlayTreasures_LoanFirst;
			this.AutoPlayTreasures_IncludingHornOfPlenty = settings.AutoPlayTreasures_IncludingHornOfPlenty;
			this.AutoPlayTreasures_HornOfPlentyFirst = settings.AutoPlayTreasures_HornOfPlentyFirst;

			this.RandomAI_Unique = settings.RandomAI_Unique;
			this.RandomAI_AllowedAIs = new List<String>(settings.RandomAI_AllowedAIs);

			this.AutomaticallyAcceptKingdomCards = settings.AutomaticallyAcceptKingdomCards;
			this.ForceColonyPlatinum = settings.ForceColonyPlatinum;
			this.ForceShelters = settings.ForceShelters;
		}
	}
}
