/* -*- mode: C; c-file-style: "gnu" -*- */
/*
 * Copyright (C) 2003 Richard Hult <richard@imendio.com>
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

#ifndef __VOLUME_BUTTON_H__
#define __VOLUME_BUTTON_H__

#include <gtk/gtktogglebutton.h>

#define TYPE_VOLUME_BUTTON            (volume_button_get_type ())
#define VOLUME_BUTTON(obj)            (G_TYPE_CHECK_INSTANCE_CAST ((obj), TYPE_VOLUME_BUTTON, VolumeButton))
#define VOLUME_BUTTON_CLASS(klass)    (G_TYPE_CHECK_CLASS_CAST ((klass), TYPE_VOLUME_BUTTON, VolumeButtonClass))
#define IS_VOLUME_BUTTON(obj)         (G_TYPE_CHECK_INSTANCE_TYPE ((obj), TYPE_VOLUME_BUTTON))
#define IS_VOLUME_BUTTON_CLASS(klass) (G_TYPE_CHECK_CLASS_TYPE ((klass), TYPE_VOLUME_BUTTON))
#define VOLUME_BUTTON_GET_CLASS(obj)  (G_TYPE_INSTANCE_GET_CLASS ((obj), TYPE_VOLUME_BUTTON, VolumeButtonClass))

typedef struct _VolumeButton      VolumeButton;
typedef struct _VolumeButtonClass VolumeButtonClass;

struct _VolumeButton
{
  GtkToggleButton parent;

  int volume;
  int revert_volume;

  GtkWidget *popup;
  GtkWidget *scale;
  GtkWidget *image;
};

struct _VolumeButtonClass
{
  GtkToggleButtonClass parent_class;
};

GType           volume_button_get_type       (void);
GtkWidget *     volume_button_new            (void);
void            volume_button_set_volume     (VolumeButton *button,
					      int           vol);


#endif /* __VOLUME_BUTTON_H__ */
