using System;

namespace Plot
{
	public class Changeable
	{
		public event EventHandler Changed = delegate {};
		
		private bool d_frozen;
		private bool d_emitChanged;
		
		public void Freeze()
		{
			d_frozen = true;
		}
		
		public void BeginIgnore()
		{
			d_frozen = true;
		}
		
		public void EndIgnore()
		{
			d_frozen = false;
			d_emitChanged = false;
		}
		
		public void Thaw()
		{
			d_frozen = false;
			
			if (d_emitChanged)
			{
				EmitChanged();
				d_emitChanged = false;
			}
		}
		
		public virtual void EmitChanged()
		{
			if (d_frozen)
			{
				d_emitChanged = true;
			}
			else
			{
				Changed(this, new EventArgs());
			}
		}
	}
}

