/*
 * This file is part of SubLib.
 * Copyright (C) 2008 Pedro Castro
 *
 * SubLib is free software; you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation; either version 2 of the License, or
 * (at your option) any later version.
 *
 * SubLib is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with this program; if not, write to the Free Software
 * Foundation, Inc., 51 Franklin St, Fifth Floor, Boston, MA  02110-1301  USA
 */

using System;

namespace SubLib.Core.Domain
{

    public class Timing : IComparable
    {
        private double frame = 0;
        private TimeSpan time = TimeSpan.Zero;

        public Timing(int frame, TimeSpan time)
        {
            this.frame = frame;
            this.time = time;
        }

        public int Frame
        {
            get { return Convert.ToInt32(frame); }
        }

        public TimeSpan Time
        {
            get { return time; }
        }

        public int CompareTo(object obj)
        {
            if (!(obj is Timing))
                throw new ArgumentException("Object is not of class Timing");

            return time.CompareTo((obj as Timing).Time);
        }



    }

}
