﻿using CoreGraphics;
using Firebase.CloudMessaging;
using Foundation;
using IdApp.Helpers;
using IdApp.Services;
using IdApp.Services.Xmpp;
using System;
using System.Threading.Tasks;
using UIKit;
using UserNotifications;
using Waher.Events;
using Waher.Runtime.Inventory;
using Xamarin.CommunityToolkit.Helpers;
using Xamarin.Forms;

namespace IdApp.iOS
{
	// The UIApplicationDelegate for the application. This class is responsible for launching the
	// User Interface of the application, as well as listening (and optionally responding) to
	// application events from iOS.
	[Register("AppDelegate")]
	public partial class AppDelegate : global::Xamarin.Forms.Platform.iOS.FormsApplicationDelegate, IMessagingDelegate
	{
		private NSObject onKeyboardShowObserver;
		private NSObject onKeyboardHideObserver;

		//
		// This method is invoked when the application has loaded and is ready to run. In this
		// method you should instantiate the window, load the UI into it and then make the window
		// visible.
		//
		// You have 17 seconds to return from this method, or iOS will terminate your application.
		//
		public override bool FinishedLaunching(UIApplication Application, NSDictionary Options)
		{
			UIApplication.SharedApplication.StatusBarStyle = UIStatusBarStyle.LightContent;

			Firebase.Core.App.Configure();
			Rg.Plugins.Popup.Popup.Init();
			ZXing.Net.Mobile.Forms.iOS.Platform.Init();
			FFImageLoading.Forms.Platform.CachedImageRenderer.Init();
			Xamarin.Forms.Forms.Init();

			// This must be called after Xamarin.Forms.Forms.Init.
			FFImageLoading.Forms.Platform.CachedImageRenderer.InitImageSourceHandler();

			FFImageLoading.Config.Configuration Configuration = FFImageLoading.Config.Configuration.Default;
			Configuration.DiskCacheDuration = TimeSpan.FromDays(1);
			FFImageLoading.ImageService.Instance.Initialize(Configuration);

			// Uncomment this to debug loading images from neuron (ensures that they are not loaded from cache).
			// FFImageLoading.ImageService.Instance.InvalidateCacheAsync(FFImageLoading.Cache.CacheType.Disk);

			this.LoadApplication(new App());

			this.RegisterKeyBoardObserver();
			this.RegisterRemoteNotifications();

			return base.FinishedLaunching(Application, Options);
		}

		public override void WillTerminate(UIApplication application)
		{
			if (this.onKeyboardShowObserver is null)
			{
				this.onKeyboardShowObserver.Dispose();
				this.onKeyboardShowObserver = null;
			}

			if (this.onKeyboardHideObserver is null)
			{
				this.onKeyboardHideObserver.Dispose();
				this.onKeyboardHideObserver = null;
			}
		}

		public override void WillEnterForeground(UIApplication Application)
		{
			base.WillEnterForeground(Application);

			this.RemoveAllNotifications();
		}

		/// <summary>
		/// Method is called when an URL with a registered schema is being opened.
		/// </summary>
		/// <param name="app">Application</param>
		/// <param name="url">URL</param>
		/// <param name="options">Options</param>
		/// <returns>If URL is handled.</returns>
		public override bool OpenUrl(UIApplication Application, NSUrl Url, NSDictionary Options)
		{
			App.OpenUrlSync(Url.AbsoluteString);
			return true;
		}

		private void RegisterKeyBoardObserver()
		{
			if (this.onKeyboardShowObserver is null)
			{
				this.onKeyboardShowObserver = UIKeyboard.Notifications.ObserveWillShow((object Sender, UIKeyboardEventArgs args) =>
				{
					NSValue Result = (NSValue)args.Notification.UserInfo.ObjectForKey(new NSString(UIKeyboard.FrameEndUserInfoKey));
					CGSize keyboardSize = Result.RectangleFValue.Size;

					MessagingCenter.Send<object, KeyboardAppearEventArgs>(this, Constants.MessagingCenter.KeyboardAppears,
						new KeyboardAppearEventArgs { KeyboardSize = (float)keyboardSize.Height });
				});
			}

			if (this.onKeyboardHideObserver is null)
			{
				this.onKeyboardHideObserver = UIKeyboard.Notifications.ObserveWillHide((object Sender, UIKeyboardEventArgs args) =>
				{
					MessagingCenter.Send<object>(this, Constants.MessagingCenter.KeyboardDisappears);
				});
			}
		}

