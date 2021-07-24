﻿using System;
using System.Text;
using System.Threading.Tasks;
using IdApp.Services;
using Tag.Neuron.Xamarin;
using Tag.Neuron.Xamarin.Services;
using Waher.IoTGateway.Setup;
using Waher.Runtime.Inventory;
using Xamarin.Essentials;
using Xamarin.Forms;

namespace IdApp.Pages.Main.Shell
{
	/// <summary>
	/// The Xamarin Forms Shell implementation of the TAG ID App.
	/// </summary>
	public partial class AppShell
	{
		/// <summary>
		/// Create a new instance of the <see cref="AppShell"/> class.
		/// </summary>
		public AppShell()
		{
			this.ViewModel = new AppShellViewModel();
			InitializeComponent();
			SetTabBarIsVisible(this, false);
			RegisterRoutes();
		}

		/// <summary>
		/// Current Neuron Service
		/// </summary>
		public INeuronService NeuronService => Types.Instantiate<INeuronService>(false);

		/// <summary>
		/// Current Network Service
		/// </summary>
		public INetworkService NetworkService => Types.Instantiate<INetworkService>(false);

		/// <summary>
		/// Current Navigation Service
		/// </summary>
		public INavigationService NavigationService => Types.Instantiate<INavigationService>(false);

		/// <summary>
		/// Current Log Service
		/// </summary>
		public ILogService LogService => Types.Instantiate<ILogService>(false);

		/// <summary>
		/// Current UI Dispatcher Service
		/// </summary>
		public IUiDispatcher UiDispatcher => Types.Instantiate<IUiDispatcher>(false);

		/// <summary>
		/// Current Contract Orchestrator Service
		/// </summary>
		public IContractOrchestratorService ContractOrchestratorService => Types.Instantiate<IContractOrchestratorService>(false);

		/// <summary>
		/// Current Thing Registry Orchestrator Service
		/// </summary>
		public IThingRegistryOrchestratorService ThingRegistryOrchestratorService => Types.Instantiate<IThingRegistryOrchestratorService>(false);

		/// <summary>
		/// Current eDaler Orchestrator Service
		/// </summary>
		public IEDalerOrchestratorService EDalerOrchestratorService => Types.Instantiate<IEDalerOrchestratorService>(false);

		/// <inheritdoc/>
		protected override void OnNavigated(ShellNavigatedEventArgs e)
		{
			base.OnNavigated(e);

			// Once MainPage is shown, hide the "Loading..." Flyout, as the user shouldn't be
			// able to navigate to that page.

			if (e.Current.Location.ToString().Contains(nameof(Pages.Main.Main.MainPage)))
				this.LoadingFlyout.IsVisible = false;
		}

		private void RegisterRoutes()
		{
			// Onboarding:
			Routing.RegisterRoute(nameof(Registration.Registration.RegistrationPage), typeof(Registration.Registration.RegistrationPage));

			// General:
			Routing.RegisterRoute(nameof(ScanQrCode.ScanQrCodePage), typeof(ScanQrCode.ScanQrCodePage));
			Routing.RegisterRoute(nameof(XmppCommunication.XmppCommunicationPage), typeof(XmppCommunication.XmppCommunicationPage));

			// Identity:
			Routing.RegisterRoute(nameof(Identity.ViewIdentity.ViewIdentityPage), typeof(Identity.ViewIdentity.ViewIdentityPage));
			Routing.RegisterRoute(nameof(Identity.PetitionIdentity.PetitionIdentityPage), typeof(Identity.PetitionIdentity.PetitionIdentityPage));

			// Contacts:
			Routing.RegisterRoute(nameof(Contacts.MyContacts.MyContactsPage), typeof(Contacts.MyContacts.MyContactsPage));

			// Contracts:
			Routing.RegisterRoute(nameof(Contracts.ClientSignature.ClientSignaturePage), typeof(Contracts.ClientSignature.ClientSignaturePage));
			Routing.RegisterRoute(nameof(Contracts.ContractTemplates.ContractTemplatesPage), typeof(Contracts.ContractTemplates.ContractTemplatesPage));
			Routing.RegisterRoute(nameof(Contracts.MyContracts.MyContractsPage), typeof(Contracts.MyContracts.MyContractsPage));
			Routing.RegisterRoute(nameof(Contracts.NewContract.NewContractPage), typeof(Contracts.NewContract.NewContractPage));
			Routing.RegisterRoute(nameof(Contracts.PetitionContract.PetitionContractPage), typeof(Contracts.PetitionContract.PetitionContractPage));
			Routing.RegisterRoute(nameof(Contracts.PetitionSignature.PetitionSignaturePage), typeof(Contracts.PetitionSignature.PetitionSignaturePage));
			Routing.RegisterRoute(nameof(Contracts.ServerSignature.ServerSignaturePage), typeof(Contracts.ServerSignature.ServerSignaturePage));
			Routing.RegisterRoute(nameof(Contracts.SignedContracts.SignedContractsPage), typeof(Contracts.SignedContracts.SignedContractsPage));
			Routing.RegisterRoute(nameof(Contracts.ViewContract.ViewContractPage), typeof(Contracts.ViewContract.ViewContractPage));

			// Things
			Routing.RegisterRoute(nameof(Things.ViewClaimThing.ViewClaimThingPage), typeof(Things.ViewClaimThing.ViewClaimThingPage));
			Routing.RegisterRoute(nameof(Things.MyThings.MyThingsPage), typeof(Things.MyThings.MyThingsPage));
			Routing.RegisterRoute(nameof(Things.ViewThing.ViewThingPage), typeof(Things.ViewThing.ViewThingPage));
			
			// Wallet
			Routing.RegisterRoute(nameof(Wallet.IssueEDaler.IssueEDalerPage), typeof(Wallet.IssueEDaler.IssueEDalerPage));
			Routing.RegisterRoute(nameof(Wallet.EDalerReceived.EDalerReceivedPage), typeof(Wallet.EDalerReceived.EDalerReceivedPage));
			Routing.RegisterRoute(nameof(Wallet.MyWallet.MyWalletPage), typeof(Wallet.MyWallet.MyWalletPage));
			Routing.RegisterRoute(nameof(Wallet.RequestPayment.RequestPaymentPage), typeof(Wallet.RequestPayment.RequestPaymentPage));
			Routing.RegisterRoute(nameof(Wallet.Payment.PaymentPage), typeof(Wallet.Payment.PaymentPage));
			Routing.RegisterRoute(nameof(Wallet.PaymentAcceptance.PaymentAcceptancePage), typeof(Wallet.PaymentAcceptance.PaymentAcceptancePage));
			Routing.RegisterRoute(nameof(Wallet.PendingPayment.PendingPaymentPage), typeof(Wallet.PendingPayment.PendingPaymentPage));
			Routing.RegisterRoute(nameof(Wallet.AccountEvent.AccountEventPage), typeof(Wallet.AccountEvent.AccountEventPage));
		}

