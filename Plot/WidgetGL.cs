using System;
using Tao.OpenGl;
using GtkGL;
using GdkGL;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Collections.Generic;

namespace Plot
{
	public class WidgetGL : Widget
	{
		private Cairo.ImageSurface d_surface;
		private int d_texture;
		private GlWidget d_gl;
		private bool d_ingl;
		private bool d_isrealized;
		private List<string> d_extensions;
		private bool d_vsync;
		private bool d_supportsVSync;
		private static List<WidgetGL> s_widgets;
		private bool d_inited;
		private bool d_initedResult;

		static WidgetGL()
		{
			s_widgets = new List<WidgetGL>();
		}

		public WidgetGL() : base()
		{
			GdkGL.Config config = new GdkGL.Config(Screen,
			                                       GdkGL.ConfigMode.Rgb |
			                                       GdkGL.ConfigMode.Depth |
			                                       GdkGL.ConfigMode.Double);

			d_isrealized = false;

			d_gl = new GlWidget(this,
			                    config,
			                    null,
			                    true,
			                    RenderType.RgbaType);
		
			Realized += delegate {
				d_isrealized = true;
				Reconfigure();
			};
		}
		
		private bool GLSupported()
		{
			return GLSupported(true);
		}
		
		private bool GLSupported(bool checkinit)
		{
			return (!checkinit || d_initedResult) && d_gl.IsGlCapable();
		}
		
		public bool VSync
		{
			get { return d_vsync; }
			set
			{
				if (d_vsync != value)
				{
					d_vsync = value;
				
					if (d_supportsVSync)
					{
						GLDo(() => {
							glXSwapIntervalSGI(d_vsync ? 1 : 0);
						});
					}
				}
			}
		}
		
		private delegate void GLDoHandler();
		
		private void GLDo(GLDoHandler handler)
		{
			GLDo(true, handler);
		}
		
		private void GLDo(bool checkinit, GLDoHandler handler)
		{
			if (!GLSupported(checkinit))
			{
				return;
			}

			bool gl = d_ingl;
			
			if (!gl)
			{
				d_gl.MakeCurrent();
			}

			d_ingl = true;
			
			handler();
			
			d_ingl = gl;
			
			if (!gl)
			{
				d_gl.SwapBuffers();
			}
		}
		
		private void Cleanup()
		{
			if (d_surface != null)
			{
				((IDisposable)d_surface).Dispose();
				d_surface = null;
			}
			
			if (d_texture != 0)
			{
				GLDo(() => {
					Gl.glDeleteTextures(1, ref d_texture);
					d_texture = 0;
				});
			}
		}
		
		public override void Destroy()
		{
			UnregisterSwapGroup();

			Cleanup();
			base.Destroy();
		}
		
		private void Reconfigure()
		{
			if (!d_isrealized)
			{
				return;
			}
			
			GLDo(false, () => {
				if (!InitGl(Allocation.Width, Allocation.Height))
				{
					return;
				}
			
				Cleanup();

				Gl.glMatrixMode(Gl.GL_PROJECTION);				
				Gl.glOrtho(0, 1, 0, 1, -1, 1);
				Gl.glViewport(0, 0, Allocation.Width, Allocation.Height);
								
				// Generate cairo image to render into
				d_surface = new Cairo.ImageSurface(Cairo.Format.ARGB32,
			                                       Allocation.Width,
			                                       Allocation.Height);
				
				// Generate texture to hold the image data				
				Gl.glGenTextures(1, out d_texture);
				Gl.glBindTexture(Gl.GL_TEXTURE_RECTANGLE_ARB, d_texture);
				Gl.glTexImage2D(Gl.GL_TEXTURE_RECTANGLE_ARB,
				                0,
				                Gl.GL_RGBA,
				                Allocation.Width,
				                Allocation.Height,
				                0,
				                Gl.GL_BGRA,
				                Gl.GL_UNSIGNED_BYTE,
				                null);
			});
		}
		
		protected override bool OnConfigureEvent(Gdk.EventConfigure evnt)
		{
			base.OnConfigureEvent(evnt);

			Reconfigure();

			return false;
		}
		
		private void DrawRectangle()
		{
			Gl.glBegin(Gl.GL_QUADS);
			{
				Gl.glTexCoord2i(0, d_surface.Height);
				Gl.glVertex2i(0, 0);
				
				Gl.glTexCoord2i(d_surface.Width, d_surface.Height);
				Gl.glVertex2i(1, 0);
				
				Gl.glTexCoord2i(d_surface.Width, 0);
				Gl.glVertex2i(1, 1);
				
				Gl.glTexCoord2i(0, 0);
				Gl.glVertex2i(0, 1);
			}
			Gl.glEnd();
		}
		
