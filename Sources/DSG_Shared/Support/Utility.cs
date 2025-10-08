using DSG.Log;
using System.Runtime.InteropServices;

namespace DSG.Shared
{
    public static class Utility
    {
        static readonly string sC = nameof(Utility);

        public static byte[]? IntPtrToByteArray(IntPtr ptr, int length)
        {
            string sM = nameof(IntPtrToByteArray);
            try
            {
                byte[] byteArray = null;
                if (ptr == IntPtr.Zero || length <= 0)
                {
                    return null;
                }
                Marshal.Copy(byteArray, 0, ptr, length);
                return byteArray;
            }
            catch (Exception ex)
            {
                LogMan.Exception(sC, sM, ex);
                return null;
            }
        }
    }
}
