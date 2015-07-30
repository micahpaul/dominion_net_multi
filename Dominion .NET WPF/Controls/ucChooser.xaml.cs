using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
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
	/// Interaction logic for ucChooser.xaml
	/// </summary>
	public partial class ucChooser : UserControl
	{
		public static readonly RoutedEvent ChooserOKClickEvent = EventManager.RegisterRoutedEvent(
			"ChooserOKClick",
			RoutingStrategy.Bubble,
			typeof(RoutedEventHandler),
			typeof(ucChooser));

		public event RoutedEventHandler ChooserOKClick
		{
			add { AddHandler(ChooserOKClickEvent, value); }
			remove { RemoveHandler(ChooserOKClickEvent, value); }
		}

		private DominionBase.Players.Player _Player = null;
		private DominionBase.Choice _Choice = null;
		private DominionBase.ChoiceResult _ChoiceResult = null;
		private List<SupplyControl> _SupplyControls = new List<SupplyControl>();

		private Guid _LastChoiceId = Guid.Empty;
		private DominionBase.ChoiceResult _LastChoiceResult = null;

		public ucChooser()
		{
			InitializeComponent();

			((INotifyCollectionChanged)lbReorder.Items).CollectionChanged += new NotifyCollectionChangedEventHandler(lbReorder_CollectionChanged);
		}

		public String Target { get; set; }

		public DominionBase.Players.Player Player
		{
			get { return _Player; }
			set { _Player = value; }
		}

		public DominionBase.ChoiceResult ChoiceResult
		{
			get { return _ChoiceResult; }
			private set { _ChoiceResult = value; }
		}

		public DominionBase.Choice Choice
		{
			get { return _Choice; }
			set { _Choice = value; }
		}

		public List<SupplyControl> SupplyControls
		{
			private get { return _SupplyControls; }
			set { _SupplyControls = value; }
		}

		public Boolean IsReady
		{
			set
			{
				this.ChoiceResult = null;

				if (!value)
				{
					_Player = null;
					_Choice = null;
					_ChoiceResult = null;
					_SupplyControls.Clear();
				}

				if (value && _Choice != null)
				{
					tbText.Text = String.Empty;
					wrapPanel1.Visibility = System.Windows.Visibility.Visible;
					wrapPanel1.Children.Clear();
					bReorder.Visibility = bCardPool.Visibility = lbCardPool.Visibility = System.Windows.Visibility.Collapsed;
					lbReorder.DataContext = null;
					bHidden.Visibility = System.Windows.Visibility.Collapsed;
					lbHidden.DataContext = null;

					wpButtons.Visibility = bOK.Visibility = cbAutoClick.Visibility = bButtonDiv.Visibility = System.Windows.Visibility.Visible;
					bNone.Visibility = System.Windows.Visibility.Visible;
					bAll.Visibility = Visibility.Collapsed;
					bOK.IsEnabled = false;

					if (_Choice.EventArgs is DominionBase.Players.AttackedEventArgs)
					{
						DominionBase.Players.AttackedEventArgs aea = _Choice.EventArgs as DominionBase.Players.AttackedEventArgs;
						tbText.Text = String.Format("{1} played {2}{0}", System.Environment.NewLine, aea.Attacker, aea.AttackCard);
					}

					foreach (SupplyControl sc in this.SupplyControls)
						sc.Clickability = SupplyVisibility.Plain;

					tbText.Text += String.Format("{0}, {1}", this.Player, Utilities.RenderText(Choice.Text));
					switch (_Choice.ChoiceType)
					{
						case DominionBase.ChoiceType.Options:
							this.ChoiceResult = new DominionBase.ChoiceResult(new List<String>());
							List<char> usedHotkeys = new List<char>();
							foreach (DominionBase.Option option in _Choice.Options)
							{
								String optionPreText = String.Empty;
								String optionShortcut = String.Empty;
								String optionText = option.Text;
								int shortcutIndex = -1;
								Boolean inTag = false;
								Boolean inElement = false;
								for (int index = 0; index < option.Text.Length; index++)
								{
									if (!inTag && option.Text[index] == '<')
									{
										inTag = true;
										inElement = true;
									}
									if (inTag && option.Text[index] == '>')
										inTag = false;
									if (inTag && option.Text[index] == '/')
										inElement = false;

									if (inTag)
										continue;

									if ((char.IsUpper(option.Text[index]) || (char.IsNumber(option.Text[index]) && !inElement)) && !usedHotkeys.Contains(option.Text[index]))
									{
										usedHotkeys.Add(option.Text[index]);
										shortcutIndex = index;
										break;
									}
								}
								if (shortcutIndex >= 0)
								{
									optionPreText = option.Text.Substring(0, shortcutIndex);
									optionShortcut = String.Format("_{0}", option.Text.Substring(shortcutIndex, 1));
									optionText = option.Text.Substring(shortcutIndex + 1);
								}

								RoutedUICommand rc = null;
								ToggleButton toggleButton = (ToggleButton)CreateButton(false, rc, option.IsRequired);

								TextBlock tbButtonText = (TextBlock)((WrapPanel)((Border)((ToggleButton)toggleButton).Content).Child).Children[2];
								((AccessText)((WrapPanel)((Border)((ToggleButton)toggleButton).Content).Child).Children[1]).Text = optionShortcut;

								if (!String.IsNullOrEmpty(optionPreText))
								{
									TextBlock tbPreTemp = (TextBlock)Utilities.RenderText(optionPreText, NET_WPF.RenderSize.Small, false)[0];
									while (tbPreTemp.Inlines.Count > 0)
										tbButtonText.Inlines.Add(tbPreTemp.Inlines.ElementAt(0));
								}

								if (!String.IsNullOrEmpty(optionShortcut))
									tbButtonText.Inlines.Add(new Run(optionShortcut.Substring(1)) { 
										FontWeight = FontWeights.Bold, 
										Foreground = Brushes.Crimson, 
										TextDecorations = TextDecorations.Underline });

								if (!String.IsNullOrEmpty(optionText))
								{
									TextBlock tbPostTemp = (TextBlock)Utilities.RenderText(optionText, NET_WPF.RenderSize.Small, false)[0];
									while (tbPostTemp.Inlines.Count > 0)
										tbButtonText.Inlines.Add(tbPostTemp.Inlines.ElementAt(0));
								}

								toggleButton.Tag = option;
								wrapPanel1.Children.Add(toggleButton);
							}
							if (_Choice.Maximum > 1 && _Choice.Maximum >= wrapPanel1.Children.Count)
								bAll.Visibility = Visibility.Visible;
							break;

						case DominionBase.ChoiceType.Cards:
							this.ChoiceResult = new DominionBase.ChoiceResult(new DominionBase.Cards.CardCollection());

							if (!_Choice.IsOrdered)
							{
								foreach (DominionBase.Cards.Card card in _Choice.Cards)
								{
									if (card.CardType == DominionBase.Cards.Universal.TypeClass.Dummy)
									{
										Border bDivider = new Border()
										{
											BorderBrush = Brushes.BlueViolet,
											BorderThickness = new Thickness(2),
											Margin = new Thickness(4)
										};

										wrapPanel1.Children.Add(bDivider);
									}
									else
									{
										ucICardButton icb = (ucICardButton)CreateButton(true, null, false);
										icb.ICard = card;
										icb.Tag = card;

										wrapPanel1.Children.Add(icb);
									}
								}
								if (_Choice.Maximum > 1 && _Choice.Maximum >= wrapPanel1.Children.OfType<ucICardButton>().Count())
									bAll.Visibility = Visibility.Visible;
							}
							else
							{
								wrapPanel1.Visibility = System.Windows.Visibility.Collapsed;

								switch (Choice.Visibility)
								{
									case DominionBase.Piles.Visibility.All:
										bReorder.Visibility = System.Windows.Visibility.Visible;

										if (_Choice.Cards.Count() == _Choice.Minimum)
										{
											ViewModel.CardListViewModel clvm = new ViewModel.CardListViewModel();
											clvm.ShowCards(Choice.Cards, Choice.Visibility);
											lbReorder.DataContext = clvm;
										}
										else
										{
											bCardPool.Visibility = lbCardPool.Visibility = System.Windows.Visibility.Visible;
											ViewModel.CardListViewModel clvmReorder = new ViewModel.CardListViewModel();
											clvmReorder.ShowCards(new DominionBase.Cards.CardCollection(), Choice.Visibility);
											lbReorder.DataContext = clvmReorder;

											ViewModel.CardListViewModel clvmPool = new ViewModel.CardListViewModel() { PreserveSourceOrdering = true };
											clvmPool.ShowCards(Choice.Cards, Choice.Visibility);
											lbCardPool.DataContext = clvmPool;
										}

										if (lbReorder.Items.Count >= _Choice.Minimum && lbReorder.Items.Count <= _Choice.Maximum)
											bOK.IsEnabled = true;
										else
											bOK.IsEnabled = false;
										break;

									case DominionBase.Piles.Visibility.None:
										bHidden.Visibility = System.Windows.Visibility.Visible;

										IEnumerable<DominionBase.Cards.Card> showCards = Choice.Cards;
										if (wMain.Settings != null && wMain.Settings.Chooser_AutomaticallyMoveStashToTop &&
											Choice.Text == "Cards have been shuffled.  You may rearrange them" &&
											Choice.Cards.Any(c => c.CardType == DominionBase.Cards.Promotional.TypeClass.Stash))
										{
											showCards = new DominionBase.Cards.CardCollection(Choice.Cards.OrderByDescending(card => card.CardType == DominionBase.Cards.Promotional.TypeClass.Stash));
										}

										ViewModel.CardListViewModel clvmHidden = new ViewModel.CardListViewModel();
										clvmHidden.ShowCards(showCards, Choice.Visibility);
										lbHidden.DataContext = clvmHidden;

										if (lbHidden.Items.Count >= _Choice.Minimum && lbHidden.Items.Count <= _Choice.Maximum)
											bOK.IsEnabled = true;
										else
											bOK.IsEnabled = false;
										break;
								}

								// "All" & "None" aren't valid for this case, either
								cbAutoClick.Visibility = bButtonDiv.Visibility = System.Windows.Visibility.Collapsed;
								bAll.Visibility = bNone.Visibility = System.Windows.Visibility.Collapsed;
							}

							break;

						case DominionBase.ChoiceType.Supplies:
							TextBlock tbSupplyNotification = new TextBlock();
							tbSupplyNotification.Text = "<-- Select a Supply pile to the left";
							tbSupplyNotification.FontSize = 14;
							wrapPanel1.Children.Add(tbSupplyNotification);
							foreach (SupplyControl sc in this.SupplyControls)
							{
								if (_Choice.Supplies.Values.Contains(sc.Supply))
								{
									if (_Choice.CardSource.CardType == DominionBase.Cards.Seaside.TypeClass.Embargo ||
										_Choice.CardSource.CardType == DominionBase.Cards.Prosperity.TypeClass.Contraband ||
										_Choice.CardSource.CardType == DominionBase.Cards.DarkAges.TypeClass.BandOfMisfits)
										sc.Clickability = SupplyVisibility.Selectable;
									else
										sc.Clickability = SupplyVisibility.Gainable;
									sc.SupplyClick += SupplyControl_SupplyClick;
								}
								else
									sc.Clickability = SupplyVisibility.NotClickable;
							}
							cbAutoClick.IsChecked = true;
							cbAutoClick.Visibility = bOK.Visibility = System.Windows.Visibility.Collapsed;
							if (Choice.Minimum > 0)
								wpButtons.Visibility = System.Windows.Visibility.Collapsed;
							break;

						case DominionBase.ChoiceType.SuppliesAndCards:
							foreach (DominionBase.Cards.Card card in _Choice.Cards)
							{
								ucICardButton icb = (ucICardButton)CreateButton(true, null, false);
								icb.ICard = card;
								icb.Tag = card;

								wrapPanel1.Children.Add(icb);
							}

							TextBlock tbSupplyCardNotification = new TextBlock();
							tbSupplyCardNotification.Text = "<-- Select a Supply pile to the left";
							if (_Choice.Cards.Count() > 0)
								tbSupplyCardNotification.Text += " or a card above";
							tbSupplyCardNotification.FontSize = 14;
							wrapPanel1.Children.Add(tbSupplyCardNotification);
							foreach (SupplyControl sc in this.SupplyControls)
							{
								if (_Choice.Supplies.Values.Contains(sc.Supply))
								{
									if (_Choice.CardSource.CardType == DominionBase.Cards.Intrigue.TypeClass.WishingWell ||
										_Choice.CardSource.CardType == DominionBase.Cards.DarkAges.TypeClass.BandOfMisfits ||
										_Choice.CardSource.CardType == DominionBase.Cards.DarkAges.TypeClass.Mystic ||
										_Choice.CardSource.CardType == DominionBase.Cards.DarkAges.TypeClass.Rebuild ||
										_Choice.CardSource.CardType == DominionBase.Cards.Guilds.TypeClass.Doctor || 
										_Choice.CardSource.CardType == DominionBase.Cards.Guilds.TypeClass.Journeyman)
										sc.Clickability = SupplyVisibility.Selectable;
									else
										sc.Clickability = SupplyVisibility.Gainable;
									sc.SupplyClick += SupplyControl_SupplyClick;
								}
								else
									sc.Clickability = SupplyVisibility.NotClickable;
							}
							cbAutoClick.IsChecked = true;
							if (Choice.Minimum > 0)
								wpButtons.Visibility = System.Windows.Visibility.Collapsed;
							break;
					}

					if (wrapPanel1.Visibility == System.Windows.Visibility.Visible)
					{
						if (_Choice.Minimum == 0)
							bNone.Visibility = Visibility.Visible;
						else
							bNone.Visibility = Visibility.Collapsed;

						if ((bAll.Visibility == System.Windows.Visibility.Collapsed && bNone.Visibility == System.Windows.Visibility.Collapsed) ||
							(bOK.Visibility == System.Windows.Visibility.Collapsed && cbAutoClick.Visibility == System.Windows.Visibility.Collapsed))
							bButtonDiv.Visibility = System.Windows.Visibility.Collapsed;
						else
							bButtonDiv.Visibility = System.Windows.Visibility.Visible;
					}

					if (wMain.Settings != null && cbAutoClick.Visibility == System.Windows.Visibility.Visible)
						cbAutoClick.IsChecked = wMain.Settings.Chooser_AutomaticallyClickWhenSatisfied;


					// Automatic chooser clicking!

					if (wMain.Settings != null && wMain.Settings.Chooser_AutomaticallyRevealMoat &&
						_Choice.EventArgs is DominionBase.Players.AttackedEventArgs &&
						_Choice.ChoiceType == DominionBase.ChoiceType.Cards &&
						_Choice.Text == "Reveal a card?" &&
						_Choice.Cards.Any(c => c.CardType == DominionBase.Cards.Base.TypeClass.Moat))
					{
						ucICardButton icbMoat = wrapPanel1.Children.OfType<ucICardButton>().FirstOrDefault(
							icb => icb.ICard is DominionBase.Cards.Card && icb.ICard.CardType == DominionBase.Cards.Base.TypeClass.Moat);
						if (icbMoat != null)
						{
							cbAutoClick.IsChecked = true;
							icbMoat.IsChecked = true;
							toggleButton_Checked(icbMoat, null);
						}
					}

					else if (wMain.Settings != null && wMain.Settings.Chooser_AutomaticallyRevealProvince &&
						_Choice.EventArgs == null && _Choice.CardSource != null &&
						_Choice.CardSource.CardType == DominionBase.Cards.Cornucopia.TypeClass.Tournament &&
						_Choice.ChoiceType == DominionBase.ChoiceType.Options &&
						_Choice.Text == "Do you want to reveal a Province from your hand?")
					{
						ToggleButton tbYes = wrapPanel1.Children.OfType<ToggleButton>().FirstOrDefault(
							tb => ((DominionBase.Option)((ToggleButton)tb).Tag).Text == "Yes");
						if (tbYes != null)
						{
							cbAutoClick.IsChecked = true;
							tbYes.IsChecked = true;
							toggleButton_Checked(tbYes, null);
						}
					}
				}
			}
		}

		private void SupplyControl_SupplyClick(object sender, RoutedEventArgs e)
		{
			DominionBase.Piles.Supply supply = (e.Source as SupplyControl).Supply;

			bOK.IsEnabled = false;

			this.ChoiceResult = new DominionBase.ChoiceResult(supply);

			if (this.ChoiceResult != null)
				bOK.IsEnabled = true;

			if (bOK.IsEnabled && this.ChoiceResult.Supply != null && cbAutoClick.IsChecked == true)
				bOK_Click(bOK, null);
		}


		void toggleButton_Checked(object sender, RoutedEventArgs e)
		{
			bool? isChecked = false;
			Object tag = null;
			if (sender is ToggleButton)
			{
				ToggleButton tb = sender as ToggleButton;
				isChecked = tb.IsChecked;
				tag = tb.Tag;
			}
			else if (sender is ucICardButton)
			{
				ucICardButton icb = sender as ucICardButton;
				isChecked = icb.IsChecked;
				tag = icb.Tag;
			}

			bOK.IsEnabled = false;

			switch (Choice.ChoiceType)
			{
				case DominionBase.ChoiceType.Options:
					if (this.ChoiceResult == null)
						this.ChoiceResult = new DominionBase.ChoiceResult(new List<String>());

					String option = ((DominionBase.Option)tag).Text;

					if (this.Choice.Maximum == 1)
					{
						foreach (ToggleButton chkdToggleButton in wrapPanel1.Children.OfType<ToggleButton>().Where(tb => this.ChoiceResult.Options.Contains(((DominionBase.Option)tb.Tag).Text)))
						{
							chkdToggleButton.IsChecked = false;
							this.ChoiceResult.Options.Remove(((DominionBase.Option)chkdToggleButton.Tag).Text);
						}
					}

					if (isChecked == true)
						this.ChoiceResult.Options.Add(option);
					else if (this.ChoiceResult.Options.Contains(option))
						this.ChoiceResult.Options.Remove(option);

					if ((Choice.Minimum > 0 || this.ChoiceResult.Options.Count > 0) &&
						this.ChoiceResult.Options.Count >= Choice.Minimum && 
						this.ChoiceResult.Options.Count <= Choice.Maximum)
						bOK.IsEnabled = true;
					break;

				case DominionBase.ChoiceType.Cards:
				case DominionBase.ChoiceType.SuppliesAndCards:
					//case DominionBase.ChoiceType.Pile:
					if (this.ChoiceResult == null)
						this.ChoiceResult = new DominionBase.ChoiceResult(new DominionBase.Cards.CardCollection());

					DominionBase.Cards.Card card = tag as DominionBase.Cards.Card;
					if (isChecked == true)
						this.ChoiceResult.Cards.Add(card);
					else if (this.ChoiceResult.Cards.Contains(card))
						this.ChoiceResult.Cards.Remove(card);

					if ((Choice.Minimum > 0 || this.ChoiceResult.Cards.Count > 0) && 
						this.ChoiceResult.Cards.Count >= Choice.Minimum && 
						this.ChoiceResult.Cards.Count <= Choice.Maximum)
						bOK.IsEnabled = true;
					break;

				case DominionBase.ChoiceType.Supplies:
					if (isChecked == true)
						this.ChoiceResult = new DominionBase.ChoiceResult(tag as DominionBase.Piles.Supply);
					else
						this.ChoiceResult = null;

					if (this.ChoiceResult != null)
						bOK.IsEnabled = true;
					break;
			}

			foreach (ToggleButton control in wrapPanel1.Children.OfType<ToggleButton>())
			{
				Panel pButtonText = (Panel)((Border)control.Content).Child;
				String option = ((DominionBase.Option)control.Tag).Text;
				if (Choice.IsOrdered)
				{
					pButtonText.Visibility = System.Windows.Visibility.Visible;

					String ordinal = String.Empty;
					switch (Choice.ChoiceType)
					{
						case DominionBase.ChoiceType.Options:
							if (this.ChoiceResult.Options.Contains(option))
								ordinal = Utilities.Ordinal(this.ChoiceResult.Options.IndexOf(option) + 1);
							break;
					}
					if (ordinal != String.Empty)
						(pButtonText.Children[0] as TextBlock).Text = String.Format("{0}: ", ordinal);
					else
						(pButtonText.Children[0] as TextBlock).Text = String.Empty;
				}
			}

			foreach (ucICardButton control in wrapPanel1.Children.OfType<ucICardButton>())
			{
				DominionBase.Cards.Card card = control.Tag as DominionBase.Cards.Card;
				if (Choice.IsOrdered)
				{
					String ordinal = String.Empty;
					switch (Choice.ChoiceType)
					{
						case DominionBase.ChoiceType.Cards:
							//case DominionBase.ChoiceType.Pile:
							if (this.ChoiceResult.Cards.Contains(card))
								control.Order = this.ChoiceResult.Cards.IndexOf(card) + 1;
							else
								control.Order = 0;
							break;
					}
				}
			}

			if (bOK.IsEnabled &&
				(this.ChoiceResult.Supply != null ||
				(this.ChoiceResult.Cards != null && (this.ChoiceResult.Cards.Count == Choice.Maximum || this.ChoiceResult.Cards.Count == this.Choice.Cards.Count())) ||
				(this.ChoiceResult.Options != null && (this.ChoiceResult.Options.Count == Choice.Maximum || this.ChoiceResult.Options.Count == this.Choice.Options.Count))
				) && cbAutoClick.IsChecked == true)
				bOK_Click(bOK, null);
		}

		private void SetToolTip(FrameworkElement element, DominionBase.ICard card)
		{
			ToolTip tt = new System.Windows.Controls.ToolTip();
			ToolTipCard ttc = new ToolTipCard();
			tt.Content = ttc;
			ToolTipService.SetShowOnDisabled(element, true);
			ToolTipService.SetHasDropShadow(element, true);
			if (wMain.Settings != null)
			{
				if (wMain.Settings.ToolTipShowDuration == ToolTipShowDuration.Off)
					ToolTipService.SetIsEnabled(element, false);
				else
				{
					ToolTipService.SetIsEnabled(element, true);
					ToolTipService.SetShowDuration(element, (int)wMain.Settings.ToolTipShowDuration);
				}
			}
			ToolTipService.SetInitialShowDelay(element, 2000);
			ToolTipService.SetBetweenShowDelay(element, 250);
			ttc.ICard = card;
			element.ToolTip = tt;
		}

		private FrameworkElement CreateButton(Boolean isICard, ICommand command, Boolean isRequired)
		{
			if (isICard)
			{
				ucICardButton icb = new ucICardButton();
				icb.ICardButtonClick += new RoutedEventHandler(toggleButton_Checked);
				icb.IsOrdered = Choice.IsOrdered;

				return icb;
			}
			else
			{
				ToggleButton tb = new ToggleButton()
				{
					Margin = new Thickness(5, 0, 5, 0),
					Height = double.NaN,
					Width = double.NaN,
					MinHeight = 40,
					MinWidth = 40,
					MaxHeight = 100,
					MaxWidth = 200,
					Background = (VisualBrush)FindResource("NormalBrush"),
					VerticalContentAlignment = System.Windows.VerticalAlignment.Stretch
				};
				Border border = new Border() { Padding = new Thickness(10, 0, 10, 0) };
				tb.Content = border;
				WrapPanel wp = new WrapPanel() { Orientation = Orientation.Horizontal, VerticalAlignment = System.Windows.VerticalAlignment.Center };
				border.Child = wp;
				if (isRequired)
				{
					border.BorderThickness = new Thickness(2);
					border.BorderBrush = Brushes.Crimson;
				}
				else
				{
					tb.ToolTip = "Optional";
				}
				wp.Children.Add(new TextBlock() { TextWrapping = TextWrapping.Wrap });
				wp.Children.Add(new AccessText() { Width = 0 });
				wp.Children.Add(new TextBlock() { TextWrapping = TextWrapping.Wrap });

				if (command == null)
					tb.Click += new RoutedEventHandler(toggleButton_Checked);
				else
					tb.Command = command;

				return tb;
			}
		}

		private void bOK_Click(object sender, RoutedEventArgs e)
		{
			((UIElement)sender).IsEnabled = false;
			if (this.ChoiceResult == null)
				this.ChoiceResult = new DominionBase.ChoiceResult();

			if (bReorder.Visibility == System.Windows.Visibility.Visible)
			{
				this.ChoiceResult = new DominionBase.ChoiceResult(new DominionBase.Cards.CardCollection());

				// Must add the cards back in reverse order
				foreach (ViewModel.CardViewModel cvm in (lbReorder.DataContext as ViewModel.CardListViewModel).Cards.Reverse())
					this.ChoiceResult.Cards.Add((DominionBase.Cards.Card)cvm.ICard);
			}
			else if (bHidden.Visibility == System.Windows.Visibility.Visible)
			{
				this.ChoiceResult = new DominionBase.ChoiceResult(new DominionBase.Cards.CardCollection());

				// Must add the cards back in reverse order
				foreach (ViewModel.CardViewModel cvm in (lbHidden.DataContext as ViewModel.CardListViewModel).Cards.Reverse())
					this.ChoiceResult.Cards.Add((DominionBase.Cards.Card)cvm.ICard);
			}

			foreach (SupplyControl sc in this.SupplyControls)
				sc.SupplyClick -= SupplyControl_SupplyClick;

			this._LastChoiceId = this.Choice.UniqueId;
			this._LastChoiceResult = this.ChoiceResult;

			RaiseEvent(new RoutedEventArgs(ChooserOKClickEvent));
			((UIElement)sender).IsEnabled = true;
		}

		private void bNone_Click(object sender, RoutedEventArgs e)
		{
			cbAutoClick.IsChecked = false;
			foreach (FrameworkElement control in wrapPanel1.Children)
			{
				if (control is ToggleButton)
				{
					((ToggleButton)control).IsChecked = false;
				}
				else if (control is ucICardButton)
				{
					((ucICardButton)control).IsChecked = false;
				}
				toggleButton_Checked(control, null);
			}
			bOK_Click(bOK, null);
		}

		private void bAll_Click(object sender, RoutedEventArgs e)
		{
			cbAutoClick.IsChecked = false;
			foreach (FrameworkElement control in wrapPanel1.Children)
			{
				if (control is ToggleButton)
					((ToggleButton)control).IsChecked = false;
				else if (control is ucICardButton)
					((ucICardButton)control).IsChecked = false;
				toggleButton_Checked(control, null);
			}
			foreach (FrameworkElement control in wrapPanel1.Children)
			{
				if (control is ToggleButton)
					((ToggleButton)control).IsChecked = true;
				else if (control is ucICardButton)
					((ucICardButton)control).IsChecked = true;
				toggleButton_Checked(control, null);
			}
			bOK_Click(bOK, null);
		}

		void lbReorder_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
		{
			if (_Choice == null)
				return;

			if (((ItemCollection)sender).Count >= _Choice.Minimum &&
				((ItemCollection)sender).Count <= _Choice.Maximum)
				bOK.IsEnabled = true;
			else
				bOK.IsEnabled = false;
		}
	}
}
