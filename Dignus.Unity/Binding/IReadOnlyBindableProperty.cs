// Copyright (c) 2021 EomTaeWook
// MIT License — https://opensource.org/licenses/MIT
// Part of Dignus Library

namespace Dignus.Unity.Binding
{
    public interface IReadOnlyBindableProperty<T>
    {
        T Value { get; }

        event BindableProperty<T>.ValueChangedHandler ValueChanged;
    }
}
