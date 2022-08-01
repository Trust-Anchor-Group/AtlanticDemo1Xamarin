﻿using System.Threading.Tasks;

namespace IdApp.Pages
{
	/// <summary>
	/// Interfaces for linkable views.
	/// </summary>
	public interface ILinkableView
	{
		/// <summary>
		/// If the current view is linkable.
		/// </summary>
		bool IsLinkable { get; }

		/// <summary>
		/// Link to the current view
		/// </summary>
		string Link { get; }

		/// <summary>
		/// Title of the current view
		/// </summary>
		Task<string> Title { get; }

		/// <summary>
		/// If linkable view has media associated with link.
		/// </summary>
		bool HasMedia { get; }

		/// <summary>
		/// Encoded media, if available.
		/// </summary>
		byte[] Media { get; }

		/// <summary>
		/// Content-Type of associated media.
		/// </summary>
		string MediaContentType { get; }
	}
}