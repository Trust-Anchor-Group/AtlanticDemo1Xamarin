namespace IdApp.Resx
{
	/// <summary>
	/// A list of svg resources
	/// </summary>
	public static class Svgs
	{
#if ATLANTICAPP
		/// <summary>
		/// Fingerptint icon
		/// </summary>
		public const string Fingerprint = "resource://IdApp.Resx.Svgs.fingerprint.svg";

		/// <summary>
		/// The camera icon
		/// </summary>
		public const string Camera = "resource://IdApp.Resx.Svgs.camera.svg";

		/// <summary>
		/// The chevron right icon
		/// </summary>
		public const string ChevronRight = "resource://IdApp.Resx.Svgs.chevron-right.svg";

		/// <summary>
		/// </summary>
		public const string AcceptedFilled = "resource://IdApp.Resx.Svgs.check_filled.svg";

		/// <summary>
		/// </summary>
		public const string RejectedFilled = "resource://IdApp.Resx.Svgs.x_filled.svg";
#else
		/// <summary>
		/// The passport icon
		/// </summary>
		public const string Passport = "resource://IdApp.Resx.Svgs.passport.svg";

		/// <summary>
		/// The images icon
		/// </summary>
		public const string Images = "resource://IdApp.Resx.Svgs.images.svg";

		/// <summary>
		/// The camera icon
		/// </summary>
		public const string Camera = "resource://IdApp.Resx.Svgs.camera.svg";

		/// <summary>
		/// The trash icon
		/// </summary>
		public const string Trash = "resource://IdApp.Resx.Svgs.trash.svg";

		/// <summary>
		/// The x-mark icon
		/// </summary>
		public const string Xmark = "resource://IdApp.Resx.Svgs.xmark.svg";

		/// <summary>
		/// The check icon
		/// </summary>
		public const string Check = "resource://IdApp.Resx.Svgs.check.svg";
#endif
	}
}
