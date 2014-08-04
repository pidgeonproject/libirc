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
    public class Configuration
    {
        public bool AggressiveMode = true;
        public bool AggressiveExceptions = false;
        public bool AggressiveBans = true;
        public bool AggressiveInvites = false;
        public bool AggressiveUsers = true;
        /// <summary>
        /// You can change this in case you want all mode data to be forwarded as raw IRC text after parsing,
        /// this is needed when you are using libirc for bouncers
        /// </summary>
        public bool ForwardModes = false;
        public string Nick = Defs.DefaultNick;
        public string Nick2 = null;
        public string GetNick2()
        {
            if (Nick2 == null)
            {
                Random rn = new Random(DateTime.Now.Millisecond);
                return Nick + rn.Next(1, 200).ToString();
            }
            return Nick2;
        }
    }
}

