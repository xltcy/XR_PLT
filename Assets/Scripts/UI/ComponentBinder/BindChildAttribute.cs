using System;
using UnityEngine;

[AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
public class BindChildAttribute : Attribute
{
    public string ChildName { get; private set; }
    public bool Required { get; private set; }
    
    public BindChildAttribute(string childName = "", bool required = false)
    {
        ChildName = childName;
        Required = required;
    }
}