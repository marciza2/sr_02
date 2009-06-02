using System;
using System.Collections.Generic;
using System.Text;

namespace Server_Class
{
    class WorkGenerationMode
    {
        public enum WGM
        {
            WGMConst,
            WGMRandom,
            WGMSinus
        }
        WGM wgm = WGM.WGMConst;
        private UInt32 packSize = 262144;
        private UInt32 packSizeStart = 1000;
        private UInt32 packSizeStop = 300000;
        private UInt32 packSizeStep = 1000;
        private UInt32 packSizeSinStep = 1;
        private int packDelay = 10;
        private UInt32 [] sinArray;
        public WorkGenerationMode(WGM w)
        {
            wgm = w;
            int points = 100;
            sinArray = new UInt32[points];
            for(int i=0; i<points; i++)
            {
                double p = Math.PI/(double)points * (double)i;
                p = 0.9 * Math.Sin(p) + 0.1;
                sinArray[i] = (UInt32)(p*packSizeStop);
            }
            switch (wgm)
            {
                case WGM.WGMConst:

                    break;
                case WGM.WGMRandom:
        
                    break;
                case WGM.WGMSinus:
                    
                    break;
            }
        }
        public UInt32 GetPackSize()
        {
            switch (wgm)
            {
                case WGM.WGMConst:
                    return packSize;
                case WGM.WGMRandom:
                    Random random = new Random();
                    return (UInt32)((0.9 * random.NextDouble() + 0.1) * packSizeStop);
                case WGM.WGMSinus:
                    packSizeStep %= (UInt32)sinArray.Length;
                    return sinArray[packSizeStep++];
                default:
                    return 1;
            }
        }
        public int GetDelay()
        {
            switch (wgm)
            {
                case WGM.WGMConst:
                    return 10;
                case WGM.WGMRandom:
                    return packDelay;
                case WGM.WGMSinus:
                    return packDelay;
                default:
                    return 1;
            }
        }
    }
}