		private void RegisterRemoteNotifications()
		{
			this.RemoveAllNotifications();

			// For display notification sent via APNS
			UNUserNotificationCenter.Current.Delegate = new UserNotificationCenterDelegate();
			// For data message sent via FCM
			Messaging.SharedInstance.Delegate = this;

			UNAuthorizationOptions AuthOptions = UNAuthorizationOptions.Alert | UNAuthorizationOptions.Badge | UNAuthorizationOptions.Sound;
			UNUserNotificationCenter.Current.RequestAuthorization(AuthOptions, (granted, error) =>
			{
			});

			UIApplication.SharedApplication.RegisterForRemoteNotifications();
		}

		[Export("application:didReceiveRemoteNotification:fetchCompletionHandler:")]
		public override async void DidReceiveRemoteNotification(UIApplication Application, NSDictionary UserInfo, Action<UIBackgroundFetchResult> CompletionHandler)
		{
			try
			{
				if (!App.IsOnboarded)
					return;

				string MessageId = UserInfo.ObjectForKey(new NSString("gcm.message_id")).ToString();
				string ChannelId = UserInfo.ObjectForKey(new NSString("channelId"))?.ToString() ?? string.Empty;
				string Title = UserInfo.ObjectForKey(new NSString("myTitle"))?.ToString() ?? string.Empty;
				string Body = UserInfo.ObjectForKey(new NSString("myBody"))?.ToString() ?? string.Empty;
				string AttachmentIcon = null;

				switch (ChannelId)
				{
					case Constants.PushChannels.Messages:
						AttachmentIcon = "NotificationChatIcon";
						Body = this.GetChatNotificationBody(Body, UserInfo);
						break;

					case Constants.PushChannels.Petitions:
						AttachmentIcon = "NotificationPetitionIcon";
						Body = this.GetPetitionNotificationBody(Body, UserInfo);
						break;

					case Constants.PushChannels.Identities:
						AttachmentIcon = "NotificationIdentitieIcon";
						Body = this.GetIdentitieNotificationBody(Body, UserInfo);
						break;

					case Constants.PushChannels.Contracts:
						AttachmentIcon = "NotificationContractIcon";
						Body = this.GetContractNotificationBody(Body, UserInfo);
						break;

					case Constants.PushChannels.EDaler:
						AttachmentIcon = "NotificationEDalerIcon";
						Body = this.GetEDalerNotificationBody(Body, UserInfo);
						break;

					case Constants.PushChannels.Tokens:
						AttachmentIcon = "NotificationTokenIcon";
						Body = this.GetTokenNotificationBody(Body, UserInfo);
						break;

					case Constants.PushChannels.Provisioning:
						AttachmentIcon = "NotificationPetitionIcon";
						Body = await this.GetProvisioningNotificationBody(Body, UserInfo);
						break;

					default:
						break;
				}

				// Create request
				UNMutableNotificationContent Content = new()
				{
					Title = Title,
					Body = Body,
				};

				if (AttachmentIcon is not null)
				{
					UNNotificationAttachmentOptions Options = new();
					NSUrl AttachmentUrl = NSBundle.MainBundle.GetUrlForResource(AttachmentIcon, "png");

					if (AttachmentUrl is not null)
					{
						UNNotificationAttachment Attachment = UNNotificationAttachment.FromIdentifier("image" + MessageId, AttachmentUrl, Options, out _);

						if (Attachment is not null)
						{
							Content.Attachments = new UNNotificationAttachment[] { Attachment };
						}
					}
				}

				UNNotificationRequest Request = UNNotificationRequest.FromIdentifier(MessageId, Content, null);
				UNUserNotificationCenter.Current.AddNotificationRequest(Request, null);

				CompletionHandler(UIBackgroundFetchResult.NewData);
			}
			catch (Exception ex)
			{
				Log.Critical(ex);
			}
		}

		private string GetChatNotificationBody(string MessageBody, NSDictionary UserInfo)
		{
			string IsObject = UserInfo.ObjectForKey(new NSString("isObject"))?.ToString() ?? string.Empty;

			if (!string.IsNullOrEmpty(IsObject) && bool.Parse(IsObject))
				MessageBody = LocalizationResourceManager.Current["MessageReceived"];

			return MessageBody;
		}

		private string GetPetitionNotificationBody(string MessageBody, NSDictionary UserInfo)
		{
			string FromJid = UserInfo.ObjectForKey(new NSString("fromJid"))?.ToString() ?? string.Empty;
			string RosterName = UserInfo.ObjectForKey(new NSString("rosterName"))?.ToString() ?? string.Empty;

			MessageBody = (string.IsNullOrEmpty(RosterName) ? FromJid : RosterName) + ": " + MessageBody;

			return MessageBody;
		}

