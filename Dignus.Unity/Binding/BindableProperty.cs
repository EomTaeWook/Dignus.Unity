// Copyright (c) 2021 EomTaeWook
// MIT License — https://opensource.org/licenses/MIT
// Part of Dignus Library

namespace Dignus.Unity.Binding
{
    public class BindableProperty<T> : IReadOnlyBindableProperty<T>
    {
        public delegate void ValueChangedHandler(T value);
        public event ValueChangedHandler ValueChanged;

        public static implicit operator T(BindableProperty<T> bindableProperty)
        {
            return bindableProperty.Value;
        }
        public static explicit operator BindableProperty<T>(T value)
        {
            return new BindableProperty<T>(value);
        }

        private T _value;
        public T Value
        {
            get => _value;
            set
            {
                if (_value != null)
                {
                    if (_value.Equals(value))
                    {
                        return;
                    }
                }
                _value = value;
                ValueChanged?.Invoke(_value);
            }
        }

        public BindableProperty()
        {
        }
        public BindableProperty(T value)
        {
            Value = value;
        }
        public void NotifyChanged()
        {
            ValueChanged?.Invoke(_value);
        }
    }
}
