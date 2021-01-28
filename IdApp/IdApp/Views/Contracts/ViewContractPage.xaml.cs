﻿using IdApp.ViewModels.Contracts;
using Tag.Neuron.Xamarin.Services;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace IdApp.Views.Contracts
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
	public partial class ViewContractPage
	{
        private readonly INavigationService navigationService;

		public ViewContractPage()
		{
            this.navigationService = DependencyService.Resolve<INavigationService>();
            this.ViewModel = new ViewContractViewModel();
			InitializeComponent();
		}

        protected override bool OnBackButtonPressed()
        {
            this.navigationService.GoBackAsync();
            return true;
        }
	}
}