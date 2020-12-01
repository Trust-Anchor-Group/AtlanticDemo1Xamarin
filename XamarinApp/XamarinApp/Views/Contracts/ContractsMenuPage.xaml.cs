﻿using System;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;
using XamarinApp.Services;

namespace XamarinApp.Views.Contracts
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class ContractsMenuPage
    {
        private readonly INavigationService navigationService;

        public ContractsMenuPage()
        {
            this.BindingContext = this;
            this.navigationService = DependencyService.Resolve<INavigationService>();
            InitializeComponent();
        }

        private async void CreatedContractsButton_Clicked(object sender, EventArgs e)
        {
            await this.navigationService.PushAsync(new MyContractsPage(true));
        }

        private async void SignedContractsButton_Clicked(object sender, EventArgs e)
        {
            await this.navigationService.PushAsync(new MyContractsPage(false));
        }

        private async void NotaryButton_Clicked(object sender, EventArgs e)
        {
            await this.navigationService.PushAsync(new NotaryMenuPage());
        }

        protected override bool OnBackButtonPressed()
        {
            this.navigationService.PopAsync();
            return true;
        }
    }
}