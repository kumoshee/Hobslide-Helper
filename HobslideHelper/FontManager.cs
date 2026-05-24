using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Text;
using System.Runtime.InteropServices;

namespace HobslideHelper
{
    public static class FontManager
    {
        private static PrivateFontCollection privateFonts =
            new PrivateFontCollection();

        private static Dictionary<string, FontFamily> families =
            new Dictionary<string, FontFamily>();

        private static bool initialized = false;

        public const string Heavy = "源柔ゴシックX Heavy";
        public const string Bold = "源柔ゴシックX Bold";

        static FontManager()
        {
            Initialize();
        }

        public static void Initialize()
        {
            if (initialized)
                return;

            LoadFont(Properties.Resources.GenJyuuGothicX_Heavy);
            LoadFont(Properties.Resources.GenJyuuGothicX_Bold);

            foreach (FontFamily family in privateFonts.Families)
            {
                families[family.Name] = family;
            }

            initialized = true;
        }

        private static void LoadFont(byte[] fontData)
        {
            IntPtr fontPtr =
                Marshal.AllocCoTaskMem(fontData.Length);

            Marshal.Copy(fontData, 0, fontPtr, fontData.Length);

            privateFonts.AddMemoryFont(fontPtr, fontData.Length);

            Marshal.FreeCoTaskMem(fontPtr);
        }

        public static Font CreateFont(
            string familyName,
            float size,
            FontStyle style = FontStyle.Bold)
        {
            return new Font(
                families[familyName],
                size,
                style,
                GraphicsUnit.Point);
        }
        public static FontFamily GetFamily(string family)
        {
            return families[family];
        }
    }
}