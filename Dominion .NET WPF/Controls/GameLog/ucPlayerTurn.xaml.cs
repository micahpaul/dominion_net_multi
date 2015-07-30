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
	/// Interaction logic for ucPlayerTurn.xaml
	/// </summary>
	public partial class ucPlayerTurn : LogSection
	{
		private int _CurrentInset = 0;
		private DominionBase.Visual.VisualPlayer _Player = null;
		public DominionBase.Visual.VisualPlayer Player { get { return _Player; } private set { _Player = value; } }
		private DominionBase.Visual.VisualPlayer _PreviousLinePlayer = null;

		public override Boolean IsExpanded { get { return expTurn.IsExpanded; } set { expTurn.IsExpanded = value; } }

		public ucPlayerTurn()
		{
			InitializeComponent();
			this.spContainer = spArea;
		}

		public override void TearDown()
		{
			base.TearDown();

			_Player = null;
			_PreviousLinePlayer = null;
		}

		protected override void Dispose(bool disposing)
		{
			// Check to see if Dispose has already been called.
			if (!this.disposed)
			{
				// If disposing equals true, dispose all managed
				// and unmanaged resources.
				if (disposing)
				{
					// Dispose managed resources.
				}

				// Call the appropriate methods to clean up
				// unmanaged resources here.
				// If disposing is false,
				// only the following code is executed.
				//spContainer.Children.Clear();
				_Player = null;
				_PreviousLinePlayer = null;

				// Note disposing has been done.
				disposed = true;
			}
		}

		public override void Push()
		{
			_CurrentInset++;
		}

		public override void Pop()
		{
			_CurrentInset--;
		}

		public override void End()
		{
		}

		private String DepthPrefix
		{
			get
			{
				StringBuilder sb = new StringBuilder();
				for (int c = 0; c < _CurrentInset; c++)
					sb.Append("... ");
				return sb.ToString();
			}
		}

		public override void New(DominionBase.Players.Player player, List<Brush> playerBrushes, DominionBase.Cards.Card grantedBy)
		{
			this.Player = new DominionBase.Visual.VisualPlayer(player);
			_CurrentInset = 0;

			if (playerBrushes[0] != Brushes.Transparent)
			{
				this.Background = playerBrushes[1];
				this.BorderBrush = playerBrushes[2];
			}

			Utilities.Log(this.LogFile, String.Format("{0} starting turn{1}", player, grantedBy == null ? "" : String.Format(" from {0}", grantedBy.Name)));

			DockPanel dp = new DockPanel();
			TextBlock tbPlayerName = new TextBlock();
			tbPlayerName.Text = player.Name;
			tbPlayerName.FontWeight = FontWeights.Bold;
			DockPanel.SetDock(tbPlayerName, Dock.Left);
			dp.Children.Add(tbPlayerName);
			tbPlayerName = new TextBlock();
			tbPlayerName.Text = " starting turn";
			DockPanel.SetDock(tbPlayerName, Dock.Left);
			dp.Children.Add(tbPlayerName);

			if (grantedBy != null)
			{
				TextBlock tbGrantedBy = new TextBlock();
				tbGrantedBy.Text = " granted by ";
				DockPanel.SetDock(tbGrantedBy, Dock.Left);
				dp.Children.Add(tbGrantedBy);

				ucCardIcon icon = CardIconUtilities.CreateCardIcon(grantedBy);
				DockPanel.SetDock(icon, Dock.Left);
				dp.Children.Add(icon);

				TextBlock tbBlank = new TextBlock();
				DockPanel.SetDock(tbBlank, Dock.Left);
				dp.Children.Add(tbBlank);
			}

			expTurn.Header = dp;
		}

		public override void Log(DominionBase.Visual.VisualPlayer player, List<Brush> playerBrushes, params Object[] items)
		{
			StringBuilder sbFullLine = new StringBuilder();

			TreeViewItem tvi = new TreeViewItem() { Margin = new Thickness(0), Padding = new Thickness(0) };
			DockPanel dp = new DockPanel();
			dp.LastChildFill = true;
			sbFullLine.Append(this.DepthPrefix);
			TreeView tvActive = tvArea;

			DominionBase.Visual.VisualPlayer lineVisualPlayer = null;

			foreach (Object item in items)
			{
				if (item is String)
				{
					foreach (UIElement elem in Utilities.RenderText((String)item, NET_WPF.RenderSize.Tiny, true))
					{
						if (elem is TextBlock)
							((TextBlock)elem).VerticalAlignment = System.Windows.VerticalAlignment.Center;
						DockPanel.SetDock(elem, Dock.Left);
						dp.Children.Add(elem);
					}

					sbFullLine.Append(item);
				}
				else if (item is DominionBase.Currency)
				{
					foreach (UIElement elem in Utilities.RenderText(((DominionBase.Currency)item).ToStringInline(), NET_WPF.RenderSize.Tiny, true))
					{
						if (elem is TextBlock)
							((TextBlock)elem).VerticalAlignment = System.Windows.VerticalAlignment.Center;
						DockPanel.SetDock(elem, Dock.Left);
						dp.Children.Add(elem);
					}

					sbFullLine.Append(Utilities.RenderText(((DominionBase.Currency)item).ToStringInline()));
				}
				else if (item is DominionBase.Currencies.CurrencyBase)
				{
					foreach (UIElement elem in Utilities.RenderText(((DominionBase.Currencies.CurrencyBase)item).ToString(), NET_WPF.RenderSize.Tiny, true))
					{
						if (elem is TextBlock)
							((TextBlock)elem).VerticalAlignment = System.Windows.VerticalAlignment.Center;
						DockPanel.SetDock(elem, Dock.Left);
						dp.Children.Add(elem);
					}

					sbFullLine.Append(Utilities.RenderText(((DominionBase.Currencies.CurrencyBase)item).ToString()));
				}
				else if (item is DominionBase.Players.Player)
				{
					lineVisualPlayer = new DominionBase.Visual.VisualPlayer((DominionBase.Players.Player)item);

					TextBlock tbLine = new TextBlock();
					tbLine.Text = lineVisualPlayer.Name;
					tbLine.VerticalAlignment = System.Windows.VerticalAlignment.Center;
					if (lineVisualPlayer.PlayerUniqueId != this.Player.PlayerUniqueId)
						tbLine.FontWeight = FontWeights.Bold;

					DockPanel.SetDock(tbLine, Dock.Left);
					dp.Children.Add(tbLine);

					sbFullLine.Append(lineVisualPlayer.Name);
				}
				else if (item is DominionBase.Visual.VisualPlayer)
				{
					lineVisualPlayer = (DominionBase.Visual.VisualPlayer)item;

					TextBlock tbLine = new TextBlock();
					tbLine.Text = lineVisualPlayer.Name;
					tbLine.VerticalAlignment = System.Windows.VerticalAlignment.Center;
					if (lineVisualPlayer.PlayerUniqueId != this.Player.PlayerUniqueId)
						tbLine.FontWeight = FontWeights.Bold;

					DockPanel.SetDock(tbLine, Dock.Left);
					dp.Children.Add(tbLine);

					sbFullLine.Append(lineVisualPlayer.Name);
				}
				else if (item is DominionBase.ICard)
				{
					ucCardIcon icon = CardIconUtilities.CreateCardIcon((DominionBase.ICard)item);
					icon.VerticalAlignment = System.Windows.VerticalAlignment.Center;
					DockPanel.SetDock(icon, Dock.Left);
					dp.Children.Add(icon);

					sbFullLine.Append(((DominionBase.ICard)item).Name);
				}
				else if (item is IEnumerable<DominionBase.ICard>)
				{
					foreach (ucCardIcon icon in CardIconUtilities.CreateCardIcons((IEnumerable<DominionBase.ICard>)item))
					{
						icon.VerticalAlignment = System.Windows.VerticalAlignment.Center;
						DockPanel.SetDock(icon, Dock.Left);
						dp.Children.Add(icon);

						if (icon.Count == 1)
							sbFullLine.Append(icon.Card.Name);
						else
							sbFullLine.AppendFormat("{0}x {1}", icon.Count, icon.Card.Name);

						TextBlock tbLine = new TextBlock();
						tbLine.Text = ", ";
						tbLine.VerticalAlignment = System.Windows.VerticalAlignment.Center;
						DockPanel.SetDock(tbLine, Dock.Left);
						dp.Children.Add(tbLine);

						sbFullLine.Append(", ");
					}

					if (((IEnumerable<DominionBase.ICard>)item).Count() > 0)
					{
						dp.Children.RemoveAt(dp.Children.Count - 1);
						sbFullLine = sbFullLine.Remove(sbFullLine.Length - 2, 2);
					}
				}
				else if (item is IEnumerable<DominionBase.Token>)
				{
					int count = ((IEnumerable<DominionBase.Token>)item).Count();
					String displayString = ((IEnumerable<DominionBase.Token>)item).First().LongDisplayString;
					if (count != 1)
						displayString = String.Format("{0}x {1}", count, displayString);

					TextBlock tbLine = new TextBlock();
					tbLine.Text = displayString;
					tbLine.VerticalAlignment = System.Windows.VerticalAlignment.Center;

					DockPanel.SetDock(tbLine, Dock.Left);
					dp.Children.Add(tbLine);

					sbFullLine.Append(displayString);
				}
			}

			if (tvi.Background != null && dp.Children.Count > 0)
			{
				if (dp.Children.Count == 1)
				{
					if (dp.Children[0] is TextBlock)
						((TextBlock)dp.Children[0]).Padding = new Thickness(2, 1, 2, 1);
					else if (dp.Children[0] is ucCardIcon)
						((ucCardIcon)dp.Children[0]).Padding = new Thickness(2, 1, 2, 1);
				}
				else
				{
					if (dp.Children[0] is TextBlock)
						((TextBlock)dp.Children[0]).Padding = new Thickness(2, 1, 0, 1);
					else if (dp.Children[0] is ucCardIcon)
						((ucCardIcon)dp.Children[0]).Padding = new Thickness(2, 1, 0, 1);
					if (dp.Children[dp.Children.Count - 1] is TextBlock)
						((TextBlock)dp.Children[dp.Children.Count - 1]).Padding = new Thickness(0, 1, 2, 1);
					else if (dp.Children[dp.Children.Count - 1] is ucCardIcon)
						((ucCardIcon)dp.Children[dp.Children.Count - 1]).Padding = new Thickness(0, 1, 2, 1);
				}
			}

			// Need something to fill the remaining gap
			Grid g = new Grid();
			g.VerticalAlignment = System.Windows.VerticalAlignment.Stretch;
			dp.Children.Add(g);

			Utilities.Log(this.LogFile, sbFullLine.ToString());

			int level = this._CurrentInset;
			ItemCollection lItems = tvArea.Items;
			TreeViewItem tviParent = null;
			while (level > 0)
			{
				if (lItems[lItems.Count - 1] is TreeViewItem)
					tviParent = (TreeViewItem)lItems[lItems.Count - 1];
				else if (lItems[lItems.Count - 1] is TreeView)
					tviParent = (TreeViewItem)((TreeView)lItems[lItems.Count - 1]).Items[((TreeView)lItems[lItems.Count - 1]).Items.Count - 1];
				tviParent.IsExpanded = true;
				lItems = tviParent.Items;
				level--;
			}

			if (player.PlayerUniqueId == this.Player.PlayerUniqueId)
			{
				lItems.Add(tvi);
				tvi.Header = dp;
				tvi.HorizontalAlignment = System.Windows.HorizontalAlignment.Stretch;
			}
			else
			{
				if (lItems.Count == 0 || !(lItems[lItems.Count - 1] is TreeView) || 
					(lItems[lItems.Count - 1] is TreeView && ((TreeView)lItems[lItems.Count - 1]).Background != playerBrushes[0]))
				{
					// This is pretty mess at the moment -- this is to detect nested Player stuff
					// (e.g. discarding Market Square when trashing a card and then revealing Watchtower to put the gained Gold on top of your deck)
					if (tviParent is TreeViewItem && 
						(((TreeViewItem)tviParent).Parent is TreeView && ((TreeView)((TreeViewItem)tviParent).Parent).Background == playerBrushes[0] ||
						((TreeViewItem)tviParent).Parent is TreeViewItem && ((TreeViewItem)((TreeViewItem)tviParent).Parent).Parent is TreeView && ((TreeView)((TreeViewItem)((TreeViewItem)tviParent).Parent).Parent).Background == playerBrushes[0]))
						lItems.Add(tvi);
					else
					{
						TreeView tvNewPlayer = new TreeView();
						tvNewPlayer.Background = playerBrushes[0];
						tvNewPlayer.Margin = new Thickness(-19, 0, 0, 0);
						tvNewPlayer.Items.Add(tvi);
						lItems.Add(tvNewPlayer);
					}
				}
				else if (lItems[lItems.Count - 1] is TreeView)
				{
					((TreeView)lItems[lItems.Count - 1]).Items.Add(tvi);
				}
				else
					lItems.Add(tvi);
				tvi.Header = dp; 
				tvi.HorizontalAlignment = System.Windows.HorizontalAlignment.Stretch;
			}
			dp.BringIntoView();

			_PreviousLinePlayer = lineVisualPlayer;
		}

		void tvi_Expanded(object sender, RoutedEventArgs e)
		{
			TreeViewItem tvi = (TreeViewItem)sender;
			if (tvi.Parent == null || !tvi.IsExpanded)
				return;
			((Rectangle)((DockPanel)tvi.Header).Children[3]).Fill = tvi.Background;
			((Rectangle)((DockPanel)tvi.Header).Children[3]).Height = 0;
		}

		void tvi_Collapsed(object sender, RoutedEventArgs e)
		{
			TreeViewItem tvi = (TreeViewItem)sender;
			if (tvi.Parent == null || tvi.IsExpanded)
				return;
			TreeViewItem tviParent = (TreeViewItem)tvi.Parent;
			((Rectangle)((DockPanel)tvi.Header).Children[3]).Fill = Brushes.Black;
			((Rectangle)((DockPanel)tvi.Header).Children[3]).Height = 1;
		}

		private ItemsControl FindPrevious(TreeViewItem tviParent)
		{
			if (tviParent == null)
				return null;

			ItemsControl tviLast = tviParent;
			if (tviLast.Items.Count > 1)
				tviLast = (ItemsControl)tviLast.Items[tviLast.Items.Count - 2];
			else
				return tviLast;

			while (tviLast.HasItems)
				tviLast = (ItemsControl)tviLast.Items[tviLast.Items.Count - 1];

			return tviLast;
		}

		private int FindPlayerDepth(TreeViewItem tvi)
		{
			ItemsControl tviCurrent = tvi;
			int levels = 0;
			while (true)
			{
				tviCurrent = (ItemsControl)tviCurrent.Parent;
				if (tviCurrent == null || tviCurrent.Background != tvi.Background)
					break;
				levels++;
			}
			return levels;
		}

		private void tvArea_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
		{
			if (sender is TreeView && !e.Handled)
			{
				e.Handled = true;
				MouseWheelEventArgs mwea = new MouseWheelEventArgs(e.MouseDevice, e.Timestamp, e.Delta);
				mwea.RoutedEvent = UIElement.MouseWheelEvent;
				mwea.Source = sender;
				(((Control)sender).Parent as UIElement).RaiseEvent(mwea);
			}
		}
	}
}
