/*
 * Created by SharpDevelop.
 * User: thinkpad
 * Date: 2009-05-02
 * Time: 17:41
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */

using System;
using System.Net;
using System.Net.Sockets;

namespace Server_Class
{
	/// <summary>
	/// Description of ClientInfo.
	/// </summary>
	public class ClientInfo
	{
		public LastStat statistics;
		public TcpClient tcpClient;	
		public ClientInfo(TcpClient tcpc)
		{
			statistics = new LastStat();
			tcpClient = tcpc;
		}
	}
}
