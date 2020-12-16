﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Waher.Networking.XMPP.Contracts;
using Xamarin.Forms;
using XamarinApp.Extensions;
using XamarinApp.Models;
using XamarinApp.Services;
using XamarinApp.Views.Contracts;

namespace XamarinApp.ViewModels.Contracts
{
    public class ViewContractViewModel : BaseViewModel
    {
        private Contract contract;
        private readonly ILogService logService;
        private readonly INavigationService navigationService;
        private readonly IContractsService contractsService;
        private readonly IContractOrchestratorService contractOrchestratorService;
        private readonly TagProfile tagProfile;
        private readonly PhotosLoader photosLoader;

        public ViewContractViewModel(Contract contract, bool isReadOnly)
        {
            this.contract = contract;
            this.IsReadOnly = isReadOnly;

            this.logService = DependencyService.Resolve<ILogService>();
            this.navigationService = DependencyService.Resolve<INavigationService>();
            this.contractsService = DependencyService.Resolve<IContractsService>();
            this.contractOrchestratorService = DependencyService.Resolve<IContractOrchestratorService>();
            this.tagProfile = DependencyService.Resolve<TagProfile>();
            this.photosLoader = new PhotosLoader(this.logService, this.contractsService);
            this.DisplayPartCommand = new Command<string>(async legalId => await ShowLegalId(legalId));
            this.SignPartAsRoleCommand = new Command<string>(async roleId => await SignContract(roleId));
            this.DisplayClientSignatureCommand = new Command<string>(async sign => await ShowClientSignature(sign));
            this.DisplayServerSignatureCommand = new Command(async () => await ShowServerSignature());
            this.ObsoleteCommand = new Command(async _ => await ObsoleteContract());
            this.ObsoleteCommand = new Command(async _ => await DeleteContract());
            this.GeneralInformation = new ObservableCollection<PartModel>();
            this.ContractParts = new ObservableCollection<PartModel>();
            this.ContractRoles = new ObservableCollection<PartModel>();
            this.ContractParameters = new ObservableCollection<ParameterModel>();
            this.ContractHumanReadableText = new ObservableCollection<string>();
            this.ContractMachineReadableText = new ObservableCollection<PartModel>();
            this.ContractClientSignatures = new ObservableCollection<PartModel>();
            this.ContractServerSignatures = new ObservableCollection<PartModel>();
            this.Photos = new ObservableCollection<ImageSource>();
        }

        protected override async Task DoBind()
        {
            await base.DoBind();
            await LoadContract();
        }

        protected override async Task DoUnbind()
        {
            this.ClearContract();
            await base.DoUnbind();
        }

        #region Properties

        public bool IsReadOnly { get; }

        public ICommand DisplayPartCommand { get; }

        public ICommand SignPartAsRoleCommand { get; }

        public ICommand DisplayClientSignatureCommand { get; }

        public ICommand DisplayServerSignatureCommand { get; }

        public ICommand ObsoleteCommand { get; }

        public ICommand DeleteCommand { get; }

        public ObservableCollection<PartModel> GeneralInformation { get; }

        public ObservableCollection<PartModel> ContractRoles { get; }

        public ObservableCollection<PartModel> ContractParts { get; }

        public ObservableCollection<ParameterModel> ContractParameters { get; }

        public ObservableCollection<string> ContractHumanReadableText { get; }

        public ObservableCollection<PartModel> ContractMachineReadableText { get; }

        public ObservableCollection<PartModel> ContractClientSignatures { get; }

        public ObservableCollection<PartModel> ContractServerSignatures { get; }

        public ObservableCollection<ImageSource> Photos { get; }

        public static readonly BindableProperty HasRolesProperty =
            BindableProperty.Create("HasRoles", typeof(bool), typeof(ViewContractViewModel), default(bool));

        public bool HasRoles
        {
            get { return (bool)GetValue(HasRolesProperty); }
            set { SetValue(HasRolesProperty, value); }
        }

