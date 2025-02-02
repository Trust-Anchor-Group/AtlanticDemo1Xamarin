﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Runtime.Serialization;
using System.Security.Permissions;
using System.Text;
using System.Text.RegularExpressions;

namespace UpdateVersionInfo
{
	public class OptionValueCollection : IList, IList<string>
	{
		private readonly List<string> values = new();
		private readonly OptionContext c;

		internal OptionValueCollection(OptionContext c)
		{
			this.c = c;
		}

		#region ICollection

		void ICollection.CopyTo(Array array, int index)
		{
			(this.values as ICollection).CopyTo(array, index);
		}

		bool ICollection.IsSynchronized
		{
			get
			{
				return (this.values as ICollection).IsSynchronized;
			}
		}
		object ICollection.SyncRoot
		{
			get
			{
				return (this.values as ICollection).SyncRoot;
			}
		}

		#endregion

		#region ICollection<T>
		public void Add(string item) { this.values.Add(item); }
		public void Clear() { this.values.Clear(); }
		public bool Contains(string item) { return this.values.Contains(item); }
		public void CopyTo(string[] array, int arrayIndex) { this.values.CopyTo(array, arrayIndex); }
		public bool Remove(string item) { return this.values.Remove(item); }
		public int Count { get { return this.values.Count; } }
		public bool IsReadOnly { get { return false; } }
		#endregion

		#region IEnumerable
		IEnumerator IEnumerable.GetEnumerator() { return this.values.GetEnumerator(); }
		#endregion

		#region IEnumerable<T>
		public IEnumerator<string> GetEnumerator() { return this.values.GetEnumerator(); }
		#endregion

		#region IList
		int IList.Add(object value) { return (this.values as IList).Add(value); }
		bool IList.Contains(object value) { return (this.values as IList).Contains(value); }
		int IList.IndexOf(object value) { return (this.values as IList).IndexOf(value); }
		void IList.Insert(int index, object value) { (this.values as IList).Insert(index, value); }
		void IList.Remove(object value) { (this.values as IList).Remove(value); }
		void IList.RemoveAt(int index) { (this.values as IList).RemoveAt(index); }
		bool IList.IsFixedSize { get { return false; } }
		object IList.this[int index] { get { return this[index]; } set { (this.values as IList)[index] = value; } }
		#endregion

		#region IList<T>
		public int IndexOf(string item) { return this.values.IndexOf(item); }
		public void Insert(int index, string item) { this.values.Insert(index, item); }
		public void RemoveAt(int index) { this.values.RemoveAt(index); }

		private void AssertValid(int index)
		{
			if (this.c.Option is null)
				throw new InvalidOperationException("OptionContext.Option is null.");

			if (index >= this.c.Option.MaxValueCount)
				throw new ArgumentOutOfRangeException(nameof(index));

			if (this.c.Option.OptionValueType == OptionValueType.Required && index >= this.values.Count)
			{
				throw new OptionException(string.Format(
							this.c.OptionSet.MessageLocalizer("Missing required value for option '{0}'."), this.c.OptionName),
						this.c.OptionName);
			}
		}

		public string this[int index]
		{
			get
			{
				this.AssertValid(index);
				return index >= this.values.Count ? null : this.values[index];
			}
			set
			{
				this.values[index] = value;
			}
		}
		#endregion

		public List<string> ToList()
		{
			return new List<string>(this.values);
		}

		public string[] ToArray()
		{
			return this.values.ToArray();
		}

		public override string ToString()
		{
			return string.Join(", ", this.values.ToArray());
		}
	}

	public class OptionContext
	{
		private Option option;
		private string name;
		private int index;
		private readonly OptionSet set;
		private readonly OptionValueCollection c;

		public OptionContext(OptionSet set)
		{
			this.set = set;
			this.c = new OptionValueCollection(this);
		}

		public Option Option
		{
			get { return this.option; }
			set { this.option = value; }
		}

