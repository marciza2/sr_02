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
        public double GetDutchStartValue(int Len, UInt32 Count)
        {
            double max = 0.0;
            foreach (ServerStatItem ssItem in Items)
                if (ssItem.Len == Len && ssItem.Time > max)
                    max = ssItem.Time;
            max *= Count;
            max *= 0.33;
            return max;
        }
        public void AddItem(MsgContent_InitData_Ans msgConInitDataAns, ClientInfo clInfo)
        {
            Items.Add(new ServerStatItem(msgConInitDataAns.Len, msgConInitDataAns.Time, clInfo));
        }
    }
}
