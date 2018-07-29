using System;
using OpenTK;
using OpenTK.Graphics.ES30;

namespace CC.Core
{
	public class AmbientShader
	{
		const string vertex_src =  @"                                      
			uniform mat4 matViewProjection;
			attribute vec4 rm_Vertex;

			void main( void )
			{
			    gl_Position = matViewProjection * rm_Vertex;
			}                                 
		";


		const string fragment_src  = @" 
			#ifdef GL_FRAGMENT_PRECISION_HIGH
			   precision highp float;
			#else
			   precision mediump float;
			#endif

			uniform vec4 color;
			void main( void )
			{
				gl_FragColor = color;
			}
		";
		//gl_FragColor = vec4(1.0, 0.0, 0.0, 1.0);

		// 
		private static AmbientShader singleton;

		public int mProgram = -1;
		public int maPositionHandle;
		public int muMVPMatrixHandle;
		public int muColorHandle;

		public static AmbientShader Singleton {
			get {
				if(singleton == null) {
					
					singleton = new AmbientShader();

					singleton.mProgram = GLHelper.createProgram(vertex_src, fragment_src);
					
					GL.UseProgram(singleton.mProgram);
					
					singleton.maPositionHandle = GL.GetAttribLocation(singleton.mProgram, "rm_Vertex");
					singleton.muColorHandle = GL.GetUniformLocation(singleton.mProgram, "color");
					singleton.muMVPMatrixHandle = GL.GetUniformLocation(singleton.mProgram, "matViewProjection");		
					
					GLHelper.GetError();


				}
				return singleton;
			}
		}

		public void Use ()
		{
			GL.UseProgram(mProgram);
		}

		public void SetColor(Vector4 color)
		{
			GL.Uniform4(muColorHandle, 1, ref color.X);
		}


		public void SetMVPMatrix(Matrix4 mat)
		{
			GL.UniformMatrix4(muMVPMatrixHandle, 1, false, ref mat.Row0.X);
		}

		public void EnableVertices(int size, VertexAttribPointerType type, bool normalized, int stride, float[] vertices)
		{
			GL.EnableVertexAttribArray(maPositionHandle);
			GL.VertexAttribPointer(maPositionHandle, size, type, normalized, stride, vertices);
		}

		public void DisableVertices ()
		{
			GL.DisableVertexAttribArray(maPositionHandle);
		}




		private AmbientShader ()
		{
		}

		// TODO: find better way to access shaders
		public static void Reset() {
			singleton = null;
		}

	}
}

