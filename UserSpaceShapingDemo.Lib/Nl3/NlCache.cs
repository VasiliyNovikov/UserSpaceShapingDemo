using System.Collections;
using System.Collections.Generic;

using UserSpaceShapingDemo.Lib.Interop;

namespace UserSpaceShapingDemo.Lib.Nl3;

internal unsafe class NlCache(LibNl3.nl_cache* cache) : NativeObject, IReadOnlyCollection<NlObject>
{
    protected override void ReleaseUnmanagedResources() => LibNl3.nl_cache_free(cache);

    public int Count => LibNl3.nl_cache_nitems(cache);

    public IEnumerator<NlObject> GetEnumerator() => new Enumerator(cache);

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    private sealed class Enumerator(LibNl3.nl_cache* cache) : IEnumerator<NlObject>
    {
        private LibNl3.nl_object* _current = null;

        public NlObject Current => new(_current);

        object IEnumerator.Current => Current;

        public bool MoveNext()
        {
            _current = _current is null
                ? LibNl3.nl_cache_get_first(cache)
                : LibNl3.nl_cache_get_next(_current);
            return _current is not null;
        }

        public void Reset() => _current = null;

        public void Dispose() => _current = null;
    }
}