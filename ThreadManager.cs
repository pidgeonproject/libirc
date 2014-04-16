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
using System.Collections.Generic;
using System.Threading;

namespace libirc
{
    /// <summary>
    /// Information about all threads that are produced by this library
    /// </summary>
    public class ThreadManager
    {
        private static List<Thread> Threads = new List<Thread>();

        /// <summary>
        /// List of all threads that were created by this library, this list can't be modified
        /// </summary>
        public static List<Thread> ThreadList
        {
            get
            {
                return new List<Thread>(Threads);
            }
        }

        public static void RemoveThread(Thread thread)
        {
            lock(Threads)
            {
                if (Threads.Contains(thread))
                {
                    Threads.Remove(thread);
                }
            }
        }

        public static void RegisterThread(Thread thread)
        {
            lock (Threads)
            {
                if (!Threads.Contains(thread))
                {
                    Threads.Add(thread);
                }
            }
        }

        public static void KillThread(Thread thread)
        {
            if (thread == null)
            {
                return;
            }
            if (Thread.CurrentThread != thread)
            {
                if (thread.ThreadState == ThreadState.WaitSleepJoin &&
                    thread.ThreadState == ThreadState.Running)
                {
                    thread.Abort();
                }
            }
            RemoveThread(thread);
        }
    }
}

