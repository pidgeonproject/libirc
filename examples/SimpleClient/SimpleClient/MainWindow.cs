using System;
using Gtk;

public partial class MainWindow: Gtk.Window
{
    libirc.Protocols.ProtocolIrc IRC = null;

    public MainWindow(): base (Gtk.WindowType.Toplevel)
    {
        Build();
		// override some defaults
		libirc.Defs.DefaultQuit = "libirc test client. See http://pidgeonclient.org/";
		libirc.Defs.DefaultVersion = "test client v 1.0";
        this.textview1.KeyPressEvent += new KeyPressEventHandler(Process);
    }

    private void Write(string text)
    {
        this.textview2.Buffer.Text += DateTime.Now.ToString() + ": " + text + "\n";
    }

    public void Debug(object sender, libirc.IProtocol.DebugLogEventArgs args)
    {
        Write("D: " + args.Message);
    }

    [GLib.ConnectBefore]
    private void Process(object sender, Gtk.KeyPressEventArgs keys)
    {
        try
        {
            if (keys.Event.KeyValue == 65293)
            {
                keys.RetVal = true;
                string command = this.textview1.Buffer.Text;
                if (IRC == null && !command.StartsWith("/"))
                {
                    Write("Not connected");
                } else if (command.StartsWith("/"))
                {
                    command = command.Substring(1);
                    string parameters = command;
                    if (command.Contains(" "))
                    {
                        parameters = parameters.Substring(parameters.IndexOf(" ") + 1);
                        command = command.Substring(0, command.IndexOf(" "));
                    }
                    command = command.ToLower();
                    switch (command)
                    {
                        case "server":
                            Write("Connecting to " + parameters);
                            IRC = new libirc.Protocols.ProtocolIrc();
                            IRC.Server = parameters;
                            IRC.DebugLogEvent += new libirc.IProtocol.DebugLogEventHandler(Debug);
							IRC.IRCNetwork = new libirc.Network(parameters, IRC);
							IRC.IRCNetwork.Nickname = "test_user";
							IRC.IRCNetwork.UserName = "Test user of libirc";
                            IRC.Open();
                            return;
						case "join":
							IRC.Join(parameters);
							return;
                    }
                    Write("Unknown command");
                }
				if (IRC != null)
				{
					if (IRC.IRCNetwork.IsConnected)
					{
						Write(IRC.IRCNetwork.Nickname + ": " + command);
						IRC.Message(command, "#test");
					} else
					{
						Write("Not connected");
					}
				}
                textview1.Buffer.Clear();
            }
        } catch (Exception fail)
        {
            Write(fail.ToString());
        }
    }

    protected void OnDeleteEvent(object sender, DeleteEventArgs a)
    {
        Application.Quit();
        a.RetVal = true;
    }
}
