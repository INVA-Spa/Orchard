﻿using System;
using System.Collections.Generic;
using System.Web;
using log4net;
using System.Xml;
using ChartaDb.ChartaTableAdapters;
using System.Text;
using System.Data;
using ChartaDb;


namespace ChartaWEB
{
    public class GestioneDeviceToken
    {
        private static readonly ILog logger = LogManager.GetLogger("GestioneDeviceToken");


        public static string RegistraToken (string token, string tipoDevice, string prod )
        {
            try
            {
                string sReturn = string.Empty;

                byte tipoDeviceByte = 0;
                switch (tipoDevice)
                {
                    case "ios":
                        tipoDeviceByte = 1;
                        break;
                    case "android":
                        tipoDeviceByte = 2;
                        break;                
                }

                bool isprod;
                switch (prod)
                {
                    case "0":
                        isprod = false;
                        break;
                    case "1":
                        isprod = true;
                        break;
                    default:
                        isprod = false;
                        break;

                }

                DEVICE_TOKENTableAdapter ta = new DEVICE_TOKENTableAdapter();
                ChartaDb.Charta.DEVICE_TOKENRow[] dr1 = (ChartaDb.Charta.DEVICE_TOKENRow[])ta.GetData().Select("DEVICE_TOKEN = '" + token + "' ");
                if (dr1.Length == 0)
                {                    
                    ta.Insert(token, tipoDeviceByte, true, isprod, DateTime.Now, null);
                }
                else
                {
                    ChartaDb.Charta.DEVICE_TOKENRow row = dr1[0];
                    row.VALIDO = true;
                    row.DATA_AGGIORNAMENTO = DateTime.Now;
                    row.ISPROD = isprod;
                    ta.Update(row);
                }


                sReturn += "<reply>";
                sReturn += "<Esito>OK</Esito>";
                sReturn += "</reply>";

                ta.Dispose();

                return sReturn;
            }
            catch (Exception ex)
            {
                logger.Error(ex);
                return Util.GestioneErrore("RegistraToken", "999", ex.Message);
            }
            

        }


        public static string DelRegistraToken(string token, string tipoDevice, string prod)
        {
            try
            {
                string sReturn = string.Empty;

                PROFILAZIONETableAdapter taP = new PROFILAZIONETableAdapter();
                taP.DeleteTokenProfilazioni(token);

                DEVICE_TOKENTableAdapter ta = new DEVICE_TOKENTableAdapter();
                ta.UpdateValiditaToken(false, DateTime.Now, token); 
               /* ChartaDb.Charta.DEVICE_TOKENRow[] dr1 = (ChartaDb.Charta.DEVICE_TOKENRow[])ta.GetData().Select("DEVICE_TOKEN = '" + token + "' ");
                if (dr1.Length != 0)
                {
                    ChartaDb.Charta.DEVICE_TOKENRow row = dr1[0];
                    row.VALIDO = false ;
                    row.DATA_AGGIORNAMENTO = DateTime.Now;
                    ta.Update(row);
                }*/


                sReturn += "<reply>";
                sReturn += "<Esito>OK</Esito>";
                sReturn += "</reply>";

                ta.Dispose();

                return sReturn;
            }
            catch (Exception ex)
            {
                logger.Error(ex);
                return Util.GestioneErrore("RegistraToken", "999", ex.Message);
            }


        }

        /*public static string GetNumeroServizio ()
        {
            try
            {
                string sReturn = string.Empty;

                SupportTableAdapter tableAdapterSupport = new SupportTableAdapter();
                DataRow[] dr1 = tableAdapterSupport.GetData().Select("key = 'NumeroServizio' ");
                string svalue = dr1[0]["value"].ToString();
                sReturn += "<reply>";
                sReturn += "<NumeroServizio>" + Util.Convert(svalue) + "</NumeroServizio>";
                sReturn += "</reply>";

                tableAdapterSupport.Dispose();


                return sReturn;

            }
            catch (Exception ex)
            {
                logger.Error(ex);
                return Util.GestioneErrore("GetNumeroServizio", "999", ex.Message);
            }

        
        }*/

    }
}
