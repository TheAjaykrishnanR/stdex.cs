/* stdex.cs
 *
 * Extensions for the C# Standard Library
 * Author: Ajaykrishnan R
 * --------------------------------------

 * MIT License
 *
 * Copyright (c) 2026 Ajaykrishnan R
 *
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights
 * to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the Software is
 * furnished to do so, subject to the following conditions:
 *
 * The above copyright notice and this permission notice shall be included in all
 * copies or substantial portions of the Software.
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
 * SOFTWARE.
 * */

#nullable enable

using System;
using System.Linq;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace stdex;

/// <summary>
/// A container for an always ordered collection. Any element added using 
/// the Add() method will be put in their respective ordering amongs the
/// already present elements, resulting in an always ordered collection.
/// </summary>
class OrderedList<T> where T: IComparable<T> {

	public OrderedList() {}

	private readonly List<T> _list = [];
	public int Count { get { return _list.Count; }}
	public T this[int i] {
		get {
			return _list[i];
		}
		set {
			_list[i] = value;
		}
	}

	public bool Contains(T a) => _list.Contains(a);

	public void Add(T item) {
		if(Count > 0) {
			for(int i = 0; i < Count; i++) {
				if(item.CompareTo(_list[i]) > 0) {
					if(i == Count - 1) {
						_list.Add(item);
						break;
					}
					if(item.CompareTo(_list[i + 1]) < 0) {
						_list.Insert(i + 1, item);
						break;
					}
					// here: item is greater than i'th and (i + 1)'th element
					// in which case we just continue
				}
				else {
					_list.Insert(i, item);
					break;
				}
			}
		}
		else _list.Add(item);
	}

	public override bool Equals(object? a) {
		if(a is null) return this is null;
		if(a is OrderedList<T> _a) {
			if(base.Equals(_a)) return true;
			if(_a.Count != Count) return false;
			for(int i = 0; i < Count; i++) {
				if(_a[i].CompareTo(this[i]) != 0)
					return false;
			}
			return true;
		}
		return false;
	}

    public override int GetHashCode()
    {
		unchecked {
			int hash = 17;
			hash = hash * 23 + _list.GetHashCode();
			hash = hash * 23 + Count.GetHashCode();

			return hash;
		}
    }

	public static bool operator ==(OrderedList<T> a, OrderedList<T> b) => a.Equals(b);
	
	public static bool operator !=(OrderedList<T> a, OrderedList<T> b) => !a.Equals(b);

	public override string ToString() {
		string text = $"OrderedList<{typeof(T).Name}>({Count})[";
		for(int i = 0; i < Count; i++) {
			text += $"{_list[i]}";
			if(i != Count - 1) text += ", ";
		}
		text += "]";
		return text;
	}
}

/// <summary>
/// High resolution hardware clocks
/// </summary>
class Clock {
	public class Now {
		private static long freq = 0;
		public static double Time() {
			if (freq == 0)
				if(Win32.Kernel32.QueryPerformanceFrequency(out freq) == 0)
					throw new Exception($"High resolution performance counter not supported, win32: {Win32.Last()}");
			if(Win32.Kernel32.QueryPerformanceCounter(out long timeStamp) == 0)
				throw new Exception($"QueryPerformanceCounter failed, win32: {Win32.Last()}");
			return (double)timeStamp / freq;
		}
		public static long Seconds() => (long)Time();
		public static long Milli() => (long)(Time()*1000);
		public static long Micro() => (long)(Time()*1000000);
	}
}

/// <summary>
/// Extensions on built-in types go here
/// </summary>
static class Extensions {
	extension(string) {
		public static string operator *(string a, int n) {
			return string.Concat(Enumerable.Repeat(a, n));
		}
	}
}

class Win32 {
	public static int Last() => Marshal.GetLastWin32Error();

	public class Kernel32 {
		[DllImport("kernel32.dll", SetLastError = true)]
		public static extern int QueryPerformanceFrequency(out long freq);

		[DllImport("kernel32.dll", SetLastError = true)]
		public static extern int QueryPerformanceCounter(out long timeStamp);
	}
}