		public string OptionName
		{
			get { return this.name; }
			set { this.name = value; }
		}

		public int OptionIndex
		{
			get { return this.index; }
			set { this.index = value; }
		}

		public OptionSet OptionSet
		{
			get { return this.set; }
		}

		public OptionValueCollection OptionValues
		{
			get { return this.c; }
		}
	}

	public enum OptionValueType
	{
		None,
		Optional,
		Required,
	}

	public abstract class Option
	{
		private readonly string prototype, description;
		private readonly string[] names;
		private readonly OptionValueType type;
		private readonly int count;
		private string[] separators;

		protected Option(string prototype, string description)
			: this(prototype, description, 1)
		{
		}

		protected Option(string prototype, string description, int maxValueCount)
		{
			if (prototype is null)
				throw new ArgumentNullException(nameof(prototype));

			if (prototype.Length == 0)
				throw new ArgumentException("Cannot be the empty string.", nameof(prototype));

			if (maxValueCount < 0)
				throw new ArgumentOutOfRangeException(nameof(maxValueCount));

			this.prototype = prototype;
			this.names = prototype.Split('|');
			this.description = description;
			this.count = maxValueCount;
			this.type = this.ParsePrototype();

			if (this.count == 0 && this.type != OptionValueType.None)
				throw new ArgumentException(
						"Cannot provide maxValueCount of 0 for OptionValueType.Required or OptionValueType.Optional.",
						nameof(maxValueCount));
			if (this.type == OptionValueType.None && maxValueCount > 1)
				throw new ArgumentException(
						string.Format("Cannot provide maxValueCount of {0} for OptionValueType.None.", maxValueCount),
						nameof(maxValueCount));
			if (Array.IndexOf(this.names, "<>") >= 0 &&
					((this.names.Length == 1 && this.type != OptionValueType.None) ||
					 (this.names.Length > 1 && this.MaxValueCount > 1)))
				throw new ArgumentException(
						"The default option handler '<>' cannot require values.",
						nameof(prototype));
		}

		public string Prototype { get { return this.prototype; } }
		public string Description { get { return this.description; } }
		public OptionValueType OptionValueType { get { return this.type; } }
		public int MaxValueCount { get { return this.count; } }

		public string[] GetNames()
		{
			return (string[])this.names.Clone();
		}

		public string[] GetValueSeparators()
		{
			if (this.separators is null)
				return Array.Empty<string>();

			return (string[])this.separators.Clone();
		}

		protected static T Parse<T>(string value, OptionContext c)
		{
			TypeConverter conv = TypeDescriptor.GetConverter(typeof(T));
			T t = default;
			try
			{
				if (value is not null)
					t = (T)conv.ConvertFromString(value);
			}
			catch (Exception e)
			{
				throw new OptionException(
						string.Format(
							c.OptionSet.MessageLocalizer("Could not convert string `{0}' to type {1} for option `{2}'."),
							value, typeof(T).Name, c.OptionName),
						c.OptionName, e);
			}
			return t;
		}

		internal string[] Names { get { return this.names; } }
		internal string[] ValueSeparators { get { return this.separators; } }

		static readonly char[] nameTerminator = new char[] { '=', ':' };

		private OptionValueType ParsePrototype()
		{
			char type = '\0';
			List<string> seps = new();
			for (int i = 0; i < this.names.Length; ++i)
			{
				string name = this.names[i];
				if (name.Length == 0)
					throw new ArgumentException("Empty option names are not supported.", "prototype");

				int end = name.IndexOfAny(nameTerminator);
				if (end == -1)
					continue;
				this.names[i] = name.Substring(0, end);
				if (type == '\0' || type == name[end])
					type = name[end];
				else
					throw new ArgumentException(
							string.Format("Conflicting option types: '{0}' vs. '{1}'.", type, name[end]),
							"prototype");
				AddSeparators(name, end, seps);
			}

			if (type == '\0')
				return OptionValueType.None;

			if (this.count <= 1 && seps.Count != 0)
				throw new ArgumentException(
						string.Format("Cannot provide key/value separators for Options taking {0} value(s).", this.count),
						"prototype");
			if (this.count > 1)
			{
				if (seps.Count == 0)
					this.separators = new string[] { ":", "=" };
				else if (seps.Count == 1 && seps[0].Length == 0)
					this.separators = null;
				else
					this.separators = seps.ToArray();
			}

			return type == '=' ? OptionValueType.Required : OptionValueType.Optional;
		}

