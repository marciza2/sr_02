/*
 * Created by SharpDevelop.
 * User: thinkpad
 * Date: 2009-05-01
 * Time: 14:11
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */

using System;
using System.Collections.Generic;
using System.Collections;
using System.Drawing;
using System.Windows.Forms;
using System.Text;
using System.Net.Sockets;
using System.Net;
using System.Threading;
using System.IO;
using System.Xml;
using System.Xml.Serialization;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters;
using System.Runtime.Serialization.Formatters.Binary;
using ZedGraph;

namespace Server_Class
{
	/// <summary>
	/// Description of MainForm.
	/// </summary>
	public partial class MainForm : Form
	{
        LineItem myCurve;
        BarItem myBar;
        private TcpListener tcpListener;
		private Thread listenThread;
		private Thread managerThread;
		private ArrayList clientsInfoList;
		private Mutex managerMutex = new Mutex();
		private volatile int wait_ans_count = 0;
        private volatile int wait_InitData_Ans_count = 0;
        private double Dutch_Act_Time = 0.0;
        private double Dutch_Time_Step = 0.0;
        private Statistics statistics;
        private Random random;
        private UInt32 packSize;
        private CommunicationMode commMode = CommunicationMode.CMFirstPriceAuction;
        private WorkGenerationMode workGenerationMode;
        byte[] searchHash = { 0x39, 0xA2, 0x77, 0x38, 0x5D, 0x32, 0x0E, 0x79, 0x45, 0xCA, 0x03, 0x3F, 0xA4, 0x00, 0x1C, 0x1C };  
        //0xdd, 0x4b, 0x21, 0xe9, 0xef, 0x71, 0xe1, 0x29, 0x11, 0x83, 0xa4, 0x6b, 0x91, 0x3a, 0xe6, 0xf2 };
        private List<UInt32> packList;
        public MainForm()
		{
			InitializeComponent();
            packList = new List<uint>();
            random = new Random();
            workGenerationMode = new WorkGenerationMode(WorkGenerationMode.WGM.WGMConst);
            statistics = new Statistics();
            zedGraphControl1.GraphPane.Title.Text = "Wykres zmiany rozmiaru \"paczki\" danych do obliczeń";
            zedGraphControl1.GraphPane.XAxis.Title.Text = "Numer zlecenia";
            zedGraphControl1.GraphPane.YAxis.Title.Text = "Rozmiar paczki";
            this.clientsInfoList = new ArrayList();
			this.tcpListener =
				new TcpListener(IPAddress.Any, 2240);
			this.listenThread =
				new Thread(new ThreadStart(ListenForClients));
			this.listenThread.Start();
            for (int i = 0; i < Enum.GetValues(typeof(CommunicationMode)).Length; i++)
                workingModeComboBox.Items.Add(Enum.GetNames(typeof(CommunicationMode))[i].Substring(2));
            workingModeComboBox.SelectedIndex = 0;
            for (int i = 0; i < Enum.GetValues(typeof(WorkGenerationMode.WGM)).Length; i++)
                workGenerationModeComboBox.Items.Add(Enum.GetNames(typeof(WorkGenerationMode.WGM))[i].Substring(3));
            workGenerationModeComboBox.SelectedIndex = 0;
		}
        // nasłuchiwanie klientów | oddzielny wątek
        private void ListenForClients()
		{
			this.tcpListener.Start();

			while (true)
			{
				// blokuje wątek aż do podłączenia się klienta
				TcpClient client = this.tcpListener.AcceptTcpClient();
				// tworzy wątek do obsługi komunikacji z klientem
				Thread clientThread =
					new Thread(new
					           ParameterizedThreadStart(HandleClientComm));
				clientThread.Start(client);
				//  

				NetworkStream clientStream = client.GetStream();
				// tworzy wiadomość
				ComMessage message = new ComMessage(ComMessage.MsgTypes.INIT_DATA);
				int [] tmp = { 4, 4, 4};
                MsgContent_InitData initData = new MsgContent_InitData(searchHash, tmp);
                message.txtMsg = "Dane inicjalizacyjne";
                message.SetContentObject(initData);
                message.conversationID = commMode.ToString("G");
                // serializujemy wiadomosc
				byte[] buffer = message.Serialize();
				System.Diagnostics.Debug.WriteLine("Wysłano " + buffer.Length);
                wait_InitData_Ans_count++;
                clientStream.Write(buffer, 0 , buffer.Length);
				clientStream.Flush();
				// dodaje TcpClient do listy klientów
				clientsInfoList.Add(new ClientInfo(client));
				Log(client.Client.RemoteEndPoint.ToString());
			}
		}
        private void HandleClientComm(object client)
		{
			TcpClient tcpClient = (TcpClient)client;
			NetworkStream clientStream = tcpClient.GetStream();
            ClientInfo tmpCl = null;
			byte[] recBuffer = new byte[4096];
			int bytesRead;

			while (true)
			{
				bytesRead = 0;

				try
				{
					// blokuje wątek aż do nadesłania wiadomości przez klienta
					bytesRead = clientStream.Read(recBuffer, 0, recBuffer.Length);
				}
				catch(Exception ex)
				{
					MessageBox.Show(ex.Message, ex.ToString(), MessageBoxButtons.OK, MessageBoxIcon.Error);
					break;
				}

				if (bytesRead == 0)
				{
					foreach (ClientInfo cl in clientsInfoList)
						if(cl.tcpClient == (TcpClient)client)
					{
						clientsInfoList.Remove(cl);
						break;
					}
					//the client has disconnected from the server
					Log("Client has disconnected");
					break;
				}

				//message has successfully been received
				MemoryStream ms = new MemoryStream(recBuffer, 0, bytesRead);
				ms.Position = 0;
				XmlSerializer xmlSer = new XmlSerializer(typeof (ComMessage));
				ComMessage recMessage = (ComMessage)xmlSer.Deserialize(ms);
				switch(recMessage.header)
				{
					case ComMessage.MsgTypes.RESULTS:
                        MsgContent_InitData_Ans msgContent_InitData_Ans_Results = (MsgContent_InitData_Ans)recMessage.GetContentObject();
                        tmpCl = null;
                        foreach (ClientInfo cl in clientsInfoList)
                        {
                            if (cl.tcpClient == tcpClient)
                                tmpCl = cl;
                            break;
                        }
                        statistics.AddItem(msgContent_InitData_Ans_Results, tmpCl);
						break;
					case ComMessage.MsgTypes.SUCCESS:
                        MessageBox.Show("Hash odnaleziony");
                        managerThread.Abort();

                        break;
					case ComMessage.MsgTypes.TIME_ESTIMATED:
                        if (commMode == CommunicationMode.CMFirstPriceAuction ||
                            commMode == CommunicationMode.CMVickreyAuction ||
                            commMode == CommunicationMode.CMSimple ||
                            commMode == CommunicationMode.CMDutchAuction)
                        {
                            lock (this)
                            {
                                wait_ans_count--;
                            }
                            foreach (ClientInfo cl in clientsInfoList)
                                if (cl.tcpClient == (TcpClient)client)
                                {
                                    Object rObj = recMessage.GetContentObject();
                                    cl.statistics.calcTime = (double)rObj;
                                    break;
                                }
                        }
                        else if (commMode == CommunicationMode.CMDutchAuction)
                        { }
						break;
                    case ComMessage.MsgTypes.INIT_DATA_ANS:
                        wait_InitData_Ans_count--;
                        MsgContent_InitData_Ans msgContent_InitData_Ans = (MsgContent_InitData_Ans)recMessage.GetContentObject();
                        tmpCl = null;
                        foreach(ClientInfo cl in clientsInfoList)
                        {
                            if (cl.tcpClient == tcpClient)
                                tmpCl = cl;
                                break;
                        }
                        statistics.AddItem(msgContent_InitData_Ans, tmpCl);
                        break;
					default:
						break;
				}
				/*ASCIIEncoding encoder = new ASCIIEncoding();
				string msg = encoder.GetString(message, 0, bytesRead);
				System.Diagnostics.Debug.WriteLine(msg);
				Log(msg);
				if(msg=="odp")
				 */
			}

			tcpClient.Close();
		}

