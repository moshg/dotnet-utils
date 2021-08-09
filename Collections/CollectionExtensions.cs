using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace mosh.Collections
{
    public static class CollectionExtensions
    {
        public static TValue GetValueOrDefault<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key, Func<TValue> valueProducer)
        {
            if (!dictionary.TryGetValue(key, out TValue? value))
                value = valueProducer();
            return value;
        }

        public static TValue GetValueOrAdd<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key, TValue value)
        {
            if (dictionary.TryGetValue(key, out TValue? gotValue))
                return gotValue;
            else
            {
                dictionary[key] = value;
                return value;
            }
        }

        public static TValue GetValueOrAdd<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key, Func<TValue> valueProducer)
        {
            if (!dictionary.TryGetValue(key, out TValue? value))
            {
                value = valueProducer();
                dictionary[key] = value;
            }
            return value;
        }

        public static IEnumerable<IGrouping<TKey, TSource>> GroupChunkBy<TSource, TKey>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector)
            => new GroupChunkEnumerable<TKey, TSource>(source, keySelector);

        internal class GroupChunkEnumerable<TKey, TElement> : IEnumerable<IGrouping<TKey, TElement>>, IEnumerator<IGrouping<TKey, TElement>>
        {
            private readonly int _threadId;
            private int _state;
            private readonly IEnumerable<TElement> _elements;
            private readonly Func<TElement, TKey> _selector;
            private IEnumerator<TElement>? _etor;
            private TKey? _key;
            private List<TElement>? _group;
            private IEqualityComparer<TKey?>? _cmp;
            private Grouping<TKey, TElement>? _current;

            internal GroupChunkEnumerable(IEnumerable<TElement> elements, Func<TElement, TKey> selector)
            {
                _threadId = Environment.CurrentManagedThreadId;
                _state = -1;
                _elements = elements;
                _selector = selector;
            }

            private GroupChunkEnumerable<TKey, TElement> Clone()
            {
                return new GroupChunkEnumerable<TKey, TElement>(_elements, _selector);
            }

            public IEnumerator<IGrouping<TKey, TElement>> GetEnumerator()
            {
                var etor = _state == -1 && _threadId == Environment.CurrentManagedThreadId ? this : Clone();
                etor._state = 0;
                return etor;
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

            public IGrouping<TKey, TElement> Current => _current!;

            object IEnumerator.Current => _current!;

            public bool MoveNext()
            {
                switch (_state)
                {
                    case 0:
                        _etor = _elements.GetEnumerator();
                        if (!_etor!.MoveNext())
                        {
                            _state = 3;
                            break;
                        }
                        var initElem = _etor.Current;
                        _key = _selector!(initElem);
                        _group = new List<TElement> { initElem };
                        _cmp = EqualityComparer<TKey?>.Default;
                        _state = 1;
                        goto case 1;
                    case 1:
                        if (!_etor!.MoveNext())
                        {
                            _current = new Grouping<TKey, TElement>(_key!, _group!.ToArray());
                            _state = 2;
                            return true;
                        }
                        var elem = _etor.Current;
                        var key = _selector!(elem);
                        if (_cmp!.Equals(key, _key))
                        {
                            _group!.Add(elem);
                            goto case 1;
                        }
                        else
                        {
                            _current = new Grouping<TKey, TElement>(_key!, _group!.ToArray());
                            _key = key;
                            _group.Clear();
                            _group.Add(elem);
                            return true;
                        }
                    case 2:
                        Dispose();
                        break;
                }
                return false;
            }

            public void Dispose()
            {
                if (_etor != null)
                {
                    _state = 3;
                    _key = default;
                    _current = null;
                    _cmp = null;
                    _group!.Clear();
                    _group = null;
                    _etor.Dispose();
                    _etor = null;
                }
            }

            public void Reset()
            {
                throw new NotImplementedException();
            }
        }

        internal class Grouping<TKey, TElement> : IGrouping<TKey, TElement>, IEnumerator<TElement>
        {
            private readonly int _threadId;
            private readonly TKey _key;
            private readonly TElement[] _elements;
            private int _index;
            private TElement? _current;

            public TKey Key => _key;

            internal Grouping(TKey key, TElement[] elements)
            {
                _threadId = Environment.CurrentManagedThreadId;
                _key = key;
                _elements = elements;
                _index = -2;
            }

            private Grouping<TKey, TElement> Clone() => new(_key, _elements);

            public IEnumerator<TElement> GetEnumerator()
            {
                var etor = _index == -1 && _threadId == Environment.CurrentManagedThreadId ? this : Clone();
                etor._index = 0;
                return etor;
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

            public TElement Current => _current!;

            object IEnumerator.Current => _current!;

            public bool MoveNext()
            {
                if (_index < _elements.Length)
                {
                    _current = _elements[_index++];
                    return true;
                }
                else
                    return false;
            }

            public void Dispose()
            {
                _index = _elements.Length;
                _current = default;
            }

            public void Reset()
            {
                throw new NotImplementedException();
            }
        }
    }
}
