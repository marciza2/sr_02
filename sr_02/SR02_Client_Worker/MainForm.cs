/*
 * Created by SharpDevelop.
 * User: thinkpad
 * Date: 2009-05-01
 * Time: 23:43
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */

using System;
using System.Security.Cryptography;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using System.Threading;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.IO;
using System.Xml;
using System.Xml.Serialization;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters;
using System.Runtime.Serialization.Formatters.Binary;
using System.Diagnostics;
using Server_Class;
using ZedGraph;

namespace Client_Worker_Class
{
	/// <summary>
	/// Description of MainForm.
	/// </summary>
    public partial class MainForm : Form
	{
        BigInteger biData; // dane do obliczeń
        byte[] searchHash = { 0xdd, 0x4b, 0x21, 0xe9, 0xef, 0x71, 0xe1, 0x29, 0x11, 0x83, 0xa4, 0x6b, 0x91, 0x3a, 0xe6, 0xf2 };

        public readonly PerformanceCounter PCPUUsage;
        Thread listeningThread;
        Thread workingThread;
        /*Socket socket;
        private int size = 1024;*/
		TcpClient tcpClient;
		bool ThreadWorking = true;
		private byte[] data = new byte[1024];
        LineItem myCurve;
        private UInt32 pack;
        private double estTime;
        private bool finished = false;
		Random random = new Random();
        Statistics statistics;
        MD5CryptoServiceProvider md5;
        public MainForm()
		{
			//
			// The InitializeComponent() call is required for Windows Forms designer support.
			//
			InitializeComponent();
			
			//
			// TODO: Add constructor code after the InitializeComponent() call.
			//
            //Process.GetCurrentProcess().PriorityClass = ProcessPriorityClass.High;
            this.DoubleBuffered = true;
            zedGraphControl1.GraphPane.Title.Text = "Wykres wydajności obliczeniowej";
            zedGraphControl1.GraphPane.XAxis.Title.Text = "Obciążenie procesora [%]";
            zedGraphControl1.GraphPane.YAxis.Title.Text = "Czas wykonania jednej operacji [ms]";
            
            Thread.CurrentThread.Priority = ThreadPriority.Highest;

            statistics = new Statistics();
            md5 = new MD5CryptoServiceProvider();

            PCPUUsage = new PerformanceCounter();
            PCPUUsage.CategoryName = "Processor";
            PCPUUsage.CounterName = "% Processor Time";
            PCPUUsage.InstanceName = "_Total";

            dataGridView1.EditMode = DataGridViewEditMode.EditProgrammatically;
            //dataGridView1.Columns[0].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            dataGridView1.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            dataGridView1.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.None;
            dataGridView1.AllowUserToResizeColumns = false;
            dataGridView1.AllowUserToResizeRows = false;

            dataGridView1.ColumnHeadersVisible = false;
            dataGridView1.RowHeadersVisible = false;
            dataGridView1.MultiSelect = false;
            dataGridView1.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dataGridView1.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            //dataGridView1.RowHeadersWidthSizeMode = DataGridViewRowHeadersWidthSizeMode.DisableResizing;
            dataGridView1.Columns[0].SortMode = DataGridViewColumnSortMode.NotSortable;
            dataGridView1.Columns[1].SortMode = DataGridViewColumnSortMode.NotSortable;

            dataGridView1.Rows.Add(8);
            dataGridView1.GridColor = SystemColors.ButtonFace;
            dataGridView1.Rows[0].Cells[0].Value = "Ogólne";
            dataGridView1.Rows[0].Cells[0].Style.BackColor = SystemColors.ButtonFace;
            dataGridView1.Rows[0].Cells[1].Style.BackColor = SystemColors.ButtonFace;
            dataGridView1.Rows[1].Cells[0].Value = "Ilość procesorów/rdzeni:";
            dataGridView1.Rows[1].Cells[1].Value = Environment.ProcessorCount.ToString();
            dataGridView1.Rows[2].Cells[0].Value = "Obciążenie procesora [%]:";
            dataGridView1.Rows[2].Cells[1].Value = String.Format("{0:0.#}", (float)PCPUUsage.NextValue());
            dataGridView1.Rows[3].Cells[0].Value = "Statystyki";
            dataGridView1.Rows[3].Cells[0].Style.BackColor = SystemColors.ButtonFace;
            dataGridView1.Rows[3].Cells[1].Style.BackColor = SystemColors.ButtonFace;
            dataGridView1.Rows[4].Cells[0].Value = "Szybkość obliczeń [op/s]:";
            dataGridView1.Rows[4].Cells[1].Value = "N/A";
            dataGridView1.Rows[5].Cells[0].Value = "Start obliczeń:";
            dataGridView1.Rows[5].Cells[1].Value = "N/A";
            dataGridView1.Rows[6].Cells[0].Value = "Koniec obliczeń:";
            dataGridView1.Rows[6].Cells[1].Value = "N/A";
            dataGridView1.Rows[7].Cells[0].Value = "Analizowany hash:";
            dataGridView1.Rows[7].Cells[1].Value = "N/A";
            dataGridView1.Rows[8].Cells[0].Value = "Czas obliczeń:";
            dataGridView1.Rows[8].Cells[1].Value = "N/A";
        }
        string ByteArrayToHexString(byte[] array)
        {
            StringBuilder ret = new StringBuilder();
            for (int i = 0; i < array.Length; i++)
            {
                ret.Append(array[i].ToString("x2").ToUpper());
                ret.Append(" ");
            }
            return ret.ToString();
        }
		void Log(string txt)
		{
            richTextBox1.AppendText(txt + Environment.NewLine);
            if (richTextBox1.Lines.Length > 500)
            {
                string[] table = richTextBox1.Text.Split(new char[] { '\n' });
                richTextBox1.Text = String.Empty;
                for(int i=100; i<table.Length; i++)
                    richTextBox1.AppendText(table[i] + Environment.NewLine);
                table = null;
            }
		}
        bool ByteArrayCompare(byte[] ar1, byte[] ar2)
        {
            if (ar1.Length != ar2.Length)
                return false;
            for (int i = 0; i < ar1.Length; i++)
                if (ar1[i] != ar2[i])
                    return false;
            return true;
        }
        void workingFun()
        {
            BigInteger biEnd = biData + pack;
            float PUStart;
            float PUEnd;
            lock (this)
            {
                PUStart = (float)PCPUUsage.NextValue();
            }
            DateTime dtStart = DateTime.Now;
            while (biEnd > biData)
            {
                byte[] hash = md5.ComputeHash(biData.getBytes());
                bool bEqual = false;
                if (hash.Length == searchHash.Length)
                {
                    int i = 0;
                    while ((i < hash.Length) && (hash[i] == searchHash[i]))
                        i += 1;
                    if (i == hash.Length)
                        bEqual = true;
                }
                if (bEqual)
                {
                    StringBuilder ret = new StringBuilder();
                    ret.Append("Hash:\n");
                    for (int i = 0; i < hash.Length; i++)
                        ret.Append("0x" + hash[i].ToString("x2").ToLower() + ", ");
                    ret.Append("\nValue:\n");
                    for (int i = 0; i < biData.getBytes().Length; i++)
                        ret.Append(biData.getBytes()[i].ToString("x2").ToLower() + ", ");
                    lock (this)
                    {
                        NetworkStream clientStream = tcpClient.GetStream();
                        ComMessage ansMessage = new ComMessage(ComMessage.MsgTypes.SUCCESS);
                        ansMessage.SetContentObject(biData.getBytes());
                        byte[] buffer = ansMessage.Serialize();
                        clientStream.Write(buffer, 0, buffer.Length);
                        clientStream.Flush();
                    }
                    MessageBox.Show(bEqual.ToString() + "\n" + ret.ToString());
                }
                biData++;
            }
            DateTime dtEnd = DateTime.Now;
            lock (this)
            {
                PUEnd = (float)PCPUUsage.NextValue();
            }
            double time = (dtEnd - dtStart).TotalMilliseconds;
            //statistics.EditLastItem();
            statistics.AddItem(biData.getBytes().Length, (PUStart+PUEnd)/2.0f , (double)time / (double)pack);
            statistics.calcCount += pack;
            lock (this)
            {
                NetworkStream clientStream = tcpClient.GetStream();
                ComMessage ansMessage = new ComMessage(ComMessage.MsgTypes.RESULTS);
                MsgContent_InitData_Ans msgContent_InitData_Ans = new MsgContent_InitData_Ans(biData.getBytes().Length, (double)time / (double)pack);
                ansMessage.SetContentObject(msgContent_InitData_Ans);
                byte [] buffer = ansMessage.Serialize();
                clientStream.Write(buffer, 0, buffer.Length);
                clientStream.Flush();							
            }
        }
		void listeningFun()
		{
			while(ThreadWorking)
			{
				//socket.BeginReceive(data, 0, size, SocketFlags.None,
				//                   new AsyncCallback(ReceiveData), socket);
				NetworkStream clientStream = tcpClient.GetStream();
				byte[] recBuffer = new byte[4096];
				int bytesRead;
				while (true)
				{
					ComMessage recMessage = new ComMessage();
					bytesRead = 0;
					try
					{
						// blokuje wątek aż do odebrania wiad. od serwera
						bytesRead = clientStream.Read(recBuffer, 0, 4096);
					}
					catch(SerializationException ex)
					{
						MessageBox.Show(ex.Message, ex.ToString(), MessageBoxButtons.OK, MessageBoxIcon.Error);
						break;
					}
					catch(SocketException ex)
					{
						MessageBox.Show(ex.Message, ex.ToString(), MessageBoxButtons.OK, MessageBoxIcon.Error);
						break;
					}
					if (bytesRead == 0)
					{
						//the client has disconnected from the server
						Log("Client has disconnected");
						break;
					}
					MemoryStream ms = new MemoryStream(recBuffer, 0, bytesRead);
					ms.Position = 0;
                    System.Diagnostics.Debug.WriteLine(ms.Length.ToString());
                    System.Diagnostics.Debug.WriteLine(ms.ToArray().ToString());
                    XmlSerializer xmlSer = new XmlSerializer(typeof(ComMessage));
					System.Diagnostics.Debug.WriteLine(Encoding.ASCII.GetString(recBuffer, 0, bytesRead));
					System.Diagnostics.Debug.WriteLine("Odebrano " + bytesRead);
					recMessage = (ComMessage)xmlSer.Deserialize(ms);
					byte [] buffer;
                    Log(recMessage.header.ToString("G"));
                    Log(recMessage.txtMsg);
                    Log(recMessage.conversationID);
                    ComMessage ansMessage;
                    switch (recMessage.header)
					{
						case ComMessage.MsgTypes.INIT_DATA:
                            MsgContent_InitData initData = (MsgContent_InitData)recMessage.GetContentObject();
                            searchHash = initData.Hash;
                            dataGridView1.Rows[7].Cells[1].Value = ByteArrayToHexString(searchHash);
							// test - benchmark
                            System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();
                            float PUStart;
                            float PUEnd;            
                            for (int i = 0; i < initData.Test.Length; i++)
                            {
                                byte[] tmpArray = new byte[initData.Test[i]]; /* tworzy tymczasową tablicę do obliczeń */
                                random.NextBytes(tmpArray); /* wypełnia losowymi wartościami */
                                lock (this)
                                {
                                    PUStart = (float)PCPUUsage.NextValue();
                                }
                                sw.Start();
                                byte[] hash = md5.ComputeHash(tmpArray);
                                sw.Stop();
                                lock (this)
                                {
                                    PUEnd = (float)PCPUUsage.NextValue();
                                }
                                double time = ((double)sw.ElapsedTicks / (double)System.Diagnostics.Stopwatch.Frequency);
                                statistics.AddItem(initData.Test[i], (PUStart + PUEnd) / 2.0f, (double)time);
                            }
                            // wyświetlenie wyników benchmark
                            Log("Benchmark");
                            for (int i = 0; i < initData.Test.Length; i++)
                            {
                                Log("Czas: " + String.Format("{0:0.########}", statistics.GetItemTime(i)) + ", CPULoad: " + String.Format("{0:0.##}",statistics.GetItemCPULoad(i)));

                            }
                            if (recMessage.conversationID.Equals(CommunicationMode.CMDutchAuction.ToString()))
                            {
                                double avgTime = 0.0;
                                for(int i=0; i<statistics.GetItemsCount(); i++)
                                {
                                    avgTime += statistics.GetItemTime(i);
                                }
                                avgTime/=statistics.GetItemsCount();
                                ansMessage = new ComMessage(ComMessage.MsgTypes.INIT_DATA_ANS);
                                MsgContent_InitData_Ans msgContent_InitData_Ans = new MsgContent_InitData_Ans(statistics.GetItemLength(0), avgTime);
                                ansMessage.SetContentObject(msgContent_InitData_Ans);
                                buffer = ansMessage.Serialize();
                                clientStream.Write(buffer, 0, buffer.Length);
                                clientStream.Flush();							
                                // odpowiedź zawierająca wyniki testów
                            }
							break;
                        case ComMessage.MsgTypes.DATA_TO_ESTIMATE:
                            if (finished)
                            {
                                statistics = new Statistics();
                                timer1.Enabled = true;
                                finished = false;
                            }
                            MsgContent_Data_To_Estimate msgContent_Data_To_Estimate = (MsgContent_Data_To_Estimate)recMessage.GetContentObject();
                            // oblicz odeślij
                            ansMessage = new ComMessage(ComMessage.MsgTypes.TIME_ESTIMATED);
                            if (workingThread != null)
                            {
                                if (workingThread.IsAlive)
                                    estTime = -1;
                                else
                                    estTime = statistics.CalcTime(msgContent_Data_To_Estimate.Len, msgContent_Data_To_Estimate.Pack, PCPUUsage.NextValue());
                            }
                            else
                                estTime = random.Next(4000) + 1000;
                            ansMessage.SetContentObject((estTime));
                            ansMessage.txtMsg = "odp";
                            buffer = ansMessage.Serialize();
                            clientStream.Write(buffer, 0, buffer.Length);
                            clientStream.Flush();
                            break;
                        case ComMessage.MsgTypes.DUTCH_TIME_TO_DO:
                            MsgContent_Dutch_Time_To_Do msgContent_Dutch_Time_To_Do = (MsgContent_Dutch_Time_To_Do)recMessage.GetContentObject();
                            // oblicz odeślij
                            ansMessage = new ComMessage(ComMessage.MsgTypes.TIME_ESTIMATED);
                            if (workingThread != null)
                            {
                                if (workingThread.IsAlive)
                                    estTime = -1;
                                else
                                    estTime = statistics.CalcTime(msgContent_Dutch_Time_To_Do.Len, msgContent_Dutch_Time_To_Do.Pack, PCPUUsage.NextValue());
                            }
                            else
                                estTime = random.Next(4000) + 1000;
                            if(estTime == -1)
                                ansMessage.SetContentObject(-1.0);
                            else if(estTime<msgContent_Dutch_Time_To_Do.Time)
                                ansMessage.SetContentObject(1.0);
                            else if(estTime<msgContent_Dutch_Time_To_Do.Time+msgContent_Dutch_Time_To_Do.Time*0.1)
                                ansMessage.SetContentObject(1.0);
                            else
                                ansMessage.SetContentObject(0.0);
                            Log("Odpowiedz na DUTCH_TIME_TODO = " + (msgContent_Dutch_Time_To_Do.Time).ToString());
                            Log("Odpowiedz na DUTCH_TIME_TODO = " + estTime.ToString());
                            Log("Odpowiedz na DUTCH_TIME_TODO = " + ((double)ansMessage.GetContentObject()).ToString());
                            ansMessage.txtMsg = "odp";
                            buffer = ansMessage.Serialize();
                            clientStream.Write(buffer, 0, buffer.Length);
                            clientStream.Flush();
                            break;
                        case ComMessage.MsgTypes.GENERAL_MSG:
							break;
						case ComMessage.MsgTypes.DATA:
                            MsgContent_Data msgContent_Data = (MsgContent_Data)recMessage.GetContentObject();
                            biData = new BigInteger((byte[])msgContent_Data.biStart, true);
                            pack = msgContent_Data.pack;
                            workingThread = new Thread(new ThreadStart(workingFun));
                            workingThread.Priority = ThreadPriority.AboveNormal;
                            if (!statistics.startOnce)
                            {
                                statistics.startTime = DateTime.Now;
                                statistics.startOnce = true;
                                statistics.startTime = DateTime.Now;
                                dataGridView1.Rows[5].Cells[1].Value = statistics.startTime.ToString();

                            }
                            workingThread.Start();
                            //MessageBox.Show("PACKSIZE = " + msgContent_Data.pack.ToString());
                            break;
                        case ComMessage.MsgTypes.FINISH:
                            timer1.Enabled = false;
                            statistics.endTime = DateTime.Now;
                            dataGridView1.Rows[6].Cells[1].Value = statistics.endTime.ToString();
                            dataGridView1.Rows[8].Cells[1].Value = statistics.GetTime().ToString();
                            finished = true;
                            break;
                        default:
							break;
					}
				}
			}
		}

		
		void ButtonConnectClick(object sender, EventArgs e)
		{
			
            tcpClient = new TcpClient();
			IPEndPoint serverEndPoint = new IPEndPoint(IPAddress.Parse(textBoxAddress.Text), 2240);
			
			try{
				tcpClient.Connect(serverEndPoint);
				Log("Connected");
				listeningThread = new Thread(new ThreadStart(listeningFun));
				listeningThread.CurrentUICulture = new System.Globalization.CultureInfo("en-US");
				listeningThread.Start();
			}
			catch(Exception ex)
			{
				Log("Connection error " + ex.Message);
			}
		}
		void ReceiveData(IAsyncResult iar)
		{
			Socket remote = (Socket)iar.AsyncState;
			int recv = remote.EndReceive(iar);
			Log("odebrałem cos");
			//string stringData = Encoding.ASCII.GetString(data, 0, recv);
			//results.Items.Add(stringData);*/
		}		
		void MainFormFormClosing(object sender, FormClosingEventArgs e)
		{
			if(listeningThread != null)
				if(listeningThread.IsAlive)
				listeningThread.Abort();
		}


