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

using Dominion.NET_WPF.Controls.GameLog;

namespace Dominion.NET_WPF.Controls
{
	/// <summary>
	/// Interaction logic for ucGameLog.xaml
	/// </summary>
	public partial class ucGameLog : UserControl
	{
		private LogSection _CurrentPlayerTurn = null;
		private LogSection _CurrentGameTurn = null;
		public String LogFile = String.Empty;
		private Dictionary<String, List<Brush>> PlayerBrushes = new Dictionary<String, List<Brush>>();

		public ucGameLog()
		{
			InitializeComponent();
		}

		public static DependencyProperty VerticalScrollBarVisibilityProperty = DependencyProperty.Register("VerticalScrollBarVisibility", 
			typeof(ScrollBarVisibility), typeof(ucGameLog));
		public static DependencyProperty HorizontalScrollBarVisibilityProperty = DependencyProperty.Register("HorizontalScrollBarVisibility",
			typeof(ScrollBarVisibility), typeof(ucGameLog));

		public ScrollBarVisibility VerticalScrollBarVisibility
		{
			get { return (ScrollBarVisibility)GetValue(VerticalScrollBarVisibilityProperty); }
			set { SetValue(VerticalScrollBarVisibilityProperty, value); }
		}

		public ScrollBarVisibility HorizontalScrollBarVisibility
		{
			get { return (ScrollBarVisibility)GetValue(HorizontalScrollBarVisibilityProperty); }
			set { SetValue(HorizontalScrollBarVisibilityProperty, value); }
		}

		public void TearDown()
		{
			_CurrentPlayerTurn = null;
			foreach (LogSection ls in spArea.Children.OfType<LogSection>())
				ls.TearDown();

			this.PlayerBrushes.Clear();
			spArea.Children.Clear();
		}

		public void Clear()
		{
			_CurrentPlayerTurn = null;
			foreach (LogSection ls in spArea.Children.OfType<LogSection>())
				ls.Dispose();

			this.PlayerBrushes.Clear();
			spArea.Children.Clear();
			svArea.ScrollToTop();
			if (System.IO.File.Exists(this.LogFile))
				Utilities.LogClear(this.LogFile);
			GC.Collect();
			GC.WaitForPendingFinalizers();
			GC.Collect();
		}

		public void Push()
		{
			if (_CurrentPlayerTurn == null)
				return;
			_CurrentPlayerTurn.Push();
		}

		public void Pop()
		{
			if (_CurrentPlayerTurn == null)
				return;
			_CurrentPlayerTurn.Pop();
		}

		public void NewSection(String title)
		{
			Utilities.Log(this.LogFile, "-------------------------------------------------------");

			if (_CurrentPlayerTurn != null)
				_CurrentPlayerTurn.End();

			_CurrentPlayerTurn = new ucGameMessage();
			_CurrentPlayerTurn.LogFile = this.LogFile;
			_CurrentPlayerTurn.New(title);

			spArea.Children.Add(_CurrentPlayerTurn);
			svArea.ScrollToBottom();
			svArea.ScrollToLeftEnd();
		}

		public void NewTurn(int turnNumber)
		{
			Utilities.Log(this.LogFile, String.Format("=======================================================", turnNumber));
			Utilities.Log(this.LogFile, String.Format("---------------------- Turn #{0} {1}---------------------", turnNumber, (new StringBuilder()).Insert(0, "-", 3 - (int)Math.Log10(turnNumber))));

			if (_CurrentGameTurn != null)
				_CurrentGameTurn.End();

			_CurrentGameTurn = new ucGameTurn();
			_CurrentGameTurn.LogFile = this.LogFile;
			(_CurrentGameTurn as ucGameTurn).New(turnNumber);

			spArea.Children.Add(_CurrentGameTurn);
			svArea.ScrollToBottom();
			svArea.ScrollToLeftEnd();
		}