		private static void AddSeparators(string name, int end, ICollection<string> seps)
		{
			int start = -1;
			for (int i = end + 1; i < name.Length; ++i)
			{
				switch (name[i])
				{
					case '{':
						if (start != -1)
							throw new ArgumentException(
									string.Format("Ill-formed name/value separator found in \"{0}\".", name),
									nameof(name));
						start = i + 1;
						break;
					case '}':
						if (start == -1)
							throw new ArgumentException(
									string.Format("Ill-formed name/value separator found in \"{0}\".", name),
									nameof(name));
						seps.Add(name[start..i]);
						start = -1;
						break;
					default:
						if (start == -1)
							seps.Add(name[i].ToString());
						break;
				}
			}
			if (start != -1)
				throw new ArgumentException(
						string.Format("Ill-formed name/value separator found in \"{0}\".", name),
						nameof(name));
		}

		public void Invoke(OptionContext c)
		{
			this.OnParseComplete(c);
			c.OptionName = null;
			c.Option = null;
			c.OptionValues.Clear();
		}

		protected abstract void OnParseComplete(OptionContext c);

		public override string ToString()
		{
			return this.Prototype;
		}
	}

	[Serializable]
	public class OptionException : Exception
	{
		private readonly string option;

		public OptionException()
		{
		}

		public OptionException(string message, string optionName)
			: base(message)
		{
			this.option = optionName;
		}

		public OptionException(string message, string optionName, Exception innerException)
			: base(message, innerException)
		{
			this.option = optionName;
		}

		protected OptionException(SerializationInfo info, StreamingContext context)
			: base(info, context)
		{
			this.option = info.GetString("OptionName");
		}

		public string OptionName
		{
			get { return this.option; }
		}

		public override void GetObjectData(SerializationInfo info, StreamingContext context)
		{
			base.GetObjectData(info, context);
			info.AddValue("OptionName", this.option);
		}
	}

	public delegate void OptionAction<TKey, TValue>(TKey key, TValue value);

	public class OptionSet : KeyedCollection<string, Option>
	{
		public OptionSet()
			: this(delegate (string f) { return f; })
		{
		}

		public OptionSet(Converter<string, string> localizer)
		{
			this.localizer = localizer;
		}

		private readonly Converter<string, string> localizer;

		public Converter<string, string> MessageLocalizer
		{
			get { return this.localizer; }
		}

		protected override string GetKeyForItem(Option item)
		{
			if (item is null)
				throw new ArgumentNullException(nameof(item));
			if (item.Names is not null && item.Names.Length > 0)
				return item.Names[0];
			// This should never happen, as it's invalid for Option to be
			// constructed w/o any names.
			throw new InvalidOperationException("Option has no names!");
		}

		[Obsolete("Use KeyedCollection.this[string]")]
		protected Option GetOptionForName(string option)
		{
			if (option is null)
				throw new ArgumentNullException(nameof(option));
			try
			{
				return base[option];
			}
			catch (KeyNotFoundException)
			{
				return null;
			}
		}

		protected override void InsertItem(int index, Option item)
		{
			base.InsertItem(index, item);
			this.AddImpl(item);
		}

		protected override void RemoveItem(int index)
		{
			base.RemoveItem(index);
			Option p = this.Items[index];
			// KeyedCollection.RemoveItem() handles the 0th item
			for (int i = 1; i < p.Names.Length; ++i)
			{
				this.Dictionary.Remove(p.Names[i]);
			}
		}

