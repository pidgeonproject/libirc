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
    /// <summary>
    /// Low level object with basic IRC user info
    /// </summary>
    public class UserInfo : Target
    {
        public string Nick;
        public string Ident;
        public string Host;
        public override string TargetName
        {
            get
            {
                return this.Nick;
            }
        }

        public UserInfo()
        {
            this.Nick = null;
            this.Ident = null;
            this.Host = null;
        }

        public UserInfo(string nick, string ident, string host)
        {
            this.Nick = nick;
            this.Ident = ident;
            this.Host = host;
        }

        public UserInfo(string source)
        {
            if (source.Contains("!"))
            {
                this.Nick = source.Substring(0, source.LastIndexOf("!", StringComparison.Ordinal));
                this.Ident = source.Substring(source.LastIndexOf("!") + 1);
                if (this.Ident.Contains("@"))
                {
                    this.Host = this.Ident.Substring(this.Ident.LastIndexOf("@") + 1);
                    this.Ident = this.Ident.Substring(0, this.Ident.LastIndexOf("@"));
                }
            }
            else
            {
                this.Nick = source;
            }
        }
    }
}

