using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

using GongSolutions.Wpf.DragDrop;

namespace Dominion.NET_WPF.ViewModel
{
	class CardListViewModel : IDropTarget
	{
		public Boolean PreserveSourceOrdering { get; set; }
		public ObservableCollection<CardViewModel> Cards { get; set; }
		public void ShowCards(IEnumerable<DominionBase.Cards.Card> cards, DominionBase.Piles.Visibility visibility)
		{
			int index = 0;
			Cards = new ObservableCollection<CardViewModel>();
			foreach (DominionBase.Cards.Card card in cards)
				Cards.Add(new CardViewModel { CardName = card.Name, ICard = card, Visibility = visibility, OriginalIndex = index++ });
		}

		public CardListViewModel()
		{
			Cards = new ObservableCollection<CardViewModel>();
		}

		void IDropTarget.DragOver(DropInfo dropInfo)
		{
			CardViewModel sourceItem = dropInfo.Data as CardViewModel;
			CardViewModel targetItem = dropInfo.TargetItem as CardViewModel;

			if (sourceItem != null && (targetItem != null || dropInfo.TargetCollection != null))
			{
				dropInfo.Effects = DragDropEffects.Move;
				if (!this.PreserveSourceOrdering)
					dropInfo.DropTargetAdorner = DropTargetAdorners.Insert;
			}
			if (targetItem == null)
			{
			}
		}

		void IDropTarget.Drop(DropInfo dropInfo)
		{
			if (dropInfo.DragInfo == null)
				return;

			ListBox lbSource = dropInfo.DragInfo.VisualSource as ListBox;
			ListBox lbTarget = dropInfo.VisualTarget as ListBox;

			int insertIndex = dropInfo.InsertIndex;
			IList<CardViewModel> sourceList = null;
			IList<CardViewModel> destinationList = GetList(dropInfo.TargetCollection);
			IEnumerable<CardViewModel> data = ExtractData(dropInfo.Data);

			if (lbSource != null && lbTarget != null && lbSource.DataContext.GetType() == lbTarget.DataContext.GetType())
				sourceList = GetList(dropInfo.DragInfo.SourceCollection);

			if (this.PreserveSourceOrdering)
			{
				foreach (CardViewModel o in data)
				{
					sourceList.Remove(o);

					insertIndex = -1;
					if (destinationList.Count > 0)
						insertIndex = destinationList.Max(cvm => cvm.OriginalIndex < o.OriginalIndex ? destinationList.IndexOf(cvm) : -1);
					if (insertIndex == -1)
						destinationList.Insert(0, o);
					else if (insertIndex == destinationList.Count - 1)
						destinationList.Add(o);
					else
						destinationList.Insert(insertIndex + 1, o);
				}
			}
			else
			{
				if (sourceList != null)
				{
					foreach (CardViewModel o in data)
					{
						int index = sourceList.IndexOf(o);

						if (index != -1)
						{
							sourceList.RemoveAt(index);

							if (sourceList == destinationList && index < insertIndex)
							{
								--insertIndex;
							}
						}
					}
				}

				foreach (CardViewModel o in data)
					destinationList.Insert(insertIndex++, o);
			}
		}

		protected static IEnumerable<CardViewModel> ExtractData(object data)
		{
			if (data is IEnumerable<CardViewModel> && !(data is string))
				return (IEnumerable<CardViewModel>)data;
			else
				return Enumerable.Repeat<CardViewModel>((CardViewModel)data, 1);
		}

		protected static IList<CardViewModel> GetList(IEnumerable enumerable)
		{
			if (enumerable is ICollectionView)
				return ((ICollectionView)enumerable).SourceCollection as IList<CardViewModel>;
			else
				return enumerable as IList<CardViewModel>;
		}
	}
}