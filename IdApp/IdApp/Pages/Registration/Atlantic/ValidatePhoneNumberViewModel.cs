using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Input;
using Waher.Content;
using Xamarin.Forms;
using IdApp.Services.Tag;
using Xamarin.CommunityToolkit.Helpers;
using Waher.Networking.XMPP;

namespace IdApp.Pages.Registration.Atlantic
{
	/// <summary>
	/// The view model to bind to when showing Step 1 of the registration flow: choosing an operator.
	/// </summary>
	public class ValidatePhoneNumberViewModel : RegistrationStepViewModel
	{
		/// <summary>
		/// Creates a new instance of the <see cref="ValidatePhoneNumberViewModel"/> class.
		/// </summary>
		public ValidatePhoneNumberViewModel() : base(RegistrationStep.ValidatePhoneNumber)
		{
			this.VerifyPhoneNrCodeCommand = new Command(async () => await this.VerifyPhoneNrCode(), this.VerifyPhoneNrCodeCanExecute);

			this.Title = LocalizationResourceManager.Current["ContactInformation"];
		}

		/// <summary>
		/// Override this method to do view model specific setup when it's parent page/view appears on screen.
		/// </summary>
		protected override async Task OnInitialize()
		{
			await base.OnInitialize();

			this.EvaluateAllCommands();
		}

		/// <inheritdoc/>
		protected override Task OnDispose()
		{
			return base.OnDispose();
		}

		/// <inheritdoc />
		public override Task DoAssignProperties()
		{
			this.EvaluateAllCommands();

			return base.DoAssignProperties();
		}

		private void EvaluateAllCommands()
		{
			this.EvaluateCommands(this.VerifyPhoneNrCodeCommand);
		}


		#region Properties

		/// <summary>
		/// See <see cref="PhoneNrVerificationCode"/>
		/// </summary>
		public static readonly BindableProperty PhoneNrVerificationCodeProperty =
			BindableProperty.Create(nameof(PhoneNrVerificationCode), typeof(string), typeof(ValidatePhoneNumberViewModel), default(string));

		/// <summary>
		/// Phone number
		/// </summary>
		public string PhoneNrVerificationCode
		{
			get => (string)this.GetValue(PhoneNrVerificationCodeProperty);
			set
			{
				this.SetValue(PhoneNrVerificationCodeProperty, value);
				this.OnPropertyChanged(nameof(this.VerifyPhoneCodeButtonEnabled));
			}
		}

		/// <summary>
		/// If Phone number is valid or not
		/// </summary>
		public bool VerifyPhoneCodeButtonEnabled => this.IsVerificationCode(this.PhoneNrVerificationCode);

		/// <summary>
		/// The command to bind to for sending a phone message code verification request.
		/// </summary>
		public ICommand VerifyPhoneNrCodeCommand { get; }

		#endregion

		#region Commands

		#region Phone Numbers

