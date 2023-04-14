using IdApp.Extensions;
using IdApp.Services.Data.PersonalNumbers;
using IdApp.Services.Tag;
using IdApp.Services.Data.Countries;
using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows.Input;
using Waher.Networking.XMPP;
using Waher.Networking.XMPP.Contracts;
using Xamarin.Forms;
using IdApp.Services;
using Xamarin.CommunityToolkit.Helpers;

namespace IdApp.Pages.Registration.Atlantic
{
	/// <summary>
	/// The view model to bind to when showing Step 3 of the registration flow: registering an identity.
	/// </summary>
	public class RegisterIdentityViewModel : RegistrationStepViewModel
	{
		/// <summary>
		/// Creates a new instance of the <see cref="RegisterIdentityModel"/> class.
		/// </summary>
		public RegisterIdentityViewModel()
		 : base(RegistrationStep.RegisterIdentity)
		{
			this.Countries = new ObservableCollection<string>();
			foreach (string country in ISO_3166_1.Countries)
				this.Countries.Add(country);

			this.SelectedCountry = null;
			this.RegisterCommand = new Command(async _ => await this.Register(), _ => this.CanRegister);

			this.Title = LocalizationResourceManager.Current["PersonalLegalInformation"];
			this.PersonalNumberPlaceholder = LocalizationResourceManager.Current["PersonalNumber"];
		}

		/// <inheritdoc />
		protected override async Task OnInitialize()
		{
			await base.OnInitialize();

			this.XmppService.ConnectionStateChanged += this.XmppService_ConnectionStateChanged;

			await this.XmppService_ConnectionStateChanged(this, this.XmppService.State);
		}

		/// <inheritdoc />
		protected override async Task OnDispose()
		{
			this.XmppService.ConnectionStateChanged -= this.XmppService_ConnectionStateChanged;

			await base.OnDispose();
		}

		#region Properties

		/// <summary>
		/// True if the user choose the educational or experimental purpose.
		/// </summary>
		public bool IsTest => this.TagProfile.IsTest;

		/// <summary>
		/// The command to bind to for performing the 'register' action.
		/// </summary>
		public ICommand RegisterCommand { get; }

		/// <summary>
		/// The list of all available countries a user can select from.
		/// </summary>
		public ObservableCollection<string> Countries { get; }

		/// <summary>
		/// </summary>
		public static readonly BindableProperty SelectedCountryProperty =
			BindableProperty.Create(nameof(SelectedCountry), typeof(string), typeof(RegisterIdentityViewModel), default(string), propertyChanged: (b, oldValue, newValue) =>
			{
				RegisterIdentityViewModel ViewModel = (RegisterIdentityViewModel)b;
				ViewModel.CanRegister = ViewModel.TryCanRegister().GetAwaiter().GetResult();

				if (!string.IsNullOrWhiteSpace(ViewModel.SelectedCountry) &&
					ISO_3166_1.TryGetCode(ViewModel.SelectedCountry, out string CountryCode))
				{
					string format = PersonalNumberSchemes.DisplayStringForCountry(CountryCode);
					if (!string.IsNullOrWhiteSpace(format))
						ViewModel.PersonalNumberPlaceholder = string.Format(LocalizationResourceManager.Current["PersonalNumberPlaceholder"], format);
					else
						ViewModel.PersonalNumberPlaceholder = LocalizationResourceManager.Current["PersonalNumber"];
				}
				else
					ViewModel.PersonalNumberPlaceholder = LocalizationResourceManager.Current["PersonalNumber"];
			});

		/// <summary>
		/// The user selected country from the list of <see cref="Countries"/>.
		/// </summary>
		public string SelectedCountry
		{
			get => (string)this.GetValue(SelectedCountryProperty);
			set => this.SetValue(SelectedCountryProperty, value);
		}

		/// <summary>
		/// The <see cref="FirstName"/>
		/// </summary>
		public static readonly BindableProperty FirstNameProperty =
			BindableProperty.Create(nameof(FirstName), typeof(string), typeof(RegisterIdentityViewModel), default(string), propertyChanged: OnPropertyChanged);

