﻿using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using IdApp.Pages.Contacts.Chat;
using IdApp.Pages.Identity.ViewIdentity;
using IdApp.Services;
using IdApp.Services.Notification;
using IdApp.Services.UI.QR;
using Waher.Networking.XMPP;
using Waher.Persistence;
using Xamarin.CommunityToolkit.Helpers;
using Xamarin.Forms;

namespace IdApp.Pages.Contacts.MyContacts
{
	/// <summary>
	/// The view model to bind to when displaying the list of contacts.
	/// </summary>
	public class ContactListViewModel : BaseViewModel
	{
		private readonly Dictionary<CaseInsensitiveString, List<ContactInfoModel>> byBareJid;
		private TaskCompletionSource<ContactInfoModel> selection;

		/// <summary>
		/// Creates an instance of the <see cref="ContactListViewModel"/> class.
		/// </summary>
		protected internal ContactListViewModel()
		{
			this.AnonymousCommand = new Command(_ => this.SelectAnonymous());
			this.ScanQrCodeCommand = new Command(async _ => await this.ScanQrCode());

			this.Contacts = new ObservableCollection<ContactInfoModel>();
			this.byBareJid = new();

			this.Description = LocalizationResourceManager.Current["ContactsDescription"];
			this.Action = SelectContactAction.ViewIdentity;
			this.selection = null;
		}

		/// <inheritdoc/>
		protected override async Task OnInitialize()
		{
			await base.OnInitialize();

			if (this.NavigationService.TryPopArgs(out ContactListNavigationArgs args))
			{
				this.Description = args.Description;
				this.Action = args.Action;
				this.selection = args.Selection;
				this.CanScanQrCode = args.CanScanQrCode;
				this.AllowAnonymous = args.AllowAnonymous;
				this.AnonymousText = string.IsNullOrEmpty(args.AnonymousText) ? LocalizationResourceManager.Current["Anonymous"] : args.AnonymousText;
			}

			SortedDictionary<CaseInsensitiveString, ContactInfo> Sorted = new();
			Dictionary<CaseInsensitiveString, bool> Jids = new();

			foreach (ContactInfo Info in await Database.Find<ContactInfo>())
			{
				Jids[Info.BareJid] = true;

				if (Info.IsThing.HasValue && Info.IsThing.Value)        // Include those with IsThing=null
					continue;

				if (Info.AllowSubscriptionFrom.HasValue && !Info.AllowSubscriptionFrom.Value)
					continue;

				Add(Sorted, Info.FriendlyName, Info);
			}

			foreach (RosterItem Item in this.XmppService.Roster)
			{
				if (Jids.ContainsKey(Item.BareJid))
					continue;

				ContactInfo Info = new()
				{
					BareJid = Item.BareJid,
					FriendlyName = Item.NameOrBareJid,
					IsThing = null
				};

				await Database.Insert(Info);

				Add(Sorted, Info.FriendlyName, Info);
			}

			NotificationEvent[] Events;

			this.Contacts.Clear();

			foreach (CaseInsensitiveString Category in this.NotificationService.GetCategories(EventButton.Contacts))
			{
				if (this.NotificationService.TryGetNotificationEvents(EventButton.Contacts, Category, out Events))
				{
					if (Sorted.TryGetValue(Category, out ContactInfo Info))
						Sorted.Remove(Category);
					else
					{
						Info = await ContactInfo.FindByBareJid(Category);

						if (Info is not null)
							Remove(Sorted, Info.FriendlyName, Info);
						else
						{
							Info = new()
							{
								BareJid = Category,
								FriendlyName = Category,
								IsThing = null
							};
						}
					}

					this.Contacts.Add(new ContactInfoModel(this, Info, Events));
				}
			}

			foreach (ContactInfo Info in Sorted.Values)
			{
				if (!this.NotificationService.TryGetNotificationEvents(EventButton.Contacts, Info.BareJid, out Events))
					Events = new NotificationEvent[0];

				this.Contacts.Add(new ContactInfoModel(this, Info, Events));
			}

			this.byBareJid.Clear();

			foreach (ContactInfoModel Contact in this.Contacts)
			{
				if (string.IsNullOrEmpty(Contact.BareJid))
					continue;

				if (!this.byBareJid.TryGetValue(Contact.BareJid, out List<ContactInfoModel> Contacts))
				{
					Contacts = new List<ContactInfoModel>();
					this.byBareJid[Contact.BareJid] = Contacts;
				}

				Contacts.Add(Contact);
			}

			this.ShowContactsMissing = Sorted.Count == 0;

			this.XmppService.OnPresence += this.Xmpp_OnPresence;
			this.NotificationService.OnNewNotification += this.NotificationService_OnNewNotification;
			this.NotificationService.OnNotificationsDeleted += this.NotificationService_OnNotificationsDeleted;
		}

