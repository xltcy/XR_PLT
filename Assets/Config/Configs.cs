using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Config
{
    public static class Configs
    {
        private static readonly ResourceConfig _language = new ResourceConfig("language", "zh-CN");

        private static readonly ResourceConfig hallIntroduction = new ResourceConfig("hall", @"");
        private static readonly ResourceConfig exhibitionIntroduction = new ResourceConfig("exhibition", @"");

        public static string Language => _language.Content;

        public static string HallIntroduction => hallIntroduction.Content;
        public static string ExhibitionIntroduction => exhibitionIntroduction.Content;
        
    }
}