		/// <summary>
		/// The user's first name
		/// </summary>
		public string FirstName
		{
			get => (string)this.GetValue(FirstNameProperty);
			set => this.SetValue(FirstNameProperty, value);
		}

		/// <summary>
		/// The <see cref="MiddleNames"/>
		/// </summary>
		public static readonly BindableProperty MiddleNamesProperty =
			BindableProperty.Create(nameof(MiddleNames), typeof(string), typeof(RegisterIdentityViewModel), default(string), propertyChanged: OnPropertyChanged);

		/// <summary>
		/// The user's middle name(s)
		/// </summary>
		public string MiddleNames
		{
			get => (string)this.GetValue(MiddleNamesProperty);
			set => this.SetValue(MiddleNamesProperty, value);
		}

		/// <summary>
		/// The <see cref="LastNames"/>
		/// </summary>
		public static readonly BindableProperty LastNamesProperty =
			BindableProperty.Create(nameof(LastNames), typeof(string), typeof(RegisterIdentityViewModel), default(string), propertyChanged: OnPropertyChanged);

		/// <summary>
		/// The user's last name(s)
		/// </summary>
		public string LastNames
		{
			get => (string)this.GetValue(LastNamesProperty);
			set => this.SetValue(LastNamesProperty, value);
		}

		/// <summary>
		/// The <see cref="PersonalNumber"/>
		/// </summary>
		public static readonly BindableProperty PersonalNumberProperty =
			BindableProperty.Create(nameof(PersonalNumber), typeof(string), typeof(RegisterIdentityViewModel), default(string), propertyChanged: OnPropertyChanged);

		/// <summary>
		/// The user's personal number
		/// </summary>
		public string PersonalNumber
		{
			get => (string)this.GetValue(PersonalNumberProperty);
			set => this.SetValue(PersonalNumberProperty, value);
		}

		/// <summary>
		/// The <see cref="PersonalNumberPlaceholder"/>
		/// </summary>
		public static readonly BindableProperty PersonalNumberPlaceholderProperty =
			BindableProperty.Create(nameof(PersonalNumberPlaceholder), typeof(string), typeof(RegisterIdentityViewModel), default(string));

		/// <summary>
		/// The personal number placeholder, used as a guide to the user to enter the correct format, which depends on the <see cref="SelectedCountry"/>.
		/// </summary>
		public string PersonalNumberPlaceholder
		{
			get => (string)this.GetValue(PersonalNumberPlaceholderProperty);
			set => this.SetValue(PersonalNumberPlaceholderProperty, value);
		}

		/// <summary>
		/// The <see cref="Address"/>
		/// </summary>
		public static readonly BindableProperty AddressProperty =
			BindableProperty.Create(nameof(Address), typeof(string), typeof(RegisterIdentityViewModel), default(string), propertyChanged: OnPropertyChanged);

		/// <summary>
		/// The user's address, line 1.
		/// </summary>
		public string Address
		{
			get => (string)this.GetValue(AddressProperty);
			set => this.SetValue(AddressProperty, value);
		}

		/// <summary>
		/// The <see cref="Address2"/>
		/// </summary>
		public static readonly BindableProperty Address2Property =
			BindableProperty.Create(nameof(Address2), typeof(string), typeof(RegisterIdentityViewModel), default(string));

		/// <summary>
		/// The user's address, line 2.
		/// </summary>
		public string Address2
		{
			get => (string)this.GetValue(Address2Property);
			set => this.SetValue(Address2Property, value);
		}

		/// <summary>
		/// The <see cref="ZipCode"/>
		/// </summary>
		public static readonly BindableProperty ZipCodeProperty =
			BindableProperty.Create(nameof(ZipCode), typeof(string), typeof(RegisterIdentityViewModel), default(string), propertyChanged: OnPropertyChanged);

		/// <summary>
		/// The user's zip code
		/// </summary>
		public string ZipCode
		{
			get => (string)this.GetValue(ZipCodeProperty);
			set => this.SetValue(ZipCodeProperty, value);
		}