		public void NewTurn(DominionBase.Players.Player player, DominionBase.Cards.Card grantedBy)
		{
			Utilities.Log(this.LogFile, "-------------------------------------------------------");

			if (_CurrentPlayerTurn != null)
				_CurrentPlayerTurn.End();

			_CurrentPlayerTurn = new ucPlayerTurn();
			_CurrentPlayerTurn.LogFile = this.LogFile;
			if (player != null)
				_CurrentPlayerTurn.New(player, this.PlayerBrushes[player.Name], grantedBy);

			if (_CurrentGameTurn != null)
			{
				(_CurrentGameTurn as ucGameTurn).Add(_CurrentPlayerTurn as ucPlayerTurn);

				if (wMain.Settings.AutoCollapseOldTurns)
				{
					IEnumerable<ucGameTurn> gameTurns = spArea.Children.OfType<ucGameTurn>();
					if (gameTurns.Count() > 1)
					{
						ucGameTurn gtOld = gameTurns.ElementAt(gameTurns.Count() - 2);
						foreach (ucPlayerTurn pt in gtOld.GetChildren(player))
							pt.IsExpanded = false;
						if (!gtOld.IsAnyExpanded)
						{
							gtOld.IsAllExpanded = true;
							gtOld.IsExpanded = false;
						}
					}
				}
			}
			else
			{
				spArea.Children.Add(_CurrentPlayerTurn);
			}

			svArea.ScrollToBottom();
			svArea.ScrollToLeftEnd();
		}

		public void Log(DominionBase.Players.Player player, params Object[] items)
		{
			if (_CurrentPlayerTurn == null)
				this.NewTurn(null, null);
			_CurrentPlayerTurn.Log(new DominionBase.Visual.VisualPlayer(player), this.PlayerBrushes[player.Name], items);
			svArea.ScrollToBottom();
			svArea.ScrollToLeftEnd();
		}

		public void Log(DominionBase.Visual.VisualPlayer player, params Object[] items)
		{
			if (_CurrentPlayerTurn == null)
				this.NewTurn(null, null);
			_CurrentPlayerTurn.Log(player, this.PlayerBrushes[player.Name], items);
			svArea.ScrollToBottom();
			svArea.ScrollToLeftEnd();
		}

		internal void Log(params Object[] items)
		{
			if (_CurrentPlayerTurn == null)
				this.NewSection(String.Empty);
			_CurrentPlayerTurn.Log(items);
			svArea.ScrollToBottom();
			svArea.ScrollToLeftEnd();
		}

		private void miCollapseAll_Click(object sender, RoutedEventArgs e)
		{
			foreach (LogSection ls in spArea.Children.OfType<LogSection>())
				ls.IsExpanded = false;
		}

		private void miExpandAll_Click(object sender, RoutedEventArgs e)
		{
			foreach (LogSection ls in spArea.Children.OfType<LogSection>())
				ls.IsExpanded = true;
		}

		internal void AddPlayerColor(String player, Color color)
		{
			ColorHls hlsValue = HLSColor.RgbToHls(color);
			this.PlayerBrushes[player] = new List<Brush>();
			this.PlayerBrushes[player].Add(new SolidColorBrush(HLSColor.HlsToRgb(hlsValue.H, Math.Min(1d, hlsValue.L * 1.1), hlsValue.S, hlsValue.A)));
			this.PlayerBrushes[player].Add(new SolidColorBrush(HLSColor.HlsToRgb(hlsValue.H, Math.Min(1d, hlsValue.L * 1.125), hlsValue.S * 0.95, hlsValue.A)));
			this.PlayerBrushes[player].Add(new SolidColorBrush(HLSColor.HlsToRgb(hlsValue.H, hlsValue.L * 0.25, hlsValue.S, hlsValue.A)));
			this.PlayerBrushes[player].ForEach((b) => b.Freeze());
		}

		private void CurrentGame_ViewGameLog_Click(object sender, RoutedEventArgs e)
		{
			if (System.IO.File.Exists(this.LogFile))
				System.Diagnostics.Process.Start(this.LogFile);
		}

		private void ContextMenu_Opened(object sender, RoutedEventArgs e)
		{
			miViewGameLog.IsEnabled = System.IO.File.Exists(this.LogFile);
		}
	}
}
