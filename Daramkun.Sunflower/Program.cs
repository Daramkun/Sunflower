using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using Daramkun.Sunflower.Properties;
using NativeWifi;

namespace Daramkun.Sunflower
{
	static class Program
	{
		/// <summary>
		/// 해당 응용 프로그램의 주 진입점입니다.
		/// </summary>
		[STAThread]
		static void Main ()
		{
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
			trayMenu.Items.Add ( "&About" );
			trayMenu.Items.Add ( "E&xit" );
			trayMenu.Items [ 0 ].Click += ( sender, e ) => { aboutBox.Show (); };
			trayMenu.Items [ 1 ].Click += ( sender, e ) => { trayIcon.Visible = false; };

			trayIcon.ContextMenuStrip = trayMenu;

			WlanClient wlanClient = new WlanClient ();
			WlanClient.WlanInterface [] interfaces = wlanClient.Interfaces;
			for ( int i = 0; i < interfaces.Length; i++ )
			{
				WlanClient.WlanInterface wlanInterface = interfaces [ i ];
				try
				{
					Console.WriteLine ( wlanInterface.InterfaceDescription );
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
		}
	}
}
