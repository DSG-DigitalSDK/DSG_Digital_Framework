using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DSG.Imaging
{
    /// <summary>
    /// Stores static palettes for 8bpp Indexed Bitmaps
    /// </summary>
    public static class PaletteUtility
    {
        static Dictionary<Palette, ColorPalette> oDictPalette;
        static Bitmap oTempBitmap;

        /// <summary>
        /// Constructs beautiful grayscale and thermal palettes!
        /// </summary>
        static PaletteUtility()
        {
            oTempBitmap = new Bitmap(16, 16, System.Drawing.Imaging.PixelFormat.Format8bppIndexed);
            oDictPalette = new Dictionary<Palette, ColorPalette>();
            oDictPalette.Add(Palette.Blue, CreatePalette(0, 0, 1, false));
            oDictPalette.Add(Palette.BlueInverted, CreatePalette(0, 0, 1, true));
            oDictPalette.Add(Palette.Cyan, CreatePalette(0, 1, 1, false));
            oDictPalette.Add(Palette.CyanInverted, CreatePalette(0, 1, 1, true));
            oDictPalette.Add(Palette.Gray, CreatePalette(1, 1, 1, false));
            oDictPalette.Add(Palette.GrayInverted, CreatePalette(1, 1, 1, true));
            oDictPalette.Add(Palette.Green, CreatePalette(0, 1, 0, false));
            oDictPalette.Add(Palette.GreenInverted, CreatePalette(0, 1, 0, true));
            oDictPalette.Add(Palette.Purple, CreatePalette(1, 0, 1, false));
            oDictPalette.Add(Palette.PurpleInverted, CreatePalette(1, 0, 1, true));
            oDictPalette.Add(Palette.Red, CreatePalette(1, 0, 0, false));
            oDictPalette.Add(Palette.RedInverted, CreatePalette(1, 0, 0, true));
            oDictPalette.Add(Palette.Yellow, CreatePalette(1, 1, 0, false));
            oDictPalette.Add(Palette.YellowInverted, CreatePalette(1, 1, 0, true));
            oDictPalette.Add(Palette.ThermalLava, CreatePaletteThermalLava(false));
            oDictPalette.Add(Palette.ThermalLavaInverted, CreatePaletteThermalLava(true));
            oDictPalette.Add(Palette.Rainbow, CreatePaletteRainbow(false));
            oDictPalette.Add(Palette.RainbowInverted, CreatePaletteRainbow(true));
            oDictPalette.Add(Palette.Iron, CreatePaletteIron(false));
            oDictPalette.Add(Palette.IronInverted, CreatePaletteIron(true));
        }

        static ColorPalette CreatePalette(int iMR, int iMG, int iMB, bool bInverted)
        {
            ColorPalette oPalette = oTempBitmap.Palette;
            for (int i = 0; i < 256; i++)
            {
                int j = bInverted ? 255 - i : i;
                oPalette.Entries[i] = Color.FromArgb(255, j * iMR, j * iMG, j * iMB);
            }
            return oPalette;
        }

        static ColorPalette CreatePaletteThermalLava(bool bInverted)
        {
            ColorPalette oPalette = oTempBitmap.Palette;
            for (int i = 0; i < 256; i++)
            {
                double x = ((double)i) / 255;
                var r = Math.Round(255 * Math.Sqrt(x));
                var g = Math.Round(255 * Math.Pow(x, 3));
                var b = Math.Round(255 * (Math.Sin(2 * Math.PI * x) >= 0 ?
                                   Math.Sin(2 * Math.PI * x) : 0));
                if (bInverted)
                {
                    oPalette.Entries[255 - i] = Color.FromArgb(255, (int)r, (int)g, (int)b);
                }
                else
                {
                    oPalette.Entries[i] = Color.FromArgb(255, (int)r, (int)g, (int)b);

                }
            }
            return oPalette;
        }


        static byte Fade(byte A, byte B, float FadeValue)
        {
            return (byte)Math.Min(255, Math.Max(0, (A + (float)(B - A) * FadeValue)));
        }

        static Color Fade(Color Source, Color Dest, float FadeValue)
        {
            byte R = Fade(Source.R, Dest.R, FadeValue);
            byte G = Fade(Source.G, Dest.G, FadeValue);
            byte B = Fade(Source.B, Dest.B, FadeValue);
            return Color.FromArgb(255, R, G, B);
        }

        static ColorPalette CreatePaletteRainbow(bool bInverted)
        {
            ColorPalette oPalette = oTempBitmap.Palette;
            int iSteps = 32;
            int iLoop = 256 / iSteps;
            float fFadeStep = 1f / iSteps;
            Color[] COL = new Color[] {
                     Color.FromArgb(0, 0, 0),       // Nero    0-31
                     Color.FromArgb(255, 0, 0),     // Rosso   32-63
                     Color.FromArgb(255, 255, 0),   // Giallo  64-95
                     Color.FromArgb(0, 255, 0),     // Verde   96-127
                     Color.FromArgb(0, 255, 255),   // Ciano   128-159
                     Color.FromArgb(0, 0, 255),     // Blu     160-191
                     Color.FromArgb(255, 0, 255),   // Viola   192-223
                     Color.FromArgb(128, 128, 128), // Bianco  224-255
                     Color.FromArgb(255, 255, 255), // Bianco
                    };
            int j = 0;
            for (int k = 0; k < iLoop; k++)
            {
                for (int i = 0; i < iSteps; i++, j++)
                {
                    var Color = Fade(COL[k], COL[k + 1], i * fFadeStep);
                    if (bInverted)
                    {
                        oPalette.Entries[255 - j] = Color;
                    }
                    else
                    {
                        oPalette.Entries[j] = Color;
                    }

                }
            }
            // Patch per avere il binaco/nero all'ultimo colore (altrimenti avrei grigetto...)
            if (bInverted)
            {
                oPalette.Entries[255] = Color.FromArgb(255, 0, 0, 0);
            }
            else
            {
                oPalette.Entries[255] = Color.FromArgb(255, 255, 255, 255);
            }
            return oPalette;
        }

        static ColorPalette CreatePaletteIron(bool bInverted)
        {
            // Palette selezionata da internet
            int[] S = { 0, 16, 32, 64, 96, 128, 144, 160, 176, 192, 208, 224, 240, 344, 434 };
            float fLast = (float)S[S.Length - 1];
            for (int i = 0; i < S.Length; i++)
            {
                S[i] = (int)((float)S[i]/fLast * 255.0);
            }
            byte[] R = { 0, 1, 13, 69, 124, 171, 186, 198, 209, 218, 226, 233, 240, 255, 255 };
            byte[] G = { 0, 0, 0, 0, 0, 0, 4, 13, 27, 44, 60, 77, 99, 200, 255 };
            byte[] B = { 0, 85, 118, 150, 157, 153, 148, 139, 120, 92, 42, 14, 0, 0, 255 };

            byte[] PR = new byte[256];
            byte[] PG = new byte[256];
            byte[] PB = new byte[256];

            for (int i = 0; i < S.Length - 1; i++)
            {
                int PalStart = S[i];
                int PalEnd = S[i + 1];
                float fWidth = (float)(PalEnd - PalStart);
                for (int j = PalStart; j < PalEnd; j++)
                {
                    float fStep = (float)j / fWidth;
                    PR[j] = Fade(R[i], R[i + 1], fStep);
                    PG[j] = Fade(G[i], G[i + 1], fStep);
                    PB[j] = Fade(B[i], B[i + 1], fStep);
                }
            }

            ColorPalette oPalette = oTempBitmap.Palette;

            if (bInverted)
            {
                for (int i = 0; i < 255; i++)
                {
                    oPalette.Entries[255 - i] = Color.FromArgb(255, PR[i], PB[i], PG[i]);
                }
            }
            else
            {
                for (int i = 0; i < 255; i++)
                {
                    oPalette.Entries[i] = Color.FromArgb(255, PR[i], PB[i], PG[i]);
                }
            }

            return oPalette;
        }


        /// <summary>
        /// Returns a rendered System.Drawing.Imaging.ColorPalette
        /// </summary>
        /// <param name="ePalette">Palette Model</param>
        /// <returns>ColorPalette</returns>
        public static ColorPalette GetPalette(Palette ePalette)
        {
            return (oDictPalette[ePalette]);
        }

        ///// <summary>
        ///// Converts a System.Drawing.Imaging.ColorPalette to System.Windows.Media.Imaging.BitmapPalette
        ///// </summary>
        ///// <param name="oPalette">Source System.Drawing.Imaging.ColorPalette</param>
        ///// <returns>WPF Palette or null if fails</returns>
        //public static System.Windows.Media.Imaging.BitmapPalette ToWindowsMediaPalette(ColorPalette oPalette)
        //{
        //    if (oPalette == null) return null;
        //    if (oPalette.Entries.Length == 0) return null;
        //    List<System.Windows.Media.Color> oColorList = new List<System.Windows.Media.Color>();
        //    for (int i = 0; i < oPalette.Entries.Length; i++)
        //    {
        //        oColorList.Add(System.Windows.Media.Color.FromArgb(oPalette.Entries[i].A, oPalette.Entries[i].R, oPalette.Entries[i].G, oPalette.Entries[i].B));
        //    }
        //    var oRet = new System.Windows.Media.Imaging.BitmapPalette(oColorList);
        //    return oRet;
        //}

        ///// <summary>
        ///// Converts a System.Drawing.Imaging.ColorPalette to System.Windows.Media.Imaging.BitmapPalette
        ///// </summary>System.Drawing.Bitmap Source (MUST BE 8BPP IIDEXED)</param>
        ///// <returns>WPF Palette or null if fails</returns>
        //public static System.Windows.Media.Imaging.BitmapPalette ToWindowsMediaPalette(Bitmap oImage)
        //{
        //    if (oImage == null) return null;
        //    return ToWindowsMediaPalette(oImage.Palette);
        //}



        ///// <summary>
        ///// Converts a System.Windows.Media.Imaging.BitmapPalette to System.Drawing.Imaging.ColorPalette
        ///// </summary>
        ///// <param name="oPalette">Source System.Windows.Media.Imaging.BitmapPalette</param>
        ///// <returns>System.Drawing.Imaging.ColorPalette or null if fails</returns>
        //public static ColorPalette ToSystemDrawingPalette(System.Windows.Media.Imaging.BitmapPalette oPalette)
        //{
        //    if (oPalette == null) return null;
        //    if (oPalette.Colors == null) return null;
        //    if (oPalette.Colors.Count == 0) return null;
        //    ColorPalette oPal = oTempBitmap.Palette;
        //    int loop = Math.Min(oPal.Entries.Length, oPalette.Colors.Count);
        //    for (int i = 0; i < loop; i++)
        //    {
        //        oPal.Entries[i] = Color.FromArgb(oPalette.Colors[i].A, oPalette.Colors[i].R, oPalette.Colors[i].G, oPalette.Colors[i].B);
        //    }
        //    for (int i = loop; i < oPal.Entries.Length; i++)
        //    {
        //        oPal.Entries[i] = Color.FromArgb(255, 0, 0, 0);
        //    }
        //    return oPal;
        //}

        ///// <summary>
        ///// Converts a System.Windows.Media.Imaging.BitmapPalette to System.Drawing.Imaging.ColorPalette
        ///// </summary>
        ///// <param name="oImage">Source System.Windows.Media.Imaging.BitmapImage (MUST BE 8BPP INDEXED)</param>
        ///// <returns>System.Drawing.Imaging.ColorPalette or null if fails</returns>
        //public static ColorPalette ToSystemDrawingPalette(System.Windows.Media.Imaging.BitmapImage oImage)
        //{
        //    if (oImage == null) return null;
        //    return ToSystemDrawingPalette(oImage.Palette);
        //}
    }
}
