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

namespace libirc
{
    public class Defs
    {
        public static readonly Version Version = new Version(1, 0, 3);
        /// <summary>
        /// The default nick
        /// 
        /// Change this to nick you want to have as a default for every new instance
        /// of network
        /// </summary>
        public static string DefaultNick =      "user";
        public static string DefaultQuit =      "Libirc - http://github.com/pidgeonproject/libirc";
        public static string DefaultVersion =   "Libirc v. " + Version + ", see http://pidgeonclient.org/ for more information about this library. ";
        public static bool UsingProfiler =      false;
        public const int DefaultIRCPort =       6667;
        public const int DefaultSSLIRCPort =    6697;

        /// <summary>
        /// Convert a date to unix one
        /// </summary>
        /// <param name="time"></param>
        /// <returns></returns>
        public static double ConvertDateToUnix(DateTime time)
        {
            DateTime EPOCH = new System.DateTime(1970, 1, 1, 0, 0, 0, 0);
            TimeSpan span = (time - EPOCH);
            return span.TotalSeconds;
        }

        /// <summary>
        /// Convert a unix timestamp to human readable time
        /// </summary>
        /// <param name="time"></param>
        /// <returns></returns>
        public static string ConvertFromUNIXToStringSafe(string time)
        {
            try
            {
                if (time == null)
                {
                    return "Unable to read: null";
                }
                double unixtimestmp = double.Parse(time);
                return (new DateTime(1970, 1, 1, 0, 0, 0)).AddSeconds(unixtimestmp).ToString();
            }
            catch (Exception)
            {
                return "Unable to read: " + time;
            }
        }

        /// <summary>
        /// Convert a unix timestamp to human readable time
        /// </summary>
        /// <param name="time"></param>
        /// <returns></returns>
        public static string ConvertFromUNIXToString(string time)
        {
            if (time == null)
            {
                return null;
            }
            double unixtimestmp = double.Parse(time);
            return (new DateTime(1970, 1, 1, 0, 0, 0)).AddSeconds(unixtimestmp).ToString();
        }

        /// <summary>
        /// Return a DateTime object from unix time
        /// </summary>
        /// <param name="time"></param>
        /// <returns></returns>
        public static DateTime ConvertFromUNIX(string time)
        {
            if (time == null)
            {
                throw new Exception("Provided time was NULL");
            }
            double unixtimestmp = double.Parse(time);
            return new DateTime(1970, 1, 1, 0, 0, 0).AddSeconds(unixtimestmp);
        }

        /// <summary>
        /// Return a DateTime object from unix time
        /// </summary>
        /// <param name="time"></param>
        /// <returns></returns>
        public static DateTime ConvertFromUNIX(double time)
        {
            return new DateTime(1970, 1, 1, 0, 0, 0).AddSeconds(time);
        }

        /// <summary>
        /// Priority of irc message
        /// </summary>
        public enum Priority
        {
            /// <summary>
            /// High
            /// </summary>
            High = 8,
            /// <summary>
            /// Normal
            /// </summary>
            Normal = 2,
            /// <summary>
            /// Low
            /// </summary>
            Low = 1,
            /// <summary>
            /// Lowest
            /// </summary>
            None = 0
        }
    }
}