		protected override void SetItem(int index, Option item)
		{
			base.SetItem(index, item);
			this.RemoveItem(index);
			this.AddImpl(item);
		}

		private void AddImpl(Option option)
		{
			if (option is null)
				throw new ArgumentNullException(nameof(option));
			List<string> added = new(option.Names.Length);
			try
			{
				// KeyedCollection.InsertItem/SetItem handle the 0th name.
				for (int i = 1; i < option.Names.Length; ++i)
				{
					this.Dictionary.Add(option.Names[i], option);
					added.Add(option.Names[i]);
				}
			}
			catch (Exception)
			{
				foreach (string name in added)
					this.Dictionary.Remove(name);
				throw;
			}
		}

		public new OptionSet Add(Option option)
		{
			base.Add(option);
			return this;
		}

		sealed class ActionOption : Option
		{
			private readonly Action<OptionValueCollection> action;

			public ActionOption(string prototype, string description, int count, Action<OptionValueCollection> action)
				: base(prototype, description, count)
			{
				this.action = action ?? throw new ArgumentNullException(nameof(action));
			}

			protected override void OnParseComplete(OptionContext c)
			{
				this.action(c.OptionValues);
			}
		}

		public OptionSet Add(string prototype, Action<string> action)
		{
			return this.Add(prototype, null, action);
		}

		public OptionSet Add(string prototype, string description, Action<string> action)
		{
			if (action is null)
				throw new ArgumentNullException(nameof(action));
			Option p = new ActionOption(prototype, description, 1,
					delegate (OptionValueCollection v) { action(v[0]); });
			base.Add(p);
			return this;
		}

		public OptionSet Add(string prototype, OptionAction<string, string> action)
		{
			return this.Add(prototype, null, action);
		}

		public OptionSet Add(string prototype, string description, OptionAction<string, string> action)
		{
			if (action is null)
				throw new ArgumentNullException(nameof(action));
			Option p = new ActionOption(prototype, description, 2,
					delegate (OptionValueCollection v) { action(v[0], v[1]); });
			base.Add(p);
			return this;
		}

		sealed class ActionOption<T> : Option
		{
			private readonly Action<T> action;

			public ActionOption(string prototype, string description, Action<T> action)
				: base(prototype, description, 1)
			{
				this.action = action ?? throw new ArgumentNullException(nameof(action));
			}

			protected override void OnParseComplete(OptionContext c)
			{
				this.action(Parse<T>(c.OptionValues[0], c));
			}
		}

		sealed class ActionOption<TKey, TValue> : Option
		{
			private readonly OptionAction<TKey, TValue> action;

			public ActionOption(string prototype, string description, OptionAction<TKey, TValue> action)
				: base(prototype, description, 2)
			{
				this.action = action ?? throw new ArgumentNullException(nameof(action));
			}

			protected override void OnParseComplete(OptionContext c)
			{
				this.action(
						Parse<TKey>(c.OptionValues[0], c),
						Parse<TValue>(c.OptionValues[1], c));
			}
		}

		public OptionSet Add<T>(string prototype, Action<T> action)
		{
			return this.Add(prototype, null, action);
		}

		public OptionSet Add<T>(string prototype, string description, Action<T> action)
		{
			return this.Add(new ActionOption<T>(prototype, description, action));
		}

		public OptionSet Add<TKey, TValue>(string prototype, OptionAction<TKey, TValue> action)
		{
			return this.Add(prototype, null, action);
		}

		public OptionSet Add<TKey, TValue>(string prototype, string description, OptionAction<TKey, TValue> action)
		{
			return this.Add(new ActionOption<TKey, TValue>(prototype, description, action));
		}

