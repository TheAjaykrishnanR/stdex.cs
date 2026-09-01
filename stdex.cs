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

using System;
using System.Linq;
using System.Collections.Generic;

namespace stdex;

/// <summary>
/// A container for an always ordered collection. Any element added using 
/// the Add() method will be put in their respective ordering amongs the
/// already present elements, resulting in an always ordered collection.
/// </summary>
class OrderedList<T> where T: IComparable<T> {
	public OrderedList() {}
	private readonly List<T> _list = [];
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
	public T this[int i] {
		get {
			return _list[i];
		}
		set {
			_list[i] = value;
		}
	}

	public bool Contains(T a) => _list.Contains(a);

	public static bool operator ==(OrderedList<T> a, OrderedList<T> b) {
		if(a.Count != b.Count)
			return false;
		for(int i = 0; i < a.Count; i++) {
			if(a[i].CompareTo(b[i]) != 0)
				return false;
		}
		return true;
	}
	public static bool operator !=(OrderedList<T> a, OrderedList<T> b) {
		return !(a == b);
	}
	public int Count { get { return _list.Count; }}

	public override string ToString() {
		string text = $"OrderedList<{typeof(T).Name}>(count: {Count})[";
		for(int i = 0; i < Count; i++) {
			text += $"{_list[i]}";
			if(i != Count - 1) text += ", ";
		}
		text += "]";
		return text;
	}
}

static class Extensions {
	extension(string) {
		public static string operator *(string a, int n) {
			return string.Concat(Enumerable.Repeat(a, n));
		}
	}
}
