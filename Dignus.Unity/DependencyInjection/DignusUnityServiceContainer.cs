// Copyright (c) 2021 EomTaeWook
// MIT License — https://opensource.org/licenses/MIT
// Part of Dignus Library

using Dignus.DependencyInjection;
using System;
using System.Reflection;

namespace Dignus.Unity.DependencyInjection
{
    public class DignusUnityServiceContainer
    {
        private static readonly ServiceContainer _serviceContainer = new ServiceContainer();

        public static ServiceContainer RegisterDependencies(Assembly assembly)
        {
            _serviceContainer.RegisterDependencies(assembly);

            return _serviceContainer;
        }

        public static T GetService<T>()
        {
            return _serviceContainer.GetService<T>();
        }

        public static T GetService<T>(params object[] args)
        {
            if (args == null || args.Length == 0)
            {
                return GetService<T>();
            }

            return DignusUnityActivator.Create<T>(_serviceContainer, args);
        }
    }
}
