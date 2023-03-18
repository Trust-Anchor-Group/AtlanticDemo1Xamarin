﻿using IdApp.Services.AttachmentCache;
using IdApp.Services.Contracts;
using IdApp.Services.Crypto;
using IdApp.Services.EventLog;
using IdApp.Services.Navigation;
using IdApp.Services.Network;
using IdApp.Services.Xmpp;
using IdApp.Services.Nfc;
using IdApp.Services.Settings;
using IdApp.Services.Storage;
using IdApp.Services.Tag;
using IdApp.Services.UI;
using Xamarin.Forms;
using IdApp.Services.Push;
using IdApp.Services.Notification;

namespace IdApp.Services
{
    /// <summary>
    /// Abstract base class for (bindable) classes that reference services in the app.
    /// </summary>
    public class ServiceReferences : BindableObject, IServiceReferences
    {
        /// <summary>
        /// Abstract base class for (bindable) classes that reference services in the app.
        /// </summary>
        public ServiceReferences()
        {
        }

        private IXmppService xmppService;
        private IUiSerializer uiSerializer;
        private ITagProfile tagProfile;
        private INavigationService navigationService;
        private ILogService logService;
        private INetworkService networkService;
        private IContractOrchestratorService contractOrchestratorService;
        private IAttachmentCacheService attachmentCacheService;
        private ICryptoService cryptoService;
        private ISettingsService settingsService;
        private IStorageService storageService;
        private INfcService nfcService;
        private INotificationService notificationService;
        private IPushNotificationService pushNotificationService;

        /// <summary>
        /// The dispatcher to use for alerts and accessing the main thread.
        /// </summary>
        public IUiSerializer UiSerializer 
        { 
            get
			{
                if (this.uiSerializer is null)
                    this.uiSerializer = App.Instantiate<IUiSerializer>();

                return this.uiSerializer;
			}
        }

        /// <summary>
        /// The XMPP service for XMPP communication.
        /// </summary>
        public IXmppService XmppService
        {
            get
            {
                if (this.xmppService is null)
                    this.xmppService = App.Instantiate<IXmppService>();

                return this.xmppService;
            }
        }

        /// <summary>
        /// TAG Profie service.
        /// </summary>
        public ITagProfile TagProfile
        {
            get
            {
                if (this.tagProfile is null)
                    this.tagProfile = App.Instantiate<ITagProfile>();

                return this.tagProfile;
            }
        }

        /// <summary>
        /// Navigation service.
        /// </summary>
        public INavigationService NavigationService
        {
            get
            {
                if (this.navigationService is null)
                    this.navigationService = App.Instantiate<INavigationService>();

                return this.navigationService;
            }
        }

        /// <summary>
        /// Log service.
        /// </summary>
        public ILogService LogService
        {
            get
            {
                if (this.logService is null)
                    this.logService = App.Instantiate<ILogService>();

                return this.logService;
            }
        }

        /// <summary>
        /// Network service.
        /// </summary>
        public INetworkService NetworkService
        {
            get
            {
                if (this.networkService is null)
                    this.networkService = App.Instantiate<INetworkService>();

                return this.networkService;
            }
        }

        /// <summary>
        /// Contract orchestrator service.
        /// </summary>
        public IContractOrchestratorService ContractOrchestratorService
        {
            get
            {
                if (this.contractOrchestratorService is null)
                    this.contractOrchestratorService = App.Instantiate<IContractOrchestratorService>();

                return this.contractOrchestratorService;
            }
        }

        /// <summary>
        /// AttachmentCache service.
        /// </summary>
        public IAttachmentCacheService AttachmentCacheService
        {
            get
            {
                if (this.attachmentCacheService is null)
                    this.attachmentCacheService = App.Instantiate<IAttachmentCacheService>();

                return this.attachmentCacheService;
            }
        }

        /// <summary>
        /// Crypto service.
        /// </summary>
        public ICryptoService CryptoService
        {
            get
            {
                if (this.cryptoService is null)
                    this.cryptoService = App.Instantiate<ICryptoService>();

                return this.cryptoService;
            }
        }

        /// <summary>
        /// Settings service.
        /// </summary>
        public ISettingsService SettingsService
        {
            get
            {
                if (this.settingsService is null)
                    this.settingsService = App.Instantiate<ISettingsService>();

                return this.settingsService;
            }
        }

        /// <summary>
        /// Storage service.
        /// </summary>
        public IStorageService StorageService
        {
            get
            {
                if (this.storageService is null)
                    this.storageService = App.Instantiate<IStorageService>();

                return this.storageService;
            }
        }

        /// <summary>
        /// Near-Field Communication (NFC) service.
        /// </summary>
        public INfcService NfcService
        {
            get
            {
                if (this.nfcService is null)
                    this.nfcService = App.Instantiate<INfcService>();

                return this.nfcService;
            }
        }

		/// <summary>
		/// Notification Service
		/// </summary>
		public INotificationService NotificationService
		{
			get
			{
				if (this.notificationService is null)
					this.notificationService = App.Instantiate<INotificationService>();

				return this.notificationService;
			}
		}

		/// <summary>
		/// Push Notification Service
		/// </summary>
		public IPushNotificationService PushNotificationService
        {
            get
            {
                if (this.pushNotificationService is null)
                    this.pushNotificationService = App.Instantiate<IPushNotificationService>();

                return this.pushNotificationService;
            }
        }

    }
}
