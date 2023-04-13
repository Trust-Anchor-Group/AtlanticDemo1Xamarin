using Plugin.Media.Abstractions;
using Plugin.Media;
using System;
using System.Threading.Tasks;
using Xamarin.CommunityToolkit.Helpers;
using Xamarin.Essentials;
using Xamarin.Forms;
using SkiaSharp;
using System.IO;
using Waher.Content.Images.Exif;
using Waher.Content.Images;
using Waher.Content.Markdown.Model.Multimedia;
using Waher.Networking.XMPP.Contracts;
using IdApp.Extensions;

namespace IdApp.Services.UI.Photos
{
	/// <summary>
	/// A helper class for manipulations with photos
	/// </summary>
	internal class DeviceCamera
	{
		/// <summary>
		/// Captures a photo
		/// </summary>
		/// <returns>Returns captured photo file path</returns>
		public static async Task<string> TakePhoto(int compressionQuality = 80)
		{
			ServiceReferences Services = new();

			if (!Services.XmppService.FileUploadIsSupported)
			{
				await Services.UiSerializer.DisplayAlert(LocalizationResourceManager.Current["TakePhoto"], LocalizationResourceManager.Current["ServerDoesNotSupportFileUpload"]);
				return null;
			}

			if (Device.RuntimePlatform == Device.iOS)
			{
				MediaFile capturedPhoto;

				try
				{
					capturedPhoto = await CrossMedia.Current.TakePhotoAsync(new StoreCameraMediaOptions()
					{
						CompressionQuality = compressionQuality,
						RotateImage = false
					});
				}
				catch (Exception ex)
				{
					await Services.UiSerializer.DisplayAlert(LocalizationResourceManager.Current["TakePhoto"], LocalizationResourceManager.Current["TakingAPhotoIsNotSupported"] + ": " + ex.Message);
					return null;
				}

				return capturedPhoto.Path;
			}
			else
			{
				FileResult capturedPhoto;

				try
				{
					capturedPhoto = await MediaPicker.CapturePhotoAsync();
					if (capturedPhoto is null)
					{
						return null;
					}
				}
				catch (Exception ex)
				{
					await Services.UiSerializer.DisplayAlert(LocalizationResourceManager.Current["TakePhoto"], LocalizationResourceManager.Current["TakingAPhotoIsNotSupported"] + ": " + ex.Message);
					return null;
				}

				return capturedPhoto.FullPath;
			}
		}

		/// <summary>
		/// Tries to downscale and compress an image
		/// </summary>
		/// <returns>true - success, false - error occurred</returns>
		public static async Task<bool> TryDownscaleAndCompressImage(string FilePath, string FileTooLargeErrorMessage, string CommonErrorMessage)
		{
			ServiceReferences Services = new();

			try
			{
				using FileStream InputStream = File.OpenRead(FilePath);
				using SKData ImageData = CompressImage(InputStream);
				byte[] Bin = null;

				if (ImageData is not null)
				{
					Bin = ImageData.ToArray();
				}
				else
				{
					Bin = File.ReadAllBytes(FilePath);
				}

				if (Bin.Length > Services.TagProfile.HttpFileUploadMaxSize.GetValueOrDefault())
				{
					await Services.UiSerializer.DisplayAlert(LocalizationResourceManager.Current["ErrorTitle"], FileTooLargeErrorMessage);
					return false;
				}

				File.Delete(FilePath);
				File.WriteAllBytes(FilePath, Bin);

				return true;
			}
			catch (Exception ex)
			{
				await Services.UiSerializer.DisplayAlert(LocalizationResourceManager.Current["ErrorTitle"], CommonErrorMessage);
				Services.LogService.LogException(ex);
				return false;
			}

		}

