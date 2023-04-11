using FFImageLoading.Svg.Forms;
using FFImageLoading.Transformations;
using System;
using System.Collections.Generic;
using System.Windows.Input;
using Xamarin.Forms;
using Xamarin.Forms.PancakeView;
using Xamarin.Forms.Xaml;

namespace IdApp.Controls.Extended
{
	/// <summary>
	/// public class used by IdApp.ExEntry
	/// </summary>
	[XamlCompilation(XamlCompilationOptions.Compile)]
	public partial class ExEntry : PancakeView
	{
		/// <summary>
		/// The Key Board property
		/// </summary>
		public static readonly BindableProperty KeyBoardEntryProperty =
			BindableProperty.Create(nameof(KeyBoardEntry), typeof(Keyboard), typeof(ExEntry), default(Keyboard), coerceValue: (o, v) => (Keyboard)v ?? Keyboard.Default);

		/// <summary>
		/// The Clear Button Visibility property
		/// </summary>
		public static readonly BindableProperty ClearButtonVisibilityEntryProperty =
			BindableProperty.Create(nameof(ClearButtonVisibilityEntry), typeof(ClearButtonVisibility), typeof(ExEntry), ClearButtonVisibility.Never);

		/// <summary>
		/// The Required property
		/// </summary>
		public static readonly BindableProperty RequiredProperty =
			BindableProperty.Create(nameof(Required), typeof(bool), typeof(ExEntry), false);

		/// <summary>
		/// The Entry Text property
		/// </summary>
		public static readonly BindableProperty EntryTextProperty =
			BindableProperty.Create(nameof(EntryText), typeof(string), typeof(ExEntry), defaultBindingMode: BindingMode.TwoWay);

		/// <summary>
		/// The Entry Text property
		/// </summary>
		public static readonly BindableProperty MaxLengthProperty =
			BindableProperty.Create(nameof(MaxLength), typeof(string), typeof(ExEntry), defaultBindingMode: BindingMode.TwoWay);

		/// <summary>
		/// The Return Type property
		/// </summary>
		public static readonly BindableProperty ReturnTypeProperty =
			BindableProperty.Create(nameof(ReturnType), typeof(ReturnType), typeof(ExEntry), ReturnType.Done);

		/// <summary>
		/// The PlaceHolder property
		/// </summary>
		public static readonly BindableProperty PlaceHolderProperty =
			BindableProperty.Create(nameof(PlaceHolder), typeof(string), typeof(ExEntry), default(string));

		/// <summary>
		/// The PlaceHolder Color property
		/// </summary>
		public static readonly BindableProperty PlaceholderColorProperty =
			BindableProperty.Create(nameof(PlaceholderColor), typeof(Color), typeof(ExEntry), default(Color));

		/// <summary>
		/// The Is SpellCheck Enabled property
		/// </summary>
		public static readonly BindableProperty IsSpellCheckEnabledProperty =
			BindableProperty.Create(nameof(IsSpellCheckEnabled), typeof(bool), typeof(ExEntry), default(bool));

		/// <summary>
		/// The Is Text Prediction Enabled property
		/// </summary>
		public static readonly BindableProperty IsTextPredictionEnabledProperty =
			BindableProperty.Create(nameof(IsTextPredictionEnabled), typeof(bool), typeof(ExEntry), default(bool));

		/// <summary>
		/// Entry Style propery
		/// </summary>
		public static readonly BindableProperty EntryStyleProperty =
			BindableProperty.Create(nameof(EntryStyle), typeof(Style), typeof(ExEntry), default(Style));

		/// <summary>
		/// The Icon Left property
		/// </summary>
		public static readonly BindableProperty IconLeftProperty = BindableProperty.Create(
			nameof(IconLeft), typeof(string), typeof(ExEntry), string.Empty);

		/// <summary>
		/// The Icon Right property
		/// </summary>
		public static readonly BindableProperty IconRightProperty = BindableProperty.Create(
			nameof(IconRight), typeof(string), typeof(ExEntry), string.Empty);