        public static readonly BindableProperty HasPartsProperty =
            BindableProperty.Create("HasParts", typeof(bool), typeof(ViewContractViewModel), default(bool));

        public bool HasParts
        {
            get { return (bool)GetValue(HasPartsProperty); }
            set { SetValue(HasPartsProperty, value); }
        }

        public static readonly BindableProperty HasParametersProperty =
            BindableProperty.Create("HasParameters", typeof(bool), typeof(ViewContractViewModel), default(bool));

        public bool HasParameters
        {
            get { return (bool)GetValue(HasParametersProperty); }
            set { SetValue(HasParametersProperty, value); }
        }

        public static readonly BindableProperty HasHumanReadableTextProperty =
            BindableProperty.Create("HasHumanReadableText", typeof(bool), typeof(ViewContractViewModel), default(bool));

        public bool HasHumanReadableText
        {
            get { return (bool)GetValue(HasHumanReadableTextProperty); }
            set { SetValue(HasHumanReadableTextProperty, value); }
        }

        public static readonly BindableProperty HasMachineReadableTextProperty =
            BindableProperty.Create("HasMachineReadableText", typeof(bool), typeof(ViewContractViewModel), default(bool));

        public bool HasMachineReadableText
        {
            get { return (bool)GetValue(HasMachineReadableTextProperty); }
            set { SetValue(HasMachineReadableTextProperty, value); }
        }

        public static readonly BindableProperty HasClientSignaturesProperty =
            BindableProperty.Create("HasClientSignatures", typeof(bool), typeof(ViewContractViewModel), default(bool));

        public bool HasClientSignatures
        {
            get { return (bool)GetValue(HasClientSignaturesProperty); }
            set { SetValue(HasClientSignaturesProperty, value); }
        }

        public static readonly BindableProperty HasServerSignaturesProperty =
            BindableProperty.Create("HasServerSignatures", typeof(bool), typeof(ViewContractViewModel), default(bool));

        public bool HasServerSignatures
        {
            get { return (bool)GetValue(HasServerSignaturesProperty); }
            set { SetValue(HasServerSignaturesProperty, value); }
        }

        public static readonly BindableProperty QrCodeProperty =
            BindableProperty.Create("QrCode", typeof(ImageSource), typeof(ViewContractViewModel), default(ImageSource), propertyChanged: (b, oldValue, newValue) =>
            {
                ViewContractViewModel viewModel = (ViewContractViewModel)b;
                viewModel.HasQrCode = newValue != null;
            });

        public ImageSource QrCode
        {
            get { return (ImageSource)GetValue(QrCodeProperty); }
            set { SetValue(QrCodeProperty, value); }
        }

        public static readonly BindableProperty HasQrCodeProperty =
            BindableProperty.Create("HasQrCode", typeof(bool), typeof(ViewContractViewModel), default(bool));

        public bool HasQrCode
        {
            get { return (bool) GetValue(HasQrCodeProperty); }
            set { SetValue(HasQrCodeProperty, value); }
        }

        public static readonly BindableProperty QrCodeWidthProperty =
            BindableProperty.Create("QrCodeWidth", typeof(int), typeof(ViewContractViewModel), 350);

        public int QrCodeWidth
        {
            get { return (int) GetValue(QrCodeWidthProperty); }
            set { SetValue(QrCodeWidthProperty, value); }
        }

        public static readonly BindableProperty QrCodeHeightProperty =
            BindableProperty.Create("QrCodeHeight", typeof(int), typeof(ViewContractViewModel), 350);

        public int QrCodeHeight
        {
            get { return (int) GetValue(QrCodeHeightProperty); }
            set { SetValue(QrCodeHeightProperty, value); }
        }

        public static readonly BindableProperty CanDeleteOrObsoleteContractProperty =
            BindableProperty.Create("CanDeleteOrObsoleteContract", typeof(bool), typeof(ViewContractViewModel), default(bool));