		public void Log(string text)
		{
			richTextBox1.AppendText(text);
			richTextBox1.AppendText(System.Environment.NewLine);
		}
		public void Log(string text, Color clr)
		{
			richTextBox1.SelectionColor = clr;
			Log(text);
			richTextBox1.SelectionColor = Color.Black;
		}
		public void Log(string text, Color clr, bool Bold)
		{
			richTextBox1.SelectionColor = clr;
			if(Bold)
				richTextBox1.SelectionFont = new Font("Courier New", 9,FontStyle.Bold);
			Log(text);
			richTextBox1.SelectionColor = Color.Black;
			richTextBox1.SelectionFont = new Font("Courier New", 9,FontStyle.Regular);
		}
		void ManageThreadFun()
		{
            DateTime startTime = DateTime.Now;
            byte[] tmp = { 0x00, 0x00, 0x00, 0x00 };// { 0x4A, 0x00, 0x36, 0x36 };// 
            byte[] tmp2 = { 0x00, 0x80, 0x00, 0x00 };// 0xFF, 0xFF, 0xFF, 0xFF }; { 0x4A, 0x41, 0x44, 0x45 };
            BigInteger biData = new BigInteger(tmp, true);
            BigInteger biStart = new BigInteger(tmp2, true);
            bool finish = false;
            while(true)
			{
                packSize = workGenerationMode.GetPackSize();
                if (biData + packSize > biStart)
                {
                    packSize = (uint)(biStart - biData).IntValue()+1;
                    finish = true;
                }
                packList.Add(packSize);
                System.Diagnostics.Debug.WriteLine("PACK SIZE = " + packSize.ToString());
                NetworkStream clientStream;
                List<ClientInfo> tmpClientInfoList;
                byte[] buffer;
				//Thread.Sleep(1000);
                if (commMode == CommunicationMode.CMFirstPriceAuction ||
                    commMode == CommunicationMode.CMVickreyAuction || 
                    commMode == CommunicationMode.CMSimple)
                {
                    tmpClientInfoList = new List<ClientInfo>(); 
                    foreach (ClientInfo cl in clientsInfoList)
                    {
                        clientStream = cl.tcpClient.GetStream();
                        // tworzy wiadomość
                        ComMessage message = new ComMessage(ComMessage.MsgTypes.DATA_TO_ESTIMATE);
                        message.txtMsg = "Ile czasu?";
                        message.conversationID = commMode.ToString("G");
                        MsgContent_Data_To_Estimate msgContent_Data_To_Estimate = new MsgContent_Data_To_Estimate(biData.getBytes().Length, packSize);
                        message.SetContentObject(msgContent_Data_To_Estimate);
                        // serializujemy wiadomosc
                        buffer = message.Serialize();
                        System.Diagnostics.Debug.WriteLine("Wysłano " + buffer.Length);
                        clientStream.Write(buffer, 0, buffer.Length);
                        clientStream.Flush();
                    }
                    lock (this)
                    {
                        wait_ans_count += clientsInfoList.Count;
                    }
                    while (wait_ans_count > 0) ;
                    Log("Odebrałem odpowiedzi od wszystkich 'pracowników'");
                    // wybierz najlepszego;
                    ComMessage ansMessage = new ComMessage(ComMessage.MsgTypes.DATA);
                    ansMessage.txtMsg = "Dane do obliczeń";
                    ansMessage.conversationID = commMode.ToString("G");
                    byte[] temp = biData.getBytes();
                    MsgContent_Data msgContent_Data = new MsgContent_Data(biData, packSize);
                    ansMessage.SetContentObject(msgContent_Data);
                    biData += packSize;
                    /*if (biData > biStart)
                    {
                        //MessageBox.Show("Koniec");
                        System.Diagnostics.Debug.WriteLine("KONIEC DANYCH");
                        // KONIEC DANYCH
                    }*/
                    /*if (workGenerationMode == WorkGenerationMode.WGMRandom)
                    {
                        if (packSize < packSizeStop)
                            packSize += packSizeStep;
                    }*/
                    ClientInfo best;
                    ClientInfo best2; // second price
                    try
                    {
                        if (clientsInfoList.Count > 0)
                        {
                            Thread.Sleep(10);
                            best = (ClientInfo)clientsInfoList[0];
                            best2 = (ClientInfo)clientsInfoList[0];
                            System.Diagnostics.Debug.WriteLine("Lista");
                            foreach (ClientInfo cl in clientsInfoList)
                            {
                                System.Diagnostics.Debug.WriteLine(cl.statistics.calcTime.ToString());                                
                                if (best.statistics.calcTime > cl.statistics.calcTime && cl.statistics.calcTime > -1)
                                    best = cl;
                                if(cl.statistics.calcTime>-1)
                                    tmpClientInfoList.Add(cl);
                            }
                            if(commMode==CommunicationMode.CMVickreyAuction)
                                foreach (ClientInfo cl in clientsInfoList)
                                {
                                    if (best2.statistics.calcTime > cl.statistics.calcTime &&
                                            cl.statistics.calcTime > best.statistics.calcTime)
                                        best2 = cl;
                                }
                            System.Diagnostics.Debug.WriteLine("best = " + best.statistics.calcTime.ToString());
                            System.Diagnostics.Debug.WriteLine("best2 = " + best2.statistics.calcTime.ToString());
                            if (best.statistics.calcTime == -1)
                            {
                                Log("Wszyscy pracownicy zajęci");
                            }
                            else
                            {
                                if (commMode == CommunicationMode.CMFirstPriceAuction)
                                    clientStream = best.tcpClient.GetStream();
                                else if (commMode == CommunicationMode.CMVickreyAuction)
                                    clientStream = best2.tcpClient.GetStream();
                                else
                                    clientStream = tmpClientInfoList[random.Next(tmpClientInfoList.Count)].tcpClient.GetStream();
                                buffer = ansMessage.Serialize();
                                System.Diagnostics.Debug.WriteLine("Wysłano " + buffer.Length);
                                clientStream.Write(buffer, 0, buffer.Length);
                                clientStream.Flush();
                            }
                        }
                    }
                    catch (ArgumentOutOfRangeException ex)
                    {
                        MessageBox.Show("Klient nagle zerwał połączenie\n" +
                            ex.Message, ex.ToString(), MessageBoxButtons.OK, MessageBoxIcon.Error);
                        // klient nagle się odłączył
                    }
                    Thread.Sleep(workGenerationMode.GetDelay());
                    if (finish)
                    {
                        Log("Koniec obliczeń");
                        foreach (ClientInfo cl in clientsInfoList)
                        {
                            clientStream = cl.tcpClient.GetStream();
                            // tworzy wiadomość
                            ComMessage message = new ComMessage(ComMessage.MsgTypes.FINISH);
                            message.conversationID = commMode.ToString("G");
                            // serializujemy wiadomosc
                            buffer = message.Serialize();
                            System.Diagnostics.Debug.WriteLine("Wysłano " + buffer.Length);
                            clientStream.Write(buffer, 0, buffer.Length);
                            clientStream.Flush();
                        }
                        DateTime endTime = DateTime.Now;
                        MessageBox.Show("Czas obliczen: " + (endTime - startTime).ToString());
                        return;
                    }
                    /*if (workGenerationMode == WorkGenerationMode.WGMRandom)
                    {
                        Thread.Sleep(packDelay);
                    }
                    else
                        Thread.Sleep(10);*/
                }
                else if (commMode == CommunicationMode.CMDutchAuction)
                {
                    int nRepos = 0;
                    Dutch_Act_Time = statistics.GetDutchStartValue(biData.getBytes().Length, packSize);
                    Dutch_Time_Step = (Dutch_Act_Time/packSize)*(packSize/10);
                    bool found = true;
                    packSize = workGenerationMode.GetPackSize();
                    while (true)
                    {
                        if(found)
                            packSize = workGenerationMode.GetPackSize();
                        if (biData + packSize > biStart)
                        {
                            packSize = (uint)(biStart - biData).IntValue()+1;
                            finish = true;
                        }
                        tmpClientInfoList = new List<ClientInfo>(); 
                        // wysyłanie do wszystkich o podanie czasu
                        foreach (ClientInfo cl in clientsInfoList)
                        {
                            clientStream = cl.tcpClient.GetStream();
                            // tworzy wiadomość
                            ComMessage message = new ComMessage(ComMessage.MsgTypes.DUTCH_TIME_TO_DO);
                            message.txtMsg = "Kto obliczy w tyle czasu?";
                            message.conversationID = commMode.ToString("G");
                            MsgContent_Dutch_Time_To_Do msgContent_Dutch_Time_To_Do = new MsgContent_Dutch_Time_To_Do(biData.getBytes().Length, packSize, Dutch_Act_Time);
                            message.SetContentObject(msgContent_Dutch_Time_To_Do);
                            // serializujemy wiadomosc
                            buffer = message.Serialize();
                            System.Diagnostics.Debug.WriteLine("Wysłano " + buffer.Length);
                            clientStream.Write(buffer, 0, buffer.Length);
                            clientStream.Flush();
                        }
                        lock (this)
                        {
                            wait_ans_count += clientsInfoList.Count;
                        }
                        // bierne czekanie na wszystkie odpowiedzi;
                        while (wait_ans_count > 0) ;
                        Log("Odebrałem odpowiedzi od wszystkich 'pracowników'");
                        // czy którykolwiek spełnił kryteria;
                        ClientInfo best = (ClientInfo) clientsInfoList[0];
                        foreach (ClientInfo cl in clientsInfoList)
                        {
                            if (cl.statistics.calcTime > 0)
                                tmpClientInfoList.Add(cl);
                            if (best.statistics.calcTime > cl.statistics.calcTime && 
                                cl.statistics.calcTime > -1)
                                    best = cl;
                        }
                        ComMessage ansMessage = new ComMessage(ComMessage.MsgTypes.DATA);
                        ansMessage.txtMsg = "Dane do obliczeń";
                        ansMessage.conversationID = commMode.ToString("G");
                        byte[] temp = biData.getBytes();
                        MsgContent_Data msgContent_Data = new MsgContent_Data(biData, packSize);
                        ansMessage.SetContentObject(msgContent_Data);
                        if (found)
                        {
                            biData += packSize;
                            found = false;
                        }
                            /*if (biData > biStart)
                        {
                            MessageBox.Show("Koniec");
                            // KONIEC DANYCH
                        }*/
                        if(tmpClientInfoList.Count==0)
                        {
                            if (best.statistics.calcTime == -1)
                            {
                                Log("Wszyscy pracownicy zajęci");
                                break;
                            }
                            else if(best.statistics.calcTime == 0)
                            {
                                Log("Żaden pracownik nie wykona zadania w podanym czasie");
                                Log("Zwiększam przewidywany czas");
                                Dutch_Act_Time+=Dutch_Time_Step;
                                nRepos++;
                                if (nRepos > 10)
                                    Dutch_Time_Step *= 2;
                            }
                        }
                        else
                        {
                            found = true;
                            Log("Wysyłam zadanie do pracownika");
                            best = tmpClientInfoList[random.Next(tmpClientInfoList.Count)];
                            clientStream = best.tcpClient.GetStream();
                            buffer = ansMessage.Serialize();
                            System.Diagnostics.Debug.WriteLine("Wysłano " + buffer.Length);
                            clientStream.Write(buffer, 0, buffer.Length);
                            clientStream.Flush();
                            //break;
                        }
                        if (finish)
                        {
                            Log("Koniec obliczeń");
                            foreach (ClientInfo cl in clientsInfoList)
                            {
                                clientStream = cl.tcpClient.GetStream();
                                // tworzy wiadomość
                                ComMessage message = new ComMessage(ComMessage.MsgTypes.FINISH);
                                message.conversationID = commMode.ToString("G");
                                // serializujemy wiadomosc
                                buffer = message.Serialize();
                                System.Diagnostics.Debug.WriteLine("Wysłano " + buffer.Length);
                                clientStream.Write(buffer, 0, buffer.Length);
                                clientStream.Flush();
                            }
                            DateTime endTime = DateTime.Now;
                            MessageBox.Show("Czas obliczen: " + (endTime - startTime).ToString());
                            return;
                        }
                    }
                }
			}
		}
		
