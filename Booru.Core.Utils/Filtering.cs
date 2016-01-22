using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace Booru.Core.Utils
{
	public class Filter
	{
		public delegate BitArray OnTagDataRequest(string Tag, int Size);
		class FilteringList : List<Filter> 
		{ 
			public void Add(string Template, action Action)
			{
				var f = new Filter(Template, Action);
				if (f.Value != null)
					base.Add(f);
			}
		}
		enum action { and, or, not, none }
		object Value;
		action Action = action.none;
		FilteringList AddResult(FilteringList l, MatchCollection mc)
		{
			Group o;
			foreach (Match m in mc)
				if ((o = m.Groups["and"]).Success || (o = m.Groups["gand"]).Success)
					l.Add(o.Value, action.and);
				else if ((o = m.Groups["or"]).Success || (o = m.Groups["gor"]).Success)
					l.Add(o.Value, action.or);
				else if ((o = m.Groups["not"]).Success || (o = m.Groups["gnot"]).Success)
					l.Add(o.Value, action.not);
			if (l.Count == 0) return null;
			return l;
		}
		Filter(string Template, action action)
		{
			Action = action;
			build(Template.Trim());
		}  

		Filter(string Template)
		{
			var tmpl = Template.Trim();
			if (!string.IsNullOrWhiteSpace(tmpl))
			{
				int i = "-+".IndexOf(tmpl[0]);
				if (i >= 0 && (tmpl = tmpl.Substring(1)).Length == 0)
					return;
				Action = i == 0 ? action.not : action.and;
				build(tmpl);
			}
		}

		void build(string Template)
		{
			var mc = Regex.Matches(Template, @"\+\((?<and>.+?)\)|\-\((?<not>.+?)\)|\((?<or>.+?)\)|\+(?<and>[^ ]+)|\-(?<not>[^ ]+)|(?<or>[^ ]+)", RegexOptions.ExplicitCapture | RegexOptions.Compiled);
			if (mc.Count > 1)
				Value = AddResult(new FilteringList(), mc);
			else
				Value = Template;
		}

		BitArray Add(BitArray dst, BitArray src)
		{
			switch (Action)
			{
				case action.or: return dst.Or(src);
				case action.not: return dst.And(src.Not());
				case action.and: return dst.And(src);
			}
			return dst;
		}

		BitArray Exec(BitArray Data, OnTagDataRequest OnRequest, SortedDictionary<string, BitArray> results,  ref string Statistics, string indent)
		{
			BitArray rslt = Data;
			var sw = new Stopwatch();
			sw.Start();
			try
			{
				if (Action == action.none) return rslt;
				if (Value is FilteringList)
				{
					Statistics += string.Format("{0}Group:\r\n", indent);
					BitArray res = new BitArray(Data.Length, false);
					foreach (var q in (FilteringList)Value)
						res = q.Exec(res, OnRequest, results, ref Statistics, indent + "\t");
					return rslt = Add(Data, res);
				}
				BitArray src;
				if(!results.TryGetValue((string)Value, out src))
					results[(string)Value] = src = OnRequest((string)Value, Data.Length);
				Statistics += string.Format("{0}Value: {1} [{2}]\r\n", indent, (string)Value, src.TrueCnt());
				return rslt = Add(Data, src);
			}
			finally
			{
				sw.Stop();
				Statistics += string.Format("{0}Execution time: {1}\r\n{0}Total entries: {2}\r\n", indent, sw.Elapsed, rslt.TrueCnt());
			}
		}

		public static BitArray Execute(string Template, int Size, OnTagDataRequest OnRequest, ref string Statistics)
		{
			BitArray rslt = new BitArray(0);
			SortedDictionary<string, BitArray> results = new SortedDictionary<string, BitArray>();
			Statistics = string.Empty;
			var sw = new Stopwatch();
			try
			{
				sw.Start();
				if (string.IsNullOrWhiteSpace(Template))
					return rslt = OnRequest(string.Empty, Size);
				return rslt = new Filter(Template).Exec(new BitArray(Size, true), OnRequest, results, ref Statistics, string.Empty);
			}
			finally
			{
				sw.Stop();
				Statistics += string.Format("\r\n\r\nTotal execution time: {0}\r\nTotal entries: {1}", sw.Elapsed, rslt.TrueCnt());
			}
		}
	}
}
