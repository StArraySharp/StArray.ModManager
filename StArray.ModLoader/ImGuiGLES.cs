using System.Numerics;
using System.Runtime.InteropServices;
using ImGuiNET;

namespace StArray.ModLoader;

/// <summary>
/// ImGui OpenGL ES 2.0 渲染后端。
/// 将 ImDrawData 渲染到当前 EGL surface 上。
/// </summary>
internal static class ImGuiGLES
{
    // ========================================================================
    // GLES 2.0 constants
    // ========================================================================
    const int GL_FLOAT = 0x1406;
    const int GL_UNSIGNED_BYTE = 0x1401;
    const int GL_TRIANGLES = 0x0004;
    const int GL_TEXTURE_2D = 0x0DE1;
    const int GL_TEXTURE0 = 0x84C0;
    const int GL_BLEND = 0x0BE2;
    const int GL_ONE_MINUS_SRC_ALPHA = 0x0303;
    const int GL_SRC_ALPHA = 0x0302;
    const int GL_SCISSOR_TEST = 0x0C11;
    const int GL_ARRAY_BUFFER = 0x8892;
    const int GL_ELEMENT_ARRAY_BUFFER = 0x8893;
    const int GL_STREAM_DRAW = 0x88E0;
    const int GL_VERTEX_SHADER = 0x8B31;
    const int GL_FRAGMENT_SHADER = 0x8B30;
    const int GL_COMPILE_STATUS = 0x8B81;
    const int GL_LINK_STATUS = 0x8B82;
    const int GL_NEAREST = 0x2600;
    const int GL_LINEAR = 0x2601;
    const int GL_TEXTURE_MIN_FILTER = 0x2801;
    const int GL_TEXTURE_MAG_FILTER = 0x2800;
    const int GL_TEXTURE_WRAP_S = 0x2802;
    const int GL_TEXTURE_WRAP_T = 0x2803;
    const int GL_CLAMP_TO_EDGE = 0x812F;
    const int GL_RGBA = 0x1908;
    const int GL_UNSIGNED_INT_8_8_8_8 = 0x8035;

    // ========================================================================
    // GLES 2.0 functions via DllImport
    // ========================================================================
    [DllImport("libGLESv2.so")]
    static extern void glViewport(int x, int y, int width, int height);
    [DllImport("libGLESv2.so")]
    static extern void glClear(uint mask);
    [DllImport("libGLESv2.so")]
    static extern void glScissor(int x, int y, int width, int height);
    [DllImport("libGLESv2.so")]
    static extern void glEnable(int cap);
    [DllImport("libGLESv2.so")]
    static extern void glDisable(int cap);
    [DllImport("libGLESv2.so")]
    static extern void glBlendFunc(int sfactor, int dfactor);
    [DllImport("libGLESv2.so")]
    static extern void glUseProgram(uint program);
    [DllImport("libGLESv2.so")]
    static extern void glBindTexture(int target, uint texture);
    [DllImport("libGLESv2.so")]
    static extern void glActiveTexture(int texture);
    [DllImport("libGLESv2.so")]
    static extern uint glCreateProgram();
    [DllImport("libGLESv2.so")]
    static extern uint glCreateShader(int type);
    [DllImport("libGLESv2.so")]
    static extern void glShaderSource(uint shader, int count, string[] source, int[] length);
    [DllImport("libGLESv2.so")]
    static extern void glCompileShader(uint shader);
    [DllImport("libGLESv2.so")]
    static extern void glGetShaderiv(uint shader, int pname, int[] status);
    [DllImport("libGLESv2.so")]
    static extern void glGetShaderInfoLog(uint shader, int bufSize, int[] length, byte[] infoLog);
    [DllImport("libGLESv2.so")]
    static extern void glAttachShader(uint program, uint shader);
    [DllImport("libGLESv2.so")]
    static extern void glLinkProgram(uint program);
    [DllImport("libGLESv2.so")]
    static extern void glGetProgramiv(uint program, int pname, int[] status);
    [DllImport("libGLESv2.so")]
    static extern int glGetUniformLocation(uint program, string name);
    [DllImport("libGLESv2.so")]
    static extern int glGetAttribLocation(uint program, string name);
    [DllImport("libGLESv2.so")]
    static extern void glUniform1i(int location, int v0);
    [DllImport("libGLESv2.so")]
    static extern void glUniformMatrix4fv(int location, int count, bool transpose, float[] value);
    [DllImport("libGLESv2.so")]
    static extern void glGenBuffers(int n, uint[] buffers);
    [DllImport("libGLESv2.so")]
    static extern void glBindBuffer(int target, uint buffer);
    [DllImport("libGLESv2.so")]
    static extern void glBufferData(int target, int size, IntPtr data, int usage);
    [DllImport("libGLESv2.so")]
    static extern void glVertexAttribPointer(int index, int size, int type, bool normalized, int stride, IntPtr pointer);
    [DllImport("libGLESv2.so")]
    static extern void glEnableVertexAttribArray(int index);
    [DllImport("libGLESv2.so")]
    static extern void glDrawElements(int mode, int count, int type, IntPtr indices);
    [DllImport("libGLESv2.so")]
    static extern void glDisableVertexAttribArray(int index);
    [DllImport("libGLESv2.so")]
    static extern void glGenTextures(int n, uint[] textures);
    [DllImport("libGLESv2.so")]
    static extern void glTexParameteri(int target, int pname, int param);
    [DllImport("libGLESv2.so")]
    static extern void glTexImage2D(int target, int level, int internalformat, int width, int height, int border, int format, int type, IntPtr pixels);
    [DllImport("libGLESv2.so")]
    static extern void glPixelStorei(int pname, int param);

