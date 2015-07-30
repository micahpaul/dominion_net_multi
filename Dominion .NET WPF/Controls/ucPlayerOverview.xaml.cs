using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
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

namespace Dominion.NET_WPF.Controls
{
	/// <summary>
	/// Interaction logic for ucPlayerOverview.xaml
	/// </summary>
	public partial class ucPlayerOverview : UserControl
	{
		public ucPlayerOverview()
		{
			InitializeComponent();
		}

		private DominionBase.Players.Player _Player = null;
		private DominionBase.Turn _Turn = null;

		public DominionBase.Players.Player Player
		{
			private get { return _Player; }
			set {
				_Player = value;
				if (_Player == null)
					return;
				switch (this.Player.PlayerType)
				{
					case DominionBase.Players.PlayerType.Human:
						iPlayerType.Source = (BitmapImage)this.Resources["imHuman"];
						tbAIType.Text = "Human";
						break;

					case DominionBase.Players.PlayerType.Computer:
						iPlayerType.Source = (BitmapImage)this.Resources["imComputer"];
						tbAIType.Text = (String)this.Player.GetType().GetProperty("AIType", BindingFlags.Instance | BindingFlags.Public | BindingFlags.GetProperty | BindingFlags.FlattenHierarchy).GetValue(this.Player, null);
						break;
				}
				tbPlayerName.Text = this.Player.Name;
			}
		}

		public DominionBase.Turn Turn
		{
			private get { return _Turn; }
			set
			{
				_Turn = value;
				if (_Turn == null)
					_Turn = new DominionBase.Turn(this.Player);

				tbCardsPlayed.Text = _Turn.CardsPlayed.ToString(true);
				if (tbCardsPlayed.Text == String.Empty)
					lCardsPlayed.Visibility = tbCardsPlayed.Visibility = System.Windows.Visibility.Collapsed;
				else
					lCardsPlayed.Visibility = tbCardsPlayed.Visibility = System.Windows.Visibility.Visible;

				tbCardsBought.Text = _Turn.CardsBought.ToString(true);
				if (tbCardsBought.Text == String.Empty)
					lCardsBought.Visibility = tbCardsBought.Visibility = System.Windows.Visibility.Collapsed;
				else
					lCardsBought.Visibility = tbCardsBought.Visibility = System.Windows.Visibility.Visible;

				tbCardsGained.Text = _Turn.CardsGained.FindAll(c => !_Turn.CardsBought.Contains(c)).ToString(true);
				if (tbCardsGained.Text == String.Empty)
					lCardsGained.Visibility = tbCardsGained.Visibility = System.Windows.Visibility.Collapsed;
				else
					lCardsGained.Visibility = tbCardsGained.Visibility = System.Windows.Visibility.Visible;

				tbCardsTrashed.Text = _Turn.CardsTrashed.ToString(true);
				if (tbCardsTrashed.Text == String.Empty)
					lCardsTrashed.Visibility = tbCardsTrashed.Visibility = System.Windows.Visibility.Collapsed;
				else
					lCardsTrashed.Visibility = tbCardsTrashed.Visibility = System.Windows.Visibility.Visible;

				tbCardsGainedAfter.Text = _Turn.CardsGainedAfter.ToString(true);
				if (tbCardsGainedAfter.Text == String.Empty)
					lCardsGainedAfter.Visibility = tbCardsGainedAfter.Visibility = System.Windows.Visibility.Collapsed;
				else
					lCardsGainedAfter.Visibility = tbCardsGainedAfter.Visibility = System.Windows.Visibility.Visible;

				tbCardsTrashedAfter.Text = _Turn.CardsTrashedAfter.ToString(true);
				if (tbCardsTrashedAfter.Text == String.Empty)
					lCardsTrashedAfter.Visibility = tbCardsTrashedAfter.Visibility = System.Windows.Visibility.Collapsed;
				else
					lCardsTrashedAfter.Visibility = tbCardsTrashedAfter.Visibility = System.Windows.Visibility.Visible;

			}
		}

		public void TearDown()
		{
			this.Player = null;
			this.Turn = null;
		}
	}
}
