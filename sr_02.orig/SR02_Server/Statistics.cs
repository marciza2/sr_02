using System;
using System.Collections.Generic;
using System.Text;

namespace Server_Class
{
    class Statistics
    {
        public class ServerStatItem
        {
            public int Len;
            public double Time;
            public ClientInfo clInfo;
            ServerStatItem() { }
            public ServerStatItem(int Length, double Time, ClientInfo clInfo)
            {
                this.Len = Length;
                this.Time = Time;
                this.clInfo = clInfo;
            }
        }
        List<ServerStatItem> Items;
        public Statistics()
        {
            Items = new List<ServerStatItem>();
        }
        public void AddItem(MsgContent_InitData_Ans msgConInitDataAns, ClientInfo clInfo)
        {
            Items.Add(new ServerStatItem(msgConInitDataAns.Len, msgConInitDataAns.Time, clInfo));
        }
    }
}
