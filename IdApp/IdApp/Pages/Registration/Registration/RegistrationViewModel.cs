﻿using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows.Input;
using IdApp.Services.Tag;
using Xamarin.Forms;
using Xamarin.Forms.Internals;
using Command = Xamarin.Forms.Command;

namespace IdApp.Pages.Registration.Registration
{
	/// <summary>
	/// The view model to bind to for displaying a registration page or view to the user.
	/// </summary>
	public class RegistrationViewModel : BaseViewModel
	{
		private bool muteStepSync;

		/// <summary>
		/// Creates a new instance of the <see cref="RegistrationViewModel"/> class.
		/// </summary>
		private RegistrationViewModel()
		{
#if ATLANTICAPP
			this.GoToPrevCommand = new Command(() => this.GoToPrev(), () => (RegistrationStep)this.CurrentStep > RegistrationStep.GetPhoneNumber);
			this.CurrentStepChangedCommand = new Command(() => this.DoStepChanged());

			this.RegistrationSteps = new ObservableCollection<RegistrationStepViewModel>
			{
				this.AddChildViewModel(new Atlantic.GetPhoneNumberViewModel()),
				this.AddChildViewModel(new Atlantic.ValidatePhoneNumberViewModel()),
				this.AddChildViewModel(new Atlantic.GetPhotoImageViewModel(RegistrationStep.GetUserPhotoImage)),
				this.AddChildViewModel(new Atlantic.GetPhotoImageViewModel(RegistrationStep.GetIdFacePhotoImage)),
				this.AddChildViewModel(new Atlantic.GetPhotoImageViewModel(RegistrationStep.GetIdBackPhotoImage)),
				this.AddChildViewModel(new Atlantic.RegisterIdentityViewModel()),
				this.AddChildViewModel(new Atlantic.ValidateIdentityViewModel()),
				this.AddChildViewModel(new Atlantic.DefinePinViewModel())
			};
#else
			this.GoToPrevCommand = new Command(() => this.GoToPrev(), () => (RegistrationStep)this.CurrentStep > RegistrationStep.ValidateContactInfo);
			this.CurrentStepChangedCommand = new Command(() => this.DoStepChanged());

			this.RegistrationSteps = new ObservableCollection<RegistrationStepViewModel>
			{
				this.AddChildViewModel(new ValidateContactInfo.ValidateContactInfoViewModel()),
				this.AddChildViewModel(new ChooseAccount.ChooseAccountViewModel()),
				this.AddChildViewModel(new RegisterIdentity.RegisterIdentityViewModel()),
				this.AddChildViewModel(new ValidateIdentity.ValidateIdentityViewModel()),
				this.AddChildViewModel(new DefinePin.DefinePinViewModel())
			};
#endif
		}

		/// <summary>
		/// Creates a new instance of the <see cref="RegistrationViewModel"/> class.
		/// </summary>
		public static async Task<RegistrationViewModel> Create()
		{
			RegistrationViewModel Result = new();

			await Result.SyncTagProfileStep();
			Result.UpdateStepTitle();

			return Result;
		}

		/// <inheritdoc />
		protected override async Task OnInitialize()
		{
			await base.OnInitialize();

			this.RegistrationSteps.ForEach(x => x.StepCompleted += this.RegistrationStep_Completed);
			await this.SyncTagProfileStep();
		}

		/// <inheritdoc />
		protected override Task OnDispose()
		{
			this.RegistrationSteps.ForEach(x => x.StepCompleted -= this.RegistrationStep_Completed);

			return base.OnDispose();
		}

		#region Properties

		/// <summary>
		/// The list of steps needed to register a digital identity.
		/// </summary>
		public ObservableCollection<RegistrationStepViewModel> RegistrationSteps { get; }

		/// <summary>
		/// The command to bind to for moving backwards to the previous step in the registration process.
		/// </summary>
		public ICommand GoToPrevCommand { get; }

		/// <summary>
		/// An opportunity to make some initialisation on the page change.
		/// </summary>
		public ICommand CurrentStepChangedCommand { get; }

		/// <summary>
		/// See <see cref="CanGoBack"/>
		/// </summary>
		public static readonly BindableProperty CanGoBackProperty =
			BindableProperty.Create(nameof(CanGoBack), typeof(bool), typeof(RegistrationViewModel), default(bool));