		/// <inheritdoc/>
		protected override async Task OnAppearing()
		{
			await base.OnAppearing();

			if (this.selection is not null && this.selection.Task.IsCompleted)
			{
				await this.NavigationService.GoBackAsync();
				return;
			}

			this.SelectedContact = null;
		}

		private static void Add(SortedDictionary<CaseInsensitiveString, ContactInfo> Sorted, CaseInsensitiveString Name, ContactInfo Info)
		{
			if (Sorted.ContainsKey(Name))
			{
				int i = 1;
				string Suffix;

				do
				{
					Suffix = " " + (++i).ToString();
				}
				while (Sorted.ContainsKey(Name + Suffix));

				Sorted[Name + Suffix] = Info;
			}
			else
				Sorted[Name] = Info;
		}

		private static void Remove(SortedDictionary<CaseInsensitiveString, ContactInfo> Sorted, CaseInsensitiveString Name, ContactInfo Info)
		{
			int i = 1;
			string Suffix = string.Empty;

			while (Sorted.TryGetValue(Name + Suffix, out ContactInfo Info2))
			{
				if (Info2.BareJid == Info.BareJid &&
					Info2.SourceId == Info.SourceId &&
					Info2.Partition == Info.Partition &&
					Info2.NodeId == Info.NodeId &&
					Info2.LegalId == Info.LegalId)
				{
					Sorted.Remove(Name + Suffix);

					i++;
					string Suffix2 = " " + i.ToString();

					while (Sorted.TryGetValue(Name + Suffix2, out Info2))
					{
						Sorted[Name + Suffix] = Info2;
						Sorted.Remove(Name + Suffix2);

						i++;
						Suffix2 = " " + i.ToString();
					}

					return;
				}

				i++;
				Suffix = " " + i.ToString();
			}
		}

		/// <inheritdoc/>
		protected override Task OnDispose()
		{
			this.XmppService.OnPresence -= this.Xmpp_OnPresence;
			this.NotificationService.OnNewNotification -= this.NotificationService_OnNewNotification;
			this.NotificationService.OnNotificationsDeleted -= this.NotificationService_OnNotificationsDeleted;

			if (this.Action != SelectContactAction.Select)
			{
				this.ShowContactsMissing = false;
				this.Contacts.Clear();
			}

			this.selection?.TrySetResult(null);

			return base.OnDispose();
		}

		/// <summary>
		/// See <see cref="ShowContactsMissing"/>
		/// </summary>
		public static readonly BindableProperty ShowContactsMissingProperty =
			BindableProperty.Create(nameof(ShowContactsMissing), typeof(bool), typeof(ContactListViewModel), default(bool));

		/// <summary>
		/// Gets or sets whether to show a contacts missing alert or not.
		/// </summary>
		public bool ShowContactsMissing
		{
			get => (bool)this.GetValue(ShowContactsMissingProperty);
			set => this.SetValue(ShowContactsMissingProperty, value);
		}

		/// <summary>
		/// <see cref="Description"/>
		/// </summary>
		public static readonly BindableProperty DescriptionProperty =
			BindableProperty.Create(nameof(Description), typeof(string), typeof(ContactListViewModel), default(string));

		/// <summary>
		/// The description to present to the user.
		/// </summary>
		public string Description
		{
			get => (string)this.GetValue(DescriptionProperty);
			set => this.SetValue(DescriptionProperty, value);
		}

		/// <summary>
		/// <see cref="CanScanQrCode"/>
		/// </summary>
		public static readonly BindableProperty CanScanQrCodeProperty =
			BindableProperty.Create(nameof(CanScanQrCode), typeof(bool), typeof(ContactListViewModel), default(bool));

		/// <summary>
		/// The description to present to the user.
		/// </summary>
		public bool CanScanQrCode
		{
			get => (bool)this.GetValue(CanScanQrCodeProperty);
			set => this.SetValue(CanScanQrCodeProperty, value);
		}

		/// <summary>
		/// <see cref="AllowAnonymous"/>
		/// </summary>
		public static readonly BindableProperty AllowAnonymousProperty =
			BindableProperty.Create(nameof(AllowAnonymous), typeof(bool), typeof(ContactListViewModel), default(bool));

		/// <summary>
		/// The description to present to the user.
		/// </summary>
		public bool AllowAnonymous
		{
			get => (bool)this.GetValue(AllowAnonymousProperty);
			set => this.SetValue(AllowAnonymousProperty, value);
		}

		/// <summary>
		/// <see cref="AnonymousText"/>
		/// </summary>
		public static readonly BindableProperty AnonymousTextProperty =
			BindableProperty.Create(nameof(AnonymousText), typeof(string), typeof(ContactListViewModel), default(string));

		/// <summary>
		/// The description to present to the user.
		/// </summary>
		public string AnonymousText
		{
			get => (string)this.GetValue(AnonymousTextProperty);
			set => this.SetValue(AnonymousTextProperty, value);
		}

		/// <summary>
		/// <see cref="Action"/>
		/// </summary>
		public static readonly BindableProperty ActionProperty =
			BindableProperty.Create(nameof(Action), typeof(SelectContactAction), typeof(ContactListViewModel), default(SelectContactAction));