		/// <summary>
		/// The <see cref="Area"/>
		/// </summary>
		public static readonly BindableProperty AreaProperty =
			BindableProperty.Create(nameof(Area), typeof(string), typeof(RegisterIdentityViewModel), default(string));

		/// <summary>
		/// The user's area
		/// </summary>
		public string Area
		{
			get => (string)this.GetValue(AreaProperty);
			set => this.SetValue(AreaProperty, value);
		}

		/// <summary>
		/// The <see cref="City"/>
		/// </summary>
		public static readonly BindableProperty CityProperty =
			BindableProperty.Create(nameof(City), typeof(string), typeof(RegisterIdentityViewModel), default(string), propertyChanged: OnPropertyChanged);

		/// <summary>
		/// The user's city
		/// </summary>
		public string City
		{
			get => (string)this.GetValue(CityProperty);
			set => this.SetValue(CityProperty, value);
		}

		/// <summary>
		/// The <see cref="Region"/>
		/// </summary>
		public static readonly BindableProperty RegionProperty =
			BindableProperty.Create(nameof(Region), typeof(string), typeof(RegisterIdentityViewModel), default(string));

		/// <summary>
		/// The user's region
		/// </summary>
		public string Region
		{
			get => (string)this.GetValue(RegionProperty);
			set => this.SetValue(RegionProperty, value);
		}

		/// <summary>
		/// The user's legal identity, set when the registration has occurred.
		/// </summary>
		public LegalIdentity LegalIdentity { get; private set; }

		/// <summary>
		/// </summary>
		public static readonly BindableProperty IsConnectedProperty =
			BindableProperty.Create(nameof(IsConnected), typeof(bool), typeof(RegisterIdentityViewModel), default(bool));

		/// <summary>
		/// Gets or sets whether the app is connected to an XMPP server.
		/// </summary>
		public bool IsConnected
		{
			get => (bool)this.GetValue(IsConnectedProperty);
			set => this.SetValue(IsConnectedProperty, value);
		}

		/// <summary>
		/// The <see cref="ConnectionStateText"/>
		/// </summary>
		public static readonly BindableProperty ConnectionStateTextProperty =
			BindableProperty.Create(nameof(ConnectionStateText), typeof(string), typeof(RegisterIdentityViewModel), default(string));

		/// <summary>
		/// The user friendly connection state text to display to the user.
		/// </summary>
		public string ConnectionStateText
		{
			get => (string)this.GetValue(ConnectionStateTextProperty);
			set => this.SetValue(ConnectionStateTextProperty, value);
		}

		/// <summary>
		/// </summary>
		public static readonly BindableProperty CanRegisterProperty =
			BindableProperty.Create(nameof(CanRegister), typeof(bool), typeof(RegisterIdentityViewModel), false);

		/// <summary>
		/// </summary>
		public bool CanRegister
		{
			get => (bool)this.GetValue(CanRegisterProperty);
			set => this.SetValue(CanRegisterProperty, value);
		}

		#endregion

		private async Task<bool> TryCanRegister()
		{
			bool Validated = await this.ValidateInput(false);
			bool CanExecute = Validated && this.XmppService.IsOnline;
			return CanExecute;
		}

		private Task XmppService_ConnectionStateChanged(object _, XmppState NewState)
		{
			this.UiSerializer.BeginInvokeOnMainThread(() =>
			{
				this.SetConnectionStateAndText(NewState);
			});

			return Task.CompletedTask;
		}

		private static void OnPropertyChanged(BindableObject b, object oldValue, object newValue)
		{
			RegisterIdentityViewModel viewModel = (RegisterIdentityViewModel)b;
			viewModel.CanRegister = viewModel.TryCanRegister().GetAwaiter().GetResult();
		}

		private void SetConnectionStateAndText(XmppState state)
		{
			this.IsConnected = state == XmppState.Connected;
			this.ConnectionStateText = state.ToDisplayText();
		}

