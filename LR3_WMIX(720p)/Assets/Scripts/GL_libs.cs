#if !(UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN)
using System;
// using System.Linq;
using System.Runtime.InteropServices;
using UnityEngine;
public unsafe static class GL_libs{
#if UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX
    private const string GL_NAME = "OpenGL4";
#else
    private const string GL_NAME = "GLESv2";
#endif
    private const ushort GL_TEXTURE_2D = 0x0DE1, GL_RGBA = 0x1908, GL_UNSIGNED_BYTE = 0x1401;
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
}
#endif