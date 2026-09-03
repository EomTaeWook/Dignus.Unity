// Copyright (c) 2021 EomTaeWook
// MIT License — https://opensource.org/licenses/MIT
// Part of Dignus Library

namespace Dignus.Unity.Attributes
{
    public class PrefabPathAttribute : System.Attribute
    {
        public string Path { get; private set; }
        public string FileName { get; private set; }
        public PrefabPathAttribute(string path, string fileName = null)
        {
            Path = path;
            FileName = fileName;
        }
    }
}
