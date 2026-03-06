using System;
using System.Drawing;
using System.Drawing.Text;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;

namespace HobslideHelper
{
    public static class CustomFont
    {
        private static PrivateFontCollection _pfc = new PrivateFontCollection();

        public static void LoadAllFonts()
        {
            if (_pfc.Families.Length > 0) return;

            LoadFontFromResource("HobslideHelper.Font.NotoSansJP-Black.ttf");
            LoadFontFromResource("HobslideHelper.Font.NotoSansJP-Bold.ttf");
        }

        public static Font GetFont(string familyName, float size, FontStyle style = FontStyle.Bold)
        {
            LoadAllFonts();
            foreach (var family in _pfc.Families)
            {
                if (family.Name.Contains(familyName)) // 名前が部分一致するかチェック
                {
                    return new Font(family, size, style);
                }
            }
            // 見つからなければデフォルト
            return new Font(FontFamily.GenericSansSerif, size, style);
        }

        private static void LoadFontFromResource(string resourceName)
        {
            var assembly = Assembly.GetExecutingAssembly();
            using (Stream stream = assembly.GetManifestResourceStream(resourceName))
            {
                if (stream == null) return;
                byte[] fontData = new byte[stream.Length];
                stream.Read(fontData, 0, (int)stream.Length);
                IntPtr dataPtr = Marshal.AllocCoTaskMem(fontData.Length);
                Marshal.Copy(fontData, 0, dataPtr, fontData.Length);
                _pfc.AddMemoryFont(dataPtr, fontData.Length);
                Marshal.FreeCoTaskMem(dataPtr);
            }
        }
    }
}
