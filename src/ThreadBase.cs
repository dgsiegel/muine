/*
 * Copyright (C) 2005 Jorn Baayen <jbaayen@gnome.org> 
 *
 * This program is free software; you can redistribute it and/or
 * modify it under the terms of the GNU General Public License as
 * published by the Free Software Foundation; either version 2 of the
 * License, or (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
 * General Public License for more details.
 *
 * You should have received a copy of the GNU General Public
 * License along with this program; if not, write to the
 * Free Software Foundation, Inc., 59 Temple Place - Suite 330,
 * Boston, MA 02111-1307, USA.
 */

using System;
using System.Collections;
using System.Threading;

namespace Muine
{
	public abstract class ThreadBase
	{
		protected bool thread_done = false;
		protected Thread thread;
		protected Queue queue;

		protected abstract void ThreadFunc ();
		protected abstract bool MainLoopIdle ();

		public ThreadBase ()
		{
			queue = Queue.Synchronized (new Queue ());

			GLib.IdleHandler idle = new GLib.IdleHandler (MainLoopIdle);
			GLib.Idle.Add (idle);

			thread = new Thread (new ThreadStart (ThreadFunc));
			thread.Priority = ThreadPriority.BelowNormal;
		}
	}
}
