using System;
// using System.Linq;
using System.Runtime.InteropServices;
using UnityEngine;
public unsafe static class GL_libs{
#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN || PLATFORM_STANDALONE_WIN
    private const string PluginName = "DX11Plugin";// GL_NAME = "d3d11",
    // private static uint D3D11CalcSubResource(
    //     uint mipSlice, uint arraySlice, uint mipLevels) => mipSlice + arraySlice * mipLevels;
    // private static readonly uint DstSubresource = D3D11CalcSubResource(0, 0, 1);
    [DllImport(PluginName)] public extern static void Release(
        ref IntPtr res, ref UIntPtr device, ref UIntPtr context);
    [DllImport(PluginName)] public extern static void ModifyTexturePixels(UIntPtr context,
        IntPtr res, int width, int height, void* dataPtr, byte pixelSize = 4, uint dstSubresource = 0);
    // [DllImport(PluginName)] public extern static void ModifyTexturePixels(UIntPtr context,
    //     IntPtr res, int width, int height, UIntPtr dataPtr, byte pixelSize = 4, uint dstSubresource = 0);
    [DllImport(PluginName)] public extern static void GetInfo(
        IntPtr res, out UIntPtr device, out UIntPtr context);
#else
#if UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX
    private const string GL_NAME = "OpenGL4";
// #elif UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN || PLATFORM_STANDALONE_WIN
//     private const string GL_NAME = "libGLESv2", egl = "libEGL";
#else
    private const string GL_NAME = "GLESv2";
#endif
    private const ushort GL_TEXTURE_2D = 0x0DE1, GL_RGBA = 0x1908, GL_RGB = 0x1907, GL_UNSIGNED_BYTE = 0x1401;
    [DllImport(GL_NAME)] private extern static void glBindTexture(uint target, uint texture);
    public static void BindTexture(uint texture) => glBindTexture(GL_TEXTURE_2D, texture);
	[DllImport(GL_NAME)] private extern static void glTexImage2D(uint target, int level,
        int internalformat, int width, int height, int border, uint format, uint type, void* data);
    public static void TexImage2D(int width, int height, void* data) => glTexImage2D(
        GL_TEXTURE_2D, 0, GL_RGBA, width, height, 0, GL_RGBA, GL_UNSIGNED_BYTE, data);
    [DllImport(GL_NAME)] private extern static void glTexSubImage2D(uint target, int level,
        int x, int y, int width, int height, uint format, uint type, void* pixels);
    // [DllImport(GL_NAME)] private extern static void glTexSubImage2D(uint target, int level,
    //     int x, int y, int width, int height, uint format, uint type, IntPtr pixels);
    public static void TexSubImage2D(int width, int height, void* data) => glTexSubImage2D(
        GL_TEXTURE_2D, 0, 0, 0, width, height, GL_RGBA, GL_UNSIGNED_BYTE, data);
    public static void TexSubImageRGB(int y, int width, int height, void* data) => glTexSubImage2D(
        GL_TEXTURE_2D, 0, 0, y, width, height, GL_RGB, GL_UNSIGNED_BYTE, data);
    // public static void TexSubImage2D(int width, int height, IntPtr data) => glTexSubImage2D(
    //     GL_TEXTURE_2D, 0, 0, 0, width, height, GL_RGBA, GL_UNSIGNED_BYTE, data);
    [DllImport(GL_NAME)] public extern static void glDeleteTextures(int count, uint* textures);
    [DllImport(GL_NAME)] public extern static void glGenTextures(int count, uint* textures);
    public static Texture2D Texture2DFromGL(Color32[] pixels, int width, int height, ref uint texture_name){
        fixed(uint* tn = &texture_name) glDeleteTextures(1, tn);
        texture_name = 0;
        fixed(uint* tn = &texture_name) glGenTextures(1, tn);
        if(texture_name == 0) return null;
        BindTexture(texture_name);
        fixed(void* p = pixels) TexImage2D(width, height, p);
        Texture2D t2d = Texture2D.CreateExternalTexture(width, height,
            TextureFormat.RGBA32, false, false, (IntPtr)texture_name);
        t2d.filterMode = FilterMode.Point;
        return t2d;
        // return Texture2D.CreateExternalTexture(width, height,
        //     TextureFormat.RGBA32, false, false, (IntPtr)texture_name);
    }
    public static Texture2D NewRGBTex(byte[] color24s, int width, int height, ref uint texture_name){
        fixed(uint* tn = &texture_name) glDeleteTextures(1, tn);
        texture_name = 0;
        fixed(uint* tn = &texture_name) glGenTextures(1, tn);
        if(texture_name == 0) return null;
        BindTexture(texture_name);
        fixed(void* p = color24s) glTexImage2D(GL_TEXTURE_2D, 0,
            GL_RGB, width, height, 0, GL_RGB, GL_UNSIGNED_BYTE, p);
        Texture2D t2d = Texture2D.CreateExternalTexture(width, height,
            TextureFormat.RGB24, false, false, (IntPtr)texture_name);
        t2d.filterMode = FilterMode.Point;
        return t2d;
    }
#endif
}