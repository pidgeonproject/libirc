//  This program is free software; you can redistribute it and/or modify
//  it under the terms of the GNU Lesser General Public License as published
//  by the Free Software Foundation; either version 2 of the License, or
//  (at your option) version 3.

//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU Lesser General Public License for more details.

//  You should have received a copy of the GNU Lesser General Public License
//  along with this program; if not, write to the
//  Free Software Foundation, Inc.,
//  51 Franklin Street, Fifth Floor, Boston, MA  02110-1301, USA.

using System;
using System.Diagnostics;

namespace libirc
{
    /// <summary>
    /// Profiler
    /// </summary>
    public class Profiler
    {
        /// <summary>
        /// Time
        /// </summary>
        private Stopwatch time = new Stopwatch();
        /// <summary>
        /// Function that is being checked
        /// </summary>
        public string Function = null;

        /// <summary>
        /// Creates a new instance with name of function
        /// </summary>
        /// <param name="function"></param>
        public Profiler(string function)
        {
            Function = function;
            time.Start();
        }

        /// <summary>
        /// Called when profiler is supposed to be stopped
        /// </summary>
        public string Done()
        {
            time.Stop();
            return ("PROFILER: " + Function + ": " + time.ElapsedMilliseconds.ToString());
        }
    }
}

