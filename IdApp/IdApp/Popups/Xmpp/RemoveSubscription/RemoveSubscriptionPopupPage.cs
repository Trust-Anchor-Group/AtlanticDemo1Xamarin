using System;
using System.Threading.Tasks;
using Rg.Plugins.Popup.Pages;
using Rg.Plugins.Popup.Services;

namespace IdApp.Popups.Xmpp.RemoveSubscription
{
    /// <summary>
    /// Asks the user if it wants to remove an existing presence subscription request as well.
    /// </summary>
    public partial class RemoveSubscriptionPopupPage : PopupPage
    {
        private readonly TaskCompletionSource<bool?> result = new TaskCompletionSource<bool?>();
        private readonly string bareJid;

        /// <summary>
        /// Asks the user if it wants to remove an existing presence subscription request as well.
        /// </summary>
        /// <param name="BareJid">Bare JID of sender of request.</param>
        public RemoveSubscriptionPopupPage(string BareJid)
        {
            this.bareJid = BareJid;

            InitializeComponent();

            this.BindingContext = this;
        }

        /// <summary>
        /// Bare JID of sender of request.
        /// </summary>
        public string BareJid => this.bareJid;

        private void OnCloseButtonTapped(object sender, EventArgs e)
        {
            Close();
        }

        /// <inheritdoc/>
        protected override bool OnBackgroundClicked()
        {
            Close();
            return false;
        }

        private async void Close()
        {
            await PopupNavigation.Instance.PopAsync();
            this.result.TrySetResult(null);
        }

        private async void OnYes(object sender, EventArgs e)
        {
            await PopupNavigation.Instance.PopAsync();
            this.result.TrySetResult(true);
        }

        private async void OnNo(object sender, EventArgs e)
        {
            await PopupNavigation.Instance.PopAsync();
            this.result.TrySetResult(false);
        }

        /// <summary>
        /// Task waiting for result.
        /// </summary>
        public Task<bool?> Result => this.result.Task;
    }
}