		[DllImport("libGL.dll")]
		private static extern int glXSwapIntervalSGI(int on);
		
		[DllImport("libGL.dll")]
		private static extern int glXJoinSwapGroupSGIX(IntPtr display, IntPtr drawable, IntPtr member);
		
		[DllImport("libGL.dll")]
		private static extern string glXQueryExtensionsString(IntPtr handle, int screen);
		
		[DllImport("libgdk-x11-2.0.dll")]
		private static extern IntPtr gdk_x11_display_get_xdisplay(IntPtr handle);
		
		[DllImport("libgdkglext-x11-1.0.dll")]
		private static extern IntPtr gdk_x11_gl_window_get_glxwindow(IntPtr handle);
		
		[DllImport("libgdkglext-x11-1.0.dll")]
		private static extern IntPtr gtk_widget_get_gl_window(IntPtr handle);
		
		private IntPtr GLXDrawable
		{
			get
			{
				return gdk_x11_gl_window_get_glxwindow(gtk_widget_get_gl_window(Handle));
			}
		}
		
		private void UnregisterSwapGroup()
		{
			if (s_widgets.Contains(this))
			{
				glXJoinSwapGroupSGIX(gdk_x11_display_get_xdisplay(Display.Handle),
				                     GLXDrawable,
				                     IntPtr.Zero);

				s_widgets.Remove(this);
			}
		}
		
		private void RegisterSwapGroup()
		{
			if (s_widgets.Count > 0)
			{
				glXJoinSwapGroupSGIX(gdk_x11_display_get_xdisplay(Display.Handle),
				                     s_widgets[s_widgets.Count - 1].GLXDrawable,
				                     GLXDrawable);
			}
			
			s_widgets.Add(this);
		}
		
		private bool InitGl(int width, int height)
		{
			if (d_inited)
			{
				return d_initedResult;
			}

			bool ret = true;
			d_inited = true;

			GLDo(false, () => {
				if (d_extensions == null)
				{
					d_extensions = new List<string>();
					
					string exts = Gl.glGetString(Gl.GL_EXTENSIONS);
					d_extensions.AddRange(exts.Split(' '));
					
					exts = glXQueryExtensionsString(gdk_x11_display_get_xdisplay(Display.Handle), Screen.Number);
					d_extensions.AddRange(exts.Split(' '));
					
					d_extensions.Sort();
					d_supportsVSync = d_extensions.Contains("GLX_SGI_swap_control");
				}
				
				if (!d_extensions.Contains("GL_ARB_texture_rectangle"))
				{
					DrawOverride = false;
					ret = false;
					
					Console.WriteLine("No texture rectangle");
					return;
				}
				
				DrawOverride = true;
				
				if (d_supportsVSync)
				{
					glXSwapIntervalSGI(d_vsync ? 1 : 0);
					
					RegisterSwapGroup();
				}

				Gl.glClearColor((float)Graph.BackgroundColor.R,
				                (float)Graph.BackgroundColor.G,
				                (float)Graph.BackgroundColor.B,
				                (float)Graph.BackgroundColor.A);
				
				Gl.glDisable(Gl.GL_DEPTH_TEST);
				Gl.glEnable(Gl.GL_TEXTURE_RECTANGLE_ARB);
			});
			
			d_initedResult = ret;
			
			return d_initedResult;
		}
		
		protected override bool OnExposeEvent(Gdk.EventExpose evnt)
		{
			base.OnExposeEvent(evnt);
			
			if (!GLSupported())
			{
				return false;
			}
			
			GLDo(() => {
				// Render in cairo image
				using (Cairo.Context ctx = new Cairo.Context(d_surface))
				{
					Gdk.CairoHelper.Rectangle(ctx, evnt.Area);
					ctx.Clip();
					
					DrawTo(ctx);
				}
				
				Gl.glMatrixMode(Gl.GL_MODELVIEW);
				Gl.glLoadIdentity();
				
				Gl.glClear(Gl.GL_COLOR_BUFFER_BIT);
				
				// Then blit the image in the texture
				Gl.glBindTexture(Gl.GL_TEXTURE_RECTANGLE_ARB, d_texture);
				Gl.glTexImage2D(Gl.GL_TEXTURE_RECTANGLE_ARB,
				                0,
				                Gl.GL_RGBA,
				                d_surface.Width,
				                d_surface.Height,
				                0,
				                Gl.GL_BGRA,
				                Gl.GL_UNSIGNED_BYTE,
				                d_surface.Data);
			
				DrawRectangle();
			});

			return true;
		}
		
		protected override Cairo.Surface CreateGraphSurface(int width, int height)
		{
			return new Cairo.ImageSurface(Cairo.Format.ARGB32, width, height);
		}
	}
}

