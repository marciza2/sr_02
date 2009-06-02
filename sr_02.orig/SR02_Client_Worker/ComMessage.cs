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
    /// Communication message class.
    /// </summary>
	[Serializable]
	[XmlRootAttribute("ComMessage", Namespace="", IsNullable=false)]
    [XmlInclude(typeof(MsgContent_InitData))]
    [XmlInclude(typeof(MsgContent_Data))]
    [XmlInclude(typeof(MsgContent_Data_To_Estimate))]
    [XmlInclude(typeof(MsgContent_InitData_Ans))]
    public class ComMessage
	{
        /// <summary>
        /// 
        /// </summary>
		public enum MsgTypes: byte {
            /// <summary>
            /// 
            /// </summary>
			INIT_DATA,			/* Dane inicjalizacyjne */
            /// <summary>
            /// 
            /// </summary>
            INIT_DATA_ANS,		/* Odpowiedź z wynikami testu */
            /// <summary>
            /// 
            /// </summary>
            DATA_TO_ESTIMATE,	/* Dane do oszacowania czasu obliczeń */
            /// <summary>
            /// 
            /// </summary>
		    TIME_ESTIMATED,		/* Oszacowany czas */
            /// <summary>
            /// 
            /// </summary>
			GENERAL_MSG,		/* Ogólna wiadomość - niezwiązana z obliczeniami */
            /// <summary>
            /// 
            /// </summary>
			RESULTS,			/* Zwracane wyniki */
            /// <summary>
            /// 
            /// </summary>
			SUCCESS,			/* Zwracane wyniki - osiągnięto cel */
            /// <summary>
            /// 
            /// </summary>
			DATA,				/* Dane do obliczeń */
            /// <summary>
            /// 
            /// </summary>
            FINISH              /* Koniec obliczeń */
        };
        /// <summary>
        /// Header
        /// </summary>
		public MsgTypes header;
        /// <summary>
        /// Content object
        /// </summary>
        public Object contentObject;
        /// <summary>
        /// Text message
        /// </summary>
		public string txtMsg;
        /// <summary>
        /// Conversation ID
        /// </summary>
        public string conversationID;
        /// <summary>
        /// Initializes a new instance of the <see cref="ComMessage"/> class.
        /// </summary>
		public ComMessage()
		{
		}
        /// <summary>
        /// Initializes a new instance of the <see cref="ComMessage"/> class.
        /// </summary>
        /// <param name="mt">The mt.</param>
		public ComMessage(MsgTypes mt)
		{
			header = mt;
		}
        /// <summary>
        /// Sets the content object.
        /// </summary>
        /// <param name="obj">The obj.</param>
		public void SetContentObject(Object obj)
		{
			this.contentObject = obj;			
		}
        /// <summary>
        /// Gets the content object.
        /// </summary>
        /// <returns></returns>
		public Object GetContentObject()
		{
			return this.contentObject;
		}
        /// <summary>
        /// Serializes this instance.
        /// </summary>
        /// <returns></returns>
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
        /// <summary>
        /// Deserializes the specified data.
        /// </summary>
        /// <param name="data">The data.</param>
		public void Deserialize(byte [] data)
		{
			/*MemoryStream ms = new MemoryStream(data);
			XmlSerializer xmlSer = new XmlSerializer(typeof(ComMessage));
			this = (ComMessage)xmlSer.Deserialize(ms);*/
		}
	}
}