		private async Task Register()
		{
			if (!(await this.ValidateInput(true)))
				return;

			string CountryCode = ISO_3166_1.ToCode(this.SelectedCountry);
			string PnrBeforeValidation = this.PersonalNumber.Trim();
			NumberInformation NumberInfo = await PersonalNumberSchemes.Validate(CountryCode, PnrBeforeValidation);

			if (NumberInfo.IsValid.HasValue && !NumberInfo.IsValid.Value)
			{
				if (string.IsNullOrWhiteSpace(NumberInfo.DisplayString))
					await this.UiSerializer.DisplayAlert(LocalizationResourceManager.Current["ErrorTitle"], LocalizationResourceManager.Current["PersonalNumberDoesNotMatchCountry"]);
				else
					await this.UiSerializer.DisplayAlert(LocalizationResourceManager.Current["ErrorTitle"], LocalizationResourceManager.Current["PersonalNumberDoesNotMatchCountry_ExpectedFormat"] + NumberInfo.DisplayString);

				return;
			}

			if (NumberInfo.PersonalNumber != PnrBeforeValidation)
				this.PersonalNumber = NumberInfo.PersonalNumber;

			if (string.IsNullOrWhiteSpace(this.TagProfile.LegalJid))
			{
				await this.UiSerializer.DisplayAlert(LocalizationResourceManager.Current["ErrorTitle"], LocalizationResourceManager.Current["OperatorDoesNotSupportLegalIdentitiesAndSmartContracts"]);
				return;
			}

			if (!this.XmppService.IsOnline)
			{
				await this.UiSerializer.DisplayAlert(LocalizationResourceManager.Current["ErrorTitle"], LocalizationResourceManager.Current["NotConnectedToTheOperator"]);
				return;
			}

			this.SetIsBusy(this.RegisterCommand);

			try
			{
				RegisterIdentityModel IdentityModel = this.CreateRegisterModel();
				LegalIdentityAttachment[] Photos = this.TagProfile.LegalPhotos;

				(bool Succeeded, LegalIdentity AddedIdentity) = await this.NetworkService.TryRequest(() =>
					this.XmppService.AddLegalIdentity(IdentityModel, Photos));

				if (Succeeded)
				{
					this.LegalIdentity = AddedIdentity;
					await this.TagProfile.SetLegalIdentity(this.LegalIdentity);
					this.UiSerializer.BeginInvokeOnMainThread(() =>
					{
						this.SetIsDone(this.RegisterCommand);
						this.OnStepCompleted(EventArgs.Empty);
					});
				}
			}
			catch (Exception ex)
			{
				this.LogService.LogException(ex);
				await this.UiSerializer.DisplayAlert(ex);
			}
			finally
			{
				this.BeginInvokeSetIsDone(this.RegisterCommand);
			}
		}

		private RegisterIdentityModel CreateRegisterModel()
		{
			RegisterIdentityModel IdentityModel = new();
			string s;

			if (!string.IsNullOrWhiteSpace(s = this.FirstName?.Trim()))
				IdentityModel.FirstName = s;

			if (!string.IsNullOrWhiteSpace(s = this.MiddleNames?.Trim()))
				IdentityModel.MiddleNames = s;

			if (!string.IsNullOrWhiteSpace(s = this.LastNames?.Trim()))
				IdentityModel.LastNames = s;

			if (!string.IsNullOrWhiteSpace(s = this.PersonalNumber?.Trim()))
				IdentityModel.PersonalNumber = s;

			if (!string.IsNullOrWhiteSpace(s = this.Address?.Trim()))
				IdentityModel.Address = s;

			if (!string.IsNullOrWhiteSpace(s = this.Address2?.Trim()))
				IdentityModel.Address2 = s;

			if (!string.IsNullOrWhiteSpace(s = this.ZipCode?.Trim()))
				IdentityModel.ZipCode = s;

			if (!string.IsNullOrWhiteSpace(s = this.Area?.Trim()))
				IdentityModel.Area = s;

			if (!string.IsNullOrWhiteSpace(s = this.City?.Trim()))
				IdentityModel.City = s;

			if (!string.IsNullOrWhiteSpace(s = this.Region?.Trim()))
				IdentityModel.Region = s;

			if (!string.IsNullOrWhiteSpace(s = this.SelectedCountry?.Trim()))
				IdentityModel.Country = s;

			if (!string.IsNullOrWhiteSpace(s = this.TagProfile?.PhoneNumber?.Trim()))
			{
				if (string.IsNullOrWhiteSpace(s) && this.TagProfile.LegalIdentity is not null)
					s = this.TagProfile.LegalIdentity[Constants.XmppProperties.Phone];

				IdentityModel.PhoneNr = s;
			}

			if (!string.IsNullOrWhiteSpace(s = this.TagProfile?.EMail?.Trim()))
			{
				if (string.IsNullOrWhiteSpace(s) && this.TagProfile.LegalIdentity is not null)
					s = this.TagProfile.LegalIdentity[Constants.XmppProperties.EMail];

				IdentityModel.EMail = s;
			}

			return IdentityModel;
		}

