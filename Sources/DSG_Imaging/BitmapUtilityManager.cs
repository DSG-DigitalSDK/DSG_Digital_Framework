using DSG.Log;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;


namespace DSG.Imaging
{
    /// <summary>
    /// Wrapper class to handle Bitmap GDI and RAW data images.
    /// <para>On GDI conversion automaticallt align even a Width RAW chunky frame to a multiple of 4 pixel</para>
    /// <para>16 bit grayscale support</para>
    /// <para>Obtains image file list similar to Halcon ListImageFiles function</para>
    /// </summary>
    public static class BitmapUtility
    {
        static readonly string sC = nameof(BitmapUtility);

        static List<string> lDefFileExtension = new List<string> { "*.jpg", "*.jpeg", "*.tif*", "*.png", "*.bmp" };

        /// <summary>
        /// Frame grabber status
        /// </summary>
        [Serializable, Obfuscation(Exclude = true)]
        public enum EnumFileSortMode
        {
            None = 0,
            Name,
            CreationTime,
            LastWriteTime
        }


        #region Import Functions

        [DllImport("kernel32.dll", EntryPoint = "CopyMemory", SetLastError = false)]
        public static extern void CopyMemory(IntPtr dest, IntPtr src, uint count);

        #endregion

        #region Properties

        static int iJpegDefaultQuality = 85;

        /// <summary>
        /// JPG Default Compression quality [0..100]
        /// </summary>
        public static int JpegDefaultQuality
        {
            get
            {
                return iJpegDefaultQuality;
            }
            set
            {
                iJpegDefaultQuality = Math.Max(1, Math.Min(100, iJpegDefaultQuality));
            }
        }

        #endregion

        #region Create from dimensions and pointer 

        /// <summary>
        /// Clones a System.Drawing.Bitmap.
        /// Fixes Width GDI+ trouble when using third parts image processing library.
        /// </summary>
        /// <param name="iWidth"></param>
        /// <param name="iHeight"></param>
        /// <param name="ePixelFormat"></param>
        /// <param name="ePalette"></param>
        /// <returns></returns>
        public static Bitmap Create(int iWidth, int iHeight, PixelFormat ePixelFormat, Palette ePalette)
        {
            int iCorrectWidth = iWidth;
            // Patch GDI+ trouble
            if (ePixelFormat == PixelFormat.Indexed)
            {
                ePixelFormat = PixelFormat.Format8bppIndexed;
            }
            if (ePixelFormat == PixelFormat.Indexed || ePixelFormat == PixelFormat.Format8bppIndexed || ePixelFormat == PixelFormat.Format16bppGrayScale)
            {
                if ((iWidth & 0x0003) != 0)
                {
                    iCorrectWidth = (iWidth + 7) & (~0x0003);
                }
            }
            Bitmap oBmp = new Bitmap(iCorrectWidth, iHeight, ePixelFormat);
            if (oBmp.PixelFormat == PixelFormat.Format8bppIndexed)
            {
                oBmp.Palette = PaletteUtility.GetPalette(ePalette);
            }
            return oBmp;
        }

        /// <summary>
        /// Creates a standard System.Drawing.Bitmap.
        /// </summary>
        /// <param name="iWidth"></param>
        /// <param name="iHeight"></param>
        /// <param name="ePixelFormat"></param>
        /// <returns></returns>
        public static Bitmap Create(int iWidth, int iHeight, PixelFormat ePixelFormat)
        {
            return Create(iWidth, iHeight, ePixelFormat, Palette.Gray);
        }

        /// <summary>
        /// Creates a standard System.Drawing.Bitmap.
        /// Pointed data is copied so the bitmap is indipendent.
        /// Fixes Width GDI+ trouble when using third parts image processing library.  
        /// </summary>
        /// <param name="oPointer">Puntatore ai dati RAW</param>
        /// <param name="iWidth">Ampiezza immagine RAW [pixel]</param>
        /// <param name="iHeight">Altezza immagine RAW [pixel]</param>
        /// <param name="iStride">Offset in byte riga successiva</param>
        /// <param name="eFormat">Formato pixel immagine raw</param>
        /// <param name="ePalette">Palette di riferimento ne caso l'immagine fosse 8-bit indexed</param>
        /// <returns>System.Drawing.Bitmap se creata, null altrimenti</returns>
        unsafe static Bitmap Create(IntPtr oPointer, int iWidth, int iHeight, int iStride, PixelFormat ePixelFormat, Palette ePalette)
        {
            string sM = nameof(Create)+"(Pointer)";
            //return null;
            if (oPointer == IntPtr.Zero)
            {
                return null;
            }
            if (ePixelFormat == PixelFormat.Undefined)
            {
                return null;
            }

            try
            {
                Bitmap bmp = Create(iWidth, iHeight, ePixelFormat, ePalette);
                BitmapData bmpdata = bmp.LockBits(new Rectangle(0, 0, iWidth, iHeight), ImageLockMode.WriteOnly, ePixelFormat);
                if (bmpdata != null)
                {
                    int StrideSRC = iStride;
                    int StrideDST = bmpdata.Stride;
                    int SizeSRC = StrideSRC * iHeight;
                    int SizeDST = StrideDST * iHeight;
                    IntPtr ptrSRC = oPointer;
                    IntPtr ptrDST = bmpdata.Scan0;
                    if (SizeSRC == SizeDST)
                    {
                        CopyMemory(ptrDST, ptrSRC, (uint)SizeSRC);
                    }
                    else
                    {
                        for (int y = 0; y < bmpdata.Height; y++)
                        {
                            int YOffsetSRC = y * StrideSRC;
                            int YOffsetDST = y * StrideDST;
                            CopyMemory(ptrDST + YOffsetDST, ptrSRC + YOffsetSRC, (uint)StrideSRC);
                        }
                    }
                    // ARGB Patch, or image will be black!
                    if (ePixelFormat == PixelFormat.Format32bppArgb)
                    {
                        Byte* ptr = (Byte*)bmpdata.Scan0.ToPointer();
                        for (int i = 3; i < SizeSRC; i += 4)
                        {
                            ptr[i] = 255;
                        }
                    }
                    if (ePixelFormat == PixelFormat.Format8bppIndexed)
                    {
                        bmp.Palette = PaletteUtility.GetPalette(ePalette);
                    }
                    bmp.UnlockBits(bmpdata);
                    return bmp;
                }
                else
                {
                    bmp.Dispose();
                    return null;
                }
            }
            catch (Exception e)
            {
                LogMan.Exception(sC,sM, e);
            }
            return null;
        }

