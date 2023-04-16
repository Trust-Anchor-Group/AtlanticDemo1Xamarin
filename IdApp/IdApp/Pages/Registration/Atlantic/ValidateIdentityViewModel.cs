using IdApp.Extensions;
using IdApp.Services.Tag;
using System;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows.Input;
using Waher.Networking.XMPP;
using Waher.Networking.XMPP.Contracts;
using Xamarin.CommunityToolkit.Helpers;
using Xamarin.Forms;

namespace IdApp.Pages.Registration.Atlantic
{
	/// <summary>
	/// The view model to bind to when showing Step 4 of the registration flow: validating an identity.
	/// </summary>
	public class ValidateIdentityViewModel : RegistrationStepViewModel
	{
		/// <summary>
		/// Creates a new instance of the <see cref="ValidateIdentityViewModel"/> class.
		/// </summary>
		public ValidateIdentityViewModel() : base(RegistrationStep.ValidateIdentity)
		{
			this.ContinueCommand = new Command(_ => this.Continue(), _ => this.IsApproved || this.IsRejected);
			this.Title = LocalizationResourceManager.Current["ValidatingInformation"];
		}

		/// <inheritdoc />
		protected override async Task OnInitialize()
		{
			await base.OnInitialize();
			this.AssignProperties();

			this.TagProfile.Changed += this.TagProfile_Changed;
			this.XmppService.ConnectionStateChanged += this.XmppService_ConnectionStateChanged;
			this.XmppService.LegalIdentityChanged += this.XmppContracts_LegalIdentityChanged;

			await this.XmppService_ConnectionStateChanged(this, this.XmppService.State);
		}

		/// <inheritdoc />
		protected override async Task OnDispose()
		{
			this.TagProfile.Changed -= this.TagProfile_Changed;
			this.XmppService.ConnectionStateChanged -= this.XmppService_ConnectionStateChanged;
			this.XmppService.LegalIdentityChanged -= this.XmppContracts_LegalIdentityChanged;
			await base.OnDispose();
		}

		#region Properties

		/// <summary>
		/// The command to bind to for continuing to the next step in the registration process.
		/// </summary>
		public ICommand ContinueCommand { get; }

		/// <summary>
		/// The <see cref="LegalId"/>
		/// </summary>
		public static readonly BindableProperty LegalIdProperty =
			BindableProperty.Create(nameof(LegalId), typeof(string), typeof(ValidateIdentityViewModel), default(string));

		/// <summary>
		/// Gets or sets the Id of the legal identity.
		/// </summary>
		public string LegalId
		{
			get => (string)this.GetValue(LegalIdProperty);
			set => this.SetValue(LegalIdProperty, value);
		}

		/// <summary>
		/// Gets or sets the legal identity.
		/// </summary>
		public LegalIdentity LegalIdentity { get; private set; }

		/// <summary>
		/// The <see cref="BareJid"/>
		/// </summary>
		public static readonly BindableProperty BareJidProperty =
			BindableProperty.Create(nameof(BareJid), typeof(string), typeof(ValidateIdentityViewModel), default(string));

		/// <summary>
		/// Gets or sets the Bare Jid registered with the XMPP server.
		/// </summary>
		public string BareJid
		{
			get => (string)this.GetValue(BareJidProperty);
			set => this.SetValue(BareJidProperty, value);
		}

		/// <summary>
		/// </summary>
		public string FullName
		{
			get
			{
				string Name = this.FirstName ?? string.Empty;

				if (this.MiddleNames.Length > 0)
				{
					Name += " ";
					Name += this.MiddleNames;
				}
				if (this.LastNames.Length > 0)
				{
					Name += " ";
					Name += this.LastNames;
				}

				if (Name.Length <= 0)
				{
					Name = "Anonymous";
				}

				return Name;
			}
		}

		/// <summary>
		/// The <see cref="FirstName"/>
		/// </summary>
		public static readonly BindableProperty FirstNameProperty =
			BindableProperty.Create(nameof(FirstName), typeof(string), typeof(ValidateIdentityViewModel), default(string));

		/// <summary>
		/// The user's first name
		/// </summary>
		public string FirstName
		{
			get => (string)this.GetValue(FirstNameProperty);
			set
			{
				this.SetValue(FirstNameProperty, value);
				this.OnPropertyChanged(nameof(this.FullName));
			}
		}

		/// <summary>
		/// The <see cref="MiddleNames"/>
		/// </summary>
		public static readonly BindableProperty MiddleNamesProperty =
			BindableProperty.Create(nameof(MiddleNames), typeof(string), typeof(ValidateIdentityViewModel), default(string));

		/// <summary>
		/// The user's middle name(s)
		/// </summary>
		public string MiddleNames
		{
			get => (string)this.GetValue(MiddleNamesProperty);
			set
			{
				this.SetValue(MiddleNamesProperty, value);
				this.OnPropertyChanged(nameof(this.FullName));
			}
		}

		/// <summary>
		/// The <see cref="LastNames"/>
		/// </summary>
		public static readonly BindableProperty LastNamesProperty =
			BindableProperty.Create(nameof(LastNames), typeof(string), typeof(ValidateIdentityViewModel), default(string));

		/// <summary>
		/// The user's last name(s)
		/// </summary>
		public string LastNames
		{
			get => (string)this.GetValue(LastNamesProperty);
			set
			{
				this.SetValue(LastNamesProperty, value);
				this.OnPropertyChanged(nameof(this.FullName));
			}
		}

