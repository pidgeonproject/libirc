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
using System.Text;

namespace libirc
{
	public partial class ProcessorIRC
	{
		/// <summary>
		/// Network
		/// </summary>
		private Network _Network = null;
		/// <summary>
		/// Protocol of this network
		/// </summary>
		private Protocol _Protocol = null;
		private string text;
		private long date = 0;
		private bool updated_text = true;
		private bool isServices = false;
		/// <summary>
		/// If true the information is considered to be a backlog from irc bouncer and will not be processed
		/// in some special parts of irc protocol like part or join
		/// </summary>
		public bool IsBacklog = false;
		/// <summary>
		/// Time
		/// </summary>
		public DateTime pong;

		private void Ping()
		{
			pong = DateTime.Now;
			return;
		}

		private bool Pong(string source, string parameters, string _value)
		{
			_Network.Transfer("PONG :" + _value, Defs.Priority.Low);
			return true;
		}

		/// <summary>
		/// Process a data that affect the current user
		/// </summary>
		/// <param name="source"></param>
		/// <param name="_data2"></param>
		/// <param name="_value"></param>
		/// <returns></returns>
		private bool ProcessThis(string source, string[] _data2, string _value)
		{
			if (source.StartsWith(_Network.Nickname + "!", StringComparison.Ordinal))
			{
				if (_data2.Length > 1)
				{
					if (_data2[1].Contains("JOIN"))
					{
						string channel = null;
						if (IsBacklog)
						{
							return true;
						}
						if (!updated_text)
						{
							return true;
						}
						if (_data2.Length > 2)
						{
							if (!string.IsNullOrEmpty(_data2[2]))
							{
								channel = _data2[2];
							}
						}
						if (channel == null)
						{
							channel = _value;
						}
						Channel curr = _Network.GetChannel(channel);
						if (curr == null)
						{
							curr = _Network.Channel(channel);
						} else
						{
							curr.ChannelWork = true;
							curr.partRequested = false;
						}
						if (updated_text)
						{
							if (_Network.Config.AggressiveMode)
							{
								_Network.Transfer("MODE " + channel, Defs.Priority.Low);
							}

							if (_Network.Config.AggressiveExceptions)
							{
								curr.IsParsingExceptionData = true;
								_Network.Transfer("MODE " + channel + " +e", Defs.Priority.Low);
							}

							if (_Network.Config.AggressiveBans)
							{
								curr.IsParsingBanData = true;
								_Network.Transfer("MODE " + channel + " +b", Defs.Priority.Low);
							}

							if (_Network.Config.AggressiveInvites)
							{
								_Network.Transfer("MODE " + channel + " +I", Defs.Priority.Low);
							}

							if (_Network.Config.AggressiveUsers)
							{
								curr.IsParsingWhoData = true;
								_Network.Transfer("WHO " + channel, Defs.Priority.Low);
							}
						}
						return true;
					}
				}

				if (_data2.Length > 1)
				{
					if (_data2[1].Contains("NICK"))
					{
						string _new = _value;
						if (string.IsNullOrEmpty(_value) && _data2.Length > 2 && !string.IsNullOrEmpty(_data2[2]))
						{
							// server is fucked
							_new = _data2[2];
							// server is totally borked
							if (_new.Contains(" "))
							{
								_new = _new.Substring(0, _new.IndexOf(" ", StringComparison.Ordinal));
							}
						}
						//! TODO: insert event
						_Network.Nickname = _new;
					}
					if (_data2[1].Contains("PART"))
					{
						string channel = _data2[2];
						if (_data2[2].Contains(_Network.ChannelPrefix))
						{
							channel = _data2[2];
							Channel c = _Network.GetChannel(channel);
							if (c != null)
							{
								c.ChannelWork = false;
								if (!c.partRequested)
								{
									c.ChannelWork = false;
								}
							}
						}
						return true;
					}
				}
			}
			return false;
		}

		/// <summary>
		/// Process the line
		/// </summary>
		/// <returns></returns>
		public bool ProfiledResult()
		{
			if (Defs.UsingProfiler)
			{
				Profiler profiler = new Profiler("IRC.ProfiledResult()");
				bool result = Result();
				profiler.Done();
				return result;
			}
			return Result();
		}