		public static SKData CompressImage(Stream inputStream)
		{
			try
			{
				using SKManagedStream ManagedStream = new(inputStream);
				using SKData ImageData = SKData.Create(ManagedStream);

				SKCodec Codec = SKCodec.Create(ImageData);
				SKBitmap SkBitmap = SKBitmap.Decode(ImageData);

				SkBitmap = DeviceCamera.HandleOrientation(SkBitmap, Codec.EncodedOrigin);

				bool Resize = false;
				int Height = SkBitmap.Height;
				int Width = SkBitmap.Width;

				// downdsample to FHD
				if ((Width >= Height) && (Width > 1920))
				{
					Height = (int)(Height * (1920.0 / Width) + 0.5);
					Width = 1920;
					Resize = true;
				}
				else if ((Height > Width) && (Height > 1920))
				{
					Width = (int)(Width * (1920.0 / Height) + 0.5);
					Height = 1920;
					Resize = true;
				}

				if (Resize)
				{
					SKImageInfo Info = SkBitmap.Info;
					SKImageInfo NewInfo = new(Width, Height, Info.ColorType, Info.AlphaType, Info.ColorSpace);
					SkBitmap = SkBitmap.Resize(NewInfo, SKFilterQuality.High);
				}

				return SkBitmap.Encode(SKEncodedImageFormat.Jpeg, 80);
			}
			catch (Exception ex)
			{
				new ServiceReferences().LogService.LogException(ex);
			}

			return null;
		}

		private static SKBitmap HandleOrientation(SKBitmap Bitmap, SKEncodedOrigin Orientation)
		{
			SKBitmap Rotated;

			switch (Orientation)
			{
				case SKEncodedOrigin.BottomRight:
					Rotated = new SKBitmap(Bitmap.Width, Bitmap.Height);

					using (SKCanvas Surface = new(Rotated))
					{
						Surface.RotateDegrees(180, Bitmap.Width / 2, Bitmap.Height / 2);
						Surface.DrawBitmap(Bitmap, 0, 0);
					}
					break;

				case SKEncodedOrigin.RightTop:
					Rotated = new SKBitmap(Bitmap.Height, Bitmap.Width);

					using (SKCanvas Surface = new(Rotated))
					{
						Surface.Translate(Rotated.Width, 0);
						Surface.RotateDegrees(90);
						Surface.DrawBitmap(Bitmap, 0, 0);
					}
					break;

				case SKEncodedOrigin.LeftBottom:
					Rotated = new SKBitmap(Bitmap.Height, Bitmap.Width);

					using (SKCanvas Surface = new(Rotated))
					{
						Surface.Translate(0, Rotated.Height);
						Surface.RotateDegrees(270);
						Surface.DrawBitmap(Bitmap, 0, 0);
					}
					break;

				default:
					return Bitmap;
			}

			return Rotated;
		}

		/// <summary>
		/// Loads photo from the specified file path
		/// </summary>
		/// <param name="FilePath">file path to load</param>
		/// <returns>Returns loaded photo or null if something went wrong</returns>
		public static async Task<Photo> LoadPhotoFromFile(string FilePath)
		{
			if (FilePath is null)
			{
				return null;
			}

			ServiceReferences Services = new();

			try
			{
				byte[] Bin;
				using (FileStream FileStream = File.OpenRead(FilePath))
				{
					long FileLength = FileStream.Length;

					Bin = new byte[FileLength];
					await FileStream.ReadAsync(Bin, 0, Bin.Length);
				}

				if (Bin is null)
					return null;

				return new(Bin, GetImageRotation(Bin));
			}
			catch (Exception ex)
			{
				Services.LogService.LogException(ex);
				return null;
			}
		}

		/// <summary>
		/// Gets the rotation angle to use, to display the image correctly in Xamarin Forms.
		/// </summary>
		/// <param name="JpegImage">Binary representation of JPEG image.</param>
		/// <returns>Rotation angle (degrees).</returns>
		public static int GetImageRotation(byte[] JpegImage)
		{
			//!!! This rotation in xamarin is limited to Android
			if (Device.RuntimePlatform == Device.iOS)
				return 0;

			if (JpegImage is null)
				return 0;

			if (!EXIF.TryExtractFromJPeg(JpegImage, out ExifTag[] Tags))
				return 0;

			return GetImageRotation(Tags);
		}

