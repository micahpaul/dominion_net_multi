using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;

namespace Dominion.NET_WPF.ViewModel
{
	class AIListViewModel
	{
		public ObservableCollection<AIViewModel> AIs { get; set; }
		public void ShowAIs(IEnumerable<Type> ais)
		{
			AIs = new ObservableCollection<AIViewModel>();
			foreach (Type type in ais)
				AIs.Add(new AIViewModel { AI = type, IsChecked = false });
		}

		public AIListViewModel()
		{
			AIs = new ObservableCollection<AIViewModel>();
		}

		public AIListViewModel(IEnumerable<Type> ais, List<String> allowedAIs)
		{
			AIs = new ObservableCollection<AIViewModel>();
			foreach (Type type in ais)
				AIs.Add(new AIViewModel { AI = type, IsChecked = allowedAIs.Contains(type.FullName) });
		}

		protected static IEnumerable<AIViewModel> ExtractData(object data)
		{
			if (data is IEnumerable<AIViewModel> && !(data is string))
				return (IEnumerable<AIViewModel>)data;
			else
				return Enumerable.Repeat<AIViewModel>((AIViewModel)data, 1);
		}

		protected static IList<AIViewModel> GetList(IEnumerable enumerable)
		{
			if (enumerable is ICollectionView)
				return ((ICollectionView)enumerable).SourceCollection as IList<AIViewModel>;
			else
				return enumerable as IList<AIViewModel>;
		}
	}
}
