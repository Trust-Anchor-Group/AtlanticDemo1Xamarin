using System.Linq;
using Xamarin.Forms;
using IdApp.Controls.Extended;
using System.Data.SqlTypes;

namespace IdApp.Helpers
{
	/// <summary>
	/// PasswordMask is a class which defines <c>IsEnabled</c> attached <see cref="BindableProperty"/> used to store the state
	/// of <see cref="Entry"/> password masking.
	/// </summary>
	public class PasswordMask : BindableObject
	{
		/// <summary>
		/// Implements the attached property that indicates if the <see cref="Entry"/> password must be masked.
		/// </summary>
		public static readonly BindableProperty IsEnabledProperty = BindableProperty.CreateAttached("IsEnabled", typeof(bool), typeof(PasswordMask),
			defaultValue: true,
			propertyChanged: (bindable, oldValue, newValue) =>
			{
				Entry Control = null;

				if (bindable is Entry Entry)
				{
					Control = Entry;
				}
				else if (bindable is ExEntry ExEntry)
				{
					Control = ExEntry.GetEntry();
				}

				if (Control is not null)
				{
					string TogglerEffectName = $"{Constants.Effects.ResolutionGroupName}.{Constants.Effects.PasswordMaskTogglerEffect}";
					if (!Control.Effects.Any(Effect => Effect.ResolveId == TogglerEffectName))
					{
						Effect Effect = Effect.Resolve(TogglerEffectName);
						Control.Effects.Add(Effect);
					}
				}
			});

		/// <summary>
		/// Gets the value indicating if the password associated with <paramref name="bindable"/> must be masked.
		/// </summary>
		public static bool GetIsEnabled(BindableObject bindable)
		{
			return (bool)bindable.GetValue(IsEnabledProperty);
		}

		/// <summary>
		/// Sets the value indicating if the password associated with <paramref name="bindable"/> must be masked.
		/// </summary>
		public static void SetIsEnabled(BindableObject bindable, bool value)
		{
			bindable.SetValue(IsEnabledProperty, value);
		}
	}
}