		protected virtual OptionContext CreateOptionContext()
		{
			return new OptionContext(this);
		}

#if LINQ
		public List<string> Parse (IEnumerable<string> arguments)
		{
			bool process = true;
			OptionContext c = CreateOptionContext ();
			c.OptionIndex = -1;
			var def = GetOptionForName ("<>");
			var unprocessed = 
				from argument in arguments
				where ++c.OptionIndex >= 0 && (process || def is not null)
					? process
						? argument == "--" 
							? (process = false)
							: !Parse (argument, c)
								? def is not null
									? Unprocessed (null, def, c, argument) 
									: true
								: false
						: def is not null
							? Unprocessed (null, def, c, argument)
							: true
					: true
				select argument;
			List<string> r = unprocessed.ToList ();
			if (c.Option is not null)
				c.Option.Invoke (c);
			return r;
		}
#else
		public List<string> Parse(IEnumerable<string> arguments)
		{
			OptionContext c = this.CreateOptionContext();
			c.OptionIndex = -1;
			bool process = true;
			List<string> unprocessed = new();
			Option def = this.Contains("<>") ? this["<>"] : null;
			foreach (string argument in arguments)
			{
				++c.OptionIndex;
				if (argument == "--")
				{
					process = false;
					continue;
				}
				if (!process)
				{
					Unprocessed(unprocessed, def, c, argument);
					continue;
				}
				if (!this.Parse(argument, c))
					Unprocessed(unprocessed, def, c, argument);
			}
			if (c.Option is not null)
				c.Option.Invoke(c);
			return unprocessed;
		}
#endif

		private static bool Unprocessed(ICollection<string> extra, Option def, OptionContext c, string argument)
		{
			if (def is null)
			{
				extra.Add(argument);
				return false;
			}
			c.OptionValues.Add(argument);
			c.Option = def;
			c.Option.Invoke(c);
			return false;
		}

		private readonly Regex valueOption = new(
			@"^(?<flag>--|-|/)(?<name>[^:=]+)((?<sep>[:=])(?<value>.*))?$");

		protected bool GetOptionParts(string argument, out string flag, out string name, out string sep, out string value)
		{
			if (argument is null)
				throw new ArgumentNullException(nameof(argument));

			flag = name = sep = value = null;
			Match m = this.valueOption.Match(argument);
			if (!m.Success)
			{
				return false;
			}
			flag = m.Groups["flag"].Value;
			name = m.Groups["name"].Value;
			if (m.Groups["sep"].Success && m.Groups["value"].Success)
			{
				sep = m.Groups["sep"].Value;
				value = m.Groups["value"].Value;
			}
			return true;
		}

		protected virtual bool Parse(string argument, OptionContext c)
		{
			if (c.Option is not null)
			{
				this.ParseValue(argument, c);
				return true;
			}

			if (!this.GetOptionParts(argument, out string f, out string n, out string s, out string v))
				return false;

			Option p;
			if (this.Contains(n))
			{
				p = this[n];
				c.OptionName = f + n;
				c.Option = p;
				switch (p.OptionValueType)
				{
					case OptionValueType.None:
						c.OptionValues.Add(n);
						c.Option.Invoke(c);
						break;
					case OptionValueType.Optional:
					case OptionValueType.Required:
						this.ParseValue(v, c);
						break;
				}
				return true;
			}
			// no match; is it a bool option?
			if (this.ParseBool(argument, n, c))
				return true;
			// is it a bundled option?
			if (this.ParseBundledValue(f, string.Concat(n + s + v), c))
				return true;

			return false;
		}

		private void ParseValue(string option, OptionContext c)
		{
			if (option is not null)
				foreach (string o in c.Option.ValueSeparators is not null
						? option.Split(c.Option.ValueSeparators, StringSplitOptions.None)
						: new string[] { option })
				{
					c.OptionValues.Add(o);
				}
			if (c.OptionValues.Count == c.Option.MaxValueCount ||
					c.Option.OptionValueType == OptionValueType.Optional)
				c.Option.Invoke(c);
			else if (c.OptionValues.Count > c.Option.MaxValueCount)
			{
				throw new OptionException(this.localizer(string.Format(
								"Error: Found {0} option values when expecting {1}.",
								c.OptionValues.Count, c.Option.MaxValueCount)),
						c.OptionName);
			}
		}

