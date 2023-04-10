using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Input;
using Waher.Content;
using Xamarin.Forms;
using IdApp.Services.Tag;
using Xamarin.CommunityToolkit.Helpers;

namespace IdApp.Pages.Registration.Atlantic
{
	/// <summary>
	/// The view model to bind to when showing Step 1 of the registration flow: choosing an operator.
	/// </summary>
	public class GetPhoneNumberViewModel : RegistrationStepViewModel
	{
		/// <summary>
		/// Creates a new instance of the <see cref="GetPhoneNumberViewModel"/> class.
		/// </summary>
		public GetPhoneNumberViewModel() : base(RegistrationStep.GetPhoneNumber)
		{
			this.SendPhoneNrCodeCommand = new Command(async () => await this.SendPhoneNrCode(), this.SendPhoneNrCodeCanExecute);

			this.Title = LocalizationResourceManager.Current["ContactInformation"];
		}

		/// <summary>
		/// Override this method to do view model specific setup when it's parent page/view appears on screen.
		/// </summary>
		protected override async Task OnInitialize()
		{
			await base.OnInitialize();

			if (string.IsNullOrEmpty(this.TagProfile.PhoneNumber))
			{
				try
				{
					object Result = await InternetContent.PostAsync(
						new Uri("https://" + Constants.Domains.IdDomain + "/ID/CountryCode.ws"), string.Empty,
						new KeyValuePair<string, string>("Accept", "application/json"));

					if (Result is Dictionary<string, object> Response &&
						Response.TryGetValue("PhoneCode", out object Obj) && Obj is string PhoneCode)
					{
						this.PhoneNumber = PhoneCode;
					}
					else
						this.PhoneNumber = "+";
				}
				catch (Exception ex)
				{
					this.PhoneNumber = "+";
					this.LogService.LogException(ex);
				}
			}
			else
				this.PhoneNumber = this.TagProfile.PhoneNumber;

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
			if (!string.IsNullOrEmpty(this.TagProfile.PhoneNumber))
			{
				this.PhoneNumber = this.TagProfile.PhoneNumber;
			}

			this.EvaluateAllCommands();

			return base.DoAssignProperties();
		}

		/// <inheritdoc/>
		protected override void OnStepCompleted(EventArgs e)
		{
			base.OnStepCompleted(e);
		}

		private void EvaluateAllCommands()
		{
			this.EvaluateCommands(this.SendPhoneNrCodeCommand);
		}

		#region Properties

		/// <summary>
		/// See <see cref="CountPhoneSeconds"/>
		/// </summary>
		public static readonly BindableProperty CountPhoneSecondsProperty =
			BindableProperty.Create(nameof(CountPhoneSeconds), typeof(int), typeof(GetPhoneNumberViewModel), default);

		/// <summary>
		/// how long the phone button will stay disabled
		/// </summary>
		public int CountPhoneSeconds
		{
			get => (int)this.GetValue(CountPhoneSecondsProperty);
			set
			{
				this.SetValue(CountPhoneSecondsProperty, value);
				this.OnPropertyChanged(nameof(this.PhoneButtonEnabled));
				this.OnPropertyChanged(nameof(this.PhoneButtonLabel));
			}
		}

		/// <summary>
		/// See <see cref="PhoneNumber"/>
		/// </summary>
		public static readonly BindableProperty PhoneNumberProperty =
			BindableProperty.Create(nameof(PhoneNumber), typeof(string), typeof(GetPhoneNumberViewModel), default(string));

		/// <summary>
		/// Phone number
		/// </summary>
		public string PhoneNumber
		{
			get => (string)this.GetValue(PhoneNumberProperty);
			set
			{
				this.SetValue(PhoneNumberProperty, value);
				this.OnPropertyChanged(nameof(this.PhoneButtonEnabled));
			}
		}

		/// <summary>
		/// If send code Phone button is disabled or not
		/// </summary>
		public bool PhoneButtonEnabled => (this.CountPhoneSeconds == 0) && this.IsInternationalPhoneNumberFormat(this.PhoneNumber);

		/// <summary>
		/// The label of the SendPhoneNrCodeButton
		/// </summary>
		public string PhoneButtonLabel
		{
			get
			{
				if (this.CountPhoneSeconds > 0)
				{
					return string.Format(LocalizationResourceManager.Current["DisabledFor"], this.CountPhoneSeconds);
				}

				return LocalizationResourceManager.Current["SendCode"];
			}
		}

