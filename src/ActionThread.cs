/*
 * Copyright © 2004 Jorn Baayen <jorn@nl.linux.org>
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
using System.Threading;
using System.Collections;

public class Action
{
	public object UserData0;
	public object UserData1;

	public void EmitPerform ()
	{
		if (Perform != null)
			Perform (this);
	}

	public delegate void PerformHandler (Action action);
	public event PerformHandler Perform;
}

public class ActionThread
{
	private Thread thread;
	private Queue queue;
	
	public ActionThread ()
	{
		queue = Queue.Synchronized (new Queue ());
		thread = new Thread (new ThreadStart (RunThread));
		thread.Start ();
	}

	private void RunThread ()
	{
		while (queue.Count > 0) {
			Action action = (Action) queue.Dequeue ();

			action.EmitPerform ();
		}

		Thread.Sleep (1000);
	}

	public void QueueAction (Action action)
	{
		queue.Enqueue (action);
	}
}
