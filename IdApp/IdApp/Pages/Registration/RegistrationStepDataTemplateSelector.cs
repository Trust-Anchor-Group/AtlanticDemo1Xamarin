using IdApp.Services.Tag;
using Xamarin.Forms;

namespace IdApp.Pages.Registration
{
    /// <summary>
    /// A data template selector for displaying various types of registration steps.
    /// </summary>
    public class RegistrationStepDataTemplateSelector : DataTemplateSelector
    {
#if ATLANTICAPP
		/// <summary>
		/// The template for geting the phone number.
		/// </summary>
		public DataTemplate GetPhoneNumber { get; set; }

		/// <summary>
		/// The template for validating the phone number.
		/// </summary>
		public DataTemplate ValidatePhoneNumber { get; set; }

        /// <summary>
        /// The photo image template.
        /// </summary>
        public DataTemplate GetPhotoImage { get; set; }

		/// <summary>
		/// The register identity template.
		/// </summary>
		public DataTemplate RegisterIdentity { get; set; }

		/// <summary>
		/// The validate identity template.
		/// </summary>
		public DataTemplate ValidateIdentity { get; set; }

		/// <summary>
		/// The define pin template.
		/// </summary>
		public DataTemplate DefinePin { get; set; }
#else
        /// <summary>
        /// The template for validating contact information.
        /// </summary>
        public DataTemplate ValidateContactInfo { get; set; }

        /// <summary>
        /// The choose account template.
        /// </summary>
        public DataTemplate ChooseAccount { get; set; }

        /// <summary>
        /// The register identity template.
        /// </summary>
        public DataTemplate RegisterIdentity { get; set; }

        /// <summary>
        /// The validate identity template.
        /// </summary>
        public DataTemplate ValidateIdentity { get; set; }

        /// <summary>
        /// The define pin template.
        /// </summary>
        public DataTemplate DefinePin { get; set; }
#endif

		/// <summary>
		/// Chooses the best matching data template based on the type of registration step.
		/// </summary>
		/// <param name="item">The step to display.</param>
		/// <param name="container"></param>
		/// <returns>Selected template</returns>
		protected override DataTemplate OnSelectTemplate(object item, BindableObject container)
        {
            RegistrationStepViewModel viewModel = (RegistrationStepViewModel)item;

#if ATLANTICAPP
			return viewModel.Step switch
			{
				RegistrationStep.GetPhoneNumber => this.GetPhoneNumber,
				RegistrationStep.ValidatePhoneNumber => this.ValidatePhoneNumber,
				RegistrationStep.GetUserPhotoImage => this.GetPhotoImage,
				RegistrationStep.GetIdFacePhotoImage => this.GetPhotoImage,
				RegistrationStep.GetIdBackPhotoImage => this.GetPhotoImage,
				RegistrationStep.RegisterIdentity => this.RegisterIdentity,
				RegistrationStep.ValidateIdentity => this.ValidateIdentity,
				RegistrationStep.Pin => this.DefinePin,
				_ => this.GetPhoneNumber,
			};
#else
			return viewModel.Step switch
			{
				RegistrationStep.Account => this.ChooseAccount,
				RegistrationStep.RegisterIdentity => this.RegisterIdentity,
				RegistrationStep.ValidateIdentity => this.ValidateIdentity,
				RegistrationStep.Pin => this.DefinePin,
				_ => this.ValidateContactInfo,
			};
#endif
		}
	}
}