		/// <summary>
		/// The command to bind to for sending and verification a code to the provided phone number.
		/// </summary>
		public ICommand SendPhoneNrCodeCommand { get; }

		#endregion

		#region Commands

		#region Phone Numbers

		private async Task SendPhoneNrCode()
		{
			if (!this.NetworkService.IsOnline)
			{
				await this.UiSerializer.DisplayAlert(LocalizationResourceManager.Current["ErrorTitle"], LocalizationResourceManager.Current["NetworkSeemsToBeMissing"]);
				return;
			}

			this.SetIsBusy(this.SendPhoneNrCodeCommand);

			try
			{
				string TrimmedNumber = this.TrimPhoneNumber(this.PhoneNumber);

				object Result = await InternetContent.PostAsync(
					new Uri("https://" + Constants.Domains.IdDomain + "/ID/SendVerificationMessage.ws"),
					new Dictionary<string, object>()
					{
						{ "Nr", TrimmedNumber }
					}, new KeyValuePair<string, string>("Accept", "application/json"));

				if (Result is Dictionary<string, object> Response &&
					Response.TryGetValue("Status", out object Obj) && Obj is bool Status && Status
					&& Response.TryGetValue("IsTemporary", out Obj) && Obj is bool IsTemporary)
				{
					if (!string.IsNullOrEmpty(this.TagProfile.PhoneNumber) && !this.TagProfile.TestOtpTimestamp.HasValue && IsTemporary)
					{
						await this.UiSerializer.DisplayAlert(LocalizationResourceManager.Current["ErrorTitle"], LocalizationResourceManager.Current["SwitchingToTestPhoneNumberNotAllowed"]);
					}
					else
					{
						this.TagProfile.SetPhone(TrimmedNumber);
						this.StartTimer("phone");

						/*!!! go to next verification step */
						await this.UiSerializer.DisplayAlert(LocalizationResourceManager.Current["SuccessTitle"],
							LocalizationResourceManager.Current["SendPhoneNumberWarning"]);
					}
				}
				else
				{
					await this.UiSerializer.DisplayAlert(LocalizationResourceManager.Current["ErrorTitle"], LocalizationResourceManager.Current["SomethingWentWrongWhenSendingPhoneCode"]);
				}
			}
			catch (Exception ex)
			{
				this.LogService.LogException(ex);
				await this.UiSerializer.DisplayAlert(LocalizationResourceManager.Current["ErrorTitle"], ex.Message, LocalizationResourceManager.Current["Ok"]);
			}
			finally
			{
				this.BeginInvokeSetIsDone(this.SendPhoneNrCodeCommand);
			}
		}

		#endregion

		#endregion // Commands

		#region CanExecute

		private bool SendPhoneNrCodeCanExecute()
		{
			if (this.IsBusy) // is connecting
				return false;

			return this.PhoneButtonEnabled;
		}

		#endregion

		#region Syntax

		private string TrimPhoneNumber(string PhoneNr)
		{
			return PhoneNr.Trim().Replace(" ", "").Replace("-", "").Replace("(", "").Replace(")", "");
		}

		private bool IsInternationalPhoneNumberFormat(string PhoneNr)
		{
			if (string.IsNullOrEmpty(PhoneNr))
				return false;

			string phoneNumber = this.TrimPhoneNumber(PhoneNr);
			return internationalPhoneNr.IsMatch(phoneNumber);
		}

		private bool IsVerificationCode(string Code)
		{
			return !string.IsNullOrEmpty(Code) && verificationCode.IsMatch(Code);
		}

		private void StartTimer(string type)
		{
			if (type == "phone")
			{
				this.CountPhoneSeconds = 30;

				Device.StartTimer(TimeSpan.FromSeconds(1), () => {
					if (this.CountPhoneSeconds > 0)
					{
						this.CountPhoneSeconds--;
						return true;
					}
					else
					{
						return false;
					}
				});
			}
		}

		private static readonly Regex internationalPhoneNr = new(@"^\+[1-9]\d{4,}$", RegexOptions.Singleline);
		private static readonly Regex verificationCode = new(@"^[1-9]\d{5}$", RegexOptions.Singleline);
		private static readonly Regex emailAddress = new(@"^[\w\d](\w|\d|[_\.-][\w\d])*@(\w|\d|[\.-][\w\d]+)+$", RegexOptions.Singleline);

		#endregion
	}
}
