// Copyright (c) 2021 EomTaeWook
// MIT License — https://opensource.org/licenses/MIT
// Part of Dignus Library

using Dignus.DependencyInjection;
using Dignus.DependencyInjection.Attributes;
using System;
using System.Reflection;

namespace Dignus.Unity.DependencyInjection
{
    public static class DignusUnityActivator
    {
        public static T Create<T>(ServiceContainer serviceContainer, params object[] runtimeArguments)
        {
            if (serviceContainer == null)
            {
                throw new ArgumentNullException(nameof(serviceContainer));
            }

            return (T)Create(serviceContainer, typeof(T), runtimeArguments);
        }

        public static object Create(ServiceContainer serviceContainer, Type targetType, params object[] runtimeArguments)
        {
            if (serviceContainer == null)
            {
                throw new ArgumentNullException(nameof(serviceContainer));
            }
            if (targetType == null)
            {
                throw new ArgumentNullException(nameof(targetType));
            }

            var args = runtimeArguments ?? Array.Empty<object>();

            if (targetType.IsInterface || targetType.IsAbstract)
            {
                if (args.Length > 0)
                {
                    throw new InvalidOperationException($"runtime arguments are not supported for interface or abstract type: {targetType.Name}");
                }

                return serviceContainer.GetService(targetType);
            }

            var constructor = SelectConstructor(targetType);
            var constructorArgs = GetConstructorArguments(serviceContainer, constructor, args);
            var instance = Activator.CreateInstance(targetType, constructorArgs);

            foreach (var property in targetType.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.SetProperty))
            {
                var injectAttr = property.GetCustomAttribute<InjectAttribute>(false);
                if (injectAttr == null)
                {
                    continue;
                }
                var propertyValue = serviceContainer.GetService(property.PropertyType);
                property.SetValue(instance, propertyValue);
            }

            return instance;
        }

        private static ConstructorInfo SelectConstructor(Type type)
        {
            var constructors = type.GetConstructors(BindingFlags.Instance | BindingFlags.Public);
            if (constructors.Length == 0)
            {
                throw new InvalidOperationException($"public constructor is required for type {type.Name}");
            }

            ConstructorInfo selectedConstructor = null;
            int injectCtorCount = 0;

            for (int i = 0; i < constructors.Length; ++i)
            {
                var constructor = constructors[i];

                if (constructor.IsDefined(typeof(InjectConstructorAttribute), false))
                {
                    if (injectCtorCount > 0)
                    {
                        throw new InvalidOperationException($"multiple constructors have been found. {type.FullName}");
                    }
                    selectedConstructor = constructor;
                    injectCtorCount++;
                }
            }

            if (injectCtorCount == 0)
            {
                if (constructors.Length > 1)
                {
                    throw new InvalidOperationException($"multiple constructors have been found. {type.Name}");
                }
                selectedConstructor = constructors[0];
            }

            return selectedConstructor;
        }

        private static object[] GetConstructorArguments(ServiceContainer serviceContainer, ConstructorInfo constructorInfo, object[] runtimeArguments)
        {
            var parameters = constructorInfo.GetParameters();
            var argumentValues = new object[parameters.Length];

            for (int i = 0; i < parameters.Length; ++i)
            {
                var parameterType = parameters[i].ParameterType;
                var matchingRuntimeArg = FindMatchingRuntimeArgument(parameterType, runtimeArguments);

                if (matchingRuntimeArg != null)
                {
                    argumentValues[i] = matchingRuntimeArg;
                }
                else
                {
                    argumentValues[i] = serviceContainer.GetService(parameterType);
                }
            }

            return argumentValues;
        }

        private static object FindMatchingRuntimeArgument(Type parameterType, object[] runtimeArguments)
        {
            object matchedArg = null;
            bool found = false;

            foreach (var arg in runtimeArguments)
            {
                if (arg == null)
                {
                    continue;
                }

                if (parameterType.IsAssignableFrom(arg.GetType()))
                {
                    if (found)
                    {
                        throw new InvalidOperationException($"multiple scene components were provided for constructor parameter type: {parameterType.Name}");
                    }

                    matchedArg = arg;
                    found = true;
                }
            }

            return matchedArg;
        }
    }
}
