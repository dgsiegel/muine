/* -*- mode: C; c-file-style: "gnu" -*- */
/*
 * Copyright (C) 2003 Richard Hult <richard@imendio.com>
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

#include <config.h>
#include <string.h>
#include <gdk/gdkkeysyms.h>
#include <gtk/gtkwindow.h>
#include <gtk/gtkframe.h>
#include <gtk/gtkvbox.h>
#include <gtk/gtkvscale.h>
#include <gtk/gtklabel.h>
#include <gtk/gtkmain.h>
#include <gtk/gtkimage.h>
#include <gtk/gtkiconfactory.h>
#include "volume-button.h"

static void     volume_button_class_init    (VolumeButtonClass *klass);
static void     volume_button_init          (VolumeButton      *button);
static void     volume_button_finalize      (GObject           *object);
static gboolean scale_key_press_event_cb    (GtkWidget         *widget,
					     GdkEventKey       *event,
					     VolumeButton      *button);
static void     scale_value_changed_cb      (GtkWidget         *widget,
					     VolumeButton      *button);
static gboolean popup_button_press_event_cb (GtkWidget         *widget,
					     GdkEventButton    *event,
					     VolumeButton      *button);
static void     show_scale                  (VolumeButton      *button);
static void     hide_scale                  (VolumeButton      *button);
static void     toggled_cb                  (GtkWidget         *widget,
					     gpointer           user_button);
static gboolean scroll_event_cb             (GtkWidget         *widget,
					     GdkEventScroll    *event,
					     VolumeButton      *button);
static void     update_image                (VolumeButton      *button,
					     int                vol);

enum {
  VOLUME_CHANGED,
  LAST_SIGNAL
};

static GObjectClass *parent_class;
static guint signals[LAST_SIGNAL];

GType
volume_button_get_type (void)
{
  static GType type = 0;
	
  if (!type)
    {
      static const GTypeInfo info =
	{
	  sizeof (VolumeButtonClass),
	  NULL,           /* base_init */
	  NULL,           /* base_finalize */
	  (GClassInitFunc) volume_button_class_init,
	  NULL,           /* class_finalize */
	  NULL,           /* class_data */
	  sizeof (VolumeButton),
	  0,
	  (GInstanceInitFunc) volume_button_init,
	};

      type = g_type_register_static (GTK_TYPE_TOGGLE_BUTTON, "VolumeButton",
				     &info, 0);
    }

  return type;
}

static void
volume_button_class_init (VolumeButtonClass *klass)
{
  GObjectClass *object_class;

  parent_class = g_type_class_peek_parent (klass);
  object_class = (GObjectClass*) klass;

  object_class->finalize = volume_button_finalize;

  signals[VOLUME_CHANGED] =
    g_signal_new ("volume_changed",
		  G_TYPE_FROM_CLASS (klass),
		  G_SIGNAL_RUN_LAST,
		  0,
		  NULL, NULL,
		  g_cclosure_marshal_VOID__INT,
		  G_TYPE_NONE, 1, G_TYPE_INT);
}

static void
volume_button_init (VolumeButton *button)
{
  g_signal_connect (button,
		    "toggled",
		    G_CALLBACK (toggled_cb),
		    button);

  g_signal_connect (button,
		    "scroll_event",
		    G_CALLBACK (scroll_event_cb),
		    button);

  button->image = gtk_image_new_from_stock ("muine-volume-medium",
					    GTK_ICON_SIZE_LARGE_TOOLBAR);
  gtk_widget_show (button->image);

  gtk_container_add (GTK_CONTAINER (button), button->image);

  update_image (button, 0);
}

static void
volume_button_finalize (GObject *object)
{
  /*  VolumeButton *button = VOLUME_BUTTON (object);*/

  if (G_OBJECT_CLASS (parent_class)->finalize)
    G_OBJECT_CLASS (parent_class)->finalize (object);
}

