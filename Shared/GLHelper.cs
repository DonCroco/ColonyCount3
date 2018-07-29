using System;
using System.Drawing;

using OpenTK;
using OpenTK.Graphics.ES30;


namespace CC.Core
{
	public class GLHelper
	{
		public static int bindFrameBuffer(uint frameBuffer, uint texName)
		{
			int[] oldFBO = new int[1];
			GL.GetInteger(GetPName.FramebufferBinding, oldFBO);
			
			// Bind frame buffer
			GL.BindFramebuffer(FramebufferTarget.Framebuffer, frameBuffer);
			GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferSlot.ColorAttachment0, TextureTarget.Texture2D, texName, 0);
			
			//			int status = glCheckFramebufferStatus(GL_FRAMEBUFFER);
			//			if (status != GL_FRAMEBUFFER_COMPLETE) {
			//				printf("Bind framebuffer failed !");
			//			}
			
			return oldFBO[0];
		}

		public static void DrawSprite (uint texId, Rectangle rect, Matrix4 mat)
		{
			DrawSprite (texId, rect, mat, null, Vector4.One);
		}

		public static void DrawSprite (uint texId, Rectangle rect, Matrix4 mat, float[] texCoords, Vector4 colorScale)
		{
			float x = rect.X;
			float y = rect.Y;
			float width = rect.Width;
			float height = rect.Height;
			
			float[] Vertices = {
				x + width, y,
				x + width, y + height,
				x, y,
				x, y + height};
			
			if (texCoords == null) {
				float[] tx = {1.0f, 0.0f, 1.0f, 1.0f, 0.0f, 0.0f, 0.0f, 1.0f};
				texCoords = tx;
			}

			GLHelper.GetError();

			TextureShader shader = TextureShader.Singleton;
			
			// Use shader
			GL.UseProgram(shader.mProgram);

			shader.SetColorScale(colorScale);

			// Set texture
			GL.ActiveTexture(TextureUnit.Texture0);						// Set active texture slot to 0
			GL.BindTexture(TextureTarget.Texture2D, texId);				// Bind 2D texture to current slot
			GL.Uniform1(shader.muTextureHandle, 0);		// Set shader sampler to texture slot 0
			
			// Set Model view
			GL.UniformMatrix4(shader.muMVPMatrixHandle, 1, false, ref mat.Row0.X);
			
			// Set vertex and texture coords
			GL.EnableVertexAttribArray(shader.maPositionHandle);
			GL.EnableVertexAttribArray(shader.maTexCoordHandle);
			
			GL.VertexAttribPointer(shader.maPositionHandle, 2, VertexAttribPointerType.Float, false, 0, Vertices);
			GL.VertexAttribPointer(shader.maTexCoordHandle, 2, VertexAttribPointerType.Float, false, 0, texCoords);
			
			GL.DrawArrays(BeginMode.TriangleStrip, 0, 4);
			
			GL.DisableVertexAttribArray(shader.maPositionHandle);
			GL.DisableVertexAttribArray(shader.maTexCoordHandle);
			
			GLHelper.GetError();
			
		}

		public static void drawRect(RectangleF rect, Vector4 color, Matrix4 MVPMatrix)
		{
			float x = rect.X;
			float y = rect.Y;
			float width = rect.Width;
			float height = rect.Height;
			
			float[] Vertices = {
				x + width, y,
				x + width, y + height,
				x, y,
				x, y + height};
			
			AmbientShader shader = AmbientShader.Singleton;
			
			// Use shader
			shader.Use();
			shader.SetColor(color);
			shader.SetMVPMatrix(MVPMatrix);
			shader.EnableVertices(2, VertexAttribPointerType.Float, false, 0, Vertices);

			GL.DrawArrays(BeginMode.TriangleStrip, 0, 4);

			shader.DisableVertices();

			GLHelper.GetError();
			
		}

		private static int loadShader(ShaderType type, string shaderCode){
			
			// create a vertex shader type (GLES20.GL_VERTEX_SHADER)
			// or a fragment shader type (GLES20.GL_FRAGMENT_SHADER)
			int shader = GL.CreateShader(type); 
			
			// add the source code to the shader and compile it
			string[] code = { shaderCode };
			int[] length = { shaderCode.Length };
			GL.ShaderSource(shader, 1, code, length);
			GL.CompileShader(shader);
			
			GLHelper.GetError();
			
			int result = (int)All.True;
			GL.GetShader(shader, ShaderParameter.CompileStatus, out result);
			if(result == (int)All.False) {
				int logLength = 0;
				GL.GetShader(shader, ShaderParameter.InfoLogLength, out logLength);
				
				System.Text.StringBuilder info = new System.Text.StringBuilder(logLength);
				GL.GetShaderInfoLog(shader, logLength, out logLength, info);
				
				Console.WriteLine("Shader compile failed:"+info);
				
				return 0;
			}
			
			
			return shader;
		}
		
		public static int createProgram(String vertexShaderCode, String fragmentShaderCode) {
			GLHelper.GetError();

			int vertexShader = loadShader(ShaderType.VertexShader, vertexShaderCode);
			int fragmentShader = loadShader(ShaderType.FragmentShader, fragmentShaderCode);
			
			int mProgram = GL.CreateProgram();             // create empty OpenGL Program
			GL.AttachShader(mProgram, vertexShader);   // add the vertex shader to program
			GL.AttachShader(mProgram, fragmentShader); // add the fragment shader to program
			GL.LinkProgram(mProgram);                  // creates OpenGL program executables
			
			GLHelper.GetError();
			
			return mProgram;
		}
		
		public static void GetError()
		{
            ErrorCode error = GL.GetErrorCode();
            if (error != ErrorCode.NoError)
            {
                Console.WriteLine("GL ERROR:" + error);
            }
        }

		public static float[] CreateSphereVertices (Vector2 pos, float r, bool bStartWithCenter)
		{
			int stepCount = 256;
			int vertexCount = bStartWithCenter ? stepCount + 2 : stepCount;
			int elementCount = vertexCount * 2;
			float[] sphereVertices = new float[elementCount];
			float angle = 0;
			float deltaAngle = 2.0f * (float)Math.PI / (float)stepCount;
			int i = 0;
			if (bStartWithCenter) {
				sphereVertices[i++] = pos.X;
				sphereVertices[i++] = pos.Y;
			}
			while (i < elementCount) {
				sphereVertices[i++] = pos.X + (float)Math.Cos(angle) * (float) r;
				sphereVertices[i++] = pos.Y + (float)Math.Sin(angle) * (float) r;
				angle += deltaAngle;
			}

			return sphereVertices;
		}
	}
}