        private void button3_Click(object sender, EventArgs e)
        {
            MessageBox.Show(Environment.ProcessorCount.ToString());
        }

        private void DisconnectButton_Click(object sender, EventArgs e)
        {
            listeningThread.Abort();
            tcpClient.Close();
        }

        private void dataGridView1_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {

        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            dataGridView1.Rows[2].Cells[1].Value = String.Format("{0:0.#}", (float)PCPUUsage.NextValue());
            if (statistics.startOnce)
                dataGridView1.Rows[4].Cells[1].Value = String.Format("{0:0.##}", statistics.GetSpeed());

            if (statistics.GetItemsCount() < 4) return;
            GraphPane myPane = zedGraphControl1.GraphPane;
            
            PointPairList list = statistics.GetOptimizedPointPairList(statistics.LastLen, 3);
            if(myCurve!=null)
                myCurve.Clear();
            myCurve = myPane.AddCurve("",
                  list, Color.Red, SymbolType.Diamond);
            myCurve.Symbol.Fill = new Fill(Color.Red);
            myCurve.Line.Fill = new Fill(Color.White, Color.Red, 45F);
            
            zedGraphControl1.AxisChange();
            zedGraphControl1.Refresh();
        }

        private void dataGridView1_SelectionChanged(object sender, EventArgs e)
        {

        }

