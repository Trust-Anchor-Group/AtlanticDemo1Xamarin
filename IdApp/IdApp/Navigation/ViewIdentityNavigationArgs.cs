﻿using Tag.Neuron.Xamarin.Services;
using Waher.Networking.XMPP.Contracts;

namespace IdApp.Navigation
{
    public class ViewIdentityNavigationArgs : NavigationArgs
    {
        public ViewIdentityNavigationArgs(LegalIdentity identity, SignaturePetitionEventArgs identityToReview)
        {
            this.Identity = identity;
            this.IdentityToReview = identityToReview;
        }
        
        public LegalIdentity Identity { get; }
        public SignaturePetitionEventArgs IdentityToReview { get; }
    }
}