		/// <summary>
		/// The Color Icon Left property
		/// </summary>
		public static readonly BindableProperty HexColorIconLeftProperty = BindableProperty.Create(
			nameof(HexColorIconLeft), typeof(string), typeof(ExEntry), defaultValue: "#00000000",
			propertyChanged: OnPropertyChanged);

		/// <summary>
		/// The Color Icon Right property
		/// </summary>
		public static readonly BindableProperty HexColorIconRightProperty = BindableProperty.Create(
			nameof(HexColorIconRight), typeof(string), typeof(ExEntry), defaultValue: "#00000000",
			propertyChanged: OnPropertyChanged);

		private static void OnPropertyChanged(BindableObject bindable, object oldValue, object newValue)
		{
			((ExEntry)bindable).SetIconColor();
		}

		private void SetIconColor()
		{
			TintTransformation Transformation1 = new()
			{
				HexColor = this.HexColorIconLeft,
				EnableSolidColor = true
			};

			TintTransformation Transformation2 = new()
			{
				HexColor = this.HexColorIconRight,
				EnableSolidColor = true
			};

			this.Image1.Transformations = new() { Transformation1 };
			this.Image2.Transformations = new() { Transformation2 };
		}

		/// <summary>
		/// The Height Icon Left property
		/// </summary>
		public static readonly BindableProperty HeightIconLeftProperty =
			BindableProperty.Create(nameof(HeightIconLeft), typeof(double), typeof(ExEntry), default(double));

		/// <summary>
		/// The Height Icon Right property
		/// </summary>
		public static readonly BindableProperty HeightIconRightProperty =
			BindableProperty.Create(nameof(HeightIconRight), typeof(double), typeof(ExEntry), default(double));

		/// <summary>
		/// The Width Icon Left property
		/// </summary>
		public static readonly BindableProperty WidthIconLeftProperty =
			BindableProperty.Create(nameof(WidthIconLeft), typeof(double), typeof(ExEntry), default(double));

		/// <summary>
		/// The Width Icon Right property
		/// </summary>

		public static readonly BindableProperty WidthIconRightProperty =
			BindableProperty.Create(nameof(WidthIconRight), typeof(double), typeof(ExEntry), default(double));

		/// <summary>
		/// The Behaviors property
		/// </summary>
		public static readonly BindableProperty EntryBehaviorsProperty =
			BindableProperty.Create(nameof(EntryBehaviors), typeof(IList<Behavior>), typeof(ExEntry), default(IList<Behavior>));

		/// <summary>
		/// The IsReadOnly property
		/// </summary>
		public static readonly BindableProperty EntryIsReadOnlyProperty =
			BindableProperty.Create(nameof(EntryIsReadOnly), typeof(bool), typeof(ExEntry), false);

		/// <summary>
		/// The Entry Command Property
		/// </summary>
		public static readonly BindableProperty RightImageCommandProperty =
			BindableProperty.Create(nameof(RightImageCommand), typeof(ICommand), typeof(ExEntry), default(ICommand));

		/// <summary>
		/// The Entry ReturnCommand Property
		/// </summary>
		public static readonly BindableProperty EntryReturnCommandProperty =
			BindableProperty.Create(nameof(EntryReturnCommand), typeof(ICommand), typeof(ExEntry), default(ICommand));

		/// <summary>
		/// The Entry IsPassword Property
		/// </summary>
		public static readonly BindableProperty EntryIsPasswordProperty =
			BindableProperty.Create(nameof(EntryIsPassword), typeof(bool), typeof(ExEntry), default(bool));

		/// <summary>
		/// The Entry Text Horizontal Alignment Property
		/// </summary>
		public static readonly BindableProperty EntryTextHorizontalAlignmentProperty =
			BindableProperty.Create(nameof(EntryTextHorizontalAlignment), typeof(TextAlignment), typeof(ExEntry), default(TextAlignment));