		/// <summary>
		/// Gets or sets whether navigation back to the previous registration step can be performed.
		/// </summary>
		public bool CanGoBack
		{
			get => (bool)this.GetValue(CanGoBackProperty);
			set => this.SetValue(CanGoBackProperty, value);
		}

		/// <summary>
		/// See <see cref="CurrentStep"/>
		/// </summary>
		public static readonly BindableProperty CurrentStepProperty =
			BindableProperty.Create(nameof(CurrentStep), typeof(int), typeof(RegistrationViewModel), default(int), propertyChanged: (b, oldValue, newValue) =>
			{
				RegistrationViewModel viewModel = (RegistrationViewModel)b;
				viewModel.UpdateStepTitle();
				viewModel.CanGoBack = viewModel.GoToPrevCommand.CanExecute(null);
			});

		/// <summary>
		/// Gets or sets the current step from the list of <see cref="RegistrationSteps"/>.
		/// </summary>
		public int CurrentStep
		{
			get => (int)this.GetValue(CurrentStepProperty);
			set
			{
				if (!this.muteStepSync)
				{
					this.SetValue(CurrentStepProperty, value);
				}
			}
		}

		/// <summary>
		/// See <see cref="CurrentStep"/>
		/// </summary>
		public static readonly BindableProperty CurrentStepTitleProperty =
			BindableProperty.Create(nameof(CurrentStepTitle), typeof(string), typeof(RegistrationViewModel), default(string));

		/// <summary>
		/// The title of the current step. Displayed in the UI.
		/// </summary>
		public string CurrentStepTitle
		{
			get => (string)this.GetValue(CurrentStepTitleProperty);
			set => this.SetValue(CurrentStepTitleProperty, value);
		}

		#endregion

		/// <summary>
		/// Temporarily mutes the synchronization of the <see cref="CurrentStep"/> property.
		/// This is a hack to workaround a bug on Android.
		/// </summary>
		public void MuteStepSync()
		{
			this.muteStepSync = true;
		}

		/// <summary>
		/// Un-mutes the synchronization of the <see cref="CurrentStep"/> property. See <see cref="MuteStepSync"/>.
		/// This is a hack to workaround a bug on Android.
		/// </summary>
		public void UnMuteStepSync()
		{
			this.muteStepSync = false;
		}

		private void UpdateStepTitle()
		{
			this.CurrentStepTitle = this.RegistrationSteps[this.CurrentStep].Title;
		}

		/// <summary>
		/// An event handler for listening to completion of the different registration steps.
		/// </summary>
		/// <param name="Sender">The event sender.</param>
		/// <param name="e">The default event args.</param>
		protected internal async void RegistrationStep_Completed(object Sender, EventArgs e)
		{
			try
			{
				RegistrationStep Step = ((RegistrationStepViewModel)Sender).Step;

#if ATLANTICAPP
				switch (Step)
				{
					case RegistrationStep.GetPhoneNumber:
					case RegistrationStep.ValidatePhoneNumber:
					case RegistrationStep.GetUserPhotoImage:
					case RegistrationStep.GetIdFacePhotoImage:
					case RegistrationStep.GetIdBackPhotoImage:
						await this.SyncTagProfileStep();
						break;

				case RegistrationStep.RegisterIdentity:
					// User connected to an existing account (as opposed to creating a new one). Copy values from the legal identity.
					if (this.TagProfile.LegalIdentity is not null)
					{
						Atlantic.RegisterIdentityViewModel vm = (Atlantic.RegisterIdentityViewModel)this.RegistrationSteps[(int)RegistrationStep.RegisterIdentity];
						vm.PopulateFromTagProfile();
					}
					await this.SyncTagProfileStep();
					break;

					case RegistrationStep.ValidateIdentity:
						await this.SyncTagProfileStep();
						break;

					case RegistrationStep.Pin:
						await App.Current.SetAppShellPageAsync();
						break;

					default: // RegistrationStep.ValidateContactInfo
						await this.SyncTagProfileStep();
						break;
				}
#else
				switch (Step)
				{
					case RegistrationStep.Account:
						// User connected to an existing account (as opposed to creating a new one). Copy values from the legal identity.
						if (this.TagProfile.LegalIdentity is not null)
						{
							RegisterIdentity.RegisterIdentityViewModel vm = (RegisterIdentity.RegisterIdentityViewModel)this.RegistrationSteps[(int)RegistrationStep.RegisterIdentity];
							vm.PopulateFromTagProfile();
						}

						await this.SyncTagProfileStep();
						break;

					case RegistrationStep.RegisterIdentity:
						await this.SyncTagProfileStep();
						break;

					case RegistrationStep.ValidateIdentity:
						await this.SyncTagProfileStep();
						break;

					case RegistrationStep.Pin:
						await App.Current.SetAppShellPageAsync();
						break;

					default: // RegistrationStep.ValidateContactInfo
						await this.SyncTagProfileStep();
						break;
				}
#endif
			}
			catch (Exception Exception)
			{
				this.LogService?.LogException(Exception);
			}
		}

