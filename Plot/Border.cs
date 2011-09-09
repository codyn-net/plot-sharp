using System;
using System.Collections.Generic;
using Biorob.Math;

namespace Plot
{
	public class Border<T> : Changeable where T : struct
	{
		private T d_left;
		private T d_top;
		private T d_right;
		private T d_bottom;

		public Border(T left, T top, T right, T bottom)
		{
			d_left = left;
			d_right = right;
			d_top = top;
			d_bottom = bottom;
		}
		
		public Border(T all) : this(all, all, all, all)
		{
		}
		
		public Border() : this(default(T), default(T), default(T), default(T))
		{
		}
		
		public void Update(T left, T top, T right, T bottom)
		{
			d_left = left;
			d_top = top;
			d_right = right;
			d_bottom = bottom;
			
			EmitChanged();
		}

		private bool Eq(T a, T b)
		{
			return EqualityComparer<T>.Default.Equals(a, b);
		}
		
		public T Left
		{
			get
			{
				return d_left;
			}
			set
			{
				if (!Eq(d_left, value))
				{
					d_left = value;
					
					EmitChanged();
				}
			}
		}
		
		public T Top
		{
			get
			{
				return d_top;
			}
			
			set
			{
				if (!Eq(d_top, value))
				{
					d_top = value;
					
					EmitChanged();
				}
			}
		}
		
		public T Right
		{
			get
			{
				return d_right;
			}
			set
			{
				if (!Eq(d_right, value))
				{
					d_right = value;
					
					EmitChanged();
				}
			}
		}
		
		public T Bottom
		{
			get
			{
				return d_bottom;
			}
			set
			{
				if (!Eq(d_bottom, value))
				{
					d_bottom = value;
				
					EmitChanged();
				}
			}
		}
	}
}
