using System;
using System.Collections.Generic;
using System.Security.Cryptography;

namespace DominionBase.Utilities
{
	public static class Shuffler
	{
		/// <summary>
		/// Performs an in-place shuffle of the list provided using strong crypto to (virtually) guarantee a randomized shuffling
		/// </summary>
		/// <typeparam name="T">Type of object to be shuffled (unimportant for this method)</typeparam>
		/// <param name="list">List of items to be shuffled</param>
		public static void Shuffle<T>(IList<T> list)
		{
			RNGCryptoServiceProvider provider = new RNGCryptoServiceProvider();
			int n = list.Count;
			while (n > 1)
			{
				byte[] box = new byte[1];
				do provider.GetBytes(box);
				while (!(box[0] < n * (Byte.MaxValue / n)));
				int k = (box[0] % n);
				n--;
				T value = list[k];
				list[k] = list[n];
				list[n] = value;
			}
		}

		/// <summary>
		/// Chooses a random item from the list provided, using strong crypto to (virtually) guarantee a natural distribution
		/// </summary>
		/// <typeparam name="T">Type of object to be chosen at random</typeparam>
		/// <param name="list">List of items to choose from</param>
		/// <returns>An item from the list, chosen at random</returns>
		public static T Choose<T>(IList<T> list)
		{
			RNGCryptoServiceProvider provider = new RNGCryptoServiceProvider();

			byte[] box = new byte[1];
			do provider.GetBytes(box);
			while (!(box[0] < list.Count * (Byte.MaxValue / list.Count)));
			int k = (box[0] % list.Count);

			return list[k];
		}
	}

	public static class Gaussian
	{
		private static Boolean _HaveNextNextGaussian = false;
		private static Double _NextNextGaussian = 0d;
		private static Random _Random = null;

		/// <summary>
		/// Returns a random number with a Gaussian distribution with a mean of 0 and a standard deviation of 1
		/// </summary>
		/// <returns>A random number with a Gaussian distribution with a mean of 0 and a standard deviation of 1</returns>
		public static Double NextGaussian()
		{
			return NextGaussian(null);
		}

		/// <summary>
		/// Returns a random number with a Gaussian distribution with a mean of 0 and a standard deviation of 1
		/// </summary>
		/// <param name="rng">The Random object to use instead of the internal one (e.g. if you need to control the seed)</param>
		/// <returns>A random number with a Gaussian distribution with a mean of 0 and a standard deviation of 1</returns>
		public static Double NextGaussian(Random rng)
		{
			if (_HaveNextNextGaussian)
			{
				_HaveNextNextGaussian = false;
				return _NextNextGaussian;
			}
			else
			{
				if (_Random == null && rng == null)
					_Random = new Random();
				double v1, v2, s;
				do
				{
					if (rng == null)
					{
						v1 = 2.0 * _Random.NextDouble() - 1;   // between -1.0 and 1.0
						v2 = 2.0 * _Random.NextDouble() - 1;   // between -1.0 and 1.0
					}
					else
					{
						lock (rng)
						{
							v1 = 2.0 * rng.NextDouble() - 1;   // between -1.0 and 1.0
							v2 = 2.0 * rng.NextDouble() - 1;   // between -1.0 and 1.0
						}
					}
					s = v1 * v1 + v2 * v2;
				} while (s >= 1 || s == 0);
				double multiplier = Math.Sqrt(-2 * Math.Log(s) / s);
				_NextNextGaussian = v2 * multiplier;
				_HaveNextNextGaussian = true;
				return v1 * multiplier;
			}
		}
	}
}
