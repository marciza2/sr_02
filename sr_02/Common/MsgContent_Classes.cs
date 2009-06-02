﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using System.Xml.Serialization;

namespace Server_Class
{
    [Serializable]
    public class MsgContent
    {
        public MsgContent() { }
    }
    public class MsgContent_InitData : MsgContent
    {
        public byte[] Hash;
        public int[] Test;
        public MsgContent_InitData() { }
        public MsgContent_InitData(byte[] Hash, int[] Test)
        {
            this.Hash = Hash;
            this.Test = Test;
        }
    }
    public class MsgContent_Data : MsgContent
    {
        public byte[] biStart;
        public UInt32 pack;
        public MsgContent_Data() { }
        public MsgContent_Data(byte [] bytes, UInt32 pack)
        {
            biStart = bytes;
            this.pack = pack;
        }
        public MsgContent_Data(BigInteger bi, UInt32 pack)
        {
            biStart = bi.getBytes();
            this.pack = pack;
        }
    }
    public class MsgContent_Data_To_Estimate : MsgContent
    {
        public int Len;
        public UInt32 Pack;
        public MsgContent_Data_To_Estimate(){}
        public MsgContent_Data_To_Estimate(int Len, UInt32 Pack)
        {
            this.Len = Len;
            this.Pack = Pack;
        }
    }
    public class MsgContent_InitData_Ans : MsgContent
    {
        public int Len;
        public double Time;
        public MsgContent_InitData_Ans() { }
        public MsgContent_InitData_Ans(int Len, double Time)
        {
            this.Len = Len;
            this.Time = Time;
        }
    }
    public class MsgContent_Dutch_Time_To_Do : MsgContent
    {
        public int Len;
        public UInt32 Pack;
        public double Time;
        public MsgContent_Dutch_Time_To_Do() { }
        public MsgContent_Dutch_Time_To_Do(int Len, UInt32 Pack, double Time)
        {
            this.Len = Len;
            this.Pack = Pack;
            this.Time = Time;
        }
    }

}