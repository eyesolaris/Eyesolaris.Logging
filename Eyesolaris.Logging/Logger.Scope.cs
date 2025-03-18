using Microsoft.Extensions.Logging;
using System.Collections;
using System.Collections.Immutable;
using System.Text;

namespace Eyesolaris.Logging
{
    public abstract partial class Logger
    {
        public abstract class Scope : IDisposable
        {
            public Scope(Logger parent)
            {
                _logger = parent;
            }

            public abstract int StateHash { get; }
            public abstract object UntypedState { get; }
            public abstract Type StateType { get; }
            public abstract string FormatState(string? formatString, IFormatProvider? formatProvider);

            public abstract IReadOnlyList<KeyValuePair<string, object?>> ParametersDict { get; }

            public void Dispose()
            {
                _logger.RemoveScopeWithLock(this);
            }

            public override string ToString()
            {
                return base.ToString()!;
            }

            private readonly Logger _logger;
        }

        protected internal class Scope<TState> : Scope
            where TState : notnull
        {
            public Scope(Logger parent, TState state)
                : base(parent)
            {
                if (typeof(TState).IsClass)
                {
                    ArgumentNullException.ThrowIfNull(state);
                }
                State = state;
                StateHash = state.GetHashCode();
                if (_isDict)
                {
                    _dictCache = _ConvertToCache((IEnumerable<KeyValuePair<string, object?>>)state, out _stringCache);
                }
                else if (_isUntypedDict)
                {
                    _dictCache = _ConvertToCache((IDictionary)state);
                }
                else
                {
                    _dictCache = ImmutableList<KeyValuePair<string, object?>>.Empty;
                }
            }

            public TState State { get; }
            public override Type StateType => _stateType;
            public override object UntypedState => State;
            public override int StateHash { get; }
            public override IReadOnlyList<KeyValuePair<string, object?>> ParametersDict
                => _dictCache;

            public override string FormatState(string? formatString, IFormatProvider? formatProvider)
            {
                if (_stringCache is not null)
                {
                    return _stringCache;
                }
                else if (State is IFormattable f)
                {
                    return f.ToString(formatString, formatProvider);
                }
                return State.ToString() ?? string.Empty;
            }

            public sealed override string ToString()
            {
                if (_stringCache is not null)
                {
                    return _stringCache;
                }
                if (_dictCache.Count > 0)
                {
                    StringBuilder sb = new();
                    foreach (var kv in _dictCache)
                    {
                        sb.Append('[');
                        sb.Append(kv.Key);
                        sb.Append("] = \"");
                        sb.Append(kv.Value?.ToString() ?? "(null)");
                        sb.Append('"');
                        return sb.ToString();
                    }
                }
                return State.ToString() ?? "(null string)";
            }

            private readonly IReadOnlyList<KeyValuePair<string, object?>> _dictCache;
            private readonly string? _stringCache;

            private static readonly Type _stateType = typeof(TState);

            private readonly bool _isDict = typeof(TState).IsAssignableTo(typeof(IEnumerable<KeyValuePair<string, object>>));
            private readonly bool _isUntypedDict = typeof(TState).IsAssignableTo(typeof(IDictionary));

            private static IReadOnlyList<KeyValuePair<string, object?>> _ConvertToCache(
                IEnumerable<KeyValuePair<string, object?>> dict,
                out string? stringCache)
            {
                stringCache = null;
                bool isCustomFormatString = false;
                IImmutableList<KeyValuePair<string, object?>> dictCache
                    = dict
                    .Where(kv =>
                    {
                        if (kv.Key == "{OriginalFormat}")
                        {
                            isCustomFormatString = true;
                            return false;
                        }
                        return true;
                    })
                    .ToImmutableList();
                if (isCustomFormatString)
                {
                    // Here, a user has called an extension method BeginScope() for a custom format string
                    stringCache = dict.ToString();
                }
                return dictCache;
            }

            private static IReadOnlyList<KeyValuePair<string, object?>> _ConvertToCache(IDictionary dict)
            {
                IReadOnlyList<KeyValuePair<string, object?>> dictCache;
                if (dict.Count > 0)
                {
                    List<KeyValuePair<string, object?>> cache = new();
                    var enumerator = dict.GetEnumerator();
                    while (enumerator.MoveNext())
                    {
                        var entry = enumerator.Entry;
                        cache.Add(
                            new KeyValuePair<string, object?>(
                                entry.Key.ToString() ?? throw new InvalidOperationException(),
                                entry.Value
                                )
                            );
                    }
                    dictCache = cache;
                }
                else
                {
                    dictCache = Array.Empty<KeyValuePair<string, object?>>();
                }
                return dictCache;
            }
        }
    }
}
