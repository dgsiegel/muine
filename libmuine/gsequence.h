/* GLIB - Library of useful routines for C programming
 * Copyright (C) 2002  Soeren Sandmann (sandmann@daimi.au.dk)
 *
 * This library is free software; you can redistribute it and/or
 * modify it under the terms of the GNU Lesser General Public
 * License as published by the Free Software Foundation; either
 * version 2 of the License, or (at your option) any later version.
 *
 * This library is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.	 See the GNU
 * Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public
 * License along with this library; if not, write to the
 * Free Software Foundation, Inc., 59 Temple Place - Suite 330,
 * Boston, MA 02111-1307, USA.
 */

#include <glib.h>

#ifndef __GSEQUENCE_H__
#define __GSEQUENCE_H__

typedef struct _GOldSequence      GOldSequence;
typedef struct _GOldSequenceNode *GOldSequencePtr;

/* GOldSequence */
GOldSequence *  g_old_sequence_new                (GDestroyNotify           data_destroy);
void         g_old_sequence_free               (GOldSequence               *seq);
void         g_old_sequence_sort               (GOldSequence               *seq,
					    GCompareDataFunc         cmp_func,
					    gpointer                 cmp_data);
GOldSequencePtr g_old_sequence_append             (GOldSequence               *seq,
					    gpointer                 data);
GOldSequencePtr g_old_sequence_prepend            (GOldSequence               *seq,
					    gpointer                 data);
GOldSequencePtr g_old_sequence_insert             (GOldSequencePtr             ptr,
					    gpointer                 data);
void         g_old_sequence_remove             (GOldSequencePtr             ptr);
GOldSequencePtr g_old_sequence_insert_sorted      (GOldSequence               *seq,
					    gpointer                 data,
					    GCompareDataFunc         cmp_func,
					    gpointer                 cmp_data);
void         g_old_sequence_insert_sequence    (GOldSequencePtr             ptr,
					    GOldSequence               *other_seq);
void         g_old_sequence_concatenate        (GOldSequence               *seq1,
					    GOldSequence               *seq);
void         g_old_sequence_remove_range       (GOldSequencePtr             begin,
					    GOldSequencePtr             end,
					    GOldSequence              **removed);
gint	     g_old_sequence_get_length         (GOldSequence               *seq);
GOldSequencePtr g_old_sequence_get_end_ptr        (GOldSequence               *seq);
GOldSequencePtr g_old_sequence_get_begin_ptr      (GOldSequence               *seq);
GOldSequencePtr g_old_sequence_get_ptr_at_pos     (GOldSequence               *seq,
					    gint                     pos);

/* GOldSequencePtr */
gboolean     g_old_sequence_ptr_is_end         (GOldSequencePtr             ptr);
gboolean     g_old_sequence_ptr_is_begin       (GOldSequencePtr             ptr);
gint         g_old_sequence_ptr_get_position   (GOldSequencePtr             ptr);
GOldSequencePtr g_old_sequence_ptr_next           (GOldSequencePtr             ptr);
GOldSequencePtr g_old_sequence_ptr_prev           (GOldSequencePtr             ptr);
GOldSequencePtr g_old_sequence_ptr_move           (GOldSequencePtr             ptr,
					    guint                    leap);
void         g_old_sequence_ptr_sort_changed   (GOldSequencePtr	     ptr,
					    GCompareDataFunc	     cmp_func,
					    gpointer		     cmp_data);
gpointer     g_old_sequence_ptr_get_data       (GOldSequencePtr             ptr);
void         g_old_sequence_ptr_set_data       (GOldSequencePtr             ptr,
					    gpointer                 data);
void         g_old_sequence_ptr_move_before    (GOldSequencePtr             ptr,
		                            GOldSequencePtr             before);

/* search */

/* return TRUE if you want to be called again with two
 * smaller segments
 */
typedef gboolean (* GOldSequenceSearchFunc) (GOldSequencePtr begin,
					  GOldSequencePtr end,
					  gpointer     data);

void         g_old_sequence_search             (GOldSequence               *seq,
					    GOldSequenceSearchFunc      f,
					    gpointer                 data);

/* debug */
gint         g_old_sequence_calc_tree_height   (GOldSequence               *seq);

#endif /* __GSEQUENCE_H__ */