		/// <summary>
		/// Gets or sets the entry text
		/// </summary>
		public string EntryText
		{
			get => (string)this.GetValue(EntryTextProperty);
			set => this.SetValue(EntryTextProperty, value);
		}
		/// <summary>
		/// Gets or sets the max length entry text
		/// </summary>
		public string MaxLength
		{
			get => (string)this.GetValue(MaxLengthProperty);
			set => this.SetValue(MaxLengthProperty, value);
		}

		/// <summary>
		/// Gets or sets the Is Spell Check Enabled
		/// </summary>
		public bool IsSpellCheckEnabled
		{
			get => (bool)this.GetValue(IsSpellCheckEnabledProperty);
			set => this.SetValue(IsSpellCheckEnabledProperty, value);
		}

		/// <summary>
		/// Gets or sets the Is Text Prediction Enabled
		/// </summary>
		public bool IsTextPredictionEnabled
		{
			get => (bool)this.GetValue(IsTextPredictionEnabledProperty);
			set => this.SetValue(IsTextPredictionEnabledProperty, value);
		}

		/// <summary>
		/// Gets or sets the ReturnType
		/// </summary>
		public ReturnType ReturnType
		{
			get => (ReturnType)this.GetValue(ReturnTypeProperty);
			set => this.SetValue(ReturnTypeProperty, value);
		}

		/// <summary>
		/// Gets or sets the KeyBoard Entry
		/// </summary>
		public Keyboard KeyBoardEntry
		{
			get => (Keyboard)this.GetValue(KeyBoardEntryProperty);
			set => this.SetValue(KeyBoardEntryProperty, value);
		}

		/// <summary>
		/// Gets or sets the Clear Button Visibility Entry
		/// </summary>
		public ClearButtonVisibility ClearButtonVisibilityEntry
		{
			get => (ClearButtonVisibility)this.GetValue(ClearButtonVisibilityEntryProperty);
			set => this.SetValue(ClearButtonVisibilityEntryProperty, value);
		}

		/// <summary>
		/// Gets or sets the required
		/// </summary>
		public bool Required
		{
			get => (bool)this.GetValue(RequiredProperty);
			set => this.SetValue(RequiredProperty, value);
		}

		/// <summary>
		/// Gets or sets the PlaceHolder
		/// </summary>
		public string PlaceHolder
		{
			get => (string)this.GetValue(PlaceHolderProperty);
			set => this.SetValue(PlaceHolderProperty, value);
		}

		/// <summary>
		/// Gets or sets the PlaceHolder Color
		/// </summary>
		public Color PlaceholderColor
		{
			get => (Color)this.GetValue(PlaceholderColorProperty);
			set => this.SetValue(PlaceholderColorProperty, value);
		}

		/// <summary>
		/// Gets or sets the Entry Style
		/// </summary>
		public Style EntryStyle
		{
			get => (Style)this.GetValue(EntryStyleProperty);
			set => this.SetValue(EntryStyleProperty, value);
		}

		/// <summary>
		/// Gets or sets the Icon Left
		/// </summary>
		public string IconLeft
		{
			get { return (string)base.GetValue(IconLeftProperty); }
			set { base.SetValue(IconLeftProperty, value); }
		}

		/// <summary>
		/// Gets or sets the Icon Right
		/// </summary>
		public string IconRight
		{
			get { return (string)base.GetValue(IconRightProperty); }
			set { base.SetValue(IconRightProperty, value); }
		}

		/// <summary>
		/// Gets or sets Color Icon Left
		/// </summary>
		public string HexColorIconLeft
		{
			get => (string)this.GetValue(HexColorIconLeftProperty);
			set => this.SetValue(HexColorIconLeftProperty, value);
		}

		/// <summary>
		/// Gets or sets Color Icon Right
		/// </summary>
		public string HexColorIconRight
		{
			get => (string)this.GetValue(HexColorIconRightProperty);
			set => this.SetValue(HexColorIconRightProperty, value);
		}

		/// <summary>
		/// Gets or sets the Height Icon Left
		/// </summary>
		public double HeightIconLeft
		{
			get => (double)this.GetValue(HeightIconLeftProperty);
			set => this.SetValue(HeightIconLeftProperty, value);
		}

