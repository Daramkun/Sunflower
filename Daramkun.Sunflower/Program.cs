using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows.Forms;
using Daramkun.Sunflower.Properties;
using Microsoft.Win32;
using NativeWifi;

namespace Daramkun.Sunflower
{
	static class Program
	{
		[DllImport ( "user32.dll" )]
		private extern static bool ExitWindowsEx ( uint uFlags, uint dwReason );

		static void Reboot ()
		{
			if ( MessageBox.Show ( "Are you doing reboot now?", "Notice", MessageBoxButtons.YesNo, MessageBoxIcon.Question ) == DialogResult.Yes )
			{
				if ( !ExitWindowsEx ( 0x00000002, 0 ) )
				{
					MessageBox.Show ( "Cannot reboot now. Please reboot manually.", "Notice", MessageBoxButtons.OK, MessageBoxIcon.Error );
				}
			}
			else MessageBox.Show ( "You need reboot for complete this action.", "Notice", MessageBoxButtons.OK, MessageBoxIcon.Information );
		}

		static void NoDelay ()
		{
			RegistryKey key = Registry.LocalMachine.CreateSubKey ( "SYSTEM\\CurrentControlSet\\Services\\TcpIp\\Parameters\\Interfaces" );
			foreach ( string name in key.GetSubKeyNames () )
			{
				RegistryKey interfaceKey = key.CreateSubKey ( name );
				interfaceKey.SetValue ( "TcpAckFrequency", 1 );
				interfaceKey.SetValue ( "TCPNoDelay", 1 );
			}
			key = Registry.LocalMachine.CreateSubKey ( "SOFTWARE\\Microsoft\\MSMQ\\Parameter" );
			key.SetValue ( "TCPNoDelay", 1 );

			Reboot ();
		}

		static void DefaultDelay ()
		{
			RegistryKey key = Registry.LocalMachine.CreateSubKey ( "SYSTEM\\CurrentControlSet\\Services\\TcpIp\\Parameters\\Interfaces" );
			foreach ( string name in key.GetSubKeyNames () )
			{
				RegistryKey interfaceKey = key.CreateSubKey ( name );
				interfaceKey.DeleteValue ( "TcpAckFrequency" );
				interfaceKey.DeleteValue ( "TCPNoDelay" );
			}
			key = Registry.LocalMachine.CreateSubKey ( "SOFTWARE\\Microsoft\\MSMQ\\Parameter" );
			key.DeleteValue ( "TCPNoDelay" );

			Reboot ();
		}

		[STAThread]
		static void Main ( string [] args )
		{
			if ( args.Length == 1 )
			{
				switch ( args [ 0 ] )
				{
					case "NoDelay":
						NoDelay ();
						break;
					case "DefaultDelay":
						DefaultDelay ();
						break;
				}

				return;
			}

			Application.EnableVisualStyles ();
			Application.SetCompatibleTextRenderingDefault ( true );

			if ( Environment.OSVersion.Platform != PlatformID.Win32NT )
			{ MessageBox.Show ( "This program only work on Windows NT System." ); return; }
			if ( Environment.OSVersion.Version.Major < 6 )
			{ MessageBox.Show ( "This program only work on Windows Vista or higher." ); return; }

			AboutBox aboutBox = new AboutBox ();

			NotifyIcon trayIcon = new NotifyIcon ();
			trayIcon.Text = "Daramkun's Sunflower";
			trayIcon.Icon = Resources.MainIcon;

			ContextMenuStrip trayMenu = new ContextMenuStrip ();
			trayMenu.Items.Add ( "Setting TCP &No Delay" );
			trayMenu.Items.Add ( "Setting TCP &Default Delay" );
			trayMenu.Items.Add ( "-" );
			trayMenu.Items.Add ( "&About" );
			trayMenu.Items.Add ( "E&xit" );
			trayMenu.Items [ 0 ].Click += ( sender, e ) =>
			{
				try
				{
					NoDelay ();
				}
				catch ( UnauthorizedAccessException )
				{
					var SelfProc = new ProcessStartInfo
					{
						UseShellExecute = true,
						WorkingDirectory = Environment.CurrentDirectory,
						FileName = Application.ExecutablePath,
						Arguments = "NoDelay",
						Verb = "runas"
					};
					try { Process.Start ( SelfProc ); }
					catch { MessageBox.Show ( "Unable to elevate!", "Notice" ); }
				}
			};
			trayMenu.Items [ 1 ].Click += ( sender, e ) =>
			{
				try
				{
					DefaultDelay ();
				}
				catch ( UnauthorizedAccessException )
				{
					var SelfProc = new ProcessStartInfo
					{
						UseShellExecute = true,
						WorkingDirectory = Environment.CurrentDirectory,
						FileName = Application.ExecutablePath,
						Arguments = "DefaultDelay",
						Verb = "runas"
					};
					try { Process.Start ( SelfProc ); }
					catch { MessageBox.Show ( "Unable to elevate!", "Notice" ); }
				}
			};
			trayMenu.Items [ 3 ].Click += ( sender, e ) => { aboutBox.Show (); };
			trayMenu.Items [ 4 ].Click += ( sender, e ) => { trayIcon.Visible = false; };

			trayIcon.ContextMenuStrip = trayMenu;

			WlanClient wlanClient = new WlanClient ();
			WlanClient.WlanInterface [] interfaces = wlanClient.Interfaces;
			Dictionary<WlanClient.WlanInterface, dynamic> settings = new Dictionary<WlanClient.WlanInterface, dynamic> ();
			
			foreach ( WlanClient.WlanInterface wlanInterface in interfaces )
			{
				try
				{
					Console.WriteLine ( wlanInterface.InterfaceDescription );
					settings.Add ( wlanInterface, new
					{
						MediaStreamingMode = wlanInterface.MediaStreamingMode,
						BackgroundScanEnabled = wlanInterface.BackgroundScanEnabled
					} );
					wlanInterface.MediaStreamingMode = true;
					wlanInterface.BackgroundScanEnabled = false;
					Debug.WriteLine ( "Leave this program running. Happy lag-free gaming!" );
				}
				catch
				{
					Debug.WriteLine ( "Sorry, didn't work!" );
					trayIcon.Icon = Resources.DisableIcon;
				}
			}

			trayIcon.Visible = true;

			while ( trayIcon.Visible ) Application.DoEvents ();

			trayIcon.ContextMenuStrip.Dispose ();
			trayIcon.Dispose ();

			foreach ( WlanClient.WlanInterface wlanInterface in interfaces )
			{
				try
				{
					wlanInterface.MediaStreamingMode = settings [ wlanInterface ].MediaStreamingMode;
					wlanInterface.BackgroundScanEnabled = settings [ wlanInterface ].BackgroundScanEnabled;
				}
				catch { }
			}
		}
	}
}
