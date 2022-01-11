﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using Waher.Content.Markdown;
using Waher.Content.Markdown.Model;
using Waher.Runtime.Inventory;

namespace IdApp.Pages.Contacts.Chat.MarkdownExtensions.Multimedia
{
	/// <summary>
	/// Implements the iotsc URI Scheme
	/// </summary>
	public class IotScUriScheme : MultimediaContent
	{
		/// <summary>
		/// Implements the iotsc URI Scheme
		/// </summary>
		public IotScUriScheme()
		{
		}

		/// <inheritdoc/>
		public override Grade Supports(MultimediaItem Item)
		{
			if (Item.Url.StartsWith("iotsc:", StringComparison.OrdinalIgnoreCase))
				return Grade.Excellent;
			else
				return Grade.NotAtAll;
		}

		/// <inheritdoc/>
		public override bool EmbedInlineLink(string Url)
		{
			return true;
		}

		/// <inheritdoc/>
		public override async Task GenerateHTML(StringBuilder Output, MultimediaItem[] Items, IEnumerable<MarkdownElement> ChildNodes, bool AloneInParagraph, MarkdownDocument Document)
		{
			foreach (MarkdownElement E in ChildNodes)
				await E.GenerateHTML(Output);
		}

		/// <inheritdoc/>
		public override async Task GenerateXAML(XmlWriter Output, TextAlignment TextAlignment, MultimediaItem[] Items, IEnumerable<MarkdownElement> ChildNodes, bool AloneInParagraph, MarkdownDocument Document)
		{
			foreach (MarkdownElement E in ChildNodes)
				await E.GenerateXAML(Output, TextAlignment);
		}

		/// <inheritdoc/>
		public override async Task GenerateXamarinForms(XmlWriter Output, TextAlignment TextAlignment, MultimediaItem[] Items, IEnumerable<MarkdownElement> ChildNodes, bool AloneInParagraph, MarkdownDocument Document)
		{
			foreach (MultimediaItem Item in Items)
			{
				Output.WriteStartElement("StackLayout");
				Output.WriteAttributeString("Orientation", "Vertical");
				Output.WriteAttributeString("HorizontalOptions", "Center");

				Output.WriteStartElement("Label");
				Output.WriteAttributeString("Text", FontAwesome.Paragraph);
				Output.WriteAttributeString("FontFamily", "{StaticResource FontAwesomeSolid}");
				Output.WriteAttributeString("FontSize", "Large");
				Output.WriteAttributeString("HorizontalOptions", "Center");
				Output.WriteEndElement();

				Output.WriteStartElement("StackLayout");
				Output.WriteAttributeString("Orientation", "Horizontal");
				Output.WriteAttributeString("HorizontalOptions", "Center");

				Output.WriteStartElement("Label");
				Output.WriteAttributeString("LineBreakMode", "WordWrap");
				Output.WriteAttributeString("TextType", "Html");
				Output.WriteAttributeString("FontSize", "Medium");
				StringBuilder Html = new StringBuilder();

				foreach (MarkdownElement E in ChildNodes)
					await E.GenerateHTML(Html);

				Output.WriteValue(Html.ToString());
				Output.WriteEndElement();

				Output.WriteEndElement();

				Output.WriteStartElement("StackLayout.GestureRecognizers");

				Output.WriteStartElement("TapGestureRecognizer");
				Output.WriteAttributeString("Command", "{Binding Path=IotScUriClicked}");
				Output.WriteAttributeString("CommandParameter", Item.Url);
				Output.WriteEndElement();

				Output.WriteEndElement();
				Output.WriteEndElement();
				break;
			}
		}
	}
}