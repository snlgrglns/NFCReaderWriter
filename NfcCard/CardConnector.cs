using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NfcCard
{
    public class CardConnector
    {
        bool pinSubmission = true;
        bool amountSubmission = true;
        bool loyaltyClaiming = true;

        int retCode;
        int hCard;
        int hContext;
        int Protocol;
        public bool connActive = false;
        public bool autoDet;
        string sCard = "ACS ACR122 0";      // change depending on reader
        public byte[] SendBuff = new byte[263];
        public byte[] RecvBuff = new byte[263];
        public int SendLen, RecvLen, nBytesRet, reqType, Aprotocol, dwProtocol, cbPciLength;
        public Card.SCARD_READERSTATE RdrState;
        public Card.SCARD_IO_REQUEST pioSendRequest;


        private void authBlock(String s)
        {
            ClearBuffers();
            SendBuff[0] = 0xFF;                         // CLA
            SendBuff[2] = 0x00;                         // P1: same for all source types 
            SendBuff[1] = 0x86;                         // INS: for stored key input
            SendBuff[3] = 0x00;                         // P2 : Memory location;  P2: for stored key input
            SendBuff[4] = 0x05;                         // P3: for stored key input
            SendBuff[5] = 0x01;                         // Byte 1: version number
            SendBuff[6] = 0x00;                         // Byte 2
            SendBuff[7] = (byte)int.Parse(s);           // Byte 3: sectore no. for stored key input
            SendBuff[8] = 0x60;                         // Byte 4 : Key A for stored key input
            SendBuff[9] = (byte)int.Parse("1");         // Byte 5 : Session key for non-volatile memory

            SendLen = 0x0A;
            RecvLen = 0x02;

            retCode = SendAPDUandDisplay(0);

            if (retCode != Card.SCARD_S_SUCCESS)
            {
                Console.Out.WriteLine("FAIL Authentication!");
                return;
            }
        }

        private void ClearBuffers()
        {
            long indx;

            for (indx = 0; indx <= 262; indx++)
            {
                RecvBuff[indx] = 0;
                SendBuff[indx] = 0;
            }
        }

        private long getIntValues(String s)
        {
            long lVal = 0;

            ClearBuffers();
            SendBuff[0] = 0xFF;                           // CLA     
            SendBuff[1] = 0xB1;                           // INS
            SendBuff[2] = 0x00;                           // P1
            SendBuff[3] = (byte)int.Parse(s);             // P2 : Block No.
            SendBuff[4] = 0x00;                           // Le

            SendLen = 0x05;
            RecvLen = 0x06;

            retCode = SendAPDUandDisplay(2);

            if (retCode != Card.SCARD_S_SUCCESS)
            {
                Console.Out.WriteLine("FAIL!");
            }

            lVal = RecvBuff[3];
            lVal = lVal + (RecvBuff[2] * 256);
            lVal = lVal + (RecvBuff[1] * 256 * 256);
            lVal = lVal + (RecvBuff[0] * 256 * 256 * 256);

            return (lVal);
        }


        private int SendAPDUandDisplay(int reqType)
        {
            int indx;
            string tmpStr = "";

            pioSendRequest.dwProtocol = Aprotocol;
            pioSendRequest.cbPciLength = 8;

            // Display Apdu In
            for (indx = 0; indx <= SendLen - 1; indx++)
            {
                tmpStr = tmpStr + " " + string.Format("{0:X2}", SendBuff[indx]);
            }

            retCode = Card.SCardTransmit(hCard, ref pioSendRequest, ref SendBuff[0],
                                 SendLen, ref pioSendRequest, ref RecvBuff[0], ref RecvLen);

            if (retCode != Card.SCARD_S_SUCCESS)
            {
                return retCode;
            }

            else
            {
                tmpStr = "";
                switch (reqType)
                {
                    case 0:
                        for (indx = (RecvLen - 2); indx <= (RecvLen - 1); indx++)
                        {
                            tmpStr = tmpStr + " " + string.Format("{0:X2}", RecvBuff[indx]);
                        }

                        if ((tmpStr).Trim() != "90 00")
                        {
                            Console.Out.WriteLine("Return bytes are not acceptable.");
                        }

                        break;

                    case 1:

                        for (indx = (RecvLen - 2); indx <= (RecvLen - 1); indx++)
                        {
                            tmpStr = tmpStr + string.Format("{0:X2}", RecvBuff[indx]);
                        }

                        if (tmpStr.Trim() != "90 00")
                        {
                            tmpStr = tmpStr + " " + string.Format("{0:X2}", RecvBuff[indx]);
                        }

                        else
                        {
                            tmpStr = "ATR : ";
                            for (indx = 0; indx <= (RecvLen - 3); indx++)
                            {
                                tmpStr = tmpStr + " " + string.Format("{0:X2}", RecvBuff[indx]);
                            }
                        }

                        break;

                    case 2:

                        for (indx = 0; indx <= (RecvLen - 1); indx++)
                        {
                            tmpStr = tmpStr + " " + string.Format("{0:X2}", RecvBuff[indx]);
                        }

                        break;
                }
            }
            return retCode;
        }

        public bool connectCard()
        {
            connActive = true;
            retCode = Card.SCardEstablishContext(Card.SCARD_SCOPE_USER, 0, 0, ref hContext);

            retCode = Card.SCardConnect(hContext, sCard, Card.SCARD_SHARE_SHARED,
                      Card.SCARD_PROTOCOL_T0 | Card.SCARD_PROTOCOL_T1, ref hCard, ref Protocol);

            if (retCode != Card.SCARD_S_SUCCESS)
            {
                Console.Out.WriteLine("Please Insert Card");
                connActive = false;
            }

            if (retCode == Card.SCARD_S_SUCCESS)
            {
                Console.Out.WriteLine("Card has been read");
            }
            return connActive;
        }

        public void submitText(String Text, String Block)
        {

            String tmpStr = Text;
            int indx;
            authBlock(Block);
            ClearBuffers();
            SendBuff[0] = 0xFF;                             // CLA
            SendBuff[1] = 0xD6;                             // INS
            SendBuff[2] = 0x00;                             // P1
            SendBuff[3] = (byte)int.Parse(Block);           // P2 : Starting Block No.
            SendBuff[4] = (byte)int.Parse("16");            // P3 : Data length

            for (indx = 0; indx <= (tmpStr).Length - 1; indx++)
            {
                SendBuff[indx + 5] = (byte)tmpStr[indx];
            }
            SendLen = SendBuff[4] + 5;
            RecvLen = 0x02;

            retCode = SendAPDUandDisplay(2);

            if (retCode != Card.SCARD_S_SUCCESS)
            {
                Console.Out.WriteLine("fail write");
            }
            else
                Console.Out.WriteLine("write success");
        }

        public string readBlock(String Block)
        {
            string tmpStr = "";
            int indx;
            authBlock(Block);
            ClearBuffers();
            SendBuff[0] = 0xFF;
            SendBuff[1] = 0xB0;
            SendBuff[2] = 0x00;
            SendBuff[3] = (byte)int.Parse(Block);
            SendBuff[4] = (byte)int.Parse("16");

            SendLen = 5;
            RecvLen = SendBuff[4] + 2;

            retCode = SendAPDUandDisplay(2);

            if (retCode != Card.SCARD_S_SUCCESS)
            {
                Console.Out.WriteLine("fail read");
            }

            // Display data in text format
            for (indx = 0; indx <= RecvLen - 1; indx++)
            {
                tmpStr = tmpStr + Convert.ToChar(RecvBuff[indx]);
            }

            return (tmpStr);
        }

        public void Close()
        {
            if (connActive)
            {
                retCode = Card.SCardDisconnect(hCard, Card.SCARD_UNPOWER_CARD);
            }

            retCode = Card.SCardReleaseContext(hCard);
        }

        public void clearBlock(string startBlock, string endBlock)
        {
            int start = int.Parse(startBlock);
            int end = int.Parse(endBlock);
            string spaces = "                ";

            for (int i = start; i <= end; i++)
            {
                if ((i + 1) % 4 != 0)
                {
                    this.submitText(spaces, i.ToString());
                }
            }
        }
    }
}