		private async Task<bool> ValidateInput(bool AlertUser)
		{
			if (string.IsNullOrWhiteSpace(this.FirstName?.Trim()))
			{
				if (AlertUser)
					await this.UiSerializer.DisplayAlert(LocalizationResourceManager.Current["InformationIsMissingOrInvalid"], LocalizationResourceManager.Current["YouNeedToProvideAFirstName"]);

				return false;
			}

			if (string.IsNullOrWhiteSpace(this.LastNames?.Trim()))
			{
				if (AlertUser)
					await this.UiSerializer.DisplayAlert(LocalizationResourceManager.Current["InformationIsMissingOrInvalid"], LocalizationResourceManager.Current["YouNeedToProvideALastName"]);

				return false;
			}

			if (string.IsNullOrWhiteSpace(this.PersonalNumber?.Trim()))
			{
				if (AlertUser)
					await this.UiSerializer.DisplayAlert(LocalizationResourceManager.Current["InformationIsMissingOrInvalid"], LocalizationResourceManager.Current["YouNeedToProvideAPersonalNumber"]);

				return false;
			}

			if (string.IsNullOrWhiteSpace(this.SelectedCountry))
			{
				if (AlertUser)
					await this.UiSerializer.DisplayAlert(LocalizationResourceManager.Current["InformationIsMissingOrInvalid"], LocalizationResourceManager.Current["YouNeedToProvideACountry"]);

				return false;
			}

			return true;
		}

		/// <inheritdoc />
		protected override async Task DoSaveState()
		{
			await base.DoSaveState();
			await this.SettingsService.SaveState(this.GetSettingsKey(nameof(this.SelectedCountry)), this.SelectedCountry);
			await this.SettingsService.SaveState(this.GetSettingsKey(nameof(this.FirstName)), this.FirstName);
			await this.SettingsService.SaveState(this.GetSettingsKey(nameof(this.MiddleNames)), this.MiddleNames);
			await this.SettingsService.SaveState(this.GetSettingsKey(nameof(this.LastNames)), this.LastNames);
			await this.SettingsService.SaveState(this.GetSettingsKey(nameof(this.PersonalNumber)), this.PersonalNumber);
			await this.SettingsService.SaveState(this.GetSettingsKey(nameof(this.Address)), this.Address);
			await this.SettingsService.SaveState(this.GetSettingsKey(nameof(this.Address2)), this.Address2);
			await this.SettingsService.SaveState(this.GetSettingsKey(nameof(this.Area)), this.Area);
			await this.SettingsService.SaveState(this.GetSettingsKey(nameof(this.City)), this.City);
			await this.SettingsService.SaveState(this.GetSettingsKey(nameof(this.ZipCode)), this.ZipCode);
			await this.SettingsService.SaveState(this.GetSettingsKey(nameof(this.Region)), this.Region);
		}