    // ========================================================================
    // Shaders (GLSL ES 1.00)
    // ========================================================================
    const string VertexShaderSrc = @"
precision mediump float;
uniform mat4 ProjMtx;
attribute vec2 Position;
attribute vec2 UV;
attribute vec4 Color;
varying vec2 Frag_UV;
varying vec4 Frag_Color;
void main() {
    Frag_UV = UV;
    Frag_Color = Color;
    gl_Position = ProjMtx * vec4(Position.xy, 0, 1);
}";

    const string FragmentShaderSrc = @"
precision mediump float;
varying vec2 Frag_UV;
varying vec4 Frag_Color;
uniform sampler2D Texture;
void main() {
    gl_FragColor = Frag_Color * texture2D(Texture, Frag_UV.st);
}";

    // ========================================================================
    // State
    // ========================================================================
    static uint _program;
    static int _attribLocationTex, _attribLocationProjMtx;
    static int _attribLocationPosition, _attribLocationUV, _attribLocationColor;
    static uint _vboHandle, _elementsHandle;
    static uint _fontTexture;

    static bool _initialized = false;
    static bool _firstFrame = true;

    // ========================================================================
    // Init / Shutdown
    // ========================================================================
    public static void Init()
    {
        if (_initialized) return;

        _program = CreateShaderProgram(VertexShaderSrc, FragmentShaderSrc);

        _attribLocationTex = glGetUniformLocation(_program, "Texture");
        _attribLocationProjMtx = glGetUniformLocation(_program, "ProjMtx");
        _attribLocationPosition = glGetAttribLocation(_program, "Position");
        _attribLocationUV = glGetAttribLocation(_program, "UV");
        _attribLocationColor = glGetAttribLocation(_program, "Color");

        var buffers = new uint[2];
        glGenBuffers(2, buffers);
        _vboHandle = buffers[0];
        _elementsHandle = buffers[1];

        CreateFontsTexture();

        _initialized = true;
    }

    public static void Shutdown()
    {
        if (!_initialized) return;
        if (_vboHandle != 0) { var b = new uint[] { _vboHandle }; /* glDeleteBuffers */ _vboHandle = 0; }
        if (_elementsHandle != 0) { _elementsHandle = 0; }
        if (_program != 0) { /* glDeleteProgram */ _program = 0; }
        _initialized = false;
    }