		private bool ParseBool(string option, string n, OptionContext c)
		{
			Option p;
			string rn;
			if (n.Length >= 1 && (n[^1] == '+' || n[^1] == '-') &&
					this.Contains((rn = n[0..^1])))
			{
				p = this[rn];
				string v = n[^1] == '+' ? option : null;
				c.OptionName = option;
				c.Option = p;
				c.OptionValues.Add(v);
				p.Invoke(c);
				return true;
			}
			return false;
		}

		private bool ParseBundledValue(string f, string n, OptionContext c)
		{
			if (f != "-")
				return false;
			for (int i = 0; i < n.Length; ++i)
			{
				Option p;
				string opt = f + n[i].ToString();
				string rn = n[i].ToString();
				if (!this.Contains(rn))
				{
					if (i == 0)
						return false;
					throw new OptionException(string.Format(this.localizer(
									"Cannot bundle unregistered option '{0}'."), opt), opt);
				}
				p = this[rn];
				switch (p.OptionValueType)
				{
					case OptionValueType.None:
						Invoke(c, opt, n, p);
						break;
					case OptionValueType.Optional:
					case OptionValueType.Required:
						{
							string v = n[(i + 1)..];
							c.Option = p;
							c.OptionName = opt;
							this.ParseValue(v.Length != 0 ? v : null, c);
							return true;
						}
					default:
						throw new InvalidOperationException("Unknown OptionValueType: " + p.OptionValueType);
				}
			}
			return true;
		}

		private static void Invoke(OptionContext c, string name, string value, Option option)
		{
			c.OptionName = name;
			c.Option = option;
			c.OptionValues.Add(value);
			option.Invoke(c);
		}

		private const int optionWidth = 29;

		public void WriteOptionDescriptions(TextWriter o)
		{
			foreach (Option p in this)
			{
				int written = 0;
				if (!this.WriteOptionPrototype(o, p, ref written))
					continue;

				if (written < optionWidth)
					o.Write(new string(' ', optionWidth - written));
				else
				{
					o.WriteLine();
					o.Write(new string(' ', optionWidth));
				}

				List<string> lines = GetLines(this.localizer(GetDescription(p.Description)));
				o.WriteLine(lines[0]);
				string prefix = new(' ', optionWidth + 2);
				for (int i = 1; i < lines.Count; ++i)
				{
					o.Write(prefix);
					o.WriteLine(lines[i]);
				}
			}
		}

		bool WriteOptionPrototype(TextWriter o, Option p, ref int written)
		{
			string[] names = p.Names;

			int i = GetNextOptionIndex(names, 0);
			if (i == names.Length)
				return false;

			if (names[i].Length == 1)
			{
				Write(o, ref written, "  -");
				Write(o, ref written, names[0]);
			}
			else
			{
				Write(o, ref written, "      --");
				Write(o, ref written, names[0]);
			}

			for (i = GetNextOptionIndex(names, i + 1);
					i < names.Length; i = GetNextOptionIndex(names, i + 1))
			{
				Write(o, ref written, ", ");
				Write(o, ref written, names[i].Length == 1 ? "-" : "--");
				Write(o, ref written, names[i]);
			}

			if (p.OptionValueType == OptionValueType.Optional ||
					p.OptionValueType == OptionValueType.Required)
			{
				if (p.OptionValueType == OptionValueType.Optional)
				{
					Write(o, ref written, this.localizer("["));
				}
				Write(o, ref written, this.localizer("=" + GetArgumentName(0, p.MaxValueCount, p.Description)));
				string sep = p.ValueSeparators is not null && p.ValueSeparators.Length > 0
					? p.ValueSeparators[0]
					: " ";
				for (int c = 1; c < p.MaxValueCount; ++c)
				{
					Write(o, ref written, this.localizer(sep + GetArgumentName(c, p.MaxValueCount, p.Description)));
				}
				if (p.OptionValueType == OptionValueType.Optional)
				{
					Write(o, ref written, this.localizer("]"));
				}
			}
			return true;
		}

