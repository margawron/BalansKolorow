using System;
using System.Runtime.InteropServices;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL4;

namespace BalansKolorow
{

    class LiveBitmapRenderer : GameWindow
    {
        int bitmapWidth, bitmapHeight;
        byte[] bitmapArray;

        int vertexShader, fragmentShader;
        string vertexShaderSource =
@"#version 330 core
layout (location = 0) in vec3 aPosition;

layout (location = 1) in vec2 aTexCoord;

out vec2 texCoord;

void main()
{
    texCoord = vec2(aTexCoord.s,1.0-aTexCoord.t);
    gl_Position = vec4(aPosition, 1.0);
}";
        string fragmentShaderSource =
@"#version 330 core
out vec4 outputColor;

in vec2 texCoord;

uniform sampler2D texture0;

void main()
{
    outputColor = texture(texture0, texCoord);
}";

        Ref<bool> shouldRun;
        float[] verticesAndTextureCoords =
        {
            1.0f,  1.0f,  0.0f, 1.0f, 1.0f,   // Top-right vert for triangle and texture
            1.0f,  -1.0f, 0.0f, 1.0f, 0.0f,  // Bottom-right vert for triangle and texture
            -1.0f, -1.0f, 0.0f, 0.0f, 0.0f,   // Bottom-left vert for triangle and texture
            -1.0f,  1.0f, 0.0f, 0.0f, 1.0f  // Top-left vert for triangle and texture
        };
        uint[] indicesCoords =
        {
            0, 1, 3,
            1, 2, 3
        };
        // Handles for OpenGL
        private int VBO; // vertex buffer object
        private int EBO; // Element buffer object
        private int VAO; // vertex array object
        private int PBO; // Pixel buffer object
        private int Texture;
        int shaderProgramHandle;


        public unsafe LiveBitmapRenderer(int bitmapW, int bitmapH, Ref<bool> keepOpen, byte[] bitmap)
            : base(800, 600, GraphicsMode.Default, "LiveRenderer")
        {
            bitmapArray = bitmap;
            bitmapHeight = bitmapH;
            bitmapWidth = bitmapW;
            shouldRun = keepOpen;
        }

        private void CompileShaders()
        {
            vertexShader = GL.CreateShader(ShaderType.VertexShader);
            GL.ShaderSource(vertexShader, vertexShaderSource);

            fragmentShader = GL.CreateShader(ShaderType.FragmentShader);
            GL.ShaderSource(fragmentShader, fragmentShaderSource);

            GL.CompileShader(vertexShader);

            string vertexErrorMessage = GL.GetShaderInfoLog(vertexShader);
            if (vertexErrorMessage != System.String.Empty)
                Console.WriteLine(vertexErrorMessage);

            GL.CompileShader(fragmentShader);
            string fragmentErrorMessage = GL.GetShaderInfoLog(fragmentShader);
            if (fragmentErrorMessage != System.String.Empty)
                Console.WriteLine(fragmentErrorMessage);
            shaderProgramHandle = GL.CreateProgram();

            GL.AttachShader(shaderProgramHandle, vertexShader);
            GL.AttachShader(shaderProgramHandle, fragmentShader);

            GL.LinkProgram(shaderProgramHandle);

            GL.DetachShader(shaderProgramHandle, vertexShader);
            GL.DetachShader(shaderProgramHandle, fragmentShader);
            GL.DeleteShader(fragmentShader);
            GL.DeleteShader(vertexShader);

        }



        protected override void OnLoad(EventArgs e)
        {
            GL.ClearColor(0.2f, 0.3f, 0.3f, 1.0f);

            VBO = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ArrayBuffer, VBO);
            GL.BufferData(BufferTarget.ArrayBuffer, verticesAndTextureCoords.Length * sizeof(float), verticesAndTextureCoords, BufferUsageHint.StaticDraw);

            EBO = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, EBO);
            GL.BufferData(BufferTarget.ElementArrayBuffer, indicesCoords.Length * sizeof(uint), indicesCoords, BufferUsageHint.StaticDraw);

            PBO = GL.GenBuffer();
            GL.BindBuffer(BufferTarget.PixelUnpackBuffer, PBO);
            GL.BufferData(BufferTarget.PixelUnpackBuffer, bitmapArray.Length, IntPtr.Zero, BufferUsageHint.StreamDraw);
            
            // Make a compiled version of a shader
            CompileShaders();
            GL.UseProgram(shaderProgramHandle);