		/// <inheritdoc />
		protected override async Task DoRestoreState()
		{
			this.SelectedCountry = await this.SettingsService.RestoreStringState(this.GetSettingsKey(nameof(this.SelectedCountry)));
			this.FirstName = await this.SettingsService.RestoreStringState(this.GetSettingsKey(nameof(this.FirstName)));
			this.MiddleNames = await this.SettingsService.RestoreStringState(this.GetSettingsKey(nameof(this.MiddleNames)));
			this.LastNames = await this.SettingsService.RestoreStringState(this.GetSettingsKey(nameof(this.LastNames)));
			this.PersonalNumber = await this.SettingsService.RestoreStringState(this.GetSettingsKey(nameof(this.PersonalNumber)));
			this.Address = await this.SettingsService.RestoreStringState(this.GetSettingsKey(nameof(this.Address)));
			this.Address2 = await this.SettingsService.RestoreStringState(this.GetSettingsKey(nameof(this.Address2)));
			this.Area = await this.SettingsService.RestoreStringState(this.GetSettingsKey(nameof(this.Area)));
			this.City = await this.SettingsService.RestoreStringState(this.GetSettingsKey(nameof(this.City)));
			this.ZipCode = await this.SettingsService.RestoreStringState(this.GetSettingsKey(nameof(this.ZipCode)));
			this.Region = await this.SettingsService.RestoreStringState(this.GetSettingsKey(nameof(this.Region)));

			await base.DoRestoreState();
		}

		/// <inheritdoc />
		public override void ClearStepState()
		{
			this.SelectedCountry = null;
			this.FirstName = string.Empty;
			this.MiddleNames = string.Empty;
			this.LastNames = string.Empty;
			this.PersonalNumber = string.Empty;
			this.Address = string.Empty;
			this.Address2 = string.Empty;
			this.Area = string.Empty;
			this.City = string.Empty;
			this.ZipCode = string.Empty;
			this.Region = string.Empty;

			this.SettingsService.RemoveState(this.GetSettingsKey(nameof(this.SelectedCountry)));
			this.SettingsService.RemoveState(this.GetSettingsKey(nameof(this.FirstName)));
			this.SettingsService.RemoveState(this.GetSettingsKey(nameof(this.MiddleNames)));
			this.SettingsService.RemoveState(this.GetSettingsKey(nameof(this.LastNames)));
			this.SettingsService.RemoveState(this.GetSettingsKey(nameof(this.PersonalNumber)));
			this.SettingsService.RemoveState(this.GetSettingsKey(nameof(this.Address)));
			this.SettingsService.RemoveState(this.GetSettingsKey(nameof(this.Address2)));
			this.SettingsService.RemoveState(this.GetSettingsKey(nameof(this.Area)));
			this.SettingsService.RemoveState(this.GetSettingsKey(nameof(this.City)));
			this.SettingsService.RemoveState(this.GetSettingsKey(nameof(this.ZipCode)));
			this.SettingsService.RemoveState(this.GetSettingsKey(nameof(this.Region)));
		}

		/// <summary>
		/// Copies values from the existing TAG Profile's Legal identity.
		/// </summary>
		public virtual void PopulateFromTagProfile()
		{
			LegalIdentity Identity = this.TagProfile.LegalIdentity;

			if (Identity is not null)
			{
				this.FirstName = Identity[Constants.XmppProperties.FirstName];
				this.MiddleNames = Identity[Constants.XmppProperties.MiddleName];
				this.LastNames = Identity[Constants.XmppProperties.LastName];
				this.PersonalNumber = Identity[Constants.XmppProperties.PersonalNumber];
				this.Address = Identity[Constants.XmppProperties.Address];
				this.Address2 = Identity[Constants.XmppProperties.Address2];
				this.ZipCode = Identity[Constants.XmppProperties.ZipCode];
				this.Area = Identity[Constants.XmppProperties.Area];
				this.City = Identity[Constants.XmppProperties.City];
				this.Region = Identity[Constants.XmppProperties.Region];
				string CountryCode = Identity[Constants.XmppProperties.Country];

				if (!string.IsNullOrWhiteSpace(CountryCode) && ISO_3166_1.TryGetCountry(CountryCode, out string Country))
					this.SelectedCountry = Country;
			}
		}
	}
}