		/// <summary>
		/// Gets or sets the Height Icon Right
		/// </summary>
		public double HeightIconRight
		{
			get => (double)this.GetValue(HeightIconRightProperty);
			set => this.SetValue(HeightIconRightProperty, value);
		}

		/// <summary>
		/// Gets or sets Width Icon Left
		/// </summary>
		public double WidthIconLeft
		{
			get => (double)this.GetValue(WidthIconLeftProperty);
			set => this.SetValue(WidthIconLeftProperty, value);
		}

		/// <summary>
		/// Gets or sets Width Icon Right
		/// </summary>
		public double WidthIconRight
		{
			get => (double)this.GetValue(WidthIconRightProperty);
			set => this.SetValue(WidthIconRightProperty, value);
		}
		/// <summary>
		/// Gets or sets Width Icon Right
		/// </summary>
		public IList<Behavior> EntryBehaviors
		{
			get => (IList<Behavior>)this.GetValue(EntryBehaviorsProperty);
			set => this.SetValue(EntryBehaviorsProperty, value);
		}

		/// <summary>
		/// Gets or sets Entry Is Read Only
		/// </summary>
		public bool EntryIsReadOnly
		{
			get => (bool)this.GetValue(EntryIsReadOnlyProperty);
			set => this.SetValue(EntryIsReadOnlyProperty, value);
		}

		/// <summary>
		/// Gets or sets Entry Command
		/// </summary>
		public ICommand RightImageCommand
		{
			get => (ICommand)this.GetValue(RightImageCommandProperty);
			set => this.SetValue(RightImageCommandProperty, value);
		}
		/// <summary>
		/// Gets or sets Entry Return Command
		/// </summary>
		public ICommand EntryReturnCommand
		{
			get => (ICommand)this.GetValue(EntryReturnCommandProperty);
			set => this.SetValue(EntryReturnCommandProperty, value);
		}

		/// <summary>
		/// Gets or sets Entry Return Command
		/// </summary>
		public bool EntryIsPassword
		{
			get => (bool)this.GetValue(EntryIsPasswordProperty);
			set => this.SetValue(EntryIsPasswordProperty, value);
		}

		/// <summary>
		/// Gets or sets Entry Horizontal Text Alignment
		/// </summary>
		public TextAlignment EntryTextHorizontalAlignment
		{
			get => (TextAlignment)this.GetValue(EntryTextHorizontalAlignmentProperty);
			set => this.SetValue(EntryTextHorizontalAlignmentProperty, value);
		}