		private async Task GoToPage(string route)
		{
			// Due to a bug in Xamarin.Forms the Flyout won't hide when you click on a MenuItem (as opposed to a FlyoutItem).
			// Therefore we have to close it manually here.
			Current.FlyoutIsPresented = false;

			// Due to a bug in Xamarin Shell the menu items can still be clicked on, even though we bind the "IsEnabled" property.
			// So we do a manual check here.
			if (this.GetViewModel<AppShellViewModel>().IsConnected)
			{
				await this.NavigationService?.GoToAsync(route);
			}
		}

		private async void ViewIdentityMenuItem_Clicked(object sender, EventArgs e)
		{
			await this.GoToPage(nameof(Identity.ViewIdentity.ViewIdentityPage));
		}

		internal async void ScanQrCodeMenuItem_Clicked(object sender, EventArgs e)
		{
            await QrCode.ScanQrCodeAndHandleResult(this.LogService, this.NeuronService, this.NavigationService,
                this.UiDispatcher, this.ContractOrchestratorService, this.ThingRegistryOrchestratorService,
				this.EDalerOrchestratorService);
		}

		private async void MyContractsMenuItem_Clicked(object sender, EventArgs e)
		{
			await this.GoToPage(nameof(Pages.Contracts.MyContracts.MyContractsPage));
		}

		private async void SignedContractsMenuItem_Clicked(object sender, EventArgs e)
		{
			await this.GoToPage(nameof(Pages.Contracts.SignedContracts.SignedContractsPage));
		}

		private async void NewContractMenuItem_Clicked(object sender, EventArgs e)
		{
			await this.GoToPage(nameof(Pages.Contracts.ContractTemplates.ContractTemplatesPage));
		}

		private async void DebugMenuItem_Clicked(object sender, EventArgs e)
		{
			try
			{
				await App.EvaluateDatabaseDiff("Debug.xml", true, false);
			}
			catch (Exception ex)
			{
				this.LogService.LogException(ex);
			}

			await this.GoToPage(nameof(XmppCommunication.XmppCommunicationPage));
		}

		private void ExitMenuItem_Clicked(object sender, EventArgs e)
		{
			Current.FlyoutIsPresented = false;
			// Break the call chain by 'posting' to the main thread, allowing the fly out menu to hide before initiating the login/out.
			this.UiDispatcher.BeginInvokeOnMainThread(async () =>
			{
				await App.Stop();
			});
		}

		private void AboutMenuItem_Clicked(object sender, EventArgs e)
		{
			Current.FlyoutIsPresented = false;
			this.UiDispatcher.BeginInvokeOnMainThread(async () =>
			{
				StringBuilder sb = new StringBuilder();
				sb.AppendLine($"Name: {AppInfo.Name}");
				sb.AppendLine($"Version: {AppInfo.VersionString}");
				sb.AppendLine($"Build date: {XmppConfiguration.BuildDate}");
				sb.AppendLine($"Build time: {XmppConfiguration.BuildTime}");
				sb.AppendLine($"Platform: {Device.RuntimePlatform}");
				sb.AppendLine($"RuntimeVersion: {GetType().Assembly.ImageRuntimeVersion}");
				sb.AppendLine($"Phone: {DeviceInfo.Manufacturer} {DeviceInfo.Model}");
				await this.UiDispatcher.DisplayAlert(AppResources.About, sb.ToString());
			});
		}

		internal async void ContactsMenuItem_Clicked(object sender, EventArgs e)
		{
			await this.NavigationService.GoToAsync(nameof(Contacts.MyContacts.MyContactsPage), 
				new Contacts.ContactListNavigationArgs(AppResources.ContactsDescription, Contacts.SelectContactAction.ViewIdentity));
		}

		internal async void ThingsMenuItem_Clicked(object sender, EventArgs e)
		{
			await this.GoToPage(nameof(Things.MyThings.MyThingsPage));
		}

	}
}