GtkWidget *
volume_button_new (void)
{
  return g_object_new (TYPE_VOLUME_BUTTON, NULL);
}

static gboolean
scale_key_press_event_cb (GtkWidget   *widget,
			  GdkEventKey *event,
			  VolumeButton *button)
{
  switch (event->keyval)
    {
    case GDK_Escape:
      hide_scale (button);

      g_signal_emit (button, signals[VOLUME_CHANGED], 0,
		     button->revert_volume);
      
      return TRUE;
      
    case GDK_KP_Enter:
    case GDK_ISO_Enter:
    case GDK_3270_Enter:
    case GDK_Return:
    case GDK_space:
    case GDK_KP_Space:
      hide_scale (button);
      return TRUE;
      
    default:
      break;
    }
  
  return FALSE;
}

static void
scale_value_changed_cb (GtkWidget    *widget,
			VolumeButton *button)
{
  int vol;

  vol = gtk_range_get_value (GTK_RANGE (widget));
  vol = CLAMP (vol, 0, 100);

  button->volume = vol;
  update_image (button, vol);

  g_signal_emit (button, signals[VOLUME_CHANGED], 0, vol);
}

static gboolean
popup_button_press_event_cb (GtkWidget      *widget,
			     GdkEventButton *event,
			     VolumeButton   *button)
{
  if (button->popup)
    {
      hide_scale (button);
      return TRUE;
    }
  
  return FALSE;
}

static void
show_scale (VolumeButton *button)
{
  GtkWidget      *frame;
  GtkWidget      *box;
  GtkAdjustment  *adj;
  GtkWidget      *label;
  GtkRequisition  req;
  int             x, y;
  int             width, height;
  GdkGrabStatus   grabbed;
  
  button->popup = gtk_window_new (GTK_WINDOW_POPUP);
  gtk_window_set_screen (GTK_WINDOW (button->popup),
			 gtk_widget_get_screen (GTK_WIDGET (button)));

  button->revert_volume = button->volume;
  
  frame = gtk_frame_new (NULL);
  gtk_container_set_border_width (GTK_CONTAINER (frame), 0);
  gtk_frame_set_shadow_type (GTK_FRAME (frame), GTK_SHADOW_OUT);
  gtk_widget_show (frame);

  gtk_container_add (GTK_CONTAINER (button->popup), frame);
	
  box = gtk_vbox_new (FALSE, 0);
  gtk_widget_show (box);

  gtk_container_add (GTK_CONTAINER (frame), box);

  adj = GTK_ADJUSTMENT (gtk_adjustment_new (button->volume, 0, 100, 5, 10, 0));
  
  button->scale = gtk_vscale_new (adj);
  gtk_scale_set_draw_value (GTK_SCALE (button->scale), FALSE);
  gtk_range_set_update_policy (GTK_RANGE (button->scale), GTK_UPDATE_CONTINUOUS);
  gtk_range_set_inverted (GTK_RANGE (button->scale), TRUE);
  gtk_widget_show (button->scale);
	
  g_signal_connect (button->popup,
		    "button_press_event",
		    G_CALLBACK (popup_button_press_event_cb),
		    button);
  
  g_signal_connect (button->scale,
		    "key_press_event",
		    G_CALLBACK (scale_key_press_event_cb),
		    button);

  g_signal_connect (button->scale,
		    "value_changed",
		    G_CALLBACK (scale_value_changed_cb),
		    button);

  label = gtk_label_new ("+");	
  gtk_widget_show (label);
  gtk_box_pack_start (GTK_BOX (box), label, FALSE, TRUE, 0);
  
  label = gtk_label_new ("-");	
  gtk_widget_show (label);
  gtk_box_pack_end (GTK_BOX (box), label, FALSE, TRUE, 0);

  gtk_box_pack_start (GTK_BOX (box), button->scale, TRUE, TRUE, 0);
	
  /* Align the popup below the button. */
  gtk_widget_size_request (button->popup, &req);
	
  gdk_window_get_origin (GTK_BUTTON (button)->event_window, &x, &y);
  gdk_drawable_get_size (GTK_BUTTON (button)->event_window, &width, &height);

  req.width = MAX (req.width, width);
  
  x += (width - req.width) / 2;
  y += height;
  
  x = MAX (0, x);
  y = MAX (0, y);
  
  gtk_widget_set_size_request (button->scale, -1, 100);
  gtk_widget_set_size_request (button->popup, req.width, -1);
  
  gtk_window_move (GTK_WINDOW (button->popup), x, y);
  gtk_widget_show (button->popup);

  gtk_widget_grab_focus (button->popup);
  gtk_grab_add (button->popup);

  grabbed = gdk_pointer_grab (button->popup->window,
			      TRUE,
			      GDK_BUTTON_PRESS_MASK | GDK_BUTTON_RELEASE_MASK | GDK_POINTER_MOTION_MASK,
			      NULL, NULL,
			      GDK_CURRENT_TIME);
  
  if (grabbed == GDK_GRAB_SUCCESS)
    {
      grabbed = gdk_keyboard_grab (button->popup->window, TRUE, GDK_CURRENT_TIME);

      if (grabbed != GDK_GRAB_SUCCESS)
	{
	  gtk_grab_remove (button->popup);
	  gtk_widget_destroy (button->popup);
	  button->popup = NULL;
	}
    }
  else
    {
	  gtk_grab_remove (button->popup);
	  gtk_widget_destroy (button->popup);
	  button->popup = NULL;
    }
}