		private string GetIdentitieNotificationBody(string MessageBody, NSDictionary UserInfo)
		{
			string LegalId = UserInfo.ObjectForKey(new NSString("legalId"))?.ToString() ?? string.Empty;

			if (!string.IsNullOrEmpty(LegalId))
			{
				MessageBody += "\n(" + LegalId + ")";
			}

			return MessageBody;
		}

		private string GetContractNotificationBody(string MessageBody, NSDictionary UserInfo)
		{
			string LegalId = UserInfo.ObjectForKey(new NSString("legalId"))?.ToString() ?? string.Empty;
			string ContractId = UserInfo.ObjectForKey(new NSString("contractId"))?.ToString() ?? string.Empty;
			string Role = UserInfo.ObjectForKey(new NSString("role"))?.ToString() ?? string.Empty;

			if (!string.IsNullOrEmpty(Role))
			{
				MessageBody += "\n" + Role;
			}

			if (!string.IsNullOrEmpty(ContractId))
			{
				MessageBody += "\n(" + ContractId + ")";
			}

			if (!string.IsNullOrEmpty(LegalId))
			{
				MessageBody += "\n(" + LegalId + ")";
			}

			return MessageBody;
		}

		private string GetEDalerNotificationBody(string MessageBody, NSDictionary UserInfo)
		{
			string Amount = UserInfo.ObjectForKey(new NSString("amount"))?.ToString() ?? string.Empty;
			string Currency = UserInfo.ObjectForKey(new NSString("currency"))?.ToString() ?? string.Empty;
			string Timestamp = UserInfo.ObjectForKey(new NSString("timestamp"))?.ToString() ?? string.Empty;

			if (!string.IsNullOrEmpty(Amount))
			{
				MessageBody += "\n" + Amount;

				if (!string.IsNullOrEmpty(Currency))
				{
					MessageBody += " " + Currency;
				}

				if (!string.IsNullOrEmpty(Timestamp))
				{
					MessageBody += " (" + Timestamp + ")";
				}
			}

			return MessageBody;
		}

		private string GetTokenNotificationBody(string MessageBody, NSDictionary UserInfo)
		{
			string Value = UserInfo.ObjectForKey(new NSString("value"))?.ToString() ?? string.Empty;
			string Currency = UserInfo.ObjectForKey(new NSString("currency"))?.ToString() ?? string.Empty;

			if (!string.IsNullOrEmpty(Value))
			{
				MessageBody += "\n" + Value;

				if (!string.IsNullOrEmpty(Currency))
				{
					MessageBody += " " + Currency;
				}
			}

			return MessageBody;
		}

		private async Task<string> GetProvisioningNotificationBody(string MessageBody, NSDictionary UserInfo)
		{
			string RemoteJid = UserInfo.ObjectForKey(new NSString("remoteJid"))?.ToString() ?? string.Empty;
			string Jid = UserInfo.ObjectForKey(new NSString("jid"))?.ToString() ?? string.Empty;
			//string Key = UserInfo.ObjectForKey(new NSString("key"))?.ToString() ?? string.Empty;
			string Query = UserInfo.ObjectForKey(new NSString("q"))?.ToString() ?? string.Empty;

			IServiceReferences ServiceReferences = App.Instantiate<IXmppService>();

			string ThingName = await ContactInfo.GetFriendlyName(Jid, ServiceReferences);

			if (string.IsNullOrWhiteSpace(MessageBody))
				MessageBody = await ContactInfo.GetFriendlyName(RemoteJid, ServiceReferences);

			return Query switch
			{
				"canRead" => string.Format(LocalizationResourceManager.Current["ReadoutRequestText"], MessageBody, ThingName),
				"canControl" => string.Format(LocalizationResourceManager.Current["ControlRequestText"], MessageBody, ThingName),
				_ => string.Format(LocalizationResourceManager.Current["AccessRequestText"], MessageBody, ThingName),
			};
		}

        private void RemoveAllNotifications()
        {
            UNUserNotificationCenter UNCenter = UNUserNotificationCenter.Current;
            UNCenter.RemoveAllDeliveredNotifications();
            UNCenter.RemoveAllPendingNotificationRequests();
        }
    }

    public class UserNotificationCenterDelegate : UNUserNotificationCenterDelegate
    {
        public UserNotificationCenterDelegate()
        {
        }
	}
}
