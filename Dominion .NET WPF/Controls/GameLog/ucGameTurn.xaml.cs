using System;
using System.Collections.Generic;
using System.Linq;
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

namespace Dominion.NET_WPF.Controls.GameLog
{
	/// <summary>
	/// Interaction logic for ucGameTurn.xaml
	/// </summary>
	public partial class ucGameTurn : LogSection
	{
		private int _TurnNumber = 0;

		public int TurnNumber { get { return _TurnNumber; } private set { _TurnNumber = value; } }

		public override Boolean IsExpanded { get { return expTurns.IsExpanded; } set { expTurns.IsExpanded = value; } }

		public Boolean IsAnyExpanded { get { return this.spArea.Children.OfType<ucPlayerTurn>().Any(pt => pt.IsExpanded); } }
		public Boolean IsAllExpanded 
		{ 
			get 
			{ 
				return this.spArea.Children.OfType<ucPlayerTurn>().All(pt => pt.IsExpanded); 
			}
			set
			{
				foreach (ucPlayerTurn pt in this.spArea.Children.OfType<ucPlayerTurn>())
					pt.IsExpanded = value;
			}
		}

		public ucGameTurn()
		{
			InitializeComponent();
			this.spContainer = spArea;
		}

		public void New(int turnNumber)
		{
			this.TurnNumber = turnNumber;

			DockPanel dp = new DockPanel();
			TextBlock tbTitle = new TextBlock();
			tbTitle.Text = String.Format("Turn #{0}", turnNumber);
			DockPanel.SetDock(tbTitle, Dock.Left);
			dp.Children.Add(tbTitle);
			expTurns.Header = dp;
		}

		public override void TearDown()
		{
			base.TearDown();

			foreach (ucPlayerTurn pt in this.spArea.Children.OfType<ucPlayerTurn>())
				pt.TearDown();
			spArea.Children.Clear();
		}

		public override void End()
		{
			Color newColor = Colors.Transparent;
			if (this.TurnNumber % 2 == 0)
				newColor = Colors.DarkGray;
				//expTurns.BorderBrush = Brushes.DarkGray;
			else
				newColor = Colors.DimGray;
				//expTurns.BorderBrush = Brushes.DimGray;

			lEdge.Stroke = new SolidColorBrush(newColor);
			LinearGradientBrush lgb = new LinearGradientBrush(new GradientStopCollection() { 
				new GradientStop(newColor, 0.0), 
				new GradientStop(newColor, 0.25), 
				new GradientStop(Colors.Transparent, 1.25) });
			lgb.Freeze();
			lTop.Stroke = lBottom.Stroke = lgb;
		}

		public void Add(ucPlayerTurn element)
		{
			this.spArea.Children.Add(element);
		}

		public IEnumerable<ucPlayerTurn> GetChildren(DominionBase.Players.Player player)
		{
			return this.spArea.Children.OfType<ucPlayerTurn>().Where(pt => pt.Player.PlayerUniqueId == player.UniqueId);
		}

		public ucPlayerTurn GetChild(DominionBase.Players.Player player)
		{
			return this.spArea.Children.OfType<ucPlayerTurn>().First(pt => pt.Player.PlayerUniqueId == player.UniqueId);
		}

		private void miCollapseThis_Click(object sender, RoutedEventArgs e)
		{
			expTurns.IsExpanded = false;
		}

		private void miExpandAll_Click(object sender, RoutedEventArgs e)
		{

		}

		private void miCollapseAll_Click(object sender, RoutedEventArgs e)
		{

		}
	}
}