static void
hide_scale (VolumeButton *button)
{
  GtkToggleButton *toggle;

  if (button->popup)
    {
      gtk_grab_remove (button->scale);
      gdk_pointer_ungrab (GDK_CURRENT_TIME);		
      gdk_keyboard_ungrab (GDK_CURRENT_TIME);

      gtk_widget_destroy (GTK_WIDGET (button->popup));

      button->popup = NULL;
    }

  toggle = GTK_TOGGLE_BUTTON (button);
  if (gtk_toggle_button_get_active (toggle))
    gtk_toggle_button_set_active (toggle, FALSE);
}

static void
toggled_cb (GtkWidget *widget, gpointer user_button)
{
  GtkToggleButton *toggle;

  toggle = GTK_TOGGLE_BUTTON (widget);
  if (gtk_toggle_button_get_active (toggle))
    show_scale (VOLUME_BUTTON (widget));
  else
    hide_scale (VOLUME_BUTTON (widget));
}

static gboolean
scroll_event_cb (GtkWidget      *widget,
		 GdkEventScroll *event,
		 VolumeButton   *button)
{
  int vol;

  vol = button->volume;
    
  switch (event->direction)
    {
    case GDK_SCROLL_UP:
      vol += 10;
      break;

    case GDK_SCROLL_DOWN:
      vol -= 10;
      break;

    default:
      return FALSE;
    }

  vol = CLAMP (vol, 0, 100);

  button->volume = vol;
  update_image (button, vol);
  
  g_signal_emit (button, signals[VOLUME_CHANGED], 0, vol);
  
  return TRUE;
}

static void
update_image (VolumeButton *button, int vol)
{
  const char *id;

  if (vol <= 0)
    id = "muine-volume-zero";
  else if (vol <= 100 / 3)
    id = "muine-volume-min";
  else if (vol <= 2 * 100 / 3)
    id = "muine-volume-medium";
  else
    id = "muine-volume-max";

  gtk_image_set_from_stock (GTK_IMAGE (button->image), id, GTK_ICON_SIZE_LARGE_TOOLBAR);
}

void
volume_button_set_volume (VolumeButton *button, int vol)
{
  if (button->volume == vol)
    return;

  button->volume = vol;

  update_image (button, vol);
  
  g_signal_emit (button, signals[VOLUME_CHANGED], 0, vol);
}