    // ========================================================================
    // Render
    // ========================================================================
    public static void RenderDrawData(ImDrawDataPtr drawData)
    {
        // Diagnostic: first frame, draw a red rect to confirm GL works
        if (_firstFrame)
        {
            _firstFrame = false;
            Mono.Log($"[ImGuiGLES] RenderDrawData: CmdLists={drawData.CmdListsCount} TotalVtx={drawData.TotalVtxCount} TotalIdx={drawData.TotalIdxCount} DisplaySize={drawData.DisplaySize}");
        }

        if (drawData.CmdListsCount == 0) return;

        var io = ImGui.GetIO();
        float fbWidth = drawData.DisplaySize.X * drawData.FramebufferScale.X;
        float fbHeight = drawData.DisplaySize.Y * drawData.FramebufferScale.Y;
        if (fbWidth <= 0 || fbHeight <= 0) return;

        drawData.ScaleClipRects(drawData.FramebufferScale);

        // Backup GL state
        // (Skip for now — assume Unity doesn't care about the ImGui GL state changes)
        glEnable(GL_BLEND);
        glBlendFunc(GL_SRC_ALPHA, GL_ONE_MINUS_SRC_ALPHA);
        glDisable(GL_SCISSOR_TEST); // Will be enabled per draw call

        float[] ortho = {
            2.0f / drawData.DisplaySize.X, 0, 0, 0,
            0, 2.0f / -drawData.DisplaySize.Y, 0, 0,
            0, 0, -1, 0,
            -1, 1, 0, 1
        };

        glUseProgram(_program);
        glUniform1i(_attribLocationTex, 0);
        glUniformMatrix4fv(_attribLocationProjMtx, 1, false, ortho);

        glBindBuffer(GL_ARRAY_BUFFER, _vboHandle);
        glBindBuffer(GL_ELEMENT_ARRAY_BUFFER, _elementsHandle);

        glEnableVertexAttribArray(_attribLocationPosition);
        glEnableVertexAttribArray(_attribLocationUV);
        glEnableVertexAttribArray(_attribLocationColor);

        int vertexSize = 5 * 4;
        glVertexAttribPointer(_attribLocationPosition, 2, GL_FLOAT, false, vertexSize, IntPtr.Zero);
        glVertexAttribPointer(_attribLocationUV, 2, GL_FLOAT, false, vertexSize, (IntPtr)(2 * 4));
        glVertexAttribPointer(_attribLocationColor, 4, GL_UNSIGNED_BYTE, true, vertexSize, (IntPtr)(4 * 4));

        for (int n = 0; n < drawData.CmdListsCount; n++)
        {
            var cmdList = drawData.CmdLists[n];
            int idxBufferOffset = 0;

            unsafe
            {
                // Upload vertex/index buffers
                glBufferData(GL_ARRAY_BUFFER, cmdList.VtxBuffer.Size * vertexSize,
                    (IntPtr)cmdList.VtxBuffer.Data, GL_STREAM_DRAW);
                glBufferData(GL_ELEMENT_ARRAY_BUFFER, cmdList.IdxBuffer.Size * 2,
                    (IntPtr)cmdList.IdxBuffer.Data, GL_STREAM_DRAW);
            }

            for (int cmdI = 0; cmdI < cmdList.CmdBuffer.Size; cmdI++)
            {
                var cmd = cmdList.CmdBuffer[cmdI];

                if (cmd.UserCallback != IntPtr.Zero)
                {
                    // User callback — skip
                    continue;
                }

                glScissor((int)cmd.ClipRect.X, (int)(fbHeight - cmd.ClipRect.W),
                    (int)(cmd.ClipRect.Z - cmd.ClipRect.X),
                    (int)(cmd.ClipRect.W - cmd.ClipRect.Y));
                glBindTexture(GL_TEXTURE_2D, (uint)cmd.TextureId);
                glDrawElements(GL_TRIANGLES, (int)cmd.ElemCount, GL_UNSIGNED_BYTE, (IntPtr)idxBufferOffset);
                idxBufferOffset += (int)cmd.ElemCount * 2;
            }
        }

        glDisableVertexAttribArray(_attribLocationPosition);
        glDisableVertexAttribArray(_attribLocationUV);
        glDisableVertexAttribArray(_attribLocationColor);
        glUseProgram(0);
    }

    // ========================================================================
    // Helpers
    // ========================================================================
    static void CreateFontsTexture()
    {
        var io = ImGui.GetIO();
        unsafe
        {
            io.Fonts.GetTexDataAsRGBA32(out byte* pixels, out int width, out int height, out int bytesPerPixel);
            var texIds = new uint[1];
            glGenTextures(1, texIds);
            _fontTexture = texIds[0];
            glBindTexture(GL_TEXTURE_2D, _fontTexture);
            glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_MIN_FILTER, GL_LINEAR);
            glTexParameteri(GL_TEXTURE_2D, GL_TEXTURE_MAG_FILTER, GL_LINEAR);
            glTexImage2D(GL_TEXTURE_2D, 0, GL_RGBA, width, height, 0, GL_RGBA, GL_UNSIGNED_BYTE, (IntPtr)pixels);
        }
        io.Fonts.SetTexID((IntPtr)_fontTexture);
    }

    static uint CreateShaderProgram(string vertexSrc, string fragmentSrc)
    {
        uint vs = CompileShader(GL_VERTEX_SHADER, vertexSrc);
        uint fs = CompileShader(GL_FRAGMENT_SHADER, fragmentSrc);
        uint program = glCreateProgram();
        glAttachShader(program, vs);
        glAttachShader(program, fs);
        glLinkProgram(program);
        return program;
    }

    static uint CompileShader(int type, string src)
    {
        uint shader = glCreateShader(type);
        glShaderSource(shader, 1, new[] { src }, new[] { src.Length });
        glCompileShader(shader);
        return shader;
    }
}
