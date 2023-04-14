using System;
using System.Threading.Tasks;
using System.Windows.Input;
using Waher.Content;
using Xamarin.Forms;
using IdApp.Services;
using IdApp.Services.Tag;
using Xamarin.CommunityToolkit.Helpers;
using SkiaSharp;
using System.IO;
using IdApp.Services.UI.Photos;
using IdApp.Extensions;
using Xamarin.Essentials;

namespace IdApp.Pages.Registration.Atlantic
{
	/// <summary>
	/// The view model to bind to when showing Step 3 of the registration flow: registering an identity.
	/// </summary>
	public class GetPhotoImageViewModel : RegistrationStepViewModel
	{
		private const string selfiePhotoFileName = "SelfiePhoto.jpg";
		private const string idFacePhotoFileName = "IdFacePhoto.jpg";
		private const string idBackPhotoFileName = "IdBackPhoto.jpg";

		private readonly string localSelfiePhotoFileName;
		private readonly string localIdFacePhotoFileName;
		private readonly string localIdBackPhotoFileName;

		private LegalIdentityAttachment thePhoto;

		/// <summary>
		/// Creates a new instance of the <see cref="RegisterIdentityModel"/> class.
		/// </summary>
		public GetPhotoImageViewModel(RegistrationStep RegistrationStep) : base(RegistrationStep)
		{
			this.Title = LocalizationResourceManager.Current["PersonalLegalInformation"];

			this.NextCommand = new Command(async _ => await this.AddPhoto(), _ => this.HasPhoto);

			this.TakePhotoCommand = new Command(async _ => {
				switch (this.Step)
				{
					case RegistrationStep.GetUserPhotoImage:
						await this.TakePhoto(0);
						break;
					case RegistrationStep.GetIdFacePhotoImage:
						await this.TakePhoto(1);
						break;
					case RegistrationStep.GetIdBackPhotoImage:
						await this.TakePhoto(2);
						break;
				};
			}, _ => !this.IsBusy);

			this.localSelfiePhotoFileName = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), selfiePhotoFileName);
			this.localIdFacePhotoFileName = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), idFacePhotoFileName);
			this.localIdBackPhotoFileName = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), idBackPhotoFileName);
		}

		/// <inheritdoc />
		protected override async Task OnInitialize()
		{
			await base.OnInitialize();

			this.NextCommand.ChangeCanExecute();
		}

		/// <inheritdoc />
		protected override async Task OnDispose()
		{
			await base.OnDispose();
		}

		#region Properties

		/// <summary>
		/// The command to bind to for taking a photo with the camera.
		/// </summary>
		public ICommand TakePhotoCommand { get; }

		/// <summary>
		/// The command to bind to for performing the 'register' action.
		/// </summary>
		public ICommand NextCommand { get; }

		/// <summary>
		/// </summary>
		public string ControlTitleLabelText
		{
			get
			{
				return this.Step switch
				{
					RegistrationStep.GetUserPhotoImage => LocalizationResourceManager.Current["PhotoFace"],
					RegistrationStep.GetIdFacePhotoImage => LocalizationResourceManager.Current["PhotoIdFace"],
					RegistrationStep.GetIdBackPhotoImage => LocalizationResourceManager.Current["PhotoIdBack"],
					_ => string.Empty
				};
			}
		}


		/// <summary>
		/// The <see cref="HasPhoto"/>
		/// </summary>
		public static readonly BindableProperty HasPhotoProperty =
			BindableProperty.Create(nameof(HasPhoto), typeof(bool), typeof(GetPhotoImageViewModel), default(bool));

		/// <summary>
		/// Gets or sets whether the user has selected a photo for their account or not.
		/// </summary>
		public bool HasPhoto
		{
			get => (bool)this.GetValue(HasPhotoProperty);
			set => this.SetValue(HasPhotoProperty, value);
		}

		/// <summary>
		/// </summary>
		public static readonly BindableProperty ImageProperty =
			BindableProperty.Create(nameof(Image), typeof(ImageSource), typeof(GetPhotoImageViewModel), default(ImageSource), propertyChanged: (b, oldValue, newValue) =>
			{
				GetPhotoImageViewModel viewModel = (GetPhotoImageViewModel)b;
				viewModel.HasPhoto = (newValue is not null);
			});

		/// <summary>
		/// The image source, i.e. the file representing the selected photo.
		/// </summary>
		public ImageSource Image
		{
			get => (ImageSource)this.GetValue(ImageProperty);
			set => this.SetValue(ImageProperty, value);
		}

		/// <summary>
		/// See <see cref="ImageRotation"/>
		/// </summary>
		public static readonly BindableProperty ImageRotationProperty =
			BindableProperty.Create(nameof(ImageRotation), typeof(int), typeof(Main.Main.MainViewModel), default(int));

		/// <summary>
		/// Gets or sets whether the current user has a photo associated with the account.
		/// </summary>
		public int ImageRotation
		{
			get => (int)this.GetValue(ImageRotationProperty);
			set => this.SetValue(ImageRotationProperty, value);
		}

		#endregion

		private static void OnPropertyChanged(BindableObject b, object oldValue, object newValue)
		{
			GetPhotoImageViewModel viewModel = (GetPhotoImageViewModel)b;
			viewModel.NextCommand.ChangeCanExecute();
		}

		private async Task StoreCapturedPhoto(string CapturedPhotoPath, int PhotoIndex)
		{
			try
			{
				if (PhotoIndex == 0)
				{
					await this.AddSelfiePhoto(CapturedPhotoPath, true);
				}
				else if (PhotoIndex == 1)
				{
					await this.AddIdFacePhoto(CapturedPhotoPath, true);
				}
				else if (PhotoIndex == 2)
				{
					await this.AddIdBackPhoto(CapturedPhotoPath, true);
				}
			}
			catch (Exception ex)
			{
				await this.UiSerializer.DisplayAlert(ex);
			}
		}

		private async Task TakePhoto(int PhotoIndex)
		{
			// iOS emulator doesn't support take photos
			if ((DeviceInfo.DeviceType == DeviceType.Virtual) && (Device.RuntimePlatform == Device.iOS))
			{
				await this.PickPhoto(PhotoIndex);
				return;
			}

			if (!this.XmppService.FileUploadIsSupported)
			{
				await this.UiSerializer.DisplayAlert(LocalizationResourceManager.Current["TakePhoto"], LocalizationResourceManager.Current["ServerDoesNotSupportFileUpload"]);
				return;
			}

			string CapturedPhotoFilePath = await DeviceCamera.TakePhoto(100);

			if (CapturedPhotoFilePath is not null)
			{
				await this.StoreCapturedPhoto(CapturedPhotoFilePath, PhotoIndex);
			}
		}

		private async Task PickPhoto(int PhotoIndex)
		{
			if (!this.XmppService.FileUploadIsSupported)
			{
				await this.UiSerializer.DisplayAlert(LocalizationResourceManager.Current["PickPhoto"], LocalizationResourceManager.Current["SelectingAPhotoIsNotSupported"]);
				return;
			}

			FileResult pickedPhoto = await MediaPicker.PickPhotoAsync();

			if (pickedPhoto is not null)
			{
				await this.StoreCapturedPhoto(pickedPhoto.FullPath, PhotoIndex);
			}
		}

		private async Task AddSelfiePhoto(byte[] Bin, string ContentType, int Rotation, bool SaveLocalCopy, bool ShowAlert)
		{
			if (Bin.Length > this.TagProfile.HttpFileUploadMaxSize.GetValueOrDefault())
			{
				if (ShowAlert)
					await this.UiSerializer.DisplayAlert(LocalizationResourceManager.Current["ErrorTitle"], LocalizationResourceManager.Current["PhotoIsTooLarge"]);

				return;
			}

			this.RemoveSelfie(SaveLocalCopy);

			if (SaveLocalCopy)
			{
				try
				{
					File.WriteAllBytes(this.localSelfiePhotoFileName, Bin);
				}
				catch (Exception e)
				{
					this.LogService.LogException(e);
				}
			}

			this.thePhoto = new LegalIdentityAttachment(this.localSelfiePhotoFileName, ContentType, Bin);
			this.ImageRotation = Rotation;
			this.Image = ImageSource.FromStream(() => new MemoryStream(Bin));

			this.NextCommand.ChangeCanExecute();
		}

		private async Task AddIdFacePhoto(byte[] Bin, string ContentType, int Rotation, bool SaveLocalCopy, bool ShowAlert)
		{
			if (Bin.Length > this.TagProfile.HttpFileUploadMaxSize.GetValueOrDefault())
			{
				if (ShowAlert)
					await this.UiSerializer.DisplayAlert(LocalizationResourceManager.Current["ErrorTitle"], LocalizationResourceManager.Current["PhotoIsTooLarge"]);

				return;
			}

			this.RemoveIdFacePhoto(SaveLocalCopy);

			if (SaveLocalCopy)
			{
				try
				{
					File.WriteAllBytes(this.localIdFacePhotoFileName, Bin);
				}
				catch (Exception e)
				{
					this.LogService.LogException(e);
				}
			}

			this.thePhoto = new LegalIdentityAttachment(this.localIdFacePhotoFileName, ContentType, Bin);
			this.ImageRotation = Rotation;
			this.Image = ImageSource.FromStream(() => new MemoryStream(Bin));

			this.NextCommand.ChangeCanExecute();
		}

		private async Task AddIdBackPhoto(byte[] Bin, string ContentType, int Rotation, bool SaveLocalCopy, bool ShowAlert)
		{
			if (Bin.Length > this.TagProfile.HttpFileUploadMaxSize.GetValueOrDefault())
			{
				if (ShowAlert)
					await this.UiSerializer.DisplayAlert(LocalizationResourceManager.Current["ErrorTitle"], LocalizationResourceManager.Current["PhotoIsTooLarge"]);

				return;
			}

			this.RemoveIdBackPhoto(SaveLocalCopy);

			if (SaveLocalCopy)
			{
				try
				{
					File.WriteAllBytes(this.localIdBackPhotoFileName, Bin);
				}
				catch (Exception e)
				{
					this.LogService.LogException(e);
				}
			}

			this.thePhoto = new LegalIdentityAttachment(this.localIdBackPhotoFileName, ContentType, Bin);
			this.ImageRotation = Rotation;
			this.Image = ImageSource.FromStream(() => new MemoryStream(Bin));

			this.NextCommand.ChangeCanExecute();
		}

		private async Task AddSelfiePhoto(string FilePath, bool SaveLocalCopy)
		{
			try
			{
				bool FallbackOriginal = true;

				if (SaveLocalCopy)
				{
					// try to downscale and compress the image
					using FileStream InputStream = File.OpenRead(FilePath);
					using SKData ImageData = DeviceCamera.CompressImage(InputStream);

					if (ImageData is not null)
					{
						FallbackOriginal = false;
						await this.AddSelfiePhoto(ImageData.ToArray(), Constants.MimeTypes.Jpeg, 0, SaveLocalCopy, true);
					}
				}

				if (FallbackOriginal)
				{
					byte[] Bin = File.ReadAllBytes(FilePath);
					if (!InternetContent.TryGetContentType(Path.GetExtension(FilePath), out string ContentType))
						ContentType = "application/octet-stream";

					await this.AddSelfiePhoto(Bin, ContentType, DeviceCamera.GetImageRotation(Bin), SaveLocalCopy, true);
				}
			}
			catch (Exception ex)
			{
				await this.UiSerializer.DisplayAlert(LocalizationResourceManager.Current["ErrorTitle"], LocalizationResourceManager.Current["FailedToLoadPhoto"]);
				this.LogService.LogException(ex);
				return;
			}
		}

		private async Task AddIdFacePhoto(string FilePath, bool SaveLocalCopy)
		{
			try
			{
				bool FallbackOriginal = true;

				if (SaveLocalCopy)
				{
					// try to downscale and compress the image
					using FileStream InputStream = File.OpenRead(FilePath);
					using SKData ImageData = DeviceCamera.CompressImage(InputStream);

					if (ImageData is not null)
					{
						FallbackOriginal = false;
						await this.AddIdFacePhoto(ImageData.ToArray(), Constants.MimeTypes.Jpeg, 0, SaveLocalCopy, true);
					}
				}

				if (FallbackOriginal)
				{
					byte[] Bin = File.ReadAllBytes(FilePath);
					if (!InternetContent.TryGetContentType(Path.GetExtension(FilePath), out string ContentType))
						ContentType = "application/octet-stream";

					await this.AddIdFacePhoto(Bin, ContentType, DeviceCamera.GetImageRotation(Bin), SaveLocalCopy, true);
				}
			}
			catch (Exception ex)
			{
				await this.UiSerializer.DisplayAlert(LocalizationResourceManager.Current["ErrorTitle"], LocalizationResourceManager.Current["FailedToLoadPhoto"]);
				this.LogService.LogException(ex);
				return;
			}
		}

		private async Task AddIdBackPhoto(string FilePath, bool SaveLocalCopy)
		{
			try
			{
				bool FallbackOriginal = true;

				if (SaveLocalCopy)
				{
					// try to downscale and compress the image
					using FileStream InputStream = File.OpenRead(FilePath);
					using SKData ImageData = DeviceCamera.CompressImage(InputStream);

					if (ImageData is not null)
					{
						FallbackOriginal = false;
						await this.AddIdBackPhoto(ImageData.ToArray(), Constants.MimeTypes.Jpeg, 0, SaveLocalCopy, true);
					}
				}

				if (FallbackOriginal)
				{
					byte[] Bin = File.ReadAllBytes(FilePath);
					if (!InternetContent.TryGetContentType(Path.GetExtension(FilePath), out string ContentType))
						ContentType = "application/octet-stream";

					await this.AddIdBackPhoto(Bin, ContentType, DeviceCamera.GetImageRotation(Bin), SaveLocalCopy, true);
				}
			}
			catch (Exception ex)
			{
				await this.UiSerializer.DisplayAlert(LocalizationResourceManager.Current["ErrorTitle"], LocalizationResourceManager.Current["FailedToLoadPhoto"]);
				this.LogService.LogException(ex);
				return;
			}
		}

		private void RemoveSelfie(bool RemoveFileOnDisc)
		{
			try
			{
				this.thePhoto = null;
				this.Image = null;

				if (RemoveFileOnDisc && File.Exists(this.localSelfiePhotoFileName))
					File.Delete(this.localSelfiePhotoFileName);
			}
			catch (Exception e)
			{
				this.LogService.LogException(e);
			}
		}

		private void RemoveIdFacePhoto(bool RemoveFileOnDisc)
		{
			try
			{
				this.thePhoto = null;
				this.Image = null;

				if (RemoveFileOnDisc && File.Exists(this.localIdFacePhotoFileName))
					File.Delete(this.localIdFacePhotoFileName);
			}
			catch (Exception e)
			{
				this.LogService.LogException(e);
			}
		}

		private void RemoveIdBackPhoto(bool RemoveFileOnDisc)
		{
			try
			{
				this.thePhoto = null;
				this.Image = null;

				if (RemoveFileOnDisc && File.Exists(this.localIdBackPhotoFileName))
					File.Delete(this.localIdBackPhotoFileName);
			}
			catch (Exception e)
			{
				this.LogService.LogException(e);
			}
		}

		private async Task AddPhoto()
		{
			this.SetIsBusy(this.NextCommand, this.TakePhotoCommand);

			try
			{
				switch (this.Step)
				{
					case RegistrationStep.GetUserPhotoImage:
						await this.TagProfile.AddLegalPhoto(this.thePhoto, 1);
						break;
					case RegistrationStep.GetIdFacePhotoImage:
						await this.TagProfile.AddLegalPhoto(this.thePhoto, 2);
						break;
					case RegistrationStep.GetIdBackPhotoImage:
						await this.TagProfile.AddLegalPhoto(this.thePhoto, 3);
						break;
				};

				this.OnStepCompleted(EventArgs.Empty);
			}
			catch (Exception ex)
			{
				this.LogService.LogException(ex);
				await this.UiSerializer.DisplayAlert(ex);
			}
			finally
			{
				this.BeginInvokeSetIsDone(this.NextCommand, this.TakePhotoCommand);
			}
		}

		/// <inheritdoc />
		protected override async Task DoSaveState()
		{
			await base.DoSaveState();
		}

		/// <inheritdoc />
		protected override async Task DoRestoreState()
		{
			try
			{
				switch (this.Step)
				{
					case RegistrationStep.GetUserPhotoImage:
						if (File.Exists(this.localSelfiePhotoFileName))
							await this.AddSelfiePhoto(this.localSelfiePhotoFileName, false);
						break;
					case RegistrationStep.GetIdFacePhotoImage:
						if (File.Exists(this.localIdFacePhotoFileName))
							await this.AddIdFacePhoto(this.localIdFacePhotoFileName, false);
						break;
					case RegistrationStep.GetIdBackPhotoImage:
						if (File.Exists(this.localIdBackPhotoFileName))
							await this.AddIdBackPhoto(this.localIdBackPhotoFileName, false);
						break;
				};
			}
			catch (Exception e)
			{
				this.LogService.LogException(e);
			}

			await base.DoRestoreState();
		}

		/// <inheritdoc />
		public override void ClearStepState()
		{
			switch (this.Step)
			{
				case RegistrationStep.GetUserPhotoImage:
					this.RemoveSelfie(true);
					break;
				case RegistrationStep.GetIdFacePhotoImage:
					this.RemoveIdFacePhoto(true);
					break;
				case RegistrationStep.GetIdBackPhotoImage:
					this.RemoveIdBackPhoto(true);
					break;
			};
		}
	}
}