		/// <summary>
		/// Gets the rotation angle to use, to display the image correctly in Xamarin Forms.
		/// </summary>
		/// <param name="Tags">EXIF Tags encoded in image.</param>
		/// <returns>Rotation angle (degrees).</returns>
		public static int GetImageRotation(ExifTag[] Tags)
		{
			foreach (ExifTag Tag in Tags)
			{
				if (Tag.Name == ExifTagName.Orientation)
				{
					if (Tag.Value is ushort Orientation)
					{
						return Orientation switch
						{
							1 => 0,// Top left. Default orientation.
							2 => 0,// Top right. Horizontally reversed.
							3 => 180,// Bottom right. Rotated by 180 degrees.
							4 => 180,// Bottom left. Rotated by 180 degrees and then horizontally reversed.
							5 => -90,// Left top. Rotated by 90 degrees counterclockwise and then horizontally reversed.
							6 => 90,// Right top. Rotated by 90 degrees clockwise.
							7 => 90,// Right bottom. Rotated by 90 degrees clockwise and then horizontally reversed.
							8 => -90,// Left bottom. Rotated by 90 degrees counterclockwise.
							_ => 0,
						};
					}
				}
			}

			return 0;
		}

		/// <summary>
		/// Loads a photo attachment.
		/// </summary>
		/// <param name="Attachment">Attachment containing photo.</param>
		/// <returns>Photo, Content-Type, Rotation</returns>
		public static async Task<(byte[], string, int)> LoadPhoto(Attachment Attachment)
		{
			PhotosLoader Loader = new();

			(byte[], string, int) Image = await Loader.LoadOnePhoto(Attachment, SignWith.LatestApprovedIdOrCurrentKeys);

			return Image;
		}

		/// <summary>
		/// Tries to load a photo from a set of attachments.
		/// </summary>
		/// <param name="Attachments">Attachments</param>
		/// <param name="MaxWith">Maximum width when displaying photo.</param>
		/// <param name="MaxHeight">Maximum height when displaying photo.</param>
		/// <returns>Filename, Width, Height, if loaded, (null,0,0) if not.</returns>
		public static Task<(string, int, int)> LoadPhotoAsTemporaryFile(Attachment[] Attachments, int MaxWith, int MaxHeight)
		{
			Attachment Photo = null;

			foreach (Attachment Attachment in Attachments.GetImageAttachments())
			{
				if (Attachment.ContentType == Constants.MimeTypes.Png)
				{
					Photo = Attachment;
					break;
				}
				else if (Photo is null)
					Photo = Attachment;
			}

			if (Photo is null)
				return Task.FromResult<(string, int, int)>((null, 0, 0));
			else
				return LoadPhotoAsTemporaryFile(Photo, MaxWith, MaxHeight);
		}

		/// <summary>
		/// Tries to load a photo from an attachments.
		/// </summary>
		/// <param name="Attachment">Attachment</param>
		/// <param name="MaxWith">Maximum width when displaying photo.</param>
		/// <param name="MaxHeight">Maximum height when displaying photo.</param>
		/// <returns>Filename, Width, Height, if loaded, (null,0,0) if not.</returns>
		public static async Task<(string, int, int)> LoadPhotoAsTemporaryFile(Attachment Attachment, int MaxWith, int MaxHeight)
		{
			(byte[] Data, string _, int _) = await LoadPhoto(Attachment);

			if (Data is not null)
			{
				string FileName = await ImageContent.GetTemporaryFile(Data);
				int Width;
				int Height;

				using (SKBitmap Bitmap = SKBitmap.Decode(Data))
				{
					Width = Bitmap.Width;
					Height = Bitmap.Height;
				}

				double ScaleWidth = ((double)MaxWith) / Width;
				double ScaleHeight = ((double)MaxHeight) / Height;
				double Scale = Math.Min(ScaleWidth, ScaleHeight);

				if (Scale < 1)
				{
					Width = (int)(Width * Scale + 0.5);
					Height = (int)(Height * Scale + 0.5);
				}

				return (FileName, Width, Height);
			}
			else
				return (null, 0, 0);
		}

	}
}
