﻿using NeuroFeatures;

namespace IdApp.Services.Notification.Wallet
{
	/// <summary>
	/// Contains information about a change in internal variables of a state-machine associated with a token.
	/// </summary>
	public class StateMachineVariablesUpdatedNotificationEvent : TokenNotificationEvent
	{
		/// <summary>
		/// Contains information about a change in internal variables of a state-machine associated with a token.
		/// </summary>
		public StateMachineVariablesUpdatedNotificationEvent()
			: base()
		{
		}

		/// <summary>
		/// Contains information about a change in internal variables of a state-machine associated with a token.
		/// </summary>
		/// <param name="e">Event arguments.</param>
		public StateMachineVariablesUpdatedNotificationEvent(VariablesUpdatedEventArgs e)
			: base(e)
		{
		}
	}
}