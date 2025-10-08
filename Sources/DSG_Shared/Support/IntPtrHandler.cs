using System;
using System.Collections.Concurrent;
using System.Runtime.InteropServices;

namespace DSG.Base
{
    /// <summary>
    /// Safely manages unmanaged memory (IntPtr) with global tracking and IDisposable.
    /// </summary>
    public class IntPtrHandler : IDisposable
    {
        // Thread-safe global dictionary to track all active unmanaged buffers.
        private static readonly ConcurrentDictionary<IntPtr, IntPtrHandler> dictHandlers =
            new ConcurrentDictionary<IntPtr, IntPtrHandler>();

        // Pointer to the allocated unmanaged memory.
        public IntPtr Handle { get; private set; }

        // Size of the allocated memory in bytes.
        public int Length { get; private set; }

        // Indicates whether the pointer is valid (non-zero).
        public bool Valid => Handle != IntPtr.Zero;

        // Flag to prevent double-free of memory.
        private bool _disposed;

        /// <summary>
        /// Allocates unmanaged memory of the specified size.
        /// Frees any existing memory first.
        /// </summary>
        public IntPtr Alloc(int iByteLength)
        {
            Free(); // Free existing memory if allocated

            Handle = Marshal.AllocHGlobal(iByteLength); // Allocate unmanaged memory
            Length = iByteLength;

            dictHandlers[Handle] = this; // Register buffer in global dictionary
            _disposed = false;
            return Handle;
        }

        /// <summary>
        /// Allocates memory and copies a range of bytes from a managed array.
        /// </summary>
        public IntPtr Alloc(byte[] byteArray, int iByteArrayStart, int iLength)
        {
            Alloc(iLength); // Allocate memory
            Marshal.Copy(byteArray, iByteArrayStart, Handle, iLength); // Copy data
            return Handle;
        }

        /// <summary>
        /// Allocates memory and copies an entire byte array.
        /// </summary>
        public IntPtr Alloc(byte[] byteArray) => Alloc(byteArray, 0, byteArray.Length);

        /// <summary>
        /// Reads a range of bytes from unmanaged memory into a managed array.
        /// </summary>
        public byte[] ToByteArray(int iStart, int iLength)
        {
            if (!Valid) return null;
            var buffer = new byte[iLength];
            Marshal.Copy(Handle, buffer, iStart, iLength);
            return buffer;
        }

        /// <summary>
        /// Reads the entire unmanaged memory into a managed byte array.
        /// </summary>
        public byte[] ToByteArray() => ToByteArray(0, Length);

        /// <summary>
        /// Copies unmanaged memory into an existing array at a given offset.
        /// </summary>
        public byte[] ToByteArray(ref byte[] byteArray, int iByteArrayStart, int iLength)
        {
            if (!Valid) return null;
            Marshal.Copy(Handle, byteArray, iByteArrayStart, iLength);
            return byteArray;
        }

        /// <summary>
        /// Frees unmanaged memory and removes the buffer from the global dictionary.
        /// Safe to call multiple times.
        /// </summary>
        public void Free()
        {
            if (_disposed || Handle == IntPtr.Zero) return; // Already freed

            dictHandlers.TryRemove(Handle, out _); // Remove from global tracking
            Marshal.FreeHGlobal(Handle); // Free unmanaged memory

            Handle = IntPtr.Zero;
            Length = 0;
            _disposed = true;
        }

        /// <summary>
        /// Implements IDisposable for deterministic memory release.
        /// </summary>
        public void Dispose()
        {
            Free();
            GC.SuppressFinalize(this); // Prevent finalizer from running
        }

        /// <summary>
        /// Finalizer: ensures memory is released if Dispose is not called.
        /// </summary>
        ~IntPtrHandler()
        {
            Free();
        }

        /// <summary>
        /// Factory method: creates and allocates a buffer of the specified size.
        /// </summary>
        public static IntPtrHandler Create(int iLength)
        {
            var handler = new IntPtrHandler();
            handler.Alloc(iLength);
            return handler;
        }

        /// <summary>
        /// Factory method: creates and allocates a buffer initialized with a byte array.
        /// </summary>
        public static IntPtrHandler Create(byte[] byteArray)
        {
            if (byteArray == null) return null;
            var handler = new IntPtrHandler();
            handler.Alloc(byteArray);
            return handler;
        }

        /// <summary>
        /// Frees a buffer by its pointer (IntPtr) via the global dictionary.
        /// </summary>
        public static void Free(IntPtr handle)
        {
            if (dictHandlers.TryRemove(handle, out var handler))
            {
                handler.Free();
            }
        }
    }
}
