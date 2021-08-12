using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace mosh.Collections
{
    public static class Enumerable
    {
        internal static class Error
        {
            internal static ArgumentNullException ArgumentNull(string paramName)
            {
                return new ArgumentNullException(paramName);
            }
        }

        private enum State
        {
            New = -1,
            Allocated = 0,
            Iterating = 1,
            WillDispose = 2,
            Disposed = 3
        }

        public static IEnumerable<IGrouping<TKey, TSource>> GroupChunkBy<TSource, TKey>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector)
        {
            if (source == null)
                throw Error.ArgumentNull(nameof(source));
            if (keySelector == null)
                throw Error.ArgumentNull(nameof(keySelector));
            return new GroupChunkEnumerable<TKey, TSource>(source, keySelector);
        }

        internal class GroupChunkEnumerable<TKey, TSource> : IEnumerable<IGrouping<TKey, TSource>>, IEnumerator<IGrouping<TKey, TSource>>
        {
            private readonly int _threadId;
            private State _state;
            private readonly IEnumerable<TSource> _source;
            private readonly Func<TSource, TKey> _selector;
            private IEnumerator<TSource>? _etor;
            private TKey? _key;
            private List<TSource>? _group;
            private IEqualityComparer<TKey?>? _cmp;
            private Grouping<TKey, TSource>? _current;

            internal GroupChunkEnumerable(IEnumerable<TSource> source, Func<TSource, TKey> selector)
            {
                _threadId = Environment.CurrentManagedThreadId;
                _state = State.New;
                _source = source;
                _selector = selector;
            }

            private GroupChunkEnumerable<TKey, TSource> Clone()
            {
                return new GroupChunkEnumerable<TKey, TSource>(_source, _selector);
            }

            public IEnumerator<IGrouping<TKey, TSource>> GetEnumerator()
            {
                var etor = _state == State.New && _threadId == Environment.CurrentManagedThreadId ? this : Clone();
                etor._state = State.Allocated;
                return etor;
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

            public IGrouping<TKey, TSource> Current => _current!;

            object IEnumerator.Current => _current!;

            public bool MoveNext()
            {
                switch (_state)
                {
                    case State.Allocated:
                        _etor = _source.GetEnumerator();
                        if (!_etor!.MoveNext())
                        {
                            _etor.Dispose();
                            _etor = null;
                            _state = State.Disposed;
                            break;
                        }
                        var initElem = _etor.Current;
                        _key = _selector!(initElem);
                        _group = new List<TSource> { initElem };
                        _cmp = EqualityComparer<TKey?>.Default;
                        _state = State.Iterating;
                        goto case State.Iterating;
                    case State.Iterating:
                        if (!_etor!.MoveNext())
                        {
                            _current = new Grouping<TKey, TSource>(_key!, _group!.ToArray());
                            _state = State.WillDispose;
                            return true;
                        }
                        var elem = _etor.Current;
                        var key = _selector!(elem);
                        if (_cmp!.Equals(key, _key))
                        {
                            _group!.Add(elem);
                            goto case State.Iterating;
                        }
                        else
                        {
                            _current = new Grouping<TKey, TSource>(_key!, _group!.ToArray());
                            _key = key;
                            _group.Clear();
                            _group.Add(elem);
                            return true;
                        }
                    case State.WillDispose:
                        Dispose();
                        break;
                }
                return false;
            }

            public void Dispose()
            {
                if (_etor != null)
                {
                    _state = State.Disposed;
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
