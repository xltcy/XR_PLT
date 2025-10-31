using System;
using UnityEngine;
using UnityEngine.UI;

[AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
public class ButtonCallbackAttribute : Attribute
{
    public string MethodName { get; private set; }
    
    public bool Required { get; private set; }
    
    public ButtonCallbackAttribute(string methodName = "", bool required = false)
    {
        MethodName = methodName;
        Required = required;
    }
}