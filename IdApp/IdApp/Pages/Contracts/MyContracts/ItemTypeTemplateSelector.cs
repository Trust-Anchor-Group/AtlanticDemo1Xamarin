﻿using IdApp.Pages.Contracts.MyContracts.ObjectModels;
using Xamarin.Forms;

namespace IdApp.Pages.Contracts.MyContracts
{
	/// <summary>
	/// Data Template Selector, based on Item Type.
	/// </summary>
	public class ItemTypeTemplateSelector : DataTemplateSelector
	{
		/// <summary>
		/// Template to use for contract header
		/// </summary>
		public DataTemplate HeaderTemplate { get; set; }

		/// <summary>
		/// Template to use for contracts
		/// </summary>
		public DataTemplate ContractTemplate { get; set; }

		/// <summary>
		/// Template to use for other items.
		/// </summary>
		public DataTemplate DefaultTemplate { get; set; }

		/// <inheritdoc/>
		protected override DataTemplate OnSelectTemplate(object item, BindableObject container)
		{
			if (item is HeaderModel)
			{
				return this.HeaderTemplate;
			}
			else if (item is ContractModel)
			{
				return this.ContractTemplate;
			}

			return this.DefaultTemplate;
		}
	}
}