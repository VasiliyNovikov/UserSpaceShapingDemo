using UserSpaceShapingDemo.Lib.Interop;

namespace UserSpaceShapingDemo.Lib.Nl3;

internal unsafe class NlCache(LibNl3.nl_cache* cache) : NativeObject
{
    protected override void ReleaseUnmanagedResources() => LibNl3.nl_cache_free(cache);

    public Enumerator GetEnumerator() => new(cache);

    public struct Enumerator(LibNl3.nl_cache* cache)
    {
        private LibNl3.nl_object* _current = null;

        public NlObject Current => new(_current);

        public bool MoveNext()
        {
            _current = _current is null
                ? LibNl3.nl_cache_get_first(cache)
                : LibNl3.nl_cache_get_next(_current);
            return _current is not null;
        }
    }
}