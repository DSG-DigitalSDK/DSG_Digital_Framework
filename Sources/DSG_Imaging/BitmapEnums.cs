using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace DSG.Imaging
{
    /// <summary>
    /// Image Save Format
    /// </summary>
    [Serializable, Obfuscation(Exclude = true)]
    public enum ImageSaveFormat
    {
        bmp,
        tif,
        png,
        jpg,
        // Future...
        // heif
    }

    /// <summary>
    /// Image Save Format
    /// </summary>
    [Serializable, Obfuscation(Exclude = true)]
    public enum VideoSaveFormat
    {
        Mkv = 0,
        Mp4,
        Avi,
        // Future...
    }


    /// <summary>
    /// 8BPP image indexed palette for chunky images
    /// </summary>
    [Serializable, Obfuscation(Exclude = true)]
    public enum Palette
    {
        Gray,
        GrayInverted,
        Red,
        RedInverted,
        Green,
        GreenInverted,
        Blue,
        BlueInverted,
        Yellow,
        YellowInverted,
        Cyan,
        CyanInverted,
        Purple,
        PurpleInverted,
        Rainbow,
        RainbowInverted,
        ThermalLava,
        ThermalLavaInverted,
        Iron,
        IronInverted
    }
}