		void MainFormFormClosing(object sender, FormClosingEventArgs e)
		{
			if(managerThread!=null)
				if(managerThread.IsAlive)
				managerThread.Abort();
			listenThread.Abort();
		}
		
		void ButtonStartClick(object sender, EventArgs e)
		{
			this.managerThread = new Thread(new ThreadStart(ManageThreadFun));
			this.managerThread.Start();
            
		}

        private void button2_Click(object sender, EventArgs e)
        {
            //byte[] integer = { 0x10, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 };
            byte[] integer = { 0xFF, 0x00, 0x11, 0x22, 0x33, 0x44, 0x55, 0x66, 0x77, 0x88 };
            BigInteger bi = new BigInteger(integer, true);
            MessageBox.Show(bi.getBytes().Length.ToString());
            MessageBox.Show(bi.ToHexString());
        }

        private void button1_Click(object sender, EventArgs e)
        {
            MessageBox.Show(CommunicationMode.CMDutchAuction.ToString());
        }

        private void workingModeComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            commMode = (CommunicationMode)(workingModeComboBox.SelectedIndex);
        }

        private void workGenerationModeComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            workGenerationMode = new WorkGenerationMode((WorkGenerationMode.WGM)workGenerationModeComboBox.SelectedIndex);
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            GraphPane myPane = zedGraphControl1.GraphPane;
            PointPairList list = new PointPairList();
            if (packList.Count > 300)
                while (packList.Count > 300)
                    packList.RemoveAt(0);
            for(int i=0; i<packList.Count; i++)
            {
                list.Add((double)(i+1), (double)packList[i]);
            }
            if (myCurve != null)
                myCurve.Clear();
            /*if (myBar != null)
                myBar.Clear();
            //myBar = myPane.AddBar("", list, Color.Green);*/
            myCurve = myPane.AddCurve("",
                  list, Color.GreenYellow, SymbolType.Diamond);
            //myCurve.Symbol.Fill = new Fill(Color.Green);
            myCurve.Symbol.Type = SymbolType.None;
            myCurve.Line.Fill = new Fill(Color.GreenYellow, Color.Green, 45F);
            //myBar.Bar.Border.Width = 20;
            zedGraphControl1.AxisChange();
            zedGraphControl1.Refresh();
        }
	}
}