		private bool Result()
		{
			string last = "";
			bool OK = false;
			if (text == null)
			{
				return false;
			}
			if (text.StartsWith(":", StringComparison.Ordinal))
			{
				last = text;
				string command = "";
				string parameters = "";
				string value = "";
				string command_body = text.Substring(1);
				string source = command_body;
				if (command_body.Contains(" :"))
				{
					if (command_body.IndexOf(" :", StringComparison.Ordinal) < 0)
					{
						_Protocol.DebugLog("Malformed text, probably hacker: " + text);
						return false;
					}
					command_body = command_body.Substring(0, command_body.IndexOf(" :", StringComparison.Ordinal));
				}
				source = source.Substring(0, source.IndexOf(" ", StringComparison.Ordinal));
				if (command_body.Length < source.Length + 1)
				{
					_Protocol.DebugLog("Invalid IRC string: " + text);
				}
				string command2 = command_body.Substring(source.Length + 1);

				if (command2.Contains(" "))
				{
					command = command2.Substring(0, command2.IndexOf(" ", StringComparison.Ordinal));
					if (command2.Length > 1 + command.Length)
					{
						parameters = command2.Substring(1 + command.Length);
						if (parameters.EndsWith(" ", StringComparison.Ordinal))
						{
							parameters = parameters.Substring(0, parameters.Length - 1);
						}
					}
				} else
				{
					command = command2;
				}

				if (text.Length > (3 + command2.Length + source.Length))
				{
					value = text.Substring(3 + command2.Length + source.Length);
				}

				if (value.StartsWith(":", StringComparison.Ordinal))
				{
					value = value.Substring(1);
				}

				string[] code = command_body.Split(' ');

				if (ProcessThis(source, code, value))
				{
					OK = true;
				}

				switch (command)
				{
					case "001":
					case "002":
					case "003":
					case "004":
						//Hooks._Network.NetworkInfo(_Network, command, parameters, value);
						break;
					case "005":
						Info(command, parameters, value);
						//Hooks._Network.NetworkInfo(_Network, command, parameters, value);
						if (!_Network.IsLoaded)
						{
							//Hooks._Network.AfterConnectToNetwork(_Network);
						}
						break;
					case "301":
						if (Idle2(command, parameters, value))
						{
							return true;
						}
						break;
					case "305":
						_Network.IsAway = false;
						break;
					case "306":
						_Network.IsAway = true;
						break;
					case "317":
							if (IdleTime(command, parameters, value))
							{
								return true;
							}
						break;
					case "321":
						if (_Network.SuppressData)
						{
							return true;
						}
						break;
					case "322":
						if (ChannelData(command, parameters, value))
						{
							return true;
						}
						break;
					case "323":
						if (_Network.SuppressData)
						{
							_Network.SuppressData = false;
							return true;
						}
						_Network.DownloadingList = false;
						break;
					case "307":
					case "310":
					case "313":
					case "378":
					case "671":
							if (WhoisText(command, parameters, value))
							{
								return true;
							}
						break;
					case "311":
							if (WhoisLoad(command, parameters, value))
							{
								return true;
							}
						break;
					case "312":
							if (WhoisSv(command, parameters, value))
							{
								return true;
							}
						break;
					case "318":
							if (WhoisFn(command, parameters, value))
							{
								return true;
							}
						break;
					case "319":
							if (WhoisCh(command, parameters, value))
							{
								return true;
							}
						break;
					case "433":
						if (!_Network.UsingNick2)
						{
							string nick = _Network.Config.GetNick2();
							_Network.UsingNick2 = true;
							_Network.Transfer("NICK " + nick, Defs.Priority.High);
							_Network.Nickname = nick;
						}
						break;
					case "PING":
						if (Pong(command, parameters, value))
						{
							return true;
						}
						break;
					case "PONG":
						Ping();
						return true;
					case "INFO":
						//_Network.SystemWindow.scrollback.InsertText(text.Substring(text.IndexOf("INFO", StringComparison.Ordinal) + 5), Pidgeon.ContentLine.MessageStyle.User,						                                             true, date, !updated_text);
						return true;
					case "NOTICE":
						if (parameters.Contains(_Network.ChannelPrefix))
						{
							Channel channel = _Network.GetChannel(parameters);
							if (channel != null)
							{
								//Graphics.Window window;
								//window = channel.RetrieveWindow();
								//if (window != null)
								{
								//	window.scrollback.InsertText("[" + source + "] " + value, Pidgeon.ContentLine.MessageStyle.Message, true, date, !updated_text);
								//	return true;
								}
							}
						}
						//_Network.SystemWindow.scrollback.InsertText("[" + source + "] " + value, Pidgeon.ContentLine.MessageStyle.Message, true, date, !updated_text);
						return true;
					case "NICK":
						if (ProcessNick(source, parameters, value))
						{
							return true;
						}
						break;
					case "INVITE":
						if (Invite(source, parameters, value))
						{
							return true;
						}
						break;
					case "PRIVMSG":
						if (ProcessPM(source, parameters, value))
						{
							return true;
						}
						break;
					case "TOPIC":
						if (Topic(source, parameters, value))
						{
							return true;
						}
						break;
					case "MODE":
						if (Mode(source, parameters, value))
						{
							return true;
						}
						break;
					case "PART":
						if (Part(source, parameters, value))
						{
							return true;
						}
						break;
					case "QUIT":
						if (Quit(source, parameters, value))
						{
							return true;
						}
						break;
					case "JOIN":
						if (Join(source, parameters, value))
						{
							return true;
						}
						break;
					case "KICK":
						if (Kick(source, parameters, value))
						{
							return true;
						}
						break;
				}

				if (command_body.Contains(" "))
				{
					switch (command)
					{
						case "315":
							if (FinishChan(code))
							{
								return true;
							}
							break;
						case "324":
							if (ChannelInfo(code, command, source, parameters, value))
							{
								return true;
							}
							break;
						case "332":
							if (ChannelTopic(code, command, source, parameters, value))
							{
								return true;
							}
							break;
						case "333":
							if (TopicInfo(code, parameters))
							{
								return true;
							}
							break;
						case "352":
							if (ParseUser(code, value))
							{
								return true;
							}
							break;
						case "353":
							if (ParseInfo(code, value))
							{
								return true;
							}
							break;
						case "366":
							return true;
						case "367":
							if (ChannelBans(code))
							{
								return true;
							}
							break;
						case "368":
							if (ChannelBans2(code))
							{
								return true;
							}
							break;
					}
				}
			} else
			{
				// malformed requests this needs to exist so that it works with some broked ircd
				string command = text;
				string value = "";
				if (command.Contains(" :"))
				{
					value = command.Substring(command.IndexOf(" :", StringComparison.Ordinal) + 2);
					command = command.Substring(0, command.IndexOf(" :", StringComparison.Ordinal));
				}
				// for extra borked ircd
				if (command.Contains(" "))
				{
					command = command.Substring(0, command.IndexOf(" ", StringComparison.Ordinal));
				}

				switch (command)
				{
					case "PING":
						Pong(command, null, value);
						OK = true;
						break;
				}
			}
			if (!OK)
			{
				// we have no idea what we just were to parse, so print it to system window
				//_Network.SystemWindow.scrollback.InsertText(text, Pidgeon.ContentLine.MessageStyle.System, true, date, true);
			}
			return true;
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="_network"></param>
		/// <param name="_text"></param>
		/// <param name="_pong"></param>
		/// <param name="_date">Date of this message, if you specify 0 the current time will be used</param>
		/// <param name="updated">If true this text will be considered as newly obtained information</param>
		public ProcessorIRC(Network _network, string _text, ref DateTime _pong, long _date = 0, bool updated = true)
		{
			_Network = _network;
			_Protocol = _network._Protocol;
			text = _text;
			pong = _pong;
			date = _date;
			updated_text = updated;
			if (_network._Protocol.GetType() == typeof(Protocols.ProtocolSv))
			{
				isServices = true;
			}
		}
	}
}
