﻿using System;
using Xamarin.Forms;

namespace Tag.Neuron.Xamarin.UI.Behaviors
{
    /// <summary>
    /// Used for moving focus to the next UI component when a button has been clicked.
    /// </summary>
    public class SetFocusOnClickedBehavior : Behavior<Button>
    {
        /// <summary>
        /// The view to move focus to.
        /// </summary>
        [TypeConverter(typeof(ReferenceTypeConverter))]
        public View SetFocusTo { get; set; }

        /// <inheritdoc/>
        protected override void OnAttachedTo(Button Button)
        {
			Button.Clicked += Button_Clicked;
            base.OnAttachedTo(Button);
        }

		/// <inheritdoc/>
		protected override void OnDetachingFrom(Button Button)
        {
            Button.Clicked -= Button_Clicked;
            base.OnDetachingFrom(Button);
        }

        private void Button_Clicked(object sender, EventArgs e)
        {
            FocusOn(SetFocusTo);
        }

        /// <summary>
        /// Sets focus on an element.
        /// </summary>
        /// <param name="Element">Element to focus on.</param>
        public static void FocusOn(View Element)
		{
            if (!(Element is null) && Element.IsVisible)
            {
                Element.Focus();

                if (Element is Entry Entry && !(Entry.Text is null))
                    Entry.CursorPosition = Entry.Text.Length;
            }
        }
    }
}