/*
 * Copyright (C) 2004 Jorn Baayen <jbaayen@gnome.org>
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
using System.Reflection;
using System.IO;

using Mono.Posix;

using MuinePluginLib;

namespace Muine
{
	public class PluginManager
	{
		private IPlayer player;
		
		private void ScanAssemblyForPlugins (Assembly assembly)
		{
			foreach (Type t in assembly.GetTypes ()) {
				if (t.IsSubclassOf (typeof (Plugin)) && !t.IsAbstract) {
					Plugin plugin = (Plugin) Activator.CreateInstance (t);

					plugin.Initialize (player);
				}
			}
		}

		private void FindAssemblies (string dir)
		{
			if (dir == null || dir == "")
				return;

			DirectoryInfo info = new DirectoryInfo (dir);
			if (!info.Exists)
				return;

			foreach (FileInfo file in info.GetFiles ()) {
				if (file.Extension == ".dll") {
					try {
						Assembly a = Assembly.LoadFrom (file.FullName);

						ScanAssemblyForPlugins (a);
					} catch (Exception e) {
						Console.WriteLine (Catalog.GetString ("Error loading plug-in {0}: {1}"), file.Name, e.Message);
					}
				}
			}
		}

		public PluginManager (IPlayer player)
		{
			this.player = player;

			string path = Environment.GetEnvironmentVariable ("MUINE_PLUGIN_PATH");

			if (path != null)
				foreach (string dir in path.Split (':'))
					FindAssemblies (dir);

			FindAssemblies (FileUtils.SystemPluginDirectory);
			FindAssemblies (FileUtils.UserPluginDirectory);
		}
	}
}