        public bool CanDeleteOrObsoleteContract
        {
            get { return (bool) GetValue(CanDeleteOrObsoleteContractProperty); }
            set { SetValue(CanDeleteOrObsoleteContractProperty, value); }
        }

        #endregion

        private void ClearContract()
        {
            this.photosLoader.CancelLoadPhotos();
            this.contract = null;
            this.GeneralInformation.Clear();
            this.ContractParameters.Clear();
            this.ContractRoles.Clear();
            this.ContractHumanReadableText.Clear();
            this.ContractMachineReadableText.Clear();
            this.ContractClientSignatures.Clear();
            this.ContractServerSignatures.Clear();
            this.HasRoles = false;
            this.HasParts = false;
            this.HasParameters = false;
            this.HasHumanReadableText = false;
            this.HasMachineReadableText = false;
            this.HasClientSignatures = false;
            this.HasServerSignatures = false;
            this.HasQrCode = false;
            this.CanDeleteOrObsoleteContract = false;
            this.Photos.Clear();
        }

        private async Task LoadContract()
        {
            try
            {
                // General Information
                this.GeneralInformation.Add(new PartModel(AppResources.Created, contract.Created.ToString(CultureInfo.CurrentUICulture)));
                if (this.contract.Updated > DateTime.MinValue)
                {
                    this.GeneralInformation.Add(new PartModel(AppResources.Created, contract.Updated.ToString(CultureInfo.CurrentUICulture)));
                }
                this.GeneralInformation.Add(new PartModel(AppResources.State, contract.State.ToString()));
                this.GeneralInformation.Add(new PartModel(AppResources.Visibility, contract.Visibility.ToString()));
                this.GeneralInformation.Add(new PartModel(AppResources.Duration, contract.Duration.ToString()));
                this.GeneralInformation.Add(new PartModel(AppResources.From, contract.From.ToString(CultureInfo.CurrentUICulture)));
                this.GeneralInformation.Add(new PartModel(AppResources.To, contract.To.ToString(CultureInfo.CurrentUICulture)));
                this.GeneralInformation.Add(new PartModel(AppResources.Archiving_Optional, contract.ArchiveOptional.ToString()));
                this.GeneralInformation.Add(new PartModel(AppResources.Archiving_Required, contract.ArchiveRequired.ToString()));
                this.GeneralInformation.Add(new PartModel(AppResources.CanActAsTemplate, contract.CanActAsTemplate.ToYesNo()));

                // QR
                byte[] png = QR.GenerateCodePng(Constants.IoTSchemes.IotSc + ":" + contract.ContractId, this.QrCodeWidth, this.QrCodeHeight);
                this.QrCode = ImageSource.FromStream(() => new MemoryStream(png));

                // Parts
                bool hasSigned = false;
                bool acceptsSignatures =
                    (contract.State == ContractState.Approved || contract.State == ContractState.BeingSigned) &&
                    (!contract.SignAfter.HasValue || contract.SignAfter.Value < DateTime.Now) &&
                    (!contract.SignBefore.HasValue || contract.SignBefore.Value > DateTime.Now);
                Dictionary<string, int> nrSignatures = new Dictionary<string, int>();

                if (contract.ClientSignatures != null)
                {
                    foreach (ClientSignature signature in contract.ClientSignatures)
                    {
                        if (signature.LegalId == this.tagProfile.LegalIdentity.Id)
                            hasSigned = true;

                        if (!nrSignatures.TryGetValue(signature.Role, out int count))
                            count = 0;

                        nrSignatures[signature.Role] = count + 1;
                    }
                }

                if (contract.SignAfter.HasValue)
                {
                    this.ContractParts.Add(new PartModel(AppResources.SignAfter, contract.SignAfter.Value.ToString(CultureInfo.CurrentUICulture)));
                }
                if (contract.SignBefore.HasValue)
                {
                    this.ContractParts.Add(new PartModel(AppResources.SignBefore, contract.SignBefore.Value.ToString(CultureInfo.CurrentUICulture)));
                }
                this.ContractParts.Add(new PartModel(AppResources.Mode, contract.PartsMode.ToString()));
                if (contract.Parts != null)
                {
                    foreach (Part part in contract.Parts)
                    {
                        PartModel model = new PartModel(part.Role, part.LegalId, part.LegalId);
                        if (!this.IsReadOnly && acceptsSignatures && !hasSigned && part.LegalId == this.tagProfile.LegalIdentity.Id)
                        {
                            model.SignAsRole = part.Role;
                            model.SignAsRoleText = string.Format(AppResources.SignAsRole, part.Role);
                        }
                        this.ContractParts.Add(model);
                    }
                }

                // Roles
                if (this.contract.Roles != null)
                {
                    foreach (Role role in this.contract.Roles)
                    {
                        string html = role.ToHTML(contract.DefaultLanguage, contract);
                        html = Waher.Content.Html.HtmlDocument.GetBody(html);

                        PartModel model = new PartModel(role.Name, html + GenerateMinMaxCountString(role.MinCount, role.MaxCount));
                        model.IsHtml = true;

                        if (!this.IsReadOnly && acceptsSignatures && !hasSigned && this.contract.PartsMode == Waher.Networking.XMPP.Contracts.ContractParts.Open &&
                            (!nrSignatures.TryGetValue(role.Name, out int count) || count < role.MaxCount))
                        {
                            model.SignAsRole = role.Name;
                            model.SignAsRoleText = string.Format(AppResources.SignAsRole, role.Name);
                        }
                        this.ContractRoles.Add(model);
                    }
                }

                // Parameters
                if (contract.Parameters != null)
                {
                    foreach (Parameter parameter in contract.Parameters)
                    {
                        ParameterModel model = new ParameterModel(parameter.Name, parameter.ObjectValue?.ToString());
                        this.ContractParameters.Add(model);
                    }
                }

                // Human readable text
                // TODO: replace this with a data template selector
                //Populate(this.HumanReadableText, this.template.ToXamarinForms(this.template.DefaultLanguage));

                // Machine readable text
                this.ContractMachineReadableText.Add(new PartModel(AppResources.ContractId, contract.ContractId.ToString()));

                if (!string.IsNullOrEmpty(contract.TemplateId))
                    this.ContractMachineReadableText.Add(new PartModel(AppResources.TemplateId, contract.TemplateId));

                this.ContractMachineReadableText.Add(new PartModel(AppResources.Digest, Convert.ToBase64String(contract.ContentSchemaDigest)));
                this.ContractMachineReadableText.Add(new PartModel(AppResources.HashFunction, contract.ContentSchemaHashFunction.ToString()));
                this.ContractMachineReadableText.Add(new PartModel(AppResources.LocalName, contract.ForMachinesLocalName.ToString()));
                this.ContractMachineReadableText.Add(new PartModel(AppResources.Namespace, contract.ForMachinesNamespace.ToString()));

                // Client signatures
                if (contract.ClientSignatures != null)
                {
                    foreach (ClientSignature signature in contract.ClientSignatures)
                    {
                        string sign = Convert.ToBase64String(signature.DigitalSignature);
                        PartModel model = new PartModel(signature.Role, $"{signature.LegalId}, {signature.BareJid}, {signature.Timestamp.ToString(CultureInfo.CurrentUICulture)}, {sign}")
                        {
                            LegalId = sign
                        };
                        this.ContractClientSignatures.Add(model);
                    }
                }

                // Server signature
                if (contract.ServerSignature != null)
                {
                    string sign = Convert.ToBase64String(contract.ServerSignature.DigitalSignature);
                    PartModel model = new PartModel(contract.Provider, $"{contract.ServerSignature.Timestamp.ToString(CultureInfo.CurrentUICulture)}, {sign}");
                    this.ContractServerSignatures.Add(model);
                }

                this.CanDeleteOrObsoleteContract = !this.IsReadOnly && !contract.IsLegallyBinding(true);

                this.HasRoles = this.ContractRoles.Count > 0;
                this.HasParts = this.ContractParts.Count > 0;
                this.HasParameters = this.ContractParameters.Count > 0;
                this.HasHumanReadableText = this.ContractHumanReadableText.Count > 0;
                this.HasMachineReadableText = this.ContractMachineReadableText.Count > 0;
                this.HasClientSignatures = this.ContractClientSignatures.Count > 0;
                this.HasServerSignatures = this.ContractServerSignatures.Count > 0;

                if (this.contract.Attachments != null)
                {
                    _ = this.photosLoader.LoadPhotos(this.contract.Attachments, this.Photos);
                }
            }
            catch (Exception ex)
            {
                this.logService.LogException(ex, new KeyValuePair<string, string>("Method", nameof(LoadContract)), new KeyValuePair<string, string>("ContractId", this.contract.ContractId));
                ClearContract();
                await this.navigationService.DisplayAlert(ex);
            }
        }