		static int GetNextOptionIndex(string[] names, int i)
		{
			while (i < names.Length && names[i] == "<>")
			{
				++i;
			}
			return i;
		}

		static void Write(TextWriter o, ref int n, string s)
		{
			n += s.Length;
			o.Write(s);
		}

		private static string GetArgumentName(int index, int maxIndex, string description)
		{
			if (description is null)
				return maxIndex == 1 ? "VALUE" : "VALUE" + (index + 1);
			string[] nameStart;
			if (maxIndex == 1)
				nameStart = new string[] { "{0:", "{" };
			else
				nameStart = new string[] { "{" + index + ":" };
			for (int i = 0; i < nameStart.Length; ++i)
			{
				int start, j = 0;
				do
				{
					start = description.IndexOf(nameStart[i], j, StringComparison.InvariantCultureIgnoreCase);
				} while (start >= 0 && j != 0 && description[j++ - 1] == '{');
				if (start == -1)
					continue;
				int end = description.IndexOf("}", start, StringComparison.InvariantCultureIgnoreCase);
				if (end == -1)
					continue;
				return description.Substring(start + nameStart[i].Length, end - start - nameStart[i].Length);
			}
			return maxIndex == 1 ? "VALUE" : "VALUE" + (index + 1);
		}

		private static string GetDescription(string description)
		{
			if (description is null)
				return string.Empty;
			StringBuilder sb = new(description.Length);
			int start = -1;
			for (int i = 0; i < description.Length; ++i)
			{
				switch (description[i])
				{
					case '{':
						if (i == start)
						{
							sb.Append('{');
							start = -1;
						}
						else if (start < 0)
							start = i + 1;
						break;
					case '}':
						if (start < 0)
						{
							if ((i + 1) == description.Length || description[i + 1] != '}')
								throw new InvalidOperationException("Invalid option description: " + description);
							++i;
							sb.Append('}');
						}
						else
						{
							sb.Append(description[start..i]);
							start = -1;
						}
						break;
					case ':':
						if (start < 0)
							goto default;
						start = i + 1;
						break;
					default:
						if (start < 0)
							sb.Append(description[i]);
						break;
				}
			}
			return sb.ToString();
		}

		private static List<string> GetLines(string description)
		{
			List<string> lines = new();
			if (string.IsNullOrEmpty(description))
			{
				lines.Add(string.Empty);
				return lines;
			}
			int length = 80 - optionWidth - 2;
			int start = 0, end;
			do
			{
				end = GetLineEnd(start, length, description);
				bool cont = false;
				if (end < description.Length)
				{
					char c = description[end];
					if (c == '-' || (char.IsWhiteSpace(c) && c != '\n'))
						++end;
					else if (c != '\n')
					{
						cont = true;
						--end;
					}
				}
				lines.Add(description[start..end]);
				if (cont)
				{
					lines[^1] += "-";
				}
				start = end;
				if (start < description.Length && description[start] == '\n')
					++start;
			} while (end < description.Length);
			return lines;
		}

		private static int GetLineEnd(int start, int length, string description)
		{
			int end = Math.Min(start + length, description.Length);
			int sep = -1;
			for (int i = start; i < end; ++i)
			{
				switch (description[i])
				{
					case ' ':
					case '\t':
					case '\v':
					case '-':
					case ',':
					case '.':
					case ';':
						sep = i;
						break;
					case '\n':
						return i;
				}
			}
			if (sep == -1 || end == description.Length)
				return end;
			return sep;
		}
	}
}