            Texture = GL.GenTexture();  // Generate handle
            GL.ActiveTexture(TextureUnit.Texture0); // Activate texture
            GL.BindTexture(TextureTarget.Texture2D, Texture);
            IntPtr ptr = GL.MapBuffer(BufferTarget.PixelUnpackBuffer, BufferAccess.WriteOnly);
            if (ptr != null)
            {
                Marshal.Copy(bitmapArray, 0, ptr, bitmapArray.Length);
            }
            GL.UnmapBuffer(BufferTarget.PixelUnpackBuffer);
            GL.BindBuffer(BufferTarget.PixelUnpackBuffer, PBO);
            checkForErrors("137");
            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgb, bitmapWidth, bitmapHeight, 0, PixelFormat.Bgra, PixelType.UnsignedByte, IntPtr.Zero); // LOAD PBO
            checkForErrors("138");
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.Repeat);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.Repeat);
            GL.GenerateMipmap(GenerateMipmapTarget.Texture2D);
            GL.BindBuffer(BufferTarget.PixelUnpackBuffer, 0);


            VAO = GL.GenVertexArray();
            GL.BindVertexArray(VAO);
           
            GL.BindBuffer(BufferTarget.ArrayBuffer, VBO);
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, EBO);

            var vertexLocation = GL.GetAttribLocation(shaderProgramHandle, "aPosition");
            GL.EnableVertexAttribArray(vertexLocation);
            GL.VertexAttribPointer(vertexLocation, 3, VertexAttribPointerType.Float, false, 5 * sizeof(float), 0);

            int texCoordLocation = GL.GetAttribLocation(shaderProgramHandle, "aTexCoord");
            GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 5 * sizeof(float), 0);
            GL.EnableVertexAttribArray(texCoordLocation);
            GL.VertexAttribPointer(texCoordLocation, 2, VertexAttribPointerType.Float, false, 5 * sizeof(float), 3 * sizeof(float));


            base.OnLoad(e);
        }

        protected override void OnRenderFrame(FrameEventArgs e)
        {
            GL.Clear(ClearBufferMask.ColorBufferBit);
            // Reload texture
            GL.BindTexture(TextureTarget.Texture2D, Texture);
            GL.BindBuffer(BufferTarget.PixelUnpackBuffer, PBO);
            GL.BufferData(BufferTarget.PixelUnpackBuffer, bitmapArray.Length, IntPtr.Zero, BufferUsageHint.StreamDraw);
            IntPtr ptr = GL.MapBuffer(BufferTarget.PixelUnpackBuffer, BufferAccess.WriteOnly);
            if (ptr != null)
            {
                Marshal.Copy(bitmapArray, 0, ptr, bitmapArray.Length);
            }
            GL.UnmapBuffer(BufferTarget.PixelUnpackBuffer);
            GL.BindBuffer(BufferTarget.PixelUnpackBuffer, PBO);
            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgb, bitmapWidth, bitmapHeight, 0, PixelFormat.Bgra, PixelType.UnsignedByte, IntPtr.Zero); // LOAD PBO
            GL.GenerateMipmap(GenerateMipmapTarget.Texture2D);
            GL.BindBuffer(BufferTarget.PixelUnpackBuffer, 0);

            // Use VAO
            GL.BindVertexArray(VAO);
            // Use texture
            GL.ActiveTexture(TextureUnit.Texture0);
            GL.BindTexture(TextureTarget.Texture2D, Texture);
            // Use Shader
            GL.UseProgram(shaderProgramHandle);

            GL.BindTexture(TextureTarget.Texture2D, Texture);

            GL.DrawElements(PrimitiveType.Triangles, indicesCoords.Length, DrawElementsType.UnsignedInt, 0);

            SwapBuffers();
            base.OnRenderFrame(e);
        }

        protected override void OnUpdateFrame(FrameEventArgs e)
        {
            if (!shouldRun.Value)
            {
                Exit();
            }

            base.OnUpdateFrame(e);
        }

        protected override void OnResize(EventArgs e)
        {
            GL.Viewport(0, 0, Width, Height);
            base.OnResize(e);
        }

        protected override void OnUnload(EventArgs e)
        {
            GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, 0);
            GL.BindBuffer(BufferTarget.PixelUnpackBuffer, 0);
            GL.BindVertexArray(0);
            GL.UseProgram(0);

            GL.DeleteBuffer(VBO);
            GL.DeleteBuffer(EBO);
            GL.DeleteBuffer(PBO);
            GL.DeleteVertexArray(VAO);
            GL.DeleteTexture(Texture);
            GL.DeleteProgram(shaderProgramHandle);

            base.OnUnload(e);
        }

        protected override void OnClosed(EventArgs e)
        {
            shouldRun.Value = false;
            base.OnClosed(e);
        }

        private void checkForErrors(string line)
        {
            var error = GL.GetError();
            while (error != ErrorCode.NoError)
            {
                Console.WriteLine($"Error on line {line}");
                Console.WriteLine($"Error {error.ToString()}");
                error = GL.GetError();
            }
        }
    }


    class Ref<T>
    {
        private Func<T> getter;
        private Action<T> setter;
        public Ref(Func<T> getter, Action<T> setter)
        {
            this.getter = getter;
            this.setter = setter;
        }

        public T Value
        {
            get { return getter(); }
            set { setter(value); }
        }

    }
}