        private static string GenerateMinMaxCountString(int min, int max)
        {
            if (min == max)
            {
                if (max == 1)
                    return string.Empty;
                return $" ({max})";
            }
            
            return $" ({min} - {max})";
        }

        private async Task ShowLegalId(string legalId)
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(legalId))
                {
                    await this.contractOrchestratorService.OpenLegalIdentity(legalId, "For inclusion as part in a contract.");
                }
            }
            catch (Exception ex)
            {
                await this.navigationService.DisplayAlert(ex);
            }
        }

        private async Task SignContract(string roleId)
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(roleId))
                {
                    Contract signedContract = await this.contractsService.SignContractAsync(this.contract, roleId, false);

                    await this.navigationService.DisplayAlert(AppResources.SuccessTitle, AppResources.ContractSuccessfullySigned);

                    await this.navigationService.ReplaceAsync(new ViewContractPage(signedContract, false));
                }
            }
            catch (Exception ex)
            {
                await this.navigationService.DisplayAlert(ex);
            }
        }

        private async Task ShowClientSignature(string sign)
        {
            try
            {
                ClientSignature signature = this.contract.ClientSignatures.FirstOrDefault(x => sign == Convert.ToBase64String(x.DigitalSignature));
                if (signature != null)
                {
                    string legalId = signature.LegalId;
                    LegalIdentity identity = await this.contractsService.GetLegalIdentityAsync(legalId);

                    await this.navigationService.PushAsync(new ClientSignaturePage(signature, identity));
                }
            }
            catch (Exception ex)
            {
                await this.navigationService.DisplayAlert(ex);
            }
        }

        private async Task ShowServerSignature()
        {
            try
            {
                await this.navigationService.PushAsync(new ServerSignaturePage(this.contract));
            }
            catch (Exception ex)
            {
                await this.navigationService.DisplayAlert(ex);
            }
        }

        private async Task ObsoleteContract()
        {
            try
            {
                Contract obsoletedContract = await this.contractsService.ObsoleteContractAsync(this.contract.ContractId);

                await this.navigationService.DisplayAlert(AppResources.SuccessTitle, AppResources.ContractHasBeenObsoleted);

                await this.navigationService.PushAsync(new ViewContractPage(obsoletedContract, false));
            }
            catch (Exception ex)
            {
                await this.navigationService.DisplayAlert(ex);
            }
        }

        private async Task DeleteContract()
        {
            try
            {
                Contract deletedContract = await this.contractsService.DeleteContractAsync(this.contract.ContractId);

                await this.navigationService.DisplayAlert(AppResources.SuccessTitle, AppResources.ContractHasBeenDeleted);

                await this.navigationService.PushAsync(new ViewContractPage(deletedContract, false));
            }
            catch (Exception ex)
            {
                await this.navigationService.DisplayAlert(ex);
            }
        }
    }
}