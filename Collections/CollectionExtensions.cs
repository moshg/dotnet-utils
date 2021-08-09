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

        internal class GroupChunkEnumerable<TKey, TElement> : IEnumerable<IGrouping<TKey, TElement>>
        {
            private readonly IEnumerable<TElement> _elements;
            private readonly Func<TElement, TKey> _selector;

            internal GroupChunkEnumerable(IEnumerable<TElement> elements, Func<TElement, TKey> keySelector)
            {
                _elements = elements;
                _selector = keySelector;
            }

            public IEnumerator<IGrouping<TKey, TElement>> GetEnumerator() =>
                new Enumerator(_elements.GetEnumerator(), _selector);
            IEnumerator IEnumerable.GetEnumerator() =>
                new Enumerator(_elements.GetEnumerator(), _selector);

            private class Enumerator : IEnumerator<Grouping<TKey, TElement>>
            {
                private int _state;
                private IEnumerator<TElement>? _etor;
                private Func<TElement, TKey>? _selector;
                private TKey? _key;
                private List<TElement>? _elements;
                private IEqualityComparer<TKey?>? _cmp;
                private Grouping<TKey, TElement>? _current;

                internal Enumerator(IEnumerator<TElement> etor, Func<TElement, TKey> selector)
                {
                    _state = 0;
                    _etor = etor;
                    _selector = selector;
                }

                public Grouping<TKey, TElement> Current => _current ?? throw new InvalidOperationException();

                object? IEnumerator.Current => _current;

                public bool MoveNext()
                {
                    switch (_state)
                    {
                        case 0:
                            if (!_etor!.MoveNext())
                            {
                                _state = 3;
                                break;
                            }
                            var initElem = _etor.Current;
                            _key = _selector!(initElem);
                            _elements = new List<TElement> { initElem };
                            _cmp = EqualityComparer<TKey?>.Default;
                            _state = 1;
                            goto case 1;
                        case 1:
                            if (!_etor!.MoveNext())
                            {
                                _current = new Grouping<TKey, TElement>(_key!, _elements!.ToArray());
                                _state = 2;
                                return true;
                            }
                            var elem = _etor.Current;
                            var key = _selector!(elem);
                            if (_cmp!.Equals(key, _key))
                            {
                                _elements!.Add(elem);
                                goto case 1;
                            }
                            else
                            {
                                _current = new Grouping<TKey, TElement>(_key!, _elements!.ToArray());
                                _key = key;
                                _elements.Clear();
                                _elements.Add(elem);
                                return true;
                            }
                        case 2:
                            Dispose();
                            _state = 3;
                            break;
                    }
                    return false;
                }

                public void Dispose()
                {
                    if (_etor != null)
                    {
                        _key = default;
                        _elements!.Clear();
                        _elements = null;
                        _cmp = null;
                        _current = null; 
                        _selector = null;
                        _etor.Dispose();
                        _etor = null;
                    }
                }

                public void Reset() => new NotSupportedException();
            }
        }

        public class Grouping<TKey, TElement> : IGrouping<TKey, TElement>
        {
            private readonly TKey _key;
            private readonly TElement[] _elements;

            public TKey Key => _key;

            internal Grouping(TKey key, TElement[] elements)
            {
                _key = key;
                _elements = elements;
            }

            public IEnumerator<TElement> GetEnumerator()
            {
                foreach (var element in _elements)
                {
                    yield return element;
                }
            }

            IEnumerator IEnumerable.GetEnumerator() => _elements.GetEnumerator();
        }
    }
}
