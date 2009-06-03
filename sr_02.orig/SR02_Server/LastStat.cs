/*
 * Created by SharpDevelop.
 * User: thinkpad
 * Date: 2009-05-02
 * Time: 17:37
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */

using System;
using System.Collections.Generic;

namespace Server_Class
{
	/// <summary>
	/// Description of Statistics.
	/// </summary>
	public class LastStat
	{

        /// <summary>
        /// Calculation time.
        /// </summary>
		public double calcTime;
        /// <summary>
        /// Initializes a new instance of the <see cref="LastStat"/> class.
        /// </summary>
		public LastStat()
		{
			calcTime = -1;
		}
	}
}
