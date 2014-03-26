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
		public const string DefaultNick = "user";
		public const string DefaultQuit = "Libirc - http://github.com/pidgeonproject/libirc";
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

