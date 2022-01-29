﻿using IdApp.Cv.Arithmetics;

namespace IdApp.Cv.Transformations
{
	/// <summary>
	/// Static class for Transformation Operations, implemented as extensions.
	/// </summary>
	public static partial class TransformationOperations
	{
		/// <summary>
		/// Improves contrast by setting the range of visible values.
		/// </summary>
		/// <param name="M">Matrix of pixel values</param>
		/// <param name="Min">Smallest value after contrast transformation.</param>
		/// <param name="Max">Largest value after contrast transformation.</param>
		public static void Contrast(this Matrix<float> M, float Min, float Max)
		{
			M.ScalarLinearTransform(-Min, 1f / (Max - Min), 0);
		}
	}
}