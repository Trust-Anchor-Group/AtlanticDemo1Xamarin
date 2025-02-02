﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using Waher.Content;
using Waher.Content.Xml;
using Waher.Events;
using Waher.Persistence;
using Waher.Persistence.Files;
using Waher.Persistence.Serialization;
using Waher.Runtime.Inventory;
using Waher.Runtime.Profiling;

namespace IdApp.Services.Storage
{
	[Singleton]
	internal sealed class StorageService : ServiceReferences, IStorageService
	{
		private readonly LinkedList<TaskCompletionSource<bool>> tasksWaiting = new();
		private readonly string dataFolder;
		private FilesProvider databaseProvider;
		private bool? initialized = null;
		private bool started = false;

		/// <summary>
		/// Creates a new instance of the <see cref="StorageService"/> class.
		/// </summary>
		public StorageService()
		{
			string appDataFolder = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
			this.dataFolder = Path.Combine(appDataFolder, "Data");
		}

		/// <summary>
		/// Folder for database.
		/// </summary>
		public string DataFolder => this.dataFolder;

		#region LifeCycle management

		/// <inheritdoc />
		public async Task Init(ProfilerThread Thread, CancellationToken? cancellationToken)
		{
			lock (this.tasksWaiting)
			{
				if (this.started)
					return;

				this.started = true;
			}

			Thread?.Start();
			try
			{
				Thread?.NewState("Provider");

				if (Database.HasProvider)
					this.databaseProvider = Database.Provider as FilesProvider;

				if (this.databaseProvider is null)
				{
					this.databaseProvider = await this.CreateDatabaseFile(Thread);

					Thread?.NewState("CheckDB");
					await this.databaseProvider.RepairIfInproperShutdown(string.Empty);

					await this.databaseProvider.Start();
				}

				Thread?.NewState("Register");

				if (this.databaseProvider is not null)
				{
					Database.Register(this.databaseProvider, false);
					this.InitDone(true);
					Thread?.Stop();
					return;
				}
			}
			catch (Exception e1)
			{
				e1 = Log.UnnestException(e1);
				Thread?.Exception(e1);
				this.LogService.LogException(e1);
			}

			try
			{
				/* On iOS the UI is not initialised at this point, need to fuind another solution
				Thread?.NewState("UI");
				if (await this.UiSerializer.DisplayAlert(LocalizationResourceManager.Current["DatabaseIssue"], LocalizationResourceManager.Current["DatabaseCorruptInfoText"], LocalizationResourceManager.Current["RepairAndContinue"], LocalizationResourceManager.Current["ContinueAnyway"]))
				*/
				//TODO: when UI is ready, show an alert that the database was reset due to unrecoverable error
				//TODO: say to close the application in a controlled manner
				{
					try
					{
						Thread?.NewState("Delete");
						Directory.Delete(this.dataFolder, true);

						Thread?.NewState("Recreate");
						this.databaseProvider = await this.CreateDatabaseFile(Thread);

						Thread?.NewState("Repair");
						await this.databaseProvider.RepairIfInproperShutdown(string.Empty);

						await this.databaseProvider.Start();

						if (!Database.HasProvider)
						{
							Thread?.NewState("Register");
							Database.Register(this.databaseProvider, false);
							this.InitDone(true);
							Thread?.Stop();
							return;
						}
					}
					catch (Exception e3)
					{
						e3 = Log.UnnestException(e3);
						Thread?.Exception(e3);
						this.LogService.LogException(e3);

						await App.Stop();
						/*
						Thread?.NewState("UI");
						await this.UiSerializer.DisplayAlert(LocalizationResourceManager.Current["DatabaseIssue"], LocalizationResourceManager.Current["DatabaseRepairFailedInfoText"], LocalizationResourceManager.Current["Ok"]);
						*/
					}
				}

				this.InitDone(false);
			}
			finally
			{
				Thread?.Stop();
			}
		}

		private void InitDone(bool Result)
		{
			lock (this.tasksWaiting)
			{
				this.initialized = Result;

				foreach (TaskCompletionSource<bool> Wait in this.tasksWaiting)
					Wait.TrySetResult(Result);

				this.tasksWaiting.Clear();
			}
		}

		/// <inheritdoc />
		public Task<bool> WaitInitDone()
		{
			lock (this.tasksWaiting)
			{
				if (this.initialized.HasValue)
					return Task.FromResult<bool>(this.initialized.Value);

				TaskCompletionSource<bool> Wait = new();
				this.tasksWaiting.AddLast(Wait);

				return Wait.Task;
			}
		}

		/// <inheritdoc />
		public async Task Shutdown()
		{
			lock (this.tasksWaiting)
			{
				this.initialized = null;
				this.started = false;
			}

			try
			{
				if (this.databaseProvider is not null)
				{
					Database.Register(new NullDatabaseProvider(), false);
					await this.databaseProvider.Flush();
					await this.databaseProvider.Stop();
					this.databaseProvider = null;
				}
			}
			catch (Exception e)
			{
				this.LogService.LogException(e);
			}
		}

		private Task<FilesProvider> CreateDatabaseFile(ProfilerThread Thread)
		{
			FilesProvider.AsyncFileIo = false;  // Asynchronous file I/O induces a long delay during startup on mobile platforms. Why??

			return FilesProvider.CreateAsync(this.dataFolder, "Default", 8192, 10000, 8192, Encoding.UTF8,
				(int)Constants.Timeouts.Database.TotalMilliseconds, this.CryptoService.GetCustomKey, Thread);
		}

		#endregion

		public async Task Insert(object obj)
		{
			await Database.Insert(obj);
			await Database.Provider.Flush();
		}

		public async Task Update(object obj)
		{
			await Database.Update(obj);
			await Database.Provider.Flush();
		}

		public Task<T> FindFirstDeleteRest<T>() where T : class
		{
			return Database.FindFirstDeleteRest<T>();
		}

		public Task<T> FindFirstIgnoreRest<T>() where T : class
		{
			return Database.FindFirstIgnoreRest<T>();
		}

		public Task Export(IDatabaseExport exportOutput)
		{
			return Database.Export(exportOutput);
		}

		/// <summary>
		/// Flags the database for repair, so that the next time the app is opened, the database will be repaired.
		/// </summary>
		public void FlagForRepair()
		{
			this.DeleteFile("Start.txt");
			this.DeleteFile("Stop.txt");
		}

		private void DeleteFile(string FileName)
		{
			try
			{
				FileName = Path.Combine(this.dataFolder, FileName);

				if (File.Exists(FileName))
					File.Delete(FileName);
			}
			catch (Exception)
			{
				// Ignore, to avoid infinite loops if event log has an inconsistency.
			}
		}
	}
}
