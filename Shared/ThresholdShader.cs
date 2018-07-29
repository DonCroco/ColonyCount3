using System;
using OpenTK;
using OpenTK.Graphics.ES30;

namespace CC.Core
{
	public class ThresholdShader
	{
		const string vertex_src =  @"                                      
			uniform mat4 matViewProjection;
			attribute vec4 rm_Vertex;
			attribute vec2 rm_TexCoord0;
			varying vec2 rm_Texcoord;
			
			void main( void )
			{
				gl_Position = matViewProjection * rm_Vertex;
				
				rm_Texcoord    = rm_TexCoord0.xy;
			}                                
		";


		
		
		const string fragment_src  = @" 
			#ifdef GL_FRAGMENT_PRECISION_HIGH
			   precision highp float;
			#else
			   precision mediump float;
			#endif

			uniform sampler2D baseMap;
			varying vec2 rm_Texcoord;
			uniform lowp vec4 color;
			uniform lowp vec4 thresholdMin;
			uniform lowp vec4 thresholdMax;

			void main( void )
			{
			   vec4 vTexColor = texture2D( baseMap, rm_Texcoord );
			    
			    vec4 stepMin = step(thresholdMin, vTexColor);
			    vec4 stepMax = 1.0 - step(thresholdMax, vTexColor);
			    vec4 step = stepMin*stepMax;
			    
			    vec4 color = mix(vec4(0,0,0,0), vec4(1,1,1,1), step);
			    
			    gl_FragColor = color.xyzw * vTexColor.w;
			}
		";

		private static ThresholdShader singleton;

		int mProgram = -1;
		public int maPositionHandle;
		public int maTexCoordHandle;
		public int muMVPMatrixHandle;
		int muTextureHandle;
		public int muThresholdMinHandle;
		public int muThresholdMaxHandle;
		int muColorHandle;

		public static ThresholdShader Singleton {
			get {
				if(singleton == null) {

					singleton = new ThresholdShader();
					singleton.mProgram = GLHelper.createProgram(vertex_src, fragment_src);
					
					GL.UseProgram(singleton.mProgram);
					
					singleton.maPositionHandle = GL.GetAttribLocation(singleton.mProgram, "rm_Vertex");
					singleton.maTexCoordHandle = GL.GetAttribLocation(singleton.mProgram, "rm_TexCoord0");
					singleton.muMVPMatrixHandle = GL.GetUniformLocation(singleton.mProgram, "matViewProjection");		
					singleton.muTextureHandle = GL.GetUniformLocation(singleton.mProgram, "baseMap");
					singleton.muThresholdMinHandle = GL.GetUniformLocation(singleton.mProgram, "thresholdMin");
					singleton.muThresholdMaxHandle = GL.GetUniformLocation(singleton.mProgram, "thresholdMax");
					singleton.muColorHandle = GL.GetUniformLocation(singleton.mProgram, "color");

				}
				return singleton;
			}
		}

		private ThresholdShader ()
		{
		}

		public void Use ()
		{
			GL.UseProgram(mProgram);
		}

		public void SetTexture (uint texName)
		{
			GL.ActiveTexture(TextureUnit.Texture0);						// Set active texture slot to 0
			GL.BindTexture(TextureTarget.Texture2D, texName);			// Bind 2D texture to current slot
			GL.Uniform1(muTextureHandle, 0);		// Set shader sampler to texture slot 0
		}

		public void setColor(Vector4 color)
		{
			GL.Uniform4(muColorHandle, 1, ref color.X);
		}


		public void SetThreshold(Vector4 min, Vector4 max)
		{
			GL.Uniform4(muThresholdMinHandle, 1, ref min.X);
			GL.Uniform4(muThresholdMaxHandle, 1, ref max.X);
		}

		// TODO: find better way to access shaders
		public static void Reset() {
			singleton = null;
		}

	}
}