		/// <summary>
		/// Creates a new instance of the <see cref="ExEntry"/> class.
		/// </summary>
		public ExEntry()
		{
			this.InitializeComponent();

			this.InnerEntry.SetBinding(Entry.TextProperty, new Binding(EntryTextProperty.PropertyName, source: this, mode: BindingMode.TwoWay));
			this.InnerEntry.SetBinding(Entry.MaxLengthProperty, new Binding(MaxLengthProperty.PropertyName, source: this));
			this.InnerEntry.SetBinding(Entry.ReturnTypeProperty, new Binding(ReturnTypeProperty.PropertyName, source: this));
			this.InnerEntry.SetBinding(Entry.PlaceholderProperty, new Binding(PlaceHolderProperty.PropertyName, source: this));
			this.InnerEntry.SetBinding(Entry.PlaceholderColorProperty, new Binding(PlaceholderColorProperty.PropertyName, source: this));
			this.InnerEntry.SetBinding(Entry.ClearButtonVisibilityProperty, new Binding(ClearButtonVisibilityEntryProperty.PropertyName, source: this));
			this.InnerEntry.SetBinding(Entry.KeyboardProperty, new Binding(KeyBoardEntryProperty.PropertyName, source: this));
			this.InnerEntry.SetBinding(Entry.IsSpellCheckEnabledProperty, new Binding(IsSpellCheckEnabledProperty.PropertyName, source: this));
			this.InnerEntry.SetBinding(Entry.IsTextPredictionEnabledProperty, new Binding(IsTextPredictionEnabledProperty.PropertyName, source: this));
			this.InnerEntry.SetBinding(Entry.StyleProperty, new Binding(EntryStyleProperty.PropertyName, source: this));
			this.InnerEntry.SetBinding(Entry.BehaviorsProperty, new Binding(EntryBehaviorsProperty.PropertyName, source: this));
			this.InnerEntry.SetBinding(Entry.IsReadOnlyProperty, new Binding(EntryIsReadOnlyProperty.PropertyName, source: this));
			this.InnerEntry.SetBinding(Entry.ReturnCommandProperty, new Binding(EntryReturnCommandProperty.PropertyName, source: this));
			this.InnerEntry.SetBinding(Entry.IsPasswordProperty, new Binding(EntryIsPasswordProperty.PropertyName, source: this));
			this.InnerEntry.SetBinding(Entry.HorizontalTextAlignmentProperty, new Binding(EntryTextHorizontalAlignmentProperty.PropertyName, source: this));

			this.Image1.SetBinding(SvgCachedImage.SourceProperty, new Binding(IconLeftProperty.PropertyName, source: this));
			this.Image2.SetBinding(SvgCachedImage.SourceProperty, new Binding(IconRightProperty.PropertyName, source: this));

			this.Image1.SetBinding(SvgCachedImage.HeightRequestProperty, new Binding(HeightIconLeftProperty.PropertyName, source: this));
			this.Image2.SetBinding(SvgCachedImage.HeightRequestProperty, new Binding(HeightIconRightProperty.PropertyName, source: this));

			this.Image1.SetBinding(SvgCachedImage.WidthRequestProperty, new Binding(WidthIconLeftProperty.PropertyName, source: this));
			this.Image2.SetBinding(SvgCachedImage.WidthRequestProperty, new Binding(WidthIconRightProperty.PropertyName, source: this));
			this.RightImageGestureRecognizer.SetBinding(TapGestureRecognizer.CommandProperty, new Binding(RightImageCommandProperty.PropertyName, source: this));
		}

		/// <summary>
		/// Evento TextChanged
		/// </summary>
		public event EventHandler<TextChangedEventArgs> TextChanged;

		/// <summary>
		/// Event sent when the entry is focused.
		/// </summary>
		public event EventHandler<FocusEventArgs> EntryFocused;

		/// <summary>
		/// Event sent when the entry is unfocused.
		/// </summary>
		public event EventHandler<FocusEventArgs> EntryUnfocused;

		/// <summary>
		/// Event sent when the entry is completed.
		/// </summary>
		public event EventHandler EntryCompleted;

		/// <summary>
		/// Event sent when the entry is completed.
		/// </summary>
		public event EventHandler RightImageClicked;


		/// <summary>
		/// Change the value of PropossalMesage between StyleId Entry/ExEntry
		/// </summary>
		protected override void OnPropertyChanged(string propertyName = null)
		{
			base.OnPropertyChanged(propertyName);

			if (propertyName == nameof(this.StyleId))
			{
				this.InnerEntry.StyleId = this.StyleId;
			}
		}
		private void OnTextChanged(object sender, TextChangedEventArgs e)
		{
			TextChanged?.Invoke(sender, e);
		}

		private void TapGestureRecognizer_Tapped(object sender, EventArgs e)
		{
			this.InnerEntry.Focus();
		}

		private void OnEntryFocused(object sender, FocusEventArgs e)
		{
			EntryFocused?.Invoke(sender, e);
		}

		private void OnEntryUnfocused(object sender, FocusEventArgs e)
		{
			EntryUnfocused?.Invoke(sender, e);
		}

		private void OnSendCompleted(object sender, EventArgs e)
		{
			EntryCompleted?.Invoke(sender, e);
		}

		private void OnRightImageCommand(object sender, EventArgs e)
		{
			RightImageClicked?.Invoke(sender, e);
		}
	}
}