		private async Task VerifyPhoneNrCode()
		{
			if (!this.NetworkService.IsOnline)
			{
				await this.UiSerializer.DisplayAlert(LocalizationResourceManager.Current["ErrorTitle"], LocalizationResourceManager.Current["NetworkSeemsToBeMissing"]);
				return;
			}

			this.SetIsBusy(this.VerifyPhoneNrCodeCommand);

			try
			{
				string TrimmedNumber = this.TagProfile.TrimmedNumber;
				bool IsTest = true;

				object Result = await InternetContent.PostAsync(
					new Uri("https://" + Constants.Domains.IdDomain + "/ID/VerifyNumber.ws"),
					new Dictionary<string, object>()
					{
						{ "Nr", TrimmedNumber },
						{ "Code", int.Parse(this.PhoneNrVerificationCode) },
						{ "Test", IsTest }
					}, new KeyValuePair<string, string>("Accept", "application/json"));

				this.PhoneNrVerificationCode = string.Empty;

				if (Result is Dictionary<string, object> Response &&
					Response.TryGetValue("Status", out object Obj) && Obj is bool Status && Status &&
					Response.TryGetValue("Domain", out Obj) && Obj is string Domain &&
					Response.TryGetValue("Key", out Obj) && Obj is string Key &&
					Response.TryGetValue("Secret", out Obj) && Obj is string Secret &&
					Response.TryGetValue("Temporary", out Obj) && Obj is bool IsTemporary)
				{
					this.TagProfile.SetIsTest(IsTest);
					this.TagProfile.SetPhone(TrimmedNumber);
					this.TagProfile.SetTestOtpTimestamp(IsTemporary ? DateTime.Now : null);

					bool DefaultConnectivity;
					try
					{
						(string HostName, int PortNumber, bool IsIpAddress) = await this.NetworkService.LookupXmppHostnameAndPort(Domain);
						DefaultConnectivity = HostName == Domain && PortNumber == Waher.Networking.XMPP.XmppCredentials.DefaultPort;
					}
					catch (Exception)
					{
						DefaultConnectivity = false;
					}

					await this.TagProfile.SetDomain(Domain, DefaultConnectivity, Key, Secret);
					await this.CreateAccount();
				}
				else
				{
					await this.TagProfile.InvalidatePhoneNumber();

					await this.UiSerializer.DisplayAlert(LocalizationResourceManager.Current["ErrorTitle"], LocalizationResourceManager.Current["UnableToVerifyCode"], LocalizationResourceManager.Current["Ok"]);
				}
			}
			catch (Exception ex)
			{
				await this.TagProfile.InvalidatePhoneNumber();

				this.LogService.LogException(ex);
				await this.UiSerializer.DisplayAlert(LocalizationResourceManager.Current["ErrorTitle"], ex.Message, LocalizationResourceManager.Current["Ok"]);
			}
			finally
			{
				this.BeginInvokeSetIsDone(this.VerifyPhoneNrCodeCommand);
			}
		}

		#endregion

		#region Account

		private async Task<bool> CreateAccount()
		{
			try
			{
				string AccountName = $"A{this.TagProfile.TrimmedNumber}";
				string passwordToUse = this.CryptoService.CreateRandomPassword();

				(string hostName, int portNumber, bool isIpAddress) = await this.NetworkService.LookupXmppHostnameAndPort(this.TagProfile.Domain);

				async Task OnConnected(XmppClient client)
				{
					if (this.TagProfile.NeedsUpdating())
						await this.XmppService.DiscoverServices(client);

					await this.TagProfile.SetAccount(AccountName, client.PasswordHash, client.PasswordHashMethod);
					this.OnStepCompleted(EventArgs.Empty);
				}

				(bool succeeded, string errorMessage) = await this.XmppService.TryConnectAndCreateAccount(this.TagProfile.Domain,
					isIpAddress, hostName, portNumber, AccountName, passwordToUse, Constants.LanguageCodes.Default,
					this.TagProfile.ApiKey, this.TagProfile.ApiSecret, typeof(App).Assembly, OnConnected);

				if (succeeded)
					return true;

				await this.UiSerializer.DisplayAlert(LocalizationResourceManager.Current["ErrorTitle"], errorMessage, LocalizationResourceManager.Current["Ok"]);
			}
			catch (Exception ex)
			{
				this.LogService.LogException(ex);
				string userMessage = string.Format(LocalizationResourceManager.Current["UnableToConnectTo"], this.TagProfile.Domain);
				string message = userMessage + Environment.NewLine + ex.Message;
				await this.UiSerializer.DisplayAlert(LocalizationResourceManager.Current["ErrorTitle"], message, LocalizationResourceManager.Current["Ok"]);
			}

			return false;
		}

		#endregion

		#endregion

		#region CanExecute

		private bool VerifyPhoneNrCodeCanExecute()
		{
			if (this.IsBusy) // is connecting
				return false;

			return this.VerifyPhoneCodeButtonEnabled;
		}

		#endregion

		#region Syntax

		private string TrimPhoneNumber(string PhoneNr)
		{
			return PhoneNr.Trim().Replace(" ", "").Replace("-", "").Replace("(", "").Replace(")", "");
		}

		private bool IsVerificationCode(string Code)
		{
			return !string.IsNullOrEmpty(Code) && verificationCode.IsMatch(Code);
		}

		private static readonly Regex internationalPhoneNr = new(@"^\+[1-9]\d{4,}$", RegexOptions.Singleline);
		private static readonly Regex verificationCode = new(@"^[1-9]\d{5}$", RegexOptions.Singleline);
		private static readonly Regex emailAddress = new(@"^[\w\d](\w|\d|[_\.-][\w\d])*@(\w|\d|[\.-][\w\d]+)+$", RegexOptions.Singleline);

		#endregion
	}
}
