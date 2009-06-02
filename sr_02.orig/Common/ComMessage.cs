/*
 * Created by SharpDevelop.
 * User: thinkpad
 * Date: 2009-05-01
 * Time: 14:43
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */

using System;
using System.IO;
using System.Xml;
using System.Xml.Serialization;

namespace Server_Class
{
	/// <summary>
	/// Description of Message.
	/// </summary>
	[Serializable]
	[XmlRootAttribute("ComMessage", Namespace="", IsNullable=false)]
    [XmlInclude(typeof(MsgContent_InitData))]
    [XmlInclude(typeof(MsgContent_Data))]
    [XmlInclude(typeof(MsgContent_Data_To_Estimate))]
    [XmlInclude(typeof(MsgContent_InitData_Ans))]
    public class ComMessage
	{
		public enum MsgTypes: byte {
			INIT_DATA,			/* Dane inicjalizacyjne */
            INIT_DATA_ANS,		/* Odpowiedź z wynikami testu */
            DATA_TO_ESTIMATE,	/* Dane do oszacowania czasu obliczeń */
		    TIME_ESTIMATED,		/* Oszacowany czas */
			GENERAL_MSG,		/* Ogólna wiadomość - niezwiązana z obliczeniami */
			RESULTS,			/* Zwracane wyniki */
			SUCCESS,			/* Zwracane wyniki - osiągnięto cel */
			DATA,				/* Dane do obliczeń */
            FINISH              /* Koniec obliczeń */
        };		
		public MsgTypes header;
        public Object contentObject;
		public string txtMsg;
        public string conversationID;
		public ComMessage()
		{
		}
		public ComMessage(MsgTypes mt)
		{
			header = mt;
		}
		public void SetContentObject(Object obj)
		{
			this.contentObject = obj;			
		}
		public Object GetContentObject()
		{
			return this.contentObject;
		}
		public byte[] Serialize()
		{
			MemoryStream ms = new MemoryStream();
			ms.Position = 0;
			XmlSerializer xmlSer = new XmlSerializer(typeof(ComMessage));
            try
            {
                xmlSer.Serialize(ms, this);
            }
            catch (InvalidOperationException ex)
            {
                System.Windows.Forms.MessageBox.Show(ex.InnerException.Message);
            }
			return ms.ToArray();						
		}
		public void Deserialize(byte [] data)
		{
			/*MemoryStream ms = new MemoryStream(data);
			XmlSerializer xmlSer = new XmlSerializer(typeof(ComMessage));
			this = (ComMessage)xmlSer.Deserialize(ms);*/
		}
	}
}