		/// <summary>
		/// The <see cref="IsApproved"/>
		/// </summary>
		public static readonly BindableProperty IsApprovedProperty =
			BindableProperty.Create(nameof(IsApproved), typeof(bool), typeof(ValidateIdentityViewModel), default(bool));

		/// <summary>
		/// Gets or sets if the user's identity is approved or not.
		/// </summary>
		public bool IsApproved
		{
			get => (bool)this.GetValue(IsApprovedProperty);
			set => this.SetValue(IsApprovedProperty, value);
		}

		/// <summary>
		/// The <see cref="IsRejected"/>
		/// </summary>
		public static readonly BindableProperty IsRejectedProperty =
			BindableProperty.Create(nameof(IsRejected), typeof(bool), typeof(ValidateIdentityViewModel), default(bool));

		/// <summary>
		/// Gets or sets if the user's identity is approved or not.
		/// </summary>
		public bool IsRejected
		{
			get => (bool)this.GetValue(IsRejectedProperty);
			set => this.SetValue(IsRejectedProperty, value);
		}

		/// <summary>
		/// The <see cref="IsCreated"/>
		/// </summary>
		public static readonly BindableProperty IsCreatedProperty =
			BindableProperty.Create(nameof(IsCreated), typeof(bool), typeof(ValidateIdentityViewModel), default(bool));

		/// <summary>
		/// Gets or sets if the user's identity has been created.
		/// </summary>
		public bool IsCreated
		{
			get => (bool)this.GetValue(IsCreatedProperty);
			set => this.SetValue(IsCreatedProperty, value);
		}

		/// <summary>
		/// The <see cref="IsConnected"/>
		/// </summary>
		public static readonly BindableProperty IsConnectedProperty =
			BindableProperty.Create(nameof(IsConnected), typeof(bool), typeof(ValidateIdentityViewModel), default(bool));

		/// <summary>
		/// Gets or sets if the app is connected to an XMPP server.
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
			BindableProperty.Create(nameof(ConnectionStateText), typeof(string), typeof(ValidateIdentityViewModel), default(string));

		/// <summary>
		/// The user friendly connection state text to display to the user.
		/// </summary>
		public string ConnectionStateText
		{
			get => (string)this.GetValue(ConnectionStateTextProperty);
			set => this.SetValue(ConnectionStateTextProperty, value);
		}

		#endregion

		private void AssignProperties()
		{
			this.LegalId = this.TagProfile.LegalIdentity?.Id;
			this.LegalIdentity = this.TagProfile.LegalIdentity;
			this.AssignBareJid();

			IdentityState State = this.TagProfile.LegalIdentity?.State ?? IdentityState.Rejected;

			if (this.TagProfile.LegalIdentity is not null)
			{
				this.FirstName = this.TagProfile.LegalIdentity[Constants.XmppProperties.FirstName];
				this.MiddleNames = this.TagProfile.LegalIdentity[Constants.XmppProperties.MiddleName];
				this.LastNames = this.TagProfile.LegalIdentity[Constants.XmppProperties.LastName];
			}
			else
			{
				this.FirstName = string.Empty;
				this.MiddleNames = string.Empty;
				this.LastNames = string.Empty;
			}

			this.IsApproved = State == IdentityState.Approved;
			this.IsCreated = State == IdentityState.Created;
			this.IsRejected = !this.IsCreated && !this.IsApproved;

			this.ContinueCommand.ChangeCanExecute();

			this.SetConnectionStateAndText(this.XmppService.State);
		}

		private void AssignBareJid()
		{
			this.BareJid = this.XmppService?.BareJid ?? string.Empty;
		}

		private void TagProfile_Changed(object Sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName == nameof(this.TagProfile.Step) || e.PropertyName == nameof(this.TagProfile.LegalIdentity))
				this.UiSerializer.BeginInvokeOnMainThread(this.AssignProperties);
			else
				this.UiSerializer.BeginInvokeOnMainThread(this.AssignBareJid);
		}

		private Task XmppService_ConnectionStateChanged(object _, XmppState NewState)
		{
			this.UiSerializer.BeginInvokeOnMainThread(async () =>
			{
				this.AssignBareJid();
				this.SetConnectionStateAndText(NewState);

				if (this.IsConnected)
				{
					await Task.Delay(Constants.Timeouts.XmppInit);
				}
			});

			return Task.CompletedTask;
		}

		private void SetConnectionStateAndText(XmppState state)
		{
			this.IsConnected = state == XmppState.Connected;
			this.ConnectionStateText = state.ToDisplayText();
		}

		private Task XmppContracts_LegalIdentityChanged(object Sender, LegalIdentityEventArgs e)
		{
			this.UiSerializer.BeginInvokeOnMainThread(() =>
			{
				this.LegalIdentity = e.Identity;
				this.TagProfile.SetLegalIdentity(e.Identity);
				this.AssignProperties();
			});

			return Task.CompletedTask;
		}

		private void Continue()
		{
			if (this.IsApproved)
			{
				this.TagProfile.SetIsValidated();
			}
			else
			{
				this.TagProfile.ClearIsValidated();
			}

			this.OnStepCompleted(EventArgs.Empty);
		}
	}
}
