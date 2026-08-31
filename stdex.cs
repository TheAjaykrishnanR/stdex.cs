using System;
using System.Collections.Generic;

namespace stdex;

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