        /// <summary>
        /// Creates a standard System.Drawing.Bitmap.
        /// Pointed data is copied so the bitmap is indipendent.
        /// Fixes Width GDI+ trouble when using third parts image processing library.
        /// Indexed images are treated as grayscaled.
        /// </summary>
        /// <param name="oPointer">Puntatore ai dati RAW</param>
        /// <param name="iWidth">Ampiezza immagine RAW [pixel]</param>
        /// <param name="iHeight">Altezza immagine RAW [pixel]</param>
        /// <param name="iStride">Ampiezza di una giga in bytes[pixel]</param>
        /// <param name="format">Formato pixel immagine raw</param>
        /// <returns>System.Drawing.Bitmap se creata, null altrimenti</returns>
        static public Bitmap Create(IntPtr oPointer, int iWidth, int iHeight, int iStride, PixelFormat ePixelFormat)
        {
            return Create(oPointer, iWidth, iHeight, iStride, ePixelFormat, Palette.Gray);
        }

        /// <summary>
        /// Creates a standard System.Drawing.Bitmap with different ouput Pixelformat.
        /// Pointed data is copied so the bitmap is indipendent.
        /// Fixes Width GDI+ trouble when using third parts image processing library. 
        /// </summary>
        /// <param name="oPointer"></param>
        /// <param name="iWidth"></param>
        /// <param name="iHeight"></param>
        /// <param name="iStride"></param>
        /// <param name="ePixelFormatSrc"></param>
        /// <param name="ePixelFormatDst"></param>
        /// <param name="e8BitPaletteDest"></param>
        /// <returns></returns>
        unsafe public static Bitmap Create(IntPtr oPointer, int iWidth, int iHeight, int iStride, PixelFormat ePixelFormatSrc, PixelFormat ePixelFormatDst, Palette ePaletteDest)
        {
            string sM = nameof(Create) + "(Pointer)";

            //return null;
            if (oPointer == IntPtr.Zero)
            {
                return null;
            }
            if (ePixelFormatSrc == PixelFormat.Undefined || ePixelFormatDst == PixelFormat.Undefined)
            {
                return null;
            }
            if (ePixelFormatSrc == ePixelFormatDst)
            {
                return Create(oPointer, iWidth, iHeight, iStride, ePixelFormatSrc, ePaletteDest);
            }
            else
            {
                ColorPalette oPalette = PaletteUtility.GetPalette(ePaletteDest);
                Bitmap oBmpDst = Create(iWidth, iHeight, ePixelFormatDst);
                if (ePixelFormatDst == PixelFormat.Format8bppIndexed)
                {
                    oBmpDst.Palette = oPalette;
                }

                // Indexed modes and grayscale are hard stuff in GDI+.
                // Not possible to convert directly using GrateGraphics and Graphics.FromImage without causing Exception.
                // Needs a low-level conversion...
                if (ePixelFormatSrc == PixelFormat.Format8bppIndexed || ePixelFormatDst == PixelFormat.Format8bppIndexed ||
                    ePixelFormatSrc == PixelFormat.Format16bppGrayScale || ePixelFormatDst == PixelFormat.Format16bppGrayScale
                    )
                {
                    BitmapData oBmpData = oBmpDst.LockBits(new Rectangle(0, 0, iWidth, iHeight), ImageLockMode.WriteOnly, ePixelFormatDst);
                    if (oBmpData == null)
                    {
                        oBmpDst.Dispose();
                        return null;
                    }
                    int StrideSRC = iStride;
                    int StrideDST = oBmpData.Stride;
                    int SizeSRC = StrideSRC * iHeight;
                    int SizeDST = StrideDST * iHeight;
                    IntPtr oIntPtrSRC = oPointer;
                    IntPtr oIntPtrDST = oBmpData.Scan0;
                    bool bConverted = false;
                    for (int y = 0; y < iHeight; y++)
                    {
                        Byte* ptrSrc = (Byte*)oIntPtrSRC.ToPointer() + y * StrideSRC;
                        Byte* ptrDst = (Byte*)oIntPtrDST.ToPointer() + y * StrideDST;
                        int PixelToCopy = iWidth;

                        //---------------------------------------------------------------------------------------------------------------------------------
                        // Paletted modes conversion
                        //---------------------------------------------------------------------------------------------------------------------------------

                        //----------------------------------------------------------------------------
                        // Gray 8 to Gray16
                        if (ePixelFormatSrc == PixelFormat.Format8bppIndexed && ePixelFormatDst == PixelFormat.Format16bppGrayScale)
                        {
                            bConverted = true;
                            for (int i = 0, j = 0; i < PixelToCopy; i++)
                            {
                                ptrDst[j++] = 0;
                                ptrDst[j++] = ptrSrc[i];
                            }
                        }
                        //----------------------------------------------------------------------------
                        // Gray 8 to Color RGB24
                        if (ePixelFormatSrc == PixelFormat.Format8bppIndexed && ePixelFormatDst == PixelFormat.Format24bppRgb)
                        {
                            for (int i = 0, j = 0; i < PixelToCopy; i++)
                            {
                                ptrDst[j++] = oPalette.Entries[ptrSrc[i]].B; //B
                                ptrDst[j++] = oPalette.Entries[ptrSrc[i]].G; //G
                                ptrDst[j++] = oPalette.Entries[ptrSrc[i]].R; //R
                            }
                            bConverted = true;
                        }
                        //----------------------------------------------------------------------------
                        // Gray 8 to Color RGB32
                        if (ePixelFormatSrc == PixelFormat.Format8bppIndexed && ePixelFormatDst == PixelFormat.Format32bppRgb)
                        {
                            for (int i = 0, j = 0; i < PixelToCopy; i++)
                            {
                                ptrDst[j++] = oPalette.Entries[ptrSrc[i]].B; //B
                                ptrDst[j++] = oPalette.Entries[ptrSrc[i]].G; //G
                                ptrDst[j++] = oPalette.Entries[ptrSrc[i]].R; //R
                                ptrDst[j++] = 0;      //A
                            }
                            bConverted = true;
                        }
                        //----------------------------------------------------------------------------
                        // Gray 8 to Color ARGB32
                        if (ePixelFormatSrc == PixelFormat.Format8bppIndexed && ePixelFormatDst == PixelFormat.Format32bppArgb)
                        {
                            for (int i = 0, j = 0; i < PixelToCopy; i++)
                            {
                                ptrDst[j++] = oPalette.Entries[ptrSrc[i]].B; //B
                                ptrDst[j++] = oPalette.Entries[ptrSrc[i]].G; //G
                                ptrDst[j++] = oPalette.Entries[ptrSrc[i]].R; //R
                                ptrDst[j++] = 255;      //A
                            }
                            bConverted = true;
                        }
                        //----------------------------------------------------------------------------
                        // RGB24 to Gray 8
                        if (ePixelFormatSrc == PixelFormat.Format24bppRgb && ePixelFormatDst == PixelFormat.Format8bppIndexed)
                        {
                            for (int i = 0, j = 0; i < PixelToCopy; i++, j += 3)
                            {
                                int val = ptrSrc[j] + ptrSrc[j + 1] + ptrSrc[j + 2];
                                val /= 3;
                                ptrDst[i] = (byte)val;
                            }
                            bConverted = true;
                        }
                        //----------------------------------------------------------------------------
                        // RGB32 to Gray 8
                        if (ePixelFormatSrc == PixelFormat.Format32bppRgb && ePixelFormatDst == PixelFormat.Format8bppIndexed)
                        {
                            for (int i = 0, j = 0; i < PixelToCopy; i++, j += 4)
                            {
                                int val = ptrSrc[j] + ptrSrc[j + 1] + ptrSrc[j + 2];
                                val /= 3;
                                ptrDst[i] = (byte)val;
                            }
                            bConverted = true;
                        }
                        //----------------------------------------------------------------------------
                        // ARGB to Gray 8
                        if (ePixelFormatSrc == PixelFormat.Format32bppArgb && ePixelFormatDst == PixelFormat.Format8bppIndexed)
                        {
                            for (int i = 0, j = 0; i < PixelToCopy; i++, j += 4)
                            {
                                int val = ptrSrc[j] + ptrSrc[j + 1] + ptrSrc[j + 2];
                                val /= 3;
                                ptrDst[i] = (byte)val;
                            }
                            bConverted = true;
                        }

                        //---------------------------------------------------------------------------------------------------------------------------------
                        // 16 bit Grayscale Conversion
                        //---------------------------------------------------------------------------------------------------------------------------------

                        //----------------------------------------------------------------------------
                        // Gray 16 to Gray8
                        if (ePixelFormatSrc == PixelFormat.Format16bppGrayScale && ePixelFormatDst == PixelFormat.Format8bppIndexed)
                        {
                            for (int i = 0, j = 1; i < PixelToCopy; i++, j += 2)
                            {
                                ptrDst[i] = ptrSrc[j];
                            }
                            bConverted = true;
                        }
                        //----------------------------------------------------------------------------
                        // Gray 16 to RGB24
                        if (ePixelFormatSrc == PixelFormat.Format16bppGrayScale && ePixelFormatDst == PixelFormat.Format24bppRgb)
                        {
                            for (int i = 0, j = 1; i < PixelToCopy; i++, j += 2)
                            {
                                ptrDst[4 * i] = ptrSrc[j];
                                ptrDst[4 * i + 1] = ptrSrc[j];
                                ptrDst[4 * i + 2] = ptrSrc[j];
                                ptrDst[4 * i + 3] = 255;
                            }
                            bConverted = true;
                        }
                        //----------------------------------------------------------------------------
                        // Gray 16 to RGB32
                        if (ePixelFormatSrc == PixelFormat.Format16bppGrayScale && ePixelFormatDst == PixelFormat.Format24bppRgb)
                        {
                            for (int i = 0, j = 1; i < PixelToCopy; i++, j += 2)
                            {
                                ptrDst[4 * i] = ptrSrc[j];
                                ptrDst[4 * i + 1] = ptrSrc[j];
                                ptrDst[4 * i + 2] = ptrSrc[j];
                                ptrDst[4 * i + 3] = 0;
                            }
                            bConverted = true;
                        }
                        //----------------------------------------------------------------------------
                        // Gray 16 to ARGB
                        if (ePixelFormatSrc == PixelFormat.Format16bppGrayScale && ePixelFormatDst == PixelFormat.Format32bppArgb)
                        {
                            for (int i = 0, j = 1; i < PixelToCopy; i++, j += 2)
                            {
                                ptrDst[4 * i] = ptrSrc[j];
                                ptrDst[4 * i + 1] = ptrSrc[j];
                                ptrDst[4 * i + 2] = ptrSrc[j];
                                ptrDst[4 * i + 3] = 255;
                            }
                            bConverted = true;
                        }
                        //----------------------------------------------------------------------------
                        // RGB24 to Gray 16
                        if (ePixelFormatSrc == PixelFormat.Format24bppRgb && ePixelFormatDst == PixelFormat.Format16bppGrayScale)
                        {
                            for (int i = 0, j = 0; i < PixelToCopy; i++, j += 3)
                            {
                                int val = ptrSrc[j] + ptrSrc[j + 1] + ptrSrc[j + 2];
                                val /= 3;
                                ptrDst[2 * i] = 0;
                                ptrDst[2 * i + 1] = (byte)val;
                            }
                            bConverted = true;
                        }
                        //----------------------------------------------------------------------------
                        // RGB32 to Gray 16
                        if (ePixelFormatSrc == PixelFormat.Format32bppRgb && ePixelFormatDst == PixelFormat.Format16bppGrayScale)
                        {
                            for (int i = 0, j = 0; i < PixelToCopy; i++, j += 4)
                            {
                                int val = ptrSrc[j] + ptrSrc[j + 1] + ptrSrc[j + 2];
                                val /= 3;
                                ptrDst[2 * i] = 0;
                                ptrDst[2 * i + 1] = (byte)val;
                            }
                            bConverted = true;
                        }
                        //----------------------------------------------------------------------------
                        // ARGB to Gray 16
                        if (ePixelFormatSrc == PixelFormat.Format32bppArgb && ePixelFormatDst == PixelFormat.Format16bppGrayScale)
                        {
                            for (int i = 0, j = 0; i < PixelToCopy; i++, j += 4)
                            {
                                int val = ptrSrc[j] + ptrSrc[j + 1] + ptrSrc[j + 2];
                                val /= 3;
                                ptrDst[2 * i] = 0;
                                ptrDst[2 * i + 1] = (byte)val;
                            }
                            bConverted = true;
                        }
                    }
                    oBmpDst.UnlockBits(oBmpData);
                    if (!bConverted)
                    {
                        oBmpDst.Dispose();
                        return null;
                    }
                }
                else
                {
                    // Color modes in GDI+ asre easy to convert...
                    try
                    {
                        using (Bitmap oBmpSrc = new Bitmap(iWidth, iHeight, iStride, ePixelFormatSrc, oPointer))
                        {
                            using (Graphics gDst = Graphics.FromImage(oBmpDst))
                            {
                                gDst.DrawImage(oBmpSrc, 0, 0);
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        LogMan.Exception(sC,sM, e);
                        return null;
                    }
                }

                return oBmpDst;
            }
        }

   
        /// Creates a standard System.Drawing.Bitmap with different ouput Pixelformat.
        /// Pointed data is copied so the bitmap is indipendent.
        /// Fixes Width GDI+ trouble when using third parts image processing library.
        /// Output indexed images are treated as grayscaled.
        /// <param name="oPointer"></param>
        /// <param name="iWidth"></param>
        /// <param name="iHeight"></param>
        /// <param name="iStride"></param>
        /// <param name="ePixelFormatSrc"></param>
        /// <param name="ePixelFormatDst"></param>
        /// <returns></returns>
        public static Bitmap Create(IntPtr oPointer, int iWidth, int iHeight, int iStride, PixelFormat ePixelFormatSrc, PixelFormat ePixelFormatDst)
        {
            return Create(oPointer, iWidth, iHeight, iStride, ePixelFormatSrc, ePixelFormatDst, Palette.Gray);
        }


        #endregion

        #region Create from GDI+ Bitmap



        /// <summary>
        /// Clones a source System.Drawing.Bitmap with PixelFormat Conversion.
        /// </summary>
        /// <param name="iWidth"></param>
        /// <param name="iHeight"></param>
        /// <param name="ePixelFormat"></param>
        /// <returns></returns>
        public static Bitmap Create(Bitmap oBitmapSrc, PixelFormat ePixelFormatDst, Palette ePaletteDest)
        {
            string sM = nameof(Create) + "(Bitmap)";

            if (oBitmapSrc == null)
            {
                return null;
            }
            lock (oBitmapSrc)
            {
                try
                {
                    var oData = oBitmapSrc.LockBits(new Rectangle(0, 0, oBitmapSrc.Width, oBitmapSrc.Height), ImageLockMode.ReadOnly, oBitmapSrc.PixelFormat);
                    if (oData != null)
                    {
                        try
                        {
                            var oBmpDest = Create(oData.Scan0, oData.Width, oData.Height, oData.Stride, oData.PixelFormat, ePixelFormatDst, ePaletteDest);
                            oBitmapSrc.UnlockBits(oData);
                            return oBmpDest;
                        }
                        catch (Exception e)
                        {
                            LogMan.Exception(sC,sM, e);
                            oBitmapSrc.UnlockBits(oData);
                            return null;
                        }
                    }
                    return null;
                }
                catch (Exception e)
                {
                    LogMan.Exception(sC, sM, e);
                    return null;
                }
            }
        }

        /// <summary>
        /// Clones a source System.Drawing.Bitmap with PixelFormat Conversion.
        /// Output indexed bitmaps are treated as grayscaled.
        /// </summary>
        /// <param name="oBitmapSrc"></param>
        /// <param name="ePixelFormatDst"></param>
        /// <returns></returns>
        public static Bitmap Create(Bitmap oBitmapSrc, PixelFormat ePixelFormatDst)
        {
            return Create(oBitmapSrc, ePixelFormatDst, Palette.Gray);
        }

        /// <summary>
        /// Clones a ROI from System.Drawing.Bitmap with PixelFormat Conversion.
        /// </summary>
        /// <param name="oBmpSrc"></param>
        /// <param name="oRoi"></param>
        /// <param name="ePixelFormatDest"></param>
        /// <returns></returns>
        public static Bitmap Create(Bitmap oBmpSrc, Rectangle oRoi, PixelFormat ePixelFormatDest)
        {
            string sM = nameof(Create) + "(Bitmap)";

            if (oBmpSrc == null)
            {
                return null;
            }
            Rectangle oRoiFull = new Rectangle(0, 0, oBmpSrc.Width, oBmpSrc.Height);
            var oRect = Rectangle.Intersect(oRoi, oRoiFull);
            if (oRect.IsEmpty)
            {
                return null;
            }
            try
            {
                // BUG on GDI+; se immagine ha una width dispari il clone SCOPPIA con OUT OF MEMORY...
                //if (oBmpSrc.Width % 2 == 0)
                //{
                //    // SAFE
                //    using (var oTmpBmp = oBmpSrc.Clone(oRect, ePixelFormatDest))
                //    {
                //        oTmpBmp.Save("C:\\TestX\\Pippo.bmp");
                //        return Create(oTmpBmp, oTmpBmp.PixelFormat);
                //    }
                //}
                //else
                //{
                    using (var oBmpTmp = new Bitmap(oBmpSrc.Width - 1, oBmpSrc.Height, oBmpSrc.PixelFormat))
                    {
                        var oDataSrc = oBmpSrc.LockBits(new Rectangle(0, 0, oBmpSrc.Width, oBmpSrc.Height), ImageLockMode.ReadOnly, oBmpSrc.PixelFormat);
                        var oDataTmp = oBmpTmp.LockBits(new Rectangle(0, 0, oBmpTmp.Width, oBmpTmp.Height), ImageLockMode.WriteOnly, oBmpSrc.PixelFormat);
                        BitBlit(oDataTmp.Scan0, 0, oDataSrc.Scan0, oDataSrc.Stride - oDataTmp.Stride, oDataTmp.Height, oDataTmp.Stride);
                        oBmpTmp.UnlockBits(oDataTmp);
                        oBmpSrc.UnlockBits(oDataSrc);
                        return Create(oBmpTmp, ePixelFormatDest);
                    }
               // }
            }
            catch (Exception e)
            {
                LogMan.Exception(sC, sM, e);
                return null;
            }
        }

        /// <summary>
        /// Clones a ROI from source System.Drawing.Bitmap.
        /// </summary>
        /// <param name="oBmpSrc"></param>
        /// <param name="oRoi"></param>
        /// <param name="ePixelFormatDest"></param>
        /// <returns></returns>
        public static Bitmap Create(Bitmap oBmpSrc, Rectangle oRoi)
        {
            if (oBmpSrc == null)
            {
                return null;
            }
            return Create(oBmpSrc, oRoi, oBmpSrc.PixelFormat);
        }

        /// <summary>
        /// Clones a ROI from source System.Drawing.Bitmap with PixelFormat Conversion.
        /// </summary>
        /// <param name="oBitmapSrc"></param>
        /// <param name="iX"></param>
        /// <param name="iY"></param>
        /// <param name="iWidth"></param>
        /// <param name="iHeight"></param>
        /// <param name="ePixelFormatDst"></param>
        /// <returns></returns>
        public static Bitmap Create(Bitmap oBitmapSrc, int iX, int iY, int iWidth, int iHeight, PixelFormat ePixelFormatDst)
        {
            return Create(oBitmapSrc, new Rectangle(iX, iY, iWidth, iHeight), ePixelFormatDst);
        }

        /// <summary>
        /// Clones a ROI from source System.Drawing.Bitmap.
        /// </summary>
        /// <param name="oBitmapSrc"></param>
        /// <param name="iX"></param>
        /// <param name="iY"></param>
        /// <param name="iWidth"></param>
        /// <param name="iHeight"></param>
        /// <returns></returns>
        public static Bitmap Create(Bitmap oBitmapSrc, int iX, int iY, int iWidth, int iHeignt)
        {
            return Create(oBitmapSrc, new Rectangle(iX, iY, iWidth, iHeignt));
        }

        /// <summary>
        /// Clones a source System.Drawing.Bitmap.
        /// </summary>
        /// <param name="iWidth"></param>
        /// <param name="iHeight"></param>
        /// <param name="ePixelFormat"></param>
        /// <returns></returns>
        public static Bitmap Create(Bitmap oBmpSource)
        {
            string sM = nameof(Create) + "(Bitmap)";

            if (oBmpSource == null)
            {
                return null;
            }
            try
            {
                var oRet = Create(oBmpSource, oBmpSource.PixelFormat);
                return oRet;
            }
            catch (Exception e)
            {
                LogMan.Exception(sC, sM, e);
                return null;
            }
        }

        #endregion

        #region Create and store from disk 

       

        /// <summary>
        /// Loads System.Drawing.Bitmap from file
        /// </summary>
        /// <param name="sFileName"></param>
        /// <returns></returns>
        public static Bitmap Create(string sFileName)
        {
            string sM = nameof(Create) + "(String)";
            try
            {
                return Bitmap.FromFile(sFileName) as Bitmap;
            }
            catch (Exception e)
            {
                LogMan.Exception(sC, sM, e);  
                return null;
            }
        }

        /// <summary>
        /// Loads System.Drawing.Bitmap from stream
        /// </summary>
        /// <param name="oStream"></param>
        /// <returns></returns>
        public static Bitmap Create(Stream oStream)
        {

            string sM = nameof(Create) + "(Stream)";
            if (oStream == null)
                return null;
            try
            {
                return Bitmap.FromStream(oStream) as Bitmap;
            }
            catch (Exception e)
            {
                LogMan.Exception(sC, sM, e);  
                return null;
            }
        }

        /// <summary>
        /// Creates a System.Drawing.Bitmap contained in a .zip source.
        /// </summary>
        /// <param name="sZippedSource">Source .zip file</param>
        /// <param name="sFileName">Bitmap name in the source file</param>
        /// <returns></returns>
        public static Bitmap Create(string sZippedSource, string sFileName)
        {
            string sM = nameof(Create) + "(Zip)";
            try
            {
                if (string.IsNullOrEmpty(sZippedSource))
                    return null;
                if (string.IsNullOrEmpty(sFileName))
                    return null;
                using (var zip = ZipFile.OpenRead(sZippedSource))
                {
                    var oZipEntry = zip.GetEntry(sFileName);
                    using (var oStream = oZipEntry.Open())
                    {
                        using (MemoryStream oMemstream = new MemoryStream())
                        {
                            oStream.CopyTo(oMemstream);
                            Bitmap oBmp = Bitmap.FromStream(oMemstream) as Bitmap;
                            return oBmp;
                        }
                    }
                }
            }
            catch (Exception e)
            {
                LogMan.Exception(sC,sM, e); 
                return null;
            }
        }

        /// <summary>
        /// Loads System.Drawing.Bitmap with PixelFormat Conversion
        /// </summary>
        /// <param name="sFileName"></param>
        /// <param name="ePixelFormatDst"></param>
        /// <returns></returns>
        public static Bitmap Create(string sFileName, PixelFormat ePixelFormatDst)
        {
            string sM = nameof(Create) + "(String)";
            try
            {
                var oBmp = Create(sFileName);
                if (oBmp == null)
                {
                    return null;
                }
                if (oBmp.PixelFormat == ePixelFormatDst)
                {
                    return oBmp;
                }

                var oBmpDst = Create(oBmp, ePixelFormatDst);
                oBmp.Dispose();
                return oBmp;
            }
            catch (Exception e)
            {
                LogMan.Exception(sC,sM, e); 
                return null;
            }
        }

        

        /// <summary>
        /// Obtains Image Codec 
        /// </summary>
        /// <param name="format">Standard System.Drawing.Imaging.Imageformat (bmp,pnf,tiff,jpeg...)</param>
        /// <returns></returns>
        static ImageCodecInfo GetEncoder(ImageFormat format)
        {
            ImageCodecInfo[] codecs = ImageCodecInfo.GetImageDecoders();
            foreach (ImageCodecInfo codec in codecs)
            {
                if (codec.FormatID == format.Guid)
                {
                    return codec;
                }
            }
            return null;
        }


        /// <summary>
        /// Saves System.Drawing.Bitmap to File
        /// Avoids GDI+ 8bit conversion saving 16bit grayscales
        /// </summary>
        /// <param name="oBitmap"></param>
        /// <param name="sFileName"></param>
        /// <param name="eSaveFormat"></param>
        /// <param name="iImageQuality"></param>
        /// <returns></returns>
        public static Boolean Save(Bitmap oBitmap, Stream oStream, ImageSaveFormat eSaveFormat, int iImageQuality)
        {
            string sM = nameof(Save) + "(stream)";
            if (oStream == null)
            {
                return false;
            }
            if (oBitmap == null)
            {
                return false;
            }
            if (oBitmap.PixelFormat == PixelFormat.DontCare)
            {
                return false;
            }
            try
            {
                switch (eSaveFormat)
                {
                    case ImageSaveFormat.jpg:
                        {
                            int iJPQ = Math.Min(100, Math.Max(1, iImageQuality));
                            ImageCodecInfo oJgpEncoder = GetEncoder(ImageFormat.Jpeg);
                            EncoderParameter oEncoderParameter = new EncoderParameter(System.Drawing.Imaging.Encoder.Quality, iJPQ);
                            EncoderParameters oEncoderParameters = new EncoderParameters(1);
                            oEncoderParameters.Param[0] = oEncoderParameter;
                            oBitmap.Save(oStream, oJgpEncoder, oEncoderParameters);
                            return true;
                        }
                    case ImageSaveFormat.png:
                        {
                            oBitmap.Save(oStream, ImageFormat.Png);
                            return true;
                        }
                    case ImageSaveFormat.bmp:
                        {
                            oBitmap.Save(oStream, ImageFormat.Bmp);
                            return true;
                        }
                    case ImageSaveFormat.tif:
                        {
                            oBitmap.Save(oStream, ImageFormat.Tiff);
                            return true;
                        }
                    default:
                        {
                            LogMan.Error(sC, sM, $"Save Format not valid : {eSaveFormat}");
                            return false;
                        }
                }
            }
            catch (Exception e)
            {
                LogMan.Exception(sC,sM, e);
                return false;
            }
        }


        /// <summary>
        /// Saves System.Drawing.Bitmap to File
        /// Avoids GDI+ 8bit conversion saving 16bit grayscales
        /// </summary>
        /// <param name="oBitmap"></param>
        /// <param name="sFileName"></param>
        /// <param name="eSaveFormat"></param>
        /// <param name="iImageQuality"></param>
        /// <returns></returns>
        public static Boolean Save(Bitmap oBitmap, string sFileName, ImageSaveFormat eSaveFormat, int iImageQuality)
        {
            string sM = nameof(Save) + "(Bitmap)";
            if (String.IsNullOrEmpty(sFileName))
            {
                return false;
            }
            if (oBitmap == null)
            {
                return false;
            }
            if (oBitmap.PixelFormat == PixelFormat.DontCare)
            {
                return false;
            }
            try
            {
                // Add file extension
                if (!sFileName.EndsWith(eSaveFormat.ToString(), StringComparison.OrdinalIgnoreCase))
                {
                    sFileName += "." + eSaveFormat.ToString();
                }
                using (var oStream = new FileStream(sFileName, FileMode.Create, FileAccess.Write))
                {
                    return Save(oBitmap, oStream, eSaveFormat, iImageQuality);
                }
            }
            catch (Exception e)
            {
                LogMan.Exception(sC,sM,e);
                return false;
            }
        }

        /// <summary>
        /// Compress System.Drawing.Bitmap to MemoryStream
        /// </summary>
        /// <param name="oBitmap"></param>
        /// <param name="eSaveFormat"></param>
        /// <param name="iImageQuality"></param>
        /// <returns></returns>
        public static MemoryStream Compress(Bitmap oBitmap, ImageSaveFormat eSaveFormat, int iImageQuality)
        {
            string sM = nameof(Compress) + "(Bitmap)";
            if (oBitmap == null)
            {
                return null;
            }
            MemoryStream oStream = new MemoryStream();
            try
            {
                // Add file extension
                if (Save(oBitmap, oStream, eSaveFormat, iImageQuality))
                {
                    return oStream;
                }
                oStream?.Dispose();
                return null;
            }
            catch (Exception e)
            {
                LogMan.Exception(sC,sM,e);  
                oStream?.Dispose();
                return null;
            }
        }

        /// <summary>
        /// Creates a formatted string for file save purposes.
        /// </summary>
        /// <param name="sFileName">Path and file base name </param>
        /// <param name="iFrameID">Image frame identifier</param>
        /// <param name="iFrameIDPadding">Image frame zero padding</param>
        /// <param name="oTimeStamp">Frame timestamp identifier</param>
        /// <param name="bUseTicks">Timestamp dump mode (DateTime or SystemTicks)</param>
        /// <returns></returns>
        public static string CreateFormattedString(string sFileName, long? iFrameID, int? iFrameIDPadding, DateTime? oTimeStamp, Boolean? bUseTicks)
        {
            string sSaveFile = sFileName;
            if (oTimeStamp != null)
            {
                if (bUseTicks != null && bUseTicks == true)
                {
                    sSaveFile += string.Format("_T{0}", oTimeStamp.Value.Ticks);
                }
                else
                {
                    sSaveFile += string.Format("_D{0:yyyyMMdd-HHmmss-fff}", oTimeStamp.Value);
                }
            }
            if (iFrameID != null && iFrameID >= 0)
            {
                string sPadder = "_F{0:D4}";
                if (iFrameIDPadding != null && iFrameIDPadding > 0 && iFrameIDPadding < 10)
                {
                    sPadder = "_F{0:D" + iFrameIDPadding.ToString() + "}";
                }
                sSaveFile += string.Format(sPadder, iFrameID);
            }
            return sSaveFile;
        }


        /// <summary>
        /// Saves System.Drawing.Bitmap to File
        /// Patches GDI+ sign bug when saving 16bit grayscales
        /// </summary>
        /// <param name="oBitmap">System.Drawing.Bitmap (GDI Bitmap) to save</param>
        /// <param name="sFileName">Base file name</param>
        /// <param name="eSaveFormat">Save format (bmp,tiff,png,jpeg)</param>
        /// <returns></returns>
        public unsafe static Boolean Save(Bitmap oBitmap, string sFileName, ImageSaveFormat eSaveFormat)
        {
            return Save(oBitmap, sFileName, eSaveFormat, JpegDefaultQuality);
        }


        /// <summary>
        /// Save System.Drawing.Bitmap to File, Appending FrameID and Timestamp data
        /// Patches GDI+ sign bug when saving 16bit grayscales
        /// </summary>
        /// <param name="oBitmap">System.Drawing.Bitmap (GDI Bitmap) to save</param>
        /// <param name="sFileName">Base file name</param>
        /// <param name="iFrameID">Index to append</param>
        /// <param name="iFrameIDPadding">Padding Number</param>
        /// <param name="oTimeStamp">Timestamp to Append</param>
        /// <param name="bUseTicks">Save simestamp as system tick</param>
        /// <param name="eSaveFormat">Save format (bmp,tiff,png,jpeg)</param>
        /// <returns></returns>
        public unsafe static Boolean Save(Bitmap oBitmap, string sFileName, long? iFrameID, int? iFrameIDPadding, DateTime? oTimeStamp, Boolean? bUseTicks, ImageSaveFormat eSaveFormat)
        {
            string sSaveFile = CreateFormattedString(sFileName, iFrameID, iFrameIDPadding, oTimeStamp, bUseTicks);
            return Save(oBitmap, sSaveFile, eSaveFormat, JpegDefaultQuality);
        }

        #endregion

        #region Misc

        /// <summary>
        /// Performs a Commodore Amiga Blitter-style copy operation.
        /// <para>You can Interlace/Deinterlace images or copy rois directly between buffers</para>
        /// <para>Operation inside the same buffer is allowed</para>
        /// <para>Warning! Application crashes when a wrong offset calculus causes memcpy buffer overflow</para>
        /// </summary>
        /// <param name="oPointerD">Destination location</param>
        /// <param name="iModuleD">Module of destination (aka the byte offset between end of row and the successive)</param>
        /// <param name="oPointerS">Source Location</param>
        /// <param name="iModuleD">Module od Source (aka the byte offset between end of row and the successive)</param>
        /// <param name="iRowsToBlit">Rows to Blit</param>
        /// <param name="ByteToCopyPerRow">Bytes to Blit</param>
        /// <returns></returns>
        public static bool BitBlit(IntPtr oPointerD, int iModuleD, IntPtr oPointerS, int iModuleS, int iRowsToBlit, int iBytesToBlit)
        {
            if( iRowsToBlit <= 0 ) return false;
            if( iBytesToBlit<= 0 ) return false;    
            if (oPointerD == IntPtr.Zero) return false;
            if (oPointerS == IntPtr.Zero) return false;
            if (oPointerD == oPointerS) return true;
            // Check for ascending or descending copy
            int iStrideS = (iBytesToBlit + iModuleS);
            int iStrideD = (iBytesToBlit + iModuleD);
            if (oPointerS.ToInt64() < oPointerD.ToInt64())
            {
                IntPtr ptrd = oPointerD;
                IntPtr ptrs = oPointerS;
                for (uint y = 0; y < iRowsToBlit; y++)
                {
                    CopyMemory(ptrd, ptrs, (uint)iBytesToBlit);
                    ptrs += iStrideS;
                    ptrd += iStrideD;
                }
            }
            else
            {
                int iPointEndS = iStrideS * iRowsToBlit;
                int iPointEndD = iStrideD * iRowsToBlit;
                IntPtr ptrs = oPointerS + iPointEndS;
                IntPtr ptrd = oPointerD + iPointEndD;
                for (int y = iRowsToBlit-1; y >=0; y--)
                {
                    ptrs -= iStrideS;
                    ptrd -= iStrideD;
                    CopyMemory(ptrd, ptrs, (uint)iBytesToBlit);
                }
            }
            return true;
        }

        /// <summary>
        /// Extract default inage file extension (with dot) for file save purposes.
        /// </summary>
        /// <param name="eFormat"></param>
        /// <param name="bWithDot"></param>
        /// <returns></returns>
        public static string GetImageFileExt(ImageSaveFormat eFormat, bool bWithDot)
        {
            string sDot = bWithDot ? "." : "";
            switch (eFormat)
            {
                case ImageSaveFormat.bmp: return sDot + "bmp";
                case ImageSaveFormat.jpg: return sDot + "jpg";
                case ImageSaveFormat.png: return sDot + "png";
                case ImageSaveFormat.tif: return sDot + "tif";
                default: return sDot + ".raw";
         
            }
        }
        /// <summary>
        /// Extract default video file extension (with dot) for file save purposes.
        /// </summary>
        /// <param name="eFormat"></param>
        /// <param name="bWithDot"></param>
        /// <returns></returns>
        public static string GetVideoFileExt(VideoSaveFormat eFormat, bool bWithDot)
        {
            string sDot = bWithDot ? "." : "";
            switch (eFormat)
            {
                case VideoSaveFormat.Avi: return sDot + "avi";
                case VideoSaveFormat.Mkv: return sDot + "mkv";
                case VideoSaveFormat.Mp4: return sDot + "mp4";
                default: return sDot + ".raw";
            }
        }




        /// <summary>
        /// Obtain list of image files in a folder
        /// </summary>
        /// <param name="sFolderName"></param>
        /// <param name="lFilterOptions"></param>
        /// <param name="bRecursive"></param>
        /// <param name="eSortMode"></param>
        /// <returns></returns>
        public static List<FileInfo> ListImageFiles(string sFolderName, List<string> lFilterOptions, bool bRecursive, EnumFileSortMode eSortMode )
        {
            string sM = nameof(ListImageFiles);

            if (lFilterOptions == null || lFilterOptions.Count == 0)
                lFilterOptions = lDefFileExtension;

            var oList = new List<FileInfo>();
            try
            {
                var oDI = new DirectoryInfo(sFolderName);
                if (!oDI.Exists)
                {
                    LogMan.Error(sC, sM, $"Directory '{sFolderName}' doesn't exist");
                    return null;
                }
                var eSearch = bRecursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
                foreach (var sFilter in lFilterOptions)
                {
                    oList.AddRange(oDI.GetFiles(sFilter, eSearch).ToList());
                }
                switch (eSortMode)
                {
                    case EnumFileSortMode.Name:
                        oList = oList.OrderBy(X => X.Name).ToList();
                        break;
                    case EnumFileSortMode.CreationTime:
                        oList = oList.OrderBy(X => X.CreationTime).ToList();
                        break;
                    case EnumFileSortMode.LastWriteTime:
                        oList = oList.OrderBy(X => X.LastWriteTime).ToList();
                        break;
                }
                return oList;
            }
            catch (Exception ex)
            {
                LogMan.Exception(sC,sM, ex);   
                return oList;
            }
        }

        /// <summary>
        /// Obtain list of image files in a folder
        /// </summary>
        /// <param name="sFolderName"></param>
        /// <param name="bRecursive"></param>
        /// <param name="eSortMode"></param>
        /// <returns></returns>
        public static List<FileInfo> ListImageFiles(string sFolderName, bool bRecursive, EnumFileSortMode eSortMode)
        {
            return ListImageFiles(sFolderName, null, bRecursive, eSortMode);
        }


        /// <summary>
        /// Check Bitmaps Width-Height dimensions 
        /// <para>Stride will not be checked</para>
        /// </summary>
        /// <param name="oBmpA"></param>
        /// <param name="oBmpB"></param>
        /// <returns></returns>
        public static bool BitmapCheckDimensions(Bitmap oBmpA, Bitmap oBmpB)
        {
            if (oBmpA == null || oBmpB == null)
                return false;
            return (oBmpA.Width == oBmpB.Width && oBmpA.Height == oBmpB.Height);
        }

        /// <summary>
        /// Check Bitmaps pixel format 
        /// </summary>
        /// <param name="oBmpA"></param>
        /// <param name="oBmpB"></param>
        /// <returns></returns>
        public static bool BitmapCheckPixelFormat(Bitmap oBmpA, Bitmap oBmpB)
        {
            if (oBmpA == null || oBmpB == null)
                return false;
            return (oBmpA.PixelFormat == oBmpB.PixelFormat);
        }


        /// <summary>
        /// Check Bitmaps parameters (Width, Height, PixelFormat)
        /// <para>Stride will not be checked</para>
        /// </summary>
        /// <param name="oBmpA"></param>
        /// <param name="oBmpB"></param>
        /// <returns></returns>
        public static bool BitmapCheck(Bitmap oBmpA, Bitmap oBmpB)
        {
            if (oBmpA == null || oBmpB == null)
                return false;
            return (oBmpA.Width == oBmpB.Width && oBmpA.Height == oBmpB.Height && oBmpA.PixelFormat == oBmpB.PixelFormat);
        }


        #endregion
    }
}