		private async void DoStepChanged()
		{
			await this.RegistrationSteps[this.CurrentStep].DoAssignProperties();
		}

		private async void GoToPrev()
		{
			try
			{
				RegistrationStep CurrentStep = (RegistrationStep)this.CurrentStep;

#if ATLANTICAPP
				switch (CurrentStep)
				{
					case RegistrationStep.ValidatePhoneNumber:
						this.RegistrationSteps[this.CurrentStep].ClearStepState();
						await this.TagProfile.ClearAccount();
						break;

						/*!!!
					case RegistrationStep.RegisterIdentity:
						this.RegistrationSteps[(int)RegistrationStep.ValidatePhoneNumber].ClearStepState();
						await this.TagProfile.ClearAccount(false);
						this.RegistrationSteps[(int)RegistrationStep.RegisterIdentity].ClearStepState();
						await this.TagProfile.ClearLegalIdentity();
						await this.TagProfile.InvalidatePhoneNumber();
						break;
						*/

					case RegistrationStep.ValidateIdentity:
						Atlantic.RegisterIdentityViewModel vm = (Atlantic.RegisterIdentityViewModel)this.RegistrationSteps[(int)RegistrationStep.RegisterIdentity];
						vm.PopulateFromTagProfile();
						this.RegistrationSteps[this.CurrentStep].ClearStepState();
						await this.TagProfile.ClearIsValidated();
						break;

					case RegistrationStep.Pin:
						this.RegistrationSteps[this.CurrentStep].ClearStepState();
						await this.TagProfile.RevertPinStep();
						break;

					default: // RegistrationStep.Operator
						await this.TagProfile.ClearDomain();
						break;
				}
#else
				switch (CurrentStep)
				{
					case RegistrationStep.Account:
						this.RegistrationSteps[this.CurrentStep].ClearStepState();
						await this.TagProfile.ClearAccount();
						break;

					case RegistrationStep.RegisterIdentity:
						this.RegistrationSteps[(int)RegistrationStep.Account].ClearStepState();
						await this.TagProfile.ClearAccount(false);
						this.RegistrationSteps[(int)RegistrationStep.RegisterIdentity].ClearStepState();
						await this.TagProfile.ClearLegalIdentity();
						await this.TagProfile.InvalidateContactInfo();
						break;

					case RegistrationStep.ValidateIdentity:
						RegisterIdentity.RegisterIdentityViewModel vm = (RegisterIdentity.RegisterIdentityViewModel)this.RegistrationSteps[(int)RegistrationStep.RegisterIdentity];
						vm.PopulateFromTagProfile();
						this.RegistrationSteps[this.CurrentStep].ClearStepState();
						await this.TagProfile.ClearIsValidated();
						break;

					case RegistrationStep.Pin:
						this.RegistrationSteps[this.CurrentStep].ClearStepState();
						await this.TagProfile.RevertPinStep();
						break;

					default: // RegistrationStep.Operator
						await this.TagProfile.ClearDomain();
						break;
				}
#endif

				await this.SyncTagProfileStep();
			}
			catch (Exception ex)
			{
				await this.UiSerializer.DisplayAlert(ex);
			}
		}

		private async Task SyncTagProfileStep()
		{
			if (this.TagProfile.Step == RegistrationStep.Complete)
				await App.Current.SetAppShellPageAsync();
			else
				this.CurrentStep = (int)this.TagProfile.Step;
		}
	}
}
