/*
 * Copyright (C) 2004 Jorn Baayen <jorn@nl.linux.org>
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

#include "gtk-glue.h"

void
gtk_glue_get_monitor_dimensions (GdkScreen *screen,
				 int x, int y,
				 int *monitor_x,
				 int *monitor_y,
				 int *monitor_width,
				 int *monitor_height)
{
	GdkRectangle rect;
	int monitor;

	monitor = gdk_screen_get_monitor_at_point (screen, x, y);
	gdk_screen_get_monitor_geometry (screen, monitor, &rect);

	*monitor_x = rect.x;
	*monitor_y = rect.y;
	*monitor_width = rect.width;
	*monitor_height = rect.height;
}
