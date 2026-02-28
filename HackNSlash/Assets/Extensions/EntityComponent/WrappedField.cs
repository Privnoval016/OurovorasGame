using System;

namespace Extensions.EntityComponent
{

    public class WrappedField<T>
    {
        private readonly Func<T> _getter;
        private readonly Action<T> _setter;

        public WrappedField(Func<T> getter, Action<T> setter = null)
        {
            _getter = getter ?? throw new ArgumentNullException(nameof(getter));
            _setter = setter;
        }

        public T Value
        {
            get => _getter();
            set
            {
                if (_setter == null)
                    throw new InvalidOperationException("This WrappedField is read-only.");
                _setter(value);
            }
        }

        public static implicit operator T(WrappedField<T> field) => field.Value;
    }
}