		/// <summary>
		/// The action to take when contact has been selected.
		/// </summary>
		public SelectContactAction Action
		{
			get => (SelectContactAction)this.GetValue(ActionProperty);
			set => this.SetValue(ActionProperty, value);
		}

		/// <summary>
		/// Holds the list of contacts to display.
		/// </summary>
		public ObservableCollection<ContactInfoModel> Contacts { get; }

		/// <summary>
		/// See <see cref="SelectedContact"/>
		/// </summary>
		public static readonly BindableProperty SelectedContactProperty =
			BindableProperty.Create(nameof(SelectedContact), typeof(ContactInfoModel), typeof(ContactListViewModel), default(ContactInfoModel));

		/// <summary>
		/// The currently selected contact, if any.
		/// </summary>
		public ContactInfoModel SelectedContact
		{
			get => (ContactInfoModel)this.GetValue(SelectedContactProperty);
			set
			{
				this.SetValue(SelectedContactProperty, value);

				if (value is not null)
					this.OnSelected(value);
			}
		}

		private void OnSelected(ContactInfoModel Contact)
		{
			this.UiSerializer.BeginInvokeOnMainThread(async () =>
			{
				this.IsOverlayVisible = true;

				try
				{
					switch (this.Action)
					{
						case SelectContactAction.ViewIdentity:
						default:
							if (Contact.LegalIdentity is not null)
							{
								await this.NavigationService.GoToAsync(nameof(ViewIdentityPage),
									new ViewIdentityNavigationArgs(Contact.LegalIdentity));
							}
							else if (!string.IsNullOrEmpty(Contact.LegalId))
								await this.ContractOrchestratorService.OpenLegalIdentity(Contact.LegalId, LocalizationResourceManager.Current["ScannedQrCode"]);
							else if (!string.IsNullOrEmpty(Contact.BareJid))
							{
								await this.NavigationService.GoToAsync(nameof(ChatPage), new ChatNavigationArgs(Contact.Contact)
								{
									UniqueId = Contact.BareJid
								});
							}

							break;

						case SelectContactAction.Select:
							this.selection?.TrySetResult(Contact);
							await this.NavigationService.GoBackAsync();
							break;
					}
				}
				finally
				{
					this.IsOverlayVisible = false;
				}
			});
		}

		/// <summary>
		/// Command executed when the user wants to scan a contact from a QR Code.
		/// </summary>
		public ICommand ScanQrCodeCommand { get; }

		/// <summary>
		/// Command executed when the user wants to perform an action with someone anonymous.
		/// </summary>
		public ICommand AnonymousCommand { get; }

		private async Task ScanQrCode()
		{
			string Code = await QrCode.ScanQrCode(LocalizationResourceManager.Current["ScanQRCode"]);
			if (string.IsNullOrEmpty(Code))
				return;

			if (Constants.UriSchemes.StartsWithIdScheme(Code))
			{
				this.OnSelected(new ContactInfoModel(this, new ContactInfo()
				{
					LegalId = Constants.UriSchemes.RemoveScheme(Code)
				}, new NotificationEvent[0]));
			}
			else if (!string.IsNullOrEmpty(Code))
				await this.UiSerializer.DisplayAlert(LocalizationResourceManager.Current["ErrorTitle"], LocalizationResourceManager.Current["TheSpecifiedCodeIsNotALegalIdentity"]);
		}

		private void SelectAnonymous()
		{
			this.OnSelected(new ContactInfoModel(this, null, new NotificationEvent[0]));
		}

		private Task Xmpp_OnPresence(object Sender, PresenceEventArgs e)
		{
			if (this.byBareJid.TryGetValue(e.FromBareJID, out List<ContactInfoModel> Contacts))
			{
				foreach (ContactInfoModel Contact in Contacts)
					Contact.PresenceUpdated();
			}

			return Task.CompletedTask;
		}

		private void NotificationService_OnNewNotification(object Sender, NotificationEventArgs e)
		{
			if (e.Event.Button == EventButton.Contacts)
				this.UpdateNotifications(e.Event.Category);
		}

		private void UpdateNotifications()
		{
			foreach (CaseInsensitiveString Category in this.NotificationService.GetCategories(EventButton.Contacts))
				this.UpdateNotifications(Category);
		}

		private void UpdateNotifications(CaseInsensitiveString Category)
		{
			if (this.byBareJid.TryGetValue(Category, out List<ContactInfoModel> Contacts) &&
				this.NotificationService.TryGetNotificationEvents(EventButton.Contacts, Category, out NotificationEvent[] Events))
			{
				foreach (ContactInfoModel Contact in Contacts)
					Contact.NotificationsUpdated(Events);
			}
		}

		private void NotificationService_OnNotificationsDeleted(object Sender, NotificationEventsArgs e)
		{
			this.UpdateNotifications();
		}

	}
}