        private void button2_Click_1(object sender, EventArgs e)
        {
           /*System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();
            sw.Start();

            MD5CryptoServiceProvider md5 = new MD5CryptoServiceProvider();
            byte[] hash = System.Text.Encoding.ASCII.GetBytes("Ala ma kota");
            hash = md5.ComputeHash(hash);

            sw.Stop();
            string tmp = "";
            for (int i = 0; i < hash.Length; i++)
                tmp += hash[i].ToString("X2") + ":";

            MessageBox.Show(tmp);
            MessageBox.Show(sw.ElapsedTicks.ToString());
            //MessageBox.Show(System.Diagnostics.Stopwatch.Frequency.ToString());
            MessageBox.Show(sw.Elapsed.ToString());
            MessageBox.Show(String.Format("{0:0.########}", ((double)sw.ElapsedTicks / (double)System.Diagnostics.Stopwatch.Frequency)));*/
            //byte [] hash = md5.ComputeHash(new byte[]{0x20, 0x50, 0x50, 0x50, 0x50, 0x50, 0x50, 0x10});
            byte[] hash = md5.ComputeHash(new byte[] { 0x30, 0x30, 0x30, 0x30, 0x30, 0x30, 0x30, 0x30 });
            string ret = "";
            for (int i = 0; i < hash.Length; i++)
                ret += "0x" + hash[i].ToString("x2").ToLower() + ", " ;
            Log(ret);
        }

        private void button1_Click(object sender, EventArgs e)
        {
            dataGridView2.DataSource = null;
            for (int i = 0; i < statistics.GetItemsCount()-1; i++)
            {

                dataGridView2.Rows.Add();
                dataGridView2.Rows[i].Cells[1].Value = String.Format("{0:0.########}", statistics.GetItemTime(i));
                dataGridView2.Rows[i].Cells[0].Value = String.Format("{0:0.##}", statistics.GetItemCPULoad(i).ToString());
            }
        }

        private void button4_Click(object sender, EventArgs e)
        {
            string ein = "faitchfoolNET";
            string zwei = "";
            for (int i = 0; i < ein.Length; i++)
                zwei += ((byte)ein[i]).ToString("X2");
            Log(zwei);

            md5 = new MD5CryptoServiceProvider();
            byte[] hash = System.Text.Encoding.ASCII.GetBytes("JADE");
            MessageBox.Show(hash[0].ToString("X2"));
            hash = md5.ComputeHash(hash);

            string tmp = "";
            for (int i = 0; i < hash.Length; i++)
                tmp += "0x" + hash[i].ToString("X2") + ", ";

            MessageBox.Show(tmp);
            Log(tmp);
            
        }


    }
}
