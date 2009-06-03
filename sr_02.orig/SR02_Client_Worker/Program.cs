/*
 * Created by SharpDevelop.
 * User: thinkpad
 * Date: 2009-05-01
 * Time: 23:43
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */

using System;
using System.Windows.Forms;


namespace Client_Worker_Class
{
	/// <summary>
	/// Class with program entry point.
	/// </summary>
	internal sealed class Program
	{
        /// <summary>
        /// Program entry point.
        /// </summary>
        /// <param name="args">The args.</param>
		[STAThread]
		private static void Main(string[] args)
		{
			Control.CheckForIllegalCrossThreadCalls = false;
			Application.EnableVisualStyles();
			Application.SetCompatibleTextRenderingDefault(false);
			Application.Run(new MainForm());
		}
		
	}
}
