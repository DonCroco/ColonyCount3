using System;
using OpenTK;
using OpenTK.Graphics.ES30;

namespace CC.Core
{
	public class TextureShader
	{
		const string vertex_src =  @"                                      
			uniform mat4 matViewProjection;
			attribute vec4 rm_Vertex;
			attribute vec2 rm_TexCoord0;
			varying vec2 rm_Texcoord;

			void main( void )
			{
			    gl_Position = matViewProjection * rm_Vertex;
			    rm_Texcoord = rm_TexCoord0.xy;
			}                                 
		";


		const string fragment_src  = @" 
			#ifdef GL_FRAGMENT_PRECISION_HIGH
			   precision highp float;
			#else
			   precision mediump float;
			#endif

			uniform sampler2D baseMap;
			uniform lowp vec4 colorScale;
			varying vec2 rm_Texcoord;


			void main( void )
			{
				gl_FragColor = colorScale*texture2D( baseMap, rm_Texcoord );
			}
		";

		// 
		private static TextureShader singleton;

		public int mProgram = -1;
		public int maPositionHandle;
		public int maTexCoordHandle;
		public int muMVPMatrixHandle;
		public int muTextureHandle;
		private int muColorScaleHandle;

		public static TextureShader Singleton {
			get {
				if(singleton == null) {
					
					singleton = new TextureShader();

					singleton.mProgram = GLHelper.createProgram(vertex_src, fragment_src);
					
					GL.UseProgram(singleton.mProgram);
					
					singleton.maPositionHandle = GL.GetAttribLocation(singleton.mProgram, "rm_Vertex");
					singleton.maTexCoordHandle = GL.GetAttribLocation(singleton.mProgram, "rm_TexCoord0");
					singleton.muMVPMatrixHandle = GL.GetUniformLocation(singleton.mProgram, "matViewProjection");		
					singleton.muTextureHandle = GL.GetUniformLocation(singleton.mProgram, "baseMap");
					singleton.muColorScaleHandle = GL.GetUniformLocation(singleton.mProgram, "colorScale");

					GLHelper.GetError();

				}
				return singleton;
			}
		}

		private TextureShader ()
		{
		}

		public void Use ()
		{
			GL.UseProgram(mProgram);
		}

		public void SetColorScale(Vector4 scale)
		{
			GL.Uniform4(muColorScaleHandle, 1, ref scale.X);
		}

		public void SetMVPMatrix(Matrix4 mat)
		{
			GL.UniformMatrix4(muMVPMatrixHandle, 1, false, ref mat.Row0.X);
		}

		public void SetTexture(uint texName) {
			GL.ActiveTexture(TextureUnit.Texture0);						// Set active texture slot to 0
			GL.BindTexture(TextureTarget.Texture2D, texName);				// Bind 2D texture to current slot
			GL.Uniform1(muTextureHandle, 0);		// Set shader sampler to texture slot 0
		}

		// TODO: find better way to access shaders
		public static void Reset() {
			singleton = null;
		}

		public void EnablePositionVertices(int size, VertexAttribPointerType type, bool normalized, int stride, float[] vertices)
		{
			GL.EnableVertexAttribArray(maPositionHandle);
			GL.VertexAttribPointer(maPositionHandle, size, type, normalized, stride, vertices);
		}
		
		public void DisablePositionVertices ()
		{
			GL.DisableVertexAttribArray(maPositionHandle);
		}

		public void EnableTextureVertices(int size, VertexAttribPointerType type, bool normalized, int stride, float[] vertices)
		{
			GL.EnableVertexAttribArray(maTexCoordHandle);
			GL.VertexAttribPointer(maTexCoordHandle, size, type, normalized, stride, vertices);
		}
		
		public void DisableTextureVertices ()
		{
			GL.DisableVertexAttribArray(maTexCoordHandle);
		}
	}
}

